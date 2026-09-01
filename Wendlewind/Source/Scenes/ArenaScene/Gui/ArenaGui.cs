using Wendlewind.Scenes.MainGameScene;
using Wendlewind.Scenes.MainGameScene.Gui;
using Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaGui : BaseGui
{
    private readonly GameContext _context;
    private readonly ArenaScene _scene;
    private readonly WorldTextHandler _worldTextHandler;
    private Widget? _activeScreen;
    private CombatScreen? _combatScreen;
    private ArenaShopScreen? _shopScreen;
    private ArenaPrepScreen? _prepScreen;
    private ArenaMapScreen? _mapScreen;

    public override WorldTextHandler WorldTextHandler => _worldTextHandler;

    public ArenaGui(GameContext context, ArenaScene scene, WorldTextHandler worldTextHandler)
    {
        _context = context;
        _scene = scene;
        _worldTextHandler = worldTextHandler;
        Desktop = new Desktop
        {
            Root = new Panel(),
            HasExternalTextInput = true
        };
        Core.ConfigureDesktopScaling(Desktop);
        InitializeConsole();

        if (_context.ArenaRun != null)
        {
            _context.ArenaRun.OnPhaseChanged += HandlePhaseChanged;
        }

        HandlePhaseChanged(_context.ArenaRun?.Phase ?? ArenaPhase.GeneralStore);
    }

    private void HandlePhaseChanged(ArenaPhase phase)
    {
        MouseAttachment?.Detach();
        ClearScreenMessage();
        ClearScreens();

        var root = (Panel)Desktop.Root;
        _activeScreen = phase switch
        {
            ArenaPhase.GeneralStore or ArenaPhase.Shop => _shopScreen = new ArenaShopScreen(this, _context, _scene.FinishShopping),
            ArenaPhase.Prep => _prepScreen = new ArenaPrepScreen(this, _context, _scene.BeginFight, _scene.ReturnToShop),
            ArenaPhase.Matching => new ArenaMatchingScreen(_scene.MatchError, _scene.ReturnToPrep),
            ArenaPhase.Combat => _combatScreen = new CombatScreen(this, _context, _scene.OnVisualCombatFinished),
            ArenaPhase.Results when _context.ArenaRun?.IsRunOver == true =>
                new ArenaRunEndScreen(_context, _scene.ReturnToMenu),
            ArenaPhase.Results => new ArenaResultsScreen(_context, _scene.ContinueFromResults),
            ArenaPhase.MerchantSelect => _mapScreen = new ArenaMapScreen(_context, _scene.SelectMerchant),
            ArenaPhase.RunEnd => new ArenaRunEndScreen(_context, _scene.ReturnToMenu),
            _ => new ArenaMatchingScreen("Unknown arena phase", _scene.ReturnToPrep)
        };

        _activeScreen.HorizontalAlignment = HorizontalAlignment.Stretch;
        _activeScreen.VerticalAlignment = VerticalAlignment.Stretch;
        root.Widgets.Add(_activeScreen);
        BringConsoleToFront();
    }

    private void ClearScreens()
    {
        _shopScreen?.RemoveFromParent();
        _shopScreen = null;
        _prepScreen?.RemoveFromParent();
        _prepScreen = null;
        _mapScreen?.RemoveFromParent();
        _mapScreen = null;
        _combatScreen?.RemoveFromParent();
        _combatScreen?.Dispose();
        _combatScreen = null;
        _worldTextHandler.Clear();
        _activeScreen?.RemoveFromParent();
        _activeScreen = null;
        TooltipHelper.Hide();
        CloseEntityWindow();
    }

    public override void Update(float deltaTime)
    {
        _shopScreen?.Update();
        _prepScreen?.Update();
        _mapScreen?.Update();
        _combatScreen?.Update(deltaTime);
        base.Update(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch, float deltaTime)
    {
        PawnRenderer.PreRenderAll(deltaTime);
        base.Draw(spriteBatch, deltaTime);
    }

    public override void Dispose()
    {
        if (_context.ArenaRun != null)
        {
            _context.ArenaRun.OnPhaseChanged -= HandlePhaseChanged;
        }

        _combatScreen?.Dispose();
        base.Dispose();
    }
}
