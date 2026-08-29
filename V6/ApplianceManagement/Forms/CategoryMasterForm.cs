using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class CategoryMasterForm : Form
    {
        private readonly CategoryRepository _repo = new CategoryRepository();
        private DataGridView dgv;
        private TextBox txtName;

        public CategoryMasterForm()
        {
            Text = "Categories";
            Size = new Size(520, 480);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);

            Controls.Add(UiHelper.CreateFormBanner("CATEGORIES", "Product categories for New Item",
                FormAccent.NewItem, FormAccent.NewItemDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White, Padding = new Padding(12) };
            top.Controls.Add(new Label { Text = "Name", Location = new Point(12, 20), AutoSize = true, Font = UiHelper.NormalFont });
            txtName = new TextBox { Location = new Point(70, 16), Size = new Size(240, 28) };
            UiHelper.StyleTextBox(txtName);
            txtName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };
            top.Controls.Add(txtName);
            var btn = new Button { Text = "ADD", Location = new Point(330, 14), Size = new Size(100, 34) };
            UiHelper.StyleAccentButton(btn, FormAccent.NewItem, FormAccent.NewItemDark);
            btn.Click += (s, e) => Save();
            top.Controls.Add(btn);
            Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.NewItem);
            Controls.Add(dgv);
            dgv.BringToFront();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _repo.GetAllActive();
            if (dgv.Columns.Contains("IsActive")) dgv.Columns["IsActive"].Visible = false;
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
                if (!DialogHelpers.Confirm(this, "Add category \"" + txtName.Text.Trim() + "\"?"))
                    return;
                _repo.Insert(txtName.Text.Trim());
                DialogHelpers.Info(this, "Category saved.");
                Tag = "NOSAVECONFIRM";
                txtName.Clear();
                RefreshGrid();
                txtName.Focus();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }
    }
}
