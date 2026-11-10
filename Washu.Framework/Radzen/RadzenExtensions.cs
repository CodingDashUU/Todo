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
}