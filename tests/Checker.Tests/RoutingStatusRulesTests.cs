using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class RoutingStatusRulesTests
{
    [Theory]
    [InlineData("117.1.1.1 4 65000 0 0 0 00:01:00 Active 0", "Active")]
    [InlineData("117.1.1.1 4 65000 0 0 0 never Idle(Admin)", "Idle(Admin)")]
    [InlineData("All peers : 3\n  Connect : 1", "Connect")]
    public void NonEstablishedBgpStateCreatesWarning(string output, string expectedState)
    {
        var finding = Assert.Single(RoutingStatusRules.EvaluateBgp(output));

        Assert.Equal(IssueCode.BgpNotEstablished, finding.Code);
        Assert.Contains(expectedState, finding.Actual, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EstablishedBgpRowsDoNotCreateWarning()
    {
        Assert.Empty(RoutingStatusRules.EvaluateBgp(
            "117.1.1.1 4 65000 10 10 0 01:00:00 Established 100"));
    }

    [Theory]
    [InlineData("Router ID: 1.1.1.1\nState : Init", IssueCode.OspfNotFull)]
    [InlineData("1.1.1.1 1 2-Way/DROther 00:00:30", IssueCode.OspfNotFull)]
    [InlineData("Peer 1.1.1.1 Session State: Down", IssueCode.BfdDown)]
    public void NonHealthyProtocolStateCreatesWarning(string output, IssueCode expectedCode)
    {
        var findings = expectedCode == IssueCode.BfdDown
            ? RoutingStatusRules.EvaluateBfd(output)
            : RoutingStatusRules.EvaluateOspf(output);

        Assert.Equal(expectedCode, Assert.Single(findings).Code);
    }

    [Theory]
    [InlineData("Router ID: 1.1.1.1\nState : Full")]
    [InlineData("1.1.1.1 1 Full/DR 00:00:30")]
    public void FullOspfStateDoesNotCreateWarning(string output) =>
        Assert.Empty(RoutingStatusRules.EvaluateOspf(output));

    [Fact]
    public void UpBfdStateDoesNotCreateWarning() =>
        Assert.Empty(RoutingStatusRules.EvaluateBfd("Peer 1.1.1.1 Session State: Up"));
}
