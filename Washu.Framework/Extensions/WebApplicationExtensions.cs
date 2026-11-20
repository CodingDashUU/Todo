using FluentValidation;
using Serilog;
using Serilog.Events;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using Identity;
using Identity.Authentication;
using Identity.Roles;
using Identity.Users;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TickerQ.DependencyInjection;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public async Task SeedRolesAsync(List<string> roles)
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AddRolesCommand(roles));
        }
        public async Task SeedPermissionsAsync(params List<PermissionRequest> perms)
        {
            await using var serviceScope = app.Services.CreateAsyncScope();
            var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AddPermissionsCommand(perms));
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
            app.MapWfHealthChecks();
            ExternalEndpoints.AddRoutes(app);
            SignOutEndpoints.AddRoutes(app);
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
    }

    extension(WebApplication)
    {
        public static async Task BuildAndRunApplicationAsync<TDbContext, TApp>(
            string[] args,
            Action<WfApplicationOptions>? configureOptions = null)
            where TDbContext : ApplicationDbContext
            where TApp : ComponentBase
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Application is starting...");
                var builder = WebApplication.CreateBuilder(args);
                
                var options = new WfApplicationOptions();
                configureOptions?.Invoke(options);
                ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
                var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
                if (Assembly.GetEntryAssembly() is {} entryAssembly) assemblies.Add(entryAssembly);
                builder.Services.AddValidatorsFromAssemblies(assemblies, filter: r => r.ValidatorType.IsPublic);
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
                builder.Services.AddWfTickerQ<TDbContext>(
                    builder.Configuration,
                    [new CronJobRequest(nameof(DeleteUserJob), "0 5 * * SUN"), ..options.CronJobs]
                    );
                options.RegisterAdditionalServices?.Invoke(builder);
                var app = builder.Build();
                await app.MigrateAsync<TDbContext>();
                app.UseWfPipeline();
                app.UseTickerQ();
                app.MapWfRazorComponents<TApp>();
                await app.SeedRolesAsync([.. InitialUserRoles.Roles]);
                await using var serviceScope = app.Services.CreateAsyncScope();
                var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new AddRolesCommand(InitialUserRoles.Roles));
                await mediator.Send(new AddPermissionsCommand([new PermissionRequest(CorePermissions.CanCheckSystemHealth, InitialUserRoles.Admin), 
                    new PermissionRequest(CorePermissions.CanCheckBackgroundJobDashboard, InitialUserRoles.Admin)]));
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
    public List<CronJobRequest> CronJobs { get; set; } = [];
    public Action<WebApplicationBuilder>? RegisterAdditionalServices { get; set; }
}