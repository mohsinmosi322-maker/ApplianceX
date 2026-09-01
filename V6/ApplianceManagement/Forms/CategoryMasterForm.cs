using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class CategoryMasterForm : Form
    {
        private readonly CategoryRepository _repo = new CategoryRepository();
        private DataGridView dgv;
        private TextBox txtName;
        private Button btnSave, btnDeactivate;
        private int _editId = 0;

        public CategoryMasterForm()
        {
            Text = "Categories";
            Size = new Size(640, 520);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner("CATEGORIES",
                "CRUD: Add · Double-click Edit · Deactivate  ·  Used by New Item",
                FormAccent.NewItem, FormAccent.NewItemDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Color.White, Padding = new Padding(12) };
            top.Controls.Add(new Label { Text = "Category name *", Location = new Point(12, 12), AutoSize = true, Font = UiHelper.SmallFont });
            txtName = new TextBox { Location = new Point(12, 34), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtName);
            txtName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };
            top.Controls.Add(txtName);

            btnSave = new Button { Text = "ADD", Location = new Point(310, 32), Size = new Size(100, 34) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.NewItem, FormAccent.NewItemDark);
            btnSave.Click += (s, e) => Save();
            top.Controls.Add(btnSave);

            var btnClear = new Button { Text = "NEW", Location = new Point(420, 32), Size = new Size(80, 34) };
            UiHelper.StyleAccentButton(btnClear, FormAccent.NewItemDark, FormAccent.NewItem);
            btnClear.Click += (s, e) => ClearEdit();
            top.Controls.Add(btnClear);

            btnDeactivate = new Button { Text = "DEACTIVATE", Location = new Point(510, 32), Size = new Size(110, 34), Enabled = false };
            UiHelper.StyleAccentButton(btnDeactivate, FormAccent.LowStock, FormAccent.LowStockDark);
            btnDeactivate.Click += (s, e) => Deactivate();
            top.Controls.Add(btnDeactivate);

            top.Controls.Add(new Label
            {
                Text = "Tip: double-click a row to edit name",
                Location = new Point(12, 72),
                AutoSize = true,
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray
            });
            Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.NewItem);
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
            txtName.Clear();
            btnSave.Text = "ADD";
            btnDeactivate.Enabled = false;
            txtName.Focus();
        }

        private void LoadRow(int rowIndex)
        {
            if (dgv.Rows[rowIndex].DataBoundItem is Category c)
            {
                _editId = c.CategoryID;
                txtName.Text = c.CategoryName ?? "";
                btnSave.Text = "UPDATE";
                btnDeactivate.Enabled = true;
                txtName.Focus();
            }
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _repo.GetAllActive();
            if (dgv.Columns.Contains("IsActive")) dgv.Columns["IsActive"].Visible = false;
        }

        private void Deactivate()
        {
            if (_editId <= 0) return;
            if (!DialogHelpers.Confirm(this, "Deactivate this category?\nExisting products keep their category id."))
                return;
            try
            {
                _repo.SetActive(_editId, false);
                DialogHelpers.Info(this, "Category deactivated.");
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
                    DialogHelpers.Error(this, "Category name required.");
                    return;
                }

                if (_editId > 0)
                {
                    if (!DialogHelpers.Confirm(this, "Update category to \"" + txtName.Text.Trim() + "\"?"))
                        return;
                    _repo.Update(_editId, txtName.Text.Trim());
                    DialogHelpers.Info(this, "Category updated.");
                    Tag = "NOSAVECONFIRM";
                    ClearEdit();
                    RefreshGrid();
                    return;
                }

                if (!DialogHelpers.Confirm(this, "Add category \"" + txtName.Text.Trim() + "\"?"))
                    return;
                _repo.Insert(txtName.Text.Trim());
                DialogHelpers.Info(this, "Category saved.");
                Tag = "NOSAVECONFIRM";
                ClearEdit();
                RefreshGrid();
                txtName.Focus();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }
    }
}
