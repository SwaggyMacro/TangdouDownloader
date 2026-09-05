using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Irihi.Avalonia.Shared.Helpers;
using ReactiveUI;
using ReactiveUI.Primitives;
using TangdouDownloader.Core;

namespace TangdouDownloader.Desktop;

public sealed class DownloadItem : ReactiveObject
{
    private bool _isSelected;
    private int _progress;
    private long _bytesReceived;
    private long _totalBytes = -1;
    private double _speedBytesPerSecond;
    private string _status = "排队中";
    private string _remainingTime = "等待开始";
    private string? _filePath;
    private string? _errorMessage;
    private string? _coverUrl;
    private Bitmap? _coverImage;
    private string _title = string.Empty;
    private string _url = string.Empty;

    public int Order { get; init; }
    public required string Vid { get; init; }
    public required string Title { get => _title; set => SetField(ref _title, value); }
    public required string Url { get => _url; set => SetField(ref _url, value); }
    public required string Quality { get; init; }
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public int Progress { get => _progress; set { if (SetField(ref _progress, value)) OnPropertyChanged(nameof(ProgressLabel)); } }
    public long BytesReceived { get => _bytesReceived; set { if (SetField(ref _bytesReceived, value)) OnPropertyChanged(nameof(TransferLabel)); } }
    public long TotalBytes { get => _totalBytes; set { if (SetField(ref _totalBytes, value)) { OnPropertyChanged(nameof(SizeLabel)); OnPropertyChanged(nameof(TransferLabel)); } } }
    public double SpeedBytesPerSecond { get => _speedBytesPerSecond; set { if (SetField(ref _speedBytesPerSecond, value)) OnPropertyChanged(nameof(TransferLabel)); } }
    public string Status
    {
        get => _status;
        set
        {
            if (!SetField(ref _status, value)) return;
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(CanResume));
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(CanOpenFile));
            OnPropertyChanged(nameof(StatusDetail));
            OnPropertyChanged(nameof(IsDownloading));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(StatusIcon));
        }
    }
    public string RemainingTime { get => _remainingTime; set { if (SetField(ref _remainingTime, value)) OnPropertyChanged(nameof(StatusDetail)); } }
    public string? FilePath { get => _filePath; set { if (SetField(ref _filePath, value)) OnPropertyChanged(nameof(CanOpenFile)); } }
    public string? ErrorMessage { get => _errorMessage; set { if (SetField(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(StatusDetail)); } } }
    public string? CoverUrl { get => _coverUrl; set => SetField(ref _coverUrl, value); }
    public Bitmap? CoverImage
    {
        get => _coverImage;
        set
        {
            if (!SetField(ref _coverImage, value)) return;
            OnPropertyChanged(nameof(HasCoverImage));
            OnPropertyChanged(nameof(ShowCoverPlaceholder));
        }
    }
    public bool HasCoverImage => CoverImage is not null;
    public bool ShowCoverPlaceholder => CoverImage is null;
    public string QualityFormat => $"{Quality} / MP4";
    public string ProgressLabel => TotalBytes > 0 ? $"{Progress}%" : BytesReceived > 0 ? "下载中" : "等待中";
    public string TransferLabel => $"{FormatBytes(BytesReceived)} / {(TotalBytes > 0 ? FormatBytes(TotalBytes) : "未知")}  {FormatBytes((long)SpeedBytesPerSecond)}/s";
    public string SizeLabel => TotalBytes > 0 ? FormatBytes(TotalBytes) : BytesReceived > 0 ? FormatBytes(BytesReceived) : "--";
    public bool CanPause => Status == "下载中" || Status == "解析中";
    public bool CanCancel => Status is "下载中" or "解析中" or "排队中" or "已暂停";
    public bool CanResume => Status == "已暂停";
    public bool CanRetry => IsFailed;
    public bool CanOpenFile => Status == "已完成" && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string StatusDetail => HasError ? ErrorMessage! : RemainingTime;

    public bool IsDownloading => Status is "下载中" or "解析中";
    public bool IsCompleted => Status == "已完成";
    public bool IsPaused => Status == "已暂停";
    public bool IsFailed => Status is "下载失败" or "解析失败";

    public string StatusIcon => Status switch
    {
        "下载中" => "●",
        "已完成" => "✓",
        "暂停" or "已暂停" => "Ⅱ",
        "下载失败" or "解析失败" => "!",
        _ => "•"
    };

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        this.RaiseAndSetIfChanged(ref field, value, name);
        return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => this.RaisePropertyChanged(name);
    internal static string FormatBytes(long value)
    {
        if (value < 1024) return $"{Math.Max(0, value)} B";
        if (value < 1024 * 1024) return $"{value / 1024d:F1} KB";
        if (value < 1024L * 1024 * 1024) return $"{value / 1024d / 1024:F1} MB";
        return $"{value / 1024d / 1024 / 1024:F2} GB";
    }
}

public sealed record HistoryItem(string Title, string Vid, string Quality, string FilePath, long BytesReceived, DateTimeOffset CompletedAt, string Url = "")
{
    public string SizeLabel => DownloadItem.FormatBytes(BytesReceived);
    public string CompletedAtLabel => CompletedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
}

public sealed class MainWindowViewModel : ReactiveObject, IDisposable
{
    private const string DownloadsPage = "downloads";
    private const string HistoryPage = "history";
    private readonly IVideoResolver _resolver;
    private readonly IVideoDownloader _downloader;
    private readonly IWorkspaceStateStore _stateStore;
    private IWorkspacePlatform _platform;
    private readonly Dictionary<DownloadItem, CancellationTokenSource> _activeDownloads = [];
    private readonly object _activeLock = new();
    private readonly CancellationTokenSource _lifetime = new();
    private static readonly HttpClient CoverClient = new();
    private string _inputText = string.Empty;
    private string _selectedQuality = "1080P";
    private int _selectedConcurrency = 4;
    private string _downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Tangdou Downloads");
    private string _page = DownloadsPage;
    private string _searchText = string.Empty;
    private string _filter = "全部";
    private string _statusMessage = "等待添加糖豆视频链接";
    private bool _isDarkTheme = true;

    public MainWindowViewModel(
        IVideoResolver? resolver = null,
        IVideoDownloader? downloader = null,
        string? dataDirectory = null,
        IWorkspaceStateStore? stateStore = null,
        IWorkspacePlatform? platform = null)
    {
        _resolver = resolver ?? new TangdouVideoService();
        _downloader = downloader ?? new VideoDownloadService();
        _stateStore = stateStore ?? new JsonWorkspaceStateStore(dataDirectory);
        _platform = platform ?? new DefaultWorkspacePlatform();
        LoadState();
        Items.CollectionChanged += OnItemsChanged;
        Refresh();

        // Navigation and filters are UI-only state changes. Keep them synchronous so
        // ReactiveUI does not raise CanExecuteChanged from a worker scheduler.
        ShowDownloadsCommand = ReactiveCommand.Create(() => { Page = DownloadsPage; });
        ShowHistoryCommand = ReactiveCommand.Create(() => { Page = HistoryPage; });
        OpenGithubCommand = ReactiveCommand.Create(() => _platform.OpenPath("https://github.com/SwaggyMacro/TangdouDownloader"));
        PasteClipboardCommand = ReactiveCommand.CreateFromTask(PasteClipboardAsync);
        ClearInputCommand = ReactiveCommand.Create(() => { InputText = string.Empty; });
        BrowseDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseDirectoryAsync);
        ResolveCommand = ReactiveCommand.CreateFromTask(ResolveAndQueueAsync);
        DeleteSelectedCommand = ReactiveCommand.CreateFromTask(DeleteSelectedAsync);
        PauseAllCommand = ReactiveCommand.CreateFromTask(PauseAllAsync);
        StartAllCommand = ReactiveCommand.Create(StartAllInBackground);
        FilterDownloadingCommand = ReactiveCommand.Create(() => SetFilter("下载中"));
        FilterCompletedCommand = ReactiveCommand.Create(() => SetFilter("已完成"));
        FilterQueuedCommand = ReactiveCommand.Create(() => SetFilter("排队中"));
        FilterAllCommand = ReactiveCommand.Create(() => SetFilter("全部"));
        RefreshCommand = ReactiveCommand.Create(Refresh);
        PauseItemCommand = ReactiveCommand.CreateFromTask<DownloadItem>(PauseItemAsync);
        ResumeItemCommand = ReactiveCommand.CreateFromTask<DownloadItem>(ResumeItemAsync);
        RetryItemCommand = ReactiveCommand.CreateFromTask<DownloadItem>(RetryItemAsync);
        CancelItemCommand = ReactiveCommand.CreateFromTask<DownloadItem>(CancelItemAsync);
        PlayItemCommand = ReactiveCommand.Create<DownloadItem>(item => { if (item.CanOpenFile) _platform.OpenPath(item.FilePath!); });
        OpenItemDirectoryCommand = ReactiveCommand.Create<DownloadItem>(item => { if (item.FilePath is not null) _platform.OpenPath(Path.GetDirectoryName(item.FilePath)!); });
        CopyHistoryUrlCommand = ReactiveCommand.CreateFromTask<HistoryItem>(CopyHistoryUrlAsync);
        OpenHistoryDirectoryCommand = ReactiveCommand.Create<HistoryItem>(item =>
        {
            var directory = Path.GetDirectoryName(item.FilePath);
            if (!string.IsNullOrWhiteSpace(directory)) _platform.OpenPath(directory);
        });

        ObserveCommandErrors(ShowDownloadsCommand, "切换页面失败");
        ObserveCommandErrors(ShowHistoryCommand, "切换页面失败");
        ObserveCommandErrors(FilterDownloadingCommand, "筛选任务失败");
        ObserveCommandErrors(FilterCompletedCommand, "筛选任务失败");
        ObserveCommandErrors(FilterQueuedCommand, "筛选任务失败");
        ObserveCommandErrors(FilterAllCommand, "筛选任务失败");
        ObserveCommandErrors(PasteClipboardCommand, "读取剪贴板失败");
        ObserveCommandErrors(BrowseDirectoryCommand, "选择保存目录失败");
        ObserveCommandErrors(ResolveCommand, "解析任务失败");
        ObserveCommandErrors(RetryItemCommand, "重试任务失败");
        ObserveCommandErrors(CopyHistoryUrlCommand, "复制视频地址失败");
    }

    public ObservableCollection<DownloadItem> Items { get; } = [];
    // The grid deliberately observes the source collection directly.  A copied collection
    // can miss collection-change notifications while an async parse is completing.
    public IReadOnlyList<DownloadItem> FilteredItems => Items.Where(item =>
    {
        var statusMatches = _filter switch
        {
            "下载中" => item.Status == "下载中",
            "已完成" => item.Status == "已完成",
            "排队中" => item.Status is "排队中" or "解析中" or "已暂停",
            _ => true
        };
        return statusMatches && MatchesSearch(item);
    }).ToList();
    public ObservableCollection<HistoryItem> HistoryItems { get; } = [];
    public IReadOnlyList<string> Qualities { get; } = ["1080P", "720P", "540P", "360P"];
    public IReadOnlyList<int> ConcurrencyOptions { get; } = [1, 2, 3, 4, 6, 8];
    public string InputText { get => _inputText; set { if (SetField(ref _inputText, value)) OnPropertyChanged(nameof(RecognizedCount)); } }
    public int RecognizedCount => DownloaderUtils.ExtractVids(InputText).Count;
    public string SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            if (SetField(ref _selectedQuality, value)) SaveSettings();
        }
    }
    public int SelectedConcurrency { get => _selectedConcurrency; set { if (SetField(ref _selectedConcurrency, Math.Clamp(value, 1, 8))) { OnPropertyChanged(nameof(WorkerPoolLabel)); SaveSettings(); } } }
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set
        {
            if (!SetField(ref _downloadDirectory, value)) return;
            OnPropertyChanged(nameof(DiskFreeLabel));
            SaveSettings();
        }
    }
    public string SearchText { get => _searchText; set { if (SetField(ref _searchText, value)) Refresh(); } }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public bool IsDownloadPage => Page == DownloadsPage;
    public bool IsHistoryPage => Page == HistoryPage;
    public bool IsDownloadsSelected => IsDownloadPage;
    public bool IsHistorySelected => IsHistoryPage;
    public bool IsAllFilterSelected => _filter == "全部";
    public bool IsDownloadingFilterSelected => _filter == "下载中";
    public bool IsCompletedFilterSelected => _filter == "已完成";
    public bool IsQueuedFilterSelected => _filter == "排队中";
    public bool IsAllTasksSelected
    {
        get => Items.Count > 0 && Items.All(item => item.IsSelected);
        set
        {
            foreach (var item in Items) item.IsSelected = value;
            OnPropertyChanged(nameof(IsAllTasksSelected));
        }
    }
    public string AppVersion => $"v{typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3)}";
    public string AllTaskLabel => $"全部 {Items.Count}";
    public string TaskSummary
    {
        get
        {
            var completed = Items.Count(item => item.Status == "已完成");
            var failed = Items.Count(item => item.Status.EndsWith("失败", StringComparison.Ordinal));
            var paused = Items.Count(item => item.Status == "已暂停");
            return $"共 {Items.Count} 项，完成 {completed}，失败 {failed}，暂停 {paused}";
        }
    }
    public string TotalSpeedLabel => DownloadItem.FormatBytes((long)Items.Sum(item => item.SpeedBytesPerSecond)) + "/s";
    public string DiskFreeLabel => GetFreeSpaceLabel();
    public string WorkerPoolLabel { get { lock (_activeLock) return $"活动 {_activeDownloads.Count} / 并发 {SelectedConcurrency}"; } }

    public ReactiveCommand<RxVoid, RxVoid> ShowDownloadsCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ShowHistoryCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> OpenGithubCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> PasteClipboardCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ClearInputCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> BrowseDirectoryCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> ResolveCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> DeleteSelectedCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> PauseAllCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> StartAllCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> FilterDownloadingCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> FilterCompletedCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> FilterQueuedCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> FilterAllCommand { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> PauseItemCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> ResumeItemCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> RetryItemCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> CancelItemCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> PlayItemCommand { get; }
    public ReactiveCommand<DownloadItem, RxVoid> OpenItemDirectoryCommand { get; }
    public ReactiveCommand<HistoryItem, RxVoid> CopyHistoryUrlCommand { get; }
    public ReactiveCommand<HistoryItem, RxVoid> OpenHistoryDirectoryCommand { get; }

    public void ConfigurePlatformServices(IWorkspacePlatform platform)
    {
        _platform = platform;
        _platform.SetTheme(_isDarkTheme);
    }

    public void UpdateThemePreference(bool isDarkTheme)
    {
        if (_isDarkTheme == isDarkTheme) return;
        _isDarkTheme = isDarkTheme;
        SaveSettings();
    }

    private string Page
    {
        get => _page;
        set
        {
            if (!SetField(ref _page, value)) return;
            OnPropertyChanged(nameof(IsDownloadPage));
            OnPropertyChanged(nameof(IsHistoryPage));
            OnPropertyChanged(nameof(IsDownloadsSelected));
            OnPropertyChanged(nameof(IsHistorySelected));
        }
    }

    private async Task PasteClipboardAsync()
    {
        var text = await _platform.ReadClipboardTextAsync();
        await OnUiAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                StatusMessage = "剪贴板中没有可识别的视频链接";
                return;
            }
            InputText = string.IsNullOrWhiteSpace(InputText) ? text : $"{InputText.TrimEnd()}\n{text}";
            StatusMessage = $"已从剪贴板读取 {DownloaderUtils.ExtractVids(text).Count} 个 VID";
        });
    }

    private async Task BrowseDirectoryAsync()
    {
        var selected = await _platform.PickDownloadDirectoryAsync();
        if (!string.IsNullOrWhiteSpace(selected)) await OnUiAsync(() => DownloadDirectory = selected);
    }

    private async Task ResolveAndQueueAsync()
    {
        try
        {
            var vids = DownloaderUtils.ExtractVids(InputText);
            if (vids.Count == 0)
            {
                await OnUiAsync(() => StatusMessage = "请输入糖豆链接或 VID");
                return;
            }

            List<DownloadItem> workItems = [];
            var duplicateCount = 0;
            await OnUiAsync(() =>
            {
                var existing = Items.Select(item => item.Vid).ToHashSet(StringComparer.Ordinal);
                var pending = vids.Where(existing.Add).ToList();
                duplicateCount = vids.Count - pending.Count;
                if (pending.Count == 0)
                {
                    StatusMessage = $"未加入新任务，{duplicateCount} 项已在列表中";
                    return;
                }

                workItems = pending.Select((vid, index) => new DownloadItem
                {
                    Order = Items.Count + index + 1,
                    Vid = vid,
                    Title = $"糖豆视频 {vid}",
                    Url = string.Empty,
                    Quality = SelectedQuality,
                    Status = "解析中"
                }).ToList();

                // A row is visible before any network operation finishes.
                foreach (var item in workItems) Items.Add(item);
                StatusMessage = $"正在解析 {workItems.Count} 个视频...";
            });

            if (workItems.Count == 0) return;

            using var gate = new SemaphoreSlim(SelectedConcurrency);
            var tasks = workItems.Select(item => ResolveItemAsync(item, gate));
            await Task.WhenAll(tasks);
            var failures = workItems.Count(item => item.Status == "解析失败");
            await OnUiAsync(() =>
            {
                InputText = string.Empty;
                StatusMessage = $"已加入 {workItems.Count - failures} 项，失败 {failures} 项，重复跳过 {duplicateCount} 项";
                Refresh();
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            await OnUiAsync(() => StatusMessage = "解析已取消");
        }
        catch (Exception exception)
        {
            await OnUiAsync(() => StatusMessage = $"解析任务失败: {DescribeFailure(exception)}");
        }
    }

    private async Task ResolveItemAsync(DownloadItem item, SemaphoreSlim gate)
    {
        var enteredGate = false;
        try
        {
            await gate.WaitAsync(_lifetime.Token);
            enteredGate = true;

            var info = await _resolver.ResolveAsync(item.Vid, _lifetime.Token);
            var url = info.SelectUrl(item.Quality);
            if (string.IsNullOrWhiteSpace(url))
                throw new HttpRequestException("接口未返回可下载的视频地址。");

            await OnUiAsync(() =>
            {
                // A canceled parsing row may already have been removed from the grid.
                if (item.Status == "已取消") return;
                item.Title = info.Title;
                item.Url = url;
                item.CoverUrl = info.CoverUrl;
                item.Status = "排队中";
            });
            if (item.Status != "已取消" && !string.IsNullOrWhiteSpace(info.CoverUrl))
                await LoadCoverAsync(item, info.CoverUrl, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            await OnUiAsync(() =>
            {
                item.Status = "已取消";
                item.RemainingTime = "解析已取消";
            });
        }
        catch (Exception exception)
        {
            await OnUiAsync(() =>
            {
                item.Status = "解析失败";
                item.ErrorMessage = exception.Message;
                item.RemainingTime = DescribeFailure(exception);
            });
        }
        finally
        {
            if (enteredGate) gate.Release();
        }
    }

    private void StartAllInBackground()
    {
        // Snapshot UI-owned state before dispatching the network and file work.
        var queue = Items.Where(item => item.Status is "排队中" or "已暂停").ToList();
        var directory = DownloadDirectory;
        var concurrency = SelectedConcurrency;
        _ = Task.Run(() => StartAllAsync(queue, directory, concurrency));
    }

    private async Task StartAllAsync(IReadOnlyList<DownloadItem> queue, string directory, int concurrency)
    {
        try
        {
            if (queue.Count == 0) { await OnUiAsync(() => StatusMessage = "没有可开始的任务"); return; }
            Directory.CreateDirectory(directory);
            using var gate = new SemaphoreSlim(concurrency);
            await Task.WhenAll(queue.Select(async item =>
            {
                await gate.WaitAsync(_lifetime.Token);
                try { await DownloadItemAsync(item, directory); }
                finally { gate.Release(); }
            }));
            await OnUiAsync(() =>
            {
                OnPropertyChanged(nameof(TaskSummary));
                OnPropertyChanged(nameof(TotalSpeedLabel));
                OnPropertyChanged(nameof(WorkerPoolLabel));
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            await OnUiAsync(() => StatusMessage = $"启动下载失败: {DescribeFailure(ex)}");
        }
    }

    private async Task ResumeItemAsync(DownloadItem? item)
    {
        if (item is null || !item.CanResume) return;
        await WaitUntilInactiveAsync(item);
        await DownloadItemAsync(item, DownloadDirectory);
    }

    private async Task RetryItemAsync(DownloadItem? item)
    {
        if (item is null || !item.CanRetry) return;

        if (item.Status == "解析失败")
        {
            await OnUiAsync(() =>
            {
                item.ErrorMessage = null;
                item.RemainingTime = "正在重新解析";
                item.Status = "解析中";
            });

            using var gate = new SemaphoreSlim(1, 1);
            await ResolveItemAsync(item, gate);
            await OnUiAsync(() =>
                StatusMessage = item.Status == "解析失败"
                    ? $"重试解析失败: {item.Title}"
                    : $"已重新解析: {item.Title}");
            return;
        }

        await DownloadItemAsync(item, DownloadDirectory);
    }

    private async Task WaitUntilInactiveAsync(DownloadItem item)
    {
        while (true)
        {
            lock (_activeLock)
            {
                if (!_activeDownloads.ContainsKey(item)) return;
            }
            await Task.Delay(50, _lifetime.Token);
        }
    }

    private async Task DownloadItemAsync(DownloadItem item, string directory)
    {
        lock (_activeLock)
        {
            if (_activeDownloads.ContainsKey(item)) return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        lock (_activeLock) _activeDownloads[item] = cancellation;
        await OnUiAsync(() =>
        {
            item.Status = "下载中";
            item.ErrorMessage = null;
            item.SpeedBytesPerSecond = 0;
            OnPropertyChanged(nameof(WorkerPoolLabel));
        });
        var stopwatch = Stopwatch.StartNew();
        long latestBytes = 0;
        long latestTotal = -1;
        long lastProgressTick = 0;
        using var speedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        var speedMonitor = MonitorSpeedAsync(item, () => (Interlocked.Read(ref latestBytes), Interlocked.Read(ref latestTotal)), stopwatch, speedCancellation.Token);
        try
        {
            var progress = new InlineProgress<DownloadProgress>(value =>
            {
                Interlocked.Exchange(ref latestBytes, value.BytesReceived);
                Interlocked.Exchange(ref latestTotal, value.TotalBytes);
                var now = Environment.TickCount64;
                if (value.Percentage >= 100 || now - Interlocked.Read(ref lastProgressTick) >= 120)
                {
                    Interlocked.Exchange(ref lastProgressTick, now);
                    Dispatcher.UIThread.Post(() => ApplyProgress(item, value));
                }
            });
            var result = await _downloader.DownloadAsync(item.Url, item.Title, directory, progress, cancellation.Token);
            await OnUiAsync(() =>
            {
                item.FilePath = result.FilePath;
                item.BytesReceived = result.BytesReceived;
                item.TotalBytes = result.BytesReceived;
                item.Progress = 100;
                item.SpeedBytesPerSecond = 0;
                item.RemainingTime = "已完成";
                item.Status = "已完成";
                AddHistory(item);
                StatusMessage = $"已完成: {item.Title}";
            });
        }
        catch (OperationCanceledException)
        {
            await OnUiAsync(() =>
            {
                if (item.Status != "已取消") item.Status = "已暂停";
                item.RemainingTime = item.Status;
            });
        }
        catch (Exception ex)
        {
            await OnUiAsync(() =>
            {
                item.Status = "下载失败";
                item.ErrorMessage = ex.Message;
                item.RemainingTime = DescribeFailure(ex);
                StatusMessage = $"下载失败: {item.Title} - {item.RemainingTime}";
            });
        }
        finally
        {
            speedCancellation.Cancel();
            try { await speedMonitor; } catch (OperationCanceledException) { }
            lock (_activeLock) { _activeDownloads.Remove(item); }
            cancellation.Dispose();
            await OnUiAsync(() =>
            {
                item.SpeedBytesPerSecond = 0;
                OnPropertyChanged(nameof(WorkerPoolLabel));
                OnPropertyChanged(nameof(TotalSpeedLabel));
                OnPropertyChanged(nameof(TaskSummary));
            });
        }
    }

    private void ApplyProgress(DownloadItem item, DownloadProgress value)
    {
        item.BytesReceived = value.BytesReceived;
        item.TotalBytes = value.TotalBytes;
        item.Progress = value.Percentage;
        if (item.SpeedBytesPerSecond <= 0)
            item.RemainingTime = "正在计算";
        OnPropertyChanged(nameof(TotalSpeedLabel));
    }

    private async Task MonitorSpeedAsync(
        DownloadItem item,
        Func<(long Bytes, long Total)> readProgress,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        long previousBytes = 0;
        var previousAt = stopwatch.Elapsed;
        try
        {
            while (true)
            {
                await Task.Delay(500, cancellationToken);
                var (bytes, total) = readProgress();
                var now = stopwatch.Elapsed;
                var seconds = Math.Max(0.001, (now - previousAt).TotalSeconds);
                var speed = Math.Max(0, (bytes - previousBytes) / seconds);
                previousBytes = bytes;
                previousAt = now;
                await OnUiAsync(() =>
                {
                    item.SpeedBytesPerSecond = speed;
                    item.RemainingTime = total > 0 && speed > 0
                        ? $"剩余 {TimeSpan.FromSeconds(Math.Max(0, total - bytes) / speed):mm\\:ss}"
                        : bytes > 0 ? "正在下载" : "等待数据";
                    OnPropertyChanged(nameof(TotalSpeedLabel));
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static async Task LoadCoverAsync(DownloadItem item, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            using var response = await CoverClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new Bitmap(stream);
            await OnUiAsync(() => item.CoverImage = bitmap);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // A missing cover must not turn a valid video into a failed task.
        }
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private async Task PauseAllAsync()
    {
        List<KeyValuePair<DownloadItem, CancellationTokenSource>> active;
        lock (_activeLock) active = _activeDownloads.ToList();
        await OnUiAsync(() =>
        {
            foreach (var (item, _) in active) item.Status = "已暂停";
        });
        foreach (var (_, token) in active) token.Cancel();
        await Task.WhenAll(active.Select(pair => WaitUntilInactiveAsync(pair.Key)));
        await OnUiAsync(() =>
        {
            StatusMessage = active.Count == 0 ? "没有正在下载的任务" : $"已暂停 {active.Count} 项任务";
            OnPropertyChanged(nameof(TaskSummary));
            OnPropertyChanged(nameof(TotalSpeedLabel));
            OnPropertyChanged(nameof(WorkerPoolLabel));
        });
    }

    private async Task PauseItemAsync(DownloadItem? item)
    {
        if (item is null) return;
        CancellationTokenSource? token;
        lock (_activeLock)
        {
            _activeDownloads.TryGetValue(item, out token);
        }
        await OnUiAsync(() => item.Status = "已暂停");
        token?.Cancel();
        if (token is not null) await WaitUntilInactiveAsync(item);
        await OnUiAsync(() =>
        {
            OnPropertyChanged(nameof(TaskSummary));
            OnPropertyChanged(nameof(TotalSpeedLabel));
            OnPropertyChanged(nameof(WorkerPoolLabel));
        });
    }

    private async Task CancelItemAsync(DownloadItem? item)
    {
        if (item is null) return;
        CancellationTokenSource? token;
        lock (_activeLock)
        {
            _activeDownloads.TryGetValue(item, out token);
        }
        await OnUiAsync(() => item.Status = "已取消");
        token?.Cancel();
        if (token is not null) await WaitUntilInactiveAsync(item);
        await OnUiAsync(() =>
        {
            Items.Remove(item);
            StatusMessage = "任务已取消并从列表移除";
        });
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToList();
        foreach (var item in selected) await CancelItemAsync(item);
        await OnUiAsync(() => StatusMessage = selected.Count == 0 ? "没有选中的任务" : $"已从任务列表删除 {selected.Count} 项");
    }

    private void SetFilter(string filter)
    {
        _filter = filter;
        Refresh();
    }

    private bool MatchesSearch(DownloadItem item)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        return item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Vid.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task CopyHistoryUrlAsync(HistoryItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Url))
        {
            StatusMessage = "该历史记录没有可复制的视频地址";
            return;
        }

        await _platform.SetClipboardTextAsync(item.Url);
        StatusMessage = "视频地址已复制到剪贴板";
    }
    private void Refresh()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Refresh);
            return;
        }

        OnPropertyChanged(nameof(AllTaskLabel));
        OnPropertyChanged(nameof(TaskSummary));
        OnPropertyChanged(nameof(FilteredItems));
        OnPropertyChanged(nameof(IsAllFilterSelected));
        OnPropertyChanged(nameof(IsDownloadingFilterSelected));
        OnPropertyChanged(nameof(IsCompletedFilterSelected));
        OnPropertyChanged(nameof(IsQueuedFilterSelected));
        OnPropertyChanged(nameof(TotalSpeedLabel));
        OnPropertyChanged(nameof(DiskFreeLabel));
        OnPropertyChanged(nameof(WorkerPoolLabel));
    }

    private void AddHistory(DownloadItem item)
    {
        if (item.FilePath is null) return;
        HistoryItems.Insert(0, new HistoryItem(item.Title, item.Vid, item.Quality, item.FilePath, item.BytesReceived, DateTimeOffset.Now, item.Url));
        PersistHistory();
    }

    private static string DescribeFailure(Exception exception)
    {
        var message = exception.Message.Replace(Environment.NewLine, " ").Trim();
        return string.IsNullOrWhiteSpace(message) ? "请求未成功完成" : message;
    }

    private static async Task OnUiAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(action);
    }

    private void ObserveCommandErrors(IHandleObservableErrors command, string operation)
    {
        command.ThrownExceptions.Subscribe(new CommandErrorObserver(exception =>
            Dispatcher.UIThread.Post(() =>
                StatusMessage = $"{operation}: {DescribeFailure(exception)}")));
    }

    private sealed class CommandErrorObserver(Action<Exception> onNext) : IObserver<Exception>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) => onNext(error);
        public void OnNext(Exception value) => onNext(value);
    }

    private void LoadState()
    {
        var state = _stateStore.Load();
        if (state.Settings is { } settings)
        {
            _selectedQuality = Qualities.Contains(settings.Quality) ? settings.Quality : _selectedQuality;
            _selectedConcurrency = ConcurrencyOptions.Contains(settings.Concurrency) ? settings.Concurrency : _selectedConcurrency;
            _downloadDirectory = string.IsNullOrWhiteSpace(settings.DownloadDirectory) ? _downloadDirectory : settings.DownloadDirectory;
            _isDarkTheme = settings.IsDarkTheme;
        }
        foreach (var item in state.History.OrderByDescending(item => item.CompletedAt)) HistoryItems.Add(item);
    }

    private void SaveSettings()
    {
        try { _stateStore.SaveSettings(new WorkspaceSettings(SelectedQuality, SelectedConcurrency, DownloadDirectory, _isDarkTheme)); }
        catch (Exception) { StatusMessage = "设置保存失败"; }
    }

    private void PersistHistory()
    {
        try { _stateStore.SaveHistory(HistoryItems); }
        catch (Exception) { StatusMessage = "历史记录保存失败"; }
    }

    private string GetFreeSpaceLabel()
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(DownloadDirectory));
            return string.IsNullOrWhiteSpace(root) ? "--" : DownloadItem.FormatBytes(new DriveInfo(root).AvailableFreeSpace);
        }
        catch (Exception) { return "--"; }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.NewItems is not null)
            foreach (DownloadItem item in eventArgs.NewItems)
                item.PropertyChanged += (_, args) =>
                {
                    switch (args.PropertyName)
                    {
                        case nameof(DownloadItem.Status):
                            OnPropertyChanged(nameof(TaskSummary));
                            OnPropertyChanged(nameof(WorkerPoolLabel));
                            // Replacing ItemsSource while a download is running makes
                            // Avalonia DataGrid restore its scroll position to the top.
                            // Only filtered views need a new projection on status changes.
                            if (_filter != "全部") Refresh();
                            break;
                        case nameof(DownloadItem.Title):
                        case nameof(DownloadItem.Vid):
                            if (!string.IsNullOrWhiteSpace(_searchText)) Refresh();
                            break;
                        case nameof(DownloadItem.IsSelected):
                            OnPropertyChanged(nameof(IsAllTasksSelected));
                            break;
                        case nameof(DownloadItem.SpeedBytesPerSecond):
                            OnPropertyChanged(nameof(TotalSpeedLabel));
                            break;
                    }
                };
        OnPropertyChanged(nameof(IsAllTasksSelected));
        Refresh();
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        if (_resolver is IDisposable disposable) disposable.Dispose();
        _lifetime.Dispose();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        this.RaiseAndSetIfChanged(ref field, value, name);
        return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => this.RaisePropertyChanged(name);
}
