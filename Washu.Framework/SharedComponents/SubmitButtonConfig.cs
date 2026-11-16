namespace Washu.Framework.SharedComponents;

using Radzen;

public sealed class SubmitButtonConfig
{
    public string ButtonText { get; init; } = "Submit";
    public bool ButtonDisabled { get; init; }
    public ButtonStyle ButtonStyle { get; init; }
    public string ButtonIcon { get; init; } = "";
}