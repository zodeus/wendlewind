namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class NecrosisHandler : BodyPartModifier
{
    private const double DamageFactorPerTick = .001f;
    private const int TotalTicksToSpread = 6000;

    private int _ticksToSpread;

    public override void Tick()
    {
        _ticksToSpread = Math.Clamp(_ticksToSpread--, 0, TotalTicksToSpread);
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
        if (BodyPart.IsDestroyed && _ticksToSpread < 1)
        {
            _ticksToSpread = TotalTicksToSpread;
            List<BodyPart> randomParts = [];
            if (BodyPart.Socket?.ParentPart != null)
            {
                randomParts.Add(BodyPart.Socket.ParentPart);
            }

            randomParts.AddRange(BodyPart.ExternalParts.InRandomOrder());

            if (randomParts.Count > 0)
            {
                var part = randomParts.RandomElement();
                Log.Info($"Necrosis spreading to {part}");
                SpreadTo(part);
            }
        }

        CheckIfLostVitalPart(BodyPart);
    }

    private void CheckIfLostVitalPart(BodyPart bodyPart)
    {
        if (bodyPart.IsFunctional) return;

        var remainingFunctionalParts = bodyPart.Body!.AllParts.Count(p => p.Type == bodyPart.Type && p.IsFunctional);
        if (bodyPart is { IsVital: true, IsFunctional: false } && remainingFunctionalParts <= 0)
        {
            bodyPart.Body.Pawn.TriggerDeath($"{bodyPart.Label} {(bodyPart.IsDestroyed ? "was destroyed" : "stopped functioning")}");
        }
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _ticksToSpread, "TicksToSpread");
        base.ExposeData();
    }
}