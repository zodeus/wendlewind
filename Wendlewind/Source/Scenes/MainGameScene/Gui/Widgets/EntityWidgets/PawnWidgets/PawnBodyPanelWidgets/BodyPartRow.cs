
using Color = Microsoft.Xna.Framework.Color;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartRow : HorizontalStackPanel
{
    private const int MaxPartsPerRow = 7;
    private readonly BaseGui _gui;
    public BodyPart? BodyPart;
    private Label _label;
    private List<BodyPartIcon> _parts = new();
    private List<HorizontalStackPanel> _rows = new();
    private VerticalStackPanel _iconContainer;
    private float _flashTime;
    private Color _flashColor = Color.White;
    private Color _baseLabelColor = Color.White;
    private readonly List<RowFloater> _floaters = [];

    public BodyPartRow(BaseGui gui)
    {
        _gui = gui;
        ClipToBounds = false;
        Spacing = 5;
        _label = new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center };
        _label.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        _label.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
        _iconContainer = new VerticalStackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
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

            BodyPartIcon partIcon = new(new ColoredRegion(new TextureRegion(part.GetWhiteIcon()), BodyPartColor.Get(bodyPart)), panel =>
            {
                panel.SetColor(BodyPartColor.Get(part));
                panel.RefreshPips(part);
            });
            partIcon.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
            partIcon.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);

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
                Core.Context.Achievements.OnItemUsed(Core.Context.PlayerPawn, item);
                item.StackSize--;
                _gui.WorldTextHandler.Add(new WorldSpaceText
                {
                    Color = Color.GreenYellow,
                    Text = item.Label,
                    DurationInTicks = 120,
                    Speed = -2,
                    Position = Mouse.GetState().Position.ToVector2()
                });
                if (item.StackSize != 0) return;

                item.Destroy();
                _gui.MouseAttachment.Detach();
            }
        }
    }

    public bool ContainsPart(BodyPart part)
    {
        if (BodyPart == null)
        {
            return false;
        }

        if (BodyPart == part || BodyPart.InternalLabel == part.InternalLabel)
        {
            return true;
        }

        return BodyPart.AllInternalParts.Any(p => p == part || p.InternalLabel == part.InternalLabel);
    }

    public void AddFloater(string text, DynamicSpriteFont font, Color color, float duration)
    {
        var lifetime = Math.Clamp(duration, 0.7f, 1.25f);
        _floaters.Add(new RowFloater
        {
            Text = text,
            Font = font,
            Color = color,
            TimeLeft = lifetime,
            Duration = lifetime,
            Stack = -_floaters.Count * 12f,
            MeasuredSize = font.MeasureString(text),
            Style = CombatFloaterStyle.CreateRandom()
        });
    }

    public void Flash(Color color)
    {
        _flashTime = 0.35f;
        _flashColor = color;
        foreach (var icon in _parts)
        {
            icon.Flash(color);
        }
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        if (BodyPart == null)
        {
            return;
        }

        UiLabel.Set(_label, BodyPart.Label);
        _baseLabelColor = BodyPartColor.Get(BodyPart);
        var labelColor = _baseLabelColor;
        if (_flashTime > 0)
        {
            _flashTime -= deltaTime;
            var t = Math.Clamp(_flashTime / 0.35f, 0f, 1f);
            labelColor = Color.Lerp(_baseLabelColor, _flashColor, t);
        }

        UiLabel.SetColor(_label, labelColor);

        foreach (var image in _parts)
        {
            image.Update(deltaTime);
        }

        for (var i = _floaters.Count - 1; i >= 0; i--)
        {
            var floater = _floaters[i];
            floater.TimeLeft -= deltaTime;
            floater.Elapsed += deltaTime;
            if (floater.TimeLeft <= 0)
            {
                _floaters.RemoveAt(i);
            }
        }
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        if (_floaters.Count == 0)
        {
            return;
        }

        var bounds = ActualBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var origin = new Vector2(
            bounds.X + Math.Min(bounds.Width * 0.55f, 180f),
            bounds.Y + bounds.Height * 0.3f);

        foreach (var floater in _floaters)
        {
            var progress = Math.Clamp(floater.Elapsed / floater.Duration, 0f, 1f);
            var fade = progress < 0.55f ? 1f : Math.Max(1f - (progress - 0.55f) / 0.45f, 0f);
            var motion = floater.Style.Evaluate(floater.Elapsed, floater.Duration);
            CombatFloaterDraw.Draw(
                context,
                floater.Font,
                floater.Text,
                origin + new Vector2(0, floater.Stack) + motion.Offset,
                floater.Color,
                fade * motion.Opacity,
                motion.Scale,
                floater.MeasuredSize);
        }
    }

    private sealed class RowFloater
    {
        public string Text = "";
        public Color Color;
        public DynamicSpriteFont Font = BaseContent.Fonts.Default.Smallest;
        public float TimeLeft;
        public float Duration;
        public float Elapsed;
        public float Stack;
        public Vector2 MeasuredSize;
        public CombatFloaterStyle Style = CombatFloaterStyle.CreateRandom();
    }
}