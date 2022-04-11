using System.Collections.Generic;
using System.IO;

namespace Grafted.Utils;

public static class FileHelpers {
    public static IEnumerable<string?> LinesFromFile(string filePath) {
        StreamReader text = Core.Content.Load<StreamReader>(filePath);
        using StreamReader reader = text;
        string? line;
        while ((line = reader.ReadLine()) != null) {
            yield return line;
        }
    }
}