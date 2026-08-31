using Microsoft.Xna.Framework.Input;
using Wendlewind.Graphics.Textures;
using Wendlewind.Scenes.ArenaScene;
using Wendlewind.Scenes.Components;
using Wendlewind.Scenes.MainGameScene;

namespace Wendlewind.Sim;

public class MainMenuScene : Scene
{
    private Desktop _desktop = null!;
    private PlayerProfile _profile = null!;
    private TextBox _usernameField = null!;
    private Label _usernameError = null!;
    private Widget _usernamePanel = null!;
    private Widget _playPanel = null!;
    private EventHandler<TextInputEventArgs>? _textInputHandler;
    private KeyboardState _previousKeyboard;

    protected override void OnStart()
    {
        _profile = PlayerProfile.LoadOrCreate();
        TryHydrateUsernameFromServer();

        _usernameField = new TextBox
        {
            Width = 320,
            Text = _profile.Username,
            TextColor = Color.White,
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(8, 4)
        };
        _usernameError = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "",
            TextColor = Color.IndianRed,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false
        };

        var confirm = MenuButton("Confirm", ConfirmUsername);
        _usernamePanel = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Choose a username",
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                _usernameField,
                _usernameError,
                confirm
            }
        };

        var playButtons = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        playButtons.Widgets.Add(MenuButton("Start New Arena", () =>
        {
            ArenaScene.StartFresh = true;
            Core.ChangeScene<ArenaScene>();
        }));
        playButtons.Widgets.Add(MenuButton("Campaign", () => Core.ChangeScene<GameScene>()));
        if (HasServerArenaProgress())
        {
            playButtons.Widgets.Add(MenuButton("Continue Arena", () => Core.ChangeScene<ArenaScene>()));
        }

        _playPanel = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"Playing as {_profile.Username}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextColor = new Color(200, 200, 200)
                },
                playButtons
            }
        };

        var banner = new Panel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            Width = 900,
            Height = 420,
            Padding = new Thickness(8)
        };
        banner.Widgets.Add(new Image
        {
            Background = new TextureRegion(BaseContent.Textures.MainMenuBackground),
            Width = 900,
            Height = 420
        });

        _desktop = new Desktop
        {
            HasExternalTextInput = true,
            Root = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20,
                Widgets = { banner, _usernamePanel, _playPanel }
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
        if (_usernamePanel.Visible
            && keyboard.IsKeyDown(Keys.Enter)
            && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            ConfirmUsername();
        }

        _previousKeyboard = keyboard;
    }

    public override void End()
    {
        if (_textInputHandler != null)
        {
            Core.Instance.Window.TextInput -= _textInputHandler;
            _textInputHandler = null;
        }
    }

    public override void Draw(float deltaTime)
    {
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

        _profile.SetUsername(username);
        TryPushUsernameToServer();
        if (_playPanel is VerticalStackPanel play && play.Widgets[0] is Label playingAs)
        {
            playingAs.Text = $"Playing as {_profile.Username}";
        }

        RefreshPanels();
    }

    private void RefreshPanels()
    {
        var ready = _profile.HasUsername;
        _usernamePanel.Visible = !ready;
        _playPanel.Visible = ready;
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

    private static CursorButton MenuButton(string text, Action onClick)
    {
        var button = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label(BaseContent.Styles.Label.Medium)
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Width = 320,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => onClick();
        return button;
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
}
