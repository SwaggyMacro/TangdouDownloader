using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace TangdouDownloader
{
    public class Headers
    {
        private readonly Uri _uri;
        private readonly List<string> _agentList;
        private readonly Random _random;

        public Headers(string url)
        {
            _uri = new Uri(url);
            _random = new Random();
            _agentList = new List<string>
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
                "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; SE 2.X MetaSr 1.0; SE 2.X MetaSr 1.0; .NET CLR 2.0.50727; SE 2.X MetaSr 1.0)",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; 360SE)",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.0 Safari/536.3",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/22.0.1207.1 Safari/537.1",
                "Mozilla/5.0 (X11; CrOS i686 2268.111.0) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.57 Safari/536.11",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1092.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1090.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/19.77.34.5 Safari/537.1",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.9 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.36 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_0) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.0 Safari/536.3",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Safari/605.1.15",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0",
                "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36"
             };
        }

        public Dictionary<string, string> BuildHeader()
        {
            string host = _uri.Host;
            string agent = _agentList[_random.Next(_agentList.Count)];

            var headers = new Dictionary<string, string>
            {
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3",
                ["Accept-Encoding"] = "gzip, deflate, br",
                ["Accept-Language"] = "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7,zh-CN;q=0.6",
                ["Connection"] = "keep-alive",
                ["Host"] = host,
                ["Pragma"] = "no-cache",
                ["Referer"] = "https://www.tangdoucdn.com/",
                ["User-Agent"] = agent
            };

            return headers;
        }
    }

    public static class Utils
    {
        public static string GetVid(string url)
        {
            string vid = null;
            if (int.TryParse(url, out int vidNumber))
            {
                vid = url;
            }
            else
            {
                var query = HttpUtility.ParseQueryString(new Uri(url).Query);
                if (query["vid"] != null)
                {
                    vid = query["vid"];
                }
            }
            return vid;
        }
        public static bool IsVideo(string url)
        {
            return url.IndexOf("music") != -1 ? false : true;
        }
    }

    public class HTML
    {
        private string _url = "http://share.tangdou.com/splay.php?vid=";
        private static readonly HttpClient _client = new HttpClient();

        public async Task<string> GetVideoUrlAsync(string url)
        {
            string vid = Utils.GetVid(url);
            if (vid == null)
            {
                throw new ArgumentException($"can not find 'vid' parameter from '{url}'");
            }

            var headers = new Headers(_url + vid).BuildHeader();
            foreach (var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await _client.GetAsync(_url + vid);
            if (response.IsSuccessStatusCode)
            {
                var pageContent = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(pageContent);
                var videoNode = doc.DocumentNode.SelectSingleNode("//video");
                return videoNode?.Attributes["src"]?.Value; // original video address
            }
            else
            {
                throw new HttpRequestException($"request error, error code: {response.StatusCode}");
            }
        }
    }

    public class VideoAPI
    {
        private string _url = "http://api-h5.tangdou.com/sample/share/main?vid=";
        private static readonly HttpClient _client = new HttpClient();

        private async Task<Dictionary<string, object>> GetApiInfoAsync(string vid)
        {
            var headers = new Headers(_url + vid).BuildHeader();
            foreach (var header in headers)
            {
                if (_client.DefaultRequestHeaders.Contains(header.Key))
                {
                    _client.DefaultRequestHeaders.Remove(header.Key);
                }
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await _client.GetAsync(_url + vid);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
            }
            else
            {
                throw new HttpRequestException($"request error, error code: {response.StatusCode}");
            }
        }

        public async Task<Dictionary<string, object>> GetVideoInfoAsync(string url)
        {
            string vid = Utils.GetVid(url);
            if (vid == null)
            {
                throw new ArgumentException($"can not find 'vid' parameter from '{url}'");
            }

            var apiDict = await GetApiInfoAsync(vid);
            var videoInfo = new Dictionary<string, object>();
            var data = (JObject)apiDict["data"];
            videoInfo["name"] = data["title"];

            var urls = new Dictionary<string, string>();
            videoInfo["urls"] = urls;

            // Get a list of video URLs in different definitions
            string videoUrl = data["video_url"].ToString();
            string[] clarity = { "H1080P", "V1080P", "H720P", "V720P", "H540P", "V540P", "H360P", "V360P" };
            _client.DefaultRequestHeaders.Host = new Uri(videoUrl).Host;
            foreach (var c in clarity)
            {
                string modifiedUrl = Regex.Replace(videoUrl, "_.[0-9]+P", $"_{c}");
                var headResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, modifiedUrl));
                if (headResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    urls[c] = modifiedUrl;
                }
            }
            return videoInfo;
        }
    }

    //public class AudioAPI
    //{
    //    private string _url = "https://api-h5.tangdou.com/sample/share/recommend?page_num=1&vid=";
    //    private static readonly HttpClient _client = new HttpClient();

    //    private async Task<Dictionary<string, object>> GetApiInfoAsync(string vid)
    //    {
    //        var headers = new Headers(_url + vid).BuildHeader();
    //        foreach (var header in headers)
    //        {
    //            _client.DefaultRequestHeaders.Add(header.Key, header.Value);
    //        }

    //        var response = await _client.GetAsync(_url + vid);
    //        if (response.IsSuccessStatusCode)
    //        {
    //            var jsonResponse = await response.Content.ReadAsStringAsync();
    //            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
    //        }
    //        else
    //        {
    //            throw new HttpRequestException($"request error, error code: {response.StatusCode}");
    //        }
    //    }

    //    public async Task<Dictionary<string, string>> GetAudioInfoAsync(string url)
    //    {
    //        string vid = Utils.GetVid(url);
    //        if (vid == null)
    //        {
    //            throw new ArgumentException($"can not find 'vid' parameter from '{url}'");
    //        }

    //        var apiDict = await GetApiInfoAsync(vid);
    //        var audioInfo = new Dictionary<string, string>();
    //        var dataList = apiDict["data"] as List<object>;
    //        var data = dataList[1] as Dictionary<string, object>;
    //        audioInfo["name"] = data["title"].ToString();
    //        audioInfo["url"] = data["mp3url"].ToString();

    //        return audioInfo;
    //    }
    //}
}