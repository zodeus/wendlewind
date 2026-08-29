using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Assets;
using Wendlewind.Graphics;
using Wendlewind.PawnLayout;
using Wendlewind.Utils;
using Num = System.Numerics;

namespace Wendlewind.Editor;

public sealed class EditorApp : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private ImGuiRenderer? _imGui;
    private EquipmentGridEditor? _gridEditor;
    private DevConsoleTool? _console;
    private TestSimTool? _testSim;
    private EditorPawnRenderer? _attackerRenderer;
    private EditorPawnRenderer? _defenderRenderer;
    private ServiceProvider? _services;
    private IServiceScope? _scope;
    private GameContext? _context;

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
        base.Initialize();
        EnsureTypesLoaded();
        DataLoader.Load();

        RenderHost.GraphicsDevice = GraphicsDevice;
        RenderHost.Content = new ContentManager(GraphicsDevice, "Content");

        _services = SimServices.BuildRoot();
        _scope = _services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<GameContext>();
        _context.Initialize();

        _imGui = new ImGuiRenderer(this);
        _attackerRenderer = new EditorPawnRenderer(GraphicsDevice, _imGui);
        _defenderRenderer = new EditorPawnRenderer(GraphicsDevice, _imGui);
        _gridEditor = new EquipmentGridEditor(_context);
        _console = new DevConsoleTool(_context);
        _testSim = new TestSimTool(_context, _attackerRenderer, _defenderRenderer);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) && Keyboard.GetState().IsKeyDown(Keys.LeftControl))
        {
            Exit();
        }

        _testSim?.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _testSim?.PreRender();
        GraphicsDevice.Clear(new Color(18, 18, 20));
        _imGui!.BeginLayout(gameTime);

        var display = ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowPos(Num.Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(display, ImGuiCond.Always);
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (ImGui.Begin("Wendlewind Editor", flags))
        {
            if (ImGui.BeginTabBar("editor-tabs"))
            {
                if (ImGui.BeginTabItem("Equipment Grid"))
                {
                    _gridEditor!.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Dev Console"))
                {
                    _console!.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Test Sim"))
                {
                    _testSim!.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();
        _imGui.EndLayout();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _attackerRenderer?.Dispose();
            _defenderRenderer?.Dispose();
            _imGui?.Dispose();
            _scope?.Dispose();
            _services?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void EnsureTypesLoaded()
    {
        _ = typeof(PawnDef);
        _ = typeof(EquipmentGridDef);
        _ = typeof(BodyPartLayoutDef);
        _ = typeof(BodyDef);
        BodyPartLayoutRegistry.EnsureLoaded();
        if (GenTypes.GetTypeInAnyAssembly("PawnDef") == null)
        {
            throw new InvalidOperationException("Simulation types were not visible to GenTypes.");
        }
    }
}
