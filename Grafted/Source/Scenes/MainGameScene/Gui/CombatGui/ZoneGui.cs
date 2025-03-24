namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public class ZoneGui : BaseGui
{
    private readonly GameContext _context;

    private CombatScreen? _combatScreen;
    private ShrineScreen? _shrineScreen;
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

        HandleZoneStateChanged(Zone.State);
    }

    private void HandleZoneMessage(ScreenMessageData message)
    {
        PushScreenMessage(message);
    }

    private void HandleZoneStateChanged(ZoneState state)
    {
        ClearScreenMessage();

        _combatResultsScreen?.RemoveFromParent();
        _combatResultsScreen = null;

        _combatScreen?.RemoveFromParent();
        _combatScreen = null;

        _shrineScreen?.RemoveFromParent();
        _shrineScreen = null;

        CloseEntityWindow();
        _shrineScreen = null;

        switch (state)
        {
            case ZoneState.Combat:
                _combatScreen = new CombatScreen(this, _context);
                (Desktop.Root as Panel)!.Widgets.Add(_combatScreen);
                break;
            case ZoneState.CombatResults:
                _combatResultsScreen = new CombatResultsScreen(this, _context);
                (Desktop.Root as Panel)!.Widgets.Add(_combatResultsScreen);
                break;
            case ZoneState.Shrine:
                _shrineScreen = new ShrineScreen(this, _context.PlayerPawn, _context.CurrentZone!.ActiveEncounter!.Def.ShrineProperties!);
                (Desktop.Root as Panel)!.Widgets.Add(_shrineScreen);
                break;
        }
    }

    public override void Update(float deltaTime)
    {
        _combatScreen?.Update();
        _combatResultsScreen?.Update();
        _shrineScreen?.Update(deltaTime);
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

    public void LeaveShrine()
    {
        Zone.Stage++;
        _combatResultsScreen = new CombatResultsScreen(this, _context);
        (Desktop.Root as Panel)!.Widgets.Add(_combatResultsScreen);

        _shrineScreen?.RemoveFromParent();
        _shrineScreen = null;
    }
}