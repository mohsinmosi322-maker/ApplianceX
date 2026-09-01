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
        private ToolStripMenuItem mnuWindows;
        private HomeScreen homeScreen;
        private Form homeHost;
        private MdiClient mdiClient;
        private int cascadeIndex = 0;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblShop, lblClock;
        private Timer clockTimer;

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
                cp.ExStyle |= 0x02000000;
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
            this.Resize += (s, e) => { if (CountOtherChildren() == 0) ForceHomeFill(); };
            this.Text = UiHelper.AppName + "  \u2014  " + UiHelper.GetShopName();
            BuildMenu();
            BuildStatusBar();
            this.Load += MainForm_Load;
        }

        private void BuildStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.BackColor = UiHelper.ThemeDark;
            statusStrip.ForeColor = Color.White;
            statusStrip.SizingGrip = false;
            lblUser = new ToolStripStatusLabel();
            lblUser.ForeColor = Color.White;
            lblUser.Text = "  User: " + (CurrentUser != null ? CurrentUser.UserName : "") +
                "  (" + (CurrentUser != null ? CurrentUser.Role : "") + ")  ";
            lblShop = new ToolStripStatusLabel();
            lblShop.ForeColor = Color.White;
            lblShop.Text = "  " + UiHelper.GetShopName() + "  ";
            lblShop.Spring = true;
            lblClock = new ToolStripStatusLabel();
            lblClock.ForeColor = Color.White;
            lblClock.Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm:ss");
            statusStrip.Items.AddRange(new ToolStripItem[] { lblUser, lblShop, lblClock });
            this.Controls.Add(statusStrip);
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                if (lblClock != null)
                    lblClock.Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm:ss");
            };
            clockTimer.Start();
        }

        private void BuildMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            menuStrip.BackColor = UiHelper.ThemeColor;
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = UiHelper.NormalFont;
            menuStrip.Padding = new Padding(8, 4, 8, 4);
            menuStrip.Renderer = new ThemeMenuRenderer();

            var mnuFile = new ToolStripMenuItem("  File  ");
            mnuFile.DropDownItems.Add("Logout", null, (s, e) => DoLogout());
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            mnuFile.DropDownItems.Add("Exit", null, (s, e) => this.Close());

            // Sale / Purchase / Returns — same access pattern
            var mnuTrans = new ToolStripMenuItem("  Transactions  ");
            mnuTrans.DropDownItems.Add("Sale\tF2", null, (s, e) => OpenChild(new SaleForm(), "SALE"));
            mnuTrans.DropDownItems.Add("Sale Return", null, (s, e) => OpenChild(new SaleReturnForm(), "SALE"));
            mnuTrans.DropDownItems.Add(new ToolStripSeparator());
            mnuTrans.DropDownItems.Add("Purchase\tF3", null, (s, e) => OpenChild(new PurchaseForm(), "PURCHASE"));
            mnuTrans.DropDownItems.Add("Purchase Return", null, (s, e) => OpenChild(new PurchaseReturnForm(), "PURCHASE"));

            // Inventory: only stock position (New Item + Stock Ops removed)
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
                    MessageBox.Show("Only Admin can manage users.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!PromptSettingsPassword()) return;
                OpenChild(new UserMasterForm(), "SETTINGS");
            });

            mnuWindows = new ToolStripMenuItem("  Windows  ");
            mnuWindows.DropDownOpening += MnuWindows_DropDownOpening;

            var mnuHelp = new ToolStripMenuItem("  Help  ");
            mnuHelp.DropDownItems.Add("Shortcuts", null, (s, e) => ShowShortcuts());
            mnuHelp.DropDownItems.Add("Application Logs", null, (s, e) => OpenChild(new LogViewerForm(), ""));
            mnuHelp.DropDownItems.Add("About", null, (s, e) =>
                MessageBox.Show(UiHelper.AppName + "  v" + UiHelper.AppVersion + "\n" + UiHelper.GetShopName() +
                    "\n\nF2 Sale   F3 Purchase   F4 Close   F9 History   F12 Save",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuTrans, mnuInv, mnuMasters, mnuAcct, mnuRep, mnuSet, mnuWindows, mnuHelp });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) { OpenChild(new SaleForm(), "SALE"); e.Handled = true; }
                if (e.KeyCode == Keys.F3) { OpenChild(new PurchaseForm(), "PURCHASE"); e.Handled = true; }
            };
        }

        private void OpenSettingsProtected()
        {
            if (!PromptSettingsPassword()) return;
            OpenChild(new SettingsForm(), "SETTINGS");
        }

        public bool PromptSettingsPassword()
        {
            string pwd = DialogHelpers.PromptPassword(this, "Enter password to open Settings:", "Settings Password");
            if (pwd == null) return false;

            string settingsPwd = AppSettings.Get("SettingsPassword");
            if (!string.IsNullOrEmpty(settingsPwd))
            {
                if (pwd == settingsPwd) return true;
                DialogHelpers.Error(this, "Incorrect settings password.");
                return false;
            }
            var repo = new Data.UserRepository();
            var u = repo.Authenticate(CurrentUser.UserName, pwd);
            if (u == null)
            {
                DialogHelpers.Error(this, "Incorrect password.");
                return false;
            }
            return true;
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

        private void SetupHome()
        {
            homeScreen = new HomeScreen(CurrentUser);
            homeScreen.Dock = DockStyle.Fill;

            homeHost = new Form();
            homeHost.Text = "Dashboard";
            homeHost.FormBorderStyle = FormBorderStyle.None;
            homeHost.ControlBox = false;
            homeHost.ShowIcon = false;
            homeHost.ShowInTaskbar = false;
            homeHost.BackColor = UiHelper.BgColor;
            homeHost.Controls.Add(homeScreen);
            homeHost.MdiParent = this;
            homeHost.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };
            homeHost.Show();
            ForceHomeFill();
        }

        private int CountOtherChildren()
        {
            int n = 0;
            foreach (Form f in this.MdiChildren)
                if (f != homeHost) n++;
            return n;
        }

        private void ForceHomeFill()
        {
            if (homeHost == null || mdiClient == null) return;
            try
            {
                homeHost.WindowState = FormWindowState.Normal;
                homeHost.Bounds = new Rectangle(0, 0, mdiClient.ClientSize.Width, mdiClient.ClientSize.Height);
                homeHost.WindowState = FormWindowState.Maximized;
                homeHost.SendToBack();
            }
            catch { }
        }

        private void SyncHome()
        {
            if (homeHost == null || homeScreen == null) return;
            if (CountOtherChildren() == 0)
            {
                cascadeIndex = 0;
                ForceHomeFill();
                homeHost.Activate();
                homeScreen.RefreshData();
            }
        }

        public void RefreshBranding()
        {
            this.Text = UiHelper.AppName + "  \u2014  " + UiHelper.GetShopName();
            this.BackColor = UiHelper.BgColor;
            if (menuStrip != null)
            {
                menuStrip.BackColor = UiHelper.ThemeColor;
                menuStrip.Font = UiHelper.NormalFont;
                menuStrip.Renderer = new ThemeMenuRenderer();
            }
            if (mdiClient != null) mdiClient.BackColor = UiHelper.BgColor;
            if (homeScreen != null) homeScreen.RefreshBranding();
            if (homeHost != null) homeHost.BackColor = UiHelper.BgColor;
            if (statusStrip != null) statusStrip.BackColor = UiHelper.ThemeDark;
            if (lblShop != null) lblShop.Text = "  " + UiHelper.GetShopName() + "  ";
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
        }

        private string GetPermKey(string menuText)
        {
            string t = (menuText ?? "").Replace("\t", " ").Trim();
            if (t.StartsWith("Sale Return")) return "SALE";
            if (t.StartsWith("Sale")) return "SALE";
            if (t.StartsWith("Customer")) return "SALE";
            if (t.StartsWith("Products") || t.StartsWith("Categories")) return "NEWITEM";
            if (t.StartsWith("Purchase") && !t.StartsWith("Purchase Report")) return "PURCHASE";
            if (t.StartsWith("Supplier")) return "PURCHASE";
            if (t.StartsWith("New Item") || t.StartsWith("New /")) return "NEWITEM";
            if (t.StartsWith("Stock")) return "INVENTORY";
            if (t.StartsWith("Low Stock")) return "REPORTS";
            if (t.Contains("Report") || t.StartsWith("Reports") || t.StartsWith("Profit")) return "REPORTS";
            if (t.StartsWith("Settings") || t.StartsWith("Appearance") || t.StartsWith("Users")) return "SETTINGS";
            return "";
        }

        private void MnuWindows_DropDownOpening(object sender, EventArgs e)
        {
            mnuWindows.DropDownItems.Clear();
            bool any = false;
            foreach (Form child in this.MdiChildren)
            {
                if (child == homeHost) continue;
                any = true;
                var item = new ToolStripMenuItem(string.IsNullOrEmpty(child.Text) ? child.GetType().Name : child.Text);
                item.Tag = child;
                item.Click += (s, ev) => ((Form)((ToolStripMenuItem)s).Tag).Activate();
                if (child == this.ActiveMdiChild) item.Checked = true;
                mnuWindows.DropDownItems.Add(item);
            }
            if (!any) mnuWindows.DropDownItems.Add("(No open windows)");
            mnuWindows.DropDownItems.Add(new ToolStripSeparator());
            mnuWindows.DropDownItems.Add("Cascade", null, (s, ev) => CascadeOthers());
            mnuWindows.DropDownItems.Add("Tile Horizontal", null, (s, ev) => this.LayoutMdi(MdiLayout.TileHorizontal));
            mnuWindows.DropDownItems.Add("Close All", null, (s, ev) =>
            {
                foreach (Form f in this.MdiChildren)
                    if (f != homeHost) f.Close();
                SyncHome();
            });
        }

        private void CascadeOthers()
        {
            int i = 0;
            foreach (Form f in this.MdiChildren)
            {
                if (f == homeHost) continue;
                f.WindowState = FormWindowState.Normal;
                f.Location = new Point(20 + i * 28, 20 + i * 28);
                i++;
            }
            ForceHomeFill();
        }

        public void OpenChild(Form child, string permKey)
        {
            if (!string.IsNullOrEmpty(permKey) &&
                !AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, permKey))
            {
                MessageBox.Show("You do not have access to this form.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                child.Dispose();
                return;
            }

            bool allowMulti = child is SaleForm || child is ReportsForm || child is SaleReturnForm;
            if (!allowMulti)
            {
                foreach (Form f in this.MdiChildren)
                {
                    if (f == homeHost) continue;
                    if (f.GetType() == child.GetType())
                    {
                        f.Activate();
                        f.BringToFront();
                        child.Dispose();
                        return;
                    }
                }
            }

            if (homeHost != null)
            {
                ForceHomeFill();
                homeHost.SendToBack();
            }

            child.MdiParent = this;
            UiHelper.ApplyFormSize(child);
            child.WindowState = FormWindowState.Normal;
            int off = cascadeIndex % 10;
            cascadeIndex++;
            child.StartPosition = FormStartPosition.Manual;
            child.Location = new Point(24 + off * 28, 24 + off * 28);
            child.FormClosed += (s, e) => SyncHome();
            child.Show();
            child.Activate();
            child.BringToFront();
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(
                "F2    New Sale\nF3    Purchase\nF4    Close window\nF5    Refresh / Load invoice\nF8    Remove line\nF9    Product history\nF12   Discount / Save",
                "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoLogout()
        {
            if (!DialogHelpers.Confirm(this, "Logout?")) return;
            foreach (Form f in this.MdiChildren)
                if (f != homeHost) f.Close();
            AppSession.SignOut();
            this.Hide();
            var login = new LoginForm();
            login.FormClosed += (s, e) => this.Close();
            login.Show();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !UiHelper.ConfirmExit())
                e.Cancel = true;
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
            if (e.Item.Owner is MenuStrip) e.TextColor = Color.White;
            else if (e.Item.Selected) e.TextColor = Color.White;
            else e.TextColor = Color.FromArgb(40, 50, 60);
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
}
