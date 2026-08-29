namespace Wendlewind.Debug;

public class FrameCounter {
    public long TotalFrames { get; private set; }
    public float TotalSeconds { get; private set; }
    public float AverageFramesPerSecond { get; private set; }
    public float CurrentFramesPerSecond { get; private set; }
    public int CurrentFrameTime { get; private set; }

    public int AverageFrameTime { get; private set; }

    public const int MaximumSamples = 100;
    public const int MaximumFrameTimeSamples = 200;

    private readonly Queue<float> _sampleBuffer = new();
    private readonly Queue<int> _sampleBufferFrameTime = new();

    public bool Update(float deltaTime) {
        CurrentFramesPerSecond = 1.0f / deltaTime;

        _sampleBuffer.Enqueue(CurrentFramesPerSecond);

        CurrentFrameTime = (int) (deltaTime * 1000);
        _sampleBufferFrameTime.Enqueue(CurrentFrameTime);

        if (_sampleBuffer.Count > MaximumSamples) {
            _sampleBuffer.Dequeue();
            AverageFramesPerSecond = _sampleBuffer.Average(i => i);
        }
        else {
            AverageFramesPerSecond = CurrentFramesPerSecond;
        }

        if (_sampleBufferFrameTime.Count > MaximumFrameTimeSamples) {
            _sampleBufferFrameTime.Dequeue();
            AverageFrameTime = _sampleBufferFrameTime.Max(i => i);
        }
        else {
            AverageFrameTime = CurrentFrameTime;
        }

        TotalFrames++;
        TotalSeconds += deltaTime;
        return true;
    }
}