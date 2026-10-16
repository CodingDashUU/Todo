namespace Washu.Framework.Identity;

using Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

public sealed class ApplicationUserStore(IFusionCache cache, FindUserHandler handler)
{
    public async Task<ApplicationUser?> GetOrAddUserAsync(Guid userId) =>
        await cache.GetOrSetAsync($"user:{userId}", 
            async ctx => await handler.HandleAsync(new FindUserQuery(userId), ctx)
        ,new FusionCacheEntryOptions(duration: TimeSpan.FromMinutes(5)));

    public void RemoveUser(Guid userId) => cache.Remove($"user:{userId}");
}