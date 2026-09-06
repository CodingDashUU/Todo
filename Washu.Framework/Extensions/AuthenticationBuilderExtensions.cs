namespace Washu.Framework.Extensions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class AuthenticationBuilderExtensions
{
    extension(AuthenticationBuilder authBuilder)
    {
        public AuthenticationBuilder AddGoogle(WebApplicationBuilder builder)
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = builder.Configuration.GetSection("ExternalProviders:Google:ClientId").Value!;
                options.ClientSecret = builder.Configuration.GetSection("ExternalProviders:Google:ClientSecret").Value!;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            return authBuilder;
        }
    }
}