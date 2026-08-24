using IDCLogChecker.Core.Reporting;
using IDCLogChecker.Core.Scanning;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class EndToEndFixtureTests
{
    public static TheoryData<string, IssueCode?, IssueSeverity?> Cases => new()
    {
        { "01-valid", null, null },
        { "02-root-missing", IssueCode.RootNotFound, IssueSeverity.Error },
        { "03-wrong-root-level", IssueCode.MissingDirectory, IssueSeverity.Error },
        { "04-missing-directory", IssueCode.MissingDirectory, IssueSeverity.Error },
        { "05-extra-directory", IssueCode.ExtraDirectory, IssueSeverity.Error },
        { "06-directory-case", IssueCode.DirectoryCaseMismatch, IssueSeverity.Error },
        { "07-missing-txt", IssueCode.MissingTxtFile, IssueSeverity.Error },
        { "08-extra-txt", IssueCode.ExtraTxtFile, IssueSeverity.Error },
        { "09-txt-case", IssueCode.TxtFileCaseMismatch, IssueSeverity.Error },
        { "10-zero-byte", IssueCode.EmptyTxtFile, IssueSeverity.Error },
        { "11-bom-only", IssueCode.EmptyTxtFile, IssueSeverity.Error },
        { "12-whitespace-only", IssueCode.EmptyTxtFile, IssueSeverity.Error },
        { "13-one-line", IssueCode.OneLineTxtFile, IssueSeverity.Warning },
        { "14-non-txt", IssueCode.NonTxtFile, IssueSeverity.Warning },
        { "15-nested-directory", IssueCode.NestedDirectory, IssueSeverity.Warning },
        { "16-chinese-space-path", null, null },
        { "17-multiple-findings", IssueCode.EmptyTxtFile, IssueSeverity.Error },
        { "18-report-export", null, null },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ApprovedCaseProducesExpectedOutcome(
        string caseId,
        IssueCode? expectedCode,
        IssueSeverity? expectedSeverity)
    {
        using var fixture = FixtureCaseFactory.Create(caseId);

        var result = await fixture.Scanner.ScanAsync(fixture.ScanPath);

        if (expectedCode is null)
        {
            Assert.DoesNotContain(result.Issues, issue => issue.Severity == IssueSeverity.Error);
        }
        else
        {
            Assert.Contains(result.Issues, issue =>
                issue.Code == expectedCode && issue.Severity == expectedSeverity);
        }

        if (caseId == "17-multiple-findings")
        {
            Assert.True(result.Issues.Select(issue => issue.Code).Distinct().Count() >= 3);
        }

        if (caseId == "18-report-export")
        {
            var reportPath = Path.Combine(fixture.WorkPath, "导出的报告.txt");
            await ChineseTextReportWriter.SaveAsync(result, reportPath);
            Assert.True(File.Exists(reportPath));
            Assert.Contains("总体结论：检查通过", await File.ReadAllTextAsync(reportPath));
        }
    }
}
