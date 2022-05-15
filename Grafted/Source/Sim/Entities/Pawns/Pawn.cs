using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Persistence;
using Grafted.Sim.Zones;
using Grafted.Utils;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly] // Used by EntityGenerator, referenced by EntityDef.EntityClass
public class Pawn : Entity, IExposable {
    private bool _isDead = false;

    public RaceDef Race = null!;
    public PawnBiography Biography = null!;
    public PawnTraits Traits = null!;
    public PawnBrain Brain = null!;
    public PawnBody Body = null!;
    public PawnSkills Skills = null!;
    public PawnInventory Inventory = null!;
    public PawnEquipment Equipment = null!;
    public PawnType PawnType = PawnType.Invalid;
    public Zone? Zone;
    public int MaxCarryWeight = 0;

    public bool IsResting;

    public PawnDef PawnDef => (PawnDef) Def;
    public string Species => PawnDef.Label;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public override Texture2D Icon => Race.Icon;
    public bool IsHungry => Body.StomachLevel < 0.6f;

    public bool IsDead {
        get => _isDead;
        set => _isDead = value;
    }

    public bool IsIncapacitated => false; //todo Health.IsIncapacitated;
    public Gender Gender => Biography.Gender;

    public int SequencePoints => Body.GetSequencePoints();

    public override void Initialize() {
        MaxCarryWeight = (int) this.GetStatValue(Defs.Stats.MaxCarryWeight);
        Biography = new PawnBiography(this);
        Traits = new PawnTraits(this);
        Brain = new PawnBrain(this);
        Body = new PawnBody(this);
        Body.Initialize();
        Skills = new PawnSkills(this);
        Inventory = new PawnInventory(this);
        Equipment = new PawnEquipment(this);
        base.Initialize();
    }

    public override void Tick() {
        if (IsDead) {
            return;
        }

        Brain.Tick();
        if (IsDead) {
            return;
        }

        Body.Tick();
        if (IsDead) {
            return;
        }

        if (IsDead) {
            return;
        }

        if (IsDead) {
            return;
        }

        Skills.Tick();

        Inventory.Tick();
        Equipment.Tick();
        base.Tick();
    }

    public DamageResponse TakeDamage(DamageRequest request) {
        BodyPart bodyPart = Body.AllExternalParts /*.Where(p => p.Type == BodyPartType.Torso)*/.RandomElementByWeight(part => part.HitWeight)!;

        DamageResponse response = new();
        foreach (Damage damage in request.RawDamages) {
            DamageRecord damageRecord = new(damage.Type, bodyPart, damage.Amount);
            request.Source.GetSkill(damage.ToolType)?.Learn(1);
            int amountToApply = damage.Amount;


            // Handle Armor
            Item? bodyPartEquipment = bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb ? bodyPart.Socket!.ParentPart!.Armor : bodyPart.Armor;
            if (bodyPartEquipment != null) {
                if (damage.Type.IsPhysicalDamage())
                    bodyPartEquipment.ApplyDurabilityLoss(damage);
                if (bodyPartEquipment.IsDestroyed) {
                    damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(bodyPartEquipment.ItemDef));
                    bodyPart.UnEquip(bodyPartEquipment);
                }

                damage.UnblockedAmount = Mathf.Clamp(amountToApply - (int) bodyPartEquipment.GetStatValue(Defs.Stats.PhysicalResistance), 0, damage.Amount);
            }

            damageRecord.ActualAmount = damage.UnblockedAmount;
            //Handle Weapon Durability
            damage.Tool.ApplyDurabilityLoss(bodyPartEquipment);
            if (damage.Tool.IsDestroyed) {
                damageRecord.DestroyedEquipment.Add(new DestroyedItemRecord(damage.Tool.ItemDef));
                request.Source.Equipment.UnEquip(damage.Tool);
            }

            damageRecord.BodyParts = bodyPart.ApplyDamage(damage);
            Body.BodyPartsDirty = true;
            response.Damages.Add(damageRecord);
            if (PawnDied(damageRecord, response)) {
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

    private bool PawnDied(DamageRecord damageRecord, DamageResponse response) {
        List<string> nonFunctionalVitalParts = new();
        string causeOfDeath = "ERROR";
        foreach (DamagedPartRecord partRecord in damageRecord.BodyParts) {
            if (partRecord.IsVital) {
                bool partIsFunctional = true;
                if (partRecord.BodyPart.IsDestroyed) {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was destroyed");
                }
                else if (partRecord.BodyPart.IsExternal && partRecord.BodyPart.IsSevered) {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} was severed");
                }
                else if (partRecord.BodyPart.IsFunctional == false) {
                    partIsFunctional = false;
                    nonFunctionalVitalParts.Add($"{partRecord.PartType} stopped functioning");
                }

                if (partIsFunctional == false && Body.AllParts.Any(p => p.Type == partRecord.PartType && p.IsFunctional) == false) {
                    _isDead = true;
                    response.Killed = true;
                    causeOfDeath = nonFunctionalVitalParts.Last();
                }
            }
        }

        if (IsDead) {
            Core.Sim.World.DeathRecords.RecordDeath(new DeathRecord {
                Round = Core.Sim.World.TotalKills + 1,
                PawnName = Label,
                CauseOfDeath = causeOfDeath
            });
            return true;
        }

        return false;
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref _isDead, "IsDead");
        Scribe_Values.Look(ref PawnType, "PawnType");
        Scribe_Values.Look(ref MaxCarryWeight, "MaxCarryWeight");
        Scribe_Defs.Look(ref Race!, "Race");
        Scribe_Deep.Look(ref Biography!, "Biography", this);
        Scribe_Deep.Look(ref Traits!, "Traits", this);
        Scribe_Deep.Look(ref Brain!, "Brain", this);
        Scribe_Deep.Look(ref Body!, "Body", this);
        Scribe_Deep.Look(ref Skills!, "Skills", this);
        Scribe_Deep.Look(ref Inventory!, "Inventory", this);
        Scribe_Deep.Look(ref Equipment!, "Equipment", this);
        Scribe_References.Look(ref Zone!, "Zone");
        base.ExposeData();
    }

    public Skill GetSkill(SkillDef skill) {
        return Skills.GetSkill(skill);
    }

    public Skill? GetSkill(ToolType toolType) {
        return Skills.GetSkill(toolType);
    }

    public IEnumerable<Item> GetAvailableToolsFor(ToolCategory usedFor) {
        foreach (Item item in Equipment.UsableItems) {
            if (item.CanBeUsedFor(usedFor)) {
                yield return item;
            }
        }
    }

    public bool HasToolFor(ToolCategory toolUse) {
        if (toolUse == ToolCategory.None) {
            return true;
        }

        foreach (Item item in Equipment) {
            if (item.ItemDef.ToolCategories.Contains(toolUse)) {
                return true;
            }
        }

        return false;
    }

    public bool HasTool(ToolType toolType) {
        if (toolType == ToolType.None) {
            return true;
        }

        foreach (Item item in Equipment) {
            if (item.ItemDef.ToolType == toolType) {
                return true;
            }
        }

        return false;
    }

    public float ChanceToHit(Pawn target) {
        return this.GetStatValue(Defs.Stats.MeleeAccuracy) * Body.Capabilities.Sight;
    }

    public void TryEat(Item? item) {
        if (item?.ItemDef.FoodProperties == null) {
            Log.Error($"failed to eat null item '{item}'");
            return;
        }

        foreach (BodyEffectDef effectDef in item.ItemDef.FoodProperties.Effects) {
            Body.Effects.TryApplyEffect(new BodyEffect {
                Def = effectDef, TicksLeft = SimTime.HoursToTicks(12)
            });
        }

        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(5));
        Body.StomachLevel = 1;
        Body.Energy += .3f;
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(5));
        Core.Sim.Messages.Push(new Message(
            $"\\c[{UiTextColor.TextColorPawn}]{Core.Sim.World.PlayerPawns[0].Label} \\c[{UiTextColor.TextColorDefault}]ate \\c[{UiTextColor.TextColorItem}]{item.Label}"
        ));

        item.StackSize--;
        if (item.StackSize < 1) {
            item.Destroy();
        }
    }
}