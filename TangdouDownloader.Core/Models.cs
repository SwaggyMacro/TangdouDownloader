namespace TangdouDownloader.Core;

public sealed record VideoInfo(string Vid, string Title, IReadOnlyDictionary<string, string> Urls, string? CoverUrl = null)
{
    // Quality preference, highest first. Availability is only known after resolving,
    // so the quality picker in the UI is decorative; the service always requests the
    // best quality the video actually has.
    private static readonly string[] QualityPreference = ["H1080P", "V1080P", "H720P", "V720P", "H540P", "V540P", "H360P", "V360P"];

    public string SelectUrl(string quality)
    {
        foreach (var key in QualityPreference)
            if (Urls.TryGetValue(key, out var selected)) return selected;
        return Urls.Values.FirstOrDefault() ?? string.Empty;
    }

    public string BestQualityLabel()
    {
        foreach (var key in QualityPreference)
            if (Urls.ContainsKey(key)) return key.TrimStart('H', 'V');
        return "默认";
    }
}

public sealed record DownloadProgress(long BytesReceived, long TotalBytes)
{
    public int Percentage => TotalBytes > 0 ? (int)Math.Clamp(BytesReceived * 100L / TotalBytes, 0, 100) : 0;
}

public sealed record DownloadResult(string FilePath, long BytesReceived, bool UsedSingleThreadFallback = false);
