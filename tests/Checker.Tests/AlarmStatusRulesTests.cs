using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class AlarmStatusRulesTests
{
    [Fact]
    public void HuaweiWhitespaceTableCreatesOneFindingPerAlarm()
    {
        const string output = """
            Sequence AlarmId Severity Date Time Description
            1392 0xF102CE Major 2026-07-31 The BFD session went Down.
            1380 0x8132249 Warning 2026-07-24 The password is default.
            1300 0x123 Critical 2026-07-20 Board failed.
            1200 0x124 Minor 2026-07-10 Temperature crossed threshold.
            """;

        var findings = AlarmStatusRules.Evaluate(output);

        Assert.Equal(4, findings.Count);
        Assert.Contains(findings, item => item.Code == IssueCode.AlarmCritical && item.Actual.Contains("0x123"));
        Assert.Contains(findings, item => item.Code == IssueCode.AlarmMajor && item.Actual.Contains("0xF102CE"));
        Assert.Contains(findings, item => item.Code == IssueCode.AlarmMinor && item.Actual.Contains("0x124"));
        Assert.Contains(findings, item => item.Code == IssueCode.AlarmWarning && item.Actual.Contains("0x8132249"));
        Assert.DoesNotContain("password is default", string.Join('\n', findings.Select(item => item.Actual)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SlashSeparatedTableCreatesOneFindingPerAlarm()
    {
        const string output = """
            1/Independent/Board/BoardFault/Critical/Start/2026-08-01/Power module failed
            2/Independent/Link/LinkDown/Major/Start/2026-08-01/Uplink down
            """;

        var findings = AlarmStatusRules.Evaluate(output);

        Assert.Equal(2, findings.Count);
        Assert.Equal(IssueCode.AlarmCritical, findings[0].Code);
        Assert.Equal(IssueCode.AlarmMajor, findings[1].Code);
    }
}
