using FluentValidation;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Washu.Framework.Identity;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Identity;
using Washu.Framework.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application...");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog((_, _, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Components.RenderTree.Renderer", LogEventLevel.Error)
        .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning)
        .MinimumLevel.Override("ZiggyCreatures.Caching.Fusion.FusionCache", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessName()
        .Enrich.WithProcessId()
        .Enrich.WithEnvironmentName()
        
        .WriteTo.Console(outputTemplate: 
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        
        .WriteTo.File(
            path: "logs/app-log-.txt",
            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: LogEventLevel.Information,
            retainedFileCountLimit: 30)
        
        .WriteTo.File(
            new JsonFormatter(renderMessage: true), 
            path: "logs/app-json-log-.json",
            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: LogEventLevel.Information,
            retainedFileCountLimit: 30)
    );
    ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddWfRateLimiting();
    builder.Services.AddHealthChecks().AddDbContextCheck<TodoDbContext>();
    builder.Services.AddWfRadzenBlazorServer();
    builder.Services.AddWfIdentityServices();
    builder.Services.AddFusionCaching();
    builder.Services.AddSingleton<IDeleteEntityTask, DeleteTodoListTask>();
    builder.Services.AddWfDbContextFactory<TodoDbContext>(builder.Configuration);
    builder.Services.AddUserTimeZoneProvider();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddAppAuthenticationStateProvider();
    builder.Services.AddNoOpAntiforgeryProvider();
    builder.Services
        .AddDefaultAuthentication()
        .AddWfExternalCookie()
        .AddWfApplicationCookie()
        .AddWfGoogle(builder.Configuration);
    builder.Services.AddTodoHandlers();
    var app = builder.Build();
    await app.MigrateAsync<TodoDbContext>();
    app.UseWfPipeline();
    app.MapWfRazorComponents<App>();
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
    Log.CloseAndFlush();
}
