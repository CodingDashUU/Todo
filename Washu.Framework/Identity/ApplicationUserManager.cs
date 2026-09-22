namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;

public sealed class ApplicationUserManager<TDbContext>(IDbContextFactory<TDbContext> factory, UserSessionManager manager)
    where TDbContext : ApplicationDbContext
{

    public async Task<ApplicationUser?> FindByUsernameAsync(string userName, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Username == userName, ct);
    }

    public async Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Message> ChangeUsernameAsync(ChangeUsernameModel model, CancellationToken ct = default)
    {
        if (model.NewName == model.OldName) return Message.Info("Identity Info", "Username has not been modified");
        var validator = new Username.Validator();
        var result = await validator.ValidateAsync(model.NewName, ct);
        if (!result.IsValid)
        {
            var errorMessage = result.Errors.First();
            return Message.Error("Invalid Username", errorMessage.ErrorMessage);
        }
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == model.OldName, ct);
        if (user is null) return Message.Error("Identity Error", "Account with the given user name not found");
        user.ChangeUsername(model.NewName);
        await dbContext.SaveChangesAsync(ct);
        return Message.Success("Identity Success", "Successfully updated username");
    }
    public async Task<Message> DeleteByUsernameAsync(string userName, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == userName, ct);
        if (user is null) return Message.Error("Identity Error", "Account with the given user name not found");
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(ct);
        manager.Invalidate(user.Id);
        return Message.Success("Identity Success", "Successfully deleted account");
    }
}