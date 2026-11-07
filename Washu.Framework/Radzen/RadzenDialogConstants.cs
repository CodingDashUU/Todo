namespace Washu.Framework.Radzen;

using global::Radzen;

public static class RadzenDialogConstants
{
    public static readonly ConfirmOptions ConfirmDeleteOptions = new()
    {
        Icon = MaterialIcons.Delete,
        IconColor = "var(--rz-danger)",
        OkButtonText = "Yes",
        CancelButtonText = "No" 
    };
}