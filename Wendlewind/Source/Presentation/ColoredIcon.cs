namespace Wendlewind.Presentation;

public sealed class ColoredIcon : IImage
{
    public ColoredIcon(IImage image, Color color)
    {
        Image = image;
        Color = color;
    }

    public IImage Image { get; }
    public Color Color { get; set; }
    public Point Size => Image.Size;

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        Image.Draw(context, dest, new Color(
            Color.R * color.R / 255,
            Color.G * color.G / 255,
            Color.B * color.B / 255,
            Color.A * color.A / 255));
    }
}
