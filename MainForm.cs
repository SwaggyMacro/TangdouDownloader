using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TangdouDownloader
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    [SuppressMessage("ReSharper", "CanSimplifyDictionaryLookupWithTryGetValue")]
    public partial class MainForm : Form
    {
        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            // default choose the first item
            cbbQuality.SelectedIndex = 0;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            // mkdir to save video
            Directory.CreateDirectory("downloads");
        }

        private void ToggleButtonStates()
        {
            btnAppend.Enabled = !btnAppend.Enabled;
            btnClear.Enabled = !btnClear.Enabled;
            btnDel.Enabled = !btnDel.Enabled;
            btnStartDownload.Enabled = !btnStartDownload.Enabled;
        }

        private async void BtnAppendClick(object sender, EventArgs e)
        {
            ToggleButtonStates();
            var videoUrls = tbUrls.Text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            await AppendItemsToListAsync(videoUrls);
            tbUrls.Text = string.Empty;
            ToggleButtonStates();
        }

        private async Task AppendItemsToListAsync(string[] videoUrls)
        {
            var videoApi = new TangdouVideoApi();
            var invalidUrls = new List<string>();
            var errUrls = new List<string>();

            var tasks = videoUrls.Select(async (url, index) =>
            {
                var listCount = lvDownloadList.Items.Count + index + 1;
                var item = new ListViewItem(listCount.ToString());

                try
                {
                    url = url.StartsWith("http") ? url : "https://www.tangdouddn.com/h5/play?vid=" + url;
                    var videoInfo = await videoApi.GetVideoInfoAsync(url);

                    item.SubItems.Add(videoInfo["name"].ToString());
                    item.SubItems.Add("等待中");
                    item.SubItems.Add("0%");
                    item.SubItems.Add(cbbQuality.Text);

                    var videoUrlsDictionary = videoInfo["urls"] as Dictionary<string, string>;
                    item.SubItems.Add(GetDownloadUrl(videoUrlsDictionary, cbbQuality.Text));
                    item.SubItems.Add(DownloaderUtils.GetVid(url));

                    lvDownloadList.Items.Add(item);
                }
                catch (UriFormatException)
                {
                    invalidUrls.Add(url);
                }
                catch
                {
                    errUrls.Add(url);
                }
            }).ToList();

            await Task.WhenAll(tasks);
            lvDownloadList.Refresh();

            ShowMessage("以下链接格式不正确，已忽略：", invalidUrls);
            ShowMessage("以下链接解析失败，已忽略：", errUrls);
        }

        private static string GetDownloadUrl(Dictionary<string, string> vUrls, string quality)
        {
            if (vUrls == null) return string.Empty;

            var qualityMap = new Dictionary<string, string>
            {
                { "1080P", "H1080P" },
                { "720P", "H720P" },
                { "560P", "H560P" },
                { "480P", "H480P" },
                { "360P", "H360P" },
                { "最高", "H1080P" },
                { "最低", "H360P" },
                { "中等", "H720P" }
            };

            return qualityMap.ContainsKey(quality) && vUrls.ContainsKey(qualityMap[quality])
                ? vUrls[qualityMap[quality]]
                : vUrls.Values.FirstOrDefault();
        }

        private void ShowMessage(string message, List<string> urls)
        {
            if (urls.Count > 0)
            {
                MessageBox.Show(this, $"{message}\r\n{string.Join("\r\n", urls)}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnStartDownloadClick(object sender, EventArgs e)
        {
            if (lvDownloadList.Items.Count == 0)
            {
                MessageBox.Show(this, "请先添加视频链接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ToggleButtonStates();

            Task.Run(async () =>
            {
                await StartDownloadsAsync();
                ToggleButtonStates();
                if (MessageBox.Show(this,
                        $"视频下载完成，已保存至软件运行目录下Downloads文件夹({Application.StartupPath + "\\Downloads"})\n是否打开视频保存目录？",
                        "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", Application.StartupPath + "\\Downloads");
                }
            });
        }

        private async Task StartDownloadsAsync()
        {
            var tasks = lvDownloadList.Items.Cast<ListViewItem>()
                .Where(item => !item.SubItems[2].Text.Equals("完成"))
                .Select(async item =>
                {
                    BeginInvoke(new Action(() => item.SubItems[2].Text = "准备下载"));

                    using (var fileDownloader = new HttpFileDownloader(item.Index))
                    {
                        fileDownloader.ProgressChanged += (sender, args) =>
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if (lvDownloadList.Items.Count > args.Id)
                                    lvDownloadList.Items[args.Id].SubItems[3].Text = $"{args.ProgressPercentage}%";
                            }));
                        };

                        var headers = new HttpHeaderBuilder(item.SubItems[5].Text).BuildHeaders();
                        foreach (var header in headers)
                        {
                            fileDownloader.AddHeader(header.Key, header.Value);
                        }

                        var cts = new CancellationTokenSource();

                        try
                        {
                            BeginInvoke(new Action(() => item.SubItems[2].Text = "下载中"));

                            await fileDownloader.DownloadFileAsync(item.SubItems[5].Text,
                                $"downloads/{fileDownloader.SanitizeFileName(item.SubItems[1].Text)}.mp4", cts.Token);

                            BeginInvoke(new Action(() => item.SubItems[2].Text = "完成"));
                        }
                        catch (OperationCanceledException)
                        {
                            BeginInvoke(new Action(() => item.SubItems[2].Text = "已取消"));
                        }
                        catch (Exception ex)
                        {
                            BeginInvoke(new Action(() => item.SubItems[2].Text = "下载失败"));
                            Trace.WriteLine(ex.Message);
                        }
                    }
                }).ToList();

            await Task.WhenAll(tasks);
        }

        private void BtnDeleteClick(object sender, EventArgs e)
        {
            // delete selected items
            foreach (ListViewItem item in lvDownloadList.SelectedItems)
            {
                lvDownloadList.Items.Remove(item);
            }
        }

        private void BtnClearClick(object sender, EventArgs e)
        {
            // clear the list
            lvDownloadList.Items.Clear();
        }

        private void OpenGitHubLinkClick(object sender, EventArgs e)
        {
            //forward to GitHub
            Process.Start("https://github.com/SwaggyMacro/TangdouDownloader");
        }

        private void OpenAliPanLinkClick(object sender, EventArgs e)
        {
            Process.Start("https://www.alipan.com/s/kRsFsvtWskD");
        }

        private void ShowUsageInstructionsClick(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "1. 在视频链接输入框内粘贴需要下载的视频地址或者VID（一行一个，回车换行）\n2. 选择清晰度（默认最高）\n3. 点击“添加”按钮添加至下载列表\n4. 点击“开始下载”按钮\n5. 下载完成后视频自动保存至软件的运行目录",
                "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
    }
}