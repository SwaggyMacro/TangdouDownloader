using System.Text;

namespace Aprillz.MewUI.Text.Editing;

/// <summary>
/// Mutable text storage for editor consumers of the read-only text view contracts.
/// Mutation and undo are intentionally kept outside the layout engine.
/// </summary>
public sealed class EditableTextDocument : IReadOnlyTextDocument
{
    private readonly StringBuilder _text = new();
    private readonly EditableLineIndex _lines = new();
    private TextEditHistory? _history;

    public EditableTextDocument(string? text = null)
        : this(text, preserveLineEndings: false)
    {
    }

    /// <summary>
    /// Preserving keeps each line's own terminator instead of rewriting every one to a line feed. A
    /// document that round-trips a file needs that; a text box does not, and the normalizing default
    /// keeps offsets from straddling a two-character terminator.
    /// </summary>
    internal EditableTextDocument(string? text, bool preserveLineEndings)
    {
        PreservesLineEndings = preserveLineEndings;
        _text.Append(Normalize(text));
        RebuildLines();
    }

    /// <summary>
    /// A document that keeps each line's own terminator, so a file read into it round-trips
    /// unchanged. Every other document rewrites terminators to a line feed.
    /// </summary>
    public static EditableTextDocument CreatePreservingLineEndings(string? text = null)
        => new(text, preserveLineEndings: true);

    /// <summary>Whether the document keeps the line terminators it was given.</summary>
    public bool PreservesLineEndings { get; }

    /// <summary>Applies this document's line-terminator policy to incoming text.</summary>
    internal string Normalize(string? text)
        => PreservesLineEndings ? text ?? string.Empty : NormalizeNewLines(text ?? string.Empty);

    public int TextLength => _text.Length;
    public long Version { get; private set; }
    public int LineCount => _lines.Count;

    public event Action<TextChange>? Changed;

    internal TextEditHistory History => _history ??= new TextEditHistory(this);

    /// <summary>
    /// Groups the edits made until the returned object is disposed into one undo step. Every edit
    /// still raises <see cref="Changed"/> as it happens, so a reader inside the group never sees
    /// stale text; only what undo treats as one step changes. Nesting extends the outermost group.
    /// </summary>
    public IDisposable BeginUndoGroup() => History.BeginGroup();

    /// <summary>Whether there is an edit to undo.</summary>
    public bool CanUndo => History.CanUndo;

    /// <summary>Whether there is an undone edit to redo.</summary>
    public bool CanRedo => History.CanRedo;

    /// <summary>
    /// Undoes one step. False when there was nothing to undo. The history belongs to the document
    /// rather than to a view, so a document with no editor undoes through this.
    /// </summary>
    public bool Undo()
    {
        if (!History.TryUndo(out int anchor, out int caret))
        {
            return false;
        }
        SelectionRestored?.Invoke(anchor, caret);
        return true;
    }

    /// <summary>Redoes one step. False when there was nothing to redo.</summary>
    public bool Redo()
    {
        if (!History.TryRedo(out int anchor, out int caret))
        {
            return false;
        }
        SelectionRestored?.Invoke(anchor, caret);
        return true;
    }

    /// <summary>
    /// Drops both stacks, so what is in the document becomes the starting point. A control that
    /// must not retain what was typed clears it; setting the text does so implicitly.
    /// </summary>
    public void ClearUndoHistory() => History.Clear();

    /// <summary>
    /// Most recent edits to keep, oldest dropped past it. Negative keeps every edit; zero records
    /// none, which is how a control that must not retain what was typed opts out of undo. A group
    /// counts as the edits it holds, and the limit never leaves half of one behind.
    /// </summary>
    public int UndoSizeLimit
    {
        get => History.SizeLimit;
        set => History.SizeLimit = value;
    }

    /// <summary>
    /// Anchor and caret an undo or redo restored. Editing sessions follow it, since the positions
    /// belong to the edit rather than to whichever session replayed it.
    /// </summary>
    internal event Action<int, int>? SelectionRestored;

    public char GetCharAt(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset >= _text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        return _text[offset];
    }

    public string GetText(int offset, int length)
    {
        ValidateRange(offset, length);
        return _text.ToString(offset, length);
    }

    public override string ToString() => _text.ToString();

    public IReadOnlyDocumentLine GetLineByNumber(int lineNumber)
    {
        if ((uint)lineNumber >= (uint)_lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }
        var line = _lines.GetByNumber(lineNumber);
        return new EditableDocumentLine(
            line.LineNumber,
            line.Offset,
            line.Length,
            GetDelimiter(line.Offset + line.Length, line.DelimiterLength));
    }

    public IReadOnlyDocumentLine GetLineByOffset(int offset)
    {
        if (offset < 0 || offset > _text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var line = _lines.GetByOffset(offset, _text.Length);
        return new EditableDocumentLine(
            line.LineNumber,
            line.Offset,
            line.Length,
            GetDelimiter(line.Offset + line.Length, line.DelimiterLength));
    }

    public int GetOffset(int line, int column)
    {
        var documentLine = _lines.GetByNumber(line);
        if (column < 0 || column > documentLine.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
        return documentLine.Offset + column;
    }

    public TextLocation GetLocation(int offset)
    {
        var line = _lines.GetByOffset(offset, _text.Length);
        return new TextLocation(line.LineNumber, Math.Min(offset - line.Offset, line.Length));
    }

    public void SetText(string? text)
    {
        string normalized = Normalize(text);
        if (ContentEquals(normalized))
        {
            return;
        }

        int previousLength = _text.Length;
        _text.Clear();
        _text.Append(normalized);
        Version++;
        RebuildLines();
        // A wholesale assignment is not an edit: it is unrecorded, drops the history, and so has no
        // undo to hand the old text to. Materializing the whole document for it would cost the most
        // and serve nobody.
        Changed?.Invoke(new TextChange(0, previousLength, normalized.Length));
    }

    public void Insert(int offset, string text) => Replace(offset, 0, text);

    public void Remove(int offset, int length) => Replace(offset, length, string.Empty);

    public void Replace(int offset, int length, string? text)
    {
        ValidateRange(offset, length);
        string normalized = Normalize(text);
        if (length == 0 && normalized.Length == 0)
        {
            return;
        }
        if (length == normalized.Length && RangeEquals(offset, normalized))
        {
            return;
        }

        var startLine = _lines.GetByOffset(offset, _text.Length);
        var endLine = _lines.GetByOffset(offset + length, _text.Length);
        bool changesLineStructure = normalized.AsSpan().IndexOfAny('\r', '\n') >= 0 ||
            offset + length > startLine.Offset + startLine.Length;
        int affectedStart = startLine.Offset;
        int affectedEnd = endLine.Offset + endLine.Length + endLine.DelimiterLength;
        bool hasFollowingLine = endLine.LineNumber + 1 < _lines.Count;

        // The only point the removed text still exists at. Pure insertions skip it entirely.
        string removed = length == 0 ? string.Empty : _text.ToString(offset, length);
        _text.Remove(offset, length);
        _text.Insert(offset, normalized);
        if (!changesLineStructure)
        {
            _lines.SetLineLength(startLine.LineNumber, startLine.Length - length + normalized.Length);
        }
        else
        {
            int affectedLength = affectedEnd - affectedStart - length + normalized.Length;
            string affectedText = _text.ToString(affectedStart, affectedLength);
            var replacement = ParseLines(affectedText, includeFinalLine: !hasFollowingLine);
            _lines.ReplaceRange(
                startLine.LineNumber,
                endLine.LineNumber - startLine.LineNumber + 1,
                replacement);
        }
        Version++;
        Changed?.Invoke(new TextChange(offset, length, normalized.Length, removed));
    }

    /// <summary>Terminator text at <paramref name="offset"/>. Length one is either return or feed.</summary>
    private string GetDelimiter(int offset, int delimiterLength) => delimiterLength switch
    {
        0 => string.Empty,
        2 => "\r\n",
        _ => _text[offset] == '\r' ? "\r" : "\n"
    };

    private void RebuildLines()
    {
        var lines = new List<EditableLineRecord>();
        int start = 0;
        for (int index = 0; index < _text.Length; index++)
        {
            char value = _text[index];
            if (value != '\r' && value != '\n')
            {
                continue;
            }
            int delimiterLength = value == '\r' && index + 1 < _text.Length && _text[index + 1] == '\n' ? 2 : 1;
            lines.Add(new EditableLineRecord(index - start, delimiterLength));
            index += delimiterLength - 1;
            start = index + 1;
        }
        lines.Add(new EditableLineRecord(_text.Length - start, 0));
        _lines.Reset(lines);
    }

    private static List<EditableLineRecord> ParseLines(string text, bool includeFinalLine)
    {
        var lines = new List<EditableLineRecord>();
        int start = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char value = text[index];
            if (value != '\r' && value != '\n')
            {
                continue;
            }
            int delimiterLength = value == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
            lines.Add(new EditableLineRecord(index - start, delimiterLength));
            index += delimiterLength - 1;
            start = index + 1;
        }
        if (includeFinalLine || start < text.Length)
        {
            lines.Add(new EditableLineRecord(text.Length - start, 0));
        }
        if (lines.Count == 0)
        {
            lines.Add(new EditableLineRecord(0, 0));
        }
        return lines;
    }

    private void ValidateRange(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > _text.Length - length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    private bool ContentEquals(string value)
    {
        if (_text.Length != value.Length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (_text[index] != value[index])
            {
                return false;
            }
        }

        return true;
    }

    private bool RangeEquals(int offset, string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (_text[offset + index] != value[index])
            {
                return false;
            }
        }
        return true;
    }

    internal static string NormalizeNewLines(string text)
        => text.IndexOf('\r') < 0
            ? text
            : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private sealed class EditableDocumentLine(int lineNumber, int offset, int length, string delimiter)
        : IReadOnlyDocumentLine
    {
        public int LineNumber { get; } = lineNumber;
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        public string Delimiter { get; } = delimiter;
        public int TotalLength => Length + Delimiter.Length;
    }
}
