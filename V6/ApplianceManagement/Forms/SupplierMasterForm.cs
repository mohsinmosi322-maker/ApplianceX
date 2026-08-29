using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SupplierMasterForm : Form
    {
        private readonly SupplierRepository _repo = new SupplierRepository();
        private DataGridView dgv;
        private TextBox txtName, txtPhone, txtAddress, txtOpening;

        public SupplierMasterForm()
        {
            Text = "Suppliers";
            Size = new Size(900, 560);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);

            Controls.Add(UiHelper.CreateFormBanner("SUPPLIERS", "Master list  ·  Opening balance seeds payable",
                FormAccent.Purchase, FormAccent.PurchaseDark));

            var left = new Panel { Dock = DockStyle.Left, Width = 320, BackColor = Color.White, Padding = new Padding(16) };
            int y = 12;
            left.Controls.Add(new Label { Text = "Name *", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 20;
            txtName = new TextBox { Location = new Point(0, y), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtName);
            left.Controls.Add(txtName);
            y += 40;
            left.Controls.Add(new Label { Text = "Phone", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 20;
            txtPhone = new TextBox { Location = new Point(0, y), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtPhone);
            left.Controls.Add(txtPhone);
            y += 40;
            left.Controls.Add(new Label { Text = "Address", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 20;
            txtAddress = new TextBox { Location = new Point(0, y), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtAddress);
            left.Controls.Add(txtAddress);
            y += 40;
            left.Controls.Add(new Label { Text = "Opening payable", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 20;
            txtOpening = new TextBox { Location = new Point(0, y), Size = new Size(140, 28), Text = "0" };
            UiHelper.StyleTextBox(txtOpening);
            left.Controls.Add(txtOpening);
            y += 48;
            var btn = new Button { Text = "ADD SUPPLIER", Location = new Point(0, y), Size = new Size(160, 36) };
            UiHelper.StyleAccentButton(btn, FormAccent.Purchase, FormAccent.PurchaseDark);
            btn.Click += (s, e) => Save();
            left.Controls.Add(btn);
            Controls.Add(left);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Purchase);
            Controls.Add(dgv);
            dgv.BringToFront();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _repo.GetAllActive();
            if (dgv.Columns.Contains("SupplierID")) dgv.Columns["SupplierID"].Visible = false;
            if (dgv.Columns.Contains("IsActive")) dgv.Columns["IsActive"].Visible = false;
        }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DialogHelpers.Error(this, "Supplier name is required.");
                    return;
                }
                decimal open = 0;
                decimal.TryParse(txtOpening.Text, out open);
                if (!DialogHelpers.Confirm(this, "Add supplier " + txtName.Text.Trim() + "?")) return;

                int id = _repo.Insert(new Supplier
                {
                    SupplierName = txtName.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                    Address = string.IsNullOrWhiteSpace(txtAddress.Text) ? null : txtAddress.Text.Trim(),
                    OpeningBalance = open
                });

                if (open != 0)
                {
                    try
                    {
                        using (var conn = DbHelper.GetConnection())
                        {
                            conn.Open();
                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO SupplierLedger(SupplierID,EntryDate,EntryType,Debit,Credit,Remarks,CreatedBy) " +
                                "VALUES(@S,GETDATE(),'OPENING',0,@Cr,'Opening payable',@By)", conn))
                            {
                                cmd.Parameters.AddWithValue("@S", id);
                                cmd.Parameters.AddWithValue("@Cr", open > 0 ? open : 0m);
                                cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch { }
                }

                DialogHelpers.Info(this, "Supplier saved.");
                Tag = "NOSAVECONFIRM";
                txtName.Clear(); txtPhone.Clear(); txtAddress.Clear(); txtOpening.Text = "0";
                RefreshGrid();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }
    }
}
