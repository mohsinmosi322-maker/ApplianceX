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
            mnuTrans.DropDownItems.Add("Sale Return", null, (s, e) => OpenChild(new SaleReturnForm(), "SALE"));
            mnuTrans.DropDownItems.Add("Purchase\tF3", null, (s, e) => OpenChild(new PurchaseForm(), "PURCHASE"));

            var mnuInv = new ToolStripMenuItem("  Inventory  ");
            mnuInv.DropDownItems.Add("New Item", null, (s, e) => OpenChild(new NewItemForm(), "NEWITEM"));
            mnuInv.DropDownItems.Add("Stock Position", null, (s, e) => OpenChild(new InventoryForm(), "INVENTORY"));

            var mnuRep = new ToolStripMenuItem("  Reports  ");
            mnuRep.DropDownItems.Add("Sales Report", null, (s, e) => OpenChild(new ReportsForm("SALES"), "REPORTS"));
            mnuRep.DropDownItems.Add("Purchase Report", null, (s, e) => OpenChild(new ReportsForm("PURCHASE"), "REPORTS"));
            mnuRep.DropDownItems.Add("Stock Report", null, (s, e) => OpenChild(new ReportsForm("STOCK"), "REPORTS"));
            mnuRep.DropDownItems.Add("Profit Report", null, (s, e) => OpenChild(new ReportsForm("PROFIT"), "REPORTS"));
            mnuRep.DropDownItems.Add("Low Stock Report", null, (s, e) => OpenChild(new LowStockForm(), "REPORTS"));

            var mnuSet = new ToolStripMenuItem("  Settings  ");
            mnuSet.DropDownItems.Add("Appearance & Limits", null, (s, e) => OpenSettingsProtected());

            mnuWindows = new ToolStripMenuItem("  Windows  ");
            mnuWindows.DropDownOpening += MnuWindows_DropDownOpening;

            var mnuHelp = new ToolStripMenuItem("  Help  ");
            mnuHelp.DropDownItems.Add("Shortcuts", null, (s, e) => ShowShortcuts());
            mnuHelp.DropDownItems.Add("About", null, (s, e) =>
                MessageBox.Show(UiHelper.AppName + "  v" + UiHelper.AppVersion + "\n" + UiHelper.GetShopName() +
                    "\n\nF2 Sale   F3 Purchase   F4 Close   F9 History   F12 Save",
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

        private void OpenSettingsProtected()
        {
            if (!PromptSettingsPassword()) return;
            OpenChild(new SettingsForm(), "SETTINGS");
        }

        public bool PromptSettingsPassword()
        {
            using (var f = new Form())
            {
                f.Text = "Settings Password";
                f.Size = new Size(360, 160);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                var lbl = new Label { Text = "Enter password to open Settings:", Location = new Point(16, 16), AutoSize = true };
                var txt = new TextBox { Location = new Point(16, 44), Size = new Size(310, 26), PasswordChar = '*' };
                var ok = new Button { Text = "Unlock", Location = new Point(16, 84), Size = new Size(100, 28), DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Location = new Point(130, 84), Size = new Size(100, 28), DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(this) != DialogResult.OK) return false;

                string pwd = txt.Text ?? "";
                string settingsPwd = AppSettings.Get("SettingsPassword");
                if (!string.IsNullOrEmpty(settingsPwd))
                {
                    if (pwd == settingsPwd) return true;
                    MessageBox.Show("Incorrect settings password.");
                    return false;
                }
                var repo = new Data.UserRepository();
                var u = repo.Authenticate(CurrentUser.UserName, pwd);
                if (u == null)
                {
                    MessageBox.Show("Incorrect password.");
                    return false;
                }
                return true;
            }
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
        /// Dashboard = borderless maximized MDI child (true wallpaper).
        /// Other forms open on top; when they close, home stays maximized.
        /// </summary>
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
            homeHost.WindowState = FormWindowState.Maximized;
            // Prevent user closing the wallpaper host
            homeHost.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };
            homeHost.Show();
        }

        private int CountOtherChildren()
        {
            int n = 0;
            foreach (Form f in this.MdiChildren)
                if (f != homeHost) n++;
            return n;
        }

        private void SyncHome()
        {
            if (homeHost == null || homeScreen == null) return;
            if (CountOtherChildren() == 0)
            {
                if (homeHost.WindowState != FormWindowState.Maximized)
                    homeHost.WindowState = FormWindowState.Maximized;
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
            if (t.StartsWith("Purchase") && !t.StartsWith("Purchase Report")) return "PURCHASE";
            if (t.StartsWith("New Item")) return "NEWITEM";
            if (t.StartsWith("Stock")) return "INVENTORY";
            if (t.StartsWith("Low Stock")) return "REPORTS";
            if (t.Contains("Report") || t.StartsWith("Reports") || t.StartsWith("Profit")) return "REPORTS";
            if (t.StartsWith("Settings") || t.StartsWith("Appearance")) return "SETTINGS";
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
            mnuWindows.DropDownItems.Add("Cascade", null, (s, ev) => this.LayoutMdi(MdiLayout.Cascade));
            mnuWindows.DropDownItems.Add("Tile Horizontal", null, (s, ev) => this.LayoutMdi(MdiLayout.TileHorizontal));
            mnuWindows.DropDownItems.Add("Close All", null, (s, ev) =>
            {
                foreach (Form f in this.MdiChildren)
                    if (f != homeHost) f.Close();
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

            child.MdiParent = this;
            UiHelper.ApplyFormSize(child);
            child.WindowState = FormWindowState.Normal;
            child.FormClosed += (s, e) => SyncHome();
            child.Show();
            child.Activate();
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(
                "F2    New Sale\nF3    Purchase\nF4    Close window\nF8    Remove line (Sale)\nF9    Product history\nUp/Down  Move in grid\nF12   Discount / Save",
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
