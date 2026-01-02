

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global

namespace Grafted.Sim.Combat;

public class EncounterProperties
{
    public ShrineProperties? ShrineProperties = null;
    public List<EncounterEnemyRecord> Enemies = new();
    public List<LootBoxDef> PotentialLootBoxes = new();
    public int? MaxBoxes;
    public bool IsBoss;
}

public class ShrineProperties
{
    private Texture2D? _texture;

    public RangeInt PartsToRestore;
    public List<BodyPartType> RestorablePartTypes = [];

    public string? TexturePath;
    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;
    public string GodLabel = "";
}

public class EncounterEnemyRecord
{
    public PawnDef PawnDef = null!;
    public string PawnName = "undefined";
    public PawnLoadoutDef Loadout = null!;
    public float SpawnWeight = 1;
    public float BodySizeFactor = 1;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
    public List<SkillValueRecord> Skills = new();
    public List<BodyEffectDef> Effects = new();
}
