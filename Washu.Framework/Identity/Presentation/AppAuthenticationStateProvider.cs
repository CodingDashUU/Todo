namespace Washu.Framework.Identity.Presentation;

using Mediator;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Users;
using Washu.Framework.Extensions;

internal sealed class AppAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(20);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        if (principal.UserId is not {} userId 
            || principal.SecurityStamp is not { } securityStamp) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var user = await sender.Send(new FindUserByIdQuery(userId), cancellationToken);
        if (user is null) return false;
        return securityStamp == user.SecurityStamp;
    }
    
}