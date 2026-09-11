using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Extension methods for <see cref="Window"/> that use overlay services.
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    /// Shows a toast notification that auto-dismisses after a duration based on text length.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Toast message.</param>
    public static void ShowToast(this Window window, string text)
    {
        var toast = window.OverlayLayer.GetOrCreateService(
            layer => new ToastService(layer));
        toast.Show(text);
    }

    /// <summary>
    /// Creates and shows a busy indicator overlay.
    /// Dispose the returned <see cref="IBusyIndicator"/> to dismiss.
    /// While active, window content is disabled and closing is prevented.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="message">Initial progress message displayed below the spinner.</param>
    /// <param name="cancellable">If <c>true</c>, an Abort button is shown and <see cref="IBusyIndicator.CancellationToken"/> becomes usable.</param>
    /// <returns>The busy indicator handle.</returns>
    public static IBusyIndicator CreateBusyIndicator(this Window window, string? message = null, bool cancellable = false)
    {
        var service = window.OverlayLayer.GetOrCreateService(
            layer => new BusyIndicatorService(layer));
        return service.Create(message, cancellable);
    }
}
