using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Cosmetics;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class CosmeticsShopScreen : Panel
{
    private static readonly Color Void = new(7, 5, 4);
    private static readonly Color IronFill = new(42, 26, 20);
    private static readonly Color IronHover = new(58, 28, 20);
    private static readonly Color IronPressed = new(20, 12, 10);
    private static readonly Color IronEdge = new(60, 46, 38);
    private static readonly Color FrameOuter = new(10, 6, 4);
    private static readonly Color FrameWood = new(42, 22, 16);
    private static readonly Color FrameInset = new(106, 58, 40);
    private static readonly Color Rust = new(110, 42, 28);
    private static readonly Color Bone = new(203, 184, 150);
    private static readonly Color Dust = new(122, 110, 88);
    private static readonly Color TabletFill = new(16, 10, 8, 230);
    private static readonly Color Error = new(196, 90, 58);
    private static readonly Color Good = new(150, 186, 122);

    private readonly PlayerProfile _profile;
    private readonly Action _onBack;
    private readonly Label _marksLabel;
    private readonly Label _statusLabel;
    private readonly VerticalStackPanel _cards;
    private PlayerProfileRecord? _remote;

    public CosmeticsShopScreen(PlayerProfile profile, Action onBack)
    {
        _profile = profile;
        _onBack = onBack;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = new SolidBrush(Void);

        _marksLabel = BodyLabel("", Bone);
        _statusLabel = BodyLabel("", Error);
        _statusLabel.Visible = false;
        _cards = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var body = new VerticalStackPanel
        {
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                DisplayLabel("CURIO SHOP", 28, Bone),
                _marksLabel,
                DisplayLabel("Name Plates", 20, Dust),
                _cards,
                _statusLabel,
                IronButton("Back", _onBack)
            }
        };

        var inset = new Panel
        {
            Background = new SolidBrush(TabletFill),
            Border = new SolidBrush(FrameInset),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(28, 22),
            Widgets = { body }
        };

        Widgets.Add(new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidBrush(FrameWood),
            Border = new SolidBrush(FrameOuter),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(7),
            Widgets = { inset }
        });

        Reload();
    }

    private void Reload()
    {
        try
        {
            using var client = new ArenaMatchClient();
            _remote = client.GetProfile(_profile.PlayerId).GetAwaiter().GetResult()
                      ?? client.EnsureProfile(_profile.PlayerId, _profile.DisplayName, _profile.Username)
                          .GetAwaiter().GetResult();
            _statusLabel.Visible = false;
        }
        catch (Exception ex)
        {
            _remote = null;
            _statusLabel.Text = ex.Message;
            _statusLabel.TextColor = Error;
            _statusLabel.Visible = true;
        }

        _marksLabel.Text = _remote == null ? "Marks unavailable" : $"{_remote.Marks} marks";
        RebuildCards();
    }

    private void RebuildCards()
    {
        _cards.Widgets.Clear();
        var name = string.IsNullOrWhiteSpace(_profile.Username) ? "Wanderer" : _profile.Username;
        foreach (var def in CosmeticCatalog.OfCategory(CosmeticCategory.NamePlate))
        {
            _cards.Widgets.Add(BuildCard(def, name));
        }

        if (_cards.Widgets.Count == 0)
        {
            _cards.Widgets.Add(BodyLabel("No name plates found.", Dust));
        }
    }

    private Widget BuildCard(CosmeticDef def, string name)
    {
        var owned = Owns(def.Moniker);
        var equipped = string.Equals(_remote?.EquippedNamePlate, def.Moniker, StringComparison.Ordinal);
        var action = IronButton(
            equipped ? "Equipped" : owned ? "Equip" : def.Price <= 0 ? "Take" : $"Buy  {def.Price}",
            () => OnCardAction(def));
        action.Enabled = !equipped;

        return new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Width = 420,
            Background = new SolidBrush(new Color(20, 12, 10, 220)),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Widgets =
            {
                new VerticalStackPanel
                {
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Widgets =
                    {
                        new NamePlateWidget(name, def.Moniker),
                        BodyLabel(def.Label, Bone),
                        BodyLabel(def.Description, Dust),
                        BodyLabel(owned ? "Owned" : $"{def.Price} marks", owned ? Good : Dust),
                        action
                    }
                }
            }
        };
    }

    private void OnCardAction(CosmeticDef def)
    {
        if (_remote == null)
        {
            return;
        }

        try
        {
            using var client = new ArenaMatchClient();
            var result = Owns(def.Moniker)
                ? client.EquipCosmetic(_profile.PlayerId, def.Moniker).GetAwaiter().GetResult()
                : client.BuyCosmetic(_profile.PlayerId, def.Moniker).GetAwaiter().GetResult();
            if (!result.Ok)
            {
                _statusLabel.Text = result.Error ?? "Could not complete that.";
                _statusLabel.TextColor = Error;
                _statusLabel.Visible = true;
                if (result.Profile != null)
                {
                    _remote = result.Profile;
                    _marksLabel.Text = $"{_remote.Marks} marks";
                    RebuildCards();
                }

                return;
            }

            _remote = result.Profile ?? _remote;
            _statusLabel.Text = Owns(def.Moniker) && string.Equals(_remote.EquippedNamePlate, def.Moniker, StringComparison.Ordinal)
                ? $"Equipped {def.Label}."
                : $"Purchased {def.Label}.";
            _statusLabel.TextColor = Good;
            _statusLabel.Visible = true;
            _marksLabel.Text = $"{_remote.Marks} marks";
            RebuildCards();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _statusLabel.TextColor = Error;
            _statusLabel.Visible = true;
        }
    }

    private bool Owns(string moniker) =>
        _remote?.OwnedCosmeticMonikers.Any(id => string.Equals(id, moniker, StringComparison.Ordinal)) == true;

    private static CursorButton IronButton(string text, Action onClick)
    {
        var button = new CursorButton
        {
            Content = new Label
            {
                Text = text,
                Font = BaseContent.Fonts.Display.Normal,
                TextColor = Bone,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12, 8),
            Background = new SolidBrush(IronFill),
            OverBackground = new SolidBrush(IronHover),
            PressedBackground = new SolidBrush(IronPressed),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1)
        };
        button.MouseEntered += (_, _) => button.Border = new SolidBrush(Rust);
        button.MouseLeft += (_, _) => button.Border = new SolidBrush(IronEdge);
        button.Click += (_, _) => onClick();
        return button;
    }

    private static Label DisplayLabel(string text, float size, Color color)
    {
        return new Label
        {
            Text = text,
            Font = BaseContent.Fonts.Display.Normal.FontSystem.GetFont(size),
            TextColor = color,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private static Label BodyLabel(string text, Color color)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true,
            Width = 380
        };
    }
}
