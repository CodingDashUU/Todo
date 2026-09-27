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
        await using var scope = scopeFactory.CreateAsyncScope();
        var currentUserService = scope.ServiceProvider.GetRequiredService<CurrentUserService<TDbContext>>();
        var user = await currentUserService.GetCurrentUserAsync();
        if (user is null) return false;
        var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
        if (!Guid.TryParse(principalStamp, out var principalStampGuid)) return false;
        return principalStampGuid == user.SecurityStamp;
    }
    
}