namespace Wendlemire.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// A body handler for orcs that provides delayed regeneration for destroyed body parts.
/// When an external part is destroyed, after 120-240 ticks, RhinoRestoration is applied
/// to the part and all its internal parts.
/// When a part is severed, the orc's strength is multiplied by 1.3.
/// </summary>
[UsedImplicitly]
public class OrcBodyHandler : RegeneratingEyesBodyHandler
{
    public OrcBodyHandler(IRng rng) : base(rng)
    {
    }

    private const int MinRegenerationDelay = 120;
    private const int MaxRegenerationDelay = 240;
    private const int RhinoRestorationDuration = 600;
    private const float StrengthMultiplierPerSever = 1.3f;
    private const double RhinoPower = 1.5;

    // Track destroyed external parts and their countdown timers
    // Key: BodyPart Id, Value: Ticks remaining until restoration
    private Dictionary<int, int> _destroyedPartTimers = new();
    
    // Track which parts we've already started timers for to avoid duplicates
    private HashSet<int> _partsWithActiveTimers = new();
    
    // Track the number of times parts have been severed to calculate strength multiplier
    private int _severedPartCount;
    
    public float StrengthMultiplier => (float)Math.Pow(StrengthMultiplierPerSever, _severedPartCount);

    public override void Tick()
    {
        base.Tick();
        if (Body.EnergyPercent < 0.9f)
        {
            Body.Energy = Body.MaxEnergy;
        } 
        CheckForDamagedParts();
        ProcessTimers();
    }

    public override void ModifyStat(StatDef stat, ref float value)
    {
        if (stat == Defs.Stats.Strength && _severedPartCount > 0)
        {
            value *= StrengthMultiplier;
        }
    }

    private void CheckForDamagedParts()
    {
        foreach (var part in Body.AllExternalParts)
        {
            // Check if part is destroyed and we haven't already started a timer for it
            if (part.HealthPercent < 0.2f && !_partsWithActiveTimers.Contains(part.Id))
            {
                StartRegenerationTimer(part);
            }
            
            // Check if part was severed (no longer in AllExternalParts means it was severed)
            // We track this by checking sockets with missing parts
            CheckForSeveredPartsIn(part);
        }
    }

    private void CheckForSeveredPartsIn(BodyPart part)
    {
        foreach (var socket in part.Sockets)
        {
            if (socket.IsExternal && socket.AttachedPart == null && !socket.IsSealed)
            {
                // Part was severed - seal it and increase strength
                socket.IsSealed = true;
                _severedPartCount++;
                Log.Info($"Orc part severed! Strength multiplier now: {StrengthMultiplier:F2}x (severed count: {_severedPartCount})");
            }
        }
    }

    private void StartRegenerationTimer(BodyPart part)
    {
        var delay = Context.Rng.Next(MinRegenerationDelay, MaxRegenerationDelay + 1);
        _destroyedPartTimers[part.Id] = delay;
        _partsWithActiveTimers.Add(part.Id);
        Log.Info($"Orc regeneration timer started for {part.Label}: {delay} ticks");
    }

    private void ProcessTimers()
    {
        var partsToRestore = new List<int>();
        
        foreach (var kvp in _destroyedPartTimers)
        {
            _destroyedPartTimers[kvp.Key] = kvp.Value - 1;
            
            if (_destroyedPartTimers[kvp.Key] <= 0)
            {
                partsToRestore.Add(kvp.Key);
            }
        }

        foreach (var partId in partsToRestore)
        {
            var part = Body.AllExternalParts.FirstOrDefault(p => p.Id == partId);
            if (part != null)
            {
                ApplyRhinoRestoration(part);
            }
            
            _destroyedPartTimers.Remove(partId);
            _partsWithActiveTimers.Remove(partId);
        }
    }

    private void ApplyRhinoRestoration(BodyPart part)
    {
        // Apply RhinoRestoration to the external part
        ApplyRhinoRestorationToPart(part);
        
        // Apply RhinoRestoration to all internal parts
        foreach (var internalPart in part.InternalParts)
        {
            ApplyRhinoRestorationToPart(internalPart);
        }
        
        Log.Info($"Orc RhinoRestoration applied to {part.Label} and its {part.InternalParts.Count} internal parts");
    }

    private void ApplyRhinoRestorationToPart(BodyPart part)
    {
        if (part.HasModifier(Defs.BodyPartModifiers.RhinoRestoration))
        {
            return;
        }
        
        var modifier = Context.Factory.CreateModifier(
            Defs.BodyPartModifiers.RhinoRestoration, 
            RhinoRestorationDuration,
            RhinoPower);
        
        modifier.ApplyToPart(part);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _severedPartCount, "SeveredPartCount");
        ScribeCollections.Look(ref _destroyedPartTimers!, "DestroyedPartTimers", LookMode.Value, LookMode.Value);
        ScribeCollections.Look(ref _partsWithActiveTimers!, "PartsWithActiveTimers", LookMode.Value);
    }
}
