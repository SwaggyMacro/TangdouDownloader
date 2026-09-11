namespace Aprillz.MewUI;

/// <summary>
/// Describes a binding validation error associated with a target property on a control.
/// </summary>
/// <param name="Property">The target property whose binding reported the validation error.</param>
/// <param name="Message">The error message suitable for validation UI and accessibility output.</param>
public sealed record ValidationError(MewProperty Property, string Message);
