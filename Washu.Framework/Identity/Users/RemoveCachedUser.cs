namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.Extensions.Logging;
using Washu.Framework.Extensions;
using ZiggyCreatures.Caching.Fusion;

public class RemoveCachedUserHandler(
    IFusionCache cache,
    ILogger<RemoveCachedUserHandler> logger
    ) : INotificationHandler<UserDeletedNotification>, 
        INotificationHandler<UserUpdatedNotification>, 
        INotificationHandler<UserNotFoundNotification>, 
        INotificationHandler<UserSignedInNotification>,  
        INotificationHandler<UserSignedOutNotification>
{
    public async ValueTask Handle(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserSignedInNotification notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserSignedOutNotification notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserNotFoundNotification notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }
}

public readonly record struct UserSignedInNotification(Guid UserId) : INotification;
public readonly record struct UserSignedOutNotification(Guid UserId) : INotification;