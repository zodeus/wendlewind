using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Sim.Achievements.Handlers;

namespace Grafted.Sim.Combat;

public enum CombatEventType
{
    Damage,
    Block,
    Dodge,
    Miss,
    Heal,
    Buff,
    Debuff,
    Death,
    StatusEffect
}

public class CombatEvent(Pawn victim, CombatEventType damage, string s, BodyPart? bodyPart = null, bool isCritical = false)
{
    public string Text { get; set; } = s;
    public Pawn Target { get; set; } = victim;
    public CombatEventType Type { get; set; } = damage;
    public BodyPart? BodyPart { get; set; } = bodyPart;
    public bool IsCritical { get; set; } = isCritical;
}

public class CombatHandler : IDisposable
{
    private readonly Encounter _encounter;
    private readonly Dictionary<Pawn, Item> _queuedItems = new();
    
    public EntityContainer Loot = new();
    public readonly List<BodyPart> SeveredLimbs = [];
    public event Action<CombatEvent>? EventOccured;
    public double TotalDirectPlayerDamage { get; private set; }
    public string? CauseOfDeath { get; private set; }
    public string? KillingWeapon { get; private set; }
    public string? KillingManeuver { get; private set; }

    public Pawn Player { get; set; }
    public Pawn Enemy { get; set; }
    public event Action<string>? CombatLogMessageAdded;

    public CombatHandler(Encounter encounter)
    {
        _encounter = encounter;
        Player = encounter.PlayerPawns.First();
        Enemy = encounter.EnemyPawns.First();

        Player.DamageTaken += OnDamageTaken;
        Player.Died += OnDeath;

        Enemy.DamageTaken += OnDamageTaken;
        Enemy.Died += OnDeath;

        Player.Body.Handler.OnBloodLost += Core.Context.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost += Core.Context.Achievements.OnBloodLost;
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        CauseOfDeath = deathEvent.Record.CauseOfDeath;
        KillingWeapon = deathEvent.Record.KillingWeapon;
        KillingManeuver = deathEvent.Record.KillingManeuver;
        LogMessage(
            $"/f[default, 32]/c[{TC.Victim}]{deathEvent.Pawn.LabelShort} /cddied from /c[{TC.Red}]{deathEvent.Record.CauseOfDeath}\n"
        );
        _encounter.Zone.Alert(
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
            KillingWeapon = deathEvent.Record.KillingWeapon,
            KillingManeuver = deathEvent.Record.KillingManeuver,
            TotalDamageDealt = TotalDirectPlayerDamage,
            Ticks = _encounter.Ticks,
            ZoneDef = _encounter.Zone.ZoneDef,
            PawnName = deathEvent.Pawn.LabelShort + (_encounter.AtBoss ? " (Boss)" : "")
        });

        // Track achievement: enemy killed
        if (deathEvent.Pawn.PawnType == PawnType.Enemy)
        {
            Core.Context.Achievements.OnEnemyKilled(deathEvent.Pawn);
        }

        EndCombat();
    }

    private void OnDamageTaken(Pawn victim, DamageRequest request, DamageResponse response)
    {
        var logs = new List<string>();
        var attacker = request.Source;

        // Record players severed body parts in order to take its equipment
        if (victim.PawnType == PawnType.Player)
        {
            foreach (var damage in response.Damages.SelectMany(r => r.BodyParts))
            {
                if (damage.BodyPart.IsExternal && damage.WasSevered)
                {
                    SeveredLimbs.Add(damage.BodyPart);
                }
            }
        }

        if (victim.PawnType == PawnType.Enemy)
        {
            TotalDirectPlayerDamage += response.TotalDamageTaken;
            Core.Context.Achievements.OnEnemyDamaged(Player, Enemy, request, response);
        }

        foreach (var damage in response.TrinketDamages)
        {
            logs.AddRange(LogDamage(victim, attacker, damage, TC.Purple2));
        }

        if (response.Missed)
        {
            EventOccured?.Invoke(new CombatEvent(attacker, CombatEventType.Miss, $"missed"));
            logs.Add($"/c[{TC.Attacker}]{attacker.LabelShort} /c[{TC.Blue}]missed /c[{TC.Victim}]{victim.LabelShort}.");
        }
        else if (response.Dodged)
        {
            EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Dodge, $"dodged"));
            logs.Add($"/c[{TC.Victim}]{victim.LabelShort} /c[{TC.Blue}]dodged attack");
        }
        else
        {
            foreach (var damage in response.Damages)
            {
                logs.AddRange(LogDamage(victim, attacker, damage, TC.Item));
            }
        }

        logs.Reverse();
        foreach (var log in logs)
        {
            LogMessage(log);
        }
    }

    private IEnumerable<string> LogDamage(Pawn victim, Pawn attacker, DamageRecord damage, string weaponColor)
    {
        yield return $"/c[{TC.Attacker}]{attacker} /c[{TC.Default}]hit /c[{TC.Victim}]{victim.LabelShort}'s /c[{TC.BodyPart}]{damage.BodyPartHit.Label}" +
                     $"/c[{TC.Default}] with /c[{weaponColor}]{damage.WeaponLabel} /c[{TC.Golden}]({damage.WeaponManeuverLabel})" +
                     $"/c[{TC.Default}] for /c[{TC.Red}]{damage.ActualAmount:N0} /c[{TC.Golden}]{damage.DamageType}/c[{TC.Default}] damage," +
                     $" blocked /c[#00e6ff]{damage.AmountBlocked}";

        if (damage.ActualAmount > 0)
        {
            EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Damage, $"{damage.ActualAmount:N0}", damage.BodyPartHit, damage.IsCritical));
        }

        if (damage.AmountBlocked > 0)
        {
            EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Block, $"{damage.AmountBlocked:N0}", damage.BodyPartHit));
        }


        foreach (var itemRecord in damage.DestroyedEquipment)
        {
            yield return $"  /c[{TC.Equipment}]{itemRecord.Def.Label} /c[{TC.Red}]destroyed";
        }

        foreach (var partRecord in damage.BodyParts)
        {
            foreach (var modifier in partRecord.AppliedModifiers)
            {
                EventOccured?.Invoke(new CombatEvent(victim, modifier.Type == BodyPartModifierType.Buff ? CombatEventType.Buff : CombatEventType.Debuff, modifier.Label, partRecord.BodyPart));
                yield return $"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Default}]afflicted with /c[{TC.Yellow}]{modifier}";
            }

            if (partRecord is { WasDestroyed: true } && (partRecord.IsVital || partRecord.BodyPart.IsExternal))
            {
                // EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Damage, $"{partRecord.PartType} destroyed"));
            }

            if (partRecord is { WasDestroyed: true, IsVital: false })
            {
                //EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Damage, $"{partRecord.PartType} destroyed"));
                yield return $"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]destroyed";
            }

            if (partRecord is { WasDestroyed: true, IsVital: true })
            {
                //EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Damage, $"{partRecord.PartType} destroyed"));
                yield return $"  /c[{TC.Red}]Vital part /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]destroyed";
            }

            if (partRecord.BodyPart.IsExternal && partRecord.WasSevered)
            {
                EventOccured?.Invoke(new CombatEvent(victim, CombatEventType.Damage, $"{partRecord.PartType} severed", partRecord.BodyPart));
                yield return $"  /c[{TC.BodyPart}]{partRecord.PartType} /c[{TC.Red}]SEVERED";
                _encounter.Zone.Alert(
                    new ScreenMessageData
                    {
                        Text = $"{victim.LabelShort}s {partRecord.PartType} has been severed",
                        Font = BaseContent.Fonts.Default.Medium,
                        Duration = 8,
                        Color = Color.Red
                    });
            }
        }

        foreach (var statusEffect in damage.DamageStatusEffects)
        {
            EventOccured?.Invoke(new CombatEvent(statusEffect.Pawn, CombatEventType.StatusEffect, statusEffect.EffectDef.Label));
            yield return $"/c[{TC.Purple2}]{statusEffect.Pawn}/c[{TC.Default}]'s " +
                         $"{statusEffect.Label}";
        }
    }

    public void Tick()
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
            Core.Context.Achievements.OnItemUsed(pawn, p);
            p.Destroy();
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

        const float energyUsedForAttack = 0.25f; //todo Move somewhere cool, you know... do something with this. Make it dynamic.
        attacker.Body.ConsumeEnergy(energyUsedForAttack);
        var damageOptions = attacker.Equipment.UsableWeapons
            .Where(w => w.UseInCombat)
            .Select(t => DamageRequest.Create(attacker, t))
            .ToList();

        if (damageOptions.Count == 0)
        {
            LogMessage($"/c[{TC.Attacker}]{attacker.LabelShort} has no usable weapons");
            return;
        }

        var damageRequest = damageOptions.RandomElement();
        damageRequest.TargetedPart = victim.Body.AllExternalParts.Where(p => p.IsDestroyed == false || p.AllInternalParts.Count != 0).RandomElementByWeight(part => part.HitWeight)!;
        victim.TakeDamage(damageRequest);
    }

    private void HandleQueuedItem(Pawn pawn, Pawn target)
    {
        if (DeQueuedItemForPawn(pawn) is not { } item) return;

        if (item is { ItemDef: { ItemType: ItemType.Potion } } potion)
        {
            // Track potion usage for achievements (player only)
            if (pawn.PawnType == PawnType.Player)
            {
                Core.Context.Achievements.OnItemUsed(pawn, potion);
            }

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
        LogMessage(
            $"/c[{TC.Attacker}]{target.LabelShort} /c[{TC.Yellow}]sipped the /c[{TC.Item}]{potion.Label}"
        );

        _encounter.Zone.Alert(new ScreenMessageData
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
        LogMessage(
            $"/c[{TC.Attacker}]{target.LabelShort} /c[{TC.Purple}]Released /c[{TC.Item}]{potion.Label}"
        );
        _encounter.Zone.Alert(new ScreenMessageData
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
                LogMessage(
                    $"/c[{TC.Attacker}]{attacker.LabelShort} /c[{TC.Yellow}]burned out /c[{TC.Victim}]{target.LabelShort}'s /c[{TC.BodyPart}]{eyeText} /c[{TC.Default}]with /c[{TC.Item}]{potion.Label}"
                );
                break;
            }
        }

        _encounter.Zone.Alert(new ScreenMessageData
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
        LogMessage(
            $"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]Sipped a /c[{TC.Item}]{potion.Label}"
        );
        if (pawn.PawnType == PawnType.Player)
        {
            _encounter.Zone.Alert(new ScreenMessageData
            {
                Text = "Sipped a Jar of Blood. Blood is good for battle, bad for the mind",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 8,
                Color = Color.DarkRed
            });
        }
    }

    private void EndCombat()
    {
        var playerIsAlive = !Player.IsDead;
        if (playerIsAlive)
        {
            CollectLoot();
            Player.Inventory.Trinkets.ForEach(t =>
            {
                t.TrinketHandler?.PostCombatAction(new PostCombatReport
                {
                    Player = Player,
                    Enemy = Enemy,
                    TotalDirectPlayerDamage = TotalDirectPlayerDamage
                });
                t.TrinketHandler?.Stop();
            });

            if (_encounter.Def.IsBoss)
            {
                _encounter.Zone.IsComplete = true;
            }
        }

        // Notify achievements of combat end
        Core.Context.Achievements.OnCombatEnd(new AchievementCombatEndContext
        {
            Player = Player,
            Enemy = Enemy,
            PlayerWon = playerIsAlive,
            TotalDamageDealt = TotalDirectPlayerDamage,
            CombatTicks = _encounter.Ticks,
            Zone = _encounter.Zone,
            CauseOfDeath = CauseOfDeath
        });

        LogMessage($"/f[default, 48]/c[{TC.Golden}]Battle is over\n");
        _encounter.State = EncounterState.Finished;
    }

    private void LogMessage(string message)
    {
        CombatLogMessageAdded?.Invoke(message);
    }

    private void CollectLoot()
    {
        for (var i = Enemy.Inventory.Count() - 1; i >= 0; i--)
        {
            var item = Enemy.Inventory[i];
            if (Core.Context.Player.HasTrinkets(item.ItemDef))
            {
                continue;
            }

            AddToLootContainer(item);
        }

        //CollectEquipment(Enemy);

        foreach (var part in SeveredLimbs)
        {
            TakePartEquipment(part);
        }

        foreach (var resource in _encounter.Zone.ZoneDef.Resources)
        {
            if (Core.Random.Chance(resource.ChanceToHarvest))
            {
                AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
            }
        }

        return;

        void TakePartEquipment(BodyPart part)
        {
            foreach (var (slot, item) in part.Equipment)
            {
                if (item == null || item.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn) continue;

                part.Equipment[slot] = null;
                AddToLootContainer(item);
            }

            foreach (var externalPart in part.ExternalParts)
            {
                TakePartEquipment(externalPart);
            }
        }
    }

    private void CollectEquipment(Pawn enemy)
    {
        const int chanceToLootEquipment = 1;
        foreach (var (bodyPart, slots) in enemy.Equipment.Slots)
        {
            foreach (var slot in slots.Where(slot => slot is not EquipmentSlotType.BuiltIn))
            {
                if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && Core.Random.Chance(chanceToLootEquipment))
                {
                    AddToLootContainer(item);
                }
            }
        }
    }

    private void AddToLootContainer(Item item)
    {
        Loot.TryAdd(item);
    }

    public void Dispose()
    {
        Player.DamageTaken -= OnDamageTaken;
        Player.Died -= OnDeath;
        Enemy.DamageTaken -= OnDamageTaken;
        Enemy.Died -= OnDeath;
        Player.Body.Handler.OnBloodLost -= Core.Context.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost -= Core.Context.Achievements.OnBloodLost;
    }
}

public class PostCombatReport
{
    public Pawn Player { get; set; } = null!;
    public Pawn Enemy { get; set; } = null!;
    public double TotalDirectPlayerDamage { get; set; }
}