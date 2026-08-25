using IDCLogChecker.Avalonia;
using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class AvaloniaViewModelTests
{
    [Fact]
    public async Task ThirdLevelCardCarriesTheManualReviewNoteUsedByTheApprovedLayout()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt", "a\nb");
        var viewModel = new MainWindowViewModel(new DirectoryScanner([
            new BaselineDevice("Device-A", ["one.txt"]),
        ])) { SelectedPath = fixture.Path };

        await viewModel.RunBatchScanAsync();

        Assert.Equal("未检测到明确异常，具体内容仍需人工确认。", viewModel.LevelThreeNote);
    }

    [Fact]
    public async Task SelectingInspectionLevelShowsOnlyErrorsForThatLevel()
    {
        using var fixture = new TestDirectory();
        fixture.CreateDirectory("logs/Extra-Device");
        var viewModel = new MainWindowViewModel(new DirectoryScanner([]));
        viewModel.ReplaceSelection([Path.Combine(fixture.Path, "logs")]);

        await viewModel.RunBatchScanAsync();
        viewModel.SelectLevel(InspectionLevel.DeviceDirectories);

        Assert.Equal("一级设备目录检查", viewModel.SelectedLevelTitle);
        Assert.Equal("1 个错误", viewModel.LevelOneText);
        Assert.Single(viewModel.VisibleIssues);
        Assert.All(viewModel.VisibleIssues, row => Assert.Equal(IssueSeverity.Error, row.Severity));

        viewModel.SelectLevel(InspectionLevel.ExecutionResults);
        Assert.Empty(viewModel.VisibleIssues);
        Assert.Equal("未检测到明确异常，具体内容仍需人工确认。", viewModel.LevelDetailMessage);
    }

    [Fact]
    public void InitialStateDirectsUserToChooseAFolder()
    {
        var viewModel = new MainWindowViewModel(new DirectoryScanner([]));

        Assert.Equal("请选择需要检查的文件夹", viewModel.StatusText);
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

    [Fact]
    public async Task IndeterminateContentHasCounterAndDedicatedFilter()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-S5552/display cpu.txt", "Device-S5552#display cpu\r\nNEW FORMAT\r\nEND\r\n");
        var viewModel = new MainWindowViewModel(new DirectoryScanner([
            new BaselineDevice("Device-S5552", ["display cpu.txt"]),
        ]))
        {
            SelectedPath = fixture.Path,
        };

        await viewModel.RunBatchScanAsync();
        viewModel.ApplyFilter(IssueFilter.Indeterminate);

        Assert.Equal("1", viewModel.IndeterminateCountText);
        Assert.Equal("0", viewModel.ContentNormalCountText);
        Assert.Equal("0", viewModel.UnsupportedContentRuleCountText);
        Assert.Single(viewModel.VisibleIssues);
        Assert.All(viewModel.VisibleIssues, row => Assert.Equal(IssueSeverity.Indeterminate, row.Severity));
    }
}
