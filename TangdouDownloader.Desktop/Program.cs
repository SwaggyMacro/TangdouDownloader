using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace TangdouDownloader.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        RegisterPlatform();

        var viewModel = new MainWindowViewModel();
        var window = new MainWindow(viewModel);
        Application.Run(window, _ =>
        {
            viewModel.ConfigurePlatformServices(new DefaultWorkspacePlatform(), window);
            Application.Current.SetThemeMode(viewModel.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light);
            Application.Current.ThemeChanged += (oldTheme, newTheme) =>
            {
                if (oldTheme.IsDark != newTheme.IsDark) viewModel.UpdateThemePreference(newTheme.IsDark);
            };
        });
    }

    private static void RegisterPlatform()
    {
#pragma warning disable CA1416
        if (OperatingSystem.IsWindows())
        {
            Win32Platform.Register();
            GdiBackend.Register();
        }
        else if (OperatingSystem.IsMacOS())
        {
            MacOSPlatform.Register();
            MewVGMacOSBackend.Register();
        }
        else if (OperatingSystem.IsLinux())
        {
            X11Platform.Register();
            MewVGX11Backend.Register();
        }
        else
        {
            throw new PlatformNotSupportedException("TangdouDownloader supports Windows, Linux and macOS.");
        }
#pragma warning restore CA1416
    }
}

