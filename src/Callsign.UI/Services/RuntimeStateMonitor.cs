using System.Text.Json;

namespace Callsign.UI.Services;

public sealed class RuntimeStateMonitor : IDisposable
{
    private readonly string _statePath;

    private readonly JsonSerializerOptions _jsonOptions = new();
    private readonly FileSystemWatcher? _watcher;

    public RuntimeStateMonitor(string? statePath = null)
    {
        _statePath = string.IsNullOrWhiteSpace(statePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Callsign",
                "Runtime",
                "state.json")
            : statePath;

        var directory = Path.GetDirectoryName(_statePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        _watcher = new FileSystemWatcher(directory, Path.GetFileName(_statePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += (_, e) => OnStateFileChanged(e.FullPath);
        _watcher.Created += (_, e) => OnStateFileChanged(e.FullPath);
        _watcher.Renamed += (_, e) => OnStateFileChanged(e.FullPath);
    }

    public event EventHandler? Changed;

    public RuntimeStateSnapshot? Read()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (!File.Exists(_statePath))
                return null;

            try
            {
                using var stream = new FileStream(
                    _statePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<RuntimeStateSnapshot>(json, _jsonOptions);
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(25);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_watcher == null)
            return;

        _watcher.Dispose();
    }

    private void OnStateFileChanged(string fullPath)
    {
        if (!string.Equals(fullPath, _statePath, StringComparison.OrdinalIgnoreCase))
            return;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
