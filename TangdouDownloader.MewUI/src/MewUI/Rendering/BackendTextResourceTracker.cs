namespace Aprillz.MewUI.Rendering;

/// <summary>
/// Tracks <see cref="BackendTextLayout"/> instances with native handles.
/// Releases native resources when layouts are no longer referenced.
/// </summary>
internal sealed class BackendTextResourceTracker
{
    private sealed class Entry(WeakReference<BackendTextLayout> weakRef, NativeHandleLease lease)
    {
        public readonly WeakReference<BackendTextLayout> WeakRef = weakRef;
        public readonly NativeHandleLease Lease = lease;
    }

    private readonly LinkedList<Entry> _layouts = new();

    public void TrackLayout(BackendTextLayout layout)
    {
        if (layout.BackendLease is { } lease && lease.Handle != 0)
        {
            _layouts.AddFirst(new Entry(new WeakReference<BackendTextLayout>(layout), lease));
        }
    }

    public void Cleanup()
    {
        var node = _layouts.First;
        while (node != null)
        {
            var next = node.Next;
            if (!node.Value.WeakRef.TryGetTarget(out _))
            {
                node.Value.Lease.Release();
                _layouts.Remove(node);
            }
            node = next;
        }
    }

    public void ReleaseAll()
    {
        foreach (var entry in _layouts)
        {
            entry.Lease.Release();
        }
        _layouts.Clear();
    }
}
