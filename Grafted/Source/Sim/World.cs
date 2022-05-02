using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;
using Microsoft.Xna.Framework;

namespace Grafted.Sim;

public enum ZoneType {
    Invalid,
    Town,
    Adventure
}

public class World : IExposable {
    public SimTime Time = null!;

    public List<Pawn> PlayerPawns = null!;
    public int TotalKills;
    public PawnDeathRecords DeathRecords = null!;

    public Zone CurrentZone = null!;

    public Dictionary<ZoneDef, Zone> Zones = null!;

    public CombatEvent? ActiveCombat;

    public void Initialize() {
        Time = new SimTime();
        PlayerPawns = new List<Pawn>();
        DeathRecords = new PawnDeathRecords();
        Zones = new Dictionary<ZoneDef, Zone>();
        foreach (ZoneDef zoneDef in DefRepository<ZoneDef>.Defs) {
            Zones[zoneDef] = new Zone { Def = zoneDef };
            if (zoneDef.ZoneType == ZoneType.Town) {
                Zones[zoneDef].Town = TownGenerator.Generate(zoneDef);
            }
        }

        CurrentZone = Zones[Defs.Zones.Intro];

        TotalKills = 0;
    }

    public void AddPlayerPawn(Pawn pawn) {
        PlayerPawns.Add(pawn);
    }

    public CombatEvent NextCombat() {
        if (CurrentZone.Def == Defs.Zones.Intro) {
            int nextCombatId = Mathf.Clamp(TotalKills + 1, 1, 16);
            return CombatGenerator.GenerateIntroCombat(PlayerPawns, nextCombatId);
        }

        return CombatGenerator.GenerateForZone(PlayerPawns, CurrentZone);
    }

    public void ExposeData() { }

    public DialogueNode NextDialogue() {
        return DialogueGenerator.Generate();
    }

    public void MoveToZone(ZoneDef zoneDef, bool progressTime = true) {
        // progress time to return to beginning of zone
        //todo MovementMultiplier
        ActiveCombat = null;
        CurrentZone.Town?.GetStructure<TownStructureHouse>()?.ExitHouse(Core.Sim.World.PlayerPawns[0]);
        if (progressTime) {
            ProgressTime(CurrentZone.DistanceTraveled * SimTime.MinutesToSeconds(SimTime.MinutesPerKm)); //roughly 10 minutes per km    
        }

        CurrentZone.Reset();
        CurrentZone = Zones[zoneDef];
        Core.Sim.World.PlayerPawns[0].Zone = CurrentZone;
        CurrentZone.Town?.GetStructure<TownStructureHouse>()?.EnterHouse(Core.Sim.World.PlayerPawns[0]);
        Core.Sim.Messages.Push(new Message(
            $"\\c[{UiTextColor.TextColorPawn}]{PlayerPawns[0]} \\c[{UiTextColor.TextColorDefault}]moved to zone \\c[{UiTextColor.TextColorZone}]{zoneDef.Label}"
        ));
    }

    public void DoZoneTravel() {
        // Update Travel Distance
        //todo MovementMultiplier
        ActiveCombat = null;
        float minutesSpentTravelling = CurrentZone.Def.MeanTimeBetweenEvents.RandomValue;
        ProgressTime(SimTime.SecondsInMinute * minutesSpentTravelling);
        float distanceTraveled = (minutesSpentTravelling / SimTime.MinutesPerKm * CurrentZone.Def.TravelSpeedFactor) + CurrentZone.DistanceTraveled;
        CurrentZone.DistanceTraveled = Mathf.Clamp(distanceTraveled, 0, CurrentZone.Def.TravelSize);
    }

    public void ProgressTimeUntil(int time) {
        while (Time.CurrentTime != time) {
            ProgressTime(1);
        }
    }

    public void ProgressTime(float seconds) {
        for (int i = 0; i < seconds; i++) {
            Time.CurrentTimeInSeconds++;
            if (Time.IsIntervalOf(SimTime.SecondsInMinute)) {
                Tick();
            }
        }
    }

    private void Tick() {
        Time.Ticks++;
        if (Time.IsIntervalOf(SimTime.SecondsInDay)) {
            Core.Sim.Messages.Push(new Message(
                $"Day {Time.CurrentTimeInSeconds / SimTime.SecondsInDay}"
            ));
        }

        if (Time.CurrentTime == 1700) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Text = "It is night",
                Color = Color.Red,
                Duration = 6
            });
        }

        foreach ((ZoneDef? _, Zone? zone) in Zones) {
            zone.Tick();
        }

        foreach (Pawn pawn in PlayerPawns) {
            pawn.Tick();
        }

        ActiveCombat?.Tick();
    }

    public void RegisterKill(Pawn pawnKilled) {
        TotalKills++;
        CurrentZone.ZoneKills++;
        CurrentZone.TotalZoneKills++;
    }
}