namespace Aprillz.MewUI.Platform;

/// <summary>
/// The services the running platform offers. Only what a caller outside the framework has business
/// with: the platform host itself stays internal, so its lifecycle, its window and dispatcher
/// creation, and its DPI and font queries, which <see cref="DpiHelper"/> and the theme's metrics
/// already surface, do not travel out with them.
/// </summary>
public sealed class PlatformServices
{
    private readonly IPlatformHost _host;

    internal PlatformServices(IPlatformHost host) => _host = host;

    /// <summary>Reads and writes the system clipboard. Null where the platform offers none.</summary>
    public IClipboardService? Clipboard => _host.Clipboard;

    /// <summary>Shows the platform's message boxes. Null where the platform offers none.</summary>
    public IMessageBoxService? MessageBox => _host.MessageBox;

    /// <summary>Shows the platform's file dialogs. Null where the platform offers none.</summary>
    public IFileDialogService? FileDialog => _host.FileDialog;

    /// <summary>
    /// The shell's file-type icons. Never null: a platform without them answers with nothing for
    /// every icon, so a caller draws its own without having to ask whether the service is there.
    /// </summary>
    public IShellIconProvider ShellIconProvider => _host.ShellIconProvider;
}
