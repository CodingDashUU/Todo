using FluentValidation;
using Serilog;
using Washu.Framework.Identity;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Identity;
using Washu.Framework.Extensions;

var builder = WebApplication.CreateBuilder(args);
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
builder.Services.AddHttpContextAccessor();
builder.Services.AddWfRateLimiting();
builder.Services.AddHealthChecks().AddDbContextCheck<TodoDbContext>();
builder.Services.AddWfRadzenBlazorServer();
builder.Services.AddWfIdentityServices();
builder.Services.AddFusionCaching();
builder.Host.UseSerilog((context, config) => 
    config.ReadFrom.Configuration(context.Configuration));
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
await app.SeedRolesAsync([.. InitialUserRoles.Roles, "Bob"]);
await app.SeedPermissionsAsync();
await app.RunAsync();
