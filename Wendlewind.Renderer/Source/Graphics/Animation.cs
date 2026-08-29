namespace Wendlewind.Graphics;

public class Animation<T> {
    private readonly T[] _keyFrames;
    private readonly float _frameDuration;

    public Animation(float frameDuration, T[] keyFrames) {
        _frameDuration = frameDuration;
        _keyFrames = keyFrames;
    }

    public T GetKeyFrame(float elapsedTime) {
        return _keyFrames[GetKeyFrameIndex(elapsedTime)];
    }

    private int GetKeyFrameIndex(float stateTime) {
        if (_keyFrames.Length == 1) return 0;
        return (int) (stateTime / _frameDuration) % _keyFrames.Length;
    }
}