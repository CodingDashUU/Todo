namespace Washu.Framework.IntegrationTests;

using AspNetCore;
using Blazor;
using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Xunit;
using Extensions;
using Microsoft.Extensions.Primitives;
using ZiggyCreatures.Caching.Fusion;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class ExternalEndpointsTests(ApplicationPostgreSqlFixture fixture) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private MessageStore _messageStore = null!;

    private sealed record ExternalSignInRequest(string Subject, string? Email = null, string? Provider = null);
    private sealed record AppSignInRequest(Guid UserId, Guid SecurityStamp);

    public async Task InitializeAsync()
    {
        var messageStore = new MessageStore(new FusionCache(
            new FusionCacheOptions()));
        _messageStore = messageStore;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton(_messageStore);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<SignInHandler>();
        builder.Services.AddScoped<GetExternalLoginInfoHandler>();
        builder.Services.AddScoped<FindUserHandler>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.Name = ".Washu.External";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        })
        .AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.Name = ".Washu.Application";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        })
        .AddGoogle(options =>
        {
            options.ClientId = "test-google-client-id";
            options.ClientSecret = "test-google-client-secret";
            options.SignInScheme = IdentityConstants.ExternalScheme;
        });

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

        _app.MapExternalEndpoints();

        // Helper endpoints for simulating external and application authentication in tests
        _app.MapPost("/test-auth/sign-in-external", async (HttpContext ctx, ExternalSignInRequest req) =>
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, req.Subject),
                new("sub", req.Subject)
            };
            if (req.Email is not null) claims.Add(new Claim(ClaimTypes.Email, req.Email));
            var identity = new ClaimsIdentity(claims, IdentityConstants.ExternalScheme);
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                ["LoginProvider"] = req.Provider ?? GoogleDefaults.AuthenticationScheme
            });
            await ctx.SignInAsync(IdentityConstants.ExternalScheme, new ClaimsPrincipal(identity), properties);
            return Results.Ok();
        });

        _app.MapPost("/test-auth/sign-in-app", async (HttpContext ctx, AppSignInRequest req) =>
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

        // Ensure InitialUserRoles.User role exists in the database
        await EnsureUserRoleExistsAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private async Task EnsureUserRoleExistsAsync()
    {
        await using var db = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        if (!await db.Roles.AnyAsync(r => r.Name == InitialUserRoles.User))
        {
            db.Roles.Add(new ApplicationRole(InitialUserRoles.User));
            await db.SaveChangesAsync();
        }
    }

    private static string? GetCookieHeader(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        var cookie = cookies.FirstOrDefault(c => c.StartsWith($"{cookieName}="));
        return cookie?.Split(';')[0];
    }

    private async Task<string> SignInExternalAsync(string subject, string? email = null, string? provider = null)
    {
        var response = await _client.PostAsJsonAsync("/test-auth/sign-in-external",
            new ExternalSignInRequest(subject, email, provider));
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the test helper should sign in successfully");

        var cookie = GetCookieHeader(response, ".Washu.External");
        cookie.Should().NotBeNull("the .Washu.External cookie must be emitted");
        return cookie!;
    }

    private async Task<string> SignInAppAsync(Guid userId, Guid securityStamp)
    {
        var response = await _client.PostAsJsonAsync("/test-auth/sign-in-app",
            new AppSignInRequest(userId, securityStamp));
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the test helper should sign in successfully");

        var cookie = GetCookieHeader(response, ".Washu.Application");
        cookie.Should().NotBeNull("the .Washu.Application cookie must be emitted");
        return cookie!;
    }

    private static Dictionary<string, StringValues> ParseLocationQuery(HttpResponseMessage response)
    {
        var location = response.Headers.Location?.OriginalString;
        if (location is null) return [];
        var queryIndex = location.IndexOf('?');
        if (queryIndex < 0) return [];
        return QueryHelpers.ParseQuery(location[queryIndex..]);
    }

    [Fact]
    public async Task ChallengeGoogle_WhenAnonymous_IssuesChallengeRedirectingToGoogleOAuth()
    {
        var response = await _client.GetAsync($"{IdentityRoutes.ChallengeGoogle}?persistCookie=true&returnUrl=%2Fdashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "anonymous request to Google challenge endpoint should return 302 redirect");
        response.Headers.Location.Should().NotBeNull("the redirect response must contain a Location header");

        var location = response.Headers.Location!.AbsoluteUri;
        location.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth", "the challenge must direct the client to Google's OAuth endpoint");
        location.Should().Contain("client_id=test-google-client-id", "the configured Google ClientId must be passed to Google");
        location.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fsignin-google", "the OAuth redirect_uri must target the Google sign-in handler");
        location.Should().Contain("state=", "the OAuth state parameter containing protected callback properties must be present");

        response.Headers.Contains("Set-Cookie").Should().BeTrue("a correlation cookie should be issued by Google auth handler");
    }

    [Fact]
    public async Task ChallengeGoogle_WhenAlreadyAuthenticated_RedirectsToSafeReturnUrl()
    {
        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var appCookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{IdentityRoutes.ChallengeGoogle}?persistCookie=false&returnUrl=%2Fmy-tasks");
        request.Headers.Add("Cookie", appCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "authenticated user should be redirected without challenging Google");
        response.Headers.Location.Should().NotBeNull("the redirect response must include a Location header");
        response.Headers.Location!.OriginalString.Should().Be("/my-tasks", "authenticated user must be redirected to the requested safe return URL");
    }

    [Fact]
    public async Task ChallengeGoogle_WhenAuthenticatedWithUnsafeReturnUrl_RedirectsToHome()
    {
        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var appCookie = await SignInAppAsync(user.Id, user.SecurityStamp);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{IdentityRoutes.ChallengeGoogle}?persistCookie=false&returnUrl=https://attacker.com");
        request.Headers.Add("Cookie", appCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "an unsafe return URL should still result in a redirect");
        response.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.Home, "unsafe return URLs must fall back to the application home route");
    }

    [Fact]
    public async Task ChallengeGoogle_WhenUserCookiePresentButUserNotFoundInDatabase_ChallengesGoogle()
    {
        // Authenticate with a random user ID that is not present in the database
        var nonExistentUserId = Guid.NewGuid();
        var appCookie = await SignInAppAsync(nonExistentUserId, Guid.NewGuid());

        var request = new HttpRequestMessage(HttpMethod.Get, $"{IdentityRoutes.ChallengeGoogle}?persistCookie=false&returnUrl=%2Fdashboard");
        request.Headers.Add("Cookie", appCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "a user not found in database should be challenged");
        response.Headers.Location!.AbsoluteUri.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth",
            "should initiate external challenge when user does not exist in database");
    }

    [Fact]
    public async Task ExternalCallback_WithoutExternalCookie_RedirectsToSignIn()
    {
        var response = await _client.GetAsync($"{IdentityRoutes.ExternalCallback}?persistCookie=true&returnUrl=%2Fdashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "callback without external cookie should return redirect");
        response.Headers.Location.Should().NotBeNull("the redirect must contain a Location header");

        var location = response.Headers.Location!.OriginalString;
        location.Should().StartWith(ApplicationRoutes.SignIn, "the redirect should navigate to the sign-in page");
        location.Should().Contain("PersistCookie=True", "the query parameters must include PersistCookie");
        location.Should().Contain("ReturnUrl=%2Fdashboard", "the query parameters must include ReturnUrl");
    }

    [Fact]
    public async Task ExternalCallback_WithNewUser_CreatesUserWithRoleAndSignsIn()
    {
        await EnsureUserRoleExistsAsync();

        var subject = $"new-google-sub_{Guid.NewGuid():N}";
        var email = $"newuser_{Guid.NewGuid():N}@example.com";
        var extCookie = await SignInExternalAsync(subject, email);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{IdentityRoutes.ExternalCallback}?persistCookie=true&returnUrl=%2Fdashboard");
        request.Headers.Add("Cookie", extCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "successful registration should redirect to return URL");
        response.Headers.Location!.OriginalString.Should().Be("/dashboard", "should redirect to the requested safe return URL");

        // Verify cookies
        var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
        setCookies.Should().Contain(c => c.Contains(".Washu.Application"), "an application authentication cookie must be emitted");
        setCookies.Should().Contain(c => c.Contains(".Washu.External") && (c.Contains("expires=Thu, 01 Jan 1970") || c.Contains("max-age=0")),
            "the external authentication cookie must be cleared on successful sign-in");

        // Verify database persistence
        await using var db = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == subject);
        user.Should().NotBeNull("the new user must be persisted in the database");
        user!.Email.Should().Be(email, "the registered user email must match the claim value");
        user.Username.Should().NotBeNullOrWhiteSpace("a valid username must be generated");

        var role = await db.Roles.SingleAsync(r => r.Name == InitialUserRoles.User);
        var hasUserRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        hasUserRole.Should().BeTrue("the new user must be assigned the InitialUserRoles.User role in UserRoles table");
    }

    [Fact]
    public async Task ExternalCallback_WithExistingUser_SignsInWithoutCreatingDuplicate()
    {
        var subject = $"existing-google-sub_{Guid.NewGuid():N}";
        var email = $"existing_{Guid.NewGuid():N}@example.com";
        var username = $"User_{Guid.NewGuid():N}";
        var existingUser = new ApplicationUser(username, email, subject);

        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            db.Users.Add(existingUser);
            await db.SaveChangesAsync();
        }

        var extCookie = await SignInExternalAsync(subject, email);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{IdentityRoutes.ExternalCallback}?persistCookie=false&returnUrl=%2Ftodos");
        request.Headers.Add("Cookie", extCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "existing user sign in should redirect");
        response.Headers.Location!.OriginalString.Should().Be("/todos", "should redirect to the requested return URL");

        var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
        setCookies.Should().Contain(c => c.Contains(".Washu.Application"), "application cookie should be emitted");

        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            var userCount = await db.Users.CountAsync(u => u.GoogleSubject == subject);
            userCount.Should().Be(1, "no duplicate user records should be created for an existing subject");
        }
    }

    [Fact]
    public async Task ExternalCallback_WhenEmailClaimMissing_RedirectsToSignInWithMessage()
    {
        var subject = $"no-email-sub_{Guid.NewGuid():N}";
        var extCookie = await SignInExternalAsync(subject, email: null);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{IdentityRoutes.ExternalCallback}?persistCookie=false&returnUrl=%2Ftodos");
        request.Headers.Add("Cookie", extCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "missing email should redirect back to sign-in");
        response.Headers.Location!.OriginalString.Should().StartWith(ApplicationRoutes.SignIn, "redirect must navigate to sign-in");

        var query = ParseLocationQuery(response);
        query.Should().ContainKey("MessageId", "the redirect query string must include MessageId");

        var messageId = query["MessageId"].ToString();
        var messages = _messageStore.Take(messageId);
        messages.Should().ContainSingle("an error message should be stored in MessageStore");
        messages[0].Type.Should().Be(MessageType.Error, "stored notification type must be Error");
        messages[0].Title.Should().Be("Registration Error", "stored notification title must match");
        messages[0].Details.Should().Be("Email was not provided or not found", "error message must state that email was missing");

        await using var db = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var exists = await db.Users.AnyAsync(u => u.GoogleSubject == subject);
        exists.Should().BeFalse("no user should be registered when email is missing");
    }

    [Fact]
    public async Task ExternalCallback_WhenEmailAlreadyRegisteredUnderDifferentSubject_RedirectsToSignInWithMessage()
    {
        var commonEmail = $"shared_{Guid.NewGuid():N}@example.com";
        var firstSubject = $"first-sub_{Guid.NewGuid():N}";
        var secondSubject = $"second-sub_{Guid.NewGuid():N}";

        var initialUser = new ApplicationUser($"User_{Guid.NewGuid():N}", commonEmail, firstSubject);
        await using (var setupDb = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            setupDb.Users.Add(initialUser);
            await setupDb.SaveChangesAsync();
        }

        var extCookie = await SignInExternalAsync(secondSubject, commonEmail);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{IdentityRoutes.ExternalCallback}?persistCookie=false&returnUrl=%2Ftodos");
        request.Headers.Add("Cookie", extCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "duplicate email registration should redirect to sign-in");
        response.Headers.Location!.OriginalString.Should().StartWith(ApplicationRoutes.SignIn, "redirect target should be sign-in");

        var query = ParseLocationQuery(response);
        query.Should().ContainKey("MessageId", "the redirect query string must include MessageId");

        var messageId = query["MessageId"].ToString();
        var messages = _messageStore.Take(messageId);
        messages.Should().ContainSingle("an error message should be stored in MessageStore");
        messages[0].Type.Should().Be(MessageType.Error, "message type should be Error");
        messages[0].Title.Should().Be("Registration Error", "message title must match");
        messages[0].Details.Should().Be("This account or email is already registered. Please sign in.",
            "error message should indicate account or email is already registered");

        await using var verifyDb = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var secondUserExists = await verifyDb.Users.AnyAsync(u => u.GoogleSubject == secondSubject);
        secondUserExists.Should().BeFalse("the new subject should not be added when email is already taken");
    }

    [Fact]
    public async Task ExternalCallback_WhenUserRoleNotFoundInDatabase_RedirectsToSignInWithMessage()
    {
        // Temporarily remove InitialUserRoles.User role from the database
        ApplicationRole? existingRole;
        await using (var db = await fixture.CreateDbContextFactory().CreateDbContextAsync())
        {
            existingRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == InitialUserRoles.User);
            if (existingRole is not null)
            {
                db.Roles.Remove(existingRole);
                await db.SaveChangesAsync();
            }
        }

        try
        {
            var subject = $"no-role-sub_{Guid.NewGuid():N}";
            var email = $"norole_{Guid.NewGuid():N}@example.com";
            var extCookie = await SignInExternalAsync(subject, email);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{IdentityRoutes.ExternalCallback}?persistCookie=false&returnUrl=%2Ftodos");
            request.Headers.Add("Cookie", extCookie);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect, "missing role should redirect to sign-in");
            response.Headers.Location!.OriginalString.Should().StartWith(ApplicationRoutes.SignIn, "redirect target should be sign-in");

            var query = ParseLocationQuery(response);
            query.Should().ContainKey("MessageId", "the redirect query string must include MessageId");

            var messageId = query["MessageId"].ToString();
            var messages = _messageStore.Take(messageId);
            messages.Should().ContainSingle("an error message should be stored in MessageStore");
            messages[0].Type.Should().Be(MessageType.Error, "message type should be Error");
            messages[0].Title.Should().Be("Login Error", "message title must match Login Error");
            messages[0].Details.Should().Be("There was an issue while logging you in", "message details should match");
        }
        finally
        {
            // Restore InitialUserRoles.User role
            await EnsureUserRoleExistsAsync();
        }
    }

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("")]
    public async Task ExternalCallback_WithUnsafeReturnUrl_RedirectsToHomeRoute(string unsafeUrl)
    {
        await EnsureUserRoleExistsAsync();

        var subject = $"safe-return-sub_{Guid.NewGuid():N}";
        var email = $"safereturn_{Guid.NewGuid():N}@example.com";
        var extCookie = await SignInExternalAsync(subject, email);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{IdentityRoutes.ExternalCallback}?persistCookie=false&returnUrl={Uri.EscapeDataString(unsafeUrl)}");
        request.Headers.Add("Cookie", extCookie);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "should return a redirect response");
        response.Headers.Location!.OriginalString.Should().Be(ApplicationRoutes.Home,
            "an unsafe returnUrl must be normalized to ApplicationRoutes.Home");
    }
}
