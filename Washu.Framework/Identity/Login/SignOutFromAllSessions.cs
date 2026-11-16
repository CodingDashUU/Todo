namespace Washu.Framework.Identity.Login;

using Extensions;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public sealed record SignOutFromAllSessionsCommand(HttpContext HttpContext) : ICommand;
public sealed class SignOutFromAllSessionsHandler(
    ILogger<SignOutFromAllSessionsHandler> logger,
    IPublisher publisher
    ) : ICommandHandler<SignOutFromAllSessionsCommand>
{
    public async ValueTask<Unit> Handle(SignOutFromAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var context = command.HttpContext;
        var userId = context.User.UserId;
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        await publisher.Publish(new UserSigningOut(context,  true), cancellationToken);
        if (userId.HasValue && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with id {UserId} signed out from all sessions", userId);
        return Unit.Value;
    }
}