namespace Washu.Framework.Blazor;

using Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Identity;

public sealed class AppAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> optionsAccessor)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    private readonly IdentityOptions _options = optionsAccessor.Value;

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(10);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        var userId = principal.FindUserId();
        if (!userId.HasValue) return false;
        var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
        if (!Guid.TryParse(principalStamp, out var principalStampGuid)) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var userStore = scope.ServiceProvider.GetRequiredService<ApplicationUserStore>();
        var user = await userStore.GetOrAddUserAsync(userId.Value);
        if (user is null) return false;
        return principalStampGuid == user.SecurityStamp;
    }
    
}