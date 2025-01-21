using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Combat;

public class CombatHandler
{
    private readonly Encounter Encounter;
    private readonly Dictionary<Pawn, Item> _queuedItems = new();
    private readonly Dictionary<Pawn, List<Item>> _activeTrinkets = new();
    private string? _deathMessage;
    public Pawn Player { get; set; }
    public Pawn Enemy { get; set; }

    public CombatHandler(Encounter encounter)
    {
        Encounter = encounter;
        Player = encounter.PlayerPawns.First();
        Enemy = encounter.EnemyPawns.First();

        Player.DamageTaken += OnDamageTaken;
        Player.Died += OnDeath;
        _activeTrinkets[Player] = [];

        Enemy.DamageTaken += OnDamageTaken;
        Enemy.Died += OnDeath;
        _activeTrinkets[Enemy] = [];
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        Encounter.LogMessage($"/f[default, 32]/c[{TC.Victim}]{deathEvent.Pawn.LabelShort} /cddied from /c[{TC.Red}]{deathEvent.Record.CauseOfDeath}\n");
        Encounter.Zone!.Alert(
            new ScreenMessageData
            {
                Text = $"{deathEvent.Pawn.LabelShort}s has died from {deathEvent.Record.CauseOfDeath}",
                Font = BaseContent.Fonts.Default.Medium,
                Duration = 8,
                Color = Color.Red
            });
        Core.Context.DeathRecords.RecordDeath(new DeathRecord
        {
            CauseOfDeath = deathEvent.Record.CauseOfDeath,
            Ticks = Encounter.Ticks,
            Biome = Encounter.Zone.BiomeDef,
            PawnName = deathEvent.Pawn.LabelShort + (Encounter.AtBoss ? " (Boss)" : "")
        });
    }

    private void OnDamageTaken(Pawn victim, DamageRequest request, DamageResponse response)
    {
        var attacker = request.Source;

        // Record players severed body parts in order to take its equipment
        if (victim.PawnType == PawnType.Player)
        {
            foreach (var damage in response.Damages.SelectMany(r => r.BodyParts))
            {
                if (damage.BodyPart.IsExternal && damage.WasSevered)
                {
                    Encounter.SeveredLimbs.Add(damage.BodyPart);
                }
            }
        }

        foreach (var damage in response.TrinketDamages)
        {
            LogDamage(victim, attacker, damage, TC.Purple2);
        }

        if (response.Missed)
        {
            Encounter.LogMessage($"/c[{TC.Attacker}]{attacker.LabelShort} /c[{TC.Blue}] missed /c[{TC.Victim}]{victim.LabelShort}.");
        }
        else if (response.Dodged)
        {
            Encounter.LogMessage($"/c[{TC.Victim}]{victim.LabelShort} /c[{TC.Blue}] dodged attack");
        }
        else
        {
            foreach (var damage in response.Damages)
            {
                LogDamage(victim, attacker, damage, TC.Item);
            }
        }
    }

    private void LogDamage(Pawn victim, Pawn attacker, DamageRecord damage, string weaponColor)
    {
        Encounter.LogMessage(
            $"/c[{TC.Attacker}]{attacker,-20}/c[{TC.Default}]hit /c[{TC.Victim}]{victim.LabelShort}'s /c[{TC.BodyPart}]{damage.BodyPartHit.Label}" +
            $"/c[{TC.Default}] with /c[{weaponColor}]{damage.WeaponLabel} /c[{TC.Golden}]({damage.WeaponManeuverLabel})" +
            $"/c[{TC.Default}] for /c[{TC.Red}]{damage.ActualAmount:N0} /c[{TC.Golden}]{damage.DamageType}/c[{TC.Default}] damage," +
            $" blocked /c[#00e6ff]{damage.AmountBlocked}"
        );

        foreach (var itemRecord in damage.DestroyedEquipment)
        {
            Encounter.LogMessage($"  /c[{TC.Equipment}]{itemRecord.Def.Label} /c[{TC.Red}]destroyed");
        }

        foreach (var partRecord in damage.BodyParts)
        {
            foreach (var modifier in partRecord.AppliedModifiers)
            {
                Encounter.LogMessage(
                    $"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Default}]afflicted with /c[{TC.Yellow}]{modifier}");
            }

            if (partRecord is { WasDestroyed: true, IsVital: false })
            {
                Encounter.LogMessage($"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]destroyed");
            }

            if (partRecord is { WasDestroyed: true, IsVital: true })
            {
                Encounter.LogMessage($"  /c[{TC.Red}]Vital part /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]destroyed");
            }

            if (partRecord.BodyPart.IsExternal && partRecord.WasSevered)
            {
                Encounter.LogMessage($"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]SEVERED");
                Encounter.Zone!.Alert(
                    new ScreenMessageData
                    {
                        Text = $"{victim.LabelShort}s {partRecord.PartType} has been severed",
                        Font = BaseContent.Fonts.Default.Medium,
                        Duration = 8,
                        Color = Color.Red
                    });
            }
        }

        foreach (var affliction in damage.SourceAfflictions)
        {
            Encounter.LogMessage(
                $"/c[{TC.Purple2}]{attacker}/c[{TC.Default}]'s /c[{TC.BodyPart}]{affliction.BodyPart.Label} " +
                $"/c[{TC.Default}]has been (/c[{TC.GreenYellow}]{affliction.Label}) "
            );
        }
    }

    public void DoFighting()
    {
        Attack(Player, Enemy);

        if (Enemy.IsDead)
        {
            return;
        }

        Attack(Enemy, Player);

        UseBloodPotionIfNeeded(Player);
        UseBloodPotionIfNeeded(Enemy);

        AutoQueueEnemyPotions();

        Enemy.Tick();
    }

    private void UseBloodPotionIfNeeded(Pawn pawn)
    {
        if (pawn.Body.BloodPercent < .1f && pawn.Equipment.PotionByDef(Defs.Items.JarOfBlood) is { } p)
        {
            UseBloodPotion(p, pawn);
            pawn.Equipment.UnEquip(p);
        }
    }

    private void AutoQueueEnemyPotions()
    {
        if (ItemQueuedFor(Enemy) != null || !Core.Random.Chance(0.01f)) return;

        var usablePotions = new List<ItemDef> { Defs.Items.AcidFlask, Defs.Items.PurpleJuice };
        foreach (var potionDef in usablePotions)
        {
            if (Enemy.Equipment.PotionByDef(potionDef) is { } potion)
            {
                QueueItemForPawn(potion, Enemy);
            }
        }
    }

    private void Attack(Pawn attacker, Pawn victim)
    {
        if (attacker.TicksToAttack > 0)
        {
            return;
        }

        HandleQueuedItem(attacker, victim);

        attacker.ResetAttackCoolDown();

        var energyUsedForAttack = 0.25f;
        attacker.Body.ConsumeEnergy(energyUsedForAttack);
        var damageOptions = attacker.Equipment.UsableWeapons
            .Select(t => CombatHelpers.CalculateDamages(attacker, t))
            .OrderByDescending(t => t.TotalRawDamage)
            .ToList();

        if (damageOptions.Any() == false)
        {
            Encounter.LogMessage($"/c[{TC.Attacker}]{attacker.LabelShort} has no usable weapons");
            return;
        }

        var damageRequest = damageOptions.First();

        damageRequest.AddTrinketResults(HandleActivatedTrinkets(attacker, victim));

        victim.TakeDamage(damageRequest);
    }

    private List<DamageRecord>? HandleActivatedTrinkets(Pawn attacker, Pawn victim)
    {
        var results = new List<DamageRecord>();
        for (var index = _activeTrinkets[attacker].Count - 1; index >= 0; index--)
        {
            var trinket = _activeTrinkets[attacker][index];
            if (trinket.TrinketHandler!.HandleCombat(attacker, victim) is { } damageRecord)
            {
                _activeTrinkets[attacker].Remove(trinket);
                results.Add(damageRecord);
            }
        }

        return results.Count != 0 ? results : null;
    }

    private void HandleQueuedItem(Pawn pawn, Pawn target)
    {
        if (DeQueuedItemForPawn(pawn) is not { } item) return;

        if (item is { ItemDef: { ItemType: ItemType.Potion } } potion)
        {
            if (potion.Def == Defs.Items.JarOfBlood)
            {
                UseBloodPotion(potion, pawn);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.AcidFlask)
            {
                UseAcidFlask(potion, pawn, target);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.PurpleJuice)
            {
                UsePurpleJuice(potion, pawn);
                pawn.Equipment.UnEquip(potion);
            }

            if (potion.Def == Defs.Items.TheDreamingPowder)
            {
                UseTheDreamingPowder(potion, target);
                pawn.Equipment.UnEquip(potion);
            }

            potion.Destroy();
        }
    }

    public void QueueItemForPawn(Item potion, Pawn pawn)
    {
        _queuedItems[pawn] = potion;
    }

    public Item? DeQueuedItemForPawn(Pawn pawn)
    {
        if (_queuedItems.ContainsKey(pawn))
        {
            var potion = _queuedItems[pawn];
            _queuedItems.Remove(pawn);
            return potion;
        }

        return null;
    }

    public Item? ItemQueuedFor(Pawn pawn)
    {
        return _queuedItems.ContainsKey(pawn) ? _queuedItems[pawn] : null;
    }

    private void UsePurpleJuice(Item potion, Pawn target)
    {
        var duration = (int)potion.GetStatValue(Defs.Stats.PotionDuration);
        target.Body.AllParts.ForEach(p => p.TryAddModifier(
            BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.PurpleRegeneration, duration)
        ));
        target.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.FeelingThePurple,
            TicksLeft = duration
        });
        Encounter.LogMessage(
            $"/c[{TC.Attacker}]{target.LabelShort} /c[{TC.Yellow}]sipped the /c[{TC.Item}]{potion.Label}"
        );

        Encounter.Zone!.Alert(new ScreenMessageData
        {
            Text = $"{target.Label} is absorbing the Purple Juice",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.GreenYellow
        });
    }

    private void UseTheDreamingPowder(Item potion, Pawn target)
    {
        //Encounter.ActivateBuff(potion, target, Core.Random.Next(3, 6));
        Encounter.LogMessage(
            $"/c[{TC.Attacker}]{target.LabelShort} /c[{TC.Purple}]Released /c[{TC.Item}]{potion.Label}"
        );
        Encounter.Zone!.Alert(new ScreenMessageData
        {
            Text = $"{target.Label} has been transfixed",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.MediumPurple
        });
    }

    private void UseAcidFlask(Item potion, Pawn attacker, Pawn target)
    {
        foreach (var eye in target.Body.AllExternalParts.Where(part => part.Type == BodyPartType.Eye).InRandomOrder())
        {
            if (Core.Random.Chance(1))
            {
                eye.HitPoints = 0;
                var eyeText = $"{eye.Socket?.Label.Split(" ")[0]} {eye.Type}";
                Encounter.LogMessage(
                    $"/c[{TC.Attacker}]{attacker.LabelShort} /c[{TC.Yellow}]burned out /c[{TC.Victim}]{target.LabelShort}'s /c[{TC.BodyPart}]{eyeText} /c[{TC.Default}]with /c[{TC.Item}]{potion.Label}"
                );
                break;
            }
        }

        Encounter.Zone!.Alert(new ScreenMessageData
        {
            Text = $"{target.Label} has been spiced with acid",
            Font = BaseContent.Fonts.Default.Large,
            Duration = 8,
            Color = Color.YellowGreen
        });
    }

    private void UseBloodPotion(Item potion, Pawn pawn)
    {
        pawn.Body.BloodAmount = pawn.Body.MaxBlood;
        Encounter.LogMessage(
            $"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]Sipped a /c[{TC.Item}]{potion.Label}"
        );
        if (pawn.PawnType == PawnType.Player)
        {
            Encounter.Zone!.Alert(new ScreenMessageData
            {
                Text = "Sipped a Jar of Blood. Blood is good for battle, bad for the mind",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 8,
                Color = Color.DarkRed
            });
        }
    }

    public void ActivateTrinketForPawn(Item trinket, Pawn pawn)
    {
        if (trinket.TrinketHandler == null)
        {
            return;
        }

        if (trinket.TrinketHandler.IsActive)
        {
            return;
        }

        if (trinket.TrinketHandler.Activate())
        {
            _activeTrinkets[pawn].Add(trinket);
        }
    }

    public void DeActivateTrinketForPawn(Item trinket, Pawn pawn)
    {
        if (trinket.TrinketHandler?.IsActive == false)
        {
            return;
        }

        trinket.TrinketHandler!.DeActivate();
        _activeTrinkets[pawn].Remove(trinket);
    }
}