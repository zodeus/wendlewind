using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class PawnBodySummary : Grid, IUpdatable
{
    private readonly Dictionary<BodyPart, Image> _bodyParts;

    public PawnBodySummary(BaseGui gui, PawnBody body)
    {
        //Border = new SolidBrush(Color.Pink);
        //BorderThickness = new Thickness(1);
        _bodyParts = new Dictionary<BodyPart, Image>();
        ColumnSpacing = 5;
        Padding = new Thickness(6, 6, 6, 6);
        var gridColumn = 0;
        var partsToIgnore = new List<BodyPartType>
        {
            BodyPartType.Finger, BodyPartType.Thumb
        };
        var gridRow = 0;
        foreach (var part in body.AllExternalParts)
        {
            if (partsToIgnore.Contains(part.Type))
            {
                continue;
            }

            Image image = new() { Background = new ColoredRegion(new TextureRegion(part.WhiteIcon), Color.White), Width = BaseContent.IconSizes.Large, Height = BaseContent.IconSizes.Large };
            image.TouchDown += (_, _) => gui.ViewEntity(part);
            _bodyParts.Add(part, image);
            Widgets.Add(image);
            SetRow(image, gridRow);
            SetColumn(image, gridColumn++);
            if (gridColumn > 4)
            {
                gridColumn = 0;
                gridRow++;
            }
        }
    }

    public void Update()
    {
        foreach ((var bodyPart, var image) in _bodyParts)
        {
            if (bodyPart.IsSevered)
            {
                image.RemoveFromParent();
                _bodyParts.Remove(bodyPart);
                continue;
            }

            var color = BodyPartColor.Get(bodyPart);
            ((ColoredRegion)image.Background).Color = color;
        }
    }
}