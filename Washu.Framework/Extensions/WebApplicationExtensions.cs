namespace Washu.Framework.Extensions;

using Constants;
using Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Notifications;

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

        public void MapIdentityEndpoints<TDbContext>()
        where TDbContext : ApplicationDbContext
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ExternalEndpoints<TDbContext>>().Map(app);
            app.MapPost("/delete-account", async (MessageStore store, HttpContext context, ApplicationUserManager<TDbContext> manager, [FromForm] string username) =>
            {
                var message = await manager.DeleteByUsernameAsync(username);
                var id = store.Store(message);
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
            });
            app.MapPost("/sign-out", async (HttpContext context) =>
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}");
            });
            app.MapPost("/change-username", async (MessageStore store, HttpContext context, ApplicationUserManager<TDbContext> manager, [FromForm] ChangeUsernameModel model) =>
            {
                var message = await manager.ChangeUsernameAsync(model);
                var id = store.Store(message);
                if (message.Title.StartsWith("Invalid") || message.Type is MessageType.Info) return TypedResults.Redirect($"{ApplicationRoutes.Settings}?messageId={id}");
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
            });
        }
    }
}