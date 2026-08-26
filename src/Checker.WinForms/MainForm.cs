using System.Diagnostics;
using IDCLogChecker.Core.Batch;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Reporting;

namespace IDCLogChecker.WinForms;

public partial class MainForm : Form
{
    private readonly BatchFormController _controller = new(BatchScanCoordinator.CreateDefault());
    private IReadOnlyList<IssueListRow> _visibleRows = [];
    private InspectionLevel _selectedLevel = InspectionLevel.ExecutionResults;
    private int _operationRowIndex = -1;
    private int _selectedFolderCopyRowIndex = -1;
    private int _issueCopyRowIndex = -1;
    private int _issueCopyColumnIndex = -1;

    public MainForm()
    {
        InitializeComponent();
        EnableLabelCopyMenus(this);
        ConfigureCopyMenus();
        ShowHome();
    }

    private static void CopyToClipboard(string? text)
    {
        if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
    }

    private static void EnableLabelCopyMenus(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Label label)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("复制文字", null, (_, _) => CopyToClipboard(label.Text));
                label.ContextMenuStrip = menu;
            }
            EnableLabelCopyMenus(control);
        }
    }

    private void ConfigureCopyMenus()
    {
        selectedFolderGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        var folderMenu = new ContextMenuStrip();
        folderMenu.Items.Add("复制文件夹名", null, (_, _) => CopySelectedFolderCell("Folder"));
        folderMenu.Items.Add("复制路径", null, (_, _) => CopySelectedFolderCell("Path"));
        selectedFolderGrid.ContextMenuStrip = folderMenu;
        selectedFolderGrid.CellMouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            _selectedFolderCopyRowIndex = e.RowIndex;
            selectedFolderGrid.ClearSelection();
            selectedFolderGrid.Rows[e.RowIndex].Selected = true;
            selectedFolderGrid.CurrentCell = selectedFolderGrid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
        };

        issueGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        var issueCopyMenu = new ContextMenuStrip();
        issueCopyMenu.Items.Add("复制此处文字", null, (_, _) =>
        {
            if (_issueCopyRowIndex >= 0 && _issueCopyColumnIndex >= 0)
                CopyToClipboard(issueGrid.Rows[_issueCopyRowIndex].Cells[_issueCopyColumnIndex].Value?.ToString());
        });
        issueCopyMenu.Items.Add("复制整条信息", null, (_, _) =>
        {
            if (_issueCopyRowIndex >= 0 && _issueCopyRowIndex < _visibleRows.Count)
                CopyToClipboard(_visibleRows[_issueCopyRowIndex].DetailText);
        });
        issueGrid.ContextMenuStrip = issueCopyMenu;
        issueGrid.CellMouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            _issueCopyRowIndex = e.RowIndex;
            _issueCopyColumnIndex = e.ColumnIndex;
            issueGrid.CurrentCell = issueGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        };
    }

    private void CopySelectedFolderCell(string columnName)
    {
        if (_selectedFolderCopyRowIndex < 0 || _selectedFolderCopyRowIndex >= selectedFolderGrid.Rows.Count) return;
        CopyToClipboard(selectedFolderGrid.Rows[_selectedFolderCopyRowIndex].Cells[columnName].Value?.ToString());
    }

    private void HomeChooseButton_Click(object? sender, EventArgs e) => ChooseFolders(false);
    private void AddFolderButton_Click(object? sender, EventArgs e) => ChooseFolders(true);

    private void ChooseFolders(bool append)
    {
        try
        {
            var paths = NativeMultiFolderPicker.Show(Handle);
            if (paths.Count > 0) AcceptSelection(paths, append);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"无法打开文件夹选择窗口：\r\n{exception.Message}", "选择失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e) => UpdateDragState(e);
    private void MainForm_DragOver(object? sender, DragEventArgs e) => UpdateDragState(e);
    private void MainForm_DragLeave(object? sender, EventArgs e) { homeDropPanel.BackColor = Color.FromArgb(247, 250, 252); }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        MainForm_DragLeave(sender, EventArgs.Empty);
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AcceptSelection(paths, _controller.SelectedPaths.Count > 0);
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var accepted = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        e.Effect = accepted ? DragDropEffects.Copy : DragDropEffects.None;
        homeDropPanel.BackColor = accepted ? Color.FromArgb(234, 246, 252) : Color.FromArgb(253, 237, 236);
    }

    private void AcceptSelection(IEnumerable<string?> paths, bool append)
    {
        var accepted = append ? _controller.AddSelection(paths) : _controller.ReplaceSelection(paths);
        if (!accepted)
        {
            MessageBox.Show(this, _controller.InputNoticeText, "没有有效文件夹", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ShowResults();
        RenderSelectedFolders();
        ResetScanResult();
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        _controller.ClearSelection();
        selectedFolderGrid.Rows.Clear();
        issueGrid.Rows.Clear();
        ShowHome();
    }

    private void SelectedFolderGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || selectedFolderGrid.Columns[e.ColumnIndex].Name != "Remove") return;
        var path = selectedFolderGrid.Rows[e.RowIndex].Tag as string;
        if (path is null || !_controller.RemoveSelection(path)) return;
        if (_controller.SelectedPaths.Count == 0) ShowHome();
        else { RenderSelectedFolders(); ResetScanResult(); }
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (!_controller.CanStart) return;
        SetBusy(true);
        try
        {
            var progress = new Progress<BatchScanProgress>(item =>
            {
                var inner = item.DirectoryProgress is { TotalDirectories: > 0 } directory ? directory.CompletedDirectories / (double)directory.TotalDirectories : 0;
                progressBar.Value = Math.Clamp((int)Math.Round((item.CompletedFolders + inner) * 100 / item.TotalFolders), 0, 100);
                statusLabel.Text = item.CompletedFolders >= item.TotalFolders ? "全部文件夹检查完成" : $"正在检查第 {item.FolderIndex}/{item.TotalFolders} 个：{Path.GetFileName(item.FolderPath)}";
            });
            await _controller.RunAsync(progress);
            progressBar.Value = 100;
            statusLabel.Text = "全部文件夹检查完成，请在左侧选择文件夹，再查看相应级别的结果";
            exportButton.Enabled = true;
            if (_controller.SelectedFolderIndex >= 0)
            {
                selectedFolderGrid.ClearSelection();
                selectedFolderGrid.Rows[_controller.SelectedFolderIndex].Selected = true;
                selectedFolderGrid.CurrentCell = selectedFolderGrid.Rows[_controller.SelectedFolderIndex].Cells[0];
            }
            RenderCurrentFolder();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"程序无法完成检查：\r\n{exception.Message}", "检查未完成", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "检查未完成，请查看说明后重试";
        }
        finally { SetBusy(false); }
    }

    private void SelectedFolderGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_controller.BatchResult is null) return;
        var index = selectedFolderGrid.CurrentRow?.Index ?? -1;
        if (_controller.SelectFolder(index)) RenderCurrentFolder();
    }

    private void LevelOneButton_Click(object? sender, EventArgs e) => SelectLevel(InspectionLevel.DeviceDirectories);
    private void LevelTwoButton_Click(object? sender, EventArgs e) => SelectLevel(InspectionLevel.CommandFiles);
    private void LevelThreeButton_Click(object? sender, EventArgs e) => SelectLevel(InspectionLevel.ExecutionResults);

    private void SelectLevel(InspectionLevel level)
    {
        _selectedLevel = level;
        if (_controller.CurrentFolder is null) { ShowRows([]); return; }
        var summary = _controller.GetLevelSummary(level);
        detailTitleLabel.Text = summary.Title + " · 结果明细";
        levelMessageLabel.Text = summary.DetailMessage;
        ShowRows(_controller.BuildIssueRows(level));
        foreach (var button in new[] { levelOneButton, levelTwoButton, levelThreeButton })
        {
            button.BackColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(202, 214, 222);
            button.FlatAppearance.BorderSize = 1;
        }
        var selectedButton = level switch { InspectionLevel.DeviceDirectories => levelOneButton, InspectionLevel.CommandFiles => levelTwoButton, _ => levelThreeButton };
        selectedButton.BackColor = Color.FromArgb(247, 252, 250);
        selectedButton.FlatAppearance.BorderColor = Color.FromArgb(25, 115, 95);
        selectedButton.FlatAppearance.BorderSize = 2;
    }

    private void RenderCurrentFolder()
    {
        if (_controller.CurrentFolder is null) return;
        levelOneButton.Text = "一级设备目录检查\r\n" + _controller.GetLevelSummary(InspectionLevel.DeviceDirectories).CardText;
        levelTwoButton.Text = "二级命令数目检查\r\n" + _controller.GetLevelSummary(InspectionLevel.CommandFiles).CardText;
        var levelThree = _controller.GetLevelSummary(InspectionLevel.ExecutionResults);
        levelThreeButton.Text = "三级执行结果检查\r\n" + levelThree.CardText + "\r\n" + levelThree.DetailMessage;
        SelectLevel(_selectedLevel);
    }

    private void ShowRows(IReadOnlyList<IssueListRow> rows)
    {
        _visibleRows = rows;
        issueGrid.Rows.Clear();
        foreach (var row in rows)
        {
            var index = issueGrid.Rows.Add(row.SeverityText, row.CategoryText, row.DeviceName, row.FileName, row.Message, row.Actual, row.Path, "打开位置  ▼");
            issueGrid.Rows[index].Tag = row;
            foreach (DataGridViewCell cell in issueGrid.Rows[index].Cells) cell.ToolTipText = cell.Value?.ToString() ?? string.Empty;
        }
        issueGrid.Visible = rows.Count > 0;
        emptyMessageLabel.Text = levelMessageLabel.Text;
        emptyMessageLabel.Visible = rows.Count == 0;
    }

    private void IssueGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || issueGrid.Columns[e.ColumnIndex].Name != "Operation") return;
        _operationRowIndex = e.RowIndex;
        var rectangle = issueGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
        operationMenu.Show(issueGrid, new Point(rectangle.Left, rectangle.Bottom));
    }

    private IssueListRow? OperationRow() => _operationRowIndex >= 0 && _operationRowIndex < _visibleRows.Count ? _visibleRows[_operationRowIndex] : null;
    private void OperationOpen_Click(object? sender, EventArgs e) { var row = OperationRow(); if (row is not null) OpenLocation(row.Path); }
    private void OperationDetails_Click(object? sender, EventArgs e)
    {
        var row = OperationRow(); if (row is null) return;
        using var dialog = new Form { Text = "错误详情", StartPosition = FormStartPosition.CenterParent, Size = new Size(760, 520), MinimumSize = new Size(560, 380), Font = Font };
        dialog.Controls.Add(new TextBox { Text = row.DetailText, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, WordWrap = false, ScrollBars = ScrollBars.Both, BackColor = Color.White });
        dialog.ShowDialog(this);
    }
    private void OperationCopy_Click(object? sender, EventArgs e) { var row = OperationRow(); if (row is null) return; Clipboard.SetText(row.DetailText); statusLabel.Text = "已复制所选错误信息"; }

    private void OpenLocation(string path)
    {
        var target = OpenLocationResolver.Resolve(path);
        if (target is null) { MessageBox.Show(this, "没有找到可打开的位置。请确认原检查文件夹仍然存在。", "无法打开位置", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try
        {
            var launch = WindowsExplorerLaunch.Build(target);
            var info = new ProcessStartInfo(launch.FileName, launch.Arguments) { UseShellExecute = true };
            Process.Start(info);
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "无法打开位置", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void ExportButton_Click(object? sender, EventArgs e)
    {
        if (_controller.BatchResult is null) return;
        using var dialog = new SaveFileDialog { Title = "导出检查数据", Filter = "文本文件 (*.txt)|*.txt", FileName = $"设备检查数据_{DateTime.Now:yyyyMMdd_HHmmss}.txt", AddExtension = true, DefaultExt = "txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { await File.WriteAllTextAsync(dialog.FileName, ChineseBatchReportWriter.Write(_controller.BatchResult), new System.Text.UTF8Encoding(false)); MessageBox.Show(this, $"数据已保存到：\r\n{dialog.FileName}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RenderSelectedFolders()
    {
        selectedFolderGrid.Rows.Clear();
        foreach (var path in _controller.SelectedPaths) { var index = selectedFolderGrid.Rows.Add(Path.GetFileName(path), path, "移除"); selectedFolderGrid.Rows[index].Tag = path; }
        selectedCountLabel.Text = $"共选择 {_controller.SelectedPaths.Count} 个文件夹";
    }

    private void ResetScanResult()
    {
        levelOneButton.Text = "一级设备目录检查\r\n—"; levelTwoButton.Text = "二级命令数目检查\r\n—"; levelThreeButton.Text = "三级执行结果检查\r\n—";
        detailTitleLabel.Text = "三级执行结果检查 · 结果明细"; levelMessageLabel.Text = "完成检查后显示结果。"; ShowRows([]);
        statusLabel.Text = "已选择文件夹，点击“开始检查”开始"; progressBar.Value = 0; startButton.Enabled = true; exportButton.Enabled = false;
    }

    private void ShowHome() { homePanel.Visible = true; homePanel.BringToFront(); resultsPanel.Visible = false; }
    private void ShowResults() { homePanel.Visible = false; resultsPanel.Visible = true; resultsPanel.BringToFront(); }
    private void SetBusy(bool busy) { homeChooseButton.Enabled = addFolderButton.Enabled = clearButton.Enabled = !busy; startButton.Enabled = !busy && _controller.CanStart; exportButton.Enabled = !busy && _controller.CanExportAll; progressBar.Visible = busy; AllowDrop = !busy; UseWaitCursor = busy; }
}
