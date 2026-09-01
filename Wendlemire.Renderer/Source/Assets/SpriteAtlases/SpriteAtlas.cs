using Wendlemire.Graphics;

namespace Wendlemire.Assets.SpriteAtlases;

public class SpriteAtlas : IDisposable {
    public readonly string[] Names;
    private Sprite[]? _sprites;
    public Sprite[] Sprites => _sprites!;

    public readonly string[] AnimationNames;
    public readonly SpriteAnimation[] SpriteAnimations;

    public SpriteAtlas(string[] names, string[] animationNames) {
        Names = names;
        _sprites = new Sprite[names.Length];

        AnimationNames = animationNames;
        SpriteAnimations = new SpriteAnimation[animationNames.Length];
    }

    public Sprite GetSprite(string name) {
        int index = Array.IndexOf(Names, name);
        return Sprites[index];
    }

    public SpriteAnimation GetAnimation(string name) {
        int index = Array.IndexOf(AnimationNames, name);
        return SpriteAnimations[index];
    }

    void IDisposable.Dispose() {
        // all our Sprites use the same Texture so we only need to dispose one of them
        if (_sprites == null) return;

        _sprites[0].Texture2D?.Dispose();
        _sprites = null;
    }
}