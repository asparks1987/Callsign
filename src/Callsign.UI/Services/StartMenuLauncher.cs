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

        if (!ValidateAppName(target, out var safetyMessage))
        {
            message = safetyMessage;
            return false;
        }

        try
        {
            SendKeys.SendWait("^{ESC}");
            Thread.Sleep(250);
            SendKeys.SendWait(EscapeSendKeysText(target));
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

    private static string EscapeSendKeysText(string value)
    {
        return string.Concat(value.Select(character => character switch
        {
            '{' => "{{}",
            '}' => "{}}",
            '+' => "{+}",
            '^' => "{^}",
            '%' => "{%}",
            '~' => "{~}",
            '(' => "{(}",
            ')' => "{)}",
            '[' => "{[}",
            ']' => "{]}",
            _ => character.ToString()
        }));
    }

    public static bool ValidateAppName(string value, out string message)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 80)
        {
            message = "App name is too long for the alpha Start menu launcher.";
            return false;
        }

        if (normalized.Contains('\\') || normalized.Contains('/') || normalized.Contains(':'))
        {
            message = "Alpha launch only accepts installed app names, not paths or URLs. Try Notepad or Calculator.";
            return false;
        }

        if (normalized.IndexOfAny(['&', '|', '>', '<', '`', '"']) >= 0)
        {
            message = "Alpha launch only accepts plain installed app names, not shell-style command text. Try Notepad or Calculator.";
            return false;
        }

        if (normalized.StartsWith("http")
            || normalized.EndsWith(".exe")
            || normalized.Contains("powershell")
            || normalized.Contains("command line")
            || normalized.Contains("command shell")
            || normalized is "cmd"
                or "command"
                or "command prompt"
                or "terminal"
                or "windows terminal"
                or "shell"
                or "bash"
                or "wsl")
        {
            message = "That request is outside the alpha free launch scope. Try a plain installed app name like Notepad or Calculator.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}
