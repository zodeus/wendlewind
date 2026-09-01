namespace Wendlemire.Sim.Persistence;

/// <summary>
/// Creates <see cref="IExposable"/> instances during Scribe load.
/// Simulation supplies a run-scoped implementation; defs stay on Activator.
/// </summary>
public interface IScribeObjectFactory
{
    object Create(Type type, object[] ctorArgs);
}
