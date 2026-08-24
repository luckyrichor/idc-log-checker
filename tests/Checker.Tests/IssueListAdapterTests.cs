using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using IDCLogChecker.WinForms;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class IssueListAdapterTests
{
    [Fact]
    public void BuildsReadableRowsWithSharedSeverityColorsAndDetails()
    {
        var error = new ScanIssue(
            IssueSeverity.Error,
            IssueCode.MissingTxtFile,
            "缺少文件。",
            "设备甲",
            @"C:\日志\设备甲\状态.txt",
            "状态.txt");
        var warning = new ScanIssue(
            IssueSeverity.Warning,
            IssueCode.OneLineTxtFile,
            "文件只有一行。",
            "设备乙",
            @"C:\日志\设备乙\版本.txt",
            Actual: "show version");
        var presentation = ResultPresentation.From(Result([error, warning]));

        var rows = IssueListAdapter.BuildRows(presentation, IssueFilter.All);

        Assert.Equal(2, rows.Count);
        Assert.Equal("错误", rows[0].SeverityText);
        Assert.Equal("#C0392B", rows[0].ColorHex);
        Assert.Equal(@"C:\日志\设备甲\状态.txt", rows[0].Path);
        Assert.Contains("期望：状态.txt", rows[0].DetailText);
        Assert.Equal("提示", rows[1].SeverityText);
        Assert.Equal("#D78C12", rows[1].ColorHex);
    }

    [Fact]
    public void AppliesTheSameErrorAndWarningFiltersAsSharedPresentation()
    {
        var issues = new[]
        {
            new ScanIssue(IssueSeverity.Error, IssueCode.EmptyTxtFile, "空文件。"),
            new ScanIssue(IssueSeverity.Warning, IssueCode.OneLineTxtFile, "只有一行。"),
        };
        var presentation = ResultPresentation.From(Result(issues));

        Assert.Single(IssueListAdapter.BuildRows(presentation, IssueFilter.Errors));
        Assert.Single(IssueListAdapter.BuildRows(presentation, IssueFilter.Warnings));
        Assert.Equal(2, IssueListAdapter.BuildRows(presentation, IssueFilter.All).Count);
    }

    [Fact]
    public void IndeterminateRowUsesPurpleAndKeepsActualAndSuggestion()
    {
        var issue = new ScanIssue(
            IssueSeverity.Indeterminate,
            IssueCode.CommandOutputUnrecognized,
            "无法确认输出格式。",
            "设备甲",
            @"C:\日志\设备甲\display cpu.txt",
            "CPU使用率字段",
            "NEW FORMAT")
        {
            RuleCode = "COMMAND_OUTPUT_UNRECOGNIZED",
            SuggestedAction = "人工查看TXT。",
        };
        var summary = Result([issue]).Summary with { IndeterminateCount = 1 };
        var result = Result([issue]) with { Summary = summary };

        var row = Assert.Single(IssueListAdapter.BuildRows(
            ResultPresentation.From(result), IssueFilter.Indeterminate));

        Assert.Equal("#7D5BA6", row.ColorHex);
        Assert.Equal("NEW FORMAT", row.Actual);
        Assert.Contains("建议：人工查看TXT。", row.DetailText);
    }

    private static ScanResult Result(IReadOnlyList<ScanIssue> issues)
    {
        var now = DateTimeOffset.Now;
        return new ScanResult(
            @"C:\日志",
            now,
            now,
            new ScanSummary(62, 62, 3660, 3660, 3660,
                issues.Count(issue => issue.Severity == IssueSeverity.Error),
                issues.Count(issue => issue.Severity == IssueSeverity.Warning)),
            issues);
    }
}
