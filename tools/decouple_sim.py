"""Strip rendering/UI types from Simulation so it can live in its own project."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SIM = ROOT / "Wendlewind" / "Source" / "Sim"
DEFN = ROOT / "Wendlewind" / "Source" / "Definitions"
UTILS = ROOT / "Wendlewind" / "Source" / "Utils"
CORE = ROOT / "Wendlewind" / "Source" / "Core.cs"


def write(path: Path, text: str) -> None:
    path.write_text(text.replace("\r\n", "\n"), encoding="utf-8")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def strip_usings(text: str) -> str:
    text = re.sub(r"^using Wendlewind\.Scenes[^\n]*\n", "", text, flags=re.M)
    text = re.sub(r"^using Wendlewind\.Graphics[^\n]*\n", "", text, flags=re.M)
    return text


def replace_core_access(text: str) -> str:
    text = text.replace("Core.Context", "GameContext.Current")
    text = text.replace("Core.Random", "GameContext.Random")
    return text


def strip_method_from(text: str, start_pat: str) -> str:
    """Remove a method (and following helpers until next public/protected/override ExposeData or class end).
    Conservative: remove from start_pat through the matching brace of that method only.
    """
    m = re.search(start_pat, text)
    if not m:
        return text
    start = m.start()
    # find opening brace after the signature
    brace = text.find("{", m.end() - 1)
    if brace < 0:
        # expression-bodied
        end = text.find(";", m.end())
        return text[:start] + text[end + 1 :]
    depth = 0
    i = brace
    while i < len(text):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return text[:start] + text[i + 1 :]
        i += 1
    return text


def strip_all_methods(text: str, start_pat: str) -> str:
    while re.search(start_pat, text):
        nxt = strip_method_from(text, start_pat)
        if nxt == text:
            break
        text = nxt
    return text


def process_sim_file(path: Path) -> None:
    text = read(path)
    original = text
    text = strip_usings(text)
    text = replace_core_access(text)

    rel = path.relative_to(SIM).as_posix()

    # Medicinals: drop custom GetInfoPanel bodies
    if "Medicinals" in rel and "GetInfoPanel" in text:
        text = strip_all_methods(text, r"public override Widget\? GetInfoPanel")

    # Weapons: drop custom info widgets
    if "Weapons" in rel:
        text = strip_all_methods(text, r"public override Widget CreateInfoWidget")
        text = strip_all_methods(text, r"private static void AddStatRow")

    # Trinkets: drop button UI
    if "Trinkets" in rel:
        text = strip_all_methods(text, r"public override void PrepareTrinketButton")
        text = strip_all_methods(text, r"public override void Update\(CursorButton")

    # Modifiers: convert GetInfoPanel -> GetInfoData
    if "Modifiers" in rel and "GetInfoPanel" in text:
        text = text.replace("public override Widget? GetInfoPanel()", "public override InfoPanelData GetInfoData()")
        text = text.replace("return BuildInfoPanel(new InfoPanelData", "return new InfoPanelData")
        text = text.replace("=> BuildInfoPanel(new InfoPanelData", "=> new InfoPanelData")

    if text != original:
        write(path, text)


def main() -> None:
    # --- GameContext static host ---
    gc = SIM / "GameContext.cs"
    text = read(gc)
    if "public static GameContext Current" not in text:
        text = text.replace(
            "public class GameContext : IExposable\n{",
            "public class GameContext : IExposable\n{\n"
            "    public static GameContext Current { get; set; } = null!;\n"
            "    public static Random Random { get; set; } = null!;\n",
        )
        write(gc, text)

    # --- Core wrappers ---
    core = read(CORE)
    core = core.replace(
        "    public static GameContext Context { get; set; } = null!;",
        "    public static GameContext Context\n"
        "    {\n"
        "        get => GameContext.Current;\n"
        "        set => GameContext.Current = value;\n"
        "    }",
    )
    core = core.replace(
        "    public static Random Random { get; private set; } = null!;",
        "    public static Random Random\n"
        "    {\n"
        "        get => GameContext.Random;\n"
        "        set => GameContext.Random = value;\n"
        "    }",
    )
    write(CORE, core)

    # --- Def: no UI types ---
    write(
        DEFN / "Def.cs",
        """namespace Wendlewind.Definitions;

public class Def {
    public string Moniker = "undefined";
    public string Label = "undefined";
    public string Description = "";
    public ushort Index = ushort.MaxValue;

    public override string ToString() {
        return Moniker;
    }

    public virtual void Initialize() {
        Log.Debug($"Initializing: {Moniker}");
    }

    public virtual void ResolveDependencies() {
        Log.Debug($"ResolveDependencies: {Moniker}");
    }
}
""",
    )

    # --- GenTypes: scan all Wendlewind assemblies ---
    gt = read(UTILS / "GenTypes.cs")
    gt = gt.replace(
        "    public static List<string> ImpliedNamespaceNames {\n"
        "        get { return _impliedNamespaceNames ??= typeof(GameContext).Assembly.GetTypes().Where(t => t.Namespace?.StartsWith(\"Wendlewind\") == true).Select(t => t.Namespace).Distinct().ToList()!; }\n"
        "    }",
        "    public static List<string> ImpliedNamespaceNames {\n"
        "        get {\n"
        "            return _impliedNamespaceNames ??= AllTypes\n"
        "                .Where(t => t.Namespace?.StartsWith(\"Wendlewind\") == true)\n"
        "                .Select(t => t.Namespace!)\n"
        "                .Distinct()\n"
        "                .ToList();\n"
        "        }\n"
        "    }",
    )
    gt = gt.replace(
        "    private static IEnumerable<Assembly> AllActiveAssemblies {\n"
        "        get { yield return Assembly.GetExecutingAssembly(); }\n"
        "    }",
        "    private static IEnumerable<Assembly> AllActiveAssemblies {\n"
        "        get {\n"
        "            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())\n"
        "            {\n"
        "                var name = assembly.GetName().Name;\n"
        "                if (name != null && name.StartsWith(\"Wendlewind\"))\n"
        "                {\n"
        "                    yield return assembly;\n"
        "                }\n"
        "            }\n"
        "        }\n"
        "    }",
    )
    write(UTILS / "GenTypes.cs", gt)

    # --- EntityDef ---
    write(
        SIM / "Entities" / "EntityDef.cs",
        """namespace Wendlewind.Sim.Entities;

public class EntityDef : Def {
    public virtual EntityType EntityType => throw new NotImplementedException($"EntityType not set for class {GetType().Name}");
    public Type EntityClass = null!;
    public Type? UiClass;
    public List<BaseStat> BaseStats = new();
    public string? TexturePath;
}
""",
    )

    # --- Entity ---
    entity = read(SIM / "Entities" / "Entity.cs")
    entity = strip_usings(entity)
    entity = re.sub(r"\s*public virtual Texture2D Icon => Def\.Icon;\n", "\n", entity)
    entity = re.sub(
        r"\s*public EntityPanelBase UiPanel\(BaseGui gui, EntityPanelProperties\? properties = null\)\s*\{\s*return Def\.UiPanelFor\(gui, this, properties\);\s*\}\s*",
        "\n",
        entity,
    )
    write(SIM / "Entities" / "Entity.cs", entity)

    # --- BodyPartDef ---
    write(
        SIM / "Entities" / "Pawns" / "BodyPartDef.cs",
        """namespace Wendlewind.Sim.Entities.Pawns;

public class BodyPartDef : EntityDef {
    public override EntityType EntityType => EntityType.BodyPart;
    public BodyPartType BodyPartType = BodyPartType.Undefined;
    public float BloodAmount = 0;
    public float HitWeight = 0;
    public bool IsVital = false;
    public bool IsOrgan = false;
    public SubstanceType Substance = SubstanceType.Undefined;
    public float MobilityFraction = 0;
    public List<BodyPartSocketDef> Sockets = new();
    public List<EquipmentSlotType>? EquipmentSlots = null;
    public AdaptiveBodyPartProperties? AdaptiveProperties;
    public string? WhiteIconTexturePath;
}
""",
    )

    # --- BodyPart WhiteIcon ---
    bp = read(SIM / "Entities" / "Pawns" / "BodyPart.cs")
    bp = replace_core_access(bp)
    bp = re.sub(r"\s*public Texture2D WhiteIcon => BodyPartDef\.WhiteIcon;\n", "\n", bp)
    write(SIM / "Entities" / "Pawns" / "BodyPart.cs", bp)

    # --- BodyEffectDef ---
    write(
        SIM / "Entities" / "Pawns" / "BodyEffectDef.cs",
        """namespace Wendlewind.Sim.Entities.Pawns;

public class BodyEffectDef : Def
{
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
    public string? Notes;
}

public class BodyStanceDef : Def
{
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
}
""",
    )

    # --- ItemDef ---
    item_def = read(SIM / "Entities" / "Items" / "ItemDef.cs")
    item_def = strip_usings(item_def)
    item_def = re.sub(r"\s*public override Type DefUiClass => typeof\(ItemDefPanel\);\n", "\n", item_def)
    write(SIM / "Entities" / "Items" / "ItemDef.cs", item_def)

    # --- Skills ---
    skills = read(SIM / "Entities" / "Pawns" / "Skills.cs")
    skills = strip_usings(skills)
    skills = re.sub(r"\s*public override Type DefUiClass => typeof\(SkillDefPanel\);\n", "\n", skills)
    write(SIM / "Entities" / "Pawns" / "Skills.cs", skills)

    # --- LootBoxDef: keep data, drop textures/UI types living in Sim ---
    write(
        SIM / "LootBoxes" / "LootBoxDef.cs",
        """namespace Wendlewind.Sim.LootBoxes;

public enum LootBoxCategory
{
    Weapons,
    Armor,
    Food,
    Supplies,
    Trinkets,
    Medicinal,
    Enchantments,
    Resources,
    Potions,
}

public enum LootBoxCollectionType
{
    Random,
    All
}

public class LootBoxTrapProperties
{
    public string? TrapLabel;
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
    public Type? UiClass;
    public LootBoxCategory Category;
    public RangeInt CollectionLimit;
    public LootBoxTrapProperties? TrapProperties;
    public List<LootBoxItem> Items = [];
    public string? TexturePath;
}
""",
    )

    # --- MedicinalProperties ---
    med = read(SIM / "Entities" / "Items" / "Medicinals" / "MedicinalProperties.cs")
    med = re.sub(
        r"\s*/// <summary>\s*/// Gets a custom info panel.*public virtual Widget\? GetInfoPanel\(Item item\) => null;\n",
        "\n",
        med,
        flags=re.S,
    )
    write(SIM / "Entities" / "Items" / "Medicinals" / "MedicinalProperties.cs", med)

    # --- WeaponHandler ---
    write(
        SIM / "Entities" / "Items" / "Weapons" / "WeaponHandler.cs",
        """namespace Wendlewind.Sim.Entities.Items.Weapons;

/// <summary>
/// Base class for unique weapon handlers that execute special effects during combat.
/// </summary>
public abstract class WeaponHandler : IExposable
{
    public Item Weapon = null!;

    public string Label => Weapon.Label;

    /// <summary>
    /// Called after the weapon successfully hits a target and deals damage.
    /// </summary>
    public virtual void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Weapon!, "Weapon");
    }

    public override string ToString()
    {
        return $"{Weapon.Label} Handler";
    }
}
""",
    )

    # --- TrinketHandler ---
    write(
        SIM / "Entities" / "Items" / "Trinkets" / "TrinketHandler.cs",
        """namespace Wendlewind.Sim.Entities.Items.Trinkets;

public abstract class TrinketHandler : IExposable
{
    public bool IsActive { get; protected set; }
    public int Cooldown;
    public int Kills;

    public Item Trinket = null!;

    public string Label => Trinket.Label;

    public virtual void Tick()
    {
        Cooldown = Math.Clamp(Cooldown - 1, 0, int.MaxValue);
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Trinket!, "Def");
        ScribeValues.Look(ref Cooldown, "Cooldown");
        ScribeValues.Look(ref Kills, "Kills");
    }

    public override string ToString()
    {
        return $"{Trinket.Label} Handler";
    }

    public virtual bool Activate()
    {
        if (Cooldown > 0)
        {
            return false;
        }

        IsActive = true;

        return true;
    }

    public virtual void DeActivate()
    {
        IsActive = false;
    }

    public virtual void Stop()
    {
        DeActivate();
    }

    public virtual void PostCombatAction(PostCombatReport postCombatReport)
    {
    }

    public virtual DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        return null;
    }

    public virtual void OnClick()
    {
    }
}
""",
    )

    # --- SlingshotHandler texture properties ---
    sling = read(SIM / "Entities" / "Items" / "Trinkets" / "SlingshotHandler.cs")
    sling = strip_usings(sling)
    sling = replace_core_access(sling)
    sling = re.sub(r"\s*private static Texture2D _boneTexture = null!;\n\s*private static Texture2D _goldTexture = null!;\n", "\n", sling)
    sling = re.sub(r"\s*private Label _cooldownLabel = null!;\n", "\n", sling)
    sling = re.sub(r"\s*private Label _chargesLabel = null!;\n", "\n", sling)
    sling = re.sub(r"\s*private Image _ammoIcon = null!;\n", "\n", sling)
    sling = re.sub(r"\s*private Widget\? _buttonContent;\n", "\n", sling)
    sling = re.sub(r"\s*private int _lastRenderedUpgradeLevel = -1;\n", "\n", sling)
    sling = re.sub(r"\s*private ColoredRegion\? _dimmedTexture;\n", "\n", sling)
    sling = sling.replace(
        "    public static Texture2D BoneTexture => _boneTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_boneTexturePath)!)!;\n"
        "    public static Texture2D GoldTexture => _goldTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_goldTexturePath)!)!;\n\n"
        "    public Texture2D CurrentTexture => _upgradeLevel switch\n"
        "    {\n"
        "        1 => BoneTexture,\n"
        "        2 => GoldTexture,\n"
        "        _ => Trinket.Icon\n"
        "    };\n",
        "    public string BoneTexturePath => _boneTexturePath;\n"
        "    public string GoldTexturePath => _goldTexturePath;\n"
        "    public string CurrentTexturePath => _upgradeLevel switch\n"
        "    {\n"
        "        1 => _boneTexturePath,\n"
        "        2 => _goldTexturePath,\n"
        "        _ => Trinket.Def.TexturePath ?? \"\"\n"
        "    };\n",
    )
    sling = strip_all_methods(sling, r"public override void PrepareTrinketButton")
    sling = strip_all_methods(sling, r"public override void Update\(CursorButton")
    write(SIM / "Entities" / "Items" / "Trinkets" / "SlingshotHandler.cs", sling)

    # --- BodyPartModifier: data-only info panels ---
    bpm = read(SIM / "Entities" / "Pawns" / "Modifiers" / "BodyPartModifier.cs")
    bpm = replace_core_access(bpm)
    # replace GetInfoPanel + builder region
    bpm = re.sub(
        r"    /// <summary>\s*/// Gets a custom info panel widget.*?#endregion\n}",
        """    public virtual InfoPanelData? GetInfoData() => null;
}

public record InfoLine(string Text, Color Color);

public class InfoPanelData
{
    public string? Title { get; init; }
    public double? Damage { get; init; }
    public string DamageSuffix { get; init; } = "damage/tick";
    public Color? DamageColor { get; init; }
    public double? Healing { get; init; }
    public string HealingSuffix { get; init; } = "health/tick";
    public Color? HealingColor { get; init; }
    public List<InfoLine> Lines { get; init; } = [];
    public bool HasSpread { get; init; }
    public bool HasPenetrated { get; init; }
    public string? CuredBy { get; init; }
    public string? BlockedBy { get; init; }
    public bool ShowPower { get; init; }
    public string TimePrefix { get; init; } = "Time";
    public Color? TimeColor { get; init; }
}

public static class InfoColors
{
    public static readonly Color Damage = new(255, 120, 80);
    public static readonly Color Spread = new(255, 180, 80);
    public static readonly Color Penetrated = new(255, 100, 100);
    public static readonly Color Cure = new(130, 200, 130);
    public static readonly Color Muted = new(150, 150, 150);
    public static readonly Color Warning = new(200, 160, 80);
    public static readonly Color Info = new(180, 220, 240);
}
""",
        bpm,
        flags=re.S,
    )
    write(SIM / "Entities" / "Pawns" / "Modifiers" / "BodyPartModifier.cs", bpm)

    # --- ScreenMessageData in Sim ---
    write(
        SIM / "ScreenMessageData.cs",
        """namespace Wendlewind.Sim;

public class ScreenMessageData
{
    public string Text = "";
    public Color Color;
    public int Duration = 10000;
}
""",
    )

    # --- Zone ---
    zone = read(SIM / "Zones" / "Zone.cs")
    zone = strip_usings(zone)
    zone = replace_core_access(zone)
    zone = zone.replace("                Font = BaseContent.Fonts.Default.Huge,\n", "")
    write(SIM / "Zones" / "Zone.cs", zone)

    # --- CombatHandler ---
    ch = read(SIM / "Combat" / "CombatHandler.cs")
    ch = strip_usings(ch)
    ch = replace_core_access(ch)
    ch = ch.replace("                        Font = BaseContent.Fonts.Default.Medium,\n", "")
    ch = ch.replace("                    Font = BaseContent.Fonts.Default.Medium,\n", "")
    ch = ch.replace("                    Font = BaseContent.Fonts.Default.Large,\n", "")
    write(SIM / "Combat" / "CombatHandler.cs", ch)

    # Process remaining sim files
    for path in SIM.rglob("*.cs"):
        if path.name in {
            "EntityDef.cs",
            "Entity.cs",
            "BodyPartDef.cs",
            "BodyPart.cs",
            "BodyEffectDef.cs",
            "ItemDef.cs",
            "LootBoxDef.cs",
            "MedicinalProperties.cs",
            "WeaponHandler.cs",
            "TrinketHandler.cs",
            "SlingshotHandler.cs",
            "BodyPartModifier.cs",
            "ScreenMessageData.cs",
            "Zone.cs",
            "CombatHandler.cs",
            "GameContext.cs",
            "BaseContent.cs",
            "MainMenuScene.cs",
        }:
            continue
        process_sim_file(path)

    print("Decouple pass complete")


if __name__ == "__main__":
    main()
