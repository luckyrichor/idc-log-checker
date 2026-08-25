using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class BatchResultPresentationTests
{
    [Fact]
    public void SelectsFirstFailureBeforeEarlierWarning()
    {
        var result = Batch([
            Folder("warning", 0, 1),
            Folder("failed", 2, 0),
            Folder("clean", 0, 0),
        ]);

        var presentation = BatchResultPresentation.From(result);

        Assert.Equal(1, presentation.DefaultSelectedIndex);
        Assert.Equal("不通过", presentation.Folders[1].StatusText);
        Assert.Equal("#C0392B", presentation.Folders[1].StatusColor);
    }

    [Fact]
    public void SelectsFirstWarningWhenThereAreNoFailuresOtherwiseFirstFolder()
    {
        Assert.Equal(1, BatchResultPresentation.From(Batch([
            Folder("clean", 0, 0), Folder("warning", 0, 2),
        ])).DefaultSelectedIndex);
        Assert.Equal(0, BatchResultPresentation.From(Batch([
            Folder("clean-a", 0, 0), Folder("clean-b", 0, 0),
        ])).DefaultSelectedIndex);
    }

    [Fact]
    public void SelectsFailureThenIndeterminateThenWarning()
    {
        var result = Batch([
            Folder("warning", 0, 1),
            Folder("indeterminate", 0, 0, 2),
            Folder("failed", 1, 0),
        ]);

        var presentation = BatchResultPresentation.From(result);

        Assert.Equal(2, presentation.DefaultSelectedIndex);
        Assert.Equal("无法确认", presentation.Folders[1].StatusText);
        Assert.Equal("#7D5BA6", presentation.Folders[1].StatusColor);
        Assert.Equal(2, presentation.Folders[1].IndeterminateCount);
    }

    private static BatchScanResult Batch(IReadOnlyList<FolderScanResult> folders) => new(
        new BatchInputResult(folders.Select(folder => folder.Path).ToArray(), [], []),
        DateTimeOffset.Now,
        DateTimeOffset.Now,
        folders);

    private static FolderScanResult Folder(string path, int errors, int warnings, int indeterminate = 0)
    {
        var now = DateTimeOffset.Now;
        return new FolderScanResult(path, new ScanResult(path, now, now,
            new ScanSummary(62, 62, 3660, 3660, 3660, errors, warnings)
            {
                IndeterminateCount = indeterminate,
            }, []));
    }
}
