using Wendlemire.Scenes.MainGameScene;
using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

namespace Wendlemire.Scenes.ArenaScene.Gui;

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
    private ArenaMatchingScreen? _matchingScreen;
    private readonly BusyOverlay _busyOverlay = new();

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

        if (_context.CurrentZone != null)
        {
            _context.CurrentZone.OnZoneMessage += HandleZoneMessage;
        }

        HandlePhaseChanged(_context.ArenaRun?.Phase ?? ArenaPhase.GeneralStore);
    }

    private void HandleZoneMessage(ScreenMessageData message)
    {
        PushScreenMessage(message);
    }

    private void HandlePhaseChanged(ArenaPhase phase)
    {
        if (phase is ArenaPhase.MerchantSelect or ArenaPhase.Results
            && _context.ArenaRun is { IsRunOver: false, CurrentMerchant: null } run)
        {
            run.AssignNextMerchant();
        }

        MouseAttachment?.Detach();
        ClearScreenMessage();
        ClearScreens();

        var root = (Panel)Desktop.Root;
        _activeScreen = phase switch
        {
            ArenaPhase.GeneralStore or ArenaPhase.Shop => _shopScreen = new ArenaShopScreen(
                this,
                _context,
                _scene.FinishShopping,
                _scene.SaveProgress),
            ArenaPhase.Prep => _prepScreen = new ArenaPrepScreen(this, _context, _scene.BeginFight, _scene.ReturnToShop, _scene.CurrentRank),
            ArenaPhase.Matching => _matchingScreen = new ArenaMatchingScreen(_scene.MatchError, _scene.ReturnToPrep),
            ArenaPhase.Combat => _combatScreen = new CombatScreen(
                this,
                _context,
                _scene.OnVisualCombatFinished,
                _scene.RecordVisualCombatResult),
            ArenaPhase.Results when _context.ArenaRun?.IsRunOver == true =>
                new ArenaRunEndScreen(_context, _scene.ReturnToMenu, _scene.LastFinishedRun, _scene.CurrentRank),
            ArenaPhase.Results or ArenaPhase.MerchantSelect =>
                _mapScreen = new ArenaMapScreen(_context, _scene.SelectMerchant, _scene.LastCombatResult),
            ArenaPhase.RunEnd => new ArenaRunEndScreen(_context, _scene.ReturnToMenu, _scene.LastFinishedRun, _scene.CurrentRank),
            _ => new ArenaMatchingScreen("Unknown arena phase", _scene.ReturnToPrep)
        };

        _activeScreen.HorizontalAlignment = HorizontalAlignment.Stretch;
        _activeScreen.VerticalAlignment = VerticalAlignment.Stretch;
        root.Widgets.Add(_activeScreen);
        BindZoneMessages();
        BringConsoleToFront();
    }

    private void BindZoneMessages()
    {
        if (_context.CurrentZone == null)
        {
            return;
        }

        _context.CurrentZone.OnZoneMessage -= HandleZoneMessage;
        _context.CurrentZone.OnZoneMessage += HandleZoneMessage;
    }

    private void ClearScreens()
    {
        _shopScreen?.RemoveFromParent();
        _shopScreen = null;
        _prepScreen?.RemoveFromParent();
        _prepScreen = null;
        _mapScreen?.RemoveFromParent();
        _mapScreen = null;
        _matchingScreen?.RemoveFromParent();
        _matchingScreen = null;
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
        _shopScreen?.Update(deltaTime);
        _prepScreen?.Update();
        _mapScreen?.Update();
        _matchingScreen?.Update(deltaTime);
        _combatScreen?.Update(deltaTime);
        _busyOverlay.Update(deltaTime);
        base.Update(deltaTime);
    }

    public void ShowBusy(string message)
    {
        _busyOverlay.ShowModal(Desktop, message);
    }

    public void HideBusy()
    {
        _busyOverlay.Hide();
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

        if (_context.CurrentZone != null)
        {
            _context.CurrentZone.OnZoneMessage -= HandleZoneMessage;
        }

        _busyOverlay.Hide();
        _combatScreen?.Dispose();
        base.Dispose();
    }
}
