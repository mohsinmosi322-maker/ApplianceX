using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class CustomerPaymentForm : Form
    {
        private ComboBox cmbCustomer;
        private TextBox txtAmount, txtRemarks;
        private Label lblBalance;
        private readonly CustomerRepository _cust = new CustomerRepository();
        private readonly CustomerAccountService _acct = new CustomerAccountService();

        public CustomerPaymentForm()
        {
            Text = "Customer Payment";
            Size = new Size(520, 360);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this);
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) { Save(); e.Handled = true; } };

            Controls.Add(UiHelper.CreateFormBanner("CUSTOMER PAYMENT", "Record payment against receivable · F12 save",
                FormAccent.Sale, FormAccent.SaleDark));

            var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = Color.White };
            Controls.Add(card);

            int y = 16;
            card.Controls.Add(new Label { Text = "Customer", Location = new Point(0, y), AutoSize = true });
            cmbCustomer = new ComboBox { Location = new Point(120, y - 2), Size = new Size(320, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCustomer.DataSource = _cust.GetAllActive();
            cmbCustomer.DisplayMember = "CustomerName";
            cmbCustomer.SelectedIndexChanged += (s, e) => RefreshBal();
            UiHelper.StyleComboBox(cmbCustomer);
            card.Controls.Add(cmbCustomer);
            y += 40;
            lblBalance = new Label { Text = "Balance: —", Location = new Point(120, y), AutoSize = true, Font = UiHelper.HeaderFont, ForeColor = FormAccent.Sale };
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
            UiHelper.StyleAccentButton(btn, FormAccent.Sale, FormAccent.SaleDark);
            btn.Click += (s, e) => Save();
            card.Controls.Add(btn);
            RefreshBal();
        }

        private void RefreshBal()
        {
            if (cmbCustomer.SelectedItem is Customer c)
                lblBalance.Text = "Balance (receivable): " + _acct.GetBalance(c.CustomerID).ToString("0.00");
        }

        private void Save()
        {
            if (!(cmbCustomer.SelectedItem is Customer c))
            {
                DialogHelpers.Error(this, "Select a customer.");
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
            if (!DialogHelpers.Confirm(this, "Record payment of " + amt.ToString("0.00") + " for " + c.CustomerName + "?"))
                return;
            try
            {
                _acct.RecordPayment(c.CustomerID, amt, txtRemarks.Text.Trim());
                DialogHelpers.Info(this, "Payment recorded.");
                Tag = "NOSAVECONFIRM";
                txtAmount.Clear();
                txtRemarks.Clear();
                RefreshBal();
            }
            catch (Exception ex)
            {
                AppLog.Error("Customer payment", ex);
                DialogHelpers.Error(this, ex.Message);
            }
        }
    }
}
