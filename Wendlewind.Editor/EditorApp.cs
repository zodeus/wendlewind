using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Utils;

namespace Wendlewind.Editor;

public sealed class EditorApp : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private ImGuiRenderer? _imGui;
    private EquipmentGridEditor? _gridEditor;
    private ServiceProvider? _services;
    private IServiceScope? _scope;

    public EditorApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1680;
        _graphics.PreferredBackBufferHeight = 1050;
        _graphics.SynchronizeWithVerticalRetrace = true;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Wendlewind Editor";
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        EnsureSimulationTypesLoaded();
        DataLoader.Load();

        _services = SimServices.BuildRoot();
        _scope = _services.CreateScope();
        var context = _scope.ServiceProvider.GetRequiredService<GameContext>();

        _imGui = new ImGuiRenderer(this);
        _gridEditor = new EquipmentGridEditor(context);
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) && Keyboard.GetState().IsKeyDown(Keys.LeftControl))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 20));
        _imGui!.BeginLayout(gameTime);
        _gridEditor!.Draw();
        _imGui.EndLayout();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _imGui?.Dispose();
            _scope?.Dispose();
            _services?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void EnsureSimulationTypesLoaded()
    {
        _ = typeof(PawnDef);
        _ = typeof(EquipmentGridDef);
        _ = typeof(BodyDef);
        if (GenTypes.GetTypeInAnyAssembly("PawnDef") == null)
        {
            throw new InvalidOperationException("Simulation types were not visible to GenTypes.");
        }
    }
}
