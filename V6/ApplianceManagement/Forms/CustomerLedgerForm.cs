using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class CustomerLedgerForm : Form
    {
        private ComboBox cmb;
        private DateTimePicker dtFrom, dtTo;
        private DataGridView dgv;
        private Label lblBal;
        private readonly CustomerRepository _cust = new CustomerRepository();
        private readonly CustomerAccountService _acct = new CustomerAccountService();

        public CustomerLedgerForm()
        {
            Text = "Customer Ledger";
            Size = new Size(900, 560);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);

            Controls.Add(UiHelper.CreateFormBanner("CUSTOMER LEDGER", "Statement from CustomerLedger table",
                FormAccent.Sale, FormAccent.SaleDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(12) };
            cmb = new ComboBox { Location = new Point(12, 14), Size = new Size(220, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.DataSource = _cust.GetAllActive();
            cmb.DisplayMember = "CustomerName";
            UiHelper.StyleComboBox(cmb);
            top.Controls.Add(cmb);
            dtFrom = new DateTimePicker { Location = new Point(250, 14), Size = new Size(120, 28), Value = DateTime.Today.AddMonths(-1) };
            dtTo = new DateTimePicker { Location = new Point(380, 14), Size = new Size(120, 28), Value = DateTime.Today };
            UiHelper.StyleDatePicker(dtFrom); UiHelper.StyleDatePicker(dtTo);
            top.Controls.Add(dtFrom); top.Controls.Add(dtTo);
            var btn = new Button { Text = "Refresh", Location = new Point(520, 12), Size = new Size(100, 32) };
            UiHelper.StyleAccentButton(btn, FormAccent.Sale, FormAccent.SaleDark);
            btn.Click += (s, e) => LoadData();
            top.Controls.Add(btn);
            lblBal = new Label { Text = "Balance: —", Location = new Point(640, 18), AutoSize = true, Font = UiHelper.HeaderFont, ForeColor = FormAccent.Sale };
            top.Controls.Add(lblBal);
            Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Sale);
            Controls.Add(dgv);
            dgv.BringToFront();
            LoadData();
        }

        private void LoadData()
        {
            if (!(cmb.SelectedItem is Customer c)) return;
            var rows = _acct.GetLedger(c.CustomerID, dtFrom.Value, dtTo.Value);
            dgv.DataSource = rows;
            lblBal.Text = "Balance: " + _acct.GetBalance(c.CustomerID).ToString("0.00");
        }
    }
}
