using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Avalonia;
using Avalonia.Styling;

namespace TangdouDownloader.Desktop;

public sealed record WorkspaceSettings(
    string Quality,
    int Concurrency,
    string DownloadDirectory,
    bool IsDarkTheme,
    int DownloadThreads = 2);

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
    Task<string?> PickDownloadDirectoryAsync();
    void OpenPath(string path);
    void SetTheme(bool isDarkTheme);
}

public sealed class DefaultWorkspacePlatform : IWorkspacePlatform
{
    public Task<string?> ReadClipboardTextAsync() => Task.FromResult<string?>(null);
    public Task SetClipboardTextAsync(string text) => Task.CompletedTask;
    public Task<string?> PickDownloadDirectoryAsync() => Task.FromResult<string?>(null);
    public void OpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception) { }
    }

    public void SetTheme(bool isDarkTheme) => Application.Current?.RequestedThemeVariant = isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
}
