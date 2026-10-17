namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public class SignInHandlerTests(PostgreSqlFixture fixture)
{
    private static (DefaultHttpContext Context, SignInHandler Handler) CreateContextAndHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.Name = ".Washu.External";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        })
        .AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.Name = ".Washu.Application";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<SignInHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = context;

        var handler = serviceProvider.GetRequiredService<SignInHandler>();
        return (context, handler);
    }

    [Fact]
    public async Task HandleAsync_ThrowsInvalidOperationException_WhenHttpContextIsNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<SignInHandler>();

        var serviceProvider = services.BuildServiceProvider();
        // Do not set HttpContext on IHttpContextAccessor (remains null)
        var handler = serviceProvider.GetRequiredService<SignInHandler>();

        var user = new ApplicationUser("TestUser", "test@example.com", "google-sub-1");
        var command = new SignInCommand(user, IsPersistent: false);

        var action = async () => await handler.HandleAsync(command);

        (await action.Should().ThrowAsync<InvalidOperationException>("an active HttpContext is mandatory for signing in"))
            .WithMessage("An active HttpContext is required", "the exception message should explicitly state that HttpContext is required");
    }

    [Fact]
    public async Task HandleAsync_SignsInUserAndSetsPrincipalOnHttpContext()
    {
        var (context, handler) = CreateContextAndHandler();
        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");

        await using (var db = fixture.CreateDbContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var command = new SignInCommand(user, IsPersistent: false, AuthenticationMethod: "Google");
        await handler.HandleAsync(command);

        context.User.Should().NotBeNull("the HttpContext.User should be populated after sign-in");
        context.User.Identity.Should().NotBeNull("the user principal should have an identity");
        context.User.Identity!.IsAuthenticated.Should().BeTrue("the identity should be authenticated after sign-in");
        context.User.Identity.AuthenticationType.Should().Be(IdentityConstants.ApplicationScheme, "the identity scheme should be the application scheme");

        var nameIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        nameIdClaim.Should().NotBeNull("the NameIdentifier claim must be present");
        nameIdClaim!.Value.Should().Be(user.Id.ToString(), "the NameIdentifier claim value should match the user's Id");

        var securityStampClaim = context.User.FindFirst("SecurityStamp");
        securityStampClaim.Should().NotBeNull("the SecurityStamp claim must be present");
        securityStampClaim!.Value.Should().Be(user.SecurityStamp.ToString(), "the SecurityStamp claim value should match the user's SecurityStamp");

        var authMethodClaim = context.User.FindFirst(ClaimTypes.AuthenticationMethod);
        authMethodClaim.Should().NotBeNull("the AuthenticationMethod claim must be added when provided");
        authMethodClaim!.Value.Should().Be("Google", "the AuthenticationMethod claim value should match the command's value");

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(c => c.Contains(".Washu.Application"), "an application authentication cookie should be issued");
    }

    [Fact]
    public async Task HandleAsync_WithoutAuthenticationMethod_DoesNotAddAuthenticationMethodClaim()
    {
        var (context, handler) = CreateContextAndHandler();
        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");

        var command = new SignInCommand(user, IsPersistent: false, AuthenticationMethod: null);
        await handler.HandleAsync(command);

        context.User.Should().NotBeNull("the HttpContext.User should be populated");
        context.User.FindFirst(ClaimTypes.AuthenticationMethod).Should().BeNull("no AuthenticationMethod claim should be added when omitted");
    }

    [Fact]
    public async Task HandleAsync_WhenIsPersistentIsTrue_EmitsPersistentCookieWithExpires()
    {
        var (context, handler) = CreateContextAndHandler();
        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");

        var command = new SignInCommand(user, IsPersistent: true, AuthenticationMethod: "Google");
        await handler.HandleAsync(command);

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        var appCookie = setCookieHeaders.FirstOrDefault(c => c != null && c.Contains(".Washu.Application"));
        appCookie.Should().NotBeNull("the application cookie should be issued");
        appCookie!.Should().Match((string? c) => c != null && (c.Contains("expires=") || c.Contains("max-age=")), "a persistent cookie should specify an expiration");
    }

    [Fact]
    public async Task HandleAsync_SignsOutOfExternalScheme_ClearingExternalCookie()
    {
        var (context, handler) = CreateContextAndHandler();

        // Pre-sign in to external scheme to simulate active external authentication
        var externalIdentity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "ext-subject"),
            new Claim(ClaimTypes.Email, "ext@example.com")
        ], IdentityConstants.ExternalScheme);
        await context.SignInAsync(IdentityConstants.ExternalScheme, new ClaimsPrincipal(externalIdentity));

        // Clear response headers recorded so far
        context.Response.Headers.Clear();

        var username = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub_{Guid.NewGuid():N}");
        var command = new SignInCommand(user, IsPersistent: false, AuthenticationMethod: "Google");

        await handler.HandleAsync(command);

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(c => c.Contains(".Washu.Application"), "the application cookie should be set");
        setCookieHeaders.Should().Contain(c => c.Contains(".Washu.External") && (c.Contains("expires=Thu, 01 Jan 1970") || c.Contains("max-age=0")),
            "the external scheme should be signed out and cleared from cookies");
    }
}
