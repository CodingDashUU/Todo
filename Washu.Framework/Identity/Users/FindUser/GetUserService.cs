using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Washu.Framework.Blazor;
using Washu.Framework.Extensions;

namespace Washu.Framework.Identity.Users.FindUser;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Usage", "BL0013:AuthenticationStateProvider.GetAuthenticationStateAsync used without subscribing to AuthenticationStateChanged")]
public class GetUserService(
    AuthenticationStateProvider provider,
    CachedFindUserHandler cachedFindUserHandler,
    NavigationManager navManager
    )
{
    public async Task<ApplicationUserInfo> GetOrRedirectAsync(string returnUrl)
    {
        var state = await provider.GetAuthenticationStateAsync();
        if (state.User is { UserId: { } userId, SecurityStamp: { } securityStamp }
            && await cachedFindUserHandler.HandleAsync(userId) is { } user
            && user.SecurityStamp == securityStamp) return ApplicationUserInfo.FromUser(user);
        Redirect(returnUrl);
        return new ApplicationUserInfo();
    }
    private void Redirect(string returnUrl) => navManager.NavigateTo($"{ApplicationRoutes.SignIn}?returnUrl={returnUrl}", forceLoad: true);
}