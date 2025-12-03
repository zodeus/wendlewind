using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Pawns
{
    public static class BodyPartExtensions
    {
        public static void PotentiallySevereLimb(this BodyPart part)
        {
            if (part is { IsExternal: true, IsSevered: false } && part.Type != BodyPartType.Eye)
            {
                var internalParts = part.AllInternalParts;
                var allInternalPartsDestroyed = internalParts.Count > 0 && internalParts.All(p => p.IsDestroyed);

                // Sever if: the part itself is destroyed, OR all internal parts are destroyed (and there are some)
                if (part.IsDestroyed && allInternalPartsDestroyed && part.Socket != null && Core.Random.Chance(.15f))
                {
                    part.Severe();
                }
            }
        }

        public static double CascadeDamageToInternalParts(this BodyPart rootPart, DamageContext ctx, List<DamagedBodyPartRecord> damagedParts)
        {
            var organsHit = 0;
            var remainingDamage = ctx.Amount;
            var maxNumberOfOrgansToHit = new RangeInt(1, 2 + 1).RandomValue;
            if (rootPart.Substance == SubstanceType.Chitin && rootPart.IsCracked == false)
            {
                return 0;
            }

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
                        < .70f => 1f,
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

                remainingDamage -= internalPart.ApplyDamage(ctx, damagedParts);
            }

            return remainingDamage;
        }

        public static void ApplyBodyPartModifiers(this BodyPart part, List<BodyPartModifierRecord> bodyPartModifiers, DamagedBodyPartRecord damagedBodyPartRecord, string weaponManeuver)
        {
            foreach (var record in bodyPartModifiers)
            {
                if (part.ApplyBodyPartModifier(record, weaponManeuver))
                {
                    damagedBodyPartRecord.AppliedModifiers.Add(record.Def);
                }
            }
        }

        public static bool ApplyBodyPartModifier(this BodyPart part, BodyPartModifierRecord record, string maneuver)
        {
            if (!Core.Random.Chance(record.Chance.RandomValue)) return false;

            var mod = BodyPartModifierGenerator.Generate(record.Def, record.DurationInTicks.RandomValue);
            mod.Maneuver = maneuver;
            return mod.ApplyToPart(part);
        }
    }
}