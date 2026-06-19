namespace Callsign.Service;

public sealed record RuntimeHostOptions(
    bool IsUserRuntime,
    bool IsWindowsServiceRuntime,
    string RuntimeRole);
