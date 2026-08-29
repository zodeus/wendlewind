using System.IO;

namespace Wendlewind.Utils;

public static class FileHelpers {
    public static IEnumerable<string?> LinesFromFile(string filePath) {
        using StreamReader reader = File.OpenText(filePath);
        string? line;
        while ((line = reader.ReadLine()) != null) {
            yield return line;
        }
    }
}