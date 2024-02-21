using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TangdouDownloader
{
    public class FileDownloader : IDisposable
    {
        private readonly HttpClient _httpClient;

        public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;
        private int Id;
        public FileDownloader(int id)
        {
            // 初始化HttpClient
            _httpClient = new HttpClient();
            this.Id = id;
        }

        // 允许设置headers的方法
        public void AddHeader(string name, string value)
        {
            if (!_httpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value))
            {
                throw new InvalidOperationException($"无法添加指定的头: {name}");
            }
        }
        public string FilterFileName(string fileName) { 
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
        public async Task DownloadFileAsync(string requestUri, string outputPath, CancellationToken cancellationToken)
        {
            // 给HttpClient发出请求并得到回应
            using (var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode(); // 确保响应状态是成功的

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var totalBytesRead = 0L;
                //var readCount = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var lastReportTime = DateTime.UtcNow;
                        var bytesSinceLastReport = 0L;

                        do
                        {
                            int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead == 0)
                            {
                                isMoreToRead = false;
                                TriggerProgressChanged(totalBytesRead, totalBytes);
                                continue;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                            totalBytesRead += bytesRead;
                            bytesSinceLastReport += bytesRead;

                            if (DateTime.UtcNow - lastReportTime > TimeSpan.FromSeconds(1) || bytesSinceLastReport > 100000)
                            {
                                TriggerProgressChanged(totalBytesRead, totalBytes);
                                lastReportTime = DateTime.UtcNow;
                                bytesSinceLastReport = 0;
                            }
                        } while (isMoreToRead);
                    }
                }
            }
        }

        protected virtual void TriggerProgressChanged(long bytesReceived, long totalBytes)
        {
            ProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(Id, bytesReceived, totalBytes));
        }

        // 确保最终释放HttpClient的资源
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    public class DownloadProgressChangedEventArgs : EventArgs
    {
        public int Id { get; }
        public long BytesReceived { get; }
        public long TotalBytesToReceive { get; }
        public int ProgressPercentage => TotalBytesToReceive > 0 ? (int)((BytesReceived * 100L) / TotalBytesToReceive) : 0;

        public DownloadProgressChangedEventArgs(int id, long received, long totalToReceive)
        {
            this.Id = id;
            BytesReceived = received;
            TotalBytesToReceive = totalToReceive;
        }
    }
}