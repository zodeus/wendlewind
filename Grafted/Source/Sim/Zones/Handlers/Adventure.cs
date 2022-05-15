using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Zones.Handlers;

public enum AdventureState {
    Unoccupied,
    Occupied,
    Traveling,
    InCombat,
    CombatResults
}

public class Adventure : ZoneHandler {
    public AdventureState State = AdventureState.Unoccupied;
    public CombatEvent? ActiveCombat { get; set; }

    public override void OnEnter() {
        State = AdventureState.Occupied;
        Progress();
    }

    public override void OnExit() {
        State = AdventureState.Unoccupied;

    }

    public void Progress() {
        //todo MovementMultiplier
        State = AdventureState.Traveling;
        float minutesSpentTravelling = Zone.Def.MeanTimeBetweenEvents.RandomValue;
        World.ProgressTime(SimTime.SecondsInMinute * minutesSpentTravelling);
        float distanceTraveled = (minutesSpentTravelling / SimTime.MinutesPerKm * Zone.Def.TravelSpeedFactor * World.PlayerPawn.Body.MovementSpeed) + Zone.DistanceTraveledThisRun;
        Zone.DistanceTraveledThisRun = Mathf.Clamp(distanceTraveled, 0, Zone.Def.TravelSize);
        if (Zone.DistanceTraveledThisRun > Zone.FurthestDistanceTraveled) {
            Zone.FurthestDistanceTraveled = Zone.DistanceTraveledThisRun;
        }

        State = AdventureState.InCombat;
        ActiveCombat = CombatGenerator.GenerateForZone(World.PlayerPawn, Zone);
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref State, "State");
        base.ExposeData();
    }
}