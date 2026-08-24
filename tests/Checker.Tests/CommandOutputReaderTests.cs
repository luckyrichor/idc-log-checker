using IDCLogChecker.Core.ContentAnalysis;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class CommandOutputReaderTests
{
    [Fact]
    public async Task NormalizationRemovesBomPromptCommandEchoPagingAndTrailingPrompt()
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile(
            "display cpu.txt",
            "\uFEFF<Device-A>\r\n<Device-A>display cpu\r\nCPU Usage : 12%\r\n---- More ----\r\n<Device-A>\r\n");

        var document = await CommandOutputReader.ReadAsync(path, default);
        var output = CommandOutputNormalizer.Normalize(document, "Device-A", "display cpu.txt");

        Assert.Equal(["CPU Usage : 12%"], output.EffectiveLines);
        Assert.Equal(5, output.RawLineCount);
    }

    [Fact]
    public async Task LargeOutputRetainsBoundedAnalysisWindows()
    {
        using var fixture = new TestDirectory();
        var content = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"route-{index:D5}"));
        var path = fixture.WriteFile("display ip routing-table.txt", content);

        var document = await CommandOutputReader.ReadAsync(path, default);

        Assert.Equal(20_000, document.RawLineCount);
        Assert.True(document.TruncatedForAnalysis);
        Assert.InRange(document.AnalysisLines.Count, 1, CommandOutputReader.MaximumRetainedLines);
        Assert.Contains("route-00000", document.AnalysisLines);
        Assert.Contains("route-19999", document.AnalysisLines);
    }
}
