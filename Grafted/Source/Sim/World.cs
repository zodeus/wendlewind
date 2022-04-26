using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
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
        CurrentZone.Reset();
        CurrentZone = Zones[zoneDef];
        Core.Sim.Messages.Push(new Message(
            $"\\c[{UiTextColor.TextColorPawn}]{PlayerPawns[0]} \\c[{UiTextColor.TextColorDefault}]moved to zone \\c[{UiTextColor.TextColorZone}]{zoneDef.Label}"
        ));
    }

    public void RegisterKill(Pawn pawn) {
        TotalKills++;
        CurrentZone.ZoneKills++;
        CurrentZone.TotalZoneKills++;
    }
}

public class TownGenerator {
    public static Town Generate(ZoneDef zoneDef) {
        return new Town {
            ZoneDef = zoneDef,
            Merchant = GenerateMerchant(zoneDef)
        };
    }

    public static TownMerchant GenerateMerchant(ZoneDef zoneDef) {
        ItemContainer container = new();
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.MendersMist, Core.Random.Next(50, 100)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.MedKit, Core.Random.Next(20, 50)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.ArterialThreads, Core.Random.Next(100, 200)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.JarOfBlood, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.PumpinJuice, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.AcidFlask, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.ShortSword, Core.Random.Next(1, 1)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.RepairKit, Core.Random.Next(20, 50)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.SoulCoin, Core.Random.Next(50, 100)));

        return new TownMerchant {
            Items = container
        };
    }
}

public class Zone {
    public ZoneDef Def;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public float DistanceTraveled = 0;
    public float FurthestDistanceTraveled = 0;
    public Town? Town;

    public string Label => Def.Label;
    public ZoneType ZoneType => Def.ZoneType;

    public float PercentTraveled => DistanceTraveled / Def.TravelSize;

    public void Reset() {
        ZoneKills = 0; // clear current zone kill count
        if (DistanceTraveled > FurthestDistanceTraveled) {
            FurthestDistanceTraveled = DistanceTraveled;
        }

        DistanceTraveled = 0;
    }
}

public class ZoneDef : Def {
    public ZoneType ZoneType = ZoneType.Invalid;
    public float TravelSize;
}

public class Town {
    public ItemContainer Storage = new();
    public TownMerchant Merchant = null!;
    public ZoneDef ZoneDef = null!;
}

public class TownMerchant {
    public ItemContainer Items = null!;
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