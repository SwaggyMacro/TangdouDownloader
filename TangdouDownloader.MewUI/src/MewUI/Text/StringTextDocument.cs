namespace Aprillz.MewUI.Text;

/// <summary>Immutable string-backed document for read-only text view consumers.</summary>
public sealed class StringTextDocument : IReadOnlyTextDocument
{
    private readonly string _text;
    private readonly StringDocumentLine[] _lines;

    public StringTextDocument(string? text, long version = 0)
    {
        _text = NormalizeNewLines(text ?? string.Empty);
        Version = version;
        var lines = new List<StringDocumentLine>();
        int start = 0;
        int lineNumber = 0;
        for (int index = 0; index < _text.Length; index++)
        {
            if (_text[index] != '\n') continue;
            lines.Add(new StringDocumentLine(lineNumber++, start, index - start, 1));
            start = index + 1;
        }
        lines.Add(new StringDocumentLine(lineNumber, start, _text.Length - start, 0));
        _lines = lines.ToArray();
    }

    public int TextLength => _text.Length;
    public long Version { get; }
    public int LineCount => _lines.Length;
    public char GetCharAt(int offset) => _text[offset];

    public string GetText(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > _text.Length - length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        return _text.Substring(offset, length);
    }

    public IReadOnlyDocumentLine GetLineByNumber(int lineNumber)
        => (uint)lineNumber < (uint)_lines.Length
            ? _lines[lineNumber]
            : throw new ArgumentOutOfRangeException(nameof(lineNumber));

    public IReadOnlyDocumentLine GetLineByOffset(int offset)
    {
        if (offset < 0 || offset > _text.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        int low = 0;
        int high = _lines.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (offset < _lines[middle].Offset) high = middle - 1;
            else if (middle + 1 < _lines.Length && offset >= _lines[middle + 1].Offset) low = middle + 1;
            else return _lines[middle];
        }
        return _lines[^1];
    }

    public int GetOffset(int line, int column)
    {
        var source = (StringDocumentLine)GetLineByNumber(line);
        if (column < 0 || column > source.Length) throw new ArgumentOutOfRangeException(nameof(column));
        return source.Offset + column;
    }

    public TextLocation GetLocation(int offset)
    {
        var line = (StringDocumentLine)GetLineByOffset(offset);
        return new TextLocation(line.LineNumber, Math.Min(offset - line.Offset, line.Length));
    }

    public override string ToString() => _text;

    private static string NormalizeNewLines(string text)
        => text.IndexOf('\r') < 0
            ? text
            : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private sealed class StringDocumentLine(int lineNumber, int offset, int length, int delimiterLength)
        : IReadOnlyDocumentLine
    {
        public int LineNumber { get; } = lineNumber;
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        public int TotalLength => Length + delimiterLength;
        public string Delimiter => delimiterLength == 0 ? string.Empty : "\n";
    }
}
