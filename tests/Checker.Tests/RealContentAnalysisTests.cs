using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class RealContentAnalysisTests
{
    [Fact]
    public async Task ScannerConvertsExecutionFailureToDetailedDeviceFileIssue()
    {
        var result = await ScanSingleAsync(
            "Device-S5552",
            "display cpu.txt",
            "Device-S5552#display cpu\r\n% Unrecognized command found at '^' position.\r\n");

        var issue = Assert.Single(result.Issues, item => item.Code == IssueCode.CommandUnrecognized);
        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.Equal("Device-S5552", issue.DeviceName);
        Assert.EndsWith("display cpu.txt", issue.Path, StringComparison.Ordinal);
        Assert.Equal("CLI_UNRECOGNIZED_COMMAND", issue.RuleCode);
        Assert.False(string.IsNullOrWhiteSpace(issue.SuggestedAction));
    }

    [Fact]
    public async Task ScannerKeepsUnknownCpuOutputAsIndeterminate()
    {
        var result = await ScanSingleAsync(
            "Device-S5552",
            "display cpu.txt",
            "Device-S5552#display cpu\r\nNEW VENDOR MESSAGE\r\nEND\r\n");

        Assert.Equal(1, result.Summary.IndeterminateCount);
        Assert.Equal(IssueSeverity.Indeterminate,
            Assert.Single(result.Issues, item => item.Code == IssueCode.CommandOutputUnrecognized).Severity);
    }

    [Fact]
    public async Task ScannerAddsNtpAndEachActiveAlarmWarning()
    {
        using var fixture = new TestDirectory();
        const string device = "Device-CE16808";
        fixture.WriteFile($"{device}/display ntp status.txt",
            "<Device>display ntp status\r\nclock status: unsynchronized\r\nclock stratum: 16\r\n");
        fixture.WriteFile($"{device}/display alarm active.txt",
            "<Device>display alarm active\r\nSequence AlarmId Severity Date Time Description\r\n" +
            "1 0x1 Critical 2026-08-24 Board failed\r\n2 0x2 Major 2026-08-24 Link down\r\n");
        var scanner = new DirectoryScanner([
            new BaselineDevice(device, ["display ntp status.txt", "display alarm active.txt"]),
        ]);

        var result = await scanner.ScanAsync(fixture.Path);

        Assert.Single(result.Issues, item => item.Code == IssueCode.NtpUnsynchronized);
        Assert.Single(result.Issues, item => item.Code == IssueCode.AlarmCritical);
        Assert.Single(result.Issues, item => item.Code == IssueCode.AlarmMajor);
        Assert.Equal(3, result.Summary.WarningCount);
    }

    [Fact]
    public async Task DedicatedSuccessfulContentIncrementsNormalCounter()
    {
        var result = await ScanSingleAsync(
            "Device-N18010",
            "show debugging.txt",
            "Device-N18010#show debugging\r\n");

        Assert.Equal(1, result.Summary.ContentNormalCount);
        Assert.Equal(0, result.Summary.UnsupportedContentRuleCount);
        Assert.Contains(result.Issues, issue => issue.Code == IssueCode.OneLineTxtFile);
    }

    private static async Task<ScanResult> ScanSingleAsync(
        string device,
        string fileName,
        string content)
    {
        using var fixture = new TestDirectory();
        fixture.WriteFile($"{device}/{fileName}", content);
        var scanner = new DirectoryScanner([new BaselineDevice(device, [fileName])]);
        return await scanner.ScanAsync(fixture.Path);
    }
}
