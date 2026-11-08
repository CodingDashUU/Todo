namespace Washu.Framework.UnitTests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Washu.Framework.Blazor;
using Washu.Framework.Identity;
using Washu.Framework.Identity.Entities;
using ZiggyCreatures.Caching.Fusion;

public class GetUserServiceTests
{
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        public string? NavigatedUri { get; private set; }
        public bool? WasForceLoaded { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigatedUri = uri;
            WasForceLoaded = forceLoad;
        }
    }

    private sealed class TestAuthStateProvider(AuthenticationState authState) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(authState);
    }

    private static (GetUserService Service, TestNavigationManager NavManager, IFusionCache Cache) CreateService(AuthenticationState authState)
    {
        var cache = new FusionCache(new FusionCacheOptions());
        var userStore = new ApplicationUserStore(cache, null!);
        var navManager = new TestNavigationManager();
        var provider = new TestAuthStateProvider(authState);
        var service = new GetUserService(provider, userStore, navManager);
        return (service, navManager, cache);
    }

    [Fact]
    public async Task GetOrRedirectAsync_WhenUserIsAnonymous_RedirectsToSignInAndReturnsEmptyInfo()
    {
        var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(anonymousPrincipal);
        var (service, navManager, _) = CreateService(authState);

        var result = await service.GetOrRedirectAsync("/dashboard");

        navManager.NavigatedUri.Should().Be($"{ApplicationRoutes.SignIn}?returnUrl=/dashboard");
        navManager.WasForceLoaded.Should().BeTrue();
        result.Should().NotBeNull();
        result.Id.Should().Be(Guid.Empty);
        result.Username.Should().Be("UNKNOWN");
    }

    [Fact]
    public async Task GetOrRedirectAsync_WhenUserHasNoSecurityStampClaim_RedirectsToSignIn()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], IdentityConstants.ApplicationScheme);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));
        var (service, navManager, _) = CreateService(authState);

        var result = await service.GetOrRedirectAsync("/todos");

        navManager.NavigatedUri.Should().Be($"{ApplicationRoutes.SignIn}?returnUrl=/todos");
        navManager.WasForceLoaded.Should().BeTrue();
        result.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task GetOrRedirectAsync_WhenUserNotFoundInUserStore_RedirectsToSignIn()
    {
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("SecurityStamp", securityStamp.ToString())
        ], IdentityConstants.ApplicationScheme);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));
        var (service, navManager, cache) = CreateService(authState);

        // Prime cache with null so user is not found
        await cache.SetAsync<ApplicationUser?>($"user:{userId}", null);

        var result = await service.GetOrRedirectAsync("/profile");

        navManager.NavigatedUri.Should().Be($"{ApplicationRoutes.SignIn}?returnUrl=/profile");
        navManager.WasForceLoaded.Should().BeTrue();
        result.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task GetOrRedirectAsync_WhenSecurityStampDoesNotMatchUserStore_RedirectsToSignIn()
    {
        var user = new ApplicationUser("Alice", "alice@example.com", "sub-alice");
        var staleSecurityStamp = Guid.NewGuid(); // different from user.SecurityStamp

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SecurityStamp", staleSecurityStamp.ToString())
        ], IdentityConstants.ApplicationScheme);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));
        var (service, navManager, cache) = CreateService(authState);

        await cache.SetAsync($"user:{user.Id}", user);

        var result = await service.GetOrRedirectAsync("/settings");

        navManager.NavigatedUri.Should().Be($"{ApplicationRoutes.SignIn}?returnUrl=/settings");
        navManager.WasForceLoaded.Should().BeTrue();
        result.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task GetOrRedirectAsync_WhenUserIsAuthenticatedAndValid_ReturnsUserInfoWithoutRedirect()
    {
        var user = new ApplicationUser("Bob", "bob@example.com", "sub-bob");
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SecurityStamp", user.SecurityStamp.ToString())
        ], IdentityConstants.ApplicationScheme);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));
        var (service, navManager, cache) = CreateService(authState);

        await cache.SetAsync($"user:{user.Id}", user);

        var result = await service.GetOrRedirectAsync("/dashboard");

        navManager.NavigatedUri.Should().BeNull("Should not redirect when user authentication and security stamp match");
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
        result.Email.Should().Be(user.Email);
    }
}
