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
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(20);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        if (principal.UserId is not {} userId) return false;
        if (principal.SecurityStamp is not { } securityStamp) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var findUserHandler = scope.ServiceProvider.GetRequiredService<FindUserHandler>();
        var user = await findUserHandler.HandleAsync(new FindUserQuery(userId), cancellationToken);
        if (user is null) return false;
        return securityStamp == user.SecurityStamp;
    }
    
}