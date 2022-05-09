using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns.BodyGenerators;
using Grafted.Utils;

namespace Grafted.Sim.Entities.Pawns;

public static class PawnGenerator {
    public static Pawn CreatePawn(PawnRequest request) {
        Pawn pawn = EntityGenerator.CreateEntity<Pawn>(request.Race.Species, true);
        pawn.Race = request.Race;
        pawn.PawnType = request.Config.PawnType;
        pawn.Initialize();
        if (request.Config.PawnName != null) {
            pawn.Biography.Name = request.Config.PawnName;
        }

        RegisterTraits(pawn);
        RegisterSkills(pawn);

        GenerateBody(pawn);
        RegisterEquipment(pawn, request.Config.EquipmentItems);
        RegisterInventory(pawn, request.Config.InventoryItems);

        return pawn;
    }

    private static void RegisterSkills(Pawn pawn) {
        if (pawn.PawnType != PawnType.Player) {
            return;
        }

        //var skills = pawn.Skills.InRandomOrder().ToList();
        //skills[0].Level = new RangeInt(2, 4).RandomValue;
        //skills[1].Level = new RangeInt(2, 4).RandomValue;
        /*RangeInt range = new(0, 3);
        foreach (Skill skill in pawn.Skills) {
            skill.Level = range.RandomValue;
        }*/
    }

    private static void RegisterTraits(Pawn pawn) {
        int numberOfTraits = new RangeInt(2, 2).RandomValue;
        foreach (TraitDef def in DefRepository<TraitDef>.Defs.InRandomOrder().Take(numberOfTraits)) {
            pawn.Traits.Add(def);
        }
    }

    public static void RegisterInventory(Pawn pawn, List<ItemDropCount> items) {
        foreach (ItemDropCount dropCount in items) {
            if (Core.Random.Chance(dropCount.ChanceToDrop)) {
                pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(dropCount.Item, dropCount.Amount.RandomValue));
            }
        }
    }

    private static void GenerateBody(Pawn pawn) {
        if (pawn.Race == Defs.Races.Glump) {
            GlumpBodyGenerator.Generate(pawn);
        }
        else if (pawn.Race == Defs.Races.InnocentRabbit) {
            RabbitBodyGenerator.Generate(pawn);
        }
        else if (pawn.Race == Defs.Races.FieldHound) {
            WolfBodyGenerator.Generate(pawn);
        }
        else if (pawn.Race == Defs.Races.TruffleBoar) {
            PigBodyGenerator.Generate(pawn);
        }
        else {
            HumanBodyGenerator.Generate(pawn);
        }
    }

    public static void RegisterEquipment(Pawn pawn, List<ItemDef> equipment) {
        Item? returnedItem = null;
        foreach (ItemDef itemDef in equipment) {
            Item item = EntityGenerator.CreateEntity<Item>(itemDef, 1);
            foreach (BodyPart bodyPart in pawn.Body.AllExternalParts) {
                if (bodyPart.EmptySlotFor(item) is not { } slot) {
                    continue;
                }

                pawn.Equipment.TryEquip(
                    bodyPart,
                    slot,
                    item
                );
                break;
            }

            if (returnedItem != null) {
                Log.Error($"{returnedItem} was returned while attempting to equip on {pawn} PawnGenerator.RegisterTools");
            }
        }
    }
}

public struct PawnRequest {
    public RaceDef Race { get; }
    public PawnConfigDef Config { get; }

    public PawnRequest(RaceDef race, PawnConfigDef config) {
        Race = race;
        Config = config;
    }
}