namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

[UsedImplicitly]
public class PawnPanel : EntityPanelBase
{
    private readonly Pawn _pawn;

    //private readonly PawnPortraitPanel _portrait;
    private readonly TabPanel _tabPanel;

    public PawnPanel(BaseGui gui, Pawn pawn, EntityPanelProperties? properties = null) : base(gui, pawn, properties)
    {
        _pawn = pawn;
        MinWidth = 1000;
        MinHeight = 800;
        var pane = new HorizontalStackPanel { Spacing = 20 };
        pane.Proportions.Add(Proportion.Auto);
        pane.Proportions.Add(Proportion.Fill);
        AddChild(pane);

        //_portrait = new PawnPortraitPanel(pawn) { Width = 150 };
        //pane.AddChild(_portrait);
        _tabPanel = new TabPanel();
        pane.AddChild((_tabPanel));

        /*PawnEquipmentGrid equipmentGrid = new(pawn.Equipment, new GridProperties {
            RightClickAction = (grid, slotDef) => {
                List<Entity> tools = pawn.Inventory.Container
                    .Where<Entity>(entity => entity is Item item && (item.ItemDef.EquipmentProperties.SlotsUsedToEquip?.Contains(slotDef) ?? false))
                    .ToList();
                if (tools.Any() == false) {
                    return;
                }

                var toolSelector = new EntitySelector(tools, entity => {
                    /*Item? unEquippedItem = pawn.Equipment.UnEquip(slotDef);
                    if (unEquippedItem != null) {
                        pawn.Inventory.Container.TryAdd(unEquippedItem);
                        //pawnEquipmentGrid.Redraw();
                        //OnPanelChanged();
                    }#1#
                    var returnedItems = pawn.Equipment.TryEquip((entity as Item)!);
                    foreach (Item returnedItem in returnedItems) {
                        pawn.Inventory.Container.TryAdd(returnedItem);
                        grid.Redraw();
                    }

                    //pawnEquipmentGrid.Redraw();
                });
                toolSelector.Show(Core.Sim.ActiveGui!.Desktop, Input.MousePosition.ToPoint() + new Point(-20, -20));
            },
        }) /*{ HorizontalAlignment = HorizontalAlignment.Center }#1#;
        */

     
        //_tabPanel.AddTab("Equipment", null, equipmentGrid);
        //_tabPanel.AddTab("Health", null, new PawnHealthPanel(pawn));
        _tabPanel.AddTab("Skills", new PawnSkillsPanel(pawn.Skills));
        //_tabPanel.AddTab("Stats", null, new PawnStatsPanel(pawn));
        //_tabPanel.AddTab("Labor", null, new PawnLaborSettingsPanel(pawn));
        //_tabPanel.AddTab("Combat", null, new PawnCombatSettingsPanel(pawn));
    }

    public override void Update()
    {
        //_portrait.Update();
        _tabPanel.Update();
    }
}