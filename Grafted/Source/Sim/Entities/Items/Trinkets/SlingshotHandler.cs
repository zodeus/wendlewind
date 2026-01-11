namespace Grafted.Sim.Entities.Items.Trinkets;

using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Entities.Pawns.Modifiers;
using Grafted.Graphics.Textures;

[UsedImplicitly]
public class SlingshotHandler : TrinketHandler, IUpgradableHandler
{
    private static string _boneTexturePath = "Entities/Item/Trinket/SlingshotBone";
    private static string _goldTexturePath = "Entities/Item/Trinket/SlingshotGold";
    private static Texture2D _boneTexture = null!;
    private static Texture2D _goldTexture = null!;
    private Item? _ammo;
    private int _upgradeLevel;
    private bool _isAutomatic;

    private Label _cooldownLabel = null!;
    private Label _chargesLabel = null!;
    private Image _ammoIcon = null!;
    private Widget? _buttonContent;
    private int _lastRenderedUpgradeLevel = -1;
    private ColoredRegion? _dimmedTexture;
    private const int CooldownValue = 180;

    public const float BoneDamageMultiplier = 1.5f;
    public const float GoldCooldownMultiplier = 0.7f;
    public Item? Ammo => _ammo;
    public int UpgradeLevel => _upgradeLevel;
    public bool IsAutomatic
    {
        get => _isAutomatic;
        set => _isAutomatic = value;
    }
    public UpgradeProperties? UpgradeProperties => Trinket.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;
    public AmmoProperties? AmmoProperties => _ammo?.ItemDef.AmmoProperties;
    public static Texture2D BoneTexture => _boneTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_boneTexturePath)!)!;
    public static Texture2D GoldTexture => _goldTexture ??= TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(_goldTexturePath)!)!;

    public Texture2D CurrentTexture => _upgradeLevel switch
    {
        1 => BoneTexture,
        2 => GoldTexture,
        _ => Trinket.Icon
    };

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeDeep.Look(ref _ammo, "Ammo");
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
        ScribeValues.Look(ref _isAutomatic, "IsAutomatic");
    }
    
    public override void Tick()
    {
        base.Tick();
        // Auto-activate if automatic mode is enabled, at level 3+, and ready to fire
        if (_isAutomatic && _upgradeLevel >= 3 && _ammo != null && Cooldown <= 0 && !IsActive)
        {
            Activate();
        }
    }

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest _, DamageResponse __)
    {
        if (_ammo == null || !IsActive) return null;

        var ammoProps = AmmoProperties!;
        var damage = ammoProps.DamageRange.RandomValue;
        // Level 1+ (Bone) gives damage bonus
        if (_upgradeLevel >= 1)
        {
            damage *= BoneDamageMultiplier;
        }

        var randomPart = victim.Body.AllExternalParts.RandomElement();
        var damageRecord = new DamageRecord(Trinket.Label, "Slingshot", ammoProps.DamageType, randomPart, damage, amountBlocked: 0);
        var damagedParts = randomPart.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Slingshot"));

        ApplyBodyPartModifiers(randomPart, damagedParts[0]);
        
        // Handle explosive ammo - apply splash damage to attached parts and parent
        if (ammoProps.IsExplosive && ammoProps.SplashDamageRange.Max > 0)
        {
            ApplyExplosiveSplashDamage(randomPart, ammoProps, damagedParts);
        }

        damageRecord.BodyParts = damagedParts;
        damageRecord.ActualAmount = damage;
        _ammo.StackSize--;

        if (_ammo.StackSize < 1)
        {
            _ammo.Destroy();
            _ammo = null;
        }
        // Level 2 (Gold) gives cooldown reduction
        Cooldown = _upgradeLevel >= 2 ? (int)(CooldownValue * GoldCooldownMultiplier) : CooldownValue;
        DeActivate();

        return damageRecord;
    }
    
    private void ApplyExplosiveSplashDamage(BodyPart hitPart, AmmoProperties ammoProps, List<DamagedBodyPartRecord> damagedParts)
    {
        var splashDamage = ammoProps.SplashDamageRange.RandomValue;
        if (_upgradeLevel >= 1)
        {
            splashDamage *= BoneDamageMultiplier;
        }
        
        // Damage parent part if exists
        if (hitPart.Socket?.ParentPart is { } parentPart && !parentPart.IsDestroyed)
        {
            var parentDamageRecord = new DamagedBodyPartRecord(parentPart)
            {
                DamageApplied = splashDamage
            };
            parentPart.HitPoints = Math.Max(0, parentPart.HitPoints - splashDamage);
            damagedParts.Add(parentDamageRecord);
            
            // Also damage parent's skin if it has one
            if (parentPart.Skin is { } parentSkin && !parentSkin.IsDestroyed)
            {
                var skinDamage = splashDamage * BodyPart.SkinDamageScaler;
                parentSkin.HitPoints = Math.Max(0, parentSkin.HitPoints - skinDamage);
            }
        }
        
        // Damage attached external parts (child parts)
        foreach (var externalPart in hitPart.ExternalParts)
        {
            if (externalPart.IsDestroyed) continue;
            
            var externalDamageRecord = new DamagedBodyPartRecord(externalPart)
            {
                DamageApplied = splashDamage
            };
            externalPart.HitPoints = Math.Max(0, externalPart.HitPoints - splashDamage);
            damagedParts.Add(externalDamageRecord);
            
            // Also damage the external part's skin if it has one
            if (externalPart.Skin is { } externalSkin && !externalSkin.IsDestroyed)
            {
                var skinDamage = splashDamage * BodyPart.SkinDamageScaler;
                externalSkin.HitPoints = Math.Max(0, externalSkin.HitPoints - skinDamage);
            }
        }
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

