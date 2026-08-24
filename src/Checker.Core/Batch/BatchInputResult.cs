namespace IDCLogChecker.Core.Batch;

public sealed record SkippedBatchInput(string? Input, string Reason);

public sealed record BatchInputResult(
    IReadOnlyList<string> ValidPaths,
    IReadOnlyList<SkippedBatchInput> SkippedItems,
    IReadOnlyList<string> DuplicatePaths)
{
    public bool HasValidPaths => ValidPaths.Count > 0;

    public string NoticeText
    {
        get
        {
            var parts = new List<string>();
            if (SkippedItems.Count > 0) parts.Add($"{SkippedItems.Count} 个项目不是有效文件夹，已跳过");
            if (DuplicatePaths.Count > 0) parts.Add($"{DuplicatePaths.Count} 个重复文件夹已自动去重");
            return string.Join("；", parts);
        }
    }
}
