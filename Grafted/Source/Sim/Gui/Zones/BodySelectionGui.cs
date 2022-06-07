using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Zones;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using SharpDX.Direct2D1;
using Image = Myra.Graphics2D.UI.Image;

namespace Grafted.Sim.Gui.Zones;

public class BodySelectionGui : ZoneGui {
    public override void Initialize(Zone zone) {
        Desktop = new Desktop {
            Root = new VerticalStackPanel {
                Margin = new Thickness(0, 50, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Widgets = {
                    new Label { Text = "Husk Selection", Font = BaseContent.Fonts.Fancy.VeryLarge, HorizontalAlignment = HorizontalAlignment.Center },
                    new HorizontalStackPanel {
                        Spacing = 50,
                        Widgets = {
                            new VerticalStackPanel {
                                Widgets = {
                                    new Image { Background = new TextureRegion(Defs.Races.Journeyman.Icon), Width = 128, Height = 128 },
                                    new Label(BaseContent.Styles.Label.Medium) {
                                        Text = Defs.Races.Journeyman.Label
                                    }
                                }
                            },
                            new VerticalStackPanel {
                                Widgets = {
                                    new Image { Background = new TextureRegion(Defs.Races.Ghoul.Icon), Width = 128, Height = 128 },
                                    new Label(BaseContent.Styles.Label.Medium) {
                                        Text = Defs.Races.Journeyman.Label
                                    }
                                }
                            }
                        }
                    }
                }
            },
            HasExternalTextInput = true
        };
    }
}