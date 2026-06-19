using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Callsign.UI.Services;

public sealed class StartMenuLauncher
{
    private static readonly string[] StartMenuRoots =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
    ];

    public bool Launch(string appName, out string message)
    {
        var target = ResolveAppName(appName);
        if (TryResolveInstalledAppName(target, out var installed))
            target = installed;

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
            if (TryResolveTrustedSystemSurface(target, out var trustedSurface))
            {
                Process.Start(trustedSurface);
                message = $"Opened {target}.";
                return true;
            }

            if (!TryPressWindowsKey())
                SendKeys.SendWait("^{ESC}");
            Thread.Sleep(600);
            SendKeys.SendWait(EscapeSendKeysText(target));
            Thread.Sleep(600);
            SendKeys.SendWait("{ENTER}");
            if (TryResolveInstalledShortcut(target, out var shortcutPath))
            {
                Thread.Sleep(800);
                Process.Start(new ProcessStartInfo
                {
                    FileName = shortcutPath,
                    UseShellExecute = true
                });
            }
            else if (TryResolveTrustedInstalledAlias(target, out var appAlias))
            {
                Thread.Sleep(800);
                Process.Start(new ProcessStartInfo
                {
                    FileName = appAlias,
                    UseShellExecute = true
                });
            }

            message = $"Opened Start menu search for '{target}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Unable to open Start menu search: {ex.Message}";
            return false;
        }
    }

    public IReadOnlyList<string> GetInstalledAppNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in StartMenuRoots.Where(Directory.Exists))
        {
            foreach (var shortcut in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(shortcut);
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
        }

        return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool TryResolveInstalledShortcut(string appName, out string shortcutPath)
    {
        shortcutPath = string.Empty;
        var normalizedAppName = NormalizeAppName(appName);
        var compactAppName = CompactAppName(normalizedAppName);
        foreach (var root in StartMenuRoots.Where(Directory.Exists))
        {
            foreach (var shortcut in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(shortcut);
                var normalizedName = NormalizeAppName(name);
                if (string.Equals(normalizedName, normalizedAppName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(CompactAppName(normalizedName), compactAppName, StringComparison.OrdinalIgnoreCase))
                {
                    shortcutPath = shortcut;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryResolveTrustedInstalledAlias(string appName, out string appAlias)
    {
        appAlias = NormalizeAppName(appName) switch
        {
            "notepad" => "notepad.exe",
            "calculator" => "calc.exe",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(appAlias);
    }

    public static bool TryResolveTrustedSystemSurface(string appName, out ProcessStartInfo startInfo)
    {
        var normalized = NormalizeAppName(appName);
        ProcessStartInfo? candidate = normalized switch
        {
            "settings" => new ProcessStartInfo
            {
                FileName = "ms-settings:",
                UseShellExecute = true
            },
            "file explorer" => new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            },
            "this pc" or "my computer" or "computer" => new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "shell:MyComputerFolder",
                UseShellExecute = true
            },
            "recycle bin" or "trash" => new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "shell:RecycleBinFolder",
                UseShellExecute = true
            },
            "desktop" => OpenFolderProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
            "documents" or "document" => OpenFolderProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            "downloads" or "download" => OpenFolderProcessInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")),
            "pictures" or "pictures folder" => OpenFolderProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
            "music" or "music folder" => OpenFolderProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)),
            "videos" or "video folder" => OpenFolderProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)),
            "control panel" => new ProcessStartInfo
            {
                FileName = "control.exe",
                UseShellExecute = true
            },
            "task manager" => new ProcessStartInfo
            {
                FileName = "taskmgr.exe",
                UseShellExecute = true
            },
            _ => null
        };

        startInfo = candidate ?? new ProcessStartInfo();
        return candidate != null;
    }

    private static ProcessStartInfo? OpenFolderProcessInfo(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        return new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = true
        };
    }

    public bool TryResolveInstalledAppName(string input, out string resolvedName)
    {
        resolvedName = ResolveAppName(input);
        if (string.IsNullOrWhiteSpace(resolvedName))
            return false;

        var candidates = GetInstalledAppNames();
        if (TryResolveCommonSpeechAlias(resolvedName, candidates, out var aliasMatch))
        {
            resolvedName = aliasMatch;
            return true;
        }

        var normalizedInput = NormalizeAppName(resolvedName);
        var compactInput = CompactAppName(normalizedInput);

        var exact = candidates.FirstOrDefault(candidate =>
            string.Equals(NormalizeAppName(candidate), normalizedInput, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
        {
            resolvedName = exact;
            return true;
        }

        var compact = candidates.FirstOrDefault(candidate =>
            string.Equals(CompactAppName(NormalizeAppName(candidate)), compactInput, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(compact))
        {
            resolvedName = compact;
            return true;
        }

        var fuzzyMatch = candidates
            .Select(candidate => new { Candidate = candidate, Score = ScoreAppNameMatch(normalizedInput, NormalizeAppName(candidate)) })
            .OrderByDescending(match => match.Score)
            .FirstOrDefault();

        if (fuzzyMatch != null && fuzzyMatch.Score >= 0.78)
        {
            resolvedName = fuzzyMatch.Candidate;
            return true;
        }

        return false;
    }

    public static string ResolveAppName(string value) =>
        NormalizeAppName(value).Length == 0 ? string.Empty : NormalizeCommonSpeechAlias(value.Trim());

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

    private static string NormalizeAppName(string value) =>
        string.Join(' ', value.ToLowerInvariant().Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string CompactAppName(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static double ScoreAppNameMatch(string input, string candidate)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(candidate))
            return 0;

        if (input == candidate)
            return 1.0;

        var compactInput = CompactAppName(input);
        var compactCandidate = CompactAppName(candidate);
        if (compactInput == compactCandidate)
            return 1.0;

        if (compactCandidate.Contains(compactInput, StringComparison.OrdinalIgnoreCase)
            || compactInput.Contains(compactCandidate, StringComparison.OrdinalIgnoreCase))
            return 0.94;

        if (candidate.Contains(input, StringComparison.OrdinalIgnoreCase) || input.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            return 0.92;

        var inputTokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (inputTokens.Length == 0 || candidateTokens.Length == 0)
            return 0;

        var overlap = inputTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
        return (double)overlap / Math.Max(inputTokens.Length, candidateTokens.Length);
    }

    private static bool TryResolveCommonSpeechAlias(string input, IReadOnlyList<string> candidates, out string resolvedName)
    {
        var alias = NormalizeCommonSpeechAlias(input);
        if (string.Equals(alias, input, StringComparison.OrdinalIgnoreCase))
        {
            resolvedName = string.Empty;
            return false;
        }

        var normalizedAlias = NormalizeAppName(alias);
        var aliasMatch = candidates.FirstOrDefault(candidate =>
            string.Equals(NormalizeAppName(candidate), normalizedAlias, StringComparison.OrdinalIgnoreCase)
            || string.Equals(CompactAppName(NormalizeAppName(candidate)), CompactAppName(normalizedAlias), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(aliasMatch))
        {
            resolvedName = aliasMatch;
            return true;
        }

        resolvedName = alias;
        return true;
    }

    public static string NormalizeCommonSpeechAlias(string value)
    {
        var normalized = NormalizeAppName(value);
        return normalized switch
        {
            "settings" or "system settings" or "windows settings" or "open settings" or "show settings" or "open the settings" or "show the settings" or "launch settings" => "Settings",
            "file explorer" or "windows explorer" or "open explorer" or "show explorer" or "explorer" or "open file explorer" or "show file explorer" or "launch file explorer" or "open windows explorer" or "open the file explorer" => "File Explorer",
            "this pc" or "my computer" or "computer" or "open this pc" or "show this pc" or "open the computer" or "show the computer" or "launch this pc" => "This PC",
            "recycle bin" or "trash" or "open recycle bin" or "show recycle bin" or "open the recycle bin" => "Recycle Bin",
            "desktop" or "open desktop" or "show desktop" or "open the desktop" => "Desktop",
            "documents" or "my documents" or "open documents" or "show documents" or "open the documents" or "show me documents" => "Documents",
            "downloads" or "downloads folder" or "open downloads" or "show downloads" or "open the downloads folder" or "show me downloads" or "launch downloads" => "Downloads",
            "pictures" or "pictures folder" or "open pictures" or "show pictures" or "open the pictures folder" => "Pictures",
            "music" or "music folder" or "open music" or "show music" or "open the music folder" => "Music",
            "videos" or "videos folder" or "open videos" or "show videos" or "open the videos folder" => "Videos",
            "control panel" or "windows control panel" or "open control panel" or "show control panel" => "Control Panel",
            "task manager" or "windows task manager" or "open task manager" or "show task manager" => "Task Manager",
            "note pad" or "not pad" or "no pad" => "Notepad",
            "calc" or "calculate" or "calculater" or "calcu later" => "Calculator",
            "google crome" or "crome" or "chrome browser" or "google chrome browser" => "Google Chrome",
            "microsoft edge" or "edge browser" or "ms edge" => "Microsoft Edge",
            "vs code" or "v s code" or "visual code" or "code editor" => "Visual Studio Code",
            _ => value.Trim()
        };
    }

    private static bool TryPressWindowsKey()
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows, dwFlags = KeyEventKeyUp } } }
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
    }

    private const int InputKeyboard = 1;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const uint KeyEventKeyUp = 0x0002;

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
