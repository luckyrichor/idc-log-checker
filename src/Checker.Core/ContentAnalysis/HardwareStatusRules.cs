using System.Globalization;
using System.Text.RegularExpressions;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class HardwareStatusRules
{
    public static IReadOnlyList<ContentFinding> Evaluate(CommandKind kind, string output) => kind switch
    {
        CommandKind.Fan => EvaluateExplicitState(output, IssueCode.FanAbnormal, "FAN_ABNORMAL", "风扇"),
        CommandKind.Power => EvaluateExplicitState(output, IssueCode.PowerAbnormal, "POWER_ABNORMAL", "电源"),
        CommandKind.Temperature => EvaluateTemperature(output),
        CommandKind.Optics => EvaluateOptics(output),
        CommandKind.Interface => EvaluateInterface(output),
        CommandKind.Storage => EvaluateStorage(output),
        _ => [],
    };

    public static IReadOnlyList<ContentFinding> EvaluateConfiguration(string output)
    {
        var findings = new List<ContentFinding>();
        foreach (var raw in Lines(output))
        {
            var line = raw.Trim();
            if (!PlainCredential().IsMatch(line)) continue;
            findings.Add(new ContentFinding(
                "PLAINTEXT_CREDENTIAL",
                IssueSeverity.Warning,
                IssueCode.SecurityRisk,
                "配置中发现明文或 simple 形式的口令配置。",
                "敏感凭据应使用设备支持的安全加密形式保存",
                SensitiveTextRedactor.Redact(line),
                "按变更流程确认影响后改用安全加密凭据，并避免在报告中传播原值。"));
        }

        return findings;
    }

    private static IReadOnlyList<ContentFinding> EvaluateExplicitState(
        string output,
        IssueCode code,
        string rule,
        string label)
    {
        var line = Lines(output).FirstOrDefault(item => AbnormalState().IsMatch(item));
        return line is null
            ? []
            : [Finding(rule, code, $"{label}状态包含明确异常标记。", $"{label}状态应为 Normal、OK 或 Present", line,
                $"检查{label}在位状态、供电、转速/输出和硬件告警。")];
    }

    private static IReadOnlyList<ContentFinding> EvaluateTemperature(string output)
    {
        foreach (var line in Lines(output))
        {
            var match = Temperature().Match(line);
            if (!match.Success || !TryNumber(match.Groups[1].Value, out var current) ||
                !TryNumber(match.Groups[2].Value, out var threshold) || current < threshold) continue;
            return [Finding("TEMPERATURE_HIGH", IssueCode.TemperatureHigh,
                "当前温度达到或超过输出中的告警阈值。", "当前温度低于告警阈值", line,
                "检查机房温度、风道、风扇和设备负载。")];
        }

        return [];
    }

    private static IReadOnlyList<ContentFinding> EvaluateOptics(string output)
    {
        var line = Lines(output).FirstOrDefault(item =>
            item.Contains("alarm", StringComparison.OrdinalIgnoreCase) &&
            !NoAlarm().IsMatch(item));
        return line is null
            ? []
            : [Finding("OPTICAL_ABNORMAL", IssueCode.OpticalAbnormal,
                "光模块诊断输出包含告警标记。", "收发光功率和模块状态在正常范围", line,
                "检查光模块、跳纤、对端光功率和阈值。")];
    }

    private static IReadOnlyList<ContentFinding> EvaluateInterface(string output)
    {
        var line = Lines(output).FirstOrDefault(item => AdminUpOperDown().IsMatch(item));
        return line is null
            ? []
            : [Finding("INTERFACE_DOWN", IssueCode.InterfaceDown,
                "接口管理状态为UP但运行状态为DOWN。", "管理状态与运行状态均为UP", line,
                "结合网络设计确认接口是否应启用；检查线缆、模块和对端接口。")];
    }

    private static IReadOnlyList<ContentFinding> EvaluateStorage(string output)
    {
        foreach (var line in Lines(output))
        {
            var match = StorageUsage().Match(line);
            if (!match.Success || !TryNumber(match.Groups[1].Value, out var usage) || usage < 90) continue;
            return [Finding("STORAGE_USAGE_HIGH", IssueCode.StorageUsageHigh,
                "存储使用率达到90%。", "存储使用率低于90%", line,
                "清理无用日志和软件包前先确认留存要求，并检查存储健康状态。")];
        }

        return [];
    }

    private static ContentFinding Finding(
        string rule, IssueCode code, string message, string expected, string actual, string action) => new(
        rule, IssueSeverity.Warning, code, message, expected,
        SensitiveTextRedactor.Redact(actual), action);

    private static IEnumerable<string> Lines(string output) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out value);

    [GeneratedRegex(@"(?i)\b(abnormal|failed|fault|absent|not\s+present)\b")]
    private static partial Regex AbnormalState();

    [GeneratedRegex(@"(?i)temperature[^\r\n]*?(?:current\s*)?(?:[:=]\s*)?(\d+(?:\.\d+)?)\s*(?:c|°c)?[^\r\n]*?threshold\s*(?:[:=]\s*)?(\d+(?:\.\d+)?)")]
    private static partial Regex Temperature();

    [GeneratedRegex(@"(?i)admin(?:status)?\s*[:=]?\s*up\b.*oper(?:status)?\s*[:=]?\s*down\b")]
    private static partial Regex AdminUpOperDown();

    [GeneratedRegex(@"(?i)(?:flash|storage)[^\r\n]*?(\d+(?:\.\d+)?)%")]
    private static partial Regex StorageUsage();

    [GeneratedRegex(@"(?i)\b(?:password\s+simple|community\s+(?:read|write)\s+\S+|secret\s+\S+)")]
    private static partial Regex PlainCredential();

    [GeneratedRegex(@"(?i)\bno\s+(?:active\s+)?alarm\b")]
    private static partial Regex NoAlarm();
}
