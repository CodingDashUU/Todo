namespace Washu.Framework.Identity;

using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications;
using Npgsql;

public sealed record ChangeUsernameCommand(Guid UserId, uint UserRowVersion, string NewName);

public sealed class ChangeUsernameHandler(IDbContextFactory<ApplicationDbContext> factory, ILogger<ChangeUsernameHandler> logger)
{
    public async Task<ChangeUsernameResult> HandleAsync(ChangeUsernameCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            logger.LogError("User with id {UserId} not found", command.UserId);
            return ChangeUsernameResult.AccountNotFound;
        }
        dbContext.SetRowVersion(user, command.UserRowVersion);
        if (await dbContext.Users.AnyAsync(u => u.Username == command.NewName && u.Id != command.UserId, ct))
        {
            logger.LogError("User with id {UserId} tried to change username, but it was already taken", command.UserId);
            return ChangeUsernameResult.UsernameTaken;
        }
        user.Username = command.NewName;
        
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "User with id {UserId} has already been modified by another session", command.UserId);
            return ChangeUsernameResult.UserAlreadyModified;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException{ SqlState: PostgresErrorCodes.UniqueViolation })
        {
            logger.LogError(ex, "User with id {UserId} tried to change username, but it was already taken", command.UserId);
            return ChangeUsernameResult.UsernameTaken;
        }
        logger.LogInformation("User with id {UserId} changed username", command.UserId);
        return ChangeUsernameResult.Success;
    }
}

public enum ChangeUsernameResult : byte { Success, UsernameTaken, UserAlreadyModified, AccountNotFound }