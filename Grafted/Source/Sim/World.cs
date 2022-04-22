using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class World : IExposable {
    public List<Pawn> PlayerPawns = null!;
    public int TotalKills;
    public PawnDeathRecords DeathRecords = null!;

    public Zone CurrentZone = null!;

    public Dictionary<ZoneDef, Zone> Zones = null!;

    public void Initialize() {
        PlayerPawns = new List<Pawn>();
        DeathRecords = new PawnDeathRecords();
        Zones = new Dictionary<ZoneDef, Zone>();
        foreach (ZoneDef zoneDef in DefRepository<ZoneDef>.Defs) {
            Zones[zoneDef] = new Zone() { Def = zoneDef };
            if (zoneDef == Defs.Zones.VillageOfTheDamned) {
                Zones[zoneDef].Town = new Town();
            }
        }

        CurrentZone = Zones[Defs.Zones.Intro];

        TotalKills = 0;
    }

    public void AddPlayerPawn(Pawn pawn) {
        PlayerPawns.Add(pawn);
    }

    public CombatEvent NextCombat() {
        int nextCombatId = Mathf.Clamp(TotalKills + 1, 1, 16);
        if (CurrentZone.Def == Defs.Zones.Intro) {
            return CombatGenerator.GenerateIntroCombat(PlayerPawns, nextCombatId);
        }

        return CombatGenerator.GenerateForZone(PlayerPawns, CurrentZone);
    }

    public void ExposeData() { }

    public DialogueNode NextDialogue() {
        return DialogueGenerator.Generate();
    }

    public void MoveToZone(ZoneDef zoneDef) {
        CurrentZone = Zones[zoneDef];
    }
}

public class Zone {
    public ZoneDef Def;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public Town? Town;
}

public class ZoneDef : Def {
    //public List<ZoneResouce> Drops;
}

public class Town {
    public ItemContainer Storage = new();
}


//todo rest
/*foreach (BodyPart part in playerPawn.Body.AllParts) {
           if (part.HealthPercent >= .97) { continue; }

           if (part.Type == BodyPartType.Skin) {
               part.HitPoints += Mathf.FloorToInt(part.MaxHitPoints * Core.Random.NextFloat(0.10f, 0.25f));
               continue;
           }

           if (part.IsDestroyed) {
               /*if (Core.Random.Chance(.04f)) {
                   part.HitPoints = 1;
               }#1#

               continue;
           }

           part.HitPoints += Mathf.FloorToInt(part.MaxHitPoints * Core.Random.NextFloat(0.03f, 0.08f));
       }*/