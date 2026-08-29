namespace Wendlewind.Utils.Timers;

/// <summary>
/// private class hiding the implementation of ITimer
/// </summary>
class Timer : ITimer {
    public object? Context { get; set; }

    private float _timeInSeconds;
    private bool _repeats;
    private bool _isDone;
    private float _elapsedTime;
    private Action<ITimer>? _onTime;


    public void Stop() {
        _isDone = true;
    }

    public void Reset() {
        _elapsedTime = 0f;
    }

    public T? GetContext<T>() {
        return (T?) Context;
    }

    internal bool Tick(float deltaTime) {
        // if stop was called before the tick then isDone will be true and we should not tick again no matter what
        if (!_isDone && _elapsedTime > _timeInSeconds) {
            _elapsedTime -= _timeInSeconds;
            _onTime?.Invoke(this);

            if (!_isDone && !_repeats)
                _isDone = true;
        }

        _elapsedTime += deltaTime;

        return _isDone;
    }

    internal void Initialize(float timeInSeconds, bool repeats, object context, Action<ITimer>? onTime) {
        _timeInSeconds = timeInSeconds;
        _repeats = repeats;
        Context = context;
        _onTime = onTime;
    }

    /// <summary>
    /// nulls out the object references so the GC can pick them up if needed
    /// </summary>
    internal void Unload() {
        Context = null;
        _onTime = null;
    }
}