using Washu.Framework.Identity.Users;
using Washu.Framework.Identity.Users.FindUser;

namespace Washu.Framework.Blazor;

using Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Identity;
using Mediator;

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
        if (principal.UserId is not {} userId) return false;
        if (principal.SecurityStamp is not { } securityStamp) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var user = await mediator.Send(new FindUserQuery(userId), cancellationToken);
        if (user is null) return false;
        return securityStamp == user.SecurityStamp;
    }
    
}