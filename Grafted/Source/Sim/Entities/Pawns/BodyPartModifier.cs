namespace Grafted.Sim.Entities.Pawns;

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
        Log.Warning($"No implementation for merging {modifier.Label} with self={this}");
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_References.Look(ref BodyPart!, "BodyPart");
        Scribe_Values.Look(ref Id, "Id");
        Scribe_Values.Look(ref Ticks, "Ticks");
        Scribe_Values.Look(ref DurationInTicks, "DurationInTicks");
        Scribe_Values.Look(ref IsExpired, "IsExpired");
        Scribe_Values.Look(ref Severity, "Severity");
    }

    public string GetUniqueId()
    {
        return "BodyPartModifier_" + Id;
    }

    public override string ToString()
    {
        return $"{Def.Moniker} Id: {Id}";
    }
}

public class BodyPartModifierDef : Def
{
    [UsedImplicitly] public Type HandlerClass = typeof(BodyPartModifier);
}

public class BodyPartModifierRecord
{
    public BodyPartModifierDef Def = null!;
    public RangeInt DurationInTicks = new RangeInt(0, 0);
    public RangeFloat Chance = RangeFloat.One;
}

[UsedImplicitly]
public class BurningAcid : BodyPartModifier
{
    public bool HasSpread = false;
    public bool HasPenetrated = false;

    public override void Tick()
    {
        if (BodyPart.Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.SoothingBalm))
        {
            IsExpired = true;
            return;
        }

        var damageMultiplier = HasSpread ? .005f : 0.0015f;
        var damage = BodyPart.HitPoints * damageMultiplier;
        BodyPart.HitPoints -= damage;
        if (HasPenetrated == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < .2f })
        {
            HasPenetrated = true;
            if (BodyPart.Socket?.ParentPart?.AllInternalParts.Count != 0)
            {
                foreach (var internalPart in BodyPart.Socket!.ParentPart!.AllInternalParts)
                {
                    SpreadTo(internalPart);
                }
            }
        }

        if (HasSpread == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < .2f })
        {
            HasSpread = true;
            if (BodyPart.Socket?.ParentPart != null)
            {
                SpreadTo(BodyPart.Socket.ParentPart);
            }
        }

        base.Tick();
        LostVitalPart();
    }

    private void LostVitalPart()
    {
        if (BodyPart.IsFunctional == false && BodyPart.Body?.AllParts.Any(p => p.Type == BodyPart.Type && p.IsFunctional) == false)
        {
           BodyPart.Body.Pawn.HandleDeath($"Vital body part {BodyPart.Label} was destroyed by {Def.Label}");
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref HasSpread, "HasSpread");
        base.ExposeData();
    }
}

[UsedImplicitly]
public class SoothingBalm : BodyPartModifier
{
    public override void Tick()
    {
        BodyPart.HitPoints += BodyPart.HitPoints * .01f;

        base.Tick();
    }
}