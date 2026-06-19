using System.Windows.Forms;

namespace Callsign.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm(new InstallerWorkflow()));
    }
}
