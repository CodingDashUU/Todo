namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications;

public sealed record DeleteUserCommand(Guid UserId);

public sealed class DeleteUserHandler(IDbContextFactory<ApplicationDbContext> factory, ILogger<DeleteUserHandler> logger)
{
    public async Task<DeleteUserResult> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var rowsDeleted = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .ExecuteDeleteAsync(ct);
        if (rowsDeleted == 0)
        {
            logger.LogError("User with id {UserId} could not be deleted because it was not found", command.UserId);
            return DeleteUserResult.AccountNotFound;
        }
        logger.LogInformation("User with id {UserId} deleted successfully", command.UserId);
        return DeleteUserResult.Success;
    }
}

public enum DeleteUserResult : byte { Success, AccountNotFound }

