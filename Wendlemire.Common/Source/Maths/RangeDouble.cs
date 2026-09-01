using System.Globalization;

namespace Wendlemire.Maths;

public struct RangeDouble : IEquatable<RangeDouble> {
    public double Min;

    public double Max;

    public static RangeDouble Zero => new(0d, 0d);

    public static RangeDouble One => new(1d, 1d);

    public static RangeDouble ZeroToOne => new(0d, 1d);

    public double Average => (Min + Max) / 2d;

    public double Roll(Random rng) => rng.NextDouble(Min, Max);

    public RangeDouble(double min, double max) {
        Min = min;
        Max = max;
    }

    public double ClampToRange(double value) {
        return Math.Clamp(value, Min, Max);
    }

    public double LerpThroughRange(double lerpPct) {
        return Min + (Max - Min) * lerpPct;
    }

    public double InverseLerpThroughRange(double d) {
        if (Max == Min) {
            return 0d;
        }
        return (d - Min) / (Max - Min);
    }

    public bool Includes(double d) {
        if (d >= Min) {
            return d <= Max;
        }

        return false;
    }

    public RangeDouble ExpandedBy(double d) {
        return new RangeDouble(Min - d, Max + d);
    }

    public static bool operator ==(RangeDouble a, RangeDouble b) {
        if (a.Min == b.Min) {
            return a.Max == b.Max;
        }

        return false;
    }

    public static bool operator !=(RangeDouble a, RangeDouble b) {
        if (a.Min == b.Min) {
            return a.Max != b.Max;
        }

        return true;
    }

    public static RangeDouble operator *(RangeDouble r, double val) {
        return new RangeDouble(r.Min * val, r.Max * val);
    }

    public static RangeDouble operator *(double val, RangeDouble r) {
        return new RangeDouble(r.Min * val, r.Max * val);
    }

    public static RangeDouble FromString(string s) {
        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        string[] array = s.Split('~');
        if (array.Length == 1) {
            double num = Convert.ToDouble(array[0], invariantCulture);
            return new RangeDouble(num, num);
        }

        return new RangeDouble(Convert.ToDouble(array[0], invariantCulture), Convert.ToDouble(array[1], invariantCulture));
    }

    public override string ToString() {
        return Min + "~" + Max;
    }

    public override int GetHashCode() {
        return Hash.HashCombineInt(Min.GetHashCode(), Max.GetHashCode());
    }

    public override bool Equals(object? obj) {
        return obj is RangeDouble rangeDouble && Equals(rangeDouble);
    }

    public bool Equals(RangeDouble other) {
        if (other.Min == Min) {
            return other.Max == Max;
        }

        return false;
    }
}
