using Microsoft.Xna.Framework.Input;
using Wendlemire.NetCode;
using Wendlemire.Scenes.ArenaScene;
using Wendlemire.Scenes.ArenaScene.Gui;
using Wendlemire.Scenes.Components;
using Wendlemire.Scenes.MainGameScene;

namespace Wendlemire.Sim;

public class MainMenuScene : Scene
{
    private static readonly Color Void = new(7, 5, 4);
    private static readonly Color IronFill = new(42, 26, 20);
    private static readonly Color IronHover = new(58, 28, 20);
    private static readonly Color IronPressed = new(20, 12, 10);
    private static readonly Color IronEdge = new(60, 46, 38);
    private static readonly Color Rust = new(110, 42, 28);
    private static readonly Color Bone = new(203, 184, 150);
    private static readonly Color Dust = new(122, 110, 88);
    private static readonly Color Field = new(20, 12, 10);
    private static readonly Color Error = new(196, 90, 58);
    private static readonly Color Veil = new(7, 5, 4, 120);
    private static readonly Color IronDisabled = new(28, 18, 14);

    private const bool ShopEnabled = false;

    private Desktop _desktop = null!;
    private PlayerProfile _profile = null!;
    private ClientSettings _clientSettings = null!;
    private TextBox _usernameField = null!;
    private TextBox _serverField = null!;
    private CursorButton _fullscreenButton = null!;
    private Label _usernameError = null!;
    private Label _connectionError = null!;
    private Label _playingAsLabel = null!;
    private Widget _usernamePanel = null!;
    private Widget _playPanel = null!;
    private Widget _menuRoot = null!;
    private EventHandler<TextInputEventArgs>? _textInputHandler;
    private KeyboardState _previousKeyboard;

    protected override void OnStart()
    {
        _profile = PlayerProfile.LoadOrCreate();
        _clientSettings = ClientSettings.LoadOrCreate();
        var versionError = PeekVersionError();
        if (versionError == null)
        {
            TryHydrateUsernameFromServer();
        }

        _usernameField = IronTextBox(_profile.Username, 320);
        _usernameError = BodyLabel("", Error);
        _usernameError.Visible = false;
        _connectionError = BodyLabel("", Error);
        _connectionError.Wrap = true;
        _connectionError.Width = 520;
        _connectionError.Visible = false;

        _usernamePanel = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                DisplayLabel("Choose a username", 22, Bone),
                _usernameField,
                _usernameError,
                IronButton("Confirm", ConfirmUsername)
            }
        };

        var playButtons = new VerticalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        playButtons.Widgets.Add(IronButton("Start New Arena", () => TryEnterArena(startFresh: true)));
        if (versionError == null && HasServerArenaProgress())
        {
            playButtons.Widgets.Add(IronButton("Continue Arena", () => TryEnterArena(startFresh: false)));
        }

        if (versionError == null)
        {
            playButtons.Widgets.Add(IronButton("Treasure Trove", OpenShop, ShopEnabled));
        }

        _playingAsLabel = BodyLabel(IdentityText(), Dust);
        _playPanel = new VerticalStackPanel
        {
            Spacing = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new VerticalStackPanel
                {
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        _playingAsLabel,
                        versionError == null ? LoadRankBadge() : BodyLabel("Unranked", Dust)
                    }
                },
                playButtons
            }
        };

        _serverField = IronTextBox(_clientSettings.ServerHost, 220);
        _fullscreenButton = IronButton(FullscreenButtonText(), ToggleFullscreen, width: 240);

        var body = new VerticalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                _usernamePanel,
                _playPanel,
                _connectionError
            }
        };

        var footer = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        BodyLabel("Server", Dust, VerticalAlignment.Center),
                        _serverField,
                        _fullscreenButton
                    }
                },
                BodyLabel($"v{GameVersion.Current}", Dust)
            }
        };

        var overlay = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(48, 40)
        };
        overlay.RowsProportions.Add(new Proportion(ProportionType.Auto));
        overlay.RowsProportions.Add(new Proportion(ProportionType.Fill));
        overlay.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var brand = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                Wordmark("WENDLEMIRE"),
                new EmberRule { Width = 360, Height = 3, HorizontalAlignment = HorizontalAlignment.Center }
            }
        };
        overlay.Widgets.Add(brand);
        overlay.Widgets.Add(body);
        Grid.SetRow(body, 1);
        overlay.Widgets.Add(footer);
        Grid.SetRow(footer, 2);

        _menuRoot = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(Void),
            Widgets =
            {
                new CoverImage(BaseContent.Textures.MainMenuBackground),
                new Panel
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidBrush(Veil)
                },
                overlay
            }
        };
        _desktop = new Desktop
        {
            HasExternalTextInput = true,
            Root = _menuRoot
        };
        Core.ConfigureDesktopScaling(_desktop);

        _textInputHandler = (_, e) => _desktop.OnChar(e.Character);
        Core.Instance.Window.TextInput += _textInputHandler;

        RefreshPanels();
        if (versionError != null)
        {
            ShowConnectionError(versionError);
        }
    }

    public override void Update(float deltaTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            PersistServerHost();
            if (_usernamePanel.Visible)
            {
                ConfirmUsername();
            }
        }

        _previousKeyboard = keyboard;
    }

    public override void End()
    {
        PersistServerHost();
        if (_textInputHandler != null)
        {
            Core.Instance.Window.TextInput -= _textInputHandler;
            _textInputHandler = null;
        }
    }

    public override void Draw(float deltaTime)
    {
        Core.GraphicsDevice.Clear(Void);
        _desktop.Render();
    }

    private void TryEnterArena(bool startFresh)
    {
        PersistServerHost();
        if (!TryEnsureCompatible())
        {
            return;
        }

        if (startFresh)
        {
            ArenaScene.StartFresh = true;
        }

        Core.ChangeScene<ArenaScene>();
    }

    private static string? PeekVersionError()
    {
        try
        {
            using var client = new ArenaMatchClient(timeout: TimeSpan.FromSeconds(5));
            client.EnsureCompatible().GetAwaiter().GetResult();
            return null;
        }
        catch (VersionMismatchException ex)
        {
            return ex.Message;
        }
        catch
        {
            return null;
        }
    }

    private bool TryEnsureCompatible()
    {
        try
        {
            using var client = new ArenaMatchClient(timeout: TimeSpan.FromSeconds(5));
            client.EnsureCompatible().GetAwaiter().GetResult();
            _connectionError.Visible = false;
            return true;
        }
        catch (Exception ex)
        {
            ShowConnectionError(ex.Message);
            return false;
        }
    }

    private void ShowConnectionError(string message)
    {
        _connectionError.Text = message;
        _connectionError.Visible = true;
    }

    private void ConfirmUsername()
    {
        var username = _usernameField.Text?.Trim() ?? "";
        if (!PlayerProfile.IsValidUsername(username))
        {
            _usernameError.Text = $"Username must be {PlayerProfile.MinUsernameLength}–{PlayerProfile.MaxUsernameLength} characters.";
            _usernameError.Visible = true;
            return;
        }

        PersistServerHost();
        _profile.SetUsername(username);
        TryPushUsernameToServer();
        _playingAsLabel.Text = IdentityText();
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        var ready = _profile.HasUsername;
        _usernamePanel.Visible = !ready;
        _playPanel.Visible = ready;
    }

    private void PersistServerHost()
    {
        if (_serverField == null || _clientSettings == null)
        {
            return;
        }

        var host = string.IsNullOrWhiteSpace(_serverField.Text)
            ? ClientSettings.DefaultHost
            : _serverField.Text.Trim();
        if (host != _clientSettings.ServerHost)
        {
            _clientSettings.SetServerHost(host);
        }
    }

    private void ToggleFullscreen()
    {
        var fullScreen = !Screen.IsFullscreen;
        Screen.SetFullscreen(fullScreen);
        _clientSettings.SetFullScreen(fullScreen);
        _fullscreenButton.Content = DisplayLabel(FullscreenButtonText(), 20, Bone);
    }

    private static string FullscreenButtonText()
    {
        return Screen.IsFullscreen ? "Fullscreen: On" : "Fullscreen: Off";
    }

    private void OpenShop()
    {
        PersistServerHost();
        _desktop.Root = new CosmeticsShopScreen(_profile, CloseShop);
        Core.ConfigureDesktopScaling(_desktop);
    }

    private void CloseShop()
    {
        _playingAsLabel.Text = IdentityText();
        _desktop.Root = _menuRoot;
        Core.ConfigureDesktopScaling(_desktop);
    }

    private string IdentityText()
    {
        var marks = LoadMarksText();
        return string.IsNullOrEmpty(marks)
            ? $"Playing as {_profile.Username}"
            : $"Playing as {_profile.Username}   ·   {marks}";
    }

    private string LoadMarksText()
    {
        try
        {
            using var client = new ArenaMatchClient();
            var remote = client.GetProfile(_profile.PlayerId).GetAwaiter().GetResult()
                         ?? client.EnsureProfile(_profile.PlayerId, _profile.DisplayName, _profile.Username)
                             .GetAwaiter().GetResult();
            return $"{remote.Marks} marks";
        }
        catch
        {
            return "";
        }
    }

    private static Widget LoadRankBadge()
    {
        try
        {
            using var client = new ArenaMatchClient();
            var profile = PlayerProfile.LoadOrCreate();
            var remote = client.GetProfile(profile.PlayerId).GetAwaiter().GetResult()
                         ?? client.EnsureProfile(profile.PlayerId, profile.DisplayName, profile.Username)
                             .GetAwaiter().GetResult();
            var rank = ArenaRank.FromRating(remote.Rating, remote.RatedRuns, remote.LegendNumber);
            return new RankBadge(rank, badgeSize: 56)
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
        catch
        {
            return BodyLabel("Unranked", Dust);
        }
    }

    private void TryHydrateUsernameFromServer()
    {
        if (_profile.HasUsername)
        {
            return;
        }

        try
        {
            using var client = new ArenaMatchClient();
            var remote = client.EnsureProfile(_profile.PlayerId, _profile.DisplayName).GetAwaiter().GetResult();
            if (PlayerProfile.IsValidUsername(remote.Username))
            {
                _profile.SetUsername(remote.Username);
            }
        }
        catch
        {
        }
    }

    private void TryPushUsernameToServer()
    {
        try
        {
            using var client = new ArenaMatchClient();
            client.EnsureProfile(_profile.PlayerId, _profile.DisplayName, _profile.Username).GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    private static CursorButton IronButton(string text, Action onClick, bool enabled = true, int width = 340)
    {
        var button = new CursorButton
        {
            Content = DisplayLabel(text, 22, enabled ? Bone : Dust),
            Width = width,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(20, 14),
            Enabled = enabled,
            Background = new SolidBrush(IronFill),
            OverBackground = new SolidBrush(IronHover),
            PressedBackground = new SolidBrush(IronPressed),
            DisabledBackground = new SolidBrush(IronDisabled),
            Border = new SolidBrush(enabled ? IronEdge : Field),
            BorderThickness = new Thickness(1)
        };
        if (!enabled)
        {
            return button;
        }

        button.MouseEntered += (_, _) => button.Border = new SolidBrush(Rust);
        button.MouseLeft += (_, _) => button.Border = new SolidBrush(IronEdge);
        button.Click += (_, _) => onClick();
        return button;
    }

    private static TextBox IronTextBox(string text, int width)
    {
        return new TextBox
        {
            Width = width,
            Text = text,
            TextColor = Bone,
            Background = new SolidBrush(Field),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6)
        };
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

    private static Label BodyLabel(string text, Color color, VerticalAlignment vertical = VerticalAlignment.Top)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = vertical
        };
    }

    private static Widget Wordmark(string text)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 7,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (var letter in text)
        {
            row.Widgets.Add(new Label
            {
                Text = letter.ToString(),
                Font = BaseContent.Fonts.Display.VeryLarge,
                TextColor = Bone
            });
        }

        return row;
    }

    private static bool HasServerArenaProgress()
    {
        try
        {
            using var client = new ArenaMatchClient();
            var profile = PlayerProfile.LoadOrCreate();
            return client.HasCurrentArena(profile.PlayerId).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    private sealed class CoverImage : Widget
    {
        private readonly Texture2D _texture;

        public CoverImage(Texture2D texture)
        {
            _texture = texture;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ClipToBounds = true;
        }

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0 || _texture.Width <= 0 || _texture.Height <= 0)
            {
                return;
            }

            var scale = Math.Max(bounds.Width / (float)_texture.Width, bounds.Height / (float)_texture.Height);
            var width = (int)MathF.Ceiling(_texture.Width * scale);
            var height = (int)MathF.Ceiling(_texture.Height * scale);
            context.Draw(_texture, new Rectangle(
                bounds.X + (bounds.Width - width) / 2,
                bounds.Y + (bounds.Height - height) / 2,
                width,
                height), Color.White);
        }
    }

    private sealed class EmberRule : Widget
    {
        private static Texture2D? _pixel;

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var pixel = Pixel();
            var y = bounds.Y + bounds.Height / 2;
            for (var x = 0; x < bounds.Width; x++)
            {
                var t = x / (float)Math.Max(1, bounds.Width - 1);
                var edge = t < 0.5f ? t * 2f : (1f - t) * 2f;
                var color = Color.Lerp(Rust, new Color(201, 160, 112), 1f - MathF.Abs(t - 0.5f) * 2f);
                color *= 0.35f + edge * 0.65f;
                context.Draw(pixel, new Rectangle(bounds.X + x, y, 1, bounds.Height), color);
            }
        }

        private static Texture2D Pixel()
        {
            if (_pixel != null)
            {
                return _pixel;
            }

            _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
            return _pixel;
        }
    }
}
