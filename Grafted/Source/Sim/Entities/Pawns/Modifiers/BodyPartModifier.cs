namespace Grafted.Sim.Entities.Pawns.Modifiers;

public static class BodyPartModifierGenerator
{
    public static BodyPartModifier Generate(BodyPartModifierDef def, int duration)
    {
        BodyPartModifier modifier = (BodyPartModifier)Activator.CreateInstance(def.HandlerClass)!;
        modifier.Def = def;
        modifier.Id = Core.Context.IdProvider.NextBodyPartModifierId();
        modifier.DurationInTicks = duration;
        modifier.Initialize();
        return modifier;
    }
}

public enum BodyPartModifierEventType
{
    Added,
    Removed
}

public abstract class BodyPartModifier : IExposable, IIdentityProvider
{
    public BodyPart BodyPart = null!;
    public BodyPartModifierDef Def = null!;
    public int Ticks;
    public int DurationInTicks;
    public int Id = -1;
    public bool IsExpired;
    public int Severity = 1;

    public int TicksRemaining => DurationInTicks - Ticks;

    public string Label => Def.Label;

    public virtual void Tick()
    {
        Ticks++;
        if (Ticks >= DurationInTicks)
        {
            IsExpired = true;
        }
    }

    public virtual void Initialize()
    {
    }

    public void SpreadTo(BodyPart part)
    {
        if (part.HasModifier(Def))
        {
            return;
        }

        var ticksRemaining = DurationInTicks - Ticks;
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Def, ticksRemaining));
    }

    public virtual void MergeWith(BodyPartModifier modifier)
    {
        DurationInTicks += modifier.DurationInTicks;
    }

    public virtual void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeReferences.Look(ref BodyPart!, "BodyPart");
        ScribeValues.Look(ref Id, "Id");
        ScribeValues.Look(ref Ticks, "Ticks");
        ScribeValues.Look(ref DurationInTicks, "DurationInTicks");
        ScribeValues.Look(ref IsExpired, "IsExpired");
        ScribeValues.Look(ref Severity, "Severity");
    }

    public string GetUniqueId()
    {
        return "BodyPartModifier_" + Id;
    }

    public override string ToString()
    {
        return $"{Def.Moniker} Id: {Id}";
    }

    public virtual void Expired()
    {
    }

    public virtual bool ApplyToPart(BodyPart part)
    {
        //todo raise event MODIFIER APPLIED
        return false;
    }
}