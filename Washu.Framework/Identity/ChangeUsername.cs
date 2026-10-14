namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Notifications;
using Npgsql;

public sealed record ChangeUsernameCommand(Guid UserId, Guid UserVersionId, string NewName);

public sealed class ChangeUsernameHandler(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<ChangeUsernameResult> HandleAsync(ChangeUsernameCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null) return ChangeUsernameResult.AccountNotFound;
        if (user.VersionId != command.UserVersionId) return ChangeUsernameResult.UserAlreadyModified;
        if (await dbContext.Users.AnyAsync(u => u.Username == command.NewName && u.Id != command.UserId, ct))
            return ChangeUsernameResult.UsernameTaken;
        user.Username = command.NewName;
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException{ SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return ChangeUsernameResult.UsernameTaken;
        }

        return ChangeUsernameResult.Success;
    }
}

public enum ChangeUsernameResult : byte { Success, UsernameTaken, UserAlreadyModified, AccountNotFound }