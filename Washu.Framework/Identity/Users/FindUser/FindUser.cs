using Microsoft.EntityFrameworkCore;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users.FindUser;

public sealed record FindUserQuery(Guid UserId);

public sealed class FindUserHandler(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<ApplicationUser?> HandleAsync(FindUserQuery query, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == query.UserId, ct);
    }
}