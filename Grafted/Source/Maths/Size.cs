using System;
using System.Diagnostics.CodeAnalysis;
using Grafted.Utils;
using Microsoft.Xna.Framework;

namespace Grafted.Maths;

public struct Size : IEquatable<Size>, IEquatableByRef<Size> {
    public static Size Empty => new(0, 0);
    public int Width;
    public int Height;

    public bool IsEmpty => Width == 0 && Height == 0;

    public Size(int width, int height) {
        Width = width;
        Height = height;
    }

    public static bool operator ==(Size first, Size second) => first.Equals(ref second);

    public bool Equals(Size size) => Equals(ref size);

    public bool Equals(ref Size size) => Width == size.Width && Height == size.Height;

    public override bool Equals(object? obj) => obj is Size size && Equals(size);

    public static bool operator !=(Size first, Size second) => !(first == second);

    public static Size operator +(Size first, Size second) => Add(first, second);

    public static Size Add(Size first, Size second) {
        Size size;
        size.Width = first.Width + second.Width;
        size.Height = first.Height + second.Height;
        return size;
    }

    public static Size operator -(Size first, Size second) => Subtract(first, second);

    public static Size operator /(Size size, int value) => new(size.Width / value, size.Height / value);

    public static Size operator *(Size size, int value) => new(size.Width * value, size.Height * value);

    public static Size Subtract(Size first, Size second) {
        Size size;
        size.Width = first.Width - second.Width;
        size.Height = first.Height - second.Height;
        return size;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() => Width.GetHashCode() * 397 ^ Height.GetHashCode();

    public static implicit operator Size(Point point) => new(point.X, point.Y);

    public static implicit operator Point(Size size) => new(size.Width, size.Height);

    public override string ToString() => string.Format("({0},{1})", Width, Height);
}