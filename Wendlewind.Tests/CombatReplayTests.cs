using Microsoft.Extensions.DependencyInjection;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Xunit;

namespace Wendlewind.Tests;

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
        var path = Path.Combine(Path.GetTempPath(), $"wendlewind-saveload-{Guid.NewGuid():N}.xml");
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
