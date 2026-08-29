using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class SupplierPaymentForm : Form
    {
        private ComboBox cmbSupplier;
        private TextBox txtAmount, txtRemarks;
        private Label lblBalance;
        private readonly SupplierRepository _sup = new SupplierRepository();
        private readonly SupplierAccountService _acct = new SupplierAccountService();

        public SupplierPaymentForm()
        {
            Text = "Supplier Payment";
            Size = new Size(520, 360);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this);
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) { Save(); e.Handled = true; } };

            Controls.Add(UiHelper.CreateFormBanner("SUPPLIER PAYMENT", "Record payment against payable · F12 save",
                FormAccent.Purchase, FormAccent.PurchaseDark));

            var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = Color.White };
            Controls.Add(card);

            int y = 16;
            card.Controls.Add(new Label { Text = "Supplier", Location = new Point(0, y), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(120, y - 2), Size = new Size(320, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSupplier.DataSource = _sup.GetAllActive();
            cmbSupplier.DisplayMember = "SupplierName";
            cmbSupplier.SelectedIndexChanged += (s, e) => RefreshBal();
            UiHelper.StyleComboBox(cmbSupplier);
            card.Controls.Add(cmbSupplier);
            y += 40;
            lblBalance = new Label { Text = "Balance: —", Location = new Point(120, y), AutoSize = true, Font = UiHelper.HeaderFont, ForeColor = FormAccent.Purchase };
            card.Controls.Add(lblBalance);
            y += 40;
            card.Controls.Add(new Label { Text = "Amount", Location = new Point(0, y), AutoSize = true });
            txtAmount = new TextBox { Location = new Point(120, y - 2), Size = new Size(160, 28) };
            UiHelper.StyleTextBox(txtAmount);
            txtAmount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtRemarks.Focus(); } };
            card.Controls.Add(txtAmount);
            y += 40;
            card.Controls.Add(new Label { Text = "Remarks", Location = new Point(0, y), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(120, y - 2), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtRemarks);
            txtRemarks.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };
            card.Controls.Add(txtRemarks);
            y += 50;
            var btn = new Button { Text = "SAVE PAYMENT (F12)", Location = new Point(120, y), Size = new Size(180, 36) };
            UiHelper.StyleAccentButton(btn, FormAccent.Purchase, FormAccent.PurchaseDark);
            btn.Click += (s, e) => Save();
            card.Controls.Add(btn);
            RefreshBal();
        }

        private void RefreshBal()
        {
            if (cmbSupplier.SelectedItem is Supplier s)
                lblBalance.Text = "Balance (payable): " + _acct.GetBalance(s.SupplierID).ToString("0.00");
        }

        private void Save()
        {
            if (!(cmbSupplier.SelectedItem is Supplier s))
            {
                DialogHelpers.Error(this, "Select a supplier.");
                return;
            }
            decimal amt = 0;
            decimal.TryParse(txtAmount.Text, out amt);
            if (amt <= 0)
            {
                DialogHelpers.Error(this, "Enter a payment amount greater than zero.");
                txtAmount.Focus();
                return;
            }
            if (!DialogHelpers.Confirm(this, "Record payment of " + amt.ToString("0.00") + " to " + s.SupplierName + "?"))
                return;
            try
            {
                _acct.RecordPayment(s.SupplierID, amt, txtRemarks.Text.Trim());
                DialogHelpers.Info(this, "Payment recorded.");
                Tag = "NOSAVECONFIRM";
                txtAmount.Clear();
                txtRemarks.Clear();
                RefreshBal();
            }
            catch (Exception ex)
            {
                AppLog.Error("Supplier payment", ex);
                DialogHelpers.Error(this, ex.Message);
            }
        }
    }
}
