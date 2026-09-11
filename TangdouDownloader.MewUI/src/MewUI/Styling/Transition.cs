namespace Aprillz.MewUI;

/// <summary>
/// Declares that a property should animate when its style-resolved value changes.
/// Attached to <see cref="Style.Transitions"/>.
/// </summary>
public sealed class Transition
{
    /// <summary>Gets the property to animate.</summary>
    public MewProperty Property { get; }

    /// <summary>Gets the animation duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the easing function.</summary>
    public Func<double, double> Easing { get; }

    public Transition(MewProperty property, TimeSpan duration, Func<double, double>? easing = null)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration), duration, "Transition duration must be greater than zero.");
        }

        Property = property;
        Duration = duration;
        Easing = easing ?? Animation.Easing.Default;
    }

    /// <summary>
    /// Convenience factory with millisecond duration.
    /// </summary>
    public static Transition Create(MewProperty property, int durationMs = 200, Func<double, double>? easing = null)
        => new(property, TimeSpan.FromMilliseconds(durationMs), easing);
}
