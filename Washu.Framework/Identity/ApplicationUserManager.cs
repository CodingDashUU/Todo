namespace Washu.Framework.Identity;

using Entities;
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

    public async Task<Message> DeleteByUsernameAsync(string userName, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == userName, ct);
        if (user is null) return Message.Error("Identity Error", "Account with the given user name not found");
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(ct);
        return Message.Success("Identity Success", "Successfully deleted account");
    }
}