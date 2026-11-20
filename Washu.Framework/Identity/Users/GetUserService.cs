namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using Washu.Framework.Blazor;
using Washu.Framework.Extensions;

public sealed class GetUserService : IDisposable
{
    private readonly AuthenticationStateProvider _provider;
    private Task<AuthenticationState>? _cachedAuthStateTask;
    private readonly IMediator _mediator;
    private readonly NavigationManager _navManager;

    public GetUserService(AuthenticationStateProvider provider,
        IMediator mediator,
        NavigationManager navManager)
    {
        _provider = provider;
        _mediator = mediator;
        _navManager = navManager;
        _provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) =>
        _cachedAuthStateTask = task;

    public async Task<ApplicationUserInfo> GetOrRedirectAsync()
    {
        var state = await (_cachedAuthStateTask ?? _provider.GetAuthenticationStateAsync());
        var principal = state.User;
        if (principal is { UserId: { } userId, SecurityStamp: { } securityStamp }
            && await _mediator.Send(new FindUserByIdQuery(userId)) is { } user
            && user.SecurityStamp == securityStamp) return ApplicationUserInfo.FromUser(user);
        Redirect(ApplicationRoutes.Home);
        return new ApplicationUserInfo();
    }

    public void Dispose() => _provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    private void Redirect(string returnUrl) => _navManager.NavigateTo($"{ApplicationRoutes.SignIn}?returnUrl={returnUrl}", forceLoad: true);
}