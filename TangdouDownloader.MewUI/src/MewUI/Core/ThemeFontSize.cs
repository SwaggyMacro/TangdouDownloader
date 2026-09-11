namespace Aprillz.MewUI;

/// <summary>
/// Steps of the theme's font size scale, resolved against <see cref="ThemeMetrics"/> by the
/// <c>FontSize</c> markup extensions.
/// </summary>
public enum ThemeFontSize
{
    /// <summary>One step below the default; captions and secondary text.</summary>
    Small,

    /// <summary>The control default (<see cref="ThemeMetrics.FontSize"/>).</summary>
    Default,

    /// <summary>One step above the default; emphasized body text.</summary>
    Medium,

    /// <summary>Section headings.</summary>
    Large,

    /// <summary>Page titles.</summary>
    ExtraLarge
}
