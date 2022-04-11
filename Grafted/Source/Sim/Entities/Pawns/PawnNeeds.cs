using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class NeedDef : Def {
    public Type NeedClass = null!;
}

public abstract class Need : IExposable {
    protected readonly Pawn Pawn;

    public NeedDef Def = null!;
    private float _currentValueInt;

    public virtual float MaxValue => 1;

    public virtual float CurrentValue {
        get => _currentValueInt;
        set => _currentValueInt = Mathf.Clamp(value, 0, MaxValue);
    }

    public virtual void SetInitialValue() {
        _currentValueInt = 1f;
    }

    protected Need(Pawn pawn) {
        Pawn = pawn;
    }

    public abstract void Tick();

    public virtual void ExposeData() {
        //Scribe_Defs.Look(ref Def, "Def");
        //Scribe_Values.Look(ref _currentValueInt, "CurrentValue");
    }
}

public class NeedFood : Need {
    private int _ticksSpentHungry;

    public NeedFood(Pawn pawn) : base(pawn) { }

    public override void Tick() {
        /*CurrentValue -= 1 / ((float) SimTime.TicksPerDay / TickIntervals.PawnNeeds); // drains in 1 day (1 / (18,000 * 1) * 150) 
        if (CurrentValue > 0) {
            _ticksSpentHungry = 0;
        }
        else {
            _ticksSpentHungry += TickIntervals.PawnNeeds;
            Pawn.HitPoints -= (TickIntervals.PawnNeeds / 2) * _ticksSpentHungry * 0.000003f;
            if (Pawn.IsDead) {
                Pawn.World.DeathRecords.RecordDeath(new DeathRecord {
                    TickRecorded = Core.Sim.Ticker.Ticks,
                    PawnName = Pawn.Label,
                    TilePosition = Pawn.Tile.Position,
                    Profession = Pawn.Profession,
                    CauseOfDeath = "Killed by hunger"
                });
            }
        }*/
    }
}

public class NeedRest : Need {
    public NeedRest(Pawn pawn) : base(pawn) { }

    public override void Tick() {
        /*if (Pawn.Jobs?.CurrentJob?.Handler is JobHandlerRest) {
            return;
        }

        CurrentValue -= (float) 1 / SimTime.TicksPerDay * TickIntervals.PawnNeeds;*/
    }
}

public class NeedBodyTemperature : Need {
    public NeedBodyTemperature(Pawn pawn) : base(pawn) { }

    public override void Tick() {
        /*float temp = Pawn.Health.CurrentTemperature;
        CurrentValue += temp is > 5 and < 50 ? 0.05f : -0.05f;
        if (CurrentValue < 0.9f) {
            Pawn.HitPoints -= Mathf.Lerp(1, 0, CurrentValue) * 3f;
            if (Pawn.IsDead) {
                Pawn.World.DeathRecords.RecordDeath(new DeathRecord {
                    TickRecorded = Core.Sim.Ticker.Ticks,
                    PawnName = Pawn.Label,
                    TilePosition = Pawn.Tile.Position,
                    Profession = Pawn.Profession,
                    CauseOfDeath = $"Killed by temperature {temp}"
                });
            }
        }*/
    }
}

public class PawnNeeds : IExposable {
    private readonly Pawn _pawn;

    private List<Need> _needs = new List<Need>();
    public IReadOnlyList<Need> Needs => _needs;

    public PawnNeeds(Pawn pawn) {
        _pawn = pawn;
        RegisterCompatibleNeeds();
    }

    private void RegisterCompatibleNeeds() {
        IReadOnlyList<NeedDef> defs = DefRepository<NeedDef>.Defs;
        for (int i = 0; i < defs.Count; i++) {
            NeedDef needDef = defs[i];
            if (RequiresNeed(needDef)) {
                RegisterNeed(needDef);
            }
        }
    }

    private bool RequiresNeed(NeedDef def) {
        /*if ((int) pawn.RaceProps.intelligence < (int) nd.minIntelligence) {
            return false;
        }*/

        /*if (def == NeedDefOf.Food) {
            return _pawn.Properties.EatsFood;
        }

        if (def == NeedDefOf.Rest) {
            return _pawn.Properties.needsRest;
        }*/

        return true;
    }

    public void Tick() {
        /*if (!_pawn.IsHashIntervalTick(TickIntervals.PawnNeeds)) return;

        for (int i = 0; i < _needs.Count; i++) {
            _needs[i].Tick();
            if (_pawn.IsDead) {
                return;
            }
        }*/
    }

    private void RegisterNeed(NeedDef needDef) {
        Need need = (Need) Activator.CreateInstance(needDef.NeedClass, _pawn)!;
        need.Def = needDef;
        need.SetInitialValue();
        _needs.Add(need);
    }

    public void ExposeData() {
        //Scribe_Collections.Look(ref _needs, "Needs", LookMode.Deep, _pawn);
    }

    public T? GetNeed<T>() where T : Need {
        for (int i = 0; i < _needs.Count; i++) {
            if (_needs[i].GetType() == typeof(T)) {
                return (T) _needs[i];
            }
        }

        return null;
    }
}