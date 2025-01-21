using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Pawns
{
    public static class BodyPartExtensions
    {
        public static void PotentiallySevereLimb(this BodyPart part)
        {
            if (part is { IsExternal: true, IsSevered: false, AllInternalParts: { Count: > 0 } })
            {
                var allInternalPartsDestroyed = true;
                foreach (var internalPart in part.AllInternalParts)
                {
                    if (!internalPart.IsDestroyed)
                    {
                        allInternalPartsDestroyed = false;
                    }
                }

                if (allInternalPartsDestroyed && part.Socket != null && Core.Random.Chance(.25f))
                {
                    part.Severe();
                }
            }
        }

        public static double CascadeDamageToInternalParts(this BodyPart rootPart, double damage, DamageType damageType, List<BodyPartModifierRecord> bodyPartModifiers,
            List<DamagedBodyPartRecord> damagedParts)
        {
            var organsHit = 0;
            var remainingDamage = damage;
            var maxNumberOfOrgansToHit = new RangeInt(1, 2 + 1).RandomValue;
            foreach (var internalPart in rootPart.InternalParts.InRandomOrder())
            {
                if (remainingDamage <= 0)
                {
                    return 0;
                }

                if (internalPart.Type == BodyPartType.Skin)
                {
                    // Skin is handled in this.ApplyDamageToExternalPart
                    continue;
                }

                // Attempt to hit critical parts 
                if (internalPart.Socket?.ParentPart is { HitPoints: > 0, Type: BodyPartType.Skull or BodyPartType.RibCage })
                {
                    var chanceToMiss = internalPart.Socket?.ParentPart?.HealthPercent switch
                    {
                        < .10f => 0.00f,
                        < .20f => 0.50f,
                        < .40f => 0.95f,
                        < .80f => 0.99f,
                        _ => 1
                    };

                    if (Core.Random.Chance(chanceToMiss))
                    {
                        continue;
                    }
                }

                // The stomach
                if (internalPart.Type is BodyPartType.Stomach && internalPart.Socket?.ParentPart?.HealthPercent > 0.5)
                {
                    continue;
                }

                if (internalPart.Type == BodyPartType.Artery)
                {
                    var chanceToMiss = internalPart.Socket?.ParentPart?.HealthPercent switch
                    {
                        < .02f => 0.00f,
                        < .05f => 0.85f,
                        < .10f => 0.90f,
                        < .50f => 0.97f,
                        < .90f => 0.99f,
                        _ => 1
                    };

                    if (Core.Random.Chance(chanceToMiss))
                    {
                        continue;
                    }
                }

                switch (internalPart.IsOrgan)
                {
                    case true when organsHit > maxNumberOfOrgansToHit:
                        continue;
                    case true:
                        organsHit++;
                        break;
                }

                remainingDamage -= internalPart.ApplyDamage(damage, damageType, bodyPartModifiers, damagedParts);
            }

            return remainingDamage;
        }

        public static void ApplyBodyPartModifiers(this BodyPart part, List<BodyPartModifierRecord> bodyPartModifiers, DamagedBodyPartRecord damagedBodyPartRecord)
        {
            foreach (var record in bodyPartModifiers)
            {
                if (!Core.Random.Chance(record.Chance.RandomValue)) continue;

                var mod = BodyPartModifierGenerator.Generate(record.Def, record.DurationInTicks.RandomValue);
                if (mod.ApplyToPart(part) is { } modifierDef)
                {
                    damagedBodyPartRecord.AppliedModifiers.Add(modifierDef);
                }
            }
        }
    }
}