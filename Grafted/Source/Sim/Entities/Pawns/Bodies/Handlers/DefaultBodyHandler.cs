namespace Grafted.Sim.Entities.Pawns.Bodies.Handlers;

public class DefaultBodyHandler : IExposable {
    private int _ticksWithEmptyStomach;

    public PawnBody Body = null!;

    public virtual float RestingMultiplier => 2;
    public virtual float SeveredArteryBloodLossFactor => 4f;
    public virtual float SeveredLimbBloodLossFactor => 6f;
    public virtual float BloodLossThreshold => .95f;
    public virtual float MaxDestroyedPartBloodLoss => 10f;
    public virtual float MaxSeveredPartBloodLoss => 15f;
    public virtual float ArteryBloodLossOffset => 1.3f;
    public virtual float BloodRegenerationFactor => 1f;
    public virtual float HealthRegenerationFactor => 0.001f;
    public virtual int MalnutritionDamageIntervalInMinutes => 10;
    public virtual float EnergyLossPerTick => 0.0004f;
    public virtual float FoodLossPerTick => 0.0015f;
    public virtual float FoodLossRestingFactor => 0.60f;
    public virtual int TicksUntilFamished => 4000;
    public virtual int EmptyStomachEnergyLossFactor => 2;
    public virtual float HungryThreshold => 0.6f;
    public virtual float MalnutritionDamageFactor => Core.Random.NextFloat(0.0001f, 0.0005f);
    public virtual bool IsFamished => _ticksWithEmptyStomach > TicksUntilFamished;
    public bool IsHungry => Body.StomachLevel < HungryThreshold;

    public virtual void Initialize(PawnBody body) {
        Body = body;
    }

    public virtual void Tick(int ticks) {
        
        if (ticks % 90 != 0) return;

        PushExternalHeat();
        HandleNutrition();
        ConsumeEnergy(EnergyLossPerTick);
        if (Body.RootSocket.AttachedPart == null) {
            Body.BloodAmount = 0;
            Body.BloodChangeLastFrame = 0;
        }
        else {
            // Blood Loss Calculations & regeneration
            float preTickBloodAmount = Body.BloodAmount;
            float preTickBloodPercent = Body.BloodPercent;
            DoBloodLoss(ticks);
            Regenerate();
            if (Math.Abs(preTickBloodPercent - Body.BloodPercent) > .00001) {
                Body.BloodChangeLastFrame = Body.BloodPercent - preTickBloodPercent;
            }
            else {
                Body.BloodAmount = preTickBloodAmount;
                Body.BloodChangeLastFrame = 0;
            }
        }
    }

    public virtual void ConsumeEnergy(float baseAmount) {
        Body.Energy -= _ticksWithEmptyStomach > 0 ? baseAmount * EmptyStomachEnergyLossFactor : baseAmount;
    }

    public virtual void PushExternalHeat() {
        const float hotThreshold = 40;
        const float coolThreshold = 18;
        const float optimalBodyTemperature = 32;
        const float roomTemperature = 22;
        float amountOfHeatToPush = 1;
        float externalTemp = Body.Pawn.Zone?.Temperature ?? 0;

        if (externalTemp > hotThreshold) {
            Body.Temperature = Math.Min(Body.Temperature + amountOfHeatToPush, externalTemp + 10);
        }
        else if (externalTemp is >= coolThreshold and <= hotThreshold) {
            if (Body.Temperature > optimalBodyTemperature) {
                Body.Temperature = Math.Max(Body.Temperature - amountOfHeatToPush, optimalBodyTemperature);
            }
            else {
                if (Body.Temperature < 32) {
                    Body.Temperature = Math.Min(Body.Temperature + amountOfHeatToPush, optimalBodyTemperature);
                }
            }
        }
        else if (externalTemp < coolThreshold) {
            Body.Temperature = Math.Max(Body.Temperature - amountOfHeatToPush, externalTemp + 10);
        }
    }

    public virtual void ExposeData() {
        Scribe_References.Look(ref Body!, "Body");
        Scribe_Values.Look(ref _ticksWithEmptyStomach, "TicksWithEmptyStomach");
    }

    protected virtual void Regenerate() {
        if (_ticksWithEmptyStomach > 1 || Body.Energy < .2 || Body.BloodPercent < 0.05 || Body.IsWarm == false) {
            return;
        }

        if (Body.RootSocket.AttachedPart == null) {
            return;
        }

        float restingBoost = Body.Pawn.IsResting ? RestingMultiplier : 1;
        // stop regenerating blood when near death

        if (Body.BloodAmount > 100) {
            Body.BloodAmount += BloodRegenerationFactor * restingBoost;
        }

        float partRegenerationFactor = HealthRegenerationFactor * restingBoost;

        void UpdateHealth(BodyPart bodyPart) {
            if (bodyPart.IsDestroyed) {
                return;
            }

            bodyPart.HitPoints += bodyPart.HitPoints * partRegenerationFactor;
        }

        void DoRegeneration(BodyPart bodyPart) {
            UpdateHealth(bodyPart);
            foreach (BodyPart internalPart in bodyPart.InternalParts) {
                UpdateHealth(internalPart);
            }

            foreach (BodyPart externalPart in bodyPart.ExternalParts) {
                DoRegeneration(externalPart);
            }
        }

        DoRegeneration(Body.RootSocket.AttachedPart);
    }

    protected virtual void TakeMalnutritionDamage() {

        foreach (BodyPart bodyPart in Body.AllParts) {
            if (bodyPart.Type == BodyPartType.Artery) {
                continue;
            }

            if (Core.Random.Chance(0.7f)) {
                continue;
            }

            bodyPart.HitPoints -= bodyPart.HitPoints * MalnutritionDamageFactor;
        }
    }

    protected virtual void DoBloodLoss(int ticks) {
        if (Body.RootSocket.AttachedPart == null) {
            return;
        }
        DoBloodLossForPart(Body.RootSocket.AttachedPart);
    }

    protected virtual void HandleNutrition() {
        float foodLossAmount = (Body.Pawn.IsResting ? FoodLossRestingFactor : 1f) * FoodLossPerTick;
        Body.StomachLevel = Mathf.Clamp(Body.StomachLevel - foodLossAmount, 0, 1);

        if (Body.StomachLevel <= 0) {
            _ticksWithEmptyStomach++;
        }
        else {
            _ticksWithEmptyStomach = 0;
        }

        // Malnutrition Calculations
        if (IsFamished) {
            Body.Handler.TakeMalnutritionDamage();
        }
    }

    private void DoBloodLossForPart(BodyPart part) {
        float bloodLossScaleFactor = part.Size / Body.Def.BloodType.Viscosity / Body.BodySizeFactor;

        if (part.HealthPercent < BloodLossThreshold) {
            //Log.Info($"{_pawn} {part} losing {bloodLossScaleFactor * (1 - part.HealthPercent)}");
            Body.BloodAmount -= bloodLossScaleFactor * (1 - part.HealthPercent);
        }

        // stop part traversal if part is an artery and it's been severed
        bool continuePartTraversal = true;
        foreach (BodyPart internalPart in part.InternalParts) {
            if (internalPart.Type != BodyPartType.Artery || internalPart.HealthPercent >= 1) {
                continue;
            }

            if (internalPart.IsDestroyed) {
                //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * severedArteryBloodLossFactor}");
                Body.BloodAmount -= Math.Max(bloodLossScaleFactor * SeveredArteryBloodLossFactor, MaxDestroyedPartBloodLoss);
                // Artery is severed stop propagating bleeding
                continuePartTraversal = true;
                continue;
            }

            //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * (1.3f - part.HealthPercent)}");
            Body.BloodAmount -= bloodLossScaleFactor * (ArteryBloodLossOffset - part.HealthPercent);
        }

        foreach (BodyPartSocket socket in part.Sockets) {
            if (socket.AttachedPart == null) {
                // part has been severed, start hemorrhaging
                if (socket.IsSealed == false) {
                    //Log.Info($"{_pawn} {socket} losing {bloodLossScaleFactor * severedLimbBloodLossFactor}");
                    Body.BloodAmount -= Math.Max(bloodLossScaleFactor * SeveredLimbBloodLossFactor, MaxSeveredPartBloodLoss);
                }

                continue;
            }

            if (continuePartTraversal && socket.AttachedPart?.IsExternal == true) {
                DoBloodLossForPart(socket.AttachedPart);
            }
        }
    }
}