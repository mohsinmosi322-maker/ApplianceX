using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    public static class UiHelper
    {
        public static Font TitleFont { get; private set; } = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static Font HeaderFont { get; private set; } = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static Font NormalFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font ButtonFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static Font SmallFont { get; private set; } = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        public static Color ThemeColor { get; private set; } = Color.FromArgb(41, 128, 185);
        public static Color ThemeDark { get; private set; } = Color.FromArgb(30, 100, 150);
        public static Color ThemeLight { get; private set; } = Color.FromArgb(174, 214, 241);
        public static Color ThemeAltRow { get; private set; } = Color.FromArgb(235, 245, 255);
        public static Color ThemeSelection { get; private set; } = Color.FromArgb(100, 160, 210);
        public static Color BgColor { get; private set; } = Color.FromArgb(245, 247, 250);
        public static Color PanelColor { get; private set; } = Color.White;
        public static Color GridHeaderColor { get; private set; } = Color.FromArgb(41, 128, 185);
        public static Color DangerColor { get; private set; } = Color.FromArgb(192, 57, 43);

        public static string AppName
        {
            get
            {
                if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.SoftwareName))
                    return LicenseReader.Current.SoftwareName;
                return "Appliance Management System";
            }
        }

        public static string AppVersion
        {
            get
            {
                if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.AppVersion))
                    return LicenseReader.Current.AppVersion;
                return "2.1.0";
            }
        }

        public static string ContactNumber
        {
            get
            {
                if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.VendorContact))
                    return LicenseReader.Current.VendorContact;
                return "+92-300-1234567";
            }
        }

        public static void InitializeTheme()
        {
            string theme = AppSettings.Get("Theme");
            if (string.IsNullOrEmpty(theme)) theme = "Blue";
            ApplyThemeName(theme);

            int size = 10;
            int.TryParse(AppSettings.Get("FontSize"), out size);
            if (size < 8) size = 10;
            if (size > 18) size = 18;
            ApplyFontSize(size);
        }

        public static void ApplyThemeName(string theme)
        {
            switch ((theme ?? "Blue").Trim())
            {
                case "Green":
                    ThemeColor = Color.FromArgb(39, 174, 96);
                    ThemeDark = Color.FromArgb(30, 132, 73);
                    ThemeLight = Color.FromArgb(171, 235, 198);
                    ThemeAltRow = Color.FromArgb(232, 248, 238);
                    ThemeSelection = Color.FromArgb(82, 190, 128);
                    break;
                case "Dark":
                    ThemeColor = Color.FromArgb(52, 73, 94);
                    ThemeDark = Color.FromArgb(33, 47, 61);
                    ThemeLight = Color.FromArgb(174, 182, 191);
                    ThemeAltRow = Color.FromArgb(236, 240, 241);
                    ThemeSelection = Color.FromArgb(93, 109, 126);
                    break;
                case "Purple":
                    ThemeColor = Color.FromArgb(142, 68, 173);
                    ThemeDark = Color.FromArgb(108, 52, 131);
                    ThemeLight = Color.FromArgb(215, 189, 226);
                    ThemeAltRow = Color.FromArgb(245, 238, 248);
                    ThemeSelection = Color.FromArgb(165, 105, 189);
                    break;
                case "Teal":
                    ThemeColor = Color.FromArgb(22, 160, 133);
                    ThemeDark = Color.FromArgb(17, 122, 101);
                    ThemeLight = Color.FromArgb(163, 228, 215);
                    ThemeAltRow = Color.FromArgb(232, 248, 245);
                    ThemeSelection = Color.FromArgb(69, 179, 157);
                    break;
                default: // Blue variants for headers/rows/selection
                    ThemeColor = Color.FromArgb(41, 128, 185);
                    ThemeDark = Color.FromArgb(30, 100, 150);
                    ThemeLight = Color.FromArgb(174, 214, 241);
                    ThemeAltRow = Color.FromArgb(235, 245, 255);
                    ThemeSelection = Color.FromArgb(100, 160, 210);
                    break;
            }
            GridHeaderColor = ThemeColor;
            BgColor = Color.FromArgb(245, 247, 250);
            PanelColor = Color.White;
        }

        public static void ApplyFontSize(int size)
        {
            TitleFont = new Font("Segoe UI", size + 4, FontStyle.Bold);
            HeaderFont = new Font("Segoe UI", size + 1, FontStyle.Bold);
            NormalFont = new Font("Segoe UI", size, FontStyle.Regular);
            ButtonFont = new Font("Segoe UI", size, FontStyle.Bold);
            SmallFont = new Font("Segoe UI", Math.Max(8, size - 1.5f), FontStyle.Regular);
        }

        public static Size GetPreferredFormSize()
        {
            // Stored as "W x H" e.g. "1024 x 768"
            string res = AppSettings.Get("FormResolution");
            if (string.IsNullOrEmpty(res)) res = "1024 x 768";
            int w = 1024, h = 768;
            try
            {
                var parts = res.ToLowerInvariant().Replace(" ", "").Split('x');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out w);
                    int.TryParse(parts[1], out h);
                }
            }
            catch { }
            if (w < 800) w = 800;
            if (h < 600) h = 600;
            if (w > 1280) w = 1280;
            if (h > 1024) h = 1024;
            return new Size(w, h);
        }

        public static void ApplyFormSize(Form form)
        {
            if (form == null || form.IsMdiContainer) return;
            var sz = GetPreferredFormSize();
            form.MinimumSize = new Size(800, 600);
            form.MaximumSize = new Size(1280, 1024);
            // Size the window; if maximized by user, keep state but update restore bounds
            if (form.WindowState == FormWindowState.Normal)
                form.Size = sz;
            else
            {
                form.WindowState = FormWindowState.Normal;
                form.Size = sz;
            }
        }

        public static void ApplyFormSizeToAllChildren(Form mdiParent)
        {
            if (mdiParent == null) return;
            foreach (Form child in mdiParent.MdiChildren)
                ApplyFormSize(child);
        }

        public static void ApplyThemeLive(Form root)
        {
            InitializeTheme();
            if (root == null) return;
            root.BackColor = BgColor;
            ApplyToControlTree(root);
            // Also apply to open MDI children
            if (root.IsMdiContainer)
            {
                foreach (Form child in root.MdiChildren)
                {
                    child.BackColor = BgColor;
                    ApplyToControlTree(child);
                }
            }
        }

        private static void ApplyToControlTree(Control c)
        {
            if (c == null) return;

            if (c is Button btn)
                StyleButton(btn);
            else if (c is TextBox txt)
                StyleTextBox(txt);
            else if (c is ComboBox cmb)
                StyleComboBox(cmb);
            else if (c is DataGridView dgv)
                StyleGrid(dgv);
            else if (c is Label lbl)
            {
                if (lbl.Font.Bold && lbl.Font.Size >= 12)
                    lbl.Font = TitleFont;
                else if (lbl.Font.Bold)
                    lbl.Font = HeaderFont;
                else
                    lbl.Font = NormalFont;
                if (lbl.ForeColor.R == 41 && lbl.ForeColor.G == 128) // old blue title
                    lbl.ForeColor = ThemeColor;
            }
            else if (c is GroupBox gb)
            {
                gb.Font = HeaderFont;
                gb.ForeColor = ThemeDark;
            }
            else if (c is CheckBox chk)
                chk.Font = NormalFont;
            else if (c is RadioButton rb)
                rb.Font = NormalFont;
            else if (c is DateTimePicker dtp)
                StyleDatePicker(dtp);
            else if (c is ListBox lb)
                lb.Font = NormalFont;
            else if (c is MenuStrip ms)
            {
                ms.BackColor = ThemeColor;
                ms.ForeColor = Color.White;
                ms.Font = NormalFont;
            }
            else if (c is StatusStrip ss)
            {
                ss.BackColor = ThemeDark;
                ss.Font = SmallFont;
            }
            else if (c is ToolStrip ts && !(c is MenuStrip) && !(c is StatusStrip))
            {
                ts.Font = ButtonFont;
            }

            foreach (Control child in c.Controls)
                ApplyToControlTree(child);
        }

        public static void StyleButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ThemeColor;
            btn.ForeColor = Color.White;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = ThemeDark;
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.Font = NormalFont;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = Color.White;
        }

        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.Font = NormalFont;
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = Color.White;
        }

        public static void StyleGrid(DataGridView dgv)
        {
            dgv.AllowUserToResizeColumns = true;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.RowHeadersVisible = false;
            dgv.BackgroundColor = Color.White;
            dgv.Font = NormalFont;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeDark;
            dgv.ColumnHeadersHeight = 36;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeAltRow;
            dgv.DefaultCellStyle.SelectionBackColor = ThemeSelection;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = NormalFont;
            dgv.RowTemplate.Height = 28;
            dgv.GridColor = ThemeLight;
        }

        public static void StyleDatePicker(DateTimePicker dtp)
        {
            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "dd/MM/yyyy";
            dtp.Font = NormalFont;
        }

        public static void FadeIn(Form form)
        {
            form.Opacity = 0;
            var t = new Timer { Interval = 12 };
            t.Tick += (s, e) =>
            {
                if (form.Opacity >= 1) { t.Stop(); t.Dispose(); }
                else form.Opacity += 0.12;
            };
            t.Start();
        }

        public static bool ConfirmExit()
        {
            return MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        public static void AttachF4Close(Form form)
        {
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F4)
                {
                    e.SuppressKeyPress = true;
                    form.Close();
                }
            };
            form.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    if (form.Tag != null && form.Tag.ToString() == "NOSAVECONFIRM") return;
                    if (!ConfirmExit()) e.Cancel = true;
                }
            };
        }

        public static decimal GetMaxDiscount(string role)
        {
            string key = role == "Admin" ? "MaxDiscountAdmin" : "MaxDiscountUser";
            string val = AppSettings.Get(key);
            decimal d = 0;
            decimal.TryParse(val, out d);
            return d;
        }

        public static bool IsPrintAllowed()
        {
            if (LicenseReader.Current != null)
                return LicenseReader.Current.AllowPrint;
            return true;
        }

        public static string GetShopName()
        {
            if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.StoreName))
                return LicenseReader.Current.StoreName;
            return "Appliance Shop";
        }

        public static string GetShopPhone()
        {
            if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.ShopPhone))
                return LicenseReader.Current.ShopPhone;
            return "";
        }
    }
}
