namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class EnchantmentsPanel : PrepCard, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly PrepItemGrid _inventory;

    public EnchantmentsPanel(BaseGui gui, Pawn pawn) : base("Enchantments")
    {
        _gui = gui;
        _pawn = pawn;
        UseFixedBody();
        VerticalAlignment = VerticalAlignment.Top;

        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Click, then a highlighted weapon or armor",
            TextColor = new Color(160, 160, 160),
            Wrap = true
        });

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.ItemType == ItemType.Enchantment,
            AttachEnchantment,
            EnchantmentTooltip,
            isHighlighted: enchantment => EnchantmentSocketing.EnchantmentHasCompatibleHost(pawn, enchantment),
            pagedRow: true);
        Body.Widgets.Add(_inventory);
    }

    public void Update()
    {
        _inventory.Update();
    }

    private string EnchantmentTooltip(Item item)
    {
        return EnchantmentSocketing.EnchantmentHasCompatibleHost(_pawn, item)
            ? "Click, then click a highlighted weapon or armor"
            : "No empty compatible socket";
    }

    private void AttachEnchantment(Item item)
    {
        if (item.IsDestroyed)
        {
            return;
        }

        _gui.MouseAttachment = new MouseAttachment(
            _gui,
            item.GetIcon(),
            leftClickAction: null,
            updateAction: attachment =>
            {
                if (Mouse.GetState().RightButton == ButtonState.Pressed)
                {
                    attachment.Detach();
                }
            })
        {
            Data = item,
            IconSize = new Size(BaseContent.IconSizes.ExtraLarge, BaseContent.IconSizes.ExtraLarge)
        };
    }
}
