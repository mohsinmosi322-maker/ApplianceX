using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class LoginForm : Form
    {
        private TextBox txtUsername, txtPassword;
        private Button btnLogin;
        private Label lblTitle, lblDbStatus;

        public LoginForm()
        {
            // Must already be loaded in Program.Main — re-check for safety
            if (!LicenseReader.TryLoad() || !LicenseReader.IsValid())
            {
                MessageBox.Show(
                    "Valid license.dat is required to run this software.\n\nPath:\n" + LicenseReader.LicensePath,
                    "License Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Load += (s, e) => { Close(); };
            }

            InitializeComponent();
            UiHelper.InitializeTheme();
            ApplyLicenseBranding();
            UiHelper.FadeIn(this);
            this.Shown += (s, e) =>
            {
                CheckDb();
                txtUsername.Focus();
            };
        }

        private void ApplyLicenseBranding()
        {
            string name = UiHelper.AppName;
            if (string.IsNullOrWhiteSpace(name)) name = "APPLIANCE X";
            if (lblTitle != null) lblTitle.Text = name.ToUpperInvariant();
            this.Text = "Sign in — " + name;
        }

        private void CheckDb()
        {
            if (lblDbStatus == null) return;
            string err;
            if (DbHelper.TryOpen(out err))
            {
                lblDbStatus.Text = "Database: connected";
                lblDbStatus.ForeColor = Color.FromArgb(27, 94, 32);
            }
            else
            {
                lblDbStatus.Text = "Database: not reachable";
                lblDbStatus.ForeColor = Color.FromArgb(183, 28, 28);
                AppLog.Warn("DB check failed: " + err);
            }
        }

        private void InitializeComponent()
        {
            this.BackColor = UiHelper.ThemeDark;
            this.ClientSize = new Size(460, 440);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Sign in";
            this.KeyPreview = true;

            Panel card = new Panel
            {
                BackColor = Color.White,
                Location = new Point(40, 36),
                Size = new Size(380, 360)
            };
            card.Paint += (s, e) =>
            {
                using (var p = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            Panel accent = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = UiHelper.ThemeColor };
            card.Controls.Add(accent);

            lblTitle = new Label
            {
                Text = "APPLIANCE X",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = UiHelper.ThemeDark,
                Location = new Point(20, 28),
                Size = new Size(340, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label lblSub = new Label
            {
                Text = "Sign in to continue",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(110, 122, 136),
                Location = new Point(20, 60),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblUser = new Label { Text = "Username", Font = UiHelper.SmallFont, ForeColor = Color.FromArgb(80, 90, 100), Location = new Point(36, 100), Size = new Size(300, 18) };
            txtUsername = new TextBox { Location = new Point(36, 120), Size = new Size(308, 30) };
            UiHelper.StyleTextBox(txtUsername);
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPassword.Focus(); }
            };

            Label lblPass = new Label { Text = "Password", Font = UiHelper.SmallFont, ForeColor = Color.FromArgb(80, 90, 100), Location = new Point(36, 164), Size = new Size(300, 18) };
            txtPassword = new TextBox { Location = new Point(36, 184), Size = new Size(308, 30), PasswordChar = '●' };
            UiHelper.StyleTextBox(txtPassword);
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoLogin(); }
            };

            btnLogin = new Button { Text = "SIGN IN", Location = new Point(36, 240), Size = new Size(308, 44) };
            UiHelper.StyleButton(btnLogin);
            btnLogin.Click += (s, e) => DoLogin();

            lblDbStatus = new Label
            {
                Text = "Database: checking…",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(140, 150, 160),
                Location = new Point(20, 295),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label hint = new Label
            {
                Text = "Requires license.dat next to the application",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(140, 150, 160),
                Location = new Point(20, 318),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.AddRange(new Control[] { lblTitle, lblSub, lblUser, txtUsername, lblPass, txtPassword, btnLogin, lblDbStatus, hint });
            this.Controls.Add(card);
        }

        private void DoLogin()
        {
            try
            {
                if (!LicenseReader.TryLoad() || !LicenseReader.IsValid())
                {
                    MessageBox.Show(
                        "Valid license.dat is required.\n\n" + LicenseReader.LicensePath,
                        "License Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ApplyLicenseBranding();

                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Please enter username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (string.IsNullOrWhiteSpace(txtUsername.Text)) txtUsername.Focus();
                    else txtPassword.Focus();
                    return;
                }

                btnLogin.Enabled = false;
                var user = new UserRepository().Authenticate(txtUsername.Text.Trim(), txtPassword.Text);
                if (user != null)
                {
                    AppLog.Info("Login success: " + user.UserName);
                    AppSession.SignIn(user);
                    this.Hide();
                    var main = new MainForm(user);
                    main.FormClosed += (s, args) => this.Close();
                    main.Show();
                }
                else
                {
                    AppLog.Warn("Login failed for user: " + txtUsername.Text.Trim());
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Login / DB error", ex);
                MessageBox.Show("Unable to connect to database.\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CheckDb();
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }
}
