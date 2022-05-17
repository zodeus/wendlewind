using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;
using Grafted.Sim.Zones;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grafted.Sim;

public class Simulation : IExposable {
    private BaseGui? _gui = null;

    public SimulationMessages Messages = new();
    public IdProvider IdProvider = new();
    public World World = null!;
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

        if ((World.CurrentZone.ZoneType == ZoneType.Adventure && World.CurrentZone.Adventure?.ActiveCombat != null && CombatSettings.IsPaused)) {
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

        if (Input.IsKeyPressed(Keys.S) && Input.IsKeyDown(Keys.LeftControl) && World.CurrentZone.ZoneType != ZoneType.Adventure) {
            Save("save.xml");
            Gui!.PushScreenMessage(new ScreenMessageData {
                Text = "Game Saved",
                Font = BaseContent.Fonts.Default.Large,
                Duration = 5,
                Color = Color.LimeGreen
            });
        }

        if (Input.IsKeyPressed(Keys.L) && Input.IsKeyDown(Keys.LeftControl)) {
            Load("save.xml");
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

        if (Input.IsKeyPressed(Keys.F2) && Input.IsKeyDown(Keys.LeftControl)) {
            ((GameScene) Core.Scene.ActiveScene!).QuickPlay();
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

    #region Persistence

    public void Save(string filePath) {
        //return;
        Log.Info("Saving Game to " + filePath);
        Scribe.Saver.InitSaving(filePath, "SaveData");
        Simulation sim = this;
        Scribe_Deep.Look(ref sim!, "Simulation");
        Scribe.Saver.FinalizeSaving();
    }

    public void Load(string filePath) {
        Scribe.Loader.InitLoading(filePath);
        if (!Scribe.EnterNode("Simulation")) {
            Log.Error("Could not find game XML node.");
            Scribe.ForceStop();
            return;
        }

        ExposeDataInternal();

        Scribe.Loader.FinalizeLoading();
        Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned);
    }


    public void ExposeData() {
        if (Scribe.State == ScribeState.LoadingObjects) {
            Log.Error("You must use Simulation.Load method to load simulation.");
            return;
        }

        ExposeDataInternal();
    }

    private void ExposeDataInternal() {
        Scribe_Deep.Look(ref World!, "World");
        Scribe_Deep.Look(ref IdProvider!, "IdProvider");
        Scribe_Deep.Look(ref Messages!, "Messages");
    }

    #endregion

    public void ChangeZone(ZoneDef zoneDef, bool progressTime = true) {
        if (progressTime) {
            if (zoneDef == Defs.Zones.VillageOfTheDamned) {
                //todo MovementMultiplier
                // progress time to return to beginning of zone
                World.ProgressTime(World.CurrentZone.DistanceTraveledThisRun * SimTime.MinutesToSeconds(SimTime.MinutesPerKm)); //roughly 10 minutes per km    
            }
            else {
                //todo distance from zones Core.Sim.World.ProgressTime(SimTime.HoursToSeconds(1));    
            }
        }

        World.CurrentZone?.Exit();
        World.CurrentZone?.Reset();
        if (World.PlayerPawn.IsDead) {
            return;
        }

        World.CurrentZone = World.Zones[zoneDef];
        World.PlayerPawn.Zone = World.CurrentZone;
        World.CurrentZone.Enter();
        Gui = World.CurrentZone.Gui;
        //Core.Sim.Gui = new TownGui(Core.Sim.World.Zones[Defs.Zones.VillageOfTheDamned].Town!);
        Messages.Push(new Message(
            $"\\c[{UiTextColor.TextColorPawn}]{World.PlayerPawn} \\c[{UiTextColor.TextColorDefault}]moved to zone \\c[{UiTextColor.TextColorZone}]{zoneDef.Label}"
        ));
        Save("save.xml");
        Log.Info("Autosaving");
    }
}