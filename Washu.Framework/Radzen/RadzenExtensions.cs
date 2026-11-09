namespace Washu.Framework.Radzen;

using global::Radzen;
using Notifications;

public static class RadzenExtensions
{
    extension(DialogService service)
    {
        public async Task<bool> ConfirmDelete(string title, string text)
        {
            var data = await service.OpenAsync<ConfirmDeleteDialog>(title, 
                new Dictionary<string, object?> { [nameof(Radzen.ConfirmDeleteDialog.Text)] = text }, 
                new DialogOptions { Width = "480px", Resizable = false, Draggable = true, Icon = MaterialIcons.Delete, IconColor = "var(--rz-danger)" });
            if (data is bool result) return result;
            return false;
        }
    }
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