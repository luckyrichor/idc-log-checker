namespace IDCLogChecker.Core.ContentAnalysis;

public sealed record CommandOutputDocument(
    string Path,
    long ByteLength,
    int RawLineCount,
    IReadOnlyList<string> AnalysisLines,
    string Preview,
    bool TruncatedForAnalysis);

public sealed record NormalizedCommandOutput(
    int RawLineCount,
    IReadOnlyList<string> EffectiveLines,
    string SafePreview,
    bool TruncatedForAnalysis)
{
    public static NormalizedCommandOutput Empty { get; } = new(0, [], string.Empty, false);
}
