namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using global::Radzen;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifications;
using System.Security.Claims;

public static class ConfigurationExtensions
{
    private const ushort ThemeCookieDuration = 365;
    extension(IServiceCollection services)
    {
        public void AddWashuFramework<TDbContext>(WebApplicationBuilder builder)
        where TDbContext : ApplicationDbContext
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<CircuitState>();
            builder.Services.AddSingleton<UserSessionManager>();
            services.AddAuthorization();
            // Rate limiting
            services.AddRateLimiter(options =>
            {
                options.AddPolicy(RateLimiterPolicy.StandardRateLimiter, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(30)
                        }));
            });
            builder.Services.AddHealthChecks().AddDbContextCheck<TDbContext>(name: "DB");
            // Radzen
            services.AddRadzenCookieThemeService(options =>
            {
                options.Name = CookieNames.Theme;
                options.Duration = TimeSpan.FromDays(ThemeCookieDuration);
            });
            
            // Identity
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory<TDbContext>>();
            services.AddScoped<ApplicationSignInManager<TDbContext>>();
            services.AddScoped<ApplicationUserManager<TDbContext>>();
            services.AddScoped<ApplicationRoleManager<TDbContext>>();
            services.AddScoped<ApplicationDbContext, TDbContext>();
            services.AddDbContextFactory<TDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        // Enable default resilient retries
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,                  // Default is 6
                            maxRetryDelay: TimeSpan.FromSeconds(30), // Max delay between retries
                            errorCodesToAdd: null              // Additional SQL/error codes if needed
                        )));
            services.AddMemoryCache();
            services.AddSingleton<MessageStore>();
            // Time zone
            services.AddScoped<UserTimeZoneProvider>(sp =>
            {
                var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
                var tzCookie = httpContext?.Request.Cookies[CookieNames.ClientTimeZone];
                try
                {
                    var instance = new UserTimeZoneProvider { TimeZoneId = !string.IsNullOrEmpty(tzCookie) ? tzCookie : "UTC" };
                    // Check if it is valid
                    _ = instance.TimeZoneInfo;
                    return instance;
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    return new UserTimeZoneProvider();
                }
            });
            
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider<TDbContext>>();
            
            // Authentication and cookies
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddCookie(IdentityConstants.ExternalScheme, options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.Cookie.Name = ".Washu.External";
                })
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromDays(14);
                    options.SlidingExpiration = true;
                    options.LoginPath = ApplicationRoutes.SignIn;
                    options.Cookie.Name = ".Washu.Application";
                    options.Events.OnRedirectToLogin = context =>
                    {
                        if (!HttpMethods.IsGet(context.Request.Method))
                        {
                            // Grab the page they were actually standing on when they submitted the form
                            var referer = context.Request.Headers.Referer.FirstOrDefault();

                            var safeReturnUrl = !string.IsNullOrWhiteSpace(referer) 
                                                   && Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                                ? uri.LocalPath
                                : ApplicationRoutes.Home;
                            // Overwrite the ReturnUrl parameter so it points to a GET page route
                            context.RedirectUri = $"{options.LoginPath}?ReturnUrl={safeReturnUrl}";
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (!Guid.TryParse(userIdText, out var userId))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(
                                IdentityConstants.ApplicationScheme);
                            return;
                        }

                        var factory = context.HttpContext.RequestServices
                            .GetRequiredService<IDbContextFactory<TDbContext>>();
                        var optionsAccessor = context.HttpContext.RequestServices.GetRequiredService<IOptions<IdentityOptions>>();
                        await using var db = await factory.CreateDbContextAsync();
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);

                        var stampType = optionsAccessor.Value.ClaimsIdentity.SecurityStampClaimType;
                        if (context.Principal is null)
                            throw new InvalidOperationException("Claims Principal cannot be null");
                        var ticketStamp = context.Principal
                            .FindFirstValue(stampType);

                        if (user is null ||
                            !Guid.TryParse(ticketStamp, out var stamp) ||
                            stamp != user.SecurityStamp)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(
                                IdentityConstants.ApplicationScheme);
                        }
                    };
                })
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration.GetSection("ExternalProviders:Google:ClientId").Value!;
                    options.ClientSecret = builder.Configuration.GetSection("ExternalProviders:Google:ClientSecret").Value!;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    
                    options.Events.OnRemoteFailure = context =>
                    {
                        using var scope = context.HttpContext.RequestServices.CreateScope();
                        var messageStore = scope.ServiceProvider.GetRequiredService<MessageStore>();
                        var id = messageStore.Store(Message.Error("External Identity Error",
                            "There was a problem signing in your account, please try again"));
                        context.Response.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRadzenComponents();
        }
    }

    extension(WebApplication app)
    {
        public async Task SeedRolesAsync<TDbContext>(List<string> roles)
            where TDbContext : ApplicationDbContext
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<ApplicationRoleManager<TDbContext>>();
            await roleManager.AddRolesAsync(roles);
        }
        public void UseWashuFramework<TDbContext>()
        where TDbContext : ApplicationDbContext
        {
            using var scope = app.Services.CreateScope();
            app.UseExceptionHandler(ApplicationRoutes.Error, createScopeForErrors: true);
            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStatusCodePagesWithReExecute(ApplicationRoutes.NotFound, createScopeForStatusCodePages: true);
            app.UseRateLimiter();
            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
        
                    var json = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            durationMs = e.Value.Duration.TotalMilliseconds
                        }),
                        totalDurationMs = report.TotalDuration.TotalMilliseconds
                    };

                    await context.Response.WriteAsJsonAsync(json);
                }
            });
            app.MapPost("/theme", (HttpContext context, [FromForm] string theme) =>
            {
                var cookie = context.Request.Cookies[CookieNames.Theme];
                if ((cookie is not null && cookie == theme) || Themes.Free.All(t => t.Value != theme)) return TypedResults.Redirect(ApplicationRoutes.Settings);
                context.Response.Cookies.Delete(CookieNames.Theme);
                context.Response.Cookies.Append(
                    CookieNames.Theme,
                    theme,
                    new CookieOptions
                    {
                        Secure = true,
                        Expires = DateTimeOffset.Now.AddDays(ThemeCookieDuration),
                        SameSite = SameSiteMode.Lax
                    });
                return TypedResults.Redirect(ApplicationRoutes.Settings);
            }).RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
            ExternalEndpoints.Map<TDbContext>(app);
            app.MapPost(IdentityRoutes.DeleteAccount, async (MessageStore store, HttpContext context,
                ApplicationUserManager<TDbContext> manager, UserSessionManager sessionManager, [FromForm] Guid circuitId) =>
            {
                var username = context.User.Identity?.Name;
                if (username is null)
                {
                    var errorId = store.Store(Message.Error("Identity Error","Invalid session, or user does not exist"));
                    return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={errorId}");
                }
                var message = await manager.DeleteByUsernameAsync(username);
                var id = store.Store(message);
                if (message.Type != MessageType.Success) return TypedResults.Redirect($"{ApplicationRoutes.ManageAccount}?messageId={id}");
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                if (context.User.FindUserId() is { } userGuid)
                    context.Response.OnCompleted(
                        static async state =>
                        {
                            var (sessionManager, userId, circuitId) =
                                ((UserSessionManager, Guid, Guid))state;

                            await sessionManager.InvalidateAsync(userId, circuitId);
                        },
                        (sessionManager, userGuid, circuitId));
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
            }).RequireAuthorization();
            app.MapPost(IdentityRoutes.SignOut, async (HttpContext context, UserSessionManager sessionManager, [FromForm] Guid circuitId) =>
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                if (context.User.FindUserId() is { } userGuid)
                    context.Response.OnCompleted(
                        static async state =>
                        {
                            var (sessionManager, userId, circuitId) =
                                ((UserSessionManager, Guid, Guid))state;

                            await sessionManager.InvalidateAsync(userId, circuitId);
                        },
                        (sessionManager, userGuid, circuitId));
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}");
            }).RequireAuthorization();
            
            app.MapPost(IdentityRoutes.ChangeUsername, async (MessageStore store, 
                HttpContext context,
                ApplicationUserManager<TDbContext> manager, UserSessionManager sessionManager, [FromForm] Guid circuitId, [FromForm] string newUsername) =>
            {
                var oldUsername = context.User.Identity?.Name;
                var userId = context.User.FindUserId();
                if (oldUsername is null || userId is not {} userGuid)
                {
                    var errorId = store.Store(Message.Error("Identity Error","Invalid session, or user does not exist"));
                    return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={errorId}");
                }
                var message = await manager.ChangeUsernameAsync(oldUsername, newUsername, userGuid);
                var id = store.Store(message.CoreMessage);
                if (!message.RedirectToSignIn) return TypedResults.Redirect($"{ApplicationRoutes.ManageAccount}?messageId={id}");
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                context.Response.OnCompleted(
                    static async state =>
                    {
                        var (sessionManager, userId, circuitId) =
                            ((UserSessionManager, Guid, Guid))state;

                        await sessionManager.InvalidateAsync(userId, circuitId);
                    },
                    (sessionManager, userGuid, circuitId));
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
                
            }).RequireAuthorization();
            app.MapGet("/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                return Results.Text(tokens.RequestToken);
            });
        }
    }
}