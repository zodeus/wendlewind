namespace Wendlemire.Editor;

internal static class EditorPaths
{
    public static string FindContentFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(new[] { dir.FullName, "Wendlemire", "Content" }.Concat(relativeSegments).ToArray());
            var parent = Path.GetDirectoryName(candidate);
            if (Directory.Exists(candidate) || (parent != null && Directory.Exists(parent)))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(new[] { AppContext.BaseDirectory, "Content" }.Concat(relativeSegments).ToArray());
    }
}
