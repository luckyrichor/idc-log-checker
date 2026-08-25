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
        UpdateLevelButtons(InspectionLevel.ExecutionResults);
    }

    private async void OnChooseFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择一个或多个巡检结果文件夹",
            AllowMultiple = true,
        });
        if (folders.Count > 0 && !AcceptSelection(folders.Select(folder => folder.Path.LocalPath), false))
            await ShowMessageAsync("没有有效文件夹", _viewModel.InputNoticeText);
    }

    private async void OnAddFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "添加一个或多个检查文件夹",
            AllowMultiple = true,
        });
        if (folders.Count > 0 && !AcceptSelection(folders.Select(folder => folder.Path.LocalPath), true))
            await ShowMessageAsync("没有有效文件夹", _viewModel.InputNoticeText);
    }

    private async void OnStartScanClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RunBatchScanAsync();
            UpdateLevelButtons(InspectionLevel.ExecutionResults);
            if (_viewModel.SelectedFolder is not null)
                SelectedFolderList.SelectedItem = _viewModel.SelectedFolders.FirstOrDefault(folder => folder.Path == _viewModel.SelectedFolder.Path);
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

        if (!AcceptSelection(items.Select(item => item.Path.LocalPath), _viewModel.HasSelection))
            await ShowMessageAsync("没有有效文件夹", _viewModel.InputNoticeText);
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var acceptsFiles = e.DataTransfer.Formats.Contains(DataFormat.File);
        e.DragEffects = acceptsFiles ? DragDropEffects.Copy : DragDropEffects.None;
        if (_viewModel.IsHome)
        {
            HomeDropZone.BorderBrush = new SolidColorBrush(Color.Parse(acceptsFiles ? "#2C90C7" : "#C0392B"));
            HomeDropZone.Background = new SolidColorBrush(Color.Parse(acceptsFiles ? "#EAF6FC" : "#FDEDEC"));
        }
    }

    private void ResetDropZone()
    {
        HomeDropZone.BorderBrush = new SolidColorBrush(Color.Parse("#91A6B8"));
        HomeDropZone.Background = new SolidColorBrush(Color.Parse("#F7FAFC"));
    }

    private bool AcceptSelection(IEnumerable<string?> paths, bool append)
    {
        var accepted = append ? _viewModel.AddSelection(paths) : _viewModel.ReplaceSelection(paths);
        _selectedIssue = null;
        return accepted;
    }

    private void OnSelectedPathChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedIssue = null;
        if (SelectedFolderList.SelectedItem is not SelectedFolderViewModel selected) return;
        _viewModel.SelectedFolder = _viewModel.FolderResults.FirstOrDefault(folder => folder.Path == selected.Path);
    }

    private void OnLevelOneClick(object? sender, RoutedEventArgs e) => SelectLevel(InspectionLevel.DeviceDirectories);
    private void OnLevelTwoClick(object? sender, RoutedEventArgs e) => SelectLevel(InspectionLevel.CommandFiles);
    private void OnLevelThreeClick(object? sender, RoutedEventArgs e) => SelectLevel(InspectionLevel.ExecutionResults);

    private void SelectLevel(InspectionLevel level)
    {
        _viewModel.SelectLevel(level);
        UpdateLevelButtons(level);
    }

    private void UpdateLevelButtons(InspectionLevel selected)
    {
        var buttons = new[]
        {
            (LevelOneButton, InspectionLevel.DeviceDirectories),
            (LevelTwoButton, InspectionLevel.CommandFiles),
            (LevelThreeButton, InspectionLevel.ExecutionResults),
        };
        foreach (var (button, level) in buttons)
        {
            var active = level == selected;
            button.BorderBrush = new SolidColorBrush(Color.Parse(active ? "#19735F" : "#CAD6DE"));
            button.BorderThickness = new global::Avalonia.Thickness(active ? 2 : 1);
            button.Background = new SolidColorBrush(Color.Parse(active ? "#F7FCFA" : "#FFFFFF"));
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _selectedIssue = null;
        _viewModel.ClearSelection();
    }

    private void OnRemoveFolderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SelectedFolderViewModel folder })
            _viewModel.RemoveSelection(folder.Path);
    }

    private void OnSelectedFolderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control
            || e.GetCurrentPoint(control).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed
            || control.ContextMenu is not { } menu) return;
        menu.Open(control);
        e.Handled = true;
    }

    private async void OnCopyFolderNameClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is SelectedFolderViewModel folder)
            await CopyTextAsync(folder.FolderName);
    }

    private async void OnCopyFolderPathClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is SelectedFolderViewModel folder)
            await CopyTextAsync(folder.Path);
    }

    private async Task CopyTextAsync(string text)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(text);
    }

    private void OnIssueSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedIssue = IssueList.SelectedItem as IssueRow;
    }

    private async void OnCopyIssueMenuTextClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.Tag is string text && !string.IsNullOrEmpty(text))
            await CopyTextAsync(text);
    }

    private async void OnRowCopyClick(object? sender, RoutedEventArgs e)
    {
        _selectedIssue = (sender as Control)?.DataContext as IssueRow ?? _selectedIssue;
        if (_selectedIssue is null) return;
        await CopyTextAsync(_selectedIssue.DetailText);
    }

    private async void OnRowOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        _selectedIssue = (sender as Control)?.DataContext as IssueRow ?? _selectedIssue;
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

    private async void OnRowDetailsClick(object? sender, RoutedEventArgs e)
    {
        _selectedIssue = (sender as Control)?.DataContext as IssueRow ?? _selectedIssue;
        if (_selectedIssue is null) return;
        await ShowDetailsAsync(_selectedIssue.DetailText);
    }

    private async Task ShowDetailsAsync(string details)
    {
        var closeButton = new Button { Content = "关闭", HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right, Padding = new global::Avalonia.Thickness(20, 8) };
        var dialog = new Window
        {
            Title = "错误详情", Width = 720, Height = 480, MinWidth = 520, MinHeight = 340,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                Margin = new global::Avalonia.Thickness(20), RowDefinitions = new RowDefinitions("*,Auto"),
                Children =
                {
                    new ScrollViewer { HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, Content = new SelectableTextBlock { Text = details, TextWrapping = TextWrapping.NoWrap } },
                    closeButton,
                },
            },
        };
        Grid.SetRow(closeButton, 1);
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
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
            "导出检查数据",
            $"设备检查数据_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
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
