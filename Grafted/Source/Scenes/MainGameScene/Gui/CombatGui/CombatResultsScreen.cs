using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatResultsScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private readonly GameContext _context;
    private readonly Widget _progressButton;
    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatResultsScreen(ZoneGui gui, GameContext context)
    {
        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;

        _context = context;
        _gameHud = new GameHud(gui, context) { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new PawnPreparationPanel(gui, context.World.Player.Pawn)
        {
            MaxHeight = 1100, // 1440p
            Margin = new Thickness(0, 0, 0, 0)
        };

        _progressButton = GenerateControlButtons();
        _progressButton.HorizontalAlignment = HorizontalAlignment.Center;

        Widgets.Add(_gameHud);
        Widgets.Add(_pawnPanel);
        Widgets.Add(_progressButton);
        if (Encounter.Def.PotentialLootBoxes.Count != 0)
        {
            Widgets.Add(new LootBoxSelectionScreen(this, context, Encounter.Def.PotentialLootBoxes, Encounter.Def.MaxBoxes));
            _pawnPanel.Visible = false;
            _progressButton.Visible = false;
        }
    }

    public void ShowScreen()
    {
        _pawnPanel.Visible = true;
        _progressButton.Visible = true;
    }

    private Widget GenerateControlButtons()
    {
        HorizontalStackPanel panel = new()
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        if (Encounter.AtBoss)
        {
            var nextZoneButton = new Button(BaseContent.Styles.Button.Large) { Content = new Label { Text = "Continue to next zone" } };
            nextZoneButton.Click += (_, _) =>
            {
                _context.Save();
                Encounter.Zone.Exit();
            };
            panel.Widgets.Add(nextZoneButton);
        }
        else
        {
            var continueButton = new Button(BaseContent.Styles.Button.Large) { Content = new Label { Text = Encounter.AtBoss ? "Boss!" : "Fight!" } };
            continueButton.Click += (_, _) =>
            {
                _context.Save();
                MoveToNextCombat();
            };
            panel.Widgets.Add(continueButton);
        }

        if (Encounter.AtBoss == false)
        {
            var combatConfig = Encounter.Zone.ZoneDef.Encounters[Encounter.Zone.Stage];
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{combatConfig.Enemies.First().PawnName} up next!"
            });
        }

        return panel;
    }

    private void MoveToNextCombat()
    {
        Encounter.Zone.NextEncounter();
    }

    public void Update()
    {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}