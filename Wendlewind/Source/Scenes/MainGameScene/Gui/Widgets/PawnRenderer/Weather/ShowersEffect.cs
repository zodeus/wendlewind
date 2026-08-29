namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Light rain showers weather effect.
/// </summary>
public class ShowersEffect : BaseWeatherEffect
{
    public override WeatherType WeatherType => WeatherType.Showers;
    public override int PrePopulateCount => 120;
    public override float SpawnRate => 100f;
    
    private readonly List<Color> _colors = new()
    {
        new Color(150, 170, 200),
    };
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;

        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Velocity = new Vector2(
                16f + (float)Random.NextDouble() * 24f,
                240f + (float)Random.NextDouble() * 80f
            ),
            Size = 0.8f + (float)Random.NextDouble() * 0.7f,
            Opacity = 0.3f + (float)Random.NextDouble() * 0.3f
        };

        Particles.Add(particle);
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // Light blue-gray rain
            var color = _colors[Random.Next(_colors.Count)] * particle.Opacity;
            
            // Rain drops are elongated vertically with slight angle
            var rainRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                Math.Max(1, (int)scaledSize),
                Math.Max(1, (int)(scaledSize * 4f))
            );
            
            spriteBatch.Draw(pixel, rainRect, pixel.SourceRect, color);
        }
    }
}


