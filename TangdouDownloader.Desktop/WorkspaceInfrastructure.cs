using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Aprillz.MewUI;

namespace TangdouDownloader.Desktop;

public sealed record WorkspaceSettings(
    string Quality,
    int Concurrency,
    string DownloadDirectory,
    bool IsDarkTheme);

public sealed record WorkspaceState(WorkspaceSettings? Settings, IReadOnlyList<HistoryItem> History);

public interface IWorkspaceStateStore
{
    WorkspaceState Load();
    void SaveSettings(WorkspaceSettings settings);
    void SaveHistory(IReadOnlyCollection<HistoryItem> history);
}

public sealed class JsonWorkspaceStateStore : IWorkspaceStateStore
{
    private readonly string _settingsPath;
    private readonly string _historyPath;

    public JsonWorkspaceStateStore(string? directory = null)
    {
        var root = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TangdouDownloader");
        _settingsPath = Path.Combine(root, "settings.json");
        _historyPath = Path.Combine(root, "history.json");
    }

    public WorkspaceState Load()
    {
        var settings = Read(_settingsPath, WorkspaceJsonSerializerContext.Default.WorkspaceSettings);
        var history = Read(_historyPath, WorkspaceJsonSerializerContext.Default.ListHistoryItem) ?? [];
        return new WorkspaceState(settings, history);
    }

    public void SaveSettings(WorkspaceSettings settings) => Write(_settingsPath, settings, WorkspaceJsonSerializerContext.Default.WorkspaceSettings);

    public void SaveHistory(IReadOnlyCollection<HistoryItem> history) => Write(_historyPath, history.Take(200).ToList(), WorkspaceJsonSerializerContext.Default.ListHistoryItem);

    private static T? Read<T>(string path, JsonTypeInfo<T> typeInfo)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize(File.ReadAllText(path), typeInfo) : default; }
        catch (Exception) { return default; }
    }

    private static void Write<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, typeInfo));
    }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(WorkspaceSettings))]
[JsonSerializable(typeof(List<HistoryItem>))]
internal sealed partial class WorkspaceJsonSerializerContext : JsonSerializerContext;

public interface IWorkspacePlatform
{
    Task<string?> ReadClipboardTextAsync();
    Task SetClipboardTextAsync(string text) => Task.CompletedTask;
    Task<string?> PickDownloadDirectoryAsync(Window? owner = null);
    void OpenPath(string path);
    void SetTheme(bool isDarkTheme);
}

public sealed class DefaultWorkspacePlatform : IWorkspacePlatform
{
    public Task<string?> ReadClipboardTextAsync()
    {
        var clipboard = Application.Current?.PlatformServices.Clipboard;
        return Task.FromResult(
            clipboard is not null && clipboard.TryGetText(out var text) ? text : null);
    }

    public Task SetClipboardTextAsync(string text)
    {
        if (Application.Current?.PlatformServices?.Clipboard is { } clipboard)
            clipboard.TrySetText(text);
        return Task.CompletedTask;
    }

    public async Task<string?> PickDownloadDirectoryAsync(Window? owner = null)
    {
        var selected = await FileDialog.SelectFolderAsync(new FolderDialogOptions
        {
            Title = "选择下载保存目录",
            PreferNative = true,
            Owner = owner,
    });
        return selected;
    }
    public void OpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception) { }
    }

    public void SetTheme(bool isDarkTheme) => Application.Current?.SetTheme(isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light);
}
