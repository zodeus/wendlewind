namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class PoisonHandler : BodyPartModifier
{
    public PoisonHandler(IRng rng)
    {
        Rng = rng;
    }

    private static RangeFloat DamageFactorPerTick = new(0.0002f, 0.001f);
    private const float SpreadChance = 0.005f; // 1% chance to spread per tick

    public override void Tick()
    {

        var externalPart = GetExternalPart();
        if (externalPart == null)
        {
            return;
        }

        // Apply damage to the external part
        var externalDamage = externalPart.MaxHitPoints * DamageFactorPerTick.Roll(Context.Rng);
        externalPart.HitPoints -= externalDamage;

        // Apply damage to all internal parts
        foreach (var internalPart in externalPart.AllInternalParts)
        {
            internalPart.HitPoints -= internalPart.MaxHitPoints * DamageFactorPerTick.Roll(Context.Rng);
        }

        // Chance to spread to adjacent arteries
        if (Context.Rng.Chance(SpreadChance))
        {
            Log.Info($"PoisonHandler: Spreading to adjacent artery");
            SpreadToAdjacentArtery();
        }

        CheckIfLostVitalPart();
        base.Tick();
    }

    private BodyPart? GetExternalPart()
    {
        var externalPart = BodyPart;
        while (externalPart != null && externalPart.IsExternal == false)
        {
            if (externalPart.Socket?.ParentPart == null)
            {
                Log.Warning($"PoisonHandler: No parent part found for {externalPart.Label}");
                return null;
            }
            externalPart = externalPart.Socket!.ParentPart;
        }
        return externalPart;
    }

    private void SpreadToAdjacentArtery()
    {
        var externalPart = GetExternalPart();
        if (externalPart == null)
        {
            Log.Info($"PoisonHandler: No external part found");
            return;
        }

        List<BodyPart> adjacentArteries = [];

        // Check parent body part for arteries
        var parentPart = externalPart.Socket?.ParentPart;
        if (parentPart != null)
        {
            var parentArtery = parentPart.AllInternalParts.FirstOrNull(p => p?.Type == BodyPartType.Artery);
            if (parentArtery != null && !parentArtery.HasModifier(Def))
            {
                adjacentArteries.Add(parentArtery);
            }
        }

        // Check child body parts for arteries
        foreach (var childPart in externalPart.ExternalParts)
        {
            var childArtery = childPart.AllInternalParts.FirstOrNull(p => p?.Type == BodyPartType.Artery);
            if (childArtery != null && !childArtery.HasModifier(Def))
            {
                adjacentArteries.Add(childArtery);
            }
        }

        if (adjacentArteries.Count > 0)
        {
            var targetArtery = adjacentArteries.RandomElement(Context.Rng);
            SpreadTo(targetArtery);
        }
    }

    public override bool ApplyToPart(BodyPart part)
    {
        // Poison can only be applied to arteries
        if (part.Type == BodyPartType.Artery)
        {
            Log.Info($"Applying poison to artery directly: {part.Label}");
            part.TryAddModifier(this);
            return true;
        }

        // If targeting an external part, find and apply to its artery
        if (part.IsExternal)
        {
            var artery = part.AllInternalParts.FirstOrNull(p => p?.Type == BodyPartType.Artery);
            if (artery != null)
            {
                Log.Info($"Applying poison to artery found in external part: {artery.Label}");
                artery.TryAddModifier(this);
                return true;
            }
        }

        return false;
    }

    public override InfoPanelData GetInfoData()
    {
        var averageDamage = (BodyPart.HitPoints * DamageFactorPerTick.Min + BodyPart.HitPoints * DamageFactorPerTick.Max) / 2;
        var lines = new List<InfoLine>
        {
            new("Damages external + internal parts", new Color(180, 100, 255)),
            new($"{SpreadChance * 100:0.#}% spread chance/tick", new Color(200, 150, 255)),
            new("Spreads through arteries", InfoColors.Spread)
        };
        if (averageDamage > 0)
            lines.Add(new($"{averageDamage:.###} avg damage/tick", InfoColors.Damage));


        return new InfoPanelData { Lines = lines };
    }
}