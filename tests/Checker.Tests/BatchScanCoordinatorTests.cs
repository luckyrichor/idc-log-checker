using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class BatchScanCoordinatorTests
{
    [Fact]
    public async Task ScansFoldersInOrderAndContinuesAfterFailedFolder()
    {
        using var fixture = new TestDirectory();
        var clean = fixture.WriteFile("01-clean/Device-A/one.txt", "第一行\n第二行\n");
        var failed = fixture.CreateDirectory("02-failed");
        var warning = fixture.WriteFile("03-warning/Device-A/one.txt", "只有一行");
        var paths = new[]
        {
            Directory.GetParent(Directory.GetParent(clean)!.FullName)!.FullName,
            failed,
            Directory.GetParent(Directory.GetParent(warning)!.FullName)!.FullName,
        };
        var progress = new CollectingProgress<BatchScanProgress>();
        var coordinator = Coordinator();

        var result = await coordinator.ScanAsync(paths, progress);

        Assert.Equal(paths, result.Folders.Select(folder => folder.Path));
        Assert.Equal(1, result.Summary.CleanCount);
        Assert.Equal(1, result.Summary.WarningCount);
        Assert.Equal(1, result.Summary.FailedCount);
        Assert.Equal(3, result.Summary.CompletedCount);
        Assert.Contains(progress.Items, item => item.FolderIndex == 2 && item.TotalFolders == 3);
        Assert.Contains(progress.Items, item => item.CompletedFolders == 3);
    }

    [Fact]
    public async Task PreservesSkippedAndDuplicateInputInformation()
    {
        using var fixture = new TestDirectory();
        var rootFile = fixture.WriteFile("valid/Device-A/one.txt", "a\nb\n");
        var root = Directory.GetParent(Directory.GetParent(rootFile)!.FullName)!.FullName;
        var input = new BatchInputResult(
            [root],
            [new SkippedBatchInput("bad.txt", "是文件")],
            [root]);

        var result = await Coordinator().ScanAsync(input);

        Assert.Same(input, result.Input);
        Assert.Single(result.Folders);
    }

    private static BatchScanCoordinator Coordinator() => new(new DirectoryScanner(
    [
        new BaselineDevice("Device-A", ["one.txt"]),
    ]));

    private sealed class CollectingProgress<T> : IProgress<T>
    {
        public List<T> Items { get; } = [];
        public void Report(T value) => Items.Add(value);
    }
}
