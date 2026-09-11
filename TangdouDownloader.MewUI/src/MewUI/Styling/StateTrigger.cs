namespace Aprillz.MewUI;

/// <summary>
/// Conditionally applies <see cref="SetterBase"/> values when the control's
/// <see cref="VisualStateFlags"/> match the specified criteria.
/// </summary>
public sealed class StateTrigger
{
    private IReadOnlyList<SetterBase>? _setters;

    /// <summary>
    /// Flags that must ALL be present for this trigger to match.
    /// Use <see cref="VisualStateFlags.None"/> when only <see cref="Exclude"/> matters.
    /// </summary>
    public VisualStateFlags Match { get; init; }

    /// <summary>
    /// Flags that must ALL be absent for this trigger to match.
    /// Common pattern: <c>Exclude = Enabled</c> to match disabled state.
    /// </summary>
    public VisualStateFlags Exclude { get; init; }

    /// <summary>
    /// Setter values to apply when this trigger matches.
    /// May contain <see cref="Setter"/> and <see cref="UnsetSetter"/> declarations.
    /// </summary>
    public required IReadOnlyList<SetterBase> Setters
    {
        get => _setters!;
        init => _setters = value;
    }

    /// <summary>
    /// Tests whether this trigger matches the given flags.
    /// </summary>
    public bool Matches(VisualStateFlags flags)
        => (flags & Match) == Match && (flags & Exclude) == 0;

    internal SetterBase[] SnapshotSetters()
    {
        if (_setters == null)
        {
            throw new InvalidOperationException("StateTrigger.Setters cannot be null.");
        }

        var snapshot = _setters.ToArray();
        _setters = snapshot;
        return snapshot;
    }
}

/// <summary>
/// Framework-defined visual state flags.
/// Public because <see cref="StateTrigger"/> (in Style definitions) references this type.
/// </summary>
[Flags]
public enum VisualStateFlags : uint
{
    None = 0,

    // Tier 1 - common (all Controls)
    /// <summary>Control is effectively enabled.</summary>
    Enabled = 1 << 0,
    /// <summary>Mouse is over or captured.</summary>
    Hot = 1 << 1,
    /// <summary>Control has focus or contains focused element.</summary>
    Focused = 1 << 2,
    /// <summary>Mouse button or activation key is held down.</summary>
    Pressed = 1 << 3,

    // Tier 2 - toggle (ToggleBase family)
    /// <summary>Toggle is in the on/checked state.</summary>
    Checked = 1 << 4,
    /// <summary>CheckBox three-state null value.</summary>
    Indeterminate = 1 << 5,

    // Tier 3 - control-specific opt-in
    /// <summary>Sub-element is open/active (dropdown, expander).</summary>
    Active = 1 << 6,
    /// <summary>Item is selected (tab, list item).</summary>
    Selected = 1 << 7,
    /// <summary>Input is read-only.</summary>
    ReadOnly = 1 << 8,

    // Tier 4 - semantic state projected from framework services
    /// <summary>One or more bindings on the control currently have an error.</summary>
    Invalid = 1 << 9,

    // Bits 10-31: reserved for future framework extension
}
