namespace Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

public class DefaultBodyHandler : IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;

    public DefaultBodyHandler()
    {
    }

    public DefaultBodyHandler(IRng rng)
    {
        Rng = rng;
    }
    public event Action<Pawn, float>? OnBloodLost;
    private const float FixedBloodLossFactor = .01f;
    private int _ticksWithEmptyStomach;
    public PawnBody Body = null!;
    public virtual float SeveredArteryBloodLossFactor => 1.7f;
    public virtual float SeveredLimbBloodLossFactor => 3f;
    public virtual float BloodLossThreshold => .95f;
    public virtual float ArteryBloodLossOffset => 1.15f;
    public virtual float FoodLossPerIteration => 0.0017f;
    public virtual int TicksUntilFamished => 7200;
    public virtual int EmptyStomachEnergyLossFactor => 2;
    public virtual float HungryThreshold => 0.85f;
    public virtual float MalnutritionDamageFactor => Context.Rng.NextFloat(0.0001f, 0.0005f);
    public virtual bool IsFamished => _ticksWithEmptyStomach > TicksUntilFamished;
    public bool IsHungry => Body.StomachLevel < HungryThreshold;
    public float Viscosity => Body.Def.BloodType!.Viscosity;

    private float? _hasThickBloodedTrait;
    public float ViscosityModifier => _hasThickBloodedTrait ??= Body.Pawn.Traits.HasTrait(Defs.Traits.ThickBlooded) ? 1.2f : 1f;

    public virtual void Initialize(PawnBody body)
    {
        Body = body;
    }

    public virtual void Tick()
    {
        HandleNutrition();
        HandleBlood();
    }

    public virtual void ModifyStat(StatDef stat, ref float value)
    {
    }

    public virtual void ConsumeEnergy(float baseAmount)
    {
        Body.Energy -= _ticksWithEmptyStomach > 0 ? baseAmount * EmptyStomachEnergyLossFactor : baseAmount;
    }

    public virtual void OnPartSevered(BodyPart part)
    {
        if (Body.Def.BloodType == null)
        {
            return;
        }

        var bodyWeight = Body.AllParts.Sum(p => p.BloodAmount);
        if (bodyWeight <= 0)
        {
            return;
        }

        var preBlood = Body.BloodAmount;
        var prePercent = Body.BloodPercent;
        var loss = preBlood * (part.GetSubtreeBloodWeight() / bodyWeight);
        if (loss <= 0)
        {
            return;
        }

        Body.BloodAmount -= loss;
        Body.BloodChangeLastFrame = Body.BloodPercent - prePercent;
        OnBloodLost?.Invoke(Body.Pawn, preBlood - Body.BloodAmount);
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Body!, "Body");
        ScribeValues.Look(ref _ticksWithEmptyStomach, "TicksWithEmptyStomach");
    }

    protected virtual void TakeMalnutritionDamage()
    {
        foreach (var bodyPart in Body.AllParts)
        {
            if (bodyPart.Type == BodyPartType.Artery)
            {
                continue;
            }

            if (Context.Rng.Chance(0.7f))
            {
                continue;
            }

            bodyPart.HitPoints -= bodyPart.HitPoints * MalnutritionDamageFactor;
        }
    }

    protected virtual void HandleBlood()
    {
        if (Body.RootSocket.AttachedPart == null)
        {
            Body.BloodAmount = 0;
            Body.BloodChangeLastFrame = 0;
        }
        else
        {
            if (Body.Def.BloodType == null || Body.RootSocket.AttachedPart == null)
            {
                return;
            }

            var preTickBloodAmount = Body.BloodAmount;
            var preTickBloodPercent = Body.BloodPercent;

            DoBloodLossForPart(Body.RootSocket.AttachedPart);

            if (Math.Abs(preTickBloodPercent - Body.BloodPercent) > .00001)
            {
                Body.BloodChangeLastFrame = Body.BloodPercent - preTickBloodPercent;
                OnBloodLost?.Invoke(Body.Pawn, preTickBloodAmount - Body.BloodAmount);
            }
            else
            {
                Body.BloodAmount = preTickBloodAmount;
                Body.BloodChangeLastFrame = 0;
            }
        }
    }

    protected virtual void HandleNutrition()
    {
        if (Context.CurrentZone?.ActiveEncounter?.State == EncounterState.InProgress)
        {
            return;
        }

        if (Context.Ticks % 20 != 0)
        {
            return;
        }

        var foodLossAmount = FoodLossPerIteration;
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
        var bloodLossScaleFactor = FixedBloodLossFactor * (part.BloodAmount / (Viscosity * ViscosityModifier) / Body.BodySizeFactor);

        if (part.HealthPercent < BloodLossThreshold)
        {
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
                Body.BloodAmount -= bloodLossScaleFactor * SeveredArteryBloodLossFactor;
                // Artery is severed stop propagating bleeding
                continuePartTraversal = false;
                continue;
            }

            Body.BloodAmount -= bloodLossScaleFactor * (ArteryBloodLossOffset - (float)part.HealthPercent);
        }

        foreach (var socket in part.Sockets)
        {
            if (socket.AttachedPart == null)
            {
                // part has been severed, start hemorrhaging
                if (socket.IsSealed == false)
                {
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