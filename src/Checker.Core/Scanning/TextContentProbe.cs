using System.Text;

namespace IDCLogChecker.Core.Scanning;

public enum TextContentKind
{
    Empty,
    OneLine,
    MultipleLines,
    Unreadable,
}

public sealed record TextProbeResult(
    TextContentKind Kind,
    long ByteLength,
    string Preview,
    string? ErrorMessage = null);

public static class TextContentProbe
{
    private const int PreviewLimit = 200;

    public static async Task<TextProbeResult> ProbeAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            var byteLength = fileInfo.Length;
            if (byteLength == 0)
            {
                return new TextProbeResult(TextContentKind.Empty, 0, string.Empty);
            }

            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: false);

            var logicalLineCount = 0;
            var hasVisibleContent = false;
            var preview = string.Empty;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                logicalLineCount++;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    hasVisibleContent = true;
                    if (preview.Length == 0)
                    {
                        preview = line.Length <= PreviewLimit ? line : line[..PreviewLimit];
                    }
                }

                if (logicalLineCount >= 2 && hasVisibleContent)
                {
                    return new TextProbeResult(TextContentKind.MultipleLines, byteLength, preview);
                }
            }

            if (!hasVisibleContent)
            {
                return new TextProbeResult(TextContentKind.Empty, byteLength, string.Empty);
            }

            return new TextProbeResult(TextContentKind.OneLine, byteLength, preview);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException
                or DecoderFallbackException)
        {
            return new TextProbeResult(
                TextContentKind.Unreadable,
                0,
                string.Empty,
                exception.Message);
        }
    }
}

