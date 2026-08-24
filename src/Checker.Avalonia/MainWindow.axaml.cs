using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Reporting;

namespace IDCLogChecker.Avalonia;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private IssueRow? _selectedIssue;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void OnChooseFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择一个或多个巡检结果文件夹",
            AllowMultiple = true,
        });
        if (folders.Count > 0 && !AcceptSelection(folders.Select(folder => folder.Path.LocalPath)))
            await ShowMessageAsync("没有有效文件夹", _viewModel.InputNoticeText);
    }

    private async void OnStartScanClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RunBatchScanAsync();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync("检查未完成", $"程序无法完成检查：\n{exception.Message}");
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e) => UpdateDragState(e);
    private void OnDragOver(object? sender, DragEventArgs e) => UpdateDragState(e);

    private void OnDragLeave(object? sender, DragEventArgs e) => ResetDropZone();

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        ResetDropZone();
        var items = e.DataTransfer.TryGetFiles();
        if (items is null)
        {
            await ShowMessageAsync("无法识别拖入内容", "请拖入一个或多个文件夹。");
            return;
        }

        if (!AcceptSelection(items.Select(item => item.Path.LocalPath)))
            await ShowMessageAsync("没有有效文件夹", _viewModel.InputNoticeText);
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var acceptsFiles = e.DataTransfer.Formats.Contains(DataFormat.File);
        e.DragEffects = acceptsFiles ? DragDropEffects.Copy : DragDropEffects.None;
        DropZone.BorderBrush = new SolidColorBrush(Color.Parse(acceptsFiles ? "#2C90C7" : "#C0392B"));
        DropZone.Background = new SolidColorBrush(Color.Parse(acceptsFiles ? "#EAF6FC" : "#FDEDEC"));
    }

    private void ResetDropZone()
    {
        DropZone.BorderBrush = new SolidColorBrush(Color.Parse("#D7E0E8"));
        DropZone.Background = Brushes.White;
    }

    private bool AcceptSelection(IEnumerable<string?> paths)
    {
        var accepted = _viewModel.ReplaceSelection(paths);
        _selectedIssue = null;
        IssueDetail.Text = "选择一条明细，可在这里查看完整说明。";
        return accepted;
    }

    private void OnFolderSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedIssue = null;
        IssueDetail.Text = "选择一条明细，可在这里查看完整说明。";
    }

    private void OnShowAllClick(object? sender, RoutedEventArgs e) => _viewModel.ApplyFilter(IssueFilter.All);
    private void OnShowErrorsClick(object? sender, RoutedEventArgs e) => _viewModel.ApplyFilter(IssueFilter.Errors);
    private void OnShowIndeterminateClick(object? sender, RoutedEventArgs e) => _viewModel.ApplyFilter(IssueFilter.Indeterminate);
    private void OnShowWarningsClick(object? sender, RoutedEventArgs e) => _viewModel.ApplyFilter(IssueFilter.Warnings);

    private void OnIssueSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedIssue = IssueList.SelectedItem as IssueRow;
        IssueDetail.Text = _selectedIssue?.DetailText ?? "选择一条明细，可在这里查看完整说明。";
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedIssue is null)
        {
            await ShowMessageAsync("尚未选择明细", "请先在检查明细中选择一条记录。");
            return;
        }

        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(_selectedIssue.DetailText);
    }

    private async void OnOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        var target = OpenLocationResolver.Resolve(_selectedIssue?.Path);
        if (target is null)
        {
            await ShowMessageAsync("无法打开位置", "没有找到可打开的位置。请确认原检查文件夹仍然存在。");
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo(OperatingSystem.IsWindows() ? "explorer.exe" : "open")
            {
                UseShellExecute = false,
            };
            if (OperatingSystem.IsWindows() && target.SelectFile)
            {
                startInfo.ArgumentList.Add($"/select,{target.Path}");
            }
            else
            {
                if (!OperatingSystem.IsWindows() && target.SelectFile) startInfo.ArgumentList.Add("-R");
                startInfo.ArgumentList.Add(target.Path);
            }
            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            await ShowMessageAsync("无法打开位置", exception.Message);
        }
    }

    private async void OnExportCurrentClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentResult is null) return;
        await SaveReportAsync(
            "导出当前文件夹报告",
            $"IDC日志检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            ChineseTextReportWriter.Write(_viewModel.CurrentResult));
    }

    private async void OnExportAllClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentBatchResult is null) return;
        await SaveReportAsync(
            "导出全部检查结果",
            $"IDC日志批量检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            ChineseBatchReportWriter.Write(_viewModel.CurrentBatchResult));
    }

    private async Task SaveReportAsync(string title, string suggestedName, string content)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = "txt",
            FileTypeChoices = [new FilePickerFileType("文本文件") { Patterns = ["*.txt"] }],
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content);
        await ShowMessageAsync("报告已导出", $"检查报告已保存到：\n{file.Path.LocalPath}");
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var closeButton = new Button
        {
            Content = "知道了",
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new global::Avalonia.Thickness(20, 8),
        };
        var dialog = new Window
        {
            Title = title,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(22),
                Spacing = 18,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    closeButton,
                },
            },
        };
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }
}
