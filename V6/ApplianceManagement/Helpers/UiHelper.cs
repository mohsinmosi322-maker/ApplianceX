using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    public static class FormAccent
    {
        // Professional palette — distinct per module, muted enough for long sessions
        public static readonly Color Sale = Color.FromArgb(25, 118, 210);       // blue
        public static readonly Color SaleDark = Color.FromArgb(13, 71, 161);
        public static readonly Color Purchase = Color.FromArgb(46, 125, 50);   // green
        public static readonly Color PurchaseDark = Color.FromArgb(27, 94, 32);
        public static readonly Color SaleReturn = Color.FromArgb(230, 126, 34); // orange
        public static readonly Color SaleReturnDark = Color.FromArgb(175, 90, 20);
        public static readonly Color PurchaseReturn = Color.FromArgb(121, 85, 72); // brown
        public static readonly Color PurchaseReturnDark = Color.FromArgb(78, 52, 46);
        public static readonly Color NewItem = Color.FromArgb(123, 31, 162);   // purple
        public static readonly Color NewItemDark = Color.FromArgb(74, 20, 140);
        public static readonly Color Inventory = Color.FromArgb(0, 121, 107);  // teal
        public static readonly Color InventoryDark = Color.FromArgb(0, 77, 64);
        public static readonly Color Reports = Color.FromArgb(55, 71, 79);     // blue-grey
        public static readonly Color ReportsDark = Color.FromArgb(38, 50, 56);
        public static readonly Color Settings = Color.FromArgb(57, 73, 171);   // indigo
        public static readonly Color SettingsDark = Color.FromArgb(40, 53, 147);
        public static readonly Color LowStock = Color.FromArgb(198, 40, 40);   // red
        public static readonly Color LowStockDark = Color.FromArgb(150, 30, 30);
        public static readonly Color Login = Color.FromArgb(33, 33, 33);
        public static readonly Color Masters = Color.FromArgb(0, 105, 92);
        public static readonly Color MastersDark = Color.FromArgb(0, 77, 64);
        public static readonly Color Accounts = Color.FromArgb(69, 90, 100);
        public static readonly Color AccountsDark = Color.FromArgb(48, 63, 71);
    }

    public static class UiHelper
    {
        public static Font TitleFont { get; private set; } = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static Font HeaderFont { get; private set; } = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static Font NormalFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font ButtonFont { get; private set; } = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static Font SmallFont { get; private set; } = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        public static Color ThemeColor { get; private set; } = Color.FromArgb(25, 118, 210);
        public static Color ThemeDark { get; private set; } = Color.FromArgb(13, 71, 161);
        public static Color ThemeLight { get; private set; } = Color.FromArgb(227, 242, 253);
        public static Color ThemeAltRow { get; private set; } = Color.FromArgb(245, 248, 252);
        public static Color ThemeSelection { get; private set; } = Color.FromArgb(66, 165, 245);
        public static Color BgColor { get; private set; } = Color.FromArgb(245, 247, 250);
        public static Color PanelColor { get; private set; } = Color.White;
        public static Color GridHeaderColor { get; private set; } = Color.FromArgb(25, 118, 210);
        public static Color DangerColor { get; private set; } = Color.FromArgb(198, 40, 40);

        public static string AppName
        {
            get
            {
                if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.SoftwareName))
                    return LicenseReader.Current.SoftwareName;
                return "ApplianceX";
            }
        }

        public static string StoreName
        {
            get
            {
                if (LicenseReader.Current != null && !string.IsNullOrEmpty(LicenseReader.Current.StoreName))
                    return LicenseReader.Current.StoreName;
                return "Store";
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

        public static Panel CreateFormBanner(string title, string description, Color accent, Color accentDark)
        {
            Panel banner = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = accent };
            banner.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accentDark });
            banner.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            banner.Controls.Add(new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(235, 245, 255),
                Location = new Point(18, 30),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            return banner;
        }

        public static void StyleAccentButton(Button btn, Color accent, Color accentDark)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = accent;
            btn.ForeColor = Color.White;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            if (btn.Height < 30) btn.Height = 34;
            btn.FlatAppearance.MouseOverBackColor = accentDark;
            btn.FlatAppearance.MouseDownBackColor = accentDark;
        }

        public static void StyleGridWithAccent(DataGridView dgv, Color headerColor)
        {
            StyleGrid(dgv);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerColor;
            dgv.GridColor = Color.FromArgb(
                Math.Min(255, headerColor.R + 90),
                Math.Min(255, headerColor.G + 90),
                Math.Min(255, headerColor.B + 90));
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
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeDark;
            dgv.ColumnHeadersHeight = 36;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeAltRow;
            dgv.DefaultCellStyle.SelectionBackColor = ThemeSelection;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = NormalFont;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgv.RowTemplate.Height = 30;
            dgv.GridColor = ThemeLight;
        }

        public static void StyleTextBox(TextBox tb)
        {
            tb.Font = NormalFont;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = Color.White;
            // Focus: select all so user can type over value immediately
            tb.Enter += (s, e) =>
            {
                BeginInvokeSelectAll(tb);
            };
            tb.Click += (s, e) =>
            {
                if (tb.SelectionLength == 0)
                    BeginInvokeSelectAll(tb);
            };
        }

        public static void StyleDatePicker(DateTimePicker dtp)
        {
            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "dd/MM/yyyy";
            dtp.Font = NormalFont;
        }

        /// <summary>
        /// Recursively wire every TextBox / ComboBox / NumericUpDown under root
        /// so focus or click selects all text (POS-friendly).
        /// </summary>
        public static void EnableAutoSelectOnFocus(Control root)
        {
            if (root == null) return;
            WireAutoSelect(root);
            root.ControlAdded += (s, e) =>
            {
                if (e.Control != null) WireAutoSelect(e.Control);
            };
        }

        private static void WireAutoSelect(Control c)
        {
            var tb = c as TextBox;
            if (tb != null)
            {
                tb.Enter -= TextBox_SelectAllHandler;
                tb.Enter += TextBox_SelectAllHandler;
                tb.Click -= TextBox_ClickSelectHandler;
                tb.Click += TextBox_ClickSelectHandler;
            }
            var cb = c as ComboBox;
            if (cb != null && cb.DropDownStyle != ComboBoxStyle.DropDownList)
            {
                cb.Enter -= Combo_SelectAllHandler;
                cb.Enter += Combo_SelectAllHandler;
            }
            var nud = c as NumericUpDown;
            if (nud != null)
            {
                nud.Enter -= Nud_SelectAllHandler;
                nud.Enter += Nud_SelectAllHandler;
            }

            foreach (Control child in c.Controls)
                WireAutoSelect(child);
        }

        private static void TextBox_SelectAllHandler(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null) BeginInvokeSelectAll(tb);
        }

        private static void TextBox_ClickSelectHandler(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.SelectionLength == 0)
                BeginInvokeSelectAll(tb);
        }

        private static void Combo_SelectAllHandler(object sender, EventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb != null)
            {
                try { cb.SelectAll(); } catch { }
            }
        }

        private static void Nud_SelectAllHandler(object sender, EventArgs e)
        {
            var nud = sender as NumericUpDown;
            if (nud != null)
            {
                try { nud.Select(0, nud.Text.Length); } catch { }
            }
        }

        private static void BeginInvokeSelectAll(TextBox tb)
        {
            if (tb == null || tb.IsDisposed) return;
            // Defer so Windows finishes focus before SelectAll
            if (tb.IsHandleCreated)
            {
                tb.BeginInvoke(new Action(() =>
                {
                    if (!tb.IsDisposed) tb.SelectAll();
                }));
            }
            else
            {
                tb.SelectAll();
            }
        }

        public static void AttachEnterNavigation(Form form)
        {
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !(form.ActiveControl is Button) && !(form.ActiveControl is DataGridView))
                {
                    e.SuppressKeyPress = true;
                    form.SelectNextControl(form.ActiveControl, true, true, true, true);
                }
            };
            // Every form gets auto-select on all text fields
            form.Shown += (s, e) => EnableAutoSelectOnFocus(form);
        }

        public static void AttachF4Close(Form form)
        {
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F4 && !e.Alt)
                {
                    e.SuppressKeyPress = true;
                    form.Close();
                }
            };
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

        public static void ApplyTheme(Form form)
        {
            form.BackColor = BgColor;
            form.Font = NormalFont;
            EnableAutoSelectOnFocus(form);
        }
    }
}
