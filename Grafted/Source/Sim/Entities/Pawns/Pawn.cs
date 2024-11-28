using Grafted.Utils.Timers;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly] // Used by EntityGenerator, referenced by EntityDef.EntityClass
public class Pawn : Entity, IExposable
{
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
    public float AttackSpeed => Body.GetAttackSpeedModifier() * this.GetStatValue(Defs.Stats.AttackSpeed);

    public event Action<Pawn, string>? OnDeath;

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

    public override void Tick(int ticks)
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

        Body.Tick(ticks);
        if (IsDead)
        {
            return;
        }

        TicksToAttack--;
        Skills.Tick();
        base.Tick(ticks);
    }

    public DamageResponse TakeDamage(DamageRequest request)
    {
        DamageResponse response = new();
        if (Core.Random.Chance(this.GetStatValue(Defs.Stats.Evasion)))
        {
            return new DamageResponse { Dodged = true };
        }

        BodyPart bodyPart = Body.AllExternalParts.RandomElementByWeight(part => part.HitWeight)!;
        foreach (var damage in request.RawDamages)
        {
            DamageRecord damageRecord = new(damage.Type, bodyPart, damage.Amount);
            request.Source.GetSkill(damage.ToolType)?.Learn(10);
            int amountToApply = damage.Amount;

            // Handle Armor
            Item? bodyPartEquipment = bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb ? bodyPart.Socket!.ParentPart!.Armor : bodyPart.Armor;
            if (bodyPartEquipment != null)
            {
                if (damage.Type.IsPhysicalDamage())
                    bodyPartEquipment.ApplyDurabilityLoss(damage);
                if (bodyPartEquipment.IsDestroyed)
                {
                    damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(bodyPartEquipment.ItemDef));
                    bodyPart.UnEquip(bodyPartEquipment);
                }

                damage.UnblockedAmount = Mathf.Clamp(amountToApply - (int)bodyPartEquipment.GetStatValue(Defs.Stats.PhysicalResistance), 0, damage.Amount);
            }

            damageRecord.ActualAmount = damage.UnblockedAmount;
            //Handle Weapon Durability
            damage.Tool.ApplyDurabilityLoss(bodyPartEquipment);
            if (damage.Tool.IsDestroyed)
            {
                damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(damage.Tool.ItemDef));
                request.Source.Equipment.UnEquip(damage.Tool);
            }

            damageRecord.BodyParts = bodyPart.ApplyDamage(damage);
            Body.BodyPartsDirty = true;
            response.Damages.Add(damageRecord);
            if (CheckIfKilledByAttack(damageRecord, response))
            {
                break;
            }
        }

        /*if (request.HealthConditions != null) {
            response.HealthConditions = new List<HealthConditionDef>();
            foreach (HealthConditionDef conditionDef in request.HealthConditions) {
                Health.TryAddHealthCondition(conditionDef);
                response.HealthConditions.Add(conditionDef);
            }
        }*/

        return response;
    }

    private bool CheckIfKilledByAttack(DamageRecord damageRecord, DamageResponse response)
    {
        List<string> nonFunctionalVitalParts = new();
        string causeOfDeath = "ERROR";
        var died = false;
        foreach (DamagedPartRecord partRecord in damageRecord.BodyParts)
        {
            if (partRecord.IsVital)
            {
                bool partIsFunctional = true;
                if (partRecord.BodyPart.IsDestroyed)
                {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was destroyed");
                }
                else if (partRecord.BodyPart.IsExternal && partRecord.BodyPart.IsSevered)
                {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was severed");
                }
                else if (partRecord.BodyPart.IsFunctional == false)
                {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} stopped functioning");
                }

                if (partIsFunctional == false && Body.AllParts.Any(p => p.Type == partRecord.PartType && p.IsFunctional) == false)
                {
                    died = true;
                    response.Killed = true;
                    causeOfDeath = nonFunctionalVitalParts.Last();
                }
            }
        }

        if (died)
        {
            HandleDeath(causeOfDeath);
            return true;
        }

        return false;
    }

    public void HandleDeath(string causeOfDeath)
    {
        IsDead = true;
        OnDeath?.Invoke(this, causeOfDeath);
        // Core.Context.World.DeathRecords.RecordDeath(new DeathRecord
        // {
        //     Round = Core.Context.World.TotalKills + 1,
        //     PawnName = Label,
        //     CauseOfDeath = causeOfDeath
        // });
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PawnType, "PawnType");
        Scribe_Defs.Look(ref Race!, "Race");
        Scribe_Deep.Look(ref Biography!, "Biography", this);
        Scribe_Deep.Look(ref Traits!, "Traits", this);
        Scribe_Deep.Look(ref Mind!, "Mind", this);
        Scribe_Deep.Look(ref Body!, "Body", this);
        Scribe_Deep.Look(ref Skills!, "Skills", this);
        Scribe_Deep.Look(ref Inventory!, "Inventory", this);
        Scribe_Deep.Look(ref Equipment!, "Equipment", this);
        Scribe_References.Look(ref Zone!, "Zone");
        base.ExposeData();
    }

    public Skill GetSkill(SkillDef skill)
    {
        return Skills.GetSkill(skill);
    }

    public Skill? GetSkill(ToolType toolType)
    {
        return Skills.GetSkill(toolType);
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

        foreach (BodyEffectRecord record in item.ItemDef.FoodProperties.Effects)
        {
            Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = record.Def, TicksLeft = record.DurationInTicks
            });
        }

        float nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        Body.StomachLevel += nutrition;
        Body.Energy += nutrition / 3;
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
        var ticksPerSecond = 60;
        TicksToAttack = Mathf.CeilToInt(ticksPerSecond / AttackSpeed);
    }
}