using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TangdouDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();
            Initial();
        }
        private void Initial()
        {
            // default choose the first item
            cbbQuality.SelectedIndex = 0;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            // mkdir to save video
            if (!System.IO.Directory.Exists("downloads"))
            {
                System.IO.Directory.CreateDirectory("downloads");
            }
        }
        private void ReverseBtnState() {
            btnAppend.Enabled = !btnAppend.Enabled;
            btnClear.Enabled = !btnClear.Enabled;
            btnDel.Enabled = !btnDel.Enabled;
            btnStartDownload.Enabled = !btnStartDownload.Enabled;
        }
        private void btnAppend_Click(object sender, EventArgs e)
        {
            ReverseBtnState();
            var urls = tbUrls.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            AppendItemsToList(urls);
            tbUrls.Text = String.Empty;
        }
        private async void AppendItemsToList(string[] urls)
        {
            VideoAPI videoAPI = new VideoAPI();
            var listCount = lvDownloadList.Items.Count;
            // first is number, second is name, third is status, fourth is progress, fifth is quality, sixth is download url, seventh is vid
            foreach (var url in urls)
            {
                listCount++;
                ListViewItem item = new ListViewItem(Convert.ToString(listCount));
                Dictionary<string, object> vInfo;
                vInfo = await videoAPI.GetVideoInfoAsync(url);
                item.SubItems.Add(vInfo["name"].ToString());
                item.SubItems.Add("等待中");
                item.SubItems.Add("0%");
                item.SubItems.Add(cbbQuality.Text);
                var vUrls = vInfo["urls"] as Dictionary<string, string>;
                switch (cbbQuality.SelectedText)
                {
                    case "1080P":
                        if (vUrls.ContainsKey("H1080P"))
                            item.SubItems.Add(vUrls["H1080P"]);
                        break;
                    case "720P":
                        if (vUrls.ContainsKey("H720P"))
                            item.SubItems.Add(vUrls["H720P"]);
                        break;
                    case "560P":
                        if (vUrls.ContainsKey("H560P"))
                            item.SubItems.Add(vUrls["H560P"]);
                        break;
                    case "480P":
                        if (vUrls.ContainsKey("H480P"))
                            item.SubItems.Add(vUrls["H480P"]);
                        break;
                    case "360P":
                        if (vUrls.ContainsKey("H360P"))
                            item.SubItems.Add(vUrls["H360P"]);
                        break;
                    case "最高":
                        if (vUrls.ContainsKey("H1080P"))
                            item.SubItems.Add(vUrls["H1080P"]);
                        else if (vUrls.ContainsKey("H720P"))
                            item.SubItems.Add(vUrls["H720P"]);
                        else if (vUrls.ContainsKey("H560P"))
                            item.SubItems.Add(vUrls["H560P"]);
                        else if (vUrls.ContainsKey("H480P"))
                            item.SubItems.Add(vUrls["H480P"]);
                        else if (vUrls.ContainsKey("H360P"))
                            item.SubItems.Add(vUrls["H360P"]);
                        break;
                    case "最低":
                        if (vUrls.ContainsKey("H360P"))
                            item.SubItems.Add(vUrls["H360P"]);
                        else if (vUrls.ContainsKey("H480P"))
                            item.SubItems.Add(vUrls["H480P"]);
                        else if (vUrls.ContainsKey("H560P"))
                            item.SubItems.Add(vUrls["H560P"]);
                        else if (vUrls.ContainsKey("H720P"))
                            item.SubItems.Add(vUrls["H720P"]);
                        else if (vUrls.ContainsKey("H1080P"))
                            item.SubItems.Add(vUrls["H1080P"]);
                        break;
                    case "中等":
                        if (vUrls.ContainsKey("H720P"))
                            item.SubItems.Add(vUrls["H720P"]);
                        else if (vUrls.ContainsKey("H560P"))
                            item.SubItems.Add(vUrls["H560P"]);
                        else if (vUrls.ContainsKey("H480P"))
                            item.SubItems.Add(vUrls["H480P"]);
                        else if (vUrls.ContainsKey("H360P"))
                            item.SubItems.Add(vUrls["H360P"]);
                        else if (vUrls.ContainsKey("H1080P"))
                            item.SubItems.Add(vUrls["H1080P"]);
                        break;
                    default:
                        if (vUrls.ContainsKey("H1080P"))
                            item.SubItems.Add(vUrls["H1080P"]);
                        else if (vUrls.ContainsKey("H560P"))
                            item.SubItems.Add(vUrls["H560P"]);
                        else if (vUrls.ContainsKey("H720P"))
                            item.SubItems.Add(vUrls["H720P"]);
                        else if (vUrls.ContainsKey("H480P"))
                            item.SubItems.Add(vUrls["H480P"]);
                        else if (vUrls.ContainsKey("H360P"))
                            item.SubItems.Add(vUrls["H360P"]);
                        break;
                }
                item.SubItems.Add(Utils.GetVid(url));
                lvDownloadList.Items.Add(item);
            }
            lvDownloadList.Refresh();
            ReverseBtnState();
        }
        private void btnStartDownload_Click(object sender, EventArgs e)
        {
            ReverseBtnState();

            Task.Run(() =>
            {
                StartDownloads();
                ReverseBtnState();
            });
        }


        private void StartDownloads()
        {
            foreach (ListViewItem item in lvDownloadList.Items)
            {
                if (item.SubItems[2].Text.Equals("完成"))
                {
                    continue;
                }
                this.BeginInvoke(new Action(() => item.SubItems[2].Text = "准备下载"));

                using (var fileDownloader = new FileDownloader(item.Index))
                {
                    fileDownloader.ProgressChanged += (sender, args) =>
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (lvDownloadList.Items.Count > args.Id)
                            {
                                lvDownloadList.Items[args.Id].SubItems[3].Text = $"{args.ProgressPercentage}%";
                            }
                        }));
                    };

                    var headers = new Headers(item.SubItems[5].Text).BuildHeader();
                    foreach (var header in headers)
                    {
                        fileDownloader.AddHeader(header.Key, header.Value);
                    }
                    var cts = new CancellationTokenSource();

                    try
                    {
                        this.BeginInvoke(new Action(() => item.SubItems[2].Text = "下载中"));

                        var downloadTask = fileDownloader.DownloadFileAsync(item.SubItems[5].Text, $"downloads/{fileDownloader.FilterFileName(item.SubItems[1].Text)}.mp4", cts.Token);
                        downloadTask.Wait(); 

                        this.BeginInvoke(new Action(() => item.SubItems[2].Text = "完成"));
                    }
                    catch (OperationCanceledException)
                    {
                        this.BeginInvoke(new Action(() => item.SubItems[2].Text = "已取消"));
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new Action(() => item.SubItems[2].Text = "下载失败"));
                        Trace.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            // delete selected items
            foreach (ListViewItem item in lvDownloadList.SelectedItems)
            {
                lvDownloadList.Items.Remove(item);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // clear the list
            lvDownloadList.Items.Clear();

        }

        private void toolStripStatusLabel3_Click(object sender, EventArgs e)
        {
            //forward to github
            System.Diagnostics.Process.Start("https://github.com/SwaggyMacro/TangdouDownloader");
        }

        private void toolStripStatusLabel4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.alipan.com/s/kRsFsvtWskD");
        }
    }
}
