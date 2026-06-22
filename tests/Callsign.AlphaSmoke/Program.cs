using Callsign.UI.Models;
using Callsign.UI.Services;
using Callsign.UI;
using Callsign.Extensions;
using Callsign.AlphaSmoke;
using NAudio.Wave;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;

var liveLaunchApp = GetArgumentValue(args, "--live-launch");
var liveBrowserTarget = GetArgumentValue(args, "--live-browser");
var liveFileSearchQuery = GetArgumentValue(args, "--live-file-search");
var scriptedSessionTranscript = GetArgumentValue(args, "--scripted-session");
var runVoiceListener = HasArgument(args, "--voice-listener");
var offlineSpeechPhrase = GetArgumentValue(args, "--offline-speech");
var checkInstalledRuntime = HasArgument(args, "--installed-runtime");
var watchServiceActionSeconds = GetArgumentValue(args, "--watch-service-action");

var checks = new List<(string Name, Action Check)>
{
    ("profile creation persists personalized callsign state", ProfileCreationPersists),
    ("wake word alone cannot execute a launch", WakeWordAloneCannotExecute),
    ("wake threshold follows sensitivity mapping", WakeThresholdSensitivityMapping),
    ("wake threshold defaults to the recall-biased fallback", WakeThresholdDefaultsToRecallBiasedFallback),
    ("fresh profiles default to more responsive wake sensitivity", WakeDefaultsFavorMoreResponsive),
    ("legacy balanced wake profiles upgrade to more responsive defaults", LegacyWakeSettingsUpgrade),
    ("legacy high wake thresholds upgrade to the new fallback", LegacyHighWakeThresholdUpgrade),
    ("fresh profiles default to faster speech segment finalization", FreshProfilesDefaultToFasterSpeechTiming),
    ("legacy speech timing upgrades to a faster segment window", LegacySpeechTimingUpgrades),
    ("matching callsign unlocks command capture and launch intent", MatchingCallsignUnlocksLaunchIntent),
    ("mismatched callsign locks out and blocks execution", MismatchedCallsignLocksOut),
    ("voice activation is required before identity confirmation", VoiceActivationRequired),
    ("identity matcher accepts exact callsign variants", IdentityMatcherAcceptsAllowedVariants),
    ("identity matcher rejects noisy or ambiguous identity text", IdentityMatcherRejectsNoisyIdentityText),
    ("identity matcher requires biometrics when configured", IdentityMatcherRequiresBiometricsWhenConfigured),
    ("identity matcher allows near text miss only after biometric match", IdentityMatcherAllowsNearTextMissOnlyAfterBiometricMatch),
    ("local voice biometric verifier compares enrolled audio", LocalVoiceBiometricVerifierComparesEnrolledAudio),
    ("local voice biometric verifier rejects stale and replayed samples", LocalVoiceBiometricVerifierRejectsReplaySamples),
    ("multi-sample enrollment rejects reused single-file submissions", MultiSampleEnrollmentRejectsReusedSingleFile),
    ("pyannote biometric verifier fails closed until enrolled", PyannoteBiometricVerifierFailsClosedUntilEnrolled),
    ("identity gate requires a separate command turn", IdentityGateRequiresSeparateCommandTurn),
    ("Start menu alpha scope accepts plain app names and rejects command text", StartMenuScopeValidation),
    ("Start menu launcher can resolve installed app names", StartMenuResolution),
    ("Start menu launcher normalizes common speech aliases", StartMenuSpeechAliasResolution),
    ("Start menu launcher resolves trusted system surfaces", TrustedSystemSurfaceResolution),
    ("browser helper resolves URLs and search phrases", BrowserTargetResolution),
    ("file search helper finds files in the intended scope", FileSearchResolution),
    ("verified service command router classifies alpha actions", ServiceCommandRouterClassifiesAlphaActions),
    ("extension pack registry loads drop-in command packs", ExtensionPackRegistryLoadsDropInPack),
    ("extension pack registry can disable and re-enable packs", ExtensionPackRegistryCanDisableAndReenablePack),
    ("voice navigation routes Callsign tabs", VoiceNavigationRoutesTabs),
    ("voice help command routes setup help", VoiceHelpCommandRoutesSetupHelp),
    ("overlay readout formatter follows the phase contract", OverlayReadoutFormatterFollowsPhaseContract),
    ("wake overlay exposes phase and live readout", WakeOverlayReadoutUpdates),
    ("visible controls overlay shows the focused target", VisibleControlsOverlayShowsFocusedTarget),
    ("runtime snapshot preserves transcript readout history", RuntimeSnapshotPreservesTranscriptHistory),
    ("dictation voice actions are recognized", DictationVoiceActionsRecognized),
    ("dictation spelling commands are recognized", DictationSpellingCommandsRecognized),
    ("scripted voice intents cover alpha service actions", ScriptedVoiceIntentsCoverAlphaActions),
    ("wake parser accepts split and common homophone wake phrases", WakeParserHandlesCommonWakePhrases),
    ("wake-like transcript cannot bypass explicit wake transition", WakeTranscriptCannotBypassWakeTransition),
    ("service worker does not promote transcript text to wake events", ServiceWorkerDoesNotPromoteTranscriptWake),
    ("wake detector uses streaming frame predictions", WakeDetectorUsesStreamingFramePredictions),
    ("wake frame is evaluated before segment gating", WakeFrameIsEvaluatedBeforeSegmentGating),
    ("wake event forces the overlay immediately", WakeEventForcesOverlayImmediately),
    ("wake overlay is preloaded before first wake", WakeOverlayIsPreloadedBeforeFirstWake),
    ("wake detector is warmed up before live listening", WakeDetectorIsWarmedUpBeforeLiveListening),
    ("packaged wake test helper uses streaming frames", PackagedWakeTestHelperUsesStreamingFrames),
    ("wake calibration helper scores enrolled samples", WakeCalibrationHelperScoresEnrolledSamples),
    ("wake calibration persists metadata", WakeCalibrationPersistsMetadata),
    ("wake training form exposes calibration", WakeTrainingFormExposesWakeCalibration),
    ("wake service evaluates a live rolling window", WakeServiceEvaluatesRollingWindow),
    ("runtime state writes are atomic", RuntimeStateWritesAreAtomic)
};

var failures = new List<string>();
foreach (var check in checks)
{
    try
    {
        check.Check();
        Console.WriteLine($"PASS: {check.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{check.Name}: {ex.Message}");
        Console.WriteLine($"FAIL: {check.Name}");
        Console.WriteLine($"      {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Alpha smoke failed:");
    foreach (var failure in failures)
        Console.WriteLine($"- {failure}");

    return 1;
}

Console.WriteLine();
Console.WriteLine("Alpha smoke passed.");

if (!string.IsNullOrWhiteSpace(liveLaunchApp))
{
    var liveLaunchExitCode = LiveLaunch(liveLaunchApp);
    if (liveLaunchExitCode != 0)
        return liveLaunchExitCode;
}

if (!string.IsNullOrWhiteSpace(liveBrowserTarget))
{
    var liveBrowserExitCode = LiveBrowser(liveBrowserTarget);
    if (liveBrowserExitCode != 0)
        return liveBrowserExitCode;
}

if (!string.IsNullOrWhiteSpace(liveFileSearchQuery))
{
    var liveFileSearchExitCode = LiveFileSearch(liveFileSearchQuery);
    if (liveFileSearchExitCode != 0)
        return liveFileSearchExitCode;
}

if (!string.IsNullOrWhiteSpace(scriptedSessionTranscript))
{
    var scriptedSessionExitCode = ScriptedSession(scriptedSessionTranscript);
    if (scriptedSessionExitCode != 0)
        return scriptedSessionExitCode;
}

if (runVoiceListener)
{
    var voiceExitCode = VoiceListenerStartup();
    if (voiceExitCode != 0)
        return voiceExitCode;
}

if (!string.IsNullOrWhiteSpace(offlineSpeechPhrase))
    return OfflineSpeechRecognition(offlineSpeechPhrase);

if (checkInstalledRuntime)
    return InstalledRuntimeSmoke();

if (!string.IsNullOrWhiteSpace(watchServiceActionSeconds))
    return WatchServiceAction(watchServiceActionSeconds);

return 0;

static void ProfileCreationPersists()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = " Echo One ",
            DisplayName = "Echo Operator",
            Settings =
            {
                VoiceEnrollmentStatus = "Activated",
                VoiceSamplesRecorded = 3,
                VoiceSamplesRequired = 3,
                LastLaunchedApp = "Notepad"
            }
        };

        store.Save(profile);
        var loaded = store.Load("echo one") ?? throw new InvalidOperationException("Profile did not load after save.");

        Require(loaded.Callsign == "echo one", $"Expected normalized callsign 'echo one', got '{loaded.Callsign}'.");
        Require(loaded.DisplayName == "Echo Operator", "Display name was not preserved.");
        Require(loaded.Settings.VoiceEnrollmentStatus == "Activated", "Voice activation state was not preserved.");
        Require(loaded.Settings.LastLaunchedApp == "Notepad", "Last launched app was not preserved.");
    }
    finally
    {
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch
        {
            // The loaded test assembly can keep the copied pack DLL locked until process exit.
        }
    }
}

static void WakeWordAloneCannotExecute()
{
    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();

    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected WaitingForIdentity, got {session.State}.");
    Require(!session.TryBeginLaunch("Notepad", out _), "Launch should not begin before identity and command capture.");
}

static void MatchingCallsignUnlocksLaunchIntent()
{
    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();

    Require(session.TryVerifyIdentity("echo one", "Echo-One", voiceEnrolled: true, out _), "Matching callsign was not accepted.");
    Require(session.State == AlphaSessionState.WaitingForCommand, $"Expected WaitingForCommand, got {session.State}.");

    Require(session.TryCaptureCommand("open Notepad please", out _), "Command capture failed.");
    Require(session.PendingApp == "Notepad", $"Expected inferred app 'Notepad', got '{session.PendingApp}'.");
    Require(session.State == AlphaSessionState.ReadyToLaunch, $"Expected ReadyToLaunch, got {session.State}.");

    Require(session.TryBeginLaunch("Notepad", out _), "Launch intent did not begin after identity and command capture.");
    Require(session.State == AlphaSessionState.Launching, $"Expected Launching, got {session.State}.");

    session.CompleteLaunch();
    Require(session.State == AlphaSessionState.Completed, $"Expected Completed, got {session.State}.");
}

static void MismatchedCallsignLocksOut()
{
    var session = new AlphaSessionStateMachine(lockoutDuration: TimeSpan.FromSeconds(30));
    session.DetectWakeWord();

    Require(!session.TryVerifyIdentity("wrong user", "echo one", voiceEnrolled: true, out _), "Mismatched callsign was accepted.");
    Require(session.State == AlphaSessionState.LockedOut, $"Expected LockedOut, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture should fail while locked out.");
}

static void VoiceActivationRequired()
{
    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();

    Require(!session.TryVerifyIdentity("echo one", "echo one", voiceEnrolled: false, out _), "Identity should not verify before voice activation.");
    Require(session.State == AlphaSessionState.Idle, $"Expected failed voice activation to cancel to Idle, got {session.State}.");
}

static void WakeThresholdSensitivityMapping()
{
    Require(Math.Abs(VoiceCommandService.ResolveWakeThreshold(null, "Balanced") - 0.02) < 0.0001, "Balanced wake threshold should resolve to 0.02.");
    Require(Math.Abs(VoiceCommandService.ResolveWakeThreshold(null, "More responsive") - 0.01) < 0.0001, "More responsive wake threshold should resolve to 0.01.");
    Require(Math.Abs(VoiceCommandService.ResolveWakeThreshold(null, "Fewer false wakes") - 0.04) < 0.0001, "Fewer false wakes threshold should resolve to 0.04.");
}

static void WakeThresholdDefaultsToRecallBiasedFallback()
{
    Require(Math.Abs(VoiceCommandService.ResolveWakeThreshold(null, null) - 0.01) < 0.0001, "Default wake threshold should bias toward recall at 0.01.");
}

static void WakeDefaultsFavorMoreResponsive()
{
    var settings = new UserSettings();
    Require(settings.VoiceWakeThreshold <= 0, $"Fresh profile wake threshold should defer to sensitivity, got {settings.VoiceWakeThreshold}.");
    Require(string.Equals(settings.VoiceWakeSensitivity, "More responsive", StringComparison.OrdinalIgnoreCase), $"Fresh profile wake sensitivity should default to More responsive, got '{settings.VoiceWakeSensitivity}'.");
}

static void LegacyWakeSettingsUpgrade()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "Legacy",
            Settings =
            {
                VoiceWakeThreshold = 0.42,
                VoiceWakeSensitivity = "Balanced"
            }
        };

        store.Save(profile);
        var loaded = store.Load("legacy") ?? throw new InvalidOperationException("Legacy profile did not load.");

        Require(loaded.Settings.VoiceWakeThreshold <= 0, $"Legacy wake threshold should upgrade to sensitivity-based defaults, got {loaded.Settings.VoiceWakeThreshold}.");
        Require(string.Equals(loaded.Settings.VoiceWakeSensitivity, "More responsive", StringComparison.OrdinalIgnoreCase), $"Legacy wake sensitivity should upgrade to More responsive, got '{loaded.Settings.VoiceWakeSensitivity}'.");
    }
    finally
    {
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch
        {
            // The loaded test assembly can keep the copied pack DLL locked until process exit.
        }
    }
}

static void LegacyHighWakeThresholdUpgrade()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "LegacyHigh",
            Settings =
            {
                VoiceWakeThreshold = 0.55,
                VoiceWakeSensitivity = "More responsive"
            }
        };

        store.Save(profile);
        var loaded = store.Load("legacyhigh") ?? throw new InvalidOperationException("Legacy high-threshold profile did not load.");

        Require(loaded.Settings.VoiceWakeThreshold <= 0, $"Legacy 0.55 wake threshold should upgrade to sensitivity-based defaults, got {loaded.Settings.VoiceWakeThreshold}.");
        Require(string.Equals(loaded.Settings.VoiceWakeSensitivity, "More responsive", StringComparison.OrdinalIgnoreCase), $"Legacy high wake sensitivity should remain More responsive, got '{loaded.Settings.VoiceWakeSensitivity}'.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void FreshProfilesDefaultToFasterSpeechTiming()
{
    var settings = new UserSettings();
    Require(settings.VoiceSilenceMilliseconds == 200, $"Fresh profile speech silence window should default to 200 ms, got {settings.VoiceSilenceMilliseconds}.");
}

static void LegacySpeechTimingUpgrades()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "LegacySpeech",
            Settings =
            {
                VoiceSilenceMilliseconds = 850
            }
        };

        store.Save(profile);
        var loaded = store.Load("legacyspeech") ?? throw new InvalidOperationException("Legacy speech timing profile did not load.");

        Require(loaded.Settings.VoiceSilenceMilliseconds == 200, $"Legacy speech silence window should upgrade to 200 ms, got {loaded.Settings.VoiceSilenceMilliseconds}.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void IdentityMatcherAcceptsAllowedVariants()
{
    Require(CallsignIdentityMatcher.Evaluate("echo one", 0.95f, "echo one").Accepted, "Exact callsign should verify.");
    Require(CallsignIdentityMatcher.Evaluate("echo-one", 0.95f, "echo one").Accepted, "Hyphen callsign variant should verify.");
    Require(CallsignIdentityMatcher.Evaluate("echo_one", 0.95f, "echo one").Accepted, "Underscore callsign variant should verify.");
    Require(CallsignIdentityMatcher.Evaluate("echoone", 0.95f, "echo one").Accepted, "Compacted callsign variant should verify.");
    Require(CallsignIdentityMatcher.Evaluate("captain", 0.95f, "echo one", new[] { "captain" }).Accepted, "Configured alias should verify.");
}

static void IdentityMatcherRejectsNoisyIdentityText()
{
    var lowConfidence = CallsignIdentityMatcher.Evaluate("echo one", 0.20f, "echo one");
    Require(!lowConfidence.Accepted, "Low-confidence identity should fail closed.");
    Require(lowConfidence.RejectReason == "identity_confidence_low", $"Expected identity_confidence_low, got {lowConfidence.RejectReason}.");
    Require(!string.IsNullOrWhiteSpace(lowConfidence.RetryPrompt), "Low-confidence identity should ask for a repeat.");

    var commandStuffed = CallsignIdentityMatcher.Evaluate("echo one open notepad", 0.95f, "echo one");
    Require(!commandStuffed.Accepted, "Identity phrase with command text should not verify.");
    Require(commandStuffed.RejectReason == "identity_ambiguous_extra_words", $"Expected identity_ambiguous_extra_words, got {commandStuffed.RejectReason}.");
    Require(commandStuffed.RetryPrompt == "Say only your callsign.", "Command-stuffed identity should ask for callsign only.");

    var mismatch = CallsignIdentityMatcher.Evaluate("wrong user", 0.95f, "echo one");
    Require(!mismatch.Accepted, "Wrong identity should fail.");
    Require(mismatch.RejectReason == "identity_mismatch", $"Expected identity_mismatch, got {mismatch.RejectReason}.");
}

static void IdentityMatcherRequiresBiometricsWhenConfigured()
{
    var missingBiometric = CallsignIdentityMatcher.Evaluate(
        "pred",
        0.95f,
        "pred",
        requireBiometric: true);
    Require(!missingBiometric.Accepted, "Identity should fail closed when biometric verification is required but unavailable.");
    Require(missingBiometric.RejectReason == "identity_biometric_unavailable", $"Expected identity_biometric_unavailable, got {missingBiometric.RejectReason}.");

    var rejectedBiometric = CallsignIdentityMatcher.Evaluate(
        "pred",
        0.95f,
        "pred",
        biometric: FakeBiometric(accepted: false),
        requireBiometric: true);
    Require(!rejectedBiometric.Accepted, "Identity should fail when biometric verification rejects the speaker.");
    Require(rejectedBiometric.RejectReason == "biometric_mismatch", $"Expected biometric_mismatch, got {rejectedBiometric.RejectReason}.");

    var acceptedBiometric = CallsignIdentityMatcher.Evaluate(
        "pred",
        0.95f,
        "pred",
        biometric: FakeBiometric(accepted: true),
        requireBiometric: true);
    Require(acceptedBiometric.Accepted, "Exact identity should pass when biometric verification succeeds.");
}

static void IdentityMatcherAllowsNearTextMissOnlyAfterBiometricMatch()
{
    var withoutBiometric = CallsignIdentityMatcher.Evaluate("pread", 0.95f, "pred");
    Require(!withoutBiometric.Accepted, "Near-miss identity text should not pass without biometric proof.");

    var failedBiometric = CallsignIdentityMatcher.Evaluate(
        "pread",
        0.95f,
        "pred",
        biometric: FakeBiometric(accepted: false),
        requireBiometric: true);
    Require(!failedBiometric.Accepted, "Near-miss identity text should not pass with failed biometric proof.");

    var weakNearMatchBiometric = CallsignIdentityMatcher.Evaluate(
        "pread",
        0.95f,
        "pred",
        biometric: FakeBiometric(accepted: true, score: 0.80),
        requireBiometric: true,
        nearMatchBiometricThreshold: 0.86);
    Require(!weakNearMatchBiometric.Accepted, "Near-miss identity text should require a stronger biometric score than exact identity text.");
    Require(weakNearMatchBiometric.RejectReason == "identity_near_match_biometric_too_weak",
        $"Expected identity_near_match_biometric_too_weak, got {weakNearMatchBiometric.RejectReason}.");

    var acceptedBiometric = CallsignIdentityMatcher.Evaluate(
        "pread",
        0.95f,
        "pred",
        biometric: FakeBiometric(accepted: true, score: 0.93),
        requireBiometric: true,
        nearMatchBiometricThreshold: 0.86);
    Require(acceptedBiometric.Accepted, "Near-miss identity text should pass when biometric verification succeeds.");
    Require(acceptedBiometric.MatchedVariant == "pred", $"Expected matched variant pred, got {acceptedBiometric.MatchedVariant}.");
}

static void LocalVoiceBiometricVerifierComparesEnrolledAudio()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.Biometric", Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(root);
        var enrolled = Path.Combine(root, "enrolled.wav");
        var sameSpeaker = Path.Combine(root, "same.wav");
        var differentSpeaker = Path.Combine(root, "different.wav");
        WriteTone(enrolled, 180, 0.40);
        WriteTone(sameSpeaker, 180, 0.39);
        WriteTone(differentSpeaker, 640, 0.40);

        var verifier = new VoiceBiometricVerificationService();
        var same = verifier.Verify(enrolled, sameSpeaker, threshold: 0.70);
        Require(same.Accepted, $"Expected similar voice sample to pass, score {same.Score:0.000}.");

        var different = verifier.Verify(enrolled, differentSpeaker, threshold: 0.92);
        Require(!different.Accepted, $"Expected different voice sample to fail at strict threshold, score {different.Score:0.000}.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void LocalVoiceBiometricVerifierRejectsReplaySamples()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.BiometricReplay", Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(root);
        var enrolled = Path.Combine(root, "enrolled.wav");
        var candidate = Path.Combine(root, "candidate.wav");
        WriteTone(enrolled, 180, 0.40);
        WriteTone(candidate, 180, 0.40);

        var verifier = new VoiceBiometricVerificationService();
        var replay = verifier.Verify(enrolled, enrolled, threshold: 0.70);
        Require(!replay.Accepted, "Enrollment audio reused as candidate should be rejected as replay.");
        Require(replay.RejectReason == "biometric_replay_enrollment_sample", $"Expected biometric_replay_enrollment_sample, got {replay.RejectReason}.");

        File.SetLastWriteTimeUtc(candidate, DateTime.UtcNow - TimeSpan.FromMinutes(10));
        var stale = verifier.Verify(enrolled, candidate, threshold: 0.70, maxCandidateAge: TimeSpan.FromSeconds(30));
        Require(!stale.Accepted, "Stale candidate audio should not verify identity.");
        Require(stale.RejectReason == "biometric_candidate_stale", $"Expected biometric_candidate_stale, got {stale.RejectReason}.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void MultiSampleEnrollmentRejectsReusedSingleFile()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.MultiSample", Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(root);
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "womprat",
            Settings =
            {
                VoiceBiometricRequired = true,
                VoiceBiometricThreshold = 0.72,
                VoiceBiometricNearMatchThreshold = 0.86
            }
        };
        store.Save(profile);

        var sample = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 1);
        WriteTone(sample, 220, 0.4);

        var verifier = new VoiceBiometricVerificationService();
        var result = verifier.EnrollFreshSamples(store, profile, new[] { sample, sample, sample });
        Require(!result.Accepted, "Enrollment should reject reused sample files.");
        Require(result.RejectReason == "pyannote_sample_set_too_small", $"Expected pyannote_sample_set_too_small, got {result.RejectReason}.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void PyannoteBiometricVerifierFailsClosedUntilEnrolled()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.PyannoteMissing", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "pred",
            Settings =
            {
                VoiceBiometricRequired = true,
                VoiceBiometricThreshold = 0.72,
                VoiceBiometricNearMatchThreshold = 0.86
            }
        };
        store.Save(profile);

        var candidate = Path.Combine(root, "candidate.wav");
        WriteTone(candidate, 180, 0.40);
        var verifier = new VoiceBiometricVerificationService();
        var result = verifier.Verify(store, profile, candidate, threshold: 0.72, maxCandidateAge: TimeSpan.FromSeconds(30));
        Require(!result.Accepted, "pyannote identity must fail closed until an embedding is enrolled.");
        Require(result.Engine == "pyannote/embedding", $"Expected pyannote engine, got {result.Engine}.");
        Require(result.RejectReason == "pyannote_enrollment_missing", $"Expected pyannote_enrollment_missing, got {result.RejectReason}.");
        Require(result.EnrollmentEmbeddingPath?.EndsWith(Path.Combine("voice-identity", "embedding.json"), StringComparison.OrdinalIgnoreCase) == true,
            $"Expected embedding path under voice-identity, got {result.EnrollmentEmbeddingPath}.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
static void IdentityGateRequiresSeparateCommandTurn()
{
    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();

    var commandStuffedIdentity = CallsignIdentityMatcher.Evaluate("echo one open Notepad", 0.95f, "echo one");
    Require(!commandStuffedIdentity.Accepted, "Identity gate must reject callsign plus command in the same utterance.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture should not work before identity verifies.");

    var identityOnly = CallsignIdentityMatcher.Evaluate("echo one", 0.95f, "echo one");
    Require(identityOnly.Accepted, "Clean identity should verify.");
    Require(session.TryVerifyIdentity("echo one", "echo one", voiceEnrolled: true, out _), "Session should accept clean identity.");
    Require(session.State == AlphaSessionState.WaitingForCommand, $"Expected WaitingForCommand, got {session.State}.");
    Require(session.TryCaptureCommand("open Notepad", out _), "Separate command turn should capture after identity verifies.");
}

static VoiceBiometricVerificationResult FakeBiometric(bool accepted, double? score = null) =>
    new(
        accepted,
        score ?? (accepted ? 0.93 : 0.20),
        0.72,
        "test-open-source-biometric",
        accepted ? null : "biometric_mismatch",
        "enrolled.wav",
        "candidate.wav");

static void WriteTone(string path, double frequency, double amplitude)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var format = new WaveFormat(16_000, 16, 1);
    using var writer = new WaveFileWriter(path, format);
    for (var sample = 0; sample < 16_000; sample++)
    {
        var value = (short)(Math.Sin(2 * Math.PI * frequency * sample / 16_000) * amplitude * short.MaxValue);
        writer.WriteByte((byte)(value & 0xff));
        writer.WriteByte((byte)((value >> 8) & 0xff));
    }
}
static void StartMenuScopeValidation()
{
    Require(StartMenuLauncher.ValidateAppName("Notepad", out _), "Plain app name should be accepted.");
    Require(StartMenuLauncher.ValidateAppName("Calculator", out _), "Plain app name should be accepted.");
    Require(!StartMenuLauncher.ValidateAppName(@"C:\Windows\notepad.exe", out _), "Paths should be rejected.");
    Require(!StartMenuLauncher.ValidateAppName("https://example.com", out _), "URLs should be rejected.");
    Require(!StartMenuLauncher.ValidateAppName("powershell", out _), "Shell apps should be rejected in alpha free scope.");
    Require(!StartMenuLauncher.ValidateAppName("wsl", out _), "WSL should be rejected by the alpha free launcher.");
    Require(!StartMenuLauncher.ValidateAppName("notepad & calc", out _), "Shell-style command text should be rejected.");
}

static void StartMenuResolution()
{
    var launcher = new StartMenuLauncher();
    Require(launcher.TryResolveInstalledAppName("Notepad", out var resolved) || resolved == "Notepad", "Resolver should preserve a plain app name.");
    Require(!string.IsNullOrWhiteSpace(resolved), "Resolved app name should not be blank.");
}

static void StartMenuSpeechAliasResolution()
{
    Require(StartMenuLauncher.ResolveAppName("note pad") == "Notepad", "Speech alias 'note pad' should normalize to Notepad.");
    Require(StartMenuLauncher.ResolveAppName("calc") == "Calculator", "Speech alias 'calc' should normalize to Calculator.");
    Require(StartMenuLauncher.ResolveAppName("google crome") == "Google Chrome", "Speech alias 'google crome' should normalize to Google Chrome.");
    Require(StartMenuLauncher.ResolveAppName("vs code") == "Visual Studio Code", "Speech alias 'vs code' should normalize to Visual Studio Code.");
    Require(StartMenuLauncher.ResolveAppName("open documents") == "Documents", "Speech alias 'open documents' should normalize to Documents.");
    Require(StartMenuLauncher.ResolveAppName("open downloads") == "Downloads", "Speech alias 'open downloads' should normalize to Downloads.");
    Require(StartMenuLauncher.ResolveAppName("open the settings") == "Settings", "Speech alias 'open the settings' should normalize to Settings.");
    Require(StartMenuLauncher.ResolveAppName("open the file explorer") == "File Explorer", "Speech alias 'open the file explorer' should normalize to File Explorer.");
    Require(StartMenuLauncher.ResolveAppName("open this pc") == "This PC", "Speech alias 'open this pc' should normalize to This PC.");
    Require(StartMenuLauncher.ResolveAppName("open recycle bin") == "Recycle Bin", "Speech alias 'open recycle bin' should normalize to Recycle Bin.");
}

static void TrustedSystemSurfaceResolution()
{
    foreach (var (phrase, expectedFileName) in new[]
             {
                 ("Settings", "ms-settings:"),
                 ("File Explorer", "explorer.exe"),
                 ("This PC", "explorer.exe"),
                 ("Recycle Bin", "explorer.exe"),
                 ("Desktop", "explorer.exe"),
                 ("Documents", "explorer.exe"),
                 ("Downloads", "explorer.exe"),
                 ("Control Panel", "control.exe"),
                 ("Task Manager", "taskmgr.exe")
             })
    {
        Require(StartMenuLauncher.TryResolveTrustedSystemSurface(phrase, out var startInfo), $"Trusted system surface should resolve: {phrase}");
        Require(string.Equals(startInfo.FileName, expectedFileName, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedFileName}' for '{phrase}', got '{startInfo.FileName}'.");
        Require(startInfo.UseShellExecute, "Trusted system surfaces should launch through the shell.");

        if (phrase is "This PC" or "Recycle Bin")
            Require(string.IsNullOrWhiteSpace(startInfo.Arguments) == false, $"Expected shell arguments for '{phrase}'.");

        if (phrase is "Desktop" or "Documents" or "Downloads")
            Require(!string.IsNullOrWhiteSpace(startInfo.Arguments), $"Expected folder arguments for '{phrase}'.");
    }
}

static void BrowserTargetResolution()
{
    Require(BrowserLaunchService.TryBuildTargetUri("https://example.com", out var directUri, out _), "Direct https URL should resolve.");
    Require(directUri?.Host == "example.com", "Direct URL should preserve host.");

    Require(BrowserLaunchService.TryBuildTargetUri("Callsign desktop assistant", out var searchUri, out _), "Search phrase should resolve.");
    Require(searchUri?.Host.Contains("bing", StringComparison.OrdinalIgnoreCase) == true, "Search phrase should route to the search engine.");

    Require(!BrowserLaunchService.TryBuildTargetUri(@"C:\temp\notes.txt", out _, out _), "Local file paths should not be treated as browser targets.");

    if (BrowserLaunchService.TryFindChrome(out var chromePath))
    {
        Require(Path.GetFileName(chromePath).Equals("chrome.exe", StringComparison.OrdinalIgnoreCase), "Chrome discovery should resolve chrome.exe.");
    }
}

static void FileSearchResolution()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.FileSearch", Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(root);
        var document = Path.Combine(root, "alpha-notes.txt");
        File.WriteAllText(document, "hello");
        var nestedDir = Path.Combine(root, "Samples");
        Directory.CreateDirectory(nestedDir);
        var nestedFile = Path.Combine(nestedDir, "beta-plan.md");
        File.WriteAllText(nestedFile, "world");

        var service = new FileSearchService();
        var report = service.Search("alpha", new[] { root }, maxResults: 10);
        Require(report.Results.Any(result => result.FullPath == document), "File search should find matching file names.");
        Require(report.SearchEngine is "fzf" or "built-in", $"Search engine should be reported, got '{report.SearchEngine}'.");
        if (report.SearchEngine == "built-in")
        {
            Require(report.Warnings.Any(warning => warning.Contains("fzf.exe", StringComparison.OrdinalIgnoreCase)),
                "Built-in fallback should report that fzf.exe was unavailable.");
        }

        var emptyReport = service.Search("does-not-exist", new[] { root }, maxResults: 10);
        Require(emptyReport.Results.Count == 0, "Non-matching file search should return no results.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void ServiceCommandRouterClassifiesAlphaActions()
{
    Require(AlphaCommandRouter.TryRoute("open chrome to example.com", out var chromeRoute), "Chrome browser command should route.");
    Require(chromeRoute.Kind == AlphaCommandKind.Browser, "Chrome command should be browser kind.");
    Require(chromeRoute.BrowserTarget == BrowserOpenTarget.Chrome, "Chrome command should request Chrome.");
    Require(chromeRoute.Target == "example.com", $"Chrome command target should be example.com, got '{chromeRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("open google chrome to example.com", out var googleChromeRoute), "Google Chrome browser command should route.");
    Require(googleChromeRoute.Kind == AlphaCommandKind.Browser, "Google Chrome command should be browser kind.");
    Require(googleChromeRoute.BrowserTarget == BrowserOpenTarget.Chrome, "Google Chrome command should request Chrome.");
    Require(googleChromeRoute.Target == "example.com", $"Google Chrome command target should be example.com, got '{googleChromeRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("open crome to example.com", out var cromeRoute), "Noisy Chrome command should route.");
    Require(cromeRoute.BrowserTarget == BrowserOpenTarget.Chrome, "Noisy Chrome command should request Chrome.");
    Require(cromeRoute.Target == "example.com", $"Noisy Chrome command target should be example.com, got '{cromeRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("search the web for callsign setup", out var browserRoute), "Default browser search should route.");
    Require(browserRoute.Kind == AlphaCommandKind.Browser, "Search command should be browser kind.");
    Require(browserRoute.BrowserTarget == BrowserOpenTarget.Default, "Generic browser command should use the default browser.");
    Require(browserRoute.Target == "callsign setup", $"Search target should be preserved, got '{browserRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("repair wakeword", out var repairWakewordRoute), "Repair wakeword command should route.");
    Require(repairWakewordRoute.Kind == AlphaCommandKind.UiAction, "Repair wakeword should be a UI action.");
    Require(repairWakewordRoute.Target == "ui-repair-wakeword", $"Expected ui-repair-wakeword target, got '{repairWakewordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("train voice identity", out var trainVoiceIdentityRoute), "Train voice identity command should route.");
    Require(trainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Train voice identity should be a UI action.");
    Require(trainVoiceIdentityRoute.Target == "ui-train-voice-identity", $"Expected ui-train-voice-identity target, got '{trainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open data folder", out var openDataFolderRoute), "Open data folder command should route.");
    Require(openDataFolderRoute.Kind == AlphaCommandKind.UiAction, "Open data folder should be a UI action.");
    Require(openDataFolderRoute.Target == "ui-open-data-folder", $"Expected ui-open-data-folder target, got '{openDataFolderRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open logs folder", out var openLogsFolderRoute), "Open logs folder command should route.");
    Require(openLogsFolderRoute.Kind == AlphaCommandKind.UiAction, "Open logs folder should be a UI action.");
    Require(openLogsFolderRoute.Target == "ui-open-logs-folder", $"Expected ui-open-logs-folder target, got '{openLogsFolderRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("create new account", out var createAccountRoute), "Create new account command should route.");
    Require(createAccountRoute.Kind == AlphaCommandKind.UiAction, "Create new account should be a UI action.");
    Require(createAccountRoute.Target == "ui-create-account", $"Expected ui-create-account target, got '{createAccountRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("save account", out var saveAccountRoute), "Save account command should route.");
    Require(saveAccountRoute.Kind == AlphaCommandKind.UiAction, "Save account should be a UI action.");
    Require(saveAccountRoute.Target == "ui-save-account", $"Expected ui-save-account target, got '{saveAccountRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete account", out var deleteAccountRoute), "Delete account command should route.");
    Require(deleteAccountRoute.Kind == AlphaCommandKind.UiAction, "Delete account should be a UI action.");
    Require(deleteAccountRoute.Target == "ui-delete-account", $"Expected ui-delete-account target, got '{deleteAccountRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next control", out var nextControlRoute), "Next control command should route.");
    Require(nextControlRoute.Kind == AlphaCommandKind.UiAction, "Next control should be a UI action.");
    Require(nextControlRoute.Target == "ui-next-control", $"Expected ui-next-control target, got '{nextControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous control", out var previousControlRoute), "Previous control command should route.");
    Require(previousControlRoute.Kind == AlphaCommandKind.UiAction, "Previous control should be a UI action.");
    Require(previousControlRoute.Target == "ui-previous-control", $"Expected ui-previous-control target, got '{previousControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("activate control", out var activateControlRoute), "Activate control command should route.");
    Require(activateControlRoute.Kind == AlphaCommandKind.UiAction, "Activate control should be a UI action.");
    Require(activateControlRoute.Target == "ui-activate-control", $"Expected ui-activate-control target, got '{activateControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press repair wakeword", out var pressRepairWakewordRoute), "Press repair wakeword command should route.");
    Require(pressRepairWakewordRoute.Kind == AlphaCommandKind.UiAction, "Press repair wakeword should be a UI action.");
    Require(pressRepairWakewordRoute.Target == "ui-activate-label:repair wakeword", $"Expected ui-activate-label:repair wakeword target, got '{pressRepairWakewordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the train voice identity button", out var clickTrainVoiceIdentityRoute), "Click train voice identity command should route.");
    Require(clickTrainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Click train voice identity should be a UI action.");
    Require(clickTrainVoiceIdentityRoute.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickTrainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click callsign", out var clickCallsignRoute), "Click callsign command should route.");
    Require(clickCallsignRoute.Kind == AlphaCommandKind.UiAction, "Click callsign should be a UI action.");
    Require(clickCallsignRoute.Target == "ui-activate-label:callsign", $"Expected ui-activate-label:callsign target, got '{clickCallsignRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click active account", out var clickActiveAccountRoute), "Click active account command should route.");
    Require(clickActiveAccountRoute.Kind == AlphaCommandKind.UiAction, "Click active account should be a UI action.");
    Require(clickActiveAccountRoute.Target == "ui-activate-label:active account", $"Expected ui-activate-label:active account target, got '{clickActiveAccountRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click browser target", out var clickBrowserTargetRoute), "Click browser target command should route.");
    Require(clickBrowserTargetRoute.Kind == AlphaCommandKind.UiAction, "Click browser target should be a UI action.");
    Require(clickBrowserTargetRoute.Target == "ui-activate-label:browser target", $"Expected ui-activate-label:browser target target, got '{clickBrowserTargetRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click browser back", out var clickBrowserBackRoute), "Click browser back command should route.");
    Require(clickBrowserBackRoute.Kind == AlphaCommandKind.UiAction, "Click browser back should be a UI action.");
    Require(clickBrowserBackRoute.Target == "ui-activate-label:browser back", $"Expected ui-activate-label:browser back target, got '{clickBrowserBackRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click search results", out var clickSearchResultsRoute), "Click search results command should route.");
    Require(clickSearchResultsRoute.Kind == AlphaCommandKind.UiAction, "Click search results should be a UI action.");
    Require(clickSearchResultsRoute.Target == "ui-activate-label:search results", $"Expected ui-activate-label:search results target, got '{clickSearchResultsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click system volume up", out var clickSystemVolumeUpRoute), "Click system volume up command should route.");
    Require(clickSystemVolumeUpRoute.Kind == AlphaCommandKind.UiAction, "Click system volume up should be a UI action.");
    Require(clickSystemVolumeUpRoute.Target == "ui-activate-label:system volume up", $"Expected ui-activate-label:system volume up target, got '{clickSystemVolumeUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show numbers", out var showNumbersRoute), "Show numbers command should route.");
    Require(showNumbersRoute.Kind == AlphaCommandKind.UiAction, "Show numbers should be a UI action.");
    Require(showNumbersRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNumbersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide visible controls", out var hideVisibleControlsRoute), "Hide visible controls command should route.");
    Require(hideVisibleControlsRoute.Kind == AlphaCommandKind.UiAction, "Hide visible controls should be a UI action.");
    Require(hideVisibleControlsRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideVisibleControlsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click 3", out var clickNumberRoute), "Click 3 command should route.");
    Require(clickNumberRoute.Kind == AlphaCommandKind.UiAction, "Click 3 should be a UI action.");
    Require(clickNumberRoute.Target == "ui-activate-label:3", $"Expected ui-activate-label:3 target, got '{clickNumberRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("find file alpha-notes", out var fileRoute), "File search command should route.");
    Require(fileRoute.Kind == AlphaCommandKind.FileSearch, "File search command should be file-search kind.");
    Require(fileRoute.Target == "alpha-notes", $"File search target should be alpha-notes, got '{fileRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("search my pc for alpha notes", out var fileSearchPcRoute), "Search my PC file command should route.");
    Require(fileSearchPcRoute.Kind == AlphaCommandKind.FileSearch, "Search my PC file command should be file-search kind.");
    Require(fileSearchPcRoute.Target == "alpha notes", $"Expected alpha notes target, got '{fileSearchPcRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find a file called alpha notes", out var fileCalledRoute), "Find a file called command should route.");
    Require(fileCalledRoute.Kind == AlphaCommandKind.FileSearch, "Find a file called command should be file-search kind.");
    Require(fileCalledRoute.Target == "alpha notes", $"Expected alpha notes target, got '{fileCalledRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("volume up", out var systemVolumeUpRoute), "System volume up command should route.");
    Require(systemVolumeUpRoute.Kind == AlphaCommandKind.SystemControl, "Volume up command should be system-control kind.");
    Require(systemVolumeUpRoute.Target == "system-volume-up", $"Expected system-volume-up target, got '{systemVolumeUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mute audio", out var systemMuteRoute), "System mute command should route.");
    Require(systemMuteRoute.Kind == AlphaCommandKind.SystemControl, "Mute command should be system-control kind.");
    Require(systemMuteRoute.Target == "system-volume-mute", $"Expected system-volume-mute target, got '{systemMuteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show desktop", out var showDesktopRoute), "System show desktop command should route.");
    Require(showDesktopRoute.Kind == AlphaCommandKind.SystemControl, "Show desktop command should be system-control kind.");
    Require(showDesktopRoute.Target == "system-show-desktop", $"Expected system-show-desktop target, got '{showDesktopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next window", out var nextWindowRoute), "Next window command should route.");
    Require(nextWindowRoute.Kind == AlphaCommandKind.SystemControl, "Next window command should be system-control kind.");
    Require(nextWindowRoute.Target == "system-next-window", $"Expected system-next-window target, got '{nextWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous window", out var previousWindowRoute), "Previous window command should route.");
    Require(previousWindowRoute.Kind == AlphaCommandKind.SystemControl, "Previous window command should be system-control kind.");
    Require(previousWindowRoute.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("task manager", out var taskManagerRoute), "Task manager command should route.");
    Require(taskManagerRoute.Kind == AlphaCommandKind.SystemControl, "Task manager command should be system-control kind.");
    Require(taskManagerRoute.Target == "system-open-task-manager", $"Expected system-open-task-manager target, got '{taskManagerRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open app folder", out var openAppFolderRoute), "Open app folder command should route.");
    Require(openAppFolderRoute.Kind == AlphaCommandKind.UiAction, "Open app folder should be a UI action.");
    Require(openAppFolderRoute.Target == "ui-open-app-folder", $"Expected ui-open-app-folder target, got '{openAppFolderRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("minimize window", out var minimizeWindowRoute), "Minimize window command should route.");
    Require(minimizeWindowRoute.Kind == AlphaCommandKind.SystemControl, "Minimize window command should be system-control kind.");
    Require(minimizeWindowRoute.Target == "system-minimize-window", $"Expected system-minimize-window target, got '{minimizeWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("maximize window", out var maximizeWindowRoute), "Maximize window command should route.");
    Require(maximizeWindowRoute.Kind == AlphaCommandKind.SystemControl, "Maximize window command should be system-control kind.");
    Require(maximizeWindowRoute.Target == "system-maximize-window", $"Expected system-maximize-window target, got '{maximizeWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("restore window", out var restoreWindowRoute), "Restore window command should route.");
    Require(restoreWindowRoute.Kind == AlphaCommandKind.SystemControl, "Restore window command should be system-control kind.");
    Require(restoreWindowRoute.Target == "system-restore-window", $"Expected system-restore-window target, got '{restoreWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press enter", out var enterRoute), "Press enter command should route.");
    Require(enterRoute.Kind == AlphaCommandKind.SystemControl, "Press enter command should be system-control kind.");
    Require(enterRoute.Target == "system-press-enter", $"Expected system-press-enter target, got '{enterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press tab", out var tabRoute), "Press tab command should route.");
    Require(tabRoute.Kind == AlphaCommandKind.SystemControl, "Press tab command should be system-control kind.");
    Require(tabRoute.Target == "system-press-tab", $"Expected system-press-tab target, got '{tabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press escape", out var escapeRoute), "Press escape command should route.");
    Require(escapeRoute.Kind == AlphaCommandKind.SystemControl, "Press escape command should be system-control kind.");
    Require(escapeRoute.Target == "system-press-escape", $"Expected system-press-escape target, got '{escapeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press backspace", out var backspaceRoute), "Press backspace command should route.");
    Require(backspaceRoute.Kind == AlphaCommandKind.SystemControl, "Press backspace command should be system-control kind.");
    Require(backspaceRoute.Target == "system-press-backspace", $"Expected system-press-backspace target, got '{backspaceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press home", out var homeRoute), "Press home command should route.");
    Require(homeRoute.Kind == AlphaCommandKind.SystemControl, "Press home command should be system-control kind.");
    Require(homeRoute.Target == "system-press-home", $"Expected system-press-home target, got '{homeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press end", out var endRoute), "Press end command should route.");
    Require(endRoute.Kind == AlphaCommandKind.SystemControl, "Press end command should be system-control kind.");
    Require(endRoute.Target == "system-press-end", $"Expected system-press-end target, got '{endRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("page up", out var pageUpRoute), "Page up command should route.");
    Require(pageUpRoute.Kind == AlphaCommandKind.SystemControl, "Page up command should be system-control kind.");
    Require(pageUpRoute.Target == "system-page-up", $"Expected system-page-up target, got '{pageUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("page down", out var pageDownRoute), "Page down command should route.");
    Require(pageDownRoute.Kind == AlphaCommandKind.SystemControl, "Page down command should be system-control kind.");
    Require(pageDownRoute.Target == "system-page-down", $"Expected system-page-down target, got '{pageDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click", out var mouseClickRoute), "Click command should route.");
    Require(mouseClickRoute.Kind == AlphaCommandKind.SystemControl, "Click command should be system-control kind.");
    Require(mouseClickRoute.Target == "system-mouse-click", $"Expected system-mouse-click target, got '{mouseClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("double click", out var mouseDoubleClickRoute), "Double click command should route.");
    Require(mouseDoubleClickRoute.Kind == AlphaCommandKind.SystemControl, "Double click command should be system-control kind.");
    Require(mouseDoubleClickRoute.Target == "system-mouse-double-click", $"Expected system-mouse-double-click target, got '{mouseDoubleClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("right click", out var mouseRightClickRoute), "Right click command should route.");
    Require(mouseRightClickRoute.Kind == AlphaCommandKind.SystemControl, "Right click command should be system-control kind.");
    Require(mouseRightClickRoute.Target == "system-mouse-right-click", $"Expected system-mouse-right-click target, got '{mouseRightClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse scroll up", out var mouseScrollUpRoute), "Mouse scroll up command should route.");
    Require(mouseScrollUpRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll up command should be system-control kind.");
    Require(mouseScrollUpRoute.Target == "system-mouse-scroll-up", $"Expected system-mouse-scroll-up target, got '{mouseScrollUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse scroll down", out var mouseScrollDownRoute), "Mouse scroll down command should route.");
    Require(mouseScrollDownRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll down command should be system-control kind.");
    Require(mouseScrollDownRoute.Target == "system-mouse-scroll-down", $"Expected system-mouse-scroll-down target, got '{mouseScrollDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system copy", out var copyRoute), "Copy command should route.");
    Require(copyRoute.Kind == AlphaCommandKind.SystemControl, "Copy command should be system-control kind.");
    Require(copyRoute.Target == "system-copy", $"Expected system-copy target, got '{copyRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system paste", out var pasteRoute), "Paste command should route.");
    Require(pasteRoute.Kind == AlphaCommandKind.SystemControl, "Paste command should be system-control kind.");
    Require(pasteRoute.Target == "system-paste", $"Expected system-paste target, got '{pasteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system cut", out var cutRoute), "Cut command should route.");
    Require(cutRoute.Kind == AlphaCommandKind.SystemControl, "Cut command should be system-control kind.");
    Require(cutRoute.Target == "system-cut", $"Expected system-cut target, got '{cutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select all", out var selectAllRoute), "Select all command should route.");
    Require(selectAllRoute.Kind == AlphaCommandKind.SystemControl, "Select all command should be system-control kind.");
    Require(selectAllRoute.Target == "system-select-all", $"Expected system-select-all target, got '{selectAllRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system save", out var saveRoute), "Save command should route.");
    Require(saveRoute.Kind == AlphaCommandKind.SystemControl, "Save command should be system-control kind.");
    Require(saveRoute.Target == "system-save", $"Expected system-save target, got '{saveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system undo", out var undoRoute), "Undo command should route.");
    Require(undoRoute.Kind == AlphaCommandKind.SystemControl, "Undo command should be system-control kind.");
    Require(undoRoute.Target == "system-undo", $"Expected system-undo target, got '{undoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system redo", out var redoRoute), "Redo command should route.");
    Require(redoRoute.Kind == AlphaCommandKind.SystemControl, "Redo command should be system-control kind.");
    Require(redoRoute.Target == "system-redo", $"Expected system-redo target, got '{redoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system find", out var findRoute), "Find command should route.");
    Require(findRoute.Kind == AlphaCommandKind.SystemControl, "Find command should be system-control kind.");
    Require(findRoute.Target == "system-find", $"Expected system-find target, got '{findRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system new window", out var newWindowRoute), "New window command should route.");
    Require(newWindowRoute.Kind == AlphaCommandKind.SystemControl, "New window command should be system-control kind.");
    Require(newWindowRoute.Target == "system-new-window", $"Expected system-new-window target, got '{newWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system close window", out var closeWindowRoute), "Close window command should route.");
    Require(closeWindowRoute.Kind == AlphaCommandKind.SystemControl, "Close window command should be system-control kind.");
    Require(closeWindowRoute.Target == "system-close-window", $"Expected system-close-window target, got '{closeWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system move previous word", out var movePreviousWordRoute), "Move previous word command should route.");
    Require(movePreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Move previous word command should be system-control kind.");
    Require(movePreviousWordRoute.Target == "system-move-previous-word", $"Expected system-move-previous-word target, got '{movePreviousWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system move next word", out var moveNextWordRoute), "Move next word command should route.");
    Require(moveNextWordRoute.Kind == AlphaCommandKind.SystemControl, "Move next word command should be system-control kind.");
    Require(moveNextWordRoute.Target == "system-move-next-word", $"Expected system-move-next-word target, got '{moveNextWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select previous word", out var selectPreviousWordRoute), "Select previous word command should route.");
    Require(selectPreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Select previous word command should be system-control kind.");
    Require(selectPreviousWordRoute.Target == "system-select-previous-word", $"Expected system-select-previous-word target, got '{selectPreviousWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select next word", out var selectNextWordRoute), "Select next word command should route.");
    Require(selectNextWordRoute.Kind == AlphaCommandKind.SystemControl, "Select next word command should be system-control kind.");
    Require(selectNextWordRoute.Target == "system-select-next-word", $"Expected system-select-next-word target, got '{selectNextWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system delete previous word", out var deletePreviousWordRoute), "Delete previous word command should route.");
    Require(deletePreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Delete previous word command should be system-control kind.");
    Require(deletePreviousWordRoute.Target == "system-delete-previous-word", $"Expected system-delete-previous-word target, got '{deletePreviousWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system delete next word", out var deleteNextWordRoute), "Delete next word command should route.");
    Require(deleteNextWordRoute.Kind == AlphaCommandKind.SystemControl, "Delete next word command should be system-control kind.");
    Require(deleteNextWordRoute.Target == "system-delete-next-word", $"Expected system-delete-next-word target, got '{deleteNextWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system move previous sentence", out var movePreviousSentenceRoute), "Move previous sentence command should route.");
    Require(movePreviousSentenceRoute.Kind == AlphaCommandKind.SystemControl, "Move previous sentence command should be system-control kind.");
    Require(movePreviousSentenceRoute.Target == "system-move-previous-sentence", $"Expected system-move-previous-sentence target, got '{movePreviousSentenceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select next sentence", out var selectNextSentenceRoute), "Select next sentence command should route.");
    Require(selectNextSentenceRoute.Kind == AlphaCommandKind.SystemControl, "Select next sentence command should be system-control kind.");
    Require(selectNextSentenceRoute.Target == "system-select-next-sentence", $"Expected system-select-next-sentence target, got '{selectNextSentenceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system delete previous paragraph", out var deletePreviousParagraphRoute), "Delete previous paragraph command should route.");
    Require(deletePreviousParagraphRoute.Kind == AlphaCommandKind.SystemControl, "Delete previous paragraph command should be system-control kind.");
    Require(deletePreviousParagraphRoute.Target == "system-delete-previous-paragraph", $"Expected system-delete-previous-paragraph target, got '{deletePreviousParagraphRoute.Target}'.");

    foreach (var (phrase, expectedTarget) in new[]
             {
                 ("find files named alpha-notes", "alpha-notes"),
                 ("find my file project notes", "project notes"),
                 ("look for folder invoices", "invoices"),
                 ("search for file budget", "budget"),
                 ("search files for callsign", "callsign")
             })
    {
        Require(AlphaCommandRouter.TryRoute(phrase, out var naturalFileRoute), $"Natural file-search phrase should route: {phrase}");
        Require(naturalFileRoute.Kind == AlphaCommandKind.FileSearch, $"Natural phrase should be file-search kind: {phrase}");
        Require(naturalFileRoute.Target == expectedTarget, $"Expected target '{expectedTarget}' for '{phrase}', got '{naturalFileRoute.Target}'.");
    }

    Require(AlphaCommandRouter.TryRoute("start dictation", out var dictationRoute), "Dictation command should route.");
    Require(dictationRoute.Kind == AlphaCommandKind.Dictation, "Dictation command should be dictation kind.");

    Require(!AlphaCommandRouter.TryRoute("open notepad", out _), "Plain app launch should remain a Start menu launch, not a special command route.");
    Require(AlphaCommandRouter.TryRoute("browser back", out var backRoute), "Browser back command should route.");
    Require(backRoute.Kind == AlphaCommandKind.Browser, "Browser back should route as browser kind.");
    Require(backRoute.Target == "browser-back", $"Expected browser-back target, got '{backRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser refresh", out var refreshRoute), "Browser refresh command should route.");
    Require(refreshRoute.Target == "browser-refresh", $"Expected browser-refresh target, got '{refreshRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser new tab", out var newTabRoute), "Browser new tab command should route.");
    Require(newTabRoute.Target == "browser-new-tab", $"Expected browser-new-tab target, got '{newTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser forward", out var forwardRoute), "Browser forward command should route.");
    Require(forwardRoute.Target == "browser-forward", $"Expected browser-forward target, got '{forwardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser close tab", out var closeTabRoute), "Browser close tab command should route.");
    Require(closeTabRoute.Target == "browser-close-tab", $"Expected browser-close-tab target, got '{closeTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser focus address bar", out var focusAddressBarRoute), "Browser focus address bar command should route.");
    Require(focusAddressBarRoute.Target == "browser-focus-address-bar", $"Expected browser-focus-address-bar target, got '{focusAddressBarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find in page", out var findInPageRoute), "Browser find in page command should route.");
    Require(findInPageRoute.Target == "browser-find", $"Expected browser-find target, got '{findInPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find next", out var findNextRoute), "Browser find next command should route.");
    Require(findNextRoute.Target == "browser-find-next", $"Expected browser-find-next target, got '{findNextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find previous", out var findPreviousRoute), "Browser find previous command should route.");
    Require(findPreviousRoute.Target == "browser-find-previous", $"Expected browser-find-previous target, got '{findPreviousRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser scroll down", out var scrollDownRoute), "Browser scroll down command should route.");
    Require(scrollDownRoute.Target == "browser-scroll-down", $"Expected browser-scroll-down target, got '{scrollDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("scroll to top", out var scrollTopRoute), "Browser scroll top command should route.");
    Require(scrollTopRoute.Target == "browser-scroll-top", $"Expected browser-scroll-top target, got '{scrollTopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser zoom in", out var zoomInRoute), "Browser zoom in command should route.");
    Require(zoomInRoute.Target == "browser-zoom-in", $"Expected browser-zoom-in target, got '{zoomInRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser zoom out", out var zoomOutRoute), "Browser zoom out command should route.");
    Require(zoomOutRoute.Target == "browser-zoom-out", $"Expected browser-zoom-out target, got '{zoomOutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser zoom reset", out var zoomResetRoute), "Browser zoom reset command should route.");
    Require(zoomResetRoute.Target == "browser-zoom-reset", $"Expected browser-zoom-reset target, got '{zoomResetRoute.Target}'.");
}

static void ExtensionPackRegistryLoadsDropInPack()
{
    var registry = PackTestSupport.CreateRegistry();
    registry.RegisterPack(new SampleCommandPack());

    var packs = registry.GetPacks();
    Require(packs.Count == 1, $"Expected one pack, found {packs.Count}.");
    Require(packs[0].PackId == "sample-pack", $"Expected sample-pack id, got '{packs[0].PackId}'.");
    Require(packs[0].LoadStatus == CallsignPackLoadStatus.Loaded, $"Expected loaded pack status, got {packs[0].LoadStatus}.");

    Require(registry.TryResolve("sample pack echo hello from alpha", out var resolution), "Pack command should route.");
    Require(resolution.PackId == "sample-pack", $"Expected sample-pack pack id, got '{resolution.PackId}'.");
    Require(resolution.CommandId == "sample-echo", $"Expected sample-echo command id, got '{resolution.CommandId}'.");
    Require(resolution.ArgumentText == "hello from alpha", $"Expected argument text to survive routing, got '{resolution.ArgumentText}'.");

    var execution = new CallsignCommandExecutionContext(
        resolution.PackId,
        resolution.CommandId,
        "sample pack echo hello from alpha",
        "sample pack echo hello from alpha",
        resolution.ArgumentText,
        "Echo One",
        DateTimeOffset.UtcNow,
        CancellationToken.None);

    Require(registry.TryExecute(execution, out var result), "Pack execution should succeed.");
    Require(result.Succeeded, $"Pack execution should succeed, got message '{result.Message}'.");
    Require(result.Message.Contains("sample-pack", StringComparison.OrdinalIgnoreCase), $"Execution message should mention the pack, got '{result.Message}'.");
}

static void ExtensionPackRegistryCanDisableAndReenablePack()
{
    var registry = PackTestSupport.CreateRegistry();
    registry.RegisterPack(new SampleCommandPack());

    Require(registry.DisablePack("sample-pack"), "Pack should be disableable.");
    Require(!registry.TryResolve("sample pack echo hello", out _), "Disabled pack should not route.");

    Require(registry.EnablePack("sample-pack"), "Pack should be re-enableable.");
    Require(registry.TryResolve("sample pack echo hello", out var resolution), "Re-enabled pack should route again.");
    Require(resolution.PackId == "sample-pack", $"Expected sample-pack id after re-enable, got '{resolution.PackId}'.");
}

static void VoiceNavigationRoutesTabs()
{
    foreach (var (phrase, expectedTarget) in new[]
             {
                 ("next tab", "Next"),
                 ("previous tab", "Previous"),
                 ("open account", "Account"),
                 ("show voice tab", "Voice"),
                 ("go to session", "Session"),
                 ("switch to dictation", "Dictation"),
                 ("browser tab", "Browser"),
                 ("open files", "Files"),
                 ("open system", "System")
            })
    {
        Require(AlphaCommandRouter.TryRouteUiNavigation(phrase, out var routeTarget), $"Voice navigation should route: {phrase}");
        Require(string.Equals(routeTarget, expectedTarget, StringComparison.OrdinalIgnoreCase), $"Expected tab '{expectedTarget}' for '{phrase}', got '{routeTarget}'.");
    }

    Require(AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one next tab", "Callsign", "echo one").Target == "Next",
        "Next-tab transcript should resolve to the Next UI target.");
    Require(AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one previous tab", "Callsign", "echo one").Target == "Previous",
        "Previous-tab transcript should resolve to the Previous UI target.");

    var parsed = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open voice tab", "Callsign", "echo one");
    Require(parsed.Kind == AlphaVoiceIntentKind.UiNavigation, $"Expected UiNavigation, got {parsed.Kind}.");
    Require(string.Equals(parsed.Target, "Voice", StringComparison.OrdinalIgnoreCase), $"Expected Voice tab target, got '{parsed.Target}'.");

    var settings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open settings", "Callsign", "echo one");
    Require(settings.Kind == AlphaVoiceIntentKind.StartMenuLaunch, $"Expected StartMenuLaunch for settings, got {settings.Kind}.");
    Require(string.Equals(settings.Target, "Settings", StringComparison.OrdinalIgnoreCase), $"Expected Settings target, got '{settings.Target}'.");

    var explorer = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open file explorer", "Callsign", "echo one");
    Require(explorer.Kind == AlphaVoiceIntentKind.StartMenuLaunch, $"Expected StartMenuLaunch for file explorer, got {explorer.Kind}.");
    Require(string.Equals(explorer.Target, "File Explorer", StringComparison.OrdinalIgnoreCase), $"Expected File Explorer target, got '{explorer.Target}'.");

    var system = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one volume up", "Callsign", "echo one");
    Require(system.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {system.Kind}.");
    Require(system.Target == "system-volume-up", $"Expected system-volume-up target, got '{system.Target}'.");

    var nextWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one next window", "Callsign", "echo one");
    Require(nextWindow.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {nextWindow.Kind}.");
    Require(nextWindow.Target == "system-next-window", $"Expected system-next-window target, got '{nextWindow.Target}'.");

    var minimizeWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one minimize window", "Callsign", "echo one");
    Require(minimizeWindow.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {minimizeWindow.Kind}.");
    Require(minimizeWindow.Target == "system-minimize-window", $"Expected system-minimize-window target, got '{minimizeWindow.Target}'.");

    var pressEnter = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press enter", "Callsign", "echo one");
    Require(pressEnter.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressEnter.Kind}.");
    Require(pressEnter.Target == "system-press-enter", $"Expected system-press-enter target, got '{pressEnter.Target}'.");

    var pageDown = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one page down", "Callsign", "echo one");
    Require(pageDown.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pageDown.Kind}.");
    Require(pageDown.Target == "system-page-down", $"Expected system-page-down target, got '{pageDown.Target}'.");

    var click = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click", "Callsign", "echo one");
    Require(click.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {click.Kind}.");
    Require(click.Target == "system-mouse-click", $"Expected system-mouse-click target, got '{click.Target}'.");

    var copy = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system copy", "Callsign", "echo one");
    Require(copy.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {copy.Kind}.");
    Require(copy.Target == "system-copy", $"Expected system-copy target, got '{copy.Target}'.");

    var save = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system save", "Callsign", "echo one");
    Require(save.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {save.Kind}.");
    Require(save.Target == "system-save", $"Expected system-save target, got '{save.Target}'.");

    var movePreviousWord = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system move previous word", "Callsign", "echo one");
    Require(movePreviousWord.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {movePreviousWord.Kind}.");
    Require(movePreviousWord.Target == "system-move-previous-word", $"Expected system-move-previous-word target, got '{movePreviousWord.Target}'.");

    var movePreviousSentence = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system move previous sentence", "Callsign", "echo one");
    Require(movePreviousSentence.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {movePreviousSentence.Kind}.");
    Require(movePreviousSentence.Target == "system-move-previous-sentence", $"Expected system-move-previous-sentence target, got '{movePreviousSentence.Target}'.");
}

static void OverlayReadoutFormatterFollowsPhaseContract()
{
    Require(OverlayReadoutFormatter.FormatPhase(AlphaSessionState.WaitingForIdentity) == "Identity", "Identity phase should format correctly.");
    Require(OverlayReadoutFormatter.FormatPhase(AlphaSessionState.WaitingForCommand) == "Command", "Command phase should format correctly.");
    Require(OverlayReadoutFormatter.FormatPhase(AlphaSessionState.ReadyToLaunch) == "Ready", "Ready phase should format correctly.");
    Require(OverlayReadoutFormatter.FormatPhase(AlphaSessionState.Launching) == "Launching", "Launching phase should format correctly.");

    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForIdentity) == "Callsign heard. Say your callsign.", "Identity phase should prompt for callsign.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForIdentity, speechActive: true) == "Hearing your callsign...", "Identity phase should reflect live speech activity.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForIdentity, "womprat") == "Heard: womprat", "Identity transcript should echo heard text.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.Idle, "Callsign") == "Heard: Callsign", "Idle listening transcript should still be echoed.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForCommand, verifiedCallsign: "womprat") == "Identity confirmed. Say the command.", "Command phase should prompt for command.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForCommand, verifiedCallsign: "womprat", speechActive: true) == "Hearing your command...", "Command phase should reflect live speech activity.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForCommand, "open notepad", verifiedCallsign: "womprat") == "Command: open notepad", "Command transcript should show the command.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.ReadyToLaunch, pendingCommand: "open notepad") == "Command: open notepad", "Ready state should show command.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.Launching, pendingApp: "Notepad") == "Launching Notepad...", "Launching state should show target app.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.Idle, speechActive: true) == "Hearing speech...", "Idle speech activity should still show active listening.");
    Require(OverlayReadoutFormatter.FormatReadout(AlphaSessionState.Idle, dictationTranscript: "hello world", dictationActive: true) == "Dictation: hello world", "Dictation mode should prefer the dictated transcript.");
}

static void WakeOverlayReadoutUpdates()
{
    using var overlay = new WakeOverlayForm();
    Require(overlay.IsReady, "Wake overlay should load the bundled callsign.gif asset.");

    overlay.SetReadout("Heard: womprat", "Identity");
    Require(string.Equals(overlay.PhaseText, "IDENTITY", StringComparison.OrdinalIgnoreCase), $"Expected phase IDENTITY, got '{overlay.PhaseText}'.");
    Require(string.Equals(overlay.AccentName, "Identity", StringComparison.OrdinalIgnoreCase), $"Expected identity accent, got '{overlay.AccentName}'.");
    Require(overlay.ReadoutText == "Heard: womprat", $"Expected identity readout, got '{overlay.ReadoutText}'.");
    overlay.SetCaptionText("Heard: womprat");
    Require(overlay.CaptionText == "Heard: womprat", $"Expected caption strip to show transcript, got '{overlay.CaptionText}'.");
    Require(overlay.TranscriptHeadingText is "LIVE TRANSCRIPT" or "LAST HEARD", $"Expected a transcript heading, got '{overlay.TranscriptHeadingText}'.");
    overlay.ShowOverlay("Heard: womprat", "Identity", captionText: null);
    Require(!overlay.CaptionText.Contains("womprat", StringComparison.OrdinalIgnoreCase), "Caption strip should clear when no live transcript is available.");

    overlay.SetAudioActivity(0.75, "Mic: active", speechActive: true);
    Require(string.Equals(overlay.LiveBadgeText, "LIVE", StringComparison.OrdinalIgnoreCase), $"Expected live badge to show LIVE during speech-like readout, got '{overlay.LiveBadgeText}'.");
    Require(overlay.ActivityText.Contains("Mic", StringComparison.OrdinalIgnoreCase), $"Expected activity text to show microphone state, got '{overlay.ActivityText}'.");
    overlay.SetAuthorityText("Authoritative user runtime hearing audio");
    Require(overlay.AuthorityText.Contains("Authoritative user runtime hearing audio", StringComparison.OrdinalIgnoreCase), $"Expected authority text to show runtime ownership, got '{overlay.AuthorityText}'.");

    overlay.SetReadout("Command: open Notepad", "Command");
    Require(string.Equals(overlay.PhaseText, "COMMAND", StringComparison.OrdinalIgnoreCase), $"Expected phase COMMAND, got '{overlay.PhaseText}'.");
    Require(string.Equals(overlay.AccentName, "Command", StringComparison.OrdinalIgnoreCase), $"Expected command accent, got '{overlay.AccentName}'.");
    Require(overlay.ReadoutText == "Command: open Notepad", $"Expected command readout, got '{overlay.ReadoutText}'.");

    overlay.SetReadout("Launching Notepad...", "Launching");
    Require(string.Equals(overlay.PhaseText, "LAUNCHING", StringComparison.OrdinalIgnoreCase), $"Expected phase LAUNCHING, got '{overlay.PhaseText}'.");
    Require(string.Equals(overlay.AccentName, "Launching", StringComparison.OrdinalIgnoreCase), $"Expected launching accent, got '{overlay.AccentName}'.");
    Require(overlay.ReadoutText == "Launching Notepad...", $"Expected launch readout, got '{overlay.ReadoutText}'.");

    Require(Math.Abs(overlay.ActivityLevel - 0.75) < 0.001, $"Expected activity level 0.75, got {overlay.ActivityLevel:0.000}.");
    Require(string.Equals(overlay.ActivityText, "Mic: active", StringComparison.OrdinalIgnoreCase), $"Expected activity label, got '{overlay.ActivityText}'.");

    overlay.SetAudioActivity(0.0, "Mic: idle", speechActive: false);
    Require(string.Equals(overlay.LiveBadgeText, "READY", StringComparison.OrdinalIgnoreCase), $"Expected live badge to show READY when not hearing speech, got '{overlay.LiveBadgeText}'.");
    Require(Math.Abs(overlay.ActivityLevel - 0.0) < 0.001, $"Expected activity level 0.0, got {overlay.ActivityLevel:0.000}.");
    Require(string.Equals(overlay.ActivityText, "Mic: idle", StringComparison.OrdinalIgnoreCase), $"Expected activity label, got '{overlay.ActivityText}'.");

    overlay.SetTranscriptHistory(new[] { "[09:10] Callsign", "[09:11] womprat", "[09:12] open Notepad" });
    Require(overlay.HistoryText.Contains("Recent speech", StringComparison.OrdinalIgnoreCase), "Overlay history should be labeled as recent speech.");
    Require(overlay.HistoryText.Contains("womprat", StringComparison.OrdinalIgnoreCase), "Overlay history should include recent speech entries.");
    Require(overlay.HistoryText.Contains(Environment.NewLine), "Overlay history should render on multiple lines.");
}

static void VisibleControlsOverlayShowsFocusedTarget()
{
    using var overlay = new VisibleControlsOverlayForm();
    var annotations = new[]
    {
        new VisibleControlOverlayAnnotation(1, new Rectangle(10, 10, 100, 30), "Active account", false),
        new VisibleControlOverlayAnnotation(2, new Rectangle(10, 50, 100, 30), "Voice", true),
        new VisibleControlOverlayAnnotation(3, new Rectangle(10, 90, 100, 30), "Session", false)
    };

    overlay.ShowOverlay(
        new Rectangle(0, 0, 800, 600),
        "Visible controls for Setup",
        "Voice cue: Hearing your command...",
        "Heard: open notepad",
        new[] { "1. Active account", "2. Voice", "3. Session" },
        annotations);

    Require(overlay.FocusText.Contains("Focused: 2. Voice", StringComparison.OrdinalIgnoreCase), $"Expected focus label to show focused control, got '{overlay.FocusText}'.");
    Require(overlay.CueText.Contains("Hearing your command", StringComparison.OrdinalIgnoreCase), $"Expected cue label to show live voice cue, got '{overlay.CueText}'.");
    Require(overlay.HeardText.Contains("open notepad", StringComparison.OrdinalIgnoreCase), $"Expected heard label to show transcript, got '{overlay.HeardText}'.");
}

static void RuntimeSnapshotPreservesTranscriptHistory()
{
    var snapshot = new RuntimeStateSnapshot(
        ServiceState: "Listening",
        RuntimeRole: "user-runtime",
        StatusMessage: "Ready",
        ActiveCallsign: "womprat",
        VerifiedCallsign: "womprat",
        PendingCommand: "open notepad",
        PendingApp: "Notepad",
        LastLaunchedApp: null,
        IsListening: true,
        ModeDescription: "service-managed session",
        UpdatedUtc: DateTime.UtcNow,
        SessionState: "WaitingForCommand",
        LastTranscriptText: "open notepad",
        LastTranscriptConfidence: 0.94,
        LastTranscriptUpdatedUtc: DateTime.UtcNow,
        RecentTranscriptHistory: new[] { "[rehearsal] Callsign womprat", "[rehearsal] open Notepad" },
        OverlayReadout: "Command: open notepad",
        RuntimeAuthorityStatus: "authoritative-user-runtime",
        IsAuthoritativeUserRuntime: true,
        ServiceDictationHistory: new[] { "Hello world", "Open Notepad" });

    var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
    var roundTripped = System.Text.Json.JsonSerializer.Deserialize<RuntimeStateSnapshot>(json)
        ?? throw new InvalidOperationException("Runtime state snapshot did not deserialize.");

    Require(roundTripped.OverlayReadout == "Command: open notepad", "Overlay readout should survive snapshot serialization.");
    Require(roundTripped.RecentTranscriptHistory is { Count: 2 }, "Recent transcript history should survive snapshot serialization.");
    Require(roundTripped.RecentTranscriptHistory[0] == "[rehearsal] Callsign womprat", "First transcript history item should be preserved.");
    Require(roundTripped.RecentTranscriptHistory[1] == "[rehearsal] open Notepad", "Second transcript history item should be preserved.");
    Require(roundTripped.ServiceDictationHistory is { Count: 2 }, "Service dictation history should survive snapshot serialization.");
    Require(roundTripped.ServiceDictationHistory[0] == "Hello world", "First dictation history item should be preserved.");
    Require(roundTripped.ServiceDictationHistory[1] == "Open Notepad", "Second dictation history item should be preserved.");
    Require(roundTripped.RuntimeAuthorityStatus == "authoritative-user-runtime", "Runtime authority status should survive snapshot serialization.");
    Require(roundTripped.IsAuthoritativeUserRuntime == true, "Runtime authority flag should survive snapshot serialization.");
}

static void DictationVoiceActionsRecognized()
{
    foreach (var (phrase, expectedAction) in new[]
             {
                 ("copy dictation", DictationVoiceAction.Copy),
                 ("paste dictated text", DictationVoiceAction.Paste),
                 ("clear text", DictationVoiceAction.Clear),
                 ("select all", DictationVoiceAction.SelectAll),
                 ("highlight all", DictationVoiceAction.SelectAll),
                 ("cut text", DictationVoiceAction.Cut),
                 ("undo that", DictationVoiceAction.Undo),
                 ("redo that", DictationVoiceAction.Redo),
                 ("go to start", DictationVoiceAction.GoToStart),
                 ("go to end", DictationVoiceAction.GoToEnd),
                 ("select to start", DictationVoiceAction.SelectToStart),
                 ("select to end", DictationVoiceAction.SelectToEnd),
                 ("delete to start", DictationVoiceAction.DeleteToStart),
                 ("delete to end", DictationVoiceAction.DeleteToEnd),
                 ("go to line start", DictationVoiceAction.GoToLineStart),
                 ("go to line end", DictationVoiceAction.GoToLineEnd),
                 ("select to line start", DictationVoiceAction.SelectToLineStart),
                 ("select to line end", DictationVoiceAction.SelectToLineEnd),
                 ("delete to line start", DictationVoiceAction.DeleteToLineStart),
                 ("delete to line end", DictationVoiceAction.DeleteToLineEnd),
                 ("go to paragraph start", DictationVoiceAction.GoToParagraphStart),
                 ("go to paragraph end", DictationVoiceAction.GoToParagraphEnd),
                 ("select to paragraph start", DictationVoiceAction.SelectToParagraphStart),
                 ("select to paragraph end", DictationVoiceAction.SelectToParagraphEnd),
                 ("delete to paragraph start", DictationVoiceAction.DeleteToParagraphStart),
                 ("delete to paragraph end", DictationVoiceAction.DeleteToParagraphEnd),
                 ("new line", DictationVoiceAction.NewLine),
                 ("new paragraph", DictationVoiceAction.NewParagraph),
                 ("delete last word", DictationVoiceAction.DeleteLastWord),
                 ("select previous word", DictationVoiceAction.SelectPreviousWord),
                 ("select next word", DictationVoiceAction.SelectNextWord),
                 ("delete previous word", DictationVoiceAction.DeletePreviousWord),
                 ("select previous sentence", DictationVoiceAction.SelectPreviousSentence),
                 ("select next sentence", DictationVoiceAction.SelectNextSentence),
                 ("delete previous sentence", DictationVoiceAction.DeletePreviousSentence),
                 ("comma", DictationVoiceAction.Comma),
                 ("period", DictationVoiceAction.Period),
                 ("question mark", DictationVoiceAction.QuestionMark),
                 ("exclamation point", DictationVoiceAction.ExclamationMark),
                 ("semicolon", DictationVoiceAction.Semicolon),
                 ("colon", DictationVoiceAction.Colon),
                 ("apostrophe", DictationVoiceAction.Apostrophe)
             })
    {
        Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction(phrase) == expectedAction, $"Expected dictation action {expectedAction} for '{phrase}'.");
    }
}

static void DictationSpellingCommandsRecognized()
{
    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell alpha bravo charlie", out var natoSpelling) && natoSpelling is not null, "NATO spelling command should be recognized.");
    Require(natoSpelling.Text == "abc", $"Expected abc, got '{natoSpelling.Text}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell it out w o m p r a t", out var spelledOut) && spelledOut is not null, "Spell it out command should be recognized.");
    Require(spelledOut.Text == "womprat", $"Expected womprat, got '{spelledOut.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("type letter w o m p r a t", out var letterSpelling) && letterSpelling is not null, "Letter spelling command should be recognized.");
    Require(letterSpelling.Text == "womprat", $"Expected womprat, got '{letterSpelling.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("insert alpha underscore one", out var symbolSpelling) && symbolSpelling is not null, "Symbol spelling command should be recognized.");
    Require(symbolSpelling.Text == "a_1", $"Expected a_1, got '{symbolSpelling.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell womprat", out var wordSpelling) && wordSpelling is not null, "Single-word spelling command should be recognized.");
    Require(wordSpelling.Text == "womprat", $"Expected womprat, got '{wordSpelling.Text}'.");
}

static void VoiceHelpCommandRoutesSetupHelp()
{
    Require(AlphaCommandRouter.TryRoute("voice help", out var voiceHelpRoute), "Voice help command should route.");
    Require(voiceHelpRoute.Kind == AlphaCommandKind.UiAction, "Voice help should be a UI action.");
    Require(voiceHelpRoute.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{voiceHelpRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("what can i say", out var whatCanISayRoute), "What can I say command should route.");
    Require(whatCanISayRoute.Kind == AlphaCommandKind.UiAction, "What can I say should be a UI action.");
    Require(whatCanISayRoute.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{whatCanISayRoute.Target}'.");

    var parsed = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one what can I say", "Callsign", "echo one");
    Require(parsed.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {parsed.Kind}.");
    Require(parsed.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{parsed.Target}'.");
}

static void ScriptedVoiceIntentsCoverAlphaActions()
{
    var launch = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open note pad", "Callsign", "echo one");
    Require(launch.ContainsCallsign, "Launch transcript should contain callsign.");
    Require(launch.Kind == AlphaVoiceIntentKind.StartMenuLaunch, $"Expected StartMenuLaunch, got {launch.Kind}.");
    Require(launch.Target == "Notepad", $"Expected Notepad alias target, got '{launch.Target}'.");

    var dictation = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start dictation", "Callsign", "echo one");
    Require(dictation.ContainsCallsign, "Dictation transcript should contain callsign.");
    Require(dictation.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation, got {dictation.Kind}.");
    var repairWakeword = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one repair wakeword", "Callsign", "echo one");
    Require(repairWakeword.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {repairWakeword.Kind}.");
    Require(repairWakeword.Target == "ui-repair-wakeword", $"Expected ui-repair-wakeword target, got '{repairWakeword.Target}'.");
    var trainVoiceIdentity = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one train voice identity", "Callsign", "echo one");
    Require(trainVoiceIdentity.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {trainVoiceIdentity.Kind}.");
    Require(trainVoiceIdentity.Target == "ui-train-voice-identity", $"Expected ui-train-voice-identity target, got '{trainVoiceIdentity.Target}'.");
    var openLogsFolder = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open logs folder", "Callsign", "echo one");
    Require(openLogsFolder.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {openLogsFolder.Kind}.");
    Require(openLogsFolder.Target == "ui-open-logs-folder", $"Expected ui-open-logs-folder target, got '{openLogsFolder.Target}'.");
    var createAccount = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one create new account", "Callsign", "echo one");
    Require(createAccount.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {createAccount.Kind}.");
    Require(createAccount.Target == "ui-create-account", $"Expected ui-create-account target, got '{createAccount.Target}'.");
    var nextControl = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one next control", "Callsign", "echo one");
    Require(nextControl.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {nextControl.Kind}.");
    Require(nextControl.Target == "ui-next-control", $"Expected ui-next-control target, got '{nextControl.Target}'.");
    var activateControl = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one activate control", "Callsign", "echo one");
    Require(activateControl.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {activateControl.Kind}.");
    Require(activateControl.Target == "ui-activate-control", $"Expected ui-activate-control target, got '{activateControl.Target}'.");
    var pressRepairWakeword = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press repair wakeword", "Callsign", "echo one");
    Require(pressRepairWakeword.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {pressRepairWakeword.Kind}.");
    Require(pressRepairWakeword.Target == "ui-activate-label:repair wakeword", $"Expected ui-activate-label:repair wakeword target, got '{pressRepairWakeword.Target}'.");
    var clickTrainVoiceIdentity = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the train voice identity button", "Callsign", "echo one");
    Require(clickTrainVoiceIdentity.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickTrainVoiceIdentity.Kind}.");
    Require(clickTrainVoiceIdentity.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickTrainVoiceIdentity.Target}'.");
    var clickCallsign = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click callsign", "Callsign", "echo one");
    Require(clickCallsign.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickCallsign.Kind}.");
    Require(clickCallsign.Target == "ui-activate-label:callsign", $"Expected ui-activate-label:callsign target, got '{clickCallsign.Target}'.");
    var clickActiveAccount = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click active account", "Callsign", "echo one");
    Require(clickActiveAccount.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickActiveAccount.Kind}.");
    Require(clickActiveAccount.Target == "ui-activate-label:active account", $"Expected ui-activate-label:active account target, got '{clickActiveAccount.Target}'.");
    var clickBrowserTarget = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click browser target", "Callsign", "echo one");
    Require(clickBrowserTarget.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickBrowserTarget.Kind}.");
    Require(clickBrowserTarget.Target == "ui-activate-label:browser target", $"Expected ui-activate-label:browser target target, got '{clickBrowserTarget.Target}'.");
    var clickBrowserBack = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click browser back", "Callsign", "echo one");
    Require(clickBrowserBack.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickBrowserBack.Kind}.");
    Require(clickBrowserBack.Target == "ui-activate-label:browser back", $"Expected ui-activate-label:browser back target, got '{clickBrowserBack.Target}'.");
    var clickSearchResults = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click search results", "Callsign", "echo one");
    Require(clickSearchResults.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickSearchResults.Kind}.");
    Require(clickSearchResults.Target == "ui-activate-label:search results", $"Expected ui-activate-label:search results target, got '{clickSearchResults.Target}'.");
    var clickSystemVolumeUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click system volume up", "Callsign", "echo one");
    Require(clickSystemVolumeUp.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickSystemVolumeUp.Kind}.");
    Require(clickSystemVolumeUp.Target == "ui-activate-label:system volume up", $"Expected ui-activate-label:system volume up target, got '{clickSystemVolumeUp.Target}'.");
    var showNumbers = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show numbers", "Callsign", "echo one");
    Require(showNumbers.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showNumbers.Kind}.");
    Require(showNumbers.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNumbers.Target}'.");
    var hideVisibleControls = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide visible controls", "Callsign", "echo one");
    Require(hideVisibleControls.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideVisibleControls.Kind}.");
    Require(hideVisibleControls.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideVisibleControls.Target}'.");
    var clickThree = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click 3", "Callsign", "echo one");
    Require(clickThree.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickThree.Kind}.");
    Require(clickThree.Target == "ui-activate-label:3", $"Expected ui-activate-label:3 target, got '{clickThree.Target}'.");
    var volumeUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one volume up", "Callsign", "echo one");
    Require(volumeUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {volumeUp.Kind}.");
    Require(volumeUp.Target == "system-volume-up", $"Expected system-volume-up target, got '{volumeUp.Target}'.");
    var taskManager = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one task manager", "Callsign", "echo one");
    Require(taskManager.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {taskManager.Kind}.");
    Require(taskManager.Target == "system-open-task-manager", $"Expected system-open-task-manager target, got '{taskManager.Target}'.");
    var restoreWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one restore window", "Callsign", "echo one");
    Require(restoreWindow.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {restoreWindow.Kind}.");
    Require(restoreWindow.Target == "system-restore-window", $"Expected system-restore-window target, got '{restoreWindow.Target}'.");
    var pressTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press tab", "Callsign", "echo one");
    Require(pressTab.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressTab.Kind}.");
    Require(pressTab.Target == "system-press-tab", $"Expected system-press-tab target, got '{pressTab.Target}'.");
    var home = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one home key", "Callsign", "echo one");
    Require(home.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {home.Kind}.");
    Require(home.Target == "system-press-home", $"Expected system-press-home target, got '{home.Target}'.");
    var mouseScrollUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mouse scroll up", "Callsign", "echo one");
    Require(mouseScrollUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {mouseScrollUp.Kind}.");
    Require(mouseScrollUp.Target == "system-mouse-scroll-up", $"Expected system-mouse-scroll-up target, got '{mouseScrollUp.Target}'.");
    var selectAll = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system select all", "Callsign", "echo one");
    Require(selectAll.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {selectAll.Kind}.");
    Require(selectAll.Target == "system-select-all", $"Expected system-select-all target, got '{selectAll.Target}'.");
    var undo = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system undo", "Callsign", "echo one");
    Require(undo.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {undo.Kind}.");
    Require(undo.Target == "system-undo", $"Expected system-undo target, got '{undo.Target}'.");
    var selectNextWord = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system select next word", "Callsign", "echo one");
    Require(selectNextWord.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {selectNextWord.Kind}.");
    Require(selectNextWord.Target == "system-select-next-word", $"Expected system-select-next-word target, got '{selectNextWord.Target}'.");
    var selectNextSentence = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system select next sentence", "Callsign", "echo one");
    Require(selectNextSentence.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {selectNextSentence.Kind}.");
    Require(selectNextSentence.Target == "system-select-next-sentence", $"Expected system-select-next-sentence target, got '{selectNextSentence.Target}'.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("cut dictation") == DictationVoiceAction.Cut, "Cut dictation phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("undo dictation") == DictationVoiceAction.Undo, "Undo dictation phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("new paragraph") == DictationVoiceAction.NewParagraph, "New paragraph phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("select previous word") == DictationVoiceAction.SelectPreviousWord, "Select previous word phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("select next word") == DictationVoiceAction.SelectNextWord, "Select next word phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("delete previous word") == DictationVoiceAction.DeletePreviousWord, "Delete previous word phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("select previous sentence") == DictationVoiceAction.SelectPreviousSentence, "Select previous sentence phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("select next sentence") == DictationVoiceAction.SelectNextSentence, "Select next sentence phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("delete previous sentence") == DictationVoiceAction.DeletePreviousSentence, "Delete previous sentence phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("comma") == DictationVoiceAction.Comma, "Comma phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("period") == DictationVoiceAction.Period, "Period phrase should be recognized.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("replace previous word with ready", out var replaceWord) && replaceWord is not null, "Previous-word replacement should be recognized.");
    Require(replaceWord!.Scope == DictationReplacementScope.PreviousWord, "Replacement scope should be previous word.");
    Require(replaceWord.ReplacementText == "ready", $"Expected replacement text ready, got '{replaceWord.ReplacementText}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("replace previous sentence with ready to go", out var replaceSentence) && replaceSentence is not null, "Previous-sentence replacement should be recognized.");
    Require(replaceSentence!.Scope == DictationReplacementScope.PreviousSentence, "Replacement scope should be previous sentence.");
    Require(replaceSentence.ReplacementText == "ready to go", $"Expected replacement text ready to go, got '{replaceSentence.ReplacementText}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("replace that with fixed text", out var replaceThat) && replaceThat is not null, "Replace-that phrase should be recognized.");
    Require(replaceThat!.Scope == DictationReplacementScope.PreviousSentence, "Replace-that should map to previous sentence scope.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("change previous phrase to ready again", out var changePhrase) && changePhrase is not null, "Change previous phrase should be recognized.");
    Require(changePhrase!.Scope == DictationReplacementScope.PreviousSentence, "Change previous phrase should map to previous sentence scope.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("correct the previous word with fixed", out var correctWord) && correctWord is not null, "Correct previous word should be recognized.");
    Require(correctWord!.Scope == DictationReplacementScope.PreviousWord, "Correct previous word should map to previous word scope.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("replace all with final draft", out var replaceAll) && replaceAll is not null, "Replace-all phrase should be recognized.");
    Require(replaceAll!.Scope == DictationReplacementScope.AllText, "Replace-all should map to all text scope.");
    Require(replaceAll.ReplacementText == "final draft", $"Expected replacement text final draft, got '{replaceAll.ReplacementText}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("replace previous paragraph with fresh section", out var replaceParagraph) && replaceParagraph is not null, "Previous-paragraph replacement should be recognized.");
    Require(replaceParagraph!.Scope == DictationReplacementScope.PreviousParagraph, "Replacement scope should be previous paragraph.");
    Require(replaceParagraph.ReplacementText == "fresh section", $"Expected replacement text fresh section, got '{replaceParagraph.ReplacementText}'.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("go to line start") == DictationVoiceAction.GoToLineStart, "Go to line start should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("go to line end") == DictationVoiceAction.GoToLineEnd, "Go to line end should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("go to paragraph start") == DictationVoiceAction.GoToParagraphStart, "Go to paragraph start should be recognized.");
    Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction("go to paragraph end") == DictationVoiceAction.GoToParagraphEnd, "Go to paragraph end should be recognized.");

    var chrome = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open crome to example.com", "Callsign", "echo one");
    Require(chrome.ContainsCallsign, "Chrome transcript should contain callsign.");
    Require(chrome.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {chrome.Kind}.");
    Require(chrome.BrowserTarget == BrowserOpenTarget.Chrome, "Noisy Chrome transcript should prefer Chrome.");
    Require(chrome.Target == "example.com", $"Expected Chrome target example.com, got '{chrome.Target}'.");

    var browserBack = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser back", "Callsign", "echo one");
    Require(browserBack.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserBack.Kind}.");
    Require(browserBack.Target == "browser-back", $"Expected browser-back target, got '{browserBack.Target}'.");

    var browserRefresh = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser refresh", "Callsign", "echo one");
    Require(browserRefresh.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserRefresh.Kind}.");
    Require(browserRefresh.Target == "browser-refresh", $"Expected browser-refresh target, got '{browserRefresh.Target}'.");

    var browserForward = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser forward", "Callsign", "echo one");
    Require(browserForward.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserForward.Kind}.");
    Require(browserForward.Target == "browser-forward", $"Expected browser-forward target, got '{browserForward.Target}'.");

    var browserCloseTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser close tab", "Callsign", "echo one");
    Require(browserCloseTab.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserCloseTab.Kind}.");
    Require(browserCloseTab.Target == "browser-close-tab", $"Expected browser-close-tab target, got '{browserCloseTab.Target}'.");

    var browserFocusAddressBar = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser focus address bar", "Callsign", "echo one");
    Require(browserFocusAddressBar.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFocusAddressBar.Kind}.");
    Require(browserFocusAddressBar.Target == "browser-focus-address-bar", $"Expected browser-focus-address-bar target, got '{browserFocusAddressBar.Target}'.");

    var fileSearch = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one find file callsign", "Callsign", "echo one");
    Require(fileSearch.ContainsCallsign, "File search transcript should contain callsign.");
    Require(fileSearch.Kind == AlphaVoiceIntentKind.FileSearch, $"Expected FileSearch, got {fileSearch.Kind}.");
    Require(fileSearch.Target == "callsign", $"Expected file-search target callsign, got '{fileSearch.Target}'.");

    var missingIdentity = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign wrong user open note pad", "Callsign", "echo one");
    Require(!missingIdentity.ContainsCallsign, "Wrong callsign transcript should not verify identity.");
    Require(missingIdentity.Kind == AlphaVoiceIntentKind.StartMenuLaunch, "Wrong identity may parse an intent, but the service gate must block it before execution.");
}

static void WakeParserHandlesCommonWakePhrases()
{
    foreach (var phrase in new[]
             {
                 "Callsign echo one open Notepad",
                 "call sign echo one open Notepad",
                 "paul sign echo one open Notepad",
                 "wall sign echo one open Notepad"
             })
    {
        Require(AlphaVoiceTranscriptParser.ContainsWakeWord(phrase, "Callsign"), $"Wake parser did not accept '{phrase}'.");
        var command = AlphaVoiceTranscriptParser.NormalizeLaunchCommand(
            AlphaVoiceTranscriptParser.ExtractCommandFromTranscript(phrase, "Callsign", "echo one"));
        Require(command.Contains("notepad", StringComparison.OrdinalIgnoreCase), $"Wake extraction lost the app command for '{phrase}'.");
    }
}

static void WakeTranscriptCannotBypassWakeTransition()
{
    var session = new AlphaSessionStateMachine();
    var transcript = "Callsign echo one open Notepad";
    Require(AlphaVoiceTranscriptParser.ContainsWakeWord(transcript, "Callsign"), "Transcript should look wake-like for this guard test.");
    Require(session.State == AlphaSessionState.Idle, "Wake-like transcript text must not move the session unless the wake event/session transition is explicit.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must fail before an explicit wake transition.");
    Require(!session.TryBeginLaunch("Notepad", out _), "Launch must fail before wake and identity transitions.");
}

static void ServiceWorkerDoesNotPromoteTranscriptWake()
{
    var repoRoot = FindRepositoryRoot();
    var workerPath = Path.Combine(repoRoot, "src", "Callsign.Service", "CallsignRuntimeWorker.cs");
    Require(File.Exists(workerPath), $"Could not find service worker source at {workerPath}.");

    var source = File.ReadAllText(workerPath);
    Require(!source.Contains("transcript-wake-rescue", StringComparison.OrdinalIgnoreCase), "Service worker must not expose a transcript-only wake rescue engine.");
    Require(!source.Contains("IsStrictWakeTranscript", StringComparison.OrdinalIgnoreCase), "Service worker must not promote wake-like transcript text into a wake event.");
}

static void WakeDetectorUsesStreamingFramePredictions()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");

    var source = File.ReadAllText(servicePath);
    Require(source.Contains("np.frombuffer(raw_frame, dtype=np.int16)", StringComparison.OrdinalIgnoreCase), "Wake detector should convert streamed bytes into 16 kHz int16 frames.");
    Require(source.Contains("model.predict(frame)", StringComparison.OrdinalIgnoreCase), "Wake detector should score streaming frames instead of a single clip call.");
    Require(!source.Contains("predict_clip", StringComparison.OrdinalIgnoreCase), "Wake detector should not rely on clip-level prediction anymore.");
    Require(source.Contains("hop_milliseconds = 20", StringComparison.OrdinalIgnoreCase), "Wake detector should use overlapping frame hops for better recall.");
    Require(source.Contains("WakeWindowMilliseconds", StringComparison.OrdinalIgnoreCase), "Wake detector should keep a longer rolling context for recall.");
    Require(source.Contains("ConvertToWakePcm16", StringComparison.OrdinalIgnoreCase), "Wake service should build the live wake window from raw converted PCM.");
}

static void WakeFrameIsEvaluatedBeforeSegmentGating()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");

    var source = File.ReadAllText(servicePath);
    var wakeLine = source.IndexOf("wakeFrame = MicrophoneAudioProcessor.ConvertToWakePcm16", StringComparison.OrdinalIgnoreCase);
    var gateLine = source.IndexOf("if (_currentSegmentWriter == null)", StringComparison.OrdinalIgnoreCase);
    Require(wakeLine >= 0, "Wake service should capture raw PCM for wake evaluation.");
    Require(gateLine >= 0, "Wake service should still guard segment-only processing.");
    Require(wakeLine < gateLine, "Wake evaluation should begin before the segment gate can return early.");
    Require(source.Contains("wakeWindowSnapshot.Length < wakeWindowBytes / 16", StringComparison.OrdinalIgnoreCase), "Wake evaluation should start on a smaller live window before scoring.");
    Require(source.Contains("if (_wakeWindowBytes < maxBytes / 8)", StringComparison.OrdinalIgnoreCase), "Wake window should fill less before the first score is attempted.");
}

static void WakeEventForcesOverlayImmediately()
{
    var repoRoot = FindRepositoryRoot();
    var uiPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    var workerPath = Path.Combine(repoRoot, "src", "Callsign.Service", "CallsignRuntimeWorker.cs");
    Require(File.Exists(uiPath), $"Could not find UI source at {uiPath}.");
    Require(File.Exists(workerPath), $"Could not find service worker source at {workerPath}.");

    var uiSource = File.ReadAllText(uiPath);
    var handlerStart = uiSource.IndexOf("private void VoiceWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)", StringComparison.OrdinalIgnoreCase);
    Require(handlerStart >= 0, "UI wake handler should exist.");
    var handlerSource = uiSource[handlerStart..];
    var overlayCall = handlerSource.IndexOf("ShowWakeOverlay(activityLevel: BuildLocalOverlayActivityLevel()", StringComparison.OrdinalIgnoreCase);
    var detectCall = handlerSource.IndexOf("_session.DetectWakeWord();", StringComparison.OrdinalIgnoreCase);
    Require(overlayCall >= 0, "UI wake handler should show the overlay.");
    Require(detectCall >= 0, "UI wake handler should still advance the session.");
    Require(overlayCall < detectCall, "UI wake handler should show the overlay before session advancement work.");

    var workerSource = File.ReadAllText(workerPath);
    var workerHandlerStart = workerSource.IndexOf("private void VoiceWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)", StringComparison.OrdinalIgnoreCase);
    Require(workerHandlerStart >= 0, "Runtime worker wake handler should exist.");
    var workerHandlerSource = workerSource[workerHandlerStart..];
    var workerOverlay = workerHandlerSource.IndexOf("_overlayReadout = FormatOverlayReadout(_session.State);", StringComparison.OrdinalIgnoreCase);
    Require(workerOverlay >= 0, "Runtime worker wake handler should update the overlay readout.");
}

static void WakeOverlayIsPreloadedBeforeFirstWake()
{
    var repoRoot = FindRepositoryRoot();
    var uiPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    Require(File.Exists(uiPath), $"Could not find UI source at {uiPath}.");

    var uiSource = File.ReadAllText(uiPath);
    var constructorStart = uiSource.IndexOf("public MainForm()", StringComparison.OrdinalIgnoreCase);
    Require(constructorStart >= 0, "Main form constructor should exist.");
    var constructorSource = uiSource[constructorStart..];
    Require(constructorSource.Contains("PreloadWakeOverlay();", StringComparison.OrdinalIgnoreCase), "Main form should preload the wake overlay before listener startup.");
    Require(uiSource.Contains("private void PreloadWakeOverlay()", StringComparison.OrdinalIgnoreCase), "Main form should have a preload helper for the wake overlay.");
}

static void WakeDetectorIsWarmedUpBeforeLiveListening()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");

    var source = File.ReadAllText(servicePath);
    Require(source.Contains("WarmUpWakeWordDetectorAsync", StringComparison.OrdinalIgnoreCase), "Wake service should warm up the detector before live listening.");
    Require(source.Contains("wake-warmup", StringComparison.OrdinalIgnoreCase), "Wake warmup should write a silent fixture before the first live wake.");
    Require(source.Contains("Task.Run(() => WarmUpWakeWordDetectorAsync", StringComparison.OrdinalIgnoreCase), "Wake service should kick warmup off during listener startup.");
}

static void PackagedWakeTestHelperUsesStreamingFrames()
{
    var repoRoot = FindRepositoryRoot();
    var helperPath = Path.Combine(repoRoot, "src", "Callsign.Setup", "Payload", "testopenwakeword.ps1");
    Require(File.Exists(helperPath), $"Could not find packaged wake test helper at {helperPath}.");

    var source = File.ReadAllText(helperPath);
    Require(source.Contains("hop_milliseconds = 20", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should use overlapping frame hops.");
    Require(source.Contains("model.predict(frame)", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should score streaming frames.");
    Require(!source.Contains("predict_clip", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should not rely on clip-level prediction.");
}

static void WakeCalibrationHelperScoresEnrolledSamples()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    var formPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");
    Require(File.Exists(formPath), $"Could not find UI form source at {formPath}.");

    var serviceSource = File.ReadAllText(servicePath);
    Require(serviceSource.Contains("TryScoreWakeWordSampleAsync", StringComparison.OrdinalIgnoreCase), "Wake service should expose a sample scoring helper.");
    Require(serviceSource.Contains("ComputeCalibratedWakeThreshold", StringComparison.OrdinalIgnoreCase), "Wake service should expose a calibrated-threshold helper.");
    Require(serviceSource.Contains("ApplyWakeCalibration", StringComparison.OrdinalIgnoreCase), "Wake service should expose a calibration helper that persists metadata.");
    Require(serviceSource.Contains("score < 0.05", StringComparison.OrdinalIgnoreCase), "Wake calibration should ignore uselessly tiny scores.");
    Require(serviceSource.Contains("VoiceWakeCalibrationVersion", StringComparison.OrdinalIgnoreCase), "Wake calibration should persist provenance.");

    var formSource = File.ReadAllText(formPath);
    Require(formSource.Contains("TryScoreWakeWordSampleAsync", StringComparison.OrdinalIgnoreCase), "Activation should score enrolled wake samples.");
    Require(formSource.Contains("ApplyWakeCalibration", StringComparison.OrdinalIgnoreCase), "Activation should persist the wake threshold from enrolled samples.");
    Require(formSource.Contains("GetWakeCalibrationSamplePaths", StringComparison.OrdinalIgnoreCase), "Activation should prefer dedicated wake samples.");
    Require(formSource.Contains("wake-samples", StringComparison.OrdinalIgnoreCase), "Wake calibration should look for dedicated wake samples on disk.");
}

static void WakeCalibrationPersistsMetadata()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    var profilePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Models", "UserProfile.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");
    Require(File.Exists(profilePath), $"Could not find user settings source at {profilePath}.");

    var serviceSource = File.ReadAllText(servicePath);
    Require(serviceSource.Contains("VoiceWakeCalibrationVersion", StringComparison.OrdinalIgnoreCase), "Wake calibration should record a configuration version.");
    Require(serviceSource.Contains("VoiceWakeCalibrationSampleCount", StringComparison.OrdinalIgnoreCase), "Wake calibration should record how many samples were used.");
    Require(serviceSource.Contains("VoiceWakeCalibratedUtc", StringComparison.OrdinalIgnoreCase), "Wake calibration should record when the threshold was tuned.");
    Require(serviceSource.Contains("VoiceWakeCalibrationSource", StringComparison.OrdinalIgnoreCase), "Wake calibration should record which sample informed the threshold.");

    var profileSource = File.ReadAllText(profilePath);
    Require(profileSource.Contains("VoiceWakeCalibrationVersion", StringComparison.OrdinalIgnoreCase), "User settings should carry wake calibration metadata.");
    Require(profileSource.Contains("VoiceWakeCalibrationSampleCount", StringComparison.OrdinalIgnoreCase), "User settings should persist wake calibration sample count.");
    Require(profileSource.Contains("VoiceWakeCalibratedUtc", StringComparison.OrdinalIgnoreCase), "User settings should persist wake calibration timestamp.");
    Require(profileSource.Contains("VoiceWakeCalibrationSource", StringComparison.OrdinalIgnoreCase), "User settings should persist wake calibration source.");
}

static void WakeTrainingFormExposesWakeCalibration()
{
    var repoRoot = FindRepositoryRoot();
    var formPath = Path.Combine(repoRoot, "src", "Callsign.UI", "VoiceIdentityTrainingForm.cs");
    Require(File.Exists(formPath), $"Could not find voice training form source at {formPath}.");

    var source = File.ReadAllText(formPath);
    Require(source.Contains("Calibrate Wakeword", StringComparison.OrdinalIgnoreCase), "Voice training form should expose a wake calibration button.");
    Require(source.Contains("REC Wake Sample", StringComparison.OrdinalIgnoreCase), "Voice training form should expose a wake sample capture button.");
    Require(source.Contains("TryScoreWakeWordSampleAsync", StringComparison.OrdinalIgnoreCase), "Voice training form should call the wake scoring helper.");
    Require(source.Contains("ApplyWakeCalibration", StringComparison.OrdinalIgnoreCase), "Voice training form should apply a profile-specific wake threshold.");
    Require(source.Contains("GetRecordedWakeSamplePaths", StringComparison.OrdinalIgnoreCase), "Voice training form should maintain dedicated wake samples.");
}

static void WakeServiceEvaluatesRollingWindow()
{
    var repoRoot = FindRepositoryRoot();
    var servicePath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "VoiceCommandService.cs");
    Require(File.Exists(servicePath), $"Could not find voice service source at {servicePath}.");

    var source = File.ReadAllText(servicePath);
    Require(source.Contains("UpdateWakeWindowLocked", StringComparison.OrdinalIgnoreCase), "Wake service should maintain a rolling live window for detection.");
    Require(source.Contains("WakeWindowMilliseconds", StringComparison.OrdinalIgnoreCase), "Wake service should declare a rolling wake window duration.");
}

static void RuntimeStateWritesAreAtomic()
{
    var repoRoot = FindRepositoryRoot();
    var stateStorePath = Path.Combine(repoRoot, "src", "Callsign.Service", "RuntimeStateStore.cs");
    Require(File.Exists(stateStorePath), $"Could not find runtime state store source at {stateStorePath}.");

    var source = File.ReadAllText(stateStorePath);
    Require(source.Contains("File.Replace(tempPath, StatePath, null)", StringComparison.OrdinalIgnoreCase), "Runtime state should be written atomically before the UI watches it.");
    Require(source.Contains("File.Move(tempPath, StatePath)", StringComparison.OrdinalIgnoreCase), "Runtime state should create the file if it does not already exist.");
}

static string FindRepositoryRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(start);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Callsign.Service", "CallsignRuntimeWorker.cs"))
                && File.Exists(Path.Combine(directory.FullName, "CANON.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }
    }

    throw new InvalidOperationException("Could not locate the Callsign repository root.");
}
static int LiveLaunch(string appName)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: launching '{appName}' through the Callsign Start menu launcher.");

    var processName = Path.GetFileNameWithoutExtension(appName.Trim());
    var before = Process.GetProcessesByName(processName)
        .Select(process => process.Id)
        .ToHashSet();
    var launcher = new StartMenuLauncher();
    if (!launcher.Launch(appName, out var message))
    {
        Console.WriteLine($"FAIL: {message}");
        return 1;
    }

    Console.WriteLine($"INFO: {message}");
    Thread.Sleep(TimeSpan.FromSeconds(5));

    var launched = Process.GetProcessesByName(processName)
        .Where(process => !before.Contains(process.Id))
        .ToList();

    if (launched.Count == 0)
    {
        var existing = Process.GetProcessesByName(processName).ToList();
        if (before.Count > 0 && existing.Count > 0)
        {
            Console.WriteLine($"PASS: '{appName}' was already running and remained available after the Start menu launch path.");
            foreach (var process in existing)
                process.Dispose();
            return 0;
        }

        foreach (var process in existing)
            process.Dispose();

        Console.WriteLine($"FAIL: No new or existing '{appName}' process was detected.");
        return 1;
    }

    Console.WriteLine($"PASS: Detected new '{appName}' process through the Start menu launch path.");
    foreach (var process in launched)
    {
        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(1500))
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
        finally
        {
            process.Dispose();
        }
    }

    return 0;
}

static int LiveBrowser(string commandOrTarget)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: opening browser target '{commandOrTarget}'.");

    var service = new BrowserLaunchService();
    var target = commandOrTarget;
    var browserTarget = BrowserOpenTarget.Default;
    if (AlphaCommandRouter.TryRoute(commandOrTarget, out var route) && route.Kind == AlphaCommandKind.Browser)
    {
        target = route.Target;
        browserTarget = route.BrowserTarget;
    }

    if (service.TryOpen(target, out var message, out var targetUri, browserTarget: browserTarget))
    {
        Console.WriteLine($"PASS: {message}");
        Console.WriteLine($"INFO: Browser URI: {targetUri}");
        return 0;
    }

    Console.WriteLine($"FAIL: {message}");
    return 1;
}

static int LiveFileSearch(string query)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: searching files for '{query}' and opening the best result in Explorer.");

    var service = new FileSearchService();
    var report = service.Search(query, maxResults: 10);
    foreach (var warning in report.Warnings)
        Console.WriteLine($"WARN: {warning}");

    if (report.Results.Count == 0)
    {
        Console.WriteLine("FAIL: No file or folder results were found.");
        return 1;
    }

    var best = report.Results[0];
    Console.WriteLine($"INFO: Best result: {best.FullPath}");
    if (service.TryOpen(best, out var message))
    {
        Console.WriteLine($"PASS: {message}");
        return 0;
    }

    Console.WriteLine($"FAIL: {message}");
    return 1;
}

static int ScriptedSession(string transcript)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: running scripted gated alpha session for transcript '{transcript}'.");

    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();
    if (session.State != AlphaSessionState.WaitingForIdentity)
    {
        Console.WriteLine($"FAIL: Wake event did not move session to identity gate. State: {session.State}");
        return 1;
    }

    var identity = CallsignIdentityMatcher.Evaluate("echo one", 1.0f, "echo one");
    if (!identity.Accepted)
    {
        Console.WriteLine("FAIL: Scripted identity stage did not accept the expected callsign.");
        return 1;
    }

    if (!session.TryVerifyIdentity("echo one", "echo one", voiceEnrolled: true, out var identityMessage))
    {
        Console.WriteLine($"FAIL: Identity gate rejected transcript: {identityMessage}");
        return 1;
    }

    var commandTranscript = AlphaVoiceTranscriptParser.ExtractCommandFromTranscript(transcript, "Callsign", "echo one");
    var intent = AlphaVoiceIntentParser.ParseVerifiedTranscript(commandTranscript, "Callsign", "echo one");
    if (string.IsNullOrWhiteSpace(intent.NormalizedCommand))
    {
        Console.WriteLine("FAIL: No command was parsed from the transcript.");
        return 1;
    }

    if (!session.TryCaptureCommand(intent.NormalizedCommand, out var captureMessage))
    {
        Console.WriteLine($"FAIL: Command capture rejected transcript: {captureMessage}");
        return 1;
    }

    switch (intent.Kind)
    {
        case AlphaVoiceIntentKind.StartMenuLaunch:
            return ScriptedSessionLaunch(session, intent.Target);
        case AlphaVoiceIntentKind.Browser:
            return ScriptedSessionBrowser(session, intent);
        case AlphaVoiceIntentKind.FileSearch:
            return ScriptedSessionFileSearch(session, intent.Target);
        case AlphaVoiceIntentKind.Dictation:
            session.CompleteLaunch();
            Console.WriteLine("PASS: Scripted session reached service dictation mode after wake and identity gates.");
            return 0;
        default:
            Console.WriteLine($"FAIL: Unsupported or empty scripted intent: {intent.Kind}");
            return 1;
    }
}

static int ScriptedSessionLaunch(AlphaSessionStateMachine session, string target)
{
    if (string.IsNullOrWhiteSpace(target))
    {
        Console.WriteLine("FAIL: Scripted launch target was blank.");
        return 1;
    }

    if (!session.TryBeginLaunch(target, out var beginMessage))
    {
        Console.WriteLine($"FAIL: Scripted session could not begin launch: {beginMessage}");
        return 1;
    }

    var processName = Path.GetFileNameWithoutExtension(target.Trim());
    var before = Process.GetProcessesByName(processName)
        .Select(process => process.Id)
        .ToHashSet();
    var launcher = new StartMenuLauncher();
    if (!launcher.Launch(target, out var launchMessage))
    {
        Console.WriteLine($"FAIL: {launchMessage}");
        return 1;
    }

    Thread.Sleep(TimeSpan.FromSeconds(5));
    var launched = Process.GetProcessesByName(processName)
        .Where(process => !before.Contains(process.Id))
        .ToList();

    session.CompleteLaunch();
    Console.WriteLine(launched.Count == 0
        ? $"PASS: Scripted gated session opened '{target}' through Start menu search; no new process was observed because an existing instance may have handled the launch."
        : $"PASS: Scripted gated session launched '{target}' through Start menu search.");
    foreach (var process in launched)
    {
        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(1500))
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
        finally
        {
            process.Dispose();
        }
    }

    return 0;
}

static int ScriptedSessionBrowser(AlphaSessionStateMachine session, AlphaVoiceIntent intent)
{
    var service = new BrowserLaunchService();
    if (!service.TryOpen(intent.Target, out var message, out var targetUri, browserTarget: intent.BrowserTarget))
    {
        Console.WriteLine($"FAIL: {message}");
        return 1;
    }

    session.CompleteLaunch();
    Console.WriteLine($"PASS: Scripted gated session opened browser target {targetUri}.");
    return 0;
}

static int ScriptedSessionFileSearch(AlphaSessionStateMachine session, string query)
{
    var service = new FileSearchService();
    var report = service.Search(query, maxResults: 10);
    foreach (var warning in report.Warnings)
        Console.WriteLine($"WARN: {warning}");

    if (report.Results.Count == 0)
    {
        Console.WriteLine($"FAIL: No file or folder results matched '{query}'.");
        return 1;
    }

    if (!service.TryOpen(report.Results[0], out var message))
    {
        Console.WriteLine($"FAIL: {message}");
        return 1;
    }

    session.CompleteLaunch();
    Console.WriteLine($"PASS: Scripted gated session opened file-search result in Explorer. {message}");
    return 0;
}

static int VoiceListenerStartup()
{
    Console.WriteLine();
    Console.WriteLine("LIVE: starting the Callsign speech listener for a microphone/recognizer smoke check.");

    using var service = new VoiceCommandService();
    var errors = new List<string>();
    service.RecognitionError += (_, error) => errors.Add(error.Message);

    try
    {
        service.Start("en-US", "Callsign", "echo one");
        Thread.Sleep(TimeSpan.FromSeconds(3));

        if (!service.IsListening)
        {
            Console.WriteLine("FAIL: Voice listener did not remain active.");
            return 1;
        }

        Console.WriteLine("PASS: Voice listener initialized and remained active.");
        if (!string.IsNullOrWhiteSpace(service.LastStartupWarning))
            Console.WriteLine($"WARN: {service.LastStartupWarning}");

        foreach (var error in errors)
            Console.WriteLine($"WARN: Recognition event reported: {error}");

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL: Voice listener startup failed: {ex.Message}");
        return 1;
    }
    finally
    {
        service.Stop();
    }
}

static int OfflineSpeechRecognition(string phrase)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: synthesizing and recognizing voice phrase '{phrase}'.");

    var tempWave = Path.Combine(Path.GetTempPath(), $"callsign-alpha-speech-{Guid.NewGuid():N}.wav");
    try
    {
        using (var synthesizer = new SpeechSynthesizer())
        {
            synthesizer.SetOutputToWaveFile(tempWave);
            synthesizer.Speak(phrase);
        }

        using var recognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));
        recognizer.LoadGrammar(CreateAlphaGrammar("Callsign", "echo one"));
        recognizer.LoadGrammar(new DictationGrammar());
        recognizer.SetInputToWaveFile(tempWave);

        var result = recognizer.Recognize(TimeSpan.FromSeconds(10));
        if (result is null)
        {
            Console.WriteLine("FAIL: No speech recognition result was produced.");
            return 1;
        }

        Console.WriteLine($"INFO: Recognized '{result.Text}' with confidence {result.Confidence:0.00}.");
        var normalized = result.Text.ToLowerInvariant();
        if (!normalized.Contains("callsign") && !normalized.Contains("call sign"))
        {
            Console.WriteLine("FAIL: Recognized text did not include the wake word.");
            return 1;
        }

        if (!normalized.Contains("notepad"))
        {
            Console.WriteLine("FAIL: Recognized text did not include the target app.");
            return 1;
        }

        Console.WriteLine("PASS: Offline speech recognition produced the alpha wake/app phrase.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL: Offline speech recognition failed: {ex.Message}");
        return 1;
    }
    finally
    {
        if (File.Exists(tempWave))
            File.Delete(tempWave);
    }
}

static int InstalledRuntimeSmoke()
{
    Console.WriteLine();
    Console.WriteLine("LIVE: checking installed Callsign runtime artifacts.");

    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var appDir = Path.Combine(localAppData, "Callsign", "App");
    var logsDir = Path.Combine(localAppData, "Callsign", "Logs");
    var runtimeStatePath = Path.Combine(localAppData, "Callsign", "Runtime", "state.json");
    var startupErrorPath = Path.Combine(localAppData, "Callsign", "Logs", "startup-error.log");
    var installerErrorPath = Path.Combine(localAppData, "Callsign", "Logs", "installer-error.log");
    var openWakeWordSetupLogPath = Path.Combine(localAppData, "Callsign", "Logs", "openwakeword-setup.log");
    var openWakeWordModelPath = Path.Combine(localAppData, "Callsign", "Models", "callsign.onnx");
    var openWakeWordRuntimePythonPath = Path.Combine(localAppData, "Callsign", "Runtime", "openwakeword", "venv", "Scripts", "python.exe");
    var installedIconPath = Path.Combine(appDir, "callsign.ico");
    var desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Callsign.lnk");
    var startupShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Callsign Runtime.lnk");
    var startMenuShortcut = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft",
        "Windows",
        "Start Menu",
        "Programs",
        "Callsign",
        "Callsign.lnk");

    var failures = new List<string>();
    var installedUiPath = Path.Combine(appDir, "Callsign.UI.exe");
    var installedServicePath = Path.Combine(appDir, "Callsign.Service.exe");
    RequireInstalledDirectory(logsDir, failures);
    RequireInstalledFile(installedUiPath, failures);
    RequireInstalledFile(installedServicePath, failures);
    RequireInstalledFile(Path.Combine(appDir, "fzf.exe"), failures);
    RequireInstalledFile(Path.Combine(appDir, "setupopenwakeword.ps1"), failures);
    RequireInstalledFile(Path.Combine(appDir, "testopenwakeword.ps1"), failures);
    RequireInstalledFile(Path.Combine(appDir, "setuppyannote.ps1"), failures);
    RequireInstalledFile(Path.Combine(appDir, "testpyannote.ps1"), failures);
    RequireInstalledFile(Path.Combine(appDir, "pyannote_audio-4.0.4.tar.gz"), failures);
    RequireInstalledFile(installedIconPath, failures);
    RequireInstalledFile(openWakeWordRuntimePythonPath, failures);
    RequireInstalledFile(desktopShortcut, failures);
    RequireInstalledFile(startMenuShortcut, failures);
    RequireInstalledFile(startupShortcut, failures);
    RequireShortcutTargetAndIcon(desktopShortcut, installedUiPath, installedIconPath, expectedArguments: null, expectedWindowStyle: 1, failures);
    RequireShortcutTargetAndIcon(startMenuShortcut, installedUiPath, installedIconPath, expectedArguments: null, expectedWindowStyle: 1, failures);
    RequireShortcutTargetAndIcon(startupShortcut, installedServicePath, installedIconPath, expectedArguments: "--user-runtime --service-installed", expectedWindowStyle: 7, failures);

    if (File.Exists(installerErrorPath))
        failures.Add($"Installer error log exists: {installerErrorPath}");

    if (File.Exists(startupErrorPath))
        failures.Add($"Startup error log exists: {startupErrorPath}");

    Console.WriteLine(File.Exists(openWakeWordModelPath)
        ? $"INFO: openWakeWord Callsign model present: {openWakeWordModelPath}"
        : $"WARN: openWakeWord Callsign model missing; wake events are disabled until this file is installed: {openWakeWordModelPath}");
    Console.WriteLine(File.Exists(openWakeWordRuntimePythonPath)
        ? $"INFO: bundled openWakeWord runtime present: {openWakeWordRuntimePythonPath}"
        : $"WARN: bundled openWakeWord runtime missing: {openWakeWordRuntimePythonPath}");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "fzf.exe"))
        ? $"INFO: fzf file-search helper present: {Path.Combine(appDir, "fzf.exe")}"
        : "WARN: fzf file-search helper missing; file search will rely on built-in fallback.");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "setupopenwakeword.ps1"))
        ? $"INFO: openWakeWord setup helper present: {Path.Combine(appDir, "setupopenwakeword.ps1")}"
        : "WARN: openWakeWord setup helper missing from installed app folder.");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "testopenwakeword.ps1"))
        ? $"INFO: openWakeWord test helper present: {Path.Combine(appDir, "testopenwakeword.ps1")}"
        : "WARN: openWakeWord test helper missing from installed app folder.");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "setuppyannote.ps1"))
        ? $"INFO: pyannote setup helper present: {Path.Combine(appDir, "setuppyannote.ps1")}"
        : "WARN: pyannote setup helper missing from installed app folder.");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "testpyannote.ps1"))
        ? $"INFO: pyannote test helper present: {Path.Combine(appDir, "testpyannote.ps1")}"
        : "WARN: pyannote test helper missing from installed app folder.");
    Console.WriteLine(File.Exists(Path.Combine(appDir, "pyannote_audio-4.0.4.tar.gz"))
        ? $"INFO: pyannote.audio source tarball present: {Path.Combine(appDir, "pyannote_audio-4.0.4.tar.gz")}"
        : "WARN: pyannote.audio source tarball missing from installed app folder.");
    Console.WriteLine(File.Exists(openWakeWordSetupLogPath)
        ? $"INFO: openWakeWord setup log present: {openWakeWordSetupLogPath}"
        : $"INFO: openWakeWord setup log has not been created yet: {openWakeWordSetupLogPath}");
    Console.WriteLine(BrowserLaunchService.TryFindChrome(out var installedChromePath)
        ? $"INFO: Chrome browser target available: {installedChromePath}"
        : "WARN: Chrome was not found; Chrome-specific voice commands will fall back to the default browser.");

    var runtimeSnapshotFreshFromUserRuntime = false;
    if (File.Exists(runtimeStatePath))
    {
        try
        {
            var json = File.ReadAllText(runtimeStatePath);
            var snapshot = System.Text.Json.JsonSerializer.Deserialize<RuntimeStateSnapshot>(json);
            if (snapshot == null)
                failures.Add($"Runtime state could not be parsed: {runtimeStatePath}");
            else if (string.IsNullOrWhiteSpace(snapshot.ServiceState))
                failures.Add("Runtime state exists but ServiceState is blank.");
            else
            {
                var stateAge = DateTime.UtcNow - snapshot.UpdatedUtc.ToUniversalTime();
                Console.WriteLine($"INFO: Runtime state is '{snapshot.ServiceState}' with mode '{snapshot.ModeDescription}'.");
                Console.WriteLine(string.IsNullOrWhiteSpace(snapshot.RuntimeRole)
                    ? "INFO: Runtime role was not reported by this installed build."
                    : $"INFO: Runtime role: {snapshot.RuntimeRole}");
                Console.WriteLine(string.IsNullOrWhiteSpace(snapshot.CurrentWakeWordEngine)
                    ? "INFO: Runtime wake engine was not reported by this installed build."
                    : $"INFO: Runtime wake engine: {snapshot.CurrentWakeWordEngine}");
                Console.WriteLine($"INFO: Runtime state age: {stateAge.TotalSeconds:0} seconds.");
                if (stateAge > TimeSpan.FromMinutes(2))
                    failures.Add($"Runtime state is stale: last update was {stateAge.TotalSeconds:0} seconds ago.");
                if (!string.Equals(snapshot.RuntimeRole, "user-runtime", StringComparison.OrdinalIgnoreCase))
                    failures.Add($"Runtime state must be written by the user-runtime, but role was '{snapshot.RuntimeRole ?? "missing"}'.");
                else if (stateAge <= TimeSpan.FromMinutes(2))
                    runtimeSnapshotFreshFromUserRuntime = true;

                if (!string.IsNullOrWhiteSpace(snapshot.LastServiceActionKind))
                {
                    Console.WriteLine(
                        $"INFO: Last service action: kind={snapshot.LastServiceActionKind}; target={snapshot.LastServiceActionTarget}; success={snapshot.LastServiceActionSucceeded}; message={snapshot.LastServiceActionMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add($"Runtime state is not readable JSON: {ex.Message}");
        }
    }
    else
    {
        failures.Add($"Runtime state file is missing: {runtimeStatePath}");
    }

    var serviceProcessCount = Process.GetProcessesByName("Callsign.Service").Length;
    var serviceProcessRunning = serviceProcessCount > 0;
    var uiProcessRunning = Process.GetProcessesByName("Callsign.UI").Any();
    var serviceRegistered = IsWindowsServiceRegistered("Callsign");
    var serviceRunning = IsWindowsServiceRunning("Callsign");
    var userRuntimeProcessCount = CountCallsignServiceProcessesWithArgument("--user-runtime");
    Console.WriteLine($"INFO: Callsign Windows service registered: {serviceRegistered}");
    Console.WriteLine($"INFO: Callsign Windows service running: {serviceRunning}");
    Console.WriteLine($"INFO: Callsign.Service process count: {serviceProcessCount}");
    Console.WriteLine($"INFO: Callsign user-runtime process count: {userRuntimeProcessCount}");
    Console.WriteLine($"INFO: Callsign.UI running: {uiProcessRunning}");
    if (!serviceRegistered)
        failures.Add("Callsign Windows service is not registered after install.");
    if (!serviceRunning)
        failures.Add("Callsign Windows service is not running after install.");
    if (!serviceProcessRunning)
        failures.Add("Callsign.Service is not running after install.");
    if (userRuntimeProcessCount != 1 && !runtimeSnapshotFreshFromUserRuntime)
        failures.Add($"Expected exactly one Callsign --user-runtime process after install, got {userRuntimeProcessCount}.");
    if (!uiProcessRunning)
        failures.Add("Callsign.UI is not running after install.");

    if (failures.Count == 0)
    {
        Console.WriteLine("PASS: Installed runtime artifacts and service/config manager processes are present.");
        return 0;
    }

    Console.WriteLine("FAIL: Installed runtime smoke found issues:");
    foreach (var failure in failures)
        Console.WriteLine($"- {failure}");
    return 1;
}

static int WatchServiceAction(string secondsValue)
{
    var timeoutSeconds = int.TryParse(secondsValue, out var parsed)
        ? Math.Clamp(parsed, 5, 300)
        : 60;
    var statePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Runtime",
        "state.json");
    var baseline = ReadRuntimeSnapshot(statePath);
    var baselineActionUtc = baseline?.LastServiceActionUtc;
    var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

    Console.WriteLine();
    Console.WriteLine($"LIVE: watching for a new Callsign service action for {timeoutSeconds} seconds.");
    Console.WriteLine("INFO: Speak a full gated command now, for example: 'Callsign echo one open Notepad'.");

    while (DateTime.UtcNow < deadline)
    {
        Thread.Sleep(1000);
        var current = ReadRuntimeSnapshot(statePath);
        if (current == null)
            continue;

        if (!current.LastServiceActionUtc.HasValue)
            continue;

        if (baselineActionUtc.HasValue && current.LastServiceActionUtc.Value <= baselineActionUtc.Value)
            continue;

        Console.WriteLine(
            $"INFO: New action: kind={current.LastServiceActionKind}; target={current.LastServiceActionTarget}; success={current.LastServiceActionSucceeded}; message={current.LastServiceActionMessage}");

        if (current.LastServiceActionSucceeded == true)
        {
            Console.WriteLine("PASS: Installed service recorded a fresh successful voice-gated action.");
            return 0;
        }

        Console.WriteLine("FAIL: Installed service recorded a fresh action, but it was not successful.");
        return 1;
    }

    Console.WriteLine("FAIL: No fresh Callsign service action was recorded before the timeout.");
    return 1;
}

static RuntimeStateSnapshot? ReadRuntimeSnapshot(string statePath)
{
    try
    {
        if (!File.Exists(statePath))
            return null;

        var json = File.ReadAllText(statePath);
        return System.Text.Json.JsonSerializer.Deserialize<RuntimeStateSnapshot>(json);
    }
    catch
    {
        return null;
    }
}

static void RequireInstalledFile(string path, List<string> failures)
{
    if (!File.Exists(path))
        failures.Add($"Missing installed file: {path}");
}

static void RequireInstalledDirectory(string path, List<string> failures)
{
    if (!Directory.Exists(path))
        failures.Add($"Missing installed directory: {path}");
}

static void RequireShortcutTargetAndIcon(
    string shortcutPath,
    string expectedTargetPath,
    string expectedIconPath,
    string? expectedArguments,
    int expectedWindowStyle,
    List<string> failures)
{
    if (!File.Exists(shortcutPath))
        return;

    try
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            failures.Add("Windows Script Host is not available for shortcut inspection.");
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not be started.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        var targetPath = ((string?)shortcut.TargetPath ?? string.Empty).Trim();
        var iconLocation = ((string?)shortcut.IconLocation ?? string.Empty).Trim();
        var arguments = ((string?)shortcut.Arguments ?? string.Empty).Trim();
        var windowStyle = (int)shortcut.WindowStyle;

        if (!Path.GetFullPath(targetPath).Equals(Path.GetFullPath(expectedTargetPath), StringComparison.OrdinalIgnoreCase))
            failures.Add($"Shortcut target mismatch for {shortcutPath}: {targetPath}");

        var iconPath = iconLocation.Split(',', 2)[0].Trim();
        if (!Path.GetFullPath(iconPath).Equals(Path.GetFullPath(expectedIconPath), StringComparison.OrdinalIgnoreCase))
            failures.Add($"Shortcut icon mismatch for {shortcutPath}: {iconLocation}");

        if (expectedArguments != null && !string.Equals(arguments, expectedArguments, StringComparison.OrdinalIgnoreCase))
            failures.Add($"Shortcut arguments mismatch for {shortcutPath}: {arguments}");

        if (windowStyle != expectedWindowStyle)
            failures.Add($"Shortcut window style mismatch for {shortcutPath}: expected {expectedWindowStyle}, got {windowStyle}.");
    }
    catch (Exception ex)
    {
        failures.Add($"Unable to inspect shortcut {shortcutPath}: {ex.Message}");
    }
}

static bool IsWindowsServiceRegistered(string serviceName)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"query {serviceName}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process == null)
            return false;

        process.WaitForExit(5000);
        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static bool IsWindowsServiceRunning(string serviceName)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"query {serviceName}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process == null)
            return false;

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return process.ExitCode == 0
            && output.Contains("STATE", StringComparison.OrdinalIgnoreCase)
            && output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}

static int CountCallsignServiceProcessesWithArgument(string argument)
{
    try
    {
        var safeArgument = argument.Replace("'", "''");
        var command = "$p = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'Callsign.Service.exe' -and $_.CommandLine -like '*" + safeArgument + "*' }); $p.Count";
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process == null)
            return -1;

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit(5000);
        return process.ExitCode == 0 && int.TryParse(output, out var count)
            ? count
            : -1;
    }
    catch
    {
        return -1;
    }
}

static Grammar CreateAlphaGrammar(string wakeWord, string callsign)
{
    var wakeChoices = new Choices(wakeWord, "call sign");
    var callsignChoices = new Choices(callsign, callsign.Replace(' ', '-'), callsign.Replace(' ', '_'));
    var actionChoices = new Choices("open", "launch", "start", "run");
    var builder = new GrammarBuilder { Culture = new CultureInfo("en-US") };
    builder.Append(wakeChoices);
    builder.Append(callsignChoices, 0, 1);
    builder.Append(actionChoices, 0, 1);
    builder.AppendDictation();
    return new Grammar(builder) { Name = "Callsign alpha offline command smoke" };
}

static string? GetArgumentValue(string[] values, string name)
{
    for (var index = 0; index < values.Length - 1; index++)
    {
        if (string.Equals(values[index], name, StringComparison.OrdinalIgnoreCase))
            return values[index + 1];
    }

    return null;
}

static bool HasArgument(string[] values, string name) =>
    values.Any(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}


