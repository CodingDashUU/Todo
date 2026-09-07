namespace Washu.Framework.Extensions;

using Constants;
using Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Security.Claims;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public async Task SeedRolesAsync<TDbContext>(string[] roles)
        where TDbContext : ApplicationDbContext
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<ApplicationRoleManager<TDbContext>>();
            await roleManager.AddRolesAsync(roles);
        }
        public void MapThemeEndpoint() =>
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
    }
}