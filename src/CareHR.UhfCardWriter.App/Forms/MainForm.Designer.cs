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
    private const int DebugRowIndex = 10;
    private const int ActionRowIndex = 11;
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
            Margin = new Padding(0, 0, 8, 0),
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
            Padding = new Padding(0, 6, 0, 4),
            BackColor = UiColors.White,
            Margin = new Padding(0),
        };
        buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        btnConnect = CreateActionButton("Connect", UiColors.CareHrBlue, UiGlyphs.Connect);
        btnStart = CreateActionButton("Start", UiColors.Success, UiGlyphs.Play);
        btnStop = CreateActionButton("Stop", UiColors.Error, UiGlyphs.Stop);
        btnConnect.Click += BtnConnect_Click;
        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnConnect.Margin = new Padding(0, 2, 0, 2);
        buttonBar.Controls.Add(btnStart, 0, 0);
        buttonBar.Controls.Add(btnStop, 1, 0);

        // Rows: Reader, Connection, Out Interface, RF Power, Hospital, Card Type,
        // Batch, Start, End, Current, Debug, Actions, filler
        leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 13,
            Padding = new Padding(14, 12, 14, 10),
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (var i = 0; i < 10; i++)
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0)); // debug
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); // Start/Stop
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblReader = CreateFieldLabel(UiGlyphs.Reader, "Reader");
        var readerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4),
        };
        readerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        readerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        cboReader = CreateCombo();
        lblReaderStatus = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Offline",
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = UiColors.TextMuted,
            TextAlign = ContentAlignment.MiddleRight,
        };
        readerRow.Controls.Add(cboReader, 0, 0);
        readerRow.Controls.Add(lblReaderStatus, 1, 0);
        leftLayout.Controls.Add(lblReader, 0, 0);
        leftLayout.Controls.Add(readerRow, 1, 0);

        lblConnection = CreateFieldLabel(UiGlyphs.Connect, "Connection");
        leftLayout.Controls.Add(lblConnection, 0, 1);
        leftLayout.Controls.Add(WrapInput(btnConnect), 1, 1);

        lblOutInterface = CreateFieldLabel(UiGlyphs.Reader, "Out Interface");
        var outInterfaceRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4),
        };
        outInterfaceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        outInterfaceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        outInterfaceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        cboOutInterface = CreateCombo();
        cboOutInterface.DropDownStyle = ComboBoxStyle.DropDownList;
        cboOutInterface.DisplayMember = nameof(OutInterfaceListItem.Name);
        cboOutInterface.ValueMember = nameof(OutInterfaceListItem.Raw);
        cboOutInterface.DataSource = DeviceConstants.OutInterfaceOptions
            .Select(o => new OutInterfaceListItem(o.Name, o.Raw))
            .ToList();
        cboOutInterface.Enabled = false;
        btnGetOutInterface = new Button
        {
            Text = "Get",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.FieldFill,
            ForeColor = UiColors.TextPrimary,
            Enabled = false,
        };
        btnGetOutInterface.FlatAppearance.BorderColor = UiColors.Border;
        btnGetOutInterface.Click += BtnGetOutInterface_Click;
        btnSetOutInterface = new Button
        {
            Text = "Set",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.CareHrBlue,
            ForeColor = Color.White,
            Enabled = false,
        };
        btnSetOutInterface.FlatAppearance.BorderSize = 0;
        btnSetOutInterface.Click += BtnSetOutInterface_Click;
        outInterfaceRow.Controls.Add(cboOutInterface, 0, 0);
        outInterfaceRow.Controls.Add(btnGetOutInterface, 1, 0);
        outInterfaceRow.Controls.Add(btnSetOutInterface, 2, 0);
        leftLayout.Controls.Add(lblOutInterface, 0, 2);
        leftLayout.Controls.Add(outInterfaceRow, 1, 2);

        lblRfPower = CreateFieldLabel(UiGlyphs.Reader, "RF Power (dBm)");
        var rfPowerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4),
        };
        rfPowerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rfPowerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        rfPowerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        cboRfPower = CreateCombo();
        cboRfPower.DropDownStyle = ComboBoxStyle.DropDownList;
        // Populate as int (not byte) so SelectedItem pattern-match / Equals stay consistent.
        for (var p = (int)DeviceConstants.RfPowerMinDbm; p <= DeviceConstants.RfPowerMaxDbm; p++)
            cboRfPower.Items.Add(p);
        cboRfPower.SelectedItem = 26;
        cboRfPower.Enabled = false;
        btnGetRfPower = new Button
        {
            Text = "Get",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.FieldFill,
            ForeColor = UiColors.TextPrimary,
            Enabled = false,
        };
        btnGetRfPower.FlatAppearance.BorderColor = UiColors.Border;
        btnGetRfPower.Click += BtnGetRfPower_Click;
        btnSetRfPower = new Button
        {
            Text = "Set",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiColors.CareHrBlue,
            ForeColor = Color.White,
            Enabled = false,
        };
        btnSetRfPower.FlatAppearance.BorderSize = 0;
        btnSetRfPower.Click += BtnSetRfPower_Click;
        rfPowerRow.Controls.Add(cboRfPower, 0, 0);
        rfPowerRow.Controls.Add(btnGetRfPower, 1, 0);
        rfPowerRow.Controls.Add(btnSetRfPower, 2, 0);
        leftLayout.Controls.Add(lblRfPower, 0, 3);
        leftLayout.Controls.Add(rfPowerRow, 1, 3);

        lblHospital = CreateFieldLabel(UiGlyphs.Hospital, "Hospital");
        cboHospital = CreateCombo();
        leftLayout.Controls.Add(lblHospital, 0, 4);
        leftLayout.Controls.Add(WrapInput(cboHospital), 1, 4);

        lblCardType = CreateFieldLabel(UiGlyphs.CardType, "Card Type");
        cboCardType = CreateCombo();
        leftLayout.Controls.Add(lblCardType, 0, 5);
        leftLayout.Controls.Add(WrapInput(cboCardType), 1, 5);

        lblBatch = CreateFieldLabel(UiGlyphs.Batch, "Batch");
        txtBatch = CreateTextBox();
        leftLayout.Controls.Add(lblBatch, 0, 6);
        leftLayout.Controls.Add(WrapInput(txtBatch), 1, 6);

        lblStart = CreateFieldLabel(UiGlyphs.Start, "Start");
        txtStart = CreateTextBox();
        leftLayout.Controls.Add(lblStart, 0, 7);
        leftLayout.Controls.Add(WrapInput(txtStart), 1, 7);

        lblEnd = CreateFieldLabel(UiGlyphs.End, "End");
        txtEnd = CreateTextBox();
        leftLayout.Controls.Add(lblEnd, 0, 8);
        leftLayout.Controls.Add(WrapInput(txtEnd), 1, 8);

        lblCurrent = CreateFieldLabel(UiGlyphs.Current, "Current");
        txtCurrent = CreateTextBox();
        txtCurrent.ReadOnly = true;
        txtCurrent.BackColor = UiColors.FieldFill;
        txtCurrent.TabStop = false;
        var currentTip = new ToolTip();
        currentTip.SetToolTip(txtCurrent, "Current serial — advances only after Write + Verify + Register success");
        currentTip.SetToolTip(lblCurrent, "Current serial — advances only after Write + Verify + Register success");
        lblCurrentHint = new Label { Visible = false, Height = 0 };
        leftLayout.Controls.Add(lblCurrent, 0, 9);
        leftLayout.Controls.Add(WrapInput(txtCurrent), 1, 9);

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

        leftLayout.SetColumnSpan(buttonBar, 2);
        leftLayout.Controls.Add(buttonBar, 0, ActionRowIndex);

        leftShell.Controls.Add(leftLayout, 0, 0);
        leftPanel.Controls.Add(leftShell);

        cardPreviewPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 132,
            BackColor = UiColors.White,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 8),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var previewLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
        };
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

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
            Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
            ForeColor = UiColors.CareHrBlue,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
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

    private static Control CreateFieldLabel(string glyph, string text)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var icon = UiGlyphs.CreateIconLabel(glyph, 10f, UiColors.CareHrBlue);
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Color.FromArgb(55, 71, 79),
        };
        row.Controls.Add(icon, 0, 0);
        row.Controls.Add(label, 1, 0);
        return row;
    }

    private static Control WrapInput(Control input)
    {
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 4, 0, 4);
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

    private static TextBox CreateTextBox() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10.5f),
        Margin = new Padding(0, 4, 0, 4),
    };

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
