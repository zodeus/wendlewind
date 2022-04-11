using System;
using System.Globalization;

namespace Grafted.Maths;

public struct RangeFloat : IEquatable<RangeFloat> {
    public float min;

    public float max;

    public static RangeFloat Zero => new RangeFloat(0f, 0f);

    public static RangeFloat One => new RangeFloat(1f, 1f);

    public static RangeFloat ZeroToOne => new RangeFloat(0f, 1f);

    public float Average => (min + max) / 2f;

    public float RandomValue => Core.Random.NextFloat(min, max);

    /*
    public float TrueMin => Mathf.Min(min, max);

    public float TrueMax => Mathf.Max(min, max);

    public float Span => TrueMax - TrueMin;
    */

    public RangeFloat(float min, float max) {
        this.min = min;
        this.max = max;
    }

    public float ClampToRange(float value) {
        return Mathf.Clamp(value, min, max);
    }

    /*public float RandomInRangeSeeded(int seed) {
        return Rand.RangeSeeded(min, max, seed);
    }*/

    public float LerpThroughRange(float lerpPct) {
        return Mathf.Lerp(min, max, lerpPct);
    }

    public float InverseLerpThroughRange(float f) {
        return Mathf.InverseLerp(min, max, f);
    }

    public bool Includes(float f) {
        if (f >= min) {
            return f <= max;
        }

        return false;
    }

    public bool IncludesEpsilon(float f) {
        if (f >= min - 1E-05f) {
            return f <= max + 1E-05f;
        }

        return false;
    }

    public RangeFloat ExpandedBy(float f) {
        return new RangeFloat(min - f, max + f);
    }

    public static bool operator ==(RangeFloat a, RangeFloat b) {
        if (a.min == b.min) {
            return a.max == b.max;
        }

        return false;
    }

    public static bool operator !=(RangeFloat a, RangeFloat b) {
        if (a.min == b.min) {
            return a.max != b.max;
        }

        return true;
    }

    public static RangeFloat operator *(RangeFloat r, float val) {
        return new RangeFloat(r.min * val, r.max * val);
    }

    public static RangeFloat operator *(float val, RangeFloat r) {
        return new RangeFloat(r.min * val, r.max * val);
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
        return min + "~" + max;
    }

    // public override int GetHashCode()
    // {
    // 	return Gen.HashCombineStruct(min.GetHashCode(), max);
    // }

    public override bool Equals(object? obj) {
        return obj is RangeFloat rangeFloat && Equals(rangeFloat);
    }

    public bool Equals(RangeFloat other) {
        if (other.min == min) {
            return other.max == max;
        }

        return false;
    }

    public override int GetHashCode() {
        unchecked {
            return (min.GetHashCode() * 397) ^ max.GetHashCode();
        }
    }
}