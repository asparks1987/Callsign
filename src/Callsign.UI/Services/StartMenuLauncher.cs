using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace Callsign.UI.Services;

public sealed record StartMenuAppCandidate(string DisplayName, double Score, string MatchKind);

public sealed record StartMenuAppResolution(
    string RequestedName,
    string NormalizedName,
    bool IsResolved,
    bool IsAmbiguous,
    string? SelectedName,
    IReadOnlyList<StartMenuAppCandidate> Candidates,
    string Message);

public sealed record StartMenuLaunchResult(
    bool Succeeded,
    string RequestedName,
    string TargetName,
    string LaunchPath,
    bool StartMenuOpened,
    bool ShellFallbackUsed,
    IReadOnlyList<string> Steps,
    string Message)
{
    public bool IsVisibleStartMenuPath =>
        Succeeded
        && StartMenuOpened
        && !ShellFallbackUsed
        && LaunchPath.Contains("start-menu", StringComparison.OrdinalIgnoreCase);
}

public sealed class StartMenuLauncher
{
    private static readonly string[] AppCandidateBareNumberPrefixes =
    [
        "click ",
        "tap ",
        "open ",
        "launch ",
        "use ",
        "pick ",
        "choose ",
        "select ",
        "go with ",
        "take "
    ];

    private static readonly string[] StartMenuRoots =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
    ];

    public bool Launch(string appName, out string message)
    {
        var result = LaunchWithResult(appName);
        message = result.Message;
        return result.Succeeded;
    }

    public StartMenuLaunchResult LaunchWithResult(string appName)
    {
        var requestedName = appName?.Trim() ?? string.Empty;
        var steps = new List<string>();
        var target = ResolveAppName(requestedName);
        steps.Add($"normalized:{target}");
        var resolution = ResolveInstalledAppName(target);
        if (resolution.IsAmbiguous)
        {
            steps.Add("blocked:ambiguous");
            return CreateLaunchResult(false, requestedName, target, "blocked-ambiguous", false, false, steps, resolution.Message);
        }

        if (resolution.IsResolved && !string.IsNullOrWhiteSpace(resolution.SelectedName))
        {
            target = resolution.SelectedName;
            steps.Add($"resolved:{target}");
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            steps.Add("blocked:empty");
            return CreateLaunchResult(false, requestedName, target, "blocked-empty", false, false, steps, "Enter an app name first.");
        }

        if (!ValidateAppName(target, out var safetyMessage))
        {
            steps.Add("blocked:validation");
            return CreateLaunchResult(false, requestedName, target, "blocked-validation", false, false, steps, safetyMessage);
        }

        try
        {
            if (TryResolveTrustedSystemSurface(target, out var trustedSurface))
            {
                steps.Add("launch:trusted-system-surface");
                Process.Start(trustedSurface);
                return CreateLaunchResult(true, requestedName, target, "trusted-system-surface", false, false, steps, $"Opened {target}.");
            }

            var openedStartMenu = TryOpenStartMenu(steps);
            steps.Add(openedStartMenu ? "start-menu:opened" : "start-menu:not-opened");
            if (openedStartMenu)
            {
                Thread.Sleep(600);
                if (!TryTypeSearchText(target, steps))
                    throw new InvalidOperationException($"The Start search box could not receive '{target}'.");

                Thread.Sleep(600);
                if (!TryPressEnter(out var enterDetail))
                {
                    steps.Add($"start-menu:press-enter-failed:{enterDetail}");
                    throw new InvalidOperationException("The Start search could not be confirmed.");
                }

                steps.Add($"start-menu:pressed-enter:{enterDetail}");
            }

            var launched = false;
            var launchPath = openedStartMenu ? "start-menu-search" : "shell-backed-fallback";
            if (TryResolveInstalledShortcut(target, out var shortcutPath))
            {
                Thread.Sleep(800);
                Process.Start(new ProcessStartInfo
                {
                    FileName = shortcutPath,
                    UseShellExecute = true
                });
                launched = true;
                steps.Add("launch:installed-shortcut");
            }
            else if (TryResolveTrustedInstalledAlias(target, out var appAlias))
            {
                Thread.Sleep(800);
                Process.Start(new ProcessStartInfo
                {
                    FileName = appAlias,
                    UseShellExecute = true
                });
                launched = true;
                steps.Add("launch:trusted-installed-alias");
            }

            if (!launched)
            {
                steps.Add("blocked:no-launch-target");
                return CreateLaunchResult(false, requestedName, target, "blocked-no-launch-target", openedStartMenu, !openedStartMenu, steps, $"No shell-backed launch target was available for '{target}'.");
            }

            if (openedStartMenu)
            {
                return CreateLaunchResult(true, requestedName, target, launchPath, true, false, steps, $"Opened Start menu search for '{target}'.");
            }

            return CreateLaunchResult(true, requestedName, target, launchPath, false, true, steps, $"Opened '{target}' through a shell-backed fallback because the Start menu could not be opened.");
        }
        catch (Exception ex)
        {
            steps.Add($"error:{ex.GetType().Name}");
            var startMenuOpened = steps.Any(step => step.Equals("start-menu:opened", StringComparison.OrdinalIgnoreCase));
            return CreateLaunchResult(false, requestedName, target, "error", startMenuOpened, false, steps, $"Unable to open Start menu search: {ex.Message}");
        }
    }

    private static StartMenuLaunchResult CreateLaunchResult(
        bool succeeded,
        string requestedName,
        string targetName,
        string launchPath,
        bool startMenuOpened,
        bool shellFallbackUsed,
        IReadOnlyList<string> steps,
        string message) =>
        new(
            succeeded,
            requestedName,
            targetName,
            launchPath,
            startMenuOpened,
            shellFallbackUsed,
            steps.ToArray(),
            message);

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
        var resolution = ResolveInstalledAppName(input);
        resolvedName = resolution.SelectedName ?? resolution.NormalizedName;
        return resolution is { IsResolved: true, IsAmbiguous: false }
            && !string.IsNullOrWhiteSpace(resolvedName);
    }

    public StartMenuAppResolution ResolveInstalledAppName(string input, int maxCandidates = 5) =>
        ResolveInstalledAppName(input, GetInstalledAppNames(), maxCandidates);

    public static StartMenuAppResolution ResolveInstalledAppName(
        string input,
        IReadOnlyList<string> installedAppNames,
        int maxCandidates = 5)
    {
        var requestedName = input?.Trim() ?? string.Empty;
        var normalizedName = ResolveAppName(requestedName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: false,
                IsAmbiguous: false,
                SelectedName: null,
                Candidates: Array.Empty<StartMenuAppCandidate>(),
                Message: "Enter an app name first.");
        }

        if (TryResolveTrustedSystemSurface(normalizedName, out _))
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: true,
                IsAmbiguous: false,
                SelectedName: normalizedName,
                Candidates: [new StartMenuAppCandidate(normalizedName, 1.0, "trusted-system-surface")],
                Message: $"Resolved trusted Windows surface '{normalizedName}'.");
        }

        var candidates = installedAppNames
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (TryResolveCommonSpeechAlias(normalizedName, candidates, out var aliasMatch))
        {
            var aliasKind = candidates.Any(candidate => string.Equals(candidate, aliasMatch, StringComparison.OrdinalIgnoreCase))
                ? "speech-alias-installed"
                : "speech-alias";
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: true,
                IsAmbiguous: false,
                SelectedName: aliasMatch,
                Candidates: [new StartMenuAppCandidate(aliasMatch, 1.0, aliasKind)],
                Message: $"Resolved speech alias '{requestedName}' to '{aliasMatch}'.");
        }

        var normalizedInput = NormalizeAppName(normalizedName);
        var compactInput = CompactAppName(normalizedInput);

        var exact = candidates.FirstOrDefault(candidate =>
            string.Equals(NormalizeAppName(candidate), normalizedInput, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: true,
                IsAmbiguous: false,
                SelectedName: exact,
                Candidates: [new StartMenuAppCandidate(exact, 1.0, "exact")],
                Message: $"Resolved '{requestedName}' to '{exact}'.");
        }

        var compact = candidates.FirstOrDefault(candidate =>
            string.Equals(CompactAppName(NormalizeAppName(candidate)), compactInput, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(compact))
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: true,
                IsAmbiguous: false,
                SelectedName: compact,
                Candidates: [new StartMenuAppCandidate(compact, 1.0, "compact-exact")],
                Message: $"Resolved '{requestedName}' to '{compact}'.");
        }

        var ranked = RankInstalledAppCandidates(normalizedName, candidates, maxCandidates);
        var launchable = ranked.Where(candidate => candidate.Score >= 0.78).ToArray();
        if (launchable.Length == 0)
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: false,
                IsAmbiguous: false,
                SelectedName: normalizedName,
                Candidates: ranked,
                Message: $"No installed app confidently matched '{requestedName}'.");
        }

        var top = launchable[0];
        var tied = launchable
            .Where(candidate => candidate.Score >= top.Score - 0.05)
            .Take(Math.Max(1, maxCandidates))
            .ToArray();
        if (tied.Length > 1)
        {
            return new StartMenuAppResolution(
                requestedName,
                normalizedName,
                IsResolved: false,
                IsAmbiguous: true,
                SelectedName: null,
                Candidates: tied,
                Message: $"Multiple installed apps match '{requestedName}'. Choose one before Callsign launches anything.");
        }

        return new StartMenuAppResolution(
            requestedName,
            normalizedName,
            IsResolved: true,
            IsAmbiguous: false,
            SelectedName: top.DisplayName,
            Candidates: [top],
            Message: $"Resolved '{requestedName}' to '{top.DisplayName}'.");
    }

    public static IReadOnlyList<StartMenuAppCandidate> RankInstalledAppCandidates(
        string input,
        IReadOnlyList<string> installedAppNames,
        int maxCandidates = 5)
    {
        var normalizedInput = NormalizeAppName(ResolveAppName(input));
        return installedAppNames
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(candidate =>
            {
                var normalizedCandidate = NormalizeAppName(candidate);
                var score = ScoreAppNameMatch(normalizedInput, normalizedCandidate);
                var matchKind = score switch
                {
                    >= 1.0 => "exact",
                    >= 0.94 => "contains",
                    >= 0.78 => "fuzzy",
                    _ => "weak"
                };
                return new StartMenuAppCandidate(candidate, score, matchKind);
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxCandidates))
            .ToArray();
    }

    public static bool TryParseAppCandidateSelectionNumber(string transcript, out int candidateNumber)
    {
        candidateNumber = 0;
        var normalized = NormalizeAppName(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (TryParseDirectAppCandidateNumber(normalized, out candidateNumber))
            return true;

        var candidateText = normalized;
        foreach (var prefix in AppCandidateBareNumberPrefixes)
        {
            if (!candidateText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            candidateText = candidateText[prefix.Length..].Trim();
            break;
        }

        foreach (var marker in new[]
                 {
                     "app ",
                     "choice ",
                     "option ",
                     "result ",
                     "candidate ",
                     "number "
                 })
        {
            if (!candidateText.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                continue;

            candidateText = candidateText[marker.Length..].Trim();
            break;
        }

        if (!TryParseDirectAppCandidateNumber(candidateText, out candidateNumber))
            return false;

        return candidateNumber is >= 1 and <= 5;
    }

    public static bool IsConfirmAppCandidateCommand(string transcript)
    {
        var normalized = NormalizeAppName(transcript);
        return normalized is "confirm app"
            or "confirm choice"
            or "confirm result"
            or "confirm selection"
            or "open selected app"
            or "launch selected app"
            or "open selected result"
            or "launch selected result";
    }

    public static bool IsClearAppCandidateCommand(string transcript)
    {
        var normalized = NormalizeAppName(transcript);
        return normalized is "cancel"
            or "clear app choices"
            or "clear app choice"
            or "clear choices"
            or "clear choice"
            or "dismiss app choices"
            or "dismiss app choice"
            or "cancel app choices"
            or "cancel app choice"
            or "hide app choices"
            or "hide app choice"
            or "close app choices"
            or "close app choice";
    }

    public static bool IsNextAppCandidateCommand(string transcript)
    {
        var normalized = NormalizeAppName(transcript);
        return normalized is "next app choice"
            or "next choice"
            or "next option"
            or "next result"
            or "move to next app choice"
            or "move to next choice";
    }

    public static bool IsPreviousAppCandidateCommand(string transcript)
    {
        var normalized = NormalizeAppName(transcript);
        return normalized is "previous app choice"
            or "previous choice"
            or "previous option"
            or "previous result"
            or "move to previous app choice"
            or "move to previous choice"
            or "last app choice"
            or "last choice";
    }

    public static string ResolveAppName(string value) =>
        NormalizeAppName(value).Length == 0 ? string.Empty : NormalizeCommonSpeechAlias(value.Trim());

    private static bool TryOpenStartMenu(ICollection<string> steps)
    {
        if (TryPressWindowsKey(out var windowsKeyDetail))
        {
            steps.Add($"start-open:sendinput-windows-key:{windowsKeyDetail}");
            if (WaitForStartMenuOrSearchSurface())
                return true;
        }
        else
        {
            steps.Add($"start-open:sendinput-windows-key-failed:{windowsKeyDetail}");
        }

        if (TryPressCtrlEscape(out var ctrlEscapeDetail))
        {
            steps.Add($"start-open:sendinput-ctrl-escape:{ctrlEscapeDetail}");
            if (WaitForStartMenuOrSearchSurface())
                return true;
        }
        else
        {
            steps.Add($"start-open:sendinput-ctrl-escape-failed:{ctrlEscapeDetail}");
        }

        TryPressWindowsKeyWithKeybdEvent();
        steps.Add("start-open:keybd-event-windows-key:sent");
        if (WaitForStartMenuOrSearchSurface())
            return true;

        if (TryPressCtrlEscapeWithSendKeys(out var sendKeysDetail))
        {
            steps.Add($"start-open:sendkeys-ctrl-escape:{sendKeysDetail}");
            if (WaitForStartMenuOrSearchSurface())
                return true;
        }
        else
        {
            steps.Add($"start-open:sendkeys-ctrl-escape-failed:{sendKeysDetail}");
        }

        if (TryInvokeStartButton(out var invokeDetail))
        {
            steps.Add($"start-open:uia-start-button:{invokeDetail}");
            if (WaitForStartMenuOrSearchSurface())
                return true;
        }
        else
        {
            steps.Add($"start-open:uia-start-button-failed:{invokeDetail}");
        }

        return false;
    }

    private static bool TryTypeSearchText(string value, ICollection<string> steps)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var inputs = new List<INPUT>(trimmed.Length * 2);
        foreach (var character in trimmed)
        {
            inputs.Add(new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KeyEventUnicode
                    }
                }
            });
            inputs.Add(new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KeyEventUnicode | KeyEventKeyUp
                    }
                }
            });
        }

        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent == inputs.Count)
        {
            steps.Add($"start-menu:type-search-sendinput:sent={sent}");
            return true;
        }

        steps.Add($"start-menu:type-search-sendinput-failed:sent={sent};lastError={Marshal.GetLastWin32Error()}");
        try
        {
            SendKeys.SendWait(EscapeSendKeysText(trimmed));
            steps.Add("start-menu:type-search-sendkeys:sent");
            return true;
        }
        catch (Exception ex)
        {
            steps.Add($"start-menu:type-search-sendkeys-failed:{ex.GetType().Name}:{ex.Message}");
            return false;
        }
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
            || ContainsUnsafeLaunchPhrase(normalized)
            || normalized is "cmd"
                or "command"
                or "command prompt"
                or "terminal"
                or "windows terminal"
                or "shell"
                or "bash"
                or "wsl"
                or "regedit"
                or "registry editor"
                or "event viewer"
                or "services"
                or "service manager"
                or "device manager"
                or "task scheduler"
                or "computer management"
                or "disk management"
                or "administrative tools"
                or "admin tools"
                or "run as administrator")
        {
            message = "Administrative and elevated tools are outside the alpha free launch scope. Try a plain installed app name like Notepad or Calculator.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool ContainsUnsafeLaunchPhrase(string normalized)
    {
        var compact = $" {normalized} ";
        foreach (var phrase in UnsafeLaunchPhrases)
        {
            if (compact.Contains($" {phrase} ", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizeAppName(string value) =>
        string.Join(' ', value.ToLowerInvariant().Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string NormalizeAutomationText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(' ', value
            .ToLowerInvariant()
            .Replace("&", " and ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Split([' ', '\t', '\r', '\n', '.', ',', ':', ';', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string CompactAppName(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static readonly string[] UnsafeLaunchPhrases =
    [
        "run as administrator",
        "run as admin",
        "administrator",
        "elevated",
        "install",
        "installer",
        "setup",
        "uninstall",
        "uninstaller",
        "remove program",
        "remove programs",
        "add remove programs",
        "programs and features",
        "security settings",
        "windows security",
        "virus threat protection",
        "firewall",
        "defender",
        "credential manager",
        "bitlocker",
        "user account control",
        "uac",
        "local security policy",
        "group policy",
        "gpedit",
        "secpol"
    ];

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

    private static bool TryParseDirectAppCandidateNumber(string value, out int candidateNumber)
    {
        candidateNumber = NormalizeAppName(value) switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            var numberText when int.TryParse(numberText, out var parsed) => parsed,
            _ => 0
        };

        return candidateNumber is >= 1 and <= 5;
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

    private static bool TryPressWindowsKey(out string detail)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows, dwFlags = KeyEventKeyUp } } }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        detail = sent == inputs.Length
            ? $"sent={sent}"
            : $"sent={sent};lastError={Marshal.GetLastWin32Error()}";
        return sent == inputs.Length;
    }

    private static bool TryPressCtrlEscape(out string detail)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl, dwFlags = KeyEventKeyUp } } }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        detail = sent == inputs.Length
            ? $"sent={sent}"
            : $"sent={sent};lastError={Marshal.GetLastWin32Error()}";
        return sent == inputs.Length;
    }

    private static bool TryPressEnter(out string detail)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEnter } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEnter, dwFlags = KeyEventKeyUp } } }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent == inputs.Length)
        {
            detail = $"sendinput;sent={sent}";
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        try
        {
            SendKeys.SendWait("{ENTER}");
            detail = $"sendkeys;sendinputSent={sent};lastError={error}";
            return true;
        }
        catch (Exception ex)
        {
            detail = $"sendinputSent={sent};lastError={error};sendkeys={ex.GetType().Name}:{ex.Message}";
            return false;
        }
    }

    private static string EscapeSendKeysText(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '+' or '^' or '%' or '~' or '(' or ')' or '[' or ']' => $"{{{character}}}",
                '{' => "{{}",
                '}' => "{}}",
                _ => character
            });
        }

        return builder.ToString();
    }

    private static void TryPressWindowsKeyWithKeybdEvent()
    {
        keybd_event((byte)VirtualKeyLeftWindows, 0, KeyEventExtendedKey, UIntPtr.Zero);
        keybd_event((byte)VirtualKeyLeftWindows, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
    }

    private static bool TryPressCtrlEscapeWithSendKeys(out string detail)
    {
        try
        {
            SendKeys.SendWait("^{ESC}");
            detail = "sent";
            return true;
        }
        catch (Exception ex)
        {
            detail = $"{ex.GetType().Name}:{ex.Message}";
            return false;
        }
    }

    private static bool TryInvokeStartButton(out string detail)
    {
        try
        {
            var root = AutomationElement.RootElement;
            if (root == null)
            {
                detail = "root-unavailable";
                return false;
            }

            var buttons = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.IsControlElementProperty, true)));

            for (var index = 0; index < buttons.Count; index++)
            {
                var button = buttons[index];
                if (!string.Equals(NormalizeAutomationText(button.Current.Name), "start", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!button.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
                    continue;

                ((InvokePattern)invokePattern).Invoke();
                Thread.Sleep(250);
                detail = "invoked";
                return true;
            }

            detail = "start-button-not-found";
        }
        catch (Exception ex)
        {
            detail = $"{ex.GetType().Name}:{ex.Message}";
            return false;
        }

        return false;
    }

    private static bool WaitForStartMenuOrSearchSurface()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            Thread.Sleep(100);
            if (IsStartMenuOrSearchSurfaceVisible())
                return true;
        }

        return false;
    }

    private static bool IsStartMenuOrSearchSurfaceVisible()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                _ = GetWindowThreadProcessId(hwnd, out var processId);
                if (processId != 0)
                {
                    using var process = Process.GetProcessById((int)processId);
                    if (process.ProcessName.Contains("StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase)
                        || process.ProcessName.Contains("SearchHost", StringComparison.OrdinalIgnoreCase)
                        || process.ProcessName.Contains("ShellExperienceHost", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // UIA fallback below may still identify the surface.
        }

        try
        {
            var root = AutomationElement.RootElement;
            if (root == null)
                return false;

            var windows = root.FindAll(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            for (var index = 0; index < windows.Count; index++)
            {
                var window = windows[index];
                var name = NormalizeAutomationText(window.Current.Name);
                if (name.Contains("start", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("search", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Best-effort verification only.
        }

        return false;
    }

    private const int InputKeyboard = 1;
    private const ushort VirtualKeyEnter = 0x0D;
    private const ushort VirtualKeyEscape = 0x1B;
    private const ushort VirtualKeyLeftCtrl = 0xA2;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
