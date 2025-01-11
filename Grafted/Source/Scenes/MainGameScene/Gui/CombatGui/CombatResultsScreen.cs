using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatResultsScreen : VerticalStackPanel
{
    private readonly LootPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private readonly GameContext _context;
    private readonly Widget _progressButton;
    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatResultsScreen(ZoneGui gui, GameContext context)
    {
        _context = context;
        _gameHud = new GameHud(context.World.Player) { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new LootPanel(gui, context.World.PlayerPawn, Encounter.Loot)
        {
            Margin = new Thickness(0, 100, 0, 0)
        };

        _progressButton = GenerateProgressButton();
        _progressButton.HorizontalAlignment = HorizontalAlignment.Center;

        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;

        Widgets.Add(_gameHud);
        Widgets.Add(_pawnPanel);
        Widgets.Add(_progressButton);

        if (Encounter.Config.PotentialLootBoxes.Any())
        {
            var box = Encounter.Config.PotentialLootBoxes.RandomElement();
            var lootBoxPanel = new LootBoxScreen(this, context, box);
            Widgets.Add(lootBoxPanel);
            _pawnPanel.Visible = false;
            _progressButton.Visible = false;
        }
    }

    public void ShowResultsScreen()
    {
        _pawnPanel.Visible = true;
        _progressButton.Visible = true;
    }

    private Widget KillsButton()
    {
        ImageButton image = new(BaseContent.Styles.Button.Large)
        {
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull], Width = 76, Height = 76,
            Padding = new Thickness(10)
        };
        image.TouchDown += (_, _) => { new PlayerKillsWindow(_context.DeathRecords).Show(Desktop); };
        return image;
    }

    private Widget GenerateProgressButton()
    {
        HorizontalStackPanel buttons = new()
        {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        if (Encounter.AtBoss)
        {
            TextButton campButton = new(BaseContent.Styles.Button.Large) { Text = "Return to camp" };
            campButton.Click += (_, _) =>
            {
                DoAutoLoot();
                Encounter.Zone!.Exit();
            };
            buttons.AddChild(campButton);
        }
        else
        {
            TextButton continueButton = new(BaseContent.Styles.Button.Large) { Text = "Fight!" };
            continueButton.Click += (_, _) =>
            {
                DoAutoLoot();
                MoveToNextCombat();
            };
            buttons.AddChild(continueButton);
        }


        return new HorizontalStackPanel { Spacing = 10, Widgets = { KillsButton(), buttons } };
    }

    private void MoveToNextCombat()
    {
        Encounter.Zone!.NextEncounter();
    }

    private bool DoAutoLoot()
    {
        var player = _context.PlayerPawn;
        var items = Encounter.Loot.AsItems().ToList();
        for (var index = items.Count - 1; index >= 0; index--)
        {
            var item = items[index];
            player.Inventory.Entities.TryAdd(item);
        }

        return true;
    }

    public void HandleInput()
    {
    }

    public void Update()
    {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}