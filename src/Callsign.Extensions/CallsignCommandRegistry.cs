using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Callsign.Extensions;

public sealed class CallsignCommandRegistry
{
    private const string DefaultPackFolderName = "Packs";
    private const string InstalledPackFolderName = "Installed";
    private const string PackStateFileName = "packs-state.json";

    private readonly object _gate = new();
    private readonly Dictionary<string, PackRegistration> _packs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CommandRegistration> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _packRoot;
    private readonly string _statePath;

    public CallsignCommandRegistry(string? packRoot = null)
    {
        _packRoot = packRoot ?? GetDefaultPackRoot();
        _statePath = Path.Combine(_packRoot, PackStateFileName);
    }

    public static CallsignCommandRegistry Shared { get; } = new();

    public string PackRoot => _packRoot;

    public IReadOnlyList<CallsignPackInfo> GetPacks()
    {
        lock (_gate)
        {
            return _packs.Values
                .Select(pack => pack.Info)
                .OrderBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<CallsignCommandResolution> GetCommands()
    {
        lock (_gate)
        {
            return _commands.Values
                .Select(command => command.ToResolution())
                .OrderBy(command => command.PackDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(command => command.CommandDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void Refresh()
    {
        var loadedPacks = LoadPackState();
        var disabledPackIds = new HashSet<string>(loadedPacks.DisabledPackIds, StringComparer.OrdinalIgnoreCase);
        var discovered = DiscoverPackRegistrations(_packRoot, disabledPackIds);

        lock (_gate)
        {
            _packs.Clear();
            _commands.Clear();

            foreach (var pack in discovered)
                AddPackLocked(pack.Module, pack.Info.AssemblyPath, pack.Info, pack.Commands, pack.Disabled);
        }
    }

    public void RegisterPack(ICallsignCommandPack module, string assemblyPath = "<in-memory>")
    {
        lock (_gate)
        {
            var descriptor = module.Descriptor;
            var packId = NormalizeKey(descriptor.PackId);
            var commands = (module.Commands ?? Array.Empty<CallsignCommandDefinition>())
                .Select(command => new CommandRegistration(
                    packId,
                    descriptor.DisplayName,
                    descriptor.Version,
                    descriptor.Tier,
                    assemblyPath,
                    module,
                    command))
                .ToArray();

            var info = new CallsignPackInfo(
                PackId: packId,
                DisplayName: descriptor.DisplayName,
                Version: descriptor.Version,
                Tier: descriptor.Tier,
                LoadStatus: CallsignPackLoadStatus.Loaded,
                AssemblyPath: assemblyPath,
                CommandCount: commands.Length,
                Message: "Loaded.",
                LoadedUtc: DateTimeOffset.UtcNow);

            AddPackLocked(module, assemblyPath, info, commands, disabled: false);
        }
    }

    public bool TryResolve(string normalizedCommand, out CallsignCommandResolution resolution)
    {
        normalizedCommand = NormalizeCommand(normalizedCommand);

        lock (_gate)
        {
            foreach (var command in _commands.Values.OrderByDescending(command => command.MatchLength))
            {
                if (!_packs.TryGetValue(command.PackId, out var pack) || pack.Disabled)
                    continue;

                if (!TryMatch(command, normalizedCommand, out var argumentText))
                    continue;

                resolution = command.ToResolution(argumentText);
                return true;
            }
        }

        resolution = default!;
        return false;
    }

    public bool TryExecute(CallsignCommandExecutionContext context, out CallsignCommandExecutionResult result)
    {
        if (!TryResolve(context.NormalizedCommand, out var resolution))
        {
            result = new CallsignCommandExecutionResult(false, "No registered command matched the spoken phrase.");
            return false;
        }

        lock (_gate)
        {
            if (!_packs.TryGetValue(resolution.PackId, out var pack))
            {
                result = new CallsignCommandExecutionResult(false, $"Pack '{resolution.PackDisplayName}' is no longer loaded.");
                return true;
            }

            if (pack.Disabled)
            {
                result = new CallsignCommandExecutionResult(false, $"Pack '{resolution.PackDisplayName}' is disabled.");
                return true;
            }

            var commandContext = context with
            {
                PackId = resolution.PackId,
                CommandId = resolution.CommandId,
                ArgumentText = resolution.ArgumentText
            };

            try
            {
                result = pack.Module.ExecuteAsync(commandContext).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                result = new CallsignCommandExecutionResult(false, ex.Message, AuditEvent: $"pack_execute_failed:{resolution.PackId}:{resolution.CommandId}");
                return true;
            }
        }
    }

    public bool DisablePack(string packId)
    {
        packId = NormalizeKey(packId);
        if (string.IsNullOrWhiteSpace(packId))
            return false;

        lock (_gate)
        {
            if (!_packs.TryGetValue(packId, out var pack))
                return false;

            pack.Info = pack.Info with { LoadStatus = CallsignPackLoadStatus.Disabled, Message = "Disabled by user." };
            pack.Disabled = true;
            PersistState();
            return true;
        }
    }

    public bool EnablePack(string packId)
    {
        packId = NormalizeKey(packId);
        if (string.IsNullOrWhiteSpace(packId))
            return false;

        lock (_gate)
        {
            if (!_packs.TryGetValue(packId, out var pack))
                return false;

            pack.Info = pack.Info with { LoadStatus = CallsignPackLoadStatus.Loaded, Message = "Enabled." };
            pack.Disabled = false;
            PersistState();
            return true;
        }
    }

    private static string GetDefaultPackRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            DefaultPackFolderName,
            InstalledPackFolderName);

    private CallsignPackState LoadPackState()
    {
        try
        {
            if (!File.Exists(_statePath))
                return CallsignPackState.Empty;

            var json = File.ReadAllText(_statePath);
            var state = JsonSerializer.Deserialize<PackStateDto>(json);
            if (state == null)
                return CallsignPackState.Empty;

            return new CallsignPackState(state.DisabledPackIds ?? Array.Empty<string>());
        }
        catch
        {
            return CallsignPackState.Empty;
        }
    }

    private void PersistState()
    {
        Directory.CreateDirectory(_packRoot);
        var dto = new PackStateDto
        {
            DisabledPackIds = _packs.Values
                .Where(pack => pack.Disabled)
                .Select(pack => pack.Info.PackId)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

        File.WriteAllText(_statePath, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IReadOnlyList<PackRegistration> DiscoverPackRegistrations(string packRoot, HashSet<string> disabledPackIds)
    {
        if (!Directory.Exists(packRoot))
            return Array.Empty<PackRegistration>();

        var registrations = new List<PackRegistration>();
        foreach (var assemblyPath in Directory.EnumerateFiles(packRoot, "*.dll", SearchOption.AllDirectories))
        {
            var pack = TryLoadPack(assemblyPath, out var info, out var module, out var commands);
            if (pack == null || module == null || info == null || commands == null)
                continue;

            var disabled = disabledPackIds.Contains(info.PackId);
            registrations.Add(new PackRegistration(module, info with
            {
                LoadStatus = disabled ? CallsignPackLoadStatus.Disabled : CallsignPackLoadStatus.Loaded,
                Message = disabled ? "Disabled by user." : "Loaded."
            }, commands, disabled));
        }

        return registrations;
    }

    private static PackModule? TryLoadPack(
        string assemblyPath,
        out CallsignPackInfo? info,
        out ICallsignCommandPack? module,
        out IReadOnlyList<CommandRegistration>? commands)
    {
        info = null;
        module = null;
        commands = null;

        if (!File.Exists(assemblyPath))
            return null;

        try
        {
            var loadContext = new CallsignPackAssemblyLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            var packType = assembly.GetTypes().FirstOrDefault(type =>
                !type.IsAbstract
                && typeof(ICallsignCommandPack).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null);

            if (packType == null)
            {
                info = new CallsignPackInfo(
                    PackId: Path.GetFileNameWithoutExtension(assemblyPath),
                    DisplayName: Path.GetFileNameWithoutExtension(assemblyPath),
                    Version: "0.0.0",
                    Tier: CallsignPackTier.Free,
                    LoadStatus: CallsignPackLoadStatus.MissingPackType,
                    AssemblyPath: assemblyPath,
                    CommandCount: 0,
                    Message: "No ICallsignCommandPack implementation was found in the assembly.",
                    LoadedUtc: DateTimeOffset.UtcNow);
                return null;
            }

            module = (ICallsignCommandPack?)Activator.CreateInstance(packType);
            if (module == null)
            {
                info = new CallsignPackInfo(
                    PackId: Path.GetFileNameWithoutExtension(assemblyPath),
                    DisplayName: Path.GetFileNameWithoutExtension(assemblyPath),
                    Version: "0.0.0",
                    Tier: CallsignPackTier.Free,
                    LoadStatus: CallsignPackLoadStatus.LoadFailure,
                    AssemblyPath: assemblyPath,
                    CommandCount: 0,
                    Message: "Pack type could not be created.",
                    LoadedUtc: DateTimeOffset.UtcNow);
                return null;
            }

            var descriptor = module.Descriptor;
            var packId = NormalizeKey(descriptor.PackId);
            var commandDefinitions = module.Commands ?? Array.Empty<CallsignCommandDefinition>();
            var packModule = module;
            commands = commandDefinitions
                .Select(command => new CommandRegistration(
                    packId,
                    descriptor.DisplayName,
                    descriptor.Version,
                    descriptor.Tier,
                    assemblyPath,
                    packModule,
                    command))
                .ToArray();

            info = new CallsignPackInfo(
                PackId: packId,
                DisplayName: descriptor.DisplayName,
                Version: descriptor.Version,
                Tier: descriptor.Tier,
                LoadStatus: CallsignPackLoadStatus.Loaded,
                AssemblyPath: assemblyPath,
                CommandCount: commands.Count,
                Message: "Loaded.",
                LoadedUtc: DateTimeOffset.UtcNow);
            return new PackModule(module);
        }
        catch (Exception ex)
        {
            info = new CallsignPackInfo(
                PackId: Path.GetFileNameWithoutExtension(assemblyPath),
                DisplayName: Path.GetFileNameWithoutExtension(assemblyPath),
                Version: "0.0.0",
                Tier: CallsignPackTier.Free,
                LoadStatus: CallsignPackLoadStatus.LoadFailure,
                AssemblyPath: assemblyPath,
                CommandCount: 0,
                Message: ex.Message,
                LoadedUtc: DateTimeOffset.UtcNow);
            return null;
        }
    }

    private static bool TryMatch(CommandRegistration command, string normalizedCommand, out string argumentText)
    {
        foreach (var phrase in command.Definition.VoicePhrases.Select(NormalizeCommand).Where(phrase => !string.IsNullOrWhiteSpace(phrase)))
        {
            if (string.Equals(normalizedCommand, phrase, StringComparison.OrdinalIgnoreCase))
            {
                argumentText = string.Empty;
                return true;
            }

            var prefix = $"{phrase} ";
            if (normalizedCommand.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                argumentText = normalizedCommand[prefix.Length..].Trim();
                return true;
            }
        }

        argumentText = string.Empty;
        return false;
    }

    private static string NormalizeCommand(string value) =>
        NormalizeKey(value).Replace("  ", " ", StringComparison.Ordinal);

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant();
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        return normalized;
    }

    private sealed record PackStateDto
    {
        public string[]? DisabledPackIds { get; set; }
    }

    private sealed class PackModule
    {
        public PackModule(ICallsignCommandPack module)
        {
            Module = module;
        }

        public ICallsignCommandPack Module { get; }
    }

    private sealed class PackRegistration
    {
        public PackRegistration(ICallsignCommandPack module, CallsignPackInfo info, IReadOnlyList<CommandRegistration> commands, bool disabled)
        {
            Module = module;
            Info = info;
            Commands = commands;
            Disabled = disabled;
        }

        public ICallsignCommandPack Module { get; }
        public CallsignPackInfo Info { get; set; }
        public IReadOnlyList<CommandRegistration> Commands { get; }
        public bool Disabled { get; set; }
    }

    private sealed record CommandRegistration(
        string PackId,
        string PackDisplayName,
        string PackVersion,
        CallsignPackTier Tier,
        string AssemblyPath,
        ICallsignCommandPack Module,
        CallsignCommandDefinition Definition)
    {
        public int MatchLength => Definition.VoicePhrases.Count == 0 ? 0 : Definition.VoicePhrases.Max(phrase => NormalizeCommand(phrase).Length);

        public CallsignCommandResolution ToResolution(string argumentText = "")
        {
            return new CallsignCommandResolution(
                PackId,
                PackDisplayName,
                PackVersion,
                Tier,
                CallsignPackLoadStatus.Loaded,
                Definition.CommandId,
                Definition.DisplayName,
                argumentText,
                Definition);
        }
    }

    private void AddPackLocked(ICallsignCommandPack module, string assemblyPath, CallsignPackInfo info, IReadOnlyList<CommandRegistration> commands, bool disabled)
    {
        var pack = new PackRegistration(module, info with
        {
            LoadStatus = disabled ? CallsignPackLoadStatus.Disabled : info.LoadStatus,
            Message = disabled ? "Disabled by user." : info.Message
        }, commands, disabled);

        _packs[info.PackId] = pack;
        foreach (var command in commands)
            _commands[command.Definition.CommandId] = command;
    }
}
