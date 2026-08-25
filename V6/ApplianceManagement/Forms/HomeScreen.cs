using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public class HomeScreen : Panel
    {
        private const string DevelopedBy = "Developed by Mohsin";
        private const string Tagline = "Inventory Management System  •  SMART • FAST • RELIABLE";
        private static readonly Pen CardBorderPen = new Pen(Color.FromArgb(226, 235, 244));

        private readonly User _user;
        private Size _laidOut;

        private Panel _tabStrip, _header, _footer;
        private Button _tabDash, _tabMain, _btnRefresh;
        private Label _lblRefreshed;
        private Label _lblStore, _lblStoreInfo, _lblRight1, _lblRight2;
        private Panel _footerIcon;
        private Label _footerIconText, _footerTitle, _footerTag, _footerCredit1, _footerCredit2;

        private Panel _welcomeView;
        private PictureBox _pic;
        private Label _lblTitle, _lblSub, _lblHint;

        private Panel _dashView;
        private Label _lblRecent, _lblLow;
        private DataGridView _dgvRecent, _dgvLow;

        private sealed class KpiCard
        {
            public Panel Panel;
            public Label Glyph, Value, Caption;
            public Color Accent;
        }

        private readonly List<KpiCard> _cards = new List<KpiCard>();

        public HomeScreen(User user)
        {
            _user = user;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = UiHelper.BgColor;
            BuildChrome();
            BuildWelcomeView();
            BuildDashboardView();
            StyleAll();
            SwitchView(true);
            this.Resize += (s, e) =>
            {
                if (this.Size == _laidOut) return;
                LayoutAll();
            };
            LayoutAll();
        }

        private void BuildChrome()
        {
            _tabStrip = new Panel { BackColor = Color.White };
            _tabDash = MakeTab("Dashboard");
            _tabMain = MakeTab("Welcome");
            _tabDash.Click += (s, e) => SwitchView(true);
            _tabMain.Click += (s, e) => SwitchView(false);

            _btnRefresh = new Button
            {
                Text = "Refresh",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(96, 32),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += (s, e) => RefreshData();

            _lblRefreshed = new Label { AutoSize = true, ForeColor = Color.FromArgb(110, 122, 136), TextAlign = ContentAlignment.MiddleRight };
            _tabStrip.Controls.AddRange(new Control[] { _tabDash, _tabMain, _btnRefresh, _lblRefreshed });

            _header = new Panel { BackColor = Color.White };
            _lblStore = new Label { AutoSize = true, Location = new Point(16, 10) };
            _lblStoreInfo = new Label { AutoSize = true, Location = new Point(18, 46), ForeColor = Color.FromArgb(110, 122, 136) };
            _lblRight1 = new Label { AutoSize = true, ForeColor = Color.FromArgb(110, 122, 136) };
            _lblRight2 = new Label { AutoSize = true, ForeColor = Color.FromArgb(110, 122, 136) };
            _header.Controls.AddRange(new Control[] { _lblStore, _lblStoreInfo, _lblRight1, _lblRight2 });

            _footer = new Panel();
            _footerIcon = new Panel { Size = new Size(38, 38), Location = new Point(14, 8) };
            _footerIconText = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White };
            _footerIcon.Controls.Add(_footerIconText);
            _footerTitle = new Label { AutoSize = true, ForeColor = Color.White, Location = new Point(64, 8) };
            _footerTag = new Label { AutoSize = true, ForeColor = Color.FromArgb(210, 224, 235), Location = new Point(64, 29) };
            _footerCredit1 = new Label { AutoSize = true, ForeColor = Color.FromArgb(210, 224, 235) };
            _footerCredit2 = new Label { AutoSize = true, ForeColor = Color.FromArgb(170, 190, 207) };
            _footer.Controls.AddRange(new Control[] { _footerIcon, _footerTitle, _footerTag, _footerCredit1, _footerCredit2 });

            Controls.AddRange(new Control[] { _tabStrip, _header, _footer });
        }

        private Button MakeTab(string text)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 32),
                Location = new Point(12, 7),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void BuildWelcomeView()
        {
            _welcomeView = new Panel();
            _pic = new PictureBox { BackColor = Color.Transparent, SizeMode = PictureBoxSizeMode.AutoSize };
            try { _pic.Image = LoadWelcomeArt(); }
            catch { _pic.Dispose(); _pic = null; }
            _lblTitle = new Label { AutoSize = true, BackColor = Color.Transparent };
            _lblSub = new Label { AutoSize = true, BackColor = Color.Transparent };
            _lblHint = new Label { AutoSize = true, BackColor = Color.Transparent };
            if (_pic != null) _welcomeView.Controls.Add(_pic);
            _welcomeView.Controls.AddRange(new Control[] { _lblTitle, _lblSub, _lblHint });
            Controls.Add(_welcomeView);
        }

        private Bitmap LoadWelcomeArt()
        {
            SvgImage svg;
            try { svg = SvgImage.FromEmbeddedResource("ApplianceManagement.Resources.main_screen.svg"); }
            catch
            {
                string file = Path.Combine(Application.StartupPath, "main_screen.svg");
                if (!File.Exists(file)) throw;
                svg = SvgImage.FromFile(file);
            }
            return svg.Render(420);
        }

        private void BuildDashboardView()
        {
            _dashView = new Panel();
            _cards.Add(MakeCard("Rs", Color.FromArgb(39, 174, 96)));
            _cards.Add(MakeCard("In", Color.FromArgb(41, 128, 185)));
            _cards.Add(MakeCard("Mo", Color.FromArgb(22, 160, 133)));
            _cards.Add(MakeCard("Pu", Color.FromArgb(142, 68, 173)));
            _cards.Add(MakeCard("St", Color.FromArgb(243, 156, 18)));
            _cards.Add(MakeCard("#", Color.FromArgb(52, 73, 94)));
            _cards.Add(MakeCard("!", Color.FromArgb(192, 57, 43)));
            _cards.Add(MakeCard("Cu", Color.FromArgb(26, 188, 156)));

            _lblRecent = new Label { AutoSize = true };
            _lblLow = new Label { AutoSize = true };

            _dgvRecent = MakeGrid();
            _dgvRecent.Columns.Add("c0", "Date");
            _dgvRecent.Columns.Add("c1", "Invoice");
            _dgvRecent.Columns.Add("c2", "Customer");
            _dgvRecent.Columns.Add("c3", "Amount");
            _dgvRecent.Columns[0].Width = 110;
            _dgvRecent.Columns[1].Width = 110;
            _dgvRecent.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _dgvRecent.Columns[3].Width = 120;
            _dgvRecent.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvRecent.Columns[3].DefaultCellStyle.Format = "N0";

            _dgvLow = MakeGrid();
            _dgvLow.Columns.Add("c0", "Product");
            _dgvLow.Columns.Add("c1", "Category");
            _dgvLow.Columns.Add("c2", "Stock");
            _dgvLow.Columns.Add("c3", "Minimum");
            _dgvLow.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _dgvLow.Columns[1].Width = 140;
            _dgvLow.Columns[2].Width = 80;
            _dgvLow.Columns[3].Width = 80;
            _dgvLow.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvLow.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dashView.Controls.AddRange(new Control[] { _lblRecent, _lblLow, _dgvRecent, _dgvLow });
            foreach (var c in _cards) _dashView.Controls.Add(c.Panel);
            Controls.Add(_dashView);
        }

        private DataGridView MakeGrid()
        {
            var dgv = new DataGridView { AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false };
            UiHelper.StyleGrid(dgv);
            return dgv;
        }

        private KpiCard MakeCard(string glyph, Color accent)
        {
            var card = new KpiCard { Accent = accent };
            card.Panel = new Panel { BackColor = Color.White, Size = new Size(210, 88) };
            card.Panel.Paint += (s, e) =>
                e.Graphics.DrawRectangle(CardBorderPen, 0, 0, card.Panel.Width - 1, card.Panel.Height - 1);
            var icon = new Panel { BackColor = accent, Size = new Size(44, 44), Location = new Point(14, 22) };
            card.Glyph = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Text = glyph };
            icon.Controls.Add(card.Glyph);
            card.Value = new Label { AutoSize = true, Location = new Point(70, 16), ForeColor = Color.FromArgb(44, 62, 80) };
            card.Caption = new Label { AutoSize = true, Location = new Point(70, 52), ForeColor = Color.FromArgb(130, 144, 158) };
            card.Panel.Controls.AddRange(new Control[] { icon, card.Value, card.Caption });
            return card;
        }

        private void SetCard(int index, string value, string caption)
        {
            _cards[index].Value.Text = value;
            _cards[index].Caption.Text = caption;
        }

        private void SwitchView(bool dashboard)
        {
            _dashView.Visible = dashboard;
            _welcomeView.Visible = !dashboard;
            StyleTabs(dashboard);
            if (dashboard) RefreshData();
        }

        private void StyleTabs(bool dashboard)
        {
            Button on = dashboard ? _tabDash : _tabMain;
            Button off = dashboard ? _tabMain : _tabDash;
            on.BackColor = UiHelper.ThemeColor;
            on.ForeColor = Color.White;
            off.BackColor = Color.FromArgb(238, 242, 247);
            off.ForeColor = Color.FromArgb(70, 85, 100);
        }

        public void RefreshData()
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime monthStart = new DateTime(today.Year, today.Month, 1);
                var sales = new SaleRepository().GetSales(monthStart, today);
                var purchases = new PurchaseRepository().GetPurchases(monthStart, today);
                var products = new ProductRepository().GetAllActive();
                var lowStock = new ProductRepository().GetLowStock();
                int customers = new CustomerRepository().CountActive();
                var todaySales = sales.Where(s => s.SaleDate.Date == today).ToList();
                var todayPurch = purchases.Where(p => p.PurchaseDate.Date == today).ToList();

                SetCard(0, "Rs " + todaySales.Sum(x => x.NetAmount).ToString("N0"), todaySales.Count + " bills today");
                SetCard(1, "Rs " + todayPurch.Sum(x => x.NetAmount).ToString("N0"), todayPurch.Count + " invoices today");
                SetCard(2, "Rs " + sales.Sum(x => x.NetAmount).ToString("N0"), "since " + monthStart.ToString("dd MMM yyyy"));
                SetCard(3, "Rs " + purchases.Sum(x => x.NetAmount).ToString("N0"), "since " + monthStart.ToString("dd MMM yyyy"));
                SetCard(4, "Rs " + products.Sum(p => (decimal)p.CurrentStock * p.SalePrice).ToString("N0"), products.Sum(p => p.CurrentStock) + " units in stock");
                SetCard(5, products.Count.ToString("N0"), "active products");
                SetCard(6, lowStock.Count.ToString("N0"), lowStock.Count == 0 ? "all items healthy" : "at or below minimum");
                SetCard(7, customers.ToString("N0"), "registered customers");

                var recent = sales.Where(s => s.SaleDate.Date >= today.AddDays(-6)).Take(12).ToList();
                _lblRecent.Text = "Recent Sales — last 7 days (" + recent.Count + ")";
                _dgvRecent.Rows.Clear();
                foreach (var s in recent)
                    _dgvRecent.Rows.Add(s.SaleDate.ToString("dd MMM HH:mm"), s.InvoiceNo, s.CustomerName, s.NetAmount);

                var lowTop = lowStock.Take(12).ToList();
                _lblLow.Text = "Low Stock Items (" + lowStock.Count + ")";
                _dgvLow.Rows.Clear();
                foreach (var p in lowTop)
                {
                    int row = _dgvLow.Rows.Add(p.ProductName, p.CategoryName, p.CurrentStock, p.MinimumStock);
                    if (p.CurrentStock <= 0)
                        _dgvLow.Rows[row].DefaultCellStyle.ForeColor = UiHelper.DangerColor;
                }
                _lblRefreshed.Text = "Refreshed: " + DateTime.Now.ToString("dd MMM yyyy HH:mm");
            }
            catch (Exception ex)
            {
                _lblRefreshed.Text = "Refresh failed — check database";
                for (int i = 0; i < _cards.Count; i++) SetCard(i, "—", ex.Message.Length > 60 ? ex.Message.Substring(0, 60) : ex.Message);
            }
            LayoutAll();
        }

        public void RefreshBranding()
        {
            StyleAll();
        }

        private void StyleAll()
        {
            BackColor = UiHelper.BgColor;
            _tabStrip.BackColor = Color.White;
            _header.BackColor = Color.White;
            _footer.BackColor = UiHelper.ThemeDark;
            _lblStore.Text = UiHelper.GetShopName().ToUpperInvariant();
            _lblStore.Font = new Font(UiHelper.TitleFont.FontFamily, UiHelper.TitleFont.Size + 2, FontStyle.Bold);
            _lblStore.ForeColor = UiHelper.ThemeDark;
            string info = UiHelper.AppName;
            string phone = UiHelper.GetShopPhone();
            if (!string.IsNullOrEmpty(phone)) info += "  •  " + phone;
            _lblStoreInfo.Text = info;
            _lblStoreInfo.Font = UiHelper.SmallFont;
            _lblRight1.Text = "System date: " + DateTime.Now.ToString("dd-MMM-yyyy");
            _lblRight2.Text = "Signed in as " + _user.FullName + " (" + _user.Role + ")";
            _lblRight1.Font = UiHelper.SmallFont;
            _lblRight2.Font = UiHelper.SmallFont;
            _btnRefresh.BackColor = UiHelper.ThemeColor;
            _btnRefresh.ForeColor = Color.White;
            _btnRefresh.Font = UiHelper.ButtonFont;
            _lblRefreshed.Font = UiHelper.SmallFont;
            _footerIcon.BackColor = Color.White;
            _footerIconText.Text = Initials(UiHelper.AppName);
            _footerIconText.Font = new Font(UiHelper.TitleFont.FontFamily, UiHelper.TitleFont.Size, FontStyle.Bold);
            _footerIconText.ForeColor = UiHelper.ThemeColor;
            _footerTitle.Text = UiHelper.AppName;
            _footerTitle.Font = new Font(UiHelper.HeaderFont.FontFamily, UiHelper.HeaderFont.Size, FontStyle.Bold);
            _footerTag.Text = Tagline;
            _footerTag.Font = UiHelper.SmallFont;
            _footerCredit1.Text = DevelopedBy;
            _footerCredit1.Font = UiHelper.SmallFont;
            _footerCredit2.Text = "© " + DateTime.Now.Year + " " + UiHelper.AppName;
            _footerCredit2.Font = UiHelper.SmallFont;
            foreach (var c in _cards)
            {
                c.Value.Font = new Font(UiHelper.TitleFont.FontFamily, UiHelper.TitleFont.Size + 1, FontStyle.Bold);
                c.Caption.Font = UiHelper.SmallFont;
                c.Glyph.Font = new Font(UiHelper.ButtonFont.FontFamily, UiHelper.ButtonFont.Size + 2, FontStyle.Bold);
            }
            _lblRecent.Font = UiHelper.HeaderFont;
            _lblRecent.ForeColor = UiHelper.ThemeDark;
            _lblLow.Font = UiHelper.HeaderFont;
            _lblLow.ForeColor = UiHelper.ThemeDark;
            _dgvRecent.Font = UiHelper.NormalFont;
            _dgvLow.Font = UiHelper.NormalFont;
            StyleTabs(_dashView.Visible);
            _lblTitle.Text = UiHelper.AppName;
            _lblTitle.Font = new Font(UiHelper.TitleFont.FontFamily, UiHelper.TitleFont.Size + 6, FontStyle.Bold);
            _lblTitle.ForeColor = UiHelper.ThemeDark;
            _lblSub.Text = "Welcome, " + _user.FullName + " (" + _user.Role + ")  —  " + UiHelper.GetShopName();
            _lblSub.Font = UiHelper.HeaderFont;
            _lblSub.ForeColor = Color.FromArgb(80, 95, 110);
            _lblHint.Text = "F2 Sale   F3 Purchase   F4 Close   F12 Save";
            _lblHint.Font = UiHelper.SmallFont;
            _lblHint.ForeColor = Color.Gray;
            LayoutAll();
        }

        private static string Initials(string name)
        {
            var parts = (name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "A";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
        }

        private void LayoutAll()
        {
            if (_tabStrip == null || Width < 50 || Height < 50) return;
            _laidOut = this.Size;
            int w = Width, h = Height;
            _tabStrip.SetBounds(0, 0, w, 48);
            _header.SetBounds(0, 48, w, 76);
            _footer.SetBounds(0, h - 54, w, 54);
            _tabDash.Location = new Point(12, 7);
            _tabMain.Location = new Point(12 + _tabDash.Width + 8, 7);
            _btnRefresh.Location = new Point(w - 12 - _btnRefresh.Width, 8);
            _lblRefreshed.Location = new Point(_btnRefresh.Left - 10 - _lblRefreshed.Width, 15);
            _lblRight1.Location = new Point(w - 16 - _lblRight1.Width, 14);
            _lblRight2.Location = new Point(w - 16 - _lblRight2.Width, 38);
            _footerIcon.Location = new Point(14, 8);
            _footerTitle.Location = new Point(64, 8);
            _footerTag.Location = new Point(64, 30);
            _footerCredit1.Location = new Point(w - 14 - _footerCredit1.Width, 8);
            _footerCredit2.Location = new Point(w - 14 - _footerCredit2.Width, 29);
            int top = 48 + 76;
            var content = new Rectangle(0, top, w, Math.Max(120, h - top - 54));
            _welcomeView.SetBounds(content.X, content.Y, content.Width, content.Height);
            _dashView.SetBounds(content.X, content.Y, content.Width, content.Height);
            LayoutWelcome(content);
            LayoutDashboard(content);
        }

        private void LayoutWelcome(Rectangle content)
        {
            var items = new List<Control>();
            if (_pic != null) items.Add(_pic);
            items.Add(_lblTitle); items.Add(_lblSub); items.Add(_lblHint);
            int total = 18 * (items.Count - 1);
            foreach (Control c in items) total += c.Height;
            int y = content.Y + (content.Height - total) / 2 - Math.Max(0, content.Height / 20);
            foreach (Control c in items)
            {
                c.Location = new Point(content.X + (content.Width - c.Width) / 2, y);
                y += c.Height + 18;
            }
        }

        private void LayoutDashboard(Rectangle content)
        {
            const int pad = 16, gap = 14, cardH = 88;
            int cardW = Math.Max(150, Math.Min(240, (content.Width - 2 * pad - 3 * gap) / 4));
            int blockW = 4 * cardW + 3 * gap;
            int x0 = content.X + Math.Max(pad, (content.Width - blockW) / 2);
            int y = content.Y + pad;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].Panel.SetBounds(x0 + (i % 4) * (cardW + gap), y + (i / 4) * (cardH + gap), cardW, cardH);
            int listsY = content.Y + pad + 2 * cardH + gap + 18;
            int gridH = Math.Max(120, content.Y + content.Height - listsY - 28 - pad);
            int gridW = Math.Max(200, (content.Width - 2 * pad - gap) / 2);
            _lblRecent.Location = new Point(content.X + pad, listsY);
            _lblLow.Location = new Point(content.X + pad + gridW + gap, listsY);
            _dgvRecent.SetBounds(content.X + pad, listsY + 26, gridW, gridH);
            _dgvLow.SetBounds(content.X + pad + gridW + gap, listsY + 26, gridW, gridH);
        }
    }
}
