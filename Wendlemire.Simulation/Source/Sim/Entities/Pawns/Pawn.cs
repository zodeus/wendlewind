using Wendlemire.Sim.Achievements.Handlers;

namespace Wendlemire.Sim.Entities.Pawns;

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
    public CombatStomach CombatStomach = null!;
    public List<ActiveIncense> ActiveIncense = [];
    public int IncenseCapacity = IncenseProperties.BaseSlots;
    public int PotionCapacity = PotionSlots.BaseSlots;
    public string NamePlateMoniker = CosmeticDefaults.NamePlate;
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
        CombatStomach = new CombatStomach(this);
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
        SetBonuses.Tick(this);
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
            var earlyExit = equipment.EquipmentHandler?.OnPreDamageTaken(request, response) ?? false;
            if (earlyExit)
            {
                DamageTaken?.Invoke(this, request, response);
                return;
            }
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
            var extraResist = TrinketResistance.SumFor(this, damage.Type);
            extraResist += this.GetStatValue(Defs.Stats.PhysicalResistance);
            if (damage.Type == DamageType.Magic)
            {
                extraResist += this.GetStatValue(Defs.Stats.MagicResistance);
            }

            var blockingTrinket = extraResist > 0 ? TrinketResistance.FirstContributor(this, damage.Type) : null;
            Item? destroyedArmor = null;
            if (bodyPartEquipment != null || extraResist > 0)
            {
                damage.Block(bodyPartEquipment, extraResist);

                if (bodyPartEquipment is { IsDestroyed: true })
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
            DamageRecord damageRecord = new(
                damage.Weapon.Label,
                request.WeaponManeuver.Label,
                damage.Type,
                bodyPart,
                damage.TotalDamage,
                amountBlocked,
                damage.IsCritical,
                damage.Weapon.ItemDef.Moniker);
            if (bodyPartEquipment != null && amountBlocked > 0)
            {
                damageRecord.BlockingItemMoniker = bodyPartEquipment.ItemDef.Moniker;
                damageRecord.BlockingItemLabel = bodyPartEquipment.Label;
            }
            else if (blockingTrinket != null && amountBlocked > 0)
            {
                damageRecord.BlockingItemMoniker = blockingTrinket.ItemDef.Moniker;
                damageRecord.BlockingItemLabel = blockingTrinket.Label;
            }

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
            var actualAmount = 0d;
            foreach (var damaged in damageRecord.BodyParts)
            {
                actualAmount += damaged.DamageApplied;
            }

            damageRecord.ActualAmount = actualAmount;

            foreach (var equipped in bodyPart.Equipment.Values)
            {
                if (equipped?.Enchantments == null)
                {
                    continue;
                }

                foreach (var enchantment in equipped.Enchantments)
                {
                    enchantment.EnchantmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
                }
            }

            foreach (var equipment in bodyPart.Equipment.Values)
            {
                if (equipment?.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.Cloak)
                {
                    continue;
                }

                equipment?.EquipmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
            }

            foreach (var equipment in Equipment)
            {
                if (equipment?.ItemDef.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.Cloak)
                {
                    continue;
                }

                equipment.EquipmentHandler?.PostPawnDamageTakenEffect(bodyPart, this, request.Source, damageRecord);
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
        if (IsDeadFromPartFailure() is not { } deathRecord)
        {
            return null;
        }

        if (!TryDescribeKillingBlow(response.Damages, deathRecord, out var weaponLabel, out var maneuverLabel, out var cause)
            && !TryDescribeKillingBlow(response.TrinketDamages, deathRecord, out weaponLabel, out maneuverLabel, out cause))
        {
            return deathRecord;
        }

        deathRecord.CauseOfDeath = cause;
        deathRecord.KillingWeapon = weaponLabel;
        deathRecord.KillingManeuver = maneuverLabel;
        return deathRecord;
    }

    private static bool TryDescribeKillingBlow(
        List<DamageRecord> damages,
        DeathRecord deathRecord,
        out string weaponLabel,
        out string maneuverLabel,
        out string cause)
    {
        weaponLabel = deathRecord.KillingWeapon ?? "";
        maneuverLabel = deathRecord.KillingManeuver ?? "";
        cause = deathRecord.CauseOfDeath ?? "";
        foreach (var damageRecord in damages)
        {
            foreach (var partRecord in damageRecord.BodyParts)
            {
                string? partCause = null;
                if (partRecord.BodyPart.IsDestroyed)
                {
                    partCause = $"{partRecord.PartType} was destroyed";
                }
                else if (partRecord.BodyPart is { IsExternal: true, IsSevered: true })
                {
                    partCause = $"{partRecord.PartType} was severed";
                }
                else if (partRecord.BodyPart.IsFunctional == false)
                {
                    partCause = $"{partRecord.PartType} stopped functioning";
                }

                if (partCause == null)
                {
                    continue;
                }

                cause = $"{partCause} ({deathRecord.FailedOrgan} failed)";
                weaponLabel = damageRecord.WeaponLabel;
                maneuverLabel = damageRecord.WeaponManeuverLabel;
                return true;
            }
        }

        return false;
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
        // Dead if ALL parts of any vital type are non-functional
        var parts = Body.AllParts;
        for (var i = 0; i < parts.Count; i++)
        {
            var candidate = parts[i];
            if (!candidate.IsVital)
            {
                continue;
            }

            var anyFunctional = false;
            for (var j = 0; j < parts.Count; j++)
            {
                var other = parts[j];
                if (other.Type != candidate.Type || !other.IsVital)
                {
                    continue;
                }

                if (other.IsFunctional)
                {
                    anyFunctional = true;
                    break;
                }
            }

            if (!anyFunctional)
            {
                return new DeathRecord
                {
                    FailedOrgan = candidate.Label,
                    CauseOfDeath = $"All {candidate.Type} organs are non-functional",
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
        ScribeDeep.Look(ref CombatStomach!, "CombatStomach", this);
        ScribeCollections.Look(ref ActiveIncense!, "ActiveIncense", LookMode.Deep);
        ScribeValues.Look(ref NamePlateMoniker!, "NamePlateMoniker", CosmeticDefaults.NamePlate);
        ScribeReferences.Look(ref Zone!, "Zone");
        if (string.IsNullOrWhiteSpace(NamePlateMoniker))
        {
            NamePlateMoniker = CosmeticDefaults.NamePlate;
        }
        MedicalChest ??= new MedicalChest(this);
        MealPlan ??= new MealPlan(this);
        CombatStomach ??= new CombatStomach(this);
        ActiveIncense ??= [];
        PruneActiveIncense();
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

        foreach (var record in item.ItemDef.FoodProperties.Effects)
        {
            if (record.Def == Defs.BodyEffects.FoodPoisoning && Traits.HasTrait(Defs.Traits.GutMicroacrobatics))
            {
                continue;
            }

            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def,
                TicksLeft = 1,
                LastsWholeEncounter = true
            });
        }

        CombatStomach.TryAdd(item.ItemDef);
        return true;
    }

    public bool RemoveIngestedFood(int index)
    {
        if (!CombatStomach.TryRemoveAt(index))
        {
            return false;
        }

        ResyncEncounterEffectStacks();
        return true;
    }

    public bool CanLightIncense(Item item, bool requireFlameStick = true)
    {
        PruneActiveIncense();
        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect == null || item.IsDestroyed || item.StackSize < 1)
        {
            return false;
        }

        if (requireFlameStick && !HasFlameStick())
        {
            return false;
        }

        if (ActiveIncense.Any(a => a.Def == incenseProps.Effect.Def))
        {
            return false;
        }

        return ActiveIncense.Count < IncenseCapacity;
    }

    public bool TryLightIncense(Item item, bool requireFlameStick = true)
    {
        if (!CanLightIncense(item, requireFlameStick))
        {
            return false;
        }

        var incenseProps = item.ItemDef.IncenseProperties!;
        ActiveIncense.Add(new ActiveIncense
        {
            Def = incenseProps.Effect.Def,
            EncountersRemaining = incenseProps.GetDurationInEncounters(),
            SourceMoniker = item.ItemDef.Moniker
        });

        Context.Achievements.OnItemUsed(this, item);
        return true;
    }

    public void ExtinguishIncense(int index)
    {
        if (index < 0 || index >= ActiveIncense.Count)
        {
            return;
        }

        ActiveIncense.RemoveAt(index);
    }

    public void PruneActiveIncense()
    {
        ActiveIncense.RemoveAll(a => a == null || a.Def == null || a.EncountersRemaining <= 0);
        if (ActiveIncense.Count > IncenseCapacity)
        {
            ActiveIncense.RemoveRange(IncenseCapacity, ActiveIncense.Count - IncenseCapacity);
        }
    }

    public void RefreshConsumableSlots(int prepRound)
    {
        var caps = PrepSlotUnlocks.ForRound(prepRound);
        MedicalChest.RefreshCapacity(caps.Medical);
        MealPlan.RefreshCapacity(caps.Food);
        CombatStomach.Capacity = MealPlan.Capacity;
        IncenseCapacity = caps.Incense;
        PruneActiveIncense();
        PotionCapacity = caps.Potion;
        UnequipLockedPotions();
    }

    private void UnequipLockedPotions()
    {
        foreach (var part in Body.AllExternalParts)
        {
            if (!part.HasEquipmentSlots)
            {
                continue;
            }

            foreach (var slot in part.EquipmentSlots!)
            {
                if (!PotionSlots.IsPotionSlot(slot) || PotionSlots.IsUnlocked(slot, PotionCapacity))
                {
                    continue;
                }

                var item = Equipment.UnEquip(part, slot);
                if (item != null)
                {
                    Inventory.TryAdd(item);
                }
            }
        }
    }

    public void ApplyBattleStartConsumables()
    {
        Body.StomachLevel = 1;
        CombatStomach.Clear();
        MealPlan.Prune();
        foreach (var item in MealPlan.Items.ToList())
        {
            if (item is { IsDestroyed: false, StackSize: > 0 })
            {
                TryEatForBattle(item);
            }
        }

        MealPlan.Prune();
        Body.ApplyBodyScale();
    }

    public void ResetIncenseForEncounter()
    {
        foreach (var incense in ActiveIncense)
        {
            incense.FiredThisEncounter = false;
        }
    }

    public bool TryIgniteIncense(ActiveIncense incense)
    {
        if (incense.FiredThisEncounter || incense.Def == null)
        {
            return false;
        }

        incense.FiredThisEncounter = true;
        Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = incense.Def,
            TicksLeft = incense.GetDurationInTicks()
        });
        return true;
    }

    public void ClearActiveIncenseEffects()
    {
        foreach (var incense in ActiveIncense)
        {
            if (incense.Def != null)
            {
                Body.Effects.TryRemove(incense.Def);
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

        DecrementStack(item);
    }

    private static void DecrementStack(Item item)
    {
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

    private void ResyncEncounterEffectStacks()
    {
        var stacks = new Dictionary<BodyEffectDef, float>();
        foreach (var food in CombatStomach.Items)
        {
            AddEffectStacks(food.Def?.FoodProperties?.Effects, stacks);
        }

        foreach (var incense in ActiveIncense)
        {
            if (incense.Def != null)
            {
                stacks[incense.Def] = stacks.GetValueOrDefault(incense.Def) + 1f;
            }
        }

        Body.Effects.SyncEncounterStacks(stacks);
    }

    private void AddEffectStacks(List<BodyEffectRecord>? records, Dictionary<BodyEffectDef, float> stacks)
    {
        if (records == null)
        {
            return;
        }

        foreach (var record in records)
        {
            if (record.Def == null)
            {
                continue;
            }

            if (record.Def == Defs.BodyEffects.FoodPoisoning && Traits.HasTrait(Defs.Traits.GutMicroacrobatics))
            {
                continue;
            }

            stacks[record.Def] = stacks.GetValueOrDefault(record.Def) + 1f;
        }
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

        return Mathf.CeilToInt(GameContext.TicksPerSecond / (AttackSpeed * 2f));
    }
}