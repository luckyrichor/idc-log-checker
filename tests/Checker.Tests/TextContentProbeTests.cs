using System.Text;
using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class TextContentProbeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   \t\r\n")]
    [InlineData("\r\n\r\n")]
    public async Task EmptyOrWhitespaceOnlyFilesAreEmpty(string content)
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile("empty.txt", content);

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.Empty, result.Kind);
    }

    [Fact]
    public async Task Utf8BomWithoutTextIsEmpty()
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile("bom.txt", string.Empty);
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetPreamble());

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.Empty, result.Kind);
    }

    [Theory]
    [InlineData("只有一行")]
    [InlineData("只有一行\r\n")]
    public async Task OneLogicalLineIsClassifiedAndPreviewed(string content)
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile("one-line.txt", content);

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.OneLine, result.Kind);
        Assert.Equal("只有一行", result.Preview);
    }

    [Fact]
    public async Task TwoLogicalLinesAreMultipleLines()
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile("two-lines.txt", "第一行\r\n第二行\r\n");

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.MultipleLines, result.Kind);
    }

    [Fact]
    public async Task PreviewIsCappedAtTwoHundredCharacters()
    {
        using var fixture = new TestDirectory();
        var path = fixture.WriteFile("long.txt", new string('甲', 250));

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.OneLine, result.Kind);
        Assert.Equal(200, result.Preview.Length);
    }

    [Fact]
    public async Task MissingFileIsUnreadableWithDetails()
    {
        using var fixture = new TestDirectory();
        var path = System.IO.Path.Combine(fixture.Path, "missing.txt");

        var result = await TextContentProbe.ProbeAsync(path);

        Assert.Equal(TextContentKind.Unreadable, result.Kind);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task ScannerTreatsEmptyAsErrorAndOneLineAsWarning()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/empty.txt", string.Empty);
        fixture.WriteFile("Device-A/one.txt", "只有一行");
        var scanner = new DirectoryScanner(
        [
            new BaselineDevice("Device-A", ["empty.txt", "one.txt"]),
        ]);

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.EmptyTxtFile && issue.Severity == IssueSeverity.Error);
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.OneLineTxtFile && issue.Severity == IssueSeverity.Warning);
    }
}
