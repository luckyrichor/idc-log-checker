using System.Text;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.Reporting;

public static class ChineseTextReportWriter
{
    public static string Write(ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var builder = new StringBuilder();
        builder.AppendLine("IDC 日志完整性检查报告");
        builder.AppendLine(new string('=', 32));
        builder.AppendLine($"检查路径：{result.RootPath}");
        builder.AppendLine($"开始时间：{result.StartedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"完成时间：{result.CompletedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"总体结论：{Conclusion(result)}");
        builder.AppendLine();
        builder.AppendLine("检查摘要");
        builder.AppendLine(new string('-', 32));
        builder.AppendLine($"设备目录：期望 {result.Summary.ExpectedDirectoryCount}，实际 {result.Summary.ActualDirectoryCount}");
        builder.AppendLine($"TXT 文件：期望 {result.Summary.ExpectedTxtFileCount}，实际 {result.Summary.ActualTxtFileCount}，已检查 {result.Summary.CheckedTxtFileCount}");
        builder.AppendLine($"错误：{result.Summary.ErrorCount}");
        builder.AppendLine($"提示：{result.Summary.WarningCount}");

        if (result.Issues.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("未发现错误或提示。");
            return builder.ToString();
        }

        AppendSection(builder, "错误明细", result.Issues.Where(issue => issue.Severity == IssueSeverity.Error));
        AppendSection(builder, "提示明细", result.Issues.Where(issue => issue.Severity == IssueSeverity.Warning));
        return builder.ToString();
    }

    public static async Task SaveAsync(
        ScanResult result,
        string path,
        CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            path,
            Write(result),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static string Conclusion(ScanResult result)
    {
        if (result.Summary.ErrorCount > 0)
        {
            return "检查不通过";
        }

        return result.Summary.WarningCount > 0
            ? "检查通过，但有提示需要关注"
            : "检查通过";
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IEnumerable<ScanIssue> issues)
    {
        var items = issues.ToArray();
        if (items.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(title);
        builder.AppendLine(new string('-', 32));
        for (var index = 0; index < items.Length; index++)
        {
            var issue = items[index];
            builder.AppendLine($"{index + 1}. 【{SeverityText(issue.Severity)}】{CodeText(issue.Code)}");
            builder.AppendLine($"   说明：{issue.Message}");
            if (!string.IsNullOrWhiteSpace(issue.DeviceName))
            {
                builder.AppendLine($"   设备：{issue.DeviceName}");
            }

            if (!string.IsNullOrWhiteSpace(issue.Path))
            {
                builder.AppendLine($"   位置：{issue.Path}");
            }

            if (!string.IsNullOrWhiteSpace(issue.Expected))
            {
                builder.AppendLine($"   期望：{issue.Expected}");
            }

            if (!string.IsNullOrWhiteSpace(issue.Actual))
            {
                builder.AppendLine($"   实际：{issue.Actual}");
            }

            builder.AppendLine();
        }
    }

    public static string CodeText(IssueCode code) => code switch
    {
        IssueCode.RootNotFound => "所选文件夹不存在",
        IssueCode.RootUnreadable => "文件夹无法读取",
        IssueCode.MissingDirectory => "缺少设备目录",
        IssueCode.ExtraDirectory => "多出设备目录",
        IssueCode.DirectoryCaseMismatch => "设备目录名称大小写不一致",
        IssueCode.MissingTxtFile => "缺少TXT文件",
        IssueCode.ExtraTxtFile => "多出TXT文件",
        IssueCode.TxtFileCaseMismatch => "TXT文件名称大小写不一致",
        IssueCode.NonTxtFile => "存在非TXT文件",
        IssueCode.NestedDirectory => "存在额外子目录",
        IssueCode.EmptyTxtFile => "TXT文件无有效内容",
        IssueCode.OneLineTxtFile => "TXT文件只有一行",
        IssueCode.UnreadableTxtFile => "TXT文件无法读取",
        _ => code.ToString(),
    };

    private static string SeverityText(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => "错误",
        IssueSeverity.Warning => "提示",
        _ => severity.ToString(),
    };
}
