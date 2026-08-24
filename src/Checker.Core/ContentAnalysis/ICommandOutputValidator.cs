namespace IDCLogChecker.Core.ContentAnalysis;

public interface ICommandOutputValidator
{
    bool CanValidate(ContentAnalysisContext context);

    CommandValidationResult Validate(ContentAnalysisContext context);
}

public sealed record CommandValidationResult(
    bool IsRecognized,
    bool IsSuccessful,
    string ExpectedDescription,
    IReadOnlyDictionary<string, string> ParsedValues)
{
    public static CommandValidationResult Unsupported { get; } =
        new(false, false, string.Empty, new Dictionary<string, string>());
}

public sealed record ContentAnalysisResult(
    bool IsContentNormal,
    bool HasDedicatedRule,
    IReadOnlyList<ContentFinding> Findings)
{
    public long ByteLength { get; init; }

    public int RawLineCount { get; init; }

    public bool HasVisibleContent { get; init; }

    public string Preview { get; init; } = string.Empty;
}
