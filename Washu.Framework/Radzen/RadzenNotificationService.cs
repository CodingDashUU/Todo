namespace Washu.Framework.Radzen;

using global::Radzen;
using Notifications;
using System.Collections.Frozen;

public sealed class RadzenNotificationService(NotificationService service)
{
    private static readonly FrozenDictionary<MessageType, NotificationSeverity> Severities =
        new Dictionary<MessageType, NotificationSeverity>
        {
            [MessageType.Info] = NotificationSeverity.Info,
            [MessageType.Error] = NotificationSeverity.Error,
            [MessageType.Success] = NotificationSeverity.Success
        }.ToFrozenDictionary();

    public const short Duration = 4000;
    public void Show(Message message)
    {
        var severity =  Severities.GetValueOrDefault(message.Type, NotificationSeverity.Error);
        service.Notify(new NotificationMessage
        {
            Summary = message.Title,
            Severity = severity,
            ShowProgress = true,
            Duration = Duration,
            Detail = message.Details,
            CloseOnClick = true,
            Style = "position: fixed; top: auto; bottom: 20px; left: 40px; right: auto;",
        });
    }
}