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
        private Button btnSave, btnDeactivate;
        private int _editId = 0;

        public SupplierMasterForm()
        {
            Text = "Suppliers";
            Size = new Size(920, 580);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner("SUPPLIERS",
                "CRUD: Add · Double-click Edit · Deactivate  ·  Opening balance seeds payable",
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
            left.Controls.Add(new Label { Text = "Opening payable (new only)", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 20;
            txtOpening = new TextBox { Location = new Point(0, y), Size = new Size(140, 28), Text = "0" };
            UiHelper.StyleTextBox(txtOpening);
            left.Controls.Add(txtOpening);
            y += 48;
            btnSave = new Button { Text = "ADD SUPPLIER", Location = new Point(0, y), Size = new Size(160, 36) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.Purchase, FormAccent.PurchaseDark);
            btnSave.Click += (s, e) => Save();
            left.Controls.Add(btnSave);
            y += 44;
            var btnClear = new Button { Text = "NEW / CLEAR", Location = new Point(0, y), Size = new Size(160, 32) };
            UiHelper.StyleAccentButton(btnClear, FormAccent.PurchaseDark, FormAccent.Purchase);
            btnClear.Click += (s, e) => ClearEdit();
            left.Controls.Add(btnClear);
            y += 40;
            btnDeactivate = new Button { Text = "DEACTIVATE", Location = new Point(0, y), Size = new Size(160, 32), Enabled = false };
            UiHelper.StyleAccentButton(btnDeactivate, FormAccent.LowStock, FormAccent.LowStockDark);
            btnDeactivate.Click += (s, e) => SoftDeactivate();
            left.Controls.Add(btnDeactivate);
            Controls.Add(left);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Purchase);
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) LoadRow(e.RowIndex); };
            Controls.Add(dgv);
            dgv.BringToFront();
            RefreshGrid();
        }

        private void ClearEdit()
        {
            _editId = 0;
            txtName.Clear(); txtPhone.Clear(); txtAddress.Clear(); txtOpening.Text = "0";
            txtOpening.Enabled = true;
            btnSave.Text = "ADD SUPPLIER";
            btnDeactivate.Enabled = false;
            txtName.Focus();
        }

        private void LoadRow(int rowIndex)
        {
            if (dgv.Rows[rowIndex].DataBoundItem is Supplier s)
            {
                _editId = s.SupplierID;
                txtName.Text = s.SupplierName ?? "";
                txtPhone.Text = s.Phone ?? "";
                txtAddress.Text = s.Address ?? "";
                txtOpening.Text = s.OpeningBalance.ToString("0.##");
                txtOpening.Enabled = false;
                btnSave.Text = "UPDATE SUPPLIER";
                btnDeactivate.Enabled = true;
                txtName.Focus();
            }
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _repo.GetAllActive();
            if (dgv.Columns.Contains("SupplierID")) dgv.Columns["SupplierID"].Visible = false;
            if (dgv.Columns.Contains("IsActive")) dgv.Columns["IsActive"].Visible = false;
        }

        private void SoftDeactivate()
        {
            if (_editId <= 0) return;
            if (!DialogHelpers.Confirm(this, "Deactivate this supplier?\nThey will no longer appear in purchase lists."))
                return;
            try
            {
                _repo.SetActive(_editId, false);
                DialogHelpers.Info(this, "Supplier deactivated.");
                Tag = "NOSAVECONFIRM";
                ClearEdit();
                RefreshGrid();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
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

                if (_editId > 0)
                {
                    if (!DialogHelpers.Confirm(this, "Update supplier " + txtName.Text.Trim() + "?")) return;
                    _repo.Update(new Supplier
                    {
                        SupplierID = _editId,
                        SupplierName = txtName.Text.Trim(),
                        Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                        Address = string.IsNullOrWhiteSpace(txtAddress.Text) ? null : txtAddress.Text.Trim()
                    });
                    DialogHelpers.Info(this, "Supplier updated.");
                    Tag = "NOSAVECONFIRM";
                    ClearEdit();
                    RefreshGrid();
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
                ClearEdit();
                RefreshGrid();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }
    }
}
