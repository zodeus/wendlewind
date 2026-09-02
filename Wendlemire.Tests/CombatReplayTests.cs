using Microsoft.Extensions.DependencyInjection;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class CombatReplayTests
{
    public CombatReplayTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void SameSeedAgrees()
    {
        CombatReplay.AssertDeterministic();
    }

    [Fact]
    public void SaveLoadRoundTripKeepsRun()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wendlemire-saveload-{Guid.NewGuid():N}.xml");
        try
        {
            var first = CombatReplay.Run();

            using var root = SimServices.BuildRoot();
            using var scope = root.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            context.Initialize(CombatReplay.DefaultRunSeed);
            context.Save(path);

            using var loadScope = root.CreateScope();
            var loaded = loadScope.ServiceProvider.GetRequiredService<GameContext>();
            loaded.Load(path);

            Assert.Equal(CombatReplay.DefaultRunSeed, loaded.RunSeed);
            Assert.NotNull(loaded.PlayerPawn);
            Assert.False(loaded.PlayerPawn.IsDead);

            var zone = loaded.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
            loaded.EnterZone(zone.ZoneDef);
            loaded.CurrentZone!.NextEncounter();
            int guard = 0;
            while (loaded.CurrentZone.ActiveEncounter!.State == EncounterState.InProgress &&
                   guard < CombatReplay.MaxTicks)
            {
                loaded.Tick();
                guard++;
            }

            Assert.True(guard > 0);
            Assert.NotEqual(EncounterState.InProgress, loaded.CurrentZone.ActiveEncounter.State);
            Assert.Equal(first.ZoneMoniker, zone.ZoneDef.Moniker);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void SnapshotHydrateSimulateAgrees()
    {
        var snapshot = BuildTemplates.TankRegen();
        CombatReplay.AssertDeterministic(
            CombatReplay.DefaultRunSeed,
            context => BuildSnapshotFactory.Apply(context.PlayerPawn, snapshot));
    }

    [Fact]
    public void LivePawnSnapshotRoundTripAgrees()
    {
        BuildSnapshot snapshot;
        using (var root = SimServices.BuildRoot())
        using (var scope = root.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            context.Initialize(CombatReplay.DefaultRunSeed);
            BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.Glasscannon());
            snapshot = BuildSnapshotFactory.ToSnapshot(context.PlayerPawn, "player", "roundtrip", CombatReplay.DefaultRunSeed);
        }

        CombatReplay.AssertDeterministic(
            CombatReplay.DefaultRunSeed,
            context => BuildSnapshotFactory.Apply(context.PlayerPawn, snapshot));
    }

    [Fact]
    public void SnapshotRoundTripKeepsPairedArmor()
    {
        using var root = SimServices.BuildRoot();
        using var applyScope = root.CreateScope();
        var applyContext = applyScope.ServiceProvider.GetRequiredService<GameContext>();
        applyContext.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(applyContext.PlayerPawn, BuildTemplates.LeatherSkirmisher());

        var snapshot = BuildSnapshotFactory.ToSnapshot(applyContext.PlayerPawn, "player", "pairs", CombatReplay.DefaultRunSeed);
        Assert.Equal(2, snapshot.EntityDefMonikers.Count(m => m == "LeatherGlove"));
        Assert.Equal(2, snapshot.EntityDefMonikers.Count(m => m == "LeatherBoot"));
        Assert.Equal(2, snapshot.EntityDefMonikers.Count(m => m == "LeatherVambrace"));
        Assert.Equal(2, snapshot.EntityDefMonikers.Count(m => m == "LeatherGreave"));

        using var hydrateScope = root.CreateScope();
        var hydrateContext = hydrateScope.ServiceProvider.GetRequiredService<GameContext>();
        hydrateContext.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(hydrateContext.PlayerPawn, snapshot);

        Assert.Equal(2, hydrateContext.PlayerPawn.Equipment.Count(i => i.Def.Moniker == "LeatherGlove"));
        Assert.Equal(2, hydrateContext.PlayerPawn.Equipment.Count(i => i.Def.Moniker == "LeatherBoot"));
        Assert.Equal(2, hydrateContext.PlayerPawn.Equipment.Count(i => i.Def.Moniker == "LeatherVambrace"));
        Assert.Equal(2, hydrateContext.PlayerPawn.Equipment.Count(i => i.Def.Moniker == "LeatherGreave"));
    }

    [Fact]
    public void ApplyKeepsWeaponCombatUseFromSnapshot()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var snapshot = BuildTemplates.DualFury() with
        {
            Weapons =
            [
                new WeaponConfig { ItemMoniker = "IronClaws", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "IronAxe", UseInCombat = false }
            ]
        };
        BuildSnapshotFactory.Apply(context.PlayerPawn, snapshot);

        var axe = context.PlayerPawn.Equipment.Weapons
            .Select(w => w.Item1)
            .Single(w => w.Def.Moniker == "IronAxe");
        var claws = context.PlayerPawn.Equipment.Weapons
            .Select(w => w.Item1)
            .Single(w => w.Def.Moniker == "IronClaws");
        Assert.False(axe.UseInCombat);
        Assert.True(claws.UseInCombat);
    }

    [Fact]
    public void AllTemplatesHydrateLoadout()
    {
        using var root = SimServices.BuildRoot();
        foreach (var template in BuildTemplates.All)
        {
            using var scope = root.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            context.Initialize(CombatReplay.DefaultRunSeed);
            BuildSnapshotFactory.Apply(context.PlayerPawn, template);

            var pawn = context.PlayerPawn;
            Assert.False(pawn.IsDead, template.BuildId);
            foreach (var pair in template.EntityDefMonikers.GroupBy(m => m).Where(g => g.Count() > 1))
            {
                var equipped = pawn.Equipment.Count(i => i.Def.Moniker == pair.Key);
                Assert.True(equipped >= pair.Count(),
                    $"{template.BuildId} should equip {pair.Count()} {pair.Key}, equipped {equipped}");
            }

            foreach (var weapon in template.Weapons)
            {
                Assert.Contains(pawn.Equipment.Weapons, w => w.Item1.Def.Moniker == weapon.ItemMoniker);
            }

            foreach (var potion in template.Potions)
            {
                Assert.Contains(pawn.Equipment.Potions, p => p.Def.Moniker == potion.ItemMoniker);
            }

            if (template.Sockets.Length > 0)
            {
                Assert.Contains(pawn.Equipment, item => item.Enchantments != null && item.Enchantments.Any());
            }

            if (template.Meal.Length > 0 || template.FoodBuffs.Length > 0)
            {
                Assert.True(pawn.MealPlan.Items.Count > 0, $"{template.BuildId} should have a configured meal");
            }

            Assert.True(template.Inventory.Length > 0, $"{template.BuildId} should include a full inventory");
            Assert.Contains(pawn.Inventory, i => i.StackSize >= BuildTemplates.FullInventoryStack);

            foreach (var trinketMoniker in BuildTemplates.AllTrinkets())
            {
                Assert.Contains(pawn.Inventory.Trinkets, t => t.Def.Moniker == trinketMoniker);
            }

            foreach (var enchantmentMoniker in BuildTemplates.AllEnchantments())
            {
                var count = pawn.Inventory.Count(i => i.Def.Moniker == enchantmentMoniker);
                Assert.True(count >= BuildTemplates.EnchantmentCopies,
                    $"{template.BuildId} should include {BuildTemplates.EnchantmentCopies} {enchantmentMoniker}");
            }
        }
    }
}
