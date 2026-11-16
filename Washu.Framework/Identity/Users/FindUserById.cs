namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using ZiggyCreatures.Caching.Fusion;

public sealed record FindUserByIdQuery(Guid UserId) : IQuery<ApplicationUser?>;

public sealed class FindUserByIdHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IFusionCache cache
    ) : IQueryHandler<FindUserByIdQuery, ApplicationUser?>
{
    public ValueTask<ApplicationUser?> Handle(
        FindUserByIdQuery query, 
        CancellationToken ct = default) =>
        cache.GetOrSetAsync($"user:{query.UserId}",
            async ctx =>
            {
                await using var dbContext = await factory.CreateDbContextAsync(ctx);
                return await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == query.UserId, ctx);
            }, token: ct);
}