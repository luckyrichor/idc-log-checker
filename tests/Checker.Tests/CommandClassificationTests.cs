using IDCLogChecker.Core.ContentAnalysis;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class CommandClassificationTests
{
    [Theory]
    [InlineData("SH-X-S5552", DeviceFamily.S5552)]
    [InlineData("SH-X-CE16808-HX", DeviceFamily.CE16808)]
    [InlineData("SH-X-N18010", DeviceFamily.N18010)]
    [InlineData("SH-X-S12516-HX", DeviceFamily.S12516)]
    [InlineData("SH-X-UNKNOWN", DeviceFamily.Unknown)]
    public void DeviceNameSuffixResolvesFamily(string name, DeviceFamily expected)
    {
        Assert.Equal(expected, DeviceFamilyResolver.Resolve(name, NormalizedCommandOutput.Empty));
    }

    [Theory]
    [InlineData("display cpu.txt", CommandKind.Cpu)]
    [InlineData("show processes cpu.txt", CommandKind.Cpu)]
    [InlineData("display ntp-service status.txt", CommandKind.NtpStatus)]
    [InlineData("display alarm active.txt", CommandKind.AlarmActive)]
    [InlineData("show ip bgp summary.txt", CommandKind.BgpSummary)]
    [InlineData("dis bgp routing-table peer 117.185.10.7 advertised-routes.txt", CommandKind.BgpAdvertisedRoutes)]
    [InlineData("show running-config.txt", CommandKind.Configuration)]
    [InlineData("unexpected.txt", CommandKind.Unknown)]
    public void FileNameResolvesCommandKind(string fileName, CommandKind expected)
    {
        Assert.Equal(expected, CommandClassifier.Classify(fileName));
    }
}
