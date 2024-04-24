using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public class CombatHandler
{
    public readonly Encounter Encounter;
    private string? _deathMessage;
    public Pawn Player { get; set; }
    public Pawn Enemy { get; set; }

    public CombatHandler(Encounter encounter)
    {
        Encounter = encounter;
        Player = encounter.PlayerPawns.First();
        Enemy = encounter.EnemyPawns.First();
    }

    public void DoFighting(int ticks)
    {
        Attack(Player, Enemy);
        if (Enemy.IsDead)
        {
            return;
        }

        Attack(Enemy, Player);
    }

    private void Attack(Pawn attacker, Pawn victim)
    {
        if (attacker.TicksToAttack > 0)
        {
            return;
        }

        attacker.ResetAttackCoolDown();
        UsePotionsIfNecessary(attacker, victim); //todo move to different tick rate

        float chanceToHit = attacker.ChanceToHit(victim);
        attacker.Body.ConsumeEnergy(0.002f);
        if (Core.Random.NextSingle() < chanceToHit)
        {
            var damageOptions = attacker.Equipment.UsableWeapons
                .Select(t => CombatHelpers.CalculateDamages(attacker, t))
                .OrderByDescending(t => t.TotalRawDamage)
                .ToList();
            if (damageOptions.Any() == false)
            {
                Encounter.LogMessage($"  \\c[{TC.Purple2}] has no usable tools");
                return;
            }

            var damageRequest = damageOptions.First();
            var damageResponse = victim.TakeDamage(damageRequest);
            if (damageResponse.Dodged)
            {
                Encounter.LogMessage($"  \\c[{TC.Victim}]{victim.LabelShort} \\c[{TC.Blue}] dodged attack");
                return;
            }

            foreach (DamagedPartRecord damage in damageResponse.Damages.SelectMany(r => r.BodyParts))
            {
                if (damage.BodyPart.IsExternal && damage.WasSevered)
                {
                    Encounter.SeveredLimbs.Add(damage.BodyPart);
                }
            }

            foreach (DamageRecord damageResult in damageResponse.Damages)
            {
                Encounter.LogMessage(
                    $"\\c[{TC.Attacker}]{attacker.ToString().PadRight(20)}\\c[{TC.Default}]hit \\c[{TC.Victim}]{victim.LabelShort}'s \\c[{TC.BodyPart}]{damageResult.BodyPartHit.Label} " +
                    $"\\c[{TC.Default}]with \\c[{TC.Item}]{damageRequest.Tool} (\\c[{TC.Golden}]{damageRequest.ToolManeuver.Label}) " +
                    $"\\c[{TC.Default}] for \\c[{TC.Red}]{damageResult.ActualAmount} \\c[{TC.Default}] \\c[{TC.Golden}]{damageResult.DamageType}\\c[{TC.Default}] damage, " +
                    $"blocked \\c[#00e6ff]{damageResult.AmountBlocked}"
                );

                foreach (DestroyedItemRecord itemRecord in damageResult.DestroyedEquipment)
                {
                    Encounter.LogMessage($"  \\c[{TC.Equipment}]{itemRecord.Def.Label} \\c[{TC.Red}]destroyed");
                }

                foreach (DamagedPartRecord partRecord in damageResult.BodyParts)
                {
                    foreach (BodyPartModifierDef modifer in partRecord.AppliedModifiers)
                    {
                        Encounter.LogMessage(
                            $"  \\c[{TC.BodyPart}]{partRecord.PartType} \\c[{TC.Default}]afflicted with \\c[{TC.Yellow}]{modifer}");
                    }

                    if (partRecord is { WasDestroyed: true, IsVital: false })
                    {
                        Encounter.LogMessage($"  \\c[{TC.BodyPart}]{partRecord.PartType} \\c[{TC.Red}]destroyed");
                    }

                    if (partRecord is { WasDestroyed: true, IsVital: true })
                    {
                        Encounter.LogMessage($"  \\c[{TC.Red}]VITAL part \\c[{TC.BodyPart}]{partRecord.PartType} \\c[{TC.Red}]destroyed");
                    }

                    if (partRecord.BodyPart.IsExternal && partRecord.WasSevered)
                    {
                        Encounter.LogMessage($"  \\c[{TC.BodyPart}]{partRecord.PartType} \\c[{TC.Red}]SEVERED");
                    }
                }
            }

            //todo
            /*if (damageResponse.HealthConditions != null) {
                foreach (HealthConditionDef condition in damageResponse.HealthConditions) {
                    CombatEvent.LogMessage($"        \\c[#b3b3b3]Inflicted \\c[{TextColorPawn}]{Target.LabelShort} \\c[#b3b3b3]with \\c[#acc700]{condition.Label}");
                }
            }*/
        }
        else
        {
            Encounter.LogMessage(
                $"\\c[{TC.Attacker}]{attacker.ToString().PadRight(20)}\\c[{TC.Purple}]missed \\c[{TC.Victim}]{victim.LabelShort} \\c[{TC.Default}]ChanceToHit = \\c[{TC.BrightBlue}]{chanceToHit:P1}");
        }
    }

    public void UsePotionsIfNecessary(Pawn pawn, Pawn target)
    {
        if (Encounter.DeQueuedPotionFor(pawn) is { } potion)
        {
            if (potion.Def == Defs.Items.JarOfBlood)
            {
                UseBloodPotion(potion, pawn);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.AcidFlask)
            {
                UseAcidFlask(potion, target);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.PumpinJuice)
            {
                UsePumpinJuice(potion, pawn);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.TheDreamingPowder)
            {
                UseTheDreamingPowder(potion, target);
                pawn.Equipment.UnEquip(potion);
            }

            potion.Destroy();
            return;
        }

        if (pawn.Body.BloodPercent < .3f && pawn.Equipment.PotionByDef(Defs.Items.JarOfBlood) is { } p)
        {
            UseBloodPotion(p, pawn);
            pawn.Equipment.UnEquip(p);
        }
    }

    private void UsePumpinJuice(Item potion, Pawn target)
    {
        Encounter.ActivateBuff(potion, target, 2);
        Encounter.LogMessage(
            $"    \\c[{TC.Yellow}]Sipped the \\c[{TC.Item}]{potion.Label}"
        );

        Encounter.Zone!.ZoneMessage(new ScreenMessageData
        {
            Text = $"{target.Label} is absorbing the Pumpin Juice",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.GreenYellow
        });
    }

    private void UseTheDreamingPowder(Item potion, Pawn target)
    {
        Encounter.ActivateBuff(potion, target, Core.Random.Next(3, 6));
        Encounter.LogMessage(
            $"    \\c[{TC.Purple}]Released \\c[{TC.Item}]{potion.Label}"
        );
        Encounter.Zone!.ZoneMessage(new ScreenMessageData
        {
            Text = $"{target.Label} has been transfixed",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.MediumPurple
        });
    }

    private void UseAcidFlask(Item potion, Pawn target)
    {
        foreach (BodyPart eye in target.Body.AllExternalParts.Where(part => part.Type == BodyPartType.Eye).InRandomOrder())
        {
            if (Core.Random.Chance(1))
            {
                eye.HitPoints = 0;
                string eyeText = $"{eye.Socket?.Label.Split(" ")[0]} {eye.Type}";
                Encounter.LogMessage(
                    $"    \\c[{TC.Yellow}]Burned out \\c[{TC.Victim}]{target.LabelShort}'s \\c[{TC.BodyPart}]{eyeText} \\c[{TC.Default}]with \\c[{TC.Item}]{potion.Label}"
                );

                if (Core.Random.Chance(.75f))
                {
                    break;
                }
            }
        }

        Encounter.Zone!.ZoneMessage(new ScreenMessageData
        {
            Text = $"{target.Label} has been spiced with acid",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.YellowGreen
        });
    }

    private void UseBloodPotion(Item potion, Pawn pawn)
    {
        float amount = potion.GetStatValue(Defs.Stats.HealingValue);
        pawn.Body.BloodAmount += amount;
        Encounter.LogMessage(
            $"    \\c[{TC.Yellow}]Sipped a \\c[{TC.Item}]{potion.Label} \\c[{TC.Default}]for \\c[{TC.Green}]{amount} \\c[{TC.Default}]blood"
        );
        if (pawn.PawnType == PawnType.Player)
        {
            Encounter.Zone!.ZoneMessage(new ScreenMessageData
            {
                Text = "Sipped a Jar of Blood. Blood is good for battle, bad for the mind",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 8,
                Color = Color.Red
            });
        }
    }
}