using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SettingsForm : Form
    {
        private SupplierRepository supplierRepo = new SupplierRepository();
        private TextBox txtSupName, txtSupPhone, txtMaxDiscAdmin, txtMaxDiscUser;
        private ComboBox cmbTheme, cmbFontSize, cmbResolution;
        private DataGridView dgvSuppliers;

        public SettingsForm()
        {
            InitializeComponent();
            UiHelper.ApplyFormSize(this);
            LoadSuppliers();
            LoadValues();
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(900, 560);
            this.MinimumSize = new Size(800, 600);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            Panel top = new Panel { Dock = DockStyle.Top, Height = 230, BackColor = UiHelper.BgColor, Padding = new Padding(10) };

            GroupBox gbTheme = new GroupBox { Text = "Appearance (live)", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(15, 10), Size = new Size(420, 200) };
            gbTheme.Controls.Add(new Label { Text = "Theme:", Font = UiHelper.NormalFont, Location = new Point(15, 30), Size = new Size(90, 22) });
            cmbTheme = new ComboBox { Location = new Point(120, 28), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbTheme);
            cmbTheme.Items.AddRange(new object[] { "Blue", "Green", "Dark", "Purple", "Teal" });
            cmbTheme.SelectedIndex = 0;
            gbTheme.Controls.Add(cmbTheme);

            gbTheme.Controls.Add(new Label { Text = "Font Size:", Font = UiHelper.NormalFont, Location = new Point(15, 65), Size = new Size(90, 22) });
            cmbFontSize = new ComboBox { Location = new Point(120, 63), Size = new Size(100, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbFontSize);
            cmbFontSize.Items.AddRange(new object[] { "9", "10", "11", "12", "14" });
            cmbFontSize.SelectedItem = "10";
            gbTheme.Controls.Add(cmbFontSize);

            gbTheme.Controls.Add(new Label { Text = "Form Size:", Font = UiHelper.NormalFont, Location = new Point(15, 100), Size = new Size(90, 22) });
            cmbResolution = new ComboBox { Location = new Point(120, 98), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbResolution);
            cmbResolution.Items.AddRange(new object[] {
                "800 x 600", "1024 x 768", "1152 x 864", "1280 x 720", "1280 x 800", "1280 x 960", "1280 x 1024"
            });
            cmbResolution.SelectedItem = "1024 x 768";
            gbTheme.Controls.Add(cmbResolution);

            Button btnApply = new Button { Text = "Apply Live", Location = new Point(120, 145), Size = new Size(140, 36) };
            UiHelper.StyleButton(btnApply);
            btnApply.Click += (s, e) =>
            {
                AppSettings.Set("Theme", cmbTheme.SelectedItem.ToString());
                AppSettings.Set("FontSize", cmbFontSize.SelectedItem.ToString());
                AppSettings.Set("FormResolution", cmbResolution.SelectedItem.ToString());
                if (MainForm.Instance != null)
                {
                    UiHelper.ApplyThemeLive(MainForm.Instance);
                    MainForm.Instance.RefreshBranding();
                    UiHelper.ApplyFormSizeToAllChildren(MainForm.Instance);
                }
                UiHelper.ApplyThemeLive(this);
                UiHelper.ApplyFormSize(this);
                MessageBox.Show("Theme, font and form size applied to open windows.");
            };
            gbTheme.Controls.Add(btnApply);
            top.Controls.Add(gbTheme);

            GroupBox gbDisc = new GroupBox { Text = "Max Discount %", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(450, 10), Size = new Size(400, 200) };
            gbDisc.Controls.Add(new Label { Text = "Admin %:", Font = UiHelper.NormalFont, Location = new Point(15, 40), Size = new Size(100, 22) });
            txtMaxDiscAdmin = new TextBox { Location = new Point(120, 38), Size = new Size(100, 26), Text = "0" };
            UiHelper.StyleTextBox(txtMaxDiscAdmin);
            gbDisc.Controls.Add(txtMaxDiscAdmin);
            gbDisc.Controls.Add(new Label { Text = "User %:", Font = UiHelper.NormalFont, Location = new Point(15, 80), Size = new Size(100, 22) });
            txtMaxDiscUser = new TextBox { Location = new Point(120, 78), Size = new Size(100, 26), Text = "0" };
            UiHelper.StyleTextBox(txtMaxDiscUser);
            gbDisc.Controls.Add(txtMaxDiscUser);
            Button btnDisc = new Button { Text = "Save Limits", Location = new Point(120, 145), Size = new Size(140, 36) };
            UiHelper.StyleButton(btnDisc);
            btnDisc.Click += (s, e) =>
            {
                AppSettings.Set("MaxDiscountAdmin", txtMaxDiscAdmin.Text.Trim());
                AppSettings.Set("MaxDiscountUser", txtMaxDiscUser.Text.Trim());
                MessageBox.Show("Discount limits saved.");
            };
            gbDisc.Controls.Add(btnDisc);
            top.Controls.Add(gbDisc);
            this.Controls.Add(top);

            // Supplier section bottom
            Panel mid = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = UiHelper.BgColor };
            GroupBox gbSup = new GroupBox { Text = "Add Supplier", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(15, 5), Size = new Size(420, 105) };
            gbSup.Controls.Add(new Label { Text = "Name:", Font = UiHelper.NormalFont, Location = new Point(15, 30), Size = new Size(60, 22) });
            txtSupName = new TextBox { Location = new Point(80, 28), Size = new Size(220, 26) };
            UiHelper.StyleTextBox(txtSupName);
            gbSup.Controls.Add(txtSupName);
            gbSup.Controls.Add(new Label { Text = "Phone:", Font = UiHelper.NormalFont, Location = new Point(15, 65), Size = new Size(60, 22) });
            txtSupPhone = new TextBox { Location = new Point(80, 63), Size = new Size(150, 26) };
            UiHelper.StyleTextBox(txtSupPhone);
            gbSup.Controls.Add(txtSupPhone);
            Button btnAddSup = new Button { Text = "Add", Location = new Point(250, 60), Size = new Size(100, 30) };
            UiHelper.StyleButton(btnAddSup);
            btnAddSup.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSupName.Text)) { MessageBox.Show("Name required."); return; }
                supplierRepo.Insert(new Supplier { SupplierName = txtSupName.Text.Trim(), Phone = txtSupPhone.Text.Trim() });
                MessageBox.Show("Supplier added.");
                txtSupName.Clear(); txtSupPhone.Clear();
                LoadSuppliers();
            };
            gbSup.Controls.Add(btnAddSup);
            mid.Controls.Add(gbSup);
            this.Controls.Add(mid);

            Panel bottomNote = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = UiHelper.BgColor };
            bottomNote.Controls.Add(new Label
            {
                Text = "Store name, software name, contact, version = Authenticator (license.dat).",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            });
            this.Controls.Add(bottomNote);

            dgvSuppliers = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgvSuppliers);
            this.Controls.Add(dgvSuppliers);
            this.Controls.SetChildIndex(dgvSuppliers, 0);
        }

        private void LoadSuppliers()
        {
            dgvSuppliers.DataSource = null;
            dgvSuppliers.DataSource = supplierRepo.GetAllActive();
            foreach (var h in new[] { "SupplierID", "IsActive", "OpeningBalance", "CreatedDate", "Address" })
                if (dgvSuppliers.Columns.Contains(h)) dgvSuppliers.Columns[h].Visible = false;
        }

        private void LoadValues()
        {
            string t = AppSettings.Get("Theme");
            if (!string.IsNullOrEmpty(t) && cmbTheme.Items.Contains(t)) cmbTheme.SelectedItem = t;
            string f = AppSettings.Get("FontSize");
            if (!string.IsNullOrEmpty(f) && cmbFontSize.Items.Contains(f)) cmbFontSize.SelectedItem = f;
            string r = AppSettings.Get("FormResolution");
            if (!string.IsNullOrEmpty(r) && cmbResolution.Items.Contains(r)) cmbResolution.SelectedItem = r;
            txtMaxDiscAdmin.Text = string.IsNullOrEmpty(AppSettings.Get("MaxDiscountAdmin")) ? "0" : AppSettings.Get("MaxDiscountAdmin");
            txtMaxDiscUser.Text = string.IsNullOrEmpty(AppSettings.Get("MaxDiscountUser")) ? "0" : AppSettings.Get("MaxDiscountUser");
        }
    }
}
