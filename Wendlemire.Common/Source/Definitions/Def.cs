namespace Wendlemire.Definitions;

public class Def {
    public string Moniker = "undefined";
    public string Label = "undefined";
    public string Description = "";
    public ushort Index = ushort.MaxValue;

    public override string ToString() {
        return Moniker;
    }

    public virtual void Initialize() {
        Log.Debug($"Initializing: {Moniker}");
    }

    public virtual void ResolveDependencies() {
        Log.Debug($"ResolveDependencies: {Moniker}");
    }
}
