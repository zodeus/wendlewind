namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class BoneCrackerHandler : TrinketHandler
{
    public const int BonesPerLevel = 12;
    private const int DefaultCooldown = 1200;
    private Label _cooldownLabel = null!;
    private int _combatBonesbroken;
    private int _selfInflictedBonesBroken;
    private int _level = 1;
    public int CombatBonesBroken => _combatBonesbroken;
    public int SelfInflictedBonesBroken => _selfInflictedBonesBroken;
    public int Level => _level;
    public int BonesForNextLevel => BonesPerLevel - ((_selfInflictedBonesBroken + _combatBonesbroken) % BonesPerLevel);
    
    public static readonly Dictionary<int, List<BodyPartType>> AllowedPartsPerLevel = new()
    {
        { 1, new List<BodyPartType> { BodyPartType.Paw, BodyPartType.Hoof, BodyPartType.Tail, BodyPartType.Finger, BodyPartType.Toe, BodyPartType.Thumb } },
        { 2, new List<BodyPartType> { BodyPartType.Foot, BodyPartType.Hand } },
        { 3, new List<BodyPartType> { BodyPartType.Leg, BodyPartType.Arm } },
        { 4, new List<BodyPartType> { BodyPartType.Neck, BodyPartType.Abdomen } },
        { 5, new List<BodyPartType> { BodyPartType.Head, BodyPartType.Torso, BodyPartType.Thorax, BodyPartType.Abdomen } },
    };

    public static int MaxLevel => AllowedPartsPerLevel.Keys.Max();

    /// <summary>
    /// Gets all body part types allowed at the specified level (cumulative from level 1).
    /// </summary>
    public static HashSet<BodyPartType> GetAllowedTypesUpToLevel(int level)
    {
        var allowedTypes = new HashSet<BodyPartType>();
        for (var i = 1; i <= level; i++)
        {
            if (AllowedPartsPerLevel.TryGetValue(i, out var types))
            {
                allowedTypes.UnionWith(types);
            }
        }
        return allowedTypes;
    }

    /// <summary>
    /// Checks if there are any breakable bones available at the current level.
    /// </summary>
    public bool HasAvailableBones(Pawn pawn)
    {
        var allowedTypes = GetAllowedTypesUpToLevel(_level);
        return pawn.Body.AllExternalParts
            .Where(part => allowedTypes.Contains(part.Type))
            .SelectMany(part => part.AllInternalParts)
            .Any(internalPart => internalPart.Substance == SubstanceType.Bone && !internalPart.IsDestroyed);
    }

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (IsActive == false || Cooldown > 0) return null;

        // Find a random body part on the target that has a non-broken bone
        var boneToBreak = FindUnbrokenBone(victim);
        if (boneToBreak == null) return null;

        // Break the bone
        _combatBonesbroken++;
        BreakBone(boneToBreak);
        Cooldown = DefaultCooldown;

        // Create damage record for the bone break
        List<DamagedBodyPartRecord> damagedParts =
        [
            new DamagedBodyPartRecord(boneToBreak)
            {
                DamageApplied = boneToBreak.MaxHitPoints,
                WasDestroyed = true,
                StoppedFunctioning = true
            }
        ];

        return new DamageRecord(
            Trinket.Label,
            "Bone Crack",
            DamageType.Magic,
            boneToBreak,
            boneToBreak.MaxHitPoints,
            amountBlocked: 0)
        {
            ActualAmount = boneToBreak.MaxHitPoints,
            BodyParts = damagedParts
        };
    }

    /// <summary>
    /// Breaks a random bone on the player's own body. Used by the UI panel.
    /// </summary>
    /// <returns>The bone that was broken, or null if no bones available.</returns>
    public BodyPart? BreakOwnBone()
    {
        var playerPawn = Core.Context.PlayerPawn;
        var boneToBreak = FindUnbrokenBone(playerPawn);
        if (boneToBreak == null) return null;

        _selfInflictedBonesBroken++;
        BreakBone(boneToBreak);

        return boneToBreak;
    }

    private BodyPart? FindUnbrokenBone(Pawn pawn)
    {
        // Build list of allowed body part types based on current level (cumulative)
        var allowedTypes = GetAllowedTypesUpToLevel(_level);
        var unbrokenBones = pawn.Body.AllExternalParts
            .Where(part => allowedTypes.Contains(part.Type))
            .SelectMany(part => part.AllInternalParts.Where(internalPart => internalPart.Substance == SubstanceType.Bone && !internalPart.IsDestroyed))
            .ToList();

        if (unbrokenBones.Count == 0) return null;

        return unbrokenBones[Core.Random.Next(unbrokenBones.Count)];
    }

    private void BreakBone(BodyPart bone)
    {
        bone.HitPoints = 0;
        if (BonesForNextLevel == BonesPerLevel)
        {
            _level++;
        }
    }

    public override void PrepareTrinketButton(CursorButton button)
    {
        var panel = new Panel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _cooldownLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Red,
            Visible = false
        };
        
        panel.Widgets.Add(_cooldownLabel);
        if (button.Content is Panel content)
        {
            content.Widgets.Add(panel);
        }
    }
    
      public override void OnClick()
    {
        if (Cooldown > 0) return;

        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    public override void Update(CursorButton button)
    {
        base.Update(button);

        if (Cooldown > 0)
        {
            _cooldownLabel.Text = Cooldown.ToString();
            _cooldownLabel.Visible = true;
        }
        else
        {
            _cooldownLabel.Visible = false;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _combatBonesbroken, "CombatBonesBroken");
        ScribeValues.Look(ref _selfInflictedBonesBroken, "SelfInflictedBonesBroken");
        ScribeValues.Look(ref _level, "Level", 1);
    }
}

