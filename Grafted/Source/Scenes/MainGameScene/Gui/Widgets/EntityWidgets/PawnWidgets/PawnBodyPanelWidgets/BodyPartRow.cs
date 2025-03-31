using System.Drawing;
using Grafted.Sim.Entities.Pawns.Modifiers;
using Color = Microsoft.Xna.Framework.Color;
using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartRow : HorizontalStackPanel
{
    private readonly BaseGui _gui;
    private readonly CombatHandler? _combatHandler;
    public BodyPart? BodyPart;
    private Label _label;
    private List<ImageCircleIcon> _parts = new();
    private BodyPartTargetButton? _targetingButton;

    public BodyPartRow(BaseGui gui, CombatHandler? combatHandler)
    {
        _gui = gui;
        _combatHandler = combatHandler;
        Spacing = 5;
        _label = new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, TextColor = Color.Black };
    }

    public void SetPart(BodyPart bodyPart, bool showInternalParts)
    {
        _parts.Clear();
        Widgets.Clear();
        BodyPart = bodyPart;
        if (bodyPart.Body?.Pawn.PawnType == PawnType.Enemy)
        {
            _targetingButton = new BodyPartTargetButton(bodyPart, _combatHandler);
            Widgets.Add(_targetingButton);
        }

        if (showInternalParts)
        {
            Widgets.Add(_label);
            _label.TouchDown += (_, _) => BodyPartClickHandler(bodyPart, true);
        }

        var parts = bodyPart.AllInternalParts
            .Where(p => p.Type == BodyPartType.Skin && showInternalParts)
            .Concat(new List<BodyPart> { bodyPart })
            .Concat(bodyPart.AllInternalParts.Where(p => p.Type != BodyPartType.Skin && showInternalParts));

        foreach (var part in parts)
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

            partIcon.TouchDown += (_, _) => BodyPartClickHandler(part, !showInternalParts);
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

        _targetingButton?.Update();

        _label.Text = $"{BodyPart.Label}";
        _label.TextColor = BodyPartColor.Get(BodyPart);
        foreach (var image in _parts)
        {
            image.Update();
        }
    }
}