namespace Washu.Framework.Identity.Users;

using Extensions;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

[RequireValidUser]
public sealed record DeleteUserCommand(Guid UserId, uint UserRowVersion, string Email) : ICarriesUserId, ICommand<Result>;

public sealed record UserDeletedNotification(Guid UserId, string Email) : INotification;

public sealed class DeleteUserHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IPublisher publisher,
    ILogger<DeleteUserHandler> logger) : ICommandHandler<DeleteUserCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteUserCommand command, CancellationToken ctx = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var user = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .FirstOrDefaultAsync(ctx);
        if (user is null)
        {
            logger.LogWarning("User with id {UserId} could not be deleted because it was not found", command.UserId);
            return new UserIdNotFoundError();
        }
        var oldEmail = user.Email;
        if (user.Email != command.Email)
            return new ValidationError(["The email provided was incorrect"]);
        if (user.RowVersion != command.UserRowVersion)
            return new ConcurrencyError();
        user.SoftDelete();
        dbContext.UpdateRowVersion(user, command.UserRowVersion);
        await dbContext.SaveChangesAsync(ctx);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} deleted successfully", command.UserId);
        await publisher.Publish(new UserDeletedNotification(command.UserId, oldEmail), ctx);
        return Result.Unit;
    }
}
