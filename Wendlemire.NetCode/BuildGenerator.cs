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
        var round = stage.TargetRound();
        var wallet = new Wallet(ArenaEconomy.BuildBudget(round));
        var kitReserve = KitReserve(stage);
        var pool = StagePool.For(stage);
        var weapons = BuyWeapons(pool, archetype, wallet, rng);
        var armor = BuyArmor(pool, archetype, wallet, kitReserve, rng);
        var sockets = BuyEnchantments(weapons, armor, pool, wallet, kitReserve, stage, rng);
        var kit = StageKit.For(stage, archetype, pool, wallet, rng);
        var cloak = BuyCloak(pool, wallet, rng);
        var trinkets = BuyTrinkets(pool, stage, wallet, rng);

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
        return new BuildSnapshot
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
            Round = round,
            GoldSpent = wallet.Spent
        };
    }

    public static int ComputeGoldSpent(BuildSnapshot snapshot)
    {
        var spent = 0;
        var counted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var moniker in snapshot.EntityDefMonikers
                     .Concat(snapshot.Weapons.Select(w => w.ItemMoniker))
                     .Concat(snapshot.Potions.Select(p => p.ItemMoniker))
                     .Concat(snapshot.Meal.Length > 0 ? snapshot.Meal : snapshot.FoodBuffs)
                     .Concat(snapshot.MedicalChest.Select(m => m.ItemMoniker))
                     .Concat(snapshot.Incense.Select(i => i.ItemMoniker)))
        {
            if (!counted.Add(moniker))
            {
                continue;
            }

            spent += CostOf(moniker);
        }

        foreach (var socket in snapshot.Sockets)
        {
            foreach (var enchant in socket.EnchantmentMonikers)
            {
                spent += CostOf(enchant);
            }
        }

        return spent;
    }

    private static int KitReserve(BuildStage stage) => stage switch
    {
        BuildStage.Early => 70,
        BuildStage.Mid => 120,
        BuildStage.Late => 160,
        _ => 200
    };

    private static int MaxEnchantments(BuildStage stage) => stage switch
    {
        BuildStage.Early => 1,
        BuildStage.Mid => 3,
        BuildStage.Late => 4,
        _ => 5
    };

    private static string[] BuyWeapons(StagePool pool, Archetype archetype, Wallet wallet, Random rng)
    {
        var oneHand = pool.Weapons.Where(w => w.EquipmentProperties?.OccupiesBothHands != true).ToList();
        var twoHand = pool.Weapons.Where(w => w.EquipmentProperties?.OccupiesBothHands == true).ToList();
        var magic = pool.Weapons.Where(IsMagic).ToList();
        var melee = pool.Weapons.Where(w => !IsMagic(w)).ToList();

        var chosen = archetype switch
        {
            Archetype.Sage => BuyOne(Prefer(magic, pool.Weapons), wallet, rng),
            Archetype.Hexer => BuyOne(
                Prefer(magic.Concat(melee.Where(IsUnique)).ToList(), melee), wallet, rng),
            Archetype.Dualist => BuyPair(oneHand.Count >= 2 ? oneHand : pool.Weapons, wallet, rng),
            Archetype.Warden => BuyOne(Prefer(melee, pool.Weapons), wallet, rng),
            Archetype.Skirmisher => BuyPair(oneHand.Count >= 2 ? oneHand : pool.Weapons, wallet, rng),
            _ => twoHand.Count > 0 && rng.NextDouble() < 0.35
                ? BuyOne(twoHand, wallet, rng)
                : BuyOne(Prefer(melee, pool.Weapons), wallet, rng)
        };

        if (archetype == Archetype.Hexer && chosen.Length == 1)
        {
            var offhand = BuyOffhand(oneHand, chosen[0], wallet, rng);
            if (offhand != null)
            {
                return [chosen[0], offhand];
            }
        }

        return chosen;
    }

    private static string[] BuyOne(List<ItemDef> weapons, Wallet wallet, Random rng)
    {
        var bought = TryBuyAffordable(weapons, wallet, rng);
        return bought == null ? [] : [bought.Moniker];
    }

    private static string? BuyOffhand(List<ItemDef> oneHand, string already, Wallet wallet, Random rng)
    {
        if (oneHand.Count < 2 || rng.NextDouble() < 0.45)
        {
            return null;
        }

        var rest = oneHand.Where(w => w.Moniker != already).ToList();
        return TryBuyAffordable(rest, wallet, rng)?.Moniker;
    }

    private static string[] BuyPair(List<ItemDef> weapons, Wallet wallet, Random rng)
    {
        if (weapons.Count == 0)
        {
            return [];
        }

        var first = TryBuyAffordable(weapons, wallet, rng);
        if (first == null)
        {
            return [];
        }

        var rest = weapons.Where(w => w != first).ToList();
        var second = TryBuyAffordable(rest.Count > 0 ? rest : weapons, wallet, rng);
        return second == null ? [first.Moniker] : [first.Moniker, second.Moniker];
    }

    private static string[] BuyArmor(StagePool pool, Archetype archetype, Wallet wallet, int kitReserve, Random rng)
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
        var pieces = BuyArmorPieces(set, wallet, kitReserve);
        if (pieces.Count == 0)
        {
            return [];
        }

        if (pool.Uniques.Count > 0 && (archetype is Archetype.Hexer or Archetype.Warden) && rng.NextDouble() < 0.7)
        {
            foreach (var unique in pool.Uniques.OrderBy(_ => rng.Next()))
            {
                var slot = unique.EquipmentProperties?.SlotUsedToEquip;
                if (slot is null or EquipmentSlotType.Invalid or EquipmentSlotType.Cloak)
                {
                    continue;
                }

                var replaced = pieces.Find(p =>
                    DefRepository<ItemDef>.GetByMoniker(p, raiseError: false)
                        ?.EquipmentProperties?.SlotUsedToEquip == slot);
                if (replaced == null || !wallet.CanAfford(unique, kitReserve))
                {
                    continue;
                }

                wallet.Refund(CostOf(replaced));
                if (!wallet.TryBuy(unique, kitReserve))
                {
                    wallet.TryBuy(DefOf(replaced), 0);
                    continue;
                }

                pieces.Remove(replaced);
                pieces.Add(unique.Moniker);
            }
        }

        return pieces.ToArray();
    }

    private static List<string> BuyArmorPieces(ArmorSet set, Wallet wallet, int kitReserve)
    {
        var defs = set.Pieces
            .Select(DefOf)
            .Where(d => d != null)
            .Cast<ItemDef>()
            .ToList();
        if (defs.Count == 0)
        {
            return [];
        }

        var setCost = ShopCatalog.ComputeSetCost(defs);
        if (wallet.CanAfford(setCost, kitReserve))
        {
            wallet.Spend(setCost);
            return set.Pieces.ToList();
        }

        var bought = new List<string>();
        foreach (var def in PrioritizeArmor(defs))
        {
            if (wallet.TryBuy(def, kitReserve))
            {
                bought.Add(def.Moniker);
            }
        }

        return bought;
    }

    private static IEnumerable<ItemDef> PrioritizeArmor(IReadOnlyList<ItemDef> pieces)
    {
        static int Rank(ItemDef def)
        {
            return def.EquipmentProperties?.SlotUsedToEquip switch
            {
                EquipmentSlotType.TorsoArmor => 0,
                EquipmentSlotType.HeadArmor => 1,
                EquipmentSlotType.NeckArmor => 2,
                EquipmentSlotType.LegArmor => 3,
                EquipmentSlotType.ArmArmor => 4,
                EquipmentSlotType.HandArmor => 5,
                EquipmentSlotType.FootArmor => 6,
                _ => 7
            };
        }

        return pieces.OrderBy(Rank);
    }

    private static string? BuyCloak(StagePool pool, Wallet wallet, Random rng)
    {
        if (pool.Cloaks.Count == 0)
        {
            return null;
        }

        return TryBuyAffordable(pool.Cloaks, wallet, rng)?.Moniker;
    }

    private static string[] BuyTrinkets(StagePool pool, BuildStage stage, Wallet wallet, Random rng)
    {
        if (pool.Trinkets.Count == 0 || stage == BuildStage.Early)
        {
            return [];
        }

        var count = stage == BuildStage.Mid ? 1 : rng.Next(1, 3);
        var bought = new List<string>();
        foreach (var def in pool.Trinkets.OrderBy(_ => rng.Next()))
        {
            if (bought.Count >= count)
            {
                break;
            }

            if (wallet.TryBuy(def))
            {
                bought.Add(def.Moniker);
            }
        }

        return bought.ToArray();
    }

    private static SocketedItemConfig[] BuyEnchantments(
        string[] weapons,
        string[] armor,
        StagePool pool,
        Wallet wallet,
        int kitReserve,
        BuildStage stage,
        Random rng)
    {
        if (pool.Enchantments.Count == 0)
        {
            return [];
        }

        var remaining = MaxEnchantments(stage);
        var sockets = new List<SocketedItemConfig>();
        foreach (var moniker in weapons.Concat(armor).OrderBy(_ => rng.Next()))
        {
            if (remaining <= 0)
            {
                break;
            }

            var def = DefOf(moniker);
            var slots = def?.EquipmentProperties?.MaxEnchantments ?? 0;
            if (def == null || slots <= 0)
            {
                continue;
            }

            var type = def.EquipmentProperties!.EquipmentType;
            var valid = pool.Enchantments
                .Where(e => e.EnchantmentProperties?.ValidEquipmentTypes.Contains(type) != false)
                .ToList();
            if (valid.Count == 0)
            {
                continue;
            }

            var picked = new List<string>();
            for (var i = 0; i < slots && remaining > 0; i++)
            {
                var enchant = TryBuyAffordable(valid, wallet, rng, kitReserve);
                if (enchant == null)
                {
                    break;
                }

                picked.Add(enchant.Moniker);
                remaining--;
            }

            if (picked.Count > 0)
            {
                sockets.Add(new SocketedItemConfig
                {
                    ItemMoniker = moniker,
                    EnchantmentMonikers = picked.ToArray()
                });
            }
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

        if (armor.Any(a => a.StartsWith("Plate", StringComparison.Ordinal)))
        {
            return "Plate";
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

    private static ItemDef? TryBuyAffordable(
        IReadOnlyList<ItemDef> items,
        Wallet wallet,
        Random rng,
        int reserve = 0)
    {
        var affordable = items.Where(d => wallet.CanAfford(d, reserve)).ToList();
        if (affordable.Count == 0)
        {
            return null;
        }

        var pick = Pick(affordable, rng);
        return wallet.TryBuy(pick, reserve) ? pick : null;
    }

    private static ItemDef? DefOf(string moniker) =>
        DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);

    private static int CostOf(string moniker) => DefOf(moniker)?.GoldCost ?? 0;

    private static int Hash(int seed, int salt) =>
        unchecked((seed * 397) ^ (salt * 7919));

    private sealed class Wallet
    {
        public int Remaining { get; private set; }
        public int Spent { get; private set; }

        public Wallet(int budget) => Remaining = Math.Max(0, budget);

        public bool CanAfford(int cost, int reserve = 0) =>
            cost <= 0 || Remaining - reserve >= cost;

        public bool CanAfford(ItemDef def, int reserve = 0) =>
            CanAfford(Math.Max(0, def.GoldCost), reserve);

        public bool TryBuy(ItemDef? def, int reserve = 0)
        {
            if (def == null)
            {
                return false;
            }

            var cost = Math.Max(0, def.GoldCost);
            if (!CanAfford(cost, reserve))
            {
                return false;
            }

            Spend(cost);
            return true;
        }

        public void Spend(int cost)
        {
            cost = Math.Max(0, cost);
            Remaining -= cost;
            Spent += cost;
        }

        public void Refund(int cost)
        {
            cost = Math.Max(0, cost);
            Remaining += cost;
            Spent = Math.Max(0, Spent - cost);
        }
    }

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
                BuildStage.Late => moniker.StartsWith("Chain") || moniker.StartsWith("WitchDoctor")
                    || moniker.StartsWith("Plate"),
                _ => moniker.StartsWith("WitchDoctor") || moniker.StartsWith("Chain")
                    || moniker.StartsWith("Leather") || moniker.StartsWith("Plate")
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
            BuildStage.Late => ["Chain", "WitchDoctor", "Plate"],
            _ => ["WitchDoctor", "Chain", "Plate"]
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
            var isHeavy = monikers.Any(m =>
                m.StartsWith("Chain") || m.StartsWith("WitchDoctor") || m.StartsWith("Plate"));
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
        public static StageKit For(BuildStage stage, Archetype archetype, StagePool pool, Wallet wallet, Random rng)
        {
            var caps = PrepSlotUnlocks.ForRound(stage.TargetRound());
            var food = new List<string>();
            foreach (var moniker in FoodPriority(stage, pool))
            {
                if (food.Count >= caps.Food)
                {
                    break;
                }

                if (TryBuyMoniker(wallet, moniker))
                {
                    food.Add(moniker);
                }
            }

            var potions = new List<PotionConfig>();
            foreach (var moniker in PotionFill(archetype, stage))
            {
                if (potions.Count >= caps.Potion)
                {
                    break;
                }

                if (potions.All(p => p.ItemMoniker != moniker) && TryBuyMoniker(wallet, moniker))
                {
                    potions.Add(DefaultPotion(moniker, potions.Count));
                }
            }

            var incense = new List<IncenseConfig>();
            foreach (var moniker in IncensePriority(stage, pool, rng))
            {
                if (incense.Count >= caps.Incense)
                {
                    break;
                }

                if (incense.All(i => i.ItemMoniker != moniker) && TryBuyMoniker(wallet, moniker))
                {
                    incense.Add(Stick(moniker));
                }
            }

            var medical = new List<MedicalChestConfig>();
            foreach (var moniker in MedicalFill(pool, rng))
            {
                if (medical.Count >= Math.Min(caps.Medical, 8))
                {
                    break;
                }

                if (medical.All(m => m.ItemMoniker != moniker) && TryBuyMoniker(wallet, moniker))
                {
                    medical.Add(DefaultMedical(moniker));
                }
            }

            return new StageKit(food.ToArray(), potions.ToArray(), medical.ToArray(), incense.ToArray());
        }

        private static bool TryBuyMoniker(Wallet wallet, string moniker)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            return def != null && wallet.TryBuy(def);
        }

        private static IEnumerable<string> FoodPriority(BuildStage stage, StagePool pool)
        {
            var preferred = stage switch
            {
                BuildStage.Early => new[] { "CookedMeat", "CookedFish" },
                BuildStage.Mid => new[] { "HeartyStew", "DriedMeat", "CookedCorn" },
                BuildStage.Late => new[] { "HeartyStew", "HoneyPot", "DriedMeat" },
                _ => new[] { "HeartyStew", "Walnut", "HoneyPot", "WondrousJam" }
            };
            return preferred.Concat(pool.Food.Select(d => d.Moniker));
        }

        private static IEnumerable<string> IncensePriority(BuildStage stage, StagePool pool, Random rng)
        {
            var preferred = stage switch
            {
                BuildStage.Early => new[] { "MullinStick" },
                BuildStage.Mid => new[] { "ShadeWood", "MullinStick" },
                BuildStage.Late => new[] { "DippedMullinStick", "ShadeWood", "MullinStick" },
                _ => new[] { "DippedMullinStick", "WitchWood", "MullinStick" }
            };
            return preferred.Concat(pool.Incense.Select(d => d.Moniker).OrderBy(_ => rng.Next()));
        }

        private static IEnumerable<string> PotionFill(Archetype archetype, BuildStage stage)
        {
            var preferred = (archetype, stage) switch
            {
                (Archetype.Hexer or Archetype.Sage, BuildStage.Early) =>
                    new[] { "JarOfBlood", "HealingPotion" },
                (Archetype.Hexer or Archetype.Sage, _) =>
                    new[] { "JarOfBlood", "AcidFlask", "HealingPotion" },
                (_, BuildStage.Early) =>
                    new[] { "JarOfBlood", "StrengthPotion" },
                _ =>
                    new[] { "JarOfBlood", "HealingPotion", "StrengthPotion" }
            };
            return preferred;
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
