namespace Washu.Framework.UnitTests;

using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Washu.Framework.Blazor;
using Washu.Framework.Identity;
using Washu.Framework.Identity.Entities;
using ZiggyCreatures.Caching.Fusion;

public class AppAuthenticationStateProviderTests
{
    private static async Task<bool> InvokeValidateAuthenticationStateAsync(
        AppAuthenticationStateProvider provider,
        AuthenticationState authState)
    {
        var method = typeof(AppAuthenticationStateProvider).GetMethod(
            "ValidateAuthenticationStateAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull("ValidateAuthenticationStateAsync method must exist");

        var task = (Task<bool>)method.Invoke(provider, [authState, CancellationToken.None])!;
        return await task;
    }

    private static (AppAuthenticationStateProvider Provider, IFusionCache Cache) CreateProvider()
    {
        var services = new ServiceCollection();
        var fusionCache = new FusionCache(new FusionCacheOptions());
        services.AddSingleton<IFusionCache>(fusionCache);

        // Dummy FindUserHandler (not invoked when values are primed in cache)
        services.AddScoped<FindUserHandler>(_ => null!);
        services.AddScoped(sp => new ApplicationUserStore(sp.GetRequiredService<IFusionCache>(), sp.GetService<FindUserHandler>()!));

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var provider = new AppAuthenticationStateProvider(
            NullLoggerFactory.Instance,
            scopeFactory);

        return (provider, fusionCache);
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_ReturnsFalse_WhenUserIdClaimIsMissing()
    {
        var (provider, _) = CreateProvider();
        var identity = new ClaimsIdentity([
            new Claim("SecurityStamp", Guid.NewGuid().ToString())
        ]);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));

        var result = await InvokeValidateAuthenticationStateAsync(provider, authState);

        result.Should().BeFalse("Missing UserId claim must fail authentication validation");
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_ReturnsFalse_WhenSecurityStampClaimIsMissing()
    {
        var (provider, _) = CreateProvider();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ]);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));

        var result = await InvokeValidateAuthenticationStateAsync(provider, authState);

        result.Should().BeFalse("Missing SecurityStamp claim must fail authentication validation");
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_ReturnsFalse_WhenUserNotFoundInStore()
    {
        var (provider, cache) = CreateProvider();
        var missingUserId = Guid.NewGuid();
        // Prime the cache with null so it returns null without calling database
        await cache.SetAsync<ApplicationUser?>($"user:{missingUserId}", null);

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, missingUserId.ToString()),
            new Claim("SecurityStamp", Guid.NewGuid().ToString())
        ]);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));

        var result = await InvokeValidateAuthenticationStateAsync(provider, authState);

        result.Should().BeFalse("When user is absent from database/cache, authentication validation must fail");
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_ReturnsFalse_WhenSecurityStampDoesNotMatch()
    {
        var (provider, cache) = CreateProvider();
        var user = new ApplicationUser("TestUser", "test@example.com", "sub-123");
        await cache.SetAsync($"user:{user.Id}", user);

        var staleSecurityStamp = Guid.NewGuid(); // different from user.SecurityStamp
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SecurityStamp", staleSecurityStamp.ToString())
        ]);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));

        var result = await InvokeValidateAuthenticationStateAsync(provider, authState);

        result.Should().BeFalse("When claim SecurityStamp differs from persisted user SecurityStamp, validation must fail");
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_ReturnsTrue_WhenSecurityStampMatches()
    {
        var (provider, cache) = CreateProvider();
        var user = new ApplicationUser("TestUser", "test@example.com", "sub-123");
        await cache.SetAsync($"user:{user.Id}", user);

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SecurityStamp", user.SecurityStamp.ToString())
        ]);
        var authState = new AuthenticationState(new ClaimsPrincipal(identity));

        var result = await InvokeValidateAuthenticationStateAsync(provider, authState);

        result.Should().BeTrue("Matching UserId and SecurityStamp must successfully validate");
    }
}
