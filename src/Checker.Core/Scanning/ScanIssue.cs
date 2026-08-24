namespace IDCLogChecker.Core.Scanning;

public sealed record ScanIssue(
    IssueSeverity Severity,
    IssueCode Code,
    string Message,
    string? DeviceName = null,
    string? Path = null,
    string? Expected = null,
    string? Actual = null);

