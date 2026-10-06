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
    public async Task<Message> DeleteByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Message.Error("Identity Error", "Account with the given user name not found");
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(ct);
        return Message.Success("Identity Success", "Successfully deleted account");
    }
}