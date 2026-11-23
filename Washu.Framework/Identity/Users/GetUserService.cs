namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Washu.Framework.Blazor;
using Washu.Framework.Extensions;

public sealed class GetUserService(
    CurrentUser currentUser,
    ISender sender,
    NavigationManager navManager)
{
    public async Task<ApplicationUserInfo> GetOrRedirectAsync()
    {
        var principal = await currentUser.GetUserAsync();
        if (principal is { UserId: { } userId, SecurityStamp: { } securityStamp }
            && await sender.Send(new FindUserByIdQuery(userId)) is { } user
            && user.SecurityStamp == securityStamp) return ApplicationUserInfo.FromUser(user);
        Redirect(ApplicationRoutes.Home);
        return new ApplicationUserInfo();
    }
    private void Redirect(string returnUrl) => navManager.NavigateTo($"{ApplicationRoutes.SignIn}?returnUrl={returnUrl}", forceLoad: true);
}
