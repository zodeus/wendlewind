
using Color = Microsoft.Xna.Framework.Color;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartRow : HorizontalStackPanel
{
    private const int MaxPartsPerRow = 7;
    private readonly BaseGui _gui;
    public BodyPart? BodyPart;
    private Label _label;
    private List<BodyPartIcon> _parts = new();
    private List<HorizontalStackPanel> _rows = new();
    private VerticalStackPanel _iconContainer;

    public BodyPartRow(BaseGui gui)
    {
        _gui = gui;
        Spacing = 5;
        _label = new Label(BaseContent.Styles.Label.Medium)
        {
            VerticalAlignment = VerticalAlignment.Top,
            TextColor = Color.Black, Margin = new Thickness(0, 5, 0, 0)
        };
        _iconContainer = new VerticalStackPanel { Spacing = 5 };
    }

    public void SetPart(BodyPart bodyPart, bool showInternalParts)
    {
        _parts.Clear();
        _rows.Clear();
        Widgets.Clear();
        _iconContainer.Widgets.Clear();
        BodyPart = bodyPart;

        if (showInternalParts)
        {
            Widgets.Add(_label);
            _label.TouchDown += (_, _) => BodyPartClickHandler(bodyPart, true);
        }

        Widgets.Add(_iconContainer);

        var parts = bodyPart.AllInternalParts
            .Where(p => p.Type == BodyPartType.Skin && showInternalParts)
            .Concat(new List<BodyPart> { bodyPart })
            .Concat(bodyPart.AllInternalParts.Where(p => p.Type != BodyPartType.Skin && showInternalParts))
            .ToList();

        HorizontalStackPanel? currentRow = null;
        int partsInCurrentRow = 0;

        foreach (var part in parts)
        {
            // Create a new row if needed
            if (currentRow == null || partsInCurrentRow >= MaxPartsPerRow)
            {
                currentRow = new HorizontalStackPanel { Spacing = 5 };
                _rows.Add(currentRow);
                _iconContainer.Widgets.Add(currentRow);
                partsInCurrentRow = 0;
            }

            BodyPartIcon partIcon = new(new ColoredRegion(new TextureRegion(part.WhiteIcon), BodyPartColor.Get(bodyPart)), panel =>
            {
                // Keep the main ring color based on the body part state
                panel.SetColor(BodyPartColor.Get(part));

                // Use small colored pips around the ring to represent all modifiers on this part
                var pipData = part.Modifiers
                    .OrderByDescending(m => m.Def.ColorPriority)
                    .Select(m => new PipData { Color = m.Def.Color, Label = m.Label })
                    .ToList();

                panel.SetPips(pipData);
            })
            { Padding = new Thickness(0, 0, 0, 0) };

            partIcon.TouchDown += (_, _) => BodyPartClickHandler(part, !showInternalParts);
            _parts.Add(partIcon);
            currentRow.Widgets.Add(partIcon);
            partsInCurrentRow++;
        }
    }

    private void BodyPartClickHandler(BodyPart part, bool useItems = false)
    {
        if (Mouse.GetState().LeftButton != ButtonState.Pressed)
        {
            return;
        }

        if (_gui.MouseAttachment == null)
        {
            _gui.ViewEntity(part);
            return;
        }

        if (useItems && _gui.MouseAttachment.Data is Item item)
        {
            if (item.ItemDef.ItemType == ItemType.Medical && item.MedicinalHandler?.ApplyToPart(item, part) == true)
            {
                item.StackSize--;
                _gui.WorldTextHandler.Add(new WorldSpaceText
                {
                    Font = BaseContent.Fonts.Default.Medium,
                    Color = Color.GreenYellow,
                    Text = item.Label,
                    DurationInTicks = 120,
                    Speed = -2,
                    Position = Mouse.GetState().Position.ToVector2()
                });
                _gui.TickGame();
                if (item.StackSize != 0) return;

                item.Destroy();
                _gui.MouseAttachment.Detach();
            }
        }
    }

    public void Update()
    {
        if (BodyPart == null)
        {
            return;
        }

        _label.Text = $"{BodyPart.Label}";
        _label.TextColor = BodyPartColor.Get(BodyPart);
        foreach (var image in _parts)
        {
            image.Update();
        }
    }
}