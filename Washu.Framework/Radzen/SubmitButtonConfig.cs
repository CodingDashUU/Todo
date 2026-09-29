namespace Washu.Framework.Radzen;

using global::Radzen;

public class SubmitButtonConfig
{
    public string ButtonText { get; set; } = "Submit";
    public bool ButtonDisabled { get; set; }
    public ButtonStyle ButtonStyle { get; set; }
    public string ButtonIcon { get; set; } = "";
}