using System.Text.RegularExpressions;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class AlarmStatusRules
{
    public static IReadOnlyList<ContentFinding> Evaluate(string output)
    {
        var findings = new List<ContentFinding>();
        foreach (var raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            var table = WhitespaceAlarm().Match(line);
            if (table.Success)
            {
                findings.Add(Create(table.Groups[2].Value, table.Groups[1].Value, line));
                continue;
            }

            if (!line.Contains('/')) continue;
            var fields = line.Split('/');
            var severity = fields.FirstOrDefault(IsSeverity);
            if (severity is not null)
            {
                findings.Add(Create(severity, fields[0], line));
            }
        }

        return findings;
    }

    private static bool IsSeverity(string value) => value.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Major", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Minor", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Warning", StringComparison.OrdinalIgnoreCase);

    private static ContentFinding Create(string severity, string alarmId, string line)
    {
        var (code, rule) = severity.ToUpperInvariant() switch
        {
            "CRITICAL" => (IssueCode.AlarmCritical, "ACTIVE_ALARM_CRITICAL"),
            "MAJOR" => (IssueCode.AlarmMajor, "ACTIVE_ALARM_MAJOR"),
            "MINOR" => (IssueCode.AlarmMinor, "ACTIVE_ALARM_MINOR"),
            _ => (IssueCode.AlarmWarning, "ACTIVE_ALARM_WARNING"),
        };
        return new ContentFinding(
            rule,
            IssueSeverity.Warning,
            code,
            $"发现一条 {severity} 级活动告警。",
            "活动告警列表为空，或告警已经确认处理",
            SensitiveTextRedactor.Redact($"告警ID {alarmId}：{line}"),
            "结合告警时间、对象和描述核实影响；处理后再次检查活动告警。" );
    }

    [GeneratedRegex(@"^\s*\d+\s+(0x[0-9A-Fa-f]+|\d+)\s+(Critical|Major|Minor|Warning)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WhitespaceAlarm();
}
