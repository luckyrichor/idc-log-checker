using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class DirectoryStructureTests
{
    [Fact]
    public async Task ExactDeviceDirectorySetHasNoStructureErrors()
    {
        using var fixture = ValidFixture();
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.DoesNotContain(result.Issues, issue => issue.Severity == IssueSeverity.Error);
        Assert.Equal(2, result.Summary.ExpectedDirectoryCount);
        Assert.Equal(2, result.Summary.ActualDirectoryCount);
    }

    [Fact]
    public async Task MissingAndExtraDeviceDirectoriesAreDetailedErrors()
    {
        using var fixture = new TestDirectory();
        fixture.CreateDirectory("Device-A");
        fixture.CreateDirectory("Device-C");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.MissingDirectory && issue.Expected == "Device-B");
        Assert.Contains(result.Issues, issue =>
            issue.Code == IssueCode.ExtraDirectory && issue.Actual == "Device-C");
    }

    [Fact]
    public async Task CaseOnlyDirectoryDifferenceIsOneSpecificError()
    {
        using var fixture = new TestDirectory();
        fixture.CreateDirectory("device-a");
        fixture.CreateDirectory("Device-B");
        var scanner = CreateScanner();

        var result = await scanner.ScanAsync(fixture.Path);

        var issue = Assert.Single(result.Issues, item => item.Code == IssueCode.DirectoryCaseMismatch);
        Assert.Equal("Device-A", issue.Expected);
        Assert.Equal("device-a", issue.Actual);
        Assert.DoesNotContain(result.Issues, item => item.Code == IssueCode.MissingDirectory);
        Assert.DoesNotContain(result.Issues, item => item.Code == IssueCode.ExtraDirectory);
    }

    private static DirectoryScanner CreateScanner() => new(
    [
        new BaselineDevice("Device-A", ["one.txt", "two.txt"]),
        new BaselineDevice("Device-B", ["status.txt"]),
    ]);

    private static TestDirectory ValidFixture()
    {
        var fixture = new TestDirectory();
        fixture.WriteFile("Device-A/one.txt");
        fixture.WriteFile("Device-A/two.txt");
        fixture.WriteFile("Device-B/status.txt");
        return fixture;
    }
}

