namespace Washu.Framework.Blazor;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Identity;

public sealed class AppAuthenticationStateProvider<TDbContext>(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> optionsAccessor)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
where TDbContext : ApplicationDbContext
{
    private readonly IdentityOptions _options = optionsAccessor.Value;

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(10);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        if (principal.Identity is not {IsAuthenticated: true}) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ApplicationUserManager<TDbContext>>();
        var userId = principal.FindFirstValue(_options.ClaimsIdentity.UserIdClaimType);
        if (userId is null || !Guid.TryParse(userId, out var userIdGuid)) return false;
        var user = await userManager.FindByIdAsync(userIdGuid, cancellationToken);
        // Check if they exist or are not banned
        if (user is not {IsBanned: false}) return false;
        // Check if the security stamp is the same
        var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
        if (!Guid.TryParse(principalStamp, out var principalStampGuid)) return false;
        return principalStampGuid == user.SecurityStamp;
    }
    
}