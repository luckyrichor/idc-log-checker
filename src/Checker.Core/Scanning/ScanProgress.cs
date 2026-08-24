namespace IDCLogChecker.Core.Scanning;

public sealed record ScanProgress(int CompletedDirectories, int TotalDirectories, string CurrentItem);

