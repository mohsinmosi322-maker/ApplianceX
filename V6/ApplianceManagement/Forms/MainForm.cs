using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        public User CurrentUser { get; private set; }

        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private ToolStripMenuItem mnuWindows;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblShop, lblBrand, lblClock;
        private Timer clockTimer;
        private HomeScreen homeScreen;
        private Form homeHost;
        private MdiClient mdiClient;
        private bool syncingHome;

        public MainForm(User user)
        {
            CurrentUser = user;
            Instance = this;
            UiHelper.InitializeTheme();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            ApplyPermissions();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED — stops child-control flicker
                return cp;
            }
        }

        private void InitializeComponent()
        {
            this.IsMdiContainer = true;
            this.BackColor = UiHelper.BgColor;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 700);
            this.FormClosing += MainForm_FormClosing;
            this.KeyPreview = true;
            this.MdiChildActivate += (s, e) => SyncHome();

            string shop = UiHelper.GetShopName();
            this.Text = UiHelper.AppName + "  —  " + shop;

            BuildMenu();
            BuildToolbar();
            BuildStatus();

            this.Load += MainForm_Load;
        }

        private void BuildMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            menuStrip.BackColor = UiHelper.ThemeColor;
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = new Font(UiHelper.NormalFont.FontFamily, UiHelper.NormalFont.Size, FontStyle.Regular);
            menuStrip.Padding = new Padding(8, 4, 8, 4);
            menuStrip.Renderer = new ThemeMenuRenderer();

            var mnuFile = new ToolStripMenuItem("  File  ");
            mnuFile.DropDownItems.Add("Logout", null, (s, e) => DoLogout());
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            mnuFile.DropDownItems.Add("Exit", null, (s, e) => this.Close());

            var mnuTrans = new ToolStripMenuItem("  Transactions  ");
            mnuTrans.DropDownItems.Add("Sale\tF2", null, (s, e) => OpenChild(new SaleForm(), "SALE"));
            mnuTrans.DropDownItems.Add("Purchase\tF3", null, (s, e) => OpenChild(new PurchaseForm(), "PURCHASE"));

            var mnuInv = new ToolStripMenuItem("  Inventory  ");
            mnuInv.DropDownItems.Add("New Item", null, (s, e) => OpenChild(new NewItemForm(), "NEWITEM"));
            mnuInv.DropDownItems.Add("Stock Position", null, (s, e) => OpenChild(new InventoryForm(), "INVENTORY"));
            mnuInv.DropDownItems.Add("Low Stock Report", null, (s, e) => OpenChild(new LowStockForm(), "INVENTORY"));

            var mnuRep = new ToolStripMenuItem("  Reports  ");
            mnuRep.DropDownItems.Add("Sales Report", null, (s, e) => OpenChild(new ReportsForm("SALES"), "REPORTS"));
            mnuRep.DropDownItems.Add("Purchase Report", null, (s, e) => OpenChild(new ReportsForm("PURCHASE"), "REPORTS"));
            mnuRep.DropDownItems.Add("Stock Report", null, (s, e) => OpenChild(new ReportsForm("STOCK"), "REPORTS"));
            mnuRep.DropDownItems.Add("Profit Report", null, (s, e) => OpenChild(new ReportsForm("PROFIT"), "REPORTS"));

            var mnuSet = new ToolStripMenuItem("  Settings  ");
            mnuSet.DropDownItems.Add("Appearance & Limits", null, (s, e) => OpenChild(new SettingsForm(), "SETTINGS"));

            mnuWindows = new ToolStripMenuItem("  Windows  ");
            mnuWindows.DropDownOpening += MnuWindows_DropDownOpening;

            var mnuHelp = new ToolStripMenuItem("  Help  ");
            mnuHelp.DropDownItems.Add("Shortcuts", null, (s, e) => ShowShortcuts());
            mnuHelp.DropDownItems.Add("About", null, (s, e) =>
                MessageBox.Show(UiHelper.AppName + "  v" + UiHelper.AppVersion + "\n" + UiHelper.GetShopName() +
                    "\n\nF2 Sale   F3 Purchase   F4 Close   F12 Save",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuTrans, mnuInv, mnuRep, mnuSet, mnuWindows, mnuHelp });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) { OpenChild(new SaleForm(), "SALE"); e.Handled = true; }
                if (e.KeyCode == Keys.F3) { OpenChild(new PurchaseForm(), "PURCHASE"); e.Handled = true; }
            };
        }

        private void BuildToolbar()
        {
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.BackColor = Color.FromArgb(248, 250, 252);
            toolStrip.Padding = new Padding(10, 8, 10, 8);
            toolStrip.ImageScalingSize = new Size(20, 20);
            toolStrip.Font = UiHelper.ButtonFont;
            toolStrip.Height = 52;
            toolStrip.Renderer = new ToolStripProfessionalRenderer(new LightToolTable());

            toolStrip.Items.Add(MakeToolButton("Sale", "F2", (s, e) => OpenChild(new SaleForm(), "SALE")));
            toolStrip.Items.Add(MakeToolButton("Purchase", "F3", (s, e) => OpenChild(new PurchaseForm(), "PURCHASE")));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(MakeToolButton("New Item", "", (s, e) => OpenChild(new NewItemForm(), "NEWITEM")));
            toolStrip.Items.Add(MakeToolButton("Stock", "", (s, e) => OpenChild(new InventoryForm(), "INVENTORY", true)));
            toolStrip.Items.Add(MakeToolButton("Low Stock", "", (s, e) => OpenChild(new LowStockForm(), "INVENTORY")));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(MakeToolButton("Reports", "", (s, e) => OpenChild(new ReportsForm("SALES"), "REPORTS")));
            toolStrip.Items.Add(MakeToolButton("Settings", "", (s, e) => OpenChild(new SettingsForm(), "SETTINGS")));

            this.Controls.Add(toolStrip);
        }

        private ToolStripButton MakeToolButton(string text, string shortcut, EventHandler click)
        {
            var b = new ToolStripButton();
            b.Text = string.IsNullOrEmpty(shortcut) ? "  " + text + "  " : "  " + text + "  (" + shortcut + ")  ";
            b.DisplayStyle = ToolStripItemDisplayStyle.Text;
            b.AutoSize = true;
            b.Padding = new Padding(8, 4, 8, 4);
            b.ForeColor = UiHelper.ThemeDark;
            b.Click += click;
            return b;
        }

        private void BuildStatus()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.BackColor = UiHelper.ThemeDark;
            statusStrip.ForeColor = Color.White;
            statusStrip.Font = UiHelper.SmallFont;
            statusStrip.SizingGrip = false;
            statusStrip.Padding = new Padding(8, 4, 12, 4);

            string shop = UiHelper.GetShopName();
            string phone = UiHelper.GetShopPhone();
            lblShop = new ToolStripStatusLabel(shop + (string.IsNullOrEmpty(phone) ? "" : "  ·  " + phone))
            {
                ForeColor = Color.White,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblUser = new ToolStripStatusLabel("  " + CurrentUser.FullName + "  ·  " + CurrentUser.Role + "  ") { ForeColor = Color.White };
            lblClock = new ToolStripStatusLabel(DateTime.Now.ToString("dd MMM yyyy  HH:mm")) { ForeColor = Color.FromArgb(220, 230, 240) };
            lblBrand = new ToolStripStatusLabel("  " + UiHelper.AppName + "  v" + UiHelper.AppVersion + "  ") { ForeColor = Color.FromArgb(200, 214, 229) };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblShop, lblUser, lblClock, lblBrand });
            this.Controls.Add(statusStrip);

            clockTimer = new Timer { Interval = 30000 };
            clockTimer.Tick += (s, e) => { if (lblClock != null) lblClock.Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"); };
            clockTimer.Start();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                if (c is MdiClient)
                {
                    mdiClient = (MdiClient)c;
                    mdiClient.BackColor = UiHelper.BgColor;
                    break;
                }
            }
            SetupHome();
        }

        /// <summary>
        /// Dashboard lives in a borderless MDI child (MdiClient only accepts Forms).
        /// No overlay Panel vs MdiClient z-order war, and no polling timer.
        /// </summary>
        private void SetupHome()
        {
            homeScreen = new HomeScreen(CurrentUser);
            homeScreen.Dock = DockStyle.Fill;

            homeHost = new Form();
            homeHost.Text = "Dashboard";
            homeHost.FormBorderStyle = FormBorderStyle.None;
            homeHost.ControlBox = false;
            homeHost.MaximizeBox = false;
            homeHost.MinimizeBox = false;
            homeHost.ShowInTaskbar = false;
            homeHost.BackColor = UiHelper.BgColor;
            homeHost.Controls.Add(homeScreen);
            homeHost.MdiParent = this;
            homeHost.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true; // dashboard stays; other windows close around it
            };
            homeHost.Show();
            homeHost.WindowState = FormWindowState.Maximized;
        }

        private int OtherChildCount()
        {
            int n = 0;
            foreach (Form f in this.MdiChildren)
                if (f != homeHost) n++;
            return n;
        }

        private void SyncHome()
        {
            if (homeHost == null || homeScreen == null || syncingHome) return;
            syncingHome = true;
            try
            {
                bool showDash = OtherChildCount() == 0;
                if (showDash)
                {
                    if (!homeHost.Visible) homeHost.Show();
                    if (homeHost.WindowState != FormWindowState.Maximized)
                        homeHost.WindowState = FormWindowState.Maximized;
                    homeHost.Activate();
                }
            }
            finally { syncingHome = false; }
        }

        public void RefreshBranding()
        {
            string shop = UiHelper.GetShopName();
            string phone = UiHelper.GetShopPhone();
            if (lblShop != null)
                lblShop.Text = shop + (string.IsNullOrEmpty(phone) ? "" : "  ·  " + phone);
            this.Text = UiHelper.AppName + "  —  " + shop;
            this.BackColor = UiHelper.BgColor;
            if (menuStrip != null)
            {
                menuStrip.BackColor = UiHelper.ThemeColor;
                menuStrip.Font = UiHelper.NormalFont;
                menuStrip.Renderer = new ThemeMenuRenderer();
            }
            if (toolStrip != null) toolStrip.Font = UiHelper.ButtonFont;
            if (statusStrip != null) { statusStrip.BackColor = UiHelper.ThemeDark; statusStrip.Font = UiHelper.SmallFont; }
            if (lblBrand != null)
                lblBrand.Text = "  " + UiHelper.AppName + "  v" + UiHelper.AppVersion + "  ";
            if (mdiClient != null) mdiClient.BackColor = UiHelper.BgColor;
            if (homeScreen != null) homeScreen.RefreshBranding();
        }

        private void ApplyPermissions()
        {
            foreach (ToolStripMenuItem top in menuStrip.Items)
            {
                string t = top.Text.Trim();
                if (t == "File" || t == "Windows" || t == "Help") continue;
                foreach (ToolStripItem item in top.DropDownItems)
                {
                    if (item is ToolStripSeparator) continue;
                    string key = GetPermKey(item.Text);
                    if (!string.IsNullOrEmpty(key) && !AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, key))
                        item.Enabled = false;
                }
            }
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (!(item is ToolStripButton btn)) continue;
                string key = GetPermKey(btn.Text);
                if (!string.IsNullOrEmpty(key) && !AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, key))
                    btn.Enabled = false;
            }
        }

        private string GetPermKey(string menuText)
        {
            string t = (menuText ?? "").Replace("\t", " ").Trim();
            if (t.StartsWith("Sale")) return "SALE";
            if (t.StartsWith("Purchase") && !t.StartsWith("Purchase Report")) return "PURCHASE";
            if (t.StartsWith("New Item")) return "NEWITEM";
            if (t.StartsWith("Stock") || t.StartsWith("Low Stock")) return "INVENTORY";
            if (t.Contains("Report") || t.StartsWith("Reports") || t.StartsWith("Profit")) return "REPORTS";
            if (t.StartsWith("Settings") || t.StartsWith("Appearance")) return "SETTINGS";
            return "";
        }

        private void MnuWindows_DropDownOpening(object sender, EventArgs e)
        {
            mnuWindows.DropDownItems.Clear();
            int listed = 0;
            foreach (Form child in this.MdiChildren)
            {
                if (child == homeHost) continue;
                listed++;
                var item = new ToolStripMenuItem(string.IsNullOrEmpty(child.Text) ? child.GetType().Name : child.Text);
                item.Tag = child;
                item.Click += (s, ev) => ((Form)((ToolStripMenuItem)s).Tag).Activate();
                if (child == this.ActiveMdiChild) item.Checked = true;
                mnuWindows.DropDownItems.Add(item);
            }
            if (listed == 0)
                mnuWindows.DropDownItems.Add("(No open windows)");
            else
            {
                mnuWindows.DropDownItems.Add(new ToolStripSeparator());
                mnuWindows.DropDownItems.Add("Cascade", null, (s, ev) => this.LayoutMdi(MdiLayout.Cascade));
                mnuWindows.DropDownItems.Add("Tile Horizontal", null, (s, ev) => this.LayoutMdi(MdiLayout.TileHorizontal));
                mnuWindows.DropDownItems.Add("Close All", null, (s, ev) =>
                {
                    foreach (Form f in this.MdiChildren)
                        if (f != homeHost) f.Close();
                    SyncHome();
                });
            }
        }

        public void OpenChild(Form child, string permKey)
        {
            OpenChild(child, permKey, false);
        }

        public void OpenChild(Form child, string permKey, bool unused)
        {
            if (!AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, permKey))
            {
                MessageBox.Show("You do not have access to this form.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                child.Dispose();
                return;
            }

            bool allowMulti = child is SaleForm || child is PurchaseForm || child is NewItemForm || child is ReportsForm;
            if (!allowMulti)
            {
                foreach (Form f in this.MdiChildren)
                {
                    if (f != homeHost && f.GetType() == child.GetType())
                    {
                        f.Activate();
                        child.Dispose();
                        return;
                    }
                }
            }

            child.MdiParent = this;
            UiHelper.ApplyFormSize(child);
            child.WindowState = FormWindowState.Normal;
            child.FormClosed += (s, e) => SyncHome();
            child.Show();
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(
                "F2    New Sale\nF3    New Purchase\nF4    Close window\nF8    Remove selected line (Sale)\nF12   Save / focus discount\nEnter  Confirm field",
                "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoLogout()
        {
            if (MessageBox.Show("Logout?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (Form f in this.MdiChildren)
                if (f != homeHost) f.Close();
            this.Hide();
            var login = new LoginForm();
            login.FormClosed += (s, e) => this.Close();
            login.Show();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!UiHelper.ConfirmExit()) e.Cancel = true;
            }
            if (e.Cancel) return;
            if (clockTimer != null) { clockTimer.Stop(); clockTimer.Dispose(); clockTimer = null; }
        }
    }

    public class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeMenuRenderer() : base(new ThemeColorTable()) { }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                using (var b = new SolidBrush(UiHelper.ThemeDark))
                    e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
            }
            else if (e.Item.Owner is MenuStrip)
            {
                using (var b = new SolidBrush(UiHelper.ThemeColor))
                    e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
            }
            else base.OnRenderMenuItemBackground(e);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Owner is MenuStrip)
                e.TextColor = Color.White;
            else if (e.Item.Selected)
                e.TextColor = Color.White;
            else
                e.TextColor = Color.FromArgb(40, 50, 60);
            base.OnRenderItemText(e);
        }
    }

    public class ThemeColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return UiHelper.ThemeDark; } }
        public override Color MenuItemSelectedGradientBegin { get { return UiHelper.ThemeDark; } }
        public override Color MenuItemSelectedGradientEnd { get { return UiHelper.ThemeDark; } }
        public override Color MenuItemBorder { get { return UiHelper.ThemeDark; } }
        public override Color MenuBorder { get { return UiHelper.ThemeColor; } }
        public override Color ToolStripDropDownBackground { get { return Color.White; } }
        public override Color ImageMarginGradientBegin { get { return Color.White; } }
        public override Color ImageMarginGradientMiddle { get { return Color.White; } }
        public override Color ImageMarginGradientEnd { get { return Color.White; } }
        public override Color MenuItemPressedGradientBegin { get { return UiHelper.ThemeColor; } }
        public override Color MenuItemPressedGradientEnd { get { return UiHelper.ThemeColor; } }
        public override Color MenuStripGradientBegin { get { return UiHelper.ThemeColor; } }
        public override Color MenuStripGradientEnd { get { return UiHelper.ThemeColor; } }
    }

    public class LightToolTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin { get { return Color.FromArgb(248, 250, 252); } }
        public override Color ToolStripGradientMiddle { get { return Color.FromArgb(248, 250, 252); } }
        public override Color ToolStripGradientEnd { get { return Color.FromArgb(248, 250, 252); } }
        public override Color ButtonSelectedHighlight { get { return UiHelper.ThemeLight; } }
        public override Color ButtonSelectedGradientBegin { get { return UiHelper.ThemeLight; } }
        public override Color ButtonSelectedGradientEnd { get { return UiHelper.ThemeLight; } }
        public override Color ButtonSelectedBorder { get { return UiHelper.ThemeColor; } }
        public override Color SeparatorDark { get { return Color.FromArgb(220, 226, 232); } }
        public override Color SeparatorLight { get { return Color.White; } }
    }
}
