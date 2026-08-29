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
        private MdiClient mdiClient;

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
            this.Text = UiHelper.AppName + "  \u2014  " + UiHelper.GetShopName();
            BuildMenu();
            this.Load += MainForm_Load;
            this.Resize += (s, e) => PositionHomeOverMdi();
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
            if (mdiClient == null) return;
            homeScreen = new HomeScreen(CurrentUser);
            homeScreen.Visible = true;
            this.Controls.Add(homeScreen);
            PositionHomeOverMdi();
            homeScreen.BringToFront();
            menuStrip.BringToFront();
        }

        private void PositionHomeOverMdi()
        {
            if (homeScreen == null || mdiClient == null) return;
            homeScreen.Bounds = mdiClient.Bounds;
            homeScreen.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        private void SyncHome()
        {
            if (homeScreen == null) return;
            bool showDash = this.MdiChildren.Length == 0;
            homeScreen.Visible = showDash;
            if (showDash)
            {
                PositionHomeOverMdi();
                homeScreen.BringToFront();
                menuStrip.BringToFront();
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
            PositionHomeOverMdi();
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
            if (this.MdiChildren.Length == 0)
            {
                mnuWindows.DropDownItems.Add("(No open windows)");
                return;
            }
            foreach (Form child in this.MdiChildren)
            {
                var item = new ToolStripMenuItem(string.IsNullOrEmpty(child.Text) ? child.GetType().Name : child.Text);
                item.Tag = child;
                item.Click += (s, ev) => ((Form)((ToolStripMenuItem)s).Tag).Activate();
                if (child == this.ActiveMdiChild) item.Checked = true;
                mnuWindows.DropDownItems.Add(item);
            }
            mnuWindows.DropDownItems.Add(new ToolStripSeparator());
            mnuWindows.DropDownItems.Add("Cascade", null, (s, ev) => this.LayoutMdi(MdiLayout.Cascade));
            mnuWindows.DropDownItems.Add("Tile Horizontal", null, (s, ev) => this.LayoutMdi(MdiLayout.TileHorizontal));
            mnuWindows.DropDownItems.Add("Close All", null, (s, ev) =>
            {
                foreach (Form f in this.MdiChildren) f.Close();
                SyncHome();
            });
        }

        public void OpenChild(Form child, string permKey)
        {
            if (!AppSettings.HasPermission(CurrentUser.UserName, CurrentUser.Role, permKey))
            {
                MessageBox.Show("You do not have access to this form.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                child.Dispose();
                return;
            }

            // Sale + Reports: multiple windows allowed. Purchase + rest: single instance, switch to existing.
            bool allowMulti = child is SaleForm || child is ReportsForm;
            if (!allowMulti)
            {
                foreach (Form f in this.MdiChildren)
                {
                    if (f.GetType() == child.GetType())
                    {
                        f.Activate();
                        f.BringToFront();
                        child.Dispose();
                        return;
                    }
                }
            }

            if (homeScreen != null) homeScreen.Visible = false;
            child.MdiParent = this;
            UiHelper.ApplyFormSize(child);
            child.WindowState = FormWindowState.Normal;
            child.FormClosed += (s, e) => SyncHome();
            child.Show();
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(
                "F2    New Sale (multiple allowed)\nF3    Purchase (one window only)\nF4    Close window\nF8    Remove selected line (Sale)\nF12   Save / focus discount\nEnter  Confirm field",
                "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoLogout()
        {
            if (MessageBox.Show("Logout?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (Form f in this.MdiChildren) f.Close();
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
