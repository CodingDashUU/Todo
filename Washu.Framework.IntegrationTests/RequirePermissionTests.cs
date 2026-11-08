namespace Washu.Framework.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using AspNetCore;
using Blazor;
using Extensions;
using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class RequirePermissionTests(ApplicationPostgreSqlFixture fixture) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private sealed record SignInRequest(Guid UserId, Guid SecurityStamp);

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddMemoryCache();
        builder.Services.AddFusionCaching();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<HasPermissionHandler>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        builder.Services.AddWfIdentityServices();
        builder.Services.RemoveAll<GetUserService>();

        var authBuilder = builder.Services.AddDefaultAuthentication();
        authBuilder.AddWfApplicationCookie();
        builder.Services.AddAuthorization();

        _app = builder.Build();

        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();

        // Protected test endpoint using RequirePermission
        _app.MapGet("/api/secure-action", () => Results.Ok("Allowed"))
            .RequirePermission("System.Administer");

        _app.MapPost("/test-auth/sign-in", async (HttpContext ctx, SignInRequest req) =>
        {
            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, req.UserId.ToString()));
            identity.AddClaim(new Claim("SecurityStamp", req.SecurityStamp.ToString()));
            await ctx.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));
            return Results.Ok();
        });

        await _app.StartAsync();

        var handler = _app.GetTestServer().CreateHandler();
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost")
        };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private static string? GetCookieHeader(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        var cookie = cookies.FirstOrDefault(c => c.StartsWith($"{cookieName}="));
        return cookie?.Split(';')[0];
    }

    private async Task<string> SignInAsync(Guid userId, Guid securityStamp)
    {
        var response = await _client.PostAsJsonAsync("/test-auth/sign-in", new SignInRequest(userId, securityStamp));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = GetCookieHeader(response, ".Washu.Application");
        cookie.Should().NotBeNull();
        return cookie!;
    }

    [Fact]
    public async Task RequirePermission_WhenAnonymous_RedirectsToAccessDenied()
    {
        var response = await _client.GetAsync("/api/secure-action");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.AccessDenied);
    }

    [Fact]
    public async Task RequirePermission_WhenUserLacksPermission_RedirectsToAccessDenied()
    {
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var cookie = await SignInAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/secure-action");
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.AccessDenied);
    }

    [Fact]
    public async Task RequirePermission_WhenUserHasPermission_ReturnsSuccess()
    {
        var user = new ApplicationUser($"Admin_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        const string permissionName = "System.Administer";

        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            db.RolePermissions.Add(new RolePermission(permissionName, role.Id));
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }

        var cookie = await SignInAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/secure-action");
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Allowed");
    }
}
