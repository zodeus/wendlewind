using Wendlewind.Sim.Entities.Pawns.Modifiers;

namespace Wendlewind.Presentation;

public static class ModifierInfoPanels
{
    public static Widget? GetInfoPanel(this BodyPartModifier modifier)
    {
        var data = modifier.GetInfoData();
        return data == null ? null : Build(modifier, data);
    }

    private static Widget Build(BodyPartModifier modifier, InfoPanelData data)
    {
        var panel = new VerticalStackPanel { Spacing = 6 };

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = data.Title ?? modifier.Def.Label,
            TextColor = modifier.Def.Color
        });

        if (data.Damage.HasValue)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• -{data.Damage.Value:0.##} {data.DamageSuffix}",
                TextColor = data.DamageColor ?? InfoColors.Damage
            });
        }

        if (data.Healing.HasValue)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• +{data.Healing.Value:0.##} {data.HealingSuffix}",
                TextColor = data.HealingColor ?? InfoColors.Cure
            });
        }

        foreach (var line in data.Lines)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• {line.Text}",
                TextColor = line.Color
            });
        }

        if (data.HasSpread)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "• Has spread to adjacent parts",
                TextColor = InfoColors.Spread
            });
        }

        if (data.HasPenetrated)
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "• Has penetrated deeper",
                TextColor = InfoColors.Penetrated
            });
        }

        if (!string.IsNullOrEmpty(data.CuredBy))
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• Cured by: {data.CuredBy}",
                TextColor = InfoColors.Cure
            });
        }

        if (!string.IsNullOrEmpty(data.BlockedBy))
        {
            panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• Blocked by: {data.BlockedBy}",
                TextColor = InfoColors.Cure
            });
        }

        var timeRemaining = modifier.DurationInTicks == 0 ? "∞" : $"{modifier.TicksRemaining}t";
        var timeText = data.ShowPower
            ? $"{data.TimePrefix}: {timeRemaining} | Power: {modifier.Power:0.#}x"
            : $"{data.TimePrefix}: {timeRemaining}";
        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = timeText,
            TextColor = data.TimeColor ?? InfoColors.Muted
        });

        return panel;
    }
}
