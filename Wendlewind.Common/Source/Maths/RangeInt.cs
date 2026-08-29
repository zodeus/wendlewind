namespace Wendlewind.Maths;

public readonly struct RangeInt(int min, int max)
{
    public readonly int Min = min;
    public readonly int Max = max;

    public int Roll(Random rng) => rng.Next(Min, Max + 1);

    public override string ToString() {
        return Min + "~" + Max;
    }

    public override int GetHashCode() {
        return Hash.HashCombineInt(Min, Max);
    }

    public override bool Equals(object? obj) {
        return obj is RangeInt rangeInt && Equals(rangeInt);
    }

    public bool Equals(RangeInt other) {
        if (Min == other.Min) {
            return Max == other.Max;
        }

        return false;
    }

    public static bool operator ==(RangeInt a, RangeInt b) {
        return a.Equals(b);
    }

    public static bool operator !=(RangeInt a, RangeInt b) {
        return !(a == b);
    }
}