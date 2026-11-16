namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics.CodeAnalysis;
using Washu.Framework.Blazor;
using Washu.Framework.Extensions;

[SuppressMessage("Usage", "BL0013:AuthenticationStateProvider.GetAuthenticationStateAsync used without subscribing to AuthenticationStateChanged")]
public class GetUserService(
    AuthenticationStateProvider provider,
    IMediator mediator,
    NavigationManager navManager
    )
{
    public async Task<ApplicationUserInfo> GetOrRedirectAsync(string returnUrl)
    {
        var state = await provider.GetAuthenticationStateAsync();
        if (state.User is { UserId: { } userId, SecurityStamp: { } securityStamp }
            && await mediator.Send(new FindUserByIdQuery(userId)) is { } user
            && user.SecurityStamp == securityStamp) return ApplicationUserInfo.FromUser(user);
        Redirect(returnUrl);
        return new ApplicationUserInfo();
    }

    public async Task<Guid?> GetUserIdAsync()
    {
        var state = await provider.GetAuthenticationStateAsync();
        return state.User.UserId;
    }
    private void Redirect(string returnUrl) => navManager.NavigateTo($"{ApplicationRoutes.SignIn}?returnUrl={returnUrl}", forceLoad: true);
}