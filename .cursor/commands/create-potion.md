# Create Potion

## Command: Create Potion

When the user says "create potion [PotionName]" or "new potion [PotionName]", follow this process to create a complete potion with handler, definition, and icon.

### Parameters
- **PotionName**: The name of the potion (e.g., "Fleshify", "IronSkin", "BloodBoil")
- **Effect Description**: What the potion does (user should provide or AI infers from name)

### Files to Create/Modify

1. **Handler Class**: `Grafted/Source/Sim/Entities/Items/Potions/[PotionName]Handler.cs`
2. **XML Definition**: `Grafted/Content/Data/Definitions/Entities/Items/Consumables/Potions.xml`
3. **Icon Texture**: `Grafted/Content/Textures/Entities/Item/Potion/[PotionName].png`

---

## Step 1: Create the Potion Handler

**Create:** `Grafted/Source/Sim/Entities/Items/Potions/[PotionName]Handler.cs`

### Handler Template

```csharp
namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for [PotionName] potion - [brief description of effect].
/// </summary>
[UsedImplicitly]
public class [PotionName]Handler : PotionHandler
{
    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;  // Set true if usable outside combat
    public override bool CanAutoUse => false;           // Set true for auto-consumption logic
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var actualTarget = target ?? user;
        
        // === IMPLEMENT POTION EFFECT HERE ===
        
        var message = $"/c[{TC.Attacker}]{actualTarget.LabelShort} /c[{TC.Yellow}]consumed the /c[{TC.Item}]{PotionLabel}";
        
        return PotionUseResult.Succeeded(
            message,
            alertMessage: "[Alert message shown on screen]",
            alertColor: Color.[ChooseColor]
        );
    }
    
    public override PotionUseResult UseOutsideCombat(Pawn user)
    {
        // Only implement if CanUseOutsideCombat is true
        return base.UseOutsideCombat(user);
    }
    
    public override string GetEffectDescription()
    {
        return "[Description shown in UI tooltip]";
    }
    
    // Optional: Implement if CanAutoUse is true
    public override PotionUseResult? TryAutoUse(Pawn pawn)
    {
        // Return UseInCombat(pawn) if conditions are met, null otherwise
        return null;
    }
}
```

### Common Potion Effect Patterns

**Modify Body Parts (iterate all parts):**
```csharp
foreach (var part in pawn.Body.AllParts)
{
    // Heal parts
    part.HitPoints = part.MaxHitPoints;
    
    // Add modifier to parts
    part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.[ModifierName], duration));
    
    // Change substance type
    part.SetSubstanceOverride(SubstanceType.Flesh);
}
```

**Modify Blood:**
```csharp
pawn.Body.BloodAmount = pawn.Body.MaxBlood;  // Restore full blood
pawn.Body.BloodAmount += 500;                 // Add blood
```

**Apply Body Effects:**
```csharp
pawn.Body.Effects.TryApplyEffect(new BodyEffect
{
    Def = Defs.BodyEffects.[EffectName],
    TicksLeft = duration
});
```

**Damage Target (offensive potion):**
```csharp
var damage = new Damage { /* configure damage */ };
target.TakeDamage(damage);
```

**Use GetDuration() for timed effects:**
```csharp
var duration = GetDuration();  // Reads from PotionDuration stat in XML
```

### Available Helper Methods (from PotionHandler base class)
- `Potion` - The potion Item instance
- `PotionLabel` - Display name of the potion
- `PotionDef` - The ItemDef for this potion
- `GetStatValue(StatDef stat)` - Get a stat value from the potion
- `GetDuration()` - Get potion duration in ticks (from PotionDuration stat)

### Available Colors for AlertColor
`Color.Red`, `Color.DarkRed`, `Color.Green`, `Color.GreenYellow`, `Color.Yellow`, 
`Color.Orange`, `Color.Purple`, `Color.Blue`, `Color.Cyan`, `Color.Pink`, 
`Color.PaleVioletRed`, `Color.Gold`, `Color.White`

---

## Step 2: Add XML Definition

**Modify:** `Grafted/Content/Data/Definitions/Entities/Items/Consumables/Potions.xml`

Add before `</Definitions>`:

```xml
<ItemDef ParentName="Potion">
    <Moniker>[PotionName]</Moniker>
    <Label>[Potion Display Name]</Label>
    <Description>[Flavorful description of the potion]</Description>
    <TexturePath>Entities/Item/Potion/[PotionName]</TexturePath>
    <PotionProperties>
        <HandlerClass>[PotionName]Handler</HandlerClass>
    </PotionProperties>
</ItemDef>
```

### Optional XML Elements

**For timed effects (add BaseStats):**
```xml
<BaseStats>
    <PotionDuration>1200</PotionDuration>  <!-- Duration in ticks (60 ticks = 1 second) -->
</BaseStats>
```

**For craftable potions (add CraftingProperties):**
```xml
<CraftingProperties>
    <AmountProduced>1</AmountProduced>
    <RequiredTrinkets>
        <ListItem>MortarAndPestle</ListItem>
        <ListItem>VialOfDuplicity</ListItem>
        <!-- Add more required trinkets -->
    </RequiredTrinkets>
    <ResourceRequirements>
        <ListItem>
            <Item>[ResourceMoniker]</Item>
            <Count>1</Count>
        </ListItem>
        <!-- Add more resources -->
    </ResourceRequirements>
</CraftingProperties>
```

---

## Step 3: Generate Icon

Use the nano-banana MCP to create the potion icon.

### Step 3a: Read Reference Image
```
Read one of the existing potion images to understand the style:
Grafted/Content/Textures/Entities/Item/Potion/JarOfBlood.png
```

### Step 3b: Generate Icon
```
command: generate-icon
Parameters:
  - imagePath: Grafted/Content/Textures/Entities/Item/Potion/JarOfBlood.png
  - prompt: "Transform this potion bottle to contain [description of liquid appearance based on potion effect]. The liquid should be [color] and look [texture/quality]. Keep the same bottle style, cork, rope wrapping, and tag. Change the tag text to say '[POTIONNAME]'. [Additional visual details]"
```

### Step 3d: Save Icon
```powershell
Copy-Item "[generated-image-path]" "Grafted/Content/Textures/Entities/Item/Potion/[PotionName].png" -Force
```

### Icon Prompt Ideas by Effect Type

| Effect Type | Liquid Description |
|-------------|-------------------|
| Healing | Glowing red/pink liquid with soft luminescence |
| Poison/Acid | Bubbling green liquid with toxic vapor |
| Fire/Heat | Orange-red liquid with flame particles inside |
| Ice/Cold | Pale blue crystalline liquid with frost on bottle |
| Strength | Deep amber liquid with golden swirls |
| Speed | Electric blue liquid with lightning sparks |
| Invisibility | Clear/translucent liquid with shimmer effect |
| Transformation | Swirling multi-colored liquid, unstable appearance |
| Necromancy | Black/purple liquid with skull vapor |
| Nature/Growth | Bright green liquid with leaf particles |

---

## Step 4: Verify

1. Check for linter errors in the handler class
2. Verify the icon displays correctly (read the saved PNG)
3. Confirm all files are in place

---

## Reference: Existing Potion Handlers

| Potion | Effect | Key Code Pattern |
|--------|--------|------------------|
| JarOfBlood | Restore all blood | `pawn.Body.BloodAmount = pawn.Body.MaxBlood` |
| SpicedChurni | Regen all parts | `AllParts.ForEach(p => p.TryAddModifier(...))` |
| AcidFlask | Damage target | Offensive damage to enemy |
| PussBomb | Apply festering | Apply body part modifiers |
| Fleshify | Transform substance | `part.SetSubstanceOverride(SubstanceType.Flesh)` |

---

## Reference: File Locations

- **Handlers:** `Grafted/Source/Sim/Entities/Items/Potions/`
- **XML Definitions:** `Grafted/Content/Data/Definitions/Entities/Items/Consumables/Potions.xml`
- **Icons:** `Grafted/Content/Textures/Entities/Item/Potion/`
- **Base Classes:** 
  - `PotionHandler.cs` - Abstract base class
  - `IPotionHandler.cs` - Interface definition
  - `PotionUseResult.cs` - Result class for potion usage

---

## Validation Checklist

- [ ] Handler class compiles without errors
- [ ] Handler has `[UsedImplicitly]` attribute
- [ ] XML definition added to Potions.xml
- [ ] HandlerClass in XML matches the class name (without namespace)
- [ ] TexturePath matches the icon location
- [ ] Icon matches the game's art style
- [ ] GetEffectDescription() returns meaningful text
