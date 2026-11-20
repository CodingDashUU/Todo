namespace Washu.Framework.Identity.Users;

using Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class CurrentUser : IDisposable
{
    private readonly AuthenticationStateProvider _provider;
    private Task<AuthenticationState>? _cachedAuthStateTask;

    public CurrentUser(AuthenticationStateProvider provider)
    {
        _provider = provider;
        _provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) =>
        _cachedAuthStateTask = task;

    public async Task<Guid?> GetUserIdAsync()
    {
        // Use cached task or fetch fresh state
        _cachedAuthStateTask ??= _provider.GetAuthenticationStateAsync();
        
        var state = await _cachedAuthStateTask;
        return state.User.UserId;
    }

    public void Dispose() => _provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
}