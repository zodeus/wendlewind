namespace Grafted.Sim.Combat;

public class CombatRecord : IExposable {
    public event Action<CombatLogMessage>? LogMessageAddedAction;

    public void ExposeData() { }

    public void LogMessage(CombatLogMessage message) {
        LogMessageAddedAction?.Invoke(message);
    }
}

public class CombatLogMessage : IExposable {
    public string Text;

    public void ExposeData() { }
}