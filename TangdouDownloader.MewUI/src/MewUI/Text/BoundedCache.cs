namespace Aprillz.MewUI.Text;

/// <summary>Fixed-capacity LRU cache that runs a dispose action on evicted and replaced values.</summary>
internal sealed class BoundedCache<TKey, TValue> : IDisposable where TKey : notnull
{
    private readonly int _capacity;
    private readonly Action<TValue> _dispose;
    private readonly Dictionary<TKey, Entry> _entries = [];
    private readonly LinkedList<TKey> _order = [];

    public BoundedCache(int capacity, Action<TValue> dispose)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
    }

    public int Count => _entries.Count;
    public IReadOnlyCollection<TValue> Values => _entries.Values.Select(static entry => entry.Value).ToArray();

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            _order.Remove(entry.Node);
            _order.AddLast(entry.Node);
            value = entry.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (_entries.Remove(key, out var replaced))
        {
            _order.Remove(replaced.Node);
            _dispose(replaced.Value);
        }
        var node = _order.AddLast(key);
        _entries.Add(key, new Entry(value, node));
        while (_entries.Count > _capacity && _order.First is LinkedListNode<TKey> oldest)
        {
            _order.RemoveFirst();
            if (_entries.Remove(oldest.Value, out var evicted))
            {
                _dispose(evicted.Value);
            }
        }
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
        {
            _dispose(entry.Value);
        }
        _entries.Clear();
        _order.Clear();
    }

    private sealed record Entry(TValue Value, LinkedListNode<TKey> Node);
}
