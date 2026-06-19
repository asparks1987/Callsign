using System.Diagnostics;
using System.Globalization;

namespace Callsign.UI.Services;

public sealed record FileSearchResult(
    string Name,
    string FullPath,
    bool IsDirectory,
    string RootPath,
    DateTime LastWriteTimeUtc)
{
    public override string ToString() =>
        $"{(IsDirectory ? "[Folder]" : "[File]")} {Name} - {FullPath}";
}

public sealed record FileSearchReport(
    IReadOnlyList<FileSearchResult> Results,
    IReadOnlyList<string> Warnings,
    string SearchEngine = "built-in");

internal sealed record SearchCandidate(
    string Name,
    string FullPath,
    bool IsDirectory,
    string RootPath,
    DateTime LastWriteTimeUtc)
{
    public FileSearchResult ToResult() => new(Name, FullPath, IsDirectory, RootPath, LastWriteTimeUtc);

    public string ToLine() =>
        string.Join('\t',
            Name,
            FullPath,
            IsDirectory ? "1" : "0",
            RootPath,
            LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));

    public static bool TryParseLine(string line, out FileSearchResult? result)
    {
        result = null;

        var parts = line.Split('\t', 5);
        if (parts.Length != 5)
            return false;

        if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            return false;

        result = new FileSearchResult(
            Name: parts[0],
            FullPath: parts[1],
            IsDirectory: parts[2] == "1",
            RootPath: parts[3],
            LastWriteTimeUtc: new DateTime(ticks, DateTimeKind.Utc));
        return true;
    }
}

public sealed class FileSearchService
{
    public FileSearchReport Search(string query, IEnumerable<string>? roots = null, int maxResults = 50)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return new FileSearchReport(Array.Empty<FileSearchResult>(), ["Enter a file name or folder name to search for."]);

        var searchRoots = (roots ?? GetDefaultRoots())
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var warnings = new List<string>();
        var candidates = EnumerateCandidates(searchRoots, maxResults * 100, warnings);

        if (TrySearchWithFzf(candidates, trimmed, maxResults, warnings, out var fzfResults))
            return new FileSearchReport(fzfResults, warnings, "fzf");

        var normalizedQuery = Normalize(trimmed);
        var fallbackResults = candidates
            .Where(candidate =>
                Normalize(candidate.Name).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                Normalize(candidate.FullPath).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.ToResult())
            .Take(maxResults)
            .ToArray();

        return new FileSearchReport(fallbackResults, warnings, "built-in");
    }

    public bool TryOpen(FileSearchResult result, out string message)
    {
        var explorerTarget = result.IsDirectory
            ? $"\"{result.FullPath}\""
            : $"/select,\"{result.FullPath}\"";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = explorerTarget,
                UseShellExecute = true
            });

            message = result.IsDirectory
                ? $"Opened folder result in Explorer: '{result.FullPath}'."
                : $"Selected file result in Explorer: '{result.FullPath}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Unable to open the selected result: {ex.Message}";
            return false;
        }
    }

    public static IReadOnlyList<string> GetDefaultRoots()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Callsign")
        };

        return roots.Where(Directory.Exists).ToArray();
    }

    private static List<SearchCandidate> EnumerateCandidates(
        IReadOnlyList<string> roots,
        int maxCandidates,
        List<string> warnings)
    {
        var results = new List<SearchCandidate>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (results.Count >= maxCandidates)
                break;

            if (!Directory.Exists(root))
            {
                warnings.Add($"Search root was not available: {root}");
                continue;
            }

            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };

                foreach (var entry in Directory.EnumerateFileSystemEntries(root, "*", options))
                {
                    if (results.Count >= maxCandidates)
                        break;

                    try
                    {
                        if (!seenPaths.Add(entry))
                            continue;

                        var name = Path.GetFileName(entry);
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var attributes = File.GetAttributes(entry);
                        var lastWrite = File.GetLastWriteTimeUtc(entry);
                        results.Add(new SearchCandidate(
                            Name: name,
                            FullPath: entry,
                            IsDirectory: attributes.HasFlag(FileAttributes.Directory),
                            RootPath: root,
                            LastWriteTimeUtc: lastWrite));
                    }
                    catch
                    {
                        // Inaccessible entries are ignored so a single problem does not stop the whole search.
                    }
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Search could not scan '{root}': {ex.Message}");
            }
        }

        return results;
    }

    private static bool TrySearchWithFzf(
        IReadOnlyList<SearchCandidate> candidates,
        string query,
        int maxResults,
        List<string> warnings,
        out IReadOnlyList<FileSearchResult> results)
    {
        results = Array.Empty<FileSearchResult>();
        var fzfPath = LocateFzfExecutable();
        if (string.IsNullOrWhiteSpace(fzfPath))
        {
            warnings.Add("fzf.exe was not available, so Callsign used the built-in file search fallback.");
            return false;
        }

        if (candidates.Count == 0)
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fzfPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            startInfo.ArgumentList.Add("--scheme=path");
            startInfo.ArgumentList.Add("--filter");
            startInfo.ArgumentList.Add(query);

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            foreach (var candidate in candidates)
                process.StandardInput.WriteLine(candidate.ToLine());

            process.StandardInput.Close();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                    warnings.Add($"fzf search returned exit code {process.ExitCode}: {error.Trim()}");
                else
                    warnings.Add($"fzf search returned exit code {process.ExitCode}.");
                return false;
            }

            var ranked = new List<FileSearchResult>();
            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (SearchCandidate.TryParseLine(line, out var result) && result != null)
                {
                    ranked.Add(result);
                    if (ranked.Count >= maxResults)
                        break;
                }
            }

            if (ranked.Count == 0)
                return false;

            results = ranked;
            return true;
        }
        catch (Exception ex)
        {
            warnings.Add($"fzf search was unavailable: {ex.Message}");
            return false;
        }
    }

    private static string? LocateFzfExecutable()
    {
        var explicitPath = Environment.GetEnvironmentVariable("CALLSIGN_FZF_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        var localCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "fzf.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "fzf.exe")
        };

        foreach (var candidate in localCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var pathEntries = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEntries))
            return null;

        foreach (var entry in pathEntries.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(entry, "fzf.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
            }
        }

        return null;
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.ToLowerInvariant().Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
