using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class LoginForm : Form
    {
        private TextBox txtUsername, txtPassword;
        private Button btnLogin;

        public LoginForm()
        {
            InitializeComponent();
            UiHelper.InitializeTheme();
            UiHelper.FadeIn(this);
            this.Shown += (s, e) => txtUsername.Focus();
        }

        private void InitializeComponent()
        {
            this.BackColor = UiHelper.ThemeDark;
            this.ClientSize = new Size(460, 420);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Sign in";
            this.KeyPreview = true;

            Panel card = new Panel
            {
                BackColor = Color.White,
                Location = new Point(40, 36),
                Size = new Size(380, 340)
            };
            card.Paint += (s, e) =>
            {
                using (var p = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            Panel accent = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = UiHelper.ThemeColor };
            card.Controls.Add(accent);

            Label lblTitle = new Label
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

            Label hint = new Label
            {
                Text = "License file required next to the application.",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(140, 150, 160),
                Location = new Point(20, 300),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.AddRange(new Control[] { lblTitle, lblSub, lblUser, txtUsername, lblPass, txtPassword, btnLogin, hint });
            this.Controls.Add(card);
        }

        private void DoLogin()
        {
            try
            {
                if (!LicenseReader.TryLoad())
                {
                    MessageBox.Show(
                        "license.dat not found.\n\nPlace a valid license next to the application.",
                        "License Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!LicenseReader.IsValid())
                {
                    MessageBox.Show(
                        "License expired on " + LicenseReader.Current.ExpiryDate.ToString("dd/MM/yyyy") + ".",
                        "License Expired", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Please enter username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (string.IsNullOrWhiteSpace(txtUsername.Text)) txtUsername.Focus();
                    else txtPassword.Focus();
                    return;
                }

                var user = new UserRepository().Authenticate(txtUsername.Text.Trim(), txtPassword.Text);
                if (user != null)
                {
                    AppLog.Info("Login success: " + user.UserName);
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
            }
        }
    }
}
