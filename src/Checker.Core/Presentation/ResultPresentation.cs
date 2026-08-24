using IDCLogChecker.Core.Reporting;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.Presentation;

public enum IssueFilter
{
    All,
    Errors,
    Indeterminate,
    Warnings,
}

public sealed record IssueRow(
    IssueSeverity Severity,
    string SeverityText,
    string CategoryText,
    string DeviceName,
    string FileName,
    string Path,
    string Message,
    string Expected,
    string Actual,
    string RuleCode,
    string SuggestedAction)
{
    public string DetailText => string.Join(
        Environment.NewLine,
        new[]
        {
            $"级别：{SeverityText}",
            $"类别：{CategoryText}",
            string.IsNullOrEmpty(DeviceName) ? null : $"设备：{DeviceName}",
            string.IsNullOrEmpty(Path) ? null : $"位置：{Path}",
            $"说明：{Message}",
            string.IsNullOrEmpty(Expected) ? null : $"期望：{Expected}",
            string.IsNullOrEmpty(Actual) ? null : $"实际：{Actual}",
            string.IsNullOrEmpty(RuleCode) ? null : $"规则编号：{RuleCode}",
            string.IsNullOrEmpty(SuggestedAction) ? null : $"建议：{SuggestedAction}",
        }.Where(line => line is not null));
}

public sealed class ResultPresentation
{
    private ResultPresentation(
        ScanResult result,
        string conclusion,
        string statusColor,
        IReadOnlyList<IssueRow> allRows)
    {
        Result = result;
        Conclusion = conclusion;
        StatusColor = statusColor;
        AllRows = allRows;
    }

    public ScanResult Result { get; }

    public string Conclusion { get; }

    public string StatusColor { get; }

    public IReadOnlyList<IssueRow> AllRows { get; }

    public static ResultPresentation From(ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var (conclusion, color) = result.Summary.ErrorCount switch
        {
            > 0 => ($"检查不通过：发现 {result.Summary.ErrorCount} 个错误", "#C0392B"),
            _ when result.Summary.IndeterminateCount > 0 =>
                ($"检查未完全确认：有 {result.Summary.IndeterminateCount} 项内容需要人工确认", "#7D5BA6"),
            _ when result.Summary.WarningCount > 0 =>
                ($"检查通过，但有 {result.Summary.WarningCount} 条提示需要关注", "#D78C12"),
            _ => ("检查通过", "#1F8A70"),
        };

        var rows = result.Issues.Select(issue => new IssueRow(
            issue.Severity,
            SeverityText(issue.Severity),
            ChineseTextReportWriter.CodeText(issue.Code),
            issue.DeviceName ?? string.Empty,
            issue.Path is null ? string.Empty : System.IO.Path.GetFileName(issue.Path),
            issue.Path ?? string.Empty,
            issue.Message,
            issue.Expected ?? string.Empty,
            issue.Actual ?? string.Empty,
            issue.RuleCode,
            issue.SuggestedAction)).ToArray();

        return new ResultPresentation(result, conclusion, color, rows);
    }

    public IReadOnlyList<IssueRow> Filter(IssueFilter filter) => filter switch
    {
        IssueFilter.Errors => AllRows.Where(row => row.Severity == IssueSeverity.Error).ToArray(),
        IssueFilter.Indeterminate => AllRows.Where(row => row.Severity == IssueSeverity.Indeterminate).ToArray(),
        IssueFilter.Warnings => AllRows.Where(row => row.Severity == IssueSeverity.Warning).ToArray(),
        _ => AllRows,
    };

    private static string SeverityText(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => "错误",
        IssueSeverity.Indeterminate => "无法确认",
        IssueSeverity.Warning => "提示",
        _ => severity.ToString(),
    };
}
