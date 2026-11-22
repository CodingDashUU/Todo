namespace Washu.Framework.Identity.Users;

using Extensions;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

[RequireValidUser]
public sealed record DeleteUserCommand(Guid UserId, uint UserRowVersion, string Email) : ICommand<Result>;

public readonly record struct UserDeletedNotification(Guid UserId) : INotification;

public sealed class DeleteUserHandler(
    IDbContextFactory<ApplicationDbContext> factory, 
    IPublisher publisher,
    ILogger<DeleteUserHandler> logger,
    CurrentUser currentUser) : ICommandHandler<DeleteUserCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteUserCommand command, CancellationToken ctx = default)
    {
        if ((await currentUser.GetUserAsync()).UserId != command.UserId)
            return new UserNotFoundError();

        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var user = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .FirstOrDefaultAsync(ctx);
        if (user is null)
        {
            logger.LogWarning("User with id {UserId} could not be deleted because it was not found", command.UserId);
            return new UserIdNotFoundError();
        }
        if (user.Email != command.Email)
            return new ValidationError(["The email provided was incorrect"]);
        if (user.RowVersion != command.UserRowVersion)
            return new ConcurrencyError();
        user.SoftDelete();
        dbContext.UpdateRowVersion(user, command.UserRowVersion);
        await dbContext.SaveChangesAsync(ctx);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} deleted successfully", command.UserId);
        await publisher.Publish(new UserDeletedNotification(command.UserId), ctx);
        return Result.Unit;
    }
}
