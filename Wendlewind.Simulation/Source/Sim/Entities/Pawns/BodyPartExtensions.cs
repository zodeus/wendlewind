namespace Wendlewind.Sim.Entities.Pawns
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
                if (part.IsDestroyed && allInternalPartsDestroyed && part.Socket != null && GameContext.Random.Chance(.15f))
                {
                    part.Severe();
                }
            }
        }

        public static double CascadeDamageToInternalParts(this BodyPart rootPart, DamageContext ctx, List<DamagedBodyPartRecord> damagedParts)
        {
            var organsHit = 0;
            var remainingDamage = ctx.Amount;
            var maxNumberOfOrgansToHit = new RangeInt(1, 4).RandomValue;
            if (rootPart.Substance == SubstanceType.Chitin && rootPart.IsCracked == false)
            {
                return 0;
            }

            var skin = rootPart.InternalParts.Where(p => p.Type == BodyPartType.Skin);
            var rest = rootPart.InternalParts.Where(p => p.Type != BodyPartType.Skin).InRandomOrder();
            var internalParts = skin.Concat(rest).ToList();

            foreach (var internalPart in internalParts)
            {
                if (remainingDamage <= 0)
                {
                    return 0;
                }

                // Skin takes reduced damage but is always hit first, does not cascade further
                if (internalPart.Type == BodyPartType.Skin)
                {
                    var skinDamage = remainingDamage * BodyPart.SkinDamageScaler;
                    var skinRemainder = internalPart.ApplyDamage(ctx.WithAmount(skinDamage), damagedParts, cascade: false);
                    // Reduce remaining damage by what skin absorbed (skinDamage - skinRemainder)
                    remainingDamage -= skinDamage - skinRemainder;
                    continue;
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

                    if (GameContext.Random.Chance(chanceToMiss))
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

                    if (GameContext.Random.Chance(chanceToMiss))
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
            if (!GameContext.Random.Chance(record.Chance.RandomValue)) return false;

            var mod = BodyPartModifierGenerator.Generate(record.Def, record.DurationInTicks.RandomValue, record.Power);
            mod.Maneuver = maneuver;
            return mod.ApplyToPart(part);
        }
    }
}