namespace Aprillz.MewUI;

/// <summary>
/// Specifies the direction of data flow in a binding.
/// </summary>
public enum BindingMode
{
    /// <summary>Source → Control only.</summary>
    OneWay,

    /// <summary>Source ↔ Control (bidirectional).</summary>
    TwoWay,
}
