namespace Washu.Framework.Identity.Users.FindUser;

using DeleteUser;
using Mediator;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using Extensions;
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