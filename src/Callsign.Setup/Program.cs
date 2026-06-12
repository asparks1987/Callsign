using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installDir = Path.Combine(localAppData, "Callsign", "App");
            var installedExe = Path.Combine(installDir, "Callsign.UI.exe");
            var fzfExe = Path.Combine(installDir, "fzf.exe");

            Directory.CreateDirectory(installDir);
            ExtractResourceOrThrow("Callsign.UI.exe", installedExe);
            TryExtractResource("fzf.exe", fzfExe);

            CreateShortcut(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft",
                    "Windows",
                    "Start Menu",
                    "Programs",
                    "Callsign",
                    "Callsign.lnk"),
                installedExe,
                installDir);

            CreateShortcut(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Callsign.lnk"),
                installedExe,
                installDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = installedExe,
                WorkingDirectory = installDir,
                UseShellExecute = true
            });

            return 0;
        }
        catch (Exception ex)
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Callsign",
                "Logs");
            Directory.CreateDirectory(logDir);
            File.WriteAllText(Path.Combine(logDir, "installer-error.log"), ex.ToString());
            MessageBox.Show(
                $"Callsign could not be installed.\n\nDetails saved to:\n{Path.Combine(logDir, "installer-error.log")}",
                "Callsign Installer Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void ExtractResourceOrThrow(string resourceName, string targetPath)
    {
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var output = File.Create(targetPath);
        payload.CopyTo(output);
    }

    private static void TryExtractResource(string resourceName, string targetPath)
    {
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (payload == null)
            return;

        using var output = File.Create(targetPath);
        payload.CopyTo(output);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)
            ?? throw new InvalidOperationException("Shortcut directory could not be resolved."));

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not be started.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = "Launch Callsign";
        shortcut.Save();
    }
}
