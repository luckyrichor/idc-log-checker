using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.Batch;

public sealed record FolderScanResult(string Path, ScanResult Result)
{
    public bool Failed => Result.Summary.ErrorCount > 0;
    public bool HasIndeterminate => !Failed && Result.Summary.IndeterminateCount > 0;
    public bool HasWarnings => !Failed && !HasIndeterminate && Result.Summary.WarningCount > 0;
    public bool Clean => !Failed && !HasIndeterminate && !HasWarnings;
}

public sealed record BatchScanSummary(
    int TotalCount,
    int CompletedCount,
    int CleanCount,
    int WarningCount,
    int FailedCount,
    int TotalErrorCount,
    int TotalWarningCount);

public sealed record BatchScanResult(
    BatchInputResult Input,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<FolderScanResult> Folders)
{
    public BatchScanSummary Summary => new(
        Input.ValidPaths.Count,
        Folders.Count,
        Folders.Count(folder => folder.Clean),
        Folders.Count(folder => folder.HasWarnings),
        Folders.Count(folder => folder.Failed),
        Folders.Sum(folder => folder.Result.Summary.ErrorCount),
        Folders.Sum(folder => folder.Result.Summary.WarningCount));
}
