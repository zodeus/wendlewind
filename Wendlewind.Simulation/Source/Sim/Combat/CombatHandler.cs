using Wendlewind.Sim.Achievements.Handlers;

namespace Wendlewind.Sim.Combat;

public class CombatHandler : IDisposable, IHasContext
{
    private const int TickHealthFlushInterval = 30;

    private readonly Encounter _encounter;
    private readonly Random _rng;
    private readonly List<CombatLogEvent> _log = [];
    private readonly Dictionary<(int PawnId, string PartKey), TickHealthAccumulator> _tickHealth = new();
    private int _lastTickHealthFlush;
    private bool _combatStarted;

    public GameContext Context { get; set; } = null!;
    public EntityContainer Loot = new();
    public List<ResourceCount> CollectedLoot { get; } = [];
    public readonly List<BodyPart> SeveredLimbs = [];
    public IReadOnlyList<CombatLogEvent> Log => _log;
    public event Action<CombatLogEvent>? CombatEventRecorded;
    public double TotalDirectPlayerDamage { get; private set; }
    public string? CauseOfDeath { get; private set; }
    public string? KillingWeapon { get; private set; }
    public string? KillingManeuver { get; private set; }

    public Random Rng => _rng;
    public Pawn Player { get; set; }
    public Pawn Enemy { get; set; }

    public CombatHandler(Encounter encounter)
    {
        _encounter = encounter;
        Context = encounter.Context;
        _rng = Context.Rng;
        Player = encounter.PlayerPawns.First();
        Enemy = encounter.EnemyPawns.First();

        Player.DamageTaken += OnDamageTaken;
        Player.Died += OnDeath;
        Enemy.DamageTaken += OnDamageTaken;
        Enemy.Died += OnDeath;

        Player.Body.TickHealthChanged += OnTickHealthChanged;
        Enemy.Body.TickHealthChanged += OnTickHealthChanged;

        Player.Body.Handler.OnBloodLost += Context.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost += Context.Achievements.OnBloodLost;
    }

    private void Record(CombatLogEvent combatEvent)
    {
        var stamped = combatEvent with { Tick = _encounter.Ticks };
        _log.Add(stamped);
        CombatEventRecorded?.Invoke(stamped);
    }

    private void OnTickHealthChanged(BodyPart part, double delta)
    {
        var pawn = part.Body?.Pawn;
        if (pawn == null || delta == 0)
        {
            return;
        }

        var key = (pawn.Id, part.InternalLabel);
        if (!_tickHealth.TryGetValue(key, out var acc))
        {
            acc = new TickHealthAccumulator(pawn, part);
            _tickHealth[key] = acc;
        }

        acc.Delta += delta;
    }

    private void FlushTickHealth(bool force = false)
    {
        if (_tickHealth.Count == 0)
        {
            return;
        }

        if (!force && _encounter.Ticks - _lastTickHealthFlush < TickHealthFlushInterval)
        {
            return;
        }

        _lastTickHealthFlush = _encounter.Ticks;
        foreach (var acc in _tickHealth.Values)
        {
            if (Math.Abs(acc.Delta) < 0.05)
            {
                continue;
            }

            Record(new CombatLogEvent
            {
                Kind = acc.Delta > 0 ? CombatEventKind.Heal : CombatEventKind.DamageOverTime,
                SubjectPawnId = acc.Pawn.Id,
                SubjectName = acc.Pawn.LabelShort,
                BodyPartKey = acc.Part.InternalLabel,
                BodyPartLabel = acc.Part.Label,
                Amount = Math.Abs(acc.Delta)
            });
        }

        _tickHealth.Clear();
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        CauseOfDeath = deathEvent.Record.CauseOfDeath;
        KillingWeapon = deathEvent.Record.KillingWeapon;
        KillingManeuver = deathEvent.Record.KillingManeuver;
        FlushTickHealth(force: true);
        Record(new CombatLogEvent
        {
            Kind = CombatEventKind.Death,
            SubjectPawnId = deathEvent.Pawn.Id,
            SubjectName = deathEvent.Pawn.LabelShort,
            Message = deathEvent.Record.CauseOfDeath
        });
        _encounter.Zone.Alert(
            new ScreenMessageData
            {
                Text = $"{deathEvent.Pawn.LabelShort}s has died from {deathEvent.Record.CauseOfDeath}",
                Duration = 8,
                Color = Color.Red
            });
        Context.DeathRecords.RecordDeath(new DeathRecord
        {
            CauseOfDeath = deathEvent.Record.CauseOfDeath,
            KillingWeapon = deathEvent.Record.KillingWeapon,
            KillingManeuver = deathEvent.Record.KillingManeuver,
            TotalDamageDealt = TotalDirectPlayerDamage,
            Ticks = _encounter.Ticks,
            ZoneDef = _encounter.Zone.ZoneDef,
            PawnName = deathEvent.Pawn.LabelShort + (_encounter.AtBoss ? " (Boss)" : "")
        });

        if (deathEvent.Pawn.PawnType == PawnType.Enemy)
        {
            Context.Achievements.OnEnemyKilled(deathEvent.Pawn);
        }

        EndCombat();
    }

    private void OnDamageTaken(Pawn victim, DamageRequest request, DamageResponse response)
    {
        var attacker = request.Source;

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
            Context.Achievements.OnEnemyDamaged(Player, Enemy, request, response);
        }

        foreach (var damage in response.TrinketDamages)
        {
            RecordDamage(victim, attacker, damage, isTrinket: true);
        }

        if (response.Missed)
        {
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.Miss,
                SubjectPawnId = victim.Id,
                SubjectName = victim.LabelShort,
                SourcePawnId = attacker.Id,
                SourceName = attacker.LabelShort
            });
        }
        else if (response.Dodged)
        {
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.Dodge,
                SubjectPawnId = victim.Id,
                SubjectName = victim.LabelShort,
                SourcePawnId = attacker.Id,
                SourceName = attacker.LabelShort
            });
        }
        else
        {
            foreach (var damage in response.Damages)
            {
                RecordDamage(victim, attacker, damage, isTrinket: false);
            }
        }
    }

    private void RecordDamage(Pawn victim, Pawn attacker, DamageRecord damage, bool isTrinket)
    {
        var subEffects = new List<CombatSubEffect>();

        foreach (var itemRecord in damage.DestroyedEquipment)
        {
            subEffects.Add(new CombatSubEffect
            {
                Kind = CombatEventKind.EquipmentDestroyed,
                ItemMoniker = itemRecord.Def.Moniker,
                ItemLabel = itemRecord.Def.Label,
                Label = itemRecord.Def.Label
            });
        }

        foreach (var partRecord in damage.BodyParts)
        {
            foreach (var modifier in partRecord.AppliedModifiers)
            {
                subEffects.Add(new CombatSubEffect
                {
                    Kind = modifier.Type == BodyPartModifierType.Buff
                        ? CombatEventKind.BuffApplied
                        : CombatEventKind.DebuffApplied,
                    SubjectPawnId = victim.Id,
                    SubjectName = victim.LabelShort,
                    BodyPartKey = partRecord.BodyPart.InternalLabel,
                    BodyPartLabel = partRecord.PartType.ToString(),
                    Label = modifier.Label
                });
            }

            if (partRecord.WasDestroyed)
            {
                subEffects.Add(new CombatSubEffect
                {
                    Kind = CombatEventKind.PartDestroyed,
                    SubjectPawnId = victim.Id,
                    SubjectName = victim.LabelShort,
                    BodyPartKey = partRecord.BodyPart.InternalLabel,
                    BodyPartLabel = partRecord.PartType.ToString(),
                    IsVital = partRecord.IsVital
                });
            }

            if (partRecord.BodyPart.IsExternal && partRecord.WasSevered)
            {
                subEffects.Add(new CombatSubEffect
                {
                    Kind = CombatEventKind.PartSevered,
                    SubjectPawnId = victim.Id,
                    SubjectName = victim.LabelShort,
                    BodyPartKey = partRecord.BodyPart.InternalLabel,
                    BodyPartLabel = partRecord.PartType.ToString()
                });
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
            subEffects.Add(new CombatSubEffect
            {
                Kind = CombatEventKind.StatusReflected,
                SubjectPawnId = statusEffect.Pawn.Id,
                SubjectName = statusEffect.Pawn.Label,
                Label = statusEffect.Label,
                ItemLabel = statusEffect.EffectDef.Label
            });
        }

        Record(new CombatLogEvent
        {
            Kind = CombatEventKind.Damage,
            SubjectPawnId = victim.Id,
            SubjectName = victim.LabelShort,
            SourcePawnId = attacker.Id,
            SourceName = attacker.Label,
            ItemLabel = damage.WeaponLabel,
            WeaponManeuverLabel = damage.WeaponManeuverLabel,
            BodyPartKey = damage.BodyPartHit.InternalLabel,
            BodyPartLabel = damage.BodyPartHit.Label,
            Amount = damage.ActualAmount,
            Blocked = damage.AmountBlocked,
            DamageType = damage.DamageType.ToString(),
            IsCritical = damage.IsCritical,
            IsTrinket = isTrinket,
            SubEffects = subEffects.ToArray()
        });
    }

    public void Tick()
    {
        if (!_combatStarted)
        {
            _combatStarted = true;
            OnCombatStart();
        }

        FlushTickHealth();

        Attack(Player, Enemy);

        if (Enemy.IsDead)
        {
            return;
        }

        Attack(Enemy, Player);

        EvaluatePotionTriggers(Player, Enemy);
        EvaluatePotionTriggers(Enemy, Player);
        EvaluateMedicalTriggers(Player, Enemy);
        EvaluateMedicalTriggers(Enemy, Player);

        Enemy.Tick();
        FlushTickHealth();
    }

    private void OnCombatStart()
    {
        Player.ApplyBattleStartConsumables();
        Enemy.ApplyBattleStartConsumables();
    }

    private void EvaluatePotionTriggers(Pawn self, Pawn enemy)
    {
        foreach (var potion in self.Equipment.Potions.ToList())
        {
            if (potion.PotionTrigger?.ShouldFire(self, enemy, _encounter.Ticks) != true)
            {
                continue;
            }

            if (potion.PotionHandler == null)
            {
                continue;
            }

            var result = potion.PotionHandler.UseInCombat(self, enemy);

            if (result.AlertMessage != null && self.PawnType == PawnType.Player)
            {
                _encounter.Zone.Alert(new ScreenMessageData
                {
                    Text = result.AlertMessage,
                    Duration = 8,
                    Color = result.AlertColor
                });
            }

            if (!result.Success)
            {
                Record(new CombatLogEvent
                {
                    Kind = CombatEventKind.System,
                    SubjectPawnId = self.Id,
                    SubjectName = self.LabelShort,
                    ItemMoniker = potion.ItemDef.Moniker,
                    ItemLabel = potion.Label,
                    Message = result.Message
                });
                continue;
            }

            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.PotionUsed,
                SubjectPawnId = self.Id,
                SubjectName = self.LabelShort,
                ItemMoniker = potion.ItemDef.Moniker,
                ItemLabel = potion.Label,
                Message = result.Message
            });

            self.Equipment.UnEquip(potion);
            Context.Achievements.OnItemUsed(self, potion);
            potion.Destroy();
        }
    }

    private void EvaluateMedicalTriggers(Pawn self, Pawn enemy)
    {
        self.MedicalChest.Prune();
        foreach (var slot in self.MedicalChest.Slots.ToList())
        {
            if (slot.Trigger?.ShouldFire(self, enemy, _encounter.Ticks) != true)
            {
                continue;
            }

            if (!TryApplyMedical(self, slot, out var partLabel, out var partKey))
            {
                continue;
            }

            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.MedicalUsed,
                SubjectPawnId = self.Id,
                SubjectName = self.LabelShort,
                ItemMoniker = slot.Item.ItemDef.Moniker,
                ItemLabel = slot.Item.Label,
                BodyPartKey = partKey,
                BodyPartLabel = partLabel
            });

            Context.Achievements.OnItemUsed(self, slot.Item);
            slot.Item.StackSize--;
            if (slot.Item.StackSize < 1)
            {
                slot.Item.Destroy();
            }
        }

        self.MedicalChest.Prune();
    }

    private static bool TryApplyMedical(Pawn self, MedicalChestSlot slot, out string? partLabel, out string? partKey)
    {
        partLabel = null;
        partKey = null;
        var item = slot.Item;
        var trigger = slot.Trigger;

        if (item.Def == Defs.Items.Cauterize || trigger.TargetSelector == MedicalTargetSelector.SeveredOrUnsealedSocket)
        {
            var socket = MedicalTrigger.FindUnsealedSocket(self);
            if (socket == null)
            {
                return false;
            }

            socket.IsSealed = true;
            partLabel = socket.Label;
            partKey = socket.ParentPart?.InternalLabel;
            return true;
        }

        if (item.MedicinalHandler == null)
        {
            return false;
        }

        if (trigger.TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            var part = self.Body.FindPartByKey(trigger.TargetPartKey);
            if (part == null || !item.MedicinalHandler.ApplyToPart(item, part))
            {
                return false;
            }

            partLabel = part.Label;
            partKey = part.InternalLabel;
            return true;
        }

        var candidates = self.Body.AllExternalParts.OrderBy(p => p.HealthPercent).ToList();
        if (trigger.TargetSelector == MedicalTargetSelector.MostDamagedPart)
        {
            var part = candidates.FirstOrDefault();
            if (part == null || !item.MedicinalHandler.ApplyToPart(item, part))
            {
                return false;
            }

            partLabel = part.Label;
            partKey = part.InternalLabel;
            return true;
        }

        foreach (var part in candidates)
        {
            if (!item.MedicinalHandler.ApplyToPart(item, part))
            {
                continue;
            }

            partLabel = part.Label;
            partKey = part.InternalLabel;
            return true;
        }

        return false;
    }

    private void Attack(Pawn attacker, Pawn victim)
    {
        if (attacker.TicksToAttack > 0)
        {
            return;
        }

        attacker.ResetAttackCoolDown();
        var damageOptions = attacker.Equipment.UsableWeapons
            .Where(w => w.UseInCombat)
            .Select(t => DamageRequest.Create(attacker, t))
            .ToList();

        if (damageOptions.Count == 0)
        {
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.System,
                SubjectPawnId = attacker.Id,
                SubjectName = attacker.LabelShort,
                Message = $"{attacker.LabelShort} has no usable weapons"
            });
            return;
        }

        var damageRequest = damageOptions.RandomElement(Context.Rng);
        damageRequest.TargetedPart = victim.Body.AllExternalParts.Where(p => p.IsDestroyed == false || p.AllInternalParts.Count != 0).RandomElementByWeight(part => part.HitWeight, Context.Rng)!;
        victim.TakeDamage(damageRequest);

        attacker.Body.ConsumeEnergyFromAttack();
    }

    private void EndCombat()
    {
        FlushTickHealth(force: true);

        Player.Body.Effects.ClearWholeEncounterEffects();
        Enemy.Body.Effects.ClearWholeEncounterEffects();

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
                Context.World.ProgressTracker.OnZoneCompleted(_encounter.Zone);
            }
        }

        Context.Achievements.OnCombatEnd(new AchievementCombatEndContext
        {
            Player = Player,
            Enemy = Enemy,
            PlayerWon = playerIsAlive,
            TotalDamageDealt = TotalDirectPlayerDamage,
            CombatTicks = _encounter.Ticks,
            Zone = _encounter.Zone,
            CauseOfDeath = CauseOfDeath
        });

        Record(new CombatLogEvent
        {
            Kind = CombatEventKind.System,
            SubjectPawnId = Player.Id,
            SubjectName = Player.LabelShort,
            Message = "Battle is over"
        });
        _encounter.State = EncounterState.Finished;
    }

    private void CollectLoot()
    {
        for (var i = Enemy.Inventory.Count() - 1; i >= 0; i--)
        {
            var item = Enemy.Inventory[i];
            if (Context.Player.HasTrinkets(item.ItemDef))
            {
                continue;
            }

            AddToLootContainer(item);
        }

        foreach (var part in SeveredLimbs)
        {
            TakePartEquipment(part);
        }

        foreach (var resource in _encounter.Zone.ZoneDef.Resources)
        {
            if (Context.Rng.Chance(resource.ChanceToHarvest))
            {
                AddToLootContainer(Context.Factory.CreateEntity<Item>(resource.Item, resource.Amount.Roll(Context.Rng)));
            }
        }

        foreach (var item in Loot.AsItems())
        {
            CollectedLoot.Add(new ResourceCount(item.ItemDef, item.StackSize));
        }

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
                if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && Context.Rng.Chance(chanceToLootEquipment))
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
        FlushTickHealth(force: true);
        Player.DamageTaken -= OnDamageTaken;
        Player.Died -= OnDeath;
        Enemy.DamageTaken -= OnDamageTaken;
        Enemy.Died -= OnDeath;
        Player.Body.TickHealthChanged -= OnTickHealthChanged;
        Enemy.Body.TickHealthChanged -= OnTickHealthChanged;
        Player.Body.Handler.OnBloodLost -= Context.Achievements.OnBloodLost;
        Enemy.Body.Handler.OnBloodLost -= Context.Achievements.OnBloodLost;
    }

    private sealed class TickHealthAccumulator(Pawn pawn, BodyPart part)
    {
        public readonly Pawn Pawn = pawn;
        public readonly BodyPart Part = part;
        public double Delta;
    }
}

public class PostCombatReport
{
    public Pawn Player { get; set; } = null!;
    public Pawn Enemy { get; set; } = null!;
    public double TotalDirectPlayerDamage { get; set; }
}
