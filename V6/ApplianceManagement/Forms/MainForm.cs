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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblStore, lblClock;
        private HomeScreen homeScreen;
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
            this.BackColor = Color.FromArgb(236, 240, 241);
            this.KeyPreview = true;

            menuStrip = new MenuStrip();
            menuStrip.BackColor = UiHelper.ThemeColor;
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable());

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
                {
                    mdi.BackColor = Color.FromArgb(236, 240, 241);
                    homeScreen = new HomeScreen(CurrentUser);
                    homeScreen.Dock = DockStyle.Fill;
                    mdi.Controls.Add(homeScreen);
                    homeScreen.SendToBack();

                    syncHomeTimer = new Timer { Interval = 400 };
                    syncHomeTimer.Tick += (s, e) => SyncHome();
                    syncHomeTimer.Start();
                    return;
                }
            }
        }

        private void SyncHome()
        {
            if (homeScreen == null || homeScreen.IsDisposed) return;
            try
            {
                bool anyOther = false;
                foreach (Form f in MdiChildren)
                {
                    if (f.Visible) { anyOther = true; break; }
                }
                homeScreen.Visible = !anyOther;
                if (!anyOther) homeScreen.SendToBack();
            }
            catch { }
        }

        private void MnuWindows_DropDownOpening(object sender, EventArgs e)
        {
            mnuWindows.DropDownItems.Clear();
            bool any = false;
            foreach (Form f in MdiChildren)
            {
                any = true;
                var item = new ToolStripMenuItem(f.Text);
                Form target = f;
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
            foreach (Form f in MdiChildren)
                f.Close();
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
                if (login.ShowDialog() == DialogResult.OK)
                {
                    // LoginForm opens new MainForm
                }
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
            if (homeScreen != null) homeScreen.Visible = false;
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
        }
    }
}
