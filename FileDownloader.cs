using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TangdouDownloader
{
    public class HttpFileDownloader : IDisposable
    {
        private readonly HttpClient _client;
        private readonly int _downloaderId;

        public HttpFileDownloader(int id)
        {
            // 初始化HttpClient
            _client = new HttpClient();
            _downloaderId = id;
        }

        // 确保最终释放HttpClient的资源
        public void Dispose()
        {
            _client.Dispose();
        }

        public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;

        // 允许设置headers的方法
        public void AddHeader(string name, string value)
        {
            if (!_client.DefaultRequestHeaders.TryAddWithoutValidation(name, value))
            {
                throw new InvalidOperationException($"无法添加指定的头: {name}");
            }
        }

        public string SanitizeFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public async Task DownloadFileAsync(string requestUri, string outputPath, CancellationToken cancellationToken)
        {
            const int maxRetryAttempts = 10;
            int attemptCount = 0;
            bool isDownloadSuccessful = false;

            while (attemptCount < maxRetryAttempts && !isDownloadSuccessful)
            {
                try
                {
                    using (var httpResponse =
                           await _client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        httpResponse.EnsureSuccessStatusCode(); // 确保响应状态是成功的

                        var totalBytes = httpResponse.Content.Headers.ContentLength ?? -1L;
                        long totalBytesRead = 0;
                        byte[] buffer = new byte[8192];
                        bool hasMoreData = true;

                        using (var outputFileStream =
                               new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            using (var responseContentStream = await httpResponse.Content.ReadAsStreamAsync())
                            {
                                DateTime lastReportTime = DateTime.UtcNow;
                                long bytesSinceLastReport = 0;

                                while (hasMoreData)
                                {
                                    int bytesRead =
                                        await responseContentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                                    if (bytesRead == 0)
                                    {
                                        hasMoreData = false;
                                        TriggerProgressChanged(totalBytesRead, totalBytes);
                                        continue;
                                    }

                                    await outputFileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                                    totalBytesRead += bytesRead;
                                    bytesSinceLastReport += bytesRead;

                                    if (DateTime.UtcNow - lastReportTime > TimeSpan.FromSeconds(1)
                                        || bytesSinceLastReport > 100000)
                                    {
                                        TriggerProgressChanged(totalBytesRead, totalBytes);
                                        lastReportTime = DateTime.UtcNow;
                                        bytesSinceLastReport = 0;
                                    }
                                }
                            }
                        }
                    }

                    isDownloadSuccessful = true; // 下载成功，退出循环
                }
                catch (Exception ex) when (attemptCount < maxRetryAttempts)
                {
                    attemptCount++;
                    Trace.WriteLine($"下载失败，重试第 {attemptCount} 次: {ex.Message}");
                    await Task.Delay(1000);
                }
            }

            if (!isDownloadSuccessful)
            {
                // throw new Exception("下载失败超过最大重试次数");
                Trace.WriteLine("下载失败超过最大重试次数");
            }
        }

        protected virtual void TriggerProgressChanged(long bytesReceived, long totalBytes)
        {
            ProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(_downloaderId, bytesReceived, totalBytes));
        }
    }

    public class DownloadProgressChangedEventArgs : EventArgs
    {
        public DownloadProgressChangedEventArgs(int id, long received, long totalToReceive)
        {
            Id = id;
            BytesReceived = received;
            TotalBytesToReceive = totalToReceive;
        }

        public int Id { get; }
        public long BytesReceived { get; }
        public long TotalBytesToReceive { get; }

        public int ProgressPercentage =>
            TotalBytesToReceive > 0 ? (int)(BytesReceived * 100L / TotalBytesToReceive) : 0;
    }
}