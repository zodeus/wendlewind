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
    public PawnBrain Brain = null!;
    public PawnBody Body = null!;
    public PawnHealth Health = null!;
    public PawnNeeds Needs = null!;
    public PawnInventory Inventory = null!;
    public PawnEquipment Equipment = null!;
    public PawnType PawnType = PawnType.Invalid;
    public PawnDef PawnDef => (PawnDef) Def;

    public string Species => PawnDef.Label;
    public override string Label => Biography.Name;
    public override string LabelShort => Biography.Name;
    public override Texture2D Icon => Race.Icon;

    public bool IsDead { get; set; } // todo need to set this 

    public bool IsIncapacitated => false; //todo Health.IsIncapacitated;

    public Gender Gender => Biography.Gender;

    public override void Initialize() {
        Biography = new PawnBiography(this);
        Brain = new PawnBrain(this);
        Body = new PawnBody(this);
        Health = new PawnHealth(this);
        Needs = new PawnNeeds(this);
        Inventory = new PawnInventory(this);
        Equipment = new PawnEquipment(this);
        base.Initialize();
    }

    public override void Tick() {
        base.Tick();
        Brain.Tick();
        Body.Tick();
        Health.Tick();
        Needs.Tick();

        if (IsDead) {
            return;
        }

        Inventory.Tick();
        Equipment.Tick();
    }

    public DamageResponse TakeDamage(DamageRequest request) {
        BodyPart bodyPart = Body.AllExternalParts.RandomElementByWeight(part => part.BodyPartDef.HitWeight)!;

        DamageResponse response = new();
        foreach (Damage damage in request.RawDamages) {
            float amountToApply = damage.Amount;
            if (damage.Type.IsPhysicalDamage()) {

                var bodyPartArmor = bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb ? bodyPart.Socket.ParentPart!.Armor : bodyPart.Armor;
                foreach (Item armor in bodyPartArmor) {
                    damage.UnblockedAmount = Mathf.Clamp(amountToApply - armor.GetStatValue(Defs.Stats.PhysicalResistance), 0, damage.Amount);
                }
            }

            List<DamagedPartRecord> damagedParts = bodyPart.ApplyDamage(damage);
            response.Damages.Add(new DamageRecord(damage.Type, bodyPart, damagedParts, damage.Amount, damage.UnblockedAmount));
            if (damagedParts.Any(p => p.IsVital && (p.WasDestroyed || p.WasSevered))) {
                IsDead = true;
                response.Killed = true;
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

    public override void ExposeData() {
        base.ExposeData();
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