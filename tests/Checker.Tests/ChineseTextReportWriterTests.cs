using System.Text;
using IDCLogChecker.Core.Reporting;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ChineseTextReportWriterTests
{
    [Fact]
    public void CleanResultSaysPassedAndShowsCounts()
    {
        var result = Result([], errorCount: 0, warningCount: 0);

        var report = ChineseTextReportWriter.Write(result);

        Assert.Contains("总体结论：检查通过", report, StringComparison.Ordinal);
        Assert.Contains("设备目录：期望 62，实际 62", report, StringComparison.Ordinal);
        Assert.Contains("TXT 文件：期望 3660，实际 3660，已检查 3660", report, StringComparison.Ordinal);
    }

    [Fact]
    public void WarningsOnlyResultSaysPassedWithAttentionNeeded()
    {
        var warning = new ScanIssue(
            IssueSeverity.Warning,
            IssueCode.OneLineTxtFile,
            "文件只有一行。",
            "Device-A",
            @"C:\Logs\Device-A\one.txt",
            "两行或更多",
            "show debug");
        var result = Result([warning], errorCount: 0, warningCount: 1);

        var report = ChineseTextReportWriter.Write(result);

        Assert.Contains("总体结论：检查通过，但有提示需要关注", report, StringComparison.Ordinal);
        Assert.Contains("【提示】TXT文件只有一行", report, StringComparison.Ordinal);
        Assert.Contains("实际：show debug", report, StringComparison.Ordinal);
    }

    [Fact]
    public void AnyErrorResultSaysFailedAndIncludesActionableDetails()
    {
        var error = new ScanIssue(
            IssueSeverity.Error,
            IssueCode.MissingDirectory,
            "缺少设备目录“Device-B”。",
            Path: @"C:\Logs\Device-B",
            Expected: "Device-B");
        var result = Result([error], errorCount: 1, warningCount: 0);

        var report = ChineseTextReportWriter.Write(result);

        Assert.Contains("总体结论：检查不通过", report, StringComparison.Ordinal);
        Assert.Contains("【错误】缺少设备目录", report, StringComparison.Ordinal);
        Assert.Contains("期望：Device-B", report, StringComparison.Ordinal);
        Assert.Contains(@"位置：C:\Logs\Device-B", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsyncWritesUtf8Report()
    {
        using var fixture = new TestDirectory();
        var output = System.IO.Path.Combine(fixture.Path, "检查报告.txt");

        await ChineseTextReportWriter.SaveAsync(Result([], 0, 0), output);

        var bytes = await File.ReadAllBytesAsync(output);
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Contains("IDC 日志完整性检查报告", await File.ReadAllTextAsync(output), StringComparison.Ordinal);
    }

    [Fact]
    public void GroupsSameContentTypeAndListsEveryDeviceAndTxtFile()
    {
        var issues = new[]
        {
            ContentIssue("Device-A", "display bgp peer.txt"),
            ContentIssue("Device-B", "show ip bgp summary.txt"),
        };
        var result = Result(issues, errorCount: 2, warningCount: 0);

        var report = ChineseTextReportWriter.Write(result);

        Assert.Contains("设备不识别命令（2）", report);
        Assert.Contains("设备：Device-A", report);
        Assert.Contains("文件：display bgp peer.txt", report);
        Assert.Contains("设备：Device-B", report);
        Assert.Contains("文件：show ip bgp summary.txt", report);
        Assert.Contains("规则编号：CLI_UNRECOGNIZED_COMMAND", report);
        Assert.Contains("建议：核对命令模板。", report);
    }

    [Fact]
    public void ReportRedactsCredentialEvenWhenIssueWasCreatedByAnotherCaller()
    {
        var issue = ContentIssue("Device-A", "show running-config.txt") with
        {
            Actual = "snmp-agent community read SecretValue",
        };

        var report = ChineseTextReportWriter.Write(Result([issue], 1, 0));

        Assert.DoesNotContain("SecretValue", report);
        Assert.Contains("***已隐藏***", report);
    }

    [Fact]
    public void IndeterminateResultHasItsOwnSummaryAndSection()
    {
        var issue = new ScanIssue(
            IssueSeverity.Indeterminate,
            IssueCode.CommandOutputUnrecognized,
            "无法确认新格式。",
            "Device-A",
            @"C:\Logs\Device-A\display cpu.txt")
        {
            RuleCode = "COMMAND_OUTPUT_UNRECOGNIZED",
            SuggestedAction = "人工查看。",
        };
        var result = Result([issue], 0, 0) with
        {
            Summary = Result([], 0, 0).Summary with { IndeterminateCount = 1 },
        };

        var report = ChineseTextReportWriter.Write(result);

        Assert.Contains("总体结论：检查未完全确认", report);
        Assert.Contains("无法确认：1", report);
        Assert.Contains("无法确认明细", report);
    }

    private static ScanIssue ContentIssue(string device, string fileName) => new(
        IssueSeverity.Error,
        IssueCode.CommandUnrecognized,
        "设备不识别该命令。",
        device,
        $@"C:\Logs\{device}\{fileName}",
        "设备应识别命令",
        "% Unrecognized command")
    {
        RuleCode = "CLI_UNRECOGNIZED_COMMAND",
        SuggestedAction = "核对命令模板。",
    };

    private static ScanResult Result(
        IReadOnlyList<ScanIssue> issues,
        int errorCount,
        int warningCount)
    {
        var start = new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.FromHours(8));
        return new ScanResult(
            @"C:\Logs",
            start,
            start.AddSeconds(3),
            new ScanSummary(62, 62, 3660, 3660, 3660, errorCount, warningCount),
            issues);
    }
}
