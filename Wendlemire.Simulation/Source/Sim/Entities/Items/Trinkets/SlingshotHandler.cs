namespace Wendlemire.Sim.Entities.Items.Trinkets;

using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class SlingshotHandler : TrinketHandler, IUpgradableHandler
{
    public SlingshotHandler(IRng rng)
    {
        Rng = rng;
    }

    private static string _boneTexturePath = "Entities/Item/Trinket/SlingshotBone";
    private static string _goldTexturePath = "Entities/Item/Trinket/SlingshotGold";
    private Item? _ammo;
    private int _upgradeLevel;
    private bool _isAutomatic;
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
    public string BoneTexturePath => _boneTexturePath;
    public string GoldTexturePath => _goldTexturePath;
    public string CurrentTexturePath => _upgradeLevel switch
    {
        1 => _boneTexturePath,
        2 => _goldTexturePath,
        _ => Trinket.Def.TexturePath ?? ""
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
        var damage = ammoProps.DamageRange.Roll(Context.Rng);
        // Level 1+ (Bone) gives damage bonus
        if (_upgradeLevel >= 1)
        {
            damage *= BoneDamageMultiplier;
        }

        var randomPart = victim.Body.AllExternalParts.RandomElement(Context.Rng);
        var damageRecord = new DamageRecord(Trinket.Label, "Slingshot", ammoProps.DamageType, randomPart, damage, amountBlocked: 0, weaponMoniker: Trinket.ItemDef.Moniker);
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
        var splashDamage = ammoProps.SplashDamageRange.Roll(Context.Rng);
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

    

    
}

