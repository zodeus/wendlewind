using System.Collections.Generic;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.CombatGuis;

internal class CombatPartyPanel : VerticalStackPanel {
    private readonly List<PawnCombatPanel> _panels;
    private readonly HorizontalStackPanel _deathRow;

    public CombatPartyPanel(CombatEvent combatEvent, List<Pawn> pawns, HorizontalAlignment pawnAlignment) {
        Spacing = 5;
        ShowGridLines = false;
        _panels = new List<PawnCombatPanel>();
        HorizontalStackPanel pawnRow = new() {
            MinHeight = 380,
            //Border = new SolidBrush(Color.Aquamarine),
            //BorderThickness = new Thickness(1),
            Spacing = 15, HorizontalAlignment = pawnAlignment,
            Margin = new Thickness(30, 0, 30, 0),
        };
        AddChild(pawnRow);
        _deathRow = new() {
            Spacing = 20,
            HorizontalAlignment = pawnAlignment,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Panel.FancyBar],
            Height = 100,
            Padding = new Thickness(60, 0, 60, 0),
            Width = 680,
        };
        AddChild(_deathRow);
        foreach (Pawn pawn in pawns) {
            bool isPlayer = pawn.PawnDef.PawnType == PawnType.Player;
            PawnCombatPanel panel = new(pawn, combatEvent, isPlayer);
            _panels.Add(panel);
            pawnRow.AddChild(panel);
        }
    }

    public void Update() {
        for (int i = _panels.Count - 1; i >= 0; i--) {
            PawnCombatPanel panel = _panels[i];
            if (panel.Pawn.IsDead) {
                panel.RemoveFromParent();
                _panels.RemoveAt(i);
                _deathRow.AddChild(new Image { Background = new TextureRegion(panel.Pawn.Icon), Width = 56, Height = 56, VerticalAlignment = VerticalAlignment.Center });
                continue;
            }

            panel.Update();
        }
    }
}