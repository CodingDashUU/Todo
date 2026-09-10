
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Washu.Framework.Blazor;
using Washu.Framework.Constants;
using Washu.Framework.Identity;
using Washu.Framework.Radzen;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Identity;
using Washu.Framework.Extensions;
using Washu.Framework.Notifications;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddExternalAuthRateLimiting()
    .AddRadzenThemeCookie()
    .AddIdentity<TodoDbContext>(builder.Configuration)
    .AddAuthenticationState<TodoDbContext>()
    .AddNotificationStore()
    .AddAuthorization();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddCookie(IdentityConstants.ExternalScheme, options =>
    {
        // Registers the missing 'Identity.External' scheme
        options.Cookie.Name = ".Washu.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(CookieConstants.DurationInDays.Application);
        options.SlidingExpiration = true;
        options.LoginPath = ApplicationRoutes.SignIn;
        options.Cookie.Name = CookieConstants.Names.Application;
    })
    .AddGoogle(builder);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<ApplicationUserManager<TodoDbContext>>();
builder.Services.AddScoped<INotificationService, RadzenNotificationService>();
builder.Services.AddScoped<ExternalEndpoints<TodoDbContext>>();
builder.Services.AddTodoCommands();
builder.Services.AddExceptionHandler<AntiforgeryExceptionHandler>();
builder.Services.AddUserTimeZoneProvider();
var app = builder.Build();
await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
await dbContext.Database.MigrateAsync();
scope.ServiceProvider.GetRequiredService<ExternalEndpoints<TodoDbContext>>().Map(app);
app.MapPost("/delete-account", async (MessageStore store, HttpContext context, ApplicationUserManager<TodoDbContext> manager, [FromForm] string username) =>
{
    var message = await manager.DeleteByUsernameAsync(username);
    var id = store.Store(message);
    if (message.Type is MessageType.Error) return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
});
app.UseExceptionHandler(ApplicationRoutes.Error, createScopeForErrors: true);
app.UseForwardedHeaders();
app.UseHsts();
app.MapThemeEndpoint();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute(ApplicationRoutes.NotFound, createScopeForStatusCodePages: true);
app.UseRateLimiter();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
await app.SeedRolesAsync<TodoDbContext>(InitialUserRoles.Roles);
await app.RunAsync();
