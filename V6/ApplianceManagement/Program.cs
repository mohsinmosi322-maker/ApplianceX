using System;
using System.Threading;
using System.Windows.Forms;
using ApplianceManagement.Forms;
using ApplianceManagement.Helpers;

namespace ApplianceManagement
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                AppLog.Info("Application start");
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                AppLog.Error("Fatal startup error", ex);
                MessageBox.Show("A fatal error occurred:\n\n" + ex.Message, "ApplianceX",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                AppLog.Info("Application exit");
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLog.Error("UI thread exception", e.Exception);
            try
            {
                MessageBox.Show(
                    "An unexpected error occurred.\n\n" + e.Exception.Message +
                    "\n\nDetails were written to the logs folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            AppLog.Error("Unhandled domain exception", ex);
        }
    }
}
