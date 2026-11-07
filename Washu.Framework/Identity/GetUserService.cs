namespace Washu.Framework.Identity;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Blazor;
using Extensions;

public class GetUserService(
    AuthenticationStateProvider provider,
    ApplicationUserStore userStore,
    NavigationManager navManager
    )
{
    public async Task<ApplicationUserInfo> GetOrRedirectAsync(string returnUrl)
    {
        var state = await provider.GetAuthenticationStateAsync();
        if (state.User is { UserId: { } userId, SecurityStamp: { } securityStamp }
            && await userStore.GetOrSetUserAsync(userId) is { } user
            && user.SecurityStamp == securityStamp) return ApplicationUserInfo.FromUser(user);
        Redirect(returnUrl);
        return new ApplicationUserInfo();
    }
    private void Redirect(string returnUrl) => navManager.NavigateTo($"{ApplicationRoutes.SignIn}?returnUrl={returnUrl}", forceLoad: true);
}