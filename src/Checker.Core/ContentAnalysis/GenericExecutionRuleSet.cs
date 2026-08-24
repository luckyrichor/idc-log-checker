using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public static class GenericExecutionRuleSet
{
    private sealed record Pattern(
        string Text,
        IssueCode Code,
        string RuleCode,
        string Message,
        string Expected);

    private static readonly Pattern[] ExplicitPatterns =
    [
        new("no such neighbor or address family", IssueCode.BgpNeighborAddressFamilyNotFound,
            "CLI_BGP_NEIGHBOR_AF_NOT_FOUND", "设备返回BGP邻居或地址族不存在。", "命令指定的BGP邻居和地址族应存在"),
        new("the peer does not exist", IssueCode.BgpPeerNotFound,
            "CLI_BGP_PEER_NOT_FOUND", "设备返回BGP对等体不存在。", "命令指定的BGP对等体应存在"),
        new("no such neighbor", IssueCode.BgpNeighborNotFound,
            "CLI_BGP_NEIGHBOR_NOT_FOUND", "设备返回BGP邻居不存在。", "命令指定的BGP邻居应存在"),
        new("too many parameters", IssueCode.CommandTooManyParameters,
            "CLI_TOO_MANY_PARAMETERS", "命令参数过多，设备未执行该命令。", "设备应接受并执行命令"),
        new("unrecognized command", IssueCode.CommandUnrecognized,
            "CLI_UNRECOGNIZED_COMMAND", "设备不识别该命令。", "设备应识别并执行命令"),
        new("invalid input", IssueCode.CommandInvalidInput,
            "CLI_INVALID_INPUT", "设备认为命令输入无效。", "命令语法应被设备接受"),
        new("incomplete command", IssueCode.CommandIncomplete,
            "CLI_INCOMPLETE_COMMAND", "设备认为命令不完整。", "命令参数应完整"),
        new("permission denied", IssueCode.CommandPermissionDenied,
            "CLI_PERMISSION_DENIED", "当前账号没有执行命令所需权限。", "账号应有读取该项状态的权限"),
    ];

    public static IReadOnlyList<ContentFinding> Evaluate(ContentAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var line in context.Output.EffectiveLines)
        {
            foreach (var pattern in ExplicitPatterns)
            {
                if (!line.Contains(pattern.Text, StringComparison.OrdinalIgnoreCase)) continue;
                return [Finding(pattern, line)];
            }
        }

        foreach (var line in context.Output.EffectiveLines.Take(16))
        {
            if (context.CommandKind is not (CommandKind.Log or CommandKind.Configuration))
            {
                var trimmed = line.TrimStart();
                if (line.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("operation timeout", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("connection timeout", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("% timeout", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return [new ContentFinding(
                        "CLI_TIMEOUT", IssueSeverity.Error, IssueCode.CommandTimeout,
                        "命令执行超时，未取得可确认的结果。", "命令应在连接有效期内完成",
                        SensitiveTextRedactor.Redact(line), "检查设备连通性和命令耗时后重新执行。")];
                }

                if (line.Contains("connection failed", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("connection refused", StringComparison.OrdinalIgnoreCase))
                {
                    return [new ContentFinding(
                        "CLI_CONNECTION_FAILED", IssueSeverity.Error, IssueCode.CommandConnectionFailed,
                        "连接设备失败，未取得命令结果。", "应成功连接设备并返回命令结果",
                        SensitiveTextRedactor.Redact(line), "检查网络、端口和登录信息后重新执行。")];
                }
            }
        }

        return [];
    }

    private static ContentFinding Finding(Pattern pattern, string line) => new(
        pattern.RuleCode,
        IssueSeverity.Error,
        pattern.Code,
        pattern.Message,
        pattern.Expected,
        SensitiveTextRedactor.Redact(line),
        "核对设备型号、命令模板和目标邻居参数后重新执行。");
}
