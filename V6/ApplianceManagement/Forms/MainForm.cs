using System;
using System.Linq;
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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblStore, lblClock;
        private HomeScreen homeScreen;
        private Form homeHost;
        private Timer syncHomeTimer;
        private ToolStripMenuItem mnuWindows;

        public MainForm(User user)
        {
            CurrentUser = user;
            Instance = this;
            UiHelper.InitializeTheme();
            InitializeComponent();
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeComponent()
        {
            this.IsMdiContainer = true;
            this.Text = UiHelper.AppName + " — " + UiHelper.GetShopName();
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;

            menuStrip = new MenuStrip();
            menuStrip.BackColor = UiHelper.ThemeColor;
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            menuStrip.Renderer = new ThemeMenuRenderer();

            var mnuFile = new ToolStripMenuItem("  File  ");
            mnuFile.DropDownItems.Add("Logout", null, (s, e) => DoLogout());
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            mnuFile.DropDownItems.Add("Exit", null, (s, e) => this.Close());

            var mnuTrans = new ToolStripMenuItem("  Transactions  ");
            mnuTrans.DropDownItems.Add("Sale\tF2", null, (s, e) => OpenChild(new SaleForm(), "SALE"));
            mnuTrans.DropDownItems.Add("Sale Return", null, (s, e) => OpenChild(new SaleReturnForm(), "SALE"));
            mnuTrans.DropDownItems.Add(new ToolStripSeparator());
            mnuTrans.DropDownItems.Add("Purchase\tF3", null, (s, e) => OpenChild(new PurchaseForm(), "PURCHASE"));
            mnuTrans.DropDownItems.Add("Purchase Return", null, (s, e) => OpenChild(new PurchaseReturnForm(), "PURCHASE"));
            mnuTrans.DropDownItems.Add(new ToolStripSeparator());
            mnuTrans.DropDownItems.Add("Invoice View", null, (s, e) => OpenChild(new InvoiceViewForm(), "SALE"));

            var mnuInv = new ToolStripMenuItem("  Inventory  ");
            mnuInv.DropDownItems.Add("Stock Position", null, (s, e) => OpenChild(new InventoryForm(), "INVENTORY"));

            var mnuMasters = new ToolStripMenuItem("  Masters  ");
            mnuMasters.DropDownItems.Add("Products (list / edit)", null, (s, e) => OpenChild(new ProductManageForm(), "NEWITEM"));
            mnuMasters.DropDownItems.Add("New / Edit Item", null, (s, e) => OpenChild(new NewItemForm(), "NEWITEM"));
            mnuMasters.DropDownItems.Add("Customers", null, (s, e) => OpenChild(new CustomerMasterForm(), "SALE"));
            mnuMasters.DropDownItems.Add("Suppliers", null, (s, e) => OpenChild(new SupplierMasterForm(), "PURCHASE"));
            mnuMasters.DropDownItems.Add("Categories", null, (s, e) => OpenChild(new CategoryMasterForm(), "NEWITEM"));

            var mnuAcct = new ToolStripMenuItem("  Accounts  ");
            mnuAcct.DropDownItems.Add("Customer Payment", null, (s, e) => OpenChild(new CustomerPaymentForm(), "SALE"));
            mnuAcct.DropDownItems.Add("Supplier Payment", null, (s, e) => OpenChild(new SupplierPaymentForm(), "PURCHASE"));
            mnuAcct.DropDownItems.Add("Customer Ledger", null, (s, e) => OpenChild(new CustomerLedgerForm(), "REPORTS"));
            mnuAcct.DropDownItems.Add("Supplier Ledger", null, (s, e) => OpenChild(new SupplierLedgerForm(), "REPORTS"));

            var mnuRep = new ToolStripMenuItem("  Reports  ");
            mnuRep.DropDownItems.Add("Sales Report", null, (s, e) => OpenChild(new ReportsForm("SALES"), "REPORTS"));
            mnuRep.DropDownItems.Add("Purchase Report", null, (s, e) => OpenChild(new ReportsForm("PURCHASE"), "REPORTS"));
            mnuRep.DropDownItems.Add("Stock Report", null, (s, e) => OpenChild(new ReportsForm("STOCK"), "REPORTS"));
            mnuRep.DropDownItems.Add("Profit Report", null, (s, e) => OpenChild(new ReportsForm("PROFIT"), "REPORTS"));
            mnuRep.DropDownItems.Add("Low Stock Report", null, (s, e) => OpenChild(new LowStockForm(), "REPORTS"));

            var mnuSet = new ToolStripMenuItem("  Settings  ");
            mnuSet.DropDownItems.Add("Appearance & Limits", null, (s, e) => OpenSettingsProtected());
            mnuSet.DropDownItems.Add("Users (CRUD)", null, (s, e) =>
            {
                if (CurrentUser == null || !string.Equals(CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "Admin only.", "Access", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                OpenChild(new UserMasterForm(), "SETTINGS");
            });

            mnuWindows = new ToolStripMenuItem("  Windows  ");
            mnuWindows.DropDownOpening += MnuWindows_DropDownOpening;

            var mnuHelp = new ToolStripMenuItem("  Help  ");
            mnuHelp.DropDownItems.Add("Shortcuts", null, (s, e) => ShowShortcuts());
            mnuHelp.DropDownItems.Add("Application Logs", null, (s, e) => OpenChild(new LogViewerForm(), ""));
            mnuHelp.DropDownItems.Add("About", null, (s, e) =>
            {
                MessageBox.Show(this, UiHelper.AppName + "\nVersion " + UiHelper.AppVersion + "\n" + UiHelper.GetShopName(),
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuTrans, mnuInv, mnuMasters, mnuAcct, mnuRep, mnuSet, mnuWindows, mnuHelp });
            foreach (ToolStripItem ti in menuStrip.Items)
            {
                ti.ForeColor = Color.White;
                var mi = ti as ToolStripMenuItem;
                if (mi != null)
                {
                    foreach (ToolStripItem sub in mi.DropDownItems)
                    {
                        if (sub is ToolStripSeparator) continue;
                        sub.ForeColor = Color.FromArgb(0x1F, 0x29, 0x37);
                        sub.BackColor = Color.White;
                    }
                }
            }
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) { OpenChild(new SaleForm(), "SALE"); e.Handled = true; }
                if (e.KeyCode == Keys.F3) { OpenChild(new PurchaseForm(), "PURCHASE"); e.Handled = true; }
            };

            statusStrip = new StatusStrip { BackColor = UiHelper.ThemeDark, ForeColor = Color.White };
            lblUser = new ToolStripStatusLabel { Text = "User: —", ForeColor = Color.White };
            lblStore = new ToolStripStatusLabel { Text = UiHelper.GetShopName(), ForeColor = Color.White, Spring = true, TextAlign = ContentAlignment.MiddleCenter };
            lblClock = new ToolStripStatusLabel { Text = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"), ForeColor = Color.White };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblUser, lblStore, lblClock });
            Controls.Add(statusStrip);

            var clock = new Timer { Interval = 1000 };
            clock.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");
            clock.Start();
        }

        public void RefreshBranding()
        {
            try
            {
                this.Text = UiHelper.AppName + " — " + UiHelper.GetShopName();
                if (menuStrip != null)
                {
                    menuStrip.BackColor = UiHelper.ThemeColor;
                    menuStrip.ForeColor = Color.White;
                    menuStrip.Renderer = new ThemeMenuRenderer();
                    foreach (ToolStripItem ti in menuStrip.Items)
                        ti.ForeColor = Color.White;
                }
                if (statusStrip != null)
                    statusStrip.BackColor = UiHelper.ThemeDark;
                if (lblStore != null)
                    lblStore.Text = UiHelper.GetShopName();
                if (lblUser != null && CurrentUser != null)
                    lblUser.Text = "User: " + CurrentUser.UserName + " (" + CurrentUser.Role + ")";
                if (homeScreen != null && !homeScreen.IsDisposed)
                    homeScreen.RefreshBranding();
            }
            catch { }
        }

        private void OpenSettingsProtected()
        {
            if (CurrentUser == null || !string.Equals(CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Admin only.", "Access", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string pwd = DialogHelpers.PromptPassword(this, "Enter admin password to unlock Settings:", "Settings");
            if (pwd == null) return;
            OpenChild(new SettingsForm(), "SETTINGS");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblUser.Text = "User: " + (CurrentUser != null ? CurrentUser.UserName + " (" + CurrentUser.Role + ")" : "—");
            lblStore.Text = UiHelper.GetShopName();
            SetupHome();
            try { SchemaBootstrap.EnsureSaleReturnTables(); } catch { }
            try { SchemaBootstrap.EnsurePurchaseReturnTables(); } catch { }
        }

        private void SetupHome()
        {
            foreach (Control c in this.Controls)
            {
                if (c is MdiClient mdi)
                    mdi.BackColor = UiHelper.BgColor;
            }

            homeHost = new Form
            {
                Text = "Dashboard",
                FormBorderStyle = FormBorderStyle.None,
                ControlBox = false,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                WindowState = FormWindowState.Maximized,
                BackColor = UiHelper.BgColor
            };
            homeScreen = new HomeScreen(CurrentUser) { Dock = DockStyle.Fill };
            homeHost.Controls.Add(homeScreen);
            homeHost.MdiParent = this;
            homeHost.Show();
            homeHost.SendToBack();

            syncHomeTimer = new Timer { Interval = 400 };
            syncHomeTimer.Tick += (s, e) => SyncHome();
            syncHomeTimer.Start();
        }

        private void SyncHome()
        {
            if (homeHost == null || homeHost.IsDisposed) return;
            try
            {
                bool anyOther = false;
                foreach (Form f in MdiChildren)
                {
                    if (f == homeHost) continue;
                    if (f.Visible) { anyOther = true; break; }
                }
                if (!anyOther)
                {
                    if (!homeHost.Visible) homeHost.Show();
                    homeHost.WindowState = FormWindowState.Maximized;
                    homeHost.SendToBack();
                }
            }
            catch { }
        }

        private void MnuWindows_DropDownOpening(object sender, EventArgs e)
        {
            mnuWindows.DropDownItems.Clear();
            bool any = false;
            foreach (Form f in MdiChildren)
            {
                if (f == homeHost) continue;
                any = true;
                var item = new ToolStripMenuItem(f.Text);
                Form target = f;
                item.ForeColor = Color.FromArgb(0x1F, 0x29, 0x37);
                item.Click += (s, ev) => { target.Activate(); };
                mnuWindows.DropDownItems.Add(item);
            }
            if (!any) mnuWindows.DropDownItems.Add("(No open windows)");
            mnuWindows.DropDownItems.Add(new ToolStripSeparator());
            mnuWindows.DropDownItems.Add("Cascade", null, (s, ev) => this.LayoutMdi(MdiLayout.Cascade));
            mnuWindows.DropDownItems.Add("Tile Horizontal", null, (s, ev) => this.LayoutMdi(MdiLayout.TileHorizontal));
            mnuWindows.DropDownItems.Add("Close All", null, (s, ev) => CloseAllChildren());
        }

        private void CloseAllChildren()
        {
            foreach (Form f in MdiChildren.Cast<Form>().ToArray())
            {
                if (f == homeHost) continue;
                f.Close();
            }
            SyncHome();
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(this,
                "F2  Sale\nF3  Purchase\nF4  Close form\nF5  Load / Refresh\nF9  Product history\nF12 Save\nEnter  Next field",
                "Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoLogout()
        {
            if (!DialogHelpers.Confirm(this, "Logout and return to login?")) return;
            Instance = null;
            this.Hide();
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() == DialogResult.OK) { }
                else Application.Exit();
            }
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!UiHelper.ConfirmExit()) e.Cancel = true;
                else Instance = null;
            }
        }

        public void OpenChild(Form child, string permKey)
        {
            if (!string.IsNullOrEmpty(permKey) && CurrentUser != null &&
                !AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, permKey))
            {
                MessageBox.Show(this, "You do not have permission for this screen.", "Access denied",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (child is PurchaseForm)
            {
                foreach (Form f in MdiChildren)
                {
                    if (f is PurchaseForm)
                    {
                        f.Activate();
                        child.Dispose();
                        return;
                    }
                }
            }

            child.MdiParent = this;
            UiHelper.ApplyFormSize(child);
            child.Show();
            child.Activate();
            if (homeHost != null && !homeHost.IsDisposed && child != homeHost)
                homeHost.SendToBack();
        }

        private class ThemeMenuRenderer : ToolStripProfessionalRenderer
        {
            public ThemeMenuRenderer() : base(new MenuColorTable()) { }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                bool onMenuStrip = e.ToolStrip is MenuStrip;
                bool selected = e.Item.Selected || e.Item.Pressed;
                if (onMenuStrip)
                    e.TextColor = Color.White;
                else if (selected)
                    e.TextColor = Color.White;
                else
                    e.TextColor = Color.FromArgb(0x1F, 0x29, 0x37);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                bool onMenuStrip = e.ToolStrip is MenuStrip;
                bool selected = e.Item.Selected || e.Item.Pressed;
                if (onMenuStrip)
                {
                    Color bg = selected ? UiHelper.ThemeDark : UiHelper.ThemeColor;
                    using (var b = new SolidBrush(bg))
                        e.Graphics.FillRectangle(b, rc);
                    return;
                }
                if (selected)
                {
                    using (var b = new SolidBrush(UiHelper.ThemeDark))
                        e.Graphics.FillRectangle(b, rc);
                    return;
                }
                using (var b = new SolidBrush(Color.White))
                    e.Graphics.FillRectangle(b, rc);
            }
        }

        private class MenuColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin { get { return UiHelper.ThemeColor; } }
            public override Color MenuStripGradientEnd { get { return UiHelper.ThemeColor; } }
            public override Color MenuItemSelected { get { return UiHelper.ThemeDark; } }
            public override Color MenuItemSelectedGradientBegin { get { return UiHelper.ThemeDark; } }
            public override Color MenuItemSelectedGradientEnd { get { return UiHelper.ThemeDark; } }
            public override Color MenuItemBorder { get { return UiHelper.ThemeDark; } }
            public override Color MenuBorder { get { return UiHelper.ThemeDark; } }
            public override Color MenuItemPressedGradientBegin { get { return UiHelper.ThemeDark; } }
            public override Color MenuItemPressedGradientEnd { get { return UiHelper.ThemeDark; } }
            public override Color ToolStripDropDownBackground { get { return Color.White; } }
            public override Color ImageMarginGradientBegin { get { return Color.White; } }
            public override Color ImageMarginGradientMiddle { get { return Color.White; } }
            public override Color ImageMarginGradientEnd { get { return Color.White; } }
            public override Color SeparatorDark { get { return Color.FromArgb(0xD1, 0xD5, 0xDB); } }
            public override Color SeparatorLight { get { return Color.White; } }
        }
    }
}
