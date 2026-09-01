﻿namespace Wendlemire.Sim.Combat;

public class ReflectedStatusEffect(Pawn pawn, Def effectDef, string label, string? itemMoniker = null)
{
    public readonly Def EffectDef = effectDef;
    public readonly Pawn Pawn = pawn;
    public readonly string Label = label;
    public readonly string? ItemMoniker = itemMoniker;
}