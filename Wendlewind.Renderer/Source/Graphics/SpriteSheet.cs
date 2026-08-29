namespace Wendlewind.Graphics;

public class SpriteSheet : IDisposable {
    public readonly int HorizontalCount;
    public readonly int VerticalCount;

    public Sprite[,] Sprites { get; }

    public SpriteSheet(Texture2D sheet, int spriteWidth, int spriteHeight) {
        if (sheet.Width % spriteWidth != 0) {
            Console.WriteLine("WARNING: Spritesheet size is off by " + sheet.Width % spriteWidth + " horizontally.");
        }

        if (spriteHeight % spriteHeight != 0) {
            Console.WriteLine("WARNING: Spritesheet size is off by " + sheet.Height % spriteHeight + " vertically.");
        }

        HorizontalCount = sheet.Width / spriteWidth;
        VerticalCount = sheet.Height / spriteHeight;

        Sprites = new Sprite[HorizontalCount, VerticalCount];
        for (int x = 0; x < HorizontalCount; x++) {
            for (int y = 0; y < VerticalCount; y++) {
                Sprites[x, y] = GetRegion(sheet, x * spriteWidth, y * spriteHeight, spriteWidth, spriteHeight);
            }
        }
    }

    public Sprite Random() {
        return Sprites[Rng.Current.Next(0, HorizontalCount - 1), Rng.Current.Next(0, VerticalCount - 1)];
    }

    private Sprite GetRegion(Texture2D texture, int x, int y, int width, int height) {
        return new Sprite(texture, x, y, width, height);
    }

    public void Dispose() {
        throw new NotImplementedException();
    }
}