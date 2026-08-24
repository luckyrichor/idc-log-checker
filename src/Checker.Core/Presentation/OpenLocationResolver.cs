namespace IDCLogChecker.Core.Presentation;

public sealed record OpenLocationTarget(string Path, bool SelectFile);

public static class OpenLocationResolver
{
    public static OpenLocationTarget? Resolve(string? issuePath)
    {
        if (string.IsNullOrWhiteSpace(issuePath)) return null;
        if (File.Exists(issuePath)) return new OpenLocationTarget(issuePath, true);
        if (Directory.Exists(issuePath)) return new OpenLocationTarget(issuePath, false);

        for (var current = System.IO.Path.GetDirectoryName(issuePath);
             !string.IsNullOrWhiteSpace(current);
             current = System.IO.Path.GetDirectoryName(current))
        {
            if (Directory.Exists(current)) return new OpenLocationTarget(current, false);
        }

        return null;
    }
}
