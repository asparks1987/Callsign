using Callsign.UI.Models;
using Callsign.UI.Services;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Callsign.Service;

public sealed class CallsignRuntimeWorker : BackgroundService
{
    private readonly ProfileStore _profileStore;
    private readonly StartMenuLauncher _launcher;
    private readonly BrowserLaunchService _browserLaunchService;
    private readonly FileSearchService _fileSearchService;
    private readonly VoiceCommandService _voiceCommandService;
    private readonly VoiceBiometricVerificationService _voiceBiometricVerificationService = new();
    private readonly RuntimeStateStore _stateStore;
    private readonly RuntimeHostOptions _hostOptions;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DateTime _processStartedUtc = DateTime.UtcNow;
    private readonly AlphaSessionStateMachine _session = new();
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
        VoiceCommandService voiceCommandService,
        RuntimeStateStore stateStore,
        RuntimeHostOptions hostOptions,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _profileStore = profileStore;
        _launcher = launcher;
        _browserLaunchService = browserLaunchService;
        _fileSearchService = fileSearchService;
        _voiceCommandService = voiceCommandService;
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

            if (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
            {
                _session.DetectWakeWord();
                _overlayReadout = FormatOverlayReadout(_session.State);
                _statusMessage = $"Wake word detected by {e.Result.Engine}. Waiting for callsign identity.";
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
                _session.DetectWakeWord();
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

    private void VoiceTranscriptReceived(object? sender, VoiceTranscriptEventArgs e)
    {
        lock (_gate)
        {
            var profile = _activeProfile;
            if (profile == null)
                return;

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

            if (_serviceDictationActive)
            {
                if (AlphaVoiceTranscriptParser.IsStopDictationCommand(e.Text))
                {
                    _serviceDictationActive = false;
                    _statusMessage = "Service dictation stopped. Review text in the configuration manager Dictation tab.";
                    RecordServiceAction("dictation", "service dictation", _statusMessage, succeeded: true);
                    RequestUiMode("Dictation");
                    WriteSnapshot();
                    return;
                }

                AppendServiceDictation(e.Text);
                _statusMessage = "Service dictation updated. Say 'stop dictation' when finished.";
                RequestUiMode("Dictation");
                WriteSnapshot();
                return;
            }

            if (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed
                && IsStrictWakeTranscript(e.Text, profile.Settings.WakeWord))
            {
                _lastWakeWordDetection = new WakeWordDetectionResult
                {
                    Detected = true,
                    Score = Math.Clamp(e.Confidence, 0f, 1f),
                    Threshold = 0.20,
                    Engine = "transcript-wake-rescue",
                    AudioQualityWarnings = e.AudioQualityWarnings,
                    TimestampUtc = DateTime.UtcNow,
                    CandidateWindowPath = e.CapturedAudioPath
                };
                _lastIdentityResult = null;
                _session.DetectWakeWord();
                _statusMessage = "Wake word recognized from speech. Waiting for callsign identity.";
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

    private static bool IsStrictWakeTranscript(string transcript, string? configuredWakeWord)
    {
        return AlphaVoiceTranscriptParser.ContainsWakeWord(transcript, configuredWakeWord ?? "Callsign");
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
            _session.TryVerifyIdentity(profile.Callsign, profile.Callsign, IsVoiceReady(profile.Settings), out _statusMessage);
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
            _session.TryVerifyIdentity(transcript, profile.Callsign, IsVoiceReady(profile.Settings), out _statusMessage);
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

    private void ExecuteVerifiedCommand(UserProfile profile, AlphaVoiceIntent intent)
    {
        _overlayReadout = FormatOverlayReadout(
            _session.State,
            intent.NormalizedCommand,
            _session.VerifiedCallsign,
            _session.PendingCommand,
            _session.PendingApp);

        if (TryExecuteBrowserCommand(intent, out var browserMessage))
        {
            _session.CompleteLaunch();
            _statusMessage = browserMessage;
            RecordServiceAction("browser", intent.Target, browserMessage, succeeded: !browserMessage.StartsWith("Unable", StringComparison.OrdinalIgnoreCase));
            return;
        }

        if (TryExecuteFileSearchCommand(intent, out var fileSearchMessage))
        {
            _session.CompleteLaunch();
            _statusMessage = fileSearchMessage;
            RecordServiceAction("file_search", intent.Target, fileSearchMessage, succeeded: !fileSearchMessage.StartsWith("No file", StringComparison.OrdinalIgnoreCase) && !fileSearchMessage.StartsWith("Unable", StringComparison.OrdinalIgnoreCase));
            return;
        }

        if (TryExecuteDictationCommand(intent, out var dictationMessage))
        {
            _session.CompleteLaunch();
            _statusMessage = dictationMessage;
            RecordServiceAction("dictation", "service dictation", dictationMessage, succeeded: true);
            return;
        }

        if (TryExecuteUiNavigationCommand(intent, out var uiNavigationMessage))
        {
            _session.CompleteLaunch();
            _statusMessage = uiNavigationMessage;
            RecordServiceAction("ui_navigation", intent.Target, uiNavigationMessage, succeeded: true);
            return;
        }

        if (TryExecuteUiActionCommand(intent, out var uiActionMessage))
        {
            _session.CompleteLaunch();
            _statusMessage = uiActionMessage;
            RecordServiceAction("ui_action", intent.Target, uiActionMessage, succeeded: true);
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
            _session.CompleteLaunch();
            _statusMessage = launchMessage;
            RecordServiceAction("start_menu_launch", _session.PendingApp ?? appName, launchMessage, succeeded: true);
        }
        else
        {
            _session.FailLaunch(launchMessage);
            _statusMessage = launchMessage;
            RecordServiceAction("start_menu_launch", _session.PendingApp ?? appName, launchMessage, succeeded: false);
        }
    }

    private bool TryExecuteBrowserCommand(AlphaVoiceIntent intent, out string message)
    {
        message = string.Empty;
        if (intent.Kind != AlphaVoiceIntentKind.Browser)
            return false;

        if (string.IsNullOrWhiteSpace(intent.Target))
        {
            message = "Browser command heard, but no website or search phrase was captured.";
            return true;
        }

        if (TryExecuteBrowserAction(intent.Target, out message))
            return true;

        if (_browserLaunchService.TryOpen(intent.Target, out message, out _, browserTarget: intent.BrowserTarget))
            return true;

        return true;
    }

    private bool TryExecuteBrowserAction(string target, out string message)
    {
        switch (target.Trim().ToLowerInvariant())
        {
            case "browser-back":
            case "browser-forward":
            case "browser-refresh":
            case "browser-new-tab":
            case "browser-close-tab":
            case "browser-focus-address-bar":
                return _browserLaunchService.TryExecuteBrowserAction(target, out message);
            default:
                message = string.Empty;
                return false;
        }
    }

    private bool TryExecuteFileSearchCommand(AlphaVoiceIntent intent, out string message)
    {
        message = string.Empty;
        if (intent.Kind != AlphaVoiceIntentKind.FileSearch)
            return false;

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
            return true;
        }

        message = openMessage;
        return true;
    }

    private bool TryExecuteDictationCommand(AlphaVoiceIntent intent, out string message)
    {
        message = string.Empty;
        if (intent.Kind != AlphaVoiceIntentKind.Dictation)
            return false;

        _serviceDictationActive = true;
        _serviceDictationSegments.Clear();
        _serviceDictationUpdatedUtc = DateTime.UtcNow;
        RequestUiMode("Dictation");
        message = "Service dictation started. Speak naturally, then say 'stop dictation' when finished.";
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

        RequestUiMode(intent.Target);
        message = $"Opening {intent.Target} action.";
        return true;
    }

    private void AppendServiceDictation(string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        _serviceDictationSegments.Add(normalized);
        _serviceDictationUpdatedUtc = DateTime.UtcNow;
    }

    private void RequestUiMode(string mode)
    {
        _requestedUiMode = mode;
        _requestedUiModeUtc = DateTime.UtcNow;
    }

    private void RecordServiceAction(string kind, string? target, string message, bool succeeded)
    {
        _lastServiceActionKind = kind;
        _lastServiceActionTarget = target;
        _lastServiceActionMessage = message;
        _lastServiceActionSucceeded = succeeded;
        _lastServiceActionUtc = DateTime.UtcNow;
        _recentServiceActions.Add(new RuntimeServiceActionSnapshot(kind, target, message, succeeded, _lastServiceActionUtc.Value));
        if (_recentServiceActions.Count > 20)
            _recentServiceActions.RemoveRange(0, _recentServiceActions.Count - 20);
        PersistRecentServiceActions();
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
