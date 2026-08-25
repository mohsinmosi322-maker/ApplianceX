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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblShop, lblBrand;

        private HomeScreen homeScreen;
        private Timer welcomeTimer;

        public MainForm(User user)
        {
            CurrentUser = user;
            Instance = this;
            UiHelper.InitializeTheme();
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            UiHelper.FadeIn(this);
            ApplyPermissions();
        }

        private void InitializeComponent()
        {
            this.IsMdiContainer = true;
            this.BackColor = UiHelper.BgColor;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;

            string shop = UiHelper.GetShopName();
            this.Text = UiHelper.AppName + " — " + shop;

            // ===== MENU STRIP ONLY (no toolbar) =====
            menuStrip = new MenuStrip();
            menuStrip.BackColor = UiHelper.ThemeColor;
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = UiHelper.NormalFont;
            menuStrip.Renderer = new ThemeMenuRenderer();

            var mnuFile = new ToolStripMenuItem("File");
            mnuFile.DropDownItems.Add("Logout", null, (s, e) => DoLogout());
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            mnuFile.DropDownItems.Add("Exit", null, (s, e) => this.Close());

            var mnuTrans = new ToolStripMenuItem("Transactions");
            mnuTrans.DropDownItems.Add("Sale", null, (s, e) => OpenChild(new SaleForm(), "SALE"));
            mnuTrans.DropDownItems.Add("Purchase", null, (s, e) => OpenChild(new PurchaseForm(), "PURCHASE"));

            var mnuInv = new ToolStripMenuItem("Inventory");
            mnuInv.DropDownItems.Add("New Item", null, (s, e) => OpenChild(new NewItemForm(), "NEWITEM"));
            mnuInv.DropDownItems.Add("Stock Position", null, (s, e) => OpenChild(new InventoryForm(), "INVENTORY"));
            mnuInv.DropDownItems.Add("Low Stock Report", null, (s, e) => OpenChild(new LowStockForm(), "INVENTORY"));

            var mnuRep = new ToolStripMenuItem("Reports");
            mnuRep.DropDownItems.Add("Sales Report", null, (s, e) => OpenChild(new ReportsForm("SALES"), "REPORTS"));
            mnuRep.DropDownItems.Add("Purchase Report", null, (s, e) => OpenChild(new ReportsForm("PURCHASE"), "REPORTS"));
            mnuRep.DropDownItems.Add("Stock Report", null, (s, e) => OpenChild(new ReportsForm("STOCK"), "REPORTS"));
            mnuRep.DropDownItems.Add("Profit Report", null, (s, e) => OpenChild(new ReportsForm("PROFIT"), "REPORTS"));

            var mnuSet = new ToolStripMenuItem("Settings");
            mnuSet.DropDownItems.Add("Settings", null, (s, e) => OpenChild(new SettingsForm(), "SETTINGS"));

            mnuWindows = new ToolStripMenuItem("Windows");
            mnuWindows.DropDownOpening += MnuWindows_DropDownOpening;

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuTrans, mnuInv, mnuRep, mnuSet, mnuWindows });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // ===== STATUS STRIP =====
            statusStrip = new StatusStrip();
            statusStrip.BackColor = UiHelper.ThemeDark;
            statusStrip.ForeColor = Color.White;
            statusStrip.Font = UiHelper.SmallFont;

            string phone = UiHelper.GetShopPhone();
            lblShop = new ToolStripStatusLabel(shop + (string.IsNullOrEmpty(phone) ? "" : "  |  " + phone))
            {
                ForeColor = Color.White,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblUser = new ToolStripStatusLabel(CurrentUser.FullName + " (" + CurrentUser.Role + ")") { ForeColor = Color.White };
            string brand = UiHelper.AppName + "  v" + UiHelper.AppVersion;
            if (!string.IsNullOrEmpty(UiHelper.ContactNumber))
                brand += "  |  " + UiHelper.ContactNumber;
            lblBrand = new ToolStripStatusLabel(brand + "  |  F4=Close  F12=Save") { ForeColor = Color.White };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblShop, lblUser, lblBrand });
            this.Controls.Add(statusStrip);

            this.Load += (s, e) =>
            {
                foreach (Control c in this.Controls)
                {
                    if (c is MdiClient)
                    {
                        c.BackColor = UiHelper.BgColor;
                        SetupHome((MdiClient)c);
                    }
                }
            };
        }

        // ===== HOME SCREEN (welcome + dashboard toggle, shown when no child windows) =====

        private void SetupHome(MdiClient mdi)
        {
            homeScreen = new HomeScreen(CurrentUser);
            mdi.Controls.Add(homeScreen);
            homeScreen.BringToFront();

            welcomeTimer = new Timer { Interval = 350 };
            welcomeTimer.Tick += (s, e) => UpdateHomeVisibility();
            welcomeTimer.Start();
        }

        private void UpdateHomeVisibility()
        {
            if (homeScreen == null) return;
            bool show = this.MdiChildren.Length == 0;
            if (show && !homeScreen.Visible)
            {
                homeScreen.RefreshBranding();
                homeScreen.RefreshData();
            }
            homeScreen.Visible = show;
        }

        public void RefreshBranding()
        {
            string shop = UiHelper.GetShopName();
            string phone = UiHelper.GetShopPhone();
            if (lblShop != null)
                lblShop.Text = shop + (string.IsNullOrEmpty(phone) ? "" : "  |  " + phone);
            this.Text = UiHelper.AppName + " — " + shop;
            this.BackColor = UiHelper.BgColor;
            if (menuStrip != null) { menuStrip.BackColor = UiHelper.ThemeColor; menuStrip.Font = UiHelper.NormalFont; }
            if (statusStrip != null) { statusStrip.BackColor = UiHelper.ThemeDark; statusStrip.Font = UiHelper.SmallFont; }
            if (lblBrand != null)
            {
                string brand = UiHelper.AppName + "  v" + UiHelper.AppVersion;
                if (!string.IsNullOrEmpty(UiHelper.ContactNumber))
                    brand += "  |  " + UiHelper.ContactNumber;
                lblBrand.Text = brand + "  |  F4=Close  F12=Save";
            }
            foreach (Control c in this.Controls)
                if (c is MdiClient) c.BackColor = UiHelper.BgColor;
            if (homeScreen != null) homeScreen.RefreshBranding();
        }

        private void ApplyPermissions()
        {
            foreach (ToolStripMenuItem top in menuStrip.Items)
            {
                if (top.Text == "File" || top.Text == "Windows") continue;
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
            switch (menuText)
            {
                case "Sale": return "SALE";
                case "Purchase": return "PURCHASE";
                case "New Item": return "NEWITEM";
                case "Stock Position":
                case "Low Stock Report": return "INVENTORY";
                case "Sales Report":
                case "Purchase Report":
                case "Stock Report":
                case "Profit Report": return "REPORTS";
                case "Settings": return "SETTINGS";
                default: return "";
            }
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
                var item = new ToolStripMenuItem(child.Text);
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
            });
        }

        public void OpenChild(Form child, string permKey)
        {
            if (homeScreen != null) homeScreen.Visible = false;
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
                    if (f.GetType() == child.GetType())
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
            child.Show();
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
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!UiHelper.ConfirmExit()) e.Cancel = true;
            }
            if (e.Cancel) return;
            if (welcomeTimer != null) { welcomeTimer.Stop(); welcomeTimer.Dispose(); welcomeTimer = null; }
        }
    }

    public class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeMenuRenderer() : base(new ThemeColorTable()) { }
    }

    public class ThemeColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(30, 100, 150);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(30, 100, 150);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(30, 100, 150);
        public override Color MenuItemBorder => Color.FromArgb(30, 100, 150);
        public override Color MenuBorder => Color.FromArgb(41, 128, 185);
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(41, 128, 185);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(41, 128, 185);
    }
}
