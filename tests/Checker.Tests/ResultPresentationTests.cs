using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ResultPresentationTests
{
    [Fact]
    public void ThreeLevelsShowRelevantErrorsAndNeverShowWarningsOrIndeterminateRows()
    {
        var issues = new[]
        {
            new ScanIssue(IssueSeverity.Error, IssueCode.MissingDirectory, "缺少目录。"),
            new ScanIssue(IssueSeverity.Error, IssueCode.MissingTxtFile, "缺少文件。"),
            new ScanIssue(IssueSeverity.Error, IssueCode.CommandUnrecognized, "命令错误。"),
            new ScanIssue(IssueSeverity.Warning, IssueCode.OneLineTxtFile, "只有一行。"),
            new ScanIssue(IssueSeverity.Indeterminate, IssueCode.CommandOutputUnrecognized, "无法确认。"),
        };
        var result = Result(3, 1, issues) with
        {
            Summary = Result(3, 1, issues).Summary with { IndeterminateCount = 1 },
        };

        var presentation = ResultPresentation.From(result);

        Assert.Equal([IssueCode.MissingDirectory], presentation.ErrorsFor(InspectionLevel.DeviceDirectories).Select(row => row.Code));
        Assert.Equal([IssueCode.MissingDirectory, IssueCode.MissingTxtFile], presentation.ErrorsFor(InspectionLevel.CommandFiles).Select(row => row.Code));
        Assert.Equal([IssueCode.CommandUnrecognized], presentation.ErrorsFor(InspectionLevel.ExecutionResults).Select(row => row.Code));
    }

    [Fact]
    public void DirectoryErrorsAlsoBlockCommandLevelButDoNotBecomeExecutionErrors()
    {
        var issues = new[]
        {
            new ScanIssue(
                IssueSeverity.Error,
                IssueCode.MissingDirectory,
                "缺少设备目录。",
                Path: @"C:\Logs\Device-A",
                Expected: "Device-A"),
            new ScanIssue(
                IssueSeverity.Error,
                IssueCode.DirectoryCaseMismatch,
                "设备目录大小写不一致。",
                Path: @"C:\Logs\device-b",
                Expected: "Device-B",
                Actual: "device-b"),
        };
        var presentation = ResultPresentation.From(Result(2, 0, issues));

        var commandRows = presentation.ErrorsFor(InspectionLevel.CommandFiles);

        Assert.Equal(2, commandRows.Count);
        Assert.All(commandRows, row => Assert.Equal("未找到对应设备目录", row.CategoryText));
        Assert.Contains(commandRows, row => row.Message.Contains("名称完全一致", StringComparison.Ordinal));
        Assert.Empty(presentation.ErrorsFor(InspectionLevel.ExecutionResults));
    }

    [Fact]
    public void LevelSummariesUseApprovedErrorAndManualConfirmationWording()
    {
        var clean = ResultPresentation.From(Result(0, 1,
        [
            new ScanIssue(IssueSeverity.Warning, IssueCode.OneLineTxtFile, "只有一行。"),
        ]));
        var failed = ResultPresentation.From(Result(1, 0,
        [
            new ScanIssue(IssueSeverity.Error, IssueCode.EmptyTxtFile, "空文件。"),
        ]));

        Assert.Equal("未发现错误", clean.LevelSummary(InspectionLevel.DeviceDirectories).CardText);
        Assert.Equal("未发现错误", clean.LevelSummary(InspectionLevel.CommandFiles).CardText);
        Assert.Equal("0 个错误", clean.LevelSummary(InspectionLevel.ExecutionResults).CardText);
        Assert.Equal("未检测到明确异常，具体内容仍需人工确认。", clean.LevelSummary(InspectionLevel.ExecutionResults).DetailMessage);
        Assert.Equal("1 个错误", failed.LevelSummary(InspectionLevel.ExecutionResults).CardText);
        Assert.Equal("其他内容暂未发现明显异常，可人工核查。", failed.LevelSummary(InspectionLevel.ExecutionResults).DetailMessage);
    }

    [Theory]
    [InlineData(0, 0, "检查通过", "#1F8A70")]
    [InlineData(0, 2, "检查通过，但有 2 条提示需要关注", "#D78C12")]
    [InlineData(3, 1, "检查不通过：发现 3 个错误", "#C0392B")]
    public void ConclusionIsPlainChineseAndSeverityColored(
        int errors,
        int warnings,
        string expectedConclusion,
        string expectedColor)
    {
        var result = Result(errors, warnings, []);

        var presentation = ResultPresentation.From(result);

        Assert.Equal(expectedConclusion, presentation.Conclusion);
        Assert.Equal(expectedColor, presentation.StatusColor);
    }

    [Fact]
    public void RowsExposeReadableLabelsAndCanBeFiltered()
    {
        var issues = new[]
        {
            new ScanIssue(
                IssueSeverity.Error,
                IssueCode.MissingDirectory,
                "缺少目录。",
                Path: @"C:\Logs\Device-A",
                Expected: "Device-A"),
            new ScanIssue(
                IssueSeverity.Warning,
                IssueCode.OneLineTxtFile,
                "只有一行。",
                "Device-B",
                @"C:\Logs\Device-B\show debug.txt",
                Actual: "show debug"),
        };
        var presentation = ResultPresentation.From(Result(1, 1, issues));

        Assert.Equal(2, presentation.AllRows.Count);
        Assert.Equal("错误", presentation.AllRows[0].SeverityText);
        Assert.Equal("缺少设备目录", presentation.AllRows[0].CategoryText);
        Assert.Single(presentation.Filter(IssueFilter.Errors));
        Assert.Single(presentation.Filter(IssueFilter.Warnings));
        Assert.Equal(2, presentation.Filter(IssueFilter.All).Count);
    }

    [Fact]
    public void IndeterminateFindingProducesIncompleteConclusionAndDetailedPurpleRow()
    {
        var issue = new ScanIssue(
            IssueSeverity.Indeterminate,
            IssueCode.CommandOutputUnrecognized,
            "未找到预期的CPU使用率字段。",
            "Device-A",
            @"C:\Logs\Device-A\display cpu.txt",
            "CPU使用率字段",
            "未知返回格式")
        {
            RuleCode = "CPU_OUTPUT_UNRECOGNIZED",
            SuggestedAction = "人工查看TXT内容。",
        };
        var result = Result(0, 0, [issue]) with
        {
            Summary = Result(0, 0, []).Summary with { IndeterminateCount = 1 },
        };

        var presentation = ResultPresentation.From(result);

        Assert.Equal("检查未完全确认：有 1 项内容需要人工确认", presentation.Conclusion);
        Assert.Equal("#7D5BA6", presentation.StatusColor);
        var row = Assert.Single(presentation.Filter(IssueFilter.Indeterminate));
        Assert.Equal("无法确认", row.SeverityText);
        Assert.Equal("CPU_OUTPUT_UNRECOGNIZED", row.RuleCode);
        Assert.Contains("建议：人工查看TXT内容。", row.DetailText);
    }

    private static ScanResult Result(
        int errorCount,
        int warningCount,
        IReadOnlyList<ScanIssue> issues)
    {
        var now = DateTimeOffset.Now;
        return new ScanResult(
            @"C:\Logs",
            now,
            now,
            new ScanSummary(62, 62, 3660, 3660, 3660, errorCount, warningCount),
            issues);
    }
}
