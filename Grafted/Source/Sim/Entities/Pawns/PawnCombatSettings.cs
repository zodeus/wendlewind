using System.Collections.Generic;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnCombatSettings : IExposable {
    private Dictionary<RaceDef, bool> AutoCombats = new();
    private Dictionary<RaceDef, bool> AutoRetreats = new();

    public void ExposeData() {
        //Scribe_Collections.Look(ref AutoCombats, "AutoCombats", LookMode.Def, LookMode.Value);
        //Scribe_Collections.Look(ref AutoRetreats, "AutoRetreats", LookMode.Def, LookMode.Value);
    }

    public void SetAutoCombatValueFor(RaceDef creature, bool enable) {
        AutoCombats[creature] = enable;
        if (enable) {
            AutoRetreats[creature] = false;
        }
    }

    public void SetAutoRetreatValueFor(RaceDef creature, bool enable) {
        AutoRetreats[creature] = enable;
        if (enable) {
            AutoCombats[creature] = false;
        }
    }

    public bool IsAutoCombatEnabledFor(RaceDef creature) => AutoCombats.ContainsKey(creature) && AutoCombats[creature];
    public bool IsAutoRetreatEnabledFor(RaceDef creature) => AutoRetreats.ContainsKey(creature) && AutoRetreats[creature];

    public bool RequiresInteractionFor(RaceDef race) {
        return IsAutoCombatEnabledFor(race) == false && IsAutoRetreatEnabledFor(race) == false;
    }
}