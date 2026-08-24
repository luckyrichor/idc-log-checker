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
    private string _statusText = "请选择需要检查的日志文件夹";
    private string _conclusion = "尚未开始检查";
    private string _statusColor = "#60758A";
    private string _directoryCountText = "—";
    private string _txtCountText = "—";
    private string _errorCountText = "0";
    private string _warningCountText = "0";
    private string _totalFolderCountText = "0";
    private string _cleanFolderCountText = "0";
    private string _warningFolderCountText = "0";
    private string _failedFolderCountText = "0";
    private bool _isBusy;
    private bool _hasResult;
    private double _progressPercent;

    public MainWindowViewModel() : this(BatchScanCoordinator.CreateDefault()) { }
    public MainWindowViewModel(DirectoryScanner scanner) : this(new BatchScanCoordinator(scanner)) { }
    public MainWindowViewModel(BatchScanCoordinator coordinator) =>
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<FolderResultViewModel> FolderResults { get; } = [];
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
    public string WarningCountText { get => _warningCountText; private set => SetField(ref _warningCountText, value); }
    public string TotalFolderCountText { get => _totalFolderCountText; private set => SetField(ref _totalFolderCountText, value); }
    public string CleanFolderCountText { get => _cleanFolderCountText; private set => SetField(ref _cleanFolderCountText, value); }
    public string WarningFolderCountText { get => _warningFolderCountText; private set => SetField(ref _warningFolderCountText, value); }
    public string FailedFolderCountText { get => _failedFolderCountText; private set => SetField(ref _failedFolderCountText, value); }
    public double ProgressPercent { get => _progressPercent; private set => SetField(ref _progressPercent, value); }

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
        SelectionSummaryText = $"已选择 {_input.ValidPaths.Count} 个文件夹";
        InputNoticeText = string.IsNullOrEmpty(_input.NoticeText)
            ? "已准备好，点击“开始检查”"
            : _input.NoticeText;
        ResetResults();
        OnPropertyChanged(nameof(SelectedPaths));
        OnPropertyChanged(nameof(SelectedPath));
        RaiseCapabilities();
        return true;
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
            WarningCountText = "0";
            VisibleIssues.Clear();
            return;
        }

        var result = _currentPresentation.Result;
        Conclusion = _currentPresentation.Conclusion;
        StatusColor = _currentPresentation.StatusColor;
        DirectoryCountText = $"{result.Summary.ActualDirectoryCount} / {result.Summary.ExpectedDirectoryCount}";
        TxtCountText = $"{result.Summary.ActualTxtFileCount} / {result.Summary.ExpectedTxtFileCount}";
        ErrorCountText = result.Summary.ErrorCount.ToString();
        WarningCountText = result.Summary.WarningCount.ToString();
        ApplyFilter(IssueFilter.All);
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
        WarningFolderCountText = "0";
        FailedFolderCountText = "0";
        DirectoryCountText = "—";
        TxtCountText = "—";
        ErrorCountText = "0";
        WarningCountText = "0";
        Conclusion = "等待开始检查";
        StatusColor = "#60758A";
        StatusText = "已选择文件夹，点击“开始检查”";
        ProgressPercent = 0;
        VisibleIssues.Clear();
    }

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
