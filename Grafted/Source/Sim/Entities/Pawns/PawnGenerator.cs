using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
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

        var skills = pawn.Skills.InRandomOrder().ToList();
        skills[0].Level = new RangeInt(2, 4).RandomValue;
        skills[1].Level = new RangeInt(2, 4).RandomValue;
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
                pawn.Inventory.Items.TryAdd(EntityGenerator.CreateEntity<Item>(dropCount.Item, dropCount.Amount.RandomValue));
            }
        }
    }

    private static void GenerateBody(Pawn pawn) {
        HumanBodyGenerator.Generate(pawn);
    }

    public static void RegisterEquipment(Pawn pawn, List<ItemDef> equipment) {
        foreach (ItemDef itemDef in equipment) {
            Item item = EntityGenerator.CreateEntity<Item>(itemDef, 1);
            var potentialParts = pawn.Body.AllParts.Where(p => {
                if (p.SlotFor(item) is not { } slot) {
                    return false;
                }

                if (p.Equipment[slot] != null) {
                    return false;
                }

                return true;
            }).ToList();

            if (potentialParts.Any() == false) {
                Log.Error($"Failed to equip {item} on {pawn}, no available body parts found");
                continue;
            }

            Item? returnedItem = pawn.Equipment.TryEquip(
                potentialParts[0],
                item
            );
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

public static class HumanBodyGenerator {
    public static void Generate(Pawn pawn) {
        pawn.Body.RootSocket = GenerateBody();
        GenerateBuiltInTools(pawn);
    }

    private static void GenerateBuiltInTools(Pawn pawn) {
        Item hand1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
        Item hand2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
        Item foot1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
        Item foot2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Hand && p.SlotFor(hand1) != null).ToList()[0], hand1);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Hand && p.SlotFor(hand2) != null).ToList()[1], hand2);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Foot && p.SlotFor(foot1) != null).ToList()[0], foot1);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Foot && p.SlotFor(foot2) != null).ToList()[1], foot2);
    }

    private static BodyPartSocket GenerateBody() {
        BodyPartSocket rootSocket = new(Defs.BodyPartSockets.HeadSocket);
        BodyPart head = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.HumanHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.HumanEye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.HumanEye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);

        //Skull
        BodyPart skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.HumanSkull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.HumanBrain);

        // Neck
        BodyPart neck = head.GetSocketsFor(BodyPartType.Neck)[0].TryAttachPart(Defs.BodyParts.HumanNeck);
        neck.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        neck.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        neck.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);

        //Torso
        BodyPart torso = neck.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.HumanTorso);
        torso.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.HumanStomach);

        //RibCage
        BodyPart ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.HumanRibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.HumanHeart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.HumanLung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.HumanLung);


        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.HumanArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.HumanArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.HumanLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.HumanLeg));

        return rootSocket;
    }

    static void MakeArm(BodyPart arm) {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        MakeHand(arm.GetSocketsFor(BodyPartType.Hand)[0].TryAttachPart(Defs.BodyParts.HumanHand));
    }

    static void MakeHand(BodyPart hand) {
        BodyPart artery = hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        //artery.HitPoints = 0;
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Thumb)[0].TryAttachPart(Defs.BodyParts.HumanThumb));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[0].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[1].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[2].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[3].TryAttachPart(Defs.BodyParts.HumanFinger));
    }

    static void MakeFinger(BodyPart finger) {
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
    }

    static void MakeLeg(BodyPart leg) {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        BodyPart foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.HumanFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
    }
}