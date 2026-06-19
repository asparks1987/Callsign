using System.Text.Json;
using Callsign.UI.Services;

namespace Callsign.Service;

public sealed class RuntimeStateStore
{
    private readonly string _runtimeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Runtime");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public string StatePath => Path.Combine(_runtimeDir, "state.json");

    public RuntimeStateSnapshot? Read()
    {
        if (!File.Exists(StatePath))
            return null;

        try
        {
            using var stream = new FileStream(
                StatePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<RuntimeStateSnapshot>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Write(RuntimeStateSnapshot snapshot)
    {
        Directory.CreateDirectory(_runtimeDir);
        var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
        var tempPath = Path.Combine(_runtimeDir, $"state.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, json);
            ReplaceStateFileWithRetry(tempPath);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private void ReplaceStateFileWithRetry(string tempPath)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (File.Exists(StatePath))
                    File.Replace(tempPath, StatePath, null);
                else
                    File.Move(tempPath, StatePath);

                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(25 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(25 * attempt);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Snapshot writes are best-effort; a stale temp file should not stop the runtime.
        }
    }
}
