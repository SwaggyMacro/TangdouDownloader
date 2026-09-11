namespace Aprillz.MewUI;

/// <summary>
/// Owner of a <see cref="PropertyValueStore"/>. Receives invalidation callbacks when property values change.
/// <para>
/// <see cref="Controls.UIElement"/> implements this with <c>InvalidateVisual</c>/<c>InvalidateLayout</c>.
/// Future <c>Brush</c> implements with a version counter.
/// </para>
/// </summary>
internal interface IPropertyOwner
{
    /// <summary>
    /// Called by <see cref="PropertyValueStore"/> when a property value changes (animation tick or snap).
    /// </summary>
    /// <param name="property">The property that changed.</param>
    /// <param name="oldValue">The previous effective value (boxed).</param>
    /// <param name="newValue">The new effective value (boxed).</param>
    void OnPropertyChanged(MewProperty property, object? oldValue, object? newValue);
}
