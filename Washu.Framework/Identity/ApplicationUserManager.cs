namespace Washu.Framework.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Notifications;

public class ApplicationUserManager<TDbContext>(IDbContextFactory<TDbContext> factory)
    where TDbContext : ApplicationDbContext
{

    public async Task<ApplicationUser?> FindByUsernameAsync(string userName, CancellationToken ct = default)
    {
        var normalizedUsername = userName.ToUpperInvariant();
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, ct);
    }

    public async Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    }
}