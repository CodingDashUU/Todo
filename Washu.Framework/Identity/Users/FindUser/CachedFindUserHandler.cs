using ZiggyCreatures.Caching.Fusion;

namespace Washu.Framework.Identity.Users.FindUser;

public sealed class CachedFindUserHandler(IFusionCache cache, FindUserHandler handler)
{
    public ValueTask<ApplicationUser?> HandleAsync(Guid userId) =>
        cache.GetOrSetAsync($"user:{userId}", 
            async ctx => await handler.HandleAsync(new FindUserQuery(userId), ctx));
    public async ValueTask RemoveUserAsync(Guid userId) => await cache.RemoveAsync($"user:{userId}");
}