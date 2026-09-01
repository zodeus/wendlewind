# Wendlemire Game Development Rules

## Command: Create New Creature

When the user says "create creature [CreatureName]" or provides a folder of body part textures, follow this process:

### Prerequisites
1. Check the texture folder at `Wendlemire/Content/Textures/Entities/Pawn/BodyParts/[CreatureName]/` to see available body parts
2. Determine body structure based on textures:
   - **Humanoid** (has Arm, Hand, Finger, Leg, Foot): Use HumanBodyGenerator as reference
   - **Quadruped** (has 4 Legs, Hooves/Paws): Use HorseBodyGenerator or WolfBodyGenerator as reference
   - **Insect** (has Thorax, Abdomen, Wings): Use MosquitoBodyGenerator or BeeBodyGenerator as reference
   - **Flying** (has Wings, no legs): Use BatBodyGenerator as reference
3. Check if creature has a Neck texture - if not, connect Torso directly to Head via TorsoSocket

### Files to Create/Modify

#### 1. Body Parts XML Definition
**Create:** `Wendlemire/Content/Data/Definitions/Entities/Pawns/Bodies/BodyParts/[CreatureName]Parts.xml`

Template structure:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Definitions>
    <BodyPartDef Name="[CreatureName]BodyPartBase" ParentName="BodyPartBase" Abstract="True">
    </BodyPartDef>
    <!-- Add BodyPartDef for each texture file -->
</Definitions>
```

Key fields for each body part:
- `Moniker`: [CreatureName][PartName] (e.g., BatHead)
- `Label`: Display name (e.g., Head)
- `BodyPartType`: Must match enum in BodyPartType.cs (Head, Torso, Arm, Hand, Leg, Foot, Wing, Tail, Paw, etc.)
- `TexturePath`: Entities/Pawn/BodyParts/[CreatureName]/[PartName]
- `Sockets`: List sockets for child parts and internal organs
- `EquipmentSlots`: Add `BuiltIn` for attack parts (head, hands, feet, claws)

**Socket rules:**
- No neck texture → Head uses `TorsoSocket` instead of `NeckSocket`
- Left/Right parts → Use `Left[Type]Socket`/`Right[Type]Socket` or positional sockets
- Wings → Use `LeftWingSocket`/`RightWingSocket`
- Claws/Paws → Use `LeftPawSocket`/`RightPawSocket` or `PawSocket`

#### 2. Body Generator Class
**Create:** `Wendlemire/Source/Sim/Entities/Pawns/Bodies/[CreatureName]BodyGenerator.cs`

```csharp
namespace Wendlemire.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class [CreatureName]BodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        // Start with head as root
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.[CreatureName]Head));
        // Add internal parts, then external parts
    }
}
```

#### 3. Body Part Layout Class
**Create:** `Wendlemire/Source/Scenes/MainGameScene/Gui/Widgets/CombatWidgets/BodyPartLayouts/[CreatureName]BodyPartLayout.cs`

```csharp
namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

[UsedImplicitly]
public class [CreatureName]BodyPartLayout : IBodyPartLayout
{
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        // { "Label", new BodyPartLayoutData(position, renderOrder, scale, rotation, flipHorizontal) }
    };
    
    public int NativeSize => 512;
    
    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        if (!PartLayoutMap.TryGetValue(part.Label, out var layoutData)) return null;
        if (part.Image == null) return null;
        return new BodyPartRenderInfo(part.Image, layoutData);
    }
}
```

**Label format:** Labels are auto-generated from socket positions:
- `Left Wing`, `Right Wing` (from LeftWingSocket/RightWingSocket)
- `Left Arm`, `Right Arm` (from LeftArmSocket/RightArmSocket)
- `Front Left Leg`, `Rear Right Leg` (from positional sockets)

#### 4. Body Definition
**Modify:** `Wendlemire/Content/Data/Definitions/Entities/Pawns/Bodies/Bodies.xml`

Add before `</Definitions>`:
```xml
<BodyDef>
    <Moniker>[CreatureName]Body</Moniker>
    <Label>[Creature Name] Body</Label>
    <BoneDensity>1</BoneDensity>
    <MaxBlood>5000</MaxBlood>
    <BloodType>AnimalBlood</BloodType>
    <GeneratorClass>[CreatureName]BodyGenerator</GeneratorClass>
    <LayoutClass>[CreatureName]BodyPartLayout</LayoutClass>
</BodyDef>
```

BloodType options: AnimalBlood, BlackPlasma, ToxicSlime, Sap, Oil, HoneySuckle

#### 5. Pawn Definition
**Modify:** `Wendlemire/Content/Data/Definitions/Entities/Pawns/RacesMonsters.xml`

Add before `</Definitions>`:
```xml
<PawnDef ParentName="BaseMonsterRace">
    <Moniker>[CreatureName]</Moniker>
    <Label>[Creature Name]</Label>
    <Species>[CreatureName]</Species>
    <Body>[CreatureName]Body</Body>
    <BaseStats>
        <AttackSpeed>1</AttackSpeed>
        <Strength>1</Strength>
        <Evasion>.05</Evasion>
        <Accuracy>.85</Accuracy>
    </BaseStats>   
</PawnDef>
```

#### 6. DefLocators
**Modify:** `Wendlemire/Source/Definitions/DefLocators.cs`

Add body part static references in the `BodyParts` class:
```csharp
public static BodyPartDef [CreatureName]Head = null!;
public static BodyPartDef [CreatureName]Torso = null!;
// etc.
```

#### 7. Biological Weapons
**Modify:** `Wendlemire/Content/Data/Definitions/Entities/Items/Weapons/BiologicalWeapons.xml`

Add weapons for attack parts (teeth, claws, feet, etc.):
```xml
<ItemDef ParentName="WeaponFlesh">
    <Moniker>[CreatureName][WeaponName]</Moniker>
    <Label>[Weapon Label]</Label>
    <Description>[Description]</Description>
    <WeaponProperties>
        <WeaponType>Teeth|Claw|Foot|Hoof</WeaponType>
        <DamageType>Sharp|Blunt</DamageType>
        <SubstanceModifiers>...</SubstanceModifiers>
    </WeaponProperties>
    <EquipmentProperties>
        <SlotUsedToEquip>BuiltIn</SlotUsedToEquip>
    </EquipmentProperties>
    <TexturePath>Entities/Pawn/BodyParts/[CreatureName]/[Part]</TexturePath>
    <BaseStats>
        <WeaponPower>50</WeaponPower>
    </BaseStats>
</ItemDef>
```

### Reference Files
- Body Generators: `Wendlemire/Source/Sim/Entities/Pawns/Bodies/`
- Body Part Layouts: `Wendlemire/Source/Scenes/MainGameScene/Gui/Widgets/CombatWidgets/BodyPartLayouts/`
- Body Part Types: `Wendlemire/Source/Sim/Entities/Pawns/BodyPartType.cs`
- Socket Definitions: `Wendlemire/Content/Data/Definitions/Entities/Pawns/Bodies/BodyParts/BodyPartSockets-*.xml`
- Existing body parts for reference: `Wendlemire/Content/Data/Definitions/Entities/Pawns/Bodies/BodyParts/`

### Validation Checklist
- [ ] All texture files have corresponding BodyPartDef entries
- [ ] Socket types match allowed BodyPartTypes
- [ ] DefLocators has all body part static references
- [ ] BodyGenerator uses correct socket types (check BodyPartType enum)
- [ ] Layout labels match auto-generated labels from socket positions
- [ ] Biological weapons exist for all BuiltIn equipment slots
- [ ] No linter errors in C# files
