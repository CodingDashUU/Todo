namespace Washu.Framework.Notifications;

public interface INotificationService
{
    public void Send(Message message);
}