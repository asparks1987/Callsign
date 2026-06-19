using System.Text.Json;

namespace Callsign.UI.Services;

public sealed class RuntimeStateMonitor
{
    private readonly string _statePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Runtime",
        "state.json");

    private readonly JsonSerializerOptions _jsonOptions = new();

    public RuntimeStateSnapshot? Read()
    {
        if (!File.Exists(_statePath))
            return null;

        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<RuntimeStateSnapshot>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
