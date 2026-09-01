using System.Collections;
using System.Xml;
using Wendlemire.Definitions.Loader;

namespace Wendlemire.Maths;

public class SimpleCurve : IEnumerable<CurvePoint> {
    public List<CurvePoint> Points = new();

    private static readonly Comparison<CurvePoint> CurvePointsComparer = delegate(CurvePoint a, CurvePoint b) {
        if (a.X < b.X) {
            return -1;
        }

        return b.X < a.X ? 1 : 0;
    };

    public int PointsCount => Points.Count;

    public CurvePoint this[int i] {
        get => Points[i];
        set => Points[i] = value;
    }

    public SimpleCurve(IEnumerable<CurvePoint> points) {
        SetPoints(points);
    }

    public SimpleCurve() { }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEnumerator<CurvePoint> GetEnumerator() {
        foreach (CurvePoint point in Points) {
            yield return point;
        }
    }

    public void SetPoints(IEnumerable<CurvePoint> newPoints) {
        Points.Clear();
        foreach (CurvePoint newPoint in newPoints) {
            Points.Add(newPoint);
        }

        SortPoints();
    }

    public void Add(float x, float y, bool sort = true) {
        CurvePoint newPoint = new(x, y);
        Add(newPoint, sort);
    }

    public void Add(CurvePoint newPoint, bool sort = true) {
        Points.Add(newPoint);
        if (sort) {
            SortPoints();
        }
    }

    public void SortPoints() {
        Points.Sort(CurvePointsComparer);
    }

    public float Evaluate(float x) {
        if (Points.Count == 0) {
            Log.Error("Evaluating a SimpleCurve with no points.");
            return 0f;
        }

        if (x <= Points[0].X) {
            return Points[0].Y;
        }

        if (x >= Points[Points.Count - 1].X) {
            return Points[Points.Count - 1].Y;
        }

        CurvePoint curvePoint = Points[0];
        CurvePoint curvePoint2 = Points[Points.Count - 1];
        for (int i = 0; i < Points.Count; i++) {
            if (x <= Points[i].X) {
                curvePoint2 = Points[i];
                if (i > 0) {
                    curvePoint = Points[i - 1];
                }

                break;
            }
        }

        float t = (x - curvePoint.X) / (curvePoint2.X - curvePoint.X);
        return Mathf.Lerp(curvePoint.Y, curvePoint2.Y, t);
    }

    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        foreach (XmlNode node in xmlRoot.FirstChild!.ChildNodes) {
            Add(ParseHelper.FromString<CurvePoint>(node.FirstChild!.Value!));
        }
    }
}

public readonly struct CurvePoint {
    private readonly Vector2 _location;

    public Vector2 Location => _location;

    public float X => _location.X;

    public float Y => _location.Y;

    public CurvePoint(float x, float y) {
        _location = new Vector2(x, y);
    }

    public CurvePoint(Vector2 location) {
        _location = location;
    }

    public static implicit operator Vector2(CurvePoint point) {
        return point._location;
    }

    public override string ToString() {
        return _location.ToString();
    }
}