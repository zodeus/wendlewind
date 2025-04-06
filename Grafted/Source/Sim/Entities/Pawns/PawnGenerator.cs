namespace Grafted.Sim.Entities.Pawns;

public static class PawnGenerator
{
    public static Pawn CreatePawn(PawnRequest request)
    {
        var pawn = EntityGenerator.CreateEntity<Pawn>(request.PawnDef, true);
        pawn.PawnType = request.PawnType;
        pawn.Initialize();
        RegisterTraits(pawn);
        pawn.Biography.Name = request.PawnName;

        GenerateBody(pawn, request.BodySizeFactor);

        RegisterEquipment(pawn, request.Loadout.EquipmentItems);
        RegisterInventory(pawn, request.Loadout.InventoryItems);

        return pawn;
    }

    public static void RegisterSkills(Pawn pawn, List<SkillValueRecord> skills)
    {
        foreach (var record in skills)
        {
            pawn.Skills.GetSkill(record.Def).Level = record.Value;
        }
        //var skills = pawn.Skills.InRandomOrder().ToList();
        //skills[0].Level = new RangeInt(2, 4).RandomValue;
        //skills[1].Level = new RangeInt(2, 4).RandomValue;
        /*RangeInt range = new(0, 3);
        foreach (Skill skill in pawn.Skills) {
            skill.Level = range.RandomValue;
        }*/
    }

    private static void RegisterTraits(Pawn pawn)
    {
        var numberOfTraits = new RangeInt(2, 2).RandomValue;
        foreach (var def in DefRepository<TraitDef>.Defs.InRandomOrder().Take(numberOfTraits))
        {
            pawn.Traits.Add(def);
        }
    }

    public static void RegisterInventory(Pawn pawn, List<ItemDropCount> items)
    {
        foreach (var dropCount in items)
        {
            if (Core.Random.Chance(dropCount.ChanceToDrop))
            {
                var amount = dropCount.Amount.RandomValue;
                if (amount > 1 && dropCount.Item.StackLimit == 1)
                {
                    for (var i = 0; i < amount; i++)
                    {
                        pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(dropCount.Item));
                    }
                }
                else
                {
                    pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(dropCount.Item, dropCount.Amount.RandomValue));
                }
            }
        }
    }

    private static void GenerateBody(Pawn pawn, float bodySizeFactor)
    {
        pawn.GenerateBody(bodySizeFactor);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (pawn.Body.AllParts.Sum(p => p.BodyPartDef.MobilityFraction) != 1)
        {
            Log.Error(pawn.Label + " has a body that does not add up to 1 mobility");
        }

        pawn.Body.RequiresLungs = pawn.Body.AllParts.Any(p => p.BodyPartDef == Defs.BodyParts.Lung);
    }

    public static void RegisterEquipment(Pawn pawn, List<ItemDef> equipment)
    {
        Item? returnedItem = null;
        foreach (var itemDef in equipment)
        {
            var item = EntityGenerator.CreateEntity<Item>(itemDef, 1);
            foreach (var bodyPart in pawn.Body.AllExternalParts)
            {
                if (bodyPart.EmptySlotFor(item) is not { } slot)
                {
                    continue;
                }

                pawn.Equipment.TryEquip(
                    bodyPart,
                    slot,
                    item
                );
                break;
            }

            if (returnedItem != null)
            {
                Log.Error($"{returnedItem} was returned while attempting to equip on {pawn} PawnGenerator.RegisterTools");
            }
        }
    }
}

public struct PawnRequest
{
    public string PawnName { get; }
    public PawnDef PawnDef { get; }
    public PawnLoadoutDef Loadout { get; }
    public PawnType PawnType { get; set; } = PawnType.Invalid;
    public float BodySizeFactor { get; set; } = 1;

    public PawnRequest(string pawnName, PawnDef pawnDef, PawnLoadoutDef loadout, PawnType pawnType, float bodySizeFactor = 1)
    {
        PawnName = pawnName;
        PawnDef = pawnDef;
        Loadout = loadout;
        PawnType = pawnType;
        BodySizeFactor = bodySizeFactor;
    }
}