using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Avalonia;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DirectoryScanner _scanner;
    private string _selectedPath = string.Empty;
    private string _statusText = "请选择需要检查的日志文件夹";
    private string _conclusion = "尚未开始检查";
    private string _statusColor = "#60758A";
    private string _directoryCountText = "—";
    private string _txtCountText = "—";
    private string _errorCountText = "0";
    private string _warningCountText = "0";
    private bool _isBusy;
    private bool _hasResult;
    private double _progressPercent;
    private ResultPresentation? _presentation;

    public MainWindowViewModel()
        : this(DirectoryScanner.CreateDefault())
    {
    }

    public MainWindowViewModel(DirectoryScanner scanner)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<IssueRow> VisibleIssues { get; } = [];

    public ScanResult? CurrentResult => _presentation?.Result;

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (SetField(ref _selectedPath, value))
            {
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string Conclusion
    {
        get => _conclusion;
        private set => SetField(ref _conclusion, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        private set => SetField(ref _statusColor, value);
    }

    public string DirectoryCountText
    {
        get => _directoryCountText;
        private set => SetField(ref _directoryCountText, value);
    }

    public string TxtCountText
    {
        get => _txtCountText;
        private set => SetField(ref _txtCountText, value);
    }

    public string ErrorCountText
    {
        get => _errorCountText;
        private set => SetField(ref _errorCountText, value);
    }

    public string WarningCountText
    {
        get => _warningCountText;
        private set => SetField(ref _warningCountText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public bool HasResult
    {
        get => _hasResult;
        private set
        {
            if (SetField(ref _hasResult, value))
            {
                OnPropertyChanged(nameof(CanExport));
            }
        }
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetField(ref _progressPercent, value);
    }

    public bool CanStart => !IsBusy && !string.IsNullOrWhiteSpace(SelectedPath);

    public bool CanExport => HasResult && !IsBusy;

    public async Task RunScanAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart)
        {
            return;
        }

        IsBusy = true;
        HasResult = false;
        VisibleIssues.Clear();
        StatusText = "正在检查，请稍候…";
        Conclusion = "检查进行中";
        StatusColor = "#315A7D";
        ProgressPercent = 0;

        try
        {
            var progress = new Progress<ScanProgress>(item =>
            {
                ProgressPercent = item.TotalDirectories == 0
                    ? 0
                    : item.CompletedDirectories * 100d / item.TotalDirectories;
                StatusText = item.CurrentItem == "检查完成"
                    ? "检查完成"
                    : $"正在检查：{item.CurrentItem}";
            });
            var result = await _scanner.ScanAsync(SelectedPath, progress, cancellationToken);
            _presentation = ResultPresentation.From(result);
            Conclusion = _presentation.Conclusion;
            StatusColor = _presentation.StatusColor;
            DirectoryCountText = $"{result.Summary.ActualDirectoryCount} / {result.Summary.ExpectedDirectoryCount}";
            TxtCountText = $"{result.Summary.ActualTxtFileCount} / {result.Summary.ExpectedTxtFileCount}";
            ErrorCountText = result.Summary.ErrorCount.ToString();
            WarningCountText = result.Summary.WarningCount.ToString();
            ProgressPercent = 100;
            StatusText = "检查完成，可查看明细或导出报告";
            HasResult = true;
            ApplyFilter(IssueFilter.All);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyFilter(IssueFilter filter)
    {
        VisibleIssues.Clear();
        if (_presentation is null)
        {
            return;
        }

        foreach (var row in _presentation.Filter(filter))
        {
            VisibleIssues.Add(row);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
