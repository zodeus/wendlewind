namespace Grafted.Sim.Entities.Pawns.Bodies.Handlers;

public class DefaultBodyHandler : IExposable
{
    private int _ticksWithEmptyStomach;

    public PawnBody Body = null!;

    public virtual float RestingMultiplier => 2;
    public virtual float SeveredArteryBloodLossFactor => 1.7f;
    public virtual float SeveredLimbBloodLossFactor => 3f;
    public virtual float BloodLossThreshold => .95f;
    public virtual float ArteryBloodLossOffset => 1.15f;
    public virtual float BloodRegenerationFactor => 1f;
    public virtual float HealthRegenerationFactor => 0.001f;
    public virtual int MalnutritionDamageIntervalInMinutes => 10;
    public virtual float FoodLossPerIteration => 0.002f;
    public virtual float FoodLossRestingFactor => 0.60f;
    public virtual int TicksUntilFamished => 2 * 60 * Core.TicksPerSecond; // minutes * seconds * ticks per second
    public virtual int EmptyStomachEnergyLossFactor => 2;
    public virtual float HungryThreshold => 0.85f;
    public virtual float MalnutritionDamageFactor => Core.Random.NextFloat(0.0001f, 0.0005f);
    public virtual bool IsFamished => _ticksWithEmptyStomach > TicksUntilFamished;
    public bool IsHungry => Body.StomachLevel < HungryThreshold;

    public virtual void Initialize(PawnBody body)
    {
        Body = body;
    }

    public virtual void Tick()
    {
        HandleNutrition();
        if (Body.RootSocket.AttachedPart == null)
        {
            Body.BloodAmount = 0;
            Body.BloodChangeLastFrame = 0;
        }
        else
        {
            var preTickBloodAmount = Body.BloodAmount;
            var preTickBloodPercent = Body.BloodPercent;
            DoBloodLoss();
            if (Math.Abs(preTickBloodPercent - Body.BloodPercent) > .00001)
            {
                Body.BloodChangeLastFrame = Body.BloodPercent - preTickBloodPercent;
            }
            else
            {
                Body.BloodAmount = preTickBloodAmount;
                Body.BloodChangeLastFrame = 0;
            }
        }
    }

    public virtual void ConsumeEnergy(float baseAmount)
    {
        Body.Energy -= _ticksWithEmptyStomach > 0 ? baseAmount * EmptyStomachEnergyLossFactor : baseAmount;
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Body!, "Body");
        ScribeValues.Look(ref _ticksWithEmptyStomach, "TicksWithEmptyStomach");
    }

    protected virtual void Regenerate()
    {
        //todo fix this
        // return;
        // if (_ticksWithEmptyStomach > WorldTime.HoursToTicks(2) || Body.Energy < .2 || Body.BloodPercent < 0.05 || Body.IsWarm == false)
        // {
        //     return;
        // }
        //
        // if (Body.RootSocket.AttachedPart == null)
        // {
        //     return;
        // }
        //
        // var restingBoost = Body.Pawn.IsResting ? RestingMultiplier : 1;
        // // stop regenerating blood when near death
        //
        // if (Body.BloodAmount > 100)
        // {
        //     Body.BloodAmount += BloodRegenerationFactor * restingBoost;
        // }
        //
        // var partRegenerationFactor = HealthRegenerationFactor * restingBoost;
        //
        // void UpdateHealth(BodyPart bodyPart)
        // {
        //     if (bodyPart.IsDestroyed)
        //     {
        //         return;
        //     }
        //
        //     bodyPart.HitPoints += bodyPart.HitPoints * partRegenerationFactor;
        // }
        //
        // void DoRegeneration(BodyPart bodyPart)
        // {
        //     UpdateHealth(bodyPart);
        //     foreach (var internalPart in bodyPart.InternalParts)
        //     {
        //         UpdateHealth(internalPart);
        //     }
        //
        //     foreach (var externalPart in bodyPart.ExternalParts)
        //     {
        //         DoRegeneration(externalPart);
        //     }
        // }
        //
        // DoRegeneration(Body.RootSocket.AttachedPart);
    }

    protected virtual void TakeMalnutritionDamage()
    {
        foreach (var bodyPart in Body.AllParts)
        {
            if (bodyPart.Type == BodyPartType.Artery)
            {
                continue;
            }

            if (Core.Random.Chance(0.7f))
            {
                continue;
            }

            bodyPart.HitPoints -= bodyPart.HitPoints * MalnutritionDamageFactor;
        }
    }

    protected virtual void DoBloodLoss()
    {
        if (Body.RootSocket.AttachedPart == null)
        {
            return;
        }

        DoBloodLossForPart(Body.RootSocket.AttachedPart);
    }

    protected virtual void HandleNutrition()
    {
        if (Core.Context.Ticks % 30 != 0)
        {
            return;
        }

        var foodLossAmount = (Body.Pawn.IsResting ? FoodLossRestingFactor : 1f) * FoodLossPerIteration;
        Body.StomachLevel = Mathf.Clamp(Body.StomachLevel - foodLossAmount, 0, 1);

        if (Body.StomachLevel <= 0)
        {
            _ticksWithEmptyStomach++;
        }
        else
        {
            _ticksWithEmptyStomach = 0;
        }

        if (IsFamished)
        {
            Body.Handler.TakeMalnutritionDamage();
        }
    }

    private void DoBloodLossForPart(BodyPart part)
    {
        var bloodLossScaleFactor = .01f * (part.Size / Body.Def.BloodType.Viscosity / Body.BodySizeFactor);

        if (part.HealthPercent < BloodLossThreshold)
        {
            //Log.Info($"{_pawn} {part} losing {bloodLossScaleFactor * (1 - part.HealthPercent)}");
            Body.BloodAmount -= bloodLossScaleFactor * (1 - (float)part.HealthPercent);
        }

        // stop part traversal if part is an artery and it's been severed
        var continuePartTraversal = true;
        foreach (var internalPart in part.InternalParts)
        {
            if (internalPart.Type != BodyPartType.Artery || internalPart.HealthPercent >= 1)
            {
                continue;
            }

            if (internalPart.IsDestroyed)
            {
                //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * severedArteryBloodLossFactor}");
                Body.BloodAmount -= bloodLossScaleFactor * SeveredArteryBloodLossFactor;
                // Artery is severed stop propagating bleeding
                continuePartTraversal = false;
                continue;
            }

            //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * (1.3f - part.HealthPercent)}");
            Body.BloodAmount -= bloodLossScaleFactor * (ArteryBloodLossOffset - (float)part.HealthPercent);
        }

        foreach (var socket in part.Sockets)
        {
            if (socket.AttachedPart == null)
            {
                // part has been severed, start hemorrhaging
                if (socket.IsSealed == false)
                {
                    //Log.Info($"{_pawn} {socket} losing {bloodLossScaleFactor * severedLimbBloodLossFactor}");
                    Body.BloodAmount -= bloodLossScaleFactor * SeveredLimbBloodLossFactor;
                }

                continue;
            }

            if (continuePartTraversal && socket.AttachedPart?.IsExternal == true)
            {
                DoBloodLossForPart(socket.AttachedPart);
            }
        }
    }
}