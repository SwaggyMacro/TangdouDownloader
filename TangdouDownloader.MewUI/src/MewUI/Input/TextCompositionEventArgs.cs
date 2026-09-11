namespace Aprillz.MewUI;

/// <summary>
/// Per-character composition attribute from the IME.
/// </summary>
public enum CompositionAttr : byte
{
    /// <summary>Character being input (unconverted). Typically shown with a dashed underline.</summary>
    Input = 0,
    /// <summary>Character in the active conversion clause. Typically shown with a thick solid underline.</summary>
    TargetConverted = 1,
    /// <summary>Character already converted but not in the active clause. Typically shown with a thin solid underline.</summary>
    Converted = 2,
    /// <summary>Character in the active clause but not yet converted. Typically shown with a thick dashed underline.</summary>
    TargetNotConverted = 3,
    /// <summary>Input error. Typically shown with a wavy underline.</summary>
    InputError = 4,
}

/// <summary>
/// Arguments for text composition (IME pre-edit) events.
/// </summary>
public sealed class TextCompositionEventArgs
{
    /// <summary>
    /// Gets the current composition text.
    /// For Start/End events this may be empty.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Per-character composition attributes. Length matches <see cref="Text"/> when available.
    /// <see langword="null"/> when the IME does not provide attribute data.
    /// </summary>
    public CompositionAttr[]? Attributes { get; }

    /// <summary>
    /// Gets or sets whether the event has been handled.
    /// </summary>
    public bool Handled { get; set; }

    public TextCompositionEventArgs(string? text = null, CompositionAttr[]? attributes = null)
    {
        Text = text ?? string.Empty;
        Attributes = attributes;
    }
}
