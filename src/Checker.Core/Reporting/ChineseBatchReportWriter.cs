using System.Text;
using IDCLogChecker.Core.Batch;

namespace IDCLogChecker.Core.Reporting;

public static class ChineseBatchReportWriter
{
    public static string Write(BatchScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var summary = result.Summary;
        var builder = new StringBuilder();
        builder.AppendLine("IDC 日志批量完整性检查报告");
        builder.AppendLine(new string('=', 42));
        builder.AppendLine($"开始时间：{result.StartedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"完成时间：{result.CompletedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"批次结论：{BatchConclusion(summary)}");
        builder.AppendLine();
        builder.AppendLine("批次汇总");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine($"文件夹总数：{summary.TotalCount}");
        builder.AppendLine($"已完成：{summary.CompletedCount}");
        builder.AppendLine($"完全通过：{summary.CleanCount}");
        builder.AppendLine($"无法确认：{summary.IndeterminateCount}");
        builder.AppendLine($"通过但有提示：{summary.WarningCount}");
        builder.AppendLine($"不通过：{summary.FailedCount}");
        builder.AppendLine($"错误合计：{summary.TotalErrorCount}");
        builder.AppendLine($"无法确认合计：{summary.TotalIndeterminateCount}");
        builder.AppendLine($"提示合计：{summary.TotalWarningCount}");
        builder.AppendLine($"内容确认正常合计：{summary.TotalContentNormalCount}");
        builder.AppendLine($"暂未配置内容规则合计：{summary.TotalUnsupportedContentRuleCount}");

        if (result.Input.SkippedItems.Count > 0 || result.Input.DuplicatePaths.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("输入处理说明");
            builder.AppendLine(new string('-', 42));
            foreach (var item in result.Input.SkippedItems)
            {
                builder.AppendLine($"已跳过：{DisplayInput(item.Input)}（{item.Reason}）");
            }
            foreach (var path in result.Input.DuplicatePaths)
            {
                builder.AppendLine($"重复文件夹：{path}（只检查一次）");
            }
        }

        for (var index = 0; index < result.Folders.Count; index++)
        {
            var folder = result.Folders[index];
            builder.AppendLine();
            builder.AppendLine(new string('=', 42));
            builder.AppendLine($"文件夹 {index + 1}/{result.Folders.Count}：{folder.Path}");
            builder.AppendLine(new string('=', 42));
            builder.Append(ChineseTextReportWriter.Write(folder.Result));
        }

        return builder.ToString();
    }

    public static async Task SaveAsync(
        BatchScanResult result,
        string path,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            path,
            Write(result),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static string BatchConclusion(BatchScanSummary summary) => summary switch
    {
        { FailedCount: > 0 } => $"不通过，{summary.FailedCount} 个文件夹存在错误",
        { IndeterminateCount: > 0 } => $"存在无法确认项，{summary.IndeterminateCount} 个文件夹需要人工确认",
        { WarningCount: > 0 } => $"通过但有提示，{summary.WarningCount} 个文件夹需要关注",
        _ => "全部通过",
    };

    private static string DisplayInput(string? input) =>
        string.IsNullOrWhiteSpace(input) ? "（空路径）" : input;
}
