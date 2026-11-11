using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users;

using DeleteUser;

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
            return new AccountNotFound();
        }
        dbContext.SetRowVersion(user, command.UserRowVersion);
        user.Username = command.NewName;
        
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "User with id {UserId} has already been modified by another session", command.UserId);
            return new UserAlreadyModified();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException{ SqlState: PostgresErrorCodes.UniqueViolation })
        {
            logger.LogError(ex, "User with id {UserId} tried to change username, but it was already taken", command.UserId);
            return new UsernameTaken();
        }
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} changed username", command.UserId);
        return new ChangeUsernameSuccess();
    }
}

public readonly struct ChangeUsernameSuccess
{
    public const string Message = "Successfully changed username";
}

public readonly struct UsernameTaken
{
    public const string Message = "The username specified is already taken";
}


public readonly union ChangeUsernameResult(ChangeUsernameSuccess, UsernameTaken, UserAlreadyModified, AccountNotFound);