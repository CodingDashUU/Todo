using Radzen;
using Washu.Framework.Notifications;

namespace Washu.Framework.Radzen.Notifications;

public static class NotificationExtensions
{
    extension(NotificationService service)
    {
        public void Show(Message message, ushort duration = RadzenNotificationConstants.Duration)
        {
            var severity = message.Type switch
            {
                MessageType.Error => NotificationSeverity.Error,
                MessageType.Info => NotificationSeverity.Info,
                MessageType.Success => NotificationSeverity.Success,
                _ => NotificationSeverity.Error
            };
            service.Notify(new NotificationMessage
            {
                Summary = message.Title,
                Severity = severity,
                ShowProgress = true,
                Duration = duration,
                Detail = message.Details,
                CloseOnClick = true,
                Style = "position: fixed; top: auto; bottom: 20px; left: 40px; right: auto;",
            });
        }
    }
}