using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    /// <summary>Reusable UI dialogs — standard confirm / password / input.</summary>
    public static class DialogHelpers
    {
        public static bool Confirm(IWin32Window owner, string message, string title = "Confirm")
        {
            return MessageBox.Show(owner, message, title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        public static void Error(IWin32Window owner, string message, string title = "Error")
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Info(IWin32Window owner, string message, string title = "Information")
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static string Prompt(IWin32Window owner, string label, string title, string defaultValue = "")
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.Size = new Size(420, 160);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                var lbl = new Label { Text = label, Location = new Point(16, 16), AutoSize = true };
                var txt = new TextBox { Location = new Point(16, 44), Size = new Size(370, 28), Text = defaultValue };
                UiHelper.StyleTextBox(txt);
                var ok = new Button { Text = "OK", Location = new Point(16, 88), Size = new Size(100, 30), DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Location = new Point(130, 88), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };
                UiHelper.StyleButton(ok);
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(owner) != DialogResult.OK) return null;
                return txt.Text;
            }
        }

        public static string PromptPassword(IWin32Window owner, string label, string title)
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.Size = new Size(360, 160);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                var lbl = new Label { Text = label, Location = new Point(16, 16), AutoSize = true };
                var txt = new TextBox { Location = new Point(16, 44), Size = new Size(310, 28), PasswordChar = '*' };
                UiHelper.StyleTextBox(txt);
                var ok = new Button { Text = "OK", Location = new Point(16, 88), Size = new Size(100, 30), DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Location = new Point(130, 88), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };
                UiHelper.StyleButton(ok);
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(owner) != DialogResult.OK) return null;
                return txt.Text;
            }
        }
    }
}
