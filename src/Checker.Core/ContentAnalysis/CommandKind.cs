namespace IDCLogChecker.Core.ContentAnalysis;

public enum CommandKind
{
    Unknown,
    Cpu,
    Memory,
    Clock,
    NtpStatus,
    Version,
    AlarmActive,
    BgpSummary,
    BgpAdvertisedRoutes,
    OspfPeer,
    BfdNeighbor,
    Configuration,
    Debugging,
    Interface,
    Fan,
    Power,
    Temperature,
    Optics,
    Storage,
    Log,
}
