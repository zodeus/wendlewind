using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly] // Used by EntityGenerator, referenced by EntityDef.EntityClass
public class Pawn : Entity {
    public RaceDef Race = null!;
    public PawnBiography Biography = null!;
    public PawnTraits Traits = null!;
    public PawnBrain Brain = null!;
    public PawnBody Body = null!;
    public PawnHealth Health = null!;
    public PawnNeeds Needs = null!;
    public PawnSkills Skills = null!;
    public PawnInventory Inventory = null!;
    public PawnEquipment Equipment = null!;
    public PawnType PawnType = PawnType.Invalid;

    private int _sequencePoints = 0;
    public bool SequencePointDirty = true;

    private bool _isDead = false;

    public PawnDef PawnDef => (PawnDef) Def;

    public string Species => PawnDef.Label;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public override Texture2D Icon => Race.Icon;

    public bool IsDead {
        get => _isDead;
        set => _isDead = value;
    }

    public bool IsIncapacitated => false; //todo Health.IsIncapacitated;

    public Gender Gender => Biography.Gender;

    public int SequencePoints {
        get {
            if (SequencePointDirty) {
                CalculateSequencePoints();
            }

            return _sequencePoints;
        }
    }

    public int MaxCarryWeight = 0;

    public override void Initialize() {
        MaxCarryWeight = (int) this.GetStatValue(Defs.Stats.MaxCarryWeight);
        Biography = new PawnBiography(this);
        Traits = new PawnTraits(this);
        Brain = new PawnBrain(this);
        Body = new PawnBody(this);
        Health = new PawnHealth(this);
        Needs = new PawnNeeds(this);
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

        Health.Tick();
        if (IsDead) {
            return;
        }

        Needs.Tick();
        if (IsDead) {
            return;
        }

        Skills.Tick();

        Inventory.Tick();
        Equipment.Tick();
        base.Tick();
    }

    private void CalculateSequencePoints() {
        _sequencePoints = 0;
        foreach (BodyPart bodyPart in Body.AllExternalParts) {
            if (bodyPart.HasMobility == false) {
                continue;
            }

            _sequencePoints += Mathf.FloorToInt(bodyPart.GetStatValue(Defs.Stats.SequencePoints));
        }

        //todo this calculates lung capacity, I think there should be a capacities list object somewhere instead, perhaps PawnBody or on Pawn? Need to add events to BodyPart to properly implement

        _sequencePoints = Mathf.RoundToInt(_sequencePoints * (Body.AllParts.Count(p => p.BodyPartDef.BodyPartType == BodyPartType.Lung && p.IsFunctional) > 1 ? 1f : .5f));
    }

    public DamageResponse TakeDamage(DamageRequest request) {
        BodyPart bodyPart = Body.AllExternalParts /*.Where(p => p.Type == BodyPartType.Torso)*/.RandomElementByWeight(part => part.BodyPartDef.HitWeight)!;

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
            response.Damages.Add(damageRecord);
            SequencePointDirty = true;
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
        return this.GetStatValue(Defs.Stats.MeleeChanceToHit) * Health.Capabilities.Sight;
    }
}