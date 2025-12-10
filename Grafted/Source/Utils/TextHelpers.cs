using System.Collections;
using System.Text;

namespace Grafted.Utils;

public static class TextHelpers {
    public static bool NullOrEmpty(this string str) {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// Formats a count for display. Numbers >= 10000 are displayed as "10k", "11k", etc.
    /// </summary>
    public static string FormatCount(int count) {
        return count >= 10000 ? $"{count / 1000}k" : count.ToString();
    }

    public static string StringFromEnumerable(IEnumerable source) {
        StringBuilder stringBuilder = new();
        foreach (object item in source) {
            stringBuilder.AppendLine("• " + item);
        }

        return stringBuilder.ToString();
    }
}