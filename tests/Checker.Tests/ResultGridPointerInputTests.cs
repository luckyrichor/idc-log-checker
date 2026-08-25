using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
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

    private static void EnsureAvaloniaStarted()
    {
        if (_avaloniaStarted) return;
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
        _avaloniaStarted = true;
    }
}
