using Wendlemire.Definitions;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Equipment;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;

namespace Wendlemire.NetCode;

public static class BuildGenerator
{
    public enum Archetype
    {
        Bruiser,
        Dualist,
        Warden,
        Skirmisher,
        Hexer,
        Sage
    }

    public static IReadOnlyList<BuildSnapshot> GenerateSet(int seed, int perStage = BuildCatalog.BuildsPerStage)
    {
        var builds = new List<BuildSnapshot>(BuildStages.Generated.Length * perStage);
        foreach (var stage in BuildStages.Generated)
        {
            var rng = new Random(Hash(seed, (int)stage));
            var archetypes = (Archetype[])Enum.GetValues(typeof(Archetype));
            for (var i = 0; i < perStage; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                builds.Add(Generate(stage, archetype, i + 1, rng));
            }
        }

        return builds;
    }

    public static BuildSnapshot Generate(BuildStage stage, Archetype archetype, int index, Random rng)
    {
        var pool = StagePool.For(stage);
        var weapons = PickWeapons(pool, archetype, rng);
        var armor = PickArmor(pool, archetype, rng);
        var cloak = PickCloak(pool, archetype, rng);
        var kit = StageKit.For(stage, archetype, pool, rng);
        var trinkets = PickTrinkets(pool, stage, rng);
        var sockets = Enchant(weapons, armor, pool, rng);

        var items = new List<string>();
        items.AddRange(weapons);
        items.AddRange(armor);
        if (cloak != null)
        {
            items.Add(cloak);
        }

        items.AddRange(kit.Potions.Select(p => p.ItemMoniker));
        items.AddRange(trinkets);

        var name = NameOf(archetype, weapons, armor);
        var snapshot = new BuildSnapshot
        {
            PlayerId = "generated",
            BuildId = $"{BuildCatalog.GeneratedPrefix}{stage}/{archetype}/{index:00}",
            PawnName = name,
            PawnDefMoniker = "HumanA",
            EntityDefMonikers = items.ToArray(),
            StanceMoniker = archetype == Archetype.Warden ? "Defensive" : "Offensive",
            Weapons = weapons
                .Select(w => new WeaponConfig { ItemMoniker = w, UseInCombat = true })
                .ToArray(),
            Potions = kit.Potions,
            Sockets = sockets,
            FoodBuffs = kit.Meal,
            Meal = kit.Meal,
            Incense = kit.Incense,
            MedicalChest = kit.Medical,
            Round = stage.TargetRound()
        };

        return BuildTemplates.WithStackableStash(snapshot);
    }

    private static string[] PickWeapons(StagePool pool, Archetype archetype, Random rng)
    {
        var oneHand = pool.Weapons.Where(w => w.EquipmentProperties?.OccupiesBothHands != true).ToList();
        var twoHand = pool.Weapons.Where(w => w.EquipmentProperties?.OccupiesBothHands == true).ToList();
        var magic = pool.Weapons.Where(IsMagic).ToList();
        var melee = pool.Weapons.Where(w => !IsMagic(w)).ToList();

        return archetype switch
        {
            Archetype.Sage => [Pick(Prefer(magic, pool.Weapons), rng).Moniker],
            Archetype.Hexer => [Pick(Prefer(magic.Concat(melee.Where(w => IsUnique(w))).ToList(), melee), rng).Moniker,
                ..MaybeOffhand(oneHand, rng)],
            Archetype.Dualist => PickPair(oneHand.Count >= 2 ? oneHand : pool.Weapons, rng),
            Archetype.Warden => [Pick(Prefer(melee, pool.Weapons), rng).Moniker],
            Archetype.Skirmisher => PickPair(oneHand.Count >= 2 ? oneHand : pool.Weapons, rng),
            _ => twoHand.Count > 0 && rng.NextDouble() < 0.35
                ? [Pick(twoHand, rng).Moniker]
                : [Pick(Prefer(melee, pool.Weapons), rng).Moniker]
        };
    }

    private static string[] MaybeOffhand(List<ItemDef> oneHand, Random rng)
    {
        if (oneHand.Count < 2 || rng.NextDouble() < 0.45)
        {
            return [];
        }

        return [Pick(oneHand, rng).Moniker];
    }

    private static string[] PickPair(List<ItemDef> weapons, Random rng)
    {
        if (weapons.Count == 0)
        {
            return [];
        }

        var first = Pick(weapons, rng);
        var rest = weapons.Where(w => w != first).ToList();
        var second = rest.Count > 0 ? Pick(rest, rng) : first;
        return [first.Moniker, second.Moniker];
    }

    private static string[] PickArmor(StagePool pool, Archetype archetype, Random rng)
    {
        if (pool.ArmorSets.Count == 0)
        {
            return [];
        }

        var preferred = archetype switch
        {
            Archetype.Warden => pool.ArmorSets.Where(s => s.IsHeavy).ToList(),
            Archetype.Skirmisher or Archetype.Dualist => pool.ArmorSets.Where(s => !s.IsHeavy).ToList(),
            Archetype.Sage or Archetype.Hexer => pool.ArmorSets.Where(s => s.IsMystic || !s.IsHeavy).ToList(),
            _ => pool.ArmorSets
        };

        var set = Pick(preferred.Count > 0 ? preferred : pool.ArmorSets, rng);
        var pieces = set.Pieces.ToList();

        if (pool.Uniques.Count > 0 && (archetype is Archetype.Hexer or Archetype.Warden) && rng.NextDouble() < 0.7)
        {
            foreach (var unique in pool.Uniques.OrderBy(_ => rng.Next()))
            {
                var slot = unique.EquipmentProperties?.SlotUsedToEquip;
                if (slot is null or EquipmentSlotType.Invalid or EquipmentSlotType.Cloak)
                {
                    continue;
                }

                pieces.RemoveAll(p =>
                    DefRepository<ItemDef>.GetByMoniker(p, raiseError: false)
                        ?.EquipmentProperties?.SlotUsedToEquip == slot);
                pieces.Add(unique.Moniker);
            }
        }

        return pieces.ToArray();
    }

    private static string? PickCloak(StagePool pool, Archetype _, Random rng)
    {
        if (pool.Cloaks.Count == 0)
        {
            return null;
        }

        return Pick(pool.Cloaks, rng).Moniker;
    }

    private static string[] PickTrinkets(StagePool pool, BuildStage stage, Random rng)
    {
        if (pool.Trinkets.Count == 0 || stage == BuildStage.Early)
        {
            return [];
        }

        var count = stage == BuildStage.Mid ? 1 : rng.Next(1, 3);
        return pool.Trinkets
            .OrderBy(_ => rng.Next())
            .Take(Math.Min(count, pool.Trinkets.Count))
            .Select(t => t.Moniker)
            .ToArray();
    }

    private static SocketedItemConfig[] Enchant(
        string[] weapons,
        string[] armor,
        StagePool pool,
        Random rng)
    {
        if (pool.Enchantments.Count == 0)
        {
            return [];
        }

        var sockets = new List<SocketedItemConfig>();
        foreach (var moniker in weapons.Concat(armor))
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            var slots = def?.EquipmentProperties?.MaxEnchantments ?? 0;
            if (slots <= 0)
            {
                continue;
            }

            var type = def!.EquipmentProperties!.EquipmentType;
            var valid = pool.Enchantments
                .Where(e => e.EnchantmentProperties?.ValidEquipmentTypes.Contains(type) != false)
                .ToList();
            if (valid.Count == 0)
            {
                continue;
            }

            var picked = new List<string>();
            for (var i = 0; i < slots; i++)
            {
                picked.Add(Pick(valid, rng).Moniker);
            }

            sockets.Add(new SocketedItemConfig
            {
                ItemMoniker = moniker,
                EnchantmentMonikers = picked.ToArray()
            });
        }

        return sockets.ToArray();
    }

    private static string NameOf(Archetype archetype, string[] weapons, string[] armor)
    {
        var weaponRoot = Root(weapons.FirstOrDefault());
        var armorRoot = ArmorRoot(armor);
        return archetype switch
        {
            Archetype.Warden => $"{armorRoot} Warden",
            Archetype.Dualist => $"Twin {weaponRoot}",
            Archetype.Skirmisher => $"{armorRoot} Raider",
            Archetype.Hexer => $"{weaponRoot} Hexer",
            Archetype.Sage => $"{weaponRoot} Sage",
            _ => $"{weaponRoot} Bruiser"
        };
    }

    private static string Root(string? moniker)
    {
        if (string.IsNullOrEmpty(moniker))
        {
            return "Iron";
        }

        foreach (var prefix in new[]
                 {
                     "StrangeWithered", "Blood", "Bone", "Stone", "Wood", "Iron", "Steel",
                     "Fire", "Storm", "Ember", "Hex", "WitchDoctor", "Chain", "Leather", "Cloth"
                 })
        {
            if (moniker.StartsWith(prefix, StringComparison.Ordinal))
            {
                return prefix == "StrangeWithered" ? "Twig" : prefix;
            }
        }

        return moniker;
    }

    private static string ArmorRoot(string[] armor)
    {
        if (armor.Any(a => a.StartsWith("WitchDoctor", StringComparison.Ordinal)))
        {
            return "Witch";
        }

        if (armor.Any(a => a.StartsWith("Chain", StringComparison.Ordinal)))
        {
            return "Chain";
        }

        if (armor.Any(a => a.StartsWith("Leather", StringComparison.Ordinal)))
        {
            return "Leather";
        }

        if (armor.Any(a => a.StartsWith("Cloth", StringComparison.Ordinal)))
        {
            return "Cloth";
        }

        return "Iron";
    }

    private static bool IsMagic(ItemDef def)
    {
        var type = def.WeaponProperties?.WeaponType;
        return type is WeaponType.Staff or WeaponType.FireStaff or WeaponType.StormStaff
            or WeaponType.Wand or WeaponType.EmberWand or WeaponType.HexWand or WeaponType.Branch;
    }

    private static bool IsUnique(ItemDef def) =>
        def.Moniker is "BloodSuckler" or "StrangeWitheredTwig";

    private static List<ItemDef> Prefer(IEnumerable<ItemDef> preferred, List<ItemDef> fallback)
    {
        var list = preferred.Where(d => d != null).Distinct().ToList();
        return list.Count > 0 ? list : fallback;
    }

    private static T Pick<T>(IReadOnlyList<T> items, Random rng)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot pick from an empty pool.");
        }

        return items[rng.Next(items.Count)];
    }

    private static int Hash(int seed, int salt) =>
        unchecked((seed * 397) ^ (salt * 7919));

    private sealed record ArmorSet(string Label, string[] Pieces, bool IsHeavy, bool IsMystic);

    private sealed class StagePool
    {
        public List<ItemDef> Weapons { get; } = [];
        public List<ArmorSet> ArmorSets { get; } = [];
        public List<ItemDef> Cloaks { get; } = [];
        public List<ItemDef> Enchantments { get; } = [];
        public List<ItemDef> Trinkets { get; } = [];
        public List<ItemDef> Uniques { get; } = [];
        public List<ItemDef> Potions { get; } = [];
        public List<ItemDef> Food { get; } = [];
        public List<ItemDef> Incense { get; } = [];
        public List<ItemDef> Medical { get; } = [];

        public static StagePool For(BuildStage stage)
        {
            var round = stage.TargetRound();
            var pool = new StagePool();
            var seenSets = new HashSet<string>(StringComparer.Ordinal);

            foreach (var merchant in DefRepository<MerchantDef>.Defs)
            {
                foreach (var offer in merchant.AllOffers)
                {
                    if (!offer.IsAvailable(round))
                    {
                        continue;
                    }

                    if (offer.IsSet && offer.SetPieces.Count > 0)
                    {
                        var key = offer.SetLabel ?? string.Join(",", offer.SetPieces.Select(p => p.Moniker));
                        if (seenSets.Add(key) && MatchesArmorTier(offer.SetPieces[0], stage))
                        {
                            pool.ArmorSets.Add(ToArmorSet(offer.SetLabel ?? "Set", offer.SetPieces));
                        }
                    }

                    foreach (var def in offer.GrantedItems)
                    {
                        if (def == null || string.IsNullOrEmpty(def.Moniker) || def.Moniker == "undefined")
                        {
                            continue;
                        }

                        SortItem(pool, def, stage);
                    }
                }
            }

            AddExtras(pool, stage);

            if (pool.ArmorSets.Count == 0)
            {
                foreach (var prefix in ArmorPrefixesFor(stage))
                {
                    var pieces = SynthesizeSet(prefix);
                    if (pieces.Length > 0)
                    {
                        pool.ArmorSets.Add(ToArmorSet(prefix, pieces.Select(p =>
                            DefRepository<ItemDef>.GetByMoniker(p, raiseError: false)!).Where(d => d != null).ToList()));
                    }
                }
            }

            return pool;
        }

        private static void SortItem(StagePool pool, ItemDef def, BuildStage stage)
        {
            var slot = def.EquipmentProperties?.SlotUsedToEquip;
            if (def.EquipmentProperties?.EquipmentType == EquipmentType.Weapon
                && slot != EquipmentSlotType.BuiltIn
                && MatchesWeaponTier(def, stage))
            {
                AddUnique(pool.Weapons, def);
                return;
            }

            if (def.EquipmentProperties?.EquipmentType == EquipmentType.Armor && slot == EquipmentSlotType.Cloak)
            {
                AddUnique(pool.Cloaks, def);
                return;
            }

            if (def.ItemType == ItemType.Enchantment && MatchesEnchantTier(def, stage))
            {
                AddUnique(pool.Enchantments, def);
                return;
            }

            if (def.ItemType == ItemType.Trinket && stage != BuildStage.Early)
            {
                AddUnique(pool.Trinkets, def);
                return;
            }

            if (def.ItemType == ItemType.Potion)
            {
                AddUnique(pool.Potions, def);
                return;
            }

            if (def.ItemType == ItemType.Food)
            {
                AddUnique(pool.Food, def);
                return;
            }

            if (def.ItemType == ItemType.Incense)
            {
                AddUnique(pool.Incense, def);
                return;
            }

            if (def.ItemType == ItemType.Medical)
            {
                AddUnique(pool.Medical, def);
                return;
            }

            if (def.Moniker is "PlagueMask" or "BlessedIronCollar")
            {
                AddUnique(pool.Uniques, def);
            }
        }

        private static void AddExtras(StagePool pool, BuildStage stage)
        {
            if (stage is BuildStage.Late or BuildStage.End)
            {
                TryAdd(pool.Weapons, "BloodSuckler");
                TryAdd(pool.Weapons, "StrangeWitheredTwig");
                TryAdd(pool.Uniques, "PlagueMask");
                TryAdd(pool.Uniques, "BlessedIronCollar");
            }

            if (stage == BuildStage.End)
            {
                TryAdd(pool.Cloaks, "ThornCloak");
                TryAdd(pool.Cloaks, "ClericCloak");
                TryAdd(pool.Cloaks, "NinjaCloak");
                TryAdd(pool.Potions, "Fleshify");
            }
        }

        private static void TryAdd(List<ItemDef> list, string moniker)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            if (def != null)
            {
                AddUnique(list, def);
            }
        }

        private static void AddUnique(List<ItemDef> list, ItemDef def)
        {
            if (list.All(d => d.Moniker != def.Moniker))
            {
                list.Add(def);
            }
        }

        private static bool MatchesArmorTier(ItemDef piece, BuildStage stage)
        {
            var moniker = piece.Moniker;
            return stage switch
            {
                BuildStage.Early => moniker.StartsWith("Cloth") || moniker.StartsWith("Leather"),
                BuildStage.Mid => moniker.StartsWith("Leather") || moniker.StartsWith("Chain"),
                BuildStage.Late => moniker.StartsWith("Chain") || moniker.StartsWith("WitchDoctor"),
                _ => moniker.StartsWith("WitchDoctor") || moniker.StartsWith("Chain") || moniker.StartsWith("Leather")
            };
        }

        private static bool MatchesWeaponTier(ItemDef def, BuildStage stage)
        {
            var moniker = def.Moniker;
            var steel = moniker.StartsWith("Steel") || moniker is "Greatsword" or "Maul" or "Poleaxe";
            return stage switch
            {
                BuildStage.Early => !steel && !moniker.Contains("Staff"),
                BuildStage.Mid => !steel,
                _ => true
            };
        }

        private static bool MatchesEnchantTier(ItemDef def, BuildStage stage)
        {
            var late = def.Moniker is "EverburningStone" or "RhinoSkin";
            var mid = def.Moniker is "BoneEater" or "BloodBath";
            return stage switch
            {
                BuildStage.Early => !late && !mid,
                BuildStage.Mid => !late,
                _ => true
            };
        }

        private static string[] ArmorPrefixesFor(BuildStage stage) => stage switch
        {
            BuildStage.Early => ["Cloth", "Leather"],
            BuildStage.Mid => ["Leather", "Chain"],
            BuildStage.Late => ["Chain", "WitchDoctor"],
            _ => ["WitchDoctor", "Chain"]
        };

        private static string[] SynthesizeSet(string prefix)
        {
            var core = new[] { $"{prefix}Helmet", $"{prefix}Gorget", $"{prefix}Tunic" };
            var limbs = new[] { $"{prefix}Glove", $"{prefix}Vambrace", $"{prefix}Greave", $"{prefix}Boot" };
            var pieces = new List<string>();
            foreach (var moniker in core)
            {
                if (DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false) != null)
                {
                    pieces.Add(moniker);
                }
            }

            foreach (var moniker in limbs)
            {
                if (DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false) != null)
                {
                    pieces.Add(moniker);
                    pieces.Add(moniker);
                }
            }

            return pieces.ToArray();
        }

        private static ArmorSet ToArmorSet(string label, IReadOnlyList<ItemDef> pieces)
        {
            var monikers = pieces.Select(p => p.Moniker).ToArray();
            var isHeavy = monikers.Any(m => m.StartsWith("Chain") || m.StartsWith("WitchDoctor"));
            var isMystic = monikers.Any(m => m.StartsWith("WitchDoctor") || m.StartsWith("Cloth"));
            return new ArmorSet(label, ExpandPairs(monikers), isHeavy, isMystic);
        }

        private static string[] ExpandPairs(string[] pieces)
        {
            var limbs = new HashSet<string>(StringComparer.Ordinal)
            {
                "Glove", "Vambrace", "Greave", "Boot"
            };
            var result = new List<string>();
            var counts = pieces.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count());
            foreach (var piece in pieces.Distinct())
            {
                var count = counts[piece];
                var isLimb = limbs.Any(piece.EndsWith);
                var copies = isLimb ? Math.Max(count, 2) : count;
                for (var i = 0; i < copies; i++)
                {
                    result.Add(piece);
                }
            }

            return result.ToArray();
        }
    }

    private sealed record StageKit(
        string[] Meal,
        PotionConfig[] Potions,
        MedicalChestConfig[] Medical,
        IncenseConfig[] Incense)
    {
        public static StageKit For(BuildStage stage, Archetype archetype, StagePool pool, Random rng)
        {
            var kit = stage switch
            {
                BuildStage.Early => new StageKit(
                    Food(["CookedMeat", "CookedFish"], pool),
                    [Blood("JarOfBlood", 0.25f), Immediately("StrengthPotion")],
                    [
                        Med("Suture", 3, MedicalTriggerType.PartBelowHealth, 0.6f),
                        Med("MedKit", 2, MedicalTriggerType.PartBelowHealth),
                        Med("Bandage", 3, MedicalTriggerType.PartBelowHealth, 0.7f),
                        Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid)
                    ],
                    [Stick("MullinStick")]),
                BuildStage.Mid => new StageKit(
                    Food(["HeartyStew", "DriedMeat", "CookedCorn"], pool),
                    [
                        Blood("JarOfBlood", 0.2f),
                        Seconds(PreferPotion(pool, archetype, "AcidFlask"), 5),
                        Immediately("StrengthPotion")
                    ],
                    [
                        Med("MedKit", 3, MedicalTriggerType.PartBelowHealth),
                        Med("Suture", 2, MedicalTriggerType.PartBelowHealth, 0.6f),
                        Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid),
                        Med("MendersMist", 2, MedicalTriggerType.PartBelowHealth),
                        Med("Antidote", 1, MedicalTriggerType.HasPoison),
                        Med("ClotPack", 2, MedicalTriggerType.SelfBloodBelow)
                    ],
                    [Stick("ShadeWood"), Stick("MullinStick")]),
                BuildStage.Late => new StageKit(
                    Food(["HeartyStew", "HoneyPot", "DriedMeat"], pool),
                    archetype is Archetype.Hexer or Archetype.Sage
                        ? [Seconds(PreferPotion(pool, archetype, "PussBomb"), 3), Seconds("BlackenedSmoke", 6), Blood("JarOfBlood", 0.2f)]
                        : [Blood("JarOfBlood", 0.2f), Parts("HealingPotion", 0.4f, 0.55f), Immediately("StrengthPotion")],
                    [
                        Med("MendersMix", 2, MedicalTriggerType.PartBelowHealth, 0.4f),
                        Med("MedKit", 2, MedicalTriggerType.PartBelowHealth),
                        Med("Cauterize", 1, MedicalTriggerType.PartSevered, selector: MedicalTargetSelector.SeveredOrUnsealedSocket),
                        Med("BoneCleanse", 1, MedicalTriggerType.PartBelowHealth),
                        Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid),
                        Med("MendersMist", 2, MedicalTriggerType.PartBelowHealth),
                        Med("Antidote", 1, MedicalTriggerType.HasPoison),
                        Med("ClotPack", 2, MedicalTriggerType.SelfBloodBelow)
                    ],
                    [Stick("DippedMullinStick", 3), Stick("ShadeWood"), Stick("MullinStick")]),
                _ => new StageKit(
                    Food(["HeartyStew", "Walnut", "HoneyPot", "WondrousJam"], pool),
                    archetype is Archetype.Hexer or Archetype.Sage
                        ? [Seconds("AcidFlask", 4), Seconds("PussBomb", 7), Seconds("BlackenedSmoke", 3), Blood("JarOfBlood", 0.15f)]
                        : [Blood("JarOfBlood", 0.15f), Parts("HealingPotion", 0.35f, 0.55f), Seconds("AcidFlask", 5), Immediately("StrengthPotion")],
                    [
                        Med("MendersMix", 3, MedicalTriggerType.PartBelowHealth, 0.4f),
                        Med("Cauterize", 1, MedicalTriggerType.PartSevered, selector: MedicalTargetSelector.SeveredOrUnsealedSocket),
                        Med("AntiNecroticSerum", 2, MedicalTriggerType.HasNecrosis),
                        Med("BoneCleanse", 1, MedicalTriggerType.PartBelowHealth),
                        Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid),
                        Med("MendersMist", 2, MedicalTriggerType.PartBelowHealth),
                        Med("Antidote", 1, MedicalTriggerType.HasPoison),
                        Med("ClotPack", 2, MedicalTriggerType.SelfBloodBelow)
                    ],
                    [Stick("DippedMullinStick", 3), Stick("WitchWood"), Stick("MullinStick")])
            };

            return PadKit(kit, stage, archetype, pool, rng);
        }

        private static StageKit PadKit(StageKit kit, BuildStage stage, Archetype archetype, StagePool pool, Random rng)
        {
            var caps = PrepSlotUnlocks.ForRound(stage.TargetRound());
            var food = kit.Meal.ToList();
            Pad(food, caps.Food, pool.Food.Select(d => d.Moniker), rng);
            if (food.Count == 0)
            {
                food.AddRange(new[] { "CookedMeat", "CookedFish" }.Where(Exists));
            }

            var potions = kit.Potions.ToList();
            foreach (var moniker in PotionFill(archetype, pool, rng))
            {
                if (potions.Count >= caps.Potion)
                {
                    break;
                }

                if (potions.All(p => p.ItemMoniker != moniker) && Exists(moniker))
                {
                    potions.Add(DefaultPotion(moniker, potions.Count));
                }
            }

            var incense = kit.Incense.ToList();
            foreach (var moniker in pool.Incense.Select(d => d.Moniker).OrderBy(_ => rng.Next()))
            {
                if (incense.Count >= caps.Incense)
                {
                    break;
                }

                if (incense.All(i => i.ItemMoniker != moniker))
                {
                    incense.Add(Stick(moniker));
                }
            }

            var medical = kit.Medical.ToList();
            foreach (var moniker in MedicalFill(pool, rng))
            {
                if (medical.Count >= Math.Min(caps.Medical, 8))
                {
                    break;
                }

                if (medical.All(m => m.ItemMoniker != moniker) && Exists(moniker))
                {
                    medical.Add(DefaultMedical(moniker));
                }
            }

            return new StageKit(food.ToArray(), potions.ToArray(), medical.ToArray(), incense.ToArray());
        }

        private static void Pad(List<string> dest, int cap, IEnumerable<string> extras, Random rng)
        {
            foreach (var moniker in extras.OrderBy(_ => rng.Next()))
            {
                if (dest.Count >= cap)
                {
                    return;
                }

                if (!dest.Contains(moniker) && Exists(moniker))
                {
                    dest.Add(moniker);
                }
            }
        }

        private static IEnumerable<string> PotionFill(Archetype archetype, StagePool pool, Random rng)
        {
            var preferred = archetype is Archetype.Hexer or Archetype.Sage
                ? new[] { "PussBomb", "BlackenedSmoke", "AcidFlask", "JarOfBlood", "HealingPotion" }
                : new[] { "JarOfBlood", "HealingPotion", "StrengthPotion", "AcidFlask", "HealingFlask" };
            return preferred.Concat(pool.Potions.Select(p => p.Moniker).OrderBy(_ => rng.Next()));
        }

        private static IEnumerable<string> MedicalFill(StagePool pool, Random rng)
        {
            var preferred = new[]
            {
                "MedKit", "Suture", "MendersMist", "BalmyOintment", "Cauterize",
                "MendersMix", "Antidote", "ClotPack", "BoneCleanse", "Bandage"
            };
            return preferred.Concat(pool.Medical.Select(m => m.Moniker).OrderBy(_ => rng.Next()));
        }

        private static PotionConfig DefaultPotion(string moniker, int index) => moniker switch
        {
            "StrengthPotion" or "Fleshify" => Immediately(moniker),
            "JarOfBlood" => Blood(moniker, 0.2f),
            "HealingPotion" or "HealingFlask" or "HealingSalve" => Parts(moniker, 0.4f, 0.55f),
            _ => Seconds(moniker, 4 + index * 2)
        };

        private static MedicalChestConfig DefaultMedical(string moniker) => moniker switch
        {
            "Cauterize" => Med(moniker, 1, MedicalTriggerType.PartSevered, selector: MedicalTargetSelector.SeveredOrUnsealedSocket),
            "BalmyOintment" => Med(moniker, 2, MedicalTriggerType.BurningOrAcid),
            "AntiNecroticSerum" => Med(moniker, 2, MedicalTriggerType.HasNecrosis),
            "Antidote" => Med(moniker, 1, MedicalTriggerType.HasPoison),
            "ClotPack" => new MedicalChestConfig
            {
                ItemMoniker = moniker,
                Charges = 2,
                Type = MedicalTriggerType.SelfBloodBelow,
                Threshold = 0.25f
            },
            "Suture" or "Bandage" => Med(moniker, 3, MedicalTriggerType.PartBelowHealth, 0.6f),
            "MendersMix" or "MendersMist" => Med(moniker, 2, MedicalTriggerType.PartBelowHealth, 0.4f),
            _ => Med(moniker, 2, MedicalTriggerType.PartBelowHealth)
        };

        private static bool Exists(string moniker) =>
            DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false) != null;

        private static string[] Food(string[] preferred, StagePool pool)
        {
            var available = preferred
                .Where(m => pool.Food.Any(f => f.Moniker == m)
                            || DefRepository<ItemDef>.GetByMoniker(m, raiseError: false) != null)
                .ToArray();
            return available.Length > 0 ? available : preferred;
        }

        private static string PreferPotion(StagePool pool, Archetype archetype, string fallback)
        {
            if (archetype is Archetype.Hexer or Archetype.Sage)
            {
                foreach (var moniker in new[] { "PussBomb", "BlackenedSmoke", "AcidFlask" })
                {
                    if (pool.Potions.Any(p => p.Moniker == moniker))
                    {
                        return moniker;
                    }
                }
            }

            return fallback;
        }

        private static PotionConfig Immediately(string moniker) => new()
        {
            ItemMoniker = moniker,
            Type = PotionTriggerType.Immediately
        };

        private static PotionConfig Seconds(string moniker, float after) => new()
        {
            ItemMoniker = moniker,
            Type = PotionTriggerType.AfterSeconds,
            AfterSeconds = after
        };

        private static PotionConfig Blood(string moniker, float threshold) => new()
        {
            ItemMoniker = moniker,
            Type = PotionTriggerType.SelfBloodBelow,
            Threshold = threshold
        };

        private static PotionConfig Parts(string moniker, float threshold, float health) => new()
        {
            ItemMoniker = moniker,
            Type = PotionTriggerType.SelfPartsDamaged,
            Threshold = threshold,
            HealthThreshold = health
        };

        private static MedicalChestConfig Med(
            string moniker,
            int charges,
            MedicalTriggerType type,
            float health = 0.5f,
            MedicalTargetSelector selector = MedicalTargetSelector.Auto) =>
            new()
            {
                ItemMoniker = moniker,
                Charges = charges,
                Type = type,
                TargetSelector = selector,
                HealthThreshold = health
            };

        private static IncenseConfig Stick(string moniker, int encounters = 2) =>
            new() { ItemMoniker = moniker, EncountersRemaining = encounters };
    }
}
