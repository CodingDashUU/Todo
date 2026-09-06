namespace Washu.Framework.Radzen;

using global::Radzen;
using Washu.Framework.Notifications;

public class RadzenNotificationService(NotificationService service) : INotificationService
{
    public void NotifyMessage(string summary, string detail, MessageType type)
    {
        var severity = type switch
        {
            MessageType.Error => NotificationSeverity.Error,
            MessageType.Info => NotificationSeverity.Info,
            MessageType.Success => NotificationSeverity.Success,
            _ => NotificationSeverity.Error
        };
        service.Notify(new NotificationMessage
        {
            Summary = summary,
            Severity = severity,
            ShowProgress = true,
            Duration = 4000,
            Detail = detail,
            CloseOnClick = true,
            Style = "position: fixed; top: auto; bottom: 20px; left: 40px; right: auto;",
        });
    }
    public void NotifyError(string summary, string detail) =>
        NotifyMessage(summary, detail, MessageType.Error);
    public void NotifyInfo(string summary, string detail) =>
        NotifyMessage(summary, detail, MessageType.Info);
    public void NotifySuccess(string summary, string detail) =>
        NotifyMessage(summary, detail, MessageType.Success);
}