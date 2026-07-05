using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Callsign.UI.Services;

public enum BrowserOpenTarget
{
    Default,
    Chrome
}

public sealed class BrowserLaunchService
{
    private const string DefaultSearchBase = "https://www.bing.com/search?q=";
    private const string BrowserFindTextActionPrefix = "browser-find-text:";
    private const string BrowserAddressTextActionPrefix = "browser-address-text:";
    private readonly bool _dryRun;
    private readonly object _scrollSync = new();
    private System.Threading.Timer? _continuousScrollTimer;
    private bool _continuousScrollActive;
    private ushort _continuousScrollKey;
    private string _continuousScrollLabel = string.Empty;

    public BrowserLaunchService(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public static string EscapeSendKeysText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var escaped = new System.Text.StringBuilder(text.Length * 2);
        foreach (var character in text)
        {
            escaped.Append(character switch
            {
                '{' => "{{}",
                '}' => "{}}",
                '+' => "{+}",
                '^' => "{^}",
                '%' => "{%}",
                '~' => "{~}",
                '(' => "{(}",
                ')' => "{)}",
                _ => character.ToString()
            });
        }

        return escaped.ToString();
    }

    public bool TryExecuteBrowserAction(string action, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(action))
        {
            message = "Browser action was empty.";
            return false;
        }

        try
        {
            var normalizedAction = action.Trim();
            if (TryParseFindTextAction(normalizedAction, out var findText))
            {
                SendKeyChord(VK_CONTROL, VK_F);
                Thread.Sleep(100);
                SendText(findText);
                message = $"Browser find text requested: {findText}";
                return true;
            }

            if (TryParseAddressTextAction(normalizedAction, out var addressText))
            {
                StopContinuousScroll();
                if (!TryBuildTargetUri(addressText, out var targetUri, out var reason))
                {
                    message = reason;
                    return false;
                }

                SendKeyChordIfNeeded(VK_CONTROL, VK_L);
                PauseInputIfNeeded(100);
                SendTextIfNeeded(targetUri!.ToString());
                SendKeyIfNeeded(VK_RETURN);
                message = $"Browser address bar target requested: {targetUri}";
                return true;
            }

            switch (normalizedAction.ToLowerInvariant())
            {
                case "browser-back":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_MENU, VK_LEFT);
                    message = "Browser back requested.";
                    return true;
                case "browser-forward":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_MENU, VK_RIGHT);
                    message = "Browser forward requested.";
                    return true;
                case "browser-refresh":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_R);
                    message = "Browser refresh requested.";
                    return true;
                case "browser-new-tab":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_T);
                    message = "Browser new tab requested.";
                    return true;
                case "browser-new-window":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_N);
                    message = "Browser new window requested.";
                    return true;
                case "browser-private-window":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_SHIFT, VK_N);
                    message = "Browser private window requested.";
                    return true;
                case "browser-bookmark-page":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_D);
                    message = "Browser bookmark page requested.";
                    return true;
                case "browser-open-bookmarks":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_SHIFT, VK_O);
                    message = "Browser bookmarks requested.";
                    return true;
                case "browser-save-page":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_S);
                    message = "Browser save page requested.";
                    return true;
                case "browser-print-page":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_P);
                    message = "Browser print page requested.";
                    return true;
                case "browser-next-tab":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_TAB);
                    message = "Browser next tab requested.";
                    return true;
                case "browser-previous-tab":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_SHIFT, VK_TAB);
                    message = "Browser previous tab requested.";
                    return true;
                case "browser-close-tab":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_W);
                    message = "Browser close tab requested.";
                    return true;
                case "browser-reopen-closed-tab":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_SHIFT, VK_T);
                    message = "Browser reopen closed tab requested.";
                    return true;
                case "browser-focus-address-bar":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_L);
                    message = "Browser address bar requested.";
                    return true;
                case "browser-home":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_MENU, VK_HOME);
                    message = "Browser home page requested.";
                    return true;
                case "browser-fullscreen":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_F11);
                    message = "Browser full screen requested.";
                    return true;
                case "browser-open-downloads":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_J);
                    message = "Browser downloads requested.";
                    return true;
                case "browser-open-history":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_H);
                    message = "Browser history requested.";
                    return true;
                case "browser-find":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_F);
                    message = "Browser find in page requested.";
                    return true;
                case "browser-find-next":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_F3);
                    message = "Browser find next requested.";
                    return true;
                case "browser-find-previous":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_SHIFT, VK_F3);
                    message = "Browser find previous requested.";
                    return true;
                case "browser-scroll-up":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_PRIOR);
                    message = "Browser scroll up requested.";
                    return true;
                case "browser-scroll-down":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_NEXT);
                    message = "Browser scroll down requested.";
                    return true;
                case "browser-start-scroll-up":
                    StartContinuousScroll(VK_PRIOR, "up");
                    message = "Browser start scrolling up requested.";
                    return true;
                case "browser-start-scroll-down":
                    StartContinuousScroll(VK_NEXT, "down");
                    message = "Browser start scrolling down requested.";
                    return true;
                case "browser-start-scroll-left":
                    StartContinuousScroll(VK_LEFT, "left");
                    message = "Browser start scrolling left requested.";
                    return true;
                case "browser-start-scroll-right":
                    StartContinuousScroll(VK_RIGHT, "right");
                    message = "Browser start scrolling right requested.";
                    return true;
                case "browser-stop-scroll":
                    StopContinuousScroll();
                    message = "Browser stop scrolling requested.";
                    return true;
                case "browser-scroll-left":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_LEFT);
                    message = "Browser scroll left requested.";
                    return true;
                case "browser-scroll-right":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_RIGHT);
                    message = "Browser scroll right requested.";
                    return true;
                case "browser-scroll-top":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_HOME);
                    message = "Browser scroll to top requested.";
                    return true;
                case "browser-scroll-bottom":
                    StopContinuousScroll();
                    SendKeyIfNeeded(VK_END);
                    message = "Browser scroll to bottom requested.";
                    return true;
                case "browser-zoom-in":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_OEM_PLUS);
                    message = "Browser zoom in requested.";
                    return true;
                case "browser-zoom-out":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_OEM_MINUS);
                    message = "Browser zoom out requested.";
                    return true;
                case "browser-zoom-reset":
                    StopContinuousScroll();
                    SendKeyChordIfNeeded(VK_CONTROL, VK_0);
                    message = "Browser zoom reset requested.";
                    return true;
                default:
                    message = $"Unknown browser action: {action}";
                    return false;
            }
        }
        catch (Exception ex)
        {
            message = $"Unable to execute browser action: {ex.Message}";
            return false;
        }
    }

    private void StartContinuousScroll(ushort virtualKey, string directionLabel)
    {
        lock (_scrollSync)
        {
            _continuousScrollKey = virtualKey;
            _continuousScrollLabel = directionLabel;
            _continuousScrollActive = true;
            if (_dryRun)
                return;

            _continuousScrollTimer ??= new System.Threading.Timer(_ => TickContinuousScroll(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _continuousScrollTimer.Change(BrowserScrollTickInterval, BrowserScrollTickInterval);
        }
    }

    private void StopContinuousScroll()
    {
        lock (_scrollSync)
        {
            _continuousScrollActive = false;
            _continuousScrollLabel = string.Empty;
            if (_dryRun)
                return;

            _continuousScrollTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void TickContinuousScroll()
    {
        ushort key;
        lock (_scrollSync)
        {
            if (!_continuousScrollActive)
                return;

            key = _continuousScrollKey;
        }

        SendKey(key);
    }

    public static bool TryParseFindTextAction(string action, out string findText)
    {
        findText = string.Empty;
        if (string.IsNullOrWhiteSpace(action)
            || !action.StartsWith(BrowserFindTextActionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        findText = action[BrowserFindTextActionPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(findText)
            && !findText.Contains('\r')
            && !findText.Contains('\n');
    }

    public static bool TryParseAddressTextAction(string action, out string addressText)
    {
        addressText = string.Empty;
        if (string.IsNullOrWhiteSpace(action)
            || !action.StartsWith(BrowserAddressTextActionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        addressText = action[BrowserAddressTextActionPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(addressText)
            && !addressText.Contains('\r')
            && !addressText.Contains('\n');
    }

    public bool TryOpen(string input, out string message, out Uri? targetUri, bool forceSearch = false, BrowserOpenTarget browserTarget = BrowserOpenTarget.Default)
    {
        targetUri = null;
        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            message = "Enter a web address or search phrase.";
            return false;
        }

        if (!TryBuildTargetUri(trimmed, out targetUri, out var reason, forceSearch))
        {
            message = reason;
            return false;
        }

        try
        {
            if (browserTarget == BrowserOpenTarget.Chrome)
            {
                if (TryFindChrome(out var chromePath))
                {
                    var chromeStart = new ProcessStartInfo
                    {
                        FileName = chromePath,
                        UseShellExecute = false
                    };
                    chromeStart.ArgumentList.Add(targetUri!.ToString());
                    Process.Start(chromeStart);

                    message = $"Opened Chrome target: {targetUri}";
                    return true;
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = targetUri!.ToString(),
                UseShellExecute = true
            });

            message = browserTarget == BrowserOpenTarget.Chrome
                ? $"Chrome was not found, so Callsign opened the default browser target: {targetUri}"
                : $"Opened browser target: {targetUri}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Unable to open the browser target: {ex.Message}";
            targetUri = null;
            return false;
        }
    }

    public static bool TryBuildTargetUri(string input, out Uri? targetUri, out string reason, bool forceSearch = false)
    {
        targetUri = null;
        reason = string.Empty;

        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            reason = "Enter a web address or search phrase.";
            return false;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var directUri))
        {
            if (IsAllowedWebScheme(directUri.Scheme))
            {
                targetUri = directUri;
                return true;
            }

            if (!forceSearch)
            {
                reason = $"Browser mode only opens http/https web targets. '{directUri.Scheme}:' targets are blocked and must use their visible Callsign command surface.";
                return false;
            }
        }

        if (!forceSearch && (trimmed.Contains('\\') || trimmed.Contains('&') || trimmed.Contains('|') || trimmed.Contains('>') || trimmed.Contains('<')))
        {
            reason = "Browser mode only accepts web addresses or search phrases, not local file paths or shell text.";
            return false;
        }

        if (trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out var wwwUri))
            {
                targetUri = wwwUri;
                return true;
            }
        }

        if (LooksLikeDomain(trimmed))
        {
            if (Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out var domainUri))
            {
                targetUri = domainUri;
                return true;
            }
        }

        var query = Uri.EscapeDataString(trimmed);
        targetUri = new Uri($"{DefaultSearchBase}{query}", UriKind.Absolute);
        return true;
    }

    public static bool TryFindChrome(out string chromePath)
    {
        var candidates = new List<string>();

        AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
        AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
        AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe");

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var pathEntry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(pathEntry))
                candidates.Add(Path.Combine(pathEntry, "chrome.exe"));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                chromePath = candidate;
                return true;
            }
        }

        chromePath = string.Empty;
        return false;
    }

    private static bool LooksLikeDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains(' '))
            return false;

        return value.Contains('.');
    }

    private static bool IsAllowedWebScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static void AddCandidate(List<string> candidates, string root, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(root))
            return;

        var parts = new string[segments.Length + 1];
        parts[0] = root;
        Array.Copy(segments, 0, parts, 1, segments.Length);
        candidates.Add(Path.Combine(parts));
    }

    private void SendTextIfNeeded(string text)
    {
        if (_dryRun)
            return;

        SendText(text);
    }

    private void SendKeyIfNeeded(ushort virtualKey)
    {
        if (_dryRun)
            return;

        SendKey(virtualKey);
    }

    private void SendKeyChordIfNeeded(ushort modifierKey, ushort key)
    {
        if (_dryRun)
            return;

        SendKeyChord(modifierKey, key);
    }

    private void SendKeyChordIfNeeded(ushort modifierKey, ushort firstKey, ushort secondKey)
    {
        if (_dryRun)
            return;

        SendKeyChord(modifierKey, firstKey, secondKey);
    }

    private void PauseInputIfNeeded(int milliseconds)
    {
        if (_dryRun)
            return;

        Thread.Sleep(milliseconds);
    }

    private static void SendText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var inputs = new List<INPUT>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = character, dwFlags = KeyEventUnicode } }
            });
            inputs.Add(new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = character, dwFlags = KeyEventUnicode | KeyEventKeyUp } }
            });
        }

        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static void SendKey(ushort virtualKey)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyChord(ushort modifierKey, ushort key)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyChord(ushort modifierKey, ushort firstKey, ushort secondKey)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = firstKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = secondKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = secondKey, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = firstKey, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private const int InputKeyboard = 1;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_RIGHT = 0x27;
    private const ushort VK_HOME = 0x24;
    private const ushort VK_END = 0x23;
    private const ushort VK_PRIOR = 0x21;
    private const ushort VK_NEXT = 0x22;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_F = 0x46;
    private const ushort VK_H = 0x48;
    private const ushort VK_J = 0x4A;
    private const ushort VK_L = 0x4C;
    private const ushort VK_N = 0x4E;
    private const ushort VK_O = 0x4F;
    private const ushort VK_D = 0x44;
    private const ushort VK_P = 0x50;
    private const ushort VK_R = 0x52;
    private const ushort VK_S = 0x53;
    private const ushort VK_T = 0x54;
    private const ushort VK_W = 0x57;
    private const ushort VK_0 = 0x30;
    private const ushort VK_OEM_MINUS = 0xBD;
    private const ushort VK_OEM_PLUS = 0xBB;
    private const ushort VK_F3 = 0x72;
    private const ushort VK_F11 = 0x7A;
    private const ushort VK_RETURN = 0x0D;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private static readonly TimeSpan BrowserScrollTickInterval = TimeSpan.FromMilliseconds(150);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }
}
