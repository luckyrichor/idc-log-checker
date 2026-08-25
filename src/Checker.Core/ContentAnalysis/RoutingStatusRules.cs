using System.Text.RegularExpressions;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class RoutingStatusRules
{
    public static IReadOnlyList<ContentFinding> EvaluateBgp(string output)
    {
        var findings = new List<ContentFinding>();
        foreach (var raw in Lines(output))
        {
            var line = raw.Trim();
            var aggregate = BgpAggregate().Match(line);
            if (aggregate.Success && int.TryParse(aggregate.Groups[2].Value, out var count) && count > 0)
            {
                findings.Add(ProtocolFinding(
                    "BGP_NOT_ESTABLISHED", IssueCode.BgpNotEstablished, "BGP", aggregate.Groups[1].Value, line,
                    "检查该状态对应邻居的链路、地址、AS和策略；管理性关闭的邻居需结合设计确认。"));
                continue;
            }

            var state = BgpState().Match(line);
            if (!state.Success) continue;
            findings.Add(ProtocolFinding(
                "BGP_NOT_ESTABLISHED", IssueCode.BgpNotEstablished, "BGP", state.Groups[1].Value, line,
                "检查邻居链路、地址、AS和策略；Idle(Admin) 需结合计划配置确认。"));
        }

        return findings;
    }

    public static IReadOnlyList<ContentFinding> EvaluateOspf(string output)
    {
        var findings = new List<ContentFinding>();
        foreach (var raw in Lines(output))
        {
            var line = raw.Trim();
            var labeled = OspfLabeledState().Match(line);
            if (labeled.Success && !labeled.Groups[1].Value.StartsWith("Full", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(ProtocolFinding(
                    "OSPF_NOT_FULL", IssueCode.OspfNotFull, "OSPF", labeled.Groups[1].Value, line,
                    "检查接口、区域、认证、MTU和双向连通性。"));
                continue;
            }

            var table = OspfTableState().Match(line);
            if (table.Success && !table.Groups[1].Value.StartsWith("Full", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(ProtocolFinding(
                    "OSPF_NOT_FULL", IssueCode.OspfNotFull, "OSPF", table.Groups[1].Value, line,
                    "检查接口、区域、认证、MTU和双向连通性。"));
            }
        }

        return findings;
    }

    public static IReadOnlyList<ContentFinding> EvaluateBfd(string output)
    {
        var findings = new List<ContentFinding>();
        foreach (var raw in Lines(output))
        {
            var line = raw.Trim();
            var state = BfdState().Match(line);
            if (!state.Success || state.Groups[1].Value.Equals("Up", StringComparison.OrdinalIgnoreCase)) continue;
            findings.Add(ProtocolFinding(
                "BFD_DOWN", IssueCode.BfdDown, "BFD", state.Groups[1].Value, line,
                "检查承载链路、对端配置、检测参数和绑定接口。"));
        }

        return findings;
    }

    private static IEnumerable<string> Lines(string output) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static ContentFinding ProtocolFinding(
        string rule,
        IssueCode code,
        string protocol,
        string state,
        string line,
        string action) => new(
            rule,
            IssueSeverity.Warning,
            code,
            $"发现{protocol}邻居或会话状态不是预期的正常状态。",
            protocol == "OSPF" ? "邻居状态为 Full" : protocol == "BFD" ? "会话状态为 Up" : "邻居状态为 Established",
            SensitiveTextRedactor.Redact($"状态 {state}：{line}"),
            action);

    [GeneratedRegex(@"(?i)^\s*[0-9A-Fa-f:.]+\s+.*\b(Active|Idle(?:\s*\(Admin\)|\(Admin\))?|Connect|OpenSent|OpenConfirm)\b")]
    private static partial Regex BgpState();

    [GeneratedRegex(@"(?i)^\s*(Active|Idle|Connect|OpenSent|OpenConfirm)\s*:\s*(\d+)\b")]
    private static partial Regex BgpAggregate();

    [GeneratedRegex(@"(?i)\bState\s*:\s*([A-Za-z0-9_-]+)")]
    private static partial Regex OspfLabeledState();

    [GeneratedRegex(@"^\s*(?:\d{1,3}\.){3}\d{1,3}\s+\d+\s+([A-Za-z0-9_-]+)(?:/\S+)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex OspfTableState();

    [GeneratedRegex(@"(?i)(?:session\s+)?state\s*:\s*(Up|Down|AdminDown|Init)")]
    private static partial Regex BfdState();
}
