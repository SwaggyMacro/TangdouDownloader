namespace Aprillz.MewUI.Text;

public interface ITextSource
{
    int TextLength { get; }
    long Version { get; }
    char GetCharAt(int offset);
    string GetText(int offset, int length);
}

public interface IReadOnlyDocumentLine
{
    int LineNumber { get; }
    int Offset { get; }
    int Length { get; }
    int TotalLength { get; }
    string Delimiter { get; }
}

public interface IReadOnlyTextDocument : ITextSource
{
    int LineCount { get; }
    IReadOnlyDocumentLine GetLineByNumber(int lineNumber);
    IReadOnlyDocumentLine GetLineByOffset(int offset);
    int GetOffset(int line, int column);
    TextLocation GetLocation(int offset);
}

public readonly record struct TextLocation(int Line, int Column);

public readonly record struct LogicalTextLine(
    int LineNumber,
    int Offset,
    int Length,
    int TotalLength);

public sealed class VisualTextLine
{
    internal VisualTextLine(
        int logicalStart,
        int logicalLength,
        int visualRow,
        Rect bounds,
        double baseline,
        ITextLayout layout,
        int layoutLineIndex)
    {
        LogicalStart = logicalStart;
        LogicalLength = logicalLength;
        VisualRow = visualRow;
        Bounds = bounds;
        Baseline = baseline;
        Layout = layout;
        LayoutLineIndex = layoutLineIndex;
    }

    public int LogicalStart { get; }
    public int LogicalLength { get; }
    public int VisualRow { get; }
    public Rect Bounds { get; internal set; }
    public double Baseline { get; }
    public ITextLayout Layout { get; }
    public int LayoutLineIndex { get; }
}

public sealed class TextLineLayout
{
    private readonly ITextLayout _layout;
    private readonly List<VisualTextLine> _visualLines;

    internal TextLineLayout(
        LogicalTextLine logicalLine,
        ITextLayout layout,
        double documentX,
        double documentY,
        ITextOffsetMap offsetMap,
        IReadOnlyList<TextPaintSpan> paintSpans,
        int visualRowOffset = 0)
    {
        LogicalLine = logicalLine;
        _layout = layout;
        OffsetMap = offsetMap;
        PaintSpans = paintSpans;
        DocumentX = documentX;
        DocumentY = documentY;
        _visualLines = new List<VisualTextLine>(layout.Lines.Count);
        for (int i = 0; i < layout.Lines.Count; i++)
        {
            var line = layout.Lines[i];
            _visualLines.Add(new VisualTextLine(
                line.TextStart,
                line.TextLength,
                visualRowOffset + i,
                new Rect(documentX + line.Bounds.X, documentY + line.Bounds.Y, line.Bounds.Width, line.Bounds.Height),
                line.Baseline,
                layout,
                i));
        }
    }

    public LogicalTextLine LogicalLine { get; }
    public IReadOnlyList<VisualTextLine> VisualLines => _visualLines;
    public double Height => _layout.ContentHeight;
    public ITextOffsetMap OffsetMap { get; }
    public IReadOnlyList<TextPaintSpan> PaintSpans { get; }
    public double DocumentX { get; private set; }
    public double DocumentY { get; private set; }

    public CharacterHit HitTest(Point lineLocalPoint) => _layout.HitTestPoint(lineLocalPoint);

    public Rect GetCaretBounds(CharacterHit hit) => _layout.GetCaretBounds(hit);

    public void GetRangeBounds(TextRange range, IList<Rect> output)
        => _layout.GetRangeBounds(range.Start, range.Length, output);

    internal CharacterHit HitTestDocument(Point documentPoint)
        => _layout.HitTestPoint(new Point(documentPoint.X - DocumentX, documentPoint.Y));

    internal Rect GetDocumentCaretBounds(CharacterHit hit)
    {
        var bounds = _layout.GetCaretBounds(hit);
        return new Rect(DocumentX + bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    /// <summary>Draws the whole line. Hosts that interleave layers call the individual passes instead.</summary>
    public void Draw(ITextRenderContext context, Point origin, in TextDrawOptions options)
    {
        DrawBackground(context, origin, in options);
        DrawForeground(context, origin, in options);
    }

    /// <summary>Paint-span backgrounds of this line.</summary>
    public void DrawBackground(ITextRenderContext context, Point origin, in TextDrawOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.DrawBackground(_layout, new Point(origin.X + DocumentX, origin.Y), Combine(in options));
    }

    /// <summary>Glyphs of this line.</summary>
    public void DrawForeground(ITextRenderContext context, Point origin, in TextDrawOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.DrawForeground(_layout, new Point(origin.X + DocumentX, origin.Y), Combine(in options));
    }

    private TextDrawOptions Combine(in TextDrawOptions options)
    {
        TextDrawOptions effective = options;
        if (PaintSpans.Count > 0)
        {
            if (options.PaintSpans.IsEmpty)
            {
                effective = options with { PaintSpans = PaintSpans.ToArray() };
            }
            else
            {
                var combined = new TextPaintSpan[PaintSpans.Count + options.PaintSpans.Length];
                for (int i = 0; i < PaintSpans.Count; i++)
                {
                    combined[i] = PaintSpans[i];
                }
                options.PaintSpans.Span.CopyTo(combined.AsSpan(PaintSpans.Count));
                effective = options with { PaintSpans = combined };
            }
        }
        return effective;
    }

    public int MapProjectedOffsetToSource(int projectedOffset)
        => OffsetMap.MapToSource(projectedOffset);

    public int MapSourceOffsetToProjected(int sourceOffset)
        => OffsetMap.MapFromSource(sourceOffset);

    internal void SetDocumentPosition(double documentX, double documentY)
    {
        DocumentX = documentX;
        DocumentY = documentY;
        for (int i = 0; i < _visualLines.Count; i++)
        {
            var source = _layout.Lines[i].Bounds;
            _visualLines[i].Bounds = new Rect(
                documentX + source.X,
                documentY + source.Y,
                source.Width,
                source.Height);
        }
    }
}

public readonly record struct TextViewport(
    double Width,
    double Height,
    double HorizontalOffset = 0,
    double VerticalOffset = 0)
{
    public Rect DocumentBounds => new(HorizontalOffset, VerticalOffset, Width, Height);
}

/// <summary>
/// A replace that happened to a document. <see cref="RemovedText"/> carries what an edit took out;
/// assigning a control's text property replaces the document wholesale rather than editing it, and
/// reports no removed text even though <see cref="RemovedLength"/> counts it.
/// </summary>
public readonly record struct TextChange(
    int Offset,
    int RemovedLength,
    int InsertedLength,
    string RemovedText)
{
    public TextChange(int offset, int removedLength, int insertedLength)
        : this(offset, removedLength, insertedLength, string.Empty)
    {
    }
}

/// <summary>
/// A point resolved against the view. <paramref name="LineNumber"/> is the line that was laid out,
/// which can cover more than its own logical line, so the document line is the one holding
/// <paramref name="DocumentOffset"/>.
/// </summary>
public readonly record struct TextViewHit(
    int DocumentOffset,
    int LineNumber,
    int VisualRow,
    CharacterHit LineHit);

public interface ITextViewLayout : IDisposable
{
    TextViewport Viewport { get; }
    IReadOnlyList<TextLineLayout> MaterializedLines { get; }
    double ExtentWidth { get; }
    double ExtentHeight { get; }
    void SetViewport(TextViewport viewport);
    void Invalidate(TextChange change);
    TextViewHit HitTest(Point viewportPoint);
    Rect GetCaretBounds(int documentOffset);
    TextLineLayout? GetLineLayout(int documentOffset);
}
