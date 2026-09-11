namespace Aprillz.MewUI;

/// <summary>
/// Image scaling / sampling quality hint for drawing bitmaps.
/// </summary>
public enum ImageScaleQuality
{
    /// <summary>
    /// Backend default.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Nearest-neighbor sampling (pixelated, sharp).
    /// </summary>
    Fast = 1,

    /// <summary>
    /// Linear sampling.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Best quality available (backend-dependent).
    /// </summary>
    HighQuality = 3,
}
