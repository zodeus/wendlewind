using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grafted.Coroutines;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class
    CombatSequence {
    public Pawn Target { get; set; }
    public Pawn Source { get; set; }
    public List<CombatSequenceStep> Steps { get; set; }
    public string? FlavorText { get; set; }
    public int TotalSequencePoints { get; set; }
    public float VisualWaitTimeMultiplier { get; set; } = 1;

    public static string TextColorPawn = "#fa00ff";
    public static string TextColorBodyPart = "#ff0085";
    public static string TextColorEquipment = "#0071ad";
    public static string TextColorEnemyPawn = "#ff4000";
    public static string TextColorDefault = "#b3b3b3";
    public static string TextColorSettlement = "#a600ff";
    public static string TextColorRed = "#b00000";
    public static string TextColorGreen = "#3dc200";
    public static string TextColorItem = "#fa9000";

    public IEnumerator Execute(CombatEvent combatEvent) {
        combatEvent.LogMessage($"    Performing \\c[#fa9000]{FlavorText} \\c[#b3b3b3](\\c[#00e6ff]{TotalSequencePoints}\\c[#b3b3b3])");
        yield return Coroutine.WaitForSeconds(0.1f);
        foreach (CombatSequenceStep step in Steps) {
            yield return Coroutine.WaitForSeconds(step.VisualWaitTime * VisualWaitTimeMultiplier);
            float chanceToHit = Source.ChanceToHit(Target);
            if (Core.Random.NextSingle() < chanceToHit) {
                DamageResponse damageResponse = Target.TakeDamage(step.Damages);
                foreach (DamagedPartRecord damage in damageResponse.Damages.SelectMany(r => r.BodyParts)) {
                    if (damage.BodyPart.IsExternal && damage.WasSevered) {
                        combatEvent.SeveredLimbs.Add(damage.BodyPart);
                    }
                }

                foreach (DamageRecord damageResult in damageResponse.Damages) {
                    combatEvent.LogMessage(
                        $"        \\c[#b3b3b3]Damaged \\c[{TextColorPawn}]{Target.LabelShort}'s \\c[{TextColorBodyPart}]{damageResult.BodyPart.Type} " +
                        $"\\c[#b3b3b3]with \\c[#fa9000]{step.Tool} \\c[#b3b3b3](\\c[#00ff11]{step.Name}" +
                        $"\\c[#b3b3b3]) for \\c[#ff0000]{damageResult.ActualAmount} \\c[#b3b3b3](\\c[#fa9000]{damageResult.DamageType}\\c[#b3b3b3]) health, " +
                        $"blocked \\c[#00e6ff]{damageResult.AmountBlocked}"
                    );

                    foreach (DestroyedItemRecord itemRecord in damageResult.DestroyedEquipment) {
                        combatEvent.LogMessage($"          \\c[{TextColorEquipment}]{itemRecord.Def.Label} \\c[{TextColorRed}]destroyed");
                    }

                    foreach (DamagedPartRecord partRecord in damageResult.BodyParts) {
                        if (partRecord.WasDestroyed && partRecord.IsVital == false) {
                            combatEvent.LogMessage($"          \\c[{TextColorBodyPart}]{partRecord.Label} \\c[{TextColorRed}]destroyed");
                        }

                        if (partRecord.WasDestroyed && partRecord.IsVital) {
                            combatEvent.LogMessage($"          \\c[{TextColorRed}]VITAL part \\c[{TextColorBodyPart}]{partRecord.Label} \\c[{TextColorRed}]destroyed");
                        }

                        if (partRecord.BodyPart.IsExternal && partRecord.WasSevered) {
                            combatEvent.LogMessage($"          \\c[{TextColorBodyPart}]{partRecord.Label} \\c[{TextColorRed}]SEVERED");
                        }
                    }
                }

                //todo
                /*if (damageResponse.HealthConditions != null) {
                    foreach (HealthConditionDef condition in damageResponse.HealthConditions) {
                        CombatEvent.LogMessage($"        \\c[#b3b3b3]Inflicted \\c[{TextColorPawn}]{Target.LabelShort} \\c[#b3b3b3]with \\c[#acc700]{condition.Label}");
                    }
                }*/

                if (Target.IsDead) {
                    //Target.Destroy();
                    combatEvent.LogMessage($"    \\c[#ff0000]Killed \\c[{TextColorPawn}]{Target.LabelShort}");
                    yield return Coroutine.WaitForSeconds(2);

                    // exiting sequence, target is dead 
                    yield break;
                }
            }
            else {
                combatEvent.LogMessage($"        Missed \\c[{TextColorPawn}]{Target.LabelShort} \\c[#b3b3b3]with \\c[#fa9000]{step.Tool} \\c[#b3b3b3]ChanceToHit = \\c[#00e6ff]{chanceToHit}");
            }
        }
    }
}