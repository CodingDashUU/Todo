using Microsoft.EntityFrameworkCore;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users.FindUser;

using Mediator;
using ZiggyCreatures.Caching.Fusion;

public sealed record FindUserQuery(Guid UserId) : IQuery<ApplicationUser?>;

public sealed class FindUserHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IFusionCache cache
    ) : IQueryHandler<FindUserQuery, ApplicationUser?>
{
    public ValueTask<ApplicationUser?> Handle(
        FindUserQuery query, 
        CancellationToken ct = default) =>
        cache.GetOrSetAsync($"user:{query.UserId}",
            async ctx =>
            {
                await using var dbContext = await factory.CreateDbContextAsync(ctx);
                return await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == query.UserId, ctx);
            }, token: ct);
}