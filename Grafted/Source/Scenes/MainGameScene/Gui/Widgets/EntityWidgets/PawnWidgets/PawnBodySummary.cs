namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnBodySummary : Grid {
    private readonly Dictionary<BodyPart, Image> _bodyParts;

    public PawnBodySummary(BaseGui gui, PawnBody body) {
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

            Image image = new() { Background = new ColoredRegion(new TextureRegion(part.WhiteIcon), Color.White), Width = 64, Height = 64, GridRow = gridRow, GridColumn = gridColumn++ };
            image.TouchDown += (_, _) => gui.ViewEntity(part);
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