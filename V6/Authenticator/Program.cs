using System;
using System.Windows.Forms;
using Authenticator.Forms;

namespace Authenticator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Authenticator cannot open without login
            using (var login = new AuthLoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK || !login.LoginSuccess)
                    return;
            }

            Application.Run(new AuthMainForm());
        }
    }
}
