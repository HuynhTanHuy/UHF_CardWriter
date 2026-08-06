using CareHR.UhfCardWriter.App.Controls;

namespace CareHR.UhfCardWriter.App.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private Panel leftPanel = null!;
    private Panel rightPanel = null!;
    private TableLayoutPanel leftLayout = null!;
    private TableLayoutPanel rightLayout = null!;
    private Label lblBrand = null!;
    private Label lblReader = null!;
    private ComboBox cboReader = null!;
    private Label lblHospital = null!;
    private ComboBox cboHospital = null!;
    private Label lblCardType = null!;
    private ComboBox cboCardType = null!;
    private Label lblStart = null!;
    private TextBox txtStart = null!;
    private Label lblEnd = null!;
    private TextBox txtEnd = null!;
    private Label lblCurrent = null!;
    private TextBox txtCurrent = null!;
    private Label lblTargetEpc = null!;
    private TextBox txtTargetEpc = null!;
    private Label lblCurrentEpc = null!;
    private TextBox txtCurrentEpc = null!;
    private Label lblBatch = null!;
    private TextBox txtBatch = null!;
    private FlowLayoutPanel buttonBar = null!;
    private Button btnConnect = null!;
    private Button btnScan = null!;
    private Button btnWrite = null!;
    private Button btnCancel = null!;
    private Button btnRefresh = null!;
    private Button btnSettings = null!;
    private StatusPanel statusPanel = null!;
    private WorkflowProgressControl workflowProgress = null!;
    private GroupBox grpResult = null!;
    private Label lblResultReader = null!;
    private Label lblResultHospital = null!;
    private Label lblResultCardType = null!;
    private Label lblResultSerial = null!;
    private Label lblResultCurrentEpc = null!;
    private Label lblResultTargetEpc = null!;
    private OperationLogControl operationLog = null!;
    private Label lblIllustration = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        Text = "CareHR UHF Card Writer";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.75f);
        BackColor = Color.FromArgb(245, 247, 248);

        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12),
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

        leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4), BackColor = Color.White };
        rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4), BackColor = Color.White };

        // --- Left ---
        leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 12,
            Padding = new Padding(12),
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (var i = 0; i < 11; i++)
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblBrand = new Label
        {
            Text = "CareHR  ·  UHF Card Writer",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(13, 115, 119),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        leftLayout.SetColumnSpan(lblBrand, 2);
        leftLayout.Controls.Add(lblBrand, 0, 0);

        lblReader = CreateFieldLabel("Reader");
        cboReader = CreateCombo();
        leftLayout.Controls.Add(lblReader, 0, 1);
        leftLayout.Controls.Add(cboReader, 1, 1);

        lblHospital = CreateFieldLabel("Hospital");
        cboHospital = CreateCombo();
        leftLayout.Controls.Add(lblHospital, 0, 2);
        leftLayout.Controls.Add(cboHospital, 1, 2);

        lblCardType = CreateFieldLabel("Card type");
        cboCardType = CreateCombo();
        leftLayout.Controls.Add(lblCardType, 0, 3);
        leftLayout.Controls.Add(cboCardType, 1, 3);

        lblStart = CreateFieldLabel("Start #");
        txtStart = CreateTextBox();
        leftLayout.Controls.Add(lblStart, 0, 4);
        leftLayout.Controls.Add(txtStart, 1, 4);

        lblEnd = CreateFieldLabel("End #");
        txtEnd = CreateTextBox();
        leftLayout.Controls.Add(lblEnd, 0, 5);
        leftLayout.Controls.Add(txtEnd, 1, 5);

        lblCurrent = CreateFieldLabel("Current #");
        txtCurrent = CreateTextBox();
        leftLayout.Controls.Add(lblCurrent, 0, 6);
        leftLayout.Controls.Add(txtCurrent, 1, 6);

        lblTargetEpc = CreateFieldLabel("Target EPC");
        txtTargetEpc = CreateTextBox();
        txtTargetEpc.Font = new Font("Consolas", 10f);
        leftLayout.Controls.Add(lblTargetEpc, 0, 7);
        leftLayout.Controls.Add(txtTargetEpc, 1, 7);

        lblCurrentEpc = CreateFieldLabel("Current EPC");
        txtCurrentEpc = CreateTextBox();
        txtCurrentEpc.ReadOnly = true;
        txtCurrentEpc.BackColor = Color.FromArgb(236, 239, 241);
        txtCurrentEpc.Font = new Font("Consolas", 10f);
        leftLayout.Controls.Add(lblCurrentEpc, 0, 8);
        leftLayout.Controls.Add(txtCurrentEpc, 1, 8);

        lblBatch = CreateFieldLabel("Batch");
        txtBatch = CreateTextBox();
        leftLayout.Controls.Add(lblBatch, 0, 9);
        leftLayout.Controls.Add(txtBatch, 1, 9);

        buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
        };
        leftLayout.SetColumnSpan(buttonBar, 2);

        btnConnect = CreateActionButton("Connect (F5)", Color.FromArgb(13, 115, 119));
        btnScan = CreateActionButton("Scan (F6)", Color.FromArgb(2, 119, 189));
        btnWrite = CreateActionButton("Write (F7)", Color.FromArgb(46, 125, 50));
        btnCancel = CreateActionButton("Cancel (Esc)", Color.FromArgb(198, 40, 40));
        btnRefresh = CreateActionButton("Refresh", Color.FromArgb(69, 90, 100));
        btnSettings = CreateActionButton("Settings", Color.FromArgb(69, 90, 100));

        btnConnect.Click += BtnConnect_Click;
        btnScan.Click += BtnScan_Click;
        btnWrite.Click += BtnWrite_Click;
        btnCancel.Click += BtnCancel_Click;
        btnRefresh.Click += BtnRefresh_Click;
        btnSettings.Click += BtnSettings_Click;

        buttonBar.Controls.AddRange(new Control[] { btnConnect, btnScan, btnWrite, btnCancel, btnRefresh, btnSettings });
        leftLayout.Controls.Add(buttonBar, 0, 10);

        leftPanel.Controls.Add(leftLayout);

        // --- Right ---
        rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8),
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        workflowProgress = new WorkflowProgressControl { Dock = DockStyle.Fill };
        statusPanel = new StatusPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8) };

        lblIllustration = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = Color.FromArgb(13, 115, 119),
            BackColor = Color.FromArgb(232, 245, 245),
            Text = "UHF Desk Reader\nPlace exactly one CareHR card in the field",
            Margin = new Padding(0, 0, 0, 8),
        };

        grpResult = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Result",
            Font = new Font("Segoe UI Semibold", 9.75f),
            Padding = new Padding(10),
        };
        var resultLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
        };
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        lblResultReader = AddResultRow(resultLayout, 0, "Reader");
        lblResultHospital = AddResultRow(resultLayout, 1, "Hospital");
        lblResultCardType = AddResultRow(resultLayout, 2, "Card type");
        lblResultSerial = AddResultRow(resultLayout, 3, "Serial");
        lblResultCurrentEpc = AddResultRow(resultLayout, 4, "Current EPC");
        lblResultTargetEpc = AddResultRow(resultLayout, 5, "Target EPC");
        grpResult.Controls.Add(resultLayout);

        operationLog = new OperationLogControl { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };

        rightLayout.Controls.Add(workflowProgress, 0, 0);
        rightLayout.Controls.Add(statusPanel, 0, 1);
        rightLayout.Controls.Add(lblIllustration, 0, 2);
        rightLayout.Controls.Add(grpResult, 0, 3);
        rightLayout.Controls.Add(operationLog, 0, 4);

        rightPanel.Controls.Add(rightLayout);

        rootLayout.Controls.Add(leftPanel, 0, 0);
        rootLayout.Controls.Add(rightPanel, 1, 0);
        Controls.Add(rootLayout);

        ResumeLayout(false);
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(55, 71, 79),
    };

    private static ComboBox CreateCombo() => new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.System,
    };

    private static TextBox CreateTextBox() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private static Button CreateActionButton(string text, Color back) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(110, 40),
        Margin = new Padding(0, 0, 8, 8),
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
        Cursor = Cursors.Hand,
    };

    private static Label AddResultRow(TableLayoutPanel layout, int row, string title)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(84, 110, 122),
        }, 0, row);
        var value = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 9f),
        };
        layout.Controls.Add(value, 1, row);
        return value;
    }
}
