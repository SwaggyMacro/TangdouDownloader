namespace Aprillz.MewUI.Text.Editing;

/// <summary>
/// Undo/redo history owned by the document. Edits recorded through this class stay undoable;
/// any other document change is unrecorded and clears the history, so stale offsets can never
/// be replayed.
/// </summary>
internal sealed class TextEditHistory
{
    private readonly EditableTextDocument _document;
    private readonly List<EditCommand> _undo = [];
    private readonly Stack<EditCommand> _redo = new();
    private int _suppressDepth;
    private int _sizeLimit = -1;
    // Edits carry the group they were recorded in, and undo walks a whole group. Ungrouped edits
    // get an id of their own, so one loop covers both and no edit can join a neighbour by accident.
    private int _nextGroupId = 1;
    private int _openGroupId;
    private int _groupDepth;
    // Set while a suppressed replace runs, so the recorder learns both whether the document changed
    // and what it removed without asking for either up front.
    private TextChange? _appliedChange;

    internal TextEditHistory(EditableTextDocument document)
    {
        _document = document;
        _document.Changed += OnDocumentChanged;
    }

    /// <summary>
    /// Most recent edits to keep, oldest dropped past it. Negative keeps every edit; zero records
    /// none, which is how a control that must not retain what was typed opts out of undo.
    /// </summary>
    public int SizeLimit
    {
        get => _sizeLimit;
        set
        {
            _sizeLimit = value;
            Trim();
        }
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Applies a replace and records it. Returns false without touching the document when the replacement is a no-op.</summary>
    public bool RecordReplace(
        int start,
        int removeLength,
        string inserted,
        int anchorBefore,
        int caretBefore,
        int anchorAfter,
        int caretAfter)
    {
        // The document decides whether this changes anything, comparing in place without building a
        // string, and hands back what it removed. Reading ahead would allocate even for a no-op.
        _appliedChange = null;
        ReplaceSuppressed(start, removeLength, inserted);
        if (_appliedChange is not TextChange applied)
        {
            return false;
        }
        Record(new EditCommand(
            start, applied.RemovedText, inserted, anchorBefore, caretBefore, anchorAfter, caretAfter));
        return true;
    }

    /// <summary>Records an already-applied edit (composition commit aggregates its transient replaces).</summary>
    public void Push(
        int start,
        string removed,
        string inserted,
        int anchorBefore,
        int caretBefore,
        int anchorAfter,
        int caretAfter)
        => Record(new EditCommand(start, removed, inserted, anchorBefore, caretBefore, anchorAfter, caretAfter));

    /// <summary>
    /// Starts a group; the edits recorded until it is disposed undo together. Nesting extends the
    /// outermost group rather than starting another, so a routine that groups its own edits stays
    /// correct when a caller groups it in turn.
    /// </summary>
    public IDisposable BeginGroup()
    {
        if (_groupDepth++ == 0)
        {
            _openGroupId = _nextGroupId++;
        }
        return new Group(this);
    }

    private void EndGroup()
    {
        if (_groupDepth > 0 && --_groupDepth == 0)
        {
            _openGroupId = 0;
        }
    }

    private void Record(in EditCommand command)
    {
        _redo.Clear();
        if (_sizeLimit == 0)
        {
            return;
        }
        _undo.Add(command with { GroupId = _openGroupId != 0 ? _openGroupId : _nextGroupId++ });
        Trim();
    }

    private void Trim()
    {
        if (_sizeLimit < 0)
        {
            return;
        }
        if (_sizeLimit == 0)
        {
            Clear();
            return;
        }
        if (_undo.Count <= _sizeLimit)
        {
            return;
        }

        int removeCount = _undo.Count - _sizeLimit;
        int cutGroupId = _undo[removeCount - 1].GroupId;
        _undo.RemoveRange(0, removeCount);
        // Half a group would undo to a state the document was never in, so the rest of a group the
        // limit cut through goes too, even though that keeps fewer edits than asked for.
        while (_undo.Count > 0 && _undo[0].GroupId == cutGroupId)
        {
            _undo.RemoveAt(0);
        }
    }

    /// <summary>Applies a replace that neither records nor clears (composition intermediates).</summary>
    public void ReplaceTransient(int start, int length, string text)
        => ReplaceSuppressed(start, length, text);

    public bool TryUndo(out int anchor, out int caret)
    {
        if (_undo.Count == 0)
        {
            anchor = caret = 0;
            return false;
        }
        int groupId = _undo[^1].GroupId;
        anchor = caret = 0;
        // Newest first, so the last one undone is the oldest of the group and its before-state is
        // the one the document was in when the group opened.
        while (_undo.Count > 0 && _undo[^1].GroupId == groupId)
        {
            var command = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            ReplaceSuppressed(command.Start, command.Inserted.Length, command.Removed);
            _redo.Push(command);
            anchor = command.AnchorBefore;
            caret = command.CaretBefore;
        }
        return true;
    }

    public bool TryRedo(out int anchor, out int caret)
    {
        if (!_redo.TryPop(out var command))
        {
            anchor = caret = 0;
            return false;
        }
        int groupId = command.GroupId;
        while (true)
        {
            ReplaceSuppressed(command.Start, command.Removed.Length, command.Inserted);
            _undo.Add(command);
            anchor = command.AnchorAfter;
            caret = command.CaretAfter;
            if (!_redo.TryPeek(out var next) || next.GroupId != groupId)
            {
                return true;
            }
            command = _redo.Pop();
        }
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private void ReplaceSuppressed(int start, int length, string text)
    {
        _suppressDepth++;
        try
        {
            _document.Replace(start, length, text);
        }
        finally
        {
            _suppressDepth--;
        }
    }

    private void OnDocumentChanged(TextChange change)
    {
        if (_suppressDepth == 0)
        {
            Clear();
            return;
        }
        _appliedChange = change;
    }

    private readonly record struct EditCommand(
        int Start,
        string Removed,
        string Inserted,
        int AnchorBefore,
        int CaretBefore,
        int AnchorAfter,
        int CaretAfter)
    {
        /// <summary>Edits sharing an id undo and redo as one step. Assigned when the edit is recorded.</summary>
        public int GroupId { get; init; }
    }

    private sealed class Group(TextEditHistory history) : IDisposable
    {
        private bool _ended;

        public void Dispose()
        {
            if (_ended)
            {
                return;
            }
            _ended = true;
            history.EndGroup();
        }
    }
}
