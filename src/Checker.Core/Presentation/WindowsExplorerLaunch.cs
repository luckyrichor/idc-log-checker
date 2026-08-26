namespace IDCLogChecker.Core.Presentation;

public sealed record WindowsExplorerLaunch(string FileName, string Arguments)
{
    public static WindowsExplorerLaunch Build(OpenLocationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var quotedPath = $"\"{target.Path.Replace("\"", "\\\"")}\"";
        return target.SelectFile
            ? new("explorer.exe", $"/select,{quotedPath}")
            : new("explorer.exe", quotedPath);
    }
}
