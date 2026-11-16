namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.Extensions.Logging;
using Washu.Framework.Extensions;
using ZiggyCreatures.Caching.Fusion;

public class RemoveCachedUserHandler(
    IFusionCache cache,
    ILogger<RemoveCachedUserHandler> logger
    ) : INotificationHandler<UserDeleted>, 
        INotificationHandler<UserUpdated>, 
        INotificationHandler<UserSignedIn>,  
        INotificationHandler<UserSignedOut>
{
    public async ValueTask Handle(UserDeleted notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserUpdated notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserSignedIn notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }

    public async ValueTask Handle(UserSignedOut notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"user:{notification.UserId}", token: cancellationToken);
        logger.LogInformation("Removed {UserId} from cache", notification.UserId);
    }
}

public readonly record struct UserSignedIn(Guid UserId) : INotification;
public readonly record struct UserSignedOut(Guid UserId) : INotification;