using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class CampOverviewPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public CampOverviewPanel(BaseGui gui, World world)
    {
        var playerPawn = world.PlayerPawn;
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities, null
        ) { Visible = !playerPawn.IsDead, MinHeight = 700, Width = 700 };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel rightColumn = new()
        {
            Visible = !playerPawn.IsDead,
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.Widgets.Add(new PawnSkillsPanel(playerPawn.Skills));
        rightColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large) { Text = "Effects", Margin = new Thickness(0, 20, 0, 0) });
        rightColumn.Widgets.Add(new VerticalStackPanel
        {
            Widgets =
            {
                new Label { Text = "  - Tarred Blood" },
                new Label { Text = "  - Synthetic Arteries" },
                new Label { Text = "  - Random Bits" },
                new Label { Text = "  - Arterial Thoughness" },
                new Label { Text = "  - Blood Bloated" },
                new Label { Text = "  - Weaved Ligaments" },
                new Label { Text = "  - Elven Grace" },
                new Label { Text = "  - God Touched" },
                new Label { Text = "  - Marked by a God" },
                new Label { Text = "  - Vampire Thirst" },
                new Label { Text = "  - Rushing River" },
            }
        });
        rightColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large) { Text = "Achievements", Margin = new Thickness(0, 20, 0, 0) });
        rightColumn.Widgets.Add(new VerticalStackPanel
        {
            Widgets =
            {
                new Label { Text = "  - Tickled brain" },
                new Label { Text = "  - Tis' but a scratch!" },
                new Label { Text = "  - Just the tip" },
                new Label { Text = "  - Two! I don't need two!" },
                new Label { Text = "  - Noticed by a god" },
                new Label { Text = "  - Blood chugger" },
                new Label { Text = "  - Vampire Wannabe" },
                new Label { Text = "  - Rushing River" },
            }
        });
        HorizontalStackPanel grid = new()
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                ZonePanel(world),
                _bodyPanel,
                _inventoryPanel,
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
            Content = new Label { Text = Defs.Zones.PeacefulMeadow.Label, HorizontalAlignment = HorizontalAlignment.Center},  
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = !world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete
        };
        peacefulMeadow.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.PeacefulMeadow).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var outskirts = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.TheOutskirts.Label, HorizontalAlignment = HorizontalAlignment.Center},  
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete && !world.GetZone(Defs.Zones.TheOutskirts).IsComplete
        };
        outskirts.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.TheOutskirts).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var grainMill = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.GrainMill.Label, HorizontalAlignment = HorizontalAlignment.Center},  
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.TheOutskirts).IsComplete && !world.GetZone(Defs.Zones.GrainMill).IsComplete
        };
        grainMill.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.GrainMill).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

        var festerpusSwamp = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = Defs.Zones.FesterpusSwamp.Label, HorizontalAlignment = HorizontalAlignment.Center}, 
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = world.GetZone(Defs.Zones.GrainMill).IsComplete && !world.GetZone(Defs.Zones.FesterpusSwamp).IsComplete
        };
        festerpusSwamp.Click += (_, _) => { new ZoneStartWindow(Defs.Zones.FesterpusSwamp).ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint()); };

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
                festerpusSwamp
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Alchemist Hut", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgotten Forest", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
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