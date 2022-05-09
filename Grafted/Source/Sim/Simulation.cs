using Grafted.Debug;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Gui;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grafted.Sim;

public class CombatSettings {
    public float Speed = .2f;
    private bool _isPaused = false;

    public bool IsPaused {
        get => _isPaused;
        set {
            _isPaused = value;
            Core.PauseCoroutines = _isPaused;
        }
    }

    public void TogglePause() {
        IsPaused = !IsPaused;
    }
}

public class Simulation {
    public SimulationMessages Messages = new();
    public IdProvider IdProvider = new();
    public World World = null!;
    private BaseGui? _gui = null;
    public CombatSettings CombatSettings = new();
    public OminousMessageSpawner OminousMessageSpawner = new();
    public bool IsPaused = false;

    public int Ticks => World.Time.Ticks;
    public bool IsCombatPaused => CombatSettings.IsPaused;

    public BaseGui? Gui {
        get => _gui;
        set {
            if (_gui != null) {
                Core.Instance.Window.TextInput -= OnWindowOnTextInput;
                if (value != null) {
                    _gui.TransferScreenMessage(value);
                }
            }

            _gui = value;
            Core.Instance.Window.TextInput += OnWindowOnTextInput;
        }
    }

    private void OnWindowOnTextInput(object? _, TextInputEventArgs args) {
        _gui!.Desktop.OnChar(args.Character);
    }

    public void Update(float deltaTime) {
        HandleInput();
        Gui!.HandleInput();
        Gui.Update(deltaTime);
    }

    public void FixedUpdate() {
        if (IsPaused) {
            return;
        }

        if ((World.ActiveCombat != null && World.ActiveCombat.State != CombatState.CombatFinished && Core.Sim.CombatSettings.IsPaused)) {
            return;
        }

        if (DebugSettings.FastLoop != null) {
            World.ProgressTime(DebugSettings.FastLoop.Value);
        }
        else {
            World.ProgressTime(1);
        }
    }

    private void HandleInput() {
        if (Input.IsKeyPressed(Keys.Space)) {
            CombatSettings.TogglePause();
        }

        if (Input.IsKeyPressed(Keys.D0)) {
            CombatSettings.Speed = 0;
        }

        if (Input.IsKeyPressed(Keys.D1)) {
            CombatSettings.Speed = .5f;
        }

        if (Input.IsKeyPressed(Keys.D2)) {
            CombatSettings.Speed = .25f;
        }

        if (Input.IsKeyPressed(Keys.D3)) {
            CombatSettings.Speed = .12f;
        }

        if (Input.IsKeyPressed(Keys.D4)) {
            CombatSettings.Speed = .06f;
        }

        if (Input.IsKeyPressed(Keys.F2)) {
            if (Input.IsKeyDown(Keys.LeftControl)) {
                ((GameScene) Core.Scene.ActiveScene!).PlayIntro();
            }
            else {
                ((GameScene) Core.Scene.ActiveScene!).QuickPlay();
            }
        }

        if (Input.IsKeyPressed(Keys.F5)) {
            DebugSettings.FastLoop = null;
        }

        if (Input.IsKeyPressed(Keys.F6)) {
            if (DebugSettings.FastLoop != null) {
                DebugSettings.FastLoop -= SimTime.SecondsInMinute;
                if (DebugSettings.FastLoop < 0) {
                    DebugSettings.FastLoop = 0;
                }
            }
        }

        if (Input.IsKeyPressed(Keys.F7)) {
            if (DebugSettings.FastLoop == null) {
                DebugSettings.FastLoop = 0;
            }

            DebugSettings.FastLoop += SimTime.SecondsInMinute;
        }

        if (Input.IsKeyPressed(Keys.F8)) {
            DebugSettings.FastLoop = 0;
        }
    }

    public void TogglePause() {
        IsPaused = !IsPaused;
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

    public void ActivateCombatEvent(CombatEvent combat) {
        World.ActiveCombat = combat;
        Gui = new CombatGui(combat);
    }
}