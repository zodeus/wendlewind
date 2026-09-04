using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities.Items;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class BuildGeneratorTests
{
    public BuildGeneratorTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void GenerateSetProducesAllStages()
    {
        var builds = BuildGenerator.GenerateSet(123, perStage: 6);

        Assert.Equal(24, builds.Count);
        Assert.Equal(6, builds.Count(b => BuildCatalog.StageOf(b) == BuildStage.Early));
        Assert.Equal(6, builds.Count(b => BuildCatalog.StageOf(b) == BuildStage.Mid));
        Assert.Equal(6, builds.Count(b => BuildCatalog.StageOf(b) == BuildStage.Late));
        Assert.Equal(6, builds.Count(b => BuildCatalog.StageOf(b) == BuildStage.End));
        Assert.Equal(builds.Count, builds.Select(b => b.BuildId).Distinct().Count());
    }

    [Fact]
    public void SameSeedIsDeterministic()
    {
        var first = BuildGenerator.GenerateSet(777);
        var second = BuildGenerator.GenerateSet(777);

        Assert.Equal(first.Select(b => b.BuildId), second.Select(b => b.BuildId));
        Assert.Equal(
            first.Select(b => string.Join(",", b.Weapons.Select(w => w.ItemMoniker))),
            second.Select(b => string.Join(",", b.Weapons.Select(w => w.ItemMoniker))));
    }

    [Fact]
    public void EarlyBuildsStayOffSteelAndStaffs()
    {
        var early = BuildGenerator.GenerateSet(42).Where(b => BuildCatalog.StageOf(b) == BuildStage.Early);

        foreach (var build in early)
        {
            foreach (var weapon in build.Weapons)
            {
                Assert.DoesNotContain("Staff", weapon.ItemMoniker);
                Assert.False(IsSteel(weapon.ItemMoniker), build.BuildId);
            }

            Assert.DoesNotContain(build.EntityDefMonikers, m => m.StartsWith("WitchDoctor"));
            Assert.DoesNotContain(build.EntityDefMonikers, m => m.StartsWith("Chain"));
            Assert.DoesNotContain(build.EntityDefMonikers, IsTrinket);
        }
    }

    [Fact]
    public void GeneratedMonikersResolve()
    {
        foreach (var build in BuildGenerator.GenerateSet(99))
        {
            Assert.NotEmpty(build.Weapons);
            foreach (var moniker in build.EntityDefMonikers.Concat(build.Weapons.Select(w => w.ItemMoniker)))
            {
                Assert.NotNull(DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false));
            }

            Assert.NotNull(DefRepository<ItemDef>.GetByMoniker(build.Weapons[0].ItemMoniker, raiseError: false));
        }
    }

    [Fact]
    public void ApplyGeneratedBuildDoesNotThrow()
    {
        using var harness = BodyTestHarness.Human();
        var build = BuildGenerator.GenerateSet(5).First(b => BuildCatalog.StageOf(b) == BuildStage.Mid);
        BuildSnapshotFactory.Apply(harness.Pawn, build);

        Assert.False(harness.Pawn.IsDead);
        Assert.NotEmpty(harness.Pawn.Equipment.Weapons);
    }

    [Fact]
    public void GeneratedBuildsStayWithinGoldBudget()
    {
        foreach (var build in BuildGenerator.GenerateSet(8))
        {
            var budget = ArenaEconomy.BuildBudget(build.Round);
            Assert.True(build.GoldSpent <= budget, $"{build.BuildId} spent {build.GoldSpent} over budget {budget}");
            Assert.True(build.GoldSpent > 0, build.BuildId);
            Assert.Empty(build.Inventory);
        }
    }

    [Fact]
    public void LateWardenCanWearPlate()
    {
        var found = false;
        for (var seed = 1; seed <= 40 && !found; seed++)
        {
            var late = BuildGenerator.GenerateSet(seed)
                .Where(b => BuildCatalog.StageOf(b) == BuildStage.Late);
            found = late.Any(b =>
                b.BuildId.Contains("/Warden/", StringComparison.Ordinal)
                && b.EntityDefMonikers.Any(m => m.StartsWith("Plate", StringComparison.Ordinal)));
        }

        Assert.True(found, "Expected at least one Late Warden with plate in 40 seeds.");
    }

    [Fact]
    public void GeneratedBuildsBuyClassWeapons()
    {
        foreach (var build in BuildGenerator.GenerateSet(8))
        {
            Assert.NotEmpty(build.Weapons);
        }
    }

    [Fact]
    public void CatalogGetFindsTemplatesAndGenerated()
    {
        BuildCatalog.Regenerate(11);
        var generated = BuildCatalog.Generated[0];

        Assert.Equal("IroncladWarden", BuildCatalog.Get("IroncladWarden").BuildId);
        Assert.Equal(generated.BuildId, BuildCatalog.Get(generated.BuildId).BuildId);
    }

    private static bool IsSteel(string moniker) =>
        moniker.StartsWith("Steel", StringComparison.Ordinal)
        || moniker is "Greatsword" or "Maul" or "Poleaxe";

    private static bool IsTrinket(string moniker)
    {
        var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
        return def?.ItemType == ItemType.Trinket;
    }
}
