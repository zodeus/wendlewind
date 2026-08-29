

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global

namespace Wendlewind.Sim.Combat;

public class MysteryProperties
{
    public RangeInt PartsToRestore;
    public List<BodyPartType> RestorablePartTypes = [];

    public List<ItemDef> OptionalRewards = [];
    public int OptionalRewardsCount = 0;
}
