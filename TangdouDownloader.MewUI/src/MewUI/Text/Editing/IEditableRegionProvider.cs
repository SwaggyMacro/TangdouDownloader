namespace Aprillz.MewUI.Text.Editing;

/// <summary>
/// Decides which parts of a document an edit may touch. Deletion answers with ranges rather than a
/// flag because a range can be partly editable, and the editable parts still have to go.
/// </summary>
public interface IEditableRegionProvider
{
    /// <summary>Whether text may be inserted at <paramref name="offset"/>.</summary>
    bool CanInsert(int offset);

    /// <summary>Writes the parts of <paramref name="range"/> that may be deleted, in document order.</summary>
    void GetDeletableRanges(TextRange range, IList<TextRange> output);
}
