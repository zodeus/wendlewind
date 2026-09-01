using System.Globalization;

namespace Wendlemire.Definitions.Loader;

public static class ParseHelper
{
    private static class Parsers<T>
    {
        public static Func<string, T>? Parser;

        public static void Register(Func<string, T>? method)
        {
            Parser = method;
            _parsers.Add(typeof(T), str => method!(str)!);
        }
    }

    private static Dictionary<Type, Func<string, object>?> _parsers;

    private static readonly char[] ColorTrimStartParameters;

    private static readonly char[] ColorTrimEndParameters;

    public static string ParseString(string str)
    {
        return str.Replace("\\n", "\n");
    }

    public static int ParseIntPermissive(string str)
    {
        if (!int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            result = (int)float.Parse(str, CultureInfo.InvariantCulture);
            Log.Warning("Parsed " + str + " as int.");
        }

        return result;
    }

    public static Vector3 FromStringVector3(string str)
    {
        str = str.TrimStart('(');
        str = str.TrimEnd(')');
        string[] array = str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        var x = Convert.ToSingle(array[0], invariantCulture);
        var y = Convert.ToSingle(array[1], invariantCulture);
        var z = Convert.ToSingle(array[2], invariantCulture);
        return new Vector3(x, y, z);
    }

    public static Vector2 FromStringVector2(string str)
    {
        str = str.TrimStart('(');
        str = str.TrimEnd(')');
        string[] array = str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        float x;
        float y;
        if (array.Length == 1)
        {
            x = (y = Convert.ToSingle(array[0], invariantCulture));
        }
        else
        {
            if (array.Length != 2)
            {
                throw new InvalidOperationException();
            }

            x = Convert.ToSingle(array[0], invariantCulture);
            y = Convert.ToSingle(array[1], invariantCulture);
        }

        return new Vector2(x, y);
    }

    public static Point FromStringPoint(string str)
    {
        str = str.TrimStart('(');
        str = str.TrimEnd(')');
        string[] array = str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        int x;
        int y;
        if (array.Length == 1)
        {
            x = (y = Convert.ToInt32(array[0], invariantCulture));
        }
        else
        {
            if (array.Length != 2)
            {
                throw new InvalidOperationException();
            }

            x = Convert.ToInt32(array[0], invariantCulture);
            y = Convert.ToInt32(array[1], invariantCulture);
        }

        return new Point(x, y);
    }

    public static Vector4 FromStringVector4Adaptive(string str)
    {
        str = str.TrimStart('(');
        str = str.TrimEnd(')');
        string[] array = str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        var x = 0f;
        var y = 0f;
        var z = 0f;
        var w = 0f;
        if (array.Length >= 1)
        {
            x = Convert.ToSingle(array[0], invariantCulture);
        }

        if (array.Length >= 2)
        {
            y = Convert.ToSingle(array[1], invariantCulture);
        }

        if (array.Length >= 3)
        {
            z = Convert.ToSingle(array[2], invariantCulture);
        }

        if (array.Length >= 4)
        {
            w = Convert.ToSingle(array[3], invariantCulture);
        }

        if (array.Length >= 5)
        {
            Log.Error($"Too many elements in vector {str}");
        }

        return new Vector4(x, y, z, w);
    }

    // public static Rec FromStringRect(string str) {
    //     str = str.TrimStart('(');
    //     str = str.TrimEnd(')');
    //     string[] array = str.Split(',');
    //     CultureInfo invariantCulture = CultureInfo.InvariantCulture;
    //     float x = Convert.ToSingle(array[0], invariantCulture);
    //     float y = Convert.ToSingle(array[1], invariantCulture);
    //     float width = Convert.ToSingle(array[2], invariantCulture);
    //     float height = Convert.ToSingle(array[3], invariantCulture);
    //     return new Rect(x, y, width, height);
    // }

    public static float ParseFloat(string str)
    {
        return float.Parse(str, CultureInfo.InvariantCulture);
    }

    public static bool ParseBool(string str)
    {
        return bool.Parse(str);
    }

    public static long ParseLong(string str)
    {
        return long.Parse(str, CultureInfo.InvariantCulture);
    }

    public static double ParseDouble(string str)
    {
        return double.Parse(str, CultureInfo.InvariantCulture);
    }

    public static sbyte ParseSByte(string str)
    {
        return sbyte.Parse(str, CultureInfo.InvariantCulture);
    }

    public static Type? ParseType(string str)
    {
        if (str == "null" || str == "Null")
        {
            return null;
        }

        var typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(str);
        if (typeInAnyAssembly == null)
        {
            Log.Error("Could not find a type named " + str);
        }

        return typeInAnyAssembly;
    }

    public static Action ParseAction(string str)
    {
        string[] array = str.Split('.');
        var methodName = array[^1];
        var typeName = (array.Length != 3) ? array[0] : (array[0] + "." + array[1]);
        var method = GenTypes.GetTypeInAnyAssembly(typeName)!.GetMethods().First(m => m.Name == methodName);
        return (Action)Delegate.CreateDelegate(typeof(Action), method);
    }


    public static Color ParseColor(string str)
    {
        string[] colors = str.TrimStart(ColorTrimStartParameters).TrimEnd(ColorTrimEndParameters).Split(',');
        var red = ParseFloat(colors[0]);
        var green = ParseFloat(colors[1]);
        var blue = ParseFloat(colors[2]);
        var isInt = red > 1f || blue > 1f || green > 1f;
        float alpha = (!isInt) ? 1 : 255;
        if (colors.Length == 4)
        {
            alpha = FromString<float>(colors[3]);
        }

        Color result;
        if (isInt == false)
        {
            throw new NotImplementedException();
            /*result.R = red;
            result.G = green;
            result.B = blue;
            result.A = alpha;
            return result;
            */
        }

        result = new Color(Mathf.RoundToInt(red), Mathf.RoundToInt(green), Mathf.RoundToInt(blue), Mathf.RoundToInt(alpha));
        return result;
    }

    public static CurvePoint ParseCurvePoint(string str)
    {
        return new CurvePoint(FromString<Vector2>(str));
    }

    /*public static NameTriple ParseNameTriple(string str)
    {
        NameTriple nameTriple = NameTriple.FromString(str);
        nameTriple.ResolveMissingPieces();
        return nameTriple;
    }*/

    /*public static FloatRange ParseFloatRange(string str) {
        return FloatRange.FromString(str);
    }
    */
    public static RangeInt ParseRangeInt(string value)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        string[] array = value.Split('~');
        if (array.Length == 1)
        {
            var num = Convert.ToInt32(array[0], invariantCulture);
            return new RangeInt(num, num);
        }

        return new RangeInt(Convert.ToInt32(array[0], invariantCulture), Convert.ToInt32(array[1], invariantCulture));
    }

    public static RangeFloat ParseRangeFloat(string value)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        string[] array = value.Split('~');
        if (array.Length == 1)
        {
            var num = Convert.ToSingle(array[0], invariantCulture);
            return new RangeFloat(num, num);
        }

        return new RangeFloat(Convert.ToSingle(array[0], invariantCulture), Convert.ToSingle(array[1], invariantCulture));
    }

    public static Size ParseSize(string value)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        value = value.TrimStart('(').TrimEnd(')');
        string[] array = value.Split(',');
        if (array.Length == 1)
        {
            var num = Convert.ToInt32(array[0], invariantCulture);
            return new Size(num, num);
        }

        return new Size(Convert.ToInt32(array[0], invariantCulture), Convert.ToInt32(array[1], invariantCulture));
    }

    /*public static QualityRange ParseQualityRange(string str)
    {
        return QualityRange.FromString(str);
    }*/

    /*public static ColorInt ParseColorInt(string str) {
        str = str.TrimStart(colorTrimStartParameters);
        str = str.TrimEnd(colorTrimEndParameters);
        string[] array = str.Split(',');
        ColorInt result = new ColorInt(255, 255, 255, 255);
        result.r = ParseIntPermissive(array[0]);
        result.g = ParseIntPermissive(array[1]);
        result.b = ParseIntPermissive(array[2]);
        if (array.Length == 4) {
            result.a = ParseIntPermissive(array[3]);
        }
        else {
            result.a = 255;
        }

        return result;
    }*/

    static ParseHelper()
    {
        _parsers = new Dictionary<Type, Func<string, object>?>();
        ColorTrimStartParameters = new[]
        {
            '(',
            'R',
            'G',
            'B',
            'A'
        };
        ColorTrimEndParameters = new[]
        {
            ')'
        };
        Parsers<string>.Register(ParseString);
        Parsers<int>.Register(ParseIntPermissive);
        Parsers<Vector3>.Register(FromStringVector3);
        Parsers<Vector2>.Register(FromStringVector2);
        Parsers<Point>.Register(FromStringPoint);
        Parsers<Vector4>.Register(FromStringVector4Adaptive);
        //Parsers<Rect>.Register(FromStringRect);
        Parsers<float>.Register(ParseFloat);
        Parsers<bool>.Register(ParseBool);
        Parsers<long>.Register(ParseLong);
        Parsers<double>.Register(ParseDouble);
        Parsers<sbyte>.Register(ParseSByte);
        Parsers<Type>.Register(ParseType!);
        // Parsers<Action>.Register(ParseAction);
        Parsers<Color>.Register(ParseColor);
        // Parsers<CellRect>.Register(ParseCellRect);
        Parsers<CurvePoint>.Register(ParseCurvePoint);
        Parsers<RangeInt>.Register(ParseRangeInt);
        Parsers<RangeFloat>.Register(ParseRangeFloat);
        Parsers<Size>.Register(ParseSize);
        //todo
        //Parsers<PublishedFileId_t>.Register(ParsePublishedFileId);
        //Parsers<Rot4>.Register(ParseRot4);
        //Parsers<NameTriple>.Register(ParseNameTriple);
        //Parsers<RangeFloat>.Register(ParseRangeFloat);
        //Parsers<QualityRange>.Register(ParseQualityRange);
        //Parsers<ColorInt>.Register(ParseColorInt);
    }

    public static T FromString<T>(string str)
    {
        Func<string, T>? parser = Parsers<T>.Parser;
        if (parser != null)
        {
            return parser(str);
        }

        return (T)FromString(str, typeof(T))!;
    }

    public static object? FromString(string str, Type itemType)
    {
        try
        {
            itemType = (Nullable.GetUnderlyingType(itemType) ?? itemType);
            if (itemType.IsEnum)
            {
                try
                {
                    /*object obj = BackCompatibility.BackCompatibleEnum(itemType, str);
                    if (obj != null)
                    {
                        return obj;
                    }*/
                    return Enum.Parse(itemType, str);
                }
                catch (ArgumentException innerException)
                {
                    throw new ArgumentException(
                        string.Concat(string.Concat("'", str, "' is not a valid value for ", itemType, ". Valid values are: \n"), TextHelpers.StringFromEnumerable(Enum.GetValues(itemType))),
                        innerException);
                }
            }

            if (_parsers.TryGetValue(itemType, out Func<string, object>? value))
            {
                return value!(str);
            }

            return null;
        }
        catch (Exception innerException2)
        {
            throw new ArgumentException(string.Concat("Exception parsing ", itemType, " from \"", str, "\""), innerException2);
        }
    }

    public static bool HandlesType(Type type)
    {
        return true;
    }
}
