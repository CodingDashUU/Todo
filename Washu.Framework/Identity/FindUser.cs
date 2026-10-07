namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;

public sealed record FindUserQuery(Guid UserId);

public sealed class FindUserHandler(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<ApplicationUser?> HandleAsync(FindUserQuery query, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.SingleOrDefaultAsync(u => u.Id == query.UserId, ct);
    }
}