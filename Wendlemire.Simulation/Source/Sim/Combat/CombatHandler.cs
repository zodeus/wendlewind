using Wendlemire.Sim.Achievements.Handlers;

namespace Wendlemire.Sim.Combat;

public class CombatHandler : IDisposable, IHasContext
{
    private const int TickHealthFlushInterval = 30;

    private readonly Encounter _encounter;
    private readonly Random _rng;
    private readonly List<CombatLogEvent> _log = [];
    private readonly Dictionary<(int PawnId, string PartKey), TickHealthAccumulator> _tickHealth = new();
    private readonly List<DamageRequest> _damageOptions = [];
    private readonly List<CombatSubEffect> _subEffects = [];
    private readonly List<Item> _potionScratch = [];
    private int _lastTickHealthFlush;
    private int _lastCloserFlush;
    private bool _combatStarted;
    private bool _closerActive;
    private bool _ended;
    private readonly Dictionary<int, double> _closerBlood = new();

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
        _subEffects.Clear();

        foreach (var itemRecord in damage.DestroyedEquipment)
        {
            _subEffects.Add(new CombatSubEffect
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
                _subEffects.Add(new CombatSubEffect
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
                _subEffects.Add(new CombatSubEffect
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
                _subEffects.Add(new CombatSubEffect
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
            _subEffects.Add(new CombatSubEffect
            {
                Kind = CombatEventKind.StatusReflected,
                SubjectPawnId = statusEffect.Pawn.Id,
                SubjectName = statusEffect.Pawn.Label,
                Label = statusEffect.Label,
                ItemLabel = statusEffect.EffectDef.Label,
                ItemMoniker = statusEffect.ItemMoniker ?? statusEffect.EffectDef.Moniker
            });
        }

        Record(new CombatLogEvent
        {
            Kind = CombatEventKind.Damage,
            SubjectPawnId = victim.Id,
            SubjectName = victim.LabelShort,
            SourcePawnId = attacker.Id,
            SourceName = attacker.Label,
            ItemMoniker = damage.WeaponMoniker,
            ItemLabel = damage.WeaponLabel,
            BlockingItemMoniker = damage.BlockingItemMoniker,
            BlockingItemLabel = damage.BlockingItemLabel,
            WeaponManeuverLabel = damage.WeaponManeuverLabel,
            BodyPartKey = damage.BodyPartHit.InternalLabel,
            BodyPartLabel = damage.BodyPartHit.Label,
            Amount = damage.ActualAmount,
            Blocked = damage.AmountBlocked,
            DamageType = damage.DamageType.ToString(),
            IsCritical = damage.IsCritical,
            IsTrinket = isTrinket,
            SubEffects = _subEffects.ToArray()
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

        if (_ended || Enemy.IsDead)
        {
            return;
        }

        Attack(Enemy, Player);

        if (_ended)
        {
            return;
        }

        EvaluatePotionTriggers(Player, Enemy);
        EvaluatePotionTriggers(Enemy, Player);
        EvaluateMedicalTriggers(Player, Enemy);
        EvaluateMedicalTriggers(Enemy, Player);
        EvaluateIncenseTriggers(Player);
        EvaluateIncenseTriggers(Enemy);

        if (_ended)
        {
            return;
        }

        TickCloser();

        if (_ended)
        {
            return;
        }

        Player.Body.Effects.Tick();
        Enemy.Tick();
        FlushTickHealth();
    }

    private void TickCloser()
    {
        if (_ended || !CombatCloser.IsActive(_encounter.Ticks))
        {
            return;
        }

        if (!_closerActive)
        {
            _closerActive = true;
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.System,
                Message = CombatCloser.StartedMessage
            });
            _encounter.Zone.Alert(new ScreenMessageData
            {
                Text = CombatCloser.StartedMessage.ToUpperInvariant(),
                Duration = 10,
                Color = Color.OrangeRed
            });
        }

        var drain = CombatCloser.BloodDrainPerTick(_encounter.Ticks);
        DrainCloserBlood(Player, drain);
        DrainCloserBlood(Enemy, drain);
        FlushCloserBlood();
        ResolveCloserDeaths();
    }

    private void DrainCloserBlood(Pawn pawn, float drain)
    {
        if (pawn.IsDead || drain <= 0)
        {
            return;
        }

        var before = pawn.Body.BloodAmount;
        pawn.Body.BloodAmount -= drain;
        var lost = before - pawn.Body.BloodAmount;
        if (lost > 0)
        {
            _closerBlood[pawn.Id] = _closerBlood.GetValueOrDefault(pawn.Id) + lost;
        }
    }

    private void FlushCloserBlood(bool force = false)
    {
        if (_closerBlood.Count == 0)
        {
            return;
        }

        if (!force && _encounter.Ticks - _lastCloserFlush < TickHealthFlushInterval)
        {
            return;
        }

        _lastCloserFlush = _encounter.Ticks;
        foreach (var (id, amount) in _closerBlood)
        {
            if (amount < 0.05)
            {
                continue;
            }

            var pawn = id == Player.Id ? Player : Enemy;
            var torso = FindTorso(pawn);
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.DamageOverTime,
                SubjectPawnId = pawn.Id,
                SubjectName = pawn.LabelShort,
                BodyPartKey = torso?.InternalLabel,
                BodyPartLabel = "blood",
                Amount = amount
            });
        }

        _closerBlood.Clear();
    }

    private static BodyPart? FindTorso(Pawn pawn)
    {
        var parts = pawn.Body.AllExternalParts;
        for (var i = 0; i < parts.Count; i++)
        {
            if (parts[i].Type == BodyPartType.Torso)
            {
                return parts[i];
            }
        }

        return parts.Count > 0 ? parts[0] : null;
    }

    private void ResolveCloserDeaths()
    {
        var playerLethal = !Player.IsDead && Player.Body.BloodAmount <= 1;
        var enemyLethal = !Enemy.IsDead && Enemy.Body.BloodAmount <= 1;

        if (playerLethal && enemyLethal)
        {
            KillCloserLoser(CombatCloser.CauseOfDeath);
            return;
        }

        if (playerLethal)
        {
            TriggerBloodDeath(Player);
            return;
        }

        if (enemyLethal)
        {
            TriggerBloodDeath(Enemy);
            return;
        }

        if (CombatCloser.ShouldHardResolve(_encounter.Ticks) && !Player.IsDead && !Enemy.IsDead)
        {
            KillCloserLoser(CombatCloser.CauseOfDeath);
        }
    }

    private void KillCloserLoser(string cause)
    {
        var loser = CombatCloser.PickLoser(Player, Enemy);
        loser.TriggerDeath(new DeathRecord
        {
            CauseOfDeath = cause,
            KillingWeapon = cause,
            KillingManeuver = cause
        });
    }

    private static void TriggerBloodDeath(Pawn pawn)
    {
        pawn.TriggerDeath(new DeathRecord
        {
            CauseOfDeath = "Blood loss",
            KillingWeapon = "Blood loss",
            KillingManeuver = "Blood loss"
        });
    }

    private void OnCombatStart()
    {
        Player.MedicalChest.ResetCooldowns();
        Enemy.MedicalChest.ResetCooldowns();
        Player.ResetIncenseForEncounter();
        Enemy.ResetIncenseForEncounter();
        Player.ApplyBattleStartConsumables();
        Enemy.ApplyBattleStartConsumables();
        ApplyPermanentInstalls(Player);
        ApplyPermanentInstalls(Enemy);
    }

    private void ApplyPermanentInstalls(Pawn self)
    {
        self.MedicalChest.Prune();
        var applied = new HashSet<ItemDef>();
        foreach (var slot in self.MedicalChest.Slots.ToList())
        {
            if (!IsBattleStartInstall(slot))
            {
                continue;
            }

            if (applied.Add(slot.Def))
            {
                TryFireMedicalSlot(self, slot);
            }

            MedicalChest.LockForRestOfCombat(slot);
        }
    }

    private static bool IsBattleStartInstall(MedicalChestSlot slot)
    {
        return slot.IsInfinite && slot.Trigger?.Type == MedicalTriggerType.Immediately;
    }

    private void EvaluatePotionTriggers(Pawn self, Pawn enemy)
    {
        _potionScratch.Clear();
        foreach (var potion in self.Equipment.Potions)
        {
            _potionScratch.Add(potion);
        }

        foreach (var potion in _potionScratch)
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

            var appliedTo = potion.PotionHandler.GetCombatApplicationTarget(self, enemy);
            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.PotionUsed,
                SubjectPawnId = self.Id,
                SubjectName = self.LabelShort,
                SourcePawnId = self.Id,
                SourceName = self.LabelShort,
                TargetPawnId = appliedTo.Id,
                TargetName = appliedTo.LabelShort,
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
            if (IsBattleStartInstall(slot)
                || !slot.HasCharge
                || _encounter.Ticks < slot.NextReadyTick)
            {
                continue;
            }

            if (slot.Trigger?.ShouldFire(self, enemy, _encounter.Ticks, slot.Def) != true)
            {
                continue;
            }

            if (!TryFireMedicalSlot(self, slot))
            {
                slot.NextReadyTick = _encounter.Ticks + MedicalChest.FailedApplyBackoffInTicks;
            }
        }
    }

    private void EvaluateIncenseTriggers(Pawn self)
    {
        for (var i = 0; i < self.ActiveIncense.Count; i++)
        {
            var incense = self.ActiveIncense[i];
            if (!incense.ShouldFire(_encounter.Ticks, i))
            {
                continue;
            }

            if (!self.TryIgniteIncense(incense))
            {
                continue;
            }

            var itemDef = incense.SourceMoniker != null
                ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
                : null;
            var label = itemDef?.Label ?? incense.Def.Label;
            var moniker = incense.SourceMoniker ?? incense.Def.Moniker;

            Record(new CombatLogEvent
            {
                Kind = CombatEventKind.IncenseLit,
                SubjectPawnId = self.Id,
                SubjectName = self.LabelShort,
                SourcePawnId = self.Id,
                SourceName = self.LabelShort,
                TargetPawnId = self.Id,
                TargetName = self.LabelShort,
                ItemMoniker = moniker,
                ItemLabel = label,
                Message = $"/c[{TC.Attacker}]{self.LabelShort} /c[{TC.Yellow}]lit /c[{TC.Item}]{label}"
            });
        }
    }

    private bool TryFireMedicalSlot(Pawn self, MedicalChestSlot slot)
    {
        var item = Context.Factory.CreateEntity<Item>(slot.Def, 1);
        if (!TryApplyMedical(self, slot, item, out var partLabel, out var partKey))
        {
            item.Destroy();
            return false;
        }

        Record(new CombatLogEvent
        {
            Kind = CombatEventKind.MedicalUsed,
            SubjectPawnId = self.Id,
            SubjectName = self.LabelShort,
            ItemMoniker = slot.Def.Moniker,
            ItemLabel = slot.Def.Label,
            BodyPartKey = partKey,
            BodyPartLabel = partLabel
        });

        Context.Achievements.OnItemUsed(self, item);
        item.Destroy();

        if (!slot.IsInfinite)
        {
            slot.Charges--;
        }

        slot.NextReadyTick = _encounter.Ticks + MedicalChest.CooldownInTicks(slot.Def);
        return true;
    }

    private bool TryApplyMedical(Pawn self, MedicalChestSlot slot, Item item, out string? partLabel, out string? partKey)
    {
        partLabel = null;
        partKey = null;
        var trigger = slot.Trigger ?? new MedicalTrigger();

        if (MedicalTrigger.CanSealSocket(slot.Def) && trigger.TargetSelector == MedicalTargetSelector.SeveredOrUnsealedSocket)
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

        var applySelf = slot.Def.MedicinalProperties?.ApplyMode == MedicalApplyMode.Self;
        foreach (var part in trigger.EnumerateApplyTargets(self, slot.Def))
        {
            if (!item.MedicinalHandler.ApplyToPart(item, part))
            {
                continue;
            }

            if (applySelf)
            {
                partLabel = self.LabelShort;
                partKey = null;
            }
            else
            {
                partLabel = part.Label;
                partKey = part.InternalLabel;
            }

            return true;
        }

        return false;
    }

    private BodyPart PickTargetedPart(Pawn victim)
    {
        var parts = victim.Body.AllExternalParts;
        if (parts.Count == 0)
        {
            throw new InvalidOperationException($"{victim.LabelShort} has no external body parts to target.");
        }

        var totalWeight = 0f;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.IsDestroyed && part.AllInternalParts.Count == 0)
            {
                continue;
            }

            totalWeight += part.HitWeight;
        }

        if (totalWeight <= 0f)
        {
            return parts[0];
        }

        var roll = (float)Context.Rng.NextDouble() * totalWeight;
        var cumulative = 0f;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.IsDestroyed && part.AllInternalParts.Count == 0)
            {
                continue;
            }

            cumulative += part.HitWeight;
            if (roll < cumulative)
            {
                return part;
            }
        }

        return parts[^1];
    }

    private void Attack(Pawn attacker, Pawn victim)
    {
        if (attacker.TicksToAttack > 0)
        {
            return;
        }

        attacker.ResetAttackCoolDown();
        _damageOptions.Clear();
        foreach (var weapon in attacker.Equipment.CombatWeapons)
        {
            _damageOptions.Add(DamageRequest.Create(attacker, weapon));
        }

        if (_damageOptions.Count == 0)
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

        var damageRequest = _damageOptions[Context.Rng.Next(_damageOptions.Count)];
        damageRequest.TargetedPart = PickTargetedPart(victim);
        victim.TakeDamage(damageRequest);

        attacker.Body.ConsumeEnergyFromAttack();
    }

    private void EndCombat()
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        FlushCloserBlood(force: true);
        FlushTickHealth(force: true);

        Player.Body.RestoreBodyScale();
        Enemy.Body.RestoreBodyScale();
        Player.ClearActiveIncenseEffects();
        Enemy.ClearActiveIncenseEffects();
        Player.Body.Effects.ClearWholeEncounterEffects();
        Enemy.Body.Effects.ClearWholeEncounterEffects();
        Player.CombatStomach.Clear();
        Enemy.CombatStomach.Clear();
        Player.MealPlan.Prune();
        Enemy.MealPlan.Prune();

        var playerIsAlive = !Player.IsDead;
        if (playerIsAlive)
        {
            if (!_encounter.Def.SkipLoot && Context.ArenaRun == null)
            {
                CollectLoot();
            }
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
