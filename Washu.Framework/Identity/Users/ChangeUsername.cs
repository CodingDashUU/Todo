namespace Washu.Framework.Identity.Users;

using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Persistence;
using Washu.Framework.Extensions;

[UniqueViolationMessage("This username is taken")]
public sealed record ChangeUsernameCommand(Guid UserId, uint UserRowVersion, string OldName, string NewName) : ICommand<Result>;

public sealed class ChangeUsernameHandler(
    IDbContextFactory<ApplicationDbContext> factory, 
    IPublisher publisher,
    ILogger<ChangeUsernameHandler> logger) : ICommandHandler<ChangeUsernameCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeUsernameCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            logger.LogWarning("User with id {UserId} not found", command.UserId);
            await publisher.Publish(new UserNotFoundNotification(command.UserId), ct);
            return UserErrors.UserNotFound;
        }

        if (user.RowVersion != command.UserRowVersion)
            return GenericErrors.ConcurrencyError;
        if (dbContext.Users.Any(u => u.Username == command.NewName))
            return Error.ValidationError("Generic.AlreadyExists", "One or more validation errors occured",
                ["This username is taken"]);
        dbContext.UpdateEntity(user, command.UserRowVersion);
        user.Username = command.NewName;
        await dbContext.SaveChangesAsync(ct);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} changed username", command.UserId);
        await publisher.Publish(new UserUpdatedNotification(command.UserId), ct);
        return Result.Unit;
    }
}
public readonly record struct UserUpdatedNotification(Guid UserId) : INotification;
public readonly record struct UserNotFoundNotification(Guid UserId) : INotification;

public class ChangeUsernameValidator : AbstractValidator<ChangeUsernameCommand>
{
    public ChangeUsernameValidator()
    {
        RuleFor(x => x.NewName)
            .SetValidator(new UsernameValidator());
            RuleFor(x => x)
            .Must(x => !x.NewName.Equals(x.OldName, StringComparison.InvariantCultureIgnoreCase)).WithMessage("New username cannot be equivalent to the old one");
    }
}