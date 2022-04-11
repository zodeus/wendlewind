using System.Collections;
using System.Text;

namespace Grafted.Utils;

public static class TextHelpers {
    public static bool NullOrEmpty(this string str) {
        return string.IsNullOrEmpty(str);
    }

    public static string StringFromEnumerable(IEnumerable source) {
        StringBuilder stringBuilder = new();
        foreach (object item in source) {
            stringBuilder.AppendLine("• " + item);
        }

        return stringBuilder.ToString();
    }
}