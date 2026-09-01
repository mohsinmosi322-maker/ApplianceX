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
        private Panel swNav, swAccent, swBg, swText;
        private Color customNav, customAccent, customBg, customText;
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
            this.Size = new Size(960, 700);
            this.MinimumSize = new Size(800, 520);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            this.Controls.Add(UiHelper.CreateFormBanner(
                "SETTINGS",
                isAdmin ? "Presets · custom colors · fonts · limits · users" : "Presets · custom colors · fonts",
                FormAccent.Settings, FormAccent.SettingsDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = isAdmin ? 480 : 320, BackColor = UiHelper.BgColor, Padding = new Padding(10), AutoScroll = true };

            GroupBox gbTheme = new GroupBox
            {
                Text = "Theme Manager — presets + custom color adjuster",
                Font = UiHelper.HeaderFont,
                ForeColor = UiHelper.ThemeDark,
                Location = new Point(15, 10),
                Size = new Size(900, 280)
            };

            cmbTheme = new ComboBox { Visible = false, Location = new Point(0, 0), Size = new Size(10, 10) };
            foreach (var n in UiHelper.ThemeNames) cmbTheme.Items.Add(n);
            cmbTheme.SelectedIndex = 0;
            gbTheme.Controls.Add(cmbTheme);

            string[] names = { "Professional Navy", "Modern Slate", "Executive Blue", "Clean Gray" };
            Color[] navs = {
                Color.FromArgb(0x17, 0x32, 0x4D), Color.FromArgb(0x33, 0x41, 0x55),
                Color.FromArgb(0x1E, 0x40, 0xAF), Color.FromArgb(0x37, 0x41, 0x51)
            };
            Color[] accents = {
                Color.FromArgb(0x25, 0x63, 0xEB), Color.FromArgb(0x3B, 0x82, 0xF6),
                Color.FromArgb(0x25, 0x63, 0xEB), Color.FromArgb(0x25, 0x63, 0xEB)
            };
            Color[] bgs = {
                Color.FromArgb(0xF5, 0xF7, 0xFA), Color.FromArgb(0xF8, 0xFA, 0xFC),
                Color.FromArgb(0xF5, 0xF7, 0xFB), Color.FromArgb(0xF3, 0xF4, 0xF6)
            };
            int cardX = 12;
            for (int ti = 0; ti < names.Length; ti++)
            {
                int idx = ti;
                var card = new Panel
                {
                    Location = new Point(cardX, 24), Size = new Size(160, 72),
                    BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Tag = "themeCard"
                };
                card.Controls.Add(new Panel { Location = new Point(8, 8), Size = new Size(28, 28), BackColor = navs[ti] });
                card.Controls.Add(new Panel { Location = new Point(40, 8), Size = new Size(28, 28), BackColor = accents[ti] });
                card.Controls.Add(new Panel { Location = new Point(72, 8), Size = new Size(28, 28), BackColor = bgs[ti], BorderStyle = BorderStyle.FixedSingle });
                card.Controls.Add(new Label { Text = names[ti], Font = new Font("Segoe UI", 7.5f), Location = new Point(8, 42), AutoSize = true, ForeColor = Color.FromArgb(0x1F, 0x29, 0x37) });
                EventHandler pick = (s2, e2) =>
                {
                    cmbTheme.SelectedIndex = idx;
                    customNav = navs[idx]; customAccent = accents[idx]; customBg = bgs[idx];
                    customText = Color.FromArgb(0x1F, 0x29, 0x37);
                    RefreshColorSwatches();
                    HighlightThemeCard(gbTheme, card);
                };
                card.Click += pick;
                foreach (Control ch in card.Controls) ch.Click += pick;
                gbTheme.Controls.Add(card);
                cardX += 168;
            }

            var gbCustom = new GroupBox
            {
                Text = "Custom theme — click a color box to open color picker",
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.ThemeDark,
                Location = new Point(12, 105),
                Size = new Size(870, 110)
            };
            customNav = Color.FromArgb(0x17, 0x32, 0x4D);
            customAccent = Color.FromArgb(0x25, 0x63, 0xEB);
            customBg = Color.FromArgb(0xF5, 0xF7, 0xFA);
            customText = Color.FromArgb(0x1F, 0x29, 0x37);

            swNav = MakeColorButton(gbCustom, "Nav / Menu", 12, customNav, c => { customNav = c; SelectCustom(); });
            swAccent = MakeColorButton(gbCustom, "Accent / Buttons", 170, customAccent, c => { customAccent = c; SelectCustom(); });
            swBg = MakeColorButton(gbCustom, "Background", 340, customBg, c => { customBg = c; SelectCustom(); });
            swText = MakeColorButton(gbCustom, "Text", 500, customText, c => { customText = c; SelectCustom(); });

            Button btnResetNavy = new Button { Text = "Reset to Navy", Location = new Point(660, 28), Size = new Size(120, 28) };
            UiHelper.StyleButton(btnResetNavy);
            btnResetNavy.Click += (s2, e2) =>
            {
                customNav = Color.FromArgb(0x17, 0x32, 0x4D);
                customAccent = Color.FromArgb(0x25, 0x63, 0xEB);
                customBg = Color.FromArgb(0xF5, 0xF7, 0xFA);
                customText = Color.FromArgb(0x1F, 0x29, 0x37);
                RefreshColorSwatches();
                cmbTheme.SelectedIndex = 0;
            };
            gbCustom.Controls.Add(btnResetNavy);

            Button btnSaveCustom = new Button { Text = "Save Custom", Location = new Point(660, 62), Size = new Size(120, 28) };
            UiHelper.StyleButton(btnSaveCustom);
            btnSaveCustom.Click += (s2, e2) =>
            {
                UiHelper.SaveCustomColors(customNav, customAccent, customBg, customText);
                SelectCustom();
                DialogHelpers.Info(this, "Custom colors saved. Click Apply Live.");
            };
            gbCustom.Controls.Add(btnSaveCustom);
            gbTheme.Controls.Add(gbCustom);

            gbTheme.Controls.Add(new Label { Text = "Font:", Font = UiHelper.NormalFont, Location = new Point(12, 228), AutoSize = true });
            cmbFontSize = new ComboBox { Location = new Point(55, 226), Size = new Size(70, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbFontSize);
            foreach (var fs in new[] { "9", "10", "11", "12", "14" }) cmbFontSize.Items.Add(fs);
            cmbFontSize.SelectedItem = "10";
            gbTheme.Controls.Add(cmbFontSize);

            gbTheme.Controls.Add(new Label { Text = "Form size:", Font = UiHelper.NormalFont, Location = new Point(140, 228), AutoSize = true });
            cmbResolution = new ComboBox { Location = new Point(220, 226), Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbResolution);
            foreach (var r in new[] { "800 x 600", "1024 x 768", "1152 x 864", "1280 x 720", "1280 x 800", "1280 x 960", "1280 x 1024" })
                cmbResolution.Items.Add(r);
            cmbResolution.SelectedItem = "1024 x 768";
            gbTheme.Controls.Add(cmbResolution);

            Button btnApply = new Button { Text = "Apply Live", Location = new Point(370, 222), Size = new Size(140, 34) };
            UiHelper.StyleButton(btnApply);
            btnApply.Click += (s2, e2) =>
            {
                string theme = UiHelper.NormalizeThemeName(cmbTheme.SelectedItem != null ? cmbTheme.SelectedItem.ToString() : UiHelper.DefaultTheme);
                if (string.Equals(theme, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    UiHelper.SaveCustomColors(customNav, customAccent, customBg, customText);
                    theme = "Custom";
                }
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
                GroupBox gbDisc = new GroupBox { Text = "Max Discount % (Admin)", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(15, 300), Size = new Size(420, 100) };
                gbDisc.Controls.Add(new Label { Text = "Admin %:", Font = UiHelper.NormalFont, Location = new Point(15, 28), Size = new Size(80, 22) });
                txtMaxDiscAdmin = new TextBox { Location = new Point(100, 26), Size = new Size(80, 26), Text = "0" };
                UiHelper.StyleTextBox(txtMaxDiscAdmin);
                gbDisc.Controls.Add(txtMaxDiscAdmin);
                gbDisc.Controls.Add(new Label { Text = "User %:", Font = UiHelper.NormalFont, Location = new Point(200, 28), Size = new Size(70, 22) });
                txtMaxDiscUser = new TextBox { Location = new Point(270, 26), Size = new Size(80, 26), Text = "0" };
                UiHelper.StyleTextBox(txtMaxDiscUser);
                gbDisc.Controls.Add(txtMaxDiscUser);
                gbDisc.Controls.Add(new Label { Text = "Lock pwd:", Font = UiHelper.SmallFont, Location = new Point(15, 62), Size = new Size(70, 22) });
                txtSettingsPwd = new TextBox { Location = new Point(100, 60), Size = new Size(120, 26), PasswordChar = '*' };
                UiHelper.StyleTextBox(txtSettingsPwd);
                gbDisc.Controls.Add(txtSettingsPwd);
                Button btnDisc = new Button { Text = "Save Limits", Location = new Point(240, 56), Size = new Size(100, 30) };
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
                Button btnBackup = new Button { Text = "Backup DB", Location = new Point(350, 56), Size = new Size(55, 30) };
                UiHelper.StyleButton(btnBackup);
                btnBackup.Click += (s, e) =>
                {
                    if (!DialogHelpers.Confirm(this, "Create a SQL Server backup of the current database?"))
                        return;
                    BackupHelper.BackupInteractive(this);
                };
                gbDisc.Controls.Add(btnBackup);
                top.Controls.Add(gbDisc);

                GroupBox gbRights = new GroupBox { Text = "User Rights", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(450, 300), Size = new Size(465, 100) };
                cmbUsers = new ComboBox { Location = new Point(12, 28), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                UiHelper.StyleComboBox(cmbUsers);
                cmbUsers.SelectedIndexChanged += (s, e) => LoadPermChecks();
                gbRights.Controls.Add(cmbUsers);
                chkSale = MkChk("Sale", 160, 30); chkPurchase = MkChk("Purchase", 215, 30);
                chkNewItem = MkChk("Item", 295, 30); chkInventory = MkChk("Inv", 345, 30);
                chkReports = MkChk("Rpt", 385, 30); chkSettings = MkChk("Set", 12, 58);
                gbRights.Controls.AddRange(new Control[] { chkSale, chkPurchase, chkNewItem, chkInventory, chkReports, chkSettings });
                Button btnPerm = new Button { Text = "Save Rights", Location = new Point(340, 55), Size = new Size(100, 30) };
                UiHelper.StyleButton(btnPerm);
                btnPerm.Click += (s, e) => SavePerms();
                gbRights.Controls.Add(btnPerm);
                top.Controls.Add(gbRights);
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
                    Height = 180
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
                gbUser.Controls.Add(new Label { Text = "Change password:", Font = UiHelper.SmallFont, Location = new Point(16, 140), AutoSize = true });
                cmbChgUser = new ComboBox { Location = new Point(130, 136), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                UiHelper.StyleComboBox(cmbChgUser);
                gbUser.Controls.Add(cmbChgUser);
                txtChgPwd = new TextBox { Location = new Point(280, 136), Size = new Size(120, 26), PasswordChar = '*' };
                UiHelper.StyleTextBox(txtChgPwd);
                gbUser.Controls.Add(txtChgPwd);
                Button btnChg = new Button { Text = "Set Pwd", Location = new Point(410, 134), Size = new Size(90, 30) };
                UiHelper.StyleButton(btnChg);
                btnChg.Click += (s, e) => ChangeUserPassword();
                gbUser.Controls.Add(btnChg);
                mid.Controls.Add(gbUser);
                this.Controls.Add(mid);
            }
        }

        private Panel MakeColorButton(Control parent, string caption, int x, Color initial, Action<Color> onPicked)
        {
            var wrap = new Panel { Location = new Point(x, 22), Size = new Size(150, 70), BackColor = Color.Transparent };
            var btn = new Panel
            {
                Location = new Point(0, 0), Size = new Size(48, 48),
                BackColor = initial, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand
            };
            var lbl = new Label { Text = caption, Font = new Font("Segoe UI", 7.5f), Location = new Point(54, 8), AutoSize = true, ForeColor = Color.FromArgb(0x1F, 0x29, 0x37) };
            var hex = new Label { Text = UiHelper.ColorToHex(initial), Font = new Font("Segoe UI", 7.5f), Location = new Point(54, 28), AutoSize = true, ForeColor = Color.Gray, Tag = "hex" };
            btn.Click += (s, e) =>
            {
                using (var dlg = new ColorDialog())
                {
                    dlg.Color = btn.BackColor;
                    dlg.FullOpen = true;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        btn.BackColor = dlg.Color;
                        hex.Text = UiHelper.ColorToHex(dlg.Color);
                        if (onPicked != null) onPicked(dlg.Color);
                    }
                }
            };
            wrap.Controls.Add(btn);
            wrap.Controls.Add(lbl);
            wrap.Controls.Add(hex);
            parent.Controls.Add(wrap);
            return btn;
        }

        private void RefreshColorSwatches()
        {
            if (swNav != null) { swNav.BackColor = customNav; UpdateHexLabel(swNav.Parent, customNav); }
            if (swAccent != null) { swAccent.BackColor = customAccent; UpdateHexLabel(swAccent.Parent, customAccent); }
            if (swBg != null) { swBg.BackColor = customBg; UpdateHexLabel(swBg.Parent, customBg); }
            if (swText != null) { swText.BackColor = customText; UpdateHexLabel(swText.Parent, customText); }
        }

        private void UpdateHexLabel(Control parent, Color c)
        {
            if (parent == null) return;
            foreach (Control ch in parent.Controls)
            {
                var lb = ch as Label;
                if (lb != null && lb.Tag != null && lb.Tag.ToString() == "hex")
                    lb.Text = UiHelper.ColorToHex(c);
            }
        }

        private void SelectCustom()
        {
            int i = -1;
            for (int x = 0; x < cmbTheme.Items.Count; x++)
                if (string.Equals(cmbTheme.Items[x].ToString(), "Custom", StringComparison.OrdinalIgnoreCase)) { i = x; break; }
            if (i >= 0) cmbTheme.SelectedIndex = i;
        }

        private void HighlightThemeCard(Control gb, Panel selected)
        {
            foreach (Control c in gb.Controls)
            {
                var p = c as Panel;
                if (p != null && p.Tag != null && p.Tag.ToString() == "themeCard")
                    p.BackColor = Color.White;
            }
            if (selected != null) selected.BackColor = Color.FromArgb(0xDB, 0xEA, 0xFE);
        }

        private void CreateUser()
        {
            if (string.IsNullOrWhiteSpace(txtNewUser.Text) || string.IsNullOrWhiteSpace(txtNewPwd.Text))
            { DialogHelpers.Error(this, "Username and password required."); return; }
            if (userRepo.ExistsUserName(txtNewUser.Text.Trim()))
            { DialogHelpers.Error(this, "Username already exists."); return; }
            if (!DialogHelpers.Confirm(this, "Create user " + txtNewUser.Text.Trim() + "?")) return;
            userRepo.Insert(new User
            {
                UserName = txtNewUser.Text.Trim(),
                FullName = string.IsNullOrWhiteSpace(txtNewFull.Text) ? txtNewUser.Text.Trim() : txtNewFull.Text.Trim(),
                Role = cmbNewRole.SelectedItem != null ? cmbNewRole.SelectedItem.ToString() : "User"
            }, txtNewPwd.Text);
            DialogHelpers.Info(this, "User created.");
            txtNewUser.Clear(); txtNewFull.Clear(); txtNewPwd.Clear();
            LoadUsersForRights(); LoadUsersForManage();
        }

        private void ChangeUserPassword()
        {
            if (cmbChgUser == null || cmbChgUser.SelectedItem == null || string.IsNullOrWhiteSpace(txtChgPwd.Text))
            { DialogHelpers.Error(this, "Select user and enter new password."); return; }
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
            DialogHelpers.Info(this, "Rights saved for " + cmbUsers.SelectedItem + ".");
        }

        private void LoadValues()
        {
            customNav = UiHelper.HexToColor(AppSettings.Get("Custom_Nav"), Color.FromArgb(0x17, 0x32, 0x4D));
            customAccent = UiHelper.HexToColor(AppSettings.Get("Custom_Accent"), Color.FromArgb(0x25, 0x63, 0xEB));
            customBg = UiHelper.HexToColor(AppSettings.Get("Custom_Bg"), Color.FromArgb(0xF5, 0xF7, 0xFA));
            customText = UiHelper.HexToColor(AppSettings.Get("Custom_Text"), Color.FromArgb(0x1F, 0x29, 0x37));
            RefreshColorSwatches();

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
