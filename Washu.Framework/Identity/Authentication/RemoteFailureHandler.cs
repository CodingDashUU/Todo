namespace Washu.Framework.Identity.Authentication;

using Blazor;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Notifications;

public sealed record RemoteFailureNotification(RemoteFailureContext Context) : INotification;
public class RemoteFailureHandler(
    MessageStore store,
    ILogger<RemoteFailureHandler> logger
    ) : INotificationHandler<RemoteFailureNotification>
{
    public ValueTask Handle(RemoteFailureNotification notification, CancellationToken cancellationToken)
    {
        var context = notification.Context;
        logger.LogWarning(context.Failure, "An error occured while signing in remotely the user with connection ID {ConnectionId}", context.HttpContext.Connection.Id);
        var id = store.Store(Message.Error("External Identity Error",
            "There was an unknown error while signing into your account. Please try again"));
        context.Response.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}");
        context.HandleResponse();
        return ValueTask.CompletedTask;
    }
}