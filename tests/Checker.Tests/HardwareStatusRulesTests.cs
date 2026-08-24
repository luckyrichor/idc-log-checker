using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class HardwareStatusRulesTests
{
    [Theory]
    [InlineData(CommandKind.Fan, "FAN1 Present YES Status Abnormal", IssueCode.FanAbnormal)]
    [InlineData(CommandKind.Power, "PWR1 Present YES Status Failed", IssueCode.PowerAbnormal)]
    [InlineData(CommandKind.Temperature, "Temperature current 86 C threshold 80 C", IssueCode.TemperatureHigh)]
    [InlineData(CommandKind.Optics, "100GE1/0/1 RX Power: -40 dBm alarm", IssueCode.OpticalAbnormal)]
    [InlineData(CommandKind.Interface, "Eth1 AdminStatus UP OperStatus DOWN", IssueCode.InterfaceDown)]
    [InlineData(CommandKind.Storage, "Flash usage: 92%", IssueCode.StorageUsageHigh)]
    public void ExplicitAbnormalStateCreatesWarning(CommandKind kind, string output, IssueCode expectedCode)
    {
        Assert.Equal(expectedCode, Assert.Single(HardwareStatusRules.Evaluate(kind, output)).Code);
    }

    [Theory]
    [InlineData(CommandKind.Fan, "FAN1 Present YES Status Normal")]
    [InlineData(CommandKind.Power, "PWR1 Present YES Status Normal")]
    [InlineData(CommandKind.Temperature, "Temperature current 40 C threshold 80 C")]
    [InlineData(CommandKind.Interface, "Eth1 AdminStatus UP OperStatus UP")]
    public void HealthyStateDoesNotCreateWarning(CommandKind kind, string output) =>
        Assert.Empty(HardwareStatusRules.Evaluate(kind, output));

    [Fact]
    public void PlainTextPasswordCreatesSecurityRiskWithoutLeakingValue()
    {
        var finding = Assert.Single(HardwareStatusRules.EvaluateConfiguration(
            "local-user admin password simple VerySecret"));

        Assert.Equal(IssueCode.SecurityRisk, finding.Code);
        Assert.DoesNotContain("VerySecret", finding.Actual);
    }
}
