namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using global::Radzen;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddWashuFramework<TDbContext>(WebApplicationBuilder builder)
        where TDbContext : ApplicationDbContext
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddExceptionHandler<AntiforgeryExceptionHandler>();
            services.AddAuthorization();
            // Rate limiting
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(RateLimiterPolicy.AuthLimiter, opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromSeconds(30);
                });
            });
            // Radzen
            services.AddRadzenCookieThemeService(options =>
            {
                options.Name = CookieConstants.Names.Theme;
                options.Duration = TimeSpan.FromDays(CookieConstants.DurationInDays.Theme);
            });
            
            // Identity
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddScoped<ExternalEndpoints<TDbContext>>();
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
                var tzCookie = httpContext?.Request.Cookies[CookieConstants.Names.ClientTimeZone];
                try
                {
                    var instance = new UserTimeZoneProvider { TimeZoneId = !string.IsNullOrEmpty(tzCookie) ? tzCookie : "UTC" };
                    // Check if it is valid
                    _ = instance.TimeZoneInfo;
                    return instance;
                }
                catch (TimeZoneNotFoundException)
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
                    // Registers the missing 'Identity.External' scheme
                    options.Cookie.Name = ".Washu.External";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                })
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromDays(CookieConstants.DurationInDays.Application);
                    options.SlidingExpiration = true;
                    options.LoginPath = ApplicationRoutes.SignIn;
                    options.Cookie.Name = CookieConstants.Names.Application;
                })
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration.GetSection("ExternalProviders:Google:ClientId").Value!;
                    options.ClientSecret = builder.Configuration.GetSection("ExternalProviders:Google:ClientSecret").Value!;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                });
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRadzenComponents();
            
            return services;
        }
    }

    extension(WebApplication app)
    {
        public async Task SeedRolesAsync<TDbContext>(string[] roles)
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
            app.MapPost(CoreEndpointRoutes.Theme, (HttpContext context, [FromForm] string theme) =>
                {
                    context.Response.Cookies.Delete(CookieConstants.Names.Theme);
                    context.Response.Cookies.Append(
                        CookieConstants.Names.Theme,
                        theme,
                        new CookieOptions
                        {
                            Secure = true,
                            Expires = DateTimeOffset.Now.AddDays(CookieConstants.DurationInDays.Theme),
                            SameSite = SameSiteMode.Lax
                        });
    
                    return TypedResults.Redirect(ApplicationRoutes.Settings);
                }).RequireRateLimiting(RateLimiterPolicy.AuthLimiter)
                .RequireAuthorization();
            scope.ServiceProvider.GetRequiredService<ExternalEndpoints<TDbContext>>().Map(app);
            app.MapPost(CoreEndpointRoutes.Identity.DeleteAccount, async (MessageStore store, HttpContext context, ApplicationUserManager<TDbContext> manager, [FromForm] string username) =>
            {
                var message = await manager.DeleteByUsernameAsync(username);
                var id = store.Store(message);
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
            });
            app.MapPost(CoreEndpointRoutes.Identity.SignOut, async (HttpContext context) =>
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}");
            });
            app.MapPost(CoreEndpointRoutes.Identity.ChangeUsername, async (MessageStore store, HttpContext context, ApplicationUserManager<TDbContext> manager, [FromForm] ChangeUsernameModel model) =>
            {
                var message = await manager.ChangeUsernameAsync(model);
                var id = store.Store(message);
                if (message.Title.StartsWith("Invalid") || message.Type is MessageType.Info) return TypedResults.Redirect($"{ApplicationRoutes.ManageAccount}?messageId={id}");
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
            });
        }
    }
}