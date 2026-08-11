using CareHR.UhfCardWriter.App.Controls;
using CareHR.UhfCardWriter.App.Presentation;
using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.App.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel shellLayout = null!;
    private Panel headerPanel = null!;
    private PictureBox picLogo = null!;
    private Label lblBrand = null!;
    private Control btnSettings = null!;
    private ToolTip tipSettings = null!;
    private Panel bodyPanel = null!;
    private TableLayoutPanel rootLayout = null!;
    private Panel leftPanel = null!;
    private Panel rightPanel = null!;
    private TableLayoutPanel leftLayout = null!;
    private Control lblReader = null!;
    private ComboBox cboReader = null!;
    private Label lblReaderStatus = null!;
    private Control lblConnection = null!;
    private Control lblOutInterface = null!;
    private ComboBox cboOutInterface = null!;
    private Button btnGetOutInterface = null!;
    private Button btnSetOutInterface = null!;
    private Control lblRfPower = null!;
    private ComboBox cboRfPower = null!;
    private Button btnGetRfPower = null!;
    private Button btnSetRfPower = null!;
    private Control lblHospital = null!;
    private ComboBox cboHospital = null!;
    private Control lblCardType = null!;
    private ComboBox cboCardType = null!;
    private Control lblBatch = null!;
    private TextBox txtBatch = null!;
    private Control lblStart = null!;
    private TextBox txtStart = null!;
    private Control lblEnd = null!;
    private TextBox txtEnd = null!;
    private Control lblCurrent = null!;
    private TextBox txtCurrent = null!;
    private Label lblCurrentHint = null!;
    private Panel debugPanel = null!;
    private Control lblTargetEpc = null!;
    private TextBox txtTargetEpc = null!;
    private Control lblCurrentEpc = null!;
    private TextBox txtCurrentEpc = null!;
    private TableLayoutPanel buttonBar = null!;
    private Button btnConnect = null!;
    private Button btnStart = null!;
    private Button btnStop = null!;
    private const int DebugRowIndex = 12;
    private const int ActionHeadingRowIndex = 13;
    private const int ActionRowIndex = 14;
    private Panel cardPreviewPanel = null!;
    private Control lblFactoryCaption = null!;
    private Label lblFactoryEpc = null!;
    private Label lblArrow = null!;
    private Control lblTargetCaption = null!;
    private Label lblTargetCard = null!;
    private StatusPanel statusPanel = null!;
    private BatchResultPanel batchResult = null!;
    private OperationLogControl operationLog = null!;
    private Panel statusBar = null!;
    private Label lblStatusBarLeft = null!;
    private Label lblStatusBarRight = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tipSettings = new ToolTip(components);
        SuspendLayout();

        Text = "CareHR UHF Card Writer";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(1200, 720);
        MinimumSize = new Size(1024, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        BackColor = UiColors.Canvas;
        try
        {
            var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "carehr-logo.ico");
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);
        }
        catch
        {
            // optional
        }

        shellLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
        };
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        // --- Header ---
        headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiColors.White,
            Padding = new Padding(12, 0, 8, 0),
        };

        var settingsHost = new Panel
        {
            Width = 40,
            Dock = DockStyle.Right,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0),
        };

        var settingsBtn = new Button
        {
            Text = string.Empty,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            TabStop = false,
            ImageAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        settingsBtn.FlatAppearance.BorderSize = 0;
        settingsBtn.UseVisualStyleBackColor = false;
        settingsBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 239, 241);
        var gearImg = UiGlyphs.CreateGearImage(22, Color.FromArgb(84, 110, 122));
        var gearHover = UiGlyphs.CreateGearImage(22, UiColors.CareHrBlue);
        settingsBtn.Image = gearImg;
        tipSettings.SetToolTip(settingsBtn, "Settings");
        settingsBtn.Click += (_, _) => BtnSettings_Click(settingsBtn, EventArgs.Empty);
        settingsBtn.MouseEnter += (_, _) => settingsBtn.Image = gearHover;
        settingsBtn.MouseLeave += (_, _) => settingsBtn.Image = gearImg;
        btnSettings = settingsBtn;
        settingsHost.Controls.Add(settingsBtn);

        var brandHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };

        picLogo = new PictureBox
        {
            Size = new Size(32, 32),
            Location = new Point(0, 10),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        try
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "carehr-logo.png");
            if (File.Exists(logoPath))
                picLogo.Image = Image.FromFile(logoPath);
        }
        catch
        {
            // optional
        }

        lblBrand = new Label
        {
            Text = "CareHR UHF Card Writer",
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = UiColors.CareHrBlue,
            AutoSize = true,
            Location = new Point(40, 14),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        brandHost.Controls.Add(picLogo);
        brandHost.Controls.Add(lblBrand);
        void AlignBrand()
        {
            var mid = Math.Max(0, (brandHost.ClientSize.Height - picLogo.Height) / 2);
            picLogo.Top = mid;
            lblBrand.Top = mid + Math.Max(0, (picLogo.Height - lblBrand.PreferredHeight) / 2);
            lblBrand.Left = picLogo.Right + 8;
        }
        brandHost.Resize += (_, _) => AlignBrand();
        AlignBrand();

        // Fill first, then Right — higher z-order docks to the outer edge first.
        headerPanel.Controls.Add(brandHost);
        headerPanel.Controls.Add(settingsHost);

        // --- Body ---
        bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = UiColors.Canvas,
        };

        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));

        leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiColors.White,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 10, 0),
            BorderStyle = BorderStyle.FixedSingle,
        };
        var leftShell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        leftShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
        };

        buttonBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 2),
            BackColor = UiColors.White,
            Margin = new Padding(0, 4, 0, 0),
        };
        buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        btnConnect = CreateActionButton("Connect", UiColors.CareHrBlue, UiGlyphs.Connect);
        btnStart = CreateActionButton("Start", UiColors.CareHrBlue, UiGlyphs.Play);
        btnStop = CreateActionButton("Stop", Color.FromArgb(170, 55, 55), UiGlyphs.Stop);
        btnConnect.Click += BtnConnect_Click;
        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnConnect.Margin = new Padding(0, 2, 0, 2);
        buttonBar.Controls.Add(btnStart, 0, 0);
        buttonBar.Controls.Add(btnStop, 1, 0);

        // Rows: Reader heading, Reader, Connection, Out Interface, RF Power,
        // Card heading, Hospital, Card Type, Batch, Start, End, Current,
        // Debug, Actions heading, Start/Stop, filler
        leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 16,
            Padding = new Padding(16, 12, 16, 12),
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // READER heading
        for (var i = 0; i < 4; i++)
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // CARD heading
        for (var i = 0; i < 6; i++)
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0)); // debug
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); // ACTIONS heading
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // Start/Stop
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var readerHeading = CreateGroupHeading("READER & CONNECTION");
        leftLayout.SetColumnSpan(readerHeading, 2);
        leftLayout.Controls.Add(readerHeading, 0, 0);

        lblReader = CreateFieldLabel(UiGlyphs.Reader, "Reader");
        var readerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(6, 4, 0, 4),
        };
        readerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        readerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        cboReader = CreateCombo();
        lblReaderStatus = new Label
        {
            Dock = DockStyle.Fill,
            Text = "○ Offline",
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = UiColors.TextMuted,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(4, 0, 0, 0),
        };
        readerRow.Controls.Add(cboReader, 0, 0);
        readerRow.Controls.Add(lblReaderStatus, 1, 0);
        leftLayout.Controls.Add(lblReader, 0, 1);
        leftLayout.Controls.Add(readerRow, 1, 1);

        lblConnection = CreateFieldLabel(UiGlyphs.Connect, "Connection");
        leftLayout.Controls.Add(lblConnection, 0, 2);
        leftLayout.Controls.Add(WrapInput(btnConnect), 1, 2);

        lblOutInterface = CreateFieldLabel(UiGlyphs.Reader, "Out Interface");
        var outInterfaceRow = CreateGetSetRow(out var outCombo, out var outGet, out var outSet);
        cboOutInterface = outCombo;
        btnGetOutInterface = outGet;
        btnSetOutInterface = outSet;
        cboOutInterface.DisplayMember = nameof(OutInterfaceListItem.Name);
        cboOutInterface.ValueMember = nameof(OutInterfaceListItem.Raw);
        cboOutInterface.DataSource = DeviceConstants.OutInterfaceOptions
            .Select(o => new OutInterfaceListItem(o.Name, o.Raw))
            .ToList();
        cboOutInterface.Enabled = false;
        btnGetOutInterface.Enabled = false;
        btnSetOutInterface.Enabled = false;
        btnGetOutInterface.Click += BtnGetOutInterface_Click;
        btnSetOutInterface.Click += BtnSetOutInterface_Click;
        leftLayout.Controls.Add(lblOutInterface, 0, 3);
        leftLayout.Controls.Add(outInterfaceRow, 1, 3);

        lblRfPower = CreateFieldLabel(UiGlyphs.Reader, "RF Power (dBm)");
        var rfPowerRow = CreateGetSetRow(out var rfCombo, out var rfGet, out var rfSet);
        cboRfPower = rfCombo;
        btnGetRfPower = rfGet;
        btnSetRfPower = rfSet;
        for (var p = (int)DeviceConstants.RfPowerMinDbm; p <= DeviceConstants.RfPowerMaxDbm; p++)
            cboRfPower.Items.Add(p);
        cboRfPower.SelectedItem = 26;
        cboRfPower.Enabled = false;
        btnGetRfPower.Enabled = false;
        btnSetRfPower.Enabled = false;
        btnGetRfPower.Click += BtnGetRfPower_Click;
        btnSetRfPower.Click += BtnSetRfPower_Click;
        leftLayout.Controls.Add(lblRfPower, 0, 4);
        leftLayout.Controls.Add(rfPowerRow, 1, 4);

        var cardHeading = CreateGroupHeading("CARD CONFIGURATION");
        leftLayout.SetColumnSpan(cardHeading, 2);
        leftLayout.Controls.Add(cardHeading, 0, 5);

        lblHospital = CreateFieldLabel(UiGlyphs.Hospital, "Hospital");
        cboHospital = CreateCombo();
        leftLayout.Controls.Add(lblHospital, 0, 6);
        leftLayout.Controls.Add(WrapInput(cboHospital), 1, 6);

        lblCardType = CreateFieldLabel(UiGlyphs.CardType, "Card Type");
        cboCardType = CreateCombo();
        leftLayout.Controls.Add(lblCardType, 0, 7);
        leftLayout.Controls.Add(WrapInput(cboCardType), 1, 7);

        lblBatch = CreateFieldLabel(UiGlyphs.Batch, "Batch");
        txtBatch = CreateTextBox();
        leftLayout.Controls.Add(lblBatch, 0, 8);
        leftLayout.Controls.Add(WrapInput(txtBatch), 1, 8);

        lblStart = CreateFieldLabel(UiGlyphs.Start, "Start");
        txtStart = CreateTextBox();
        leftLayout.Controls.Add(lblStart, 0, 9);
        leftLayout.Controls.Add(WrapInput(txtStart), 1, 9);

        lblEnd = CreateFieldLabel(UiGlyphs.End, "End");
        txtEnd = CreateTextBox();
        leftLayout.Controls.Add(lblEnd, 0, 10);
        leftLayout.Controls.Add(WrapInput(txtEnd), 1, 10);

        lblCurrent = CreateFieldLabel(UiGlyphs.Current, "Current");
        txtCurrent = CreateTextBox();
        txtCurrent.ReadOnly = true;
        txtCurrent.BackColor = UiColors.FieldFill;
        txtCurrent.TabStop = false;
        var currentTip = new ToolTip();
        currentTip.SetToolTip(txtCurrent, "Current serial — advances only after Write + Verify + Register success");
        currentTip.SetToolTip(lblCurrent, "Current serial — advances only after Write + Verify + Register success");
        lblCurrentHint = new Label { Visible = false, Height = 0 };
        leftLayout.Controls.Add(lblCurrent, 0, 11);
        leftLayout.Controls.Add(WrapInput(txtCurrent), 1, 11);

        debugPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
        var debugLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
        };
        debugLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        debugLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        debugLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        debugLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        lblTargetEpc = CreateFieldLabel(UiGlyphs.TargetCard, "Target EPC");
        txtTargetEpc = CreateTextBox();
        txtTargetEpc.Font = new Font("Consolas", 9f);
        txtTargetEpc.ReadOnly = true;
        txtTargetEpc.BackColor = UiColors.FieldFill;
        lblCurrentEpc = CreateFieldLabel(UiGlyphs.FactoryCard, "Factory EPC");
        txtCurrentEpc = CreateTextBox();
        txtCurrentEpc.ReadOnly = true;
        txtCurrentEpc.Font = new Font("Consolas", 9f);
        txtCurrentEpc.BackColor = UiColors.FieldFill;
        debugLayout.Controls.Add(lblTargetEpc, 0, 0);
        debugLayout.Controls.Add(txtTargetEpc, 1, 0);
        debugLayout.Controls.Add(lblCurrentEpc, 0, 1);
        debugLayout.Controls.Add(txtCurrentEpc, 1, 1);
        debugPanel.Controls.Add(debugLayout);
        leftLayout.SetColumnSpan(debugPanel, 2);
        leftLayout.Controls.Add(debugPanel, 0, DebugRowIndex);

        var actionsHeading = CreateGroupHeading("ACTIONS");
        leftLayout.SetColumnSpan(actionsHeading, 2);
        leftLayout.Controls.Add(actionsHeading, 0, ActionHeadingRowIndex);

        leftLayout.SetColumnSpan(buttonBar, 2);
        leftLayout.Controls.Add(buttonBar, 0, ActionRowIndex);

        leftShell.Controls.Add(leftLayout, 0, 0);
        leftPanel.Controls.Add(leftShell);

        cardPreviewPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 168,
            BackColor = UiColors.White,
            Padding = new Padding(16, 10, 16, 12),
            Margin = new Padding(0, 0, 0, 8),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var previewLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));  // Factory caption
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));  // Factory EPC
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));  // Arrow
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));  // Target caption
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // Target number — fixed so 28px font is not clipped

        lblFactoryCaption = CreateCenteredCaption(UiGlyphs.FactoryCard, "Factory Card");
        lblFactoryEpc = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            ForeColor = UiColors.TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
        };
        lblArrow = new Label
        {
            Text = UiGlyphs.ArrowDown,
            Dock = DockStyle.Fill,
            Font = UiGlyphs.IconFont(10f),
            ForeColor = UiColors.CareHrBlue,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        lblTargetCaption = CreateCenteredCaption(UiGlyphs.TargetCard, "Target Card");
        lblTargetCard = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 26f, FontStyle.Bold),
            ForeColor = UiColors.CareHrBlue,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false,
            AutoSize = false,
            Padding = new Padding(0, 2, 0, 4),
        };

        previewLayout.Controls.Add(lblFactoryCaption, 0, 0);
        previewLayout.Controls.Add(lblFactoryEpc, 0, 1);
        previewLayout.Controls.Add(lblArrow, 0, 2);
        previewLayout.Controls.Add(lblTargetCaption, 0, 3);
        previewLayout.Controls.Add(lblTargetCard, 0, 4);
        cardPreviewPanel.Controls.Add(previewLayout);

        statusPanel = new StatusPanel
        {
            Dock = DockStyle.Top,
            Height = 210,
            Margin = new Padding(0, 8, 0, 0),
        };
        batchResult = new BatchResultPanel
        {
            Dock = DockStyle.Top,
            Height = BatchResultPanel.PreferredPanelHeight,
            MinimumSize = new Size(0, BatchResultPanel.PreferredPanelHeight),
            Margin = new Padding(0, 8, 0, 8),
        };
        operationLog = new OperationLogControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };

        rightPanel.Controls.Add(operationLog);
        rightPanel.Controls.Add(batchResult);
        rightPanel.Controls.Add(statusPanel);
        rightPanel.Controls.Add(cardPreviewPanel);

        rootLayout.Controls.Add(leftPanel, 0, 0);
        rootLayout.Controls.Add(rightPanel, 1, 0);
        bodyPanel.Controls.Add(rootLayout);

        statusBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(250, 250, 250),
            Padding = new Padding(12, 0, 12, 0),
        };
        lblStatusBarLeft = new Label
        {
            Text = "Reader offline",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = UiColors.TextMuted,
            Location = new Point(12, 6),
        };
        lblStatusBarRight = new Label
        {
            Text = "Version 1.0.0",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = UiColors.TextMuted,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        statusBar.Controls.Add(lblStatusBarLeft);
        statusBar.Controls.Add(lblStatusBarRight);
        statusBar.Resize += (_, _) =>
        {
            lblStatusBarRight.Location = new Point(statusBar.ClientSize.Width - lblStatusBarRight.Width - 12, 6);
        };

        shellLayout.Controls.Add(headerPanel, 0, 0);
        shellLayout.Controls.Add(bodyPanel, 0, 1);
        shellLayout.Controls.Add(statusBar, 0, 2);
        Controls.Add(shellLayout);

        ResumeLayout(false);
    }

    private void SetDebugRowVisible(bool visible)
    {
        debugPanel.Visible = visible;
        if (leftLayout.RowStyles.Count > DebugRowIndex)
            leftLayout.RowStyles[DebugRowIndex].Height = visible ? 72f : 0f;
    }

    private static Control CreateCenteredCaption(string glyph, string text)
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var pair = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18));
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var icon = UiGlyphs.CreateIconLabel(glyph, 8.5f, UiColors.CareHrBlue);
        icon.Size = new Size(18, 18);
        icon.Dock = DockStyle.None;
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5f),
            ForeColor = UiColors.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 1, 0, 0),
        };
        pair.Controls.Add(icon, 0, 0);
        pair.Controls.Add(label, 1, 0);
        host.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 0);
        host.Controls.Add(pair, 1, 0);
        host.Controls.Add(new Panel { Dock = DockStyle.Fill }, 2, 0);
        return host;
    }

    private static Control CreateGroupHeading(string text) => new Label
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
        ForeColor = UiColors.CareHrBlue,
        TextAlign = ContentAlignment.BottomLeft,
        Padding = new Padding(0, 6, 0, 2),
        Margin = new Padding(0),
    };

    private static TableLayoutPanel CreateGetSetRow(out ComboBox combo, out Button getBtn, out Button setBtn)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(6, 4, 0, 4),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));

        combo = CreateCombo();
        getBtn = new Button
        {
            Text = "Get",
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.FieldFill,
            ForeColor = UiColors.TextPrimary,
            Enabled = false,
        };
        getBtn.FlatAppearance.BorderColor = UiColors.Border;
        setBtn = new Button
        {
            Text = "Set",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.CareHrBlue,
            ForeColor = Color.White,
            Enabled = false,
        };
        setBtn.FlatAppearance.BorderSize = 0;
        row.Controls.Add(combo, 0, 0);
        row.Controls.Add(getBtn, 1, 0);
        row.Controls.Add(setBtn, 2, 0);
        return row;
    }

    private static Control CreateFieldLabel(string glyph, string text)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 8, 0),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var icon = UiGlyphs.CreateIconLabel(glyph, 10f, UiColors.CareHrBlue);
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.25f),
            ForeColor = Color.FromArgb(55, 71, 79),
            Padding = new Padding(0, 0, 2, 0),
        };
        row.Controls.Add(icon, 0, 0);
        row.Controls.Add(label, 1, 0);
        return row;
    }

    private static Control WrapInput(Control input)
    {
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(6, 4, 0, 4);
        return input;
    }

    private static ComboBox CreateCombo() => new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.System,
        Font = new Font("Segoe UI", 10f),
        Margin = new Padding(0, 4, 0, 4),
    };

    private static TextBox CreateTextBox()
    {
        var tb = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5f),
            Margin = new Padding(0, 4, 0, 4),
        };
        // WinForms TextBox ignores Padding — use EM_SETMARGINS for 10px inner indent.
        void ApplyMargins(object s, EventArgs e)
        {
            NativeTextMargins.Set(tb, leftPx: 10, rightPx: 10);
            tb.HandleCreated -= ApplyMargins;
        }

        if (tb.IsHandleCreated)
            NativeTextMargins.Set(tb, 10, 10);
        else
            tb.HandleCreated += ApplyMargins;

        return tb;
    }

    private static Button CreateActionButton(string text, Color back, string glyph)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 4, 4, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 8, 0),
            UseVisualStyleBackColor = false,
            FlatAppearance = { BorderSize = 0 },
        };

        var img = UiGlyphs.CreateGlyphImage(glyph, 18, Color.White);
        if (img is not null)
            btn.Image = img;

        return btn;
    }
}

/// <summary>Sets left/right text margins inside a WinForms TextBox (Padding is ignored by TextBox).</summary>
internal static class NativeTextMargins
{
    private const int EmSetMargins = 0x00D3;
    private const int EcLeftMargin = 0x0001;
    private const int EcRightMargin = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public static void Set(TextBox textBox, int leftPx, int rightPx)
    {
        if (textBox is null || !textBox.IsHandleCreated)
            return;

        var margins = (IntPtr)((rightPx << 16) | (leftPx & 0xFFFF));
        SendMessage(textBox.Handle, EmSetMargins, (IntPtr)(EcLeftMargin | EcRightMargin), margins);
    }
}
