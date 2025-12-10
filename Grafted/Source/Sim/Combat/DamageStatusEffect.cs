namespace Grafted.Sim.Combat;

public class DamageStatusEffect(Pawn pawn, Def effectDef, string label)
{
    public readonly Def EffectDef = effectDef;
    public readonly Pawn Pawn = pawn;
    public readonly string Label = label;
}