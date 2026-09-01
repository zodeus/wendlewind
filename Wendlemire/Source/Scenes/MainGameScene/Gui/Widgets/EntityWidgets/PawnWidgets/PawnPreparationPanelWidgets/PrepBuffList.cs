namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class PrepBuffList : VerticalStackPanel
{
    private static readonly string[] StatOrder =
    [
        "Strength",
        "AttackSpeed",
        "Accuracy",
        "Evasion",
        "MoveSpeed",
        "PhysicalResistance"
    ];

    public PrepBuffList()
    {
        Spacing = 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    public void SetEffects(IEnumerable<BodyEffectDef> effects)
    {
        Widgets.Clear();
        var unique = effects
            .Where(e => e != null)
            .Distinct()
            .ToList();

        var offsets = new Dictionary<StatDef, float>();
        var factors = new Dictionary<StatDef, float>();
        var specials = new List<string>();

        foreach (var def in unique)
        {
            var hasStats = false;
            if (def.AffectedStats != null)
            {
                foreach (var record in def.AffectedStats)
                {
                    if (record.Stat == null)
                    {
                        continue;
                    }

                    if (record.Offset is { } offset)
                    {
                        Add(offsets, record.Stat, offset);
                        hasStats = true;
                    }

                    if (record.Factor is { } factor)
                    {
                        Add(factors, record.Stat, factor);
                        hasStats = true;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(def.Notes))
            {
                specials.Add(def.Notes);
            }
            else if (!hasStats)
            {
                var text = !string.IsNullOrWhiteSpace(def.Description) && def.Description != "undefined"
                    ? def.Description
                    : def.Label;
                if (!string.IsNullOrWhiteSpace(text) && text != "undefined")
                {
                    specials.Add(text);
                }
            }
        }

        var stats = offsets.Keys
            .Concat(factors.Keys)
            .Distinct()
            .OrderBy(StatRank)
            .ThenBy(s => s.Label)
            .ToList();

        if (stats.Count == 0 && specials.Count == 0)
        {
            Visible = false;
            return;
        }

        foreach (var stat in stats)
        {
            Widgets.Add(CreateStatRow(
                stat.Label,
                offsets.GetValueOrDefault(stat),
                factors.GetValueOrDefault(stat),
                offsets.ContainsKey(stat),
                factors.ContainsKey(stat)));
        }

        foreach (var special in specials.Distinct())
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = special,
                TextColor = new Color(200, 180, 140),
                Wrap = true
            });
        }

        Visible = true;
    }

    public static IEnumerable<BodyEffectDef> FromMeal(Pawn pawn)
    {
        var seen = new HashSet<BodyEffectDef>();
        foreach (var item in pawn.MealPlan.Items)
        {
            var records = item?.ItemDef.FoodProperties?.Effects;
            if (records == null)
            {
                continue;
            }

            foreach (var record in records)
            {
                if (record.Def == null)
                {
                    continue;
                }

                if (record.Def == Defs.BodyEffects.FoodPoisoning && pawn.Traits.HasTrait(Defs.Traits.GutMicroacrobatics))
                {
                    continue;
                }

                if (seen.Add(record.Def))
                {
                    yield return record.Def;
                }
            }
        }
    }

    public static IEnumerable<BodyEffectDef> FromIncense(Pawn pawn)
    {
        var seen = new HashSet<BodyEffectDef>();
        foreach (var incense in pawn.ActiveIncense)
        {
            if (incense.Def != null && seen.Add(incense.Def))
            {
                yield return incense.Def;
            }
        }
    }

    public static IEnumerable<BodyEffectDef> FromPrep(Pawn pawn)
    {
        return FromMeal(pawn).Concat(FromIncense(pawn));
    }

    private static void Add(Dictionary<StatDef, float> totals, StatDef stat, float value)
    {
        totals[stat] = totals.GetValueOrDefault(stat) + value;
    }

    private static int StatRank(StatDef stat)
    {
        var index = Array.IndexOf(StatOrder, stat.Moniker);
        return index >= 0 ? index : StatOrder.Length;
    }

    private static Widget CreateStatRow(string name, float offset, float factor, bool hasOffset, bool hasFactor)
    {
        var values = new HorizontalStackPanel { Spacing = 8 };
        if (hasOffset)
        {
            values.Widgets.Add(ValueLabel(FormatOffset(offset), offset >= 0));
        }

        if (hasFactor)
        {
            values.Widgets.Add(ValueLabel(FormatFactor(factor), factor >= 0));
        }

        var row = new HorizontalStackPanel { Spacing = 10 };
        row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = name,
            TextColor = new Color(180, 180, 180),
            Width = 110
        });
        row.Widgets.Add(values);
        return row;
    }

    private static Label ValueLabel(string text, bool positive)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = positive ? new Color(100, 200, 100) : new Color(220, 100, 100)
        };
    }

    private static string FormatOffset(float value)
    {
        var sign = value > 0 ? "+" : "";
        return Math.Abs(value - MathF.Round(value)) < 0.001f
            ? $"{sign}{value:0}"
            : $"{sign}{value:0.##}";
    }

    private static string FormatFactor(float value)
    {
        var percent = value * 100f;
        var sign = percent > 0 ? "+" : "";
        return $"{sign}{percent:0.#}%";
    }
}
