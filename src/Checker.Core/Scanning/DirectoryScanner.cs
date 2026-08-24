using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.ContentAnalysis;

namespace IDCLogChecker.Core.Scanning;

public sealed class DirectoryScanner
{
    private readonly IReadOnlyList<BaselineDevice> _devices;
    private readonly ContentAnalyzer _contentAnalyzer = new();

    public DirectoryScanner(IReadOnlyList<BaselineDevice> devices)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
    }

    public static DirectoryScanner CreateDefault() => new(BaselineManifest.LoadEmbedded().Devices);

    public Task<ScanResult> ScanAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Scan(rootPath, progress, cancellationToken), cancellationToken);

    private ScanResult Scan(
        string rootPath,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.Now;
        var issues = new List<ScanIssue>();
        var expectedTxtCount = _devices.Sum(device => device.TxtFiles.Count);
        var actualDirectoryCount = 0;
        var actualTxtCount = 0;
        var checkedTxtCount = 0;
        var contentNormalCount = 0;
        var unsupportedContentRuleCount = 0;

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            issues.Add(new ScanIssue(
                IssueSeverity.Error,
                IssueCode.RootNotFound,
                "所选文件夹不存在，请重新选择巡检结果所在的文件夹。",
                Path: rootPath));
            return BuildResult();
        }

        DirectoryInfo[] actualDirectories;
        try
        {
            actualDirectories = new DirectoryInfo(rootPath).GetDirectories();
            actualDirectoryCount = actualDirectories.Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            issues.Add(new ScanIssue(
                IssueSeverity.Error,
                IssueCode.RootUnreadable,
                $"无法读取所选文件夹：{exception.Message}",
                Path: rootPath));
            return BuildResult();
        }

        var expectedDirectoryNames = _devices.Select(device => device.Name).ToArray();
        var actualDirectoryNames = actualDirectories.Select(directory => directory.Name).ToArray();
        var directoryDiff = CompareExactNames(expectedDirectoryNames, actualDirectoryNames);

        foreach (var (expected, actual) in directoryDiff.CaseMismatches)
        {
            issues.Add(new ScanIssue(
                IssueSeverity.Error,
                IssueCode.DirectoryCaseMismatch,
                $"设备目录名称大小写不一致：应为“{expected}”，实际为“{actual}”。",
                Path: System.IO.Path.Combine(rootPath, actual),
                Expected: expected,
                Actual: actual));
        }

        foreach (var missing in directoryDiff.Missing)
        {
            issues.Add(new ScanIssue(
                IssueSeverity.Error,
                IssueCode.MissingDirectory,
                $"缺少设备目录“{missing}”。",
                Path: System.IO.Path.Combine(rootPath, missing),
                Expected: missing));
        }

        foreach (var extra in directoryDiff.Extra)
        {
            issues.Add(new ScanIssue(
                IssueSeverity.Error,
                IssueCode.ExtraDirectory,
                $"多出了基准中不存在的设备目录“{extra}”。",
                Path: System.IO.Path.Combine(rootPath, extra),
                Actual: extra));
        }

        var actualByExactName = actualDirectories.ToDictionary(directory => directory.Name, StringComparer.Ordinal);
        for (var index = 0; index < _devices.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var device = _devices[index];
            progress?.Report(new ScanProgress(index, _devices.Count, device.Name));

            if (!actualByExactName.TryGetValue(device.Name, out var deviceDirectory))
            {
                continue;
            }

            FileInfo[] files;
            DirectoryInfo[] nestedDirectories;
            try
            {
                files = deviceDirectory.GetFiles();
                nestedDirectories = deviceDirectory.GetDirectories();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Error,
                    IssueCode.RootUnreadable,
                    $"无法读取设备目录“{device.Name}”：{exception.Message}",
                    device.Name,
                    deviceDirectory.FullName));
                continue;
            }

            foreach (var nestedDirectory in nestedDirectories.OrderBy(directory => directory.Name, StringComparer.Ordinal))
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Warning,
                    IssueCode.NestedDirectory,
                    $"设备目录“{device.Name}”内存在额外子目录“{nestedDirectory.Name}”，程序不会递归检查该目录。",
                    device.Name,
                    nestedDirectory.FullName,
                    Actual: nestedDirectory.Name));
            }

            var txtFiles = files
                .Where(file => file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            actualTxtCount += txtFiles.Length;

            foreach (var nonTxtFile in files
                         .Except(txtFiles)
                         .OrderBy(file => file.Name, StringComparer.Ordinal))
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Warning,
                    IssueCode.NonTxtFile,
                    $"设备目录“{device.Name}”内存在非 TXT 文件“{nonTxtFile.Name}”。",
                    device.Name,
                    nonTxtFile.FullName,
                    Actual: nonTxtFile.Name));
            }

            var fileDiff = CompareExactNames(device.TxtFiles, txtFiles.Select(file => file.Name));
            foreach (var (expected, actual) in fileDiff.CaseMismatches)
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Error,
                    IssueCode.TxtFileCaseMismatch,
                    $"设备“{device.Name}”中的 TXT 文件名大小写不一致：应为“{expected}”，实际为“{actual}”。",
                    device.Name,
                    System.IO.Path.Combine(deviceDirectory.FullName, actual),
                    expected,
                    actual));
            }

            foreach (var missing in fileDiff.Missing)
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Error,
                    IssueCode.MissingTxtFile,
                    $"设备“{device.Name}”缺少 TXT 文件“{missing}”。",
                    device.Name,
                    System.IO.Path.Combine(deviceDirectory.FullName, missing),
                    Expected: missing));
            }

            foreach (var extra in fileDiff.Extra)
            {
                issues.Add(new ScanIssue(
                    IssueSeverity.Error,
                    IssueCode.ExtraTxtFile,
                    $"设备“{device.Name}”多出了 TXT 文件“{extra}”。",
                    device.Name,
                    System.IO.Path.Combine(deviceDirectory.FullName, extra),
                    Actual: extra));
            }

            var txtByExactName = txtFiles.ToDictionary(file => file.Name, StringComparer.Ordinal);
            foreach (var expectedFileName in device.TxtFiles)
            {
                if (!txtByExactName.TryGetValue(expectedFileName, out var file))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                checkedTxtCount++;
                ContentAnalysisResult analysis;
                try
                {
                    analysis = _contentAnalyzer.AnalyzeAsync(
                            device.Name, expectedFileName, file.FullName, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (exception is IOException
                    or UnauthorizedAccessException
                    or System.Security.SecurityException
                    or System.Text.DecoderFallbackException)
                {
                    issues.Add(new ScanIssue(
                        IssueSeverity.Error,
                        IssueCode.UnreadableTxtFile,
                        $"无法读取 TXT 文件“{expectedFileName}”：{exception.Message}",
                        device.Name,
                        file.FullName,
                        Expected: "文件可读取",
                        Actual: exception.Message));
                    continue;
                }

                if (!analysis.HasVisibleContent)
                {
                    issues.Add(new ScanIssue(
                        IssueSeverity.Error,
                        IssueCode.EmptyTxtFile,
                        $"TXT 文件“{expectedFileName}”没有有效内容（0 字节、仅 BOM 或仅空白）。",
                        device.Name,
                        file.FullName,
                        Expected: "至少包含一行有效内容",
                        Actual: $"{analysis.ByteLength} 字节"));
                    continue;
                }

                if (analysis.RawLineCount == 1)
                {
                    issues.Add(new ScanIssue(
                        IssueSeverity.Warning,
                        IssueCode.OneLineTxtFile,
                        $"TXT 文件“{expectedFileName}”只有一行，请人工确认命令是否确实没有返回正文。",
                        device.Name,
                        file.FullName,
                        Expected: "通常应包含两行或更多内容",
                        Actual: analysis.Preview));
                }

                if (!analysis.HasDedicatedRule)
                {
                    unsupportedContentRuleCount++;
                }
                else if (analysis.IsContentNormal)
                {
                    contentNormalCount++;
                }

                foreach (var finding in analysis.Findings)
                {
                    issues.Add(new ScanIssue(
                        finding.Severity,
                        finding.Code,
                        finding.Message,
                        device.Name,
                        file.FullName,
                        finding.Expected,
                        finding.Actual)
                    {
                        RuleCode = finding.RuleCode,
                        SuggestedAction = finding.SuggestedAction,
                    });
                }
            }
        }

        progress?.Report(new ScanProgress(_devices.Count, _devices.Count, "检查完成"));
        return BuildResult();

        ScanResult BuildResult()
        {
            var orderedIssues = issues
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.DeviceName, StringComparer.Ordinal)
                .ThenBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code)
                .ToArray();
            var summary = new ScanSummary(
                _devices.Count,
                actualDirectoryCount,
                expectedTxtCount,
                actualTxtCount,
                checkedTxtCount,
                orderedIssues.Count(issue => issue.Severity == IssueSeverity.Error),
                orderedIssues.Count(issue => issue.Severity == IssueSeverity.Warning))
            {
                IndeterminateCount = orderedIssues.Count(issue => issue.Severity == IssueSeverity.Indeterminate),
                ContentNormalCount = contentNormalCount,
                UnsupportedContentRuleCount = unsupportedContentRuleCount,
            };
            return new ScanResult(rootPath, startedAt, DateTimeOffset.Now, summary, orderedIssues);
        }
    }

    private static NameDiff CompareExactNames(IEnumerable<string> expectedNames, IEnumerable<string> actualNames)
    {
        var expected = expectedNames.ToHashSet(StringComparer.Ordinal);
        var actual = actualNames.ToHashSet(StringComparer.Ordinal);
        var missing = expected.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var extra = actual.Except(expected, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var caseMismatches = new List<(string Expected, string Actual)>();

        foreach (var expectedName in missing.ToArray())
        {
            var actualName = extra.FirstOrDefault(name =>
                name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            if (actualName is null)
            {
                continue;
            }

            caseMismatches.Add((expectedName, actualName));
            missing.Remove(expectedName);
            extra.Remove(actualName);
        }

        return new NameDiff(missing, extra, caseMismatches);
    }

    private sealed record NameDiff(
        IReadOnlyList<string> Missing,
        IReadOnlyList<string> Extra,
        IReadOnlyList<(string Expected, string Actual)> CaseMismatches);
}
