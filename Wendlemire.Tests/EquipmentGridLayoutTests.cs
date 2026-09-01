using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class EquipmentGridLayoutTests
{
    public EquipmentGridLayoutTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void HumanAuthoredGridPlacesEveryVisibleSlot()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        var pawn = PawnGenerator.CreatePawn(
            context,
            new PawnRequest(
                "GridTest",
                DefRepository<PawnDef>.GetByMoniker("HumanA")!,
                DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")!,
                PawnType.Player));

        var expected = pawn.Equipment.Slots
            .Where(pair => pair.Value.Count > 0 && !EquipmentGridLayout.IsHiddenPart(pair.Key))
            .SelectMany(pair => pair.Value.Select(slot => (pair.Key, slot)))
            .ToList();

        var layout = EquipmentGridLayout.Build(pawn);
        var def = EquipmentGridDef.ForBody(pawn.Body.Def);

        Assert.NotNull(def);
        Assert.Equal(def.Columns, layout.Columns);
        Assert.Equal(def.Rows, layout.Rows);
        Assert.Equal(expected.Count, layout.Slots.Count);

        var missing = expected
            .Where(key => !layout.Slots.ContainsKey(key))
            .Select(key => $"{key.Key.InternalLabel}:{key.slot}")
            .ToList();
        Assert.True(missing.Count == 0, "Unplaced slots: " + string.Join(", ", missing));
    }
}
