namespace Wendlewind.Maths;

public static class Hash {
    public static int HashCombine<T>(int seed, T obj) {
        int num = obj?.GetHashCode() ?? 0;
        return (int) (seed ^ (num + 2654435769u + (seed << 6) + (seed >> 2)));
    }

    public static int HashCombineStruct<T>(int seed, T obj) where T : struct {
        return (int) (seed ^ (obj.GetHashCode() + 2654435769u + (seed << 6) + (seed >> 2)));
    }

    public static int HashCombineInt(int seed, int value) {
        return (int) (seed ^ (value + 2654435769u + (seed << 6) + (seed >> 2)));
    }

    public static int HashCombineInt(int v1, int v2, int v3, int v4) {
        int num = 352654597;
        int num2 = num;
        num = (((num << 5) + num + (num >> 27)) ^ v1);
        num2 = (((num2 << 5) + num2 + (num2 >> 27)) ^ v2);
        num = (((num << 5) + num + (num >> 27)) ^ v3);
        num2 = (((num2 << 5) + num2 + (num2 >> 27)) ^ v4);
        return num + num2 * 1566083941;
    }

    public static int HashOffset(this int baseInt) {
        return HashCombineInt(baseInt, 169495093);
    }
}