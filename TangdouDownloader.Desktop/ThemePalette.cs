using Aprillz.MewUI;

namespace TangdouDownloader.Desktop;

internal static class ThemePalette
{
    public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);
    public static readonly Color White = Color.FromRgb(255, 255, 255);
    public static readonly Color Soft = Color.FromArgb(24, 128, 128, 128);
    public static readonly Color Accent = Color.FromRgb(0x33, 0x70, 0xFF);
    public static readonly Color Success = Color.FromRgb(0x00, 0xB4, 0x2A);
    public static readonly Color Warning = Color.FromRgb(0xFF, 0x7D, 0x00);
    public static readonly Color Danger = Color.FromRgb(0xF5, 0x3F, 0x3F);
    public static readonly Color Neutral = Color.FromRgb(0x4E, 0x50, 0x58);
    public static readonly Color TextSecondary = Color.FromRgb(0x9C, 0xA3, 0xAF);

    public static Color AccentSoft => Soften(Accent);
    public static Color SuccessSoft => Soften(Success);
    public static Color WarningSoft => Soften(Warning);
    public static Color DangerSoft => Soften(Danger);

    private static Color Soften(Color color) => color.WithAlpha(28);
}
