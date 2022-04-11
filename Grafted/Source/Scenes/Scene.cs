using Grafted.UI;
using Microsoft.Xna.Framework;

namespace Grafted.Scenes;

public abstract class Scene {
    public Camera MainCamera;

    protected Scene() {
        MainCamera = new Camera();
    }

    public virtual void Update(float deltaTime) { }

    public virtual void FixedUpdate() { }
    public virtual void Draw(float deltaTime) { }

    #region Scene lifecycle

    protected virtual void OnStart() { }

    internal void Begin() {
        UpdateResolutionScaler();
        Core.Emitter.AddObserver(CoreEvent.GraphicsDeviceReset, OnGraphicsDeviceReset);
        OnStart();
    }

    public virtual void End() {
        Core.Emitter.RemoveObserver(CoreEvent.GraphicsDeviceReset, OnGraphicsDeviceReset);
    }

    #endregion

    #region Resolution

    protected virtual void OnGraphicsDeviceReset() => UpdateResolutionScaler();

    private void UpdateResolutionScaler() {
        //todo actually calculate the resolution scale...
        Input._resolutionScale = new Vector2(1, 1);
        Input._resolutionOffset = new Point(0, 0);
    }

    #endregion
}