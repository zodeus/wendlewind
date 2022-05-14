using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnBodySummary : Grid {
    private readonly Dictionary<BodyPart, Image> _bodyParts;

    public PawnBodySummary(PawnBody body) {
        //ShowGridLines = true;
        _bodyParts = new Dictionary<BodyPart, Image>();
        ColumnSpacing = 5;
        Padding = new Thickness(6, 6, 6, 6);
        //Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        int gridColumn = 0;
        var partsToIgnore = new List<BodyPartType> {
            BodyPartType.Finger, BodyPartType.Thumb, BodyPartType.Foot
        };
        int gridRow = 0;
        foreach (BodyPart part in body.AllExternalParts) {
            if (partsToIgnore.Contains(part.Type)) { continue; }

            Image image = new() { Background = new ColoredRegion(new TextureRegion(part.Icon), Color.White), Width = 24, Height = 24, GridRow = gridRow, GridColumn = gridColumn++ };
            image.TouchDown += (_, _) => Core.Sim.Gui!.ViewEntity(part);
            _bodyParts.Add(part, image);
            AddChild(image);
            if (gridColumn > 5) {
                gridColumn = 0;
                gridRow++;
            }
        }
    }

    public void Update() {
        foreach ((BodyPart bodyPart, Image image) in _bodyParts) {
            if (bodyPart.IsSevered) {
                image.RemoveFromParent();
                _bodyParts.Remove(bodyPart);
                continue;
            }
            Color color = BodyPartColor.Get(bodyPart);
            ((ColoredRegion) image.Background).Color = color;
        }
    }
}