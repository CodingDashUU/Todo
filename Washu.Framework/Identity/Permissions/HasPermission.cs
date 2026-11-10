using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;
using ZiggyCreatures.Caching.Fusion;

namespace Washu.Framework.Identity.Permissions;

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
                .ForUser(userId.Value)
                .SelectMany(ur => ur.Role.Permissions)
                .Select(rp => rp.Name)
                .Distinct()
                .ToHashSetAsync(ctx);
        });

        return permissions.Contains(query.PermissionName);
    }
}