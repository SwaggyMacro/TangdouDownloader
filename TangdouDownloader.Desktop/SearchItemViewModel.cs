using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using TangdouDownloader.Core;

namespace TangdouDownloader.Desktop;

/// <summary>
/// One row of the online-search result grid. Covers download lazily so opening the
/// search tab never blocks on image traffic.
/// </summary>
public sealed class SearchItemViewModel : ReactiveObject
{
    private bool _isSelected;
    private Bitmap? _coverImage;

    public required string Vid { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required int DurationSeconds { get; init; }
    public required long Hits { get; init; }
    public required bool HasTeach { get; init; }
    public string? CoverUrl { get; init; }

    public bool IsSelected { get => _isSelected; set => this.RaiseAndSetIfChanged(ref _isSelected, value); }
    public Bitmap? CoverImage
    {
        get => _coverImage;
        set
        {
            if (_coverImage == value) return;
            _coverImage = value;
            this.RaisePropertyChanged(nameof(HasCoverImage));
            this.RaisePropertyChanged(nameof(ShowCoverPlaceholder));
        }
    }
    public bool HasCoverImage => CoverImage is not null;
    public bool ShowCoverPlaceholder => CoverImage is null;
    public string DurationLabel => DurationSeconds > 0 ? $"{TimeSpan.FromSeconds(DurationSeconds):m\\:ss}" : "--";
    public string HitsLabel => Hits switch
    {
        >= 100_000_000 => $"{Hits / 100_000_000d:F1}亿",
        >= 10_000 => $"{Hits / 10_000d:F1}万",
        > 0 => Hits.ToString(),
        _ => "--",
    };
    public string TeachLabel => HasTeach ? "教学" : string.Empty;

    public async Task LoadCoverAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (CoverImage is not null || string.IsNullOrWhiteSpace(CoverUrl)) return;
        try
        {
            using var request = TangdouVideoService.CreateRequest(HttpMethod.Get, CoverUrl);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new Bitmap(stream);
            await Dispatcher.UIThread.InvokeAsync(() => CoverImage = bitmap);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // A missing cover must not break the search results.
        }
    }
}
