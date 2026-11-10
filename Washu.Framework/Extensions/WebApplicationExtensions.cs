using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Washu.Framework.Identity.Permissions;
using Washu.Framework.Identity.Persistence;
using Washu.Framework.Identity.Roles;
using Washu.Framework.Identity.Users;
using Washu.Framework.Identity.Users.FindUser;

namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using Identity;
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
            }).RequirePermission(CorePermissions.CanCheckSystemHealth);
        internal void MapSignOutEndpoints()
        {
            app.MapGet(IdentityRoutes.SignOut, async (HttpContext context, ILogger<SignOut> logger, [FromQuery] string? messageId) =>
                {
                    var userId = context.User.UserId;
                    if (!userId.HasValue)
                        logger.LogWarning("There is no UserId claim for connection {ConnectionId}", context.Connection.Id);
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        if (userId.HasValue)
                            logger.LogInformation("User with id {UserId} signed out", userId);
                        else logger.LogInformation("User with connection {ConnectionId} signed out",
                            context.Connection.Id);   
                    }
                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";
                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
            app.MapGet(IdentityRoutes.SignOutFromAllSessions, async (HttpContext context, ILogger<SignOutFromAllSessions> logger, CachedFindUserHandler cachedFindUserHandler, IDbContextFactory<ApplicationDbContext> factory,[FromQuery] string? messageId) =>
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
                        if (rowsChanged == 1 && logger.IsEnabled(LogLevel.Information))
                            logger.LogInformation("Changed security stamp for user with id {UserId}", userId);
                        await cachedFindUserHandler.RemoveUserAsync(userId.Value);
                    }
                    else logger.LogWarning("There is no UserId claim for connection {ConnectionId}", context.Connection.Id);
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        if (userId.HasValue)
                            logger.LogInformation("User with id {UserId} signed out from all sessions", userId);
                        else logger.LogInformation("User with connection {ConnectionId} signed out",
                            context.Connection.Id);   
                    }
                    var targetUrl = string.IsNullOrWhiteSpace(messageId)
                        ? ApplicationRoutes.SignIn
                        : $"{ApplicationRoutes.SignIn}?messageId={messageId}";

                    return TypedResults.Redirect(targetUrl);
                })
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
        }
    }

    extension(WebApplication)
    {
        public static async Task BuildAndRunApplicationAsync<TDbContext, TApp>(
            string[] args,
            Action<WfApplicationOptions>? configureOptions = null)
            where TDbContext : ApplicationDbContext
            where TApp : ComponentBase
        {
            var options = new WfApplicationOptions();
            configureOptions?.Invoke(options);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Application is starting...");
                var builder = WebApplication.CreateBuilder(args);
                ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
                builder.Host.UseWfLogging();
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddWfRateLimiting();
                builder.Services.AddHealthChecks().AddDbContextCheck<TDbContext>();
                builder.Services.AddWfDbContextFactory<TDbContext>(builder.Configuration);
                builder.Services.AddWfRadzenBlazorServer();
                builder.Services.AddWfIdentityServices();
                builder.Services.AddFusionCaching();
                builder.Services.AddUserTimeZoneProvider();
                builder.Services.AddCascadingAuthenticationState();
                builder.Services.AddAppAuthenticationStateProvider();
                builder.Services.AddNoOpAntiforgeryProvider();
                builder.Services
                    .AddDefaultAuthentication()
                    .AddWfExternalCookie()
                    .AddWfApplicationCookie()
                    .AddWfGoogle(builder.Configuration);
                options.RegisterAdditionalServices?.Invoke(builder);
                var app = builder.Build();
                await app.MigrateAsync<TDbContext>();
                app.UseWfPipeline();
                app.MapWfRazorComponents<TApp>();
                await app.SeedRolesAsync([.. InitialUserRoles.Roles]);
                await app.SeedPermissionsAsync();
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly due to an exception during startup");
            }
            finally
            { 
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
public class WfApplicationOptions
{
    public Action<WebApplicationBuilder>? RegisterAdditionalServices { get; set; }
}
file abstract class SignOut;
file abstract class SignOutFromAllSessions;