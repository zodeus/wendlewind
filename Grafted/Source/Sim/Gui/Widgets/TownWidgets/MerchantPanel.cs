using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.Widgets.EntityWidgets;
using Grafted.Sim.Zones.Handlers;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.TownWidgets;

public class MerchantPanel : HorizontalStackPanel, IUpdatable {
    private readonly MerchantContainerPanel _inventoryPanel;
    private readonly MerchantContainerPanel _merchantPanel;

    public MerchantPanel(Pawn playerPawn, Town town) {
        DefaultProportion = Proportion.Auto;
        _inventoryPanel = new MerchantContainerPanel(playerPawn.Inventory.Entities, town.GetStructure<TownStructureMerchant>()!.Entities, "Inventory", MerchantTransactionType.Sell) {
            Visible = !playerPawn.IsDead, MinHeight = 700, MinWidth = 650
        };

        _merchantPanel = new MerchantContainerPanel(town.GetStructure<TownStructureMerchant>()!.Entities, playerPawn.Inventory.Entities, "Merchant", MerchantTransactionType.Buy) {
            Margin = new Thickness(50, 0, 0, 0),
            Visible = !playerPawn.IsDead, MinHeight = 700, MinWidth = 650
        };
        AddChild(_inventoryPanel);
        AddChild(_merchantPanel);
    }

    public void Update() {
        _inventoryPanel.Update();
        _merchantPanel.Update();
    }
}

public enum MerchantTransactionType {
    Buy,
    Sell
}