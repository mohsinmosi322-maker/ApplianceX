using System;
using System.Windows.Forms;
using ApplianceManagement.Forms;

namespace ApplianceManagement
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Theme applied after settings available on login form
            Application.Run(new LoginForm());
        }
    }
}
