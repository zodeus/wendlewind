namespace Grafted.Sim;

public class CombatSpeed {
    public const float Slow = .5f;
    public const float Normal = .25f;
    public const float Fast = .10f;
}

public class CombatSettings {
    public float Speed = CombatSpeed.Normal;
    private bool _isPaused = false;

    public bool IsPaused {
        get => _isPaused;
        set {
            _isPaused = value;
            Core.PauseCoroutines = _isPaused;
        }
    }

    public void TogglePause() {
        IsPaused = !IsPaused;
    }
}