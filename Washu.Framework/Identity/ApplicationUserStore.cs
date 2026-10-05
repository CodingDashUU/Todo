namespace Washu.Framework.Identity;

using Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

public sealed class ApplicationUserStore<TDbContext>(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    where TDbContext : ApplicationDbContext
{
    public event Func<Task>? OnUserChanged;
    public async Task<ApplicationUser?> GetOrAddUserAsync(Guid userId)
    {
        if (cache.TryGetValue(userId, out ApplicationUser? cachedUser)) return cachedUser;
        await using var scope = scopeFactory.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<ApplicationUserManager<TDbContext>>();
        var user = await manager.FindByIdAsync(userId);
        if (user is not null) cache.Set(userId, user, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5));
        return user;
    }

    public void RemoveUser(Guid userId) => cache.Remove(userId);
    
    public async Task UserHasChangedAsync()
    {
        var op = OnUserChanged?.Invoke();
        if (op is not null) await op;
    }
}