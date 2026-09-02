using Wendlemire.NetCode;
using Wendlemire.Sim.Cosmetics;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class NamePlateWidget : Label
{
    public NamePlateWidget(string name, string? moniker = null, Action? onClick = null)
    {
        var def = CosmeticCatalog.Get(moniker) ?? CosmeticCatalog.Get(ArenaMarks.DefaultNamePlate);
        Text = name;
        Height = 44;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(12);
        TextColor = def?.TextColor ?? new Color(203, 184, 150);
        Background = ResolveFrame(def);

        if (onClick != null)
        {
            TouchDown += (_, _) => onClick();
        }
    }

    private static IBrush ResolveFrame(CosmeticDef? def)
    {
        var key = string.IsNullOrWhiteSpace(def?.FrameAtlasKey)
            ? BaseContent.Styles.Atlas.Panel.MediumFrame
            : def!.FrameAtlasKey;
        try
        {
            return Stylesheet.Current.Atlas[key];
        }
        catch
        {
            return Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        }
    }
}
