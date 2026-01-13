namespace Grafted.Sim.Entities.Pawns.Modifiers;

public static class BodyPartModifierGenerator
{
    public static BodyPartModifier Generate(BodyPartModifierDef def, int duration, double power)
    {
        BodyPartModifier modifier = (BodyPartModifier)Activator.CreateInstance(def.HandlerClass)!;
        modifier.Def = def;
        modifier.Id = Core.Context.IdProvider.NextBodyPartModifierId();
        modifier.DurationInTicks = duration;
        modifier.Power = power;
        modifier.Initialize();
        return modifier;
    }
}

public enum BodyPartModifierEventType
{
    Added,
    Removed
}

public abstract class BodyPartModifier : IExposable, IIdentityProvider
{
    public BodyPart BodyPart = null!;
    public BodyPartModifierDef Def = null!;
    public int Ticks;
    public int DurationInTicks;
    public int Id = -1;
    public bool IsExpired;
    public int Severity = 1;
    public string Maneuver = "undefined";
    public double Power = 1.0;
    public int TicksRemaining => DurationInTicks - Ticks;
    public string Label => Def.Label;
    public virtual List<SubstanceType> AllowedSubstances => [];

    public virtual void Tick()
    {
        Ticks++;
        if (Ticks >= DurationInTicks)
        {
            IsExpired = true;
        }
    }

    public virtual void Initialize()
    {
    }

    public virtual void SpreadTo(BodyPart part)
    {
        if (AllowedSubstances.Count > 0 && AllowedSubstances.Contains(part.Substance) == false) return;
        
        if (part.HasModifier(Def))
        {
            return;
        }

        var ticksRemaining = DurationInTicks - Ticks;
        var spreadDuration = Math.Max(ticksRemaining, 3); // Minimum 3 ticks to prevent cascade
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Def, spreadDuration, Power));
    }

    public virtual void MergeWith(BodyPartModifier modifier)
    {
        DurationInTicks += modifier.DurationInTicks;
    }

    public virtual void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeReferences.Look(ref BodyPart!, "BodyPart");
        ScribeValues.Look(ref Id, "Id");
        ScribeValues.Look(ref Ticks, "Ticks");
        ScribeValues.Look(ref DurationInTicks, "DurationInTicks");
        ScribeValues.Look(ref IsExpired, "IsExpired");
        ScribeValues.Look(ref Severity, "Severity");
        ScribeValues.Look(ref Power, "Power");
    }

    public string GetUniqueId()
    {
        return "BodyPartModifier_" + Id;
    }

    public override string ToString()
    {
        return $"{Def.Moniker} Id: {Id}";
    }


    public virtual bool ApplyToPart(BodyPart part)
    {
        throw new NotImplementedException();
    }

    protected virtual void CheckIfLostVitalPart()
    {

        if (BodyPart.Body == null)
        {
            Log.Warning($"BodyPartModifier.CheckIfLostVitalPart failed to get Body for {BodyPart}");
            return;
        }

        if (BodyPart.Body.Pawn.IsDeadFromPartFailure() is { } deathRecord)
        {
            deathRecord.CauseOfDeath = $"{deathRecord.FailedOrgan} failure from {Def.Label}";
            deathRecord.KillingWeapon = Def.Label;
            deathRecord.KillingManeuver = "Affliction";
            BodyPart.Body.Pawn.TriggerDeath(deathRecord);
        }
    }

    /// <summary>
    /// Gets a custom info panel widget for this body part modifier.
    /// Override to provide a custom panel with infographics or detailed explanations.
    /// </summary>
    /// <returns>A custom widget, or null to use the default panel</returns>
    public virtual Widget? GetInfoPanel() => null;

    #region Info Panel Builder

    protected static class InfoColors
    {
        public static readonly Color Damage = new(255, 120, 80);
        public static readonly Color Spread = new(255, 180, 80);
        public static readonly Color Penetrated = new(255, 100, 100);
        public static readonly Color Cure = new(130, 200, 130);
        public static readonly Color Muted = new(150, 150, 150);
        public static readonly Color Warning = new(200, 160, 80);
        public static readonly Color Info = new(180, 220, 240);
    }

    protected record InfoLine(string Text, Color Color);

    protected class InfoPanelData
    {
        public string? Title { get; init; }
        public double? Damage { get; init; }
        public string DamageSuffix { get; init; } = "damage/tick";
        public Color? DamageColor { get; init; }
        public double? Healing { get; init; }
        public string HealingSuffix { get; init; } = "health/tick";
        public Color? HealingColor { get; init; }
        public List<InfoLine> Lines { get; init; } = [];
        public bool HasSpread { get; init; }
        public bool HasPenetrated { get; init; }
        public string? CuredBy { get; init; }
        public string? BlockedBy { get; init; }
        public bool ShowPower { get; init; }
        public string TimePrefix { get; init; } = "Time";
        public Color? TimeColor { get; init; }
    }

    protected Widget BuildInfoPanel(InfoPanelData data)
    {
        var panel = new VerticalStackPanel { Spacing = 6 };

        // Title
        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = data.Title ?? Def.Label,
            TextColor = Def.Color
        });

        // Damage line
        if (data.Damage.HasValue)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• -{data.Damage.Value:0.##} {data.DamageSuffix}",
                TextColor = data.DamageColor ?? InfoColors.Damage
            });
        }

        // Healing line
        if (data.Healing.HasValue)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• +{data.Healing.Value:0.##} {data.HealingSuffix}",
                TextColor = data.HealingColor ?? InfoColors.Cure
            });
        }

        // Custom lines
        foreach (var line in data.Lines)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• {line.Text}",
                TextColor = line.Color
            });
        }

        // Spread status
        if (data.HasSpread)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "• Has spread to adjacent parts",
                TextColor = InfoColors.Spread
            });
        }

        // Penetration status
        if (data.HasPenetrated)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "• Has penetrated deeper",
                TextColor = InfoColors.Penetrated
            });
        }

        // Cure info
        if (!string.IsNullOrEmpty(data.CuredBy))
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• Cured by: {data.CuredBy}",
                TextColor = InfoColors.Cure
            });
        }

        // Blocked by info
        if (!string.IsNullOrEmpty(data.BlockedBy))
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• Blocked by: {data.BlockedBy}",
                TextColor = InfoColors.Cure
            });
        }

        // Time remaining
        var timeRemaining = DurationInTicks == 0 ? "∞" : $"{TicksRemaining}t";
        var timeText = data.ShowPower 
            ? $"{data.TimePrefix}: {timeRemaining} | Power: {Power:0.#}x" 
            : $"{data.TimePrefix}: {timeRemaining}";
        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = timeText,
            TextColor = data.TimeColor ?? InfoColors.Muted
        });

        return panel;
    }

    #endregion
}