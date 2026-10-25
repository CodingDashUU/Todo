namespace Washu.Framework.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
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
using Notifications;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class SignOutAndHealthEndpointsTests(ApplicationPostgreSqlFixture fixture) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private MessageStore _messageStore = null!;

    private sealed record AppSignInRequest(Guid UserId, Guid SecurityStamp);

    public async Task InitializeAsync()
    {
        _messageStore = new MessageStore(new FusionCache(new FusionCacheOptions()));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddMemoryCache();
        builder.Services.AddFusionCaching();
        builder.Services.AddSingleton(_messageStore);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        builder.Services.AddWfIdentityServices();

        var authBuilder = builder.Services.AddDefaultAuthentication();
        authBuilder.AddWfExternalCookie();
        authBuilder.AddWfApplicationCookie();

        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimiterPolicy.StandardRateLimiter, _ =>
                RateLimitPartition.GetNoLimiter("test"));
        });

        _app = builder.Build();

        _app.UseRouting();
        _app.UseRateLimiter();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapSignOutEndpoints();
        _app.MapWfHealthChecks();

        // Helper endpoint to sign in directly with application scheme
        _app.MapPost("/test-auth/sign-in-app", async (HttpContext ctx, AppSignInRequest req) =>
        {
            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, req.UserId.ToString()));
            identity.AddClaim(new Claim("SecurityStamp", req.SecurityStamp.ToString()));
            await ctx.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));
            return Results.Ok();
        });

        // Test protected endpoint to check if principal is authenticated
        _app.MapGet("/test-protected", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated == true)
                return Results.Ok(new { UserId = ctx.User.UserId });
            return Results.Unauthorized();
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

    private async Task<string> SignInAppAsync(Guid userId, Guid securityStamp)
    {
        var response = await _client.PostAsJsonAsync("/test-auth/sign-in-app",
            new AppSignInRequest(userId, securityStamp));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookie = GetCookieHeader(response, ".Washu.Application");
        cookie.Should().NotBeNull("the .Washu.Application cookie must be emitted");
        return cookie!;
    }

    [Fact]
    public async Task SeedPermissionsAsync_SeedsAdminPermissionAndIsIdempotent()
    {
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
            if (!await db.Roles.AnyAsync(r => r.Name == InitialUserRoles.Admin))
            {
                db.Roles.Add(new ApplicationRole(InitialUserRoles.Admin));
                await db.SaveChangesAsync();
            }

        // First seeding
        await _app.SeedPermissionsAsync();

        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            var adminRole = await db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Name == InitialUserRoles.Admin);
            adminRole.Permissions.Should().Contain(p => p.Name == CorePermissions.CanCheckSystemHealth);
        }

        // Second seeding (idempotent check)
        var act = async () => await _app.SeedPermissionsAsync();
        await act.Should().NotThrowAsync("SeedPermissionsAsync should be idempotent");
    }

    [Fact]
    public async Task SignOutEndpoint_RedirectsToSignIn_AndClearsApplicationCookie()
    {
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var appCookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, IdentityRoutes.SignOut);
        request.Headers.Add("Cookie", appCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.SignIn);

        var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
        setCookies.Should().Contain(c => c.Contains(".Washu.Application") && (c.Contains("expires=Thu, 01 Jan 1970") || c.Contains("max-age=0")),
            "SignOut must emit cookie expiration to remove the application cookie");
    }

    [Fact]
    public async Task SignOutEndpoint_WithMessageId_RedirectsToSignInWithMessageIdQueryParam()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{IdentityRoutes.SignOut}?messageId=test-msg-123");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be($"{ApplicationRoutes.SignIn}?messageId=test-msg-123");
    }

    [Fact]
    public async Task SignOutFromAllSessions_ChangesSecurityStamp_AndInvalidatesActiveSessions()
    {
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var initialSecurityStamp = user.SecurityStamp;
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var appCookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        // Verify protected access works before sign out
        var checkRequest1 = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        checkRequest1.Headers.Add("Cookie", appCookie);
        var checkResponse1 = await _client.SendAsync(checkRequest1);
        checkResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Call /signout-all
        var signOutRequest = new HttpRequestMessage(HttpMethod.Get, IdentityRoutes.SignOutFromAllSessions);
        signOutRequest.Headers.Add("Cookie", appCookie);
        var signOutResponse = await _client.SendAsync(signOutRequest);

        signOutResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        signOutResponse.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.SignIn);

        // Verify database SecurityStamp was changed
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            var updatedUser = await db.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.SecurityStamp.Should().NotBe(initialSecurityStamp, "SignOutFromAllSessions must change the user's SecurityStamp");
        }

        // Verify subsequent request with old cookie fails validation and is rejected
        var checkRequest2 = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        checkRequest2.Headers.Add("Cookie", appCookie);
        var checkResponse2 = await _client.SendAsync(checkRequest2);
        checkResponse2.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Old session cookie must be rejected because SecurityStamp changed");
    }

    [Fact]
    public async Task HealthEndpoint_WhenAnonymous_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/health-wf");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Unauthenticated access to /health-wf must return 404");
    }

    [Fact]
    public async Task HealthEndpoint_WhenAuthenticatedWithoutPermission_ReturnsNotFound()
    {
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var cookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, "/health-wf");
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Authenticated user without CanCheckSystemHealth must return 404");
    }

    [Fact]
    public async Task HealthEndpoint_WhenUserHasCanCheckSystemHealthPermission_ReturnsOkWithJsonReport()
    {
        var user = new ApplicationUser($"AdminUser_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);

            var adminRole = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == InitialUserRoles.Admin);
            if (adminRole is null)
            {
                adminRole = new ApplicationRole(InitialUserRoles.Admin);
                db.Roles.Add(adminRole);
                await db.SaveChangesAsync();
            }

            var hasPerm = await db.RolePermissions.AnyAsync(rp => rp.Name == CorePermissions.CanCheckSystemHealth);
            if (!hasPerm) db.RolePermissions.Add(new RolePermission(CorePermissions.CanCheckSystemHealth, adminRole.Id));

            var hasUserRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);
            if (!hasUserRole) db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

            await db.SaveChangesAsync();
        }

        var cookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, "/health-wf");
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        using var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().Be("Healthy");
        jsonDoc.RootElement.TryGetProperty("checks", out _).Should().BeTrue();
    }
}
