using Wendlewind.NetCode;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Xunit;

namespace Wendlewind.Tests;

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
}
