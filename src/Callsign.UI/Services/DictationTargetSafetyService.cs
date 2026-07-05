using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Callsign.UI.Services;

public sealed record DictationTargetInfo(string WindowTitle, string ProcessName);

public static class DictationTargetSafetyService
{
    private static readonly string[] SensitiveTerms =
    [
        "password",
        "passcode",
        "pin",
        "2fa",
        "two factor",
        "verification code",
        "one-time code",
        "otp",
        "security code",
        "secret",
        "private key",
        "seed phrase",
        "recovery phrase",
        "wallet",
        "credit card",
        "card number",
        "cvv",
        "cvc",
        "payment",
        "bank"
    ];

    private static readonly string[] SensitiveProcesses =
    [
        "credentialui",
        "logonui",
        "1password",
        "bitwarden",
        "keepass",
        "lastpass"
    ];

    public static bool TryGetForegroundTarget(out DictationTargetInfo target)
    {
        target = new DictationTargetInfo(string.Empty, string.Empty);
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero)
            return false;

        var titleLength = GetWindowTextLength(window);
        var titleBuilder = new StringBuilder(Math.Max(256, titleLength + 1));
        _ = GetWindowText(window, titleBuilder, titleBuilder.Capacity);

        _ = GetWindowThreadProcessId(window, out var processId);
        var processName = string.Empty;
        if (processId != 0)
        {
            try
            {
                processName = Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                processName = string.Empty;
            }
        }

        target = new DictationTargetInfo(titleBuilder.ToString(), processName);
        return !string.IsNullOrWhiteSpace(target.WindowTitle) || !string.IsNullOrWhiteSpace(target.ProcessName);
    }

    public static bool IsSensitiveTarget(DictationTargetInfo target, out string reason)
    {
        var title = target.WindowTitle ?? string.Empty;
        var processName = target.ProcessName ?? string.Empty;

        foreach (var term in SensitiveTerms)
        {
            if (title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Foreground window looks sensitive because its title contains '{term}'.";
                return true;
            }
        }

        foreach (var process in SensitiveProcesses)
        {
            if (processName.Contains(process, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Foreground app looks sensitive because its process is '{processName}'.";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
