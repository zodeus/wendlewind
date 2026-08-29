namespace Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// A body handler that allows the Hydra to regenerate severed heads.
/// When a head is destroyed/severed, it regrows with double the previous max health.
/// Each head regeneration also increases the pawn's strength by 1.
/// </summary>
[UsedImplicitly]
public class HydraBodyHandler : DefaultBodyHandler
{
    private const int RegenerationCooldownTicks = 120; // Ticks between regeneration checks
    private const float HitPointsMultiplier = 1.5f;
    private int _ticksSinceLastCheck;
    
    // Track original max HP for heads that have been severed
    private Dictionary<int, float> _headMaxHealthOnSever = new();
    
    // Track accumulated strength bonus from head regenerations
    public float StrengthBonus { get; private set; }
    
    public override void Tick()
    {
        base.Tick();
        _ticksSinceLastCheck++;
        
        if (_ticksSinceLastCheck >= RegenerationCooldownTicks)
        {
            _ticksSinceLastCheck = 0;
            RegenerateHeads();
        }
    }
    
    public override void ModifyStat(StatDef stat, ref float value)
    {
        if (stat == Defs.Stats.Strength)
        {
            value += StrengthBonus;
        }
    }

    private void RegenerateHeads()
    {
        // Torso is the root socket for Hydra
        var torso = Body.RootSocket.AttachedPart;
        if (torso == null)
        {
            return;
        }

        // Check each head socket on the torso
        foreach (var socket in torso.GetSocketsFor(BodyPartType.Head))
        {
            if (socket.AttachedPart == null && !socket.IsSealed)
            {
                // Head was severed, regenerate it!
                RegenerateHead(socket);
            }
            else if (socket.AttachedPart is { IsDestroyed: true })
            {
                // Head is destroyed but still attached - sever it first to trigger regrowth
                var destroyedHead = socket.AttachedPart;
                var previousMaxHp = (float)destroyedHead.MaxHitPoints;
                _headMaxHealthOnSever[socket.Id] = previousMaxHp;

                // Severe the destroyed head
                destroyedHead.Severe();
            }
        }
    }
    
    private void RegenerateHead(BodyPartSocket socket)
    {
        // Determine which head def to use based on socket position
        BodyPartDef headDef = socket.Position switch
        {
            BodyPartPosition.Left => Defs.BodyParts.HydraHeadTwo,
            BodyPartPosition.Right => Defs.BodyParts.HydraHeadThree,
            _ => Defs.BodyParts.HydraHeadOne // Center head
        };
        
        // Create the new head
        var newHead = socket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(headDef));
        
        // Double the max health based on what it was when severed
        if (_headMaxHealthOnSever.TryGetValue(socket.Id, out var previousMaxHp))
        {
            var newMaxHp = previousMaxHp * HitPointsMultiplier;
            newHead.MaxHitPoints = newMaxHp;
            newHead.HitPoints = newMaxHp;
            _headMaxHealthOnSever.Remove(socket.Id);
        }
        else
        {
            // First regeneration - double from base
            newHead.MaxHitPoints *= HitPointsMultiplier;
            newHead.HitPoints = newHead.MaxHitPoints;
        }
        
        // Add internal parts (skin, bone, artery)
        newHead.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        newHead.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        newHead.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        
        // Equip teeth weapon on the new head
        newHead.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(
            DefRepository<ItemDef>.GetByMoniker("HydraTeeth")!);
        
        // Increase strength bonus by 1
        StrengthBonus += 1f;
        
        Log.Info($"Hydra regenerated a head! New max HP: {newHead.MaxHitPoints}, Strength bonus now: {StrengthBonus}");
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _ticksSinceLastCheck, "TicksSinceLastCheck");
        
        var strengthBonus = StrengthBonus;
        ScribeValues.Look(ref strengthBonus, "StrengthBonus");
        StrengthBonus = strengthBonus;
        
        ScribeCollections.Look(ref _headMaxHealthOnSever!, "HeadMaxHealthOnSever", LookMode.Value, LookMode.Value);
    }
}
