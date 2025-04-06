using System.IO;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Scenes.MainGameScene;

public class GameScene : Scene
{
    private CampGui _campGui = null!;
    private GameContext _context = null!;
    private GameState _currentGameState = GameState.Camp;
    private WorldTextHandler _worldTextHandler;
    private BaseGui? ActiveGui { get; set; }

    protected override void OnStart()
    {
        _context = new GameContext();
        Core.Context = _context;
        _context.OnStateChanged += HandleOnStateChanged;
        _worldTextHandler = new WorldTextHandler();
        //todo dispose of this
        //Core.Instance.Window.ClientSizeChanged += (_, _) => ReloadGui();

        if (File.Exists("save.xml"))
        {
            _context.Load("save.xml");
        }
        else
        {
            QuickPlay();
        }

        StartGame();
    }

    private void ReloadGui()
    {
        ActiveGui?.Dispose();
        ActiveGui = _currentGameState switch
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
        if (state == GameState.Restart)
        {
            _currentGameState = GameState.Camp;
            QuickPlay();
            return;
        }

        _currentGameState = state;

        ReloadGui();
    }

    private void QuickPlay()
    {
        Core.ClearCoroutines();
        _context.World = WorldGenerator.GenerateNewWorld();
        _context.CurrentZone = null;
        _context.Ticks = 0;
        _context.DeathRecords.Reset();
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

    private void HandleInput()
    {
        if (Input.IsKeyPressed(Keys.Space))
        {
            _context.TogglePause();
        }

        if (Input.IsKeyPressed(Keys.F5))
        {
            _context.Save("save.xml");
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Saved",
                Font = BaseContent.Fonts.Default.Huge,
                Duration = 3,
                Color = Color.WhiteSmoke
            });
        }

        if (Input.IsKeyPressed(Keys.F9))
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

        if (Input.IsKeyPressed(Keys.Q))
        {
            ActiveGui?.MouseAttachment?.Detach();
        }

        if (Input.IsKeyPressed(Keys.F2))
        {
            QuickPlay();
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "New Game",
                Font = BaseContent.Fonts.Default.Huge,
                Duration = 3,
                Color = Color.WhiteSmoke
            });
        }

        if (Input.IsKeyPressed(Keys.F12))
        {
            ReloadGui();
        }
    }
}