using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Avalonia;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly BatchScanCoordinator _coordinator;
    private BatchInputResult _input = new([], [], []);
    private BatchResultPresentation? _batchPresentation;
    private ResultPresentation? _currentPresentation;
    private FolderResultViewModel? _selectedFolder;
    private string _selectionSummaryText = "尚未选择文件夹";
    private string _inputNoticeText = "可选择或拖入一个或多个文件夹";
    private string _statusText = "请选择需要检查的文件夹";
    private string _conclusion = "尚未开始检查";
    private string _statusColor = "#60758A";
    private string _directoryCountText = "—";
    private string _txtCountText = "—";
    private string _errorCountText = "0";
    private string _indeterminateCountText = "0";
    private string _warningCountText = "0";
    private string _contentNormalCountText = "0";
    private string _unsupportedContentRuleCountText = "0";
    private string _totalFolderCountText = "0";
    private string _cleanFolderCountText = "0";
    private string _indeterminateFolderCountText = "0";
    private string _warningFolderCountText = "0";
    private string _failedFolderCountText = "0";
    private bool _isBusy;
    private bool _hasResult;
    private double _progressPercent;
    private InspectionLevel _selectedLevel = InspectionLevel.ExecutionResults;
    private string _levelOneText = "—";
    private string _levelTwoText = "—";
    private string _levelThreeText = "—";
    private string _selectedLevelTitle = "三级执行结果检查";
    private string _levelDetailMessage = "完成检查后显示结果。";
    private string _levelThreeNote = string.Empty;
    private string _levelOneColor = "#60758A";
    private string _levelTwoColor = "#60758A";
    private string _levelThreeColor = "#60758A";

    public MainWindowViewModel() : this(BatchScanCoordinator.CreateDefault()) { }
    public MainWindowViewModel(DirectoryScanner scanner) : this(new BatchScanCoordinator(scanner)) { }
    public MainWindowViewModel(BatchScanCoordinator coordinator) =>
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<FolderResultViewModel> FolderResults { get; } = [];
    public ObservableCollection<SelectedFolderViewModel> SelectedFolders { get; } = [];
    public ObservableCollection<IssueRow> VisibleIssues { get; } = [];
    public IReadOnlyList<string> SelectedPaths => _input.ValidPaths;
    public BatchScanResult? CurrentBatchResult => _batchPresentation?.Result;
    public ScanResult? CurrentResult => _currentPresentation?.Result;

    public string SelectedPath
    {
        get => SelectedPaths.FirstOrDefault() ?? string.Empty;
        set => ReplaceSelection([value]);
    }

    public FolderResultViewModel? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetField(ref _selectedFolder, value))
            {
                PresentSelectedFolder();
                OnPropertyChanged(nameof(CanExportCurrent));
                OnPropertyChanged(nameof(CanExport));
            }
        }
    }

    public string SelectionSummaryText { get => _selectionSummaryText; private set => SetField(ref _selectionSummaryText, value); }
    public string InputNoticeText { get => _inputNoticeText; private set => SetField(ref _inputNoticeText, value); }
    public string StatusText { get => _statusText; private set => SetField(ref _statusText, value); }
    public string Conclusion { get => _conclusion; private set => SetField(ref _conclusion, value); }
    public string StatusColor { get => _statusColor; private set => SetField(ref _statusColor, value); }
    public string DirectoryCountText { get => _directoryCountText; private set => SetField(ref _directoryCountText, value); }
    public string TxtCountText { get => _txtCountText; private set => SetField(ref _txtCountText, value); }
    public string ErrorCountText { get => _errorCountText; private set => SetField(ref _errorCountText, value); }
    public string IndeterminateCountText { get => _indeterminateCountText; private set => SetField(ref _indeterminateCountText, value); }
    public string WarningCountText { get => _warningCountText; private set => SetField(ref _warningCountText, value); }
    public string ContentNormalCountText { get => _contentNormalCountText; private set => SetField(ref _contentNormalCountText, value); }
    public string UnsupportedContentRuleCountText { get => _unsupportedContentRuleCountText; private set => SetField(ref _unsupportedContentRuleCountText, value); }
    public string TotalFolderCountText { get => _totalFolderCountText; private set => SetField(ref _totalFolderCountText, value); }
    public string CleanFolderCountText { get => _cleanFolderCountText; private set => SetField(ref _cleanFolderCountText, value); }
    public string IndeterminateFolderCountText { get => _indeterminateFolderCountText; private set => SetField(ref _indeterminateFolderCountText, value); }
    public string WarningFolderCountText { get => _warningFolderCountText; private set => SetField(ref _warningFolderCountText, value); }
    public string FailedFolderCountText { get => _failedFolderCountText; private set => SetField(ref _failedFolderCountText, value); }
    public double ProgressPercent { get => _progressPercent; private set => SetField(ref _progressPercent, value); }
    public string LevelOneText { get => _levelOneText; private set => SetField(ref _levelOneText, value); }
    public string LevelTwoText { get => _levelTwoText; private set => SetField(ref _levelTwoText, value); }
    public string LevelThreeText { get => _levelThreeText; private set => SetField(ref _levelThreeText, value); }
    public string SelectedLevelTitle { get => _selectedLevelTitle; private set => SetField(ref _selectedLevelTitle, value); }
    public string LevelDetailMessage { get => _levelDetailMessage; private set => SetField(ref _levelDetailMessage, value); }
    public string LevelThreeNote { get => _levelThreeNote; private set => SetField(ref _levelThreeNote, value); }
    public string LevelOneColor { get => _levelOneColor; private set => SetField(ref _levelOneColor, value); }
    public string LevelTwoColor { get => _levelTwoColor; private set => SetField(ref _levelTwoColor, value); }
    public string LevelThreeColor { get => _levelThreeColor; private set => SetField(ref _levelThreeColor, value); }

    public bool IsBusy
    {
        get => _isBusy;
        private set { if (SetField(ref _isBusy, value)) RaiseCapabilities(); }
    }

    public bool HasResult
    {
        get => _hasResult;
        private set { if (SetField(ref _hasResult, value)) RaiseCapabilities(); }
    }

    public bool CanStart => !IsBusy && _input.HasValidPaths;
    public bool HasSelection => _input.HasValidPaths;
    public bool IsHome => !_input.HasValidPaths;
    public bool HasVisibleIssues => VisibleIssues.Count > 0;
    public bool HasNoVisibleIssues => !HasVisibleIssues;
    public bool CanExportAll => HasResult && !IsBusy;
    public bool CanExportCurrent => CanExportAll && SelectedFolder is not null;
    public bool CanExport => CanExportCurrent;

    public bool ReplaceSelection(IEnumerable<string?> paths)
    {
        if (IsBusy) return false;
        var normalized = BatchInputNormalizer.Normalize(paths);
        if (!normalized.HasValidPaths)
        {
            InputNoticeText = BuildRejectedNotice(normalized);
            return false;
        }

        _input = normalized;
        RefreshSelectedFolders();
        SelectionSummaryText = $"已选择 {_input.ValidPaths.Count} 个文件夹";
        InputNoticeText = string.IsNullOrEmpty(_input.NoticeText)
            ? "已准备好，点击“开始检查”开始"
            : _input.NoticeText;
        ResetResults();
        OnPropertyChanged(nameof(SelectedPaths));
        OnPropertyChanged(nameof(SelectedPath));
        RaiseCapabilities();
        return true;
    }

    public bool AddSelection(IEnumerable<string?> paths)
    {
        if (IsBusy) return false;
        var additions = BatchInputNormalizer.Normalize(paths);
        if (!additions.HasValidPaths)
        {
            InputNoticeText = BuildRejectedNotice(additions);
            return false;
        }

        var combined = BatchInputNormalizer.Normalize(_input.ValidPaths.Concat(additions.ValidPaths));
        _input = new BatchInputResult(combined.ValidPaths, additions.SkippedItems, combined.DuplicatePaths.Concat(additions.DuplicatePaths).ToArray());
        RefreshSelectedFolders();
        SelectionSummaryText = $"已选择 {_input.ValidPaths.Count} 个文件夹";
        InputNoticeText = string.IsNullOrEmpty(additions.NoticeText) ? "文件夹已添加" : additions.NoticeText;
        ResetResults();
        OnPropertyChanged(nameof(SelectedPaths));
        RaiseCapabilities();
        return true;
    }

    public bool RemoveSelection(string path)
    {
        if (IsBusy) return false;
        var remaining = _input.ValidPaths.Where(item => !PathEquals(item, path)).ToArray();
        if (remaining.Length == _input.ValidPaths.Count) return false;
        _input = new BatchInputResult(remaining, [], []);
        RefreshSelectedFolders();
        SelectionSummaryText = remaining.Length == 0 ? "尚未选择文件夹" : $"已选择 {remaining.Length} 个文件夹";
        InputNoticeText = remaining.Length == 0 ? "可选择或拖入一个或多个文件夹" : "已移除所选文件夹";
        ResetResults();
        OnPropertyChanged(nameof(SelectedPaths));
        RaiseCapabilities();
        return true;
    }

    public void ClearSelection()
    {
        if (IsBusy) return;
        _input = new BatchInputResult([], [], []);
        SelectedFolders.Clear();
        SelectionSummaryText = "尚未选择文件夹";
        InputNoticeText = "可选择或拖入一个或多个文件夹";
        ResetResults();
        StatusText = "请选择需要检查的文件夹";
        OnPropertyChanged(nameof(SelectedPaths));
        RaiseCapabilities();
    }

    public Task RunScanAsync(CancellationToken cancellationToken = default) => RunBatchScanAsync(cancellationToken);

    public async Task RunBatchScanAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart) return;
        IsBusy = true;
        HasResult = false;
        FolderResults.Clear();
        SelectedFolder = null;
        VisibleIssues.Clear();
        StatusText = "正在检查，请稍候…";
        Conclusion = "批量检查进行中";
        StatusColor = "#315A7D";
        ProgressPercent = 0;

        try
        {
            var progress = new Progress<BatchScanProgress>(item =>
            {
                var directoryFraction = item.DirectoryProgress is { TotalDirectories: > 0 } inner
                    ? inner.CompletedDirectories / (double)inner.TotalDirectories
                    : 0;
                ProgressPercent = item.TotalFolders == 0
                    ? 0
                    : Math.Min(100, (item.CompletedFolders + directoryFraction) * 100d / item.TotalFolders);
                StatusText = item.CompletedFolders >= item.TotalFolders
                    ? "全部文件夹检查完成"
                    : $"正在检查第 {item.FolderIndex}/{item.TotalFolders} 个：{Path.GetFileName(item.FolderPath)}";
            });
            var result = await _coordinator.ScanAsync(_input, progress, cancellationToken);
            _batchPresentation = BatchResultPresentation.From(result);
            foreach (var folder in _batchPresentation.Folders)
                FolderResults.Add(new FolderResultViewModel(folder));

            var summary = result.Summary;
            TotalFolderCountText = summary.TotalCount.ToString();
            CleanFolderCountText = summary.CleanCount.ToString();
            IndeterminateFolderCountText = summary.IndeterminateCount.ToString();
            WarningFolderCountText = summary.WarningCount.ToString();
            FailedFolderCountText = summary.FailedCount.ToString();
            ProgressPercent = 100;
            StatusText = "全部文件夹检查完成，可选择左侧文件夹查看明细";
            HasResult = true;
            if (_batchPresentation.DefaultSelectedIndex >= 0)
                SelectedFolder = FolderResults[_batchPresentation.DefaultSelectedIndex];
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyFilter(IssueFilter filter)
    {
        VisibleIssues.Clear();
        if (_currentPresentation is null) return;
        foreach (var row in _currentPresentation.Filter(filter)) VisibleIssues.Add(row);
    }

    public void SelectLevel(InspectionLevel level)
    {
        _selectedLevel = level;
        VisibleIssues.Clear();
        if (_currentPresentation is null)
        {
            SelectedLevelTitle = level switch
            {
                InspectionLevel.DeviceDirectories => "一级设备目录检查",
                InspectionLevel.CommandFiles => "二级命令数目检查",
                _ => "三级执行结果检查",
            };
            LevelDetailMessage = "完成检查后显示结果。";
            return;
        }

        var summary = _currentPresentation.LevelSummary(level);
        SelectedLevelTitle = summary.Title;
        LevelDetailMessage = summary.DetailMessage;
        foreach (var row in _currentPresentation.ErrorsFor(level)) VisibleIssues.Add(row);
        OnPropertyChanged(nameof(HasVisibleIssues));
        OnPropertyChanged(nameof(HasNoVisibleIssues));
    }

    private void PresentSelectedFolder()
    {
        _currentPresentation = SelectedFolder?.Presentation.Detail;
        if (_currentPresentation is null)
        {
            Conclusion = HasResult ? "请选择左侧文件夹" : "尚未开始检查";
            StatusColor = "#60758A";
            DirectoryCountText = "—";
            TxtCountText = "—";
            ErrorCountText = "0";
            IndeterminateCountText = "0";
            WarningCountText = "0";
            ContentNormalCountText = "0";
            UnsupportedContentRuleCountText = "0";
            VisibleIssues.Clear();
            return;
        }

        var result = _currentPresentation.Result;
        Conclusion = _currentPresentation.Conclusion;
        StatusColor = _currentPresentation.StatusColor;
        DirectoryCountText = $"{result.Summary.ActualDirectoryCount} / {result.Summary.ExpectedDirectoryCount}";
        TxtCountText = $"{result.Summary.ActualTxtFileCount} / {result.Summary.ExpectedTxtFileCount}";
        ErrorCountText = result.Summary.ErrorCount.ToString();
        IndeterminateCountText = result.Summary.IndeterminateCount.ToString();
        WarningCountText = result.Summary.WarningCount.ToString();
        ContentNormalCountText = result.Summary.ContentNormalCount.ToString();
        UnsupportedContentRuleCountText = result.Summary.UnsupportedContentRuleCount.ToString();
        var levelOne = _currentPresentation.LevelSummary(InspectionLevel.DeviceDirectories);
        var levelTwo = _currentPresentation.LevelSummary(InspectionLevel.CommandFiles);
        var levelThree = _currentPresentation.LevelSummary(InspectionLevel.ExecutionResults);
        LevelOneText = levelOne.CardText;
        LevelTwoText = levelTwo.CardText;
        LevelThreeText = levelThree.CardText;
        LevelThreeNote = levelThree.DetailMessage;
        LevelOneColor = levelOne.ErrorCount > 0 ? "#B94339" : "#16715F";
        LevelTwoColor = levelTwo.ErrorCount > 0 ? "#B94339" : "#16715F";
        LevelThreeColor = levelThree.ErrorCount > 0 ? "#B94339" : "#16715F";
        SelectLevel(_selectedLevel);
    }

    private void ResetResults()
    {
        _batchPresentation = null;
        _currentPresentation = null;
        FolderResults.Clear();
        SelectedFolder = null;
        HasResult = false;
        TotalFolderCountText = _input.ValidPaths.Count.ToString();
        CleanFolderCountText = "0";
        IndeterminateFolderCountText = "0";
        WarningFolderCountText = "0";
        FailedFolderCountText = "0";
        DirectoryCountText = "—";
        TxtCountText = "—";
        ErrorCountText = "0";
        IndeterminateCountText = "0";
        WarningCountText = "0";
        ContentNormalCountText = "0";
        UnsupportedContentRuleCountText = "0";
        Conclusion = "等待开始检查";
        StatusColor = "#60758A";
        StatusText = "已选择文件夹，点击“开始检查”开始";
        ProgressPercent = 0;
        VisibleIssues.Clear();
        LevelOneText = LevelTwoText = LevelThreeText = "—";
        SelectedLevelTitle = "三级执行结果检查";
        LevelDetailMessage = "完成检查后显示结果。";
        LevelThreeNote = string.Empty;
        LevelOneColor = LevelTwoColor = LevelThreeColor = "#60758A";
    }

    private void RefreshSelectedFolders()
    {
        SelectedFolders.Clear();
        foreach (var path in _input.ValidPaths) SelectedFolders.Add(new SelectedFolderViewModel(path));
    }

    private static bool PathEquals(string left, string right) => string.Equals(
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static string BuildRejectedNotice(BatchInputResult result)
    {
        var detail = result.SkippedItems.FirstOrDefault()?.Reason;
        return string.IsNullOrEmpty(detail)
            ? "没有找到可检查的文件夹，原选择保持不变"
            : $"没有找到可检查的文件夹，原选择保持不变：{detail}";
    }

    private void RaiseCapabilities()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(CanExportAll));
        OnPropertyChanged(nameof(CanExportCurrent));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsHome));
        OnPropertyChanged(nameof(HasVisibleIssues));
        OnPropertyChanged(nameof(HasNoVisibleIssues));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
