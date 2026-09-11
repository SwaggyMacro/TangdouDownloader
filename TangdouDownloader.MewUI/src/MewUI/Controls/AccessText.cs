using Aprillz.MewUI.Rendering;
using Aprillz.MewUI.Diagnostics;
using Aprillz.MewUI.Text;

namespace Aprillz.MewUI.Controls;

/// <summary>
/// A text element that parses "_" access key markers. The displayed text has markers removed
/// ("_File" → "File") and an underline is drawn under the access key character while the Window's
/// access keys are active. The raw markup is a bindable property (<see cref="RawTextProperty"/>), and the
/// element registers/unregisters with the Window's AccessKeyManager automatically. Activation is delegated
/// to the owning control via <see cref="UIElement.OnAccessKey"/>.
/// </summary>
internal sealed class AccessText : TextBlockBase
{
    /// <summary>The raw text with "_" markers; the display text strips them.</summary>
    public static readonly MewProperty<string> RawTextProperty =
        MewProperty<string>.Register<AccessText>(nameof(RawText), string.Empty,
            MewPropertyOptions.AffectsLayout,
            static (self, _, _) => self.ApplyRawText());

    private string _display = string.Empty;
    private Window? _registeredWindow;

    /// <summary>
    /// Gets the parsed access key character, or default if none.
    /// </summary>
    public char AccessKey { get; private set; }

    /// <summary>
    /// Gets the index in the display text where the underline should be drawn (-1 if none).
    /// </summary>
    public int UnderlineIndex { get; private set; } = -1;

    /// <summary>
    /// Gets or sets the raw text with "_" markers.
    /// </summary>
    public string RawText
    {
        get => GetValue(RawTextProperty);
        set => SetValue(RawTextProperty, value ?? string.Empty);
    }

    protected override string DisplayText => _display;

    private void ApplyRawText()
    {
        UnregisterAccessKey();

        string rawText = GetValue(RawTextProperty);
        if (AccessKeyHelper.TryParse(rawText, out var key, out var display))
        {
            AccessKey = key;
            UnderlineIndex = AccessKeyHelper.GetUnderlineIndex(rawText);
            _display = display;
        }
        else
        {
            AccessKey = default;
            UnderlineIndex = -1;
            _display = rawText;
        }

        InvalidateTextLayout();
        RegisterAccessKey();
    }

    protected override void OnVisualRootChanged(Element? oldRoot, Element? newRoot)
    {
        base.OnVisualRootChanged(oldRoot, newRoot);
        UnregisterAccessKey();
        RegisterAccessKey();
    }

    private void RegisterAccessKey()
    {
        if (AccessKey == default)
            return;

        var root = FindVisualRoot();
        if (root is not Window window)
            return;

        window.AccessKeyManager.Register(AccessKey, this, OnAccessKey);
        _registeredWindow = window;
    }

    private void UnregisterAccessKey()
    {
        if (_registeredWindow == null) return;
        _registeredWindow.AccessKeyManager.Unregister(this);
        _registeredWindow = null;
    }

    protected override void OnGetTextPaintSpans(IList<TextPaintSpan> output)
    {
        if (UnderlineIndex < 0 || UnderlineIndex >= _display.Length)
            return;

        if (!GetValue(Window.ShowAccessKeysProperty))
            return;

        output.Add(new TextPaintSpan(
            new TextRange(UnderlineIndex, 1),
            Decoration: TextDecoration.Underline));
    }
}
