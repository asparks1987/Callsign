using System.Diagnostics;

namespace Callsign.UI.Services;

public sealed class BrowserLaunchService
{
    private const string DefaultSearchBase = "https://www.bing.com/search?q=";

    public bool TryOpen(string input, out string message, out Uri? targetUri, bool forceSearch = false)
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
            Process.Start(new ProcessStartInfo
            {
                FileName = targetUri!.ToString(),
                UseShellExecute = true
            });

            message = $"Opened browser target: {targetUri}";
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

    private static bool LooksLikeDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains(' '))
            return false;

        return value.Contains('.');
    }
}
