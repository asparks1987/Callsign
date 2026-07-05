using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Callsign.Extensions;

public sealed class CallsignCommandRegistry
{
    private const string DefaultPackFolderName = "Packs";
    private const string InstalledPackFolderName = "Installed";
    private const string PackStateFileName = "packs-state.json";

    private readonly object _gate = new();
    private readonly Dictionary<string, PackRegistration> _packs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CommandRegistration> _commands = new();
    private readonly string _packRoot;
    private readonly string _statePath;
    private readonly CallsignEntitlementState _entitlements;

    public CallsignCommandRegistry(string? packRoot = null, CallsignEntitlementState? entitlements = null)
    {
        _packRoot = packRoot ?? GetDefaultPackRoot();
        _statePath = Path.Combine(_packRoot, PackStateFileName);
        _entitlements = entitlements ?? CallsignEntitlementState.FreeOnly;
    }

    public static CallsignCommandRegistry Shared { get; } = new();

    public string PackRoot => _packRoot;

    public static IReadOnlyList<string> ExpandImportablePackPaths(IEnumerable<string> sourcePaths)
    {
        if (sourcePaths == null)
            return Array.Empty<string>();

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawPath in sourcePaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            if (File.Exists(rawPath) && Path.GetExtension(rawPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                normalized.Add(Path.GetFullPath(rawPath));
                continue;
            }

            if (!Directory.Exists(rawPath))
                continue;

            foreach (var dll in Directory.EnumerateFiles(rawPath, "*.dll", SearchOption.AllDirectories))
                normalized.Add(Path.GetFullPath(dll));
        }

        return normalized.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

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
                return _commands
                    .Select(command =>
                    {
                        var loadStatus = _packs.TryGetValue(command.PackId, out var pack)
                            ? pack.Info.LoadStatus
                            : CallsignPackLoadStatus.MissingAssembly;
                        if (loadStatus == CallsignPackLoadStatus.Loaded && !IsTierEntitled(command.Definition.Tier))
                            loadStatus = CallsignPackLoadStatus.EntitlementRequired;
                        return command.ToResolution(loadStatus: loadStatus);
                    })
                    .OrderBy(command => command.PackDisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(command => command.CommandDisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }
    }

    public void Refresh()
    {
        var loadedPacks = LoadPackState();
        var disabledPackIds = new HashSet<string>(loadedPacks.DisabledPackIds, StringComparer.OrdinalIgnoreCase);
        var disabledAssemblyPaths = new HashSet<string>(loadedPacks.DisabledAssemblyPaths.Select(path => NormalizeAssemblyPath(path)), StringComparer.OrdinalIgnoreCase);
        var discovered = DiscoverPackRegistrations(_packRoot, disabledPackIds, disabledAssemblyPaths);

        lock (_gate)
        {
            _packs.Clear();
            _commands.Clear();

            foreach (var pack in discovered)
                AddPackLocked(pack.Module, pack.Info.AssemblyPath, pack.Info, pack.Commands, pack.Disabled);
        }
    }

    public CallsignPackImportResult ImportPack(string sourceAssemblyPath, bool enableImmediately = false, bool allowOverwrite = false)
    {
        if (string.IsNullOrWhiteSpace(sourceAssemblyPath))
            return new CallsignPackImportResult(false, "Choose a command pack DLL to import.");

        if (!string.Equals(Path.GetExtension(sourceAssemblyPath), ".dll", StringComparison.OrdinalIgnoreCase))
            return new CallsignPackImportResult(false, "Community command packs must be .dll files.", SourcePath: sourceAssemblyPath);

        var sourceFullPath = Path.GetFullPath(sourceAssemblyPath);
        if (!File.Exists(sourceFullPath))
            return new CallsignPackImportResult(false, "The selected command pack DLL was not found.", SourcePath: sourceFullPath);

        Directory.CreateDirectory(_packRoot);
        var installedPath = Path.Combine(_packRoot, Path.GetFileName(sourceFullPath));
        if (File.Exists(installedPath) && !allowOverwrite && !string.Equals(sourceFullPath, Path.GetFullPath(installedPath), StringComparison.OrdinalIgnoreCase))
            return new CallsignPackImportResult(false, "A command pack with that file name is already installed. Remove it or choose a different pack file.", sourceFullPath, installedPath);

        if (!string.Equals(sourceFullPath, Path.GetFullPath(installedPath), StringComparison.OrdinalIgnoreCase))
            File.Copy(sourceFullPath, installedPath, overwrite: allowOverwrite);

        Refresh();

        var packId = NormalizeKey(Path.GetFileNameWithoutExtension(installedPath));
        if (!enableImmediately)
        {
            var importedPack = GetPacks()
                .FirstOrDefault(pack => string.Equals(Path.GetFullPath(pack.AssemblyPath), installedPath, StringComparison.OrdinalIgnoreCase));

            if (importedPack != null && !string.IsNullOrWhiteSpace(importedPack.PackId))
            {
                packId = importedPack.PackId;
                if (importedPack.LoadStatus is not (CallsignPackLoadStatus.EntitlementRequired or CallsignPackLoadStatus.SignatureRequired))
                    DisablePack(packId);
            }
        }

        var finalPack = GetPacks()
            .FirstOrDefault(pack => string.Equals(pack.PackId, packId, StringComparison.OrdinalIgnoreCase));
        var finalStatus = finalPack?.LoadStatus
            ?? (enableImmediately ? CallsignPackLoadStatus.Loaded : CallsignPackLoadStatus.Disabled);

        lock (_gate)
        {
            if (_packs.TryGetValue(packId, out var registeredPack))
                registeredPack.Info = registeredPack.Info with { WasImported = true };
        }

        return new CallsignPackImportResult(
            true,
            finalStatus switch
            {
                CallsignPackLoadStatus.SignatureRequired => FormatSignatureRequiredMessage(finalPack?.Tier ?? CallsignPackTier.Free),
                CallsignPackLoadStatus.EntitlementRequired => FormatEntitlementRequiredMessage(finalPack?.Tier ?? CallsignPackTier.Pro),
                _ => enableImmediately
                    ? "Command pack imported and enabled."
                    : "Command pack imported disabled. Review it, then enable it from the Packs tab."
            },
            sourceFullPath,
            installedPath,
            packId,
            finalStatus);
    }

    public void RegisterPack(ICallsignCommandPack module, string assemblyPath = "<in-memory>")
    {
        lock (_gate)
        {
            var descriptor = module.Descriptor;
            var packId = NormalizeKey(descriptor.PackId);
            var commandDefinitions = module.Commands ?? Array.Empty<CallsignCommandDefinition>();
            if (!TryValidatePackMetadata(descriptor, commandDefinitions, out var validationMessage))
            {
                var invalidInfo = new CallsignPackInfo(
                    PackId: string.IsNullOrWhiteSpace(packId) ? NormalizeKey(descriptor.DisplayName) : packId,
                    DisplayName: string.IsNullOrWhiteSpace(descriptor.DisplayName) ? "Invalid command pack" : descriptor.DisplayName,
                    Version: string.IsNullOrWhiteSpace(descriptor.Version) ? "0.0.0" : descriptor.Version,
                    Tier: descriptor.Tier,
                    LoadStatus: CallsignPackLoadStatus.InvalidPack,
                    AssemblyPath: assemblyPath,
                    CommandCount: commandDefinitions.Count,
                    Message: validationMessage,
                    LoadedUtc: DateTimeOffset.UtcNow,
                    IsCommunity: descriptor.IsCommunity,
                    SignatureStatus: descriptor.SignatureStatus,
                    RequiresSignature: descriptor.RequiresSignature);
                AddPackLocked(module, assemblyPath, invalidInfo, Array.Empty<CommandRegistration>(), disabled: true);
                return;
            }

            var loadStatus = GetGatedLoadStatus(descriptor.Tier, descriptor.RequiresSignature, descriptor.SignatureStatus);
            var commands = commandDefinitions
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
                LoadStatus: loadStatus,
                AssemblyPath: assemblyPath,
                CommandCount: commands.Length,
                Message: FormatLoadStatusMessage(loadStatus, descriptor.Tier),
                LoadedUtc: DateTimeOffset.UtcNow,
                IsCommunity: descriptor.IsCommunity,
                SignatureStatus: descriptor.SignatureStatus,
                RequiresSignature: descriptor.RequiresSignature);

            AddPackLocked(module, assemblyPath, info, commands, disabled: loadStatus != CallsignPackLoadStatus.Loaded);
        }
    }

    public bool TryResolve(string normalizedCommand, out CallsignCommandResolution resolution)
    {
        normalizedCommand = NormalizeCommand(normalizedCommand);

        lock (_gate)
        {
        foreach (var command in _commands.OrderByDescending(command => command.MatchLength))
            {
                if (!_packs.TryGetValue(command.PackId, out var pack) || pack.Disabled)
                    continue;

                if (!IsTierEntitled(command.Definition.Tier))
                    continue;

                if (!TryMatch(command, normalizedCommand, out var argumentText))
                    continue;

                resolution = command.ToResolution(argumentText, pack.Info.LoadStatus);
                return true;
            }
        }

        resolution = default!;
        return false;
    }

    public bool TryExecute(CallsignCommandExecutionContext context, out CallsignCommandExecutionResult result) =>
        TryExecute(
            context,
            out result,
            identityVerified: false,
            freshIdentityVerified: false,
            approvalGranted: false);

    public bool TryExecute(
        CallsignCommandExecutionContext context,
        out CallsignCommandExecutionResult result,
        bool identityVerified,
        bool freshIdentityVerified = false,
        bool approvalGranted = false)
    {
        if (!TryResolve(context.NormalizedCommand, out var resolution))
        {
            result = new CallsignCommandExecutionResult(false, "No registered command matched the spoken phrase.");
            return false;
        }

        var policy = CallsignCommandPolicy.Evaluate(resolution.Definition, identityVerified, freshIdentityVerified);
        if (policy.Decision == CallsignPolicyDecision.BlockedDangerousAction)
        {
            result = new CallsignCommandExecutionResult(
                false,
                policy.Reason,
                AuditEvent: $"command_blocked:{resolution.PackId}:{resolution.CommandId}",
                PolicyDecision: policy.Decision,
                PolicyApprovalRequirement: policy.ApprovalRequirement,
                PolicyRiskTier: policy.RiskTier);
            return true;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireFreshIdentity)
        {
            result = new CallsignCommandExecutionResult(
                false,
                policy.Reason,
                AuditEvent: $"fresh_identity_required:{resolution.PackId}:{resolution.CommandId}",
                PolicyDecision: policy.Decision,
                PolicyApprovalRequirement: policy.ApprovalRequirement,
                PolicyRiskTier: policy.RiskTier);
            return true;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireApproval && !approvalGranted)
        {
            result = new CallsignCommandExecutionResult(
                false,
                policy.Reason,
                AuditEvent: $"approval_required:{resolution.PackId}:{resolution.CommandId}",
                PolicyDecision: policy.Decision,
                PolicyApprovalRequirement: policy.ApprovalRequirement,
                PolicyRiskTier: policy.RiskTier);
            return true;
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

            if (pack.Module == null)
            {
                result = new CallsignCommandExecutionResult(false, $"Pack '{resolution.PackDisplayName}' is not loaded.");
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

            if (pack.Info.LoadStatus is CallsignPackLoadStatus.InvalidPack
                or CallsignPackLoadStatus.MissingAssembly
                or CallsignPackLoadStatus.MissingPackType
                or CallsignPackLoadStatus.DuplicatePackId
                or CallsignPackLoadStatus.LoadFailure)
            {
                pack.Disabled = true;
                PersistState();
                return false;
            }

            if (pack.Info.RequiresSignature && !IsSignatureSatisfied(pack.Info.SignatureStatus))
            {
                pack.Info = pack.Info with
                {
                    LoadStatus = CallsignPackLoadStatus.SignatureRequired,
                    Message = FormatSignatureRequiredMessage(pack.Info.Tier)
                };
                pack.Disabled = true;
                PersistState();
                return false;
            }

            if (!IsTierEntitled(pack.Info.Tier))
            {
                pack.Info = pack.Info with
                {
                    LoadStatus = CallsignPackLoadStatus.EntitlementRequired,
                    Message = FormatEntitlementRequiredMessage(pack.Info.Tier)
                };
                pack.Disabled = true;
                PersistState();
                return false;
            }

            pack.Info = pack.Info with { LoadStatus = CallsignPackLoadStatus.Loaded, Message = "Enabled." };
            pack.Disabled = false;
            PersistState();
            if (pack.Module == null)
            Refresh();
            return true;
        }
    }

    public bool RemovePack(string packId, out string? message, bool deleteAssemblyFile = true)
    {
        packId = NormalizeKey(packId);
        message = null;

        if (string.IsNullOrWhiteSpace(packId))
        {
            message = "No pack identifier was provided.";
            return false;
        }

        lock (_gate)
        {
            if (!_packs.TryGetValue(packId, out var pack))
            {
                message = "The selected pack is not currently known to Callsign.";
                return false;
            }

            var assemblyPath = pack.Info.AssemblyPath;
            var loadContext = pack.LoadContext;
            pack.Module = null;
            pack.Commands = Array.Empty<CommandRegistration>();
            pack.LoadContext = null;
            _packs.Remove(packId);
            _commands.RemoveAll(command => string.Equals(command.PackId, packId, StringComparison.OrdinalIgnoreCase));
            PersistState();

            if (loadContext != null)
            {
                loadContext.Unload();
                loadContext = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(250);
            }

            if (deleteAssemblyFile && IsManagedAssemblyPath(assemblyPath))
            {
                if (!TryDeleteFileWithRetry(assemblyPath, out var deleteMessage))
                {
                    message = deleteMessage;
                    return false;
                }
            }
        }

        return true;
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

            return new CallsignPackState(
                state.DisabledPackIds ?? Array.Empty<string>(),
                state.DisabledAssemblyPaths ?? Array.Empty<string>());
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
                .ToArray(),
            DisabledAssemblyPaths = _packs.Values
                .Where(pack => pack.Disabled && IsPersistableAssemblyPath(pack.Info.AssemblyPath))
                .Select(pack => NormalizeAssemblyPath(_packRoot, pack.Info.AssemblyPath))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

        File.WriteAllText(_statePath, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
    }

    private IReadOnlyList<PackRegistration> DiscoverPackRegistrations(
        string packRoot,
        HashSet<string> disabledPackIds,
        HashSet<string> disabledAssemblyPaths)
    {
        if (!Directory.Exists(packRoot))
            return Array.Empty<PackRegistration>();

        var registrations = new List<PackRegistration>();
        foreach (var assemblyPath in Directory.EnumerateFiles(packRoot, "*.dll", SearchOption.AllDirectories))
        {
            var assemblyToken = NormalizeAssemblyPath(packRoot, assemblyPath);
            var pack = TryLoadPack(assemblyPath, out var info, out var module, out var commands, out var loadContext);
            if (pack == null || module == null || info == null || commands == null)
            {
                if (disabledAssemblyPaths.Contains(assemblyToken))
                {
                    registrations.Add(new PackRegistration(
                        null,
                        CreateDisabledPlaceholderInfo(assemblyPath, "Disabled before load. Enable this pack to inspect and register its commands."),
                        Array.Empty<CommandRegistration>(),
                        disabled: true));
                }

                continue;
            }

            var invalidOrFailed = info.LoadStatus is not CallsignPackLoadStatus.Loaded;
            var gatedStatus = invalidOrFailed
                ? info.LoadStatus
                : GetGatedLoadStatus(info.Tier, info.RequiresSignature, info.SignatureStatus);
            var gateRequired = gatedStatus != CallsignPackLoadStatus.Loaded;
            var disabled = gateRequired || disabledPackIds.Contains(info.PackId) || disabledAssemblyPaths.Contains(assemblyToken);
            registrations.Add(new PackRegistration(module, info with
            {
                LoadStatus = invalidOrFailed
                    ? info.LoadStatus
                    : gateRequired
                    ? gatedStatus
                    : disabled
                        ? CallsignPackLoadStatus.Disabled
                        : CallsignPackLoadStatus.Loaded,
                Message = invalidOrFailed
                    ? info.Message
                    : gateRequired
                    ? FormatLoadStatusMessage(gatedStatus, info.Tier)
                    : disabled
                        ? "Disabled by user."
                        : "Loaded."
            }, commands, disabled, loadContext));
        }

        return registrations;
    }

    private static PackModule? TryLoadPack(
        string assemblyPath,
        out CallsignPackInfo? info,
        out ICallsignCommandPack? module,
        out IReadOnlyList<CommandRegistration>? commands,
        out CallsignPackAssemblyLoadContext? loadContext)
    {
        info = null;
        module = null;
        commands = null;
        loadContext = null;

        if (!File.Exists(assemblyPath))
            return null;

        try
        {
            loadContext = new CallsignPackAssemblyLoadContext(assemblyPath);
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
            if (!TryValidatePackMetadata(descriptor, commandDefinitions, out var validationMessage))
            {
                info = new CallsignPackInfo(
                    PackId: string.IsNullOrWhiteSpace(packId) ? NormalizeKey(descriptor.DisplayName) : packId,
                    DisplayName: string.IsNullOrWhiteSpace(descriptor.DisplayName) ? "Invalid command pack" : descriptor.DisplayName,
                    Version: string.IsNullOrWhiteSpace(descriptor.Version) ? "0.0.0" : descriptor.Version,
                    Tier: descriptor.Tier,
                    LoadStatus: CallsignPackLoadStatus.InvalidPack,
                    AssemblyPath: assemblyPath,
                    CommandCount: commandDefinitions.Count,
                    Message: validationMessage,
                    LoadedUtc: DateTimeOffset.UtcNow,
                    IsCommunity: descriptor.IsCommunity,
                    SignatureStatus: descriptor.SignatureStatus,
                    RequiresSignature: descriptor.RequiresSignature);
                commands = Array.Empty<CommandRegistration>();
                return new PackModule(module);
            }

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
                LoadedUtc: DateTimeOffset.UtcNow,
                IsCommunity: descriptor.IsCommunity,
                SignatureStatus: descriptor.SignatureStatus,
                RequiresSignature: descriptor.RequiresSignature);
            return new PackModule(module);
        }
        catch (Exception ex)
        {
            loadContext?.Unload();
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

    private static bool TryValidatePackMetadata(
        CallsignPackDescriptor descriptor,
        IReadOnlyList<CallsignCommandDefinition> commands,
        out string message)
    {
        if (string.IsNullOrWhiteSpace(descriptor.PackId))
        {
            message = "Command pack metadata is invalid: PackId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
        {
            message = "Command pack metadata is invalid: DisplayName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(descriptor.Version))
        {
            message = "Command pack metadata is invalid: Version is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(descriptor.Description))
        {
            message = "Command pack metadata is invalid: Description is required.";
            return false;
        }

        if (commands.Count == 0)
        {
            message = "Command pack metadata is invalid: at least one command is required.";
            return false;
        }

        var commandIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command.CommandId))
            {
                message = "Command pack metadata is invalid: every command requires a CommandId.";
                return false;
            }

            if (!commandIds.Add(NormalizeKey(command.CommandId)))
            {
                message = $"Command pack metadata is invalid: duplicate command id '{command.CommandId}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.DisplayName))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires a DisplayName.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Description))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires a Description.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Category))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires a Category.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.HelpText))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires HelpText.";
                return false;
            }

            if (command.Examples == null || !command.Examples.Any(example => !string.IsNullOrWhiteSpace(example)))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires at least one example.";
                return false;
            }

            if (command.VoicePhrases == null || !command.VoicePhrases.Any(phrase => !string.IsNullOrWhiteSpace(phrase)))
            {
                message = $"Command pack metadata is invalid: command '{command.CommandId}' requires at least one voice phrase.";
                return false;
            }

            foreach (var phrase in command.VoicePhrases)
            {
                if (string.IsNullOrWhiteSpace(phrase))
                {
                    message = $"Command pack metadata is invalid: command '{command.CommandId}' has an empty voice phrase.";
                    return false;
                }

                var normalizedPhrase = NormalizeCommand(phrase);
                if (!phrases.Add(normalizedPhrase))
                {
                    message = $"Command pack metadata is invalid: duplicate voice phrase '{phrase}'.";
                    return false;
                }
            }
        }

        message = "Command pack metadata is valid.";
        return true;
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
        public string[]? DisabledAssemblyPaths { get; set; }
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
        public PackRegistration(ICallsignCommandPack? module, CallsignPackInfo info, IReadOnlyList<CommandRegistration> commands, bool disabled, CallsignPackAssemblyLoadContext? loadContext = null)
        {
            Module = module;
            Info = info;
            Commands = commands;
            Disabled = disabled;
            LoadContext = loadContext;
        }

        public ICallsignCommandPack? Module { get; set; }
        public CallsignPackInfo Info { get; set; }
        public IReadOnlyList<CommandRegistration> Commands { get; set; }
        public bool Disabled { get; set; }
        public CallsignPackAssemblyLoadContext? LoadContext { get; set; }
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

        public CallsignCommandResolution ToResolution(string argumentText = "", CallsignPackLoadStatus loadStatus = CallsignPackLoadStatus.Loaded)
        {
            return new CallsignCommandResolution(
                PackId,
                PackDisplayName,
                PackVersion,
                Definition.Tier,
                loadStatus,
                Definition.CommandId,
                Definition.DisplayName,
                argumentText,
                Definition);
        }
    }

    private void AddPackLocked(ICallsignCommandPack? module, string assemblyPath, CallsignPackInfo info, IReadOnlyList<CommandRegistration> commands, bool disabled, CallsignPackAssemblyLoadContext? loadContext = null)
    {
        var pack = new PackRegistration(module, info with
        {
            LoadStatus = disabled && info.LoadStatus == CallsignPackLoadStatus.Loaded ? CallsignPackLoadStatus.Disabled : info.LoadStatus,
            Message = disabled && info.LoadStatus == CallsignPackLoadStatus.Loaded ? "Disabled by user." : info.Message
        }, commands, disabled, loadContext);

        _packs[info.PackId] = pack;
        foreach (var command in commands)
            _commands.Add(command);
    }

    private static CallsignPackInfo CreateDisabledPlaceholderInfo(string assemblyPath, string message)
    {
        var packId = NormalizeKey(Path.GetFileNameWithoutExtension(assemblyPath));
        return new CallsignPackInfo(
            PackId: packId,
            DisplayName: Path.GetFileNameWithoutExtension(assemblyPath),
            Version: "unknown",
            Tier: CallsignPackTier.Free,
            LoadStatus: CallsignPackLoadStatus.Disabled,
            AssemblyPath: assemblyPath,
            CommandCount: 0,
            Message: message,
            LoadedUtc: DateTimeOffset.UtcNow,
            IsCommunity: true,
            WasImported: true,
            SignatureStatus: "unverified",
            RequiresSignature: false);
    }

    private bool IsTierEntitled(CallsignPackTier tier) => _entitlements.Allows(tier);

    private CallsignPackLoadStatus GetGatedLoadStatus(CallsignPackTier tier, bool requiresSignature, string? signatureStatus)
    {
        if (requiresSignature && !IsSignatureSatisfied(signatureStatus))
            return CallsignPackLoadStatus.SignatureRequired;

        if (!IsTierEntitled(tier))
            return CallsignPackLoadStatus.EntitlementRequired;

        return CallsignPackLoadStatus.Loaded;
    }

    private static bool IsSignatureSatisfied(string? signatureStatus) =>
        string.Equals(signatureStatus, "signed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(signatureStatus, "valid", StringComparison.OrdinalIgnoreCase)
        || string.Equals(signatureStatus, "trusted", StringComparison.OrdinalIgnoreCase);

    private static string FormatLoadStatusMessage(CallsignPackLoadStatus status, CallsignPackTier tier) =>
        status switch
        {
            CallsignPackLoadStatus.SignatureRequired => FormatSignatureRequiredMessage(tier),
            CallsignPackLoadStatus.EntitlementRequired => FormatEntitlementRequiredMessage(tier),
            _ => "Loaded."
        };

    private static string FormatEntitlementRequiredMessage(CallsignPackTier tier) =>
        $"{tier} entitlement is required before this command pack can load.";

    private static string FormatSignatureRequiredMessage(CallsignPackTier tier) =>
        $"{tier} command packs that require signing must have a valid signature before they can load.";

    private static bool IsPersistableAssemblyPath(string assemblyPath) =>
        !string.IsNullOrWhiteSpace(assemblyPath)
        && !assemblyPath.StartsWith("<", StringComparison.Ordinal)
        && assemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    private static bool TryDeleteFileWithRetry(string path, out string? message)
    {
        message = null;

        for (var attempt = 1; attempt <= 20; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                    return true;

                File.Delete(path);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                message = $"Unable to delete pack file '{path}': {ex.Message}";
                Thread.Sleep(250);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        return false;
    }

    private bool IsManagedAssemblyPath(string assemblyPath)
    {
        if (!IsPersistableAssemblyPath(assemblyPath))
            return false;

        try
        {
            var fullPackRoot = Path.GetFullPath(_packRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var fullAssemblyPath = Path.GetFullPath(assemblyPath);
            var relativePath = Path.GetRelativePath(fullPackRoot, fullAssemblyPath);
            return !relativePath.StartsWith("..", StringComparison.Ordinal)
                   && !Path.IsPathRooted(relativePath);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeAssemblyPath(string assemblyPath) =>
        NormalizeKey(assemblyPath.Replace('\\', '/'));

    private static string NormalizeAssemblyPath(string packRoot, string assemblyPath)
    {
        try
        {
            var relativePath = Path.GetRelativePath(Path.GetFullPath(packRoot), Path.GetFullPath(assemblyPath));
            if (!relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath))
                return NormalizeAssemblyPath(relativePath);
        }
        catch
        {
        }

        return NormalizeAssemblyPath(Path.GetFileName(assemblyPath));
    }
}
