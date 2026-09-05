using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TangdouDownloader.Core;

public static class DownloaderUtils
{
    private static readonly Regex VidPattern = new(@"(?:^|[?&/])vid[=/](\d+)|\b(\d{8,})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractVids(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in VidPattern.Matches(input))
        {
            var vid = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (seen.Add(vid)) result.Add(vid);
        }

        if (result.Count == 0 && long.TryParse(input.Trim(), out _)) result.Add(input.Trim());
        return result;
    }

    public static string? GetVid(string? input)
    {
        return ExtractVids(input).FirstOrDefault();
    }

    public static bool IsVideo(string input) => !input.Contains("music", StringComparison.OrdinalIgnoreCase);

    public static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}

public interface IVideoResolver
{
    Task<VideoInfo> ResolveAsync(string input, CancellationToken cancellationToken = default);
}

public interface IVideoDownloader
{
    Task<DownloadResult> DownloadAsync(
        string url,
        string title,
        string directory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<DownloadResult> DownloadAsync(
        string url,
        string title,
        string directory,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken,
        int downloadThreads,
        Action? onSingleThreadFallback = null)
        => DownloadAsync(url, title, directory, progress, cancellationToken);
}

public sealed class TangdouVideoService : IVideoResolver, IDisposable
{
    private readonly HttpClient _client;
    private static readonly string[] Resolutions = ["H1080P", "V1080P", "H720P", "V720P", "H540P", "V540P", "H360P", "V360P"];

    public TangdouVideoService()
    {
        _client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    }

    public async Task<VideoInfo> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        var vid = DownloaderUtils.GetVid(input) ?? throw new ArgumentException("请输入有效的糖豆视频链接或 VID。");
        Exception? lastError = null;
        foreach (var endpoint in new[] { "https://api-h5.tangdou.com/sample/share/main?vid=", "https://api-h5.tangdou.com/mtangdou/video/play?vid=" })
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, endpoint + vid);
                using var response = await _client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
                if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) continue;
                // The main branch reads the video's title directly from data.title. Keep
                // that contract so nested author/name fields cannot replace the title.
                var title = GetDirectString(data, "title", "video_title", "videoTitle");
                if (string.IsNullOrWhiteSpace(title)) title = GetString(data, "title", "video_title", "videoTitle");
                title = title?.Trim();
                if (string.Equals(title, vid, StringComparison.OrdinalIgnoreCase)
                    || (title is not null && long.TryParse(title, out _))) title = null;
                var coverUrl = NormalizeMediaUrl(
                    GetDirectString(data, "cover", "cover_url", "cover_img", "share_img", "pic", "pic_url", "picurl", "image", "image_url", "thumbnail", "poster", "poster_url", "video_img")
                    ?? GetString(data, "cover", "cover_url", "cover_img", "share_img", "pic", "pic_url", "picurl", "image", "image_url", "thumbnail", "poster", "poster_url", "video_img"));
                var rawUrl = GetDirectString(data, "play_url", "video_url", "playUrl", "videoUrl")
                    ?? GetString(data, "play_url", "video_url", "playUrl", "videoUrl");
                if (string.IsNullOrWhiteSpace(rawUrl)) continue;
                var urls = await ProbeUrlsAsync(rawUrl, cancellationToken);
                if (urls.Count == 0) urls["默认"] = rawUrl;
                return new VideoInfo(vid, string.IsNullOrWhiteSpace(title) ? "未命名视频" : title!, urls, coverUrl);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                lastError = ex;
            }
        }
        throw new HttpRequestException($"无法解析视频 {vid}。", lastError);
    }

    private static string? GetString(JsonElement data, params string[] names)
    {
        if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in data.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var first = property.Value.EnumerateArray().FirstOrDefault(item => item.ValueKind == JsonValueKind.String);
                        if (first.ValueKind == JsonValueKind.String) return first.GetString();
                    }
                }

                var nested = GetString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in data.EnumerateArray())
            {
                var nested = GetString(value, names);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        return null;
    }

    private static string? GetDirectString(JsonElement data, params string[] names)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in data.EnumerateObject())
        {
            if (!names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase))) continue;
            if (property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var first = property.Value.EnumerateArray().FirstOrDefault(item => item.ValueKind == JsonValueKind.String);
                if (first.ValueKind == JsonValueKind.String) return first.GetString();
            }
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                return GetDirectString(property.Value, "url", "src", "path", "value");
            }
        }
        return null;
    }

    private static string? NormalizeMediaUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.StartsWith("//", StringComparison.Ordinal)) return "https:" + value;
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute)) return absolute.ToString();
        return Uri.TryCreate(new Uri("https://api-h5.tangdou.com/"), value, out var relative)
            ? relative.ToString()
            : null;
    }

    private async Task<Dictionary<string, string>> ProbeUrlsAsync(string rawUrl, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await Task.WhenAll(Resolutions.Select(async resolution =>
        {
            var candidate = Regex.Replace(rawUrl, @"_.[0-9]+P", "_" + resolution);
            try
            {
                using var request = CreateRequest(HttpMethod.Head, candidate);
                using var response = await _client.SendAsync(request, cancellationToken);
                // The original downloader treats every response except a concrete 404 as an
                // available quality. Some CDN nodes reject HEAD with a non-2xx status but
                // still serve the same URL to a GET request.
                if (response.StatusCode != HttpStatusCode.NotFound) lock (result) result[resolution] = candidate;
            }
            catch (HttpRequestException) { }
        }));
        return result;
    }

    public static HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7,zh-CN;q=0.6");
        request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.tangdoucdn.com/");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122 Safari/537.36");
        return request;
    }

    public void Dispose() => _client.Dispose();
}

public sealed class VideoDownloadService : IVideoDownloader
{
    private readonly HttpClient _client = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });

    public Task<DownloadResult> DownloadAsync(
        string url,
        string title,
        string directory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => DownloadAsync(url, title, directory, progress, cancellationToken, 2);

    public async Task<DownloadResult> DownloadAsync(
        string url,
        string title,
        string directory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default,
        int downloadThreads = 2,
        Action? onSingleThreadFallback = null)
    {
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, DownloaderUtils.SanitizeFileName(title) + ".mp4");
        var partialPath = filePath + ".part";

        // Preserve existing resumable downloads. Segment files are intentionally
        // removed on interruption, while the established .part format can resume.
        if (downloadThreads <= 1 || File.Exists(partialPath))
            return await DownloadSequentiallyAsync(url, filePath, partialPath, progress, cancellationToken);

        var total = await GetRangeLengthAsync(url, cancellationToken);
        if (total is null)
        {
            onSingleThreadFallback?.Invoke();
            return await DownloadSequentiallyAsync(url, filePath, partialPath, progress, cancellationToken, usedSingleThreadFallback: true);
        }

        var segmentCount = (int)Math.Min(Math.Max(1, downloadThreads), total.Value);
        if (segmentCount <= 1)
            return await DownloadSequentiallyAsync(url, filePath, partialPath, progress, cancellationToken);

        var segmentPaths = Enumerable.Range(0, segmentCount)
            .Select(index => partialPath + $".segment-{index:D2}")
            .ToArray();
        var ranges = Enumerable.Range(0, segmentCount)
            .Select(index => new ByteRange(
                total.Value * index / segmentCount,
                total.Value * (index + 1) / segmentCount - 1))
            .ToArray();
        long received = 0;
        progress?.Report(new DownloadProgress(received, total.Value));

        try
        {
            await Task.WhenAll(Enumerable.Range(0, segmentCount).Select(index =>
                DownloadSegmentAsync(
                    url,
                    segmentPaths[index],
                    ranges[index],
                    bytes =>
                    {
                        var current = Interlocked.Add(ref received, bytes);
                        progress?.Report(new DownloadProgress(current, total.Value));
                    },
                    cancellationToken)));

            await MergeSegmentsAsync(segmentPaths, partialPath, cancellationToken);
            File.Move(partialPath, filePath, overwrite: true);
            return new DownloadResult(filePath, received);
        }
        catch (RangeNotSupportedException)
        {
            DeleteFiles(segmentPaths);
            onSingleThreadFallback?.Invoke();
            return await DownloadSequentiallyAsync(url, filePath, partialPath, progress, cancellationToken, usedSingleThreadFallback: true);
        }
        catch
        {
            DeleteFiles(segmentPaths);
            throw;
        }
    }

    private async Task<long?> GetRangeLengthAsync(string url, CancellationToken cancellationToken)
    {
        using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(0, 0);
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) response.EnsureSuccessStatusCode();
        var contentRange = response.Content.Headers.ContentRange;
        return response.StatusCode == HttpStatusCode.PartialContent
            && contentRange?.From == 0
            && contentRange?.To == 0
            && contentRange.Length is > 0
            ? contentRange.Length
            : null;
    }

    private async Task DownloadSegmentAsync(
        string url,
        string segmentPath,
        ByteRange range,
        Action<int> reportBytes,
        CancellationToken cancellationToken)
    {
        using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(range.Start, range.End);
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) response.EnsureSuccessStatusCode();
        var contentRange = response.Content.Headers.ContentRange;
        if (response.StatusCode != HttpStatusCode.PartialContent
            || contentRange?.From != range.Start
            || contentRange.To != range.End)
            throw new RangeNotSupportedException();
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(segmentPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        var buffer = new byte[81920];
        long received = 0;
        int count;
        while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            reportBytes(count);
            received += count;
        }
        if (received != range.End - range.Start + 1)
            throw new RangeNotSupportedException();
    }

    private static async Task MergeSegmentsAsync(IReadOnlyList<string> segmentPaths, string partialPath, CancellationToken cancellationToken)
    {
        await using var target = new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        foreach (var segmentPath in segmentPaths)
        {
            await using var source = new FileStream(segmentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            await source.CopyToAsync(target, cancellationToken);
        }
        DeleteFiles(segmentPaths);
    }

    private async Task<DownloadResult> DownloadSequentiallyAsync(
        string url,
        string filePath,
        string partialPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken,
        bool usedSingleThreadFallback = false)
    {
        var existing = File.Exists(partialPath) ? new FileInfo(partialPath).Length : 0L;

        using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, url);
        if (existing > 0)
            request.Headers.Range = new RangeHeaderValue(existing, null);

        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        // A server that ignores Range returns the complete body. In that case the
        // partial file cannot be appended safely, so restart it from byte zero.
        var append = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (!append) existing = 0;
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? -1;
        var total = append
            ? response.Content.Headers.ContentRange?.Length ?? (contentLength > 0 ? existing + contentLength : -1)
            : contentLength;
        long received = existing;
        progress?.Report(new DownloadProgress(received, total));

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(partialPath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
        var buffer = new byte[81920];
        int count;
        while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            received += count;
            progress?.Report(new DownloadProgress(received, total));
        }

        target.Close();
        File.Move(partialPath, filePath, overwrite: true);
        return new DownloadResult(filePath, received, usedSingleThreadFallback);
    }

    private static void DeleteFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private readonly record struct ByteRange(long Start, long End);

    private sealed class RangeNotSupportedException : Exception;
}
