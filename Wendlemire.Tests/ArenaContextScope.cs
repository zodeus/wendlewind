using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Sim;

namespace Wendlemire.Tests;

internal sealed class ArenaContextScope : IDisposable
{
    private readonly ServiceProvider _root = SimServices.BuildRoot();
    private readonly IServiceScope _scope;

    public GameContext Context { get; }

    public ArenaContextScope(string playerId = "tester", string playerName = "Tester", int runSeed = 99)
    {
        _scope = _root.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<GameContext>();
        Context.InitializeArena(playerId, playerName, runSeed);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}
