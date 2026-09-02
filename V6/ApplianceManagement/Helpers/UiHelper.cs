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
        public static readonly string[] ThemeNames = new[]
        {
            "Professional Navy",
            "Modern Slate",
            "Executive Blue",
            "Clean Gray",
            "Custom"
        };

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

        public static string ColorToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static Color HexToColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            string h = hex.Trim().TrimStart('#');
            if (h.Length != 6) return fallback;
            try
            {
                int r = Convert.ToInt32(h.Substring(0, 2), 16);
                int g = Convert.ToInt32(h.Substring(2, 2), 16);
                int b = Convert.ToInt32(h.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            catch { return fallback; }
        }

        public static Color DarkenColor(Color c, float factor)
        {
            if (factor < 0f) factor = 0f;
            if (factor > 1f) factor = 1f;
            return Color.FromArgb((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));
        }

        public static Color LightenColor(Color c, float mix)
        {
            if (mix < 0f) mix = 0f;
            if (mix > 1f) mix = 1f;
            return Color.FromArgb(
                (int)(c.R + (255 - c.R) * mix),
                (int)(c.G + (255 - c.G) * mix),
                (int)(c.B + (255 - c.B) * mix));
        }

        public static void ApplyCustomColors(Color nav, Color accent, Color bg, Color text)
        {
            ThemeColor = nav;
            ThemeDark = DarkenColor(nav, 0.72f);
            ThemeLight = LightenColor(nav, 0.88f);
            AccentColor = accent;
            AccentDark = DarkenColor(accent, 0.82f);
            BgColor = bg;
            PanelColor = Color.White;
            TextColor = text;
            SecondaryTextColor = Color.FromArgb(0x6B, 0x72, 0x80);
            BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
            GridHeaderColor = nav;
            ThemeSelection = LightenColor(accent, 0.82f);
            ThemeAltRow = Color.FromArgb(0xF8, 0xFA, 0xFC);
            SuccessColor = Color.FromArgb(0x16, 0xA3, 0x4A);
            WarningColor = Color.FromArgb(0xD9, 0x77, 0x06);
            DangerColor = Color.FromArgb(0xDC, 0x26, 0x26);
            DangerDark = Color.FromArgb(0xB9, 0x1C, 0x1C);
            CurrentThemeName = "Custom";
        }

        public static void SaveCustomColors(Color nav, Color accent, Color bg, Color text)
        {
            AppSettings.Set("Custom_Nav", ColorToHex(nav));
            AppSettings.Set("Custom_Accent", ColorToHex(accent));
            AppSettings.Set("Custom_Bg", ColorToHex(bg));
            AppSettings.Set("Custom_Text", ColorToHex(text));
            AppSettings.Set("Theme", "Custom");
        }

        public static void LoadAndApplyCustomFromSettings()
        {
            Color nav = HexToColor(AppSettings.Get("Custom_Nav"), Color.FromArgb(0x17, 0x32, 0x4D));
            Color accent = HexToColor(AppSettings.Get("Custom_Accent"), Color.FromArgb(0x25, 0x63, 0xEB));
            Color bg = HexToColor(AppSettings.Get("Custom_Bg"), Color.FromArgb(0xF5, 0xF7, 0xFA));
            Color text = HexToColor(AppSettings.Get("Custom_Text"), Color.FromArgb(0x1F, 0x29, 0x37));
            ApplyCustomColors(nav, accent, bg, text);
        }

        public static Panel CreateFormBanner(string title, string description, Color accent, Color accentDark)
        {
            // Paint-only banner: NO child controls (avoids white-box gaps on MDI)
            string t = title ?? "";
            string d = description ?? "";
            Panel banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ThemeColor,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Tag = "FormBanner"
            };
            // Force solid paint under MDI — prevents white holes
            typeof(Control).GetMethod("SetStyle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(banner, new object[] {
                    ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true });
            banner.Paint += (s, e) =>
            {
                Rectangle r = banner.ClientRectangle;
                e.Graphics.Clear(ThemeColor);
                using (var bg = new SolidBrush(ThemeColor))
                    e.Graphics.FillRectangle(bg, r);
                using (var edge = new SolidBrush(ThemeDark))
                    e.Graphics.FillRectangle(edge, 0, 0, 5, r.Height);
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

        public static void StylePrimaryButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = AccentColor;
            btn.ForeColor = Color.White;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = AccentDark;
        }

        public static void StyleSecondaryButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = PanelColor;
            btn.ForeColor = TextColor;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = ThemeLight;
        }

        public static void StyleDangerButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = DangerColor;
            btn.ForeColor = Color.White;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = DangerDark;
        }

        public static void StyleSuccessButton(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = SuccessColor;
            btn.ForeColor = Color.White;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x15, 0x80, 0x3D);
        }

        public static void StyleGridWithAccent(DataGridView dgv, Color headerColor) { StyleGrid(dgv); }

        public static string NormalizeThemeName(string theme)
        {
            if (string.IsNullOrWhiteSpace(theme)) return DefaultTheme;
            string t = theme.Trim();
            foreach (var name in ThemeNames)
                if (string.Equals(name, t, StringComparison.OrdinalIgnoreCase))
                    return name;
            switch (t.ToLowerInvariant())
            {
                case "custom": return "Custom";
                case "blue":
                case "green":
                case "teal":
                case "purple":
                case "dark":
                case "navy": return DefaultTheme;
                default: return DefaultTheme;
            }
        }

        public static void InitializeTheme()
        {
            string theme = AppSettings.Get("Theme");
            theme = NormalizeThemeName(theme);
            ApplyThemeName(theme);
            string fs = AppSettings.Get("FontSize");
            int size = 10;
            if (!string.IsNullOrEmpty(fs)) int.TryParse(fs, out size);
            if (size < 9) size = 9;
            if (size > 14) size = 14;
            ApplyFontSize(size);
        }

        public static void ApplyThemeName(string theme)
        {
            theme = NormalizeThemeName(theme);
            CurrentThemeName = theme;
            switch (theme)
            {
                case "Modern slate":
                case "Modern Slate":
                    ThemeColor = Color.FromArgb(0x33, 0x41, 0x55);
                    ThemeDark = Color.FromArgb(0x1E, 0x29, 0x3B);
                    ThemeLight = Color.FromArgb(0xE2, 0xE8, 0xF0);
                    AccentColor = Color.FromArgb(0x3B, 0x82, 0xF6);
                    AccentDark = Color.FromArgb(0x25, 0x63, 0xEB);
                    BgColor = Color.FromArgb(0xF8, 0xFA, 0xFC);
                    PanelColor = Color.White;
                    TextColor = Color.FromArgb(0x0F, 0x17, 0x2A);
                    SecondaryTextColor = Color.FromArgb(0x64, 0x74, 0x8B);
                    BorderColor = Color.FromArgb(0xCB, 0xD5, 0xE1);
                    GridHeaderColor = Color.FromArgb(0x33, 0x41, 0x55);
                    ThemeSelection = Color.FromArgb(0xDB, 0xEA, 0xFE);
                    ThemeAltRow = Color.FromArgb(0xF1, 0xF5, 0xF9);
                    SuccessColor = Color.FromArgb(0x16, 0xA3, 0x4A);
                    WarningColor = Color.FromArgb(0xD9, 0x77, 0x06);
                    DangerColor = Color.FromArgb(0xDC, 0x26, 0x26);
                    DangerDark = Color.FromArgb(0xB9, 0x1C, 0x1C);
                    break;
                case "Executive Blue":
                    ThemeColor = Color.FromArgb(0x1E, 0x40, 0xAF);
                    ThemeDark = Color.FromArgb(0x1E, 0x3A, 0x8A);
                    ThemeLight = Color.FromArgb(0xDB, 0xEA, 0xFE);
                    AccentColor = Color.FromArgb(0x25, 0x63, 0xEB);
                    AccentDark = Color.FromArgb(0x1D, 0x4E, 0xD8);
                    BgColor = Color.FromArgb(0xF5, 0xF7, 0xFB);
                    PanelColor = Color.White;
                    TextColor = Color.FromArgb(0x17, 0x20, 0x33);
                    SecondaryTextColor = Color.FromArgb(0x64, 0x74, 0x8B);
                    BorderColor = Color.FromArgb(0xD6, 0xDC, 0xE5);
                    GridHeaderColor = Color.FromArgb(0x1E, 0x40, 0xAF);
                    ThemeSelection = Color.FromArgb(0xDB, 0xEA, 0xFE);
                    ThemeAltRow = Color.FromArgb(0xEE, 0xF2, 0xFF);
                    SuccessColor = Color.FromArgb(0x15, 0x80, 0x3D);
                    WarningColor = Color.FromArgb(0xB4, 0x53, 0x09);
                    DangerColor = Color.FromArgb(0xB9, 0x1C, 0x1C);
                    DangerDark = Color.FromArgb(0x99, 0x1B, 0x1B);
                    break;
                case "Clean Gray":
                    ThemeColor = Color.FromArgb(0x37, 0x41, 0x51);
                    ThemeDark = Color.FromArgb(0x1F, 0x29, 0x37);
                    ThemeLight = Color.FromArgb(0xE5, 0xE7, 0xEB);
                    AccentColor = Color.FromArgb(0x25, 0x63, 0xEB);
                    AccentDark = Color.FromArgb(0x1D, 0x4E, 0xD8);
                    BgColor = Color.FromArgb(0xF3, 0xF4, 0xF6);
                    PanelColor = Color.White;
                    TextColor = Color.FromArgb(0x11, 0x18, 0x27);
                    SecondaryTextColor = Color.FromArgb(0x6B, 0x72, 0x80);
                    BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
                    GridHeaderColor = Color.FromArgb(0x37, 0x41, 0x51);
                    ThemeSelection = Color.FromArgb(0xE5, 0xE7, 0xEB);
                    ThemeAltRow = Color.FromArgb(0xF9, 0xFA, 0xFB);
                    SuccessColor = Color.FromArgb(0x16, 0xA3, 0x4A);
                    WarningColor = Color.FromArgb(0xD9, 0x77, 0x06);
                    DangerColor = Color.FromArgb(0xDC, 0x26, 0x26);
                    DangerDark = Color.FromArgb(0xB9, 0x1C, 0x1C);
                    break;
                case "Custom":
                    LoadAndApplyCustomFromSettings();
                    break;
                case "Professional Navy":
                default:
                    ThemeColor = Color.FromArgb(0x17, 0x32, 0x4D);
                    ThemeDark = Color.FromArgb(0x10, 0x26, 0x3A);
                    ThemeLight = Color.FromArgb(0xDC, 0xEA, 0xF5);
                    AccentColor = Color.FromArgb(0x25, 0x63, 0xEB);
                    AccentDark = Color.FromArgb(0x1D, 0x4E, 0xD8);
                    BgColor = Color.FromArgb(0xF5, 0xF7, 0xFA);
                    PanelColor = Color.White;
                    TextColor = Color.FromArgb(0x1F, 0x29, 0x37);
                    SecondaryTextColor = Color.FromArgb(0x6B, 0x72, 0x80);
                    BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
                    GridHeaderColor = Color.FromArgb(0x17, 0x32, 0x4D);
                    ThemeSelection = Color.FromArgb(0xDB, 0xEA, 0xFE);
                    ThemeAltRow = Color.FromArgb(0xF8, 0xFA, 0xFC);
                    SuccessColor = Color.FromArgb(0x16, 0xA3, 0x4A);
                    WarningColor = Color.FromArgb(0xD9, 0x77, 0x06);
                    DangerColor = Color.FromArgb(0xDC, 0x26, 0x26);
                    DangerDark = Color.FromArgb(0xB9, 0x1C, 0x1C);
                    break;
            }
        }

        public static void ApplyFontSize(int size)
        {
            TitleFont = new Font("Segoe UI", size + 4, FontStyle.Bold);
            HeaderFont = new Font("Segoe UI", size + 1, FontStyle.Bold);
            NormalFont = new Font("Segoe UI", size, FontStyle.Regular);
            ButtonFont = new Font("Segoe UI", size, FontStyle.Bold);
            SmallFont = new Font("Segoe UI", Math.Max(8f, size - 1.5f), FontStyle.Regular);
        }

        public static Size GetPreferredFormSize()
        {
            string res = AppSettings.Get("FormResolution");
            if (string.IsNullOrEmpty(res)) res = "1024 x 768";
            int w = 1024, h = 768;
            try
            {
                var parts = res.ToLowerInvariant().Replace(" ", "").Split('x');
                if (parts.Length == 2) { int.TryParse(parts[0], out w); int.TryParse(parts[1], out h); }
            }
            catch { }
            if (w < 800) w = 800; if (h < 600) h = 600;
            if (w > 1280) w = 1280; if (h > 1024) h = 1024;
            return new Size(w, h);
        }

        public static void ApplyFormSize(Form form)
        {
            if (form == null || form.IsMdiContainer) return;
            var sz = GetPreferredFormSize();
            form.MinimumSize = new Size(800, 600);
            form.MaximumSize = new Size(1280, 1024);
            if (form.WindowState == FormWindowState.Normal) form.Size = sz;
            else { form.WindowState = FormWindowState.Normal; form.Size = sz; }
        }

        public static void ApplyFormSizeToAllChildren(Form mdiParent)
        {
            if (mdiParent == null) return;
            foreach (Form child in mdiParent.MdiChildren) ApplyFormSize(child);
        }

        public static void ApplyThemeLive(Form root)
        {
            InitializeTheme();
            if (root == null) return;
            root.BackColor = BgColor;
            ApplyToControlTree(root);
            if (root.IsMdiContainer)
            {
                foreach (Control c in root.Controls)
                    if (c is MdiClient mdi) mdi.BackColor = BgColor;
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
            if (c is Button btn) StyleButton(btn);
            else if (c is TextBox txt) StyleTextBox(txt);
            else if (c is ComboBox cmb) StyleComboBox(cmb);
            else if (c is DataGridView dgv) StyleGrid(dgv);
            else if (c is Label lbl)
            {
                if (lbl.Font.Bold && lbl.Font.Size >= 12) lbl.Font = TitleFont;
                else if (lbl.Font.Bold) lbl.Font = HeaderFont;
                else lbl.Font = NormalFont;
            }
            else if (c is GroupBox gb) { gb.Font = HeaderFont; gb.ForeColor = ThemeDark; }
            else if (c is CheckBox chk) chk.Font = NormalFont;
            else if (c is RadioButton rb) rb.Font = NormalFont;
            else if (c is DateTimePicker dtp) StyleDatePicker(dtp);
            else if (c is ListBox lb) { lb.Font = NormalFont; lb.BackColor = PanelColor; lb.ForeColor = TextColor; }
            else if (c is MenuStrip ms) { ms.BackColor = ThemeColor; ms.ForeColor = Color.White; ms.Font = NormalFont; }
            else if (c is StatusStrip ss) { ss.BackColor = ThemeDark; ss.Font = SmallFont; }
            else if (c is ToolStrip ts && !(c is MenuStrip) && !(c is StatusStrip)) ts.Font = ButtonFont;
            else if (c is Panel p)
            {
                if (p.Tag != null && p.Tag.ToString() == "FormBanner")
                {
                    p.BackColor = ThemeColor;
                    p.Invalidate();
                }
            }
            foreach (Control child in c.Controls) ApplyToControlTree(child);
        }

        public static void StyleButton(Button btn)
        {
            if (btn == null) return;
            string t = (btn.Text ?? "").ToUpperInvariant();
            if (t.Contains("DELETE") || t.Contains("REMOVE") || t.Contains("VOID")) { StyleDangerButton(btn); return; }
            if (t.Contains("CANCEL") || t.Contains("CLOSE") || t == "NO" || t.Contains("BACK")) { StyleSecondaryButton(btn); return; }
            StylePrimaryButton(btn);
        }

        public static void StyleTextBox(TextBox txt)
        {
            if (txt == null) return;
            txt.Font = NormalFont;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = Color.White;
            txt.ForeColor = TextColor;
            txt.Enter += (s, e) => BeginInvokeSelectAll(txt);
            txt.Click += (s, e) => { if (txt.SelectionLength == 0) BeginInvokeSelectAll(txt); };
        }

        private static void BeginInvokeSelectAll(TextBox tb)
        {
            if (tb == null || tb.IsDisposed) return;
            if (tb.IsHandleCreated)
                tb.BeginInvoke(new Action(() => { if (!tb.IsDisposed) tb.SelectAll(); }));
            else tb.SelectAll();
        }

        public static void EnableAutoSelectOnFocus(Control root)
        {
            if (root == null) return;
            WireAutoSelect(root);
            root.ControlAdded += (s, e) => { if (e.Control != null) WireAutoSelect(e.Control); };
        }

        private static void WireAutoSelect(Control c)
        {
            var tb = c as TextBox;
            if (tb != null)
            {
                tb.Enter -= AutoSelect_TextEnter; tb.Enter += AutoSelect_TextEnter;
                tb.Click -= AutoSelect_TextClick; tb.Click += AutoSelect_TextClick;
            }
            var cb = c as ComboBox;
            if (cb != null && cb.DropDownStyle != ComboBoxStyle.DropDownList)
            { cb.Enter -= AutoSelect_ComboEnter; cb.Enter += AutoSelect_ComboEnter; }
            var nud = c as NumericUpDown;
            if (nud != null) { nud.Enter -= AutoSelect_NudEnter; nud.Enter += AutoSelect_NudEnter; }
            foreach (Control child in c.Controls) WireAutoSelect(child);
        }

        private static void AutoSelect_TextEnter(object sender, EventArgs e) { var tb = sender as TextBox; if (tb != null) BeginInvokeSelectAll(tb); }
        private static void AutoSelect_TextClick(object sender, EventArgs e) { var tb = sender as TextBox; if (tb != null && tb.SelectionLength == 0) BeginInvokeSelectAll(tb); }
        private static void AutoSelect_ComboEnter(object sender, EventArgs e) { var cb = sender as ComboBox; if (cb != null) { try { cb.SelectAll(); } catch { } } }
        private static void AutoSelect_NudEnter(object sender, EventArgs e) { var nud = sender as NumericUpDown; if (nud != null) { try { nud.Select(0, nud.Text.Length); } catch { } } }

        public static void StyleComboBox(ComboBox cmb)
        {
            if (cmb == null) return;
            cmb.Font = NormalFont; cmb.FlatStyle = FlatStyle.Flat; cmb.BackColor = Color.White; cmb.ForeColor = TextColor;
        }

        public static void StyleGrid(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.BackgroundColor = PanelColor;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeDark;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 36;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.DefaultCellStyle.BackColor = PanelColor;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.Font = NormalFont;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.DefaultCellStyle.SelectionBackColor = ThemeSelection;
            dgv.DefaultCellStyle.SelectionForeColor = TextColor;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeAltRow;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextColor;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = ThemeSelection;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextColor;
            dgv.RowTemplate.Height = 30;
            dgv.GridColor = BorderColor;
        }

        public static void StyleDatePicker(DateTimePicker dtp)
        {
            if (dtp == null) return;
            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "dd/MM/yyyy";
            dtp.Font = NormalFont;
        }

        public static void FadeIn(Form form)
        {
            form.Opacity = 0;
            var t = new Timer { Interval = 12 };
            t.Tick += (s, e) => { if (form.Opacity >= 1) { t.Stop(); t.Dispose(); } else form.Opacity += 0.12; };
            t.Start();
        }

        public static bool ConfirmExit()
        {
            return MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        public static void AttachF4Close(Form form, bool confirmOnClose = true)
        {
            form.KeyPreview = true;
            form.KeyDown += (s, e) => { if (e.KeyCode == Keys.F4) { e.SuppressKeyPress = true; form.Close(); } };
            if (!confirmOnClose) { form.Tag = "NOSAVECONFIRM"; return; }
            form.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    if (form.Tag != null && form.Tag.ToString() == "NOSAVECONFIRM") return;
                    if (!ConfirmExit()) e.Cancel = true;
                }
            };
        }

        public static void AttachF4Close(Form form) { AttachF4Close(form, true); }

        public static void AttachEnterNavigation(Form form)
        {
            if (form == null) return;
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Tab) return;
                Control c = form.ActiveControl;
                if (c is TextBox || c is ComboBox || c is NumericUpDown || c is CheckBox || c is DateTimePicker)
                {
                    if (c is TextBox tb && tb.Multiline && e.KeyCode == Keys.Enter) return;
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    form.SelectNextControl(c, true, true, true, true);
                }
            };
            form.Shown += (s, e) => EnableAutoSelectOnFocus(form);
        }

        public static decimal GetMaxDiscount(string role)
        {
            string key = role == "Admin" ? "MaxDiscountAdmin" : "MaxDiscountUser";
            string val = AppSettings.Get(key);
            decimal d = 0; decimal.TryParse(val, out d); return d;
        }

        public static bool IsPrintAllowed()
        {
            if (LicenseReader.Current != null) return LicenseReader.Current.AllowPrint;
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
