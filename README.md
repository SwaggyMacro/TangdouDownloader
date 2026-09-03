### 🎯使用说明
1. 在视频链接输入框内粘贴需要下载的视频地址或者VID（一行一个，回车换行）
2. 选择清晰度（默认最高）
3. 点击“添加”按钮添加至下载列表
4. 点击“开始下载”按钮
5. 下载完成后视频自动保存至软件的运行目录

### 📝更新日志
#### v0.5
- **修复视频解析失败问题**：
  - **问题原因**：糖豆网近期服务端策略变更，严格响应 `Accept-Encoding: gzip` 请求头并返回 Gzip 压缩后的响应数据。由于 `HttpClient` 原先未配置自动解压缩，导致读取到的 JSON 为二进制压缩乱码，触发 `JsonReaderException` 异常并提示解析失败。
  - **解决方法**：为 `HttpClient` 配置 `HttpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate`，并移除手动硬编码的 `Accept-Encoding` 请求头，由底层网络库自动透明协商并解压数据。
- **并发安全性与请求隔离优化**：
  - 彻底移除对单例 `HttpClient.DefaultRequestHeaders` 的全局修改，改用单次请求独立的 `HttpRequestMessage` 与 `TryAddWithoutValidation`，杜绝批量添加多视频链接时的线程竞争与请求头错乱。
- **清晰度探测性能提升**：
  - 各档清晰度（1080P/720P/540P/360P 等）的 HEAD 探测由原本的“串行遍历”优化为 **`Task.WhenAll` 并发异步探测**，单视频解析耗时减少约 60%~75%。

#### v0.4
- 修复糖豆网视频链接解析失败问题（更新 API 接口及 `play_url` 视频字段）。
- 修复 14 位数值 VID 导致的数据溢出与解析报错问题。
- 修复下载完成提示打开的文件夹路径与实际保存目录不一致的问题。
- 新增下载列表双击交互：双击已完成列自动打开文件夹并高亮定位视频文件，双击未完成列开始下载。
- 优化视频链接粘贴兼容性，支持 `https://www.tangdou.com/play/?vid=...`、分享页链接与纯 VID 输入。

### 🖼️操作截图
![Animation](https://github.com/SwaggyMacro/TangdouDownloader/assets/38845682/2125be8c-4ce0-4d2c-8c23-efec48550898)
