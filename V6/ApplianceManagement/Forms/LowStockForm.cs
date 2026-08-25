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
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            this.Padding = new Padding(12);
            UiHelper.AttachF4Close(this);

            var list = new ProductRepository().GetLowStock();

            Panel top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
            top.Controls.Add(new Label
            {
                Text = "Items at or below minimum stock:  " + list.Count,
                Font = UiHelper.HeaderFont,
                ForeColor = UiHelper.DangerColor,
                AutoSize = true,
                Location = new Point(16, 16)
            });
            this.Controls.Add(top);

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);
            dgv.BringToFront();
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate", "PurchasePrice" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
        }
    }
}
