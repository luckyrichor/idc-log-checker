using System.Text;

namespace IDCLogChecker.Core.ContentAnalysis;

public static class CommandOutputReader
{
    private const int WindowSize = 512;
    public const int MaximumRetainedLines = WindowSize * 2;

    public static async Task<CommandOutputDocument> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var file = new FileInfo(path);
        var first = new List<string>(WindowSize);
        var last = new Queue<string>(WindowSize);
        var count = 0;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            count++;
            if (first.Count < WindowSize)
            {
                first.Add(line);
                continue;
            }

            if (last.Count == WindowSize)
            {
                last.Dequeue();
            }

            last.Enqueue(line);
        }

        var truncated = count > MaximumRetainedLines;
        IReadOnlyList<string> retained = truncated
            ? [.. first, .. last]
            : count <= WindowSize
                ? first
                : [.. first, .. last];
        var preview = string.Join(Environment.NewLine, retained.Take(32));
        return new CommandOutputDocument(path, file.Length, count, retained, preview, truncated);
    }
}
