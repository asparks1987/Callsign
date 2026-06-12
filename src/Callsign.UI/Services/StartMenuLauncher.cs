using System.Windows.Forms;
using System.Threading;

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

    public bool TryResolveInstalledAppName(string input, out string resolvedName)
    {
        resolvedName = ResolveAppName(input);
        if (string.IsNullOrWhiteSpace(resolvedName))
            return false;

        var candidates = GetInstalledAppNames();
        var normalizedInput = NormalizeAppName(resolvedName);

        var exact = candidates.FirstOrDefault(candidate =>
            string.Equals(NormalizeAppName(candidate), normalizedInput, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
        {
            resolvedName = exact;
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
        NormalizeAppName(value).Length == 0 ? string.Empty : value.Trim();

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

    private static double ScoreAppNameMatch(string input, string candidate)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(candidate))
            return 0;

        if (input == candidate)
            return 1.0;

        if (candidate.Contains(input, StringComparison.OrdinalIgnoreCase) || input.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            return 0.92;

        var inputTokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (inputTokens.Length == 0 || candidateTokens.Length == 0)
            return 0;

        var overlap = inputTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
        return (double)overlap / Math.Max(inputTokens.Length, candidateTokens.Length);
    }
}
