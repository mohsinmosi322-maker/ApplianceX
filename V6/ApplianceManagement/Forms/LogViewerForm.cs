using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    /// <summary>Read-only view of daily AppLog files under /logs.</summary>
    public partial class LogViewerForm : Form
    {
        private ComboBox cmbFiles;
        private TextBox txtLog;
        private readonly string _logDir;

        public LogViewerForm()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Text = "Application Logs";
            Size = new Size(900, 560);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);

            Controls.Add(UiHelper.CreateFormBanner(
                "LOGS",
                "Diagnostics from AppLog  ·  F4 close",
                FormAccent.Settings, FormAccent.SettingsDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "File", Location = new Point(12, 14), AutoSize = true, Font = UiHelper.NormalFont });
            cmbFiles = new ComboBox
            {
                Location = new Point(50, 10),
                Size = new Size(280, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            UiHelper.StyleComboBox(cmbFiles);
            cmbFiles.SelectedIndexChanged += (s, e) => LoadSelected();
            top.Controls.Add(cmbFiles);
            var btnRefresh = new Button { Text = "Refresh", Location = new Point(350, 8), Size = new Size(100, 32) };
            UiHelper.StyleAccentButton(btnRefresh, FormAccent.Settings, FormAccent.SettingsDark);
            btnRefresh.Click += (s, e) => LoadFileList();
            top.Controls.Add(btnRefresh);
            var btnOpenFolder = new Button { Text = "Open folder", Location = new Point(460, 8), Size = new Size(110, 32) };
            UiHelper.StyleAccentButton(btnOpenFolder, FormAccent.SettingsDark, FormAccent.Settings);
            btnOpenFolder.Click += (s, e) =>
            {
                try
                {
                    if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
                    System.Diagnostics.Process.Start("explorer.exe", _logDir);
                }
                catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
            };
            top.Controls.Add(btnOpenFolder);
            Controls.Add(top);

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                WordWrap = false
            };
            Controls.Add(txtLog);
            txtLog.BringToFront();
            LoadFileList();
        }

        private void LoadFileList()
        {
            cmbFiles.Items.Clear();
            if (!Directory.Exists(_logDir))
            {
                txtLog.Text = "No logs folder yet. Errors and login events will appear here after use.";
                return;
            }
            var files = Directory.GetFiles(_logDir, "app-*.log");
            Array.Sort(files);
            Array.Reverse(files);
            foreach (var f in files)
                cmbFiles.Items.Add(Path.GetFileName(f));
            if (cmbFiles.Items.Count > 0)
                cmbFiles.SelectedIndex = 0;
            else
                txtLog.Text = "No log files found.";
        }

        private void LoadSelected()
        {
            if (cmbFiles.SelectedItem == null) return;
            string path = Path.Combine(_logDir, cmbFiles.SelectedItem.ToString());
            try
            {
                // share for concurrent writers
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                    txtLog.Text = sr.ReadToEnd();
                if (txtLog.TextLength > 0)
                {
                    txtLog.SelectionStart = txtLog.TextLength;
                    txtLog.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                txtLog.Text = "Could not read log: " + ex.Message;
            }
        }
    }
}
