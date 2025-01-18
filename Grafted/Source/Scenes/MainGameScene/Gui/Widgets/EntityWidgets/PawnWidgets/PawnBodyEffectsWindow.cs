namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class PawnBodyEffectsWindow : Window
{
    private readonly Pawn _pawn;
    private Dictionary<BodyEffect, BodyEffectRow> _cachedEffects = new();
    private readonly VerticalStackPanel _container;
    private List<BodyEffect> _effectsToRemove = new();

    public PawnBodyEffectsWindow(Pawn pawn)
    {
        TitlePanel.Visible = false;
        //CloseButton.Visible = false;
        _pawn = pawn;
        Title = "Effects";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
//        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold], new Color(255, 255, 255, 100));
        _container = new VerticalStackPanel() { Spacing = 20 };
        Content = _container;
    }

    public void Update()
    {
        foreach (BodyEffect effect in _pawn.Body.Effects)
        {
            if (_cachedEffects.ContainsKey(effect))
            {
                continue;
            }

            _cachedEffects.Add(effect, new BodyEffectRow(effect));
            _container.Widgets.Add(_cachedEffects[effect]);
        }

        foreach ((var effect, var panel) in _cachedEffects)
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
        }

        Visible = _cachedEffects.Any();
    }

    private sealed class BodyEffectRow : VerticalStackPanel
    {
        private readonly BodyEffect _effect;
        private readonly Label _durationLabel;

        public BodyEffectRow(BodyEffect effect)
        {
            _effect = effect;
            _durationLabel = new Label(BaseContent.Styles.Label.Small);
            Widgets.Add(new Label { Text = effect.Def.Label });


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
}