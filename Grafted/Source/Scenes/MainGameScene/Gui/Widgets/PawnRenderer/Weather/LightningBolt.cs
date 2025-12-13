namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Represents a lightning bolt flash with branching paths.
/// </summary>
public class LightningBolt
{
    public List<Vector2> Points = new();
    public float Lifetime;
    public float MaxLifetime;
    public float Brightness;
    public float Thickness;
    public List<List<Vector2>> Branches = new();
    
    public bool IsExpired => Lifetime <= 0;
    public float Alpha => Math.Clamp(Lifetime / (MaxLifetime * 0.3f), 0f, 1f);
    
    public void Update(float deltaTime)
    {
        Lifetime -= deltaTime;
        Brightness = Alpha;
    }
}



