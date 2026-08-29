using System.IO;
using Wendlewind.Graphics;

namespace Wendlewind.Assets.SpriteAtlases.Loader;

/// <summary>
/// temporary class used when loading a SpriteAtlas and by the sprite atlas editor
/// </summary>
internal class SpriteAtlasData {
    public readonly List<string> Names = new();
    public readonly List<Rectangle> SourceRects = new();
    public readonly List<Vector2> Origins = new();

    public readonly List<string> AnimationNames = new();
    public readonly List<int> AnimationFps = new();
    public readonly List<List<int>> AnimationFrames = new();

    public SpriteAtlas AsSpriteAtlas(Texture2D? texture) {
        SpriteAtlas atlas = new(Names.ToArray(), AnimationNames.ToArray());
        for (int i = 0; i < atlas.Sprites.Length; i++) {
            atlas.Sprites[i] = new Sprite(texture!, SourceRects[i], Origins[i]);
        }

        for (int i = 0; i < atlas.SpriteAnimations.Length; i++) {
            Sprite[] sprites = new Sprite[AnimationFrames[i].Count];
            for (int j = 0; j < sprites.Length; j++) {
                sprites[j] = atlas.Sprites[AnimationFrames[i][j]];
            }

            atlas.SpriteAnimations[i] = new SpriteAnimation(sprites, AnimationFps[i]);
        }

        return atlas;
    }

    public void Clear() {
        Names.Clear();
        SourceRects.Clear();
        Origins.Clear();

        AnimationNames.Clear();
        AnimationFps.Clear();
        AnimationFrames.Clear();
    }

    public void SaveToFile(string filename) {
        if (File.Exists(filename))
            File.Delete(filename);

        using StreamWriter writer = new(filename);
        for (int i = 0; i < Names.Count; i++) {
            writer.WriteLine(Names[i]);

            Rectangle rect = SourceRects[i];
            writer.WriteLine("\t{0},{1},{2},{3}", rect.X, rect.Y, rect.Width, rect.Height);
            writer.WriteLine("\t{0},{1}", Origins[i].X, Origins[i].Y);
        }

        if (AnimationNames.Count <= 0) return;

        writer.WriteLine();
        for (int i = 0; i < AnimationNames.Count; i++) {
            writer.WriteLine(AnimationNames[i]);
            writer.WriteLine("\t{0}", AnimationFps[i]);
            writer.WriteLine("\t{0}", string.Join(",", AnimationFrames[i]));
        }
    }
}