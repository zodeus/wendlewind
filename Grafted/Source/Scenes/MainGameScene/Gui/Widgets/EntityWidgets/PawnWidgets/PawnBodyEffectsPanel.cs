using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class PawnBodyEffectsPanel : HorizontalStackPanel, IUpdatable
{
    private Dictionary<BodyEffect, BodyEffectRow> _cachedEffects = new();
    private List<BodyEffect> _effectsToRemove = new();

    private Window? _effectWindow;
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;

    public PawnBodyEffectsPanel(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 0;
    }

    public void Update()
    {
        foreach (BodyEffect effect in _pawn.Body.Effects)
        {
            if (_cachedEffects.ContainsKey(effect))
            {
                continue;
            }

            var panel = new BodyEffectRow(effect);
            panel.EffectClicked += OnEffectClicked;
            _cachedEffects.Add(effect, panel);
            Widgets.Add(_cachedEffects[effect]);
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
                _cachedEffects[bodyEffect].EffectClicked -= OnEffectClicked;
                _cachedEffects.Remove(bodyEffect);
            }

            _effectsToRemove.Clear();
        }

        ((PawnBodyEffectPanel?)_effectWindow?.Content)?.Update();
        Visible = _cachedEffects.Any();
    }

    private void OnEffectClicked(BodyEffect effect, Point position)
    {
        if (_effectWindow?.IsPlaced == true)
        {
            _effectWindow.Close();
        }

        _effectWindow = new Window
        {
            Title = effect.Def.Label,
            Content = new PawnBodyEffectPanel(effect)
        };
        _effectWindow.Show(_gui.Desktop, position);
    }

    private sealed class BodyEffectRow : VerticalStackPanel, IUpdatable
    {
        private readonly BodyEffect _effect;
        private readonly Label _durationLabel;

        public BodyEffectRow(BodyEffect effect)
        {
            Spacing = 0;
            _effect = effect;
            _durationLabel = new Label(BaseContent.Styles.Label.Small)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                //Border = new SolidBrush(Color.Azure),
                //BorderThickness = new Thickness(1)
            };
            var button = new Button
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                Width = BaseContent.IconSizes.Large,
                Height = BaseContent.IconSizes.Large,
                Content = new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets =
                    {
                        new Image
                        {
                            HorizontalAlignment = HorizontalAlignment.Center, 
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = new TextureRegion(effect.Def.Texture),
                            Height = (int)(BaseContent.IconSizes.Medium),
                            Width = (int)(BaseContent.IconSizes.Medium)
                        }
                    }
                }
            };
            button.Click += (_, _) =>
            {
                // Convert screen mouse position to UI coordinate space (accounting for UI scaling)
                var screenPos = Mouse.GetState().Position;
                var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
                var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);
                EffectClicked?.Invoke(_effect, new Point(uiX, uiY + 30));
            };
            Widgets.Add(button);
            Widgets.Add(_durationLabel);
        }

        public event Action<BodyEffect, Point>? EffectClicked;

        public void Update()
        {
            _durationLabel.Text = $"{_effect.TicksLeft}";
        }
    }
}

public sealed class PawnBodyEffectPanel : VerticalStackPanel
{
    private readonly BodyEffect _effect;
    private readonly Label _durationLabel;

    public PawnBodyEffectPanel(BodyEffect effect)
    {
        _effect = effect;
        _durationLabel = new Label(BaseContent.Styles.Label.Small);

        Widgets.Add(_durationLabel);
        if (effect.Def.Notes != null)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = effect.Def.Notes, Wrap = true, Margin = new Thickness(26, 0, 0, 0) });
        }

        if (effect.Def.AffectedStats == null)
        {
            return;
        }

        foreach (var affectedStat in effect.Def.AffectedStats)
        {
            var factor = affectedStat.Factor != null ? $"/c[{(affectedStat.Factor > 0 ? TC.Green : TC.Red)}]*{affectedStat.Factor} " : "";
            var offset = affectedStat.Offset != null ? $"/c[{(affectedStat.Offset > 0 ? TC.Green : TC.Red)}]+{affectedStat.Offset} " : "";
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"  {affectedStat.Stat.Label} {offset}{factor}" });
        }
    }

    public void Update()
    {
        _durationLabel.Text = $"  Ticks left /c[{TC.Blue}] {_effect.TicksLeft}";
    }
}