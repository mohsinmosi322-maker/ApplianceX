using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Authenticator.Forms
{
    /// <summary>
    /// Master gate for Authenticator. Default password only accepted when auth_master.dat is missing;
    /// user is then forced to set a new password (not shown on UI).
    /// </summary>
    public class AuthLoginForm : Form
    {
        private TextBox txtPassword;
        private Button btnLogin;
        private const string DefaultPassword = "master123";

        private static string MasterFile
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth_master.dat"); }
        }

        public bool LoginSuccess { get; private set; }

        public AuthLoginForm()
        {
            InitializeComponent();
            this.Shown += (s, e) => txtPassword.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Authenticator Login";
            this.ClientSize = new Size(380, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.KeyPreview = true;

            Label title = new Label
            {
                Text = "AUTHENTICATOR",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Location = new Point(20, 20),
                Size = new Size(340, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(title);

            Label sub = new Label
            {
                Text = "Master password required",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(20, 52),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(sub);

            this.Controls.Add(new Label
            {
                Text = "Password:",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(40, 90),
                Size = new Size(90, 22)
            });

            txtPassword = new TextBox
            {
                Location = new Point(130, 88),
                Size = new Size(200, 26),
                PasswordChar = '●',
                Font = new Font("Segoe UI", 10F)
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryLogin();
                }
            };
            this.Controls.Add(txtPassword);

            btnLogin = new Button
            {
                Text = "LOGIN",
                Location = new Point(130, 130),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => TryLogin();
            this.Controls.Add(btnLogin);
        }

        private void TryLogin()
        {
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Enter password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            bool firstRun = !File.Exists(MasterFile);
            if (!Verify(txtPassword.Text))
            {
                MessageBox.Show("Invalid master password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            if (firstRun)
            {
                if (!PromptNewMasterPassword())
                    return;
            }

            LoginSuccess = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool PromptNewMasterPassword()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Set Master Password";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.ClientSize = new Size(360, 180);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label
                {
                    Text = "First run: choose a new master password (min 8 characters).",
                    Location = new Point(20, 15),
                    Size = new Size(320, 40)
                };
                var t1 = new TextBox { Location = new Point(20, 60), Size = new Size(310, 26), PasswordChar = '●' };
                var t2 = new TextBox { Location = new Point(20, 95), Size = new Size(310, 26), PasswordChar = '●' };
                var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(150, 135), Size = new Size(90, 30) };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(250, 135), Size = new Size(80, 30) };
                dlg.Controls.AddRange(new Control[] { lbl, t1, t2, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                if (string.IsNullOrEmpty(t1.Text) || t1.Text.Length < 8)
                {
                    MessageBox.Show("Password must be at least 8 characters.");
                    return false;
                }
                if (t1.Text != t2.Text)
                {
                    MessageBox.Show("Passwords do not match.");
                    return false;
                }
                if (t1.Text == DefaultPassword)
                {
                    MessageBox.Show("Choose a password different from the installation default.");
                    return false;
                }

                SetMasterPassword(t1.Text);
                MessageBox.Show("Master password saved. Keep it secure.", "Authenticator", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
        }

        private static bool Verify(string password)
        {
            string hash = Hash(password);
            if (!File.Exists(MasterFile))
                return password == DefaultPassword;

            try
            {
                string stored = File.ReadAllText(MasterFile).Trim();
                return string.Equals(stored, hash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return password == DefaultPassword;
            }
        }

        public static void SetMasterPassword(string newPassword)
        {
            File.WriteAllText(MasterFile, Hash(newPassword));
        }

        private static string Hash(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("AuthMaster|" + raw));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpper();
            }
        }
    }
}
