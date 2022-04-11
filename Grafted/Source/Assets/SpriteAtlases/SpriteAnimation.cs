using Grafted.Graphics;

namespace Grafted.Assets.SpriteAtlases;

public class SpriteAnimation {
    public readonly Sprite[] Sprites;
    public readonly float FrameRate;

    public SpriteAnimation(Sprite[] sprites, float frameRate) {
        Sprites = sprites;
        FrameRate = frameRate;
    }
}