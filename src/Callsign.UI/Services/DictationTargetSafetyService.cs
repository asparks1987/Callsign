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
        "bank",
        "credential",
        "credentials",
        "authentication"
    ];

    private static readonly string[] ExternalSubmissionTerms =
    [
        "compose",
        "new message",
        "reply",
        "inbox",
        "email",
        "mail",
        "chat",
        "message",
        "messenger",
        "teams",
        "slack",
        "discord",
        "post",
        "comment",
        "tweet",
        "publish",
        "upload",
        "form",
        "submit",
        "checkout",
        "order",
        "purchase"
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

    private static readonly string[] ExternalSubmissionProcesses =
    [
        "outlook",
        "olk",
        "hxoutlook",
        "mail",
        "teams",
        "msteams",
        "slack",
        "discord",
        "zoom",
        "webex",
        "telegram",
        "signal",
        "whatsapp",
        "thunderbird"
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

        foreach (var process in ExternalSubmissionProcesses)
        {
            if (processName.Contains(process, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Foreground app looks like an external communication target because its process is '{processName}'.";
                return true;
            }
        }

        foreach (var term in ExternalSubmissionTerms)
        {
            if (title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Foreground window looks like an external communication or submission target because its title contains '{term}'.";
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
