namespace Wendlemire.Presentation;

public sealed class AnimatedTextureRegion : IImage
{
    private readonly TextureRegion[] _frames;
    private readonly float _frameRate;
    private readonly bool _pingPong;

    public AnimatedTextureRegion(Texture2D[] frames, float frameRate, bool pingPong)
    {
        if (frames == null || frames.Length == 0)
        {
            throw new ArgumentException("AnimatedTextureRegion requires at least one frame.", nameof(frames));
        }

        _frames = new TextureRegion[frames.Length];
        for (var i = 0; i < frames.Length; i++)
        {
            _frames[i] = new TextureRegion(frames[i]);
        }

        _frameRate = frameRate > 0 ? frameRate : 6;
        _pingPong = pingPong;
        Size = _frames[0].Size;
    }

    public Point Size { get; }

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        _frames[CurrentFrameIndex()].Draw(context, dest, color);
    }

    private int CurrentFrameIndex()
    {
        var count = _frames.Length;
        if (count == 1)
        {
            return 0;
        }

        var period = _pingPong ? (count - 1) * 2 : count;
        var frame = (int)(Core.TotalTime * _frameRate) % period;
        if (_pingPong && frame >= count)
        {
            return period - frame;
        }

        return frame;
    }
}
