using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Entities.Items.Weapons;

/// <summary>
/// Handler for the Strange Withered Twig unique weapon.
/// Deals no base damage but applies a random assortment of debilitating modifiers to the victim.
/// The twig channels ancient, decaying energies that corrupt the body in strange ways.
/// 
/// Upgrades:
/// - Level 1: 3-5 afflictions per hit (from 2-4)
/// - Level 2: Duration increased to 30-120 ticks (from 10-60)
/// - Level 3: Adds Burning and Acid to possible afflictions
/// </summary>
[UsedImplicitly]
public class StrangeWitheredTwigHandler : WeaponHandler, IUpgradableHandler
{
    // Base values
    private const int BaseMinModifiers = 2;
    private const int BaseMaxModifiers = 4;
    private const int BaseMinDuration = 60;
    private const int BaseMaxDuration = 120;
    
    // Level 1 upgrade: increased affliction count
    private const int Level1MinModifiers = 3;
    private const int Level1MaxModifiers = 5;
    
    // Level 2 upgrade: increased duration
    private const int Level2MinDuration = 120;
    private const int Level2MaxDuration = 360;
    
    private int _charges;
    private int _chargesToTrigger = 1;
    private int _upgradeLevel;
    
    // Tracking stats
    private int _totalModifiersInflicted;
    private int _totalCasts;

    // IUpgradableHandler implementation
    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Weapon.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;

    // Computed properties based on upgrade level
    private int MinModifiersToApply => _upgradeLevel >= 1 ? Level1MinModifiers : BaseMinModifiers;
    private int MaxModifiersToApply => _upgradeLevel >= 1 ? Level1MaxModifiers : BaseMaxModifiers;
    private int MinDurationTicks => _upgradeLevel >= 2 ? Level2MinDuration : BaseMinDuration;
    private int MaxDurationTicks => _upgradeLevel >= 2 ? Level2MaxDuration : BaseMaxDuration;
    private bool HasAcid => _upgradeLevel >= 3;
    private bool HasBurning => _upgradeLevel >= 3;

    // Available modifiers to apply and their weights
    private (BodyPartModifierDef Def, float Weight)[] GetPossibleModifiers()
    {
        var baseModifiers = new List<(BodyPartModifierDef Def, float Weight)>
        {
            (Defs.BodyPartModifiers.Festering, 0.5f),
            (Defs.BodyPartModifiers.BloodDrain, 0.20f),
            (Defs.BodyPartModifiers.RotLung, 0.05f),
            (Defs.BodyPartModifiers.Necrosis, 0.1f),
        };
        
        // Level 3: Add Burning
        if (HasBurning)
        {
            baseModifiers.Add((Defs.BodyPartModifiers.Burning, 0.50f));
        }

        // Level 3: Add Acid
        if (HasAcid)
        {
            baseModifiers.Add((Defs.BodyPartModifiers.Acid, 0.50f));
        }
        return baseModifiers.ToArray();
    }

    public override void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
        _charges++;
        if (_charges < _chargesToTrigger)
        {
            return;
        }
        var targetPart = damageRecord.BodyPartHit;
        if (targetPart == null)
        {
            return;
        }

        _charges = 0;
        _totalCasts++;
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
            var modifier = BodyPartModifierGenerator.Generate(modifierDef.Def, duration, 1.0);
            // Apply to the hit part
            modifier.ApplyToPart(targetPart);
            appliedModifiers.Add(modifierDef.Def.Label);
            _totalModifiersInflicted++;
        }

        // Report what modifiers were applied
        if (appliedModifiers.Count > 0)
        {
            var effectsList = string.Join(", ", appliedModifiers);
            damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(
                victim,
                Weapon.ItemDef,
                $"Withered Twig inflicts: {effectsList}"));
        }
    }

    public override Widget CreateInfoWidget(BaseGui gui)
    {
        var infoPanel = new VerticalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Top };

        // Upgrade level display
        if (_upgradeLevel > 0)
        {
            var upgradeLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Upgrade Level: {_upgradeLevel}",
                TextColor = new Color(218, 165, 32) // Gold
            };
            infoPanel.Widgets.Add(upgradeLabel);
        }

        // Unique mechanic explanation
        var mechanicPanel = new VerticalStackPanel { Spacing = 3 };
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Corrupting Touch",
            TextColor = new Color(139, 90, 43)
        });
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"• {MinModifiersToApply}-{MaxModifiersToApply} afflictions per hit",
            TextColor = _upgradeLevel >= 1 ? new Color(100, 200, 100) : new Color(160, 130, 100),
            Wrap = true,
            MaxWidth = 240
        });
        mechanicPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"• Duration: {MinDurationTicks / 60f:0.#}-{MaxDurationTicks / 60f:0.#}s",
            TextColor = _upgradeLevel >= 2 ? new Color(100, 200, 100) : new Color(140, 110, 80),
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

        AddStatRow(statsGrid, 0, "Casts", $"{_totalCasts}", new Color(139, 90, 43));
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
        ScribeValues.Look(ref _totalCasts, "TotalHits");
        ScribeValues.Look(ref _chargesToTrigger, "ChargesToTrigger");
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }
}
