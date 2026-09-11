using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Aprillz.MewUI;
using TangdouDownloader.Core;

namespace TangdouDownloader.Desktop;

public sealed class DownloadItem : INotifyPropertyChanged
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
    private ImageSource? _coverImage;
    private string _title = string.Empty;
    private string _url = string.Empty;

    public int Order { get; init; }
    public required string Vid { get; init; }
    public required string Title { get => _title; set => SetField(ref _title, value); }
    public required string Url { get => _url; set => SetField(ref _url, value); }
    public string OriginalUrl { get; init; } = string.Empty;
    public required string Quality { get; init; }
    public int DownloadThreads { get; set; } = 1;
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public bool? IsSelectedNullable
    {
        get => _isSelected;
        set => IsSelected = value ?? false;
    }
    public int Progress { get => _progress; set { if (SetField(ref _progress, value)) OnPropertyChanged(nameof(ProgressValue)); OnPropertyChanged(nameof(ProgressLabel)); } }
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
            OnPropertyChanged(nameof(StatusBackground));
            OnPropertyChanged(nameof(StatusForeground));
            OnPropertyChanged(nameof(StatusBorder));
        }
    }
    public string RemainingTime { get => _remainingTime; set { if (SetField(ref _remainingTime, value)) OnPropertyChanged(nameof(StatusDetail)); } }
    public string? FilePath { get => _filePath; set { if (SetField(ref _filePath, value)) OnPropertyChanged(nameof(CanOpenFile)); } }
    public string? ErrorMessage { get => _errorMessage; set { if (SetField(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(StatusDetail)); } } }
    public string? CoverUrl { get => _coverUrl; set => SetField(ref _coverUrl, value); }
    public ImageSource? CoverImage
    {
        get => _coverImage;
        set
        {
            if (!SetField(ref _coverImage, value)) return;
            OnPropertyChanged(nameof(CoverImageSource));
            OnPropertyChanged(nameof(HasCoverImage));
            OnPropertyChanged(nameof(ShowCoverPlaceholder));
        }
    }
    public bool HasCoverImage => CoverImage is not null;
    public bool ShowCoverPlaceholder => CoverImage is null;
    public IImageSource? CoverImageSource => CoverImage;
    public string VidText => Vid;
    public string QualityFormat => $"{Quality} / MP4";
    public string ProgressLabel => TotalBytes > 0 ? $"{Progress}%" : BytesReceived > 0 ? "下载中" : "等待中";
    public double ProgressValue => Math.Clamp(Progress, 0, 100);
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
    public Color StatusBackground => Status switch
    {
        "下载中" or "解析中" => ThemePalette.AccentSoft,
        "已完成" => ThemePalette.SuccessSoft,
        "已暂停" => ThemePalette.WarningSoft,
        "下载失败" or "解析失败" => ThemePalette.DangerSoft,
        _ => ThemePalette.Soft,
    };
    public Color StatusForeground => Status switch
    {
        "下载中" or "解析中" => ThemePalette.Accent,
        "已完成" => ThemePalette.Success,
        "已暂停" => ThemePalette.Warning,
        "下载失败" or "解析失败" => ThemePalette.Danger,
        _ => ThemePalette.TextSecondary,
    };
    public Color StatusBorder => StatusBackground;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;
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

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private const string DownloadsPage = "downloads";
    private const string HistoryPage = "history";
    private const int CoverThumbnailWidth = 168;
    private const int CoverThumbnailHeight = 104;
    private const double CoverThumbnailCornerRadius = 16;
    private readonly IVideoResolver _resolver;
    private readonly IVideoDownloader _downloader;
    private readonly IWorkspaceStateStore _stateStore;
    private IWorkspacePlatform _platform;
    private Window? _owner;
    private readonly Dictionary<DownloadItem, CancellationTokenSource> _activeDownloads = [];
    private readonly object _activeLock = new();
    private readonly CancellationTokenSource _lifetime = new();
    private SynchronizationContext? _uiContext = SynchronizationContext.Current;
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
            if (!SetField(ref _selectedQuality, value)) return;
            SaveSettings();
            ShowToast($"清晰度已设为 {value}");
        }
    }
    public int SelectedConcurrency
    {
        get => _selectedConcurrency;
        set
        {
            if (!SetField(ref _selectedConcurrency, Math.Clamp(value, 1, 8))) return;
            OnPropertyChanged(nameof(WorkerPoolLabel));
            SaveSettings();
            ShowToast($"同时下载数已设为 {_selectedConcurrency}");
        }
    }
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
    public event Action<string>? ToastRequested;
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
            OnPropertyChanged(nameof(IsAllTasksSelectedNullable));
        }
    }
    public bool? IsAllTasksSelectedNullable
    {
        get => Items.Count == 0 ? false : Items.All(item => item.IsSelected) ? true : Items.Any(item => item.IsSelected) ? null : false;
        set => IsAllTasksSelected = value == true;
    }
    public string AppVersion => $"v{typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3)}";
    public bool IsDarkTheme => _isDarkTheme;
    public string AllTaskLabel => $"全部 {Items.Count}";
    public Color NavBackground => IsDownloadPage ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color NavForeground => IsDownloadPage ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color DownloadsBackground => IsDownloadPage ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color DownloadsForeground => IsDownloadPage ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color HistoryBackground => IsHistoryPage ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color HistoryForeground => IsHistoryPage ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color AllFilterBackground => IsAllFilterSelected ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color AllFilterForeground => IsAllFilterSelected ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color DownloadingFilterBackground => IsDownloadingFilterSelected ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color DownloadingFilterForeground => IsDownloadingFilterSelected ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color CompletedFilterBackground => IsCompletedFilterSelected ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color CompletedFilterForeground => IsCompletedFilterSelected ? ThemePalette.Accent : ThemePalette.TextSecondary;
    public Color QueuedFilterBackground => IsQueuedFilterSelected ? ThemePalette.AccentSoft : ThemePalette.Transparent;
    public Color QueuedFilterForeground => IsQueuedFilterSelected ? ThemePalette.Accent : ThemePalette.TextSecondary;
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
    public string WorkerPoolLabel { get { lock (_activeLock) return $"活动 {_activeDownloads.Count} / 同时 {SelectedConcurrency}"; } }

    public void ShowDownloads() => Page = DownloadsPage;
    public void ShowHistory() => Page = HistoryPage;
    public void OpenGithub() => _platform.OpenPath("https://github.com/SwaggyMacro/TangdouDownloader");
    public void ClearInput() => InputText = string.Empty;
    public void StartAll() => StartAllInBackground();
    public void FilterDownloading() => SetFilter("下载中");
    public void FilterCompleted() => SetFilter("已完成");
    public void FilterQueued() => SetFilter("排队中");
    public void FilterAll() => SetFilter("全部");
    public void PlayItem(DownloadItem item) { if (item.CanOpenFile) _platform.OpenPath(item.FilePath!); }
    public void OpenItemDirectory(DownloadItem item) { if (item.FilePath is not null) _platform.OpenPath(Path.GetDirectoryName(item.FilePath)!); }
    public void OpenHistoryDirectory(HistoryItem item)
    {
        var directory = Path.GetDirectoryName(item.FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) _platform.OpenPath(directory);
    }

    public async void PasteClipboard()
    {
        try { await PasteClipboardAsync(); }
        catch (Exception exception) { ShowToast($"读取剪贴板失败: {DescribeFailure(exception)}"); }
    }

    public async void BrowseDirectory()
    {
        try { await BrowseDirectoryAsync(); }
        catch (Exception exception) { ShowToast($"选择目录失败: {DescribeFailure(exception)}"); }
    }

    public async void ResolveAndQueue()
    {
        try { await ResolveAndQueueAsync(); }
        catch (Exception exception) { ShowToast($"加入队列失败: {DescribeFailure(exception)}"); }
    }

    public void RefreshTasks() => Refresh();

    public async void PauseAll()
    {
        try { await PauseAllAsync(); }
        catch (Exception exception) { ShowToast($"暂停失败: {DescribeFailure(exception)}"); }
    }

    public async void PauseItem(DownloadItem? item)
    {
        try { await PauseItemAsync(item); }
        catch (Exception exception) { ShowToast($"暂停失败: {DescribeFailure(exception)}"); }
    }

    public async void ResumeItem(DownloadItem? item)
    {
        try { await ResumeItemAsync(item); }
        catch (Exception exception) { ShowToast($"继续下载失败: {DescribeFailure(exception)}"); }
    }

    public async void RetryItem(DownloadItem? item)
    {
        try { await RetryItemAsync(item); }
        catch (Exception exception) { ShowToast($"重试失败: {DescribeFailure(exception)}"); }
    }

    public async void CancelItem(DownloadItem? item)
    {
        try { await CancelItemAsync(item); }
        catch (Exception exception) { ShowToast($"取消任务失败: {DescribeFailure(exception)}"); }
    }

    public async void DeleteSelected()
    {
        try { await DeleteSelectedAsync(); }
        catch (Exception exception) { ShowToast($"删除任务失败: {DescribeFailure(exception)}"); }
    }

    public async void CopyHistoryUrl(HistoryItem? item)
    {
        try { await CopyHistoryUrlAsync(item); }
        catch (Exception exception) { ShowToast($"复制地址失败: {DescribeFailure(exception)}"); }
    }

    public void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        OnPropertyChanged(nameof(IsDarkTheme));
        _platform.SetTheme(_isDarkTheme);
        SaveSettings();
        ShowToast(_isDarkTheme ? "已切换到深色主题" : "已切换到浅色主题");
    }

    public void ConfigurePlatformServices(IWorkspacePlatform platform, Window? owner = null)
    {
        _platform = platform;
        _owner = owner;
        _uiContext = SynchronizationContext.Current;
        _platform.SetTheme(_isDarkTheme);
    }

    public void UpdateThemePreference(bool isDarkTheme)
    {
        if (_isDarkTheme == isDarkTheme) return;
        _isDarkTheme = isDarkTheme;
        OnPropertyChanged(nameof(IsDarkTheme));
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
            OnPropertyChanged(nameof(NavBackground));
            OnPropertyChanged(nameof(NavForeground));
            OnPropertyChanged(nameof(DownloadsBackground));
            OnPropertyChanged(nameof(DownloadsForeground));
            OnPropertyChanged(nameof(HistoryBackground));
            OnPropertyChanged(nameof(HistoryForeground));
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
                ShowToast("剪贴板中没有可识别的视频链接");
                return;
            }
            InputText = string.IsNullOrWhiteSpace(InputText) ? text : $"{InputText.TrimEnd()}\n{text}";
            ShowToast($"已从剪贴板读取 {DownloaderUtils.ExtractVids(text).Count} 个 VID");
        });
    }

    private async Task BrowseDirectoryAsync()
    {
        var selected = await _platform.PickDownloadDirectoryAsync(_owner);
        if (!string.IsNullOrWhiteSpace(selected))
            await OnUiAsync(() =>
            {
                DownloadDirectory = selected;
                ShowToast("保存目录已更新");
            });
    }

    private async Task ResolveAndQueueAsync()
    {
        try
        {
            var vids = DownloaderUtils.ExtractVids(InputText);
            var originalUrls = BuildOriginalUrls(InputText);
            if (vids.Count == 0)
            {
                await OnUiAsync(() => ShowToast("请输入糖豆链接或 VID"));
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
                    ShowToast($"未加入新任务，{duplicateCount} 项已在列表中");
                    return;
                }

                workItems = pending.Select((vid, index) => new DownloadItem
                {
                    Order = Items.Count + index + 1,
                    Vid = vid,
                    Title = $"糖豆视频 {vid}",
                    Url = string.Empty,
                    OriginalUrl = originalUrls.TryGetValue(vid, out var originalUrl) ? originalUrl : vid,
                    Quality = SelectedQuality,
                    Status = "解析中"
                }).ToList();

                // A row is visible before any network operation finishes.
                foreach (var item in workItems) Items.Add(item);
                ShowToast($"正在解析 {workItems.Count} 个视频...");
            });

            if (workItems.Count == 0) return;

            using var gate = new SemaphoreSlim(SelectedConcurrency);
            var tasks = workItems.Select(item => ResolveItemAsync(item, gate));
            await Task.WhenAll(tasks);
            var failures = workItems.Count(item => item.Status == "解析失败");
            await OnUiAsync(() =>
            {
                InputText = string.Empty;
                ShowToast($"已加入 {workItems.Count - failures} 项，失败 {failures} 项，重复跳过 {duplicateCount} 项");
                Refresh();
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            await OnUiAsync(() => ShowToast("解析已取消"));
        }
        catch (Exception exception)
        {
            await OnUiAsync(() => ShowToast($"解析任务失败: {DescribeFailure(exception)}"));
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
                item.TotalBytes = info.SelectSize(item.Quality);
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
        catch (OperationCanceledException)
        {
            await OnUiAsync(() =>
            {
                item.Status = "已暂停";
                item.RemainingTime = "解析已暂停";
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
        if (queue.Count == 0)
        {
            ShowToast("没有可开始的任务");
            return;
        }
        ShowToast($"开始下载 {queue.Count} 项任务");
        _ = Task.Run(() => StartAllAsync(queue, directory, concurrency));
    }

    private async Task StartAllAsync(IReadOnlyList<DownloadItem> queue, string directory, int concurrency)
    {
        try
        {
            if (queue.Count == 0) return;
            Directory.CreateDirectory(directory);
            using var gate = new SemaphoreSlim(concurrency);
            await Task.WhenAll(queue.Select(async item =>
            {
                await gate.WaitAsync(_lifetime.Token);
                try
                {
                    var itemThreads = item.Status == "已暂停" ? item.DownloadThreads : 1;
                    await DownloadItemAsync(item, directory, itemThreads);
                }
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
            await OnUiAsync(() => ShowToast($"启动下载失败: {DescribeFailure(ex)}"));
        }
    }

    private async Task ResumeItemAsync(DownloadItem? item)
    {
        if (item is null || !item.CanResume) return;
        await WaitUntilInactiveAsync(item);
        await OnUiAsync(() => ShowToast($"继续下载: {item.Title}"));
        await DownloadItemAsync(item, DownloadDirectory, item.DownloadThreads);
    }

    private async Task RetryItemAsync(DownloadItem? item)
    {
        if (item is null || !item.CanRetry) return;

        await OnUiAsync(() =>
        {
            item.ErrorMessage = null;
            item.RemainingTime = "正在重新解析";
            item.Status = "解析中";
            ShowToast($"正在重新解析: {item.Title}");
        });

        using var gate = new SemaphoreSlim(1, 1);
        await ResolveItemAsync(item, gate);
        if (item.Status == "解析失败")
        {
            await OnUiAsync(() => ShowToast($"重试解析失败: {item.Title}"));
            return;
        }

        if (item.Status != "排队中" || string.IsNullOrWhiteSpace(item.Url)) return;
        await OnUiAsync(() => ShowToast($"已重新解析，开始下载: {item.Title}"));
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

    private async Task DownloadItemAsync(DownloadItem item, string directory, int downloadThreads = 1)
    {
        lock (_activeLock)
        {
            if (_activeDownloads.ContainsKey(item)) return;
        }

        item.DownloadThreads = downloadThreads;
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
                    PostToUi(() => ApplyProgress(item, value));
                }
            });
            var result = await _downloader.DownloadAsync(
                item.Url,
                item.Title,
                directory,
                progress,
                cancellation.Token,
                downloadThreads);
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
                ShowToast($"已完成: {item.Title}");
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
                ShowToast($"下载失败: {item.Title} - {item.RemainingTime}");
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

    private async Task LoadCoverAsync(DownloadItem item, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "image/png,image/jpeg,image/*;q=0.8,*/*;q=0.5");
            using var response = await CoverClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var bitmap = CreateRoundedThumbnail(TryCreateImageSource(bytes));
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
            ShowToast(active.Count == 0 ? "没有正在下载的任务" : $"已暂停 {active.Count} 项任务");
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
            ShowToast($"已暂停: {item.Title}");
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
            ShowToast("任务已取消并从列表移除");
        });
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToList();
        foreach (var item in selected) await CancelItemAsync(item);
        await OnUiAsync(() => ShowToast(selected.Count == 0 ? "没有选中的任务" : $"已从任务列表删除 {selected.Count} 项"));
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
            await OnUiAsync(() => ShowToast("该历史记录没有可复制的视频地址"));
            return;
        }

        await _platform.SetClipboardTextAsync(item.Url);
        await OnUiAsync(() => ShowToast("视频地址已复制到剪贴板"));
    }
    private void Refresh()
    {
        if (!ReferenceEquals(SynchronizationContext.Current, _uiContext))
        {
            PostToUi(Refresh);
            return;
        }

        OnPropertyChanged(nameof(AllTaskLabel));
        OnPropertyChanged(nameof(TaskSummary));
        OnPropertyChanged(nameof(FilteredItems));
        OnPropertyChanged(nameof(IsAllFilterSelected));
        OnPropertyChanged(nameof(IsDownloadingFilterSelected));
        OnPropertyChanged(nameof(IsCompletedFilterSelected));
        OnPropertyChanged(nameof(IsQueuedFilterSelected));
        OnPropertyChanged(nameof(AllFilterBackground));
        OnPropertyChanged(nameof(AllFilterForeground));
        OnPropertyChanged(nameof(DownloadingFilterBackground));
        OnPropertyChanged(nameof(DownloadingFilterForeground));
        OnPropertyChanged(nameof(CompletedFilterBackground));
        OnPropertyChanged(nameof(CompletedFilterForeground));
        OnPropertyChanged(nameof(QueuedFilterBackground));
        OnPropertyChanged(nameof(QueuedFilterForeground));
        OnPropertyChanged(nameof(TotalSpeedLabel));
        OnPropertyChanged(nameof(DiskFreeLabel));
        OnPropertyChanged(nameof(WorkerPoolLabel));
    }

    private void AddHistory(DownloadItem item)
    {
        if (item.FilePath is null) return;
        var originalUrl = string.IsNullOrWhiteSpace(item.OriginalUrl) ? item.Vid : item.OriginalUrl;
        HistoryItems.Insert(0, new HistoryItem(item.Title, item.Vid, item.Quality, item.FilePath, item.BytesReceived, DateTimeOffset.Now, originalUrl));
        PersistHistory();
    }

    private static ImageSource? TryCreateImageSource(byte[] bytes)
    {
        if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return ImageSource.FromBytes(bytes);
        if (bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ImageSource.FromBytes(bytes);
        if (bytes.Length > 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
            return ImageSource.FromBytes(bytes);

        if (ImageConversion.TryDecodeWebPToBmp(bytes, out var bmp))
            return ImageSource.FromBytes(bmp);
        return null;
    }

    private static ImageSource? CreateRoundedThumbnail(ImageSource? source)
    {
        if (source is null) return null;

        try
        {
            var sourceWidth = source.PixelWidth;
            var sourceHeight = source.PixelHeight;
            if (sourceWidth <= 0 || sourceHeight <= 0) return null;

            var sourcePixels = new byte[checked(sourceWidth * sourceHeight * 4)];
            source.CopyPixels(sourcePixels, sourceWidth * 4);

            var result = new byte[CoverThumbnailWidth * CoverThumbnailHeight * 4];
            var scale = Math.Max(
                CoverThumbnailWidth / (double)sourceWidth,
                CoverThumbnailHeight / (double)sourceHeight);
            var cropWidth = CoverThumbnailWidth / scale;
            var cropHeight = CoverThumbnailHeight / scale;
            var cropLeft = (sourceWidth - cropWidth) / 2;
            var cropTop = (sourceHeight - cropHeight) / 2;

            for (var y = 0; y < CoverThumbnailHeight; y++)
            {
                var sourceY = cropTop + ((y + 0.5) * cropHeight / CoverThumbnailHeight) - 0.5;
                for (var x = 0; x < CoverThumbnailWidth; x++)
                {
                    var sourceX = cropLeft + ((x + 0.5) * cropWidth / CoverThumbnailWidth) - 0.5;
                    var destinationOffset = (y * CoverThumbnailWidth + x) * 4;
                    SampleBilinear(sourcePixels, sourceWidth, sourceHeight, sourceX, sourceY, result, destinationOffset);
                    result[destinationOffset + 3] = (byte)(result[destinationOffset + 3] * RoundedCornerCoverage(x, y));
                }
            }

            return ImageSource.FromBgraPixels(CoverThumbnailWidth, CoverThumbnailHeight, result, hasAlpha: true);
        }
        catch (Exception)
        {
            return source;
        }
    }

    private static void SampleBilinear(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        double sourceX,
        double sourceY,
        byte[] destination,
        int destinationOffset)
    {
        var left = Math.Clamp((int)Math.Floor(sourceX), 0, sourceWidth - 1);
        var top = Math.Clamp((int)Math.Floor(sourceY), 0, sourceHeight - 1);
        var right = Math.Min(left + 1, sourceWidth - 1);
        var bottom = Math.Min(top + 1, sourceHeight - 1);
        var horizontal = Math.Clamp(sourceX - left, 0, 1);
        var vertical = Math.Clamp(sourceY - top, 0, 1);

        for (var channel = 0; channel < 4; channel++)
        {
            var topValue = source[(top * sourceWidth + left) * 4 + channel] * (1 - horizontal) +
                           source[(top * sourceWidth + right) * 4 + channel] * horizontal;
            var bottomValue = source[(bottom * sourceWidth + left) * 4 + channel] * (1 - horizontal) +
                              source[(bottom * sourceWidth + right) * 4 + channel] * horizontal;
            destination[destinationOffset + channel] = (byte)Math.Round(topValue * (1 - vertical) + bottomValue * vertical);
        }
    }

    private static double RoundedCornerCoverage(int x, int y)
    {
        var pixelX = x + 0.5;
        var pixelY = y + 0.5;
        var cornerX = pixelX < CoverThumbnailCornerRadius
            ? CoverThumbnailCornerRadius
            : pixelX > CoverThumbnailWidth - CoverThumbnailCornerRadius
                ? CoverThumbnailWidth - CoverThumbnailCornerRadius
                : pixelX;
        var cornerY = pixelY < CoverThumbnailCornerRadius
            ? CoverThumbnailCornerRadius
            : pixelY > CoverThumbnailHeight - CoverThumbnailCornerRadius
                ? CoverThumbnailHeight - CoverThumbnailCornerRadius
                : pixelY;
        var distance = Math.Sqrt(Math.Pow(pixelX - cornerX, 2) + Math.Pow(pixelY - cornerY, 2));
        return Math.Clamp(CoverThumbnailCornerRadius + 0.5 - distance, 0, 1);
    }

    private static Dictionary<string, string> BuildOriginalUrls(string input)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var vid in DownloaderUtils.ExtractVids(line)) result.TryAdd(vid, line);
        }

        var fallback = input.Trim();
        foreach (var vid in DownloaderUtils.ExtractVids(input)) result.TryAdd(vid, fallback);
        return result;
    }

    private static string DescribeFailure(Exception exception)
    {
        var message = exception.Message.Replace(Environment.NewLine, " ").Trim();
        return string.IsNullOrWhiteSpace(message) ? "请求未成功完成" : message;
    }

    private async Task OnUiAsync(Action action)
    {
        if (ReferenceEquals(SynchronizationContext.Current, _uiContext) || _uiContext is null)
        {
            action();
            return;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _uiContext.Post(_ =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception exception) { completion.SetException(exception); }
        }, null);
        await completion.Task;
    }

    private void ShowToast(string message)
    {
        StatusMessage = message;
        ToastRequested?.Invoke(message);
    }

    private void PostToUi(Action action)
    {
        if (ReferenceEquals(SynchronizationContext.Current, _uiContext) || _uiContext is null)
            action();
        else
            _uiContext.Post(_ => action(), null);
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
        catch (Exception) { ShowToast("设置保存失败"); }
    }

    private void PersistHistory()
    {
        try { _stateStore.SaveHistory(HistoryItems); }
        catch (Exception) { ShowToast("历史记录保存失败"); }
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
                            // Replacing ItemsSource while a download is running can
                            // restore a grid's scroll position to the top.
                            // Only filtered views need a new projection on status changes.
                            if (_filter != "全部") Refresh();
                            break;
                        case nameof(DownloadItem.Title):
                        case nameof(DownloadItem.Vid):
                            if (!string.IsNullOrWhiteSpace(_searchText)) Refresh();
                            break;
                        case nameof(DownloadItem.IsSelected):
                            OnPropertyChanged(nameof(IsAllTasksSelected));
                            OnPropertyChanged(nameof(IsAllTasksSelectedNullable));
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
        field = value;
        OnPropertyChanged(name);
        return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
