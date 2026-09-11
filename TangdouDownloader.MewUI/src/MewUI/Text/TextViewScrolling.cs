namespace Aprillz.MewUI.Text;

/// <summary>Scroll arithmetic shared by the text hosts.</summary>
internal static class TextViewScrolling
{
    /// <summary>
    /// Offset that brings <paramref name="start"/>..<paramref name="length"/> into a viewport of
    /// <paramref name="viewportLength"/>. Content longer than the viewport centres instead of
    /// sticking to one edge, so a request larger than the view does not hide its middle.
    /// </summary>
    public static double ResolveOffset(double offset, double viewportLength, double start, double length)
    {
        if (viewportLength <= 0)
        {
            return offset;
        }

        double end = start + length;
        if (start < offset)
        {
            return end > offset + viewportLength ? start + length / 2 : start;
        }
        if (end > offset + viewportLength)
        {
            return end - viewportLength;
        }
        return offset;
    }
}
