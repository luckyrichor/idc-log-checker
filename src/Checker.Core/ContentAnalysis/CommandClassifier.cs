namespace IDCLogChecker.Core.ContentAnalysis;

public static class CommandClassifier
{
    public static CommandKind Classify(string fileName)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(fileName)
            .Trim()
            .ToLowerInvariant();
        if (name.Contains("advertised-route", StringComparison.Ordinal) ||
            (name.Contains("neighbor out", StringComparison.Ordinal) && name.Contains("bgp", StringComparison.Ordinal)))
            return CommandKind.BgpAdvertisedRoutes;
        if (name.Contains("bgp", StringComparison.Ordinal) &&
            (name.Contains("summary", StringComparison.Ordinal) || name.EndsWith("peer", StringComparison.Ordinal) || name.Contains(" peer ", StringComparison.Ordinal)))
            return CommandKind.BgpSummary;
        if (name.Contains("ospf", StringComparison.Ordinal) && name.Contains("peer", StringComparison.Ordinal) ||
            name.Contains("ospf neighbor", StringComparison.Ordinal)) return CommandKind.OspfPeer;
        if (name.Contains("bfd neighbor", StringComparison.Ordinal)) return CommandKind.BfdNeighbor;
        if (name.Contains("ntp", StringComparison.Ordinal)) return CommandKind.NtpStatus;
        if (name.Contains("cpu", StringComparison.Ordinal) || name.Contains("processor", StringComparison.Ordinal)) return CommandKind.Cpu;
        if (name.Contains("memory", StringComparison.Ordinal)) return CommandKind.Memory;
        if (name.Contains("clock", StringComparison.Ordinal)) return CommandKind.Clock;
        if (name.Contains("version", StringComparison.Ordinal)) return CommandKind.Version;
        if (name.Contains("alarm active", StringComparison.Ordinal) || name.Contains("logging alarm", StringComparison.Ordinal)) return CommandKind.AlarmActive;
        if (name.Contains("debug", StringComparison.Ordinal)) return CommandKind.Debugging;
        if (name.Contains("configuration", StringComparison.Ordinal) || name.Contains("running-config", StringComparison.Ordinal) || name.Contains("startup-config", StringComparison.Ordinal)) return CommandKind.Configuration;
        if (name.Contains("transceiver", StringComparison.Ordinal) || name.Contains("optical", StringComparison.Ordinal)) return CommandKind.Optics;
        if (name.Contains("temperature", StringComparison.Ordinal) || name.Contains("environment", StringComparison.Ordinal)) return CommandKind.Temperature;
        if (name.Contains("fan", StringComparison.Ordinal)) return CommandKind.Fan;
        if (name.Contains("power", StringComparison.Ordinal)) return CommandKind.Power;
        if (name.Contains("interface", StringComparison.Ordinal) || name.Contains("intf", StringComparison.Ordinal) || name.Contains("aggregateport", StringComparison.Ordinal)) return CommandKind.Interface;
        if (name.Contains("log", StringComparison.Ordinal) || name.Contains("trapbuffer", StringComparison.Ordinal)) return CommandKind.Log;
        return CommandKind.Unknown;
    }
}
