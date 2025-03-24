using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartRow : HorizontalStackPanel
{
    private readonly BaseGui _gui;
    public BodyPart? BodyPart;
    private Label _label;
    private List<ImageCircleIcon> _parts = new();

    public BodyPartRow(BaseGui gui)
    {
        _gui = gui;
        Spacing = 5;
        _label = new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, TextColor = Color.Black };
    }

    public void SetPart(BodyPart bodyPart, bool showInternalParts)
    {
        _parts.Clear();
        Widgets.Clear();
        BodyPart = bodyPart;
        if (showInternalParts)
        {
            Widgets.Add(_label);
            _label.TouchDown += (_, _) => BodyPartClickHandler(bodyPart, true);
        }

        var parts = bodyPart.AllInternalParts
            .Where(p => p.Type == BodyPartType.Skin && showInternalParts)
            .Concat(new List<BodyPart> { bodyPart })
            .Concat(bodyPart.AllInternalParts.Where(p => p.Type != BodyPartType.Skin && showInternalParts));

        foreach (BodyPart part in parts)
        {
            ImageCircleIcon partIcon = new(new ColoredRegion(new TextureRegion(part.WhiteIcon), BodyPartColor.Get(bodyPart)), panel =>
            {
                var buffColor = part.Modifiers.Where(m => m.Def.Type == BodyPartModifierType.Buff)
                    .OrderByDescending(m => m.Def.ColorPriority).FirstOrNull()?.Def.Color;
                var debuffColor = part.Modifiers.Where(m => m.Def.Type == BodyPartModifierType.Debuff)
                    .OrderByDescending(m => m.Def.ColorPriority).FirstOrNull()?.Def.Color;
                if (buffColor != null || debuffColor != null)
                {
                    var color = buffColor;
                    if (debuffColor != null)
                    {
                        color = debuffColor;
                    }
                    panel.SetColor(color!.Value);
                }
                else
                {
                    panel.SetColor(BodyPartColor.Get(part));
                }
            });

            partIcon.TouchDown += (_, _) => BodyPartClickHandler(part);
            _parts.Add(partIcon);
            Widgets.Add(partIcon);
        }
    }

    private void BodyPartClickHandler(BodyPart part, bool useItems = false)
    {
        if (Input.LeftMouseButtonReleased)
        {
            return;
        }

        if (Input.RightMouseButtonReleased)
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
        foreach (ImageCircleIcon image in _parts)
        {
            image.Update();
        }
    }
}