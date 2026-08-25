using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class LowStockForm : Form
    {
        public LowStockForm()
        {
            this.Text = "Low Stock Report";
            this.Size = new Size(960, 560);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            var dgv = new DataGridView { Location = new Point(15, 15), Size = new Size(920, 470) };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);

            var list = new ProductRepository().GetLowStock();
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate", "PurchasePrice" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;

            this.Controls.Add(new Label
            {
                Text = "Items at or below minimum stock level: " + list.Count,
                Font = UiHelper.HeaderFont,
                ForeColor = UiHelper.DangerColor,
                Location = new Point(15, 500),
                Size = new Size(500, 25)
            });
        }
    }
}
