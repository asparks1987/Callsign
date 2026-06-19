using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;

namespace Callsign.Setup;

internal enum InstallerAction
{
    Install,
    Repair,
    Uninstall
}

internal sealed record InstallerDiscovery(
    bool HasPreviousInstall,
    bool HasAppFiles,
    bool HasService,
    bool HasShortcuts,
    string InstallDirectory,
    string ModelDirectory,
    string LogsDirectory,
    IReadOnlyList<string> Evidence)
{
    public static InstallerDiscovery Empty(string installDirectory, string modelDirectory, string logsDirectory) =>
        new(false, false, false, false, installDirectory, modelDirectory, logsDirectory, Array.Empty<string>());
}

internal sealed record InstallerProgressEvent(
    int Percent,
    string Stage,
    string Message,
    string? FilePath = null);

internal sealed class InstallerWorkflow
{
    private const string ServiceName = "Callsign";
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _installRoot;
    private readonly string _installDir;
    private readonly string _modelDir;
    private readonly string _logsDir;
    private readonly string _runtimeManifestsDir;
    private readonly string _startupShortcut;
    private readonly string _desktopShortcut;
    private readonly string _startMenuShortcut;
    private readonly string _installerErrorLog;
    private readonly string _installerProgressLog;

    public InstallerWorkflow()
    {
        _installRoot = Path.Combine(_localAppData, ServiceName);
        _installDir = Path.Combine(_installRoot, "App");
        _modelDir = Path.Combine(_installRoot, "Models");
        _logsDir = Path.Combine(_installRoot, "Logs");
        _runtimeManifestsDir = Path.Combine(_installRoot, "Runtime", "manifests");
        _startupShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Callsign Runtime.lnk");
        _desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Callsign.lnk");
        _startMenuShortcut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            ServiceName,
            "Callsign.lnk");
        _installerErrorLog = Path.Combine(_logsDir, "installer-error.log");
        _installerProgressLog = Path.Combine(_logsDir, "installer-progress.log");
    }

    public InstallerDiscovery Detect()
    {
        var evidence = new List<string>();
        var installedExe = Path.Combine(_installDir, "Callsign.UI.exe");
        var serviceExe = Path.Combine(_installDir, "Callsign.Service.exe");
        var modelPath = Path.Combine(_modelDir, "callsign.onnx");

        var hasAppFiles = Directory.Exists(_installDir)
            || File.Exists(installedExe)
            || File.Exists(serviceExe)
            || File.Exists(modelPath);
        if (hasAppFiles)
            evidence.Add($"App files were found under '{_installDir}'.");

        var hasShortcuts = File.Exists(_desktopShortcut) || File.Exists(_startupShortcut) || File.Exists(_startMenuShortcut);
        if (hasShortcuts)
            evidence.Add("One or more Callsign shortcuts were found.");

        var hasService = IsServiceRegistered(ServiceName);
        if (hasService)
            evidence.Add("The Callsign Windows service is registered.");

        return new InstallerDiscovery(
            hasAppFiles || hasShortcuts || hasService,
            hasAppFiles,
            hasService,
            hasShortcuts,
            _installDir,
            _modelDir,
            _logsDir,
            evidence);
    }

    public async Task ExecuteAsync(InstallerAction action, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        await Task.Run(() => Execute(action, progress, cancellationToken), cancellationToken);
    }

    private void Execute(InstallerAction action, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_installDir);
        Directory.CreateDirectory(_modelDir);
        Directory.CreateDirectory(_logsDir);
        Directory.CreateDirectory(_runtimeManifestsDir);

        TryDeleteFile(_installerErrorLog);
        TryDeleteFile(_installerProgressLog);
        WriteInstallerProgress($"Installer action started: {action}. Administrator={IsAdministrator()}.");

        switch (action)
        {
            case InstallerAction.Install:
            case InstallerAction.Repair:
                ExecuteInstallOrRepair(progress, cancellationToken, action);
                break;
            case InstallerAction.Uninstall:
                ExecuteUninstall(progress, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown installer action.");
        }
    }

    private void ExecuteInstallOrRepair(IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken, InstallerAction action)
    {
        Report(progress, 0, action == InstallerAction.Install ? "Installing" : "Repairing", "Preparing the Callsign installation layout.");
        cancellationToken.ThrowIfCancellationRequested();

        StopExistingCallsignProcesses();
        WriteInstallerProgress("Stopped any existing Callsign processes.");
        Report(progress, 10, action == InstallerAction.Install ? "Installing" : "Repairing", "Existing Callsign processes were stopped.");

        var installedExe = Path.Combine(_installDir, "Callsign.UI.exe");
        var serviceExe = Path.Combine(_installDir, "Callsign.Service.exe");
        var fzfExe = Path.Combine(_installDir, "fzf.exe");
        var iconPath = Path.Combine(_installDir, "callsign.ico");
        var pythonRuntimeDir = Path.Combine(_installRoot, "Runtime", "python310");
        var openWakeWordSetupScript = Path.Combine(_installDir, "setupopenwakeword.ps1");
        var openWakeWordTestScript = Path.Combine(_installDir, "testopenwakeword.ps1");
        var openWakeWordResourcesDir = Path.Combine(_installDir, "openwakeword-resources");
        var openWakeWordWheelhouseDir = Path.Combine(_installDir, "openwakeword-wheelhouse");
        var pyannoteSetupScript = Path.Combine(_installDir, "setuppyannote.ps1");
        var pyannoteTestScript = Path.Combine(_installDir, "testpyannote.ps1");
        var pyannoteWheelhouseDir = Path.Combine(_installDir, "pyannote-wheelhouse");
        var pyannoteModelCacheDir = Path.Combine(_installRoot, "Runtime", "pyannote", "hub");
        var pyannoteAudioTarball = Path.Combine(_installDir, "pyannote_audio-4.0.4.tar.gz");
        var thirdPartySources = Path.Combine(_installDir, "THIRD_PARTY_SOURCES.md");
        var callsignWakeModel = Path.Combine(_modelDir, "callsign.onnx");

        ExtractResourceOrThrow("Callsign.UI.exe", installedExe, progress, 20, "Installed configuration manager", cancellationToken, "ui.manifest.json");
        TryExtractResource("Callsign.Service.exe", serviceExe, progress, 24, "Installed background service", cancellationToken, "service.manifest.json");
        TryExtractResource("fzf.exe", fzfExe, progress, 28, "Installed file search helper", cancellationToken, "fzf.manifest.json");
        TryExtractResource("callsign.ico", iconPath, progress, 30, "Installed Callsign icon", cancellationToken, "callsign.ico.manifest.json");
        TryExtractEmbeddedZip("python-runtime-win-x64.zip", pythonRuntimeDir, progress, 32, "Python runtime extracted.", cancellationToken, "python-runtime.manifest.json", new[] { "python.exe" });
        TryExtractResource("setupopenwakeword.ps1", openWakeWordSetupScript, progress, 34, "Installed openWakeWord setup helper", cancellationToken, "setupopenwakeword.manifest.json");
        TryExtractResource("testopenwakeword.ps1", openWakeWordTestScript, progress, 38, "Installed openWakeWord test helper", cancellationToken, "testopenwakeword.manifest.json");
        TryExtractEmbeddedZip("openwakeword-resources.zip", openWakeWordResourcesDir, progress, 40, "openWakeWord feature resources extracted.", cancellationToken, "openwakeword-resources.manifest.json", new[] { "melspectrogram.onnx", "embedding_model.onnx" });
        TryExtractEmbeddedZip("openwakeword-wheelhouse.zip", openWakeWordWheelhouseDir, progress, 42, "openWakeWord wheelhouse extracted.", cancellationToken, "openwakeword-wheelhouse.manifest.json", new[] { "openwakeword*.whl", "onnxruntime*.whl", "numpy*.whl" });
        TryExtractResource("setuppyannote.ps1", pyannoteSetupScript, progress, 44, "Installed pyannote setup helper", cancellationToken, "setuppyannote.manifest.json");
        TryExtractResource("testpyannote.ps1", pyannoteTestScript, progress, 46, "Installed pyannote test helper", cancellationToken, "testpyannote.manifest.json");
        TryExtractEmbeddedZip("pyannote-wheelhouse.zip", pyannoteWheelhouseDir, progress, 52, "pyannote wheelhouse extracted.", cancellationToken, "pyannote-wheelhouse.manifest.json", new[] { "pyannote_audio*.whl", "torch*.whl", "torchaudio*.whl", "numpy*.whl", "scipy*.whl", "soundfile*.whl", "huggingface_hub*.whl", "omegaconf*.whl" });
        TryExtractEmbeddedZip("pyannote-model-cache.zip", pyannoteModelCacheDir, progress, 54, "pyannote model cache extracted.", cancellationToken, "pyannote-model-cache.manifest.json", new[] { "models--pyannote--embedding", "config.yaml", "pytorch_model.bin", "model.safetensors" });
        TryExtractResource("pyannote_audio-4.0.4.tar.gz", pyannoteAudioTarball, progress, 56, "Installed pyannote.audio source tarball", cancellationToken, "pyannote_audio-4.0.4.manifest.json");
        TryExtractResource("THIRD_PARTY_SOURCES.md", thirdPartySources, progress, 58, "Installed third-party source notice", cancellationToken, "THIRD_PARTY_SOURCES.manifest.json");
        TryExtractResource("callsign.onnx", callsignWakeModel, progress, 60, "Installed openWakeWord Callsign model", cancellationToken, "callsign.onnx.manifest.json");
        WriteInstallerProgress("Payload extraction completed.");

        CreateShortcut(
            _startMenuShortcut,
            installedExe,
            _installDir,
            iconPath,
            arguments: null,
            windowStyle: 1,
            progress,
            64,
            "Created Start menu shortcut");
        CreateShortcut(
            _desktopShortcut,
            installedExe,
            _installDir,
            iconPath,
            arguments: null,
            windowStyle: 1,
            progress,
            68,
            "Created desktop shortcut");
        CreateShortcut(
            _startupShortcut,
            serviceExe,
            _installDir,
            iconPath,
            arguments: "--user-runtime --service-installed",
            windowStyle: 7,
            progress,
            72,
            "Created startup shortcut");

        var serviceInstalled = false;
        if (File.Exists(serviceExe))
        {
            serviceInstalled = InstallService(serviceExe, progress, cancellationToken);
        }

        RunOpenWakeWordSetup(openWakeWordSetupScript, progress, cancellationToken);
        RunPyannoteSetup(pyannoteSetupScript, progress, cancellationToken);
        StartUserRuntime(serviceExe, _installDir, serviceInstalled, progress, cancellationToken);

        progress.Report(new InstallerProgressEvent(100, action == InstallerAction.Install ? "Install complete" : "Repair complete", "Callsign is installed and ready."));
        WriteInstallerProgress($"{action} completed successfully. ServiceInstalled={serviceInstalled}.");
    }

    private void ExecuteUninstall(IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        Report(progress, 0, "Uninstalling", "Preparing to remove Callsign from this device.");
        cancellationToken.ThrowIfCancellationRequested();

        StopExistingCallsignProcesses();
        WriteInstallerProgress("Stopped any existing Callsign processes before uninstall.");
        Report(progress, 15, "Uninstalling", "Existing Callsign processes were stopped.");

        RemoveService(progress, cancellationToken);
        RemoveShortcut(_startMenuShortcut, progress, 35, "Removed Start menu shortcut");
        RemoveShortcut(_desktopShortcut, progress, 40, "Removed desktop shortcut");
        RemoveShortcut(_startupShortcut, progress, 45, "Removed startup shortcut");

        TryDeleteDirectory(_installRoot);
        progress.Report(new InstallerProgressEvent(100, "Uninstall complete", "Callsign and its local install folder were removed."));
        WriteInstallerProgress("Uninstall completed successfully.");
    }

    private bool InstallService(string serviceExe, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        Report(progress, 76, "Installing service", "Installing the Callsign Windows service.");
        cancellationToken.ThrowIfCancellationRequested();

        var serviceInstall = new ProcessStartInfo
        {
            FileName = serviceExe,
            Arguments = "--install-service",
            WorkingDirectory = _installDir,
            UseShellExecute = !IsAdministrator()
        };
        if (serviceInstall.UseShellExecute)
        {
            serviceInstall.Verb = "runas";
            serviceInstall.WindowStyle = ProcessWindowStyle.Normal;
        }
        else
        {
            serviceInstall.CreateNoWindow = true;
            serviceInstall.WindowStyle = ProcessWindowStyle.Hidden;
        }

        var process = Process.Start(serviceInstall);
        if (process == null)
        {
            Report(progress, 80, "Installing service", "The service installer could not be started.");
            return false;
        }

        var exited = process.WaitForExit(60000);
        var exitCode = exited ? process.ExitCode : -1;
        WriteInstallerProgress($"Service installer exited={exited}; exitCode={exitCode}.");
        if (!exited || exitCode != 0)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort cleanup.
            }

            Report(progress, 80, "Installing service", $"Service install failed with exit code {exitCode}.");
            return false;
        }

        Report(progress, 80, "Installing service", "The Callsign Windows service is installed.");
        return true;
    }

    private void RemoveService(IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        Report(progress, 25, "Removing service", "Stopping and removing the Callsign Windows service.");
        cancellationToken.ThrowIfCancellationRequested();

        var serviceExe = Path.Combine(_installDir, "Callsign.Service.exe");
        if (File.Exists(serviceExe))
        {
            var uninstall = new ProcessStartInfo
            {
                FileName = serviceExe,
                Arguments = "--uninstall-service",
                WorkingDirectory = _installDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                using var process = Process.Start(uninstall);
                process?.WaitForExit(30000);
                WriteInstallerProgress($"Service uninstall helper exited with {(process?.HasExited == true ? process.ExitCode : -1)}.");
            }
            catch (Exception ex)
            {
                WriteInstallerProgress($"Service uninstall helper failed: {ex.Message}");
            }
        }

        RunHiddenProcess("sc.exe", "stop Callsign", waitMilliseconds: 10000);
        RunHiddenProcess("sc.exe", "delete Callsign", waitMilliseconds: 10000);
        Report(progress, 30, "Removing service", "The Callsign service was removed.");
    }

    private string GetManifestPath(string manifestName) =>
        Path.Combine(_runtimeManifestsDir, manifestName);

    private static string ComputeSha256(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return ComputeSha256(stream);
    }

    private string ComputeEmbeddedResourceSha256(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        return ComputeSha256(stream);
    }

    private bool TryReadManifestHash(string manifestName, out string? resourceHash)
    {
        resourceHash = null;
        var manifestPath = GetManifestPath(manifestName);
        if (!File.Exists(manifestPath))
            return false;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (document.RootElement.TryGetProperty("resourceHash", out var hashValue))
            {
                resourceHash = hashValue.GetString();
                return !string.IsNullOrWhiteSpace(resourceHash);
            }
        }
        catch
        {
        }

        return false;
    }

    private void WriteManifest(string manifestName, string resourceName, string resourceHash, string targetPath, IReadOnlyList<string> sentinels)
    {
        Directory.CreateDirectory(_runtimeManifestsDir);
        var manifestPath = GetManifestPath(manifestName);
        var payload = new
        {
            resourceName,
            resourceHash,
            targetPath,
            sentinels,
            installedUtc = DateTime.UtcNow.ToString("o")
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool HasSentinelFiles(string directory, IReadOnlyList<string> sentinels)
    {
        if (!Directory.Exists(directory))
            return false;

        foreach (var sentinel in sentinels)
        {
            if (sentinel.Contains('*') || sentinel.Contains('?'))
            {
                if (!Directory.EnumerateFiles(directory, sentinel, SearchOption.AllDirectories).Any())
                    return false;
                continue;
            }

            var exactPath = Path.Combine(directory, sentinel);
            if (File.Exists(exactPath))
                continue;

            if (Directory.Exists(exactPath))
                continue;

            return false;
        }

        return true;
    }

    private static bool HasFileHash(string path, string expectedHash) =>
        File.Exists(path) && string.Equals(ComputeFileSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase);

    private void RunOpenWakeWordSetup(string setupScript, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        if (!File.Exists(setupScript))
        {
        Report(progress, 86, "Wake setup", $"openWakeWord setup helper was missing: {setupScript}.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        WriteInstallerProgress($"Starting bundled openWakeWord setup helper: {setupScript}. Bundled runtime and wheelhouse should already be in place.");
        var exitCode = RunPowerShellSetup(
            setupScript,
            "-InstallPythonPackages",
            progress,
            cancellationToken,
            "Installing openWakeWord from bundled wheels",
            86,
            90,
            TimeSpan.FromMinutes(10));

        WriteInstallerProgress($"Bundled openWakeWord setup exitCode={exitCode}.");
        Report(progress, 90, "Wake setup", exitCode == 0
            ? "openWakeWord runtime and model setup completed from bundled wheels."
            : $"openWakeWord setup returned exit code {exitCode}. See the progress log above for details.");
    }

    private void RunPyannoteSetup(string setupScript, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        if (!File.Exists(setupScript))
        {
            Report(progress, 91, "Identity setup", $"pyannote setup helper was missing: {setupScript}.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        WriteInstallerProgress($"Starting bundled pyannote identity setup helper: {setupScript}. Bundled runtime and wheelhouse should already be in place.");
        var exitCode = RunPowerShellSetup(
            setupScript,
            "-InstallPythonPackages -DownloadModel -TestEmbedding",
            progress,
            cancellationToken,
            "Installing pyannote from bundled wheels (large offline package set)",
            91,
            93,
            TimeSpan.FromMinutes(15));

        WriteInstallerProgress($"Bundled pyannote setup exitCode={exitCode}.");
        Report(progress, 93, "Identity setup", exitCode == 0
            ? "pyannote voice identity runtime and model cache setup completed from bundled wheels."
            : $"pyannote identity setup returned exit code {exitCode}. See the progress log above for details.");
    }

    private int RunPowerShellSetup(
        string setupScript,
        string setupArguments,
        IProgress<InstallerProgressEvent> progress,
        CancellationToken cancellationToken,
        string stage,
        int startPercent,
        int endPercent,
        TimeSpan timeout)
    {
        var workingDirectory = Path.GetDirectoryName(setupScript) ?? Environment.CurrentDirectory;
        var escapedScript = EscapePowerShellArgument(setupScript);
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{escapedScript}\" {setupArguments}";
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            },
            EnableRaisingEvents = true
        };

        var latestPercent = startPercent;
        var lastHeartbeat = DateTime.UtcNow;
        void ReportProcessLine(string prefix, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            latestPercent = Math.Min(endPercent - 1, latestPercent + 1);
            Report(progress, latestPercent, stage, $"{prefix}{line.Trim()}");
        }

        process.OutputDataReceived += (_, eventArgs) => ReportProcessLine(string.Empty, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => ReportProcessLine("warning: ", eventArgs.Data);

        Report(progress, startPercent, stage, $"Starting {Path.GetFileName(setupScript)}.");
        if (!process.Start())
            return -1;

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var startedUtc = DateTime.UtcNow;
        while (!process.WaitForExit(500))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                TryKillProcessTree(process);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var elapsed = DateTime.UtcNow - startedUtc;
            if (elapsed > timeout)
            {
                TryKillProcessTree(process);
                Report(progress, endPercent, stage, $"{Path.GetFileName(setupScript)} timed out after {timeout.TotalMinutes:0} minutes.");
                return -1;
            }

            if (DateTime.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(5))
            {
                lastHeartbeat = DateTime.UtcNow;
                Report(progress, Math.Min(endPercent - 1, latestPercent), stage, $"{Path.GetFileName(setupScript)} is still working ({elapsed:mm\\:ss} elapsed).");
            }
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private void StartUserRuntime(string serviceExe, string installDir, bool serviceInstalled, IProgress<InstallerProgressEvent> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(serviceExe))
        {
            Report(progress, 94, "Starting runtime", $"Installed service runtime was not found at '{serviceExe}'.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = serviceExe,
            Arguments = serviceInstalled ? "--user-runtime --service-installed" : "--user-runtime --service-fallback",
            WorkingDirectory = installDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        Report(progress, 96, "Starting runtime", "The user runtime was started.");
    }

    private void ExtractResourceOrThrow(string resourceName, string targetPath, IProgress<InstallerProgressEvent> progress, int percent, string message, CancellationToken cancellationToken, string? manifestName = null)
    {
        if (!TryExtractResource(resourceName, targetPath, progress, percent, message, cancellationToken, manifestName))
            throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
    }

    private bool TryExtractResource(string resourceName, string targetPath, IProgress<InstallerProgressEvent> progress, int percent, string message, CancellationToken cancellationToken, string? manifestName = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var hashPayload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (hashPayload == null)
            return false;

        var resourceHash = ComputeSha256(hashPayload);
        if (!string.IsNullOrWhiteSpace(manifestName) && TryReadManifestHash(manifestName, out var installedHash) && string.Equals(installedHash, resourceHash, StringComparison.OrdinalIgnoreCase) && HasFileHash(targetPath, resourceHash))
        {
            var cacheMessage = resourceName switch
            {
                "callsign.onnx" => "Callsign wake model unchanged, skipping copy.",
                "Callsign.UI.exe" => "Callsign.UI.exe unchanged, skipping copy.",
                "Callsign.Service.exe" => "Callsign.Service.exe unchanged, skipping copy.",
                "fzf.exe" => "fzf.exe unchanged, skipping copy.",
                _ => $"{message} cache hit."
            };
            Report(progress, percent, "Extracting files", cacheMessage, targetPath);
            return true;
        }

        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        ExtractPayload(payload, targetPath);
        if (!string.IsNullOrWhiteSpace(manifestName))
            WriteManifest(manifestName, resourceName, resourceHash, targetPath, Array.Empty<string>());
        Report(progress, percent, "Extracting files", message, targetPath);
        return true;
    }

    private bool TryExtractEmbeddedZip(string resourceName, string targetDirectory, IProgress<InstallerProgressEvent> progress, int percent, string message, CancellationToken cancellationToken, string manifestName, IReadOnlyList<string> sentinels)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var hashPayload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (hashPayload == null)
            return false;

        var resourceHash = ComputeSha256(hashPayload);
        if (TryReadManifestHash(manifestName, out var installedHash) &&
            string.Equals(installedHash, resourceHash, StringComparison.OrdinalIgnoreCase) &&
            HasSentinelFiles(targetDirectory, sentinels))
        {
            var cacheMessage = resourceName switch
            {
                "python-runtime-win-x64.zip" => "Python runtime unchanged, skipping extraction.",
                "openwakeword-resources.zip" => "openWakeWord resources unchanged, skipping extraction.",
                "openwakeword-wheelhouse.zip" => "openWakeWord wheelhouse unchanged, skipping extraction.",
                "pyannote-wheelhouse.zip" => "pyannote wheelhouse unchanged, skipping extraction.",
                "pyannote-model-cache.zip" => "pyannote model cache unchanged, skipping extraction.",
                _ => $"{message} cache hit."
            };
            Report(progress, percent, "Extracting files", cacheMessage, targetDirectory);
            return true;
        }

        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        var tempZip = Path.Combine(Path.GetTempPath(), $"{resourceName}.{Environment.ProcessId}.zip");
        try
        {
            using (var output = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                payload.CopyTo(output);
                output.Flush(true);
            }

            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, recursive: true);

            Directory.CreateDirectory(targetDirectory);
            ZipFile.ExtractToDirectory(tempZip, targetDirectory, overwriteFiles: true);
            WriteManifest(manifestName, resourceName, resourceHash, targetDirectory, sentinels);
            Report(progress, percent, "Extracting files", message, targetDirectory);
            return true;
        }
        catch (Exception ex)
        {
            WriteInstallerProgress($"Optional zip extraction failed for '{resourceName}': {ex}");
            Report(progress, percent, "Extracting files", $"{message} Failed: {ex.Message}", resourceName);
            return false;
        }
        finally
        {
            TryDeleteFile(tempZip);
        }
    }

    private static void ExtractPayload(Stream payload, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException("Install target directory could not be resolved."));

        var tempPath = $"{targetPath}.{Environment.ProcessId}.installing";
        if (File.Exists(tempPath))
            File.Delete(tempPath);

        using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            payload.CopyTo(output);
            output.Flush(true);
        }

        ReplaceInstalledFile(tempPath, targetPath);
    }

    private static void ReplaceInstalledFile(string tempPath, string targetPath)
    {
        try
        {
            if (File.Exists(targetPath))
            {
                File.SetAttributes(targetPath, FileAttributes.Normal);
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
        }
        catch (IOException ex)
        {
            throw new IOException(
                $"Unable to update '{targetPath}' because it is still in use. Close Callsign or stop the Callsign service, then run the installer again.",
                ex);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    private void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string workingDirectory,
        string iconPath,
        string? arguments,
        int windowStyle,
        IProgress<InstallerProgressEvent> progress,
        int percent,
        string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)
            ?? throw new InvalidOperationException("Shortcut directory could not be resolved."));

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not be started.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        if (!string.IsNullOrWhiteSpace(arguments))
            shortcut.Arguments = arguments;
        shortcut.Description = "Launch Callsign";
        shortcut.WindowStyle = windowStyle;
        if (File.Exists(iconPath))
            shortcut.IconLocation = iconPath;
        shortcut.Save();

        Report(progress, percent, "Creating shortcuts", message, shortcutPath);
    }

    private void RemoveShortcut(string path, IProgress<InstallerProgressEvent> progress, int percent, string message)
    {
        if (File.Exists(path))
            TryDeleteFile(path);
        Report(progress, percent, "Removing shortcuts", message, path);
    }

    private void StopExistingCallsignProcesses()
    {
        RunHiddenProcess("sc.exe", "stop Callsign", waitMilliseconds: 8000);
        StopProcessesByName("Callsign.Service");
        StopProcessesByName("Callsign.UI");
        StopProcessesByName("Callsign-Run");
    }

    private static void StopProcessesByName(string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                    continue;

                if (!process.CloseMainWindow())
                    process.Kill(entireProcessTree: true);

                if (!process.WaitForExit(5000))
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort shutdown.
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static int RunHiddenProcess(string fileName, string arguments, int waitMilliseconds)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (process == null)
                return 1;

            return process.WaitForExit(waitMilliseconds) ? process.ExitCode : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static bool IsServiceRegistered(string serviceName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query {serviceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process == null)
                return false;

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string EscapePowerShellArgument(string value) =>
        value.Replace("`", "``", StringComparison.Ordinal)
            .Replace("\"", "`\"", StringComparison.Ordinal);

    private void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            catch
            {
                // Best-effort.
            }
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            WriteInstallerProgress($"Unable to delete '{path}': {ex.Message}");
            throw;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private void Report(IProgress<InstallerProgressEvent> progress, int percent, string stage, string message, string? filePath = null)
    {
        var eventData = new InstallerProgressEvent(Math.Clamp(percent, 0, 100), stage, message, filePath);
        WriteInstallerProgress($"{stage}: {message}{(filePath == null ? string.Empty : $" [{filePath}]")}");
        progress.Report(eventData);
    }

    private void WriteInstallerProgress(string message)
    {
        try
        {
            Directory.CreateDirectory(_logsDir);
            File.AppendAllText(_installerProgressLog, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Installer diagnostics are best-effort only.
        }
    }
}
