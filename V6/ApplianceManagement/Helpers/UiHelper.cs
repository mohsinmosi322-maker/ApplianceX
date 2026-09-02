using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    public static class FormAccent
    {
        public static Color Sale { get { return UiHelper.ThemeColor; } }
        public static Color SaleDark { get { return UiHelper.ThemeDark; } }
        public static Color Purchase { get { return UiHelper.ThemeColor; } }
        public static Color PurchaseDark { get { return UiHelper.ThemeDark; } }
        public static Color SaleReturn { get { return UiHelper.ThemeColor; } }
        public static Color SaleReturnDark { get { return UiHelper.ThemeDark; } }
        public static Color PurchaseReturn { get { return UiHelper.ThemeColor; } }
        public static Color PurchaseReturnDark { get { return UiHelper.ThemeDark; } }
        public static Color NewItem { get { return UiHelper.ThemeColor; } }
        public static Color NewItemDark { get { return UiHelper.ThemeDark; } }
        public static Color Inventory { get { return UiHelper.ThemeColor; } }
        public static Color InventoryDark { get { return UiHelper.ThemeDark; } }
        public static Color Reports { get { return UiHelper.ThemeColor; } }
        public static Color ReportsDark { get { return UiHelper.ThemeDark; } }
        public static Color Settings { get { return UiHelper.ThemeColor; } }
        public static Color SettingsDark { get { return UiHelper.ThemeDark; } }
        public static Color LowStock { get { return UiHelper.DangerColor; } }
        public static Color LowStockDark { get { return UiHelper.DangerDark; } }
        public static Color Login { get { return UiHelper.ThemeDark; } }
        public static Color Masters { get { return UiHelper.ThemeColor; } }
        public static Color MastersDark { get { return UiHelper.ThemeDark; } }
        public static Color Accounts { get { return UiHelper.ThemeColor; } }
        public static Color AccountsDark { get { return UiHelper.ThemeDark; } }
    }

    public static class UiHelper
    {
        public static readonly string[] ThemeNames = new[] { "Professional Navy", "Modern Slate", "Executive Blue", "Clean Gray", "Custom" };
        public const string DefaultTheme = "Professional Navy";
        public static Font TitleFont { get; private set; } = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static Font HeaderFont { get; private set; } = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static Font NormalFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font ButtonFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static Font SmallFont { get; private set; } = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static Color ThemeColor { get; private set; } = Color.FromArgb(0x17, 0x32, 0x4D);
        public static Color ThemeDark { get; private set; } = Color.FromArgb(0x10, 0x26, 0x3A);
        public static Color ThemeLight { get; private set; } = Color.FromArgb(0xDC, 0xEA, 0xF5);
        public static Color ThemeAltRow { get; private set; } = Color.FromArgb(0xF8, 0xFA, 0xFC);
        public static Color ThemeSelection { get; private set; } = Color.FromArgb(0xDB, 0xEA, 0xFE);
        public static Color BgColor { get; private set; } = Color.FromArgb(0xF5, 0xF7, 0xFA);
        public static Color PanelColor { get; private set; } = Color.White;
        public static Color GridHeaderColor { get; private set; } = Color.FromArgb(0x17, 0x32, 0x4D);
        public static Color TextColor { get; private set; } = Color.FromArgb(0x1F, 0x29, 0x37);
        public static Color SecondaryTextColor { get; private set; } = Color.FromArgb(0x6B, 0x72, 0x80);
        public static Color BorderColor { get; private set; } = Color.FromArgb(0xD1, 0xD5, 0xDB);
        public static Color AccentColor { get; private set; } = Color.FromArgb(0x25, 0x63, 0xEB);
        public static Color AccentDark { get; private set; } = Color.FromArgb(0x1D, 0x4E, 0xD8);
        public static Color SuccessColor { get; private set; } = Color.FromArgb(0x16, 0xA3, 0x4A);
        public static Color WarningColor { get; private set; } = Color.FromArgb(0xD9, 0x77, 0x06);
        public static Color DangerColor { get; private set; } = Color.FromArgb(0xDC, 0x26, 0x26);
        public static Color DangerDark { get; private set; } = Color.FromArgb(0xB9, 0x1C, 0x1C);
        public static string CurrentThemeName { get; private set; } = DefaultTheme;

        public static string AppName { get { if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.SoftwareName)) return LicenseReader.Current.SoftwareName; return "Appliance Management System"; } }
        public static string AppVersion { get { if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.AppVersion)) return LicenseReader.Current.AppVersion; return "2.1.0"; } }
        public static string ContactNumber { get { if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.VendorContact)) return LicenseReader.Current.VendorContact; return "+92-300-1234567"; } }

        public static string ColorToHex(Color c) { return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B); }
        public static Color HexToColor(string hex, Color fallback) { if (string.IsNullOrWhiteSpace(hex)) return fallback; string h = hex.Trim().TrimStart('#'); if (h.Length != 6) return fallback; try { return Color.FromArgb(Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); } catch { return fallback; } }
        public static Color DarkenColor(Color c, float factor) { if (factor < 0f) factor = 0f; if (factor > 1f) factor = 1f; return Color.FromArgb((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor)); }
        public static Color LightenColor(Color c, float mix) { if (mix < 0f) mix = 0f; if (mix > 1f) mix = 1f; return Color.FromArgb((int)(c.R + (255 - c.R) * mix), (int)(c.G + (255 - c.G) * mix), (int)(c.B + (255 - c.B) * mix)); }

        public static void ApplyCustomColors(Color nav, Color accent, Color bg, Color text) { ThemeColor = nav; ThemeDark = DarkenColor(nav, 0.72f); ThemeLight = LightenColor(nav, 0.88f); AccentColor = accent; AccentDark = DarkenColor(accent, 0.82f); BgColor = bg; PanelColor = Color.White; TextColor = text; SecondaryTextColor = Color.FromArgb(0x6B, 0x72, 0x80); BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB); GridHeaderColor = nav; ThemeSelection = LightenColor(accent, 0.82f); ThemeAltRow = Color.FromArgb(0xF8, 0xFA, 0xFC); SuccessColor = Color.FromArgb(0x16, 0xA3, 0x4A); WarningColor = Color.FromArgb(0xD9, 0x77, 0x06); DangerColor = Color.FromArgb(0xDC, 0x26, 0x26); DangerDark = Color.FromArgb(0xB9, 0x1C, 0x1C); CurrentThemeName = "Custom"; }
        public static void SaveCustomColors(Color nav, Color accent, Color bg, Color text) { AppSettings.Set("Custom_Nav", ColorToHex(nav)); AppSettings.Set("Custom_Accent", ColorToHex(accent)); AppSettings.Set("Custom_Bg", ColorToHex(bg)); AppSettings.Set("Custom_Text", ColorToHex(text)); AppSettings.Set("Theme", "Custom"); }
        public static void LoadAndApplyCustomFromSettings() { ApplyCustomColors(HexToColor(AppSettings.Get("Custom_Nav"), Color.FromArgb(0x17, 0x32, 0x4D)), HexToColor(AppSettings.Get("Custom_Accent"), Color.FromArgb(0x25, 0x63, 0xEB)), HexToColor(AppSettings.Get("Custom_Bg"), Color.FromArgb(0xF5, 0xF7, 0xFA)), HexToColor(AppSettings.Get("Custom_Text"), Color.FromArgb(0x1F, 0x29, 0x37))); }

        public static Panel CreateFormBanner(string title, string description, Color accent, Color accentDark)
        {
            string t = title ?? "";
            string d = description ?? "";
            Panel banner = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = ThemeColor, Padding = new Padding(0), Margin = new Padding(0), Tag = "FormBanner" };
            typeof(Control).GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(banner, new object[] { ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true });
            banner.Paint += (s, e) =>
            {
                Rectangle r = banner.ClientRectangle;
                e.Graphics.Clear(ThemeColor);
                using (var bg = new SolidBrush(ThemeColor)) e.Graphics.FillRectangle(bg, r);
                using (var edge = new SolidBrush(ThemeDark)) e.Graphics.FillRectangle(edge, 0, 0, 5, r.Height);
                using (var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold))
                using (var descFont = new Font("Segoe UI", 8.25F, FontStyle.Regular))
                using (var titleBrush = new SolidBrush(Color.White))
                using (var descBrush = new SolidBrush(Color.FromArgb(210, 220, 230)))
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString(t, titleFont, titleBrush, 14f, 4f);
                    e.Graphics.DrawString(d, descFont, descBrush, 14f, 26f);
                }
            };
            banner.Resize += (s, e) => banner.Invalidate();
            return banner;
        }

        public static void StyleAccentButton(Button btn, Color accent, Color accentDark) { StylePrimaryButton(btn); }
        public static void StylePrimaryButton(Button btn) { if (btn == null) return; btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = AccentColor; btn.ForeColor = Color.White; btn.Font = ButtonFont; btn.Cursor = Cursors.Hand; if (btn.Height < 30) btn.Height = 34; btn.FlatAppearance.MouseOverBackColor = AccentDark; }
        public static void StyleSecondaryButton(Button btn) { if (btn == null) return; btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = BorderColor; btn.BackColor = PanelColor; btn.ForeColor = TextColor; btn.Font = ButtonFont; btn.Cursor = Cursors.Hand; if (btn.Height < 30) btn.Height = 34; btn.FlatAppearance.MouseOverBackColor = ThemeLight; }
        public static void StyleDangerButton(Button btn) { if (btn == null) return; btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = DangerColor; btn.ForeColor = Color.White; btn.Font = ButtonFont; btn.Cursor = Cursors.Hand; if (btn.Height < 30) btn.Height = 34; btn.FlatAppearance.MouseOverBackColor = DangerDark; }
        public static void StyleSuccessButton(Button btn) { if (btn == null) return; btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = SuccessColor; btn.ForeColor = Color.White; btn.Font = ButtonFont; btn.Cursor = Cursors.Hand; if (btn.Height < 30) btn.Height = 34; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x15, 0x80, 0x3D); }
        public static void StyleGridWithAccent(DataGridView dgv, Color headerColor) { StyleGrid(dgv); }
        public static void StyleButton(Button btn) { if (btn == null) return; string t = (btn.Text ?? "").ToUpperInvariant(); if (t.Contains("DELETE") || t.Contains("REMOVE") || t.Contains("VOID")) { StyleDangerButton(btn); return; } if (t.Contains("CANCEL") || t.Contains("CLOSE") || t == "NO" || t.Contains("BACK")) { StyleSecondaryButton(btn); return; } StylePrimaryButton(btn); }
        public static void StyleTextBox(TextBox txt) { if (txt == null) return; txt.Font = NormalFont; txt.BorderStyle = BorderStyle.FixedSingle; txt.BackColor = Color.White; txt.ForeColor = TextColor; txt.Enter += (s, e) => BeginInvokeSelectAll(txt); txt.Click += (s, e) => { if (txt.SelectionLength == 0) BeginInvokeSelectAll(txt); }; }
        private static void BeginInvokeSelectAll(TextBox tb) { if (tb == null || tb.IsDisposed) return; if (tb.IsHandleCreated) tb.BeginInvoke(new Action(() => { if (!tb.IsDisposed) tb.SelectAll(); })); else tb.SelectAll(); }

        public static string NormalizeThemeName(string theme) { if (string.IsNullOrWhiteSpace(theme)) return DefaultTheme; string t = theme.Trim(); foreach (var name in ThemeNames) if (string.Equals(name, t, StringComparison.OrdinalIgnoreCase)) return name; return DefaultTheme; }
        public static void ApplyTheme(string themeName) { CurrentThemeName = NormalizeThemeName(themeName); switch (CurrentThemeName) { case "Modern Slate": ThemeColor = Color.FromArgb(0x33, 0x41, 0x55); ThemeDark = Color.FromArgb(0x1E, 0x29, 0x3B); ThemeLight = Color.FromArgb(0xE2, 0xE8, 0xF0); AccentColor = Color.FromArgb(0x3B, 0x82, 0xF6); AccentDark = Color.FromArgb(0x25, 0x63, 0xEB); BgColor = Color.FromArgb(0xF1, 0xF5, 0xF9); PanelColor = Color.White; GridHeaderColor = Color.FromArgb(0x33, 0x41, 0x55); break; case "Executive Blue": ThemeColor = Color.FromArgb(0x1E, 0x40, 0xAF); ThemeDark = Color.FromArgb(0x1E, 0x3A, 0x8A); ThemeLight = Color.FromArgb(0xDB, 0xEA, 0xFE); AccentColor = Color.FromArgb(0x25, 0x63, 0xEB); AccentDark = Color.FromArgb(0x1D, 0x4E, 0xD8); BgColor = Color.FromArgb(0xF8, 0xFA, 0xFC); PanelColor = Color.White; GridHeaderColor = Color.FromArgb(0x1E, 0x40, 0xAF); break; case "Clean Gray": ThemeColor = Color.FromArgb(0x37, 0x41, 0x51); ThemeDark = Color.FromArgb(0x1F, 0x29, 0x37); ThemeLight = Color.FromArgb(0xF3, 0xF4, 0xF6); AccentColor = Color.FromArgb(0x4B, 0x55, 0x63); AccentDark = Color.FromArgb(0x37, 0x41, 0x51); BgColor = Color.FromArgb(0xF9, 0xFA, 0xFB); PanelColor = Color.White; GridHeaderColor = Color.FromArgb(0x37, 0x41, 0x51); break; default: ThemeColor = Color.FromArgb(0x17, 0x32, 0x4D); ThemeDark = Color.FromArgb(0x10, 0x26, 0x3A); ThemeLight = Color.FromArgb(0xDC, 0xEA, 0xF5); AccentColor = Color.FromArgb(0x25, 0x63, 0xEB); AccentDark = Color.FromArgb(0x1D, 0x4E, 0xD8); BgColor = Color.FromArgb(0xF5, 0xF7, 0xFA); PanelColor = Color.White; GridHeaderColor = Color.FromArgb(0x17, 0x32, 0x4D); break; } TextColor = Color.FromArgb(0x1F, 0x29, 0x37); SecondaryTextColor = Color.FromArgb(0x6B, 0x72, 0x80); BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB); ThemeSelection = LightenColor(AccentColor, 0.82f); ThemeAltRow = Color.FromArgb(0xF8, 0xFA, 0xFC); }

        public static void StyleGrid(DataGridView dgv) { if (dgv == null) return; dgv.BackgroundColor = Color.White; dgv.BorderStyle = BorderStyle.None; dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; dgv.EnableHeadersVisualStyles = false; dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderColor; dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont; dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; dgv.ColumnHeadersHeight = 36; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; dgv.DefaultCellStyle.Font = NormalFont; dgv.DefaultCellStyle.ForeColor = TextColor; dgv.DefaultCellStyle.SelectionBackColor = ThemeSelection; dgv.DefaultCellStyle.SelectionForeColor = TextColor; dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeAltRow; dgv.RowTemplate.Height = 28; dgv.RowHeadersVisible = false; dgv.AllowUserToAddRows = false; dgv.AllowUserToDeleteRows = false; dgv.AllowUserToResizeRows = false; dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgv.MultiSelect = false; dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; }

        public static void ApplyToControlTree(Control c) { if (c == null) return; if (c is Form f) { f.BackColor = BgColor; f.Font = NormalFont; } else if (c is Label lbl) { if (lbl.Font.Bold && lbl.Font.Size >= 12) lbl.Font = TitleFont; else if (lbl.Font.Bold) lbl.Font = HeaderFont; else lbl.Font = NormalFont; if (lbl.Parent != null && lbl.Parent.Tag != null && lbl.Parent.Tag.ToString() == "FormBanner") { lbl.BackColor = ThemeColor; lbl.ForeColor = Color.White; } else lbl.ForeColor = TextColor; } else if (c is Button btn) StyleButton(btn); else if (c is TextBox txt) StyleTextBox(txt); else if (c is DataGridView dgv) StyleGrid(dgv); else if (c is Panel p) { if (p.Tag != null && p.Tag.ToString() == "FormBanner") p.BackColor = ThemeColor; } else if (c is MenuStrip ms) { ms.BackColor = ThemeColor; ms.ForeColor = Color.White; ms.Font = NormalFont; } foreach (Control child in c.Controls) ApplyToControlTree(child); }

        public static bool ConfirmExit() { return MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes; }
        public static void AttachF4Close(Form form, bool confirmOnClose = true) { form.KeyPreview = true; form.KeyDown += (s, e) => { if (e.KeyCode == Keys.F4) { e.SuppressKeyPress = true; form.Close(); } }; if (!confirmOnClose) { form.Tag = "NOSAVECONFIRM"; return; } form.FormClosing += (s, e) => { if (e.CloseReason == CloseReason.UserClosing) { if (form.Tag != null && form.Tag.ToString() == "NOSAVECONFIRM") return; if (!ConfirmExit()) e.Cancel = true; } }; }
        public static void AttachF4Close(Form form) { AttachF4Close(form, true); }
        public static void AttachEnterNavigation(Form form) { if (form == null) return; form.KeyPreview = true; form.KeyDown += (s, e) => { if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Tab) return; Control c = form.ActiveControl; if (c is TextBox || c is ComboBox || c is NumericUpDown || c is CheckBox || c is DateTimePicker) { if (c is TextBox tb && tb.Multiline && e.KeyCode == Keys.Enter) return; e.SuppressKeyPress = true; e.Handled = true; form.SelectNextControl(c, true, true, true, true); } }; }

        public static decimal GetMaxDiscount(string role) { string key = role == "Admin" ? "MaxDiscountAdmin" : "MaxDiscountUser"; string val = AppSettings.Get(key); decimal d = 0; decimal.TryParse(val, out d); return d; }
        public static bool IsPrintAllowed() { if (LicenseReader.Current != null) return LicenseReader.Current.AllowPrint; return true; }
        public static string GetShopName() { if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.StoreName)) return LicenseReader.Current.StoreName; return "Appliance Shop"; }
        public static string GetShopPhone() { if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.ShopPhone)) return LicenseReader.Current.ShopPhone; return ""; }
        public static void FadeIn(Form form) { form.Opacity = 0; var t = new Timer { Interval = 12 }; t.Tick += (s, e) => { if (form.Opacity >= 1) { t.Stop(); t.Dispose(); } else form.Opacity += 0.12; }; t.Start(); }
        public static void StyleComboBox(ComboBox cmb) { if (cmb == null) return; cmb.Font = NormalFont; cmb.FlatStyle = FlatStyle.Flat; cmb.BackColor = Color.White; cmb.ForeColor = TextColor; }
        public static void StyleDateTimePicker(DateTimePicker dtp) { if (dtp == null) return; dtp.Format = DateTimePickerFormat.Custom; dtp.CustomFormat = "dd/MM/yyyy"; dtp.Font = NormalFont; }
    }
}
