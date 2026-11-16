namespace Washu.Framework.Identity.Login;

using Extensions;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Persistence;
using System.Security.Claims;
using Users;

public sealed record UserSigningIn(HttpContext HttpContext, Guid UserId) : INotification;
public sealed record UserSigningOut(HttpContext HttpContext, bool ChangeStamp) : INotification;
public class AuthAuditingHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<AuthAuditingHandler> logger
    ) : INotificationHandler<UserSigningIn>, INotificationHandler<UserSigningOut>
{
    public async ValueTask Handle(UserSigningIn notification, CancellationToken cancellationToken)
    {
        var userId = notification.UserId;
        var context = notification.HttpContext;
        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        var rowsChanged = await dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => 
                    s.SetProperty(u => u.LastSignedInDate, DateTimeOffset.UtcNow),
                cancellationToken
            );
        if (rowsChanged != 1)
        {
            await context.SignOutAsync(
                IdentityConstants.ApplicationScheme);
            logger.LogWarning(
                "Could not update 'LastSignedInDate' for user with id {UserId}. Rows changed: {RowsChanged}",
                userId, rowsChanged);
        }

        else if (logger.IsEnabled(LogLevel.Information)) logger.LogInformation("User with id {UserId} signing in", userId);

        var publisher = context.RequestServices
            .GetRequiredService<IPublisher>();
        await publisher.Publish(new UserSignedIn(userId), cancellationToken);
    }

    public async ValueTask Handle(UserSigningOut notification, CancellationToken cancellationToken)
    {
        var context = notification.HttpContext;
        var userId = context.User.UserId;
        if (!userId.HasValue)
        {
            logger.LogError(
                "No userId found in ClaimsPrincipal for connection {Id}",
                context.Connection.Id);
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rowsChanged = await dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s =>
                {
                    s.SetProperty(u => u.LastSignedOutDate, DateTimeOffset.UtcNow);
                    s.SetProperty(u => u.SecurityStamp, Guid.CreateVersion7());
                },
                cancellationToken
            );
        if (rowsChanged == 1 && logger.IsEnabled(LogLevel.Information) && notification.ChangeStamp)
            logger.LogInformation("Changed security stamp for user with id {UserId}", userId);
        if (rowsChanged != 1)
            logger.LogWarning(
                "Could not update auditing for user with id {UserId}. Rows changed: {RowsChanged}",
                userId, rowsChanged);

        else if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Updated auditing for user with id {UserId}", userId);

        var publisher = context.RequestServices
            .GetRequiredService<IPublisher>();
        await publisher.Publish(new UserSignedOut(userId.Value), cancellationToken);
    }
}