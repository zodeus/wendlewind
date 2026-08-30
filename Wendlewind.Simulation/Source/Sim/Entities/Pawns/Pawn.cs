namespace Wendlewind.Sim.Entities.Pawns;

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
    public MedicalChest MedicalChest = null!;
    public MealPlan MealPlan = null!;
    public List<ActiveIncense> ActiveIncense = [];
    public PawnType PawnType = PawnType.Invalid;
    public Zone? Zone;
    public int TicksToAttack;

    public PawnDef PawnDef => (PawnDef)Def;
    public string Species => PawnDef.Species;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public bool IsHungry => Body.IsHungry;
    public bool IsFamished => Body.IsFamished;
    public bool IsDead { get; private set; }
    public Gender Gender => Biography.Gender;
    public float AttackSpeed => Body.GetAttackSpeedModifier() * this.GetStatValue(Defs.Stats.AttackSpeed);

    public void GenerateBody(float bodySizeFactor)
    {
        Body.BodySizeFactor = bodySizeFactor;
        Body.BloodAmount = Body.MaxBlood;
        PawnDef.Body.CreateGenerator(Context.Factory).Generate(this);
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
        MedicalChest = new MedicalChest(this);
        MealPlan = new MealPlan(this);
        ActiveIncense = [];
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
        Equipment.Tick();
        base.Tick();
    }

    public void TakeDamage(DamageRequest request)
    {
        var bodyPart = request.TargetedPart;
        DamageResponse response = new();
        if (Context.Rng.Chance(request.Source.ChanceToHit()) == false)
        {
            response.Missed = true;
            DamageTaken?.Invoke(this, request, response);
            return;
        }

        if (Context.Rng.Chance(this.GetStatValue(Defs.Stats.Evasion)))
        {
            response.Dodged = true;
            DamageTaken?.Invoke(this, request, response);
            return;
        }


        // Handle Equipment Pre-Damage Taken Effects
        foreach (var equipment in bodyPart.Equipment.Values)
        {
            if (equipment == null) continue;
            var earlyExit = equipment?.EquipmentHandler?.OnPreDamageTaken(request, response) ?? false;
            DamageTaken?.Invoke(this, request, response);
            if (earlyExit) return;
        }

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
            DamageRecord damageRecord = new(damage.Weapon.Label, request.WeaponManeuver.Label, damage.Type, bodyPart, damage.TotalDamage, amountBlocked, damage.IsCritical);

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

            // Handle Equipment Post-Damage Effects
            foreach (var equipment in bodyPart.Equipment.Values.Where(e => e?.ItemDef.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.Cloak))
            {
                equipment?.EquipmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
            }

            // Handle Cloak Post-Damage Effects
            foreach (var equipment in Equipment.Where(e => e?.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.Cloak))
            {
                equipment?.EquipmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
            }
 
            // Handle Weapon Handler (unique weapon effects)
            damage.Weapon.WeaponHandler?.OnHit(request.Source, this, request, damageRecord);

            // Finish up
            Body.BodyPartsDirty = true;
            response.Damages.Add(damageRecord);
        }

        if (request.Source.PawnType == PawnType.Player)
        {
            foreach (var trinket in request.Source.Inventory.Trinkets)
            {
                if (trinket.TrinketHandler == null) continue;
                var damageRecord = trinket.TrinketHandler!.PostAttackHandler(this, request, response);
                if (damageRecord is not { })
                {
                    continue;
                }
                response.TrinketDamages.Add(damageRecord);
            }
        }

        DamageTaken?.Invoke(this, request, response);
        if (CheckIfKilledByDamage(response) is DeathRecord deathRecord)
        {
            TriggerDeath(deathRecord);
            return;
        }
    }

    private DeathRecord? CheckIfKilledByDamage(DamageResponse response)
    {
        List<string> nonFunctionalVitalParts = [];
        foreach (var damageRecord in response.Damages.Concat(response.TrinketDamages))
        {
            foreach (var partRecord in damageRecord.BodyParts)
            {
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

                if (IsDeadFromPartFailure() is { } deathRecord && nonFunctionalVitalParts.Any())
                {
                    deathRecord.CauseOfDeath = $"{nonFunctionalVitalParts.First()} ({deathRecord.FailedOrgan} failed)";
                    deathRecord.KillingWeapon = damageRecord.WeaponLabel;
                    deathRecord.KillingManeuver = damageRecord.WeaponManeuverLabel;
                    return deathRecord;
                }
            }
        }
        return null;
    }

    public void TriggerDeath(DeathRecord deathRecord)
    {
        IsDead = true;
        Died?.Invoke(new DeathEvent
        {
            Pawn = this,
            Record = new DeathRecord
            {
                PawnName = Label,
                CauseOfDeath = deathRecord.CauseOfDeath,
                KillingWeapon = deathRecord.KillingWeapon,
                KillingManeuver = deathRecord.KillingManeuver
            }
        });
    }

    public DeathRecord? IsDeadFromPartFailure()
    {
        // Group vital parts by type - you're dead if ALL parts of any vital type are non-functional
        var vitalPartsByType = Body.AllParts
            .Where(p => p.IsVital)
            .GroupBy(p => p.Type);

        foreach (var group in vitalPartsByType)
        {
            var functionalCount = group.Count(p => p.IsFunctional);
            if (functionalCount == 0)
            {
                return new DeathRecord
                {
                    FailedOrgan = group.First().Label,
                    CauseOfDeath = $"All {group.Key} organs are non-functional",
                    KillingWeapon = "Organ failure",
                    KillingManeuver = "Organ failure"
                };
            }
        }

        return null;
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
        ScribeDeep.Look(ref MedicalChest!, "MedicalChest", this);
        ScribeDeep.Look(ref MealPlan!, "MealPlan", this);
        ScribeCollections.Look(ref ActiveIncense!, "ActiveIncense", LookMode.Deep);
        ScribeReferences.Look(ref Zone!, "Zone");
        MedicalChest ??= new MedicalChest(this);
        MealPlan ??= new MealPlan(this);
        ActiveIncense ??= [];
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
        return this.GetStatValue(Defs.Stats.Accuracy) * Body.Capabilities.Sight;
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
            if (record.Def == Defs.BodyEffects.FoodPoisoning && Traits.HasTrait(Defs.Traits.GutMicroacrobatics))
            {
                continue;
            }

            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def,
                TicksLeft = (int)(record.DurationInTicks * goldenLipsMultiplier)
            });
        }

        ApplyEatCost(item);
        return true;
    }

    public bool TryEatForBattle(Item? item)
    {
        if (item?.ItemDef.FoodProperties == null)
        {
            Log.Error($"failed to eat null item '{item}'");
            return false;
        }

        var goldenLipsMultiplier = HasActiveEffect(Defs.BodyEffects.GoldenLips) ? 1.5f : 1f;
        foreach (var record in item.ItemDef.FoodProperties.Effects)
        {
            if (record.Def == Defs.BodyEffects.FoodPoisoning && Traits.HasTrait(Defs.Traits.GutMicroacrobatics))
            {
                continue;
            }

            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def,
                TicksLeft = Math.Max(1, (int)(record.DurationInTicks * goldenLipsMultiplier)),
                LastsWholeEncounter = true
            });
        }

        ApplyEatCost(item);
        return true;
    }

    public bool TryLightIncense(Item item, bool requireFlameStick = true)
    {
        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect == null)
        {
            return false;
        }

        if (requireFlameStick && !HasFlameStick())
        {
            return false;
        }

        var charges = incenseProps.GetDurationInEncounters();
        var existing = ActiveIncense.FirstOrDefault(a => a.Def == incenseProps.Effect.Def);
        if (existing != null)
        {
            existing.EncountersRemaining += charges;
        }
        else
        {
            ActiveIncense.Add(new ActiveIncense
            {
                Def = incenseProps.Effect.Def,
                EncountersRemaining = charges,
                SourceMoniker = item.ItemDef.Moniker
            });
        }

        Context.Achievements.OnItemUsed(this, item);
        item.StackSize--;
        if (item.StackSize < 1)
        {
            item.Destroy();
        }

        return true;
    }

    public void ApplyBattleStartConsumables()
    {
        Body.StomachLevel = 0;
        MealPlan.Prune();
        foreach (var item in MealPlan.Items.ToList())
        {
            TryEatForBattle(item);
        }

        MealPlan.Prune();

        for (var i = ActiveIncense.Count - 1; i >= 0; i--)
        {
            var incense = ActiveIncense[i];
            if (incense.Def == null || incense.EncountersRemaining <= 0)
            {
                ActiveIncense.RemoveAt(i);
                continue;
            }

            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = incense.Def,
                TicksLeft = 1,
                LastsWholeEncounter = true
            });

            incense.EncountersRemaining--;
            if (incense.EncountersRemaining <= 0)
            {
                ActiveIncense.RemoveAt(i);
            }
        }
    }

    public bool HasFlameStick()
    {
        if (Inventory.Trinkets.Any(t => t.Def == Defs.Items.FlameStick))
        {
            return true;
        }

        return PawnType == PawnType.Player && Context.World.Player.HasTrinket(Defs.Items.FlameStick);
    }

    private void ApplyEatCost(Item item)
    {
        FoodConsumed?.Invoke(this, item);

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        if (Traits.HasTrait(Defs.Traits.PotBellied))
        {
            nutrition *= 0.75f;
        }

        Body.StomachLevel = Mathf.Clamp(Body.StomachLevel + nutrition, 0f, 1f);
        Body.Energy = Body.MaxEnergy;

        item.StackSize--;
        if (item.StackSize < 1)
        {
            item.Destroy();
        }
    }

    private bool HasActiveEffect(BodyEffectDef effect)
    {
        return Body.Effects.Has(effect);
    }

    public void ResetAttackCoolDown()
    {
        TicksToAttack = CalculateTicksToAttack();
    }

    public int CalculateTicksToAttack()
    {
        if (AttackSpeed <= 0)
        {
            return 99999;
        }
        if (AttackSpeed > GameContext.TicksPerSecond)
        {
            Log.Warning($"{Label} has attack speed greater than {GameContext.TicksPerSecond}, setting to 1");
            return 1;
        }

        return Mathf.CeilToInt(GameContext.TicksPerSecond / AttackSpeed);
    }
}