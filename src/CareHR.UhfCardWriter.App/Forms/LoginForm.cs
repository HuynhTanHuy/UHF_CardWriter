using CareHR.UhfCardWriter.App.Presentation;
using CareHR.UhfCardWriter.Application.Abstractions;

namespace CareHR.UhfCardWriter.App.Forms;

/// <summary>
/// Minimal CareHR login dialog. On success sets <see cref="IWriterAuthSession"/> and returns OK.
/// Does not open <see cref="MainForm"/>.
/// </summary>
public sealed class LoginForm : Form
{
    private readonly ICareHrLoginClient _loginClient;
    private readonly IWriterAuthSession _authSession;

    private readonly TextBox _txtUsername = new();
    private readonly TextBox _txtPassword = new();
    private readonly Button _btnLogin = new();
    private readonly Label _lblStatus = new();

    public LoginForm(ICareHrLoginClient loginClient, IWriterAuthSession authSession)
    {
        _loginClient = loginClient ?? throw new ArgumentNullException(nameof(loginClient));
        _authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));

        Text = "CareHR UHF Card Writer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(420, 280);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = UiColors.Canvas;
        AcceptButton = _btnLogin;

        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(28, 20, 28, 16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var title = new Label
        {
            Text = "CareHR UHF Card Writer",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12f),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = UiColors.CareHrBlue,
        };

        var lblUser = new Label
        {
            Text = "Tên đăng nhập",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
        };

        _txtUsername.Dock = DockStyle.Fill;
        _txtUsername.BorderStyle = BorderStyle.FixedSingle;

        var lblPass = new Label
        {
            Text = "Mật khẩu",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
        };

        _txtPassword.Dock = DockStyle.Fill;
        _txtPassword.BorderStyle = BorderStyle.FixedSingle;
        _txtPassword.UseSystemPasswordChar = true;

        var buttonPanel = new Panel { Dock = DockStyle.Fill };
        _btnLogin.Text = "Đăng nhập";
        _btnLogin.Size = new Size(140, 34);
        _btnLogin.Anchor = AnchorStyles.None;
        _btnLogin.BackColor = UiColors.CareHrBlue;
        _btnLogin.ForeColor = Color.White;
        _btnLogin.FlatStyle = FlatStyle.Flat;
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Cursor = Cursors.Hand;
        _btnLogin.Click += async (_, _) => await LoginAsync().ConfigureAwait(true);
        buttonPanel.Controls.Add(_btnLogin);
        buttonPanel.Resize += (_, _) =>
        {
            _btnLogin.Left = (buttonPanel.ClientSize.Width - _btnLogin.Width) / 2;
            _btnLogin.Top = (buttonPanel.ClientSize.Height - _btnLogin.Height) / 2;
        };

        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.TextAlign = ContentAlignment.TopCenter;
        _lblStatus.ForeColor = UiColors.TextMuted;
        _lblStatus.Text = "Status: Chưa đăng nhập";

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 1);
        root.Controls.Add(lblUser, 0, 2);
        root.Controls.Add(_txtUsername, 0, 3);
        root.Controls.Add(lblPass, 0, 4);
        root.Controls.Add(_txtPassword, 0, 5);
        root.Controls.Add(buttonPanel, 0, 6);
        root.Controls.Add(_lblStatus, 0, 7);

        Controls.Add(root);

        Shown += (_, _) => _txtUsername.Focus();
    }

    private async Task LoginAsync()
    {
        if (!_btnLogin.Enabled)
            return;

        var username = _txtUsername.Text?.Trim() ?? string.Empty;
        var password = _txtPassword.Text ?? string.Empty;

        _btnLogin.Enabled = false;
        _txtUsername.Enabled = false;
        _txtPassword.Enabled = false;
        _lblStatus.ForeColor = UiColors.TextMuted;
        _lblStatus.Text = "Status: Đang đăng nhập…";

        try
        {
            var result = await _loginClient.LoginAsync(username, password).ConfigureAwait(true);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Token))
            {
                _lblStatus.ForeColor = UiColors.Error;
                _lblStatus.Text = "Status: " + (string.IsNullOrWhiteSpace(result.Message)
                    ? "Đăng nhập thất bại."
                    : result.Message);
                _txtPassword.SelectAll();
                _txtPassword.Focus();
                return;
            }

            _authSession.SetToken(result.Token);
            _lblStatus.ForeColor = UiColors.Success;
            _lblStatus.Text = "Status: Đăng nhập thành công";
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            _lblStatus.ForeColor = UiColors.Error;
            _lblStatus.Text = "Status: Đăng nhập thất bại do lỗi kết nối.";
        }
        finally
        {
            if (!IsDisposed && DialogResult != DialogResult.OK)
            {
                _btnLogin.Enabled = true;
                _txtUsername.Enabled = true;
                _txtPassword.Enabled = true;
            }
        }
    }
}
