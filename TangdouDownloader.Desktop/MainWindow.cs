using System.ComponentModel;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace TangdouDownloader.Desktop;

public sealed class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private GridView _taskGrid = null!;
    private GridView _historyGrid = null!;
    private ComboBox _qualityCombo = null!;
    private ComboBox _concurrencyCombo = null!;
    private GlyphElement _minimizeGlyph = null!;
    private GlyphElement _maximizeGlyph = null!;
    private GlyphElement _closeGlyph = null!;
    private TextBlock _githubGlyph = null!;
    private TextBlock _themeToggleGlyph = null!;
    private TextBlock _titleBarTitle = null!;
    private Button _githubButton = null!;
    private Button _themeToggleButton = null!;
    private Button _minimizeButton = null!;
    private Button _maximizeButton = null!;
    private Button _closeButton = null!;
    private Border _statusBar = null!;
    private CheckBox _selectAllCheck = null!;
    private bool _syncingSelectAllCheck;

    // Keep the custom client-area title bar readable in both application themes.
    private static readonly Color TitleBarBackground = Color.FromRgb(38, 42, 48);
    private static readonly Color TitleBarText = Color.FromRgb(255, 255, 255);

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.ToastRequested += ShowToast;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.FilteredItems))
                _taskGrid.ItemsSource = CreateTaskItems();
            if (e.PropertyName == nameof(MainWindowViewModel.IsAllTasksSelectedNullable))
                SyncSelectAllCheck();
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedQuality))
                SelectCombo(_qualityCombo, _viewModel.SelectedQuality);
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedConcurrency))
                SelectCombo(_concurrencyCombo, _viewModel.SelectedConcurrency);
            if (e.PropertyName == nameof(MainWindowViewModel.IsDarkTheme) && Application.IsRunning)
                ApplyWindowTheme(Application.Current.Theme);
        };

        // Apply after the framework finishes reconciling themed styles. The title-bar
        // controls intentionally use an explicit foreground so light mode stays readable.
        ThemeChanged += (_, newTheme) => ApplyWindowTheme(newTheme);

        StyleSheet = CreateTitleBarStyles();

        Padding = new Thickness(0);
        Title = "糖豆视频下载工具";
        this.Resizable(1440, 900, 980, 620, 2400, 1400)
            .StartCenterScreen()
            .ExtendClientAreaTitleBarHeight(36)
            .FontFamily("Segoe UI Variable, Segoe UI, PingFang SC, Microsoft YaHei")
            .FontSize(12.5)
            .Icon(IconSource.FromResource(typeof(MainWindow).Assembly, "TangdouDownloader.Desktop.Assets.logo.ico"));
    }

    protected override void OnBuild()
    {
        var taskItems = CreateTaskItems();
        var historyItems = new ItemsView<HistoryItem>(_viewModel.HistoryItems, item => item.Title);

        var root = new Grid()
            .Rows("36,Auto,*,Auto")
            .Children(
                CreateTitleBar().Row(0),
                CreateNavigationBar().Row(1),
                CreatePageHost(taskItems, historyItems).Row(2),
                CreateStatusBar().Row(3));

        this.WithTheme((theme, window) => window.Background(theme.Palette.WindowBackground));
        Content = root;
        if (Application.IsRunning)
            ApplyWindowTheme(Application.Current.Theme);
    }

    protected override void OnThemeChanged(Theme oldTheme, Theme newTheme)
    {
        base.OnThemeChanged(oldTheme, newTheme);
        ApplyWindowTheme(newTheme);
    }

    private void ApplyWindowTheme(Theme theme)
    {
        var foreground = TitleBarText;

        if (_titleBarTitle is not null) _titleBarTitle.Foreground = foreground;
        if (_githubGlyph is not null) _githubGlyph.Foreground = foreground;
        if (_themeToggleGlyph is not null)
        {
            _themeToggleGlyph.Foreground = foreground;
            _themeToggleGlyph.Text = theme.IsDark ? "☀" : "☾";
        }

        if (_githubButton is not null) _githubButton.Foreground = foreground;
        if (_themeToggleButton is not null) _themeToggleButton.Foreground = foreground;
        if (_minimizeButton is not null) _minimizeButton.Foreground = foreground;
        if (_maximizeButton is not null) _maximizeButton.Foreground = foreground;
        if (_closeButton is not null) _closeButton.Foreground = foreground;
        if (_minimizeGlyph is not null) _minimizeGlyph.Foreground = foreground;
        if (_maximizeGlyph is not null) _maximizeGlyph.Foreground = foreground;
        if (_closeGlyph is not null) _closeGlyph.Foreground = foreground;

        if (_statusBar is not null)
        {
            _statusBar.Background = theme.IsDark
                ? theme.Palette.ControlBackground
                : Color.FromRgb(242, 242, 245);
            _statusBar.BorderBrush = theme.Palette.ControlBorder;
        }
    }

    private void ShowToast(string message) => WindowExtensions.ShowToast(this, message);

    private static StyleSheet CreateTitleBarStyles()
    {
        return new StyleSheet()
            .With("titlebar-chrome", () => new Style(typeof(Button))
            {
                Setters =
                [
                    Setter.Create(Control.BackgroundProperty, TitleBarBackground),
                    Setter.Create(Control.BorderThicknessProperty, 0.0),
                    Setter.Create(Control.CornerRadiusProperty, 0.0),
                    Setter.Create(Control.PaddingProperty, new Thickness(0)),
                ],
                Triggers =
                [
                    new StateTrigger
                    {
                        Match = VisualStateFlags.Hot,
                        Setters = [Setter.Create(Control.BackgroundProperty, Color.FromRgb(58, 64, 72))],
                    },
                    new StateTrigger
                    {
                        Match = VisualStateFlags.Pressed,
                        Setters = [Setter.Create(Control.BackgroundProperty, Color.FromRgb(72, 80, 90))],
                    },
                ],
            })
            .With("titlebar-close", () => new Style(typeof(Button))
            {
                Setters =
                [
                    Setter.Create(Control.BackgroundProperty, TitleBarBackground),
                    Setter.Create(Control.BorderThicknessProperty, 0.0),
                    Setter.Create(Control.CornerRadiusProperty, 0.0),
                    Setter.Create(Control.PaddingProperty, new Thickness(0)),
                ],
                Triggers =
                [
                    new StateTrigger
                    {
                        Match = VisualStateFlags.Hot,
                        Setters =
                        [
                            Setter.Create(Control.BackgroundProperty, Color.FromRgb(232, 17, 35)),
                            Setter.Create(Control.ForegroundProperty, Color.FromRgb(255, 255, 255)),
                        ],
                    },
                    new StateTrigger
                    {
                        Match = VisualStateFlags.Pressed,
                        Setters =
                        [
                            Setter.Create(Control.BackgroundProperty, Color.FromRgb(200, 12, 28)),
                            Setter.Create(Control.ForegroundProperty, Color.FromRgb(255, 255, 255)),
                        ],
                    },
                ],
            });
    }

    private FrameworkElement CreateTitleBar()
    {
        _githubGlyph = new TextBlock().Text("🔗").FontSize(18)
            .WithTheme((theme, text) => text.Foreground(TitleBarForeground(theme)))
            .VerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center);
        _githubButton = new Button()
            .Content(_githubGlyph)
            .WithTheme((theme, button) => button.Foreground(TitleBarForeground(theme)))
            .OnClick(_viewModel.OpenGithub)
            .StyleName("titlebar-chrome")
            .MinWidth(46)
            .MinHeight(36);

        _themeToggleGlyph = new TextBlock().Text(_viewModel.IsDarkTheme ? "☀" : "☾").FontSize(18)
            .WithTheme((theme, text) =>
            {
                text.Foreground(TitleBarForeground(theme));
                text.Text = theme.IsDark ? "☀" : "☾";
            })
            .VerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center);
        _themeToggleButton = new Button()
            .Content(_themeToggleGlyph)
            .WithTheme((theme, button) => button.Foreground(TitleBarForeground(theme)))
            .OnClick(_viewModel.ToggleTheme)
            .StyleName("titlebar-chrome")
            .Size(46, 36);

        _minimizeGlyph = new GlyphElement().Kind(GlyphKind.WindowMinimize).GlyphSize(4)
            .WithTheme((theme, glyph) => glyph.Foreground(TitleBarForeground(theme)));
        _minimizeButton = new Button()
            .Content(_minimizeGlyph)
            .WithTheme((theme, button) => button.Foreground(TitleBarForeground(theme)))
            .OnClick(Minimize)
            .StyleName("titlebar-chrome")
            .Size(46, 36);

        _maximizeGlyph = new GlyphElement().Kind(GlyphKind.WindowMaximize).GlyphSize(4)
            .WithTheme((theme, glyph) => glyph.Foreground(TitleBarForeground(theme)));
        _maximizeButton = new Button()
            .Content(_maximizeGlyph)
            .WithTheme((theme, button) => button.Foreground(TitleBarForeground(theme)))
            .OnClick(() =>
            {
                if (WindowState == WindowState.Maximized) Restore();
                else Maximize();
            })
            .StyleName("titlebar-chrome")
            .Size(46, 36);

        _closeGlyph = new GlyphElement().Kind(GlyphKind.Cross).GlyphSize(4)
            .WithTheme((theme, glyph) => glyph.Foreground(TitleBarForeground(theme)));
        _closeButton = new Button()
            .Content(_closeGlyph)
            .WithTheme((theme, button) => button.Foreground(TitleBarForeground(theme)))
            .OnClick(Close)
            .StyleName("titlebar-close")
            .Size(46, 36);

        ClientSizeChanged += _ => _maximizeGlyph.Kind =
            WindowState == WindowState.Maximized ? GlyphKind.WindowRestore : GlyphKind.WindowMaximize;

        // Keep the custom title visually distinct from the native window caption in light mode.
        _titleBarTitle = new TextBlock().Text("糖豆视频下载工具")
            .FontWeight(FontWeight.Bold)
            .FontSize(13)
            .Foreground(TitleBarText)
            .WithTheme((theme, text) => text.Foreground(TitleBarText))
            .Column(1).CenterVertical();
        var titleBar = new Grid()
            .Columns("Auto,*,Auto,Auto,Auto,Auto,Auto")
            .Children(
                new Image
                {
                    Source = ImageSource.FromResource(typeof(MainWindow).Assembly, "TangdouDownloader.Desktop.Assets.logo.ico"),
                    Width = 20,
                    Height = 20,
                }.Column(0).Margin(12, 0, 8, 0).CenterVertical(),
                _titleBarTitle,
                _githubButton.Column(2).CenterVertical(),
                _themeToggleButton.Column(3).CenterVertical(),
                _minimizeButton.Column(4).CenterVertical(),
                _maximizeButton.Column(5).CenterVertical(),
                _closeButton.Column(6).CenterVertical());

        var titleBarHost = new Border()
            .Child(titleBar)
            .Background(TitleBarBackground)
            .Foreground(TitleBarText);

        titleBarHost.MouseDown += args =>
        {
            if (args.Button != MouseButton.Left) return;
            if (args.ClickCount >= 2)
            {
                args.Handled = true;
                if (WindowState == WindowState.Maximized) Restore();
                else Maximize();
                return;
            }

            if (args.ClickCount == 1)
            {
                args.Handled = true;
                DragMove();
            }
        };
        return titleBarHost;
    }
    private FrameworkElement CreateNavigationBar()
    {
        var downloadsButton = new Button()
            .OnClick(_viewModel.ShowDownloads)
            .Content(new StackPanel().Horizontal().Spacing(7).Children(
                new GlyphElement().Kind(GlyphKind.ChevronDown).GlyphSize(4),
                new TextBlock().Text("下载任务")))
            .Bind(Control.BackgroundProperty, _viewModel, x => x.DownloadsBackground)
            .Bind(TextElement.ForegroundProperty, _viewModel, x => x.DownloadsForeground)
            .Padding(new Thickness(14, 7, 14, 7))
            .MinWidth(112);

        var historyButton = new Button()
            .OnClick(_viewModel.ShowHistory)
            .Content(new StackPanel().Horizontal().Spacing(7).Children(
                new GlyphElement().Kind(GlyphKind.Hamburger).GlyphSize(4),
                new TextBlock().Text("历史记录")))
            .Bind(Control.BackgroundProperty, _viewModel, x => x.HistoryBackground)
            .Bind(TextElement.ForegroundProperty, _viewModel, x => x.HistoryForeground)
            .Padding(new Thickness(14, 7, 14, 7))
            .MinWidth(112);

        return new Border()
            .Padding(new Thickness(20, 8, 20, 8))
            .WithTheme((theme, border) => border.Background(theme.Palette.ControlBackground))
            .Child(new StackPanel().Horizontal().Spacing(8).Children(downloadsButton, historyButton));
    }
    private FrameworkElement CreatePageHost(ItemsView<DownloadItem> taskItems, ItemsView<HistoryItem> historyItems)
    {
        return new Grid().Children(
            CreateDownloadPage(taskItems)
                .Bind(UIElement.IsVisibleProperty, _viewModel, x => x.IsDownloadPage),
            CreateHistoryPage(historyItems)
                .Bind(UIElement.IsVisibleProperty, _viewModel, x => x.IsHistoryPage));
    }

    private FrameworkElement CreateDownloadPage(ItemsView<DownloadItem> taskItems)
    {
        return new Grid()
            .Rows("Auto,*,Auto")
            .Margin(20)
            .Spacing(16)
            .Children(
                CreateInputCard().Row(0),
                CreateTaskCard(taskItems).Row(1),
                CreateMetricCards().Row(2));
    }

    private FrameworkElement CreateInputCard()
    {
        var input = new MultiLineTextBox()
            .Wrap()
            .Placeholder("粘贴糖豆链接或 VID")
            .Height(94)
            .Bind(MultiLineTextBox.TextProperty, _viewModel, x => x.InputText);

        var pasteButton = new Button()
            .OnClick(_viewModel.PasteClipboard)
            .Content(new StackPanel().Horizontal().Spacing(6).Children(
                new GlyphElement().Kind(GlyphKind.Plus).GlyphSize(3.5).WithTheme((theme, glyph) => glyph.Foreground(theme.Palette.Accent)),
                new TextBlock().Text("从剪贴板粘贴")));

        var browseButton = new Button()
            .OnClick(_viewModel.BrowseDirectory).Margin(5,0,0,0)
            .Content(new StackPanel().Horizontal().Spacing(5).Children(
                new GlyphElement().Kind(GlyphKind.ChevronRight).GlyphSize(3.5).WithTheme((theme, glyph) => glyph.Foreground(theme.Palette.Accent)),
                new TextBlock().Text("浏览")));

        _qualityCombo = CreateCombo("QualityCombo", _viewModel.Qualities, _viewModel.SelectedQuality, value =>
        {
            if (value is string quality) _viewModel.SelectedQuality = quality;
        });
        _concurrencyCombo = CreateCombo("ConcurrencyCombo", _viewModel.ConcurrencyOptions, _viewModel.SelectedConcurrency, value =>
        {
            if (value is int concurrency) _viewModel.SelectedConcurrency = concurrency;
        });
        var controls = new StackPanel().Horizontal().Spacing(16).Children(
            Field("清晰度", _qualityCombo.Width(92)),
            Field("同时下载数", _concurrencyCombo.Width(72)),
            Field("保存目录", new Grid().Columns("*,Auto").Children(new TextBox()
                .Width(308)
                .Bind(TextBox.TextProperty, _viewModel, x => x.DownloadDirectory).Column(0), browseButton.Column(1))));

        var actionButton = new Button().OnClick(_viewModel.ResolveAndQueue)
            .Content(new StackPanel().Horizontal().Spacing(6).Children(
                new GlyphElement().Kind(GlyphKind.Plus).GlyphSize(4), new TextBlock().Text("加入队列")))
            .Background(ThemePalette.Accent).Foreground(ThemePalette.White);
        var deleteButton = new Button().OnClick(_viewModel.DeleteSelected)
            .Content(new StackPanel().Horizontal().Spacing(6).Children(
                new GlyphElement().Kind(GlyphKind.Cross).GlyphSize(3.5), new TextBlock().Text("删除选中")))
            .Background(ThemePalette.Danger).Foreground(ThemePalette.White);
        var pauseButton = new Button().OnClick(_viewModel.PauseAll)
            .Content(new StackPanel().Horizontal().Spacing(6).Children(
                new GlyphElement().Kind(GlyphKind.Minus).GlyphSize(3.5), new TextBlock().Text("暂停全部")))
            .Background(ThemePalette.Warning).Foreground(ThemePalette.White);
        var startButton = new Button().OnClick(_viewModel.StartAll)
            .Content(new StackPanel().Horizontal().Spacing(6).Children(
                new GlyphElement().Kind(GlyphKind.ChevronRight).GlyphSize(3.5), new TextBlock().Text("全部开始下载")))
            .Background(ThemePalette.Accent).Foreground(ThemePalette.White);

        return CreateCard()
            .Padding(16)
            .Child(new Grid()
                .Rows("Auto,Auto,Auto")
                .Spacing(12)
                .Children(
                    new Grid().Columns("*,Auto").Children(
                        new StackPanel().Horizontal().Spacing(8).Children(
                            new GlyphElement().Kind(GlyphKind.Plus).GlyphSize(5).CenterVertical().WithTheme((theme, glyph) => glyph.Foreground(theme.Palette.Accent)),
                            new TextBlock().Text("添加糖豆视频下载链接").Bold().CenterVertical(),
                            new TextBlock().Text("支持批量输入，每行一条").FontSize(11).WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))).CenterVertical()),
                        pasteButton.Column(1)),
                    input.Row(1),
                    new Grid().Columns("*,Auto").Spacing(12).Row(2).Children(
                        controls.Column(0).CenterVertical(),
                        new StackPanel().Horizontal().Spacing(9).Column(1).CenterVertical().Children(
                            actionButton, deleteButton, pauseButton, startButton))));
    }

    private FrameworkElement CreateTaskCard(ItemsView<DownloadItem> taskItems)
    {
        var filters = new StackPanel().Horizontal().Spacing(8).Children(
            FilterButton(_viewModel.FilterAll, x => x.AllFilterBackground, x => x.AllFilterForeground)
                .Content(new TextBlock().Bind(TextBlock.TextProperty, _viewModel, x => x.AllTaskLabel)),
            FilterButton(_viewModel.FilterDownloading, x => x.DownloadingFilterBackground, x => x.DownloadingFilterForeground).Content("下载中"),
            FilterButton(_viewModel.FilterCompleted, x => x.CompletedFilterBackground, x => x.CompletedFilterForeground).Content("已完成"),
            FilterButton(_viewModel.FilterQueued, x => x.QueuedFilterBackground, x => x.QueuedFilterForeground).Content("排队中"));

        _selectAllCheck = new CheckBox().Center().IsThreeState();
        _selectAllCheck.CheckedChanged += value =>
        {
            if (!_syncingSelectAllCheck)
                _viewModel.IsAllTasksSelectedNullable = value;
        };
        SyncSelectAllCheck();

        _taskGrid = new GridView()
            .RowHeight(78)
            .HeaderHeight(42)
            .CornerRadius(0)
            .ItemsSource(taskItems)
            .Columns(
                new GridViewColumn<DownloadItem>()
                    .PixelWidth(64)
                    .Header("")
                    .HeaderTemplate(_ => _selectAllCheck)
                    .Template(
                        build: _ => new SelectionCheckBox(),
                        bind: (box, item) => box.HorizontalAlignment(HorizontalAlignment.Center).SetItem(item)),
                    new GridViewColumn<DownloadItem>().Header("序号").PixelWidth(72).HeaderTextAlignment(TextAlignment.Center)
                        .Template(build: _ => new TextBlock().HorizontalAlignment(HorizontalAlignment.Center), bind: (block, item) => block.Text = item.Order.ToString()),
                new GridViewColumn<DownloadItem>().Header("视频信息（标题 / VID）").HeaderTextAlignment(TextAlignment.Center).StarWidth().Template(
                    build: _ => new Grid().Columns("Auto,*").Spacing(10),
                    bind: (grid, item) =>
                    {
                        grid.Clear();
                        var cover = new Border().Size(84, 52).CornerRadius(8).CenterVertical().ClipToBounds()
                            .WithTheme((theme, border) => border.Background(theme.Palette.ControlBackground).BorderBrush(theme.Palette.ControlBorder));
                        var image = new Image().Bind(Image.SourceProperty, item, x => x.CoverImageSource)
                            .StretchMode(Stretch.UniformToFill)
                            .Bind(UIElement.IsVisibleProperty, item, x => x.HasCoverImage);
                        var placeholder = new GlyphElement().Kind(GlyphKind.ChevronRight).GlyphSize(6).Center()
                            .WithTheme((theme, text) => text.Foreground(theme.Palette.Accent))
                            .Bind(UIElement.IsVisibleProperty, item, x => x.ShowCoverPlaceholder);
                        cover.Child(new Grid().Children(image, placeholder));
                        var text = new StackPanel().Vertical().Spacing(3).CenterVertical().Children(
                            new TextBlock().Bind(TextBlock.TextProperty, item, x => x.Title).FontSize(14).Bold(),
                            new StackPanel().Horizontal().Spacing(3).Children(
                                new TextBlock().Text("VID：").FontSize(11).WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))),
                                new TextBlock().Bind(TextBlock.TextProperty, item, x => x.VidText).FontFamily("Consolas").FontSize(11).WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165)))));
                        grid.Children(cover, text.Column(1));
                    }),
                new GridViewColumn<DownloadItem>().Header("清晰度 / 格式").HeaderTextAlignment(TextAlignment.Center).PixelWidth(130).Template(
                    build: _ => new Border().Center().CornerRadius(4).Padding(new Thickness(7, 2, 7, 2)).Height(24),
                    bind: (border, item) =>
                    {
                        border.Child = null;
                        border.Child(new TextBlock().Bind(TextBlock.TextProperty, item, x => x.QualityFormat).FontSize(11).CenterVertical()
                            .WithTheme((theme, text) => text.Foreground(theme.Palette.Accent)));
                    }),
                new GridViewColumn<DownloadItem>().Header("当前状态").HeaderTextAlignment(TextAlignment.Center).PixelWidth(110).Template(
                    build: _ => new Border().Center().CornerRadius(4).Padding(new Thickness(7, 2, 7, 2)).Height(24),
                    bind: (border, item) =>
                    {
                        border.Child = null;
                        var text = new TextBlock().Bind(TextBlock.TextProperty, item, x => x.Status).FontSize(11);
                        border.Child(text);
                        border.Bind(Control.BackgroundProperty, item, x => x.StatusBackground);
                        border.Bind(Control.BorderBrushProperty, item, x => x.StatusBorder);
                        text.Bind(TextElement.ForegroundProperty, item, x => x.StatusForeground);
                    }),
                new GridViewColumn<DownloadItem>().Header("下载进度与实时速度").HeaderTextAlignment(TextAlignment.Center).StarWidth().Template(
                    build: _ => new StackPanel().Vertical().Spacing(4).CenterVertical(),
                    bind: (panel, item) =>
                    {
                        panel.Clear();
                        var bar = new ProgressBar().Minimum(0).Maximum(100).Column(1).Margin(0, 3, 0, 0)
                            .Bind(RangeBase.ValueProperty, item, x => x.ProgressValue, mode: BindingMode.OneWay);
                        panel.Children(
                            new Grid().Columns("Auto,*,Auto").Children(
                                new TextBlock().Bind(TextBlock.TextProperty, item, x => x.ProgressLabel).FontSize(11).Bold().Column(2).Margin(3, 0, 0, 0)
                                    .WithTheme((theme, text) => text.Foreground(theme.Palette.Accent)),
                                bar,
                                new TextBlock().Bind(TextBlock.TextProperty, item, x => x.RemainingTime).FontSize(11).Column(0).Margin(new Thickness(0, 0, 3 ,0))
                                    .WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165)))),
                            new TextBlock().Bind(TextBlock.TextProperty, item, x => x.TransferLabel).FontSize(10).HorizontalAlignment(HorizontalAlignment.Right)
                                .WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))));
                    }),
                new GridViewColumn<DownloadItem>().Header("大小").HeaderTextAlignment(TextAlignment.Center).PixelWidth(95).Template(
                    build: _ => new TextBlock().HorizontalAlignment(HorizontalAlignment.Center),
                    bind: (block, item) => block.Text = item.SizeLabel ),
                new GridViewColumn<DownloadItem>().Header("操作控制").PixelWidth(240).HeaderTextAlignment(TextAlignment.Center).Template(
                    build: _ => new StackPanel().Horizontal().Spacing(6).Center(),
                    bind: (panel, item) =>
                    {
                        panel.Clear();
                        Button Item(string text, GlyphKind glyph, Action<DownloadItem> action, Color color) => new Button()
                            .Content(new StackPanel().Horizontal().Spacing(4).Children(
                                new GlyphElement().Kind(glyph).GlyphSize(3),
                                new TextBlock().Text(text)))
                            .OnClick(() => action(item)).Background(color).Foreground(ThemePalette.White);
                        var pause = Item("暂停", GlyphKind.Minus, _viewModel.PauseItem, ThemePalette.Warning).Bind(Button.IsVisibleProperty, item, x => x.CanPause);
                        var resume = Item("继续", GlyphKind.ChevronRight, _viewModel.ResumeItem, ThemePalette.Accent).Bind(Button.IsVisibleProperty, item, x => x.CanResume);
                        var retry = Item("重试", GlyphKind.ChevronDown, _viewModel.RetryItem, ThemePalette.Warning).Bind(Button.IsVisibleProperty, item, x => x.CanRetry);
                        var cancel = Item("取消", GlyphKind.Cross, _viewModel.CancelItem, ThemePalette.Danger).Bind(Button.IsVisibleProperty, item, x => x.CanCancel);
                        var play = Item("打开", GlyphKind.ChevronRight, _viewModel.PlayItem, ThemePalette.Accent).Bind(Button.IsVisibleProperty, item, x => x.CanOpenFile);
                        var folder = Item("位置", GlyphKind.ChevronRight, _viewModel.OpenItemDirectory, ThemePalette.Neutral).Bind(Button.IsVisibleProperty, item, x => x.CanOpenFile);
                        panel.Children(pause, resume, retry, cancel, play, folder);
                    }));

        return CreateCard()
            .Child(new Grid().Rows("Auto,*,Auto").Children(
                new Grid().Margin(16, 14, 16, 8).Columns("*,Auto").Children(
                    new StackPanel().Horizontal().Spacing(8).Children(
                        new StackPanel().Horizontal().Spacing(7).CenterVertical().Children(
                            new GlyphElement().Kind(GlyphKind.Hamburger).GlyphSize(4),
                            new TextBlock().Text("任务列表").Bold()),
                        filters.CenterVertical()),
                    new StackPanel().Horizontal().Spacing(7).Column(1).Children(
                        new TextBox().Width(230).Placeholder("检索标题或 VID...")
                            .Bind(TextBox.TextProperty, _viewModel, x => x.SearchText),
                        new Button().Content("刷新").IsVisible(false).OnClick(_viewModel.RefreshTasks).MinHeight(32))),
                _taskGrid.Row(1),
                new Grid().Margin(16, 8, 16, 11).Columns("*,Auto").Row(2).Children(
                    new TextBlock().Bind(TextBlock.TextProperty, _viewModel, x => x.TaskSummary)
                        .WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))),
                    new TextBlock().Bind(TextBlock.TextProperty, _viewModel, x => x.StatusMessage).Column(1)
                        .WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))))));
    }
    private FrameworkElement CreateMetricCards()
    {
        FrameworkElement Metric(string title, string valueProperty, Func<MainWindowViewModel, string> valueGetter)
        {
            var value = new ObservableValue<string>(valueGetter(_viewModel));
            _viewModel.PropertyChanged += (_, eventArgs) =>
            {
                if (eventArgs.PropertyName == valueProperty) value.Value = valueGetter(_viewModel);
            };

            return CreateCard().Padding(14).Child(new StackPanel().Vertical().Spacing(4).Children(
                new TextBlock().Text(title).FontSize(11).WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))),
                new TextBlock().BindText(value).FontSize(15).Bold()));
        }

        return new Grid().Columns("*,*,*").Spacing(12).Children(
            Metric("实时下载总速率", nameof(MainWindowViewModel.TotalSpeedLabel), x => x.TotalSpeedLabel),
            Metric("可用磁盘空间", nameof(MainWindowViewModel.DiskFreeLabel), x => x.DiskFreeLabel).Column(1),
            Metric("工作线程", nameof(MainWindowViewModel.WorkerPoolLabel), x => x.WorkerPoolLabel).Column(2));
    }

    private FrameworkElement CreateHistoryPage(ItemsView<HistoryItem> historyItems)
    {
        _historyGrid = new GridView()
            .ItemsSource(historyItems)
            .Columns(
                new GridViewColumn<HistoryItem>().Header("完成时间").PixelWidth(150).Text(x => x.CompletedAtLabel),
                new GridViewColumn<HistoryItem>().Header("标题").StarWidth().Text(x => x.Title),
                new GridViewColumn<HistoryItem>().Header("VID").PixelWidth(120).Text(x => x.Vid),
                new GridViewColumn<HistoryItem>().Header("清晰度").PixelWidth(90).Text(x => x.Quality),
                new GridViewColumn<HistoryItem>().Header("大小").PixelWidth(100).Text(x => x.SizeLabel),
                new GridViewColumn<HistoryItem>().Header("操作").PixelWidth(230).Template(
                    build: _ => new StackPanel().Horizontal().Spacing(6).Center(),
                    bind: (panel, item) =>
                    {
                        panel.Clear();
                        panel.Children(
                            new Button().OnClick(() => _viewModel.OpenHistoryDirectory(item)).Content(
                                new StackPanel().Horizontal().Spacing(5).Children(
                                    new GlyphElement().Kind(GlyphKind.ChevronRight).GlyphSize(3), new TextBlock().Text("打开位置").FontSize(12))),
                            new Button().OnClick(() => _viewModel.CopyHistoryUrl(item)).Content(
                                new StackPanel().Horizontal().Spacing(5).Children(
                                    new GlyphElement().Kind(GlyphKind.Plus).GlyphSize(3), new TextBlock().Text("复制地址").FontSize(12))));
                    }));

        return CreateCard().Margin(20).Padding(24).Child(new Grid().Rows("Auto,*").Spacing(18).Children(
            new StackPanel().Vertical().Children(
                new TextBlock().Text("历史记录").FontSize(18).Bold(),
                new TextBlock().Text("已完成的下载会保存在这里").WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165)))),
            _historyGrid.Row(1)));
    }

    private FrameworkElement CreateStatusBar()
    {
        _statusBar = new Border()
            .Padding(new Thickness(14, 7, 14, 7))
            .WithTheme((theme, border) =>
            {
                // Keep the light status strip visibly distinct from the white content area.
                border.Background(theme.IsDark ? theme.Palette.ControlBackground : Color.FromRgb(242, 242, 245));
                border.BorderBrush = theme.Palette.ControlBorder;
                border.BorderThickness = 1;
            })
            .Child(new StackPanel().Horizontal().Spacing(14).Children(
                new TextBlock().Text("就绪").WithTheme((_, text) => text.Foreground(ThemePalette.Success)),
                new TextBlock().Bind(TextBlock.TextProperty, _viewModel, x => x.AppVersion)
                    .WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(185)))));
        return _statusBar;
    }

    private Button FilterButton(Action action, Func<MainWindowViewModel, Color> background, Func<MainWindowViewModel, Color> foreground)
    {
        var backgroundValue = new ObservableValue<Color>(background(_viewModel));
        var foregroundValue = new ObservableValue<Color>(foreground(_viewModel));
        _viewModel.PropertyChanged += (_, _) =>
        {
            backgroundValue.Value = background(_viewModel);
            foregroundValue.Value = foreground(_viewModel);
        };

        return new Button().OnClick(action).Padding(new Thickness(10, 4, 10, 4)).MinWidth(76).FontSize(12)
            .Bind(Control.BackgroundProperty, backgroundValue)
            .Bind(TextElement.ForegroundProperty, foregroundValue);
    }
    private static ComboBox CreateCombo<T>(string name, IReadOnlyList<T> values, T selected, Action<object?> apply)
    {
        var combo = new ComboBox().ItemsSource(new ItemsView<T>(values, value => value?.ToString() ?? string.Empty));
        combo.SelectedItem = selected;
        combo.OnSelectionChanged(value => apply(value));
        return combo;
    }

    private static FrameworkElement Field(string label, FrameworkElement control)
    {
        return new StackPanel().Horizontal().Spacing(7).Children(
            new TextBlock().Text(label).CenterVertical().WithTheme((theme, text) => text.Foreground(theme.Palette.WindowText.WithAlpha(165))),
            control.CenterVertical());
    }

    private static void SelectCombo(ComboBox combo, object? value)
    {
        combo.SelectedItem = value;
    }

    private void SyncSelectAllCheck()
    {
        if (_selectAllCheck is null)
            return;

        _syncingSelectAllCheck = true;
        try
        {
            _selectAllCheck.IsChecked = _viewModel.IsAllTasksSelectedNullable;
        }
        finally
        {
            _syncingSelectAllCheck = false;
        }
    }

    private ItemsView<DownloadItem> CreateTaskItems() =>
        new(_viewModel.FilteredItems, item => item.Title);

    private static Border CreateCard() => new Border()
        .BorderThickness(1)
        .CornerRadius(12)
        .WithTheme((theme, border) =>
        {
            border.Background(theme.Palette.ContainerBackground);
            border.BorderBrush(theme.Palette.ControlBorder);
        });

    private static Color TitleBarForeground(Theme theme) =>
        TitleBarText;

    private sealed class SelectionCheckBox : CheckBox
    {
        private DownloadItem? _item;
        private PropertyChangedEventHandler? _itemHandler;

        public SelectionCheckBox()
        {
            IsThreeState = true;
            CheckedChanged += value =>
            {
                if (_item is not null && _item.IsSelectedNullable != value)
                    _item.IsSelectedNullable = value;
            };
        }

        public void SetItem(DownloadItem? item)
        {
            if (!ReferenceEquals(_item, item))
            {
                if (_item is not null && _itemHandler is not null)
                    _item.PropertyChanged -= _itemHandler;

                _item = item;
                _itemHandler = null;

                if (_item is not null)
                {
                    _itemHandler = (_, args) =>
                    {
                        if (args.PropertyName == nameof(DownloadItem.IsSelected) &&
                            IsChecked != _item.IsSelectedNullable)
                            IsChecked = _item.IsSelectedNullable;
                    };
                    _item.PropertyChanged += _itemHandler;
                }
            }

            IsChecked = item?.IsSelectedNullable;
        }
    }
}
