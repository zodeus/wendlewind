# Generate Game Icon

## Command: Generate Game Icon

When the user says "generate icon [ItemName]" or "create icon for [ItemName]", follow this process to generate a game icon using the nano-banana MCP tool.

### Parameters
- **ItemName**: The name of the item to create an icon for (e.g., "Fleshify", "HealthPotion")
- **Dimensions**: Optional dimensions in format WIDTHxHEIGHT (default: 256x256). User can specify like "generate icon Fleshify 512x512"
- **Category**: The item category determines reference images and output path:
  - `Potion` → Reference: `Wendlemire/Content/Textures/Entities/Item/Potion/`
  - `Trinket` → Reference: `Wendlemire/Content/Textures/Entities/Item/Trinket/`
  - `Medical` → Reference: `Wendlemire/Content/Textures/Entities/Item/Medical/`
  - `Weapon` → Reference: `Wendlemire/Content/Textures/Entities/Item/Equipment/Weapon/`
  - `Armor` → Reference: `Wendlemire/Content/Textures/Entities/Item/Equipment/Armor/`
  - `Resource` → Reference: `Wendlemire/Content/Textures/Entities/Item/Resource/`

### Process

#### Step 1: Identify Reference Image
1. Determine the item category from context or ask the user
2. Navigate to the appropriate texture folder
3. Select an existing image that matches the style needed (e.g., for potions, use JarOfBlood.png or SpicedChurni.png)
4. Read the reference image to understand the art style

#### Step 2: Gather Item Description
1. Check if there's an existing XML definition for the item in:
   - `Wendlemire/Content/Data/Definitions/Entities/Items/` (search subdirectories)
2. If found, use the `<Description>` field to understand what the item looks like
3. If not found, ask the user for a description or infer from the item name

#### Step 3: Generate the Icon
Use the nano-banana MCP `edit_image` tool with the reference image:

```
Tool: mcp_nano-banana_edit_image
Parameters:
  - imagePath: [path to reference image]
  - prompt: [detailed prompt describing the desired icon]
```

**Prompt Template:**
```
Transform this into a [ItemName] icon. [Description of what the item should look like]. 
Keep the same art style, bottle/container shape (if applicable), and level of detail.
Change the tag/label text to say "[ITEMNAME]".
Background=Plain Black
[Any specific visual elements like colors, effects, materials]

```

#### Step 5: Save to Correct Location
1. Get the generated image path from nano-banana output
2. Copy to the correct textures folder:

```powershell
Copy-Item "[generated-image-path]" "Wendlemire/Content/Textures/Entities/Item/[Category]/[ItemName].png" -Force
```

#### Step 6: Verify
Read the saved image to confirm it looks correct and display it to the user.

### Example Prompts by Category

**Potions:**
```
Transform this into a [Name] potion. The liquid should be [color/appearance]. 
Keep the same bottle style, cork, rope wrapping, and tag. 
Change the tag text to say "[NAME]".
The liquid should [specific visual qualities like "glow", "bubble", "have particles", etc.]
```

**Trinkets:**
```
Create a [Name] trinket icon. It should be a [physical description].
Match the illustrated game item style with detailed shading and a slightly worn look.
[Specific materials, colors, and features]
```

**Medical Items:**
```
Create a [Name] medical item. It should be a [physical description like "syringe", "bandage", "vial"].
Keep the same illustrated style with clean lines and muted medical colors.
[Specific details]
```

**Weapons:**
```
Create a [Name] weapon. It should be a [weapon type] with [distinctive features].
Match the illustrated fantasy weapon style with detailed metalwork and handle wrapping.
[Materials, enchantments, wear patterns]
```

### Troubleshooting

**Rate Limits:**
If you get a 429 error, wait the specified time (usually shown in error message) and retry.

**Wrong Style:**
If the generated image doesn't match the game's art style, try:
1. Using a different reference image
2. Being more specific about "illustrated game icon style" in the prompt
3. Adding "hand-painted look with visible brush strokes"


### Output Paths Reference
```
Wendlemire/Content/Textures/Entities/Item/
├── Potion/           # Consumable potions
├── Trinket/          # Equipment trinkets
├── Medical/          # Healing items
├── Incense/          # Burnable items
├── Supplies/         # Misc supplies
├── Resource/         # Crafting resources
│   ├── Organic/
│   ├── Metal/
│   ├── Cloth/
│   ├── Stone/
│   └── Wood/
└── Equipment/
    ├── Weapon/       # Weapons
    ├── Armor/        # Armor pieces
    │   ├── Chain/
    │   ├── Leather/
    │   └── ...
    └── Bag/          # Bags/containers
```
