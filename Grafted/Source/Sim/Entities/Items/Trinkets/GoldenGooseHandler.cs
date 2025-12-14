
namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class GoldenGooseHandler : TrinketHandler
{
    private const int MaxHunger = 100;
    private const int HungerPerEncounter = 5;
    
    private static readonly Color HungerFullColor = Color.GreenYellow;   // Golden
    private static readonly Color HungerEmptyColor = Color.SandyBrown;   // Brown
    
    private HorizontalProgressBar _hungerBar = null!;

    private readonly Dictionary<ItemDef, int> _foodList = new() {
        { Defs.Items.RawFish, 10 },
        { Defs.Items.RawMeat, 5 },
        { Defs.Items.RawCorn, 3 },
        { Defs.Items.RawGrain, 3 },
    };
    
    private int _hunger = 20;
    
    /// <summary>
    /// Current hunger level (0-100). Higher hunger = more beans produced.
    /// </summary>
    public int Hunger
    {
        get => _hunger;
        private set => _hunger = Math.Clamp(value, 0, MaxHunger);
    }
    
    /// <summary>
    /// Returns how many golden beans the goose will produce based on current hunger.
    /// </summary>
    public int BeansToGenerate => Hunger switch
    {
        >= 80 => 3,
        >= 50 => 2,
        >= 10 => 1,
        _ => 0
    };

    public Color GetHungerColor()
    {
        var maxBeans = MaxHunger - 20;
        return Color.Lerp(HungerEmptyColor, HungerFullColor, Hunger / maxBeans);
    }
    
    /// <summary>
    /// Checks if the given item can be fed to the goose.
    /// </summary>
    public bool CanEat(ItemDef itemDef)
    {
        return _foodList.ContainsKey(itemDef);
    }
    
    public int GetNutritionValue(Item item)
    {
        return _foodList[item.ItemDef];
    }
    
    /// <summary>
    /// Feeds an item to the goose, increasing its hunger.
    /// Returns the nutrition value gained.
    /// </summary>
    public int Feed(Item item)
    {
        if (!CanEat(item.ItemDef)) return 0;
        Hunger += _foodList[item.ItemDef];
        
        return _foodList[item.ItemDef];
    }

    public override void PrepareTrinketButton(Button button)
    {
        var panel = new Panel
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        _hungerBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(2, 0, 2, 0),
            Filler = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], 
                HungerFullColor),
            Value = (float)Hunger / MaxHunger * 100
        };
        
        panel.Widgets.Add(_hungerBar);
        
        if (button.Content is Panel content)
        {
            content.Widgets.Add(panel);
        }
    }
    
    public override void Update(Button button)
    {
        base.Update(button);
        
        var hungerPercent = (float)Hunger / MaxHunger;
        _hungerBar.Value = hungerPercent * 100;
        
        // Interpolate color from brown (empty) to golden (full)
        var barColor = GetHungerColor();
        _hungerBar.Filler = new ColoredRegion(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], 
            barColor);
    }
    
    public override void PostCombatAction(PostCombatReport postCombatReport)
    {
        var beansToGenerate = BeansToGenerate;

        if (beansToGenerate > 0)
        {
            // Create golden beans and add to player inventory
            var beans = EntityGenerator.CreateEntity<Item>(Defs.Items.GoldenBean, beansToGenerate);
            Core.Context.PlayerPawn.Inventory.TryAdd(beans);
        }

        // Goose gets hungrier after each encounter
        Hunger -= HungerPerEncounter;
    }
    
    public override void Tick()
    {
        // Goose doesn't tick cooldown
    }
    
    public override void ExposeData()
    {
        ScribeValues.Look(ref _hunger, "Hunger", 50);
        base.ExposeData();
    }
}

