using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class UserMasterForm : Form
    {
        private readonly UserRepository _repo = new UserRepository();
        private DataGridView dgv;
        private TextBox txtUser, txtFull, txtPass;
        private ComboBox cmbRole;
        private User selected;

        public UserMasterForm()
        {
            Text = "Users";
            Size = new Size(900, 560);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner("USERS",
                "Admin only  ·  Create · Change role · Reset password · Activate / Deactivate",
                FormAccent.Settings, FormAccent.SettingsDark));

            var left = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = Color.White, Padding = new Padding(16) };
            int y = 8;
            left.Controls.Add(new Label { Text = "Username *", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont }); y += 18;
            txtUser = new TextBox { Location = new Point(0, y), Size = new Size(250, 26) }; UiHelper.StyleTextBox(txtUser); left.Controls.Add(txtUser); y += 36;
            left.Controls.Add(new Label { Text = "Full name", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont }); y += 18;
            txtFull = new TextBox { Location = new Point(0, y), Size = new Size(250, 26) }; UiHelper.StyleTextBox(txtFull); left.Controls.Add(txtFull); y += 36;
            left.Controls.Add(new Label { Text = "Role", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont }); y += 18;
            cmbRole = new ComboBox { Location = new Point(0, y), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbRole);
            cmbRole.Items.AddRange(new object[] { "Admin", "Manager", "Cashier" });
            cmbRole.SelectedIndex = 2;
            left.Controls.Add(cmbRole); y += 36;
            left.Controls.Add(new Label { Text = "Password (new / reset)", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont }); y += 18;
            txtPass = new TextBox { Location = new Point(0, y), Size = new Size(250, 26), UseSystemPasswordChar = true }; UiHelper.StyleTextBox(txtPass); left.Controls.Add(txtPass); y += 44;

            var btnAdd = new Button { Text = "CREATE USER", Location = new Point(0, y), Size = new Size(140, 34) };
            UiHelper.StyleAccentButton(btnAdd, FormAccent.Settings, FormAccent.SettingsDark);
            btnAdd.Click += (s, e) => CreateUser();
            left.Controls.Add(btnAdd); y += 42;

            var btnRole = new Button { Text = "SET ROLE", Location = new Point(0, y), Size = new Size(120, 30) };
            UiHelper.StyleAccentButton(btnRole, FormAccent.SettingsDark, FormAccent.Settings);
            btnRole.Click += (s, e) => SetRole();
            left.Controls.Add(btnRole);
            var btnPwd = new Button { Text = "RESET PWD", Location = new Point(130, y), Size = new Size(120, 30) };
            UiHelper.StyleAccentButton(btnPwd, FormAccent.SettingsDark, FormAccent.Settings);
            btnPwd.Click += (s, e) => ResetPwd();
            left.Controls.Add(btnPwd); y += 40;

            var btnOff = new Button { Text = "DEACTIVATE", Location = new Point(0, y), Size = new Size(120, 30) };
            UiHelper.StyleAccentButton(btnOff, FormAccent.LowStock, FormAccent.LowStockDark);
            btnOff.Click += (s, e) => SetActive(false);
            left.Controls.Add(btnOff);
            var btnOn = new Button { Text = "ACTIVATE", Location = new Point(130, y), Size = new Size(120, 30) };
            UiHelper.StyleAccentButton(btnOn, FormAccent.Purchase, FormAccent.PurchaseDark);
            btnOn.Click += (s, e) => SetActive(true);
            left.Controls.Add(btnOn);
            Controls.Add(left);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Settings);
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                selected = dgv.CurrentRow.DataBoundItem as User;
                if (selected == null) return;
                txtUser.Text = selected.UserName;
                txtFull.Text = selected.FullName ?? "";
                if (cmbRole.Items.Contains(selected.Role)) cmbRole.SelectedItem = selected.Role;
                txtPass.Clear();
            };
            Controls.Add(dgv);
            dgv.BringToFront();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _repo.GetAll();
            if (dgv.Columns.Contains("PasswordHash")) dgv.Columns["PasswordHash"].Visible = false;
        }

        private void CreateUser()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                {
                    DialogHelpers.Error(this, "Username and password required.");
                    return;
                }
                if (_repo.ExistsUserName(txtUser.Text.Trim()))
                {
                    DialogHelpers.Error(this, "Username already exists.");
                    return;
                }
                if (!DialogHelpers.Confirm(this, "Create user " + txtUser.Text.Trim() + "?")) return;
                _repo.Insert(new User
                {
                    UserName = txtUser.Text.Trim(),
                    FullName = string.IsNullOrWhiteSpace(txtFull.Text) ? txtUser.Text.Trim() : txtFull.Text.Trim(),
                    Role = cmbRole.SelectedItem != null ? cmbRole.SelectedItem.ToString() : "Cashier"
                }, txtPass.Text);
                DialogHelpers.Info(this, "User created.");
                txtPass.Clear();
                RefreshGrid();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }

        private void SetRole()
        {
            if (selected == null) { DialogHelpers.Error(this, "Select a user."); return; }
            string role = cmbRole.SelectedItem != null ? cmbRole.SelectedItem.ToString() : "Cashier";
            if (!DialogHelpers.Confirm(this, "Set role of " + selected.UserName + " to " + role + "?")) return;
            _repo.SetRole(selected.UserID, role);
            DialogHelpers.Info(this, "Role updated.");
            RefreshGrid();
        }

        private void ResetPwd()
        {
            if (selected == null) { DialogHelpers.Error(this, "Select a user."); return; }
            if (string.IsNullOrWhiteSpace(txtPass.Text))
            {
                DialogHelpers.Error(this, "Enter new password.");
                return;
            }
            if (!DialogHelpers.Confirm(this, "Reset password for " + selected.UserName + "?")) return;
            _repo.ChangePassword(selected.UserID, txtPass.Text);
            DialogHelpers.Info(this, "Password reset.");
            txtPass.Clear();
        }

        private void SetActive(bool active)
        {
            if (selected == null) { DialogHelpers.Error(this, "Select a user."); return; }
            if (selected.UserID == AppSession.UserId && !active)
            {
                DialogHelpers.Error(this, "You cannot deactivate your own account.");
                return;
            }
            if (!DialogHelpers.Confirm(this, (active ? "Activate " : "Deactivate ") + selected.UserName + "?")) return;
            _repo.SetActive(selected.UserID, active);
            DialogHelpers.Info(this, active ? "Activated." : "Deactivated.");
            RefreshGrid();
        }
    }
}
