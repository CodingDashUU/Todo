using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId);

public sealed class DeleteUserHandler(IDbContextFactory<ApplicationDbContext> factory, ILogger<DeleteUserHandler> logger)
{
    public async Task<DeleteUserResult> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .FirstOrDefaultAsync(ct);
        if (user is null)
        {
            logger.LogError("User with id {UserId} could not be deleted because it was not found", command.UserId);
            return new AccountNotFound();
        }
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(ct);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} deleted successfully", command.UserId);
        return new DeleteUserSuccess();
    }
}

public readonly struct DeleteUserSuccess
{
    public const string Message = "Successfully deleted account";
}
public readonly union DeleteUserResult(DeleteUserSuccess, AccountNotFound);

