namespace IDCLogChecker.Core.ContentAnalysis;

internal sealed class CoreCommandValidator(
    CommandKind kind,
    string expectedDescription,
    Func<IReadOnlyList<string>, bool> success) : ICommandOutputValidator
{
    public bool CanValidate(ContentAnalysisContext context) => context.CommandKind == kind;

    public CommandValidationResult Validate(ContentAnalysisContext context) => new(
        true,
        success(context.Output.EffectiveLines),
        expectedDescription,
        new Dictionary<string, string>());
}

internal static class CoreCommandValidators
{
    public static IReadOnlyList<ICommandOutputValidator> All { get; } =
    [
        Validator(CommandKind.Cpu, "包含当前CPU使用率字段", lines => Any(lines,
            "cpu using", "cpu usage", "cpu utilization")),
        Validator(CommandKind.Memory, "包含内存总量、使用量或使用率字段", lines => Any(lines,
            "memory using percentage", "used rate", "freeratio", "system memory", "memory utilization")),
        Validator(CommandKind.Clock, "包含设备当前时间", lines => Any(lines,
            "utc", "beijing", "time zone", "clock")),
        Validator(CommandKind.NtpStatus, "包含NTP同步状态或层级字段", lines => Any(lines,
            "clock status", "clock is synchronized", "clock is unsynchronized", "stratum", "synchronization state")),
        Validator(CommandKind.Version, "包含设备软件或系统版本字段", lines => Any(lines,
            "software, version", "software version", "system version", "uptime", "bootrom")),
        Validator(CommandKind.AlarmActive, "包含活动告警表头、告警记录或无活动告警说明", lines => Any(lines,
            "alarmid", "severity", "no active alarm", "no alarm", "/critical/", "/major/", "/minor/", "/warning/")),
        Validator(CommandKind.BgpSummary, "包含BGP邻居汇总、邻居表头或状态字段", lines => Any(lines,
            "total number of peers", "total number of neighbors", "state/pfxrcd", "peers in established state", "bgp neighbor")),
        Validator(CommandKind.BgpAdvertisedRoutes, "包含BGP发布路由表头、路由数量或明确无路由说明", lines => Any(lines,
            "network", "routes advertised", "total number of routes", "route distinguisher", "no such neighbor", "the peer does not exist")),
        Validator(CommandKind.OspfPeer, "包含OSPF进程、邻居或状态字段", lines => Any(lines,
            "ospf process", "ospf instance", "router id", "neighbors", "state")),
        Validator(CommandKind.BfdNeighbor, "包含BFD邻居或会话状态字段", lines => Any(lines,
            "bfd", "local discriminator", "session state", "neighbor")),
        Validator(CommandKind.Configuration, "包含设备配置正文", lines => lines.Count >= 2 && Any(lines,
            "sysname", "hostname", "version", "interface", "#")),
        Validator(CommandKind.Debugging, "调试开关列表；无调试项时允许正文为空", _ => true),
        Validator(CommandKind.Interface, "包含接口名称和管理或运行状态字段", lines => Any(lines,
            "interface", "protocol", "admin", "oper", "physical", "link")),
        Validator(CommandKind.Fan, "包含风扇编号和状态字段", lines => Any(lines,
            "fanid", "fan-id", "fan status", "fan state")),
        Validator(CommandKind.Power, "包含电源编号和状态字段", lines => Any(lines,
            "powerid", "power id", "power status", "power state", "pwr")),
        Validator(CommandKind.Temperature, "包含温度传感器或当前温度字段", lines => Any(lines,
            "temperature", "temp", "celsius")),
        Validator(CommandKind.Optics, "包含光模块、收发功率或诊断字段", lines => Any(lines,
            "transceiver", "optical", "rx power", "tx power", "wavelength")),
        Validator(CommandKind.Storage, "包含存储容量或使用率字段", lines => Any(lines,
            "storage", "flash", "filesystem", "free space")),
        Validator(CommandKind.Log, "包含日志正文、日志计数或明确无日志说明", lines => lines.Count > 0),
    ];

    private static CoreCommandValidator Validator(
        CommandKind kind,
        string expected,
        Func<IReadOnlyList<string>, bool> success) => new(kind, expected, success);

    private static bool Any(IReadOnlyList<string> lines, params string[] markers) =>
        lines.Any(line => markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase)));
}
