namespace Washu.Framework.Notifications;

public interface INotificationService
{
    public void NotifyMessage(string summary, string detail, MessageType type);
}