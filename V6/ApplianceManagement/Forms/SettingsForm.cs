using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SettingsForm : Form
    {
        private UserRepository userRepo = new UserRepository();
        private TextBox txtMaxDiscAdmin, txtMaxDiscUser, txtSettingsPwd;
        private TextBox txtNewUser, txtNewFull, txtNewPwd, txtChgPwd;
        private ComboBox cmbTheme, cmbFontSize, cmbResolution, cmbUsers, cmbNewRole, cmbChgUser;
        private CheckBox chkSale, chkPurchase, chkNewItem, chkInventory, chkReports, chkSettings;
        private bool isAdmin;

        public SettingsForm()
        {
            isAdmin = MainForm.Instance != null &&
                      string.Equals(MainForm.Instance.CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            InitializeComponent();
            UiHelper.ApplyFormSize(this);
            LoadValues();
            if (isAdmin) { LoadUsersForRights(); LoadUsersForManage(); }
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(940, 640);
            this.MinimumSize = new Size(800, 520);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            this.Controls.Add(UiHelper.CreateFormBanner(
                "SETTINGS",
                isAdmin
                    ? "Appearance · discount limits · users · menu rights · backup"
                    : "Appearance only",
                FormAccent.Settings, FormAccent.SettingsDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = isAdmin ? 360 : 270, BackColor = UiHelper.BgColor, Padding = new Padding(10) };

            GroupBox gbTheme = new GroupBox { Text = "Theme Manager (live)", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(15, 10), Size = new Size(420, 250) };
            gbTheme.Controls.Add(new Label
            {
                Text = "Select POS theme (4 professional themes):",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Location = new Point(15, 22),
                AutoSize = true
            });

            cmbTheme = new ComboBox
            {
                Location = new Point(15, 44),
                Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiHelper.NormalFont
            };
            UiHelper.StyleComboBox(cmbTheme);
            cmbTheme.Items.Clear();
            cmbTheme.Items.Add("Professional Navy");
            cmbTheme.Items.Add("Modern State".Replace("State", "Slate"));
            cmbTheme.Items.Add("Executive Blue");
            cmbTheme.Items.Add("Clean Gray");
            cmbTheme.SelectedIndex = 0;
            gbTheme.Controls.Add(cmbTheme);

            var lstThemes = new ListBox
            {
                Location = new Point(15, 78),
                Size = new Size(250, 72),
                Font = UiHelper.NormalFont,
                IntegralHeight = true
            };
            lstThemes.Items.Add("Professional Navy (default)");
            lstThemes.Items.Add("Modern Slate");
            lstThemes.Items.Add("Executive Blue");
            lstThemes.Items.Add("Clean Gray");
            lstThemes.SelectedIndex = 0;
            lstThemes.SelectedIndexChanged += (s, e) =>
            {
                if (lstThemes.SelectedIndex < 0) return;
                if (lstThemes.SelectedIndex < cmbTheme.Items.Count)
                    cmbTheme.SelectedIndex = lstThemes.SelectedIndex;
            };
            cmbTheme.SelectedIndexChanged += (s, e) =>
            {
                if (cmbTheme.SelectedIndex >= 0 && cmbTheme.SelectedIndex < lstThemes.Items.Count)
                    lstThemes.SelectedIndex = cmbTheme.SelectedIndex;
            };
            gbTheme.Controls.Add(lstThemes);

            gbTheme.Controls.Add(new Label { Text = "Font Size:", Font = UiHelper.NormalFont, Location = new Point(280, 44), Size = new Size(90, 22) });
            cmbFontSize = new ComboBox { Location = new Point(280, 68), Size = new Size(100, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbFontSize);
            cmbFontSize.Items.Add("9");
            cmbFontSize.Items.Add("10");
            cmbFontSize.Items.Add("11");
            cmbFontSize.Items.Add("12");
            cmbFontSize.Items.Add("14");
            cmbFontSize.SelectedItem = "10";
            gbTheme.Controls.Add(cmbFontSize);

            gbTheme.Controls.Add(new Label { Text = "Form Size:", Font = UiHelper.NormalFont, Location = new Point(280, 100), Size = new Size(90, 22) });
            cmbResolution = new ComboBox { Location = new Point(280, 124), Size = new Size(120, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbResolution);
            foreach (var r in new[] { "800 x 600", "1024 x 768", "1152 x 864", "1280 x 720", "1280 x 800", "1280 x 960", "1280 x 1024" })
                cmbResolution.Items.Add(r);
            cmbResolution.SelectedItem = "1024 x 768";
            gbTheme.Controls.Add(cmbResolution);

            Button btnApply = new Button { Text = "Apply Live", Location = new Point(15, 160), Size = new Size(160, 36) };
            UiHelper.StyleButton(btnApply);
            btnApply.Click += (s, e) =>
            {
                string theme = UiHelper.NormalizeThemeName(cmbTheme.SelectedItem != null ? cmbTheme.SelectedItem.ToString() : UiHelper.DefaultTheme);
                AppSettings.Set("Theme", theme);
                AppSettings.Set("FontSize", cmbFontSize.SelectedItem.ToString());
                AppSettings.Set("FormResolution", cmbResolution.SelectedItem.ToString());
                if (MainForm.Instance != null)
                {
                    UiHelper.ApplyThemeLive(MainForm.Instance);
                    MainForm.Instance.RefreshBranding();
                    UiHelper.ApplyFormSizeToAllChildren(MainForm.Instance);
                }
                UiHelper.ApplyThemeLive(this);
                UiHelper.ApplyFormSize(this);
                DialogHelpers.Info(this, "Theme applied: " + theme);
            };
            gbTheme.Controls.Add(btnApply);
            top.Controls.Add(gbTheme);

            if (isAdmin)
            {
                GroupBox gbDisc = new GroupBox { Text = "Max Discount % (Admin)", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(450, 10), Size = new Size(420, 190) };
                gbDisc.Controls.Add(new Label { Text = "Admin %:", Font = UiHelper.NormalFont, Location = new Point(15, 30), Size = new Size(100, 22) });
                txtMaxDiscAdmin = new TextBox { Location = new Point(130, 28), Size = new Size(100, 26), Text = "0" };
                UiHelper.StyleTextBox(txtMaxDiscAdmin);
                gbDisc.Controls.Add(txtMaxDiscAdmin);
                gbDisc.Controls.Add(new Label { Text = "User %:", Font = UiHelper.NormalFont, Location = new Point(15, 65), Size = new Size(100, 22) });
                txtMaxDiscUser = new TextBox { Location = new Point(130, 63), Size = new Size(100, 26), Text = "0" };
                UiHelper.StyleTextBox(txtMaxDiscUser);
                gbDisc.Controls.Add(txtMaxDiscUser);
                gbDisc.Controls.Add(new Label { Text = "Settings lock pwd:", Font = UiHelper.SmallFont, Location = new Point(15, 100), Size = new Size(110, 22) });
                txtSettingsPwd = new TextBox { Location = new Point(130, 98), Size = new Size(160, 26), PasswordChar = '*' };
                UiHelper.StyleTextBox(txtSettingsPwd);
                gbDisc.Controls.Add(txtSettingsPwd);
                Button btnDisc = new Button { Text = "Save Limits", Location = new Point(130, 140), Size = new Size(120, 36) };
                UiHelper.StyleButton(btnDisc);
                btnDisc.Click += (s, e) =>
                {
                    AppSettings.Set("MaxDiscountAdmin", txtMaxDiscAdmin.Text.Trim());
                    AppSettings.Set("MaxDiscountUser", txtMaxDiscUser.Text.Trim());
                    if (!string.IsNullOrWhiteSpace(txtSettingsPwd.Text))
                        AppSettings.Set("SettingsPassword", txtSettingsPwd.Text.Trim());
                    DialogHelpers.Info(this, "Saved.");
                };
                gbDisc.Controls.Add(btnDisc);
                Button btnBackup = new Button { Text = "Backup DB", Location = new Point(260, 140), Size = new Size(120, 36) };
                UiHelper.StyleAccentButton(btnBackup, FormAccent.Settings, FormAccent.SettingsDark);
                btnBackup.Click += (s, e) =>
                {
                    if (!DialogHelpers.Confirm(this, "Create a SQL Server backup of the current database?"))
                        return;
                    BackupHelper.BackupInteractive(this);
                };
                gbDisc.Controls.Add(btnBackup);
                top.Controls.Add(gbDisc);

                GroupBox gbRights = new GroupBox { Text = "User Rights (menu access)", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(15, 270), Size = new Size(855, 70) };
                cmbUsers = new ComboBox { Location = new Point(12, 28), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                UiHelper.StyleComboBox(cmbUsers);
                cmbUsers.SelectedIndexChanged += (s, e) => LoadPermChecks();
                gbRights.Controls.Add(cmbUsers);
                chkSale = MkChk("Sale", 185, 30); chkPurchase = MkChk("Purchase", 255, 30);
                chkNewItem = MkChk("New Item", 350, 30); chkInventory = MkChk("Inventory", 445, 30);
                chkReports = MkChk("Reports", 545, 30); chkSettings = MkChk("Settings", 640, 30);
                gbRights.Controls.AddRange(new Control[] { chkSale, chkPurchase, chkNewItem, chkInventory, chkReports, chkSettings });
                Button btnPerm = new Button { Text = "Save Rights", Location = new Point(740, 24), Size = new Size(100, 32) };
                UiHelper.StyleButton(btnPerm);
                btnPerm.Click += (s, e) => SavePerms();
                gbRights.Controls.Add(btnPerm);
                top.Controls.Add(gbRights);
            }
            else
            {
                top.Controls.Add(new Label
                {
                    Text = "Discount limits and users are managed by Admin.",
                    Font = UiHelper.SmallFont,
                    ForeColor = Color.Gray,
                    Location = new Point(450, 40),
                    AutoSize = true
                });
            }

            this.Controls.Add(top);

            if (isAdmin)
            {
                Panel mid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
                GroupBox gbUser = new GroupBox
                {
                    Text = "Create User / Change Password",
                    Font = UiHelper.HeaderFont,
                    ForeColor = UiHelper.ThemeDark,
                    Dock = DockStyle.Top,
                    Height = 200
                };
                gbUser.Controls.Add(new Label { Text = "Username", Font = UiHelper.SmallFont, Location = new Point(16, 28), AutoSize = true });
                txtNewUser = new TextBox { Location = new Point(100, 24), Size = new Size(140, 26) }; UiHelper.StyleTextBox(txtNewUser); gbUser.Controls.Add(txtNewUser);
                gbUser.Controls.Add(new Label { Text = "Full name", Font = UiHelper.SmallFont, Location = new Point(260, 28), AutoSize = true });
                txtNewFull = new TextBox { Location = new Point(330, 24), Size = new Size(160, 26) }; UiHelper.StyleTextBox(txtNewFull); gbUser.Controls.Add(txtNewFull);
                gbUser.Controls.Add(new Label { Text = "Password", Font = UiHelper.SmallFont, Location = new Point(16, 62), AutoSize = true });
                txtNewPwd = new TextBox { Location = new Point(100, 58), Size = new Size(140, 26), PasswordChar = '*' }; UiHelper.StyleTextBox(txtNewPwd); gbUser.Controls.Add(txtNewPwd);
                gbUser.Controls.Add(new Label { Text = "Role", Font = UiHelper.SmallFont, Location = new Point(260, 62), AutoSize = true });
                cmbNewRole = new ComboBox { Location = new Point(330, 58), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                UiHelper.StyleComboBox(cmbNewRole);
                cmbNewRole.Items.AddRange(new object[] { "User", "Admin" });
                cmbNewRole.SelectedIndex = 0;
                gbUser.Controls.Add(cmbNewRole);
                Button btnCreate = new Button { Text = "Create User", Location = new Point(100, 96), Size = new Size(140, 34) };
                UiHelper.StyleButton(btnCreate);
                btnCreate.Click += (s, e) => CreateUser();
                gbUser.Controls.Add(btnCreate);

                gbUser.Controls.Add(new Label { Text = "Change password for:", Font = UiHelper.SmallFont, Location = new Point(16, 148), AutoSize = true });
                cmbChgUser = new ComboBox { Location = new Point(150, 144), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                UiHelper.StyleComboBox(cmbChgUser);
                gbUser.Controls.Add(cmbChgUser);
                txtChgPwd = new TextBox { Location = new Point(300, 144), Size = new Size(120, 26), PasswordChar = '*' };
                UiHelper.StyleTextBox(txtChgPwd);
                gbUser.Controls.Add(txtChgPwd);
                Button btnChg = new Button { Text = "Set Pwd", Location = new Point(430, 142), Size = new Size(100, 30) };
                UiHelper.StyleButton(btnChg);
                btnChg.Click += (s, e) => ChangeUserPassword();
                gbUser.Controls.Add(btnChg);
                mid.Controls.Add(gbUser);
                this.Controls.Add(mid);
            }
        }

        private void CreateUser()
        {
            if (string.IsNullOrWhiteSpace(txtNewUser.Text) || string.IsNullOrWhiteSpace(txtNewPwd.Text))
            {
                DialogHelpers.Error(this, "Username and password required.");
                return;
            }
            if (userRepo.ExistsUserName(txtNewUser.Text.Trim()))
            {
                DialogHelpers.Error(this, "Username already exists.");
                return;
            }
            if (!DialogHelpers.Confirm(this, "Create user " + txtNewUser.Text.Trim() + "?")) return;
            userRepo.Insert(new User
            {
                UserName = txtNewUser.Text.Trim(),
                FullName = string.IsNullOrWhiteSpace(txtNewFull.Text) ? txtNewUser.Text.Trim() : txtNewFull.Text.Trim(),
                Role = cmbNewRole.SelectedItem != null ? cmbNewRole.SelectedItem.ToString() : "User"
            }, txtNewPwd.Text);
            DialogHelpers.Info(this, "User created.");
            txtNewUser.Clear(); txtNewFull.Clear(); txtNewPwd.Clear();
            LoadUsersForRights();
            LoadUsersForManage();
        }

        private void ChangeUserPassword()
        {
            if (cmbChgUser == null || cmbChgUser.SelectedItem == null || string.IsNullOrWhiteSpace(txtChgPwd.Text))
            {
                DialogHelpers.Error(this, "Select user and enter new password.");
                return;
            }
            string uname = cmbChgUser.SelectedItem.ToString();
            User target = null;
            foreach (var u in userRepo.GetAll())
                if (u.UserName == uname) { target = u; break; }
            if (target == null) return;
            if (!DialogHelpers.Confirm(this, "Change password for " + uname + "?")) return;
            userRepo.ChangePassword(target.UserID, txtChgPwd.Text);
            DialogHelpers.Info(this, "Password updated for " + uname);
            txtChgPwd.Clear();
        }

        private void LoadUsersForManage()
        {
            if (cmbChgUser == null) return;
            var names = new List<string>();
            foreach (var u in userRepo.GetAll()) names.Add(u.UserName);
            cmbChgUser.DataSource = names;
        }

        private CheckBox MkChk(string t, int x, int y)
        {
            return new CheckBox { Text = t, Font = UiHelper.SmallFont, Location = new Point(x, y), AutoSize = true, Checked = true };
        }

        private void LoadUsersForRights()
        {
            if (cmbUsers == null) return;
            var users = userRepo.GetAll();
            var names = new List<string>();
            foreach (var u in users)
                if (!string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    names.Add(u.UserName);
            cmbUsers.DataSource = names;
            LoadPermChecks();
        }

        private void LoadPermChecks()
        {
            if (cmbUsers == null || cmbUsers.SelectedItem == null) return;
            string u = cmbUsers.SelectedItem.ToString();
            string p = AppSettings.GetUserPermissions(u).ToUpperInvariant();
            chkSale.Checked = p.Contains("SALE");
            chkPurchase.Checked = p.Contains("PURCHASE");
            chkNewItem.Checked = p.Contains("NEWITEM");
            chkInventory.Checked = p.Contains("INVENTORY");
            chkReports.Checked = p.Contains("REPORTS");
            chkSettings.Checked = p.Contains("SETTINGS");
        }

        private void SavePerms()
        {
            if (cmbUsers == null || cmbUsers.SelectedItem == null) return;
            var parts = new List<string>();
            if (chkSale.Checked) parts.Add("SALE");
            if (chkPurchase.Checked) parts.Add("PURCHASE");
            if (chkNewItem.Checked) parts.Add("NEWITEM");
            if (chkInventory.Checked) parts.Add("INVENTORY");
            if (chkReports.Checked) parts.Add("REPORTS");
            if (chkSettings.Checked) parts.Add("SETTINGS");
            AppSettings.SetUserPermissions(cmbUsers.SelectedItem.ToString(), string.Join(",", parts.ToArray()));
            DialogHelpers.Info(this, "Rights saved for " + cmbUsers.SelectedItem + ". User must re-login to apply menus.");
        }

        private void LoadValues()
        {
            string t = UiHelper.NormalizeThemeName(AppSettings.Get("Theme"));
            int ti = -1;
            for (int i = 0; i < cmbTheme.Items.Count; i++)
            {
                if (string.Equals(cmbTheme.Items[i].ToString(), t, StringComparison.OrdinalIgnoreCase))
                { ti = i; break; }
            }
            cmbTheme.SelectedIndex = ti >= 0 ? ti : 0;
            string f = AppSettings.Get("FontSize");
            if (!string.IsNullOrEmpty(f) && cmbFontSize.Items.Contains(f)) cmbFontSize.SelectedItem = f;
            string r = AppSettings.Get("FormResolution");
            if (!string.IsNullOrEmpty(r) && cmbResolution.Items.Contains(r)) cmbResolution.SelectedItem = r;
            if (isAdmin)
            {
                txtMaxDiscAdmin.Text = string.IsNullOrEmpty(AppSettings.Get("MaxDiscountAdmin")) ? "0" : AppSettings.Get("MaxDiscountAdmin");
                txtMaxDiscUser.Text = string.IsNullOrEmpty(AppSettings.Get("MaxDiscountUser")) ? "0" : AppSettings.Get("MaxDiscountUser");
            }
        }
    }
}
