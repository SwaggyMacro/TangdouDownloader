namespace Aprillz.MewUI.Input;

/// <summary>
/// Platform IME seam over an editable text surface: caret/selection/document access and
/// undo-preserving composition commit beyond what <see cref="ITextCompositionClient"/> exposes.
/// </summary>
// Internal so the legacy and rebuilt text input hierarchies can both satisfy the platform
// backends without widening the public composition contract.
internal interface ITextCompositionEditor : ITextCompositionClient
{
    /// <summary>
    /// Gets the current caret index in document coordinates.
    /// </summary>
    int CaretPosition { get; }

    /// <summary>
    /// Gets the length of the active composition run.
    /// </summary>
    int CompositionLength { get; }

    /// <summary>
    /// Gets the current selection endpoints in document coordinates (may be unordered).
    /// </summary>
    (int Start, int End) SelectionRange { get; }

    /// <summary>
    /// Aligns caret/selection to a platform-provided replacement range before composition text is applied.
    /// </summary>
    void SetSelectionRangeForPlatform(int start, int end);

    /// <summary>
    /// Gets the document length in UTF-16 code units.
    /// </summary>
    int TextLength { get; }

    /// <summary>
    /// Returns a substring of the document.
    /// </summary>
    string GetTextSubstring(int start, int length);

    /// <summary>
    /// Commits the active composition into the document, preserving it in undo history.
    /// </summary>
    void CommitActiveComposition();
}
