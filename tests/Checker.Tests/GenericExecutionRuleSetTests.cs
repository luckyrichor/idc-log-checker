using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class GenericExecutionRuleSetTests
{
    [Theory]
    [InlineData("% Unrecognized command found at '^' position.", IssueCode.CommandUnrecognized)]
    [InlineData("% No such neighbor or address family(BGP Instance AS 1)", IssueCode.BgpNeighborAddressFamilyNotFound)]
    [InlineData("Info: The peer does not exist.", IssueCode.BgpPeerNotFound)]
    [InlineData("%Info 4031: No such neighbor.", IssueCode.BgpNeighborNotFound)]
    [InlineData("Error: Too many parameters found at '^' position.", IssueCode.CommandTooManyParameters)]
    [InlineData("% Invalid input detected", IssueCode.CommandInvalidInput)]
    [InlineData("% Incomplete command", IssueCode.CommandIncomplete)]
    [InlineData("Permission denied", IssueCode.CommandPermissionDenied)]
    public void ExplicitCliFailureIsAnError(string line, IssueCode expectedCode)
    {
        var finding = Assert.Single(GenericExecutionRuleSet.Evaluate(Context(CommandKind.BgpSummary, line)));

        Assert.Equal(expectedCode, finding.Code);
        Assert.Equal(IssueSeverity.Error, finding.Severity);
        Assert.False(string.IsNullOrWhiteSpace(finding.RuleCode));
    }

    [Fact]
    public void SpecificNeighborAddressFamilyFailureIsNotDoubleCounted()
    {
        var findings = GenericExecutionRuleSet.Evaluate(Context(
            CommandKind.BgpSummary,
            "% No such neighbor or address family(BGP Instance AS 1)"));

        Assert.Single(findings);
        Assert.Equal(IssueCode.BgpNeighborAddressFamilyNotFound, findings[0].Code);
    }

    [Theory]
    [InlineData("A job failed after timeout")]
    [InlineData("connection failed for a historical session")]
    public void LogBodyWordsDoNotBecomeCurrentExecutionErrors(string line)
    {
        Assert.Empty(GenericExecutionRuleSet.Evaluate(Context(CommandKind.Log, line)));
    }

    [Theory]
    [InlineData(CommandKind.Interface, "ARP type: ARPA, ARP Timeout: 3600 seconds")]
    [InlineData(CommandKind.BgpSummary, "Hold timer timeout interval: 180 seconds")]
    public void ProtocolTimeoutFieldsDoNotBecomeExecutionFailures(CommandKind kind, string line)
    {
        Assert.Empty(GenericExecutionRuleSet.Evaluate(Context(kind, line)));
    }

    private static ContentAnalysisContext Context(CommandKind kind, params string[] lines)
    {
        var document = new CommandOutputDocument("sample.txt", 10, lines.Length, lines, string.Join('\n', lines), false);
        var output = new NormalizedCommandOutput(lines.Length, lines, string.Join('\n', lines), false);
        return new ContentAnalysisContext("Device-A", "sample.txt", "sample.txt", DeviceFamily.Unknown, kind, document, output);
    }
}
