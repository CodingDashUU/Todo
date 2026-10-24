namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public async Task SeedRolesAsync(List<string> roles)
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            var addRolesHandler = serviceScope.ServiceProvider.GetRequiredService<AddRolesHandler>();
            await addRolesHandler.HandleAsync(new AddRolesCommand(roles));
        }
        public async Task SeedPermissionsAsync()
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            await using var dbContext = await serviceScope.ServiceProvider
                .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
                .CreateDbContextAsync();
            if (await dbContext.RolePermissions.AnyAsync(rp => rp.Name == CorePermissions.CanCheckSystemHealth)) return;
            var role = dbContext.Roles.First(r  => r.Name == InitialUserRoles.Admin);
            dbContext.RolePermissions.Add(new RolePermission(CorePermissions.CanCheckSystemHealth, role.Id));
            await dbContext.SaveChangesAsync();
        }
        public void UseWfPipeline()
        {
            app.UseExceptionHandler(ApplicationRoutes.Error, createScopeForErrors: true);
            app.UseStatusCodePagesWithReExecute(ApplicationRoutes.NotFound, createScopeForStatusCodePages: true);
            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseHttpsRedirection();
            app.MapStaticAssets();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.MapExternalEndpoints();
            app.MapWfHealthChecks();
            app.MapSignOutEndpoints();
        }

        public void MapWfRazorComponents<TApp>()
            where TApp : ComponentBase =>
            app.MapRazorComponents<TApp>()
                .AddInteractiveServerRenderMode()
                .RequireRateLimiting(RateLimiterPolicy.BlazorHandshakeRateLimiter)
                .DisableAntiforgery();

        public async Task MigrateAsync<TDbContext>()
        where TDbContext : ApplicationDbContext
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        internal void MapWfHealthChecks() =>
            app.MapHealthChecks("/health-wf", new HealthCheckOptions
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
            }).AddEndpointFilter(async (context, next) =>
            {
                var handler = context.HttpContext.RequestServices.GetRequiredService<HasPermissionHandler>();
                if (!await handler.HandleAsync(new HasPermissionQuery(CorePermissions.CanCheckSystemHealth)))
                    return TypedResults.NotFound();
                return await next(context);
            });

        internal void MapSignOutEndpoints()
        {
            app.MapGet(IdentityRoutes.SignOut, async (HttpContext context, [FromQuery] string? messageId) =>
                {
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";

                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
            app.MapGet(IdentityRoutes.SignOutFromAllSessions, async (HttpContext context, ApplicationUserStore userStore, IDbContextFactory<ApplicationDbContext> factory,[FromQuery] string? messageId) =>
                {
                    var userId = context.User.UserId;

                    if (userId.HasValue)
                    {
                        await using var dbContext = await factory.CreateDbContextAsync();
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                        if (user is not null)
                        {
                            user.ChangeSecurityStamp();
                            await dbContext.SaveChangesAsync();
                        }

                        await userStore.RemoveUserAsync(userId.Value);
                    }
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";

                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
        }
    }
}