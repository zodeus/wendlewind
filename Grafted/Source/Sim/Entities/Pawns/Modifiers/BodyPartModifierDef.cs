namespace Grafted.Sim.Entities.Pawns.Modifiers;

public class BodyPartModifierDef : Def
{
    [UsedImplicitly] public Type HandlerClass = typeof(BodyPartModifier);
    public Color Color = Color.Pink;
    public int ColorPriority = 0;
    public BodyPartModifierType Type = BodyPartModifierType.Undefined;
}

public enum BodyPartModifierType
{
    Undefined,
    Buff,
    Debuff
}