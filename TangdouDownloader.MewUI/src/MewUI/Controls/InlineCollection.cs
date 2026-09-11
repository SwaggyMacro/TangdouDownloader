using System.Collections;

namespace Aprillz.MewUI.Controls;

/// <summary>
/// The ordered runs that make up a <see cref="TextBlock"/>'s content. Mutating the collection or any
/// run it holds notifies the owner so it can reflow.
/// </summary>
public sealed class InlineCollection : IList<Run>
{
    private readonly List<Run> _runs = [];
    private readonly Action<RunChange> _onChanged;

    internal InlineCollection(Action<RunChange> onChanged) => _onChanged = onChanged;

    public int Count => _runs.Count;
    public bool IsReadOnly => false;

    public Run this[int index]
    {
        get => _runs[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Detach(_runs[index]);
            _runs[index] = Attach(value);
            _onChanged(RunChange.Text);
        }
    }

    public void Add(Run item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _runs.Add(Attach(item));
        _onChanged(RunChange.Text);
    }

    public void Insert(int index, Run item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _runs.Insert(index, Attach(item));
        _onChanged(RunChange.Text);
    }

    public bool Remove(Run item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_runs.Remove(item))
        {
            return false;
        }

        Detach(item);
        _onChanged(RunChange.Text);
        return true;
    }

    public void RemoveAt(int index)
    {
        Detach(_runs[index]);
        _runs.RemoveAt(index);
        _onChanged(RunChange.Text);
    }

    public void Clear()
    {
        if (_runs.Count == 0)
        {
            return;
        }

        foreach (var run in _runs)
        {
            Detach(run);
        }
        _runs.Clear();
        _onChanged(RunChange.Text);
    }

    public bool Contains(Run item) => _runs.Contains(item);
    public void CopyTo(Run[] array, int arrayIndex) => _runs.CopyTo(array, arrayIndex);
    public int IndexOf(Run item) => _runs.IndexOf(item);
    public List<Run>.Enumerator GetEnumerator() => _runs.GetEnumerator();
    IEnumerator<Run> IEnumerable<Run>.GetEnumerator() => _runs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _runs.GetEnumerator();

    private Run Attach(Run run)
    {
        run.Changed += OnRunChanged;
        return run;
    }

    private void Detach(Run run) => run.Changed -= OnRunChanged;

    private void OnRunChanged(Run run, RunChange change) => _onChanged(change);
}
