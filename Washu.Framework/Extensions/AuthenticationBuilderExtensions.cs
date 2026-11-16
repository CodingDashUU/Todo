using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Extensions;

using Blazor;
using Identity;
using Identity.Login;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifications;

public static class AuthenticationBuilderExtensions
{
    extension(IServiceCollection services)
    {
        public AuthenticationBuilder AddDefaultAuthentication() => services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });
    } 
    extension(AuthenticationBuilder builder)
    {
        public AuthenticationBuilder AddWfExternalCookie() => builder.AddCookie(IdentityConstants.ExternalScheme,
            options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Cookie.Name = ".Washu.External";
            });
        public AuthenticationBuilder AddWfApplicationCookie() => builder.AddCookie(IdentityConstants.ApplicationScheme,
            options =>

        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.LoginPath = ApplicationRoutes.SignIn;
            options.AccessDeniedPath = ApplicationRoutes.AccessDenied;
            options.Cookie.Name = ".Washu.Application";
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.RedirectUri = options.AccessDeniedPath;
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToLogin = context =>
            {
                if (!HttpMethods.IsGet(context.Request.Method))
                {
                    var referer = context.Request.Headers.Referer.FirstOrDefault();

                    var safeReturnUrl = !string.IsNullOrWhiteSpace(referer)
                                        && Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                        ? uri.LocalPath
                        : ApplicationRoutes.Home;
                    context.RedirectUri = $"{options.LoginPath}?ReturnUrl={safeReturnUrl}";
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });
        public AuthenticationBuilder AddWfGoogle(ConfigurationManager configuration) => builder.AddGoogle(options =>
        {
            options.ClientId = configuration.GetSection("ExternalProviders:Google:ClientId").Value!;
            options.ClientSecret = configuration.GetSection("ExternalProviders:Google:ClientSecret").Value!;
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
    }
}