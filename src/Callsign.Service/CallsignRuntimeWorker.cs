using Callsign.UI.Models;
using Callsign.UI.Services;
using Callsign.Extensions;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Callsign.Service;

public sealed class CallsignRuntimeWorker : BackgroundService
{
    private readonly ProfileStore _profileStore;
    private readonly StartMenuLauncher _launcher;
    private readonly BrowserLaunchService _browserLaunchService;
    private readonly FileSearchService _fileSearchService;
    private readonly SystemControlService _systemControlService;
    private readonly VoiceCommandService _voiceCommandService;
    private readonly AlphaAuditLog _auditLog;
    private readonly VoiceBiometricVerificationService _voiceBiometricVerificationService = new();
    private readonly RuntimeStateStore _stateStore;
    private readonly RuntimeHostOptions _hostOptions;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DateTime _processStartedUtc = DateTime.UtcNow;
    private readonly AlphaSessionStateMachine _session = new();
    private static readonly TimeSpan ExtensionCommandIdentityFreshness = TimeSpan.FromSeconds(90);
    private readonly object _gate = new();
    private UserProfile? _activeProfile;
    private WakeWordDetectionResult? _lastWakeWordDetection;
    private CallsignIdentityResult? _lastIdentityResult;
    private string? _lastTranscriptText;
    private double? _lastTranscriptConfidence;
    private DateTime? _lastTranscriptUpdatedUtc;
    private readonly List<string> _recentTranscriptHistory = [];
    private string? _overlayReadout;
    private readonly List<string> _serviceDictationSegments = [];
    private bool _serviceDictationActive;
    private DateTime? _serviceDictationStartedUtc;
    private DateTime? _serviceDictationUpdatedUtc;
    private string? _requestedUiMode;
    private DateTime? _requestedUiModeUtc;
    private string? _lastServiceActionKind;
    private string? _lastServiceActionTarget;
    private string? _lastServiceActionMessage;
    private bool? _lastServiceActionSucceeded;
    private DateTime? _lastServiceActionUtc;
    private readonly List<RuntimeServiceActionSnapshot> _recentServiceActions = [];
    private string _statusMessage = "Callsign service starting.";

    public CallsignRuntimeWorker(
        ProfileStore profileStore,
        StartMenuLauncher launcher,
        BrowserLaunchService browserLaunchService,
        FileSearchService fileSearchService,
        SystemControlService systemControlService,
        VoiceCommandService voiceCommandService,
        RuntimeStateStore stateStore,
        RuntimeHostOptions hostOptions,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _profileStore = profileStore;
        _launcher = launcher;
        _browserLaunchService = browserLaunchService;
        _fileSearchService = fileSearchService;
        _systemControlService = systemControlService;
        _voiceCommandService = voiceCommandService;
        _auditLog = new AlphaAuditLog(profileStore);
        _stateStore = stateStore;
        _hostOptions = hostOptions;
        _hostApplicationLifetime = hostApplicationLifetime;

        if (_hostOptions.IsUserRuntime)
        {
            _voiceCommandService.TranscriptReceived += VoiceTranscriptReceived;
            _voiceCommandService.WakeWordEvaluated += VoiceWakeWordEvaluated;
            _voiceCommandService.WakeWordDetected += VoiceWakeWordDetected;
            _voiceCommandService.RecognitionError += VoiceRecognitionError;
            _voiceCommandService.ListeningStateChanged += (_, _) => WriteSnapshot();
            _voiceCommandService.SpeechActivityChanged += (_, _) => WriteSnapshot();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_hostOptions.IsUserRuntime)
        {
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            return;
        }

        WriteSnapshot();
        CallsignCommandRegistry.Shared.Refresh();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (RuntimeControlFiles.TryConsumeStopUserRuntimeRequest(_processStartedUtc))
            {
                _statusMessage = "User runtime stop requested by configuration manager.";
                WriteSnapshot();
                _hostApplicationLifetime.StopApplication();
                return;
            }

            RefreshActiveProfile();
            EnsureVoiceRuntime();
            ConsumeClearActionHistoryRequest();
            ConsumeClearTranscriptHistoryRequest();
            ConsumeScriptedTranscriptRequest();
            _session.Tick();
            WriteSnapshot();
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _voiceCommandService.Stop();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private void RefreshActiveProfile()
    {
        var profiles = _profileStore.GetProfiles();
        var preferred = profiles
            .Where(profile => IsVoiceReady(profile.Settings))
            .OrderByDescending(profile => profile.UpdatedUtc)
            .FirstOrDefault()
            ?? profiles.OrderByDescending(profile => profile.UpdatedUtc).FirstOrDefault();

        if (preferred == null)
        {
            lock (_gate)
            {
                if (_activeProfile != null)
                {
                    _activeProfile = null;
                    _session.Reset();
                    _lastIdentityResult = null;
                    _statusMessage = "No account is available. Create a Callsign profile in the UI to begin.";
                }
            }

            return;
        }

        lock (_gate)
        {
            if (_activeProfile?.Callsign == preferred.Callsign)
            {
                _activeProfile = preferred;
                return;
            }

            _activeProfile = preferred;
            _session.Reset();
            _lastIdentityResult = null;
            _statusMessage = $"Active profile set to '{preferred.Callsign}'.";
        }
    }

    private void EnsureVoiceRuntime()
    {
        UserProfile? profile;
        lock (_gate)
            profile = _activeProfile;

        if (profile == null || !IsVoiceReady(profile.Settings))
        {
            if (_voiceCommandService.IsListening)
                _voiceCommandService.Stop();
            return;
        }

        if (_voiceCommandService.IsListening)
            return;

        _voiceCommandService.Start(
            profile.Settings.LanguageCode,
            profile.Settings.WakeWord,
            profile.Callsign,
            profile.Settings.VoiceWakeThreshold,
            profile.Settings.VoiceWakeSensitivity,
            profile.Settings.VoiceWakeDiagnosticsEnabled,
            MicrophoneAudioSettings.From(profile.Settings));
        lock (_gate)
            _statusMessage = $"Listening in the background with {profile.Callsign}.";
    }

    private void VoiceWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)
    {
        lock (_gate)
        {
            _lastWakeWordDetection = e.Result;
            _lastIdentityResult = null;
            if (_activeProfile == null)
            {
                WriteSnapshot();
                return;
            }

            _overlayReadout = FormatOverlayReadout(_session.State);
            _statusMessage = $"Wake word detected by {e.Result.Engine}. Waiting for callsign identity.";

            if (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
            {
                _session.DetectWakeWord(AlphaSessionStateMachine.AudioWakeDetectorSource);
            }

            WriteSnapshot();
        }
    }

    private void VoiceWakeWordEvaluated(object? sender, WakeWordDetectedEventArgs e)
    {
        lock (_gate)
        {
            _lastWakeWordDetection = e.Result;
            WriteSnapshot();
        }
    }

    private void ConsumeScriptedTranscriptRequest()
    {
        if (!RuntimeControlFiles.TryConsumeScriptedTranscriptRequest(out var transcript))
            return;

        lock (_gate)
        {
            if (_activeProfile == null)
            {
                _statusMessage = "Scripted transcript ignored because no active profile is available.";
                WriteSnapshot();
                return;
            }

            if (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
                _session.DetectWakeWord(AlphaSessionStateMachine.ScriptedTranscriptControlSource);
        }

        VoiceTranscriptReceived(
            sender: this,
            e: new VoiceTranscriptEventArgs(transcript, 1.0f));
    }

    private void ConsumeClearActionHistoryRequest()
    {
        if (!RuntimeControlFiles.TryConsumeClearActionHistoryRequest())
            return;

        lock (_gate)
        {
            _recentServiceActions.Clear();
            _lastServiceActionKind = null;
            _lastServiceActionTarget = null;
            _lastServiceActionMessage = null;
            _lastServiceActionSucceeded = null;
            _lastServiceActionUtc = null;
            PersistRecentServiceActions();
            _statusMessage = "Service action history cleared for verification.";
            WriteSnapshot();
        }
    }

    private void ConsumeClearTranscriptHistoryRequest()
    {
        if (!RuntimeControlFiles.TryConsumeClearTranscriptHistoryRequest())
            return;

        lock (_gate)
        {
            _recentTranscriptHistory.Clear();
            _lastTranscriptText = null;
            _lastTranscriptConfidence = null;
            _lastTranscriptUpdatedUtc = null;
            _overlayReadout = FormatOverlayReadout(_session.State);
            _statusMessage = "Recent speech history cleared.";
            WriteSnapshot();
        }
    }

    private void VoiceTranscriptReceived(object? sender, VoiceTranscriptEventArgs e)
    {
        lock (_gate)
        {
            var profile = _activeProfile;
            if (profile == null)
                return;

            if (IsIgnorableSpeechTranscript(e.Text))
                return;

            if (AlphaVoiceTranscriptParser.IsStopListeningCommand(e.Text))
            {
                _voiceCommandService.Stop();
                _statusMessage = "Background listening stopped.";
                _session.Cancel("Voice listener stopped.");
                _lastIdentityResult = null;
                _serviceDictationActive = false;
                WriteSnapshot();
                return;
            }

            if (AlphaVoiceTranscriptParser.IsCancelCommand(e.Text))
            {
                _session.Cancel("Session cancelled.");
                _lastIdentityResult = null;
                _serviceDictationActive = false;
                _statusMessage = _session.StatusMessage;
                WriteSnapshot();
                return;
            }

            if (!_serviceDictationActive && !AcceptsSessionTranscript(_session.State))
            {
                _statusMessage = $"Listening in the background with {profile.Callsign}.";
                WriteSnapshot();
                return;
            }

            _lastTranscriptText = e.Text;
            _lastTranscriptConfidence = Math.Clamp(e.Confidence, 0f, 1f);
            _lastTranscriptUpdatedUtc = DateTime.UtcNow;
            AppendTranscriptHistory(e.Text, _lastTranscriptConfidence.Value, _lastTranscriptUpdatedUtc.Value);
            _overlayReadout = FormatOverlayReadout(
                _session.State,
                e.Text,
                _session.VerifiedCallsign,
                _session.PendingCommand,
                _session.PendingApp,
                _lastIdentityResult?.RetryPrompt,
                _lastTranscriptConfidence,
                speechActive: _voiceCommandService.IsSpeechActive,
                dictationTranscript: _serviceDictationSegments.Count == 0 ? null : string.Join(" ", _serviceDictationSegments),
                dictationActive: _serviceDictationActive);

            if (_serviceDictationActive)
            {
                if (HasServiceDictationExceededDuration())
                {
                    _serviceDictationActive = false;
                    _statusMessage = $"Service dictation reached the {DictationReviewTextService.MaxCaptureSeconds / 60}-minute capture limit. Review text is preserved in the Dictation tab.";
                    RecordServiceAction("dictation", "service dictation", _statusMessage, succeeded: true, profile);
                    RequestUiMode("Dictation");
                    WriteSnapshot();
                    return;
                }

                if (AlphaVoiceTranscriptParser.IsStopDictationCommand(e.Text))
                {
                    _serviceDictationActive = false;
                    _statusMessage = "Service dictation stopped. Review text in the configuration manager Dictation tab.";
                    RecordServiceAction("dictation", "service dictation", _statusMessage, succeeded: true, profile);
                    RequestUiMode("Dictation");
                    WriteSnapshot();
                    return;
                }

                if (!AppendServiceDictation(e.Text, out var boundaryMessage))
                {
                    _serviceDictationActive = false;
                    _statusMessage = boundaryMessage;
                    RecordServiceAction("dictation", "service dictation", _statusMessage, succeeded: true, profile);
                    RequestUiMode("Dictation");
                    WriteSnapshot();
                    return;
                }

                _statusMessage = "Service dictation updated. Say 'stop dictation' when finished.";
                RequestUiMode("Dictation");
                WriteSnapshot();
                return;
            }

            if (_session.State == AlphaSessionState.WaitingForIdentity)
            {
                HandleIdentityTranscript(profile, e.Text, e.Confidence, e.CapturedAudioPath);
                WriteSnapshot();
                return;
            }

            if (e.Confidence < 0.45f)
            {
                _statusMessage = "Heard speech, but confidence was too low. Waiting for a clearer phrase.";
                WriteSnapshot();
                return;
            }

            var wakeWord = string.IsNullOrWhiteSpace(profile.Settings.WakeWord) ? "Callsign" : profile.Settings.WakeWord;

            var intent = AlphaVoiceIntentParser.ParseVerifiedTranscript(e.Text, wakeWord, profile.Callsign);
            var normalizedCommand = intent.NormalizedCommand;

            if (_session.State == AlphaSessionState.WaitingForCommand && !string.IsNullOrWhiteSpace(normalizedCommand))
            {
                _session.TryCaptureCommand(normalizedCommand, out _statusMessage);
                ExecuteVerifiedCommand(profile, intent);
            }

            WriteSnapshot();
        }
    }

    private static bool AcceptsSessionTranscript(AlphaSessionState state) =>
        state is AlphaSessionState.WaitingForIdentity
            or AlphaSessionState.WaitingForCommand
            or AlphaSessionState.ReadyToLaunch
            or AlphaSessionState.Launching;

    private static bool IsIgnorableSpeechTranscript(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return true;

        var trimmed = transcript.Trim();
        return trimmed.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("BLANK_AUDIO", StringComparison.OrdinalIgnoreCase);
    }

    private void HandleIdentityTranscript(UserProfile profile, string transcript, float confidence, string? capturedAudioPath)
    {
        var threshold = Math.Max(
            CallsignIdentityMatcher.DefaultConfidenceThreshold,
            profile.Settings.VoiceCommandConfidenceThreshold);
        var biometric = _voiceBiometricVerificationService.Verify(
            _profileStore,
            profile,
            capturedAudioPath,
            profile.Settings.VoiceBiometricThreshold,
            TimeSpan.FromSeconds(Math.Clamp(profile.Settings.VoiceBiometricMaxCandidateAgeSeconds, 5, 300)));
        var result = CallsignIdentityMatcher.Evaluate(
            transcript,
            confidence,
            profile.Callsign,
            profile.Settings.VoiceCallsignAliases,
            threshold,
            biometric,
            profile.Settings.VoiceBiometricRequired,
            profile.Settings.VoiceBiometricNearMatchThreshold);

        _lastIdentityResult = result;
        if (result.Accepted)
        {
            _session.TryVerifyIdentity(result, profile.Callsign, IsVoiceReady(profile.Settings), profile.Settings.VoiceBiometricRequired, out _statusMessage);
            _overlayReadout = FormatOverlayReadout(
                _session.State,
                result.MatchedVariant ?? profile.Callsign,
                _session.VerifiedCallsign,
                _session.PendingCommand,
                _session.PendingApp,
                result.RetryPrompt,
                confidence,
                speechActive: _voiceCommandService.IsSpeechActive,
                dictationTranscript: _serviceDictationSegments.Count == 0 ? null : string.Join(" ", _serviceDictationSegments),
                dictationActive: _serviceDictationActive);
            return;
        }

        if (string.Equals(result.RejectReason, "identity_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            _session.TryVerifyIdentity(result, profile.Callsign, IsVoiceReady(profile.Settings), profile.Settings.VoiceBiometricRequired, out _statusMessage);
            _overlayReadout = FormatOverlayReadout(
                _session.State,
                transcript,
                _session.VerifiedCallsign,
                _session.PendingCommand,
                _session.PendingApp,
                result.RetryPrompt,
                confidence,
                speechActive: _voiceCommandService.IsSpeechActive,
                dictationTranscript: _serviceDictationSegments.Count == 0 ? null : string.Join(" ", _serviceDictationSegments),
                dictationActive: _serviceDictationActive);
            return;
        }

        _overlayReadout = FormatOverlayReadout(
            _session.State,
            transcript,
            _session.VerifiedCallsign,
            _session.PendingCommand,
            _session.PendingApp,
            result.RetryPrompt,
            confidence,
            speechActive: _voiceCommandService.IsSpeechActive,
            dictationTranscript: _serviceDictationSegments.Count == 0 ? null : string.Join(" ", _serviceDictationSegments),
            dictationActive: _serviceDictationActive);
        _statusMessage = result.RetryPrompt ?? "Say your callsign again.";
    }

    private void ExecuteVerifiedCommand(
        UserProfile profile,
        AlphaVoiceIntent intent,
        bool completeSession = true,
        int shortcutDepth = 0,
        HashSet<string>? activeExtensionCommands = null)
    {
        _overlayReadout = FormatOverlayReadout(
            _session.State,
            intent.NormalizedCommand,
            _session.VerifiedCallsign,
            _session.PendingCommand,
            _session.PendingApp);

        if (TryExecuteBrowserCommand(intent, profile, out var browserMessage, out var browserSucceeded))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = browserMessage;
            RecordServiceAction("browser", intent.Target, browserMessage, succeeded: browserSucceeded, profile);
            return;
        }

        if (TryExecuteFileSearchCommand(intent, profile, out var fileSearchMessage, out var fileSearchSucceeded))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = fileSearchMessage;
            RecordServiceAction("file_search", intent.Target, fileSearchMessage, succeeded: fileSearchSucceeded, profile);
            return;
        }

        if (TryExecuteSystemControlCommand(intent, profile, out var systemMessage, out var systemSucceeded))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = systemMessage;
            RecordServiceAction("system", intent.Target, systemMessage, succeeded: systemSucceeded, profile);
            return;
        }

        if (TryExecuteDictationCommand(intent, profile, out var dictationMessage, out var dictationSucceeded))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = dictationMessage;
            RecordServiceAction("dictation", "service dictation", dictationMessage, succeeded: dictationSucceeded, profile);
            return;
        }

        if (TryExecuteUiNavigationCommand(intent, out var uiNavigationMessage))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = uiNavigationMessage;
            RecordServiceAction("ui_navigation", intent.Target, uiNavigationMessage, succeeded: true, profile);
            return;
        }

        if (TryExecuteUiActionCommand(intent, out var uiActionMessage))
        {
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = uiActionMessage;
            RecordServiceAction("ui_action", intent.Target, uiActionMessage, succeeded: true, profile);
            return;
        }

        if (TryExecuteExtensionCommand(intent, profile, out var extensionResult))
        {
            if (extensionResult.Succeeded && extensionResult.FollowUpSteps is { Count: > 0 })
            {
                activeExtensionCommands ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var commandLabelKey = $"{intent.PackId}/{intent.Target}";
                if (!activeExtensionCommands.Add(commandLabelKey))
                {
                    extensionResult = extensionResult with
                    {
                        Succeeded = false,
                        Message = "Service voice shortcut execution detected a loop and was blocked."
                    };
                }
                else
                {
                    extensionResult = ExecuteServiceVoiceShortcutFollowUpSteps(
                        profile,
                        intent,
                        extensionResult,
                        shortcutDepth + 1,
                        activeExtensionCommands);
                    activeExtensionCommands.Remove(commandLabelKey);
                }
            }

            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = extensionResult.Message;
            RecordServiceAction("extension_command", $"{intent.PackId}/{intent.Target}", extensionResult.Message, succeeded: extensionResult.Succeeded, profile);
            return;
        }

        var appName = _session.PendingApp;
        if (string.IsNullOrWhiteSpace(appName))
            appName = intent.Kind == AlphaVoiceIntentKind.StartMenuLaunch
                ? intent.Target
                : AlphaVoiceTranscriptParser.InferAppName(intent.NormalizedCommand);

        if (string.IsNullOrWhiteSpace(appName))
            return;

        _overlayReadout = FormatOverlayReadout(
            AlphaSessionState.Launching,
            pendingApp: _session.PendingApp ?? appName,
            pendingCommand: _session.PendingCommand);
        _session.TryBeginLaunch(appName, out _statusMessage);
        if (_launcher.Launch(_session.PendingApp ?? appName, out var launchMessage))
        {
            profile.Settings.LastLaunchedApp = _session.PendingApp ?? appName;
            _profileStore.Save(profile);
            if (completeSession)
                _session.CompleteLaunch();
            _statusMessage = launchMessage;
            RecordServiceAction("start_menu_launch", _session.PendingApp ?? appName, launchMessage, succeeded: true, profile);
        }
        else
        {
            _session.FailLaunch(launchMessage);
            _statusMessage = launchMessage;
            RecordServiceAction("start_menu_launch", _session.PendingApp ?? appName, launchMessage, succeeded: false, profile);
        }
    }

    private CallsignCommandExecutionResult ExecuteServiceVoiceShortcutFollowUpSteps(
        UserProfile profile,
        AlphaVoiceIntent sourceIntent,
        CallsignCommandExecutionResult baseResult,
        int shortcutDepth,
        HashSet<string> activeExtensionCommands)
    {
        const int maxServiceShortcutDepth = 4;
        if (shortcutDepth > maxServiceShortcutDepth)
        {
            return baseResult with
            {
                Succeeded = false,
                Message = "Service voice shortcut execution exceeded the nesting limit and was blocked."
            };
        }

        RequestUiMode("Shortcuts");
        var steps = baseResult.FollowUpSteps ?? Array.Empty<CallsignFollowUpStep>();
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            switch (step.Kind)
            {
                case CallsignFollowUpStepKind.Wait:
                    var waitMilliseconds = Math.Clamp(
                        step.DurationMilliseconds,
                        VoiceShortcutConstants.MinWaitMilliseconds,
                        VoiceShortcutConstants.MaxWaitMilliseconds);
                    _statusMessage = $"Service voice shortcut '{sourceIntent.Target}' waiting {waitMilliseconds} ms before the next visible step.";
                    WriteSnapshot();
                    Thread.Sleep(waitMilliseconds);
                    break;
                case CallsignFollowUpStepKind.Command:
                    var spokenCommand = step.Value?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(spokenCommand))
                    {
                        return baseResult with
                        {
                            Succeeded = false,
                            Message = $"Service voice shortcut '{sourceIntent.Target}' contains an empty command step."
                        };
                    }

                    if (string.IsNullOrWhiteSpace(profile.Callsign))
                    {
                        return baseResult with
                        {
                            Succeeded = false,
                            Message = "Select an account before running service voice shortcuts."
                        };
                    }

                    _statusMessage = $"Service voice shortcut '{sourceIntent.Target}' running step {index + 1} of {steps.Count}: {spokenCommand}";
                    var wakeWord = string.IsNullOrWhiteSpace(profile.Settings.WakeWord)
                        ? "Callsign"
                        : profile.Settings.WakeWord;
                    var transcript = $"{wakeWord} {profile.Callsign} {spokenCommand}";
                    var intent = AlphaVoiceIntentParser.ParseVerifiedTranscript(transcript, wakeWord, profile.Callsign);
                    if (string.IsNullOrWhiteSpace(intent.NormalizedCommand))
                    {
                        return baseResult with
                        {
                            Succeeded = false,
                            Message = $"Service voice shortcut step '{spokenCommand}' could not be parsed."
                        };
                    }

                    ExecuteVerifiedCommand(
                        profile,
                        intent,
                        completeSession: false,
                        shortcutDepth: shortcutDepth,
                        activeExtensionCommands: activeExtensionCommands);

                    if (_lastServiceActionSucceeded == false)
                    {
                        return baseResult with
                        {
                            Succeeded = false,
                            Message = $"Service voice shortcut step '{spokenCommand}' did not complete: {_lastServiceActionMessage}"
                        };
                    }

                    break;
            }
        }

        return baseResult with
        {
            Message = $"Service voice shortcut '{sourceIntent.Target}' completed {steps.Count} visible step(s)."
        };
    }

    private bool TryExecuteBrowserCommand(AlphaVoiceIntent intent, UserProfile profile, out string message, out bool succeeded)
    {
        message = string.Empty;
        succeeded = false;
        if (intent.Kind != AlphaVoiceIntentKind.Browser)
            return false;

        if (!TryAuthorizeServiceBuiltInIntent(intent, profile, out message))
        {
            RequestUiMode("Browser");
            return true;
        }

        RequestUiMode("Browser");
        if (string.IsNullOrWhiteSpace(intent.Target))
        {
            message = "Browser command heard, but no website or search phrase was captured.";
            return true;
        }

        if (IsBrowserActionTarget(intent.Target))
        {
            succeeded = _browserLaunchService.TryExecuteBrowserAction(intent.Target, out message);
            return true;
        }

        if (_browserLaunchService.TryOpen(intent.Target, out message, out _, browserTarget: intent.BrowserTarget))
        {
            succeeded = true;
            return true;
        }

        return true;
    }

    private static bool IsBrowserActionTarget(string? target) =>
        !string.IsNullOrWhiteSpace(target)
        && target.Trim().StartsWith("browser-", StringComparison.OrdinalIgnoreCase);

    private bool TryExecuteFileSearchCommand(AlphaVoiceIntent intent, UserProfile profile, out string message, out bool succeeded)
    {
        message = string.Empty;
        succeeded = false;
        if (intent.Kind != AlphaVoiceIntentKind.FileSearch)
            return false;

        if (!TryAuthorizeServiceBuiltInIntent(intent, profile, out message))
        {
            RequestUiMode("Files");
            return true;
        }

        RequestUiMode("Files");
        if (string.IsNullOrWhiteSpace(intent.Target))
        {
            message = "File search command heard, but no file or folder name was captured.";
            return true;
        }

        var report = _fileSearchService.Search(intent.Target, maxResults: 5);
        if (report.Results.Count == 0)
        {
            var noResultMessage = $"No file or folder results matched '{intent.Target}' using {report.SearchEngine} search.";
            message = report.Warnings.Count == 0
                ? noResultMessage
                : $"{noResultMessage} Warnings: {string.Join(" ", report.Warnings)}";
            return true;
        }

        var best = report.Results[0];
        if (_fileSearchService.TryOpen(best, out var openMessage))
        {
            openMessage = $"{openMessage} Search engine: {report.SearchEngine}.";
            message = report.Warnings.Count == 0
                ? openMessage
                : $"{openMessage} Warnings: {string.Join(" ", report.Warnings)}";
            succeeded = true;
            return true;
        }

        message = openMessage;
        return true;
    }

    private bool TryExecuteSystemControlCommand(AlphaVoiceIntent intent, UserProfile profile, out string message, out bool succeeded)
    {
        message = string.Empty;
        succeeded = false;
        if (intent.Kind != AlphaVoiceIntentKind.SystemControl)
            return false;

        if (!TryAuthorizeServiceBuiltInIntent(intent, profile, out message))
        {
            RequestUiMode("System");
            return true;
        }

        RequestUiMode("System");
        var action = intent.Target?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(action))
        {
            message = "System command heard, but no visible system action was captured.";
            return true;
        }

        if (action.StartsWith("system-switch-window:", StringComparison.OrdinalIgnoreCase))
        {
            var requestedWindow = action["system-switch-window:".Length..].Trim();
            var resolution = _systemControlService.ResolveVisibleWindow(requestedWindow, ignoredProcessId: Environment.ProcessId);
            if (resolution.IsAmbiguous)
            {
                message = $"{resolution.Message} Open the System tab to choose the visible numbered window.";
                return true;
            }

            if (!resolution.IsResolved || resolution.SelectedCandidate == null)
            {
                message = resolution.Message;
                return true;
            }

            succeeded = _systemControlService.TryActivateVisibleWindow(resolution.SelectedCandidate.Handle, out var switchMessage);
            message = succeeded
                ? FormatServiceSystemVisibleStatus(action, switchMessage)
                : switchMessage;
            return true;
        }

        succeeded = _systemControlService.TryExecute(action, out var actionMessage);
        message = succeeded
            ? FormatServiceSystemVisibleStatus(action, actionMessage)
            : actionMessage;
        return true;
    }

    private bool TryAuthorizeServiceBuiltInIntent(AlphaVoiceIntent intent, UserProfile profile, out string message)
    {
        var definition = CreateServiceBuiltInCommandDefinition(intent);
        var identityVerified = string.Equals(_session.VerifiedCallsign, profile.Callsign, StringComparison.OrdinalIgnoreCase);
        var freshIdentity = _session.HasFreshIdentity(UpdateCheckService.DefaultIdentityFreshness);
        var policy = CallsignCommandPolicy.Evaluate(definition, identityVerified, freshIdentity);

        if (policy.Decision is CallsignPolicyDecision.BlockedDangerousAction or CallsignPolicyDecision.Deny)
        {
            message = policy.Reason;
            return false;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireFreshIdentity)
        {
            message = policy.Reason;
            return false;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireApproval)
        {
            message = $"{definition.Kind} command '{definition.DisplayName}' requires visible approval in the visible Callsign surface before execution.";
            return false;
        }

        message = policy.Reason;
        return true;
    }

    private static CallsignCommandDefinition CreateServiceBuiltInCommandDefinition(AlphaVoiceIntent intent)
    {
        var kind = intent.Kind switch
        {
            AlphaVoiceIntentKind.Browser => CallsignCommandKind.Browser,
            AlphaVoiceIntentKind.FileSearch => CallsignCommandKind.FileSearch,
            AlphaVoiceIntentKind.Dictation => CallsignCommandKind.Dictation,
            AlphaVoiceIntentKind.SystemControl => CallsignCommandKind.SystemControl,
            AlphaVoiceIntentKind.UiAction => CallsignCommandKind.UiAction,
            AlphaVoiceIntentKind.StartMenuLaunch => CallsignCommandKind.StartMenuLaunch,
            _ => CallsignCommandKind.Extension
        };

        var risk = intent.Kind switch
        {
            AlphaVoiceIntentKind.FileSearch => CallsignCommandRiskTier.Observe,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("print", StringComparison.OrdinalIgnoreCase) => CallsignCommandRiskTier.LocalStateChange,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("close-window", StringComparison.OrdinalIgnoreCase) => CallsignCommandRiskTier.LocalStateChange,
            AlphaVoiceIntentKind.SystemControl => CallsignCommandRiskTier.LocalReversible,
            AlphaVoiceIntentKind.Dictation => CallsignCommandRiskTier.LocalStateChange,
            _ => CallsignCommandRiskTier.LocalReversible
        };

        var privacy = intent.Kind switch
        {
            AlphaVoiceIntentKind.FileSearch => CallsignCommandPrivacyImpact.FilePath,
            AlphaVoiceIntentKind.Dictation => CallsignCommandPrivacyImpact.Clipboard,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("snipping-toolbar", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.ScreenshotOrOcr,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("clipboard-history", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.Clipboard,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("copy", StringComparison.OrdinalIgnoreCase)
                || intent.Target.Contains("paste", StringComparison.OrdinalIgnoreCase)
                || intent.Target.Contains("cut", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.Clipboard,
            _ => CallsignCommandPrivacyImpact.WindowTitleOrProcess
        };

        var approval = intent.Kind switch
        {
            AlphaVoiceIntentKind.Dictation => CallsignCommandApprovalRequirement.RequireFreshIdentity,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("snipping-toolbar", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("clipboard-history", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("print", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("close-window", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            _ => CallsignCommandApprovalRequirement.None
        };

        var actionTarget = string.IsNullOrWhiteSpace(intent.Target) ? intent.NormalizedCommand : intent.Target;
        return new CallsignCommandDefinition(
            CommandId: $"builtin.{intent.Kind.ToString().ToLowerInvariant()}",
            DisplayName: FormatServiceSystemVisibleStatus(actionTarget, $"{actionTarget} requested."),
            VoicePhrases: [intent.NormalizedCommand],
            Description: "Built-in free Voice Access parity command.",
            Kind: kind,
            Tier: CallsignPackTier.Free,
            RiskTier: risk,
            VisibleAction: true,
            Target: actionTarget,
            Category: intent.Kind.ToString(),
            PrivacyImpact: privacy,
            ApprovalRequirement: approval,
            HelpText: "Built-in Callsign command executed only after wake and identity verification.",
            Examples: [intent.NormalizedCommand],
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus);
    }

    private static string FormatServiceSystemVisibleStatus(string action, string statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
            return string.Empty;

        var trimmedMessage = statusMessage.Trim();
        if (!trimmedMessage.EndsWith("requested.", StringComparison.OrdinalIgnoreCase))
            return trimmedMessage;

        var baseLabel = trimmedMessage[..^"requested.".Length].Trim();
        if (string.IsNullOrWhiteSpace(baseLabel))
            return "System action completed visibly.";

        return $"{baseLabel} requested visibly through the System surface.";
    }

    private bool TryExecuteDictationCommand(AlphaVoiceIntent intent, UserProfile profile, out string message, out bool succeeded)
    {
        message = string.Empty;
        succeeded = false;
        if (intent.Kind != AlphaVoiceIntentKind.Dictation)
            return false;

        if (!TryAuthorizeServiceBuiltInIntent(intent, profile, out message))
        {
            RequestUiMode("Dictation");
            return true;
        }

        _serviceDictationActive = true;
        _serviceDictationSegments.Clear();
        _serviceDictationStartedUtc = DateTime.UtcNow;
        _serviceDictationUpdatedUtc = DateTime.UtcNow;
        RequestUiMode("Dictation");
        message = $"Service dictation started. Speak naturally, then say 'stop dictation' when finished. Capture is bounded to {DictationReviewTextService.MaxCaptureSeconds / 60} minutes and {DictationReviewTextService.MaxReviewCharacters:N0} reviewed characters.";
        succeeded = true;
        return true;
    }

    private bool TryExecuteUiNavigationCommand(AlphaVoiceIntent intent, out string message)
    {
        message = string.Empty;
        if (intent.Kind != AlphaVoiceIntentKind.UiNavigation)
            return false;

        if (string.IsNullOrWhiteSpace(intent.Target))
        {
            message = "Voice navigation heard, but no tab name was captured.";
            return true;
        }

        RequestUiMode(intent.Target);
        message = $"Opening {intent.Target} tab.";
        return true;
    }

    private bool TryExecuteUiActionCommand(AlphaVoiceIntent intent, out string message)
    {
        message = string.Empty;
        if (intent.Kind != AlphaVoiceIntentKind.UiAction)
            return false;

        if (string.IsNullOrWhiteSpace(intent.Target))
        {
            message = "Voice action heard, but no action target was captured.";
            return true;
        }

        if (intent.Target.StartsWith("ui-blocked-external-side-effect", StringComparison.OrdinalIgnoreCase))
        {
            RequestUiMode(intent.Target);
            message = "Blocked external side effect. Callsign will not submit, send, upload, post, pay, accept terms, or run downloaded software from an alpha voice command.";
            return true;
        }

        RequestUiMode(intent.Target);
        message = $"Opening {intent.Target} action.";
        return true;
    }

    private bool TryExecuteExtensionCommand(AlphaVoiceIntent intent, UserProfile profile, out CallsignCommandExecutionResult result)
    {
        result = new CallsignCommandExecutionResult(false, string.Empty);
        if (intent.Kind != AlphaVoiceIntentKind.ExtensionCommand)
            return false;

        var context = new CallsignCommandExecutionContext(
            intent.PackId,
            intent.Target,
            intent.NormalizedCommand,
            intent.NormalizedCommand,
            intent.ArgumentText,
            profile.Callsign,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var identityVerified = string.Equals(_session.VerifiedCallsign, profile.Callsign, StringComparison.OrdinalIgnoreCase);
        var freshIdentityVerified = _session.HasFreshIdentity(ExtensionCommandIdentityFreshness);

        if (!CallsignCommandRegistry.Shared.TryExecute(
            context,
            out result,
            identityVerified: identityVerified,
            freshIdentityVerified: freshIdentityVerified,
            approvalGranted: false))
        {
            result = new CallsignCommandExecutionResult(false, "No extension pack command matched the spoken phrase.");
            return true;
        }

        return true;
    }

    private bool AppendServiceDictation(string text, out string boundaryMessage)
    {
        boundaryMessage = string.Empty;
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return true;

        if (_serviceDictationSegments.Count >= DictationReviewTextService.MaxServiceDictationSegments)
        {
            boundaryMessage = $"Service dictation reached the {DictationReviewTextService.MaxServiceDictationSegments}-segment capture limit. Review text is preserved in the Dictation tab.";
            return false;
        }

        var currentTextLength = string.Join(" ", _serviceDictationSegments).Length;
        var separatorLength = currentTextLength == 0 ? 0 : 1;
        var remainingCharacters = DictationReviewTextService.MaxReviewCharacters - currentTextLength - separatorLength;
        if (remainingCharacters <= 0)
        {
            boundaryMessage = $"Service dictation reached the {DictationReviewTextService.MaxReviewCharacters:N0}-character review limit. Review text is preserved in the Dictation tab.";
            return false;
        }

        if (normalized.Length > remainingCharacters)
        {
            _serviceDictationSegments.Add(normalized[..remainingCharacters]);
            _serviceDictationUpdatedUtc = DateTime.UtcNow;
            boundaryMessage = $"Service dictation reached the {DictationReviewTextService.MaxReviewCharacters:N0}-character review limit. Review text is preserved in the Dictation tab.";
            return false;
        }

        _serviceDictationSegments.Add(normalized);
        _serviceDictationUpdatedUtc = DateTime.UtcNow;
        return true;
    }

    private bool HasServiceDictationExceededDuration() =>
        _serviceDictationStartedUtc.HasValue
        && DateTime.UtcNow - _serviceDictationStartedUtc.Value >= TimeSpan.FromSeconds(DictationReviewTextService.MaxCaptureSeconds);

    private void RequestUiMode(string mode)
    {
        _requestedUiMode = mode;
        _requestedUiModeUtc = DateTime.UtcNow;
    }

    private void RecordServiceAction(string kind, string? target, string message, bool succeeded, UserProfile? profile = null)
    {
        var visibleMessage = message;
        var auditWarning = RecordServiceAudit(profile, kind, target, message, succeeded);
        if (!string.IsNullOrWhiteSpace(auditWarning))
            visibleMessage = $"{message} Audit warning: {auditWarning}";

        _lastServiceActionKind = kind;
        _lastServiceActionTarget = target;
        _lastServiceActionMessage = visibleMessage;
        _lastServiceActionSucceeded = succeeded;
        _lastServiceActionUtc = DateTime.UtcNow;
        if (string.Equals(_statusMessage, message, StringComparison.Ordinal))
            _statusMessage = visibleMessage;
        _recentServiceActions.Add(new RuntimeServiceActionSnapshot(kind, target, visibleMessage, succeeded, _lastServiceActionUtc.Value));
        if (_recentServiceActions.Count > 20)
            _recentServiceActions.RemoveRange(0, _recentServiceActions.Count - 20);
        PersistRecentServiceActions();
    }

    private string? RecordServiceAudit(UserProfile? profile, string kind, string? target, string message, bool succeeded)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.Callsign))
            return null;

        return _auditLog.TryRecordCommand(
            profile,
            eventType: "alpha.service_command_execution",
            actionName: $"service_{kind}",
            status: succeeded ? "succeeded" : "blocked_or_failed",
            out _,
            commandFamily: kind,
            actionTarget: target,
            details: message,
            success: succeeded,
            verificationMethod: "visible_status",
            verificationSummary: succeeded
                ? "Service command reached a visible Callsign status surface after wake and identity verification."
                : "Service command did not execute or completed unsuccessfully; visible Callsign status captured the reason.",
            auditSource: "service_runtime")
            ? null
            : "Service audit logging failed; review profile storage and disk permissions before trusting this action history.";
    }

    private static string FormatOverlayReadout(
        AlphaSessionState state,
        string? transcript = null,
        string? verifiedCallsign = null,
        string? pendingCommand = null,
        string? pendingApp = null,
        string? identityRetryPrompt = null,
        double? transcriptConfidence = null,
        bool speechActive = false,
        string? dictationTranscript = null,
        bool dictationActive = false)
    {
        var readout = OverlayReadoutFormatter.FormatReadout(
            state,
            transcript,
            transcriptConfidence.HasValue ? (float?)transcriptConfidence.Value : null,
            verifiedCallsign,
            pendingCommand,
            pendingApp,
            identityRetryPrompt,
            speechActive: speechActive,
            dictationTranscript: dictationTranscript,
            dictationActive: dictationActive);

        if (!transcriptConfidence.HasValue)
            return readout;

        var confidenceText = $"{Math.Clamp(transcriptConfidence.Value, 0d, 1d):P0}";
        if (string.IsNullOrWhiteSpace(transcript))
            return readout;

        if (readout.StartsWith("Heard:", StringComparison.OrdinalIgnoreCase))
            return $"{readout} ({confidenceText})";

        if (readout.StartsWith("Command:", StringComparison.OrdinalIgnoreCase))
            return $"{readout} ({confidenceText})";

        if (readout.StartsWith("Launching", StringComparison.OrdinalIgnoreCase))
            return $"{readout} ({confidenceText})";

        return readout;
    }

    private void PersistRecentServiceActions()
    {
        try
        {
            var runtimeDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Callsign",
                "Runtime");
            Directory.CreateDirectory(runtimeDir);
            File.WriteAllText(
                Path.Combine(runtimeDir, "recent-service-actions.json"),
                JsonSerializer.Serialize(_recentServiceActions));
        }
        catch
        {
            // Action-history persistence is best-effort; state.json still carries the in-memory snapshot.
        }
    }

    private void VoiceRecognitionError(object? sender, VoiceRecognitionErrorEventArgs e)
    {
        lock (_gate)
        {
            _statusMessage = e.Message;
            WriteSnapshot();
        }
    }

    private void AppendTranscriptHistory(string transcript, double confidence, DateTime updatedUtc)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return;

        var entry = $"[{updatedUtc.ToLocalTime():t}] {transcript.Trim()} ({confidence:P0})";
        if (_recentTranscriptHistory.Count > 0
            && string.Equals(_recentTranscriptHistory[0], entry, StringComparison.OrdinalIgnoreCase))
            return;

        _recentTranscriptHistory.Insert(0, entry);
        while (_recentTranscriptHistory.Count > 8)
            _recentTranscriptHistory.RemoveAt(_recentTranscriptHistory.Count - 1);
    }

    private void WriteSnapshot()
    {
        UserProfile? profile;
        lock (_gate)
            profile = _activeProfile;

        var telemetry = _voiceCommandService.CurrentAudioTelemetry;
        var lastAudioPacketUtc = _voiceCommandService.LastAudioPacketUtc ?? telemetry?.Utc;
        var secondsSinceLastAudioPacket = lastAudioPacketUtc.HasValue
            ? (DateTime.UtcNow - lastAudioPacketUtc.Value).TotalSeconds
            : (double?)null;
        var canHearAudio = _voiceCommandService.IsListening
            && lastAudioPacketUtc.HasValue
            && telemetry != null
            && (telemetry.RawPeak > 0.001
                || telemetry.RawRms > 0.001
                || telemetry.ProcessedPeak > 0.001
                || telemetry.ProcessedRms > 0.001)
            && (!secondsSinceLastAudioPacket.HasValue || secondsSinceLastAudioPacket.Value <= 2.5);

        var snapshot = new RuntimeStateSnapshot(
            ServiceState: _voiceCommandService.IsListening ? "Listening" : "Idle",
            RuntimeRole: _hostOptions.RuntimeRole,
            StatusMessage: _statusMessage,
            ActiveCallsign: profile?.Callsign,
            VerifiedCallsign: _session.VerifiedCallsign,
            PendingCommand: _session.PendingCommand,
            PendingApp: _session.PendingApp,
            LastLaunchedApp: profile?.Settings.LastLaunchedApp,
            IsListening: _voiceCommandService.IsListening,
            ModeDescription: _voiceCommandService.CurrentModeDescription,
            UpdatedUtc: DateTime.UtcNow,
            SessionState: _session.State.ToString(),
            LastTranscriptText: _lastTranscriptText,
            LastTranscriptConfidence: _lastTranscriptConfidence,
            LastTranscriptUpdatedUtc: _lastTranscriptUpdatedUtc,
            RecentTranscriptHistory: _recentTranscriptHistory.ToArray(),
            OverlayReadout: _overlayReadout,
            CurrentWakeWordEngine: _voiceCommandService.CurrentWakeWordEngine,
            CurrentProcessId: Environment.ProcessId,
            ProcessStartedUtc: _processStartedUtc,
            ActiveMicrophoneDeviceName: _voiceCommandService.ActiveCaptureDeviceName,
            LastAudioPacketUtc: lastAudioPacketUtc,
            CanHearAudio: canHearAudio,
            SecondsSinceLastAudioPacket: secondsSinceLastAudioPacket,
            RuntimeAuthorityStatus: _hostOptions.IsUserRuntime ? "authoritative-user-runtime" : "windows-service-supervisor",
            IsAuthoritativeUserRuntime: _hostOptions.IsUserRuntime,
            LastWakeWordEngine: _lastWakeWordDetection?.Engine,
            LastWakeWordScore: _lastWakeWordDetection?.Score,
            WakeWordThreshold: _lastWakeWordDetection?.Threshold,
            LastWakeTransitionSource: _session.LastWakeTransitionSource,
            WakeWordAudioQualityWarnings: _lastWakeWordDetection?.AudioQualityWarnings,
            IsSpeechActive: _voiceCommandService.IsSpeechActive,
            LastSpeechActivityUtc: _voiceCommandService.LastSpeechActivityUtc,
            LastMicrophoneLevelState: _voiceCommandService.CurrentAudioTelemetry?.LevelState,
            LastMicrophoneRawRms: _voiceCommandService.CurrentAudioTelemetry?.RawRms,
            LastMicrophonePeak: _voiceCommandService.CurrentAudioTelemetry?.RawPeak,
            LastMicrophoneGainDb: _voiceCommandService.CurrentAudioTelemetry?.AppliedGainDb,
            LastMicrophoneNoiseFloorRms: _voiceCommandService.CurrentAudioTelemetry?.NoiseFloorRms,
            LastMicrophoneSpeechThresholdRms: _voiceCommandService.CurrentAudioTelemetry?.SpeechThresholdRms,
            LastMicrophoneClippingRatio: _voiceCommandService.CurrentAudioTelemetry?.ClippingRatio,
            LastMicrophoneWarnings: _voiceCommandService.CurrentAudioTelemetry?.Warnings,
            ServiceDictationActive: _serviceDictationActive,
            ServiceDictationText: _serviceDictationSegments.Count == 0 ? null : string.Join(" ", _serviceDictationSegments),
            ServiceDictationUpdatedUtc: _serviceDictationUpdatedUtc,
            ServiceDictationHistory: _serviceDictationSegments.ToArray(),
            RequestedUiMode: _requestedUiMode,
            RequestedUiModeUtc: _requestedUiModeUtc,
            LastIdentityTranscript: _lastIdentityResult?.Transcript,
            LastIdentityAccepted: _lastIdentityResult?.Accepted,
            LastIdentityMatchedVariant: _lastIdentityResult?.MatchedVariant,
            LastIdentityConfidence: _lastIdentityResult?.Confidence,
            LastIdentityRejectReason: _lastIdentityResult?.RejectReason,
            LastIdentityRetryPrompt: _lastIdentityResult?.RetryPrompt,
            LastIdentityBiometricAccepted: _lastIdentityResult?.Biometric?.Accepted,
            LastIdentityBiometricScore: _lastIdentityResult?.Biometric?.Score,
            LastIdentityBiometricThreshold: _lastIdentityResult?.Biometric?.Threshold,
            LastIdentityBiometricDistance: _lastIdentityResult?.Biometric?.Distance,
            LastIdentityBiometricNearMatchThreshold: _lastIdentityResult?.Biometric?.NearMatchThreshold,
            LastIdentityBiometricEngine: _lastIdentityResult?.Biometric?.Engine,
            LastIdentityBiometricRejectReason: _lastIdentityResult?.Biometric?.RejectReason,
            LastIdentityEnrollmentEmbeddingPath: _lastIdentityResult?.Biometric?.EnrollmentEmbeddingPath,
            LastServiceActionKind: _lastServiceActionKind,
            LastServiceActionTarget: _lastServiceActionTarget,
            LastServiceActionMessage: _lastServiceActionMessage,
            LastServiceActionSucceeded: _lastServiceActionSucceeded,
            LastServiceActionUtc: _lastServiceActionUtc,
            RecentServiceActions: _recentServiceActions.ToArray());

        _stateStore.Write(snapshot);
    }

    private static bool IsVoiceReady(UserSettings settings) =>
        settings.VoiceSamplesRecorded >= settings.VoiceSamplesRequired;

}
