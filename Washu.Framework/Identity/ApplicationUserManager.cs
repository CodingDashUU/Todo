namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;
using Npgsql;

public sealed class ApplicationUserManager(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<bool> DoesUserExists(string username, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.AnyAsync(u => u.Username == username, ct);
    }
}