namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal static class PrepSlots
{
    public const int Size = 44;
    public const int Pad = 3;
    public const int Spacing = 4;

    public static Panel Frame(Widget content)
    {
        return new Panel
        {
            Width = Size,
            Height = Size,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(Pad),
            Widgets = { content }
        };
    }
}
