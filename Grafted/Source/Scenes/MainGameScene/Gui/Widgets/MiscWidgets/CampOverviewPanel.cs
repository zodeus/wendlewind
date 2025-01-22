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
        ) { MinHeight = 700, Width = 600,VerticalAlignment = VerticalAlignment.Stretch};

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel rightColumn = new()
        {
            Visible = !playerPawn.IsDead,
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(new PawnSkillsPanel(playerPawn.Skills) { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large) { Text = "Effects", Margin = new Thickness(0, 20, 0, 0) });
        rightColumn.Widgets.Add(new VerticalStackPanel
        {
            Widgets =
            {
                new Label { Text = "  - Tarred Blood" },
                new Label { Text = "  - Synthetic Arteries" },
                new Label { Text = "  - Random Bits" },
                new Label { Text = "  - Arterial Toughness" },
                new Label { Text = "  - Blood Bloated" },
                new Label { Text = "  - Carbon Weaved Ligaments" },
                new Label { Text = "  - Elven Grace" },
                new Label { Text = "  - God Touched" },
                new Label { Text = "  - Marked by a God" },
                new Label { Text = "  - Vampire Thirst" },
                new Label { Text = "  - Trinket Sniffer" },
            }
        });
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
            Content = new Label { Text = Defs.Zones.PeacefulMeadow.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = !world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete
        };
        peacefulMeadow.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.PeacefulMeadow).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var outskirts = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.TheOutskirts.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete && !world.GetZone(Defs.Zones.TheOutskirts).IsComplete
        };
        outskirts.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.TheOutskirts).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var grainMill = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.GrainMill.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.TheOutskirts).IsComplete && !world.GetZone(Defs.Zones.GrainMill).IsComplete
        };
        grainMill.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.GrainMill).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var festerpusSwamp = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.FesterpusSwamp.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.GrainMill).IsComplete && !world.GetZone(Defs.Zones.FesterpusSwamp).IsComplete
        };
        festerpusSwamp.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.FesterpusSwamp).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var forgottenForest = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.ForgottenForest.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.FesterpusSwamp).IsComplete && !world.GetZone(Defs.Zones.ForgottenForest).IsComplete
        };
        forgottenForest.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.ForgottenForest).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var dampCave = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.DampCave.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.ForgottenForest).IsComplete && !world.GetZone(Defs.Zones.DampCave).IsComplete
        };
        dampCave.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.DampCave).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };
    
        var cemetery = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.Cemetery.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.DampCave).IsComplete && !world.GetZone(Defs.Zones.Cemetery).IsComplete
        };
        cemetery.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.Cemetery).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

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
                cemetery
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