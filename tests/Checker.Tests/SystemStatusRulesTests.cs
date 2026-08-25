using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class SystemStatusRulesTests
{
    [Theory]
    [InlineData("System CPU Using Percentage : 72%", "72")]
    [InlineData("CPU Usage : 72% Max: 90%", "72")]
    [InlineData("72% in last 5 seconds", "72")]
    [InlineData("CPU utilization in five seconds: 72.0%", "72")]
    public void CurrentCpuAtSeventyPercentCreatesWarning(string output, string expected)
    {
        var finding = Assert.Single(SystemStatusRules.EvaluateCpu(output));

        Assert.Equal(IssueCode.CpuUsageHigh, finding.Code);
        Assert.Contains(expected, finding.Actual);
    }

    [Theory]
    [InlineData("CPU Usage : 69%", 0)]
    [InlineData("CPU Usage : 70%", 1)]
    [InlineData("CPU Usage : 90%", 1)]
    public void CpuThresholdUsesCurrentValue(string output, int expectedCount)
    {
        Assert.Equal(expectedCount, SystemStatusRules.EvaluateCpu(output).Count);
    }

    [Theory]
    [InlineData("Memory Using Percentage: 70%")]
    [InlineData("System Memory: 100KB total, 70KB used, 30KB free, 70.00% used rate")]
    [InlineData("Memory FreeRatio : 30%")]
    public void MemoryAtSeventyPercentCreatesWarning(string output)
    {
        Assert.Equal(IssueCode.MemoryUsageHigh, Assert.Single(SystemStatusRules.EvaluateMemory(output)).Code);
    }

    [Theory]
    [InlineData("clock status: synchronized\nclock stratum: 2", 0)]
    [InlineData("Clock is synchronized, stratum 3", 0)]
    [InlineData("clock status: unsynchronized\nclock stratum: 16", 1)]
    [InlineData("Clock is unsynchronized, stratum 16", 1)]
    public void NtpStateIsInterpretedWithoutSubstringConfusion(string output, int expectedCount)
    {
        Assert.Equal(expectedCount, SystemStatusRules.EvaluateNtp(output).Count);
    }
}
