using System.IO;
using System.Windows.Forms.VisualStyles;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Scenes.MainGameScene.Gui.CombatGui;

namespace Grafted.Scenes.MainGameScene;

public class GameScene : Scene
{
    private CampGui _campGui = null!;
    private GameContext _context = null!;
    private GameState _currentState = GameState.Camp;
    private BaseGui ActiveGui { get; set; } = null!;

    protected override void OnStart()
    {
        _context = new GameContext();
        Core.Context = _context;
        _context.OnStateChanged += HandleOnStateChanged;

        if (DebugSettings.QuickLoad && File.Exists("save.xml"))
        {
            _context.Load("save.xml");
        }
        else
        {
            QuickPlay();
        }

        StartGame();
    }

    private void StartGame()
    {
        _campGui = new CampGui(_context.World);
        ActiveGui = _campGui;
    }

    private void HandleOnStateChanged(GameState state)
    {
        _currentState = state;
        if (ActiveGui is ZoneGui)
        {
            ActiveGui.Dispose();
        }

        if (state == GameState.Restart)
        {
            QuickPlay();
            return;
        }

        ActiveGui = state switch
        {
            GameState.Zone => new ZoneGui(_context),
            GameState.Camp => new CampGui(_context.World), //todo this should only be instantiated once, but it doesn't refresh properly
            _ => ActiveGui
        };
    }

    private void QuickPlay()
    {
        Core.ClearCoroutines();
        _context.World = WorldGenerator.GenerateNewWorld(Defs.PawnConfigs.PlayerPawn);
        _context.Ticks = 0;
        StartGame();
    }

    public override void Update(float deltaTime)
    {
        HandleInput();
        ActiveGui.Update(deltaTime);
    }

    public override void Draw(float deltaTime)
    {
        ActiveGui.Draw(Core.Graphics.Batcher, deltaTime);
    }

    public override void FixedUpdate()
    {
        _context.Tick();
    }

    private void HandleInput()
    {
        if (Input.IsKeyPressed(Keys.Space))
        {
        }

        if (Input.IsKeyPressed(Keys.S) && Input.IsKeyDown(Keys.LeftControl))
        {
            _context.Save("save.xml");
            _campGui.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Saved",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 5,
                Color = Color.LimeGreen
            });
        }

        if (Input.IsKeyPressed(Keys.L) && Input.IsKeyDown(Keys.LeftControl))
        {
            _context.Load("save.xml");
        }

        if (Input.IsKeyPressed(Keys.F2) && Input.IsKeyDown(Keys.LeftControl))
        {
            QuickPlay();
        }
    }
}