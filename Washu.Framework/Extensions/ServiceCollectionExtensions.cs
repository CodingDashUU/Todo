namespace Washu.Framework.Extensions;

using Constants;
using global::Radzen;
using Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications;
using Blazor;
using Identity.Entities;
using Microsoft.AspNetCore.Components.Authorization;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddExternalAuthRateLimiting()
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(RateLimiterPolicy.AuthLimiter, opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromSeconds(30);
                });
            });
            return services;
        }
        
        public IServiceCollection AddRadzenThemeCookie()
        {
            services.AddRadzenCookieThemeService(options =>
            {
                options.Name = CookieConstants.Names.Theme;
                options.Duration = TimeSpan.FromDays(CookieConstants.DurationInDays.Theme);
            });
            return services;
        }
        public IServiceCollection AddIdentity<TDbContext>(ConfigurationManager config)
            where TDbContext : ApplicationDbContext
        {
            services.AddSingleton<PermissionManager>();
            var connectionString = config.GetConnectionString("DefaultConnection");
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory<TDbContext>>();
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
            return services;
        }
        
        public IServiceCollection AddNotificationStore()
        {
            services.AddMemoryCache();
            services.AddSingleton<MessageStore>();
            return services;
        }

        public IServiceCollection AddUserTimeZoneProvider()
        {
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
            return services;
        }
        public IServiceCollection AddAuthenticationState<TDbContext>()
        where TDbContext : ApplicationDbContext
        {
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider<TDbContext>>();
            return services;
        }
        
    }
}