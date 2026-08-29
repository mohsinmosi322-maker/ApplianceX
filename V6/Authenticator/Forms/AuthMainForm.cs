using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Authenticator.Helpers;

namespace Authenticator.Forms
{
    public partial class AuthMainForm : Form
    {
        private TextBox txtStore, txtPhone, txtPrefix, txtConn, txtClientId, txtDays, txtSoftware, txtContact, txtAppVer;
        private DateTimePicker dtExpiry;
        private CheckBox chkPrint;
        private Label lblStatus;
        private TabControl tabs;

        public AuthMainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Appliance Management - Authenticator (Deploy + Admin Tools)";
            this.Size = new Size(640, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            Label title = new Label
            {
                Text = "DEPLOYMENT AUTHENTICATOR",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Location = new Point(20, 10),
                Size = new Size(580, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(title);

            tabs = new TabControl { Location = new Point(15, 45), Size = new Size(600, 600) };

            var tabLic = new TabPage("License / Deploy");
            int y = 15;
            AddL(tabLic, "Store Name:", 15, y); txtStore = AddT(tabLic, 180, y, 380); y += 34;
            AddL(tabLic, "Shop Phone:", 15, y); txtPhone = AddT(tabLic, 180, y, 380); y += 34;
            AddL(tabLic, "Software Name:", 15, y); txtSoftware = AddT(tabLic, 180, y, 380); txtSoftware.Text = "Appliance Management System"; y += 34;
            AddL(tabLic, "Vendor Contact:", 15, y); txtContact = AddT(tabLic, 180, y, 380); txtContact.Text = "+92-300-1234567"; y += 34;
            AddL(tabLic, "App Version:", 15, y); txtAppVer = AddT(tabLic, 180, y, 120); txtAppVer.Text = "2.1.0"; y += 34;
            AddL(tabLic, "Invoice Prefix:", 15, y); txtPrefix = AddT(tabLic, 180, y, 120); txtPrefix.Text = "INV-"; y += 34;
            AddL(tabLic, "License Days:", 15, y);
            txtDays = AddT(tabLic, 180, y, 80); txtDays.Text = "365";
            Button btnCalc = new Button { Text = "Set Expiry", Location = new Point(280, y - 2), Size = new Size(100, 28) };
            btnCalc.Click += (s, e) => { int d = 365; int.TryParse(txtDays.Text, out d); dtExpiry.Value = DateTime.Today.AddDays(d); };
            tabLic.Controls.Add(btnCalc); y += 34;
            AddL(tabLic, "Expiry Date:", 15, y);
            dtExpiry = new DateTimePicker { Location = new Point(180, y - 2), Size = new Size(180, 26), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today.AddYears(1) };
            tabLic.Controls.Add(dtExpiry); y += 34;
            chkPrint = new CheckBox { Text = "Allow Bill Printing", Font = new Font("Segoe UI", 10F), Location = new Point(180, y), Size = new Size(200, 24), Checked = true };
            tabLic.Controls.Add(chkPrint); y += 34;
            AddL(tabLic, "SQL Connection:", 15, y); y += 24;
            txtConn = new TextBox { Location = new Point(15, y), Size = new Size(550, 26), Text = "Data Source=.;Initial Catalog=APPLIANCE_DB;Integrated Security=True" };
            tabLic.Controls.Add(txtConn); y += 34;
            AddL(tabLic, "Client ID:", 15, y); txtClientId = AddT(tabLic, 180, y, 160);
            txtClientId.Text = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); y += 45;

            Button btnGen = new Button { Text = "GENERATE license.dat", Location = new Point(15, y), Size = new Size(220, 38), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnGen.Click += BtnGenerate_Click;
            tabLic.Controls.Add(btnGen);
            Button btnLoad = new Button { Text = "LOAD license.dat", Location = new Point(250, y), Size = new Size(180, 38), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnLoad.Click += BtnLoad_Click;
            tabLic.Controls.Add(btnLoad);
            Button btnPwd = new Button { Text = "Change Password", Location = new Point(450, y), Size = new Size(130, 38), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnPwd.Click += (s, ev) => ChangeMasterPassword();
            tabLic.Controls.Add(btnPwd); y += 50;
            lblStatus = new Label { Text = "Discount limits = main app Settings. This tool: shop, license, print, admin tools.", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.Gray, Location = new Point(15, y), Size = new Size(550, 40) };
            tabLic.Controls.Add(lblStatus);
            tabs.TabPages.Add(tabLic);

            var tabTools = new TabPage("Admin Tools (Client DB)");
            Label info = new Label
            {
                Text = "Ye tools client ke database par chalte hain.\nPehle License tab mein Connection String set karein.",
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(20, 20),
                Size = new Size(540, 50)
            };
            tabTools.Controls.Add(info);

            Button b1 = MakeToolBtn("Manage Products (edit / disable)", 20, 90);
            b1.Click += (s, e) => OpenProductTool();
            tabTools.Controls.Add(b1);

            Button b2 = MakeToolBtn("Modify Sale (paid / remarks)", 20, 145);
            b2.Click += (s, e) => OpenSaleModTool();
            tabTools.Controls.Add(b2);

            Button b3 = MakeToolBtn("Modify Purchase (paid / remarks)", 20, 200);
            b3.Click += (s, e) => OpenPurchaseModTool();
            tabTools.Controls.Add(b3);

            Button bTest = MakeToolBtn("Test Connection", 20, 270);
            bTest.BackColor = Color.FromArgb(52, 73, 94);
            bTest.Click += (s, e) => TestConn();
            tabTools.Controls.Add(bTest);

            Button bReset = MakeToolBtn("Reset Client Data (sales / purchases / stock)", 20, 340);
            bReset.BackColor = Color.FromArgb(192, 57, 43);
            bReset.Click += (s, e) => ResetClientData();
            tabTools.Controls.Add(bReset);

            Label warn = new Label
            {
                Text = "Reset: pehle check karega ke data already hai. Haan par sales, purchases,\ninventory clear ho jayegi. Users / shop settings safe rehte hain.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 40, 40),
                Location = new Point(20, 400),
                Size = new Size(540, 50)
            };
            tabTools.Controls.Add(warn);

            tabs.TabPages.Add(tabTools);
            this.Controls.Add(tabs);
        }

        private Button MakeToolBtn(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(400, 42),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
        }

        private void AddL(Control parent, string t, int x, int y)
        {
            parent.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 10F), Location = new Point(x, y + 2), Size = new Size(160, 22) });
        }
        private TextBox AddT(Control parent, int x, int y, int w)
        {
            var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 26), Font = new Font("Segoe UI", 10F) };
            parent.Controls.Add(t);
            return t;
        }

        private string Conn()
        {
            return string.IsNullOrWhiteSpace(txtConn.Text)
                ? "Data Source=.;Initial Catalog=APPLIANCE_DB;Integrated Security=True"
                : txtConn.Text.Trim();
        }

        private void TestConn()
        {
            try
            {
                using (var c = new SqlConnection(Conn()))
                {
                    c.Open();
                    MessageBox.Show("Connection OK.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Failed: " + ex.Message); }
        }

        private void ResetClientData()
        {
            try
            {
                using (var c = new SqlConnection(Conn()))
                {
                    c.Open();
                    int sales = Scalar(c, "SELECT COUNT(1) FROM SaleHeader");
                    int purs = Scalar(c, "SELECT COUNT(1) FROM PurchaseHeader");
                    int inv = Scalar(c, "SELECT COUNT(1) FROM InventoryTransaction");
                    int prods = Scalar(c, "SELECT COUNT(1) FROM Products WHERE IsActive=1");

                    if (sales + purs + inv == 0)
                    {
                        MessageBox.Show("No transactional data found. Nothing to reset.", "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string msg =
                        "Data already exists in this database:\n\n" +
                        "  Sales invoices     : " + sales + "\n" +
                        "  Purchase invoices  : " + purs + "\n" +
                        "  Inventory rows     : " + inv + "\n" +
                        "  Active products    : " + prods + "\n\n" +
                        "Reset / remove this transactional data?\n\n" +
                        "YES = delete sales, purchases, inventory + set stock to 0\n" +
                        "NO  = cancel (no change)";

                    if (MessageBox.Show(msg, "Confirm Reset Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return;

                    if (MessageBox.Show("Final confirm: DELETE all sales & purchases?", "Final Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return;

                    using (var tx = c.BeginTransaction())
                    {
                        Exec(c, tx, "DELETE FROM SaleDetail");
                        Exec(c, tx, "DELETE FROM SaleHeader");
                        Exec(c, tx, "DELETE FROM PurchaseDetail");
                        Exec(c, tx, "DELETE FROM PurchaseHeader");
                        Exec(c, tx, "DELETE FROM InventoryTransaction");
                        Exec(c, tx, "UPDATE Products SET CurrentStock = 0");
                        Exec(c, tx, "IF EXISTS (SELECT 1 FROM Settings WHERE SettingName='NextInvoiceNumber') UPDATE Settings SET SettingValue='1' WHERE SettingName='NextInvoiceNumber'");
                        Exec(c, tx, "IF EXISTS (SELECT 1 FROM Settings WHERE SettingName='NextPurchaseInvoiceNumber') UPDATE Settings SET SettingValue='1' WHERE SettingName='NextPurchaseInvoiceNumber'");
                        tx.Commit();
                    }
                    MessageBox.Show("Transactional data cleared. Users and products list kept.", "Reset Done");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset failed: " + ex.Message);
            }
        }

        private static int Scalar(SqlConnection c, string sql)
        {
            using (var cmd = new SqlCommand(sql, c))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void Exec(SqlConnection c, SqlTransaction tx, string sql)
        {
            using (var cmd = new SqlCommand(sql, c, tx))
                cmd.ExecuteNonQuery();
        }

        private void OpenProductTool()
        {
            try { using (var f = new AdminProductForm(Conn())) f.ShowDialog(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OpenSaleModTool()
        {
            try { using (var f = new AdminSaleModifyForm(Conn())) f.ShowDialog(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OpenPurchaseModTool()
        {
            try { using (var f = new AdminPurchaseModifyForm(Conn())) f.ShowDialog(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStore.Text))
            {
                MessageBox.Show("Store name required.");
                return;
            }
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "License file|license.dat";
                sfd.FileName = "license.dat";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var info = new LicenseHelper.LicenseInfo
                {
                    StoreName = txtStore.Text.Trim(),
                    ShopPhone = txtPhone.Text.Trim(),
                    InvoicePrefix = txtPrefix.Text.Trim(),
                    ExpiryDate = dtExpiry.Value.Date,
                    AllowPrint = chkPrint.Checked,
                    MaxDiscountAdmin = 0,
                    MaxDiscountUser = 0,
                    ConnectionString = txtConn.Text.Trim(),
                    ClientId = txtClientId.Text.Trim(),
                    SoftwareName = txtSoftware != null ? txtSoftware.Text.Trim() : "Appliance Management System",
                    VendorContact = txtContact != null ? txtContact.Text.Trim() : "",
                    AppVersion = txtAppVer != null ? txtAppVer.Text.Trim() : "2.1.0"
                };
                LicenseHelper.SaveLicense(sfd.FileName, info);
                lblStatus.Text = "Saved: " + sfd.FileName + " | Expiry: " + info.ExpiryDate.ToString("dd/MM/yyyy");
                MessageBox.Show("license.dat generated.\nCopy next to client ApplianceManagement.exe");
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "License|license.dat|All|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var info = LicenseHelper.LoadLicense(ofd.FileName);
                    if (info == null) { MessageBox.Show("Invalid."); return; }
                    txtStore.Text = info.StoreName;
                    txtPhone.Text = info.ShopPhone;
                    txtPrefix.Text = info.InvoicePrefix;
                    dtExpiry.Value = info.ExpiryDate;
                    chkPrint.Checked = info.AllowPrint;
                    txtConn.Text = info.ConnectionString;
                    txtClientId.Text = info.ClientId;
                    if (txtSoftware != null) txtSoftware.Text = info.SoftwareName ?? "";
                    if (txtContact != null) txtContact.Text = info.VendorContact ?? "";
                    if (txtAppVer != null) txtAppVer.Text = info.AppVersion ?? "";
                    lblStatus.Text = LicenseHelper.IsExpired(info) ? "EXPIRED" : "Valid until " + info.ExpiryDate.ToString("dd/MM/yyyy");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void ChangeMasterPassword()
        {
            using (var f = new Form())
            {
                f.Text = "Change Master Password";
                f.Size = new Size(360, 160);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                var t = new TextBox { Location = new Point(20, 20), Size = new Size(300, 26), PasswordChar = '\u25cf' };
                var b = new Button { Text = "Save", Location = new Point(20, 60), Size = new Size(100, 30) };
                b.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(t.Text)) { MessageBox.Show("Enter password."); return; }
                    AuthLoginForm.SetMasterPassword(t.Text.Trim());
                    MessageBox.Show("Master password updated.");
                    f.DialogResult = DialogResult.OK;
                    f.Close();
                };
                f.Controls.Add(t);
                f.Controls.Add(b);
                f.ShowDialog(this);
            }
        }
    }
}
