using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class ProductManageForm : Form
    {
        private readonly ProductRepository repo = new ProductRepository();
        private DataGridView dgv;
        private TextBox txtSearch, txtName, txtSale, txtPur, txtMin, txtPack;
        private CheckBox chkActive;
        private Label lblStatus, lblHint;
        private Product selected;

        public ProductManageForm()
        {
            InitializeComponent();
            LoadGrid("");
        }

        private void InitializeComponent()
        {
            Text = "Manage Products";
            Size = new Size(1040, 640);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this);
            UiHelper.AttachEnterNavigation(this);

            // ---- RIGHT edit panel first in tree (docked last for edge) ----
            var edit = new Panel
            {
                Dock = DockStyle.Right,
                Width = 310,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            edit.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 230)))
                    e.Graphics.DrawLine(pen, 0, 0, 0, edit.Height);
            };

            int y = 4;
            edit.Controls.Add(new Label
            {
                Text = "EDIT PRODUCT",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = FormAccent.NewItemDark,
                Location = new Point(0, y),
                AutoSize = true
            });
            y += 32;

            edit.Controls.Add(Lbl("Product name", y)); y += 18;
            txtName = new TextBox { Location = new Point(0, y), Size = new Size(270, 28) };
            UiHelper.StyleTextBox(txtName);
            edit.Controls.Add(txtName); y += 38;

            edit.Controls.Add(Lbl("Sale price (pack)", y)); y += 18;
            txtSale = new TextBox { Location = new Point(0, y), Size = new Size(150, 28) };
            UiHelper.StyleTextBox(txtSale);
            edit.Controls.Add(txtSale); y += 38;

            edit.Controls.Add(Lbl("Purchase price (pack)", y)); y += 18;
            txtPur = new TextBox { Location = new Point(0, y), Size = new Size(150, 28) };
            UiHelper.StyleTextBox(txtPur);
            edit.Controls.Add(txtPur); y += 38;

            edit.Controls.Add(Lbl("Pack size", y)); y += 18;
            txtPack = new TextBox { Location = new Point(0, y), Size = new Size(100, 28) };
            UiHelper.StyleTextBox(txtPack);
            edit.Controls.Add(txtPack); y += 38;

            edit.Controls.Add(Lbl("Min stock (base units)", y)); y += 18;
            txtMin = new TextBox { Location = new Point(0, y), Size = new Size(100, 28) };
            UiHelper.StyleTextBox(txtMin);
            edit.Controls.Add(txtMin); y += 38;

            chkActive = new CheckBox
            {
                Text = "Active (sale / purchase)",
                Location = new Point(0, y),
                AutoSize = true,
                Font = UiHelper.NormalFont,
                Checked = true
            };
            edit.Controls.Add(chkActive); y += 32;

            lblStatus = new Label
            {
                Text = "Status: —",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            edit.Controls.Add(lblStatus); y += 28;

            lblHint = new Label
            {
                Text = "Select a product from the list.",
                Location = new Point(0, y),
                Size = new Size(270, 40),
                ForeColor = Color.FromArgb(120, 120, 130),
                Font = UiHelper.SmallFont
            };
            edit.Controls.Add(lblHint); y += 48;

            var btnSave = new Button { Text = "SAVE CHANGES", Location = new Point(0, y), Size = new Size(270, 36) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.NewItem, FormAccent.NewItemDark);
            btnSave.Click += (s, e) => Save();
            edit.Controls.Add(btnSave); y += 44;

            var btnDisable = new Button { Text = "DISABLE", Location = new Point(0, y), Size = new Size(130, 34) };
            UiHelper.StyleAccentButton(btnDisable, FormAccent.LowStock, FormAccent.LowStockDark);
            btnDisable.Click += (s, e) =>
            {
                if (selected == null) return;
                if (!DialogHelpers.Confirm(this, "Disable this product? It will not show in Sale/Purchase.")) return;
                repo.SetActive(selected.ProductID, false);
                DialogHelpers.Info(this, "Disabled.");
                LoadGrid(txtSearch.Text);
            };
            edit.Controls.Add(btnDisable);

            var btnReactivate = new Button { Text = "REACTIVATE", Location = new Point(140, y), Size = new Size(130, 34) };
            UiHelper.StyleAccentButton(btnReactivate, FormAccent.Purchase, FormAccent.PurchaseDark);
            btnReactivate.Click += (s, e) =>
            {
                if (selected == null) return;
                if (!DialogHelpers.Confirm(this, "Reactivate this product for sale/purchase?")) return;
                repo.SetActive(selected.ProductID, true);
                DialogHelpers.Info(this, "Reactivated.");
                LoadGrid(txtSearch.Text);
            };
            edit.Controls.Add(btnReactivate);

            // ---- GRID ----
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.NewItem);
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                DataPropertyName = "ProductName",
                HeaderText = "Product Name",
                FillWeight = 45
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductCode",
                DataPropertyName = "ProductCode",
                HeaderText = "Code",
                FillWeight = 12
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SalePrice",
                DataPropertyName = "SalePrice",
                HeaderText = "Sale (pack)",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 14
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PurchasePrice",
                DataPropertyName = "PurchasePrice",
                HeaderText = "Cost (pack)",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 14
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PackSize",
                DataPropertyName = "PackSize",
                HeaderText = "Pack",
                DefaultCellStyle = { Format = "0.####", Alignment = DataGridViewContentAlignment.MiddleCenter },
                FillWeight = 8
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CurrentStock",
                DataPropertyName = "CurrentStock",
                HeaderText = "Stock",
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                FillWeight = 8
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IsActive",
                DataPropertyName = "IsActive",
                HeaderText = "Active",
                FillWeight = 8
            });
            dgv.SelectionChanged += (s, e) => BindSelected();
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) txtName.Focus(); };

            // ---- SEARCH BAR ----
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 8)
            };
            top.Controls.Add(new Label
            {
                Text = "Search by name / code",
                Font = UiHelper.NormalFont,
                ForeColor = Color.FromArgb(80, 80, 90),
                Location = new Point(8, 14),
                AutoSize = true
            });
            txtSearch = new TextBox { Location = new Point(170, 10), Size = new Size(360, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid(txtSearch.Text);
            top.Controls.Add(txtSearch);
            top.Controls.Add(new Label
            {
                Text = "Type product name to filter · click row to edit",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(140, 140, 150),
                Location = new Point(540, 16),
                AutoSize = true
            });

            // ---- BANNER ----
            var banner = UiHelper.CreateFormBanner(
                "PRODUCTS",
                "Search by name · Edit rates / pack · Disable / Reactivate  ·  Prices are PACK prices",
                FormAccent.NewItem, FormAccent.NewItemDark);

            // Dock order: Fill → Right → Top (search) → Top (banner last = outer)
            SuspendLayout();
            Controls.Add(dgv);
            Controls.Add(edit);
            Controls.Add(top);
            Controls.Add(banner);
            ResumeLayout(true);

            UiHelper.EnableAutoSelectOnFocus(this);
        }

        private static Label Lbl(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(0, y),
                AutoSize = true,
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(90, 90, 100)
            };
        }

        private void BindSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.DataBoundItem == null)
            {
                selected = null;
                return;
            }
            selected = dgv.CurrentRow.DataBoundItem as Product;
            if (selected == null) return;

            txtName.Text = selected.ProductName ?? "";
            txtSale.Text = selected.SalePrice.ToString("0.00");
            txtPur.Text = selected.PurchasePrice.ToString("0.00");
            txtMin.Text = selected.MinimumStock.ToString();
            txtPack.Text = selected.PackSize > 0 ? selected.PackSize.ToString("0.####") : "1";
            chkActive.Checked = selected.IsActive;
            lblStatus.Text = selected.IsActive ? "Status: ACTIVE" : "Status: DISABLED";
            lblStatus.ForeColor = selected.IsActive ? Color.FromArgb(46, 125, 50) : Color.FromArgb(198, 40, 40);
            lblHint.Text = "Code: " + selected.ProductCode +
                           (string.IsNullOrEmpty(selected.UnitOfMeasure) ? "" : "  ·  UOM: " + selected.UnitOfMeasure);
        }

        private void LoadGrid(string kw)
        {
            try
            {
                var list = string.IsNullOrWhiteSpace(kw)
                    ? repo.GetAllForManage()
                    : repo.SearchAll(kw.Trim());

                // Prefer name order (repo already ORDERS BY ProductName)
                int keepId = selected != null ? selected.ProductID : 0;
                dgv.DataSource = null;
                dgv.DataSource = list;

                if (list.Count == 0)
                {
                    selected = null;
                    lblHint.Text = "No products match this search.";
                    return;
                }

                // Restore selection if possible
                if (keepId > 0)
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        var p = row.DataBoundItem as Product;
                        if (p != null && p.ProductID == keepId)
                        {
                            row.Selected = true;
                            dgv.CurrentCell = row.Cells[0];
                            break;
                        }
                    }
                }
                else if (dgv.Rows.Count > 0)
                {
                    dgv.Rows[0].Selected = true;
                    dgv.CurrentCell = dgv.Rows[0].Cells[0];
                }
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, "Could not load products: " + ex.Message);
            }
        }

        private void Save()
        {
            if (selected == null)
            {
                DialogHelpers.Warn(this, "Select a product from the list first.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DialogHelpers.Warn(this, "Product name is required.");
                txtName.Focus();
                return;
            }

            decimal sale, pur, pack;
            int min;
            if (!decimal.TryParse(txtSale.Text, out sale) || sale < 0)
            {
                DialogHelpers.Warn(this, "Invalid sale price.");
                txtSale.Focus();
                return;
            }
            if (!decimal.TryParse(txtPur.Text, out pur) || pur < 0)
            {
                DialogHelpers.Warn(this, "Invalid purchase price.");
                txtPur.Focus();
                return;
            }
            if (!decimal.TryParse(txtPack.Text, out pack) || pack <= 0) pack = 1;
            if (!int.TryParse(txtMin.Text, out min) || min < 0) min = 0;

            try
            {
                repo.UpdateFull(
                    selected.ProductID,
                    txtName.Text.Trim(),
                    pur,
                    sale,
                    min,
                    chkActive.Checked,
                    selected.UnitOfMeasure,
                    pack);

                DialogHelpers.Info(this,
                    "Saved.\nUnit sale: " + Math.Round(sale / pack, 4).ToString("0.####") +
                    "\nUnit cost: " + Math.Round(pur / pack, 4).ToString("0.####"));
                LoadGrid(txtSearch.Text);
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, "Save failed: " + ex.Message);
            }
        }
    }
}
