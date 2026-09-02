using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// Quick product edit (F1 from Sale/Purchase).
    /// Margin: RP = TP / ((100 - Disc%) / 100)
    ///         TP = RP * ((100 - Disc%) / 100)
    /// </summary>
    public class ProductQuickEditForm : Form
    {
        private readonly ProductRepository productRepo = new ProductRepository();
        private readonly Product product;
        private TextBox txtName, txtTp, txtDisc, txtRp;
        private bool busy;

        public bool Saved { get; private set; }

        public ProductQuickEditForm(Product p)
        {
            product = p ?? throw new ArgumentNullException(nameof(p));
            InitializeComponent();
            LoadProduct();
            UiHelper.AttachEnterNavigation(this);
            UiHelper.AttachF4Close(this, true);
        }

        private void InitializeComponent()
        {
            Text = "Quick Edit Product";
            Size = new Size(420, 280);
            MinimumSize = new Size(380, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            BackColor = UiHelper.BgColor;

            Controls.Add(UiHelper.CreateFormBanner("EDIT PRODUCT", "F1 quick edit · margin Disc%", FormAccent.NewItem, FormAccent.NewItemDark));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(16, 12, 16, 8)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 4; i++)
                body.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            body.Controls.Add(new Label { Text = "Name", Font = UiHelper.NormalFont, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            txtName = new TextBox { Dock = DockStyle.Fill };
            UiHelper.StyleTextBox(txtName);
            body.Controls.Add(txtName, 1, 0);

            body.Controls.Add(new Label { Text = "TP (Cost)", Font = UiHelper.NormalFont, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            txtTp = new TextBox { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtTp);
            txtTp.TextChanged += (s, e) => OnTpOrDiscChanged();
            body.Controls.Add(txtTp, 1, 1);

            body.Controls.Add(new Label { Text = "Disc %", Font = UiHelper.NormalFont, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            txtDisc = new TextBox { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtDisc);
            txtDisc.TextChanged += (s, e) => OnTpOrDiscChanged();
            body.Controls.Add(txtDisc, 1, 2);

            body.Controls.Add(new Label { Text = "RP (Sale)", Font = UiHelper.NormalFont, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            txtRp = new TextBox { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtRp);
            txtRp.TextChanged += (s, e) => OnRpChanged();
            body.Controls.Add(txtRp, 1, 3);

            Controls.Add(body);

            var foot = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(16, 8, 16, 8) };
            var btnSave = new Button { Text = "SAVE", Size = new Size(100, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var btnCancel = new Button { Text = "CANCEL (F4)", Size = new Size(120, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StylePrimaryButton(btnSave);
            UiHelper.StyleSecondaryButton(btnCancel);
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            foot.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(foot.ClientSize.Width - btnCancel.Width - 16, 8);
                btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 8, 8);
            };
            foot.Controls.Add(btnSave);
            foot.Controls.Add(btnCancel);
            Controls.Add(foot);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadProduct()
        {
            busy = true;
            txtName.Text = product.ProductName ?? "";
            decimal tp = product.PurchasePrice;
            decimal rp = product.UnitSalePrice;
            txtTp.Text = tp.ToString("0.##");
            txtRp.Text = rp.ToString("0.##");
            decimal disc = 0;
            if (rp > 0 && tp >= 0 && tp <= rp)
                disc = Math.Round(100m * (1m - tp / rp), 2);
            txtDisc.Text = disc.ToString("0.##");
            busy = false;
        }

        private void OnTpOrDiscChanged()
        {
            if (busy) return;
            busy = true;
            decimal tp = 0, disc = 0;
            decimal.TryParse(txtTp.Text, out tp);
            decimal.TryParse(txtDisc.Text, out disc);
            if (disc < 0) disc = 0;
            if (disc >= 100) disc = 99.99m;
            decimal factor = (100m - disc) / 100m;
            decimal rp = factor > 0 ? Math.Round(tp / factor, 2) : tp;
            txtRp.Text = rp.ToString("0.##");
            busy = false;
        }

        private void OnRpChanged()
        {
            if (busy) return;
            busy = true;
            decimal rp = 0, disc = 0;
            decimal.TryParse(txtRp.Text, out rp);
            decimal.TryParse(txtDisc.Text, out disc);
            if (disc < 0) disc = 0;
            if (disc >= 100) disc = 99.99m;
            decimal factor = (100m - disc) / 100m;
            decimal tp = Math.Round(rp * factor, 2);
            txtTp.Text = tp.ToString("0.##");
            busy = false;
        }

        private void Save()
        {
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogHelpers.Warn(this, "Product name is required.");
                txtName.Focus();
                return;
            }
            decimal tp = 0, rp = 0;
            decimal.TryParse(txtTp.Text, out tp);
            decimal.TryParse(txtRp.Text, out rp);
            if (tp < 0 || rp < 0)
            {
                DialogHelpers.Warn(this, "Prices cannot be negative.");
                return;
            }
            product.ProductName = name;
            product.PurchasePrice = tp;
            product.UnitSalePrice = rp;
            try
            {
                productRepo.Update(product);
                Saved = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, "Save failed: " + ex.Message);
            }
        }
    }
}
