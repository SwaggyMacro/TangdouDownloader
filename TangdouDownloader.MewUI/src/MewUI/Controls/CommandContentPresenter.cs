namespace Aprillz.MewUI.Controls;

/// <summary>
/// Materializes command presentation for controls that opt into generated command content.
/// </summary>
internal sealed class CommandContentPresenter : StackPanel
{
    internal CommandContentPresenter()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 6;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        IsHitTestVisible = false;
    }

    internal void Update(
        CommandPresentation presentation,
        CommandPresentationMode mode,
        IconTemplateSize iconSize)
    {
        Clear();

        bool showIcon = mode is CommandPresentationMode.Icon or CommandPresentationMode.TextAndIcon;
        bool showText = mode is CommandPresentationMode.Text or CommandPresentationMode.TextAndIcon;

        if (showIcon && presentation.Icon is IconTemplate iconTemplate)
        {
            var icon = iconTemplate.Build(iconSize);
            icon.Width = iconSize.Dip;
            icon.Height = iconSize.Dip;
            icon.IsHitTestVisible = false;
            Add(icon);
        }

        if (showText && presentation.AccessText is string accessText)
        {
            Add(new AccessText
            {
                RawText = accessText,
                IsHitTestVisible = false,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            });
        }
    }
}
