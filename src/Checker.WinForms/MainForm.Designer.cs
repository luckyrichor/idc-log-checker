#nullable enable

namespace IDCLogChecker.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private Panel homePanel = null!;
    private Panel resultsPanel = null!;
    private Panel homeDropPanel = null!;
    private Button homeChooseButton = null!;
    private Button clearButton = null!;
    private Button addFolderButton = null!;
    private Button startButton = null!;
    private Button exportButton = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private DataGridView selectedFolderGrid = null!;
    private Label selectedCountLabel = null!;
    private Button levelOneButton = null!;
    private Button levelTwoButton = null!;
    private Button levelThreeButton = null!;
    private FlowLayoutPanel categoryPanel = null!;
    private Label detailTitleLabel = null!;
    private Label levelMessageLabel = null!;
    private DataGridView issueGrid = null!;
    private Label emptyMessageLabel = null!;
    private ContextMenuStrip operationMenu = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        Text = "设备检查工具";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        ClientSize = new Size(1360, 860);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        BackColor = Color.FromArgb(244, 247, 250);
        AllowDrop = true;
        DragEnter += MainForm_DragEnter;
        DragOver += MainForm_DragOver;
        DragLeave += MainForm_DragLeave;
        DragDrop += MainForm_DragDrop;

        homePanel = BuildHomePanel();
        resultsPanel = BuildResultsPanel();
        Controls.Add(resultsPanel);
        Controls.Add(homePanel);
        ResumeLayout(false);
    }

    private Panel BuildHomePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        var header = MakeHeader();
        panel.Controls.Add(header);

        var card = new TableLayoutPanel { Width = 820, Height = 440, BackColor = Color.White, ColumnCount = 1, RowCount = 4, Padding = new Padding(42), Anchor = AnchorStyles.None };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        card.Controls.Add(new Label { Text = "选择需要检查的文件夹", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 21F, FontStyle.Bold), ForeColor = Color.FromArgb(21, 35, 59) }, 0, 0);
        card.Controls.Add(new Label { Text = "可以一次选择多个文件夹，也可以直接拖入", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.FromArgb(96, 117, 138) }, 0, 1);
        homeDropPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 250, 252), Margin = new Padding(10) };
        homeDropPanel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, homeDropPanel.ClientRectangle, Color.FromArgb(145, 166, 184), ButtonBorderStyle.Dashed);
        homeDropPanel.Controls.Add(new Label { Text = "＋\r\n把文件夹拖到这里\r\n支持一次拖入多个文件夹", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 14F, FontStyle.Bold), ForeColor = Color.FromArgb(41, 68, 93) });
        card.Controls.Add(homeDropPanel, 0, 2);
        homeChooseButton = MakeButton("选择文件夹", Color.FromArgb(31, 138, 112), Color.White, 220);
        homeChooseButton.Anchor = AnchorStyles.None;
        homeChooseButton.TextAlign = ContentAlignment.MiddleCenter;
        homeChooseButton.Click += HomeChooseButton_Click;
        card.Controls.Add(homeChooseButton, 0, 3);
        panel.Controls.Add(card);
        panel.Resize += (_, _) => card.Location = new Point((panel.ClientSize.Width - card.Width) / 2, Math.Max(header.Bottom + 35, (panel.ClientSize.Height - card.Height) / 2));
        return panel;
    }

    private Panel BuildResultsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(17, 0, 17, 18) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(root);

        var header = MakeHeader();
        root.Controls.Add(header, 0, 0);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(new Label { Text = "检查结果", AutoSize = true, Location = new Point(0, 7), Font = new Font(Font.FontFamily, 15F, FontStyle.Bold), ForeColor = Color.FromArgb(25, 49, 73) });
        statusLabel = new Label { AutoEllipsis = true, Location = new Point(1, 36), Width = 500, Height = 22, ForeColor = Color.FromArgb(104, 124, 141) };
        progressBar = new ProgressBar { Visible = false, Location = new Point(1, 57), Width = 300, Height = 4 };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 550, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 13, 0, 0), WrapContents = false };
        clearButton = MakeButton("清除并返回首页", Color.White, Color.FromArgb(163, 64, 55), 140); clearButton.FlatAppearance.BorderColor = Color.FromArgb(215, 164, 158); clearButton.Click += ClearButton_Click;
        addFolderButton = MakeButton("添加文件夹", Color.White, Color.FromArgb(56, 82, 105), 105); addFolderButton.Click += AddFolderButton_Click;
        startButton = MakeButton("开始检查", Color.FromArgb(15, 128, 106), Color.White, 105); startButton.Click += StartButton_Click;
        exportButton = MakeButton("导出数据", Color.FromArgb(15, 128, 106), Color.White, 95); exportButton.Click += ExportButton_Click;
        actions.Controls.AddRange([clearButton, addFolderButton, startButton, exportButton]);
        heading.Controls.Add(statusLabel); heading.Controls.Add(progressBar); heading.Controls.Add(actions); root.Controls.Add(heading, 0, 1);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 240, Panel1MinSize = 215, Panel2MinSize = 650, BackColor = Color.FromArgb(203, 214, 222) };
        root.Controls.Add(split, 0, 2);
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = Color.White };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        left.Controls.Add(new Label { Text = "所选文件夹\r\n点击名称切换右侧结果", Dock = DockStyle.Fill, Padding = new Padding(12, 8, 0, 0), TextAlign = ContentAlignment.TopLeft, Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(43, 70, 93), BackColor = Color.FromArgb(233, 239, 243) }, 0, 0);
        selectedFolderGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeColumns = true, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, ScrollBars = ScrollBars.Both, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
        selectedFolderGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(207, 234, 246); selectedFolderGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(25, 49, 73);
        selectedFolderGrid.Columns.Add("Folder", "文件夹"); selectedFolderGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Remove", HeaderText = "操作", Text = "移除", UseColumnTextForButtonValue = true }); selectedFolderGrid.Columns.Add("Path", "绝对路径");
        SetColumn(selectedFolderGrid.Columns[0], 180, 120); SetColumn(selectedFolderGrid.Columns[1], 75, 65); SetColumn(selectedFolderGrid.Columns[2], 430, 200);
        selectedFolderGrid.SelectionChanged += SelectedFolderGrid_SelectionChanged; selectedFolderGrid.CellContentClick += SelectedFolderGrid_CellContentClick;
        left.Controls.Add(selectedFolderGrid, 0, 1);
        selectedCountLabel = new Label { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(82, 105, 125), BackColor = Color.FromArgb(247, 249, 251) };
        left.Controls.Add(selectedCountLabel, 0, 2); split.Panel1.Controls.Add(left);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, BackColor = Color.White };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 117)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var levels = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(7), BackColor = Color.FromArgb(246, 249, 250) };
        for (var i = 0; i < 3; i++) levels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        levelOneButton = MakeLevelButton("一级设备目录检查"); levelOneButton.Click += LevelOneButton_Click;
        levelTwoButton = MakeLevelButton("二级命令数目检查"); levelTwoButton.Click += LevelTwoButton_Click;
        levelThreeButton = MakeLevelButton("三级执行结果检查"); levelThreeButton.Click += LevelThreeButton_Click;
        levels.Controls.Add(levelOneButton, 0, 0); levels.Controls.Add(levelTwoButton, 1, 0); levels.Controls.Add(levelThreeButton, 2, 0); right.Controls.Add(levels, 0, 0);
        categoryPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10, 5, 10, 5), BackColor = Color.White };
        right.Controls.Add(categoryPanel, 0, 1);
        detailTitleLabel = new Label { Text = "三级执行结果检查 · 结果明细", Dock = DockStyle.Fill, Padding = new Padding(11, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(43, 72, 95) };
        levelMessageLabel = new Label { Visible = false };
        right.Controls.Add(detailTitleLabel, 0, 2);
        var issueHost = new Panel { Dock = DockStyle.Fill };
        issueGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeColumns = true, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, ScrollBars = ScrollBars.Both, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
        issueGrid.RowTemplate.Height = 48;
        issueGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(207, 234, 246); issueGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(25, 49, 73);
        issueGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Operation", HeaderText = "操作", Text = "打开位置  ▼", UseColumnTextForButtonValue = true }); issueGrid.Columns.Add("Status", "状态"); issueGrid.Columns.Add("Category", "具体类别"); issueGrid.Columns.Add("Device", "设备目录"); issueGrid.Columns.Add("File", "TXT 文件"); issueGrid.Columns.Add("Message", "说明"); issueGrid.Columns.Add("Actual", "实际内容"); issueGrid.Columns.Add("Path", "完整路径");
        SetColumn(issueGrid.Columns[0], 125, 110); SetColumn(issueGrid.Columns[1], 75, 65); SetColumn(issueGrid.Columns[2], 190, 130); SetColumn(issueGrid.Columns[3], 230, 140); SetColumn(issueGrid.Columns[4], 250, 140); SetColumn(issueGrid.Columns[5], 390, 220); SetColumn(issueGrid.Columns[6], 320, 180); SetColumn(issueGrid.Columns[7], 450, 230);
        issueGrid.CellContentClick += IssueGrid_CellContentClick;
        emptyMessageLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 12F), ForeColor = Color.FromArgb(96, 117, 138), BackColor = Color.White, Visible = false };
        issueHost.Controls.Add(emptyMessageLabel); issueHost.Controls.Add(issueGrid); right.Controls.Add(issueHost, 0, 3); split.Panel2.Controls.Add(right);

        operationMenu = new ContextMenuStrip();
        operationMenu.Items.Add("打开位置", null, OperationOpen_Click);
        operationMenu.Items.Add("查看详情", null, OperationDetails_Click);
        operationMenu.Items.Add("复制信息", null, OperationCopy_Click);
        return panel;
    }

    private Panel MakeHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(21, 35, 59) };
        header.Controls.Add(new Label { Text = "✓", ForeColor = Color.FromArgb(117, 206, 183), BackColor = Color.FromArgb(21, 45, 72), BorderStyle = BorderStyle.FixedSingle, Font = new Font(Font.FontFamily, 16F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Size = new Size(38, 38), Location = new Point(22, 18) });
        header.Controls.Add(new Label { Text = "设备检查工具", ForeColor = Color.White, Font = new Font(Font.FontFamily, 14F, FontStyle.Bold), AutoSize = true, Location = new Point(72, 16) });
        header.Controls.Add(new Label { Text = "设备目录、命令数目、执行结果，按三级顺序检查", ForeColor = Color.FromArgb(189, 204, 218), Font = new Font(Font.FontFamily, 8.5F), AutoSize = true, Location = new Point(73, 43) });
        return header;
    }

    private Button MakeLevelButton(string title) => new() { Text = title + "\r\n—", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 5, 0), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(21, 35, 59), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0), Font = new Font(Font, FontStyle.Bold), Cursor = Cursors.Hand };

    private Button MakeButton(string text, Color backColor, Color foreColor, int width) => new() { Text = text, Width = width, Height = 36, Margin = new Padding(4, 0, 4, 0), FlatStyle = FlatStyle.Flat, BackColor = backColor, ForeColor = foreColor, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };

    private static void SetColumn(DataGridViewColumn column, int width, int minimumWidth) { column.Width = width; column.MinimumWidth = minimumWidth; column.Resizable = DataGridViewTriState.True; }
}
