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

public enum InspectionLevel
{
    DeviceDirectories,
    CommandFiles,
    ExecutionResults,
}

public sealed record LevelResultSummary(
    InspectionLevel Level,
    string Title,
    int ErrorCount,
    string CardText,
    string DetailMessage);

public sealed record IssueCategorySummary(string CategoryText, int Count);

public sealed record IssueRow(
    IssueCode Code,
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
            issue.Code,
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

    public IReadOnlyList<IssueRow> ErrorsFor(InspectionLevel level)
    {
        var errors = AllRows.Where(row => row.Severity == IssueSeverity.Error);
        if (level == InspectionLevel.CommandFiles)
        {
            return errors
                .Where(row => LevelOf(row.Code) == level || BlocksCommandFileCheck(row.Code))
                .Select(row => BlocksCommandFileCheck(row.Code) ? AsCommandFileBlocker(row) : row)
                .ToArray();
        }

        return errors.Where(row => LevelOf(row.Code) == level).ToArray();
    }

    public IReadOnlyList<IssueCategorySummary> ErrorCategoriesFor(InspectionLevel level)
    {
        return ErrorsFor(level)
            .GroupBy(row => row.CategoryText, StringComparer.Ordinal)
            .Select(group => new IssueCategorySummary(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.CategoryText, StringComparer.Ordinal)
            .ToArray();
    }

    public LevelResultSummary LevelSummary(InspectionLevel level)
    {
        var count = ErrorsFor(level).Count;
        var title = level switch
        {
            InspectionLevel.DeviceDirectories => "一级设备目录检查",
            InspectionLevel.CommandFiles => "二级命令数目检查",
            _ => "三级执行结果检查",
        };
        var card = level == InspectionLevel.ExecutionResults || count > 0
            ? $"{count} 个错误"
            : "未发现错误";
        var message = level switch
        {
            InspectionLevel.ExecutionResults when count > 0 => "其他内容暂未发现明显异常，可人工核查。",
            InspectionLevel.ExecutionResults => "未检测到明确异常，具体内容仍需人工确认。",
            _ when count == 0 => "未发现错误",
            _ => $"发现 {count} 个错误，请查看下方结果明细。",
        };
        return new LevelResultSummary(level, title, count, card, message);
    }

    private static InspectionLevel LevelOf(IssueCode code) => code switch
    {
        IssueCode.RootNotFound or IssueCode.RootUnreadable or
        IssueCode.MissingDirectory or IssueCode.ExtraDirectory or IssueCode.DirectoryCaseMismatch
            => InspectionLevel.DeviceDirectories,
        IssueCode.MissingTxtFile or IssueCode.ExtraTxtFile or IssueCode.TxtFileCaseMismatch
            => InspectionLevel.CommandFiles,
        _ => InspectionLevel.ExecutionResults,
    };

    private static bool BlocksCommandFileCheck(IssueCode code) => code is
        IssueCode.RootNotFound or IssueCode.RootUnreadable or
        IssueCode.MissingDirectory or IssueCode.ExtraDirectory or IssueCode.DirectoryCaseMismatch;

    private static IssueRow AsCommandFileBlocker(IssueRow row) => row.Code switch
    {
        IssueCode.MissingDirectory => row with
        {
            CategoryText = "未找到对应设备目录",
            Message = $"未找到名称完全一致的设备目录“{row.Expected}”，无法进行二级命令数目检查。",
        },
        IssueCode.DirectoryCaseMismatch => row with
        {
            CategoryText = "未找到对应设备目录",
            Message = $"未找到名称完全一致的设备目录“{row.Expected}”；实际目录“{row.Actual}”大小写不一致，不允许继续进行二级检查。",
        },
        IssueCode.ExtraDirectory => row with
        {
            CategoryText = "设备目录无对应基准",
            Message = $"设备目录“{row.Actual}”不在基准中，无法进行二级命令数目检查。",
        },
        _ => row with
        {
            CategoryText = "二级检查无法完成",
            Message = "受一级设备目录错误影响，无法进行二级命令数目检查。",
        },
    };

    private static string SeverityText(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => "错误",
        IssueSeverity.Indeterminate => "无法确认",
        IssueSeverity.Warning => "提示",
        _ => severity.ToString(),
    };
}
