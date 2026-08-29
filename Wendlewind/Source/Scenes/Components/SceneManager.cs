namespace Wendlewind.Scenes.Components;

public class SceneManager
{
    private readonly Dictionary<Type, Scene> _scenes = new();

    public Scene? ActiveScene { get; private set; }

    public void RegisterScene<T>(T scene) where T : Scene
    {
        _scenes.Add(typeof(T), scene);
    }

    public void Load<T>() where T : Scene
    {
        ActiveScene?.End();
        ActiveScene = _scenes[typeof(T)];
        ActiveScene.Begin();
    }

    public void Update(float deltaTime)
    {
        ActiveScene?.Update(deltaTime);
    }

    public void FixedUpdate()
    {
        ActiveScene?.FixedUpdate();
    }

    public void Draw(float deltaTime)
    {
        ActiveScene?.Draw(deltaTime);
    }
}