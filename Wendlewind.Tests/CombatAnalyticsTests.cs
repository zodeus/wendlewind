using System.Text.Json;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Combat;
using Xunit;

namespace Wendlewind.Tests;

[Collection("Sim")]
public class CombatAnalyticsTests
{
    public CombatAnalyticsTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void FromKnownLogComputesDurationBandAndSideTotals()
    {
        const int attackerId = 1;
        const int defenderId = 2;
        CombatLogEvent[] log =
        [
            new() { Kind = CombatEventKind.Damage, Tick = 10, SourcePawnId = attackerId, SubjectPawnId = defenderId, Amount = 50 },
            new() { Kind = CombatEventKind.Damage, Tick = 20, SourcePawnId = attackerId, SubjectPawnId = defenderId, Amount = 40, Blocked = 5 },
            new() { Kind = CombatEventKind.Heal, Tick = 30, SubjectPawnId = defenderId, Amount = 15 },
            new() { Kind = CombatEventKind.DamageOverTime, Tick = 40, SubjectPawnId = defenderId, Amount = 8 },
            new() { Kind = CombatEventKind.Miss, Tick = 50, SourcePawnId = attackerId, SubjectPawnId = defenderId },
            new() { Kind = CombatEventKind.Dodge, Tick = 60, SourcePawnId = attackerId, SubjectPawnId = defenderId },
            new() { Kind = CombatEventKind.PotionUsed, Tick = 70, SubjectPawnId = attackerId },
            new() { Kind = CombatEventKind.MedicalUsed, Tick = 80, SubjectPawnId = defenderId },
            new()
            {
                Kind = CombatEventKind.Damage,
                Tick = 90,
                SourcePawnId = attackerId,
                SubjectPawnId = defenderId,
                Amount = 10,
                SubEffects =
                [
                    new CombatSubEffect
                    {
                        Kind = CombatEventKind.PartSevered,
                        SubjectPawnId = defenderId,
                        BodyPartLabel = "Arm"
                    }
                ]
            }
        ];

        var analytics = CombatAnalytics.From(log, ticks: 1800, attackerId, defenderId, "BoneAxe", "Chop",
            attackerBloodPercent: 0.82f, defenderBloodPercent: 0.11f);

        Assert.Equal(1800, analytics.DurationTicks);
        Assert.Equal(30, analytics.DurationSeconds);
        Assert.True(analytics.InTargetBand);
        Assert.Equal(100, analytics.Attacker.DamageDealt);
        Assert.Equal(15, analytics.Defender.Healing);
        Assert.Equal(8, analytics.Defender.DotTaken);
        Assert.Equal(3, analytics.Attacker.Hits);
        Assert.Equal(1, analytics.Attacker.Misses);
        Assert.Equal(1, analytics.Defender.Dodges);
        Assert.Equal(1, analytics.Defender.Blocks);
        Assert.Equal(1, analytics.Attacker.PotionUses);
        Assert.Equal(1, analytics.Defender.MedicalUses);
        Assert.Equal(1, analytics.Defender.Severs);
        Assert.Equal(0.82f, analytics.Attacker.BloodPercent);
        Assert.Equal(0.11f, analytics.Defender.BloodPercent);
        Assert.Equal(100 / 30.0, analytics.Attacker.DamagePerSecond);
        Assert.Equal(10, analytics.FirstDamageTick);
        Assert.Equal(90, analytics.LastDamageTick);
        Assert.Equal("BoneAxe", analytics.KillingWeapon);
        Assert.Equal("Chop", analytics.KillingManeuver);
    }

    [Theory]
    [InlineData(1799, false)]
    [InlineData(1800, true)]
    [InlineData(3600, true)]
    [InlineData(3601, false)]
    public void TargetBandIsInclusiveThirtyToSixtySeconds(int ticks, bool expected)
    {
        var analytics = CombatAnalytics.From([], ticks, 1, 2);
        Assert.Equal(expected, analytics.InTargetBand);
        Assert.Equal(ticks / 60.0, analytics.DurationSeconds);
    }

    [Fact]
    public void DuelSimulationIncludesLogAndAnalytics()
    {
        var attacker = BuildTemplates.TankRegen() with { PlayerId = "a" };
        var defender = BuildTemplates.AcidRusher() with { PlayerId = "b" };
        var simulation = DuelSimulator.Simulate(attacker, defender, CombatReplay.DefaultRunSeed);

        Assert.False(string.IsNullOrEmpty(simulation.Result.MatchId));
        Assert.NotEmpty(simulation.Log);
        Assert.Equal(simulation.Result.Ticks, simulation.Analytics.DurationTicks);
        Assert.Equal(
            CombatAnalytics.TicksToSeconds(simulation.Result.Ticks),
            simulation.Analytics.DurationSeconds);
        Assert.True(simulation.Analytics.Attacker.DamageDealt + simulation.Analytics.Defender.DamageDealt > 0);
    }

    [Fact]
    public void CombatLogAndAnalyticsRoundTripSourceGenJson()
    {
        var analytics = CombatAnalytics.From(
            [
                new CombatLogEvent
                {
                    Kind = CombatEventKind.Damage,
                    Tick = 12,
                    SourcePawnId = 1,
                    SubjectPawnId = 2,
                    Amount = 7
                }
            ],
            ticks: 2400,
            attackerPawnId: 1,
            defenderPawnId: 2,
            killingWeapon: "IronSword",
            killingManeuver: "Slash");
        var log = new CombatLogRecord
        {
            MatchId = "abc",
            Events =
            [
                new CombatLogEvent { Kind = CombatEventKind.Death, Tick = 2400, SubjectPawnId = 2, Message = "bleed" }
            ]
        };

        var analyticsJson = JsonSerializer.Serialize(analytics, NetCodeJsonContext.Default.FightAnalytics);
        var restoredAnalytics = JsonSerializer.Deserialize(analyticsJson, NetCodeJsonContext.Default.FightAnalytics);
        Assert.NotNull(restoredAnalytics);
        Assert.Equal(40, restoredAnalytics.DurationSeconds);
        Assert.True(restoredAnalytics.InTargetBand);
        Assert.Equal("IronSword", restoredAnalytics.KillingWeapon);

        var logJson = JsonSerializer.Serialize(log, NetCodeJsonContext.Default.CombatLogRecord);
        var restoredLog = JsonSerializer.Deserialize(logJson, NetCodeJsonContext.Default.CombatLogRecord);
        Assert.NotNull(restoredLog);
        Assert.Equal("abc", restoredLog.MatchId);
        Assert.Equal(CombatEventKind.Death, restoredLog.Events[0].Kind);
        Assert.Contains("durationSeconds", analyticsJson);
    }
}
