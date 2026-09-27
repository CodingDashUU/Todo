namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;
using Npgsql;

public sealed class ApplicationUserManager<TDbContext>(IDbContextFactory<TDbContext> factory)
    where TDbContext : ApplicationDbContext
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
    public async Task<Message> ChangeUsernameAsync(string oldName, string newName, Guid userId, CancellationToken ct = default)
    {
        if (newName == oldName) Message.Info("Identity Info", "Username has not been modified");
        var validator = new Username.Validator();
        var result = await validator.ValidateAsync(newName, ct);
        if (!result.IsValid)
        {
            var errorMessage = result.Errors.First();
            return Message.Error("Invalid Username", errorMessage.ErrorMessage);
        }
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Message.Error("Identity Error", "Account was not found");
        if (await dbContext.Users.AnyAsync(u => u.Username == newName && u.Id != userId, ct))
            return Message.Error("Identity Error", "Username provided is already taken");
        user.Username = newName;
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException{ SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Message.Error("Identity Error","That username or sign-in identity is already in use.");
        }
        return Message.Success("Identity Success", "Successfully updated username");
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