using System.Globalization;
using System.Text.RegularExpressions;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class SystemStatusRules
{
    public static IReadOnlyList<ContentFinding> EvaluateCpu(string output)
    {
        var value = ParseCpu(output);
        return value is >= 70
            ? [UsageFinding("CPU_USAGE_HIGH", IssueCode.CpuUsageHigh, "CPU", value.Value)]
            : [];
    }

    public static IReadOnlyList<ContentFinding> EvaluateMemory(string output)
    {
        var value = ParseMemory(output);
        return value is >= 70
            ? [UsageFinding("MEMORY_USAGE_HIGH", IssueCode.MemoryUsageHigh, "内存", value.Value)]
            : [];
    }

    public static IReadOnlyList<ContentFinding> EvaluateNtp(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];
        var unsynchronized = output.Contains("unsynchronized", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("not synchronized", StringComparison.OrdinalIgnoreCase);
        var stratum = StratumRegex().Match(output);
        if (!unsynchronized && (!stratum.Success || !int.TryParse(stratum.Groups[1].Value, out var level) || level < 16))
        {
            return [];
        }

        var actual = string.Join("；", output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("sync", StringComparison.OrdinalIgnoreCase) || line.Contains("stratum", StringComparison.OrdinalIgnoreCase))
            .Take(3));
        return
        [
            new ContentFinding(
                "NTP_UNSYNCHRONIZED",
                IssueSeverity.Warning,
                IssueCode.NtpUnsynchronized,
                "设备时钟未与NTP时间源同步。",
                "时钟状态为 synchronized，且 stratum 小于 16",
                SensitiveTextRedactor.Redact(actual),
                "检查NTP服务器可达性、NTP配置和设备时间后重新采集。")
        ];
    }

    private static double? ParseCpu(string output)
    {
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.TrimStart().StartsWith("Max CPU", StringComparison.OrdinalIgnoreCase)) continue;
            var match = CpuLabeledRegex().Match(line);
            if (!match.Success) match = CpuValueFirstRegex().Match(line);
            if (match.Success && TryPercent(match.Groups[1].Value, out var value)) return value;
        }

        return null;
    }

    private static double? ParseMemory(string output)
    {
        var free = FreeRatioRegex().Match(output);
        if (free.Success && TryPercent(free.Groups[1].Value, out var freeRatio)) return 100 - freeRatio;
        var used = MemoryUsedRegex().Match(output);
        if (used.Success && TryPercent(used.Groups[1].Value, out var value)) return value;
        var suffix = UsedRateSuffixRegex().Match(output);
        return suffix.Success && TryPercent(suffix.Groups[1].Value, out value) ? value : null;
    }

    private static bool TryPercent(string text, out double value) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);

    private static ContentFinding UsageFinding(string rule, IssueCode code, string label, double value) => new(
        rule,
        IssueSeverity.Warning,
        code,
        $"当前{label}使用率达到告警阈值。",
        $"当前{label}使用率低于 70%",
        $"{label}当前使用率：{value:0.##}%",
        $"观察{label}占用趋势，必要时定位高占用进程或业务负载。");

    [GeneratedRegex(@"(?i)(?:system\s+cpu\s+using\s+percentage|cpu\s+usage|cpu\s+utilization\s+(?:for|in)\s+(?:five|5)\s+seconds)\s*:?\s*(\d+(?:\.\d+)?)%")]
    private static partial Regex CpuLabeledRegex();

    [GeneratedRegex(@"(?i)(\d+(?:\.\d+)?)%\s+in\s+(?:the\s+)?last\s+5\s+seconds")]
    private static partial Regex CpuValueFirstRegex();

    [GeneratedRegex(@"(?i)(?:memory\s+using\s+percentage|memory\s+utilization|used\s+rate)\s*:?\s*(\d+(?:\.\d+)?)%")]
    private static partial Regex MemoryUsedRegex();

    [GeneratedRegex(@"(?i)(\d+(?:\.\d+)?)%\s+used\s+rate")]
    private static partial Regex UsedRateSuffixRegex();

    [GeneratedRegex(@"(?i)memory\s+freeratio\s*:?\s*(\d+(?:\.\d+)?)%")]
    private static partial Regex FreeRatioRegex();

    [GeneratedRegex(@"(?i)stratum\s*:?\s*(\d+)")]
    private static partial Regex StratumRegex();
}
