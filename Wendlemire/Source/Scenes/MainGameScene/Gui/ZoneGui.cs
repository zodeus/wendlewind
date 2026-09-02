using Wendlemire.Scenes.MainGameScene.Gui.CombatGui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.DevConsole;

namespace Wendlemire.Scenes.MainGameScene.Gui;

public class ZoneGui : BaseGui
{
    private readonly GameContext _context;
    private readonly WorldTextHandler _worldTextHandler;
    private readonly Zone _zone;
    private CombatPreparationScreen? _combatPreparationScreen;
    private TestSimSelectorScreen? _testSimSelectorScreen;
    private CombatScreen? _combatScreen;
    private MysteryScreen? _shrineScreen;
    private CombatResultsScreen? _combatResultsScreen;
    private Zone Zone => _zone;
    public override WorldTextHandler WorldTextHandler => _worldTextHandler;

    public ZoneGui(GameContext context, WorldTextHandler worldTextHandler)
    {
        _context = context;
        _worldTextHandler = worldTextHandler;
        _zone = context.CurrentZone!;
        Desktop = new Desktop
        {
            Root = new Panel(),
            HasExternalTextInput = true
        };
        Core.ConfigureDesktopScaling(Desktop);
        InitializeConsole();

        Zone.OnStateChanged += HandleZoneStateChanged;
        Zone.OnZoneMessage += HandleZoneMessage;

        HandleZoneStateChanged(Zone.State);
    }

    private void HandleZoneMessage(ScreenMessageData message)
    {
        PushScreenMessage(message);
    }

    private void HandleZoneStateChanged(ZoneState state)
    {
        MouseAttachment?.Detach();
        ClearScreenMessage();

        _combatPreparationScreen?.RemoveFromParent();
        _combatPreparationScreen = null;
        _testSimSelectorScreen?.RemoveFromParent();
        _testSimSelectorScreen = null;

        _combatResultsScreen?.RemoveFromParent();
        _combatResultsScreen = null;

        _combatScreen?.RemoveFromParent();
        _combatScreen?.Dispose();
        _worldTextHandler.Clear();
        _combatScreen = null;

        _shrineScreen?.RemoveFromParent();
        _shrineScreen = null;

        CloseEntityWindow();
        _shrineScreen = null;

        switch (state)
        {
            case ZoneState.Preparation:
                if (DebugSettings.TestSimMode)
                {
                    _testSimSelectorScreen = new TestSimSelectorScreen(this, _context);
                    (Desktop.Root as Panel)!.Widgets.Add(_testSimSelectorScreen);
                }
                else
                {
                    _combatPreparationScreen = new CombatPreparationScreen(this, _context);
                    (Desktop.Root as Panel)!.Widgets.Add(_combatPreparationScreen);
                }
                break;
            case ZoneState.Combat:
                _combatScreen = new CombatScreen(this, _context)
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                (Desktop.Root as Panel)!.Widgets.Add(_combatScreen);
                break;
            case ZoneState.CombatResults:
                _combatResultsScreen = new CombatResultsScreen(this, _context);
                (Desktop.Root as Panel)!.Widgets.Add(_combatResultsScreen);
                break;
            case ZoneState.Mystery:
                _shrineScreen = new MysteryScreen(this, _context.PlayerPawn, _context.CurrentZone!.ActiveEncounter!.Def.MysteryProperties!);
                (Desktop.Root as Panel)!.Widgets.Add(_shrineScreen);
                break;
        }
        
        // Ensure console stays on top
        BringConsoleToFront();
    }

    public override void Update(float deltaTime)
    {
        _combatPreparationScreen?.Update();
        _testSimSelectorScreen?.Update();
        _combatScreen?.Update(deltaTime);
        _combatResultsScreen?.Update();
        _shrineScreen?.Update(deltaTime);
        base.Update(deltaTime);
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void Draw(SpriteBatch spriteBatch, float deltaTime)
    {
        // Pre-render body renderers BEFORE drawing the background to avoid
        // backbuffer being discarded when render targets switch
        PawnRenderer.PreRenderAll(deltaTime);
        
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(
            Zone.ZoneDef.GetBackground(), new Rectangle(0, 0, Screen.Width, Screen.Height),
            new Color(255, 255, 255, 0.1f)
        );
        spriteBatch.End();
        
        base.Draw(spriteBatch, deltaTime);
    }

    public override void Dispose()
    {
        Zone.OnStateChanged -= HandleZoneStateChanged;
        Zone.OnZoneMessage -= HandleZoneMessage;
        base.Dispose();
    }

    public void LeaveMystery()
    {
        Zone.Stage++;
        _combatResultsScreen = new CombatResultsScreen(this, _context);
        (Desktop.Root as Panel)!.Widgets.Add(_combatResultsScreen);

        _shrineScreen?.RemoveFromParent();
        _shrineScreen = null;
        
        // Ensure console stays on top
        BringConsoleToFront();
    }
}