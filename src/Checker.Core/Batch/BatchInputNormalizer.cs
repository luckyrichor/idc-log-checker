namespace IDCLogChecker.Core.Batch;

public static class BatchInputNormalizer
{
    public static BatchInputResult Normalize(IEnumerable<string?> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);
        var valid = new List<string>();
        var skipped = new List<SkippedBatchInput>();
        var duplicates = new List<string>();

        foreach (var input in paths)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                skipped.Add(new SkippedBatchInput(input, "路径为空，已跳过。"));
                continue;
            }

            string fullPath;
            try
            {
                fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(input.Trim()));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                skipped.Add(new SkippedBatchInput(input, "路径格式无效，已跳过。"));
                continue;
            }

            if (File.Exists(fullPath))
            {
                skipped.Add(new SkippedBatchInput(input, "该项目是文件，不是文件夹，已跳过。"));
                continue;
            }

            if (!Directory.Exists(fullPath))
            {
                skipped.Add(new SkippedBatchInput(input, "文件夹不存在或无法访问，已跳过。"));
                continue;
            }

            if (!seen.Add(fullPath))
            {
                duplicates.Add(fullPath);
                continue;
            }

            valid.Add(fullPath);
        }

        return new BatchInputResult(valid, skipped, duplicates);
    }
}
