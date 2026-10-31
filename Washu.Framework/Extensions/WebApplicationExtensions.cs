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
using Microsoft.Extensions.Logging;
using Serilog;

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
            app.UseSerilogRequestLogging(); 
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
            app.MapGet(IdentityRoutes.SignOut, async (HttpContext context, ILogger<SignOut> logger, [FromQuery] string? messageId) =>
                {
                    var userId = context.User.UserId;
                    if (!userId.HasValue)
                        logger.LogWarning("There is no UserId claim for connection {ConnectionId}", context.Connection.Id);
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    if (userId.HasValue)
                        logger.LogInformation("User with id {UserId} signed out", userId);
                    else logger.LogInformation("User with connection {ConnectionId} signed out",
                        context.Connection.Id);
                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";
                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
            app.MapGet(IdentityRoutes.SignOutFromAllSessions, async (HttpContext context, ILogger<SignOutFromAllSessions> logger, ApplicationUserStore userStore, IDbContextFactory<ApplicationDbContext> factory,[FromQuery] string? messageId) =>
                {
                    var userId = context.User.UserId;

                    if (userId.HasValue)
                    {
                        await using var dbContext = await factory.CreateDbContextAsync();
                        var rowsChanged = await dbContext.Users
                            .Where(u => u.Id == userId.Value)
                            .ExecuteUpdateAsync(s =>
                                s.SetProperty(u => u.SecurityStamp, Guid.CreateVersion7())
                            );
                        if (rowsChanged == 1)
                            logger.LogInformation("Changed security stamp for user with id {UserId}", userId);
                        await userStore.RemoveUserAsync(userId.Value);
                    }
                    else logger.LogWarning("There is no UserId claim for connection {ConnectionId}", context.Connection.Id);
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    if (userId.HasValue)
                        logger.LogInformation("User with id {UserId} signed out from all sessions", userId);
                    else logger.LogInformation("User with connection {ConnectionId} signed out",
                        context.Connection.Id);
                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";

                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
        }
    }
}
file abstract class SignOut;
file abstract class SignOutFromAllSessions;