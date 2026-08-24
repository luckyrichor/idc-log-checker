namespace IDCLogChecker.Core.Scanning;

public sealed record ScanResult(
    string RootPath,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    ScanSummary Summary,
    IReadOnlyList<ScanIssue> Issues)
{
    public bool Passed => Summary.ErrorCount == 0;
}

