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
        public async Task SeedPermissionsAsync(ImmutableArray<string> roles, ImmutableDictionary<string, string[]> permissions)
        {
            using var serviceScope = app.Services.CreateScope();
            throw new NotSupportedException();
            // var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            //
            // foreach (var role in roles)
            // {
            //     var identityRole = await roleManager.FindByNameAsync(role);
            //     if (identityRole is null) continue;
            //
            //     var existingClaims = await roleManager.GetClaimsAsync(identityRole);
            //
            //     foreach (var permission in permissions[role]) 
            //         if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
            //             await roleManager.AddClaimAsync(identityRole, new Claim("permission", permission));
            // }
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