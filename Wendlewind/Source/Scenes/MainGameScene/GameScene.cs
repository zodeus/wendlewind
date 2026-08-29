using System.IO;
using Wendlewind.Scenes.Components;
using Wendlewind.Scenes.MainGameScene.Gui;

namespace Wendlewind.Scenes.MainGameScene;

public class GameScene : Scene
{
    private float _startOverCooldown = 3f;
    private MapGui _mapGui = null!;
    private GameContext _context = new ();
    private GameState _currentGameState = GameState.Map;
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
        TryStartSmokeEncounter();
    }

    private void ReloadGui()
    {
        ActiveGui?.Dispose();
        
        // Safety check: If we think we're in a zone but there's no current zone, fall back to camp
        var effectiveState = _currentGameState;
        if (effectiveState == GameState.Zone && _context.CurrentZone == null)
        {
            effectiveState = GameState.Map;
        }
        
        ActiveGui = effectiveState switch
        {
            GameState.Map => new MapGui(_context, _worldTextHandler), 
            GameState.Zone => new ZoneGui(_context, _worldTextHandler),
            _ => ActiveGui
        };
    }

    private void StartGame()
    {
        ActiveGui?.Dispose();
        _mapGui = new MapGui(_context, _worldTextHandler);
        ActiveGui = _mapGui;
    }

    private void HandleOnStateChanged(GameState state)
    {
        ActiveGui?.Dispose();
        if (state == GameState.StartOver)
        {
            _currentGameState = GameState.Map;
            StartOver();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Ahhh.... A new specimen...",
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

    private void TryStartSmokeEncounter()
    {
        if (Environment.GetEnvironmentVariable("WENDLEWIND_SMOKE") != "1")
        {
            return;
        }

        var zone = _context.World.Zones.OrderBy(z => z.ZoneDef.Stage).FirstOrDefault();
        if (zone == null)
        {
            return;
        }

        _context.EnterZone(zone.ZoneDef);
        _context.CurrentZone?.NextEncounter();
    }

    private void StartOver()
    {
        ActiveGui = null;
        StartGame();
    }

    public override void Update(float deltaTime)
    {
        if (_startOverCooldown > 0)
        {
            _startOverCooldown -= deltaTime;
        }

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

        // Toggle developer console with ~ key
        if (WasKeyJustPressed(Keys.OemTilde, currentKeyboardState))
        {
            ActiveGui?.ToggleConsole();
            _previousKeyboardState = currentKeyboardState;
            return;
        }

        // When console is open, don't process other game inputs
        if (ActiveGui?.IsConsoleOpen == true)
        {
            _previousKeyboardState = currentKeyboardState;
            return;
        }

        if (WasKeyJustPressed(Keys.Space, currentKeyboardState))
        {
            _context.TogglePause();
        }

        if (WasKeyJustPressed(Keys.F2, currentKeyboardState))
        {
            NewGame();
            return;
        }

        if (WasKeyJustPressed(Keys.F5, currentKeyboardState))
        {
            _context.Save();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Saved",
                Duration = 3,
                Color = Color.WhiteSmoke
            });
        }

        if (WasKeyJustPressed(Keys.F6, currentKeyboardState) && _startOverCooldown <= 0)
        {
            _context.StartOver();
            _startOverCooldown = 3f;
            return;
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