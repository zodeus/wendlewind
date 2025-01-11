using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public class ZoneGui : BaseGui
{
    private readonly GameContext _context;
    private readonly PawnBodyEffectsWindow _pawnBodyEffectsWindow;

    private CombatScreen? _combatScreen;
    private CombatResultsScreen? _combatResultsScreen;
    private Zone Zone => _context.CurrentZone!;

    public ZoneGui(GameContext context)
    {
        _context = context;
        Desktop = new Desktop
        {
            Scale = new Vector2(1, 1),
            Root = new Panel(),
            HasExternalTextInput = true
        };

        Zone.OnStateChanged += HandleZoneStateChanged;
        Zone.OnZoneMessage += HandleZoneMessage;

        //todo this is hack
        HandleZoneStateChanged(ZoneState.Combat);

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(Core.Context.World.PlayerPawn);
        _pawnBodyEffectsWindow.Show(Desktop, new Point(50, 20));
    }

    private void HandleZoneMessage(ScreenMessageData message)
    {
        PushScreenMessage(message);
    }

    private void HandleZoneStateChanged(ZoneState state)
    {
        if (state == ZoneState.Combat)
        {
            _combatResultsScreen?.RemoveFromParent();
            _combatResultsScreen = null;
            _combatScreen = new CombatScreen(this, _context);
            (Desktop.Root as Panel)!.AddChild(_combatScreen);
        }

        if (state == ZoneState.CombatResults)
        {
            ClearScreenMessage();
            _combatScreen?.RemoveFromParent();
            _combatScreen = null;
            _combatResultsScreen = new CombatResultsScreen(this, _context);
            (Desktop.Root as Panel)!.AddChild(_combatResultsScreen);
        }
    }

    public override void Update(float deltaTime)
    {
        _combatScreen?.Update();
        _combatResultsScreen?.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    public override void HandleInput()
    {
        _combatResultsScreen?.HandleInput();
        base.HandleInput();
    }

    public override void Draw(SpriteBatch spriteBatch, float deltaTime)
    {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(Zone.BiomeDef.BackgroundTexture, new Rectangle(0, 0, Screen.Width, Screen.Height),
            new Color(255, 255, 255, Zone.BiomeDef.BackgroundTextureTransparency));
        spriteBatch.End();

        base.Draw(spriteBatch, deltaTime);
    }

    public override void Dispose()
    {
        Zone.OnStateChanged -= HandleZoneStateChanged;
        Zone.OnZoneMessage -= HandleZoneMessage;
    }
}