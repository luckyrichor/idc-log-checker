using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using IDCLogChecker.Avalonia;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ResultGridPointerInputTests
{
    private static bool _avaloniaStarted;

    [Fact]
    public void BlankAreaBelowRowsParticipatesInPointerHitTesting()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var window = new MainWindow { Width = 1280, Height = 820 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));
        viewModel.VisibleIssues.Add(new IssueRow(
            IssueCode.MissingDirectory,
            IssueSeverity.Error,
            "错误",
            "缺少设备目录",
            "设备-A",
            string.Empty,
            directory.Path,
            "测试问题",
            string.Empty,
            string.Empty,
            "TEST",
            "人工检查"));

        window.Show();
        var noIssuesOverlay = window.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Child is SelectableTextBlock text
                && text.Text == viewModel.LevelDetailMessage);
        noIssuesOverlay.IsVisible = false;
        Dispatcher.UIThread.RunJobs();
        var resultGrid = Assert.IsType<DataGrid>(window.FindControl<DataGrid>("IssueList"));
        var blankPoint = new Point(resultGrid.Bounds.Width / 2, resultGrid.Bounds.Height / 2);
        var windowPoint = Assert.IsType<Point>(resultGrid.TranslatePoint(blankPoint, window));
        var horizontalScrollBar = resultGrid.GetVisualDescendants()
            .OfType<ScrollBar>()
            .Single(scrollBar => scrollBar.Name == "PART_HorizontalScrollbar");
        Assert.Equal(0, horizontalScrollBar.Value);
        window.MouseWheel(windowPoint, new Vector(-1, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(horizontalScrollBar.Value > 0);

        var issueText = resultGrid.GetVisualDescendants()
            .OfType<TextBlock>()
            .Single(text => text.Text == "设备-A");
        Assert.NotNull(issueText.GetVisualAncestors().OfType<Border>().FirstOrDefault(border => border.ContextMenu is not null));

        var selectedFolderRow = window.GetVisualDescendants()
            .OfType<Grid>()
            .Single(grid => grid.ContextMenu is not null);
        Assert.NotNull(selectedFolderRow.Background);
        window.Close();
    }

    [Fact]
    public void PersistentScrollBarsRemainThickWithoutPointerHover()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var window = new MainWindow { Width = 1280, Height = 820 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));
        for (var index = 0; index < 80; index++)
        {
            viewModel.VisibleIssues.Add(new IssueRow(
                IssueCode.CommandUnrecognized,
                IssueSeverity.Error,
                "错误",
                "设备不识别命令",
                $"设备-{index}",
                "display command.txt",
                directory.Path,
                "测试问题",
                string.Empty,
                string.Empty,
                "TEST",
                "人工检查"));
        }
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var resultGrid = Assert.IsType<DataGrid>(window.FindControl<DataGrid>("IssueList"));
        var gridBars = resultGrid.GetVisualDescendants().OfType<ScrollBar>().ToArray();
        var horizontal = gridBars.Single(item => item.Orientation == Orientation.Horizontal);
        var vertical = gridBars.Single(item => item.Orientation == Orientation.Vertical);

        Assert.True(horizontal.Bounds.Height >= 16, $"结果明细横向滚动条只有 {horizontal.Bounds.Height}px");
        Assert.True(vertical.Bounds.Width >= 16, $"结果明细纵向滚动条只有 {vertical.Bounds.Width}px");
        var horizontalThumb = horizontal.GetVisualDescendants().OfType<Thumb>().Single();
        var verticalThumb = vertical.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.True(horizontalThumb.Bounds.Height >= 10, $"结果明细横向滑块只有 {horizontalThumb.Bounds.Height}px");
        Assert.True(verticalThumb.Bounds.Width >= 10, $"结果明细纵向滑块只有 {verticalThumb.Bounds.Width}px");
        Assert.True(horizontalThumb.RenderTransform?.Value == Matrix.Identity, "结果明细横向滑块仍会在未悬停时缩细");
        Assert.True(verticalThumb.RenderTransform?.Value == Matrix.Identity, "结果明细纵向滑块仍会在未悬停时缩细");

        var folders = Assert.IsType<ListBox>(window.FindControl<ListBox>("SelectedFolderList"));
        var folderHorizontal = folders.GetVisualDescendants()
            .OfType<ScrollBar>()
            .Single(item => item.Orientation == Orientation.Horizontal);
        Assert.True(folderHorizontal.Bounds.Height >= 16, $"所选文件夹横向滚动条只有 {folderHorizontal.Bounds.Height}px");
        var folderThumb = folderHorizontal.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.True(folderThumb.Bounds.Height >= 10, $"所选文件夹横向滑块只有 {folderThumb.Bounds.Height}px");
        Assert.True(folderThumb.RenderTransform?.Value == Matrix.Identity, "所选文件夹横向滑块仍会在未悬停时缩细");
        window.Close();
    }

    [Fact]
    public void ResultsLayoutKeepsCategoryScrollBarSeparateAndUsesRequestedColumnOrder()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var window = new MainWindow { Width = 1060, Height = 700 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));
        for (var index = 0; index < 10; index++)
            viewModel.CategoryFilters.Add(new IssueCategoryOption($"测试分类{index}", 100 + index, index == 0));

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var categoryButton = window.GetVisualDescendants()
            .OfType<Button>()
            .First(button => button.Content?.ToString() == "测试分类0 100");
        var categoryViewer = categoryButton.GetVisualAncestors().OfType<ScrollViewer>().First();
        var categoryBar = categoryViewer.GetVisualDescendants()
            .OfType<ScrollBar>()
            .Single(item => item.Orientation == Orientation.Horizontal);
        var buttonBottom = Assert.IsType<Point>(categoryButton.TranslatePoint(
            new Point(0, categoryButton.Bounds.Height), categoryViewer)).Y;
        var barTop = Assert.IsType<Point>(categoryBar.TranslatePoint(new Point(0, 0), categoryViewer)).Y;
        Assert.True(buttonBottom + 3 <= barTop, $"分类按钮底部 {buttonBottom}px 与滚动条顶部 {barTop}px 重叠");

        var resultGrid = Assert.IsType<DataGrid>(window.FindControl<DataGrid>("IssueList"));
        Assert.Equal("操作", resultGrid.Columns[0].Header);
        Assert.Equal(48, resultGrid.RowHeight);

        var start = window.GetVisualDescendants().OfType<Button>().Single(button => button.Content?.ToString() == "开始检查");
        var export = window.GetVisualDescendants().OfType<Button>().Single(button => button.Content?.ToString() == "导出数据");
        Assert.Equal(export.Background, start.Background);
        Assert.Equal(export.Foreground, start.Foreground);
        window.Close();
    }

    [Fact]
    public void FullPathColumnExpandsToShowTheEntirePathAtTheRightScrollLimit()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var longPath = System.IO.Path.Combine(
            directory.Path,
            "设备目录-" + new string('长', 90),
            "show bgp ipv6 unicast neighbors 2409-801e--4 advertised-routes.txt");
        var window = new MainWindow { Width = 1060, Height = 700 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));
        viewModel.VisibleIssues.Add(new IssueRow(
            IssueCode.CommandUnrecognized, IssueSeverity.Error, "错误", "设备不识别命令",
            "设备-A", "display command.txt", longPath, "测试问题",
            string.Empty, string.Empty, "TEST", "人工检查"));

        window.Show();
        var noIssuesOverlay = window.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Child is SelectableTextBlock text
                && text.Text == viewModel.LevelDetailMessage);
        noIssuesOverlay.IsVisible = false;
        Dispatcher.UIThread.RunJobs();

        var resultGrid = Assert.IsType<DataGrid>(window.FindControl<DataGrid>("IssueList"));
        var pathColumn = resultGrid.Columns.Single(column => column.Header?.ToString() == "完整路径");
        var pathText = resultGrid.GetVisualDescendants()
            .OfType<TextBlock>()
            .Single(text => text.Text == longPath);

        Assert.True(pathColumn.Width.IsAuto, "完整路径列仍是固定宽度，滚动到最右侧仍会裁掉路径末尾");
        Assert.True(pathColumn.ActualWidth >= pathText.DesiredSize.Width,
            $"完整路径列宽 {pathColumn.ActualWidth}px，不足以容纳 {pathText.DesiredSize.Width}px 的路径文字");
        window.Close();
    }

    [Fact]
    public void SelectedFolderRemoveButtonStaysBesideNameAndPaneHasColumnSplitter()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var window = new MainWindow { Width = 1280, Height = 820 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var folders = Assert.IsType<ListBox>(window.FindControl<ListBox>("SelectedFolderList"));
        var removeButton = folders.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => button.Content?.ToString() == "移除");
        Assert.Contains(
            removeButton.GetVisualAncestors().OfType<StackPanel>(),
            panel => panel.Orientation == Orientation.Horizontal);

        var splitter = Assert.Single(window.GetVisualDescendants().OfType<GridSplitter>());
        Assert.Equal(GridResizeDirection.Columns, splitter.ResizeDirection);
        Assert.True(splitter.IsVisible);
        window.Close();
    }

    [Fact]
    public void ResultsSelectionPaletteIsLightAndCellFocusBorderIsTransparent()
    {
        EnsureAvaloniaStarted();
        var window = new MainWindow();

        Assert.True(window.TryFindResource("DataGridRowHoveredBackgroundColor", out var hovered));
        Assert.True(window.TryFindResource("DataGridRowSelectedBackgroundBrush", out var selected));
        Assert.True(window.TryFindResource("DataGridCellFocusVisualPrimaryBrush", out var primaryFocus));
        Assert.True(window.TryFindResource("DataGridCellFocusVisualSecondaryBrush", out var secondaryFocus));

        Assert.Equal(Color.Parse("#F1F7FA"), Assert.IsType<SolidColorBrush>(hovered).Color);
        Assert.Equal(Color.Parse("#DCEFF7"), Assert.IsType<SolidColorBrush>(selected).Color);
        Assert.Equal(0, Assert.IsType<SolidColorBrush>(primaryFocus).Color.A);
        Assert.Equal(0, Assert.IsType<SolidColorBrush>(secondaryFocus).Color.A);
    }

    [Fact]
    public void SelectedRowUsesLightBackgroundAndFocusedCellHasNoVisualBorder()
    {
        EnsureAvaloniaStarted();
        using var directory = new TestDirectory();
        var window = new MainWindow { Width = 1280, Height = 820 };
        var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        Assert.True(viewModel.ReplaceSelection([directory.Path]));
        viewModel.VisibleIssues.Add(new IssueRow(
            IssueCode.CommandUnrecognized, IssueSeverity.Error, "错误", "设备不识别命令",
            "设备-A", "display command.txt", directory.Path, "测试问题",
            string.Empty, string.Empty, "TEST", "人工检查"));

        window.Show();
        var noIssuesOverlay = window.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Child is SelectableTextBlock text
                && text.Text == viewModel.LevelDetailMessage);
        noIssuesOverlay.IsVisible = false;
        var resultGrid = Assert.IsType<DataGrid>(window.FindControl<DataGrid>("IssueList"));
        resultGrid.SelectedIndex = 0;
        resultGrid.Focus();
        Dispatcher.UIThread.RunJobs();

        var row = resultGrid.GetVisualDescendants().OfType<DataGridRow>().First();
        var background = row.GetVisualDescendants().OfType<Rectangle>()
            .Single(item => item.Name == "BackgroundRectangle");
        Assert.Equal(Color.Parse("#DCEFF7"), Assert.IsAssignableFrom<ISolidColorBrush>(background.Fill).Color);
        Assert.Equal(1, background.Opacity);

        var cell = row.GetVisualDescendants().OfType<DataGridCell>().First();
        cell.Focus();
        Dispatcher.UIThread.RunJobs();
        var focusVisual = cell.GetVisualDescendants().OfType<Grid>()
            .Single(item => item.Name == "FocusVisual");
        Assert.False(focusVisual.IsVisible);
        window.Close();
    }

    private static void EnsureAvaloniaStarted()
    {
        if (_avaloniaStarted) return;
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
        _avaloniaStarted = true;
    }
}
