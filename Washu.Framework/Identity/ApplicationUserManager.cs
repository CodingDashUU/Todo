namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;

public sealed class ApplicationUserManager<TDbContext>(IDbContextFactory<TDbContext> factory)
    where TDbContext : ApplicationDbContext
{
    public async Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<IdentityMessage> ChangeUsernameAsync(string oldName, string newName, CancellationToken ct = default)
    {
        if (newName == oldName) return new IdentityMessage(Message.Info("Identity Info", "Username has not been modified"));
        var validator = new Username.Validator();
        var result = await validator.ValidateAsync(newName, ct);
        if (!result.IsValid)
        {
            var errorMessage = result.Errors.First();
            return new IdentityMessage(Message.Error("Invalid Username", errorMessage.ErrorMessage));
        }
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == oldName, ct);
        if (user is null) return new IdentityMessage(Message.Error("Identity Error", "Account with the given user name not found"), true);
        if (await dbContext.Users.AnyAsync(u => u.Username == newName, ct))
            return new IdentityMessage(Message.Error("Identity Error", "Username provided is already taken"));
        user.ChangeUsername(newName);
        await dbContext.SaveChangesAsync(ct);
        return new IdentityMessage(Message.Success("Identity Success", "Successfully updated username"), true);
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