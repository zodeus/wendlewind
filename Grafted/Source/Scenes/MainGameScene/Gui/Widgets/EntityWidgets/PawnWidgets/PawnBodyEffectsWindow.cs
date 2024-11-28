namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnBodyEffectsWindow : Window {
    private readonly Pawn _pawn;
    private Dictionary<BodyEffect, BodyEffectRow> _cachedEffects = new();
    private readonly VerticalStackPanel _container;
    private List<BodyEffect> _effectsToRemove = new();

    public PawnBodyEffectsWindow(Pawn pawn) {
        _pawn = pawn;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold], new Color(255, 255, 255, 50));
        _container = new VerticalStackPanel();
        Content = _container;
    }

    public void Update() {
        foreach (BodyEffect effect in _pawn.Body.Effects) {
            if (_cachedEffects.ContainsKey(effect)) {
                continue;
            }

            _cachedEffects.Add(effect, new BodyEffectRow(effect));
            _container.AddChild(_cachedEffects[effect]);
        }

        foreach ((BodyEffect? effect, BodyEffectRow? panel) in _cachedEffects) {
            if (effect.IsExpired) {
                _cachedEffects[effect].RemoveFromParent();
                _effectsToRemove.Add(effect);
            }

            panel.Update();
        }

        if (_effectsToRemove.Any()) {
            foreach (BodyEffect bodyEffect in _effectsToRemove) {
                _cachedEffects.Remove(bodyEffect);
            }

            _effectsToRemove.Clear();
        }
    }

    private class BodyEffectRow : VerticalStackPanel {
        private readonly BodyEffect _effect;
        private readonly Label _durationLabel;

        public BodyEffectRow(BodyEffect effect) {
            _effect = effect;
            _durationLabel = new Label(BaseContent.Styles.Label.Small);
            AddChild(new Label { Text = effect.Def.Label });
            AddChild(_durationLabel);
            if (effect.Def.AffectedStats == null) {
                return;
            }

            foreach (AffectedStatRecord affectedStat in effect.Def.AffectedStats) {
                string factor = affectedStat.Factor != null ? $"/c[{(affectedStat.Factor > 0 ?TC.Green :TC.Red)}]*{affectedStat.Factor} " : "";
                string offset = affectedStat.Offset != null ? $"/c[{(affectedStat.Offset > 0 ?TC.Green :TC.Red)}]+{affectedStat.Offset} " : "";
                AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"  {affectedStat.Stat.Label} {offset}{factor}" });
            }
        }

        public void Update() {
            _durationLabel.Text = $"  Ticks left /c[{TC.Blue}] {_effect.TicksLeft}";
        }
    }
}