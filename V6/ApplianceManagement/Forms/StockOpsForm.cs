using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class StockOpsForm : Form
    {
        private readonly ProductRepository _products = new ProductRepository();
        private readonly StockOpsService _ops = new StockOpsService();
        private ComboBox cmbMode, cmbProduct;
        private TextBox txtQty, txtCost, txtReason;

        public StockOpsForm()
        {
            Text = "Stock Operations";
            Size = new Size(560, 420);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this);

            Controls.Add(UiHelper.CreateFormBanner("STOCK OPERATIONS",
                "Opening · Adjustment · Damage — all via inventory ledger",
                FormAccent.Inventory, FormAccent.InventoryDark));

            var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = Color.White };
            Controls.Add(card);

            int y = 20;
            card.Controls.Add(new Label { Text = "Operation", Location = new Point(0, y), AutoSize = true, Font = UiHelper.NormalFont });
            cmbMode = new ComboBox { Location = new Point(140, y - 2), Size = new Size(280, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMode.Items.AddRange(new object[] { "Opening Stock", "Adjustment (+/-)", "Damage / Write-off" });
            cmbMode.SelectedIndex = 0;
            UiHelper.StyleComboBox(cmbMode);
            card.Controls.Add(cmbMode);
            y += 44;

            card.Controls.Add(new Label { Text = "Product", Location = new Point(0, y), AutoSize = true, Font = UiHelper.NormalFont });
            cmbProduct = new ComboBox { Location = new Point(140, y - 2), Size = new Size(340, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProduct.DataSource = _products.GetAllActive();
            cmbProduct.DisplayMember = "ToString";
            UiHelper.StyleComboBox(cmbProduct);
            card.Controls.Add(cmbProduct);
            y += 44;

            card.Controls.Add(new Label { Text = "Quantity (units)", Location = new Point(0, y), AutoSize = true, Font = UiHelper.NormalFont });
            txtQty = new TextBox { Location = new Point(140, y - 2), Size = new Size(120, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            card.Controls.Add(txtQty);
            card.Controls.Add(new Label { Text = "(Adjustment: use negative to reduce)", Location = new Point(280, y), AutoSize = true, Font = UiHelper.SmallFont, ForeColor = Color.Gray });
            y += 44;

            card.Controls.Add(new Label { Text = "Unit cost", Location = new Point(0, y), AutoSize = true, Font = UiHelper.NormalFont });
            txtCost = new TextBox { Location = new Point(140, y - 2), Size = new Size(120, 28), Text = "0" };
            UiHelper.StyleTextBox(txtCost);
            card.Controls.Add(txtCost);
            y += 44;

            card.Controls.Add(new Label { Text = "Reason", Location = new Point(0, y), AutoSize = true, Font = UiHelper.NormalFont });
            txtReason = new TextBox { Location = new Point(140, y - 2), Size = new Size(340, 28) };
            UiHelper.StyleTextBox(txtReason);
            card.Controls.Add(txtReason);
            y += 56;

            var btn = new Button { Text = "POST", Location = new Point(140, y), Size = new Size(140, 36) };
            UiHelper.StyleAccentButton(btn, FormAccent.Inventory, FormAccent.InventoryDark);
            btn.Click += (s, e) => Post();
            card.Controls.Add(btn);
        }

        private void Post()
        {
            if (!(cmbProduct.SelectedItem is Product p)) { MessageBox.Show("Select product."); return; }
            int qty = 0; int.TryParse(txtQty.Text, out qty);
            decimal cost = 0; decimal.TryParse(txtCost.Text, out cost);
            string reason = txtReason.Text.Trim();
            try
            {
                string mode = cmbMode.SelectedItem.ToString();
                if (mode.StartsWith("Opening"))
                    _ops.Opening(p.ProductID, qty, cost > 0 ? cost : p.PurchasePrice, reason);
                else if (mode.StartsWith("Damage"))
                    _ops.Damage(p.ProductID, qty, cost > 0 ? cost : p.PurchasePrice, reason);
                else
                    _ops.Adjust(p.ProductID, qty, cost > 0 ? cost : p.PurchasePrice, reason);
                MessageBox.Show("Posted to inventory ledger.");
                Tag = "NOSAVECONFIRM";
                txtQty.Text = "1";
                txtReason.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
