using Microsoft.Xna.Framework.Input;
using Wendlemire.Graphics.Textures;
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
    private static readonly Color FrameOuter = new(10, 6, 4);
    private static readonly Color FrameWood = new(42, 22, 16);
    private static readonly Color FrameInset = new(106, 58, 40);
    private static readonly Color Rust = new(110, 42, 28);
    private static readonly Color Bone = new(203, 184, 150);
    private static readonly Color Dust = new(122, 110, 88);
    private static readonly Color Field = new(20, 12, 10);
    private static readonly Color Error = new(196, 90, 58);

    private const int HeroWidth = 900;
    private const int HeroHeight = 600;

    private Desktop _desktop = null!;
    private PlayerProfile _profile = null!;
    private ClientSettings _clientSettings = null!;
    private TextBox _usernameField = null!;
    private TextBox _serverField = null!;
    private CursorButton _fullscreenButton = null!;
    private Label _usernameError = null!;
    private Label _playingAsLabel = null!;
    private Widget _usernamePanel = null!;
    private Widget _playPanel = null!;
    private EventHandler<TextInputEventArgs>? _textInputHandler;
    private KeyboardState _previousKeyboard;

    protected override void OnStart()
    {
        _profile = PlayerProfile.LoadOrCreate();
        _clientSettings = ClientSettings.LoadOrCreate();
        TryHydrateUsernameFromServer();

        _usernameField = IronTextBox(_profile.Username, 320);
        _usernameError = BodyLabel("", Error);
        _usernameError.Visible = false;

        _usernamePanel = new VerticalStackPanel
        {
            Spacing = 10,
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
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        playButtons.Widgets.Add(IronButton("Start New Arena", () =>
        {
            PersistServerHost();
            ArenaScene.StartFresh = true;
            Core.ChangeScene<ArenaScene>();
        }));
        if (HasServerArenaProgress())
        {
            playButtons.Widgets.Add(IronButton("Continue Arena", () =>
            {
                PersistServerHost();
                Core.ChangeScene<ArenaScene>();
            }));
        }

        _playingAsLabel = BodyLabel($"Playing as {_profile.Username}", Dust);
        _playPanel = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _playingAsLabel,
                LoadRankBadge(),
                playButtons
            }
        };

        _serverField = IronTextBox(_clientSettings.ServerHost, 240);
        var serverPanel = new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                BodyLabel("Server", Dust, VerticalAlignment.Center),
                _serverField
            }
        };

        _fullscreenButton = IronButton(FullscreenButtonText(), ToggleFullscreen);

        _desktop = new Desktop
        {
            HasExternalTextInput = true,
            Root = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 14,
                Widgets =
                {
                    BuildHero(),
                    new EmberRule { Width = 360, Height = 3, HorizontalAlignment = HorizontalAlignment.Center },
                    Wordmark("WENDLEMIRE"),
                    _usernamePanel,
                    _playPanel,
                    serverPanel,
                    _fullscreenButton
                }
            }
        };
        Core.ConfigureDesktopScaling(_desktop);

        _textInputHandler = (_, e) => _desktop.OnChar(e.Character);
        Core.Instance.Window.TextInput += _textInputHandler;

        RefreshPanels();
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
        _playingAsLabel.Text = $"Playing as {_profile.Username}";
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
        _fullscreenButton.Content = DisplayLabel(FullscreenButtonText(), 22, Bone);
    }

    private static string FullscreenButtonText()
    {
        return Screen.IsFullscreen ? "Fullscreen: On" : "Fullscreen: Off";
    }

    private static Widget BuildHero()
    {
        var art = new Panel
        {
            Width = HeroWidth,
            Height = HeroHeight,
            Background = new TextureRegion(BaseContent.Textures.MainMenuBackground)
        };

        var inset = new Panel
        {
            Background = new SolidBrush(FrameWood),
            Border = new SolidBrush(FrameInset),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(1),
            Widgets = { art }
        };

        return new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidBrush(FrameWood),
            Border = new SolidBrush(FrameOuter),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(7),
            Widgets = { inset }
        };
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

    private static CursorButton IronButton(string text, Action onClick)
    {
        var button = new CursorButton
        {
            Content = DisplayLabel(text, 22, Bone),
            Width = 280,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(16, 10),
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
