using System.Diagnostics;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Reporting;

namespace IDCLogChecker.WinForms;

public partial class MainForm : Form
{
    private readonly BatchFormController _controller = new(BatchScanCoordinator.CreateDefault());
    private IReadOnlyList<IssueListRow> _visibleRows = [];

    public MainForm()
    {
        InitializeComponent();
        ApplyIdleState();
    }

    private void ChooseFolderButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var paths = NativeMultiFolderPicker.Show(Handle);
            if (paths.Count > 0) AcceptSelection(paths);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"无法打开文件夹选择窗口：\r\n{exception.Message}", "选择失败",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e) => UpdateDragState(e);
    private void MainForm_DragOver(object? sender, DragEventArgs e) => UpdateDragState(e);

    private void MainForm_DragLeave(object? sender, EventArgs e)
    {
        inputPanel.BackColor = Color.White;
        inputPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
    }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        MainForm_DragLeave(sender, EventArgs.Empty);
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AcceptSelection(paths);
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var accepted = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        e.Effect = accepted ? DragDropEffects.Copy : DragDropEffects.None;
        inputPanel.BackColor = accepted ? Color.FromArgb(234, 246, 252) : Color.FromArgb(253, 237, 236);
        inputPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
    }

    private void AcceptSelection(IEnumerable<string?> paths)
    {
        if (!_controller.ReplaceSelection(paths))
        {
            MessageBox.Show(this, _controller.InputNoticeText, "没有有效文件夹",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        selectionSummaryLabel.Text = _controller.SelectionSummaryText;
        inputNoticeLabel.Text = _controller.InputNoticeText;
        totalValueLabel.Text = _controller.SelectedPaths.Count.ToString();
        cleanValueLabel.Text = "0";
        batchWarningValueLabel.Text = "0";
        failedValueLabel.Text = "0";
        folderGrid.Rows.Clear();
        issueGrid.Rows.Clear();
        ApplyCurrentFolder(null);
        exportAllButton.Enabled = false;
        exportCurrentButton.Enabled = false;
        startButton.Enabled = true;
        statusLabel.Text = "已选择文件夹，点击“开始检查”";
        progressBar.Value = 0;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (!_controller.CanStart)
        {
            MessageBox.Show(this, "请先选择需要检查的文件夹。", "尚未选择文件夹",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            var progress = new Progress<BatchScanProgress>(item =>
            {
                var inner = item.DirectoryProgress is { TotalDirectories: > 0 } directory
                    ? directory.CompletedDirectories / (double)directory.TotalDirectories
                    : 0;
                progressBar.Value = Math.Clamp(
                    (int)Math.Round((item.CompletedFolders + inner) * 100 / item.TotalFolders), 0, 100);
                statusLabel.Text = item.CompletedFolders >= item.TotalFolders
                    ? "全部文件夹检查完成"
                    : $"正在检查第 {item.FolderIndex}/{item.TotalFolders} 个：{Path.GetFileName(item.FolderPath)}";
            });
            await _controller.RunAsync(progress);
            RenderBatchResult();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"程序无法完成批量检查：\r\n{exception.Message}", "检查未完成",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "检查未完成，请查看说明后重试";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RenderBatchResult()
    {
        var summary = _controller.Summary!;
        totalValueLabel.Text = summary.TotalCount.ToString();
        cleanValueLabel.Text = summary.CleanCount.ToString();
        batchWarningValueLabel.Text = summary.WarningCount.ToString();
        failedValueLabel.Text = summary.FailedCount.ToString();
        folderGrid.Rows.Clear();
        foreach (var folder in _controller.FolderRows)
        {
            var rowIndex = folderGrid.Rows.Add(
                folder.FolderName,
                folder.StatusText,
                folder.ErrorCount,
                folder.WarningCount);
            var row = folderGrid.Rows[rowIndex];
            row.Tag = folder.Path;
            row.Cells[0].ToolTipText = folder.Path;
            row.Cells[1].Style.ForeColor = ColorTranslator.FromHtml(folder.StatusColor);
            row.Cells[1].Style.Font = new Font(folderGrid.Font, FontStyle.Bold);
        }

        progressBar.Value = 100;
        statusLabel.Text = "全部文件夹检查完成，可在左侧切换查看结果";
        exportAllButton.Enabled = true;
        if (_controller.SelectedFolderIndex >= 0)
        {
            folderGrid.ClearSelection();
            folderGrid.Rows[_controller.SelectedFolderIndex].Selected = true;
            folderGrid.CurrentCell = folderGrid.Rows[_controller.SelectedFolderIndex].Cells[0];
            RenderCurrentFolder();
        }
    }

    private void FolderGrid_SelectionChanged(object? sender, EventArgs e)
    {
        var index = folderGrid.CurrentRow?.Index ?? -1;
        if (_controller.SelectFolder(index)) RenderCurrentFolder();
    }

    private void RenderCurrentFolder()
    {
        var folder = _controller.CurrentFolder;
        if (folder is null)
        {
            ApplyCurrentFolder(null);
            return;
        }

        var result = folder.Detail.Result;
        currentConclusionLabel.Text = folder.Detail.Conclusion;
        currentConclusionLabel.ForeColor = ColorTranslator.FromHtml(folder.StatusColor);
        currentPathLabel.Text = folder.Path;
        directoriesValueLabel.Text = $"{result.Summary.ActualDirectoryCount} / {result.Summary.ExpectedDirectoryCount}";
        txtValueLabel.Text = $"{result.Summary.ActualTxtFileCount} / {result.Summary.ExpectedTxtFileCount}";
        errorsValueLabel.Text = result.Summary.ErrorCount.ToString();
        warningsValueLabel.Text = result.Summary.WarningCount.ToString();
        exportCurrentButton.Enabled = true;
        ShowRows(IssueFilter.All);
    }

    private void ApplyCurrentFolder(object? unused)
    {
        currentConclusionLabel.Text = "尚未选择检查结果";
        currentConclusionLabel.ForeColor = Color.FromArgb(96, 117, 138);
        currentPathLabel.Text = "—";
        directoriesValueLabel.Text = "—";
        txtValueLabel.Text = "—";
        errorsValueLabel.Text = "0";
        warningsValueLabel.Text = "0";
        detailTextBox.Text = "选择一条明细，可在这里查看完整说明。";
        exportCurrentButton.Enabled = false;
        copyButton.Enabled = false;
        openLocationButton.Enabled = false;
    }

    private void FilterButton_Click(object? sender, EventArgs e) => ShowRows(sender == errorsButton
        ? IssueFilter.Errors
        : sender == warningsButton ? IssueFilter.Warnings : IssueFilter.All);

    private void ShowRows(IssueFilter filter)
    {
        issueGrid.Rows.Clear();
        detailTextBox.Text = "选择一条明细，可在这里查看完整说明。";
        _visibleRows = _controller.BuildIssueRows(filter);
        foreach (var row in _visibleRows)
        {
            var index = issueGrid.Rows.Add(row.SeverityText, row.CategoryText, row.DeviceName, row.FileName, row.Message);
            issueGrid.Rows[index].Cells[0].Style.ForeColor = ColorTranslator.FromHtml(row.ColorHex);
            issueGrid.Rows[index].Cells[0].Style.Font = new Font(issueGrid.Font, FontStyle.Bold);
        }
    }

    private void IssueGrid_SelectionChanged(object? sender, EventArgs e)
    {
        var index = issueGrid.CurrentRow?.Index ?? -1;
        detailTextBox.Text = index >= 0 && index < _visibleRows.Count
            ? _visibleRows[index].DetailText
            : "选择一条明细，可在这里查看完整说明。";
        copyButton.Enabled = index >= 0 && index < _visibleRows.Count;
        openLocationButton.Enabled = copyButton.Enabled;
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(detailTextBox.Text))
        {
            Clipboard.SetText(detailTextBox.Text);
            statusLabel.Text = "已复制所选明细";
        }
    }

    private void OpenLocationButton_Click(object? sender, EventArgs e)
    {
        var index = issueGrid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _visibleRows.Count) return;
        var target = OpenLocationResolver.Resolve(_visibleRows[index].Path);
        if (target is null)
        {
            MessageBox.Show(this, "没有找到可打开的位置。请确认原检查文件夹仍然存在。", "无法打开位置",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
            startInfo.ArgumentList.Add(target.SelectFile ? $"/select,{target.Path}" : target.Path);
            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法打开位置", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ExportCurrentButton_Click(object? sender, EventArgs e)
    {
        if (_controller.CurrentResult is null) return;
        await SaveReportAsync("导出当前文件夹报告", $"IDC日志检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            ChineseTextReportWriter.Write(_controller.CurrentResult));
    }

    private async void ExportAllButton_Click(object? sender, EventArgs e)
    {
        if (_controller.BatchResult is null) return;
        await SaveReportAsync("导出全部检查结果", $"IDC日志批量检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            ChineseBatchReportWriter.Write(_controller.BatchResult));
    }

    private async Task SaveReportAsync(string title, string name, string content)
    {
        using var dialog = new SaveFileDialog
        {
            Title = title, Filter = "文本文件 (*.txt)|*.txt", FileName = name,
            AddExtension = true, DefaultExt = "txt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await File.WriteAllTextAsync(dialog.FileName, content, new System.Text.UTF8Encoding(false));
            MessageBox.Show(this, $"报告已保存到：\r\n{dialog.FileName}", "报告已导出",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "报告导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyIdleState()
    {
        selectionSummaryLabel.Text = "尚未选择文件夹";
        inputNoticeLabel.Text = "可点击选择多个文件夹，也可将多个文件夹拖入窗口";
        totalValueLabel.Text = cleanValueLabel.Text = batchWarningValueLabel.Text = failedValueLabel.Text = "0";
        ApplyCurrentFolder(null);
        statusLabel.Text = "请选择需要检查的日志文件夹";
        startButton.Enabled = false;
        exportAllButton.Enabled = false;
        progressBar.Value = 0;
    }

    private void SetBusy(bool busy)
    {
        chooseFolderButton.Enabled = !busy;
        startButton.Enabled = !busy && _controller.CanStart;
        allButton.Enabled = !busy;
        errorsButton.Enabled = !busy;
        warningsButton.Enabled = !busy;
        AllowDrop = !busy;
        UseWaitCursor = busy;
        if (busy)
        {
            exportAllButton.Enabled = false;
            exportCurrentButton.Enabled = false;
        }
        else
        {
            exportAllButton.Enabled = _controller.CanExportAll;
            exportCurrentButton.Enabled = _controller.CanExportCurrent;
        }
    }
}
