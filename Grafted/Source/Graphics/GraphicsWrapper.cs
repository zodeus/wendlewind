using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Graphics;

/// <summary>
/// wrapper class that holds in instance of a Batcher and helpers so that it can be passed around and draw anything.
/// </summary>
public class GraphicsWrapper {
    /// <summary>
    /// All 2D rendering is done through this Batcher instance
    /// </summary>
    public SpriteBatch Batcher;

    /// <summary>
    /// A sprite used to draw rectangles, lines, circles, etc. 
    /// Will be generated at startup, but you can replace this with a sprite from your atlas to reduce texture swaps.
    /// Should be a 1x1 white pixel
    /// </summary>
    public Sprite PixelTexture;

    public GraphicsWrapper() {
        Batcher = new SpriteBatch(Core.GraphicsDevice);
        //todo this should reference something better
        PixelTexture = new Sprite(Core.Content.Load<Texture2D>("pixel.png"), 0, 0, 1, 1);
    }

    /// <summary>
    /// helper method that generates a single color texture of the given dimensions
    /// </summary>
    /// <returns>The single color texture.</returns>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="color">Color.</param>
    public static Texture2D CreateSingleColorTexture(int width, int height, Color color) {
        Texture2D texture = new(Core.GraphicsDevice, width, height);
        Color[] data = new Color[width * height];
        for (var i = 0; i < data.Length; i++)
            data[i] = color;

        texture.SetData(data);
        return texture;
    }


    public void Unload() {
        if (PixelTexture != null) {
            PixelTexture.Texture2D?.Dispose();
        }

        PixelTexture = null!;

        Batcher.Dispose();
        Batcher = null!;
    }
}