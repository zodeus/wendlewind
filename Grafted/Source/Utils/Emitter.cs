namespace Grafted.Utils;

/// <summary>
/// simple event emitter that is designed to have its generic constraint be either an int or an enum
/// </summary>
public class Emitter<T> where T : struct, IComparable, IFormattable {
    private readonly Dictionary<T, List<Action>> _messageTable;

    public Emitter() {
        _messageTable = new Dictionary<T, List<Action>>();
    }

    /// <summary>
    /// if using an enum as the generic constraint you may want to pass in a custom comparer to avoid boxing/unboxing. See the CoreEventsComparer
    /// for an example implementation.
    /// </summary>
    /// <param name="customComparer">Custom comparer.</param>
    public Emitter(IEqualityComparer<T> customComparer) {
        _messageTable = new Dictionary<T, List<Action>>(customComparer);
    }

    public void AddObserver(T eventType, Action handler) {
        if (!_messageTable.TryGetValue(eventType, out List<Action>? list)) {
            list = new List<Action>();
            _messageTable.Add(eventType, list);
        }

        Insist.IsFalse(list.Contains(handler), "You are trying to add the same observer twice");
        list.Add(handler);
    }

    public void RemoveObserver(T eventType, Action handler) {
        _messageTable[eventType].Remove(handler);
    }

    public void Emit(T eventType) {
        if (!_messageTable.TryGetValue(eventType, out List<Action>? list)) return;

        for (int i = list.Count - 1; i >= 0; i--) {
            list[i]();
        }
    }
}