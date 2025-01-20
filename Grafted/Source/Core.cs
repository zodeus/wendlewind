using System.Collections;
using System.IO;
using AssetManagementBase;
using FontStashSharp;
using FontStashSharp.RichText;
using Grafted.Assets;
using Grafted.Coroutines;
using Grafted.Definitions.Loader;
using Grafted.Graphics;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene;
using Grafted.Utils.Timers;
using Myra;

namespace Grafted;

public class Core : Game
{
    public const int TicksPerSecond = 60;
    public new static GraphicsDevice GraphicsDevice { get; private set; } = null!;
    public new static ContentManager Content { get; private set; } = null!;

    public static Core Instance { get; private set; } = null!;

    public static Emitter<CoreEvent> Emitter { get; private set; } = null!;

    public static GameContext Context { get; set; } = null!;
    public static SceneManager Scene { get; } = new();
    public static GraphicsWrapper Graphics { get; private set; } = null!;

    public static Random Random { get; private set; } = null!;
    public static bool PauseInBackground { get; set; } = false;
    public static bool PauseCoroutines { get; set; } = false;

    //todo move this somewhere better
    public static FrameCounter FrameCounter = new();

    private readonly TimerManager _timerManager = new();
    private readonly CoroutineManager _coroutineManager = new();

    /// <summary>
    /// used to coalesce GraphicsDeviceReset events
    /// </summary>
    private ITimer? _graphicsDeviceChangeTimer;

    /// <summary>
    /// default SamplerState used by Materials. Note that this must be set at launch! Changing it after that time will result in only
    /// Materials created after it was set having the new SamplerState
    /// </summary>
    public static SamplerState DefaultSamplerState = new() { Filter = TextureFilter.Point };

    private readonly LogicTimer _fixedUpdateTimer;

    public Core(bool isFullScreen = false)
    {
        InactiveSleepTime = TimeSpan.Zero;
        Instance = this;
        Emitter = new Emitter<CoreEvent>(new CoreEventComparer());
        GraphicsDeviceManager graphics = new(this)
        {
            IsFullScreen = isFullScreen,
            SynchronizeWithVerticalRetrace = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
        };
        graphics.DeviceReset += OnGraphicsDeviceReset;
        Screen.Initialize(graphics);
        if (isFullScreen)
        {
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }
        else
        {
            //Screen.SetSize(2560, 1440);
            Screen.SetSize(3820, 2040);
        }

        Window.Title = "Grafted";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnGraphicsDeviceReset;

        IsMouseVisible = true;
        IsFixedTimeStep = false;
        _fixedUpdateTimer = new LogicTimer(FixedUpdate);
    }

    protected override void Initialize()
    {
        base.Initialize();
        GraphicsDevice = base.GraphicsDevice;

        Random = new Random();

        const string contentDirectory = "Content";
        if (!Directory.Exists(contentDirectory))
        {
            throw new Exception($"Content folder does not exists {contentDirectory}");
        }

        Content = new ContentManager(GraphicsDevice, contentDirectory);
        Graphics = new GraphicsWrapper();

        //Scene.Load<LoadingScene>();
        DataLoader.Load();

        BaseContent.Initialize();
        BaseContent.Fonts.Load();


        #region GUI

        MyraEnvironment.Game = this;
        MyraEnvironment.DefaultAssetManager = AssetManager.CreateFileAssetManager(Path.Combine(contentDirectory, "UI"));
        Stylesheet.Current = MyraEnvironment.DefaultAssetManager.LoadStylesheet("milgreth_ui_skin.xmms");

        RichTextDefaults.FontResolver = p =>
        {
            // Parse font name and size
            var args = p.Split(',');
            var fontName = args[0].Trim();
            var fontSize = int.Parse(args[1].Trim());
            // _fontCache is field of type Dictionary<string, FontSystem>
            // It is used to cache fonts
            FontSystem fontSystem = BaseContent.Fonts.Default.Normal.FontSystem;
            // Return the required font
            return fontSystem.GetFont(fontSize);
        };

        #endregion

        Scene.RegisterScene(new MainMenuScene());
        Scene.RegisterScene(new GameScene());
        Scene.RegisterScene(new GameOverScene());
        ChangeScene<MainMenuScene>();

        _fixedUpdateTimer.Start();


        //SoundEffect sound = MonoSoundManager.GetEffect("Content/Audio/winds.mp3");
        //sound.Play();
    }

    public static void ChangeScene<T>() where T : Scene => Scene.Load<T>();

    public new static void Exit() => ((Game)Instance).Exit();

    protected override void Update(GameTime gameTime)
    {
        if (PauseInBackground && !IsActive)
        {
            SuppressDraw();
            return;
        }

        // int frameTime = gameTime.ElapsedGameTime.Milliseconds;
        // if (frameTime > 10)
        // {
        //     Log.Error($"Slow Frame T={frameTime}");
        // }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        FrameCounter.Update(deltaTime);
        Input.Update();
        Scene.Update(deltaTime);
        _fixedUpdateTimer.Update();
        _timerManager.Update(deltaTime);
        if (PauseCoroutines == false)
        {
            _coroutineManager.Update(deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Scene.FixedUpdate();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (PauseInBackground && !IsActive) return;
        GraphicsDevice.Clear(Color.Black);
        Scene.Draw((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    private void OnGraphicsDeviceReset(object? sender, EventArgs e)
    {
        // coalesce reset events
        if (_graphicsDeviceChangeTimer != null)
        {
            _graphicsDeviceChangeTimer.Reset();
        }
        else
        {
            _graphicsDeviceChangeTimer = Schedule(0.05f, false, this, t =>
            {
                (t.Context as Core)!._graphicsDeviceChangeTimer = null;
                Emitter.Emit(CoreEvent.GraphicsDeviceReset);
            });
        }
    }

    #region Systems access

    public static ICoroutine StartCoroutine(IEnumerator enumerator)
    {
        return Instance._coroutineManager.StartCoroutine(enumerator);
    }

    public static void ClearCoroutines()
    {
        Instance._coroutineManager.Clear();
    }

    /// <summary>
    /// schedules a one-time or repeating timer that will call the passed in Action
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds.</param>
    /// <param name="repeats">If set to <c>true</c> repeats.</param>
    /// <param name="context">Context.</param>
    /// <param name="onTime">On time.</param>
    public static ITimer Schedule(float timeInSeconds, bool repeats, object context, Action<ITimer>? onTime)
    {
        return Instance._timerManager.Schedule(timeInSeconds, repeats, context, onTime);
    }

    /// <summary>
    /// schedules a one-time timer that will call the passed in Action after timeInSeconds
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds.</param>
    /// <param name="context">Context.</param>
    /// <param name="onTime">On time.</param>
    public static ITimer Schedule(float timeInSeconds, object context, Action<ITimer>? onTime)
    {
        return Instance._timerManager.Schedule(timeInSeconds, false, context, onTime);
    }

    /// <summary>
    /// schedules a one-time or repeating timer that will call the passed in Action
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds.</param>
    /// <param name="repeats">If set to <c>true</c> repeats.</param>
    /// <param name="onTime">On time.</param>
    public static ITimer Schedule(float timeInSeconds, bool repeats, Action<ITimer>? onTime)
    {
        return Instance._timerManager.Schedule(timeInSeconds, repeats, null!, onTime);
    }

    /// <summary>
    /// schedules a one-time timer that will call the passed in Action after timeInSeconds
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds.</param>
    /// <param name="onTime">On time.</param>
    public static ITimer Schedule(float timeInSeconds, Action<ITimer>? onTime)
    {
        return Instance._timerManager.Schedule(timeInSeconds, false, null!, onTime);
    }

    #endregion
}