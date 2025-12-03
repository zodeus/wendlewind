using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly]
public class Pawn : Entity
{
    public event Action<Pawn, DamageRequest, DamageResponse>? DamageTaken; 
    public event Action<DeathEvent>? Died;
    public event Action<Pawn, Item>? FoodConsumed;

    public PawnBiography Biography = null!;
    public PawnTraits Traits = null!;
    public PawnMind Mind = null!;
    public PawnBody Body = null!;
    public PawnSkills Skills = null!;
    public PawnInventory Inventory = null!;
    public PawnEquipment Equipment = null!;
    public PawnType PawnType = PawnType.Invalid;
    public Zone? Zone;

    public int TicksToAttack;

    public PawnDef PawnDef => (PawnDef)Def;
    public string Species => PawnDef.Species;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public override Texture2D Icon => PawnDef.Icon;
    public bool IsHungry => Body.IsHungry;
    public bool IsFamished => Body.IsFamished;
    public bool IsDead { get; private set; }
    public Gender Gender => Biography.Gender;
    public float AttackSpeed => Body.GetAttackSpeedModifier() * this.GetStatValue(Defs.Stats.AttackSpeed);

    public void GenerateBody(float bodySizeFactor)
    {
        Body.BodySizeFactor = bodySizeFactor;
        Body.BloodAmount = Body.MaxBlood;
        PawnDef.Body.Generator.Generate(this);
        Body.BodyPartsDirty = true;
    }

    public override void Initialize()
    {
        Biography = new PawnBiography(this);
        Traits = new PawnTraits(this);
        Mind = new PawnMind(this);
        Body = new PawnBody(this);
        Body.Initialize();
        Skills = new PawnSkills(this);
        Equipment = new PawnEquipment(this);
        Inventory = new PawnInventory(this);
        base.Initialize();
    }

    public override void Tick()
    {
        if (IsDead)
        {
            return;
        }

        Mind.Tick();
        if (IsDead)
        {
            return;
        }

        Body.Tick();
        if (IsDead)
        {
            return;
        }

        TicksToAttack--;

        // Check if change in attack speed should reduce attack time 
        if (CalculateTicksToAttack() is var ticks && ticks < TicksToAttack)
        {
            TicksToAttack = ticks;
        }

        Skills.Tick();
        Inventory.Tick();
        base.Tick();
    }

    public void TakeDamage(DamageRequest request)
    {
        DamageResponse response = new();

        var isTargetingImpActive = false;
        foreach (var trinket in request.Trinkets)
        {
            if (request.TargetedPart != null && trinket.TrinketHandler is TargetingImpHandler { Charges: > 0 } handler)
            {
                handler.Charges--;
                isTargetingImpActive = true;
            }

            if (trinket.TrinketHandler!.HandleAttack(request, this) is { } damageRecord)
            {
                response.TrinketDamages.Add(damageRecord);
            }
        }

        if (CheckIfKilledByDamage(response) is { } causeOfDeath)
        {
            DamageTaken?.Invoke(this, request, response);
            TriggerDeath(causeOfDeath);
            return;
        }

        if (Core.Random.Chance(request.Source.ChanceToHit()) == false && isTargetingImpActive == false)
        {
            response.Missed = true;
            DamageTaken?.Invoke(this, request, response);
            return;
        }

        var chanceToHitTargetedPart = 0.2f;
        if (request.TargetedPart != null && isTargetingImpActive == false && Core.Random.Chance(chanceToHitTargetedPart) == false)
        {
            response.Missed = true;
            DamageTaken?.Invoke(this, request, response);
            return;
        }

        if (Core.Random.Chance(this.GetStatValue(Defs.Stats.Evasion)))
        {
            response.Dodged = true;
            DamageTaken?.Invoke(this, request, response);
            return;
        }

        var bodyPart = request.TargetedPart == null
            ? Body.AllExternalParts.Where(p => p.IsDestroyed == false || p.AllInternalParts.Count != 0).RandomElementByWeight(part => part.HitWeight)!
            : Body.AllExternalParts.First(p => Equals(p, request.TargetedPart));

        foreach (var damage in request.RawDamages)
        {
            if (request.Source.PawnType == PawnType.Player)
            {
                request.Source.GetSkill(damage.WeaponType)?.Learn(1);
                request.Source.GetSkill(request.Source.Body.Stance)?.Learn(0.1f);
            }

            // Handle Armor
            var isPartCoveredByParentArmor = bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb;
            var bodyPartEquipment = isPartCoveredByParentArmor ? bodyPart.Socket?.ParentPart?.Armor : bodyPart.Armor;
            Item? destroyedArmor = null;
            if (bodyPartEquipment != null)
            {
                if (damage.Type.IsPhysicalDamage())
                {
                    bodyPartEquipment.ApplyDurabilityLoss(damage);
                }

                damage.Block(bodyPartEquipment);

                if (bodyPartEquipment.IsDestroyed)
                {
                    destroyedArmor = bodyPartEquipment;
                    if (isPartCoveredByParentArmor)
                    {
                        bodyPart.Socket!.ParentPart!.UnEquip(bodyPartEquipment);
                    }
                    else
                    {
                        bodyPart.UnEquip(bodyPartEquipment);
                    }
                }
            }

            // Create damage record after blocking is calculated
            var amountBlocked = damage.TotalDamage - damage.TotalUnblockedDamage;
            DamageRecord damageRecord = new(damage.Weapon.Label, request.WeaponManeuver.Label, damage.Type, bodyPart, damage.TotalDamage, amountBlocked);
            
            if (destroyedArmor != null)
            {
                damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(destroyedArmor.ItemDef));
            }

            //Handle Weapon Durability
            damage.Weapon.ApplyDurabilityLoss(bodyPartEquipment);
            if (damage.Weapon.IsDestroyed)
            {
                damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(damage.Weapon.ItemDef));
                request.Source.Equipment.UnEquip(damage.Weapon);
            }

            // Apply Damage
            damageRecord.BodyParts = bodyPart.ApplyDamageToExternalPart(damage);
            damageRecord.ActualAmount = damageRecord.BodyParts.Sum(p => p.DamageApplied);

            // Handle Enchantments
            var enchantments = bodyPart.Equipment.Values.SelectMany(e => e?.Enchantments?.ToList() ?? []);
            foreach (var enchantment in enchantments)
            {
                enchantment.EnchantmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
            }

            // Finish up
            Body.BodyPartsDirty = true;
            response.Damages.Add(damageRecord);
        }

        DamageTaken?.Invoke(this, request, response);

        if (CheckIfKilledByDamage(response) is { } cause)
        {
            TriggerDeath(cause);
        }
    }

    private string? CheckIfKilledByDamage(DamageResponse response)
    {
        List<string> nonFunctionalVitalParts = [];
        foreach (var damageRecord in response.Damages.Concat(response.TrinketDamages))
        {
            foreach (var partRecord in damageRecord.BodyParts)
            {
                if (!partRecord.IsVital) continue;

                if (partRecord.BodyPart.IsDestroyed)
                {
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was destroyed");
                }
                else if (partRecord.BodyPart is { IsExternal: true, IsSevered: true })
                {
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was severed");
                }
                else if (partRecord.BodyPart.IsFunctional == false)
                {
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} stopped functioning");
                }

                if (partRecord.BodyPart.DidPawnDieFromPartFailure() && nonFunctionalVitalParts.Any())
                {
                    return nonFunctionalVitalParts.First();
                }
            }
        }

        if (IsDead && nonFunctionalVitalParts.Count == 0)
        {
            Log.Error("Pawn is dead but no non-functional vital parts were found");
        }

        return null;
    }

    public void TriggerDeath(string causeOfDeath)
    {
        IsDead = true;
        Died?.Invoke(new DeathEvent
        {
            Pawn = this,
            Record = new DeathRecord
            {
                PawnName = Label,
                CauseOfDeath = causeOfDeath
            }
        });
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref PawnType, "PawnType");
        ScribeDeep.Look(ref Biography!, "Biography", this);
        ScribeDeep.Look(ref Traits!, "Traits", this);
        ScribeDeep.Look(ref Mind!, "Mind", this);
        ScribeDeep.Look(ref Body!, "Body", this);
        ScribeDeep.Look(ref Skills!, "Skills", this);
        ScribeDeep.Look(ref Inventory!, "Inventory", this);
        ScribeDeep.Look(ref Equipment!, "Equipment", this);
        ScribeReferences.Look(ref Zone!, "Zone");
        base.ExposeData();
    }

    public Skill GetSkill(SkillDef skill)
    {
        return Skills.GetSkill(skill);
    }

    public Skill? GetSkill(WeaponType weaponType)
    {
        return Skills.GetSkill(weaponType);
    }

    public Skill? GetSkill(BodyStanceDef stance)
    {
        return Skills.GetSkill(stance);
    }

    public float ChanceToHit()
    {
        return this.GetStatValue(Defs.Stats.MeleeAccuracy) * Body.Capabilities.Sight;
    }

    public bool TryEat(Item? item)
    {
        if (item?.ItemDef.FoodProperties == null)
        {
            Log.Error($"failed to eat null item '{item}'");
            return false;
        }

        var goldenLipsMultiplier = HasActiveEffect(Defs.BodyEffects.GoldenLips) ? 1.5f : 1f;
        foreach (var record in item.ItemDef.FoodProperties.Effects)
        {
            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def, TicksLeft = (int)(record.DurationInTicks * goldenLipsMultiplier)
            });
        }
        
        FoodConsumed?.Invoke(this, item);

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        Body.StomachLevel += nutrition;
        Body.Energy = Body.MaxEnergy;

        item.StackSize--;
        if (item.StackSize < 1)
        {
            item.Destroy();
        }

        return true;
    }

    private bool HasActiveEffect(BodyEffectDef effect)
    {
        return Body.Effects.Has(effect);
    }

    public void ResetAttackCoolDown()
    {
        TicksToAttack = CalculateTicksToAttack();
    }

    private int CalculateTicksToAttack()
    {
        if (AttackSpeed <= 0)
        {
            return 99999;
        }

        return Mathf.CeilToInt(Core.TicksPerSecond / AttackSpeed);
    }
}