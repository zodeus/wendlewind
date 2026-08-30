using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatResultsScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private readonly GameContext _context;
    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatResultsScreen(ZoneGui gui, GameContext context)
    {
        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;

        _context = context;
        _gameHud = new GameHud(gui, context) { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new PawnPreparationPanel(gui, context.World.Player.Pawn);
        _pawnPanel.SetControls(CreateZoneControls());

        Widgets.Add(_gameHud);
        Widgets.Add(_pawnPanel);
        if (Encounter.Def.PotentialLootBoxes.Count != 0)
        {
            Widgets.Add(new LootBoxSelectionScreen(this, context, Encounter.Def.PotentialLootBoxes, Encounter.Def.MaxBoxes));
            _pawnPanel.Visible = false;
        }
    }

    public void ShowScreen()
    {
        _pawnPanel.Visible = true;
    }

    private Widget CreateZoneControls()
    {
        var controlsWrapper = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        var zoneControls = ZoneControls.ForResults(
            Encounter,
            onContinue: () =>
            {
                _context.Save();
                Encounter.Zone.NextEncounter();
            },
            onExit: () =>
            {
                _context.Save();
                Encounter.Zone.Exit();
            }
        );
        controlsWrapper.Widgets.Add(zoneControls);

        return controlsWrapper;
    }

    public void Update()
    {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}