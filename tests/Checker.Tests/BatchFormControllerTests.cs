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

    private static BatchFormController Controller() => new(new BatchScanCoordinator(
        new DirectoryScanner([new BaselineDevice("Device-A", ["one.txt"])])));
}
