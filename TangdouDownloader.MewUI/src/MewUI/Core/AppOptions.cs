namespace Aprillz.MewUI;

/// <summary>
/// Application-wide defaults applied by <see cref="ApplicationBuilder"/> before starting the message loop.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Gets or sets the default theme mode.
    /// </summary>
    public ThemeVariant? ThemeMode { get; set; }

    /// <summary>
    /// Gets or sets the default accent (built-in).
    /// </summary>
    public Accent? Accent { get; set; }

    /// <summary>
    /// Gets or sets the default accent as a custom color.
    /// Takes precedence over <see cref="Accent"/> when set.
    /// </summary>
    public Color? AccentColor { get; set; }

    /// <summary>
    /// Gets or sets the light theme seed palette.
    /// </summary>
    public ThemeSeed? LightSeed { get; set; }

    /// <summary>
    /// Gets or sets the dark theme seed palette.
    /// </summary>
    public ThemeSeed? DarkSeed { get; set; }

    /// <summary>
    /// Gets or sets theme metric overrides.
    /// </summary>
    public ThemeMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets when the run loop ends automatically as windows close. Applied to
    /// <see cref="Application.ShutdownMode"/> at run start; leave null to keep the default.
    /// </summary>
    public ShutdownMode? ShutdownMode { get; set; }
}
