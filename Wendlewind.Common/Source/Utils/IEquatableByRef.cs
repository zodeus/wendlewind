namespace Wendlewind.Utils;

public interface IEquatableByRef<T> {
    bool Equals(ref T other);
}