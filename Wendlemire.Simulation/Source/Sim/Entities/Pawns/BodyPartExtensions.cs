namespace Wendlemire.Sim.Entities.Pawns
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
                if (part.IsDestroyed && allInternalPartsDestroyed && part.Socket != null && part.Context.Rng.Chance(.15f))
                {
                    part.Severe();
                }
            }
        }

        public static double CascadeDamageToInternalParts(this BodyPart rootPart, DamageContext ctx, List<DamagedBodyPartRecord> damagedParts)
        {
            var organsHit = 0;
            var remainingDamage = ctx.Amount;
            var maxNumberOfOrgansToHit = new RangeInt(1, 4).Roll(rootPart.Context.Rng);
            if (rootPart.Substance == SubstanceType.Chitin && rootPart.IsCracked == false)
            {
                return 0;
            }

            var internals = rootPart.InternalParts;
            foreach (var internalPart in internals)
            {
                if (internalPart.Type != BodyPartType.Skin)
                {
                    continue;
                }

                var skinDamage = remainingDamage * BodyPart.SkinDamageScaler;
                var skinRemainder = internalPart.ApplyDamage(ctx.WithAmount(skinDamage), damagedParts, cascade: false);
                remainingDamage -= skinDamage - skinRemainder;
            }

            if (remainingDamage <= 0)
            {
                return 0;
            }

            var rest = new List<BodyPart>(internals.Count);
            foreach (var internalPart in internals)
            {
                if (internalPart.Type != BodyPartType.Skin)
                {
                    rest.Add(internalPart);
                }
            }

            for (var i = rest.Count - 1; i > 0; i--)
            {
                var swapIndex = rootPart.Context.Rng.Next(i + 1);
                (rest[i], rest[swapIndex]) = (rest[swapIndex], rest[i]);
            }

            foreach (var internalPart in rest)
            {
                if (remainingDamage <= 0)
                {
                    return 0;
                }

                // Attempt to hit critical parts 
                if (internalPart.Socket?.ParentPart is { HitPoints: > 0, Type: BodyPartType.Skull or BodyPartType.RibCage })
                {
                    var chanceToMiss = internalPart.Socket?.ParentPart?.HealthPercent switch
                    {
                        < .10f => 0.00f,
                        < .20f => 0.30f,
                        < .40f => 0.50f,
                        < .60f => 0.70f,
                        < .80f => 0.85f,
                        < .90f => 0.95f,
                        < .99f => 0.99f,
                        _ => 1
                    };

                    if (rootPart.Context.Rng.Chance(chanceToMiss))
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

                    if (rootPart.Context.Rng.Chance(chanceToMiss))
                    {
                        continue;
                    }
                }

                switch (internalPart.IsOrgan)
                {
                    case true when organsHit >= maxNumberOfOrgansToHit:
                        continue;
                    case true:
                        organsHit++;
                        break;
                }

                remainingDamage = internalPart.ApplyDamage(ctx.WithAmount(remainingDamage), damagedParts);
            }

            return remainingDamage;
        }

        public static Item? CoveringArmor(this BodyPart part)
        {
            if (part.Armor != null)
            {
                return part.Armor;
            }

            var current = part.Socket?.ParentPart;
            while (current != null)
            {
                if (current.Armor != null)
                {
                    return current.Armor;
                }

                current = current.Socket?.ParentPart;
            }

            return null;
        }

        public static void ApplyBodyPartModifiers(this BodyPart part, List<BodyPartModifierRecord> bodyPartModifiers, DamagedBodyPartRecord damagedBodyPartRecord, string weaponManeuver)
        {
            foreach (var record in bodyPartModifiers)
            {
                var scaled = ScaleOffensiveModifierForArmor(part, record);
                if (part.ApplyBodyPartModifier(scaled, weaponManeuver))
                {
                    damagedBodyPartRecord.AppliedModifiers.Add(record.Def);
                }
            }
        }

        public static bool ApplyBodyPartModifier(this BodyPart part, BodyPartModifierRecord record, string maneuver)
        {
            if (!part.Context.Rng.Chance(record.Chance.Roll(part.Context.Rng))) return false;

            var mod = part.Context.Factory.CreateModifier(record.Def, record.DurationInTicks.Roll(part.Context.Rng), record.Power);
            mod.Maneuver = maneuver;
            return mod.ApplyToPart(part);
        }

        private static BodyPartModifierRecord ScaleOffensiveModifierForArmor(BodyPart part, BodyPartModifierRecord record)
        {
            if (part.CoveringArmor() == null)
            {
                return record;
            }

            return new BodyPartModifierRecord
            {
                Def = record.Def,
                DurationInTicks = record.DurationInTicks,
                Chance = record.Chance * CombatBalance.ArmoredDotChanceFactor,
                Power = record.Power * CombatBalance.ArmoredDotPowerFactor
            };
        }

        public static float GetSubtreeBloodWeight(this BodyPart part)
        {
            var sum = part.BloodAmount;
            foreach (var socket in part.Sockets)
            {
                if (socket.AttachedPart != null)
                {
                    sum += socket.AttachedPart.GetSubtreeBloodWeight();
                }
            }

            return sum;
        }
    }
}