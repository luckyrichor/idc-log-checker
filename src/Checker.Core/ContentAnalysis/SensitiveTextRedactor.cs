using System.Text.RegularExpressions;

namespace IDCLogChecker.Core.ContentAnalysis;

public static partial class SensitiveTextRedactor
{
    public const int MaximumLength = 500;

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var redacted = SensitiveLine().Replace(
            text,
            match => $"{match.Groups[1].Value} ***已隐藏***");
        const string truncationMarker = "……[截断]";
        return redacted.Length <= MaximumLength
            ? redacted
            : string.Concat(
                redacted.AsSpan(0, MaximumLength - truncationMarker.Length),
                truncationMarker);
    }

    [GeneratedRegex(
        @"(?im)^([^\r\n]*?(?:password|cipher|secret|authentication[- ]?key|community)\b)[^\r\n]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveLine();
}
