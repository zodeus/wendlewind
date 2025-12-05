namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class ShrineScreen : VerticalStackPanel
{
    public ShrineScreen(ZoneGui gui, Pawn playerPawn, ShrineProperties shrine)
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        var wheel = new WheelOfParts(playerPawn, shrine);
        wheel.OnSkipped += () => gui.LeaveShrine();
        Widgets.Add(wheel);
    }
}