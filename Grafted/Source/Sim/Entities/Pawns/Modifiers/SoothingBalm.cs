namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class SoothingBalm : BodyPartModifier
{
    public override void Tick()
    {
        RemoveBurningAndAcid();
        base.Tick();
    }

    private void RemoveBurningAndAcid()
    {
        foreach (var modifier in BodyPart.Modifiers)
        {
            if (modifier.Def == Defs.BodyPartModifiers.Burning ||
                modifier.Def == Defs.BodyPartModifiers.Acid)
            {
                modifier.IsExpired = true;
            }
        }
    }

    public override Widget? GetInfoPanel() => BuildInfoPanel(new InfoPanelData
    {
        Lines =
        [
            new("Removes Burning effects", new Color(240, 108, 7)),
            new("Removes Acid effects", new Color(237, 245, 22))
        ]
    });
}