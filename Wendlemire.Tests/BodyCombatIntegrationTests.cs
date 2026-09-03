using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class BodyCombatIntegrationTests
{
    public BodyCombatIntegrationTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void SeededCombatDamagesBodyPartsAndRecordsBodyCauseOfDeath()
    {
        float? bloodAfterWound = null;
        var (summary, log) = CombatReplay.RunWithLog(
            CombatReplay.DefaultRunSeed,
            context =>
            {
                BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.Glasscannon());
                context.PlayerPawn.MedicalChest.Clear();
                var arm = context.PlayerPawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm);
                arm.Severe();
                bloodAfterWound = context.PlayerPawn.Body.BloodAmount;
            });

        var bodyHits = log.Where(e =>
            e.Kind is CombatEventKind.Damage or CombatEventKind.PartDestroyed or CombatEventKind.PartSevered
            && !string.IsNullOrWhiteSpace(e.BodyPartLabel)).ToList();
        var death = log.FirstOrDefault(e => e.Kind == CombatEventKind.Death);

        Assert.NotEmpty(bodyHits);
        Assert.True(bloodAfterWound > 1);
        Assert.True(summary.Ticks > 0);
        if (!summary.PlayerAlive)
        {
            Assert.False(string.IsNullOrWhiteSpace(summary.CauseOfDeath));
            Assert.True(
                summary.CauseOfDeath!.Contains("Blood", StringComparison.OrdinalIgnoreCase)
                || summary.CauseOfDeath.Contains("organ", StringComparison.OrdinalIgnoreCase)
                || summary.CauseOfDeath.Contains("destroyed", StringComparison.OrdinalIgnoreCase)
                || summary.CauseOfDeath.Contains("failed", StringComparison.OrdinalIgnoreCase)
                || summary.CauseOfDeath.Contains("severed", StringComparison.OrdinalIgnoreCase),
                summary.CauseOfDeath);
        }
        else
        {
            Assert.Contains(log, e => e.Kind == CombatEventKind.Death);
        }

        Assert.True(death != null || bodyHits.Count > 0);
    }

    [Fact]
    public void CombatTicksPlayerAndEnemyEffectsAtTheSameRate()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(1);
        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        context.CurrentZone!.NextEncounter();
        var encounter = context.CurrentZone.ActiveEncounter
                        ?? throw new InvalidOperationException("NextEncounter did not create an encounter.");
        var player = encounter.PlayerPawns.First();
        var enemy = encounter.EnemyPawns.First();

        const int duration = 400;
        player.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Mulled,
            TicksLeft = duration
        });
        enemy.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Mulled,
            TicksLeft = duration
        });

        for (var i = 0; i < 20 && encounter.State == EncounterState.InProgress; i++)
        {
            context.Tick();
        }

        Assert.False(player.IsDead);
        Assert.False(enemy.IsDead);
        var playerLeft = player.Body.Effects.Single(e => e.Def == Defs.BodyEffects.Mulled).TicksLeft;
        var enemyLeft = enemy.Body.Effects.Single(e => e.Def == Defs.BodyEffects.Mulled).TicksLeft;
        Assert.Equal(playerLeft, enemyLeft);
        Assert.Equal(duration - encounter.Ticks, playerLeft);
    }
}
