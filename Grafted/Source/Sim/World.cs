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
    public PawnDeathRecords DeathRecords = null!;
    public Zone CurrentZone = null!;
    public Dictionary<ZoneDef, Zone> Zones = null!;
    public OminousMessageSpawner OminousMessageSpawner = null!;
    public Player Player = null!;
    public int TotalKills;

    public Pawn PlayerPawn => Player.Pawn;

    public void Initialize(Player player) {
        Player = player;
        Time = new SimTime();
        DeathRecords = new PawnDeathRecords();
        Zones = new Dictionary<ZoneDef, Zone>();
        OminousMessageSpawner = new OminousMessageSpawner();
        foreach (ZoneDef zoneDef in DefRepository<ZoneDef>.Defs) {
            Zones[zoneDef] = new Zone();
            Zones[zoneDef].Initialize(this, zoneDef);
        }

        TotalKills = 0;
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

        Player.Tick();
        OminousMessageSpawner.Tick();
    }

    public void RegisterKill(Pawn pawnKilled) {
        TotalKills++;
        CurrentZone.ZoneKills++;
        CurrentZone.TotalZoneKills++;
    }

    public void ExposeData() {
        Scribe_Deep.Look(ref OminousMessageSpawner!, "OminousMessageSpawner");
        Scribe_Deep.Look(ref Time!, "Time");
        Scribe_Deep.Look(ref Player!, "Player");
        Scribe_Deep.Look(ref DeathRecords!, "DeathRecords");
        Scribe_References.Look(ref CurrentZone!, "CurrentZone");
        Scribe_Collections.Look(ref Zones!, "Zones", LookMode.Def, LookMode.Deep);
        Scribe_Values.Look(ref TotalKills, "TotalKills");
    }

    public string GetUniqueId() {
        return "world";
    }
}