using Wendlewind.Sim.Achievements.Handlers;

namespace Wendlewind.Sim.Combat;

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
    private readonly Random _rng;
    private readonly Dictionary<Pawn, Item> _queuedItems = new();
    public EntityContainer Loot = new();
    public List<ResourceCount> CollectedLoot { get; } = [];
    public readonly List<BodyPart> SeveredLimbs = [];
    public event Action<CombatEvent>? EventOccured;
    public double TotalDirectPlayerDamage { get; private set; }
    public string? CauseOfDeath { get; private set; }
    public string? KillingWeapon { get; private set; }
    public string? KillingManeuver { get; private set; }

    public Random Rng => _rng;
    public Pawn Player { get; set; }
    public Pawn Enemy { get; set; }
    public event Action<string>? CombatLogMessageAdded;

    public CombatHandler(Encounter encounter)
    {
        _encounter = encounter;
        _rng = GameContext.Current.Rng;
        Player = encounter.PlayerPawns.First();
        Enemy = encounter.EnemyPawns.First();

        Player.DamageTaken += OnDamageTaken;
        Player.Died += OnDeath;

        Enemy.DamageTaken += OnDamageTaken;
        Enemy.Died += OnDeath;

        Player.Body.Handler.OnBloodLost += GameContext.Current.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost += GameContext.Current.Achievements.OnBloodLost;
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
                Duration = 8,
                Color = Color.Red
            });
        GameContext.Current.DeathRecords.RecordDeath(new DeathRecord
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
            GameContext.Current.Achievements.OnEnemyKilled(deathEvent.Pawn);
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
            GameContext.Current.Achievements.OnEnemyDamaged(Player, Enemy, request, response);
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
                        Duration = 8,
                        Color = Color.Red
                    });
            }
        }

        foreach (var statusEffect in damage.ReflectedEffects)
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

        AutoConsumePotions(Player);
        AutoConsumePotions(Enemy);

        AutoQueueEnemyPotions();

        Enemy.Tick();
    }

    private void AutoConsumePotions(Pawn pawn)
    {
        foreach (var potion in pawn.Equipment.Potions.ToList())
        {
            if (potion.PotionHandler?.TryAutoUse(pawn) is not { } result) 
                continue;
            
            LogMessage(result.Message);
            if (result.AlertMessage != null && pawn.PawnType == PawnType.Player)
            {
                _encounter.Zone.Alert(new ScreenMessageData
                {
                    Text = result.AlertMessage,
                    Duration = 8,
                    Color = result.AlertColor
                });
            }
            pawn.Equipment.UnEquip(potion);
            GameContext.Current.Achievements.OnItemUsed(pawn, potion);
            potion.Destroy();
        }
    }

    private void AutoQueueEnemyPotions()
    {
        if (ItemQueuedFor(Enemy) != null || !GameContext.Random.Chance(0.01f)) return;

        var usablePotions = new List<ItemDef> { Defs.Items.AcidFlask, Defs.Items.SpicedChurni };
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
        
        attacker.Body.ConsumeEnergyFromAttack();
    }

    private void HandleQueuedItem(Pawn pawn, Pawn target)
    {
        if (DeQueuedItemForPawn(pawn) is not { } item) return;

        if (item is { ItemDef: { ItemType: ItemType.Potion } } potion)
        {
            // Track potion usage for achievements (player only)
            if (pawn.PawnType == PawnType.Player)
            {
                GameContext.Current.Achievements.OnItemUsed(pawn, potion);
            }

            // Use the potion handler if available
            if (potion.PotionHandler != null)
            {
                var result = potion.PotionHandler.UseInCombat(pawn, target);
                LogMessage(result.Message);
                
                if (result.AlertMessage != null)
                {
                    _encounter.Zone.Alert(new ScreenMessageData
                    {
                        Text = result.AlertMessage,
                            Duration = 8,
                        Color = result.AlertColor
                    });
                }
            }
            else
            {
                // Fallback for potions without handlers
                LogMessage($"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]used /c[{TC.Item}]{potion.Label}");
            }

            pawn.Equipment.UnEquip(potion);
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
                GameContext.Current.World.ProgressTracker.OnZoneCompleted(_encounter.Zone);
            }
        }

        // Notify achievements of combat end
        GameContext.Current.Achievements.OnCombatEnd(new AchievementCombatEndContext
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
            if (GameContext.Current.Player.HasTrinkets(item.ItemDef))
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
            if (GameContext.Random.Chance(resource.ChanceToHarvest))
            {
                AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
            }
        }

        // Save loot for display before auto-collecting
        foreach (var item in Loot.AsItems())
        {
            CollectedLoot.Add(new ResourceCount(item.ItemDef, item.StackSize));
        }

        // Auto loot
        var items = Loot.AsItems().ToList();
        if (items == null) return;

        for (var index = items.Count - 1; index >= 0; index--)
        {
            var item = items[index];
            Player.Inventory.TryAdd(item);
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
                if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && GameContext.Random.Chance(chanceToLootEquipment))
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
        Player.Body.Handler.OnBloodLost -= GameContext.Current.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost -= GameContext.Current.Achievements.OnBloodLost;
    }
}

public class PostCombatReport
{
    public Pawn Player { get; set; } = null!;
    public Pawn Enemy { get; set; } = null!;
    public double TotalDirectPlayerDamage { get; set; }
}