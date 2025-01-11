using Grafted.Graphics.Textures;
using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Sim.Entities;

namespace Grafted.Sim.LootBoxes;

public class LootBoxPanel : LootBoxPanelBase
{
}

public class LootBoxPanelBase
{
}

public enum LootBoxCategory
{
    Weapons,
    Armor,
    Edibles,
    Trinkets,
    Medicinal,
    Crafting
}

public enum LootBoxRarity
{
    // Most Items
    Primitive,
    IronAge,
    Curious,
    Majestic,
    Celestial,
    GodBlessed,
    Dangerous,

    // Foods
    Morsel,
    Snack,
    Meal,
    Bounty,
    SummerHarvest,
    
    //Trinkets
    Basic,
    Normal,
}

public enum LootBoxCollectionType
{
    Random,
    All
}

public class LootBoxTrapProperties
{
}

public class LootBoxItem
{
    public ItemDef ItemDef = null!;
    public RangeInt Amount = new(1, 1);
    public float Weight = 1;
    public float ChanceToDrop = 1;
}

public class LootBoxDef : Def
{
    private Texture2D? _texture;
    private Texture2D? _iconTexture;

    public Type UiClass = typeof(LootBoxPanel);
    public LootBoxCategory Category;
    public LootBoxRarity Rarity;
    public RangeInt CollectionLimit;
    public LootBoxTrapProperties? TrapProperties;
    public List<LootBoxItem> Items = [];
    public string? TexturePath;

    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;
    public virtual Texture2D Icon => _iconTexture ??= TexturePath != null ? TextureUtils.PreMultiply(Texture)! : BaseContent.Textures.BadTexture;

    public LootBoxPanelBase UiPanelFor(BaseGui gui, Entity entity, EntityPanelProperties? properties = null)
    {
        return (LootBoxPanelBase)Activator.CreateInstance(UiClass, gui, entity, properties)!;
    }
}