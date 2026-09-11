using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Fluent API extensions for <see cref="GlyphElement"/>.
/// </summary>
public static class GlyphExtensions
{
    /// <summary>
    /// Sets the glyph kind.
    /// </summary>
    /// <param name="element">Target glyph element.</param>
    /// <param name="kind">Glyph kind.</param>
    /// <returns>The element for chaining.</returns>
    public static GlyphElement Kind(this GlyphElement element, GlyphKind kind)
    {
        element.Kind = kind;
        return element;
    }

    /// <summary>
    /// Sets the glyph size.
    /// </summary>
    /// <param name="element">Target glyph element.</param>
    /// <param name="size">Glyph size in DIPs.</param>
    /// <returns>The element for chaining.</returns>
    public static GlyphElement GlyphSize(this GlyphElement element, double size)
    {
        element.GlyphSize = size;
        return element;
    }

    /// <summary>
    /// Sets the stroke thickness.
    /// </summary>
    /// <param name="element">Target glyph element.</param>
    /// <param name="thickness">Stroke thickness.</param>
    /// <returns>The element for chaining.</returns>
    public static GlyphElement StrokeThickness(this GlyphElement element, double thickness)
    {
        element.StrokeThickness = thickness;
        return element;
    }

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    /// <param name="element">Target glyph element.</param>
    /// <param name="color">Foreground color.</param>
    /// <returns>The element for chaining.</returns>
    public static GlyphElement Foreground(this GlyphElement element, Color color)
    {
        element.Foreground = color;
        return element;
    }
}
