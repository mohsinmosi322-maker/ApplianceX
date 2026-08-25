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

        public LoginForm()
        {
            InitializeComponent();
            UiHelper.InitializeTheme();
            UiHelper.FadeIn(this);
            this.Shown += (s, e) => txtUsername.Focus();
        }

        private void InitializeComponent()
        {
            this.BackColor = UiHelper.BgColor;
            this.ClientSize = new Size(400, 340);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.KeyPreview = true;

            Panel card = new Panel { BackColor = Color.White, Location = new Point(30, 25), Size = new Size(340, 280) };

            Label lblTitle = new Label
            {
                Text = "APPLIANCE MANAGEMENT",
                Font = UiHelper.TitleFont,
                ForeColor = UiHelper.ThemeColor,
                Location = new Point(10, 20),
                Size = new Size(320, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblUser = new Label { Text = "Username", Font = UiHelper.NormalFont, Location = new Point(30, 70), Size = new Size(100, 20) };
            txtUsername = new TextBox { Location = new Point(30, 92), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtUsername);
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    txtPassword.Focus();
                }
            };

            Label lblPass = new Label { Text = "Password", Font = UiHelper.NormalFont, Location = new Point(30, 130), Size = new Size(100, 20) };
            txtPassword = new TextBox { Location = new Point(30, 152), Size = new Size(280, 28), PasswordChar = '●' };
            UiHelper.StyleTextBox(txtPassword);
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    btnLogin.Focus();
                }
            };

            btnLogin = new Button { Text = "LOGIN", Location = new Point(30, 200), Size = new Size(280, 40) };
            UiHelper.StyleButton(btnLogin);
            btnLogin.Click += (s, e) => DoLogin();
            btnLogin.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    DoLogin();
                }
            };

            card.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUsername, lblPass, txtPassword, btnLogin });
            this.Controls.Add(card);
        }

        private void DoLogin()
        {
            try
            {
                if (!LicenseReader.TryLoad())
                {
                    MessageBox.Show(
                        "license.dat not found.\n\nApplication cannot start without a valid license.\nContact vendor and place license.dat next to the application.",
                        "License Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!LicenseReader.IsValid())
                {
                    MessageBox.Show(
                        "License expired on " + LicenseReader.Current.ExpiryDate.ToString("dd/MM/yyyy") + ".\n\nContact vendor to renew.",
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
