using System.Windows.Forms;
using System.Threading;

namespace Callsign.UI.Services;

public sealed class StartMenuLauncher
{
    public bool Launch(string appName, out string message)
    {
        var target = appName.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            message = "Enter an app name first.";
            return false;
        }

        try
        {
            SendKeys.SendWait("^{ESC}");
            Thread.Sleep(250);
            SendKeys.SendWait(target);
            Thread.Sleep(150);
            SendKeys.SendWait("{ENTER}");
            message = $"Opened Start menu search for '{target}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Unable to open Start menu search: {ex.Message}";
            return false;
        }
    }
}
