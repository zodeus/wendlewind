namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public enum EffectsPanelOrientation
{
    Vertical,   // Stack vertically, overflow to new columns
    Horizontal  // Stack horizontally, overflow to new rows
}

public sealed class PawnBodyEffectsPanel : Panel, IUpdatable
{
    private const int MaxItemsVertical = 5;
    private const int MaxItemsHorizontal = 8;

    private Dictionary<BodyEffect, BodyEffectRow> _cachedEffects = new();
    private List<BodyEffect> _effectsToRemove = new();
    private StackPanel _container = null!;

    private readonly Pawn _pawn;
    private readonly EffectsPanelOrientation _orientation;

    public PawnBodyEffectsPanel(BaseGui gui, Pawn pawn, EffectsPanelOrientation orientation = EffectsPanelOrientation.Horizontal)
    {
        _pawn = pawn;
        _orientation = orientation;

        // Create the outer container based on orientation
        // Both need VerticalAlignment.Bottom so items grow upward and don't get clipped at top
        if (_orientation == EffectsPanelOrientation.Vertical)
        {
            // Vertical: columns arranged horizontally, items stacked vertically within each column
            _container = new HorizontalStackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Top };
        }
        else
        {
            // Horizontal: rows arranged vertically, items stacked horizontally within each row
            _container = new VerticalStackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Top };
        }

        Widgets.Add(_container);
    }

    private void RebuildLayout()
    {
        _container.Widgets.Clear();

        var effects = _cachedEffects.Values.ToList();

        if (_orientation == EffectsPanelOrientation.Vertical)
        {
            // Vertical orientation: columns with up to MaxItemsVertical items each
            // Columns grow right to left (newest columns on the left)
            var columnCount = (effects.Count + MaxItemsVertical - 1) / MaxItemsVertical;

            // Iterate in reverse so columns are added right to left
            for (int col = columnCount - 1; col >= 0; col--)
            {
                var column = new VerticalStackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Top };
                _container.Widgets.Add(column);

                for (int row = 0; row < MaxItemsVertical; row++)
                {
                    var index = col * MaxItemsVertical + row;
                    if (index < effects.Count)
                    {
                        column.Widgets.Add(effects[index]);
                    }
                }
            }
        }
        else
        {
            // Horizontal orientation: rows with up to MaxItemsHorizontal items each
            var rowCount = (effects.Count + MaxItemsHorizontal - 1) / MaxItemsHorizontal;

            for (int r = 0; r < rowCount; r++)
            {
                var row = new HorizontalStackPanel { Spacing = 2 };
                _container.Widgets.Add(row);

                for (int col = 0; col < MaxItemsHorizontal; col++)
                {
                    var index = r * MaxItemsHorizontal + col;
                    if (index < effects.Count)
                    {
                        row.Widgets.Add(effects[index]);
                    }
                }
            }
        }
    }

    public void Update()
    {
        var needsRebuild = false;

        foreach (BodyEffect effect in _pawn.Body.Effects)
        {
            if (_cachedEffects.ContainsKey(effect))
            {
                continue;
            }

            var panel = new BodyEffectRow(effect);
            _cachedEffects.Add(effect, panel);
            needsRebuild = true;
        }

        foreach (var (effect, panel) in _cachedEffects)
        {
            if (effect.IsExpired)
            {
                _cachedEffects[effect].RemoveFromParent();
                _effectsToRemove.Add(effect);
            }

            panel.Update();
        }

        if (_effectsToRemove.Any())
        {
            foreach (var bodyEffect in _effectsToRemove)
            {
                _cachedEffects.Remove(bodyEffect);
            }

            _effectsToRemove.Clear();
            needsRebuild = true;
        }

        if (needsRebuild)
        {
            RebuildLayout();
        }

        Visible = _cachedEffects.Any();
    }

    private sealed class BodyEffectRow : Panel, IUpdatable
    {
        private readonly BodyEffect _effect;
        private readonly Label _durationLabel;
        private PawnBodyEffectTooltip? _tooltipContent;

        public BodyEffectRow(BodyEffect effect)
        {
            _effect = effect;
            _durationLabel = new Label(BaseContent.Styles.Label.Small)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0, 0, 0, 2),
            };

            var button = new CursorButton
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                Width = BaseContent.IconSizes.Large,
                Height = BaseContent.IconSizes.Large,
                Padding = new Thickness(10, 10, 10, 6),
                Content = new Panel
                {
                    Background = new TextureRegion(effect.Def.GetTexture()),
                    Widgets =
                    {
                        _durationLabel
                    }
                }
            };
            button.WithTooltip(() =>
            {
                _tooltipContent ??= new PawnBodyEffectTooltip(_effect);
                return _tooltipContent;
            });

            Width = BaseContent.IconSizes.Large;
            Height = BaseContent.IconSizes.Large;
            Widgets.Add(button);
        }

        public void Update()
        {
            var ticks = _effect.TicksLeft;
            _durationLabel.Text = FormatTicks(ticks);

            // Lerp from red (0 ticks) to green (5000+ ticks)
            var t = Math.Clamp(ticks / 5000f, 0f, 1f);
            _durationLabel.TextColor = Color.Lerp(Color.Red, Color.LawnGreen, t);

            TooltipHelper.UpdatePosition();

            // Update tooltip content
            _tooltipContent?.Update();
        }
    }

    internal static string FormatTicks(int ticks) => ticks >= 10000 ? $"{ticks / 1000}k" : $"{ticks}";
}

public sealed class PawnBodyEffectTooltip : VerticalStackPanel
{
    private readonly BodyEffect _effect;
    private readonly Label _durationLabel;

    public PawnBodyEffectTooltip(BodyEffect effect)
    {
        _effect = effect;
        _durationLabel = new Label(BaseContent.Styles.Label.Small) { TextColor = Color.LightBlue };

        Widgets.Add(new HorizontalStackPanel
        {
            Widgets = {
                new Label(BaseContent.Styles.Label.Small) { Text = "Duration:", Width = 120 },
                _durationLabel
            }
        });
        if (effect.Def.Notes != null)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = effect.Def.Notes, Wrap = true });
        }

        if (effect.Def.AffectedStats == null)
        {
            return;
        }

        foreach (var affectedStat in effect.Def.AffectedStats)
        {
            var factor = affectedStat.Factor != null ? $"/c[{(affectedStat.Factor > 0 ? TC.Green : TC.Red)}]*{affectedStat.Factor} " : "";
            var offset = affectedStat.Offset != null ? $"/c[{(affectedStat.Offset > 0 ? TC.Green : TC.Red)}]+{affectedStat.Offset} " : "";
            Widgets.Add(new HorizontalStackPanel
            {
                Widgets = {
                    new Label(BaseContent.Styles.Label.Small) { Text = $"{affectedStat.Stat.Label}", Width = 120 },
                    new Label(BaseContent.Styles.Label.Small) { Text = $"{offset}{factor}" },
                }
            });
        }
    }

    public void Update()
    {
        _durationLabel.Text = $"{_effect.TicksLeft:N0}";
    }
}