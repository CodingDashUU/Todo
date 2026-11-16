namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

public sealed record DeleteUserCommand(Guid UserId) : ICommand<DeleteUserResult>;

public readonly record struct UserDeleted(Guid UserId) : INotification;

public sealed class DeleteUserHandler(
    IDbContextFactory<ApplicationDbContext> factory, 
    IPublisher publisher,
    ILogger<DeleteUserHandler> logger) : ICommandHandler<DeleteUserCommand, DeleteUserResult>
{
    public async ValueTask<DeleteUserResult> Handle(DeleteUserCommand command, CancellationToken ctx = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var user = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .FirstOrDefaultAsync(ctx);
        if (user is null)
        {
            logger.LogError("User with id {UserId} could not be deleted because it was not found", command.UserId);
            return new AccountNotFound();
        }
        user.SoftDelete();
        await dbContext.SaveChangesAsync(ctx);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} deleted successfully", command.UserId);
        await publisher.Publish(new UserDeleted(command.UserId), ctx);
        return new DeleteUserSuccess();
    }
}

public readonly struct DeleteUserSuccess
{
    public const string Message = "Successfully deleted account";
}
public readonly union DeleteUserResult(DeleteUserSuccess, AccountNotFound);

