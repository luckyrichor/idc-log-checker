using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using IDCLogChecker.WinForms;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class BatchFormControllerTests
{
    [Fact]
    public async Task LevelRowsExposeOnlyErrorsForTheSelectedLayer()
    {
        using var fixture = new TestDirectory();
        fixture.CreateDirectory("logs/Extra-Device");
        var controller = new BatchFormController(new BatchScanCoordinator(new DirectoryScanner([])));
        controller.ReplaceSelection([Path.Combine(fixture.Path, "logs")]);
        await controller.RunAsync();

        var summary = controller.GetLevelSummary(InspectionLevel.DeviceDirectories);
        var rows = controller.BuildIssueRows(InspectionLevel.DeviceDirectories);

        Assert.Equal("1 个错误", summary.CardText);
        Assert.Single(rows);
        Assert.Equal("错误", rows[0].SeverityText);
        Assert.Empty(controller.BuildIssueRows(InspectionLevel.ExecutionResults));
    }

    [Fact]
    public void AddRemoveAndClearSelectionSupportTheResultsPageWorkflow()
    {
        using var fixture = new TestDirectory();
        var first = fixture.CreateDirectory("first");
        var second = fixture.CreateDirectory("second");
        var controller = Controller();

        Assert.True(controller.ReplaceSelection([first]));
        Assert.True(controller.AddSelection([first, second]));
        Assert.Equal([first, second], controller.SelectedPaths);
        Assert.True(controller.RemoveSelection(first));
        Assert.Equal([second], controller.SelectedPaths);

        controller.ClearSelection();
        Assert.Empty(controller.SelectedPaths);
        Assert.False(controller.CanStart);
        Assert.Equal("尚未选择文件夹", controller.SelectionSummaryText);
    }

    [Fact]
    public void ValidSelectionReplacesOldBatchButWhollyInvalidSelectionDoesNot()
    {
        using var fixture = new TestDirectory();
        var first = fixture.CreateDirectory("first");
        var second = fixture.CreateDirectory("second");
        var file = fixture.WriteFile("file.txt");
        var controller = Controller();

        Assert.True(controller.ReplaceSelection([first, second, first, file]));
        Assert.Equal(2, controller.SelectedPaths.Count);
        Assert.Contains("自动去重", controller.InputNoticeText);
        Assert.Contains("已跳过", controller.InputNoticeText);

        Assert.False(controller.ReplaceSelection([file]));
        Assert.Equal(2, controller.SelectedPaths.Count);
    }

    [Fact]
    public async Task BuildsFolderRowsAndSwitchesCurrentIssueDetails()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("warning/Device-A/one.txt", "只有一行");
        fixture.CreateDirectory("failed");
        var controller = Controller();
        controller.ReplaceSelection([
            Path.Combine(fixture.Path, "warning"),
            Path.Combine(fixture.Path, "failed"),
        ]);

        await controller.RunAsync();

        Assert.Equal(2, controller.FolderRows.Count);
        Assert.Equal(1, controller.SelectedFolderIndex);
        Assert.Equal("不通过", controller.FolderRows[1].StatusText);
        Assert.True(controller.CanExportAll);
        Assert.True(controller.CanExportCurrent);

        controller.SelectFolder(0);
        var warnings = controller.BuildIssueRows(IssueFilter.Warnings);
        Assert.Single(warnings);
        Assert.Equal("提示", warnings[0].SeverityText);
    }

    [Fact]
    public async Task ExposesIndeterminateFolderAndIssueFilter()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("logs/Device-S5552/display cpu.txt", "prompt\nNEW FORMAT\nEND\n");
        var controller = new BatchFormController(new BatchScanCoordinator(new DirectoryScanner([
            new BaselineDevice("Device-S5552", ["display cpu.txt"]),
        ])));
        controller.ReplaceSelection([Path.Combine(fixture.Path, "logs")]);

        await controller.RunAsync();

        Assert.Equal(1, controller.Summary?.IndeterminateCount);
        Assert.Equal("无法确认", controller.CurrentFolder?.StatusText);
        Assert.Single(controller.BuildIssueRows(IssueFilter.Indeterminate));
    }

    private static BatchFormController Controller() => new(new BatchScanCoordinator(
        new DirectoryScanner([new BaselineDevice("Device-A", ["one.txt"])])));
}
