using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// F1 quick edit: Name, TP, Disc%, RP.
    /// RP = TP / ((100-Disc%)/100). Disc% = 100*(1 - TP/RP).
    /// </summary>
    public class ProductQuickEditForm : Form
    {
        private readonly ProductService _svc = new ProductService();
        private readonly Product _product;

        private TextBox txtName, txtTp, txtDisc, txtRp;
        private bool calcBusy;
        public bool Saved { get; private set; }

        public ProductQuickEditForm(Product product)
        {
            if (product == null) throw new ArgumentNullException("product");
            _product = product;
            InitializeComponent();
            LoadProduct();
        }

        private void InitializeComponent()
        {
            Text = "Edit Product (F1)";
            Size = new Size(460, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachEnterNavigation(this);
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F12) { e.Handled = true; Save(); }
                if (e.KeyCode == Keys.Escape) { e.Handled = true; DialogResult = DialogResult.Cancel; Close(); }
            };

            Controls.Add(UiHelper.CreateFormBanner(
                "EDIT PRODUCT",
                "Margin: RP = TP / ((100-Disc%)/100)",
                FormAccent.NewItem, FormAccent.NewItemDark));

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
            Controls.Add(card);
            Controls.SetChildIndex(card, 0);

            int y = 12;
            card.Controls.Add(new Label { Text = "Code", Location = new Point(0, y + 4), AutoSize = true, Font = UiHelper.SmallFont, ForeColor = Color.Gray });
            card.Controls.Add(new Label
            {
                Text = _product.ProductCode ?? "",
                Location = new Point(120, y + 2),
                AutoSize = true,
                Font = UiHelper.HeaderFont
            });
            y += 36;

            card.Controls.Add(new Label { Text = "Name", Location = new Point(0, y + 4), Size = new Size(110, 22), Font = UiHelper.NormalFont });
            txtName = new TextBox { Location = new Point(120, y), Size = new Size(260, 28) };
            UiHelper.StyleTextBox(txtName);
            card.Controls.Add(txtName);
            y += 40;

            card.Controls.Add(new Label { Text = "TP (Purchase)", Location = new Point(0, y + 4), Size = new Size(110, 22), Font = UiHelper.NormalFont });
            txtTp = new TextBox { Location = new Point(120, y), Size = new Size(120, 28), TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtTp);
            txtTp.TextChanged += (s, e) => OnTpOrDiscChanged();
            card.Controls.Add(txtTp);
            y += 40;

            card.Controls.Add(new Label { Text = "Disc %", Location = new Point(0, y + 4), Size = new Size(110, 22), Font = UiHelper.NormalFont });
            txtDisc = new TextBox { Location = new Point(120, y), Size = new Size(80, 28), TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtDisc);
            txtDisc.TextChanged += (s, e) => OnTpOrDiscChanged();
            card.Controls.Add(txtDisc);
            card.Controls.Add(new Label
            {
                Text = "RP = TP / ((100-Disc%)/100)",
                Location = new Point(210, y + 6),
                AutoSize = true,
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray
            });
            y += 40;

            card.Controls.Add(new Label { Text = "RP (Sale)", Location = new Point(0, y + 4), Size = new Size(110, 22), Font = UiHelper.NormalFont });
            txtRp = new TextBox { Location = new Point(120, y), Size = new Size(120, 28), TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(txtRp);
            txtRp.TextChanged += (s, e) => OnRpChanged();
            card.Controls.Add(txtRp);
            y += 48;

            var btnSave = new Button { Text = "SAVE (F12)", Location = new Point(120, y), Size = new Size(120, 36) };
            var btnClose = new Button { Text = "CLOSE", Location = new Point(250, y), Size = new Size(100, 36) };
            UiHelper.StyleButton(btnSave);
            UiHelper.StyleButton(btnClose);
            btnSave.Click += (s, e) => Save();
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            card.Controls.Add(btnSave);
            card.Controls.Add(btnClose);
        }

        private void LoadProduct()
        {
            calcBusy = true;
            txtName.Text = _product.ProductName ?? "";
            decimal tp = _product.PurchasePrice;
            decimal rp = _product.SalePrice;
            txtTp.Text = tp.ToString("0.00");
            txtRp.Text = rp.ToString("0.00");
            decimal disc = 0;
            if (rp > 0) disc = Math.Round(100m * (1m - tp / rp), 2);
            if (disc < 0) disc = 0;
            if (disc >= 100) disc = 99.99m;
            txtDisc.Text = disc.ToString("0.##");
            calcBusy = false;
        }

        private void OnTpOrDiscChanged()
        {
            // RP = TP / ((100 - Disc%) / 100) = TP * 100 / (100 - Disc%)
            if (calcBusy) return;
            calcBusy = true;
            decimal tp = 0, disc = 0;
            decimal.TryParse(txtTp.Text, out tp);
            decimal.TryParse(txtDisc.Text, out disc);
            if (disc < 0) disc = 0;
            if (disc >= 100) disc = 99.99m;
            decimal factor = (100m - disc) / 100m;
            decimal rp = factor > 0 ? Math.Round(tp / factor, 2) : 0;
            txtRp.Text = rp.ToString("0.00");
            calcBusy = false;
        }

        private void OnRpChanged()
        {
            // Disc% = 100 * (1 - TP/RP)
            if (calcBusy) return;
            calcBusy = true;
            decimal tp = 0, rp = 0;
            decimal.TryParse(txtTp.Text, out tp);
            decimal.TryParse(txtRp.Text, out rp);
            decimal disc = 0;
            if (rp > 0) disc = Math.Round(100m * (1m - tp / rp), 2);
            if (disc < 0) disc = 0;
            if (disc >= 100) disc = 99.99m;
            txtDisc.Text = disc.ToString("0.##");
            calcBusy = false;
        }

        private void Save()
        {
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogHelpers.Warn(this, "Name required.");
                txtName.Focus();
                return;
            }
            decimal tp = 0, rp = 0;
            decimal.TryParse(txtTp.Text, out tp);
            decimal.TryParse(txtRp.Text, out rp);
            if (tp < 0) tp = 0;
            if (rp < 0) rp = 0;
            try
            {
                _svc.Update(
                    _product.ProductID,
                    name,
                    tp,
                    rp,
                    _product.MinimumStock,
                    true,
                    _product.UnitOfMeasure,
                    _product.PackSize > 0 ? _product.PackSize : 1);
                _product.ProductName = name;
                _product.PurchasePrice = tp;
                _product.SalePrice = rp;
                Saved = true;
                DialogHelpers.Info(this, "Product updated.\nTP: " + tp.ToString("0.00") + "  RP: " + rp.ToString("0.00"));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, ex.Message);
            }
        }
    }
}
