namespace Washu.Framework.Identity.Users;

using Mediator;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;using TickerQ.Utilities.Interfaces.Managers;

public class SendAccountDeletionEmailHandle(
    TimeTickerEntityQueue queue
    ) : INotificationHandler<UserDeletedNotification>
{
    public ValueTask Handle(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        queue.TryEnqueue(new TimeTickerEntity
        {
            Function = nameof(AccountDeletionAlertJob),
            ExecutionTime = DateTime.UtcNow.AddSeconds(1),
            Request =
                TickerHelper.CreateTickerRequest(new AccountDeletionAlertContext(notification.UserId,
                    notification.Email)),
            Retries = 5,
            RetryIntervals = [10, 30, 60, 90, 120]
        });
        return ValueTask.CompletedTask;
    }
}