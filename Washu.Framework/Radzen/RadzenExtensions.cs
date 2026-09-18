namespace Washu.Framework.Radzen;

using global::Radzen;
using Notifications;

public static class RadzenExtensions
{
    extension(NotificationService service)
    {
        public void Show(Message message)
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
                Duration = RadzenNotificationConstants.Duration,
                Detail = message.Details,
                CloseOnClick = true,
                Style = "position: fixed; top: auto; bottom: 20px; left: 40px; right: auto;",
            });
        }
    }
}