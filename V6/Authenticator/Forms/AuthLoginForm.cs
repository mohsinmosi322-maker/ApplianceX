using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Authenticator.Forms
{
    /// <summary>
    /// Authenticator must not open without login.
    /// Default password: master123 (change after first login via main form if needed).
    /// Password hash stored in auth_master.dat next to the exe.
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
            this.ClientSize = new Size(380, 220);
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

            this.Controls.Add(new Label
            {
                Text = "Default: master123",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Location = new Point(40, 180),
                Size = new Size(300, 20)
            });
        }

        private void TryLogin()
        {
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Enter password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (Verify(txtPassword.Text))
            {
                LoginSuccess = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid master password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
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
