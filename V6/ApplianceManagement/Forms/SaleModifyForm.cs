using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SaleModifyForm : Form
    {
        private SaleRepository saleRepo = new SaleRepository();
        private DataGridView dgv;
        private DateTimePicker dtFrom, dtTo;
        private TextBox txtPaid, txtRemarks;
        private SaleHeader selected;

        public SaleModifyForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Modify Sale";
            this.Size = new Size(920, 540);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            this.Controls.Add(new Label { Text = "From:", Location = new Point(15, 15), Size = new Size(45, 22), Font = UiHelper.NormalFont });
            dtFrom = new DateTimePicker { Location = new Point(65, 12), Size = new Size(120, 26), Value = DateTime.Today.AddDays(-30) };
            UiHelper.StyleDatePicker(dtFrom);
            this.Controls.Add(dtFrom);
            this.Controls.Add(new Label { Text = "To:", Location = new Point(200, 15), Size = new Size(30, 22), Font = UiHelper.NormalFont });
            dtTo = new DateTimePicker { Location = new Point(235, 12), Size = new Size(120, 26), Value = DateTime.Today };
            UiHelper.StyleDatePicker(dtTo);
            this.Controls.Add(dtTo);
            Button btnLoad = new Button { Text = "Load", Location = new Point(380, 10), Size = new Size(90, 30) };
            UiHelper.StyleButton(btnLoad);
            btnLoad.Click += (s, e) => LoadData();
            this.Controls.Add(btnLoad);

            dgv = new DataGridView { Location = new Point(15, 50), Size = new Size(880, 320) };
            UiHelper.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.DataBoundItem == null) return;
                selected = dgv.CurrentRow.DataBoundItem as SaleHeader;
                if (selected == null) return;
                txtPaid.Text = selected.PaidAmount.ToString("0.00");
                txtRemarks.Text = selected.Remarks ?? "";
            };
            this.Controls.Add(dgv);

            this.Controls.Add(new Label { Text = "Paid Amount:", Location = new Point(15, 390), Size = new Size(100, 22), Font = UiHelper.NormalFont });
            txtPaid = new TextBox { Location = new Point(120, 387), Size = new Size(120, 26) };
            UiHelper.StyleTextBox(txtPaid);
            this.Controls.Add(txtPaid);
            this.Controls.Add(new Label { Text = "Remarks:", Location = new Point(260, 390), Size = new Size(70, 22), Font = UiHelper.NormalFont });
            txtRemarks = new TextBox { Location = new Point(340, 387), Size = new Size(300, 26) };
            UiHelper.StyleTextBox(txtRemarks);
            this.Controls.Add(txtRemarks);

            Button btnSave = new Button { Text = "Update Invoice", Location = new Point(660, 385), Size = new Size(140, 32) };
            UiHelper.StyleButton(btnSave);
            btnSave.Click += (s, e) =>
            {
                if (selected == null) { MessageBox.Show("Select an invoice."); return; }
                decimal paid = 0; decimal.TryParse(txtPaid.Text, out paid);
                saleRepo.UpdateHeader(selected.SaleID, paid, selected.NetAmount - paid, txtRemarks.Text.Trim());
                MessageBox.Show("Invoice updated.");
                LoadData();
            };
            this.Controls.Add(btnSave);

            this.Controls.Add(new Label
            {
                Text = "Note: Line items / stock cannot be changed here. For wrong items, make adjustment sale/purchase separately.",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Location = new Point(15, 430),
                Size = new Size(880, 40)
            });
        }

        private void LoadData()
        {
            var list = saleRepo.GetSales(dtFrom.Value, dtTo.Value);
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "SaleID", "CustomerID", "Details" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
        }
    }
}
