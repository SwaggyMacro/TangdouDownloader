using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TangdouDownloader.Core;

public sealed record SearchResultItem(
    string Vid,
    string Title,
    string Author,
    int DurationSeconds,
    long Hits,
    string CreateTime,
    bool HasTeach,
    string? CoverUrl);

public sealed record SearchResult(IReadOnlyList<SearchResultItem> Items, bool Matched, int Page, string Keyword);

public interface IVideoSearchService
{
    Task<SearchResult> SearchAsync(string keyword, int page = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Talks to the native app's search endpoint (aa.tangdou.com:12308). Every request
/// must carry a hash signature or the server replies code=1 "异常请求".
/// Signing rule, reverse-engineered from the app's HttpCommonQueryInterceptor:
/// concatenate business params then common params as key=value joined by '&amp;'
/// (spaces in common-param values become underscores), append the salt, MD5 the
/// whole string. No login/token is required.
/// </summary>
public sealed class TangdouSearchService : IVideoSearchService, IDisposable
{
    private const string ApiBase = "https://aa.tangdou.com:12308/api.php";
    private const string ImageBase = "https://bimg.tangdou.com";
    private const string AppVersion = "8.8.5";
    private const string Salt = "305%daf5g7ra05$#+6%pm!ud922u!(_t#elidt7q2t";

    private readonly HttpClient _client;

    public TangdouSearchService()
    {
        _client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    }

    public async Task<SearchResult> SearchAsync(string keyword, int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) throw new ArgumentException("请输入要搜索的歌名或舞名。");
        page = Math.Max(1, page);

        // Business params keep insertion order and their raw values when signing.
        var business = new List<KeyValuePair<string, string>>
        {
            new("mod", "search"),
            new("ac", "all_video"),
            new("key", keyword.Trim()),
            new("type", "0"),
            new("page", page.ToString()),
            new("source", "搜索"),
            new("client_module", "cat"),
        };
        var common = new List<KeyValuePair<string, string>>
        {
            new("client", "2"),
            new("channel", "official"),
            new("ver", AppVersion),
            new("package", "com.bokecc.dance"),
            new("version", AppVersion),
            new("smallvideo", "1"),
            new("time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
            new("xinge", string.Empty),
            new("div", AppVersion),
            new("dic", "official"),
            new("startid", "1"),
            new("allstartid", "1"),
            new("stepid", "0"),
            new("sdkversion", "13"),
            new("isvirtualapp", "0"),
        };

        var signBuilder = new StringBuilder();
        var queryBuilder = new StringBuilder();
        void Append(KeyValuePair<string, string> p, bool commonParam)
        {
            if (signBuilder.Length > 0) signBuilder.Append('&');
            if (queryBuilder.Length > 0) queryBuilder.Append('&');
            // Only common-param values have spaces replaced with underscores when signing.
            var signValue = commonParam ? p.Value.Replace(' ', '_') : p.Value;
            signBuilder.Append(p.Key).Append('=').Append(signValue);
            queryBuilder.Append(Uri.EscapeDataString(p.Key)).Append('=').Append(Uri.EscapeDataString(signValue));
        }
        foreach (var p in business) Append(p, false);
        foreach (var p in common) Append(p, true);

        var signSource = signBuilder.ToString() + Salt;
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signSource))).ToLowerInvariant();
        var url = $"{ApiBase}?{queryBuilder}&hash={hash}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "okhttp/3.12.1");
        request.Headers.TryAddWithoutValidation("Referer", "tangdou.app");
        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        if (root.TryGetProperty("code", out var code))
        {
            // "code" arrives as either a JSON number or a string depending on the endpoint.
            var codeText = code.ValueKind == JsonValueKind.String ? code.GetString() : code.GetRawText();
            if (codeText != "0")
            {
                var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : null;
                throw new HttpRequestException($"搜索接口返回错误：{msg ?? $"code={codeText}"}");
            }
        }
        if (!root.TryGetProperty("datas", out var datas) || datas.ValueKind != JsonValueKind.Array)
            return new SearchResult([], false, page, keyword.Trim());

        var items = new List<SearchResultItem>();
        var matched = false;
        foreach (var element in datas.EnumerateArray())
        {
            var title = GetString(element, "title") ?? string.Empty;
            var highlight = GetString(element, "highlight");
            if (highlight?.Contains("<em>", StringComparison.OrdinalIgnoreCase) == true) matched = true;
            var vid = GetString(element, "vid") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(vid)) continue;
            int.TryParse(GetString(element, "duration"), out var duration);
            long.TryParse(GetString(element, "hits_total"), out var hits);
            int.TryParse(GetString(element, "teach"), out var teach);
            var pic = GetString(element, "pic");
            if (!string.IsNullOrWhiteSpace(pic) && pic.StartsWith('/')) pic = ImageBase + pic;
            items.Add(new SearchResultItem(
                vid,
                string.IsNullOrWhiteSpace(title) ? "未命名视频" : title,
                GetString(element, "name") ?? string.Empty,
                duration,
                hits,
                GetString(element, "createtime") ?? string.Empty,
                teach == 1,
                pic));
        }
        // Some keywords silently fall back to a popular-recommendation feed (code is
        // still 0) where no item highlights the keyword; flag that so the UI can tell
        // the user to try another word instead of showing 20 unrelated videos.
        return new SearchResult(items, matched, page, keyword.Trim());
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal)) continue;
            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => null,
            };
        }
        return null;
    }

    public void Dispose() => _client.Dispose();
}
