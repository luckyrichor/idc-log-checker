using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.WinForms;

public sealed class BatchFormController
{
    private readonly BatchScanCoordinator _coordinator;
    private BatchInputResult _input = new([], [], []);
    private BatchResultPresentation? _presentation;

    public BatchFormController(BatchScanCoordinator coordinator) =>
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

    public IReadOnlyList<string> SelectedPaths => _input.ValidPaths;
    public IReadOnlyList<FolderResultPresentation> FolderRows => _presentation?.Folders ?? [];
    public int SelectedFolderIndex { get; private set; } = -1;
    public FolderResultPresentation? CurrentFolder =>
        SelectedFolderIndex >= 0 && SelectedFolderIndex < FolderRows.Count
            ? FolderRows[SelectedFolderIndex]
            : null;
    public BatchScanResult? BatchResult => _presentation?.Result;
    public ScanResult? CurrentResult => CurrentFolder?.Detail.Result;
    public BatchScanSummary? Summary => BatchResult?.Summary;
    public string SelectionSummaryText { get; private set; } = "尚未选择文件夹";
    public string InputNoticeText { get; private set; } = "可选择或拖入一个或多个文件夹";
    public bool CanStart => _input.HasValidPaths;
    public bool CanExportAll => BatchResult is not null;
    public bool CanExportCurrent => CurrentFolder is not null;

    public bool ReplaceSelection(IEnumerable<string?> paths)
    {
        var normalized = BatchInputNormalizer.Normalize(paths);
        if (!normalized.HasValidPaths)
        {
            InputNoticeText = "没有找到可检查的文件夹，原选择保持不变";
            if (normalized.SkippedItems.Count > 0)
                InputNoticeText += $"：{normalized.SkippedItems[0].Reason}";
            return false;
        }

        _input = normalized;
        _presentation = null;
        SelectedFolderIndex = -1;
        SelectionSummaryText = $"已选择 {_input.ValidPaths.Count} 个文件夹";
        InputNoticeText = string.IsNullOrEmpty(_input.NoticeText)
            ? "已准备好，点击“开始检查”开始"
            : _input.NoticeText;
        return true;
    }

    public bool AddSelection(IEnumerable<string?> paths)
    {
        var additions = BatchInputNormalizer.Normalize(paths);
        if (!additions.HasValidPaths)
        {
            InputNoticeText = "没有找到可添加的文件夹，原选择保持不变";
            if (additions.SkippedItems.Count > 0) InputNoticeText += $"：{additions.SkippedItems[0].Reason}";
            return false;
        }

        var combined = BatchInputNormalizer.Normalize(_input.ValidPaths.Concat(additions.ValidPaths));
        _input = new BatchInputResult(combined.ValidPaths, additions.SkippedItems,
            combined.DuplicatePaths.Concat(additions.DuplicatePaths).ToArray());
        ResetPresentation();
        SelectionSummaryText = $"已选择 {_input.ValidPaths.Count} 个文件夹";
        InputNoticeText = string.IsNullOrEmpty(additions.NoticeText) ? "文件夹已添加" : additions.NoticeText;
        return true;
    }

    public bool RemoveSelection(string path)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var remaining = _input.ValidPaths.Where(item => !string.Equals(item, path, comparison)).ToArray();
        if (remaining.Length == _input.ValidPaths.Count) return false;
        _input = new BatchInputResult(remaining, [], []);
        ResetPresentation();
        SelectionSummaryText = remaining.Length == 0 ? "尚未选择文件夹" : $"已选择 {remaining.Length} 个文件夹";
        InputNoticeText = remaining.Length == 0 ? "可选择或拖入一个或多个文件夹" : "已移除所选文件夹";
        return true;
    }

    public void ClearSelection()
    {
        _input = new BatchInputResult([], [], []);
        ResetPresentation();
        SelectionSummaryText = "尚未选择文件夹";
        InputNoticeText = "可选择或拖入一个或多个文件夹";
    }

    private void ResetPresentation()
    {
        _presentation = null;
        SelectedFolderIndex = -1;
    }

    public async Task<BatchScanResult> RunAsync(
        IProgress<BatchScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanStart) throw new InvalidOperationException("尚未选择可检查的文件夹。");
        var result = await _coordinator.ScanAsync(_input, progress, cancellationToken);
        _presentation = BatchResultPresentation.From(result);
        SelectedFolderIndex = _presentation.DefaultSelectedIndex;
        return result;
    }

    public bool SelectFolder(int index)
    {
        if (index < 0 || index >= FolderRows.Count) return false;
        SelectedFolderIndex = index;
        return true;
    }

    public IReadOnlyList<IssueListRow> BuildIssueRows(IssueFilter filter) =>
        CurrentFolder is null
            ? []
            : IssueListAdapter.BuildRows(CurrentFolder.Detail, filter);

    public LevelResultSummary GetLevelSummary(InspectionLevel level) =>
        CurrentFolder?.Detail.LevelSummary(level)
        ?? new LevelResultSummary(level, string.Empty, 0, "—", "完成检查后显示结果。");

    public IReadOnlyList<IssueListRow> BuildIssueRows(InspectionLevel level) =>
        CurrentFolder is null
            ? []
            : IssueListAdapter.BuildRows(CurrentFolder.Detail.ErrorsFor(level));

    public IReadOnlyList<IssueCategorySummary> GetErrorCategories(InspectionLevel level) =>
        CurrentFolder?.Detail.ErrorCategoriesFor(level) ?? [];

    public int GetErrorCount(InspectionLevel level) =>
        CurrentFolder?.Detail.ErrorsFor(level).Count ?? 0;
}
