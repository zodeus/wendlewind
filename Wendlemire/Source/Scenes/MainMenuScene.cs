using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
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
    private TextBox _passwordField = null!;
    private TextBox _registerUsernameField = null!;
    private TextBox _registerEmailField = null!;
    private TextBox _registerPasswordField = null!;
    private TextBox _serverField = null!;
    private CursorButton _fullscreenButton = null!;
    private CursorButton _muteButton = null!;
    private Label _authError = null!;
    private Label _registerError = null!;
    private Label _connectionError = null!;
    private Window? _registerWindow;
    private Label _playingAsLabel = null!;
    private Widget _authPanel = null!;
    private Widget _playPanel = null!;
    private VerticalStackPanel _playButtons = null!;
    private Panel _rankBadgeHost = null!;
    private string? _versionError;
    private Widget _menuRoot = null!;
    private EventHandler<TextInputEventArgs>? _textInputHandler;
    private KeyboardState _previousKeyboard;
    private readonly BusyOverlay _busyOverlay = new();
    private bool _busy;
    private Task? _busyTask;
    private MenuBusyKind _busyKind;
    private bool _pendingStartFresh;
    private bool _pendingAuthRegister;
    private AuthSession? _pendingSession;
    private string? _pendingConnectionError;
    private string? _pendingAuthError;

    protected override void OnStart()
    {
        _profile = PlayerProfile.LoadOrCreate();
        _clientSettings = ClientSettings.LoadOrCreate();
        _versionError = PeekVersionError();
        if (_versionError == null)
        {
            TryRestoreSession();
        }

        _usernameField = IronTextBox(_profile.Username, 320, hint: "Username");
        _passwordField = IronTextBox("", 320, password: true, hint: "Password");
        _authError = BodyLabel("", Error);
        _authError.Visible = false;
        _connectionError = BodyLabel("", Error);
        _connectionError.Wrap = true;
        _connectionError.Width = 520;
        _connectionError.Visible = false;

        _authPanel = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                DisplayLabel("Log in", 22, Bone),
                LabeledField("Username", _usernameField),
                LabeledField("Password", _passwordField),
                _authError,
                IronButton("Log in", SubmitLogin, width: 320),
                IronButton("Register", OpenRegisterDialog, width: 220)
            }
        };

        _playButtons = new VerticalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _playingAsLabel = BodyLabel("", Dust);
        _rankBadgeHost = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
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
                        _rankBadgeHost
                    }
                },
                _playButtons
            }
        };
        RebuildPlayPanel();

        _serverField = IronTextBox(_clientSettings.ServerHost, 220);
        _fullscreenButton = IronButton(FullscreenButtonText(), ToggleFullscreen, width: 240);
        _muteButton = IronButton(MuteButtonText(), ToggleMute, width: 180);

        var body = new VerticalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                _authPanel,
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
                        _fullscreenButton,
                        _muteButton
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
        if (_versionError != null)
        {
            ShowConnectionError(_versionError);
        }
    }

    public override void Update(float deltaTime)
    {
        _busyOverlay.Update(deltaTime);
        PollBusy();

        var keyboard = Keyboard.GetState();
        if (!_busy && keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            PersistServerHost();
            if (_registerWindow is { IsPlaced: true })
            {
                SubmitRegister();
            }
            else if (_authPanel.Visible)
            {
                SubmitLogin();
            }
        }

        _previousKeyboard = keyboard;
    }

    public override void End()
    {
        if (_busyTask != null)
        {
            try
            {
                _busyTask.GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        _busyOverlay.Hide();
        CloseRegisterDialog();
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
        if (_busy)
        {
            return;
        }

        PersistServerHost();
        _pendingStartFresh = startFresh;
        _pendingConnectionError = null;
        BeginBusy(MenuBusyKind.EnterArena, "Connecting...", Task.Run(async () =>
        {
            using var client = new ArenaMatchClient(timeout: TimeSpan.FromSeconds(5));
            await client.EnsureCompatible();
        }));
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

    private void BeginBusy(MenuBusyKind kind, string message, Task work)
    {
        _busy = true;
        _busyKind = kind;
        _busyTask = work;
        _busyOverlay.ShowModal(_desktop, message);
    }

    private void PollBusy()
    {
        if (_busyTask is not { IsCompleted: true } task)
        {
            return;
        }

        _busyTask = null;
        _busy = false;
        var kind = _busyKind;
        _busyKind = MenuBusyKind.None;
        _busyOverlay.Hide();

        if (task.IsFaulted && _pendingConnectionError == null && _pendingAuthError == null)
        {
            _pendingConnectionError = task.Exception?.GetBaseException().Message ?? "Could not reach the server.";
        }

        switch (kind)
        {
            case MenuBusyKind.EnterArena:
                FinishEnterArena();
                break;
            case MenuBusyKind.Auth:
                FinishAuth();
                break;
        }
    }

    private void FinishEnterArena()
    {
        if (_pendingConnectionError != null)
        {
            ArenaScene.StartFresh = false;
            ShowConnectionError(_pendingConnectionError);
            _pendingConnectionError = null;
            return;
        }

        _connectionError.Visible = false;
        ArenaScene.StartFresh = _pendingStartFresh;
        Core.ChangeScene<ArenaScene>();
    }

    private void ShowConnectionError(string message)
    {
        _connectionError.Text = message;
        _connectionError.Visible = true;
    }

    private void SubmitLogin()
    {
        SubmitAuth(
            register: false,
            _usernameField.Text?.Trim() ?? "",
            email: "",
            _passwordField.Text ?? "");
    }

    private void SubmitRegister()
    {
        SubmitAuth(
            register: true,
            _registerUsernameField.Text?.Trim() ?? "",
            _registerEmailField.Text?.Trim() ?? "",
            _registerPasswordField.Text ?? "");
    }

    private void OpenRegisterDialog()
    {
        if (_registerWindow is { IsPlaced: true })
        {
            return;
        }

        _registerUsernameField = IronTextBox(
            string.IsNullOrWhiteSpace(_usernameField.Text) ? _profile.Username : _usernameField.Text.Trim(),
            320,
            hint: "Username");
        _registerEmailField = IronTextBox(_profile.Email, 320, hint: "Email");
        _registerPasswordField = IronTextBox("", 320, password: true, hint: "Password");
        _registerError = BodyLabel("", Error);
        _registerError.Visible = false;

        var content = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(22, 18),
            Widgets =
            {
                DisplayLabel("Register", 22, Bone),
                LabeledField("Username", _registerUsernameField),
                LabeledField("Email", _registerEmailField),
                LabeledField("Password", _registerPasswordField),
                _registerError,
                IronButton("Create account", SubmitRegister, width: 320),
                IronButton("Cancel", CloseRegisterDialog, width: 180)
            }
        };

        _registerWindow = new Window
        {
            Title = "",
            Content = content,
            Background = new SolidBrush(new Color(20, 12, 10, 245)),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1)
        };
        _registerWindow.TitlePanel.Visible = false;
        _registerWindow.Closed += (_, _) => _registerWindow = null;
        _registerWindow.ShowModal(_desktop);
        _registerWindow.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
        var bounds = _registerWindow.ActualBounds;
        _registerWindow.Left = (Core.ReferenceResolution.X - bounds.Width) / 2;
        _registerWindow.Top = Math.Max(80, (Core.ReferenceResolution.Y - bounds.Height) / 2);
    }

    private void CloseRegisterDialog()
    {
        _registerWindow?.Close();
        _registerWindow = null;
    }

    private void SubmitAuth(bool register, string username, string email, string password)
    {
        if (_busy)
        {
            return;
        }

        if (!PlayerProfile.IsValidUsername(username))
        {
            ShowAuthError(register, $"Username must be {PlayerProfile.MinUsernameLength}–{PlayerProfile.MaxUsernameLength} characters.");
            return;
        }

        if (register && !AccountStore.TryNormalizeEmail(email, out _, out var emailError))
        {
            ShowAuthError(register, emailError);
            return;
        }

        if (password.Length < PlayerProfile.MinPasswordLength)
        {
            ShowAuthError(register, $"Password must be at least {PlayerProfile.MinPasswordLength} characters.");
            return;
        }

        PersistServerHost();
        _pendingAuthRegister = register;
        _pendingSession = null;
        _pendingConnectionError = null;
        _pendingAuthError = null;
        var playerId = _profile.PlayerId;
        BeginBusy(MenuBusyKind.Auth, register ? "Registering..." : "Signing in...", Task.Run(async () =>
        {
            try
            {
                using var compat = new ArenaMatchClient(timeout: TimeSpan.FromSeconds(5));
                await compat.EnsureCompatible();
            }
            catch (Exception ex)
            {
                _pendingConnectionError = ex.Message;
                return;
            }

            try
            {
                using var client = new ArenaMatchClient();
                _pendingSession = register
                    ? await client.Register(username, password, email, playerId)
                    : await client.Login(username, password);
            }
            catch (Exception ex)
            {
                _pendingAuthError = ex.Message;
            }
        }));
    }

    private void FinishAuth()
    {
        if (_pendingConnectionError != null)
        {
            ShowConnectionError(_pendingConnectionError);
            _pendingConnectionError = null;
            return;
        }

        _connectionError.Visible = false;
        var register = _pendingAuthRegister;
        if (_pendingAuthError != null)
        {
            ShowAuthError(register, _pendingAuthError);
            _pendingAuthError = null;
            return;
        }

        var session = _pendingSession;
        _pendingSession = null;
        if (session is not { Authenticated: true } || string.IsNullOrWhiteSpace(session.Token))
        {
            ShowAuthError(register, session?.Error ?? (register ? "Could not register." : "Wrong username or password."));
            return;
        }

        _profile.ApplyAccount(session.PlayerId, session.Username, session.Email, session.Token);
        _passwordField.Text = "";
        _authError.Visible = false;
        CloseRegisterDialog();
        RebuildPlayPanel();
        RefreshPanels();
    }

    private void Logout()
    {
        _profile.ClearSession();
        _passwordField.Text = "";
        _authError.Visible = false;
        RefreshPanels();
    }

    private void ShowAuthError(bool register, string message)
    {
        var label = register ? _registerError : _authError;
        label.Text = message;
        label.Visible = true;
    }

    private void RefreshPanels()
    {
        var ready = _profile.HasSession;
        _authPanel.Visible = !ready;
        _playPanel.Visible = ready;
    }

    private void RebuildPlayPanel()
    {
        _playingAsLabel.Text = IdentityText();
        _rankBadgeHost.Widgets.Clear();
        _rankBadgeHost.Widgets.Add(_versionError == null ? LoadRankBadge() : BodyLabel("Unranked", Dust));
        _playButtons.Widgets.Clear();
        _playButtons.Widgets.Add(IronButton("Start New Arena", () => TryEnterArena(startFresh: true)));
        if (_versionError == null && HasServerArenaProgress())
        {
            _playButtons.Widgets.Add(IronButton("Continue Arena", () => TryEnterArena(startFresh: false)));
        }

        if (_versionError == null)
        {
            _playButtons.Widgets.Add(IronButton("Treasure Trove", OpenShop, ShopEnabled));
        }

        _playButtons.Widgets.Add(IronButton("Log out", Logout, width: 220));
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

    private void ToggleMute()
    {
        var muted = !_clientSettings.AudioMuted;
        _clientSettings.SetAudioMuted(muted);
        Core.Audio.SetMuted(muted);
        _muteButton.Content = DisplayLabel(MuteButtonText(), 20, Bone);
    }

    private string MuteButtonText()
    {
        return _clientSettings.AudioMuted ? "Audio: Off" : "Audio: On";
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

    private void TryRestoreSession()
    {
        if (string.IsNullOrWhiteSpace(_profile.SessionToken))
        {
            return;
        }

        try
        {
            using var client = new ArenaMatchClient();
            var session = client.GetSession().GetAwaiter().GetResult();
            if (session is { Authenticated: true } && PlayerProfile.IsValidUsername(session.Username))
            {
                _profile.ApplyAccount(session.PlayerId, session.Username, session.Email, _profile.SessionToken);
                return;
            }

            _profile.ClearSession();
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

    private static Widget LabeledField(string label, TextBox field, string? note = null)
    {
        var caption = string.IsNullOrEmpty(note) ? label : $"{label}  ·  {note}";
        return new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                BodyLabel(caption, Dust),
                field
            }
        };
    }

    private static TextBox IronTextBox(string text, int width, bool password = false, string hint = "")
    {
        return new TextBox
        {
            Width = width,
            Text = text,
            HintText = hint,
            TextColor = Bone,
            Background = new SolidBrush(Field),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6),
            PasswordField = password
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

    private enum MenuBusyKind
    {
        None,
        EnterArena,
        Auth
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
