using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;
using ZiggyCreatures.Caching.Fusion;

namespace Washu.Framework.Identity.Permissions;

using Mediator;

public sealed record HasPermissionQuery(string PermissionName) : IQuery<bool>;
public sealed class HasPermissionHandler(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IHttpContextAccessor httpContextAccessor,
    IFusionCache cache) : IQueryHandler<HasPermissionQuery, bool>
{
    public async ValueTask<bool> Handle(HasPermissionQuery query, CancellationToken ctx)
    {
        var userId = httpContextAccessor.HttpContext?.User.UserId;

        if (!userId.HasValue) return false;

        var cacheKey = $"perms:{userId}";
        var permissions = await cache.GetOrSetAsync(cacheKey, async ictx =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ictx);
            return await db.UserRoles
                .ForUser(userId.Value)
                .SelectMany(ur => ur.Role.Permissions)
                .Select(rp => rp.Name)
                .Distinct()
                .ToHashSetAsync(ictx);
        }, token: ctx);

        return permissions.Contains(query.PermissionName);
    }
}