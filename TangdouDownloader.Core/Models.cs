namespace TangdouDownloader.Core;

public sealed record VideoInfo(string Vid, string Title, IReadOnlyDictionary<string, string> Urls, string? CoverUrl = null)
{
    public string SelectUrl(string quality)
    {
        var preferred = quality switch
        {
            "1080P" or "最高" => "H1080P",
            "720P" or "中等" => "H720P",
            "540P" => "H540P",
            "360P" or "最低" => "H360P",
            _ => "H1080P"
        };
        if (Urls.TryGetValue(preferred, out var selected)) return selected;
        foreach (var key in new[] { "H1080P", "V1080P", "H720P", "V720P", "H540P", "V540P", "H360P", "V360P" })
            if (Urls.TryGetValue(key, out selected)) return selected;
        return Urls.Values.FirstOrDefault() ?? string.Empty;
    }
}

public sealed record DownloadProgress(long BytesReceived, long TotalBytes)
{
    public int Percentage => TotalBytes > 0 ? (int)Math.Clamp(BytesReceived * 100L / TotalBytes, 0, 100) : 0;
}

public sealed record DownloadResult(string FilePath, long BytesReceived, bool UsedSingleThreadFallback = false);
