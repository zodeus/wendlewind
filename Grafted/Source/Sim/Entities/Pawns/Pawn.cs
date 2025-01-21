using Grafted.Utils.Timers;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly] // Used by EntityGenerator, referenced by EntityDef.EntityClass
public class Pawn : Entity, IExposable
{
    public event Action<Pawn, DamageRequest, DamageResponse>? DamageTaken; //todo - actions
    public event Action<DeathEvent>? Died; //todo - actions

    public RaceDef Race = null!;
    public PawnBiography Biography = null!;
    public PawnTraits Traits = null!;
    public PawnMind Mind = null!;
    public PawnBody Body = null!;
    public PawnSkills Skills = null!;
    public PawnInventory Inventory = null!;
    public PawnEquipment Equipment = null!;
    public PawnType PawnType = PawnType.Invalid;
    public Zone? Zone;

    public bool IsResting;

    public int TicksToAttack;

    public PawnDef PawnDef => (PawnDef)Def;
    public string Species => PawnDef.Label;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public override Texture2D Icon => Race.Icon;
    public bool IsHungry => Body.IsHungry;
    public bool IsFamished => Body.IsFamished;
    public bool IsDead { get; private set; }
    public bool IsIncapacitated => false; //todo Health.IsIncapacitated;
    public Gender Gender => Biography.Gender;
    public float MaxAttackSpeed => this.GetStatValue(Defs.Stats.AttackSpeed);
    public float AttackSpeed => Body.GetAttackSpeedModifier() * this.GetStatValue(Defs.Stats.AttackSpeed) * Equipment.WeaponAttackSpeedModifier;

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
        Skills.Tick();
        Inventory.Tick();
        base.Tick();
    }

    public void TakeDamage(DamageRequest request)
    {
        DamageResponse response = new();

        // Add trinket damages 
        response.TrinketDamages.AddRange(request.TrinketResults);
        CheckIfKilledByDamage(response);
        if (IsDead)
        {
            DamageTaken?.Invoke(this, request, response);
            return;
        }

        if (Core.Random.Chance(request.Source.ChanceToHit(this)) == false)
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

        var bodyPart = Body.AllExternalParts
            .Where(p => p.IsDestroyed == false || p.AllInternalParts.Count != 0)
            .RandomElementByWeight(part => part.HitWeight)!;

        foreach (var damage in request.RawDamages)
        {
            if (request.Source.PawnType == PawnType.Player)
            {
                request.Source.GetSkill(damage.WeaponType)?.Learn(1);
            }

            DamageRecord damageRecord = new(damage.Tool.Label, request.WeaponManeuver.Label, damage.Type, bodyPart, damage.TotalDamage);

            // Handle Armor
            var isPartCoveredByParentArmor = bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb;
            var bodyPartEquipment = isPartCoveredByParentArmor ? bodyPart.Socket?.ParentPart?.Armor : bodyPart.Armor;
            if (bodyPartEquipment != null)
            {
                if (damage.Type.IsPhysicalDamage())
                {
                    bodyPartEquipment.ApplyDurabilityLoss(damage);
                }

                damage.Block(bodyPartEquipment);

                if (bodyPartEquipment.IsDestroyed)
                {
                    damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(bodyPartEquipment.ItemDef));
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

            //Handle Weapon Durability
            damage.Tool.ApplyDurabilityLoss(bodyPartEquipment);
            if (damage.Tool.IsDestroyed)
            {
                damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(damage.Tool.ItemDef));
                request.Source.Equipment.UnEquip(damage.Tool);
            }

            // Apply Damage
            damageRecord.ActualAmount = damage.TotalUnblockedDamage;
            damageRecord.BodyParts = bodyPart.ApplyDamageToExternalPart(damage);

            // Handle Enchantments
            var enchantments = bodyPart.Equipment.Values.SelectMany(e => e?.Enchantments?.ToList() ?? []);
            foreach (var enchantment in enchantments)
            {
                enchantment.EnchantmentHandler!.HandlePawnTakeDamageEffect(bodyPart, this, request.Source, damageRecord);
            }

            // Finish up
            Body.BodyPartsDirty = true;
            response.Damages.Add(damageRecord);
        }

        DamageTaken?.Invoke(this, request, response);

        CheckIfKilledByDamage(response);
    }

    private bool CheckIfKilledByDamage(DamageResponse response)
    {
        List<string> nonFunctionalVitalParts = [];
        var causeOfDeath = "ERROR";
        var died = false;
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

                if (partRecord.BodyPart.DidPawnDieFromPartFailure())
                {
                    died = true;
                    causeOfDeath = nonFunctionalVitalParts.Last();
                }
            }
        }

        if (!died) return false;

        TriggerDeath(causeOfDeath);

        return true;
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
        ScribeDefs.Look(ref Race!, "Race");
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

    public float ChanceToHit(Pawn target)
    {
        return this.GetStatValue(Defs.Stats.MeleeAccuracy) * Body.Capabilities.Sight;
    }

    public void TryEat(Item? item)
    {
        if (item?.ItemDef.FoodProperties == null)
        {
            Log.Error($"failed to eat null item '{item}'");
            return;
        }

        foreach (var record in item.ItemDef.FoodProperties.Effects)
        {
            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def, TicksLeft = record.DurationInTicks
            });
        }

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        Body.StomachLevel += nutrition;
        //Body.Energy += nutrition / 3;
        Body.Energy = Body.MaxEnergy;
        Core.Context.Messages.Push(new Message(
            $"/c[{TC.Victim}]{Core.Context.Player.Label} /c[{TC.Default}]ate /c[{TC.Item}]{item.Label}"
        ));

        item.StackSize--;
        if (item.StackSize < 1)
        {
            item.Destroy();
        }
    }

    public void ResetAttackCoolDown()
    {
        TicksToAttack = Mathf.CeilToInt(Core.TicksPerSecond / AttackSpeed);
    }
}