using System.Collections;
using Wendlemire.Definitions.Loader;

namespace Wendlemire.Definitions;

public static class DefRepository<T> where T : Def {
    private static readonly List<T> DefsList = new();

    private static readonly Dictionary<string, T> DefsByMoniker = new();

    public static IReadOnlyList<T> Defs => DefsList;

    public static void Add(IEnumerable<T> defs) {
        foreach (T def in defs) {
            Add(def);
        }
    }

    public static void AddFiltered(IEnumerable<Def> defs) {
        foreach (T def in defs.OfType<T>()) {
            Add(def);
        }
    }

    public static void Add(T def) {
        DefsList.Add(def);
        if (DefsByMoniker.ContainsKey(def.Moniker)) {
            Log.Error($"Failed to register Def {def}, key already exists in DefDatabase::DefsByMoniker");
            return;
        }

        DefsByMoniker.Add(def.Moniker, def);

        if (DefsList.Count > ushort.MaxValue) {
            Log.Error(string.Concat("Exceeded maximum number of defs ", typeof(T), "; over ", ushort.MaxValue));
        }

        def.Index = (ushort) (DefsList.Count - 1);
    }

    private static void Remove(T def) {
        DefsByMoniker.Remove(def.Moniker);
        DefsList.Remove(def);
        SetIndices();
    }

    private static void SetIndices() {
        for (int i = 0; i < DefsList.Count; i++) {
            DefsList[i].Index = (ushort) i;
        }
    }

    public static T? GetByMoniker(string moniker, bool raiseError = true) {
        DefsByMoniker.TryGetValue(moniker, out T? def);
        if (raiseError && def == null) {
            throw new ArgumentException($"Def not found ${typeof(T)}: {moniker}");
        }

        return def;
    }

    public static T RandomElement(Random rng) {
        return DefsList.RandomElement(rng);
    }

    /// <summary>
    /// Used By <see cref="DataLoader.Load"/>
    /// </summary>
    [UsedImplicitly]
    public static void ResolveDefDependencies() {
        foreach (T def in Defs) {
            def.ResolveDependencies();
        }
    }
}

public static class DefRepository {
    public static Def? GetDef(Type defType, string defName, bool raiseError = true) {
        return (Def?) GenericHelpers.InvokeStaticMethodOnGenericType(typeof(DefRepository<>), defType, "GetByMoniker", defName, raiseError);
    }

    public static IEnumerable<Def> GetAllDefsInDatabaseForDef(Type defType) {
        return ((IEnumerable) GenericHelpers.GetStaticPropertyOnGenericType(typeof(DefRepository<>), defType, "Defs")).Cast<Def>();
    }

    public static IEnumerable<Type> AllDefTypesWithDatabases() {
        foreach (Type item in typeof(Def).Subclasses()) {
            if (!item.IsAbstract && !(item == typeof(Def))) {
                bool flag = false;
                Type? baseType = item.BaseType;
                while (baseType != null && baseType != typeof(Def)) {
                    if (!baseType.IsAbstract) {
                        flag = true;
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                if (!flag) {
                    yield return item;
                }
            }
        }
    }
}