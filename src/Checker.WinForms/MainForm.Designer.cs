#nullable enable

namespace IDCLogChecker.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TextBox pathTextBox = null!;
    private Button chooseFolderButton = null!;
    private Button startButton = null!;
    private Label conclusionLabel = null!;
    private Label directoriesValueLabel = null!;
    private Label txtValueLabel = null!;
    private Label errorsValueLabel = null!;
    private Label warningsValueLabel = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private Button allButton = null!;
    private Button errorsButton = null!;
    private Button warningsButton = null!;
    private Button exportButton = null!;
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
        MinimumSize = new Size(1040, 720);
        ClientSize = new Size(1180, 800);
        Font = new Font("Microsoft YaHei UI", 10F);
        BackColor = Color.FromArgb(244, 247, 250);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(22, 16, 22, 18) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 57, 82), Padding = new Padding(18, 10, 18, 8) };
        header.Controls.Add(new Label { Text = "IDC 日志完整性检查工具", ForeColor = Color.White, Font = new Font(Font.FontFamily, 20F, FontStyle.Bold), AutoSize = true, Location = new Point(16, 8) });
        header.Controls.Add(new Label { Text = "基准：62 个设备目录 · 3,660 个 TXT 文件", ForeColor = Color.FromArgb(207, 224, 238), AutoSize = true, Location = new Point(19, 45) });
        root.Controls.Add(header, 0, 0);

        var pathPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 10, 0, 6) };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        pathTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "请选择直接包含 62 个设备目录的文件夹", Margin = new Padding(0, 2, 10, 2) };
        chooseFolderButton = MakeButton("选择文件夹…", Color.FromArgb(224, 232, 239), Color.FromArgb(30, 57, 82));
        chooseFolderButton.Click += ChooseFolderButton_Click;
        startButton = MakeButton("开始检查", Color.FromArgb(40, 112, 147), Color.White);
        startButton.Font = new Font(Font, FontStyle.Bold);
        startButton.Click += StartButton_Click;
        pathPanel.Controls.Add(pathTextBox, 0, 0);
        pathPanel.Controls.Add(chooseFolderButton, 1, 0);
        pathPanel.Controls.Add(startButton, 2, 0);
        root.Controls.Add(pathPanel, 0, 1);

        var summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = Color.White, Padding = new Padding(16, 12, 16, 10) };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        for (var i = 0; i < 4; i++) summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        conclusionLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font.FontFamily, 15F, FontStyle.Bold) };
        summary.Controls.Add(conclusionLabel, 0, 0);
        directoriesValueLabel = AddSummaryCard(summary, 1, "设备目录");
        txtValueLabel = AddSummaryCard(summary, 2, "TXT 文件");
        errorsValueLabel = AddSummaryCard(summary, 3, "错误", Color.FromArgb(192, 57, 43));
        warningsValueLabel = AddSummaryCard(summary, 4, "提示", Color.FromArgb(215, 140, 18));
        root.Controls.Add(summary, 0, 2);

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 9, 0, 5) };
        allButton = MakeButton("全部", Color.FromArgb(49, 90, 125), Color.White); allButton.Click += FilterButton_Click;
        errorsButton = MakeButton("只看错误", Color.White, Color.FromArgb(192, 57, 43)); errorsButton.Click += FilterButton_Click;
        warningsButton = MakeButton("只看提示", Color.White, Color.FromArgb(180, 113, 10)); warningsButton.Click += FilterButton_Click;
        exportButton = MakeButton("导出报告", Color.FromArgb(31, 138, 112), Color.White); exportButton.Click += ExportButton_Click;
        actionPanel.Controls.AddRange([allButton, errorsButton, warningsButton, exportButton]);
        root.Controls.Add(actionPanel, 0, 3);

        issueGrid = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
        };
        issueGrid.Columns.Add("Severity", "级别");
        issueGrid.Columns.Add("Category", "问题类型");
        issueGrid.Columns.Add("Device", "设备目录");
        issueGrid.Columns.Add("File", "文件名");
        issueGrid.Columns.Add("Message", "说明");
        issueGrid.Columns[0].FillWeight = 45;
        issueGrid.Columns[1].FillWeight = 105;
        issueGrid.Columns[2].FillWeight = 100;
        issueGrid.Columns[3].FillWeight = 115;
        issueGrid.Columns[4].FillWeight = 230;
        issueGrid.SelectionChanged += IssueGrid_SelectionChanged;
        root.Controls.Add(issueGrid, 0, 4);

        var detailPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
        detailPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        detailTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White };
        detailPanel.Controls.Add(detailTextBox, 0, 0);
        detailPanel.SetColumnSpan(detailTextBox, 2);
        statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(75, 91, 105) };
        progressBar = new ProgressBar { Dock = DockStyle.Right, Width = 220, Height = 14, Margin = new Padding(0, 8, 8, 7) };
        var statusPanel = new Panel { Dock = DockStyle.Fill }; statusPanel.Controls.Add(statusLabel); statusPanel.Controls.Add(progressBar);
        copyButton = MakeButton("复制明细", Color.White, Color.FromArgb(30, 57, 82)); copyButton.Click += CopyButton_Click;
        openLocationButton = MakeButton("打开位置", Color.White, Color.FromArgb(30, 57, 82)); openLocationButton.Click += OpenLocationButton_Click;
        var detailButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        detailButtons.Controls.Add(openLocationButton); detailButtons.Controls.Add(copyButton);
        detailPanel.Controls.Add(statusPanel, 0, 1);
        detailPanel.Controls.Add(detailButtons, 1, 1);
        root.Controls.Add(detailPanel, 0, 5);
        ResumeLayout(false);
    }

    private Button MakeButton(string text, Color backColor, Color foreColor) => new()
    {
        Text = text, Width = 120, Height = 36, Margin = new Padding(5, 0, 5, 0),
        FlatStyle = FlatStyle.Flat, BackColor = backColor, ForeColor = foreColor, Cursor = Cursors.Hand,
    };

    private Label AddSummaryCard(TableLayoutPanel parent, int column, string caption, Color? valueColor = null)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(5), BackColor = Color.FromArgb(247, 249, 251) };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomCenter, ForeColor = Color.FromArgb(90, 105, 119) }, 0, 0);
        var value = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter, Font = new Font(Font.FontFamily, 15F, FontStyle.Bold), ForeColor = valueColor ?? Color.FromArgb(30, 57, 82) };
        panel.Controls.Add(value, 0, 1);
        parent.Controls.Add(panel, column, 0);
        return value;
    }
}
