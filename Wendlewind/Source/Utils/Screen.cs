namespace Wendlewind.Utils;

public static class Screen {
    internal static GraphicsDeviceManager GraphicsManager = null!;

    internal static void Initialize(GraphicsDeviceManager graphicsManager) => GraphicsManager = graphicsManager;

    public static int Width {
        get => GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
        set => GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferWidth = value;
    }

    public static int Height {
        get => GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
        set => GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferHeight = value;
    }

    public static Vector2 Size => new(Width, Height);


    public static Vector2 Center => new(Width / 2, Height / 2);

    public static int PreferredBackBufferWidth {
        get => GraphicsManager.PreferredBackBufferWidth;
        set => GraphicsManager.PreferredBackBufferWidth = value;
    }

    public static int PreferredBackBufferHeight {
        get => GraphicsManager.PreferredBackBufferHeight;
        set => GraphicsManager.PreferredBackBufferHeight = value;
    }

    public static int MonitorWidth => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;

    public static int MonitorHeight => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

    public static SurfaceFormat BackBufferFormat =>
        GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferFormat;

    public static SurfaceFormat PreferredBackBufferFormat {
        get => GraphicsManager.PreferredBackBufferFormat;
        set => GraphicsManager.PreferredBackBufferFormat = value;
    }

    public static bool SynchronizeWithVerticalRetrace {
        get => GraphicsManager.SynchronizeWithVerticalRetrace;
        set => GraphicsManager.SynchronizeWithVerticalRetrace = value;
    }

    // defaults to Depth24Stencil8
    public static DepthFormat PreferredDepthStencilFormat {
        get => GraphicsManager.PreferredDepthStencilFormat;
        set => GraphicsManager.PreferredDepthStencilFormat = value;
    }

    public static bool IsFullscreen {
        get => GraphicsManager.IsFullScreen;
        set => GraphicsManager.IsFullScreen = value;
    }

    public static DisplayOrientation SupportedOrientations {
        get => GraphicsManager.SupportedOrientations;
        set => GraphicsManager.SupportedOrientations = value;
    }

    public static void ApplyChanges() => GraphicsManager.ApplyChanges();

    public static void SetSize(int width, int height) {
        PreferredBackBufferWidth = width;
        PreferredBackBufferHeight = height;
        ApplyChanges();
    }
}