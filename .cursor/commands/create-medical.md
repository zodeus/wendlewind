# Create Medical

## Command: Create Medical

When the user says "create medical [MedicalName]" or "new medical [MedicalName]", follow this process to create a complete medical item with handler, definition, and icon.

### Parameters
- **MedicalName**: The name of the medical item (e.g., "SutureTape", "NerveSalve", "BoneGlue")
- **Effect Description**: What the medical item does (user should provide or AI infers from name)

### Files to Create/Modify

1. **Handler Class**: `Wendlemire/Source/Sim/Entities/Items/Medicinals/[MedicalName]Handler.cs`
2. **XML Definition**: `Wendlemire/Content/Data/Definitions/Entities/Items/Consumables/Medical.xml`
3. **Icon Texture**: `Wendlemire/Content/Textures/Entities/Item/Medical/[MedicalName].png`

**Note:** Custom info panels are implemented inside the handler's `GetInfoPanel()` method, not as separate panel classes.

---

## Step 1: Create the Medical Handler

**Create:** `Wendlemire/Source/Sim/Entities/Items/Medicinals/[MedicalName]Handler.cs`

### Handler Template

```csharp
namespace Wendlemire.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for [MedicalName] - [brief description of effect].
/// </summary>
[UsedImplicitly]
public class [MedicalName]Handler : MedicinalHandler
{
    // Define colors for the info panel infographic
    private static readonly Color PrimaryColor = new(180, 120, 120);     // Main effect color
    private static readonly Color SuccessColor = new(130, 200, 130);     // Green for healed/success
    private static readonly Color WarningColor = new(200, 160, 80);      // Orange for warnings

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        // === IMPLEMENT MEDICAL EFFECT HERE ===
        
        // Get duration if needed (for modifiers)
        // var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        
        // Get healing value if needed
        // var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        
        // Return true if the item was successfully applied
        // Return false if the item cannot be applied (e.g., part already at full health)
        return true;
    }
    
    /// <summary>
    /// Returns a custom info panel widget for this medical item.
    /// The widget is displayed inside MedicinalPanel when hovering over the item.
    /// </summary>
    public override Widget? GetInfoPanel(Item item)
    {
        var panel = new VerticalStackPanel
        {
            Padding = new Thickness(20),
            MinWidth = 300,
            Spacing = 8,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };

        // Header Section: Icon + Description
        panel.Widgets.Add(CreateHeader(item));

        // Stack size
        if (item.IsStackable)
        {
            panel.Widgets.Add(new Label("small")
            {
                Text = $"Stack Size: x{item.StackSize}",
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        // How It Works section
        panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 12, 0, 8) });
        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "How It Works",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Add custom infographic content here...
        // panel.Widgets.Add(CreateInfographic());

        return panel;
    }

    private static HorizontalStackPanel CreateHeader(Item item)
    {
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 72,
            Height = 72
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 64,
            Height = 64,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var descPanel = new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        descPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Def.Description,
            Wrap = true,
            MaxWidth = 200
        });

        return new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 10),
            Widgets = { iconFrame, descPanel }
        };
    }
}
```

### Common Medical Effect Patterns

**Heal a specific body part:**
```csharp
public override bool ApplyToPart(Item item, BodyPart part)
{
    if (part.HealthPercent >= 1) return false;
    
    part.HitPoints = part.MaxHitPoints;
    return true;
}
```

**Heal part and all internal parts:**
```csharp
public override bool ApplyToPart(Item item, BodyPart part)
{
    if (part.HealthPercent >= 1 && part.AllInternalParts.All(p => p.HealthPercent >= 1))
        return false;

    part.HitPoints = part.MaxHitPoints;
    foreach (BodyPart internalPart in part.AllInternalParts)
    {
        internalPart.HitPoints = internalPart.MaxHitPoints;
    }
    return true;
}
```

**Heal specific internal part types (e.g., arteries only):**
```csharp
public override bool ApplyToPart(Item item, BodyPart part)
{
    foreach (var internalPart in part.InternalParts)
    {
        if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1)
        {
            internalPart.HitPoints = internalPart.MaxHitPoints;
            return true;
        }
    }
    return false;
}
```

**Apply a healing pool that distributes across parts:**
```csharp
private double _healAmount;

public override bool ApplyToPart(Item item, BodyPart part)
{
    var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
    _healAmount = healingValue;
    HealPart(part);
    return _healAmount < healingValue; // true if some healing was used
}

private void HealPart(BodyPart bodyPart)
{
    if (_healAmount <= 0) return;
    
    var currentHealth = bodyPart.HitPoints;
    bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _healAmount);
    _healAmount -= bodyPart.HitPoints - currentHealth;
    
    // Optionally spread to external parts
    foreach (var externalPart in bodyPart.ExternalParts)
    {
        HealPart(externalPart);
    }
}
```

**Apply body part modifier:**
```csharp
public override bool ApplyToPart(Item item, BodyPart part)
{
    var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
    part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.[ModifierName], duration));
    
    foreach (var internalPart in part.AllInternalParts)
    {
        internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.[ModifierName], duration));
    }
    return true;
}
```

**Target parts with specific conditions (e.g., necrosis):**
```csharp
public override bool ApplyToPart(Item item, BodyPart part)
{
    var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
    if (part.HasModifier(Defs.BodyPartModifiers.Necrosis) && !part.HasModifier(Defs.BodyPartModifiers.NecrosisSerum))
    {
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.NecrosisSerum, duration));
        return true;
    }
    return false;
}
```

### Available Body Part Types
- `BodyPartType.Organ` - Internal organs (heart, liver, etc.)
- `BodyPartType.Artery` - Blood vessels
- `BodyPartType.Skin` - Outer skin layer
- `BodyPartType.Bone` - Skeletal structure
- `BodyPartType.Muscle` - Muscle tissue
- `BodyPartType.Tendon` - Connective tissue

### Available Substance Types
- `SubstanceType.Flesh` - Organic flesh
- `SubstanceType.Bone` - Bone material
- `SubstanceType.Metal` - Mechanical/metal parts

---

## Step 2: Add XML Definition

**Modify:** `Wendlemire/Content/Data/Definitions/Entities/Items/Consumables/Medical.xml`

Add before `</Definitions>`:

```xml
<ItemDef ParentName="MedicalItem">
    <Moniker>[MedicalName]</Moniker>
    <Label>[Medical Display Name]</Label>
    <Description>[Flavorful description of the medical item]</Description>
    <TexturePath>Entities/Item/Medical/[MedicalName]</TexturePath>
    <MedicinalProperties>
        <HandlerClass>[MedicalName]Handler</HandlerClass>
        <DurationInTicks>800</DurationInTicks>  <!-- Optional: only needed for timed effects -->
    </MedicinalProperties>
    <BaseStats>
        <HealingValue>200</HealingValue>  <!-- Optional: only needed for healing pool effects -->
    </BaseStats>
    <CraftingProperties>
        <AmountProduced>1</AmountProduced>
        <RequiredTrinkets>
            <ListItem>MortarAndPestle</ListItem>
            <ListItem>TinkersToolbox</ListItem>
            <!-- Add more trinkets as appropriate -->
        </RequiredTrinkets>
        <ResourceRequirements>
            <ListItem>
                <Item>[ResourceMoniker]</Item>
                <Count>1</Count>
            </ListItem>
            <!-- Add 1-3 resources thematically appropriate -->
        </ResourceRequirements>
    </CraftingProperties>
</ItemDef>
```

### Note on UI Panels

All medical items automatically use `MedicinalPanel` which calls the handler's `GetInfoPanel()` method.
- If `GetInfoPanel()` returns a widget, that widget is displayed
- If `GetInfoPanel()` returns null, a default layout (icon, description, stats) is shown

**Do NOT add `<UiClass>` to individual medical items** - the base `MedicalItem` already sets `<UiClass>MedicinalPanel</UiClass>`.

### Available Resources for Crafting

| Resource | Moniker | Theme/Use |
|----------|---------|-----------|
| Healing Root | HealingRoot | Healing, restoration |
| Suture | Suture | Repairing arteries |
| Thread | Thread | Basic crafting material |
| Churni Root | NettleLeaf | Base ingredient, regeneration |
| Gold Cap Mushroom | GoldCapMushroom | Soothing, calming effects |
| Inky Cap Mushroom | InkyCapMushroom | Anti-necrotic properties |
| Venom Vial | VenomVial | Poison antidotes |
| Bone Shard | BoneShard | Bone repair |
| Ichor | Ichor | Dark medicine, transformation |

### Available Trinkets (Required Tools)

| Trinket | Moniker | Use Case |
|---------|---------|----------|
| Mortar and Pestle | MortarAndPestle | Grinding ingredients |
| Tinker's Toolbox | TinkersToolbox | Precision crafting |
| Vial of Duplicity | VialOfDuplicity | Liquid preparations |
| Cooking Pot | CookingPot | Brewing/boiling |
| Flame Stick | FlameStick | Heating/cauterizing |
| Weeping Bucket | WeepingBucket | Distillation |
| Steroid Injector | SteroidInjector | Injection preparations |
| Mechanical Eye | MechanicalEye | Precision work |

### Crafting Complexity Guidelines

- **Simple medical items**: 1-2 trinkets, 1-2 resources
- **Standard medical items**: 3-4 trinkets, 2-3 resources
- **Complex medical items**: 5+ trinkets, 3+ resources, may require other medical items

---

## Step 3: Generate Icon

Use the nano-banana MCP to create the medical icon.

### Step 3a: Read Reference Image
```
Read one of the existing medical images to understand the style:
Wendlemire/Content/Textures/Entities/Item/Medical/MedKit.png
```

### Step 3b: Generate Icon
```
command: generate-icon
Parameters:
  - imagePath: Wendlemire/Content/Textures/Entities/Item/Medical/MedKit.png
  - prompt: "Create a [type of medical item] icon in the same art style. [Description of appearance]. Maintain similar lighting, texture, and level of detail."
```

### Step 3c: Save Icon
```powershell
Copy-Item "[generated-image-path]" "Wendlemire/Content/Textures/Entities/Item/Medical/[MedicalName].png" -Force
```

### Icon Prompt Ideas by Medical Type

| Medical Type | Visual Description |
|--------------|-------------------|
| Bandage/Wrap | Rolled cloth bandage with slight blood staining |
| Ointment/Salve | Small jar or tin with colored cream visible |
| Serum/Injection | Glass syringe or vial with colored liquid |
| Pills/Tablets | Small bottle with visible pills or capsules |
| Sutures/Threads | Spool of surgical thread with needle |
| Patch | Medical patch or gauze with tape |
| Spray/Mist | Spray bottle with vapor or particles |
| Bone Tools | Surgical instruments, splints |

---

## Step 4: Optional - Implement Custom Info Panel in Handler

If the medical item has complex behavior that benefits from visual explanation, implement `GetInfoPanel()` in the handler.

**The panel is built directly in the handler class** - no separate panel file needed.

### Info Panel Building Blocks

**Step rows (numbered instructions):**
```csharp
private static HorizontalStackPanel CreateStepRow(string stepNum, string text, Color textColor)
{
    return new HorizontalStackPanel
    {
        Spacing = 8,
        Margin = new Thickness(0, 4, 0, 4),
        Widgets =
        {
            new Label("small")
            {
                Text = stepNum,
                TextColor = BaseContent.Colors.Text.Golden,
                Width = 20
            },
            new Label("small")
            {
                Text = text,
                TextColor = textColor
            }
        }
    };
}
```

**Status boxes (colored labels):**
```csharp
private static Widget CreateStatusBox(string label, Color color)
{
    return new Panel
    {
        Background = new SolidBrush(color),
        Padding = new Thickness(6, 3),
        Widgets =
        {
            new Label("small")
            {
                Text = label,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Scale = new Vector2(0.85f, 0.85f)
            }
        }
    };
}
```

**Dark info container:**
```csharp
var infoContainer = new Panel
{
    Background = new SolidBrush(new Color(25, 25, 30)),
    Padding = new Thickness(12),
    Margin = new Thickness(0, 0, 0, 8)
};
```

**Legend with grid:**
```csharp
private static void AddLegendRow(Grid grid, int row, string key, string value, Color valueColor)
{
    var keyLabel = new Label("small")
    {
        Text = key,
        TextColor = new Color(150, 150, 150)
    };
    Grid.SetColumn(keyLabel, 0);
    Grid.SetRow(keyLabel, row);
    grid.Widgets.Add(keyLabel);

    var valueLabel = new Label("small")
    {
        Text = value,
        TextColor = valueColor
    };
    Grid.SetColumn(valueLabel, 1);
    Grid.SetRow(valueLabel, row);
    grid.Widgets.Add(valueLabel);
}
```

### Example: Complete GetInfoPanel Implementation

See existing handlers for full examples:
- `MendersMistHandler` - Complex healing pool with socket propagation
- `MedKitHandler` - Full heal with part type visualization
- `SutureHandler` - Targeted artery repair
- `BalmyOintmentHandler` - Modifier application with duration
- `AntiNecroticSerumHandler` - Conditional treatment
- `MendersMixHandler` - Combined healing + modifier + socket spread

---

## Step 5: Verify

1. Check for linter errors in the handler class
2. Verify the icon displays correctly (read the saved PNG)
3. Confirm all files are in place
4. Build and test

---

## Reference: Existing Medical Handlers

| Medical Item | Effect | Key Code Pattern | GetInfoPanel |
|--------------|--------|------------------|--------------|
| MedKit | Fully heals part + internals | `part.HitPoints = part.MaxHitPoints` for all | ✓ Shows all part types healed |
| Suture | Repairs damaged arteries | Target `BodyPartType.Artery` only | ✓ Shows artery repair flow |
| MendersMist | Pool-based healing through sockets | Recursive heal with amount tracking | ✓ Socket propagation diagram |
| BalmyOintment | Applies soothing modifier | `TryAddModifier(SoothingBalm, duration)` | ✓ Shows balm coverage |
| AntiNecroticSerum | Counters necrosis | Check `HasModifier(Necrosis)` | ✓ Shows necrosis treatment |
| MendersMix | Heals + applies balm, travels sockets | Combo of healing and modifiers | ✓ Combined effects diagram |

All handlers implement `GetInfoPanel()` to provide visual infographics explaining their behavior.

---

## Reference: File Locations

- **Handlers:** `Wendlemire/Source/Sim/Entities/Items/Medicinals/`
- **XML Definitions:** `Wendlemire/Content/Data/Definitions/Entities/Items/Consumables/Medical.xml`
- **Icons:** `Wendlemire/Content/Textures/Entities/Item/Medical/`
- **UI Panel:** `Wendlemire/Source/Scenes/MainGameScene/Gui/Widgets/EntityWidgets/MedicinalPanel.cs`
- **Base Classes:** 
  - `MedicinalHandler.cs` - Abstract base class with `GetInfoPanel()` method
  - `MedicinalProperties.cs` - Properties container

---

## Validation Checklist

- [ ] Handler class compiles without errors
- [ ] Handler has `[UsedImplicitly]` attribute
- [ ] XML definition added to Medical.xml
- [ ] HandlerClass in XML matches the class name (without namespace)
- [ ] TexturePath matches the icon location
- [ ] CraftingProperties defined with appropriate trinkets and resources
- [ ] Icon matches the game's art style
- [ ] `GetInfoPanel()` implemented with informative infographic (optional but recommended)
- [ ] **Do NOT add `<UiClass>` to item** - all medical items use `MedicinalPanel` from base
- [ ] Verify build
