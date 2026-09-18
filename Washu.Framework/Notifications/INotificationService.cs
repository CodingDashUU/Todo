namespace Washu.Framework.Notifications;

public interface INotificationService
{
    void Send(Message message);
}