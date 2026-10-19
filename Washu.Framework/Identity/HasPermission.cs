namespace Washu.Framework.Identity;

using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

public sealed record HasPermissionQuery(string PermissionName);
public sealed class HasPermissionHandler(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IHttpContextAccessor httpContextAccessor,
    IFusionCache cache)
{
    public async Task<bool> HandleAsync(HasPermissionQuery query)
    {
        var userId = httpContextAccessor.HttpContext?.User.UserId;

        if (!userId.HasValue) return false;

        var cacheKey = $"perms:{userId}";
        var permissions = await cache.GetOrSetAsync(cacheKey, async ctx =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ctx);
            return await db.UserRoles
                .Where(ur => ur.UserId == userId.Value)
                .SelectMany(ur => ur.Role.Permissions)
                .Select(rp => rp.Name)
                .Distinct()
                .ToHashSetAsync(ctx);
        });

        return permissions.Contains(query.PermissionName);
    }
}