using IDCLogChecker.Avalonia;
using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class AvaloniaViewModelTests
{
    [Fact]
    public void InitialStateDirectsUserToChooseAFolder()
    {
        var viewModel = new MainWindowViewModel(new DirectoryScanner([]));

        Assert.Equal("请选择需要检查的日志文件夹", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasResult);
        Assert.False(viewModel.CanExport);
    }

    [Fact]
    public async Task ScanUpdatesSummaryAndWarningFilterUsingRealScanner()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt", "只有一行");
        var scanner = new DirectoryScanner(
        [
            new BaselineDevice("Device-A", ["one.txt"]),
        ]);
        var viewModel = new MainWindowViewModel(scanner)
        {
            SelectedPath = fixture.Path,
        };

        await viewModel.RunScanAsync();
        viewModel.ApplyFilter(IssueFilter.Warnings);

        Assert.True(viewModel.HasResult);
        Assert.True(viewModel.CanExport);
        Assert.Equal("检查通过，但有 1 条提示需要关注", viewModel.Conclusion);
        Assert.Equal("0", viewModel.ErrorCountText);
        Assert.Equal("1", viewModel.WarningCountText);
        Assert.Single(viewModel.VisibleIssues);
        Assert.Equal("提示", viewModel.VisibleIssues[0].SeverityText);
    }

    [Fact]
    public void InvalidRootSelectionIsRejectedWithoutReplacingCurrentSelection()
    {
        var viewModel = new MainWindowViewModel(new DirectoryScanner([]));

        Assert.False(viewModel.ReplaceSelection([
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")),
        ]));
        Assert.False(viewModel.CanStart);
        Assert.Empty(viewModel.SelectedPaths);
    }
}
