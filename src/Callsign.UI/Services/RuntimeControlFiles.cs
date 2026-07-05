namespace Callsign.UI.Services;

public static class RuntimeControlFiles
{
    private static readonly string RuntimeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Runtime");

    public static string StopUserRuntimeRequestPath => Path.Combine(RuntimeDir, "stop-user-runtime.request");
    public static string ScriptedTranscriptRequestPath => Path.Combine(RuntimeDir, "scripted-transcript.request");
    public static string ClearActionHistoryRequestPath => Path.Combine(RuntimeDir, "clear-action-history.request");
    public static string ClearTranscriptHistoryRequestPath => Path.Combine(RuntimeDir, "clear-transcript-history.request");

    public static string RuntimeControlLogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Logs",
        "runtime-control.log");

    public static void RequestStopUserRuntime()
    {
        Directory.CreateDirectory(RuntimeDir);
        File.WriteAllText(StopUserRuntimeRequestPath, DateTime.UtcNow.ToString("O"));
        WriteControlLog("Configuration manager requested user-runtime stop.");
    }

    public static void ClearStopUserRuntimeRequest()
    {
        try
        {
            if (File.Exists(StopUserRuntimeRequestPath))
            {
                File.Delete(StopUserRuntimeRequestPath);
                WriteControlLog("Configuration manager cleared pending user-runtime stop request before start.");
            }
        }
        catch
        {
            WriteControlLog("Configuration manager could not clear pending user-runtime stop request.");
        }
    }

    public static bool TryConsumeStopUserRuntimeRequest(DateTime processStartedUtc)
    {
        if (!File.Exists(StopUserRuntimeRequestPath))
            return false;

        try
        {
            var value = File.ReadAllText(StopUserRuntimeRequestPath).Trim();
            if (DateTime.TryParse(value, out var requestedUtc)
                && requestedUtc.ToUniversalTime() < processStartedUtc)
            {
                File.Delete(StopUserRuntimeRequestPath);
                WriteControlLog("Ignored stale user-runtime stop request from before this runtime started.");
                return false;
            }

            File.Delete(StopUserRuntimeRequestPath);
            WriteControlLog("User-runtime consumed stop request.");
            return true;
        }
        catch
        {
            WriteControlLog("User-runtime could not consume stop request cleanly.");
            return false;
        }
    }

    public static void RequestScriptedTranscript(string transcript)
    {
        Directory.CreateDirectory(RuntimeDir);
        File.WriteAllText(ScriptedTranscriptRequestPath, transcript);
        WriteControlLog($"Configuration manager queued scripted transcript request: {transcript}");
    }

    public static bool TryConsumeScriptedTranscriptRequest(out string transcript)
    {
        transcript = string.Empty;
        if (!File.Exists(ScriptedTranscriptRequestPath))
            return false;

        try
        {
            transcript = File.ReadAllText(ScriptedTranscriptRequestPath).Trim();
            File.Delete(ScriptedTranscriptRequestPath);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                WriteControlLog("Ignored blank scripted transcript request.");
                return false;
            }

            WriteControlLog($"User-runtime consumed scripted transcript request: {transcript}");
            return true;
        }
        catch
        {
            WriteControlLog("User-runtime could not consume scripted transcript request cleanly.");
            transcript = string.Empty;
            return false;
        }
    }

    public static void RequestClearActionHistory()
    {
        Directory.CreateDirectory(RuntimeDir);
        File.WriteAllText(ClearActionHistoryRequestPath, DateTime.UtcNow.ToString("O"));
        WriteControlLog("Requested user-runtime action history clear.");
    }

    public static bool TryConsumeClearActionHistoryRequest()
    {
        if (!File.Exists(ClearActionHistoryRequestPath))
            return false;

        try
        {
            File.Delete(ClearActionHistoryRequestPath);
            WriteControlLog("User-runtime consumed action history clear request.");
            return true;
        }
        catch
        {
            WriteControlLog("User-runtime could not consume action history clear request cleanly.");
            return false;
        }
    }

    public static void RequestClearTranscriptHistory()
    {
        Directory.CreateDirectory(RuntimeDir);
        File.WriteAllText(ClearTranscriptHistoryRequestPath, DateTime.UtcNow.ToString("O"));
        WriteControlLog("Requested user-runtime transcript history clear.");
    }

    public static bool TryConsumeClearTranscriptHistoryRequest()
    {
        if (!File.Exists(ClearTranscriptHistoryRequestPath))
            return false;

        try
        {
            File.Delete(ClearTranscriptHistoryRequestPath);
            WriteControlLog("User-runtime consumed transcript history clear request.");
            return true;
        }
        catch
        {
            WriteControlLog("User-runtime could not consume transcript history clear request cleanly.");
            return false;
        }
    }

    private static void WriteControlLog(string message)
    {
        try
        {
            var logDir = Path.GetDirectoryName(RuntimeControlLogPath);
            if (!string.IsNullOrWhiteSpace(logDir))
                Directory.CreateDirectory(logDir);

            File.AppendAllText(RuntimeControlLogPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Runtime control diagnostics are best-effort only.
        }
    }
}
