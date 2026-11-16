namespace Washu.Framework.Identity.Login;

using Blazor;
using Extensions;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public sealed record SignOutCommand(HttpContext HttpContext) : ICommand;
public class SignOutHandler(
    ILogger<SignOutHandler> logger,
    IPublisher publisher
    ) : ICommandHandler<SignOutCommand>
{
    public async ValueTask<Unit> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        var context = command.HttpContext;
        var userId = context.User.UserId;
        if (!userId.HasValue)
            logger.LogWarning("There is no UserId claim for connection {ConnectionId}", context.Connection.Id);
        await publisher.Publish(new UserSigningOut(context, false), cancellationToken);
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        if (!logger.IsEnabled(LogLevel.Information)) return Unit.Value;
        if (userId.HasValue)
            logger.LogInformation("User with id {UserId} signed out", userId);
        else logger.LogInformation("User with connection {ConnectionId} signed out",
            context.Connection.Id);
        return Unit.Value;
    }
}