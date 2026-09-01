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

    private static readonly Color TotalColor = new(200, 180, 120);

    private Pawn? _totalsPawn;
    private Dictionary<StatDef, float> _offsets = [];
    private Dictionary<StatDef, float> _factors = [];
    private readonly List<AttributeRow> _attributeRows = [];
    private string _attributeSignature = "";

    private readonly record struct AttributeRow(StatDef Stat, Widget Widget, Label TotalLabel);

    public PrepBuffList()
    {
        Spacing = 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    public void SetEffects(IEnumerable<BodyEffectDef> effects)
    {
        ClearTotalsState();
        Widgets.Clear();
        Collect(effects, out var offsets, out var factors, out var specials);

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

        AddSpecials(specials);
        Visible = true;
    }

    public void SetAttributes(Pawn pawn, IEnumerable<BodyEffectDef> effects)
    {
        _totalsPawn = pawn;
        Collect(effects, out _offsets, out _factors, out var specials);

        var stats = CoreStats()
            .Concat(_offsets.Keys)
            .Concat(_factors.Keys)
            .Where(s => s.UiDisplay)
            .Distinct()
            .OrderBy(StatRank)
            .ThenBy(s => s.Label)
            .ToList();

        var signature = string.Join(",", stats.Select(s =>
                          $"{s.Moniker}:{_offsets.GetValueOrDefault(s)}:{_factors.GetValueOrDefault(s)}"))
                      + "|"
                      + string.Join(",", specials);

        if (signature != _attributeSignature)
        {
            _attributeSignature = signature;
            Widgets.Clear();
            _attributeRows.Clear();

            foreach (var stat in stats)
            {
                var hasOffset = _offsets.ContainsKey(stat);
                var hasFactor = _factors.ContainsKey(stat);
                var row = CreateAttributeRow(
                    stat,
                    _offsets.GetValueOrDefault(stat),
                    _factors.GetValueOrDefault(stat),
                    hasOffset,
                    hasFactor);
                _attributeRows.Add(row);
                Widgets.Add(row.Widget);
            }

            AddSpecials(specials);
        }

        RefreshTotals();
        Visible = true;
    }

    public void Update()
    {
        RefreshTotals();
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

    private void ClearTotalsState()
    {
        _totalsPawn = null;
        _offsets = [];
        _factors = [];
        _attributeRows.Clear();
        _attributeSignature = "";
    }

    private void RefreshTotals()
    {
        if (_totalsPawn == null)
        {
            return;
        }

        foreach (var row in _attributeRows)
        {
            var hasOffset = _offsets.ContainsKey(row.Stat);
            var hasFactor = _factors.ContainsKey(row.Stat);
            row.TotalLabel.Text = FormatTotal(ProjectedValue(
                _totalsPawn,
                row.Stat,
                _offsets.GetValueOrDefault(row.Stat),
                _factors.GetValueOrDefault(row.Stat),
                hasOffset,
                hasFactor));
        }
    }

    private static void Collect(
        IEnumerable<BodyEffectDef> effects,
        out Dictionary<StatDef, float> offsets,
        out Dictionary<StatDef, float> factors,
        out List<string> specials)
    {
        offsets = new Dictionary<StatDef, float>();
        factors = new Dictionary<StatDef, float>();
        specials = [];

        foreach (var def in effects.Where(e => e != null).Distinct())
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

        specials = specials.Distinct().ToList();
    }

    private static IEnumerable<StatDef> CoreStats()
    {
        return DefRepository<StatDef>.Defs.Where(s => s.UiDisplay);
    }

    private static float ProjectedValue(
        Pawn pawn,
        StatDef stat,
        float offset,
        float factor,
        bool hasOffset,
        bool hasFactor)
    {
        var value = pawn.GetStatValue(stat);
        if (hasFactor)
        {
            value += value * factor;
        }

        if (hasOffset)
        {
            value += offset;
        }

        return value;
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

    private void AddSpecials(IEnumerable<string> specials)
    {
        foreach (var special in specials)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = special,
                TextColor = new Color(200, 180, 140),
                Wrap = true
            });
        }
    }

    private static AttributeRow CreateAttributeRow(
        StatDef stat,
        float offset,
        float factor,
        bool hasOffset,
        bool hasFactor)
    {
        var values = new HorizontalStackPanel { Spacing = 8 };
        var total = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = TotalColor
        };
        values.Widgets.Add(total);

        if (hasOffset)
        {
            values.Widgets.Add(ValueLabel($"({FormatOffset(offset)})", offset >= 0));
        }

        if (hasFactor)
        {
            values.Widgets.Add(ValueLabel($"({FormatFactor(factor)})", factor >= 0));
        }

        var row = new HorizontalStackPanel { Spacing = 10 };
        row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = stat.Label,
            TextColor = new Color(180, 180, 180),
            Width = 110
        });
        row.Widgets.Add(values);
        return new AttributeRow(stat, row, total);
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

    private static string FormatTotal(float value)
    {
        return $"{value:0.00}";
    }
}
