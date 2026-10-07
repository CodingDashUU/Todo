namespace Washu.Framework.Identity;

using Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

public sealed class ApplicationUserStore(IMemoryCache cache, FindUserHandler handler)
{
    public event Func<Task>? OnUserChanged;
    public async Task<ApplicationUser?> GetOrAddUserAsync(Guid userId) =>
        await cache.GetOrCreateAsync(userId, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await handler.HandleAsync(new FindUserQuery(userId));
        });

    public void RemoveUser(Guid userId) => cache.Remove(userId);
    
    public async Task UserHasChangedAsync()
    {
        var op = OnUserChanged?.Invoke();
        if (op is not null) await op;
    }
}