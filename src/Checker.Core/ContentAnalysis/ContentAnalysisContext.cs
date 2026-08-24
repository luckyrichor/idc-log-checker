namespace IDCLogChecker.Core.ContentAnalysis;

public sealed record ContentAnalysisContext(
    string DeviceName,
    string FileName,
    string Path,
    DeviceFamily DeviceFamily,
    CommandKind CommandKind,
    CommandOutputDocument Document,
    NormalizedCommandOutput Output);
