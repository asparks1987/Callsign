using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Callsign.Service;
using Callsign.UI.Services;

var arguments = args.Select(argument => argument.Trim()).ToArray();
if (arguments.Any(argument => argument.Equals("--install-service", StringComparison.OrdinalIgnoreCase)))
{
    var exitCode = InstallService();
    Environment.Exit(exitCode);
}

if (arguments.Any(argument => argument.Equals("--uninstall-service", StringComparison.OrdinalIgnoreCase)))
{
    var exitCode = UninstallService();
    Environment.Exit(exitCode);
}

var builder = Host.CreateApplicationBuilder(args);
var isWindowsServiceRuntime = arguments.Any(argument => argument.Equals("--run-service", StringComparison.OrdinalIgnoreCase));
var isUserRuntime = arguments.Any(argument => argument.Equals("--user-runtime", StringComparison.OrdinalIgnoreCase))
    || !isWindowsServiceRuntime;
var runtimeRole = isUserRuntime
    ? "user-runtime"
    : "windows-service-supervisor";

Mutex? userRuntimeMutex = null;
if (isUserRuntime)
{
    userRuntimeMutex = new Mutex(initiallyOwned: true, name: @"Local\Callsign.UserRuntime", createdNew: out var createdUserRuntimeMutex);
    if (!createdUserRuntimeMutex)
    {
        WriteRuntimeStartupLog("Duplicate user-runtime launch ignored because another user runtime already owns Local\\Callsign.UserRuntime.");
        userRuntimeMutex.Dispose();
        return;
    }
}

WriteRuntimeStartupLog($"Starting Callsign runtime role: {runtimeRole}.");

builder.Services.AddWindowsService(options => options.ServiceName = "Callsign");
builder.Services.AddSingleton(new RuntimeHostOptions(isUserRuntime, isWindowsServiceRuntime, runtimeRole));
builder.Services.AddSingleton<RuntimeStateStore>();
builder.Services.AddSingleton<ProfileStore>();
builder.Services.AddSingleton<StartMenuLauncher>();
builder.Services.AddSingleton<BrowserLaunchService>();
builder.Services.AddSingleton<FileSearchService>();
builder.Services.AddSingleton<VoiceCommandService>();
builder.Services.AddHostedService<CallsignRuntimeWorker>();

var host = builder.Build();
try
{
    await host.RunAsync();
}
finally
{
    WriteRuntimeStartupLog($"Stopping Callsign runtime role: {runtimeRole}.");
    userRuntimeMutex?.ReleaseMutex();
    userRuntimeMutex?.Dispose();
}

static int InstallService()
{
    var serviceExe = Environment.ProcessPath
        ?? throw new InvalidOperationException("Service path could not be resolved.");

    WriteServiceInstallLog($"InstallService starting. ProcessPath='{serviceExe}'.");
    RunScCommand("stop Callsign");

    var createArguments = string.Join(' ', new[]
    {
        "create Callsign",
        $"binPath= \"\\\"{serviceExe}\\\" --run-service\"",
        "start= auto",
        "DisplayName= Callsign"
    });

    var create = RunScCommand(createArguments);
    WriteServiceInstallLog($"sc create exit code: {create}.");
    if (create != 0)
    {
        var configArguments = string.Join(' ', new[]
        {
            "config Callsign",
            $"binPath= \"\\\"{serviceExe}\\\" --run-service\"",
            "start= auto",
            "DisplayName= Callsign"
        });

        var config = RunScCommand(configArguments);
        WriteServiceInstallLog($"sc config exit code: {config}.");
        if (config != 0)
            return create;
    }

    RunScCommand("description Callsign \"Callsign background voice service.\"");
    var start = StartService();
    WriteServiceInstallLog($"sc start exit code: {start}.");
    return start;
}

static int UninstallService()
{
    RunScCommand("stop Callsign");
    return RunScCommand("delete Callsign");
}

static int StartService()
{
    return RunScCommand("start Callsign");
}

static int RunScCommand(string arguments)
{
    WriteServiceInstallLog($"Running: sc.exe {arguments}");
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    });

    if (process == null)
        return 1;

    process.WaitForExit();
    try
    {
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(stdout))
            WriteServiceInstallLog($"stdout: {stdout.Trim()}");
        if (!string.IsNullOrWhiteSpace(stderr))
            WriteServiceInstallLog($"stderr: {stderr.Trim()}");
    }
    catch
    {
        // Best-effort diagnostics only.
    }

    return process.ExitCode;
}

static void WriteRuntimeStartupLog(string message)
{
    try
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Logs");
        Directory.CreateDirectory(logDir);
        File.AppendAllText(
            Path.Combine(logDir, "runtime-startup.log"),
            $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
    }
    catch
    {
        // Startup diagnostics are best-effort only.
    }
}

static void WriteServiceInstallLog(string message)
{
    try
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Logs");
        Directory.CreateDirectory(logDir);
        File.AppendAllText(
            Path.Combine(logDir, "service-install.log"),
            $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
    }
    catch
    {
        // Install diagnostics are best-effort only.
    }
}
