using System.Text.RegularExpressions;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class CommandOutputNormalizer
{
    public static NormalizedCommandOutput Normalize(
        CommandOutputDocument document,
        string deviceName,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(document);
        var command = System.IO.Path.GetFileNameWithoutExtension(fileName).Trim();
        var effective = new List<string>(document.AnalysisLines.Count);
        foreach (var raw in document.AnalysisLines)
        {
            var line = raw.Trim().TrimStart('\uFEFF').Trim();
            if (line.Length == 0 || PagingLine().IsMatch(line) || IsPromptOnly(line))
            {
                continue;
            }

            var withoutPrompt = StripLeadingPrompt(line);
            if (withoutPrompt.Equals(command, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            effective.Add(withoutPrompt);
        }

        var safePreview = string.Join(Environment.NewLine, effective.Take(24));
        return new NormalizedCommandOutput(
            document.RawLineCount,
            effective,
            safePreview,
            document.TruncatedForAnalysis);
    }

    private static bool IsPromptOnly(string line) =>
        AnglePrompt().IsMatch(line) || SquarePrompt().IsMatch(line) || HashPrompt().IsMatch(line);

    private static string StripLeadingPrompt(string line)
    {
        var match = LeadingPrompt().Match(line);
        return match.Success ? line[match.Length..].Trim() : line;
    }

    [GeneratedRegex(@"^-+\s*More\s*-+$", RegexOptions.IgnoreCase)]
    private static partial Regex PagingLine();

    [GeneratedRegex(@"^<[^>]+>$")]
    private static partial Regex AnglePrompt();

    [GeneratedRegex(@"^\[[^\]]+\]$")]
    private static partial Regex SquarePrompt();

    [GeneratedRegex(@"^[\w.()/-]+#$")]
    private static partial Regex HashPrompt();

    [GeneratedRegex(@"^(?:<[^>]+>|\[[^\]]+\]|[\w.()/-]+#)\s*")]
    private static partial Regex LeadingPrompt();
}
