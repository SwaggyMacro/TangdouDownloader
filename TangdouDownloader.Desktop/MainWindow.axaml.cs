using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Ursa.Controls;
using Avalonia.Platform.Storage;

namespace TangdouDownloader.Desktop;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        Opened += (_, _) => viewModel.ConfigurePlatformServices(new WindowWorkspacePlatform(this));
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += (_, _) => viewModel.UpdateThemePreference(app.ActualThemeVariant == ThemeVariant.Dark);
    }

    private void OnTaskGridBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        e.Cancel = true;
    }

    private sealed class WindowWorkspacePlatform(MainWindow window) : IWorkspacePlatform
    {
        public async Task<string?> ReadClipboardTextAsync() => window.Clipboard is { } clipboard ? await clipboard.TryGetTextAsync() : null;

        public async Task SetClipboardTextAsync(string text)
        {
            if (window.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(text);
        }

        public async Task<string?> PickDownloadDirectoryAsync()
        {
            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择下载保存目录",
                AllowMultiple = false
            });
            return folders.FirstOrDefault()?.TryGetLocalPath();
        }

        public void OpenPath(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception) { }
        }

        public void SetTheme(bool isDarkTheme) => Application.Current?.RequestedThemeVariant = isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
