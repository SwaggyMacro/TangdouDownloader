using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TangdouDownloader
{
    /// <summary>
    /// Builds a dictionary of headers (including a random User-Agent) for HTTP requests.
    /// </summary>
    public class HttpHeaderBuilder
    {
        private readonly List<string> _userAgents;
        private readonly Random _randomGenerator;
        private readonly Uri _resourceUri;

        public HttpHeaderBuilder(string url)
        {
            _resourceUri = new Uri(url);
            _randomGenerator = new Random();
            _userAgents = new List<string>
            {
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/22.0.1207.1 Safari/537.1",
                "Mozilla/5.0 (X11; CrOS i686 2268.111.0) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.57 Safari/536.11",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1092.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1090.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/19.77.34.5 Safari/537.1",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.9 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.36 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; SE 2.X MetaSr 1.0)",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_0) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; 360SE)",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Safari/605.1.15",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0",
                "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36"
            };
        }

        /// <summary>
        /// Builds a dictionary of request headers with a randomly selected User-Agent.
        /// </summary>
        /// <returns>Dictionary of headers for the HTTP request.</returns>
        public Dictionary<string, string> BuildHeaders()
        {
            string host = _resourceUri.Host;
            string randomUserAgent = _userAgents[_randomGenerator.Next(_userAgents.Count)];

            var headers = new Dictionary<string, string>
            {
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3",
                ["Accept-Language"] = "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7,zh-CN;q=0.6",
                ["Connection"] = "keep-alive",
                ["Host"] = host,
                ["Pragma"] = "no-cache",
                ["Referer"] = "https://www.tangdoucdn.com/",
                ["User-Agent"] = randomUserAgent
            };

            return headers;
        }
    }

    /// <summary>
    /// Common utility methods used in the downloader.
    /// </summary>
    public static class DownloaderUtils
    {
        /// <summary>
        /// Tries to parse the 'vid' parameter from a URL or use the URL directly if it's an integer.
        /// </summary>
        /// <param name="url">The video URL or vid value.</param>
        /// <returns>The 'vid' parameter or null if not found.</returns>
        public static string GetVid(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            url = url.Trim();
            if (long.TryParse(url, out _))
            {
                return url;
            }

            var match = Regex.Match(url, @"vid=(\d+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            try
            {
                var query = HttpUtility.ParseQueryString(new Uri(url).Query);
                var vid = query["vid"];
                if (!string.IsNullOrEmpty(vid)) return vid;
            }
            catch
            {
                // Ignored if invalid URI format
            }

            match = Regex.Match(url, @"(\d{8,})");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Checks if the given URL is a video link by verifying it doesn't contain 'music'.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if it's recognized as a video link, otherwise false.</returns>
        public static bool IsVideo(string url)
        {
            return url.IndexOf("music", StringComparison.Ordinal) == -1;
        }
    }

    /// <summary>
    /// Provides methods to fetch and parse HTML pages.
    /// </summary>
    public class HtmlParser
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        private const string VideoShareUrl = "https://www.tangdou.com/play/?vid=";

        /// <summary>
        /// Fetches the video URL from a Tangdou share page using HTML parsing.
        /// </summary>
        /// <param name="sourceUrl">The Tangdou share URL or numeric vid value.</param>
        /// <returns>The direct video URL if found.</returns>
        public async Task<string> GetVideoUrlAsync(string sourceUrl)
        {
            string vid = DownloaderUtils.GetVid(sourceUrl);
            if (vid == null)
            {
                throw new ArgumentException($"Can not find 'vid' parameter from '{sourceUrl}'");
            }

            var requestUrl = VideoShareUrl + vid;
            var headerBuilder = new HttpHeaderBuilder(requestUrl);
            var headers = headerBuilder.BuildHeaders();

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await Client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request error, error code: {response.StatusCode}");
                }

                var pageContent = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(pageContent);

                // Attempt to extract video src from the <video> tag
                var videoNode = doc.DocumentNode.SelectSingleNode("//video");
                return videoNode?.GetAttributeValue("src", null);
            }
        }
    }

    /// <summary>
    /// Calls Tangdou API endpoints to retrieve video info and URLs.
    /// </summary>
    public class TangdouVideoApi
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        /// <summary>
        /// Internal method to fetch API response given a 'vid'.
        /// Uses sample/share/main as primary endpoint for full video files and falls back to mtangdou/video/play.
        /// </summary>
        private async Task<Dictionary<string, object>> FetchApiInfoAsync(string vid)
        {
            var apiUrls = new[]
            {
                "https://api-h5.tangdou.com/sample/share/main?vid=" + vid,
                "https://api-h5.tangdou.com/mtangdou/video/play?vid=" + vid
            };

            foreach (var requestUrl in apiUrls)
            {
                try
                {
                    var headerBuilder = new HttpHeaderBuilder(requestUrl);
                    var headers = headerBuilder.BuildHeaders();

                    using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }

                        var response = await Client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
                            if (dict != null && dict.ContainsKey("data") && dict["data"] is JObject data && data.Count > 0)
                            {
                                return dict;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue to fallback endpoint
                }
            }

            throw new HttpRequestException($"Request error: cannot fetch video metadata for vid '{vid}'");
        }

        /// <summary>
        /// Retrieves video metadata (title and URLs in different resolutions) from Tangdou's API.
        /// </summary>
        /// <param name="videoUrl">The Tangdou share URL or numeric vid value.</param>
        /// <returns>A dictionary containing the video's title and a sub-dictionary of available URLs.</returns>
        public async Task<Dictionary<string, object>> GetVideoInfoAsync(string videoUrl)
        {
            string vid = DownloaderUtils.GetVid(videoUrl);
            if (vid == null)
            {
                throw new ArgumentException($"Can not find 'vid' parameter from '{videoUrl}'");
            }

            var apiDict = await FetchApiInfoAsync(vid);
            var videoInfo = new Dictionary<string, object>();

            // Extracting video details
            var data = (JObject)apiDict["data"];
            if (data == null)
            {
                throw new Exception($"API returned error or empty data for vid '{vid}'");
            }
            videoInfo["name"] = data["title"]?.ToString();

            var urlMap = new Dictionary<string, string>();
            videoInfo["urls"] = urlMap;

            // Build a list of potential video URLs in different definitions
            var videoUrlRaw = data["play_url"]?.ToString() ?? data["video_url"]?.ToString();
            string[] resolutions = { "H1080P", "V1080P", "H720P", "V720P", "H540P", "V540P", "H360P", "V360P" };

            if (!string.IsNullOrEmpty(videoUrlRaw))
            {
                var checkTasks = new List<Task>();
                foreach (var resolution in resolutions)
                {
                    var res = resolution;
                    checkTasks.Add(Task.Run(async () =>
                    {
                        var modifiedUrl = Regex.Replace(videoUrlRaw, "_.[0-9]+P", $"_{res}");
                        var headerBuilder = new HttpHeaderBuilder(modifiedUrl);
                        var headers = headerBuilder.BuildHeaders();

                        using (var headRequest = new HttpRequestMessage(HttpMethod.Head, modifiedUrl))
                        {
                            foreach (var header in headers)
                            {
                                headRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }

                            try
                            {
                                var headResponse = await Client.SendAsync(headRequest);
                                if (headResponse.StatusCode != HttpStatusCode.NotFound)
                                {
                                    lock (urlMap)
                                    {
                                        urlMap[res] = modifiedUrl;
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore failed probe
                            }
                        }
                    }));
                }

                await Task.WhenAll(checkTasks);

                // If no probe matched, fallback to raw video URL
                if (urlMap.Count == 0)
                {
                    urlMap["默认"] = videoUrlRaw;
                }
            }

            return videoInfo;
        }
    }

    // Uncomment and adapt this class if audio info is needed in the future
    /*
    public class AudioApi
    {
        private const string ApiUrl = "https://api-h5.tangdou.com/sample/share/recommend?page_num=1&vid=";
        private static readonly HttpClient Client = new HttpClient();

        private async Task<Dictionary<string, object>> GetApiInfoAsync(string vid)
        {
            var headerBuilder = new HttpHeaderBuilder(ApiUrl + vid);
            var headers = headerBuilder.BuildHeaders();

            Client.DefaultRequestHeaders.Clear();
            foreach (var header in headers)
            {
                Client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await Client.GetAsync(ApiUrl + vid);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request error, error code: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
        }

        public async Task<Dictionary<string, string>> GetAudioInfoAsync(string url)
        {
            string vid = DownloaderUtils.GetVid(url);
            if (vid == null)
            {
                throw new ArgumentException($"Can not find 'vid' parameter from '{url}'");
            }

            var apiDict = await GetApiInfoAsync(vid);
            var audioInfo = new Dictionary<string, string>();
            var dataList = apiDict["data"] as List<object>;
            var data = dataList[1] as Dictionary<string, object>;

            audioInfo["name"] = data["title"].ToString();
            audioInfo["url"] = data["mp3url"].ToString();

            return audioInfo;
        }
    }
    */
}