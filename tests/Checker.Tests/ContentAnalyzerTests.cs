using IDCLogChecker.Core.ContentAnalysis;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class ContentAnalyzerTests
{
    [Fact]
    public async Task UnknownCpuFormatIsIndeterminateInsteadOfSuccess()
    {
        var result = await AnalyzeAsync("Device-S5552", "display cpu.txt", "NEW VENDOR MESSAGE");

        var finding = Assert.Single(result.Findings);
        Assert.Equal(IssueSeverity.Indeterminate, finding.Severity);
        Assert.Equal(IssueCode.CommandOutputUnrecognized, finding.Code);
        Assert.False(result.IsContentNormal);
        Assert.True(result.HasDedicatedRule);
    }

    [Theory]
    [InlineData("show debugging.txt", "Device-N18010#show debugging\r\n")]
    [InlineData("display debugging.txt", "<Device-CE16808>display debugging\r\n<Device-CE16808>\r\n")]
    public async Task EmptyDebuggingBodyIsAllowed(string fileName, string content)
    {
        var result = await AnalyzeAsync("Device-N18010", fileName, content);

        Assert.Empty(result.Findings);
        Assert.True(result.IsContentNormal);
        Assert.True(result.HasDedicatedRule);
    }

    [Fact]
    public async Task ExplicitCommandErrorStopsSuccessValidation()
    {
        var result = await AnalyzeAsync(
            "Device-S5552",
            "display cpu.txt",
            "% Unrecognized command found at '^' position.");

        var finding = Assert.Single(result.Findings);
        Assert.Equal(IssueCode.CommandUnrecognized, finding.Code);
        Assert.Equal(IssueSeverity.Error, finding.Severity);
    }

    [Fact]
    public async Task UnsupportedCommandIsCountedWithoutBeingCalledNormal()
    {
        var result = await AnalyzeAsync("Device-A", "custom command.txt", "some result");

        Assert.Empty(result.Findings);
        Assert.False(result.IsContentNormal);
        Assert.False(result.HasDedicatedRule);
    }

    private static async Task<ContentAnalysisResult> AnalyzeAsync(string deviceName, string fileName, string content)
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile(fileName, content);
        return await new ContentAnalyzer().AnalyzeAsync(deviceName, fileName, path, default);
    }
}
