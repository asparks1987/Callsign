using System.Diagnostics;
using System.Windows.Forms;

namespace Callsign.UI.Services;

public enum BrowserOpenTarget
{
    Default,
    Chrome
}

public sealed class BrowserLaunchService
{
    private const string DefaultSearchBase = "https://www.bing.com/search?q=";

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
            switch (action.Trim().ToLowerInvariant())
            {
                case "browser-back":
                    SendKeys.SendWait("%{LEFT}");
                    message = "Browser back requested.";
                    return true;
                case "browser-forward":
                    SendKeys.SendWait("%{RIGHT}");
                    message = "Browser forward requested.";
                    return true;
                case "browser-refresh":
                    SendKeys.SendWait("^r");
                    message = "Browser refresh requested.";
                    return true;
                case "browser-new-tab":
                    SendKeys.SendWait("^t");
                    message = "Browser new tab requested.";
                    return true;
                case "browser-close-tab":
                    SendKeys.SendWait("^w");
                    message = "Browser close tab requested.";
                    return true;
                case "browser-focus-address-bar":
                    SendKeys.SendWait("^l");
                    message = "Browser address bar requested.";
                    return true;
                case "browser-find":
                    SendKeys.SendWait("^f");
                    message = "Browser find in page requested.";
                    return true;
                case "browser-find-next":
                    SendKeys.SendWait("{F3}");
                    message = "Browser find next requested.";
                    return true;
                case "browser-find-previous":
                    SendKeys.SendWait("+{F3}");
                    message = "Browser find previous requested.";
                    return true;
                case "browser-scroll-up":
                    SendKeys.SendWait("{PGUP}");
                    message = "Browser scroll up requested.";
                    return true;
                case "browser-scroll-down":
                    SendKeys.SendWait("{PGDN}");
                    message = "Browser scroll down requested.";
                    return true;
                case "browser-scroll-top":
                    SendKeys.SendWait("{HOME}");
                    message = "Browser scroll to top requested.";
                    return true;
                case "browser-scroll-bottom":
                    SendKeys.SendWait("{END}");
                    message = "Browser scroll to bottom requested.";
                    return true;
                case "browser-zoom-in":
                    SendKeys.SendWait("^{ADD}");
                    message = "Browser zoom in requested.";
                    return true;
                case "browser-zoom-out":
                    SendKeys.SendWait("^{-}");
                    message = "Browser zoom out requested.";
                    return true;
                case "browser-zoom-reset":
                    SendKeys.SendWait("^0");
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

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var directUri)
            && (string.Equals(directUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(directUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            targetUri = directUri;
            return true;
        }

        if (!forceSearch && (trimmed.Contains('\\') || trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || trimmed.Contains('&') || trimmed.Contains('|') || trimmed.Contains('>') || trimmed.Contains('<')))
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

    private static void AddCandidate(List<string> candidates, string root, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(root))
            return;

        var parts = new string[segments.Length + 1];
        parts[0] = root;
        Array.Copy(segments, 0, parts, 1, segments.Length);
        candidates.Add(Path.Combine(parts));
    }
}
