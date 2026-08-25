using IDCLogChecker.Core.ContentAnalysis;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class CommandValidationRegistryTests
{
    [Theory]
    [InlineData(CommandKind.Cpu, "System CPU Using Percentage : 12%")]
    [InlineData(CommandKind.Memory, "Memory Using Percentage: 20%")]
    [InlineData(CommandKind.NtpStatus, "clock status: synchronized")]
    [InlineData(CommandKind.Version, "VRP (R) software, Version 8")]
    [InlineData(CommandKind.BgpSummary, "Total number of peers : 2")]
    [InlineData(CommandKind.OspfPeer, "OSPF process 10, 3 Neighbors, 3 is Full:")]
    [InlineData(CommandKind.Fan, "fan-id status mode\n1 ok normal")]
    public void KnownStructuralMarkerConfirmsCommandOutput(CommandKind kind, string output)
    {
        var context = Context(kind, output.Split('\n'));

        var result = CommandValidationRegistry.Validate(context);

        Assert.True(result.IsRecognized);
        Assert.True(result.IsSuccessful);
        Assert.False(string.IsNullOrWhiteSpace(result.ExpectedDescription));
    }

    [Fact]
    public void NewCpuFormatIsRecognizedButNotConfirmed()
    {
        var result = CommandValidationRegistry.Validate(Context(CommandKind.Cpu, "NEW VENDOR MESSAGE"));

        Assert.True(result.IsRecognized);
        Assert.False(result.IsSuccessful);
    }

    private static ContentAnalysisContext Context(CommandKind kind, params string[] lines)
    {
        var document = new CommandOutputDocument("sample.txt", 10, lines.Length, lines, string.Join('\n', lines), false);
        var output = new NormalizedCommandOutput(lines.Length, lines, string.Join('\n', lines), false);
        return new ContentAnalysisContext("Device-S5552", "sample.txt", "sample.txt", DeviceFamily.S5552, kind, document, output);
    }
}
