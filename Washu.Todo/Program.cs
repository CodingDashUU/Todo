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
var app = builder.Build();
await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
await dbContext.Database.MigrateAsync();
app.UseWashuFramework<TodoDbContext>();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
await app.SeedRolesAsync<TodoDbContext>(InitialUserRoles.Roles);
await app.RunAsync();
