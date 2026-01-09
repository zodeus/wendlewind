using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Entities.Items.Weapons;

/// <summary>
/// Handler for the Strange Withered Twig unique weapon.
/// Deals no base damage but applies a random assortment of debilitating modifiers to the victim.
/// The twig channels ancient, decaying energies that corrupt the body in strange ways.
/// </summary>
[UsedImplicitly]
public class StrangeWitheredTwigHandler : WeaponHandler
{
    private const int MinModifiersToApply = 2;
    private const int MaxModifiersToApply = 5;
    private const int MinDurationTicks = 300;
    private const int MaxDurationTicks = 1200;
    
    // Tracking stats
    private int _totalModifiersInflicted;
    private int _totalHits;

    // Available modifiers to apply and their weights
    private static (BodyPartModifierDef Def, float Weight)[] GetPossibleModifiers() =>
    [
        (Defs.BodyPartModifiers.Necrosis, 0.15f),
        (Defs.BodyPartModifiers.Festering, 0.25f),
        (Defs.BodyPartModifiers.RotLung, 0.20f),
        (Defs.BodyPartModifiers.Acid, 0.15f),
        (Defs.BodyPartModifiers.BloodDrain, 0.25f)
    ];

    public override void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
        // The twig always channels its strange power, even on weak hits
        var targetPart = damageRecord.BodyPartHit;
        if (targetPart == null || targetPart.IsDestroyed)
        {
            return;
        }

        _totalHits++;
        
        // Determine how many modifiers to apply this hit
        var modifierCount = Core.Random.Next(MinModifiersToApply, MaxModifiersToApply + 1);
        var appliedModifiers = new List<string>();

        // Try to apply random modifiers
        var possibleModifiers = GetPossibleModifiers();
        var shuffledModifiers = possibleModifiers.InRandomOrder().Take(modifierCount).ToList();
        
        foreach (var modifierDef in shuffledModifiers)
        {
            // Create the modifier with random duration using the generator
            var duration = Core.Random.Next(MinDurationTicks, MaxDurationTicks + 1);
            var modifier = BodyPartModifierGenerator.Generate(modifierDef.Def, duration);

            // Apply to the hit part
            targetPart.TryAddModifier(modifier);
            appliedModifiers.Add(modifierDef.Def.Label);
            _totalModifiersInflicted++;
        }

        // Report what modifiers were applied
        if (appliedModifiers.Count > 0)
        {
            var effectsList = string.Join(", ", appliedModifiers);
            damageRecord.DamageStatusEffects.Add(new DamageStatusEffect(
                victim,
                Weapon.ItemDef,
                $"Withered Twig inflicts: {effectsList}"));
        }
    }

    public override Widget CreateInfoWidget(BaseGui gui)
    {
        var infoPanel = new VerticalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Top };
        
        // Unique mechanic explanation
        var mechanicPanel = new VerticalStackPanel { Spacing = 3 };
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "⚗ Corrupting Touch",
            TextColor = new Color(139, 90, 43)
        });
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"• {MinModifiersToApply}-{MaxModifiersToApply} afflictions per hit",
            TextColor = new Color(160, 130, 100),
            Wrap = true,
            MaxWidth = 240
        });
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"• Duration: {MinDurationTicks / 60f:0.#}-{MaxDurationTicks / 60f:0.#}s",
            TextColor = new Color(140, 110, 80),
            Wrap = true,
            MaxWidth = 240
        });
        infoPanel.Widgets.Add(mechanicPanel);
        
        // Possible afflictions (wrap into grid for compact display)
        infoPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });
        var afflictionsPanel = new VerticalStackPanel { Spacing = 2 };
        afflictionsPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Afflictions:",
            TextColor = Color.Gray
        });
        
        var modsFlow = new VerticalStackPanel { Spacing = 1 };
        foreach (var mod in GetPossibleModifiers())
        {
            modsFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• {mod.Def.Label}",
                TextColor = mod.Def.Color
            });
        }
        afflictionsPanel.Widgets.Add(modsFlow);
        infoPanel.Widgets.Add(afflictionsPanel);
        
        // Stats section
        infoPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });
        var statsGrid = new Grid { ColumnSpacing = 10, RowSpacing = 1 };
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        
        AddStatRow(statsGrid, 0, "Hits", $"{_totalHits}", new Color(139, 90, 43));
        AddStatRow(statsGrid, 1, "Afflictions", $"{_totalModifiersInflicted}", Color.IndianRed);
        
        infoPanel.Widgets.Add(statsGrid);
        
        return infoPanel;
    }
    
    private static void AddStatRow(Grid grid, int row, string label, string value, Color valueColor)
    {
        var keyLabel = new Label(BaseContent.Styles.Label.Small) { Text = $"{label}:", TextColor = Color.Gray };
        Grid.SetRow(keyLabel, row);
        Grid.SetColumn(keyLabel, 0);
        grid.Widgets.Add(keyLabel);
        
        var valueLabel = new Label(BaseContent.Styles.Label.Small) { Text = value, TextColor = valueColor };
        Grid.SetRow(valueLabel, row);
        Grid.SetColumn(valueLabel, 1);
        grid.Widgets.Add(valueLabel);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _totalModifiersInflicted, "TotalModifiersInflicted");
        ScribeValues.Look(ref _totalHits, "TotalHits");
    }
}
