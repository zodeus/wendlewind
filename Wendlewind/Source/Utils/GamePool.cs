namespace Wendlewind.Utils;

public class GamePool<T> where T : class {
    private readonly T[] _pool;
    private readonly Func<T> _creator;
    private int _count;

    public GamePool(Func<T> creator, int capacity = 8) {
        _pool = new T[capacity];
        _creator = creator;
    }

    public T Get() {
        if (_count <= 0) return _creator();

        _count--;
        T result = _pool[_count];
        _pool[_count] = default!;
        return result;
    }

    public void Put(T gameObject) {
        _pool[_count] = gameObject;
        _count++;
    }
}