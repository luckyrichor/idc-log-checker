using System.Text;
using IDCLogChecker.Core.ContentAnalysis;
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
        builder.AppendLine($"无法确认：{result.Summary.IndeterminateCount}");
        builder.AppendLine($"提示：{result.Summary.WarningCount}");
        builder.AppendLine($"内容确认正常：{result.Summary.ContentNormalCount}");
        builder.AppendLine($"暂未配置内容规则：{result.Summary.UnsupportedContentRuleCount}");

        if (result.Issues.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("未发现错误或提示。");
            return builder.ToString();
        }

        AppendSection(builder, "错误明细", result.Issues.Where(issue => issue.Severity == IssueSeverity.Error));
        AppendSection(builder, "无法确认明细", result.Issues.Where(issue => issue.Severity == IssueSeverity.Indeterminate));
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

        if (result.Summary.IndeterminateCount > 0)
        {
            return "检查未完全确认，需要人工查看无法确认项";
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
        var items = issues
            .OrderBy(issue => CodeText(issue.Code), StringComparer.Ordinal)
            .ThenBy(issue => issue.DeviceName, StringComparer.Ordinal)
            .ThenBy(issue => issue.Path, StringComparer.Ordinal)
            .ToArray();
        if (items.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(title);
        builder.AppendLine(new string('-', 32));
        var groups = items.GroupBy(issue => issue.Code).ToArray();
        for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            var group = groups[groupIndex].ToArray();
            builder.AppendLine($"{groupIndex + 1}. 【{SeverityText(group[0].Severity)}】{CodeText(group[0].Code)}（{group.Length}）");
            for (var itemIndex = 0; itemIndex < group.Length; itemIndex++)
            {
                var issue = group[itemIndex];
                builder.AppendLine($"   {groupIndex + 1}.{itemIndex + 1} 说明：{Safe(issue.Message)}");
                if (!string.IsNullOrWhiteSpace(issue.DeviceName))
                {
                    builder.AppendLine($"       设备：{Safe(issue.DeviceName)}");
                }

                if (!string.IsNullOrWhiteSpace(issue.Path))
                {
                    builder.AppendLine($"       文件：{DisplayFileName(issue.Path)}");
                    builder.AppendLine($"       位置：{Safe(issue.Path)}");
                }

                if (!string.IsNullOrWhiteSpace(issue.RuleCode))
                {
                    builder.AppendLine($"       规则编号：{Safe(issue.RuleCode)}");
                }

                if (!string.IsNullOrWhiteSpace(issue.Expected))
                {
                    builder.AppendLine($"       期望：{Safe(issue.Expected)}");
                }

                if (!string.IsNullOrWhiteSpace(issue.Actual))
                {
                    builder.AppendLine($"       实际：{Safe(issue.Actual)}");
                }

                if (!string.IsNullOrWhiteSpace(issue.SuggestedAction))
                {
                    builder.AppendLine($"       建议：{Safe(issue.SuggestedAction)}");
                }

                builder.AppendLine();
            }
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
        IssueCode.CommandUnrecognized => "设备不识别命令",
        IssueCode.CommandInvalidInput => "命令输入无效",
        IssueCode.CommandIncomplete => "命令不完整",
        IssueCode.CommandTooManyParameters => "命令参数过多",
        IssueCode.CommandPermissionDenied => "命令权限不足",
        IssueCode.CommandTimeout => "命令执行超时",
        IssueCode.CommandConnectionFailed => "设备连接失败",
        IssueCode.CommandNoEffectiveOutput => "命令没有有效输出",
        IssueCode.CommandOutputUnrecognized => "命令输出无法确认",
        IssueCode.BgpNeighborAddressFamilyNotFound => "BGP邻居或地址族不存在",
        IssueCode.BgpPeerNotFound => "BGP对等体不存在",
        IssueCode.BgpNeighborNotFound => "BGP邻居不存在",
        IssueCode.CpuUsageHigh => "CPU使用率较高",
        IssueCode.MemoryUsageHigh => "内存使用率较高",
        IssueCode.NtpUnsynchronized => "NTP未同步",
        IssueCode.AlarmCritical => "紧急活动告警",
        IssueCode.AlarmMajor => "重要活动告警",
        IssueCode.AlarmMinor => "次要活动告警",
        IssueCode.AlarmWarning => "提示级活动告警",
        IssueCode.BgpNotEstablished => "BGP邻居未建立",
        IssueCode.OspfNotFull => "OSPF邻居未达到Full",
        IssueCode.BfdDown => "BFD会话未Up",
        IssueCode.InterfaceDown => "接口状态为Down",
        IssueCode.FanAbnormal => "风扇状态异常",
        IssueCode.PowerAbnormal => "电源状态异常",
        IssueCode.TemperatureHigh => "温度较高",
        IssueCode.OpticalAbnormal => "光模块状态异常",
        IssueCode.StorageUsageHigh => "存储使用率较高",
        IssueCode.SecurityRisk => "配置存在安全风险",
        _ => code.ToString(),
    };

    private static string SeverityText(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => "错误",
        IssueSeverity.Indeterminate => "无法确认",
        IssueSeverity.Warning => "提示",
        _ => severity.ToString(),
    };

    private static string Safe(string text) => SensitiveTextRedactor.Redact(text);

    private static string DisplayFileName(string path) =>
        System.IO.Path.GetFileName(path.Replace('\\', '/'));
}
