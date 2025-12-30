using System.IO;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Scenes.MainGameScene;

public class GameScene : Scene
{
    private CampGui _campGui = null!;
    private GameContext _context = new ();
    private GameState _currentGameState = GameState.Camp;
    private WorldTextHandler _worldTextHandler = new();
    private KeyboardState _previousKeyboardState;
    private BaseGui? ActiveGui { get; set; }

    protected override void OnStart()
    {
        Core.Context = _context;
        _context.OnStateChanged += HandleOnStateChanged;

        if (File.Exists("save.xml"))
        {
            _context.Load("save.xml");
        }
        else
        {
            NewGame();
        }

        StartGame();
    }

    private void ReloadGui()
    {
        ActiveGui?.Dispose();
        
        // Safety check: If we think we're in a zone but there's no current zone, fall back to camp
        var effectiveState = _currentGameState;
        if (effectiveState == GameState.Zone && _context.CurrentZone == null)
        {
            effectiveState = GameState.Camp;
        }
        
        ActiveGui = effectiveState switch
        {
            GameState.Zone => new ZoneGui(_context, _worldTextHandler),
            GameState.Camp => new CampGui(_context, _worldTextHandler), //todo this should only be instantiated once, but it doesn't refresh properly
            _ => ActiveGui
        };
    }

    private void StartGame()
    {
        ActiveGui?.Dispose();
        _campGui = new CampGui(_context, _worldTextHandler);
        ActiveGui = _campGui;
    }

    private void HandleOnStateChanged(GameState state)
    {
        ActiveGui?.Dispose();
        if (state == GameState.StartOver)
        {
            _currentGameState = GameState.Camp;
            StartOver();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Ahhh.... A new specimen...",
                Font = BaseContent.Fonts.Default.Huge,
                Duration = 3,
                Color = Color.WhiteSmoke
            });
            return;
        }

        _currentGameState = state;

        ReloadGui();
    }

    private void NewGame()
    {
        Core.ClearCoroutines();
        _context.Initialize();
        ActiveGui = null;
        StartGame();
    }

    private void StartOver()
    {
        ActiveGui = null;
        StartGame();
    }

    public override void Update(float deltaTime)
    {
        HandleInput();
        ActiveGui?.Update(deltaTime);
    }

    public override void Draw(float deltaTime)
    {
        ActiveGui?.Draw(Core.Graphics.Batcher, deltaTime);
        _worldTextHandler.Render(Core.Graphics.Batcher, deltaTime);
    }

    public override void FixedUpdate()
    {
        for (int i = 0; i < DebugSettings.CombatSpeed; i++)
        {
            _context.Tick();
            _worldTextHandler.Tick();
        }
    }

    private bool WasKeyJustPressed(Keys key, KeyboardState currentState)
    {
        return currentState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }

    private void HandleInput()
    {
        var currentKeyboardState = Keyboard.GetState();

        if (WasKeyJustPressed(Keys.Space, currentKeyboardState))
        {
            _context.TogglePause();
        }

        if (WasKeyJustPressed(Keys.F2, currentKeyboardState))
        {
            NewGame();
            return;
        }

        if (currentKeyboardState.IsKeyDown(Keys.Q))
        {
            ActiveGui?.MouseAttachment?.Detach();
        }

        if (WasKeyJustPressed(Keys.F5, currentKeyboardState))
        {
            _context.Save();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Saved",
                Font = BaseContent.Fonts.Default.Huge,
                Duration = 3,
                Color = Color.WhiteSmoke
            });
        }

        if (WasKeyJustPressed(Keys.F9, currentKeyboardState))
        {
            ActiveGui?.Dispose();
            ActiveGui = null;
            _context.Load("save.xml");
            StartGame();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Loaded",
                Font = BaseContent.Fonts.Default.Huge,
                Duration = 3,
                Color = Color.WhiteSmoke
            });
        }

        if (WasKeyJustPressed(Keys.F12, currentKeyboardState))
        {
            ReloadGui();
        }

        _previousKeyboardState = currentKeyboardState;
    }
}