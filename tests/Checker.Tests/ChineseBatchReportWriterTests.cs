using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Reporting;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ChineseBatchReportWriterTests
{
    [Fact]
    public void WritesAggregateInputNotesAndOrderedFolderSections()
    {
        var batch = new BatchScanResult(
            new BatchInputResult(
                [@"C:\批次\通过", @"C:\批次\失败"],
                [new SkippedBatchInput(@"C:\批次\说明.txt", "该项目是文件，不是文件夹，已跳过。")],
                [@"C:\批次\通过"]),
            DateTimeOffset.Parse("2026-08-24T10:00:00+08:00"),
            DateTimeOffset.Parse("2026-08-24T10:01:00+08:00"),
            [Folder(@"C:\批次\通过", 0, 0), Folder(@"C:\批次\失败", 2, 1)]);

        var report = ChineseBatchReportWriter.Write(batch);

        Assert.Contains("文件夹总数：2", report);
        Assert.Contains("完全通过：1", report);
        Assert.Contains("不通过：1", report);
        Assert.Contains("已跳过：C:\\批次\\说明.txt", report);
        Assert.Contains("重复文件夹：C:\\批次\\通过", report);
        Assert.True(report.IndexOf(@"C:\批次\通过", StringComparison.Ordinal) <
                    report.LastIndexOf(@"C:\批次\失败", StringComparison.Ordinal));
        Assert.Contains("总体结论：检查不通过", report);
    }

    [Fact]
    public async Task SavesUtf8WithoutBom()
    {
        using var fixture = new TestDirectory();
        var batch = new BatchScanResult(
            new BatchInputResult([fixture.Path], [], []),
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            [Folder(fixture.Path, 0, 1)]);
        var output = Path.Combine(fixture.Path, "批量报告.txt");

        await ChineseBatchReportWriter.SaveAsync(batch, output);

        var bytes = await File.ReadAllBytesAsync(output);
        Assert.False(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Contains("通过但有提示", await File.ReadAllTextAsync(output));
    }

    [Fact]
    public void BatchSummaryCountsIndeterminateFoldersBeforeWarnings()
    {
        var now = DateTimeOffset.Now;
        var summary = new ScanSummary(62, 62, 3660, 3660, 3660, 0, 0)
        {
            IndeterminateCount = 2,
        };
        var batch = new BatchScanResult(
            new BatchInputResult([@"C:\批次\待确认"], [], []),
            now,
            now,
            [new FolderScanResult(@"C:\批次\待确认", new ScanResult(@"C:\批次\待确认", now, now, summary, []))]);

        var report = ChineseBatchReportWriter.Write(batch);

        Assert.Contains("批次结论：存在无法确认项，1 个文件夹需要人工确认", report);
        Assert.Contains("无法确认：1", report);
        Assert.Contains("无法确认合计：2", report);
    }

    private static FolderScanResult Folder(string path, int errors, int warnings)
    {
        var now = DateTimeOffset.Now;
        var issues = new List<ScanIssue>();
        for (var i = 0; i < errors; i++)
            issues.Add(new ScanIssue(IssueSeverity.Error, IssueCode.EmptyTxtFile, $"错误 {i + 1}"));
        for (var i = 0; i < warnings; i++)
            issues.Add(new ScanIssue(IssueSeverity.Warning, IssueCode.OneLineTxtFile, $"提示 {i + 1}"));
        return new FolderScanResult(path, new ScanResult(path, now, now,
            new ScanSummary(62, 62, 3660, 3660, 3660, errors, warnings), issues));
    }
}
