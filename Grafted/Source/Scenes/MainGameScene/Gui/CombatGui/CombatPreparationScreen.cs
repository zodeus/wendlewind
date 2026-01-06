using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatPreparationScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private readonly GameContext _context;
    private readonly Encounter _encounter;

    public CombatPreparationScreen(ZoneGui gui, GameContext context)
    {
        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;

        _context = context;
        _encounter = CombatGenerator.GenerateForZone(context.PlayerPawn, context.CurrentZone!);
        _gameHud = new GameHud(gui, context) { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new PawnPreparationPanel(gui, context.World.Player.Pawn);
        _pawnPanel.SetControls(CreateZoneControls());

        Widgets.Add(_gameHud);
        Widgets.Add(_pawnPanel);

        SetProportionType(_gameHud, ProportionType.Auto);
        SetProportionType(_pawnPanel, ProportionType.Fill);
    }

    private Widget CreateZoneControls()
    {
        var controlsWrapper = new Panel
        {
            Margin = new Thickness(0, 0, 0, 20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var zoneControls = new ZoneControls(_encounter, Continue);
        controlsWrapper.Widgets.Add(zoneControls);

        return controlsWrapper;
    }

    private void Continue()
    {
        _context.Save();
        _encounter.Zone.NextEncounter();
    }

    public void Update()
    {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}
