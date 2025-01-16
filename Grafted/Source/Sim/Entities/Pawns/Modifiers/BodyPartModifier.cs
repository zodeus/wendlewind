namespace Grafted.Sim.Entities.Pawns.Modifiers;

public static class BodyPartModifierGenerator
{
    public static BodyPartModifier Generate(BodyPartModifierDef def, int duration)
    {
        BodyPartModifier modifer = (BodyPartModifier)Activator.CreateInstance(def.HandlerClass)!;
        modifer.Def = def;
        modifer.Id = Core.Context.IdProvider.NextBodyPartModifierId();
        modifer.DurationInTicks = duration;
        modifer.Initialize();
        return modifer;
    }
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
        if (part.HasModifer(Def))
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
}