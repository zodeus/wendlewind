using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class TrinketResistanceTests
{
    private const double BasicMidHit = 40;
    private const int BlockPrecision = 2;
    private const float WardResist = 12f;
    private const float ChainResist = 34f;

    public TrinketResistanceTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void IronWardBlocksPhysicalMidHit()
    {
        using var harness = BodyTestHarness.Human();
        GiveTrinket(harness, "IronWard");
        var record = Strike(harness, "IronSword");

        Assert.Equal(CombatBalance.BlockedAmount(BasicMidHit, WardResist), record.AmountBlocked, BlockPrecision);
        Assert.Equal("IronWard", record.BlockingItemMoniker);
    }

    [Fact]
    public void FlameWardDoesNotBlockPhysicalMidHit()
    {
        using var harness = BodyTestHarness.Human();
        GiveTrinket(harness, "FlameWard");
        var record = Strike(harness, "IronSword");

        Assert.Equal(0, record.AmountBlocked);
        Assert.Null(record.BlockingItemMoniker);
    }

    [Fact]
    public void FlameWardBlocksFireMidHit()
    {
        using var harness = BodyTestHarness.Human();
        GiveTrinket(harness, "FlameWard");
        var record = Strike(harness, "EmberWand");

        Assert.Equal(CombatBalance.BlockedAmount(BasicMidHit, WardResist), record.AmountBlocked, BlockPrecision);
        Assert.Equal("FlameWard", record.BlockingItemMoniker);
    }

    [Fact]
    public void IronWardDoesNotBlockFireMidHit()
    {
        using var harness = BodyTestHarness.Human();
        GiveTrinket(harness, "IronWard");
        var record = Strike(harness, "EmberWand");

        Assert.Equal(0, record.AmountBlocked);
        Assert.Null(record.BlockingItemMoniker);
    }

    [Fact]
    public void IronWardStacksWithChainInOneReductionPool()
    {
        using var harness = BodyTestHarness.Human();
        GiveTrinket(harness, "IronWard");
        harness.EquipArmor("ChainVambrace");
        var record = Strike(harness, "IronSword");

        Assert.Equal(CombatBalance.BlockedAmount(BasicMidHit, ChainResist + WardResist), record.AmountBlocked, BlockPrecision);
        Assert.Equal("ChainVambrace", record.BlockingItemMoniker);
    }

    [Theory]
    [InlineData("IronWard", "PhysicalResistance", 12f)]
    [InlineData("FlameWard", "FireResistance", 12f)]
    [InlineData("FrostWard", "IceResistance", 12f)]
    [InlineData("VitriolWard", "AcidResistance", 12f)]
    [InlineData("VenomWard", "PoisonResistance", 12f)]
    [InlineData("HexWard", "MagicResistance", 12f)]
    public void WardsHaveExpectedResistance(string moniker, string statMoniker, float expected)
    {
        using var harness = BodyTestHarness.Human();
        var item = harness.CreateItem(moniker);
        var stat = DefRepository<StatDef>.GetByMoniker(statMoniker)
                   ?? throw new InvalidOperationException($"Missing stat '{statMoniker}'.");
        Assert.Equal(expected, item.GetStatValue(stat));
    }

    private static void GiveTrinket(BodyTestHarness harness, string moniker)
    {
        Assert.True(harness.Pawn.Inventory.TryAdd(harness.CreateItem(moniker)));
    }

    private static DamageRecord Strike(BodyTestHarness harness, string weapon)
    {
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var arm = harness.External(BodyPartType.Arm);
        return Assert.Single(harness.Strike(attacker, arm, BasicMidHit, weapon).Damages);
    }
}
