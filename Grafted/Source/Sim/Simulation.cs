using Grafted.Sim.Gui;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grafted.Sim;

public class Simulation {
    public SimulationMessages Messages = new();
    public IdProvider IdProvider = new();
    public World World = null!;
    private BaseGui? _gui = null;
    public int Ticks => World.Time.Ticks;

    public BaseGui? Gui {
        get => _gui;
        set {
            if (_gui != null) {
                Core.Instance.Window.TextInput -= OnWindowOnTextInput;
            }

            _gui = value;
            Core.Instance.Window.TextInput += OnWindowOnTextInput;
        }
    }

    private void OnWindowOnTextInput(object? _, TextInputEventArgs args) {
        _gui!.Desktop.OnChar(args.Character);
    }

    public bool IsPaused => Core.PauseCoroutines;

    public float GameSpeed = .2f;

    public void Update(float deltaTime) {
        HandleInput();
        Gui!.HandleInput();
        Gui.Update(deltaTime);
    }

    public void FixedUpdate() {
        //todo this is super hacky, need to move combat events out of async co-routines, threads are contending 
        if (Core.Sim.Gui is not CombatGui) {
            Core.Sim.World.Time.ProgressTime(1);
        }
    }

    private void HandleInput() {
        if (Input.IsKeyPressed(Keys.Space)) {
            Core.Sim.TogglePause();
        }

        if (Input.IsKeyPressed(Keys.D0)) {
            Core.Sim.GameSpeed = 0;
        }

        if (Input.IsKeyPressed(Keys.D1)) {
            Core.Sim.GameSpeed = .5f;
        }

        if (Input.IsKeyPressed(Keys.D2)) {
            Core.Sim.GameSpeed = .25f;
        }

        if (Input.IsKeyPressed(Keys.D3)) {
            Core.Sim.GameSpeed = .12f;
        }

        if (Input.IsKeyPressed(Keys.D4)) {
            Core.Sim.GameSpeed = .06f;
        }

        if (Input.IsKeyPressed(Keys.F2)) {
            ((GameScene) Core.Scene.ActiveScene!).QuickPlay();
        }
    }

    public void TogglePause() {
        Core.PauseCoroutines = !Core.PauseCoroutines;
    }

    public void Draw(SpriteBatch spriteBatch, float deltaTime) {
        Gui.Render(spriteBatch, deltaTime);
    }

    public void Save(string filePath) {
        Log.Info("Saving Game to " + filePath);
    }

    public void Load(string filePath) {
        Log.Info("Loading from " + filePath);
    }
}