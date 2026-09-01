﻿namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class NecrosisSerumHandler : BodyPartModifier
{
    public NecrosisSerumHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void Tick()
    {
        base.Tick();
        if (!IsExpired) return;

        var modifier = BodyPart.Modifiers.FirstOrNull(m => m?.Def == Defs.BodyPartModifiers.Necrosis);
        if (modifier!=null)
        {
            modifier.IsExpired = true;
        }
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }

    public override InfoPanelData GetInfoData() => new InfoPanelData
    {
        Lines =
        [
            new("Anti-necrotic treatment", InfoColors.Cure),
            new("Cures Necrosis when expired", InfoColors.Info)
        ],
        TimePrefix = "Time remaining"
    };
}