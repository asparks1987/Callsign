using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Callsign.Extensions;

namespace Callsign.UI.Services;

public sealed class UpdateCheckService
{
    private const int WindowsErrorCancelled = 1223;
    private const string UpdateStateFile = "updates-state.json";
    private const string DownloadFolder = "Updates";
    private const string InstallerFilePrefix = "Callsign-Setup";

    public const string DefaultUpdateServer = "http://localhost:5087";
    public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(25);
    public static readonly TimeSpan DefaultIdentityFreshness = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HttpClient SharedClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private string _serverUrl;
    private readonly string _channel;
    private readonly TimeSpan _checkInterval;
    private readonly HttpClient _client;
    private readonly string _statePath;
    private readonly string _downloadDirectory;
    private readonly string _installedExecutablePath;
    private UpdateCheckState _state;

    private bool _isChecking;

    public UpdateCheckService(string channel = "alpha", string? serverUrl = null, TimeSpan? checkInterval = null, HttpClient? httpClient = null, string? statePath = null)
    {
        _serverUrl = string.IsNullOrWhiteSpace(serverUrl)
            ? Environment.GetEnvironmentVariable("CALLSIGN_UPDATE_SERVER") ?? DefaultUpdateServer
            : serverUrl.Trim();
        _channel = string.IsNullOrWhiteSpace(channel) ? "alpha" : channel.Trim();
        _checkInterval = checkInterval ?? DefaultCheckInterval;
        _client = httpClient ?? SharedClient;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _statePath = string.IsNullOrWhiteSpace(statePath)
            ? Path.Combine(localAppData, "Callsign", UpdateStateFile)
            : statePath;
        _downloadDirectory = Path.Combine(localAppData, "Callsign", DownloadFolder);
        _installedExecutablePath = GetCurrentExecutablePath();
        _state = LoadState(_statePath);
    }

    public string ServerUrl => _serverUrl;
    public string Channel => _channel;
    public TimeSpan CheckInterval => _checkInterval;
    public bool IsChecking => _isChecking;
    public DateTimeOffset? LastCheckUtc => _state.LastCheckUtc;
    public DateTimeOffset? LastCheckInUtc => _state.LastCheckInUtc;
    public string? LastKnownVersion => _state.LastKnownVersion;
    public DateTimeOffset? LastUpdateAttemptedUtc => _state.LastUpdateAttemptedUtc;
    public string? LastDownloadedInstallerPath => _state.LastDownloadedInstallerPath;
    public CallsignUpdateManifest? PendingManifest => _state.PendingManifest;

    public string DescribeStatus(DateTimeOffset nowUtc)
    {
        var lastCheck = _state.LastCheckUtc.HasValue
            ? $"{_state.LastCheckUtc.Value.ToLocalTime():g}"
            : "never";
        var nextDue = _state.LastCheckUtc.HasValue
            ? (_state.LastCheckUtc.Value + _checkInterval).ToLocalTime().ToString("g")
            : "on startup";
        var lastKnownVersion = string.IsNullOrWhiteSpace(_state.LastKnownVersion)
            ? "unknown"
            : _state.LastKnownVersion;
        var lastCheckIn = _state.LastCheckInUtc.HasValue
            ? $"{_state.LastCheckInUtc.Value.ToLocalTime():g}"
            : "never";
        var pendingVersion = _state.PendingManifest?.Version;
        var pending = string.IsNullOrWhiteSpace(pendingVersion) ? "none" : pendingVersion;
        var lastAttempt = _state.LastUpdateAttemptedUtc.HasValue
            ? $"{_state.LastUpdateAttemptedUtc.Value.ToLocalTime():g}"
            : "never";
        var lastDownloadedInstaller = !string.IsNullOrWhiteSpace(_state.LastDownloadedInstallerPath)
            ? (_state.LastDownloadedInstallerPath.Length > 120
                ? _state.LastDownloadedInstallerPath[^120..]
                : _state.LastDownloadedInstallerPath)
            : "none";

        return $"Server {ServerUrl}; channel {Channel}; last check {lastCheck}; next due {nextDue}; last check-in {lastCheckIn}; last known version {lastKnownVersion}; pending manifest {pending}; last install attempt {lastAttempt}; last downloaded installer {lastDownloadedInstaller}; cadence every {CheckInterval.TotalHours:0} hours.";
    }

    public Task<UpdateCheckResult> CheckForUpdateInBackgroundAsync(bool force = false, bool attemptInstall = true, CancellationToken cancellationToken = default) =>
        CheckForUpdateAsync(force, attemptInstall, showProgress: null, cancellationToken);

    public bool IsCheckDue(DateTimeOffset nowUtc)
    {
        if (_state.LastCheckUtc == null)
            return true;

        return nowUtc - _state.LastCheckUtc.Value >= _checkInterval;
    }

    public void SetServerUrl(string? serverUrl)
    {
        _serverUrl = string.IsNullOrWhiteSpace(serverUrl)
            ? Environment.GetEnvironmentVariable("CALLSIGN_UPDATE_SERVER") ?? DefaultUpdateServer
            : serverUrl.Trim();
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        bool force,
        bool attemptInstall,
        Action<string>? showProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (_isChecking)
            return new UpdateCheckResult(false, false, "Update check already running.", null);

        if (!force && !IsCheckDue(DateTimeOffset.UtcNow))
            return new UpdateCheckResult(false, false, "Update check is not due yet.", _state.PendingManifest);

        _isChecking = true;
        try
        {
            showProgress?.Invoke("Checking for updates...");

            var manifest = await TryFetchLatestManifestAsync(cancellationToken);
            if (manifest == null)
            {
                return new UpdateCheckResult(false, false, "Update server did not return a valid release manifest.", _state.PendingManifest);
            }

            _state = _state with { LastKnownVersion = manifest.Version, PendingManifest = manifest };
            _state = _state with { LastCheckUtc = DateTimeOffset.UtcNow };
            SaveState(_state, _statePath);

            var updateAvailable = IsUpdateAvailable(manifest);
            if (!updateAvailable)
                return new UpdateCheckResult(true, false, $"Callsign is up to date (v{manifest.Version}).", manifest);

            if (attemptInstall && !string.IsNullOrWhiteSpace(manifest.InstallerUrl))
            {
                showProgress?.Invoke($"Downloading Callsign installer {manifest.Version}.");
                var installerPath = await DownloadInstallerAsync(manifest, cancellationToken);
                if (installerPath == null)
                    return new UpdateCheckResult(false, true, "Update manifest was available, but installer download failed.", manifest);

                _state = _state with { LastDownloadedInstallerPath = installerPath };
                SaveState(_state, _statePath);

                if (!await VerifyInstallerAsync(installerPath, manifest, cancellationToken))
                {
                    return new UpdateCheckResult(false, true, "Downloaded installer checksum mismatch. Update was not installed.", manifest);
                }

                showProgress?.Invoke("Installer downloaded. Starting update.");
                var installResult = await TryRunInstallerAsync(installerPath, cancellationToken);

                if (installResult.Success)
                {
                    _state = _state with { LastUpdateAttemptedUtc = DateTimeOffset.UtcNow };
                    SaveState(_state, _statePath);
                    return new UpdateCheckResult(true, true, "An update installer has been launched.", manifest, installerPath, true);
                }

                return new UpdateCheckResult(
                    true,
                    true,
                    installResult.RequiresManualApproval
                        ? "Installer downloaded; manual approval is required to complete installation."
                        : "Installer launch failed. You can run the downloaded installer manually.",
                    manifest,
                    installerPath,
                    false,
                    ManualInstallRecommended: true);
            }

            return new UpdateCheckResult(true, true, "An update is available.", manifest, null, false);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, false, $"Update check failed: {ex.Message}", _state.PendingManifest);
        }
        finally
        {
            _isChecking = false;
        }
    }

    public async Task SendCheckInAsync(string? accountId, string? appVersion, string? deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = BuildCheckInPayloadSnapshot(accountId, appVersion, deviceId).ToClientPayload();

            var endpoint = BuildUrl("/api/checkins");
            var content = JsonContent.Create(payload);
            using var response = await _client.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            _state = _state with { LastCheckInUtc = DateTimeOffset.UtcNow };
            SaveState(_state, _statePath);
        }
        catch
        {
            // Best-effort only.
        }
    }

    internal UpdateCheckInPayloadSnapshot BuildCheckInPayloadSnapshot(string? accountId, string? appVersion, string? deviceId) =>
        BuildCheckInPayloadSnapshot(accountId, appVersion, deviceId, DateTimeOffset.UtcNow);

    internal UpdateCheckInPayloadSnapshot BuildCheckInPayloadSnapshot(string? accountId, string? appVersion, string? deviceId, DateTimeOffset requestedAtUtc)
    {
        return new UpdateCheckInPayloadSnapshot(
            AccountId: BuildPrivacyPreservingIdentifier("account", accountId),
            Channel: _channel,
            AppVersion: appVersion,
            DeviceId: BuildPrivacyPreservingIdentifier("device", deviceId),
            InstalledVersion: appVersion,
            InstalledAtUtc: requestedAtUtc,
            RequestedAtUtc: requestedAtUtc);
    }

    internal static string? BuildPrivacyPreservingIdentifier(string scope, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "id" : scope.Trim().ToLowerInvariant();
        var normalizedValue = value.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{normalizedScope}:{normalizedValue}"));
        return $"sha256:{Convert.ToHexString(bytes).ToLowerInvariant()[..32]}";
    }

    private async Task<CallsignUpdateManifest?> TryFetchLatestManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = BuildUrl($"/api/releases/latest?channel={Uri.EscapeDataString(_channel)}");
            using var response = await _client.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseText))
                return null;

            var versionElement = ReadManifestJson(responseText);
            return versionElement == null
                ? null
                : ConvertReleaseFeedToManifest(versionElement);
        }
        catch
        {
            return null;
        }
    }

    private static UpdateReleaseFeedItem? ReadManifestJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var first = document.RootElement.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object)
                    return JsonSerializer.Deserialize<UpdateReleaseFeedItem>(first.GetRawText(), ManifestJsonOptions);
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return JsonSerializer.Deserialize<UpdateReleaseFeedItem>(json, ManifestJsonOptions);
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<string?> DownloadInstallerAsync(CallsignUpdateManifest manifest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifest.InstallerUrl))
            return null;

        Directory.CreateDirectory(_downloadDirectory);
        var fileName = $"{InstallerFilePrefix}-{SanitizeFilePart(manifest.Version)}.exe";
        var targetPath = Path.Combine(_downloadDirectory, fileName);

        try
        {
            using var response = await _client.GetAsync(
                manifest.InstallerUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = File.Create(targetPath);
            await source.CopyToAsync(destination, cancellationToken);

            return targetPath;
        }
        catch
        {
            if (File.Exists(targetPath))
            {
                try
                {
                    File.Delete(targetPath);
                }
                catch
                {
                }
            }

            return null;
        }
    }

    private async Task<bool> VerifyInstallerAsync(string installerPath, CallsignUpdateManifest manifest, CancellationToken cancellationToken)
    {
        var expectedHash = manifest.InstallerSha256?.Trim();
        if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Equals("pending", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IsValidHexSha256(expectedHash))
            return true;

        var actualHash = await TryGetFileHashAsync(installerPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(actualHash))
            return false;

        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<InstallerRunResult> TryRunInstallerAsync(string installerPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(installerPath))
            return new InstallerRunResult(false, false);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = Path.GetDirectoryName(installerPath) ?? Environment.CurrentDirectory
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return new InstallerRunResult(false, false);

            // Give installer a chance to start. Actual completion can run in background.
            await Task.Delay(1200, cancellationToken);
            return new InstallerRunResult(true, false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == WindowsErrorCancelled)
        {
            return new InstallerRunResult(false, true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            return new InstallerRunResult(false, false);
        }
        catch (OperationCanceledException)
        {
            return new InstallerRunResult(false, false);
        }
    }

    private static bool IsUpdateAvailable(CallsignUpdateManifest manifest)
    {
        var localVersion = GetLocalVersionString();
        if (string.IsNullOrWhiteSpace(localVersion))
            return true;

        var localRank = ParseVersionRank(localVersion);
        var remoteRank = ParseVersionRank(manifest.Version);
        if (remoteRank.HasValue && localRank.HasValue && remoteRank > localRank)
            return true;

        return !string.Equals(manifest.Version, localVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static CallsignUpdateManifest ConvertReleaseFeedToManifest(UpdateReleaseFeedItem release)
    {
        var installerUrl = ChooseInstallerUrl(release.InstallerUrl, release.ArtifactUrl);
        var installerHash = release.InstallerSha256 ?? release.Sha256;
        var installerSize = release.InstallerSizeBytes > 0 ? release.InstallerSizeBytes : release.SizeBytes;
        var addedCommands = ConvertCommandChanges(release.AddedCommands);
        var changedCommands = ConvertCommandChanges(release.ChangedCommands);
        var removedCommands = ConvertCommandChanges(release.RemovedCommands);
        var packChanges = ConvertPackChanges(release.ExtensionPackChanges);
        var featureHighlights = ConvertFeatureHighlights(release.FeatureHighlights);

        return new CallsignUpdateManifest(
            release.Version ?? "0.0.0a",
            installerUrl,
            installerHash ?? string.Empty,
            installerSize,
            ChooseReleaseNotes(release.Notes, release.Title),
            AddedCommands: addedCommands,
            ChangedCommands: changedCommands,
            RemovedCommands: removedCommands,
            ExtensionPackChanges: packChanges,
            SplashSummary: string.IsNullOrWhiteSpace(release.Notes) ? release.Title : release.Notes,
            PublishedUtc: ParsePublishedDate(release.PublishedUtc),
            FeatureHighlights: featureHighlights);
    }

    private static IReadOnlyList<CallsignUpdateCommandChange> ConvertCommandChanges(IEnumerable<FeedCommandChangeItem>? commandChanges)
    {
        if (commandChanges == null)
            return Array.Empty<CallsignUpdateCommandChange>();

        var list = new List<CallsignUpdateCommandChange>();
        foreach (var change in commandChanges)
        {
            if (string.IsNullOrWhiteSpace(change.CommandId) || string.IsNullOrWhiteSpace(change.DisplayName))
                continue;

            var tier = TryParsePackTier(change.Tier);
            list.Add(new CallsignUpdateCommandChange(
                change.CommandId,
                change.DisplayName,
                change.Category,
                change.Summary,
                tier));
        }

        return list;
    }

    private static IReadOnlyList<CallsignUpdateExtensionChange> ConvertPackChanges(IEnumerable<FeedPackChangeItem>? packChanges)
    {
        if (packChanges == null)
            return Array.Empty<CallsignUpdateExtensionChange>();

        var list = new List<CallsignUpdateExtensionChange>();
        foreach (var change in packChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PackId) || string.IsNullOrWhiteSpace(change.DisplayName))
                continue;

            var tier = TryParsePackTier(change.Tier);
            list.Add(new CallsignUpdateExtensionChange(
                change.PackId,
                change.DisplayName,
                change.Version,
                tier,
                change.Summary,
                change.SignatureStatus,
                false));
        }

        return list;
    }

    private static IReadOnlyList<CallsignUpdateFeatureChange> ConvertFeatureHighlights(IEnumerable<FeedFeatureChangeItem>? featureHighlights)
    {
        if (featureHighlights == null)
            return Array.Empty<CallsignUpdateFeatureChange>();

        var list = new List<CallsignUpdateFeatureChange>();
        foreach (var feature in featureHighlights)
        {
            if (string.IsNullOrWhiteSpace(feature.FeatureId) || string.IsNullOrWhiteSpace(feature.DisplayName))
                continue;

            list.Add(new CallsignUpdateFeatureChange(
                feature.FeatureId,
                feature.DisplayName,
                feature.Category,
                feature.Summary));
        }

        return list;
    }

    private static CallsignPackTier TryParsePackTier(string? value) =>
        Enum.TryParse<CallsignPackTier>(value, true, out var tier) ? tier : CallsignPackTier.Free;

    private static string ChooseInstallerUrl(string? installerUrl, string? artifactUrl)
    {
        return !string.IsNullOrWhiteSpace(installerUrl)
            ? installerUrl
            : artifactUrl ?? string.Empty;
    }

    private static string ChooseReleaseNotes(string? notes, string? title) =>
        !string.IsNullOrWhiteSpace(notes) ? notes :
        !string.IsNullOrWhiteSpace(title) ? title :
        "Update available for Callsign.";

    private static DateTimeOffset? ParsePublishedDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(value, out var parsed))
            return parsed;

        return null;
    }

    private string BuildUrl(string path)
    {
        return $"{_serverUrl.TrimEnd('/')}{path}";
    }

    private static string GetCurrentExecutablePath()
    {
        try
        {
            var processPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(processPath))
                return processPath;
        }
        catch
        {
        }

        var location = Assembly.GetExecutingAssembly().Location;
        return string.IsNullOrWhiteSpace(location) ? string.Empty : location;
    }

    private static async Task<string?> TryGetFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            await using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }

    private static UpdateCheckState LoadState(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new UpdateCheckState();

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<UpdateCheckState>(json, ManifestJsonOptions);
            return state ?? new UpdateCheckState();
        }
        catch
        {
            return new UpdateCheckState();
        }
    }

    private static void SaveState(UpdateCheckState state, string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var serialized = JsonSerializer.Serialize(state, ManifestJsonOptions);
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(tempPath, serialized);
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch
        {
            // Best-effort persistence.
        }
    }

    private static bool IsValidSha256(string hash) =>
        hash.Length == 64 && hash.All(c => Uri.IsHexDigit(c));

    private static bool IsValidHexSha256(string hash) => IsValidSha256(hash);

    private static string SanitizeFilePart(string value)
    {
        var sanitized = new StringBuilder();
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c is '.' or '_' or '-')
                sanitized.Append(c);
            else
                sanitized.Append('_');
        }

        return sanitized.ToString();
    }

    private static string GetLocalVersionString()
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(GetCurrentExecutablePath());
            return string.IsNullOrWhiteSpace(info.ProductVersion) ? info.FileVersion ?? string.Empty : info.ProductVersion;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int? ParseVersionRank(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        if (int.TryParse(new string(version.Where(char.IsDigit).ToArray()), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
            return numeric;

        return null;
    }

    private sealed record UpdateReleaseFeedItem(
        string? Channel,
        string? Version,
        string? Title,
        string? Notes,
        string? ArtifactUrl,
        string? InstallerUrl,
        string? Sha256,
        long SizeBytes,
        string? InstallerSha256,
        long InstallerSizeBytes,
        string? PublishedUtc,
        IReadOnlyList<FeedCommandChangeItem>? AddedCommands = null,
        IReadOnlyList<FeedCommandChangeItem>? ChangedCommands = null,
        IReadOnlyList<FeedCommandChangeItem>? RemovedCommands = null,
        IReadOnlyList<FeedPackChangeItem>? ExtensionPackChanges = null,
        IReadOnlyList<FeedFeatureChangeItem>? FeatureHighlights = null);

    private sealed record FeedCommandChangeItem(
        string CommandId,
        string DisplayName,
        string Category,
        string Summary,
        string? Tier = null);

    private sealed record FeedPackChangeItem(
        string PackId,
        string DisplayName,
        string Version,
        string? Tier = null,
        string Summary = "",
        string? SignatureStatus = null);

    private sealed record FeedFeatureChangeItem(
        string FeatureId,
        string DisplayName,
        string Category,
        string Summary);

    private sealed record InstallerRunResult(bool Success, bool RequiresManualApproval);

    private sealed record UpdateCheckState(
        DateTimeOffset? LastCheckUtc = null,
        DateTimeOffset? LastCheckInUtc = null,
        string? LastKnownVersion = null,
        CallsignUpdateManifest? PendingManifest = null,
        DateTimeOffset? LastUpdateAttemptedUtc = null,
        string? LastDownloadedInstallerPath = null);
}

public sealed record UpdateCheckResult(
    bool Succeeded,
    bool UpdateAvailable,
    string Message,
    CallsignUpdateManifest? Manifest = null,
    string? InstallerPath = null,
    bool InstallerStarted = false,
    bool ManualInstallRecommended = false);

internal sealed record UpdateCheckInPayloadSnapshot(
    string? AccountId,
    string Channel,
    string? AppVersion,
    string? DeviceId,
    string? InstalledVersion,
    DateTimeOffset InstalledAtUtc,
    DateTimeOffset RequestedAtUtc)
{
    internal ClientCheckInPayload ToClientPayload() =>
        new(AccountId, Channel, AppVersion, DeviceId, InstalledVersion, InstalledAtUtc, RequestedAtUtc);
}

internal sealed record ClientCheckInPayload(
    string? AccountId,
    string Channel,
    string? AppVersion,
    string? DeviceId,
    string? InstalledVersion,
    DateTimeOffset InstalledAtUtc,
    DateTimeOffset RequestedAtUtc);
