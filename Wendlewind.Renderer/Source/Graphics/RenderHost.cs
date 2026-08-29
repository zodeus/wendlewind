using Wendlewind.Assets;

namespace Wendlewind.Graphics;

public static class RenderHost
{
    public static GraphicsDevice GraphicsDevice { get; set; } = null!;
    public static ContentManager Content { get; set; } = null!;
    public static Sprite PixelTexture { get; set; } = null!;
}
