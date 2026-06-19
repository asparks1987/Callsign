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
            var json = File.ReadAllText(StatePath);
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
        var tempPath = StatePath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(StatePath))
            File.Replace(tempPath, StatePath, null);
        else
            File.Move(tempPath, StatePath);
    }
}
