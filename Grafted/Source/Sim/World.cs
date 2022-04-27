using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;

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

    public void Initialize() {
        Time = new SimTime();
        PlayerPawns = new List<Pawn>();
        DeathRecords = new PawnDeathRecords();
        Zones = new Dictionary<ZoneDef, Zone>();
        foreach (ZoneDef zoneDef in DefRepository<ZoneDef>.Defs) {
            Zones[zoneDef] = new Zone() { Def = zoneDef };
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

    public void MoveToZone(ZoneDef zoneDef) {
        // progress time to return to beginning of zone
        //todo MovementMultiplier
        ProgressTime(CurrentZone.DistanceTraveled * SimTime.MinutesToSeconds(SimTime.MinutesPerKm)); //roughly 10 minutes per km
        CurrentZone.Reset();
        CurrentZone = Zones[zoneDef];
        Core.Sim.Messages.Push(new Message(
            $"\\c[{UiTextColor.TextColorPawn}]{PlayerPawns[0]} \\c[{UiTextColor.TextColorDefault}]moved to zone \\c[{UiTextColor.TextColorZone}]{zoneDef.Label}"
        ));
    }

    public void DoZoneTravel() {
        // Update Travel Distance
        //todo MovementMultiplier
        float minutesSpentTravelling = Core.Random.Next(4, 23);
        ProgressTime(SimTime.SecondsInMinute * minutesSpentTravelling);
        CurrentZone.DistanceTraveled += minutesSpentTravelling / SimTime.MinutesPerKm;
    }

    public void ProgressTime(float seconds) {
        while (seconds > 0) {
            seconds--;
            Time.CurrentTimeInSeconds++;
            if (Time.CurrentTimeInSeconds % SimTime.SecondsInMinute == 0) {
                Time.Ticks++;
                foreach (Pawn pawn in PlayerPawns) {
                    pawn.Tick();
                }

                // tick temporary pawns
                if (Core.Sim.Gui is CombatGui gui) {
                    foreach (Pawn pawn in gui.CombatEvent.EnemyPawns) {
                        pawn.Tick();
                    }
                }
            }
        }
    }

    public void RegisterKill(Pawn pawnKilled) {
        TotalKills++;
        CurrentZone.ZoneKills++;
        CurrentZone.TotalZoneKills++;
    }
}