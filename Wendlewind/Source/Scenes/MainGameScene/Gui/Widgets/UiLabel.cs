namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets;

internal static class UiLabel
{
    public static void Set(Label label, string text)
    {
        if (!string.Equals(label.Text, text, StringComparison.Ordinal))
        {
            label.Text = text;
        }
    }

    public static void Set(Label label, string text, Color color)
    {
        Set(label, text);
        if (label.TextColor != color)
        {
            label.TextColor = color;
        }
    }

    public static void SetColor(Label label, Color color)
    {
        if (label.TextColor != color)
        {
            label.TextColor = color;
        }
    }
}
