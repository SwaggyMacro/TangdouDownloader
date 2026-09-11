using Aprillz.MewUI.Rendering;
using Aprillz.MewUI.Text;

namespace Aprillz.MewUI.Controls;

/// <summary>Bounded retained-layout set for menu captions and shortcut labels.</summary>
internal sealed class MenuTextLayouts
{
    private const int MaxEntries = 256;
    private readonly Dictionary<LayoutKey, ITextLayout> _layouts = [];

    public void Invalidate() => _layouts.Clear();

    public Size Measure(
        IGraphicsFactory factory,
        string text,
        uint dpi,
        in TextRunStyle style,
        TextAlignment alignment = TextAlignment.Left)
        => string.IsNullOrEmpty(text)
            ? Size.Empty
            : GetOrCreate(factory, text, dpi, in style, double.PositiveInfinity, double.PositiveInfinity, alignment)!.MeasuredSize;

    public ITextLayout? GetOrCreate(
        IGraphicsFactory factory,
        string text,
        uint dpi,
        in TextRunStyle style,
        double width,
        double height,
        TextAlignment alignment = TextAlignment.Left)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var key = new LayoutKey(
            text,
            dpi,
            style,
            NormalizeForKey(width),
            NormalizeForKey(height),
            alignment);
        if (_layouts.TryGetValue(key, out var layout))
        {
            return layout;
        }

        if (_layouts.Count >= MaxEntries)
        {
            _layouts.Clear();
        }

        layout = TextLayoutOperations.GetOrCreate(
            factory,
            text,
            dpi,
            in style,
            width,
            height,
            alignment: alignment);
        _layouts.Add(key, layout);
        return layout;
    }

    public static void Draw(
        IGraphicsContext context,
        ITextLayout layout,
        Rect bounds,
        Color color,
        bool showAccessKey = false,
        int accessKeyIndex = -1)
    {
        ReadOnlyMemory<TextPaintSpan> spans = default;
        if (showAccessKey && accessKeyIndex >= 0)
        {
            spans = new[]
            {
                new TextPaintSpan(
                    new TextRange(accessKeyIndex, 1),
                    Decoration: TextDecoration.Underline)
            };
        }

        TextLayoutOperations.DrawInBounds(
            context,
            layout,
            bounds,
            color,
            TextAlignment.Center,
            paintSpans: spans);
    }

    private static double NormalizeForKey(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return double.IsPositiveInfinity(value) ? double.PositiveInfinity : Math.Round(value, 3);
    }

    private readonly record struct LayoutKey(
        string Text,
        uint Dpi,
        TextRunStyle Style,
        double Width,
        double Height,
        TextAlignment Alignment);
}
