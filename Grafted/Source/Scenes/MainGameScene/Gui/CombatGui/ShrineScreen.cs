namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class ShrineScreen : VerticalStackPanel
{
    private readonly WheelOfParts _wheel;

    public ShrineScreen(ZoneGui gui, Pawn playerPawn, ShrineProperties shrine)
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        _wheel = new WheelOfParts(playerPawn, shrine);
        _wheel.OnSkipped += () => gui.LeaveShrine();
        Widgets.Add(_wheel);
    }

    public void Update(float deltaTime)
    {
        _wheel.Update(deltaTime);
    }
}