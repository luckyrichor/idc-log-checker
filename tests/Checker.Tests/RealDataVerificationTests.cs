using IDCLogChecker.Core.Scanning;
using Xunit;
using Xunit.Abstractions;

namespace IDCLogChecker.Tests;

public sealed class RealDataVerificationTests
{
    private readonly ITestOutputHelper _output;

    public RealDataVerificationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task GeneratedFullValidCaseMatchesEntireEmbeddedBaseline()
    {
        var projectRoot = FindProjectRoot();
        var path = Path.Combine(projectRoot, "test", ".generated", "01-valid");
        Assert.True(Directory.Exists(path), "请先运行 test/automated/generate-cases.sh");

        var result = await DirectoryScanner.CreateDefault().ScanAsync(path);

        Assert.Equal(62, result.Summary.ActualDirectoryCount);
        Assert.Equal(3660, result.Summary.ActualTxtFileCount);
        Assert.Equal(3660, result.Summary.CheckedTxtFileCount);
        Assert.Empty(result.Issues);
    }

    [Theory]
    [InlineData("LogRst_20260823_2359")]
    [InlineData("LogRst_20260801_0004")]
    public async Task RealSourceSnapshotIsScannedReadOnlyAndStructurallyExact(string sourceName)
    {
        var projectRoot = FindProjectRoot();
        var sourcePath = Path.GetFullPath(Path.Combine(projectRoot, "..", sourceName));
        Assert.True(Directory.Exists(sourcePath), $"找不到真实数据目录：{sourcePath}");
        var before = SnapshotMetadata(sourcePath);

        var result = await DirectoryScanner.CreateDefault().ScanAsync(sourcePath);

        var after = SnapshotMetadata(sourcePath);
        Assert.Equal(before, after);
        Assert.Equal(62, result.Summary.ActualDirectoryCount);
        Assert.Equal(3660, result.Summary.ActualTxtFileCount);
        Assert.DoesNotContain(result.Issues, IsStructuralError);
        _output.WriteLine(
            "{0}: errors={1}, warnings={2}, empty={3}, oneLine={4}",
            sourceName,
            result.Summary.ErrorCount,
            result.Summary.WarningCount,
            result.Issues.Count(issue => issue.Code == IssueCode.EmptyTxtFile),
            result.Issues.Count(issue => issue.Code == IssueCode.OneLineTxtFile));
    }

    private static bool IsStructuralError(ScanIssue issue) =>
        issue.Code is IssueCode.MissingDirectory
            or IssueCode.ExtraDirectory
            or IssueCode.DirectoryCaseMismatch
            or IssueCode.MissingTxtFile
            or IssueCode.ExtraTxtFile
            or IssueCode.TxtFileCaseMismatch;

    private static string[] SnapshotMetadata(string root) => Directory
        .EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Select(path =>
        {
            var info = new FileInfo(path);
            return $"{Path.GetRelativePath(root, path)}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        })
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IDCLogChecker.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("无法定位项目根目录。");
    }
}
