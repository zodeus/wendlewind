namespace Wendlewind.Scenes.Components;

public abstract class Scene {

    protected Scene() {
    }

    public virtual void Update(float deltaTime) { }

    public virtual void FixedUpdate() { }
    public virtual void Draw(float deltaTime) { }

    #region Scene lifecycle

    protected virtual void OnStart() { }

    internal void Begin() {
        OnStart();
    }

    public virtual void End() {
    }

    #endregion
}