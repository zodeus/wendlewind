namespace Grafted.Sim.Entities.Items.Trinkets;

using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Entities.Pawns.Modifiers;
using Grafted.Graphics.Textures;

public enum SlingshotUpgradeLevel
{
    None,
    Bone,
    Gold
}

[UsedImplicitly]
public class SlingshotHandler : TrinketHandler
{
    private static string _boneTexturePath = "Entities/Item/Trinket/SlingshotBone";
    private static string _goldTexturePath = "Entities/Item/Trinket/SlingshotGold";
    private static Texture2D _boneTexture = null!;
    private static Texture2D _goldTexture = null!;
    private Item? _ammo;
    private SlingshotUpgradeLevel _upgradeLevel = SlingshotUpgradeLevel.None;

    private Label _cooldownLabel = null!;
    private Label _chargesLabel = null!;
    private Image _ammoIcon = null!;
    private Widget? _buttonContent;
    private SlingshotUpgradeLevel _lastRenderedUpgradeLevel = SlingshotUpgradeLevel.None;
    private ColoredRegion? _dimmedTexture;
    private const int CooldownValue = 180;

    public const float BoneDamageMultiplier = 1.5f;
    public const float GoldCooldownMultiplier = 0.7f;
    public Item? Ammo => _ammo;
    public SlingshotUpgradeLevel UpgradeLevel => _upgradeLevel;
    public AmmoProperties? AmmoProperties => _ammo?.ItemDef.AmmoProperties;
    public static Texture2D BoneTexture => _boneTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_boneTexturePath)!)!;
    public static Texture2D GoldTexture => _goldTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_goldTexturePath)!)!;

    public static List<ResourceCount> BoneUpgradeCost = [
        new(Defs.Items.BoneShard, 10),
        new(Defs.Items.LeatherScraps, 4)
    ];

    public static List<ResourceCount> GoldUpgradeCost = [
        new(Defs.Items.GoldenBean, 5),
        new(Defs.Items.Fang, 1),
        new(Defs.Items.LeatherScraps, 6)
    ];

    public static List<ItemDef> RequiredTrinkets = [
        Defs.Items.TinkersToolbox
    ];

    public Texture2D CurrentTexture => _upgradeLevel switch
    {
        SlingshotUpgradeLevel.Bone => BoneTexture,
        SlingshotUpgradeLevel.Gold => GoldTexture,
        _ => Trinket.Icon
    };

    public SlingshotUpgradeLevel? NextUpgrade => _upgradeLevel switch
    {
        SlingshotUpgradeLevel.None => SlingshotUpgradeLevel.Bone,
        SlingshotUpgradeLevel.Bone => SlingshotUpgradeLevel.Gold,
        _ => null
    };

    public List<ResourceCount> GetUpgradeCost(SlingshotUpgradeLevel level) => level switch
    {
        SlingshotUpgradeLevel.Bone => BoneUpgradeCost,
        SlingshotUpgradeLevel.Gold => GoldUpgradeCost,
        _ => []
    };

    public bool CanUpgrade(PawnInventory inventory)
    {
        var next = NextUpgrade;
        if (next == null) return false;

        // Check required trinkets
        foreach (var trinketDef in RequiredTrinkets)
        {
            if (!inventory.Trinkets.Any(t => t.Def == trinketDef))
                return false;
        }

        // Check resource costs
        var costs = GetUpgradeCost(next.Value);
        foreach (var cost in costs)
        {
            if (inventory.AmountOf(cost.Item) < cost.Count)
                return false;
        }

        return true;
    }

    public bool TryUpgrade(PawnInventory inventory)
    {
        var next = NextUpgrade;
        if (next == null || !CanUpgrade(inventory)) return false;

        var costs = GetUpgradeCost(next.Value);
        
        // Deduct resources
        List<Item> takenResources = [];
        foreach (var cost in costs)
        {
            var taken = inventory.Take(cost);
            if (taken == null)
            {
                // Rollback if something fails
                foreach (var item in takenResources)
                    inventory.TryAdd(item);
                return false;
            }
            takenResources.Add(taken);
        }

        // Destroy taken resources
        foreach (var item in takenResources)
            item.Destroy();

        _upgradeLevel = next.Value;
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeDeep.Look(ref _ammo, "Ammo");
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest _, DamageResponse __)
    {
        if (_ammo == null || !IsActive) return null;

        var ammoProps = AmmoProperties!;
        var damage = ammoProps.DamageRange.RandomValue;
        if(_upgradeLevel == SlingshotUpgradeLevel.Bone || _upgradeLevel == SlingshotUpgradeLevel.Gold)
        {
            damage *= BoneDamageMultiplier;
        }

        var randomPart = victim.Body.AllExternalParts.RandomElement();
        var damageRecord = new DamageRecord(Trinket.Label, "Slingshot", ammoProps.DamageType, randomPart, damage, amountBlocked: 0);
        var damagedParts = randomPart.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Slingshot"));

        ApplyBodyPartModifiers(randomPart, damagedParts[0]);

        damageRecord.BodyParts = damagedParts;
        damageRecord.ActualAmount = damage;
        _ammo.StackSize--;

        if (_ammo.StackSize < 1)
        {
            _ammo.Destroy();
            _ammo = null;
        }
        Cooldown = _upgradeLevel == SlingshotUpgradeLevel.Gold ? (int)(CooldownValue * GoldCooldownMultiplier) : CooldownValue;
        DeActivate();

        return damageRecord;
    }

    public void ApplyBodyPartModifiers(BodyPart part, DamagedBodyPartRecord damagedBodyPartRecord)
    {
        foreach (var modifier in AmmoProperties!.BodyPartModifiers)
        {
            if (modifier.Def.HandlerClass == typeof(RotLung))
            {
                var lungs = part.Body?.AllParts.Where(p => p?.Type == BodyPartType.Lung);
                if (lungs != null)
                {
                    foreach (var lung in lungs)
                    {
                        damagedBodyPartRecord.AppliedModifiers.Add(modifier.Def);
                        lung.ApplyBodyPartModifier(modifier, "Slingshot");
                    }
                }

            }
            else
            {
                damagedBodyPartRecord.AppliedModifiers.Add(modifier.Def);
                part.ApplyBodyPartModifier(modifier, "Slingshot");
            }
        }
    }

    public override void OnClick()
    {
        if (_ammo == null) return;
        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    public Item? LoadAmmo(Item? ammo)
    {
        var oldAmmo = _ammo;
        _ammo = ammo;
        return oldAmmo;
    }

    public override void PrepareTrinketButton(CursorButton button)
    {
        var panel = new Panel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _cooldownLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Red,
            Visible = false
        };

        _chargesLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Margin = new Thickness(15, 20, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextColor = Color.Gold,
            Visible = false
        };

        _ammoIcon = new Image
        {
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, -4, -4, 0),
            Visible = false
        };

        panel.Widgets.Add(_cooldownLabel);
        panel.Widgets.Add(_chargesLabel);
        panel.Widgets.Add(_ammoIcon);

        if (button.Content is Panel content)
        {
            content.Widgets.Add(panel);
        }
        
        _buttonContent = button.Content;
    }

    public override void Update(CursorButton button)
    {
        // Handle upgrade level texture changes
        if (_buttonContent != null)
        {
            // Regenerate dimmed texture when upgrade level changes
            if (_lastRenderedUpgradeLevel != _upgradeLevel)
            {
                var currentTexture = new TextureRegion(CurrentTexture);
                _dimmedTexture = new ColoredRegion(currentTexture, new Color(100, 100, 100));
                _lastRenderedUpgradeLevel = _upgradeLevel;
            }
            
            // Apply appropriate texture based on state
            if (IsActive)
            {
                button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
                _buttonContent.Background = new TextureRegion(CurrentTexture);
            }
            else if (Cooldown > 0)
            {
                button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
                _buttonContent.Background = _dimmedTexture;
            }
            else
            {
                button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
                _buttonContent.Background = new TextureRegion(CurrentTexture);
            }
        }
        else
        {
            base.Update(button);
        }

        if (Cooldown > 0)
        {
            _cooldownLabel.Text = Cooldown.ToString();
            _cooldownLabel.Visible = true;
            _chargesLabel.Visible = false;
            _ammoIcon.Visible = false;
        }
        else if (_ammo != null)
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Text = $"+{_ammo.StackSize}";
            _chargesLabel.Visible = true;
            _ammoIcon.Background = new TextureRegion(_ammo.Icon);
            _ammoIcon.Visible = true;
        }
        else
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Visible = false;
            _ammoIcon.Visible = false;
            button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
        }
    }
}

