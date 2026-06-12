using System;
using System.Windows.Forms;
using System.IO;

namespace Callsign.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ShowStartupError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    ShowStartupError(ex);
            };
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Callsign",
                "Logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "startup-error.log");
            File.WriteAllText(logFile, $"{DateTime.UtcNow:O}{Environment.NewLine}{ex}");
            MessageBox.Show(
                $"Callsign could not start.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details saved to:{Environment.NewLine}{logFile}",
                "Callsign Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // no-op: avoid secondary failures while reporting startup issues
        }
    }
}
