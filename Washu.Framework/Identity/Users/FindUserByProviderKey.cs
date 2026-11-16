namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using ZiggyCreatures.Caching.Fusion;

public sealed record FindUserByProviderKeyQuery(string ProviderKey) : IQuery<ApplicationUser?>;

public sealed class FindUserByProviderKeyHandler(
    IDbContextFactory<ApplicationDbContext> factory
) : IQueryHandler<FindUserByProviderKeyQuery, ApplicationUser?>
{
    public async ValueTask<ApplicationUser?> Handle(
        FindUserByProviderKeyQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.GoogleSubject == query.ProviderKey, cancellationToken);
    }
}