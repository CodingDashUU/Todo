using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Identity;
using Washu.Framework.Radzen;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Identity;
using Washu.Framework.Extensions;
using Washu.Framework.Notifications;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWashuFramework<TodoDbContext>(builder);
builder.Services.AddTodoCommands();
builder.Services.AddHealthChecks().AddDbContextCheck<TodoDbContext>(name: "DB");
var app = builder.Build();
await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
await dbContext.Database.MigrateAsync();
app.UseWashuFramework<TodoDbContext>();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/healthz", new HealthCheckOptions
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
});
await app.SeedRolesAsync<TodoDbContext>(InitialUserRoles.Roles);
await app.RunAsync();
