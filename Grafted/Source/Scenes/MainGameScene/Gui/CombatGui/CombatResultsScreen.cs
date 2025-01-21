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
        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;
        
        _context = context;
        _gameHud = new GameHud(gui, context) { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new LootPanel(gui, context.World.PlayerPawn, Encounter.Loot)
        {
            //MaxHeight = 1200, // 1440p
            //MaxHeight = 1600, // 1440p
            Margin = new Thickness(0, 80, 0, 0)
        };

        _progressButton = GenerateControlButtons();
        _progressButton.HorizontalAlignment = HorizontalAlignment.Center;

        Widgets.Add(_gameHud);
        Widgets.Add(_pawnPanel);
        Widgets.Add(_progressButton);
        //SetProportionType(_gameHud, ProportionType.Auto);
       // SetProportionType(_pawnPanel, ProportionType.Fill);
       // SetProportionType(_progressButton, ProportionType.Auto);
        if (Encounter.Def.PotentialLootBoxes.Count != 0)
        {
            var box = Encounter.Def.PotentialLootBoxes.RandomElement();
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
            TextButton campButton = new(BaseContent.Styles.Button.Large) { Text = "Return to camp" };
            campButton.Click += (_, _) =>
            {
                DoAutoLoot();
                Encounter.Zone!.Exit();
            };
            panel.Widgets.Add(campButton);
        }
        else
        {
            TextButton continueButton = new(BaseContent.Styles.Button.Large) { Text = Encounter.AtBoss? "Boss!": "Fight!" };
            continueButton.Click += (_, _) =>
            {
                DoAutoLoot();
                MoveToNextCombat();
            };
            panel.Widgets.Add(continueButton);
        }

        if (Encounter.AtBoss == false)
        {
            var combatConfig = DefRepository<EncounterDef>.Defs
                .Where(d => d.Biome == Encounter.Zone!.BiomeDef)
                .Take(new Range(Encounter.Zone!.Stage, Encounter.Zone.Stage + 1))
                .First();
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