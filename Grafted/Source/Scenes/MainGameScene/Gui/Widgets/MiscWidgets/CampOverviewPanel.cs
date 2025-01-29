using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class CampOverviewPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public CampOverviewPanel(BaseGui gui, GameContext context)
    {
        var playerPawn = context.PlayerPawn;
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities, null
        ) { MinHeight = 700, MaxHeight = 1000, Width = 600, VerticalAlignment = VerticalAlignment.Stretch };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel rightColumn = new()
        {
            Visible = !playerPawn.IsDead,
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(new PawnSkillsPanel(playerPawn.Skills) { Margin = new Thickness(0, 50, 0, 20) });

        HorizontalStackPanel grid = new()
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                ZonePanel(context.World),
                _bodyPanel,
                new VerticalStackPanel
                {
                    Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Fill },
                    Spacing = 5,
                    Widgets =
                    {
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Combat, item => gui.ViewEntity(item)),
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.NonCombat, item => gui.ViewEntity(item)),
                        _inventoryPanel
                    }
                },
                rightColumn
            }
        };
        Widgets.Add(grid);
    }

    public void Update()
    {
        _bodyPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }

    private Widget ZonePanel(World world)
    {
        var peacefulMeadow = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.PeacefulMeadow.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = !world.GetZone(Defs.Biomes.PeacefulMeadow).IsComplete
        };
        peacefulMeadow.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.PeacefulMeadow).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var outskirts = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.TheOutskirts.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.PeacefulMeadow).IsComplete && !world.GetZone(Defs.Biomes.TheOutskirts).IsComplete
        };
        outskirts.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.TheOutskirts).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var grainMill = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.GrainMill.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.TheOutskirts).IsComplete && !world.GetZone(Defs.Biomes.GrainMill).IsComplete
        };
        grainMill.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.GrainMill).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var festerpusSwamp = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.FesterpusSwamp.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.GrainMill).IsComplete && !world.GetZone(Defs.Biomes.FesterpusSwamp).IsComplete
        };
        festerpusSwamp.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.FesterpusSwamp).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var forgottenForest = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.ForgottenForest.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.FesterpusSwamp).IsComplete && !world.GetZone(Defs.Biomes.ForgottenForest).IsComplete
        };
        forgottenForest.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.ForgottenForest).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var dampCave = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.DampCave.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.ForgottenForest).IsComplete && !world.GetZone(Defs.Biomes.DampCave).IsComplete
        };
        dampCave.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.DampCave).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var cemetery = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.Cemetery.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.DampCave).IsComplete && !world.GetZone(Defs.Biomes.Cemetery).IsComplete
        };
        cemetery.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.Cemetery).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var mineShafts = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Biomes.Mineshaft.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Biomes.Cemetery).IsComplete && !world.GetZone(Defs.Biomes.Mineshaft).IsComplete
        };
        mineShafts.Click += (_, _) => { new ZoneStartWindow(Defs.Biomes.Mineshaft).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        return new VerticalStackPanel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10,
            Padding = new Thickness(20),
            Widgets =
            {
                new VerticalStackPanel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(15),
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Large) { Text = "Village of the Damned", HorizontalAlignment = HorizontalAlignment.Center }
                    }
                },
                peacefulMeadow,
                outskirts,
                grainMill,
                festerpusSwamp,
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Alchemist Hut", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                forgottenForest,
                dampCave,
                cemetery,
                mineShafts
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgemaster's Quarry", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Mage Tower", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Field of Vegetables", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Blood Court", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "His Rectory", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Scarlet Chapel", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Steamy Oil Vents", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
            }
        };
    }
}