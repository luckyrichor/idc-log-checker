using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public sealed record ContentFinding(
    string RuleCode,
    IssueSeverity Severity,
    IssueCode Code,
    string Message,
    string Expected,
    string Actual,
    string SuggestedAction);
