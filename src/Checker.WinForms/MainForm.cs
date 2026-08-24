using System.Diagnostics;
using IDCLogChecker.Core.Presentation;
using IDCLogChecker.Core.Reporting;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.WinForms;

public partial class MainForm : Form
{
    private readonly DirectoryScanner _scanner = DirectoryScanner.CreateDefault();
    private ResultPresentation? _presentation;
    private IReadOnlyList<IssueListRow> _visibleRows = [];

    public MainForm()
    {
        InitializeComponent();
        ApplyIdleState();
    }

    private void ChooseFolderButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择直接包含 62 个设备目录的文件夹",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            pathTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(pathTextBox.Text))
        {
            MessageBox.Show(this, "请先选择需要检查的文件夹。", "尚未选择文件夹",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            var progress = new Progress<ScanProgress>(item =>
            {
                progressBar.Maximum = Math.Max(1, item.TotalDirectories);
                progressBar.Value = Math.Min(progressBar.Maximum, item.CompletedDirectories);
                statusLabel.Text = item.CurrentItem == "检查完成"
                    ? "检查完成"
                    : $"正在检查：{item.CurrentItem}";
            });
            var result = await _scanner.ScanAsync(pathTextBox.Text.Trim(), progress, CancellationToken.None);
            _presentation = ResultPresentation.From(result);
            conclusionLabel.Text = _presentation.Conclusion;
            conclusionLabel.ForeColor = ColorTranslator.FromHtml(_presentation.StatusColor);
            directoriesValueLabel.Text = $"{result.Summary.ActualDirectoryCount} / {result.Summary.ExpectedDirectoryCount}";
            txtValueLabel.Text = $"{result.Summary.ActualTxtFileCount} / {result.Summary.ExpectedTxtFileCount}";
            errorsValueLabel.Text = result.Summary.ErrorCount.ToString();
            warningsValueLabel.Text = result.Summary.WarningCount.ToString();
            progressBar.Maximum = 100;
            progressBar.Value = 100;
            statusLabel.Text = "检查完成，可筛选明细或导出报告";
            ShowRows(IssueFilter.All);
            exportButton.Enabled = true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"程序无法完成检查：\r\n{exception.Message}", "检查未完成",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "检查未完成，请查看错误说明后重试";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FilterButton_Click(object? sender, EventArgs e)
    {
        ShowRows(sender == errorsButton
            ? IssueFilter.Errors
            : sender == warningsButton ? IssueFilter.Warnings : IssueFilter.All);
    }

    private void ShowRows(IssueFilter filter)
    {
        issueGrid.Rows.Clear();
        detailTextBox.Text = "选择一条明细，可在这里查看完整说明。";
        if (_presentation is null)
        {
            return;
        }

        _visibleRows = IssueListAdapter.BuildRows(_presentation, filter);
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
        if (_presentation is null || index < 0 || index >= _visibleRows.Count)
        {
            return;
        }

        var displayed = _visibleRows[index];
        if (string.IsNullOrWhiteSpace(displayed.Path))
        {
            MessageBox.Show(this, "这条明细没有可打开的位置。", "无法打开位置",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{displayed.Path}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法打开位置",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ExportButton_Click(object? sender, EventArgs e)
    {
        if (_presentation is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "导出检查报告",
            Filter = "文本文件 (*.txt)|*.txt",
            FileName = $"IDC日志检查报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            AddExtension = true,
            DefaultExt = "txt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await ChineseTextReportWriter.SaveAsync(_presentation.Result, dialog.FileName);
            MessageBox.Show(this, $"报告已保存到：\r\n{dialog.FileName}", "报告已导出",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "报告导出失败",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyIdleState()
    {
        conclusionLabel.Text = "尚未开始检查";
        conclusionLabel.ForeColor = Color.FromArgb(96, 117, 138);
        directoriesValueLabel.Text = "—";
        txtValueLabel.Text = "—";
        errorsValueLabel.Text = "0";
        warningsValueLabel.Text = "0";
        statusLabel.Text = "请选择需要检查的日志文件夹";
        exportButton.Enabled = false;
        copyButton.Enabled = false;
        openLocationButton.Enabled = false;
    }

    private void SetBusy(bool busy)
    {
        pathTextBox.Enabled = !busy;
        chooseFolderButton.Enabled = !busy;
        startButton.Enabled = !busy;
        allButton.Enabled = !busy;
        errorsButton.Enabled = !busy;
        warningsButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
