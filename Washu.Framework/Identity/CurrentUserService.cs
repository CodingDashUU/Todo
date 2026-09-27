namespace Washu.Framework.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using Entities;
using Extensions;
using Microsoft.AspNetCore.Http;

public class CurrentUserService<TDbContext>( 
    ApplicationUserManager<TDbContext> userManager, IHttpContextAccessor httpContextAccessor)
where TDbContext : ApplicationDbContext
{
    private ApplicationUser? _cachedUser;
    public event Func<Task>? OnUserChanged;
    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (_cachedUser is not null) return _cachedUser;
        var httpContext = httpContextAccessor.HttpContext;
        var userClaims = httpContext?.User;

        if (userClaims?.Identity is not {IsAuthenticated: true}) return null;
        
        var userId = userClaims.FindUserId();

        if (userId.HasValue)
            _cachedUser = await userManager.FindByIdAsync(userId.Value);

        return _cachedUser;
    }
    public void ClearCache() => _cachedUser = null;

    public async Task UserHasChangedAsync()
    {
        var op = OnUserChanged?.Invoke();
        if (op is not null) await op;
    }
}