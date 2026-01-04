namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class MysteryScreen : VerticalStackPanel
{
    private readonly WheelOfParts? _wheel;

    public MysteryScreen(ZoneGui gui, Pawn playerPawn, MysteryProperties shrine)
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        if (playerPawn.Body.AllExternalParts.Any(p => p.Sockets.Any(s => s.AttachedPart == null)) == false)
        {
            CommingSoon(gui);
            return;
        }

        _wheel = new WheelOfParts(playerPawn, shrine);
        _wheel.OnSkipped += () => gui.LeaveMystery();
        Widgets.Add(_wheel);
    }

    public void Update(float deltaTime)
    {
        _wheel?.Update(deltaTime);
    }

    private void CommingSoon(ZoneGui gui)
    {
        var button = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = "Coming soon" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };
        button.Click += (_, _) => gui.LeaveMystery();
        Widgets.Add(button);
    }
}