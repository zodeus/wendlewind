using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Combat;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class CombatCloserTests : IDisposable
{
    public CombatCloserTests()
    {
        TestData.EnsureLoaded();
        CombatCloser.ResetTiming();
    }

    public void Dispose()
    {
        CombatCloser.ResetTiming();
    }

    [Fact]
    public void DrainCurveStartsAtOneAndEscalates()
    {
        Assert.Equal(0f, CombatCloser.BloodDrainPerTick(CombatCloser.StartTicks - 1));
        Assert.Equal(1f, CombatCloser.BloodDrainPerTick(CombatCloser.StartTicks));
        Assert.Equal(4f, CombatCloser.BloodDrainPerTick(CombatCloser.StartTicks + 20 * 60), 2);
    }

    [Fact]
    public void PickLoserPrefersLowerBloodThenFewerVitalsThenDefender()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.Pawn;
        var defender = harness.CreatePawn("HumanA", "Defender");

        attacker.Body.BloodAmount = attacker.Body.MaxBlood * 0.4f;
        defender.Body.BloodAmount = defender.Body.MaxBlood * 0.8f;
        Assert.Same(attacker, CombatCloser.PickLoser(attacker, defender));

        attacker.Body.BloodAmount = attacker.Body.MaxBlood;
        defender.Body.BloodAmount = defender.Body.MaxBlood * 0.2f;
        Assert.Same(defender, CombatCloser.PickLoser(attacker, defender));

        attacker.Body.BloodAmount = attacker.Body.MaxBlood;
        defender.Body.BloodAmount = defender.Body.MaxBlood;
        Assert.Same(defender, CombatCloser.PickLoser(attacker, defender));
    }

    [Fact]
    public void ShortFightDoesNotStartWasting()
    {
        var sim = DuelSimulator.Simulate(Axe("A"), Axe("B"), seed: 3);
        Assert.True(sim.Result.Ticks < CombatCloser.StartTicks, $"expected a sub-90s fight, got {sim.Result.Ticks} ticks");
        Assert.DoesNotContain(sim.Log, e => e.Kind == CombatEventKind.System && e.Message == CombatCloser.StartedMessage);
        Assert.DoesNotContain(sim.Log, e => e.Message == CombatCloser.CauseOfDeath);
    }

    [Fact]
    public void HardResolveEndsStallWithOneDeath()
    {
        CombatCloser.OverrideTimingForTests(startTicks: 8, hardResolveTicks: 20);
        var sim = DuelSimulator.Simulate(Club("A"), Club("B"), seed: 1);

        Assert.True(sim.Result.Ticks <= 20);
        Assert.True(sim.Result.Ticks >= 8);
        Assert.Contains(sim.Log, e => e.Kind == CombatEventKind.System && e.Message == CombatCloser.StartedMessage);
        Assert.Equal(1, sim.Log.Count(e => e.Kind == CombatEventKind.Death));
        Assert.Equal(1, sim.Log.Count(e => e.Kind == CombatEventKind.System && e.Message == "Battle is over"));
        Assert.Equal(CombatCloser.CauseOfDeath, sim.Result.CauseOfDeath);
        Assert.True(sim.Result.WinnerPlayerId is "A" or "B");
    }

    [Fact]
    public void CloserDrainsBloodAndFinishesBeforeHardCap()
    {
        CombatCloser.OverrideTimingForTests(startTicks: 1, hardResolveTicks: 20_000);
        var sim = DuelSimulator.Simulate(Club("A"), Club("B"), seed: 2);

        Assert.Contains(sim.Log, e => e.Kind == CombatEventKind.System && e.Message == CombatCloser.StartedMessage);
        Assert.Contains(sim.Log, e => e.Kind == CombatEventKind.DamageOverTime && e.BodyPartLabel == "blood");
        Assert.True(sim.Result.Ticks < 20_000);
        Assert.True(sim.Analytics.Attacker.BloodPercent < 1 || sim.Analytics.Defender.BloodPercent < 1);
    }

    [Fact]
    public void TiedVitalityAttackerWinsOnHardResolve()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.Pawn;
        var defender = harness.CreatePawn("HumanA", "Defender");
        attacker.Body.BloodAmount = defender.Body.BloodAmount = attacker.Body.MaxBlood;
        Assert.Equal(CombatCloser.CountFunctionalVitals(attacker), CombatCloser.CountFunctionalVitals(defender));
        Assert.Same(defender, CombatCloser.PickLoser(attacker, defender));
    }

    [Fact]
    public void HealStackVsChainFinishesBeforeHardCap()
    {
        var ticks = new List<int>();
        for (var seed = 1; seed <= 4; seed++)
        {
            var sim = DuelSimulator.Simulate(
                Named(BuildTemplates.WitchDoctorSage(), "A"),
                Named(BuildTemplates.IroncladWarden(), "B"),
                seed);
            ticks.Add(sim.Result.Ticks);
            Assert.True(sim.Result.Ticks < CombatCloser.HardResolveTicks,
                $"seed {seed} hit hard cap at {sim.Result.Ticks} ticks, cause={sim.Result.CauseOfDeath}");
        }

        ticks.Sort();
        Assert.True(ticks[ticks.Count / 2] < CombatCloser.HardResolveTicks);
    }

    [Fact]
    public void FormatterStylesWastingAnnouncement()
    {
        var line = CombatLogFormatter.Format(new CombatLogEvent
        {
            Kind = CombatEventKind.System,
            Message = CombatCloser.StartedMessage
        });
        Assert.Contains(CombatCloser.StartedMessage, line);
        Assert.Contains("/f[default, 48]", line);
    }

    private static BuildSnapshot Named(BuildSnapshot snapshot, string id) =>
        snapshot with { PlayerId = id, BuildId = id };

    private static BuildSnapshot Club(string id) => Armed(id, "WoodClub");

    private static BuildSnapshot Axe(string id) => Armed(id, "BoneAxe");

    private static BuildSnapshot Armed(string id, string weapon) => new()
    {
        PlayerId = id,
        BuildId = id,
        PawnDefMoniker = "HumanA",
        EntityDefMonikers = [weapon],
        StanceMoniker = "Offensive",
        Weapons = [new WeaponConfig { ItemMoniker = weapon, UseInCombat = true }]
    };
}
