using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class StageEndGui : SimulationGui {

    public StageEndGui() {
       

        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
        //todo fairly certain there is an issue here, deregister this event when gui's change?
        Core.Instance.Window.TextInput += (_, a) => {
            Desktop.OnChar(a.Character);
        };
    }
    

    private Widget GenerateProgressButton() {
        TextButton button;
            button = new TextButton(BaseContent.Styles.Button.Large) {
                Text = "Carry on",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            button.Click += (_, _) => {
                Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            };

        return new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {  button }
        };
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {

        base.Render(spriteBatch, deltaTime);
    }
}