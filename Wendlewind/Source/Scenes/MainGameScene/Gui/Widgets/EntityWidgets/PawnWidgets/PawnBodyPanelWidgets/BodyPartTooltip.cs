using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartTooltip : VerticalStackPanel
{
    private static readonly Color Muted = new(180, 180, 180);
    private static readonly Color Problem = new(220, 90, 90);
    private static readonly Color Accent = new(220, 180, 100);

    private readonly BodyPart _part;
    private readonly Image _icon;
    private readonly Label _title;
    private readonly Label _typeAndHp;
    private readonly Label _description;
    private readonly Label _parent;
    private readonly Label _bleeding;
    private readonly Label _destroyed;
    private readonly Label _cracked;
    private readonly Label _brokenBones;
    private readonly Label _noMobility;
    private readonly Label _nonFunctional;
    private readonly Label _arteryDestroyed;
    private readonly Label _vital;
    private readonly VerticalStackPanel _modifiers;
    private readonly VerticalStackPanel _equipped;
    private int _modifierSignature = int.MinValue;
    private int _equippedSignature = int.MinValue;

    public BodyPartTooltip(BodyPart part)
    {
        _part = part;
        Spacing = 4;
        Padding = new Thickness(4);
        MaxWidth = 280;

        var header = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        _icon = new Image
        {
            Background = new ColoredRegion(new TextureRegion(part.GetIcon()), Color.White),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Widgets.Add(_icon);

        _title = new Label(BaseContent.Styles.Label.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Widgets.Add(_title);
        Widgets.Add(header);

        _typeAndHp = new Label(BaseContent.Styles.Label.Small);
        Widgets.Add(_typeAndHp);

        _description = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Muted,
            Wrap = true,
            MaxWidth = 260
        };
        Widgets.Add(_description);

        _parent = new Label(BaseContent.Styles.Label.Small) { TextColor = Muted };
        Widgets.Add(_parent);

        _vital = StatusLabel("Vital", Accent);
        _bleeding = StatusLabel("Bleeding");
        _destroyed = StatusLabel("Mutilated");
        _cracked = StatusLabel("Cracked");
        _brokenBones = StatusLabel("Broken bones");
        _noMobility = StatusLabel("No mobility");
        _nonFunctional = StatusLabel("Non-functional");
        _arteryDestroyed = StatusLabel("Artery destroyed");

        _modifiers = new VerticalStackPanel { Spacing = 2 };
        Widgets.Add(_modifiers);

        _equipped = new VerticalStackPanel { Spacing = 2 };
        Widgets.Add(_equipped);

        Refresh();
    }

    public void Refresh()
    {
        var tint = BodyPartColor.Get(_part);
        ((ColoredRegion)_icon.Background).Color = tint;
        _title.Text = _part.Label;
        _title.TextColor = tint;

        _typeAndHp.Text = $"{_part.Type}  ·  {FormatHealth(_part)}";
        _typeAndHp.TextColor = tint;

        var description = _part.Def.Description;
        _description.Text = description ?? "";
        _description.Visible = !string.IsNullOrWhiteSpace(description);

        var parentLabel = _part.Socket?.ParentPart?.Label;
        _parent.Text = parentLabel == null ? "" : $"Parent: {parentLabel}";
        _parent.Visible = parentLabel != null;

        _vital.Visible = _part.IsVital;
        _bleeding.Visible = _part.IsBleeding;
        _destroyed.Visible = _part.IsDestroyed;
        _cracked.Visible = _part.IsCracked;
        _brokenBones.Visible = _part.HasBrokenBones;
        _noMobility.Visible = !_part.HasMobility;
        _nonFunctional.Visible = !_part.IsFunctional;
        _arteryDestroyed.Visible = !_part.IsArteryFunctional;

        RefreshModifiers();
        RefreshEquipped();
    }

    private void RefreshModifiers()
    {
        var signature = 0;
        foreach (var modifier in _part.Modifiers)
        {
            signature = HashCode.Combine(signature, modifier.Id, modifier.Label, modifier.TicksRemaining);
        }

        if (signature == _modifierSignature)
        {
            return;
        }

        _modifierSignature = signature;
        _modifiers.Widgets.Clear();
        if (_part.Modifiers.Count == 0)
        {
            return;
        }

        _modifiers.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Modifiers",
            TextColor = Accent,
            Margin = new Thickness(0, 4, 0, 0)
        });

        foreach (var modifier in _part.Modifiers)
        {
            var timeRemaining = modifier.DurationInTicks == 0 ? "\u221e" : modifier.TicksRemaining + "t";
            _modifiers.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{modifier.Label}  {timeRemaining}",
                TextColor = modifier.Def.Color
            });
        }
    }

    private void RefreshEquipped()
    {
        var equipped = _part.Equipment.Values.Where(i => i is { IsDestroyed: false }).ToList();
        var signature = 0;
        foreach (var item in equipped)
        {
            signature = HashCode.Combine(signature, item!.Id, item.Label);
        }

        if (signature == _equippedSignature)
        {
            return;
        }

        _equippedSignature = signature;
        _equipped.Widgets.Clear();
        if (equipped.Count == 0)
        {
            return;
        }

        _equipped.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Equipped",
            TextColor = Accent,
            Margin = new Thickness(0, 4, 0, 0)
        });

        foreach (var item in equipped)
        {
            _equipped.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item!.Label,
                TextColor = Color.White
            });
        }
    }

    private Label StatusLabel(string text, Color? color = null)
    {
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color ?? Problem,
            Visible = false
        };
        Widgets.Add(label);
        return label;
    }

    private static string FormatHealth(BodyPart part)
    {
        if (part.HitPoints < 2)
        {
            return $"{part.HitPoints:N1}/{part.MaxHitPoints:N0}";
        }

        return $"{Math.Ceiling(part.HitPoints):N0}/{part.MaxHitPoints:N0}";
    }
}
