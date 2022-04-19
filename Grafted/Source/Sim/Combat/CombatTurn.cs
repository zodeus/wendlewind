using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Grafted.Coroutines;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Utils;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Combat;

public class CombatTurn {
    public readonly CombatEvent _combatEvent;
    public List<Pawn> Pawns = new();
    public List<CombatSequence> Sequences = new();
    public Dictionary<Pawn, PawnTurnData> PawnTurnData = new();
    public Pawn? CurrentPawn { get; set; }

    public CombatTurn(CombatEvent combatEvent) {
        _combatEvent = combatEvent;

    }

    public void Prepare() {
        Pawns = Pawns.OrderByDescending(pawn => StatExtensions.GetStatValue(pawn, Defs.Stats.AttackSpeed)).ToList();
    }

    public IEnumerator Run() {
        _combatEvent.LogMessage($"\\c[white]Starting turn {_combatEvent.CurrentTurnNum}");
        yield return Coroutine.WaitForSeconds(0.25f);
        foreach (Pawn attacker in Pawns) {
            CurrentPawn = attacker;
            if (attacker.IsDead) {
                continue;
            }

            PawnTurnData turnData = new(attacker);
            if (_combatEvent.HasBuff(attacker, Defs.Items.PumpinJuice)) {
                turnData.TotalSequencePoints += 8;
                turnData.AvailableSequencePoints = turnData.TotalSequencePoints;
            }

            _combatEvent.LogMessage(
                $"\n\\c[{CombatSequence.TextColorPawn}]{attacker.Label}'s \\c[#b3b3b3]Turn \\c[#b3b3b3]SP (\\c[#00e6ff]{turnData.AvailableSequencePoints}\\c[#b3b3b3])"
            );
            if (_combatEvent.HasBuff(attacker, Defs.Items.PumpinJuice)) {
                _combatEvent.LogMessage(
                    $"    \\c[{CombatSequence.TextColorPawn}]{attacker.Label} \\c[{CombatSequence.TextColorBlue}]feels the pump!"
                );
            }

            PawnTurnData.Add(attacker, turnData);
            if (turnData.AvailableSequencePoints > 0) {
                int circuitBreaker = 0;
                Pawn? target = null;
                do {
                    circuitBreaker++;
                    if (circuitBreaker > 100) {
                        Log.Error($"Circuit breaking {GetType().Name}");

                        break;
                    }

                    if (target == null || target.IsDead) {
                        target = GetNewTarget(attacker)!;
                        // all targets are dead, end combat
                        if (_combatEvent.State == CombatState.CombatEnd) {
                            break;
                        }
                    }

                    //todo this is slow here :(
                    var tools = attacker.GetAvailableToolsFor(ToolCategory.Combat).ToList();
                    if (tools.Any() == false) {
                        _combatEvent.LogMessage($"    \\c[{CombatSequence.TextColorPawn}]{attacker.LabelShort} \\c[{CombatSequence.TextColorRed}]has no usable weapons!!!");
                        break;
                    }

                    List<CombatSequence> allSequences = attacker.GetPotentialCombatSequencesFor(tools, turnData.AvailableSequencePoints, target);
                    if (attacker.PawnType == PawnType.Player && _combatEvent.IsInteractive && attacker.Brain.CombatSettings.IsAutoCombatEnabledFor(target.Race) == false) {
                        yield return Core.StartCoroutine(SetTurnInteractive());
                        if (_combatEvent.State is CombatState.TurnEnd or CombatState.CombatEnd) {
                            break;
                        }
                    }

                    UsePotionsIfNecessary(attacker, target, turnData);
                    if (_combatEvent.ShouldAttemptRetreat(this, attacker)) {
                        turnData.AvailableSequencePoints -= turnData.AvailableSequencePoints;
                        _combatEvent.AttemptRetreat();
                        //end turn
                        break;
                    }

                    var usableSequences = allSequences.Where(s => s.TotalSequencePoints <= turnData.AvailableSequencePoints).ToList();
                    if (!usableSequences.Any()) {
                        //End turn when there is no points left

                        break;
                    }

                    CombatSequence sequence = usableSequences.RandomElementByWeight(combatSequence => {
                        return combatSequence.Steps.Sum(step => step.Damages.TotalRawDamage);
                        //todo health conditions
                        //float conditionMultiplier = combatSequence.Steps.Any(step => step.Damages.InflictsConditions) ? 100 : 1;
                        //return totalRawDamage * conditionMultiplier;
                    })!;
                    turnData.AvailableSequencePoints -= sequence.TotalSequencePoints;
                    IEnumerator handler = ExecuteSequence(sequence, turnData);
                    while (handler.MoveNext()) {
                        yield return handler.Current;
                    }
                } while (_combatEvent.State != CombatState.CombatEnd);

                if (_combatEvent.State == CombatState.CombatEnd) {
                    break;
                }
            }

            if (turnData.AvailableSequencePoints > 0) {
                Log.Debug($"CombatEvent -- Pawn: {attacker.Label} is exiting without exhausting all sequence points, this is a bug");
            }

            yield return Coroutine.WaitForSeconds(0.25f);

        }

        foreach (Pawn pawn in Pawns) {
            if (pawn.IsDead) {
                _combatEvent.RemoveBuffsFor(pawn);
                continue;
            }

            float currentBlood = pawn.Body.BloodAmount;
            pawn.Tick();
            int bloodLost = (int) (currentBlood - pawn.Body.BloodAmount);
            if (pawn.IsDead && pawn.Body.BloodLevel <= 0) {
                _combatEvent.LogMessage($"    \\c[{CombatSequence.TextColorPawn}]{pawn.LabelShort} \\c[{CombatSequence.TextColorRed}]died from blood loss");
            }

            if (bloodLost > 0) {
                _combatEvent.LogMessage($"\\c[{CombatSequence.TextColorPawn}]{pawn.LabelShort} is losing blood \\c[{CombatSequence.TextColorRed}]-{bloodLost}");
            }
        }

        for (int index = _combatEvent.Buffs.Count - 1; index >= 0; index--) {
            CombatBuff buff = _combatEvent.Buffs[index];
            buff.Duration--;
            if (buff.Duration <= 0) {
                _combatEvent.RemoveBuff(buff);
                //if (buff.Def == PUMP)) {
                _combatEvent.LogMessage($"\\c[{CombatSequence.TextColorPawn}]{buff.Pawn.LabelShort} feels heavy from pump drain");
                //_combatEvent.ActivateBuff(Heavy, );
                //}
            }
        }

        _combatEvent.LogMessage("\\c[white]Turn is over\n");
        yield return Coroutine.WaitForSeconds(0.5f);
    }

    private Pawn GetNewTarget(Pawn attacker) {
        Pawn target = Pawns.InRandomOrder().First(target => target!.PawnType != attacker.PawnType && target.IsDead == false);
        return target;
    }

    private void UsePotionsIfNecessary(Pawn pawn, Pawn target, PawnTurnData turnData) {
        if (turnData.AvailableSequencePoints < 1) {
            return;
        }

        if (_combatEvent.DeQueuedPotionFor(pawn) is { } potion) {
            if (potion.Def == Defs.Items.JarOfBlood) {
                UseBloodPotion(potion, pawn, turnData);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.AcidFlask) {
                UseAcidFlask(potion, target, turnData);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.PumpinJuice) {
                UsePumpinJuice(potion, pawn, turnData);
                pawn.Equipment.UnEquip(potion);
            }

            potion.Destroy();

            if (turnData.AvailableSequencePoints < 1) {
                return;
            }
        }

        if (pawn.Body.BloodLevel < .3f && pawn.Equipment.PotionByDef(Defs.Items.JarOfBlood) is { } p) {
            UseBloodPotion(p, pawn, turnData);
            pawn.Equipment.UnEquip(p);
        }
    }

    private void UsePumpinJuice(Item potion, Pawn target, PawnTurnData turnData) {
        _combatEvent.ActivateBuff(potion, target, 2);
        _combatEvent.LogMessage(
            $"    \\c[{CombatSequence.TextColorYellow}]Sipped the \\c[{CombatSequence.TextColorItem}]{potion.Label}"
        );
        Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
            Text = $"{target.Label} is absorbing the Pumpin Juice",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.GreenYellow
        });
    }

    private void UseAcidFlask(Item potion, Pawn target, PawnTurnData turnData) {
        turnData.AvailableSequencePoints -= 1;
        foreach (BodyPart eye in target.Body.AllExternalParts.Where(part => part.Type == BodyPartType.Eye).InRandomOrder()) {
            if (Core.Random.Chance(1)) {
                eye.HitPoints = 0;
                string eyeText = $"{eye.Socket?.Label.Split(" ")[0]} {eye.Type}";
                _combatEvent.LogMessage(
                    $"    \\c[{CombatSequence.TextColorYellow}]Burned out \\c[{CombatSequence.TextColorPawn}]{target.LabelShort}'s \\c[{CombatSequence.TextColorBodyPart}]{eyeText} \\c[{CombatSequence.TextColorDefault}]with \\c[{CombatSequence.TextColorItem}]{potion.Label}"
                );

                if (Core.Random.Chance(.75f)) {
                    break;
                }
            }
        }

        Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
            Text = $"{target.Label} has been spiced with acid",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.YellowGreen
        });
    }

    private void UseBloodPotion(Item potion, Pawn pawn, PawnTurnData turnData) {
        float amount = 3000; //potion.GetStatValue(Defs.Stats.HealingValue);
        pawn.Body.BloodAmount += amount;
        turnData.AvailableSequencePoints -= 1;
        _combatEvent.LogMessage(
            $"    \\c[{CombatSequence.TextColorYellow}]Sipped a \\c[{CombatSequence.TextColorItem}]{potion.Label} \\c[{CombatSequence.TextColorDefault}]for \\c[{CombatSequence.TextColorGreen}]{amount} \\c[{CombatSequence.TextColorDefault}]blood"
        );
        if (pawn.PawnType == PawnType.Player) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Text = "Sipped a Jar of Blood. Blood is good for battle, bad for the mind",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 8,
                Color = Color.Red
            });
        }
    }

    private IEnumerator SetTurnInteractive() {
        _combatEvent.State = CombatState.TurnInteractive;
        while (_combatEvent.State == CombatState.TurnInteractive) {
            yield return Coroutine.WaitForSeconds(0.25f);
        }
    }

    public IEnumerator ExecuteSequence(CombatSequence sequence, PawnTurnData pawnTurnData, Action? finishAction = null) {
        Sequences.Add(sequence);
        IEnumerator handler = sequence.Execute(_combatEvent);
        while (handler.MoveNext()) {
            /*if (sequence.Source.IsDestroyed || sequence.Target.IsDestroyed) {
                break;
            }*/

            yield return handler.Current;
        }

        var target = sequence.Target;
        if (target.IsDead) {
            _combatEvent.CombatRecord.Pawns.First(p => p.Id == target.Id).WasKilled = true;
            if (_combatEvent.IsPartyDead(target)) {
                _combatEvent.State = CombatState.CombatEnd;
            }

            yield return Coroutine.WaitForSeconds(2);
        }

        if (_combatEvent.State == CombatState.TurnInteractive && pawnTurnData.AvailableSequencePoints <= 0) {
            _combatEvent.LogMessage("Sequence Points Exhausted\n");
            yield return Coroutine.WaitForSeconds(.25f);
            _combatEvent.State = CombatState.Turn;
        }

        finishAction?.Invoke();
    }

    public void RegisterPawns(List<Pawn> pawns) {
        foreach (Pawn pawn in pawns) {
            if (pawn.IsDead) {
                continue;
            }

            Pawns.Add(pawn);
        }
    }
}