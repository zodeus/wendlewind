namespace Wendlemire.Sim.Persistence;

public class LoadedObjectDirectory
{
    private readonly Dictionary<string, IIdentityProvider> _allObjectsByLoadId = new Dictionary<string, IIdentityProvider>();

    public void Clear()
    {
        _allObjectsByLoadId.Clear();
    }

    public void RegisterLoaded(IIdentityProvider reffable)
    {
        // if (Prefs.DevMode) {
        var text = "[excepted]";
        try
        {
            text = reffable.GetUniqueId();
        }
        catch (Exception)
        {
            //ignored 
        }

        var text2 = $"[excepted: casting {reffable.GetType().Name} to string]";
        try
        {
            text2 = reffable.ToString()!;
        }
        catch (Exception)
        {
            //      ignored
        }

        if (_allObjectsByLoadId.TryGetValue(text, out var value))
        {
            var text3 = "";
            Log.Error(string.Concat("Cannot register ", reffable.GetType(), " ", text2, ", (id=", text, " in loaded object directory. Id already used by ", value.GetType(), " ",
                value.ToString(), ".", text3));
            return;
        }
        // }

        try
        {
            _allObjectsByLoadId.Add(reffable.GetUniqueId(), reffable);
        }
        catch (Exception ex5)
        {
            var text4 = "[excepted]";
            try
            {
                text4 = reffable.GetUniqueId();
            }
            catch (Exception)
            {
                // ignored
            }

            var text5 = "[excepted]";
            try
            {
                text5 = reffable.ToString()!;
            }
            catch (Exception)
            {
                // ignored
            }

            Log.Error(string.Concat("Exception registering ", reffable.GetType(), " ", text5, " in loaded object directory with unique load ID ", text4, ": ", ex5));
        }
    }

    public T? ObjectWithLoadId<T>(string loadId)
    {
        if (loadId.NullOrEmpty() || loadId == "null")
        {
            return default;
        }

        if (_allObjectsByLoadId.TryGetValue(loadId, out var value))
        {
            try
            {
                return (T)value;
            }
            catch (Exception ex)
            {
                Log.Error(string.Concat("Exception getting object with load id ", loadId, " of type ", typeof(T), ". What we loaded was ", value.ToString(), ". Exception:\n", ex));
                return default;
            }
        }

        Log.Warning(string.Concat("Could not resolve reference to object with loadID ", loadId, " of type ", typeof(T),
            ". Was it compressed away, destroyed, had no ID number, or not saved/loaded right? curParent=", Scribe.Loader.CurParent?.ToString(), " curPathRelToParent=",
            Scribe.Loader.CurPathRelToParent));
        return default;
    }
}