using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.Batch;

public sealed class BatchScanCoordinator
{
    private readonly DirectoryScanner _scanner;

    public BatchScanCoordinator(DirectoryScanner scanner) =>
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));

    public static BatchScanCoordinator CreateDefault() => new(DirectoryScanner.CreateDefault());

    public Task<BatchScanResult> ScanAsync(
        IReadOnlyList<string> paths,
        IProgress<BatchScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        ScanAsync(new BatchInputResult(paths, [], []), progress, cancellationToken);

    public async Task<BatchScanResult> ScanAsync(
        BatchInputResult input,
        IProgress<BatchScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var startedAt = DateTimeOffset.Now;
        var results = new List<FolderScanResult>(input.ValidPaths.Count);

        for (var index = 0; index < input.ValidPaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = input.ValidPaths[index];
            var innerProgress = new InlineProgress<ScanProgress>(item => progress?.Report(
                new BatchScanProgress(index, input.ValidPaths.Count, index + 1, path, item)));
            ScanResult result;
            try
            {
                result = await _scanner.ScanAsync(path, innerProgress, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var now = DateTimeOffset.Now;
                var issue = new ScanIssue(
                    IssueSeverity.Error,
                    IssueCode.RootUnreadable,
                    $"检查该文件夹时发生异常：{exception.Message}",
                    Path: path,
                    Expected: "文件夹可以正常检查",
                    Actual: exception.Message);
                result = new ScanResult(path, now, now,
                    new ScanSummary(0, 0, 0, 0, 0, 1, 0), [issue]);
            }

            results.Add(new FolderScanResult(path, result));
            progress?.Report(new BatchScanProgress(
                index + 1, input.ValidPaths.Count, index + 1, path, null));
        }

        return new BatchScanResult(input, startedAt, DateTimeOffset.Now, results);
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
