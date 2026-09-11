using Aprillz.MewUI.Controls;

using System.Runtime.CompilerServices;

namespace Aprillz.MewUI;

/// <summary>
/// Opaque command-resolution origin: an element (resolved through its context chain) or a
/// standalone <see cref="CommandScope"/>. Compares by reference identity of the captured origin.
/// </summary>
public readonly struct CommandTarget : IEquatable<CommandTarget>
{
    private readonly object? _origin;

    private CommandTarget(object origin) => _origin = origin;

    /// <summary>
    /// Gets whether this target captures no origin.
    /// </summary>
    public bool IsEmpty => _origin == null;

    /// <summary>
    /// Creates a target that resolves commands starting at the given element's context chain.
    /// </summary>
    public static CommandTarget From(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new CommandTarget(element);
    }

    /// <summary>
    /// Creates a target that resolves commands against the given scope and its explicit parents.
    /// </summary>
    public static CommandTarget From(CommandScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new CommandTarget(scope);
    }

    internal object? Origin => _origin;

    internal Element? OriginElement => _origin as Element;

    public bool Equals(CommandTarget other) => ReferenceEquals(_origin, other._origin);

    public override bool Equals(object? obj) => obj is CommandTarget other && Equals(other);

    public override int GetHashCode() => _origin != null ? RuntimeHelpers.GetHashCode(_origin) : 0;

    public static bool operator ==(CommandTarget left, CommandTarget right) => left.Equals(right);

    public static bool operator !=(CommandTarget left, CommandTarget right) => !left.Equals(right);
}
