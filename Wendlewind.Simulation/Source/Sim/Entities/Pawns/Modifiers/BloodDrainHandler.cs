namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BloodDrainHandler : BodyPartModifier
{
    private const float BloodDrainPerTick = 1f;
    
    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Flesh];

    public override void Tick()
    {
        
        if (BodyPart.IsSevered || BodyPart.Body == null)
        {
            return;
        }
        
        // Drain blood from the pawn
        BodyPart.Body.BloodAmount -= BloodDrainPerTick;
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        // Blood drain can only be applied to external flesh parts
        if (part.IsExternal == false)
        {
            return false;
        }
        
        if (AllowedSubstances.Contains(part.Substance) == false)
        {
            return false;
        }
        
        part.TryAddModifier(this);
        return true;
    }

    public override InfoPanelData GetInfoData() => new InfoPanelData
    {
        Damage = BloodDrainPerTick,
        DamageSuffix = "blood/tick",
        DamageColor = new Color(180, 60, 60),
        Lines = [new("Stops if part severed", InfoColors.Muted)]
    };
}
