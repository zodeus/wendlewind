using System.Globalization;

namespace Wendlewind.Maths;

public struct RangeFloat : IEquatable<RangeFloat> {
    public float Min;

    public float Max;

    public static RangeFloat Zero => new(0f, 0f);

    public static RangeFloat One => new(1f, 1f);

    public static RangeFloat ZeroToOne => new(0f, 1f);

    public float Average => (Min + Max) / 2f;

    public float RandomValue => Rng.Current.NextFloat(Min, Max);

    public RangeFloat(float min, float max) {
        Min = min;
        Max = max;
    }

    public float ClampToRange(float value) {
        return Mathf.Clamp(value, Min, Max);
    }

    public float LerpThroughRange(float lerpPct) {
        return Mathf.Lerp(Min, Max, lerpPct);
    }

    public float InverseLerpThroughRange(float f) {
        return Mathf.InverseLerp(Min, Max, f);
    }

    public bool Includes(float f) {
        if (f >= Min) {
            return f <= Max;
        }

        return false;
    }

    public RangeFloat ExpandedBy(float f) {
        return new RangeFloat(Min - f, Max + f);
    }

    public static bool operator ==(RangeFloat a, RangeFloat b) {
        if (a.Min == b.Min) {
            return a.Max == b.Max;
        }

        return false;
    }

    public static bool operator !=(RangeFloat a, RangeFloat b) {
        if (a.Min == b.Min) {
            return a.Max != b.Max;
        }

        return true;
    }

    public static RangeFloat operator *(RangeFloat r, float val) {
        return new RangeFloat(r.Min * val, r.Max * val);
    }

    public static RangeFloat operator *(float val, RangeFloat r) {
        return new RangeFloat(r.Min * val, r.Max * val);
    }

    public static RangeFloat FromString(string s) {
        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        string[] array = s.Split('~');
        if (array.Length == 1) {
            float num = Convert.ToSingle(array[0], invariantCulture);
            return new RangeFloat(num, num);
        }

        return new RangeFloat(Convert.ToSingle(array[0], invariantCulture), Convert.ToSingle(array[1], invariantCulture));
    }

    public override string ToString() {
        return Min + "~" + Max;
    }

    public override int GetHashCode()
    {
        return Hash.HashCombineInt(Min.GetHashCode(), Max.GetHashCode());
    }

    public override bool Equals(object? obj)
    {
        return obj is RangeFloat rangeFloat && Equals(rangeFloat);
    }

    public bool Equals(RangeFloat other) {
        if (other.Min == Min) {
            return other.Max == Max;
        }

        return false;
    }
}