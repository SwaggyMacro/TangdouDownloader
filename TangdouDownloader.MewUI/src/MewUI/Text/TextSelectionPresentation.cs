namespace Aprillz.MewUI.Text;

/// <summary>Converts a document selection into paint-only spans for a laid-out line.</summary>
public static class TextSelectionPresentation
{
    /// <summary>
    /// The span covering the part of the selection that falls on this line, in the line's own
    /// coordinates. False when none of it does.
    /// </summary>
    public static bool TryCreateSpan(
        TextLineLayout line,
        TextRange documentSelection,
        Color foreground,
        Color background,
        out TextPaintSpan span)
    {
        ArgumentNullException.ThrowIfNull(line);
        var logical = line.LogicalLine;
        int lineStart = logical.Offset;
        int lineEnd = lineStart + logical.Length;
        int start = Math.Max(documentSelection.Start, lineStart);
        int end = Math.Min(documentSelection.End, lineEnd);
        if (end <= start)
        {
            span = default;
            return false;
        }

        // Paint spans address the laid-out text, which a projection can make longer or shorter than
        // the document text. Taking the document offsets straight would misplace the highlight by
        // whatever the projection moved them, while the caret, which is mapped, stayed right.
        int projectedStart = line.MapSourceOffsetToProjected(start - lineStart);
        int projectedEnd = line.MapSourceOffsetToProjected(end - lineStart);
        if (projectedEnd <= projectedStart)
        {
            span = default;
            return false;
        }
        span = new TextPaintSpan(
            new TextRange(projectedStart, projectedEnd - projectedStart),
            foreground,
            background);
        return true;
    }
}
