namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Notifications;
using Npgsql;

public sealed record ChangeUsernameCommand(Guid UserId, string NewName);

public sealed class ChangeUsernameHandler(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<Message> HandleAsync(ChangeUsernameCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null) return Message.Error("Identity Error", "Account was not found");
        if (await dbContext.Users.AnyAsync(u => u.Username == command.NewName && u.Id != command.UserId, ct))
            return Message.Error("Identity Error", "Username provided is already taken");
        user.Username = command.NewName;
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
}