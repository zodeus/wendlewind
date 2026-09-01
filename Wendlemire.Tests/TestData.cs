using Wendlemire.Definitions.Loader;
using Wendlemire.Sim.Entities.Pawns;

namespace Wendlemire.Tests;

internal static class TestData
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        lock (LoadLock)
        {
            if (_loaded)
            {
                return;
            }

            foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "Wendlemire*.dll"))
            {
                var name = System.Reflection.AssemblyName.GetAssemblyName(path);
                if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != name.Name))
                {
                    System.Reflection.Assembly.Load(name);
                }
            }

            _ = typeof(PawnDef);
            if (Wendlemire.Utils.GenTypes.GetTypeInAnyAssembly("PawnDef") == null)
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

    public static string FindContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var definitions = Path.Combine(dir.FullName, "Content", "Data", "Definitions");
            if (Directory.Exists(definitions))
            {
                return dir.FullName;
            }

            var client = Path.Combine(dir.FullName, "Wendlemire", "Content", "Data", "Definitions");
            if (Directory.Exists(client))
            {
                return Path.Combine(dir.FullName, "Wendlemire");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Content/Data/Definitions for DataLoader.");
    }
}
