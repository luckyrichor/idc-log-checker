using IDCLogChecker.Avalonia;
using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class AvaloniaBatchViewModelTests
{
    [Fact]
    public void ReplacesSelectionWithValidFoldersAndPreservesItForWhollyInvalidInput()
    {
        using var fixture = new TestDirectory();
        var first = fixture.CreateDirectory("first");
        var second = fixture.CreateDirectory("second");
        var file = fixture.WriteFile("not-folder.txt");
        var viewModel = ViewModel();

        var accepted = viewModel.ReplaceSelection([first, second, first, file]);

        Assert.True(accepted);
        Assert.Equal(2, viewModel.SelectedPaths.Count);
        Assert.Equal("已选择 2 个文件夹", viewModel.SelectionSummaryText);
        Assert.Contains("1 个项目不是有效文件夹", viewModel.InputNoticeText);
        Assert.Contains("1 个重复文件夹", viewModel.InputNoticeText);

        Assert.False(viewModel.ReplaceSelection([file, Path.Combine(fixture.Path, "missing")]));
        Assert.Equal(2, viewModel.SelectedPaths.Count);
    }

    [Fact]
    public async Task BatchScanShowsAggregateCountsAndDefaultsToFirstFailure()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("01-warning/Device-A/one.txt", "只有一行");
        fixture.CreateDirectory("02-failed");
        fixture.WriteFile("03-clean/Device-A/one.txt", "a\nb\n");
        var paths = new[]
        {
            Path.Combine(fixture.Path, "01-warning"),
            Path.Combine(fixture.Path, "02-failed"),
            Path.Combine(fixture.Path, "03-clean"),
        };
        var viewModel = ViewModel();
        viewModel.ReplaceSelection(paths);

        await viewModel.RunBatchScanAsync();

        Assert.Equal("3", viewModel.TotalFolderCountText);
        Assert.Equal("1", viewModel.CleanFolderCountText);
        Assert.Equal("1", viewModel.WarningFolderCountText);
        Assert.Equal("1", viewModel.FailedFolderCountText);
        Assert.Equal(3, viewModel.FolderResults.Count);
        Assert.Equal("02-failed", viewModel.SelectedFolder?.FolderName);
        Assert.True(viewModel.CanExportAll);
        Assert.True(viewModel.CanExportCurrent);
        Assert.Equal("检查不通过：发现 1 个错误", viewModel.Conclusion);
    }

    [Fact]
    public async Task SelectingAnotherFolderChangesDetailAndIssueFilter()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("warning/Device-A/one.txt", "只有一行");
        fixture.WriteFile("clean/Device-A/one.txt", "a\nb");
        var viewModel = ViewModel();
        viewModel.ReplaceSelection([
            Path.Combine(fixture.Path, "warning"),
            Path.Combine(fixture.Path, "clean"),
        ]);
        await viewModel.RunBatchScanAsync();

        viewModel.SelectedFolder = viewModel.FolderResults[0];
        viewModel.ApplyFilter(IssueFilter.Warnings);
        Assert.Single(viewModel.VisibleIssues);
        Assert.Equal("1", viewModel.WarningCountText);

        viewModel.SelectedFolder = viewModel.FolderResults[1];
        Assert.Empty(viewModel.VisibleIssues);
        Assert.Equal("0", viewModel.WarningCountText);
    }

    [Fact]
    public async Task BatchSummaryShowsIndeterminateFolderCount()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("indeterminate/Device-S5552/display cpu.txt", "prompt\nNEW FORMAT\nEND\n");
        var scanner = new DirectoryScanner([
            new BaselineDevice("Device-S5552", ["display cpu.txt"]),
        ]);
        var viewModel = new MainWindowViewModel(new BatchScanCoordinator(scanner));
        viewModel.ReplaceSelection([Path.Combine(fixture.Path, "indeterminate")]);

        await viewModel.RunBatchScanAsync();

        Assert.Equal("1", viewModel.IndeterminateFolderCountText);
        Assert.Equal("无法确认", viewModel.SelectedFolder?.StatusText);
        Assert.Contains("无法确认 1", viewModel.SelectedFolder?.CountText);
    }

    private static MainWindowViewModel ViewModel()
    {
        var scanner = new DirectoryScanner([
            new BaselineDevice("Device-A", ["one.txt"]),
        ]);
        return new MainWindowViewModel(new BatchScanCoordinator(scanner));
    }
}
