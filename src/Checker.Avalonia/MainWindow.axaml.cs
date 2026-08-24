using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
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
            Title = "选择包含 62 个设备目录的巡检结果文件夹",
            AllowMultiple = false,
        });
        if (folders.Count == 1)
        {
            _viewModel.SelectedPath = folders[0].Path.LocalPath;
        }
    }

    private async void OnStartScanClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RunScanAsync();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync("检查未完成", $"程序无法完成检查：\n{exception.Message}");
        }
    }

    private void OnShowAllClick(object? sender, RoutedEventArgs e) =>
        _viewModel.ApplyFilter(IssueFilter.All);

    private void OnShowErrorsClick(object? sender, RoutedEventArgs e) =>
        _viewModel.ApplyFilter(IssueFilter.Errors);

    private void OnShowWarningsClick(object? sender, RoutedEventArgs e) =>
        _viewModel.ApplyFilter(IssueFilter.Warnings);

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
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(_selectedIssue.DetailText);
        }
    }

    private async void OnOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedIssue is null || string.IsNullOrWhiteSpace(_selectedIssue.Path))
        {
            await ShowMessageAsync("无法打开位置", "请先选择一条包含文件或目录位置的记录。");
            return;
        }

        try
        {
            var path = _selectedIssue.Path;
            Process.Start(OperatingSystem.IsWindows()
                ? new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }
                : new ProcessStartInfo("open", $"-R \"{path}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            await ShowMessageAsync("无法打开位置", exception.Message);
        }
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentResult is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出检查报告",
            SuggestedFileName = $"IDC日志检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            DefaultExtension = "txt",
            FileTypeChoices =
            [
                new FilePickerFileType("文本文件") { Patterns = ["*.txt"] },
            ],
        });
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(ChineseTextReportWriter.Write(_viewModel.CurrentResult));
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
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(22),
                Spacing = 18,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
                    closeButton,
                },
            },
        };
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }
}
