namespace IDCLogChecker.Core.Scanning;

public sealed record ScanSummary(
    int ExpectedDirectoryCount,
    int ActualDirectoryCount,
    int ExpectedTxtFileCount,
    int ActualTxtFileCount,
    int CheckedTxtFileCount,
    int ErrorCount,
    int WarningCount)
{
    public int IndeterminateCount { get; init; }

    public int ContentNormalCount { get; init; }

    public int UnsupportedContentRuleCount { get; init; }
}
