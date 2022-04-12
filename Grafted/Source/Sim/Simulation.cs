using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.UI;
using Grafted.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grafted.Sim;

public class Simulation {
    public SimulationMessages Messages = new();
    public IdProvider IdProvider = new();
    public World World = null!;
    public SimulationGui? Gui { get; set; }
    public bool IsPaused => Core.PauseCoroutines;

    public float GameSpeed = .2f;

    public void Update(float deltaTime) {
        HandleInput();
        Gui!.HandleInput();
        Gui.Update(deltaTime);
    }

    public void FixedUpdate() { }

    private void HandleInput() {
        if (Input.IsKeyPressed(Keys.Space)) {
            Core.Sim.TogglePause();
        }
    }

    public void TogglePause() {
        Core.PauseCoroutines = !Core.PauseCoroutines;
    }

    public void Draw(SpriteBatch spriteBatch, float deltaTime) {
        Gui.Render(spriteBatch);
    }

    public void Save(string filePath) {
        Log.Info("Saving Game to " + filePath);
    }

    public void Load(string filePath) {
        Log.Info("Loading from " + filePath);
    }
}