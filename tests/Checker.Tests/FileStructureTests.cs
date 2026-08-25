using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class FileStructureTests
{
    [Fact]
    public async Task MissingAndExtraTxtFilesAreDetailedErrors()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt");
        fixture.WriteFile("Device-A/extra.txt");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.MissingTxtFile && issue.Expected == "two.txt");
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.ExtraTxtFile && issue.Actual == "extra.txt");
    }

    [Fact]
    public async Task CaseOnlyTxtDifferenceIsOneSpecificError()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt");
        fixture.WriteFile("Device-A/TWO.txt");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        var issue = Assert.Single(result.Issues, item => item.Code == IssueCode.TxtFileCaseMismatch);
        Assert.Equal("two.txt", issue.Expected);
        Assert.Equal("TWO.txt", issue.Actual);
    }

    [Fact]
    public async Task ExtraTxtFileIsStillCheckedForExecutionErrors()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt", "prompt\nnormal output");
        fixture.WriteFile("Device-A/two.txt", "prompt\nnormal output");
        fixture.WriteFile("Device-A/extra.txt", "prompt\n% Unrecognized command found at '^' position.");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue => issue.Code == IssueCode.ExtraTxtFile);
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.CommandUnrecognized &&
            Path.GetFileName(issue.Path) == "extra.txt");
    }

    [Fact]
    public async Task CaseMismatchedTxtFileIsStillCheckedForExecutionErrors()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt", "prompt\nnormal output");
        fixture.WriteFile("Device-A/TWO.txt", "prompt\n% Unrecognized command found at '^' position.");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue => issue.Code == IssueCode.TxtFileCaseMismatch);
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.CommandUnrecognized &&
            Path.GetFileName(issue.Path) == "TWO.txt");
    }

    [Fact]
    public async Task NonTxtFilesAndNestedDirectoriesAreWarnings()
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt");
        fixture.WriteFile("Device-A/two.txt");
        fixture.WriteFile("Device-A/notes.csv", "说明");
        fixture.CreateDirectory("Device-A/archive");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.NonTxtFile && issue.Severity == IssueSeverity.Warning);
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.NestedDirectory && issue.Severity == IssueSeverity.Warning);
        Assert.DoesNotContain(result.Issues, issue => issue.Code == IssueCode.ExtraTxtFile);
    }

    private static DirectoryScanner CreateScanner() => new(
    [
        new BaselineDevice("Device-A", ["one.txt", "two.txt"]),
    ]);
}
