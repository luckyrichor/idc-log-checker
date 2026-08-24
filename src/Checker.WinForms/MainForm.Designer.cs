#nullable enable

namespace IDCLogChecker.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel inputPanel = null!;
    private Label selectionSummaryLabel = null!;
    private Label inputNoticeLabel = null!;
    private Button chooseFolderButton = null!;
    private Button startButton = null!;
    private Label totalValueLabel = null!;
    private Label cleanValueLabel = null!;
    private Label batchWarningValueLabel = null!;
    private Label failedValueLabel = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private DataGridView folderGrid = null!;
    private Label currentConclusionLabel = null!;
    private Label currentPathLabel = null!;
    private Label directoriesValueLabel = null!;
    private Label txtValueLabel = null!;
    private Label errorsValueLabel = null!;
    private Label warningsValueLabel = null!;
    private Button allButton = null!;
    private Button errorsButton = null!;
    private Button warningsButton = null!;
    private Button exportCurrentButton = null!;
    private Button exportAllButton = null!;
    private Button copyButton = null!;
    private Button openLocationButton = null!;
    private DataGridView issueGrid = null!;
    private TextBox detailTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        Text = "IDC 日志完整性检查工具（WinForms）";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 740);
        ClientSize = new Size(1280, 840);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        BackColor = Color.FromArgb(244, 247, 250);
        AllowDrop = true;
        DragEnter += MainForm_DragEnter;
        DragOver += MainForm_DragOver;
        DragLeave += MainForm_DragLeave;
        DragDrop += MainForm_DragDrop;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(20, 14, 20, 16) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 57, 82) };
        header.Controls.Add(new Label { Text = "IDC 日志完整性检查工具", ForeColor = Color.White, Font = new Font(Font.FontFamily, 19F, FontStyle.Bold), AutoSize = true, Location = new Point(18, 8) });
        header.Controls.Add(new Label { Text = "支持多选和拖入多个文件夹 · 基准 62 个设备目录 / 3,660 个 TXT", ForeColor = Color.FromArgb(207, 224, 238), AutoSize = true, Location = new Point(20, 43) });
        root.Controls.Add(header, 0, 0);

        inputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.White, Padding = new Padding(14, 9, 10, 8), Margin = new Padding(0, 7, 0, 5) };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        var inputText = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        inputText.RowStyles.Add(new RowStyle(SizeType.Percent, 52)); inputText.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        selectionSummaryLabel = new Label { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(30, 57, 82), TextAlign = ContentAlignment.BottomLeft };
        inputNoticeLabel = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(96, 117, 138), TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true };
        inputText.Controls.Add(selectionSummaryLabel, 0, 0); inputText.Controls.Add(inputNoticeLabel, 0, 1);
        chooseFolderButton = MakeButton("选择多个文件夹…", Color.FromArgb(224, 232, 239), Color.FromArgb(30, 57, 82), 150);
        chooseFolderButton.Click += ChooseFolderButton_Click;
        startButton = MakeButton("开始检查", Color.FromArgb(40, 112, 147), Color.White, 135);
        startButton.Font = new Font(Font, FontStyle.Bold); startButton.Click += StartButton_Click;
        inputPanel.Controls.Add(inputText, 0, 0); inputPanel.Controls.Add(chooseFolderButton, 1, 0); inputPanel.Controls.Add(startButton, 2, 0);
        root.Controls.Add(inputPanel, 0, 1);

        var batchSummary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
        for (var i = 0; i < 4; i++) batchSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        totalValueLabel = AddSummaryCard(batchSummary, 0, "本批次文件夹", Color.FromArgb(30, 57, 82));
        cleanValueLabel = AddSummaryCard(batchSummary, 1, "完全通过", Color.FromArgb(31, 138, 112));
        batchWarningValueLabel = AddSummaryCard(batchSummary, 2, "有提示", Color.FromArgb(215, 140, 18));
        failedValueLabel = AddSummaryCard(batchSummary, 3, "不通过", Color.FromArgb(192, 57, 43));
        root.Controls.Add(batchSummary, 0, 2);

        var statusPanel = new Panel { Dock = DockStyle.Fill };
        statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(75, 91, 105), AutoEllipsis = true };
        progressBar = new ProgressBar { Dock = DockStyle.Right, Width = 250, Height = 12, Margin = new Padding(0, 10, 0, 10) };
        statusPanel.Controls.Add(statusLabel); statusPanel.Controls.Add(progressBar);
        root.Controls.Add(statusPanel, 0, 3);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 325, FixedPanel = FixedPanel.Panel1, Panel1MinSize = 270, Panel2MinSize = 600, BackColor = Color.FromArgb(215, 224, 232) };
        root.Controls.Add(split, 0, 4);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.White };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(new Label { Text = "文件夹检查结果", Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold), BackColor = Color.FromArgb(234, 240, 245) }, 0, 0);
        folderGrid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        folderGrid.Columns.Add("Folder", "文件夹"); folderGrid.Columns.Add("Status", "状态"); folderGrid.Columns.Add("Errors", "错误"); folderGrid.Columns.Add("Warnings", "提示");
        folderGrid.Columns[0].FillWeight = 150; folderGrid.Columns[1].FillWeight = 72; folderGrid.Columns[2].FillWeight = 45; folderGrid.Columns[3].FillWeight = 45;
        folderGrid.SelectionChanged += FolderGrid_SelectionChanged;
        left.Controls.Add(folderGrid, 0, 1); split.Panel1.Controls.Add(left);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, BackColor = Color.White };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 88)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(right);

        var currentSummary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(12, 8, 12, 6) };
        currentSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); for (var i = 1; i < 5; i++) currentSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.5F));
        var currentText = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        currentConclusionLabel = new Label { Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 13F, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
        currentPathLabel = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(96, 117, 138), TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true };
        currentText.Controls.Add(currentConclusionLabel, 0, 0); currentText.Controls.Add(currentPathLabel, 0, 1); currentSummary.Controls.Add(currentText, 0, 0);
        directoriesValueLabel = AddSummaryCard(currentSummary, 1, "设备目录", Color.FromArgb(30, 57, 82));
        txtValueLabel = AddSummaryCard(currentSummary, 2, "TXT 文件", Color.FromArgb(30, 57, 82));
        errorsValueLabel = AddSummaryCard(currentSummary, 3, "错误", Color.FromArgb(192, 57, 43));
        warningsValueLabel = AddSummaryCard(currentSummary, 4, "提示", Color.FromArgb(215, 140, 18));
        right.Controls.Add(currentSummary, 0, 0);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8, 5, 8, 5) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        allButton = MakeButton("全部", Color.FromArgb(49, 90, 125), Color.White, 72); allButton.Click += FilterButton_Click;
        errorsButton = MakeButton("只看错误", Color.White, Color.FromArgb(192, 57, 43), 90); errorsButton.Click += FilterButton_Click;
        warningsButton = MakeButton("只看提示", Color.White, Color.FromArgb(180, 113, 10), 90); warningsButton.Click += FilterButton_Click;
        filters.Controls.AddRange([allButton, errorsButton, warningsButton]);
        var exports = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        exportAllButton = MakeButton("导出全部", Color.FromArgb(31, 138, 112), Color.White, 90); exportAllButton.Click += ExportAllButton_Click;
        exportCurrentButton = MakeButton("导出当前", Color.White, Color.FromArgb(31, 138, 112), 90); exportCurrentButton.Click += ExportCurrentButton_Click;
        exports.Controls.Add(exportAllButton); exports.Controls.Add(exportCurrentButton);
        actions.Controls.Add(filters, 0, 0); actions.Controls.Add(exports, 1, 0); right.Controls.Add(actions, 0, 1);

        var headerRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = Color.FromArgb(234, 240, 245), Padding = new Padding(8, 5, 8, 4) };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9)); headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24)); headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        foreach (var (text, column) in new[] { ("级别", 0), ("问题类型", 1), ("设备目录", 2), ("文件名", 3), ("说明", 4) })
            headerRow.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, column, 0);
        right.Controls.Add(headerRow, 0, 2);

        issueGrid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, ColumnHeadersVisible = false };
        issueGrid.Columns.Add("Severity", "级别"); issueGrid.Columns.Add("Category", "问题类型"); issueGrid.Columns.Add("Device", "设备目录"); issueGrid.Columns.Add("File", "文件名"); issueGrid.Columns.Add("Message", "说明");
        issueGrid.Columns[0].FillWeight = 45; issueGrid.Columns[1].FillWeight = 100; issueGrid.Columns[2].FillWeight = 120; issueGrid.Columns[3].FillWeight = 100; issueGrid.Columns[4].FillWeight = 135;
        issueGrid.SelectionChanged += IssueGrid_SelectionChanged; right.Controls.Add(issueGrid, 0, 3);

        var detailPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 7, 0, 0) };
        detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225));
        detailTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White };
        var detailButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        openLocationButton = MakeButton("打开位置", Color.White, Color.FromArgb(30, 57, 82), 100); openLocationButton.Click += OpenLocationButton_Click;
        copyButton = MakeButton("复制明细", Color.White, Color.FromArgb(30, 57, 82), 100); copyButton.Click += CopyButton_Click;
        detailButtons.Controls.Add(openLocationButton); detailButtons.Controls.Add(copyButton);
        detailPanel.Controls.Add(detailTextBox, 0, 0); detailPanel.Controls.Add(detailButtons, 1, 0); root.Controls.Add(detailPanel, 0, 5);
        ResumeLayout(false);
    }

    private Button MakeButton(string text, Color backColor, Color foreColor, int width) => new()
    {
        Text = text, Width = width, Height = 34, Margin = new Padding(4, 0, 4, 0), FlatStyle = FlatStyle.Flat,
        BackColor = backColor, ForeColor = foreColor, Cursor = Cursors.Hand,
    };

    private Label AddSummaryCard(TableLayoutPanel parent, int column, string caption, Color valueColor)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(4), BackColor = Color.FromArgb(247, 249, 251) };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomCenter, ForeColor = Color.FromArgb(90, 105, 119) }, 0, 0);
        var value = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter, Font = new Font(Font.FontFamily, 14F, FontStyle.Bold), ForeColor = valueColor };
        panel.Controls.Add(value, 0, 1); parent.Controls.Add(panel, column, 0); return value;
    }
}
