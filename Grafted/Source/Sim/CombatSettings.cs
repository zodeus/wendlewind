namespace Grafted.Sim;

public class CombatSettings {
    public float Speed = .2f;
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