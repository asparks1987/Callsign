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
            ClearStartupErrorLog();
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

    private static void ClearStartupErrorLog()
    {
        try
        {
            var logFile = GetStartupErrorLogPath();
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
        catch
        {
            // no-op: stale startup diagnostics should not block a normal launch
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        try
        {
            var logFile = GetStartupErrorLogPath();
            var logDir = Path.GetDirectoryName(logFile)
                ?? throw new InvalidOperationException("Startup log directory could not be resolved.");
            Directory.CreateDirectory(logDir);
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

    private static string GetStartupErrorLogPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Logs",
            "startup-error.log");
}
