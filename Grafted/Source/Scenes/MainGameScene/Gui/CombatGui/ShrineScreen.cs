namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class MysteryScreen : VerticalStackPanel
{
    private readonly WheelOfParts _wheel;

    public MysteryScreen(ZoneGui gui, Pawn playerPawn, MysteryProperties shrine)
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        _wheel = new WheelOfParts(playerPawn, shrine);
        _wheel.OnSkipped += () => gui.LeaveMystery();
        Widgets.Add(_wheel);
    }

    public void Update(float deltaTime)
    {
        _wheel.Update(deltaTime);
    }
}