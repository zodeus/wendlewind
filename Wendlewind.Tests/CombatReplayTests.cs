using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Definitions.Loader;
using Wendlewind.Sim;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Xunit;

namespace Wendlewind.Tests;

public class CombatReplayTests
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    public CombatReplayTests()
    {
        lock (LoadLock)
        {
            if (_loaded)
            {
                return;
            }

            foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "Wendlewind*.dll"))
            {
                var name = System.Reflection.AssemblyName.GetAssemblyName(path);
                if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != name.Name))
                {
                    System.Reflection.Assembly.Load(name);
                }
            }

            _ = typeof(PawnDef);
            if (Wendlewind.Utils.GenTypes.GetTypeInAnyAssembly("PawnDef") == null)
            {
                throw new InvalidOperationException(
                    "PawnDef was not visible to GenTypes. Assemblies=" +
                    string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name)));
            }

            Directory.SetCurrentDirectory(FindContentRoot());
            DataLoader.Load();
            _loaded = true;
        }
    }

    private static string FindContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var definitions = Path.Combine(dir.FullName, "Content", "Data", "Definitions");
            if (Directory.Exists(definitions))
            {
                return dir.FullName;
            }

            var client = Path.Combine(dir.FullName, "Wendlewind", "Content", "Data", "Definitions");
            if (Directory.Exists(client))
            {
                return Path.Combine(dir.FullName, "Wendlewind");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Content/Data/Definitions for DataLoader.");
    }

    [Fact]
    public void SameSeedAgrees()
    {
        CombatReplay.AssertDeterministic();
    }

    [Fact]
    public void SaveLoadRoundTripKeepsRun()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wendlewind-saveload-{Guid.NewGuid():N}.xml");
        try
        {
            var first = CombatReplay.Run();

            using var root = SimServices.BuildRoot();
            using var scope = root.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            context.Initialize(CombatReplay.DefaultRunSeed);
            context.Save(path);

            using var loadScope = root.CreateScope();
            var loaded = loadScope.ServiceProvider.GetRequiredService<GameContext>();
            loaded.Load(path);

            Assert.Equal(CombatReplay.DefaultRunSeed, loaded.RunSeed);
            Assert.NotNull(loaded.PlayerPawn);
            Assert.False(loaded.PlayerPawn.IsDead);

            var zone = loaded.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
            loaded.EnterZone(zone.ZoneDef);
            loaded.CurrentZone!.NextEncounter();
            int guard = 0;
            while (loaded.CurrentZone.ActiveEncounter!.State == EncounterState.InProgress &&
                   guard < CombatReplay.MaxTicks)
            {
                loaded.Tick();
                guard++;
            }

            Assert.True(guard > 0);
            Assert.NotEqual(EncounterState.InProgress, loaded.CurrentZone.ActiveEncounter.State);
            Assert.Equal(first.ZoneMoniker, zone.ZoneDef.Moniker);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
