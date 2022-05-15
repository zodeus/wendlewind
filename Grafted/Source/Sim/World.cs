using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;
using Grafted.Sim.Zones;
using Microsoft.Xna.Framework;

namespace Grafted.Sim;

public enum ZoneType {
    Invalid,
    Town,
    Adventure,
    SpecialEvent
}

public class World : IExposable, IIdentityProvider {
    public SimTime Time = null!;
    public List<Pawn> PlayerPawns = null!;
    public PawnDeathRecords DeathRecords = null!;
    public Zone CurrentZone = null!;
    public Dictionary<ZoneDef, Zone> Zones = null!;
    public int TotalKills;

    public Pawn PlayerPawn => PlayerPawns[0];

    public void Initialize() {
        Time = new SimTime();
        PlayerPawns = new List<Pawn>();
        DeathRecords = new PawnDeathRecords();
        Zones = new Dictionary<ZoneDef, Zone>();
        foreach (ZoneDef zoneDef in DefRepository<ZoneDef>.Defs) {
            Zones[zoneDef] = new Zone();
            Zones[zoneDef].Initialize(this, zoneDef);
        }

        TotalKills = 0;
    }

    public void AddPlayerPawn(Pawn pawn) {
        PlayerPawns.Add(pawn);
    }

    public void ProgressUntilTimeOfDay(int time) {
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

        Core.Sim.OminousMessageSpawner.Tick();
    }

    public void RegisterKill(Pawn pawnKilled) {
        TotalKills++;
        CurrentZone.ZoneKills++;
        CurrentZone.TotalZoneKills++;
    }

    public void ExposeData() {
        Scribe_Deep.Look(ref Time!, "Time");
        Scribe_Collections.Look(ref PlayerPawns!, "PlayerPawns", LookMode.Deep);
        Scribe_Deep.Look(ref DeathRecords!, "DeathRecords");
        Scribe_References.Look(ref CurrentZone!, "CurrentZone");
        Scribe_Collections.Look(ref Zones!, "Zones", LookMode.Def, LookMode.Deep);
        Scribe_Values.Look(ref TotalKills, "TotalKills");
    }

    public string GetUniqueId() {
        return "world";
    }
}