using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// Master authenticator: unlocks settings, print control, optional encrypted connection string.
    /// Default master password: master123 (change after first login).
    /// </summary>
    public partial class AuthenticatorForm : Form
    {
        private TextBox txtMaster, txtNewMaster, txtConn, txtMaxAdmin, txtMaxUser;
        private CheckBox chkAllowPrint;
        private const string MasterKeyFile = "auth.dat";
        private const string DefaultMaster = "master123";

        public AuthenticatorForm()
        {
            InitializeComponent();
            LoadValues();
        }

        private void InitializeComponent()
        {
            this.Text = "Authenticator - Protected Settings";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            Label title = new Label
            {
                Text = "AUTHENTICATOR",
                Font = UiHelper.TitleFont,
                ForeColor = UiHelper.ThemeColor,
                Location = new Point(20, 15),
                Size = new Size(460, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(title);

            this.Controls.Add(new Label { Text = "Master Password:", Font = UiHelper.NormalFont, Location = new Point(30, 55), Size = new Size(140, 22) });
            txtMaster = new TextBox { Location = new Point(180, 52), Size = new Size(280, 26), PasswordChar = '●' };
            UiHelper.StyleTextBox(txtMaster);
            this.Controls.Add(txtMaster);

            Button btnUnlock = new Button { Text = "Unlock", Location = new Point(180, 88), Size = new Size(120, 32) };
            UiHelper.StyleButton(btnUnlock);
            btnUnlock.Click += (s, e) => Unlock();
            this.Controls.Add(btnUnlock);

            // Protected panel
            GroupBox gb = new GroupBox { Text = "Protected Options", Font = UiHelper.HeaderFont, Location = new Point(20, 130), Size = new Size(460, 280) };
            gb.Enabled = false;
            gb.Name = "gbProtected";

            chkAllowPrint = new CheckBox { Text = "Allow Bill Printing", Font = UiHelper.NormalFont, Location = new Point(20, 30), Size = new Size(200, 24) };
            gb.Controls.Add(chkAllowPrint);

            gb.Controls.Add(new Label { Text = "Max Discount Admin %:", Font = UiHelper.NormalFont, Location = new Point(20, 65), Size = new Size(160, 22) });
            txtMaxAdmin = new TextBox { Location = new Point(190, 62), Size = new Size(80, 26), Text = "0" };
            UiHelper.StyleTextBox(txtMaxAdmin);
            gb.Controls.Add(txtMaxAdmin);

            gb.Controls.Add(new Label { Text = "Max Discount User %:", Font = UiHelper.NormalFont, Location = new Point(20, 100), Size = new Size(160, 22) });
            txtMaxUser = new TextBox { Location = new Point(190, 97), Size = new Size(80, 26), Text = "0" };
            UiHelper.StyleTextBox(txtMaxUser);
            gb.Controls.Add(txtMaxUser);

            gb.Controls.Add(new Label { Text = "SQL Connection String:", Font = UiHelper.NormalFont, Location = new Point(20, 135), Size = new Size(200, 22) });
            txtConn = new TextBox { Location = new Point(20, 158), Size = new Size(420, 26) };
            UiHelper.StyleTextBox(txtConn);
            gb.Controls.Add(txtConn);

            gb.Controls.Add(new Label { Text = "New Master Password:", Font = UiHelper.NormalFont, Location = new Point(20, 195), Size = new Size(160, 22) });
            txtNewMaster = new TextBox { Location = new Point(190, 192), Size = new Size(200, 26), PasswordChar = '●' };
            UiHelper.StyleTextBox(txtNewMaster);
            gb.Controls.Add(txtNewMaster);

            Button btnSave = new Button { Text = "Save Protected Settings", Location = new Point(20, 235), Size = new Size(200, 34) };
            UiHelper.StyleButton(btnSave);
            btnSave.Click += (s, e) => SaveProtected();
            gb.Controls.Add(btnSave);

            this.Controls.Add(gb);

            this.Controls.Add(new Label
            {
                Text = "Default master password: master123",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Location = new Point(20, 420),
                Size = new Size(400, 20)
            });
        }

        private void LoadValues()
        {
            string ap = AppSettings.Get("AllowBillPrint");
            chkAllowPrint.Checked = string.IsNullOrEmpty(ap) || ap == "1";
            txtMaxAdmin.Text = AppSettings.Get("MaxDiscountAdmin");
            if (string.IsNullOrEmpty(txtMaxAdmin.Text)) txtMaxAdmin.Text = "0";
            txtMaxUser.Text = AppSettings.Get("MaxDiscountUser");
            if (string.IsNullOrEmpty(txtMaxUser.Text)) txtMaxUser.Text = "0";
            try
            {
                // Read current connection from App.config path relative
                string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplianceManagement.exe.config");
                if (!File.Exists(cfg)) cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App.config");
                // leave empty; user can paste
            }
            catch { }
        }

        private bool VerifyMaster(string pwd)
        {
            string stored = AppSettings.Get("MasterPasswordHash");
            if (string.IsNullOrEmpty(stored))
                return pwd == DefaultMaster;
            return stored == Hash(pwd);
        }

        private static string Hash(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpper();
            }
        }

        private void Unlock()
        {
            if (!VerifyMaster(txtMaster.Text))
            {
                MessageBox.Show("Invalid master password.", "Authenticator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (Control c in this.Controls)
            {
                if (c is GroupBox && c.Name == "gbProtected")
                    c.Enabled = true;
            }
            MessageBox.Show("Unlocked.", "Authenticator");
        }

        private void SaveProtected()
        {
            AppSettings.Set("AllowBillPrint", chkAllowPrint.Checked ? "1" : "0");
            AppSettings.Set("MaxDiscountAdmin", txtMaxAdmin.Text.Trim());
            AppSettings.Set("MaxDiscountUser", txtMaxUser.Text.Trim());

            if (!string.IsNullOrWhiteSpace(txtNewMaster.Text))
            {
                AppSettings.Set("MasterPasswordHash", Hash(txtNewMaster.Text));
                MessageBox.Show("Master password updated.");
            }

            // Encrypt and store connection string snippet in Settings (for reference)
            if (!string.IsNullOrWhiteSpace(txtConn.Text))
            {
                try
                {
                    string enc = EncryptString(txtConn.Text.Trim());
                    AppSettings.Set("EncryptedConnectionString", enc);
                    MessageBox.Show("Connection string encrypted and stored.\nUpdate App.config manually with decrypted value when needed, or use EncryptedConnectionString from Settings.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Encrypt failed: " + ex.Message);
                }
            }

            MessageBox.Show("Protected settings saved.", "Success");
        }

        private static string EncryptString(string plain)
        {
            byte[] key = Encoding.UTF8.GetBytes("AppMgmtKey16Byte"); // 16 bytes
            byte[] iv = Encoding.UTF8.GetBytes("AppMgmtIV16Byte!"); // 16 bytes
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] data = Encoding.UTF8.GetBytes(plain);
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string DecryptString(string cipher)
        {
            byte[] key = Encoding.UTF8.GetBytes("AppMgmtKey16Byte");
            byte[] iv = Encoding.UTF8.GetBytes("AppMgmtIV16Byte!");
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                byte[] data = Convert.FromBase64String(cipher);
                using (var ms = new MemoryStream(data))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                    return sr.ReadToEnd();
            }
        }
    }
}
