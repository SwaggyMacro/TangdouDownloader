namespace Aprillz.MewUI;

/// <summary>
/// A persistent command presentation (e.g. a command button or an open menu) whose visual state
/// the framework re-queries during command state evaluation passes.
/// </summary>
internal interface ICommandSource
{
    /// <summary>
    /// Re-queries CanExecute for this source and invalidates its visual only when the result changed.
    /// </summary>
    void EvaluateCommandState();
}

/// <summary>
/// Per-window registry of attached command sources; the evaluation pass visits only these
/// registered sources, never the whole visual tree.
/// </summary>
internal sealed class CommandStateTracker
{
    private readonly List<ICommandSource> _sources = new();
    private readonly List<ICommandSource> _evaluationScratch = new();

    public bool HasSources => _sources.Count > 0;

    public void Register(ICommandSource source)
    {
        if (!_sources.Contains(source))
        {
            _sources.Add(source);
            source.EvaluateCommandState();
        }
    }

    public void Unregister(ICommandSource source) => _sources.Remove(source);

    public void Clear()
    {
        _sources.Clear();
        _evaluationScratch.Clear();
    }

    public void EvaluateAll()
    {
        if (_sources.Count == 0)
        {
            return;
        }

        // Snapshot so a source may unregister (e.g. a menu closing) during its own evaluation.
        _evaluationScratch.Clear();
        _evaluationScratch.AddRange(_sources);
        for (int i = 0; i < _evaluationScratch.Count; i++)
        {
            _evaluationScratch[i].EvaluateCommandState();
        }

        _evaluationScratch.Clear();
    }
}
