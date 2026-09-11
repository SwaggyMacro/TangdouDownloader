namespace Aprillz.MewUI.Input;

/// <summary>
/// Controls IME (Input Method Editor) behavior for text input controls.
/// </summary>
public enum ImeMode
{
    /// <summary>
    /// Default IME behavior. IME is active and responds to user input.
    /// </summary>
    Auto,

    /// <summary>
    /// IME is disabled. No composition is possible. Use for password fields, numeric inputs, etc.
    /// </summary>
    Disabled,

    /// <summary>
    /// Forces alphanumeric (direct input) mode. IME remains associated but does not compose.
    /// </summary>
    AlphaNumeric,
}
