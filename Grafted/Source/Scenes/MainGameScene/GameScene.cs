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
    private BaseGui? ActiveGui { get; set; }

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
        ActiveGui?.Dispose();
        _campGui = new CampGui(_context.World);
        ActiveGui = _campGui;
    }

    private void HandleOnStateChanged(GameState state)
    {
        ActiveGui?.Dispose();
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
        _context.DeathRecords.Reset();
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
    }

    public override void FixedUpdate()
    {
        for (int i = 0; i < DebugSettings.CombatSpeed; i++)
        {
            _context.Tick();
        }
    }

    private void HandleInput()
    {
        if (Input.IsKeyPressed(Keys.Space))
        {
            _context.TogglePause();
        }
        
        if (Input.IsKeyPressed(Keys.S) && Input.IsKeyDown(Keys.LeftControl))
        {
            _context.Save("save.xml");
            ActiveGui!.PushScreenMessage(new ScreenMessageData
            {
                Text = "Game Saved",
                Font = BaseContent.Fonts.Default.VeryLarge,
                Duration = 3,
                Color = Color.LimeGreen
            });
        }

        if (Input.IsKeyPressed(Keys.L) && Input.IsKeyDown(Keys.LeftControl))
        {
            ActiveGui?.Dispose();
            ActiveGui = null;
            _context.Load("save.xml");
            StartGame();
        }

        if (Input.IsKeyPressed(Keys.F2) && Input.IsKeyDown(Keys.LeftControl))
        {
            QuickPlay();
        }
    }
}