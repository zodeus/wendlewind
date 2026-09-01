namespace Wendlewind.NetCode;

public static class ServerData
{
    public const string DirectoryName = "data";

    public static string ResolveDirectory()
    {
        var fromEnv = Environment.GetEnvironmentVariable("WENDLEWIND_DATA");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        var baseDir = AppContext.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        if (File.Exists(Path.Combine(projectDir, "Wendlewind.Server.csproj")))
        {
            return Path.Combine(projectDir, DirectoryName);
        }

        return Path.Combine(baseDir, DirectoryName);
    }

    public static string EnsureDirectory()
    {
        var directory = ResolveDirectory();
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "players"));
        TryMigrateLegacyPool(directory);
        return directory;
    }

    public static string PoolPath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, "build-pool.json");
    }

    public static string CodesPath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, "activation-codes.json");
    }

    private static void TryMigrateLegacyPool(string dataDirectory)
    {
        var destination = PoolPath(dataDirectory);
        if (File.Exists(destination))
        {
            return;
        }

        var legacy = Path.Combine(AppContext.BaseDirectory, "build-pool.json");
        if (File.Exists(legacy))
        {
            File.Copy(legacy, destination);
        }
    }
}
