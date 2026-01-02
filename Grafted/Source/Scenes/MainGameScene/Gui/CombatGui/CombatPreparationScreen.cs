using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatPreparationScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private readonly GameContext _context;
    private readonly Widget _progressButton;
    private Encounter _encounter;

    public CombatPreparationScreen(ZoneGui gui, GameContext context)
    {
        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;

        _context = context;
        _encounter = CombatGenerator.GenerateForZone(context.PlayerPawn, context.CurrentZone!);
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
    }

    private Widget GenerateControlButtons()
    {
        HorizontalStackPanel panel = new()
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        string text = _encounter.Def.MysteryProperties != null ? "Continue to mystery" : _encounter.AtBoss ? "Boss!" : "Fight!";
        var continueButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = text }
        };
        continueButton.Click += (_, _) =>
        {
            _context.Save();
            Continue();
        };
        panel.Widgets.Add(continueButton);

        var combatConfig = _encounter.Zone.ZoneDef.Encounters[_encounter.Zone.Stage];
        if (combatConfig.Enemies.Count > 1)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{combatConfig.Enemies.First().PawnName} up next!"
            });
        } 

        return panel;
    }

    private void Continue()
    {
        _encounter.Zone.NextEncounter();
    }

    public void Update()
    {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}
