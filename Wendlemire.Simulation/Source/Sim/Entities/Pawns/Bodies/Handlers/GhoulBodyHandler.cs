﻿namespace Wendlemire.Sim.Entities.Pawns.Bodies.Handlers;

public class GhoulBodyHandler : DefaultBodyHandler
{
    public GhoulBodyHandler(IRng rng)
    {
        Rng = rng;
    }

    private const int RegenerationCooldownTicks = 30;
    private int _ticksSinceLastCheck;

    public override void Tick()
    {
        base.Tick();
        _ticksSinceLastCheck++;

        if (_ticksSinceLastCheck >= RegenerationCooldownTicks)
        {
            _ticksSinceLastCheck = 0;
            RegenerateFingers();
        }
    }

    private void RegenerateFingers()
    {
        var hands = Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand);
        foreach (var hand in hands)
        {
            // Check thumb sockets
            foreach (var thumbSocket in hand.GetSocketsFor(BodyPartType.Thumb).Where(s => s.AttachedPart == null && !s.IsSealed))
            {
                GhoulBodyGenerator.MakeFingerForSocket(thumbSocket, Defs.BodyParts.GhoulThumb);
            }

            // Check finger sockets
            foreach (var fingerSocket in hand.GetSocketsFor(BodyPartType.Finger).Where(s => s.AttachedPart == null && !s.IsSealed))
            {
                GhoulBodyGenerator.MakeFingerForSocket(fingerSocket, Defs.BodyParts.GhoulFinger);
            }
        }
    }

    protected override void HandleBlood()
    {
    }

    public override void OnPartSevered(BodyPart part)
    {
    }

    protected override void HandleNutrition()
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _ticksSinceLastCheck, "TicksSinceLastCheck");
    }
}