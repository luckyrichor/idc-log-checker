using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ResultPresentationTests
{
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
