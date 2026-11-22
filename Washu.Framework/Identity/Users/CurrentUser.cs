namespace Washu.Framework.Identity.Users;

using Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

/// <summary>
/// Provides the current circuit's principal to handlers.
/// The principal is loaded on demand because child components may issue mediator
/// requests before an asynchronously initialized layout has finished rendering.
/// </summary>
public sealed class CurrentUser : IDisposable
{
    private readonly AuthenticationStateProvider _provider;
    private Task<AuthenticationState>? _cachedAuthenticationState;

    public CurrentUser(AuthenticationStateProvider provider)
    {
        _provider = provider;
        _provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public ClaimsPrincipal User { get; private set; } = new();

    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        var state = await (_cachedAuthenticationState ??= _provider.GetAuthenticationStateAsync());
        return User = state.User;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) =>
        _cachedAuthenticationState = task;

    public void Dispose() => _provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
}