using Grafted.Graphics.Textures;
using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Sim.Combat;

public class EncounterDef : Def
{
    public List<EncounterEnemyRecord> Enemies = new();
    public List<LootBoxDef> PotentialLootBoxes = new();
    public BiomeDef Biome = null!;
    public ShrineProperties? ShrineProperties = null;
    public bool IsBoss;
}

public class ShrineProperties
{
    private Texture2D? _texture;

    public RangeInt PartsToRestore;
    public List<BodyPartType> RestorablePartTypes = [];

    public string? TexturePath;
    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;
}

public class EncounterEnemyRecord
{
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public float SpawnWeight = 1;
    public string PawnName = null;
    public float BodySizeFactor = 1;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
    public List<SkillValueRecord> Skills = new();
    public List<BodyEffectDef> Effects = new();
}