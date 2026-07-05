using Callsign.UI.Models;
using Callsign.UI.Services;
using Callsign.UI;
using Callsign.Extensions;
using Callsign.AlphaSmoke;
using NAudio.Wave;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Text;
using System.Text.Json;

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
    ("dictation vocabulary stores local profile words", DictationVocabularyStoresLocalProfileWords),
    ("dictation review options shape incoming visible text", DictationReviewOptionsShapeIncomingVisibleText),
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
    ("session command capture requires biometric identity proof", SessionCommandCaptureRequiresBiometricIdentityProof),
    ("identity matcher allows near text miss only after biometric match", IdentityMatcherAllowsNearTextMissOnlyAfterBiometricMatch),
    ("local voice biometric verifier compares enrolled audio", LocalVoiceBiometricVerifierComparesEnrolledAudio),
    ("local voice biometric verifier rejects stale and replayed samples", LocalVoiceBiometricVerifierRejectsReplaySamples),
    ("multi-sample enrollment rejects reused single-file submissions", MultiSampleEnrollmentRejectsReusedSingleFile),
    ("pyannote biometric verifier fails closed until enrolled", PyannoteBiometricVerifierFailsClosedUntilEnrolled),
    ("identity gate requires a separate command turn", IdentityGateRequiresSeparateCommandTurn),
    ("Start menu alpha scope accepts plain app names and rejects command text", StartMenuScopeValidation),
    ("Start menu launcher can resolve installed app names", StartMenuResolution),
    ("Start menu launcher requires confirmation for ambiguous app matches", StartMenuAmbiguousResolutionRequiresConfirmation),
    ("Start menu launcher normalizes common speech aliases", StartMenuSpeechAliasResolution),
    ("Start menu launcher resolves trusted system surfaces", TrustedSystemSurfaceResolution),
    ("visible window switching resolves named matches and ambiguity", VisibleWindowSwitchingResolvesNamedMatchesAndAmbiguity),
    ("browser helper resolves URLs and search phrases", BrowserTargetResolution),
    ("browser action execution covers continuous scrolling controls", BrowserActionExecutionCoversContinuousScrollingControls),
    ("file search helper finds files in the intended scope", FileSearchResolution),
    ("verified service command router classifies alpha actions", ServiceCommandRouterClassifiesAlphaActions),
    ("extension pack registry loads drop-in command packs", ExtensionPackRegistryLoadsDropInPack),
    ("extension pack registry can disable and re-enable packs", ExtensionPackRegistryCanDisableAndReenablePack),
    ("extension pack imports community DLLs disabled by default", ExtensionPackImportDisablesCommunityDllByDefault),
    ("extension pack import marks imported packs and builds splash manifest", ExtensionPackImportMarksImportedPacksAndBuildsSplashManifest),
    ("extension pack folder import expands dlls", ExtensionPackFolderImportExpandsDlls),
    ("extension pack removal and reimport work as rollback", ExtensionPackRemovalAndReimportWorksAsRollback),
    ("extension pack import can overwrite an installed copy", ExtensionPackImportCanOverwriteInstalledCopy),
    ("extension pack registry rejects invalid metadata", ExtensionPackRegistryRejectsInvalidMetadata),
    ("extension pack registry gates paid tiers by entitlement", ExtensionPackRegistryGatesPaidTiersByEntitlement),
    ("extension pack registry gates command tiers by entitlement", ExtensionPackRegistryGatesCommandTiersByEntitlement),
    ("extension pack registry requires signatures when declared", ExtensionPackRegistryRequiresSignaturesWhenDeclared),
    ("extension pack execution enforces policy at registry boundary", ExtensionPackExecutionEnforcesPolicyAtRegistryBoundary),
    ("voice shortcuts store persists local shortcut definitions", VoiceShortcutsStorePersistsLocalShortcutDefinitions),
    ("voice shortcuts pack exposes enabled shortcuts and follow-up steps", VoiceShortcutsPackExposesEnabledShortcutsAndFollowUpSteps),
    ("voice shortcuts surface is wired into routing and discovery", VoiceShortcutsSurfaceIsWiredIntoRoutingAndDiscovery),
    ("alpha audit log writes correlation and verification fields", AlphaAuditLogWritesCorrelationAndVerificationFields),
    ("command policy evaluates parity metadata", CommandPolicyEvaluatesParityMetadata),
    ("update manifest carries splash command changes", UpdateManifestCarriesSplashCommandChanges),
    ("update splash presents manifest details", UpdateSplashPresentsManifestDetails),
    ("update check failure does not advance the due window", UpdateCheckFailureDoesNotAdvanceDueWindow),
    ("update check success advances the due window", UpdateCheckSuccessAdvancesDueWindow),
    ("update status surfaces cadence and next due", UpdateCheckServiceStatusIncludesCadenceAndNextDue),
    ("update timer uses a 25-hour cadence and startup forces a visible check", UpdateTimerUsesTwentyFiveHourCadenceAndStartupForcesCheck),
    ("startup walkthrough presents clean install steps", StartupWalkthroughPresentsCleanInstallSteps),
    ("startup walkthrough is reachable from account tab", StartupWalkthroughIsReachableFromAccountTab),
    ("alpha v1 checklist verifies walkthrough artifacts", AlphaV1ChecklistVerifiesWalkthroughArtifacts),
    ("voice access parity evidence script preserves release gates", VoiceAccessParityEvidenceScriptPreservesReleaseGates),
    ("voice tab explains enrollment next steps and failures", VoiceTabExplainsEnrollmentNextStepsAndFailures),
    ("voice navigation routes Callsign tabs", VoiceNavigationRoutesTabs),
    ("voice help command routes setup help", VoiceHelpCommandRoutesSetupHelp),
    ("command discovery lists built-in and extension commands", CommandDiscoveryListsBuiltInAndExtensionCommands),
    ("shared visual style defines macOS Voice Control evidence tokens", SharedVisualStyleDefinesEvidenceTokens),
    ("command palette filters commands with macOS-style status", CommandPaletteFiltersCommandsWithStatus),
    ("verified session routes built-in parity command families", VerifiedSessionRoutesBuiltInParityFamilies),
    ("system control dry-run covers app switching and window management", SystemControlDryRunCoversAppSwitchingAndWindowManagement),
    ("system control dry-run covers mouse and scrolling", SystemControlDryRunCoversMouseAndScrolling),
    ("system control dry-run covers keyboard commands", SystemControlDryRunCoversKeyboardCommands),
    ("system control dry-run covers safe settings", SystemControlDryRunCoversSafeSettings),
    ("overlay readout formatter follows the phase contract", OverlayReadoutFormatterFollowsPhaseContract),
    ("wake overlay exposes phase and live readout", WakeOverlayReadoutUpdates),
    ("visible controls overlay shows the focused target", VisibleControlsOverlayShowsFocusedTarget),
    ("desktop visible controls prioritize actionable targets", DesktopVisibleControlsPrioritizeActionableTargets),
    ("desktop visible controls normalize UI Automation labels", DesktopVisibleControlsNormalizeLabels),
    ("desktop visible controls expose taskbar capture support", DesktopVisibleControlsExposeTaskbarCaptureSupport),
    ("desktop visible controls expose named-window capture support", DesktopVisibleControlsExposeNamedWindowCaptureSupport),
    ("mouse grid supports current-window scope", MouseGridSupportsCurrentWindowScope),
    ("mouse grid supports marked drag and undo", MouseGridSupportsMarkedDragAndUndo),
    ("mouse grid overlay calculates numbered cells", MouseGridOverlayCalculatesNumberedCells),
    ("keyboard overlay presents visible keys", KeyboardOverlayPresentsVisibleKeys),
    ("runtime snapshot preserves transcript readout history", RuntimeSnapshotPreservesTranscriptHistory),
    ("runtime state monitor reads controlled runtime state", RuntimeStateMonitorReadsControlledRuntimeState),
    ("runtime mic status formatter explains authoritative audio", RuntimeMicStatusFormatterExplainsAuthoritativeAudio),
    ("runtime hearing proof formatter shows mic and packet state", RuntimeHearingProofFormatterShowsMicAndPacketState),
    ("runtime authority formatter explains listener ownership", RuntimeAuthorityFormatterExplainsListenerOwnership),
    ("runtime ownership evaluator explains duplicate runtimes", RuntimeOwnershipEvaluatorExplainsDuplicateRuntimes),
    ("dictation voice actions are recognized", DictationVoiceActionsRecognized),
    ("dictation spelling commands are recognized", DictationSpellingCommandsRecognized),
    ("dictation target-text commands are recognized", DictationTargetTextCommandsRecognized),
    ("dictation formatting commands are recognized", DictationFormattingCommandsRecognized),
    ("dictation correction alternatives are recognized", DictationCorrectionAlternativesRecognized),
    ("dictation paste blocks sensitive targets", DictationPasteBlocksSensitiveTargets),
    ("scripted voice intents cover alpha service actions", ScriptedVoiceIntentsCoverAlphaActions),
    ("wake parser accepts split and common homophone wake phrases", WakeParserHandlesCommonWakePhrases),
    ("wake transition source distinguishes audio detector from scripted control", WakeTransitionSourceDistinguishesAudioFromScriptedControl),
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
    ("voice identity training surface explains enrollment next steps and failures", VoiceIdentityTrainingSurfaceExplainsNextStepsAndFailures),
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

static void DictationVocabularyStoresLocalProfileWords()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "Echo One",
            DisplayName = "Echo Operator",
            Settings =
            {
                DictationFluidModeEnabled = true,
                DictationAutomaticPunctuationEnabled = false,
                DictationProfanityFilterEnabled = false
            }
        };

        var added = DictationVocabularyService.Add(profile, "Womprat");
        Require(added.Status == DictationVocabularyAddStatus.Added, $"Expected Added, got {added.Status}.");
        Require(added.Word == "womprat", $"Expected normalized word 'womprat', got '{added.Word}'.");
        Require(profile.Settings.DictationVocabulary.Contains("womprat"), "Vocabulary list should contain womprat.");

        var phrase = DictationVocabularyService.Add(profile, "Project Zephyr");
        Require(phrase.Status == DictationVocabularyAddStatus.Added, $"Expected phrase Added, got {phrase.Status}.");
        Require(profile.Settings.DictationVocabulary.Contains("project zephyr"), "Vocabulary list should contain project zephyr.");

        var duplicate = DictationVocabularyService.Add(profile, "WOMPRAT");
        Require(duplicate.Status == DictationVocabularyAddStatus.AlreadyExists, $"Expected AlreadyExists, got {duplicate.Status}.");
        Require(profile.Settings.DictationVocabulary.Count == 2, $"Duplicate vocabulary should not grow the list, got {profile.Settings.DictationVocabulary.Count}.");

        var invalid = DictationVocabularyService.Add(profile, "!");
        Require(invalid.Status == DictationVocabularyAddStatus.Invalid, $"Expected Invalid, got {invalid.Status}.");

        store.Save(profile);
        var loaded = store.Load("echo one") ?? throw new InvalidOperationException("Profile did not load after vocabulary save.");
        Require(loaded.Settings.DictationVocabulary.Contains("womprat"), "Vocabulary word should persist to profile settings.");
        Require(loaded.Settings.DictationVocabulary.Contains("project zephyr"), "Vocabulary phrase should persist to profile settings.");
        Require(loaded.Settings.DictationFluidModeEnabled, "Fluid dictation setting should persist to profile settings.");
        Require(!loaded.Settings.DictationAutomaticPunctuationEnabled, "Automatic punctuation setting should persist to profile settings.");
        Require(!loaded.Settings.DictationProfanityFilterEnabled, "Profanity filter setting should persist to profile settings.");
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
            // Temporary profile cleanup is best-effort on Windows.
        }
    }
}

static void DictationReviewOptionsShapeIncomingVisibleText()
{
    var automaticPunctuation = DictationReviewTextService.AppendReviewedText(
        string.Empty,
        "hello world",
        DictationCasingMode.Default,
        fluidDictationEnabled: false,
        automaticPunctuationEnabled: true,
        profanityFilterEnabled: false);
    Require(automaticPunctuation == "Hello world.", $"Expected automatic punctuation to add sentence casing and a period, got '{automaticPunctuation}'.");

    var profanityFiltered = DictationReviewTextService.AppendReviewedText(
        automaticPunctuation,
        "this is shit",
        DictationCasingMode.Default,
        fluidDictationEnabled: false,
        automaticPunctuationEnabled: true,
        profanityFilterEnabled: true);
    Require(profanityFiltered == "Hello world. This is s**t.", $"Expected profanity filter to mask the visible review text, got '{profanityFiltered}'.");

    var rawReviewText = DictationReviewTextService.BuildReviewedText(
        ["hello world", "still raw"],
        DictationCasingMode.Default,
        fluidDictationEnabled: false,
        automaticPunctuationEnabled: false,
        profanityFilterEnabled: false);
    Require(rawReviewText == "hello world still raw", $"Expected raw dictation review text when options are off, got '{rawReviewText}'.");

    var fluidReviewText = DictationReviewTextService.AppendReviewedText(
        string.Empty,
        "um i think we should go now",
        DictationCasingMode.Default,
        fluidDictationEnabled: true,
        automaticPunctuationEnabled: false,
        profanityFilterEnabled: false);
    Require(fluidReviewText == "I think we should go now.", $"Expected fluid dictation to remove filler words and shape the visible sentence, got '{fluidReviewText}'.");

    var fluidRebuild = DictationReviewTextService.BuildReviewedText(
        ["uh we are ready", "i can help"],
        DictationCasingMode.Default,
        fluidDictationEnabled: true,
        automaticPunctuationEnabled: false,
        profanityFilterEnabled: false);
    Require(fluidRebuild == "We are ready. I can help.", $"Expected fluid dictation rebuild to shape service-fed review text, got '{fluidRebuild}'.");
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

static void SessionCommandCaptureRequiresBiometricIdentityProof()
{
    var missingBiometric = CallsignIdentityMatcher.Evaluate(
        "echo one",
        0.95f,
        "echo one",
        requireBiometric: true);
    var session = new AlphaSessionStateMachine();
    session.DetectWakeWord();
    Require(!session.TryVerifyIdentity(missingBiometric, "echo one", voiceEnrolled: true, requireBiometric: true, out var missingMessage), "Session should reject missing biometric proof.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected missing biometric proof to stay in WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must remain blocked without biometric proof.");
    Require(missingMessage.Contains("callsign", StringComparison.OrdinalIgnoreCase) || missingMessage.Contains("biometric", StringComparison.OrdinalIgnoreCase), $"Expected a useful missing biometric message, got '{missingMessage}'.");

    var rejectedBiometric = CallsignIdentityMatcher.Evaluate(
        "echo one",
        0.95f,
        "echo one",
        biometric: FakeBiometric(accepted: false),
        requireBiometric: true);
    session.Reset();
    session.DetectWakeWord();
    Require(!session.TryVerifyIdentity(rejectedBiometric, "echo one", voiceEnrolled: true, requireBiometric: true, out var rejectedMessage), "Session should reject failed biometric proof.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected rejected biometric proof to stay in WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must remain blocked after biometric rejection.");
    Require(rejectedMessage.Contains("callsign", StringComparison.OrdinalIgnoreCase) || rejectedMessage.Contains("biometric", StringComparison.OrdinalIgnoreCase), $"Expected a useful rejected biometric message, got '{rejectedMessage}'.");

    var staleBiometric = new VoiceBiometricVerificationResult(
        false,
        0,
        0.72,
        "test-open-source-biometric",
        "biometric_candidate_stale",
        "enrolled.wav",
        "candidate.wav",
        Distance: 1);
    var staleIdentity = CallsignIdentityMatcher.Evaluate(
        "echo one",
        0.95f,
        "echo one",
        biometric: staleBiometric,
        requireBiometric: true);
    session.Reset();
    session.DetectWakeWord();
    Require(!session.TryVerifyIdentity(staleIdentity, "echo one", voiceEnrolled: true, requireBiometric: true, out _), "Session should reject stale biometric candidate audio.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected stale biometric proof to stay in WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must remain blocked after stale biometric proof.");

    var weakNearMatch = CallsignIdentityMatcher.Evaluate(
        "ekko one",
        0.95f,
        "echo one",
        biometric: FakeBiometric(accepted: true, score: 0.80),
        requireBiometric: true,
        nearMatchBiometricThreshold: 0.86);
    session.Reset();
    session.DetectWakeWord();
    Require(!session.TryVerifyIdentity(weakNearMatch, "echo one", voiceEnrolled: true, requireBiometric: true, out _), "Session should reject weak near-match biometric proof.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected weak near-match proof to stay in WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must remain blocked after weak near-match proof.");

    var acceptedIdentity = CallsignIdentityMatcher.Evaluate(
        "echo one",
        0.95f,
        "echo one",
        biometric: FakeBiometric(accepted: true),
        requireBiometric: true);
    session.Reset();
    session.DetectWakeWord();
    Require(session.TryVerifyIdentity(acceptedIdentity, "echo one", voiceEnrolled: true, requireBiometric: true, out _), "Session should accept matching callsign with accepted biometric proof.");
    Require(session.State == AlphaSessionState.WaitingForCommand, $"Expected accepted biometric identity to enter WaitingForCommand, got {session.State}.");
    Require(session.TryCaptureCommand("open Notepad", out _), "Command capture should open only after accepted biometric identity proof.");
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

        var duplicateA = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 2);
        var duplicateB = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 3);
        WriteTone(duplicateA, 260, 0.4);
        WriteTone(duplicateB, 260, 0.4);
        var duplicateContent = verifier.EnrollFreshSamples(store, profile, new[] { sample, duplicateA, duplicateB });
        Require(!duplicateContent.Accepted, "Enrollment should reject distinct files that contain duplicated sample audio.");
        Require(duplicateContent.RejectReason == "pyannote_sample_set_not_distinct", $"Expected pyannote_sample_set_not_distinct, got {duplicateContent.RejectReason}.");

        var distinctA = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 4);
        var distinctB = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 5);
        var distinctC = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, 6);
        WriteTone(distinctA, 220, 0.4);
        WriteTone(distinctB, 330, 0.4);
        WriteTone(distinctC, 440, 0.4);
        _ = verifier.EnrollFreshSamples(store, profile, new[] { distinctA, distinctB, distinctC });

        var proof = VoiceBiometricVerificationService.ReadEnrollmentSampleProof(store, profile);
        Require(proof.Accepted, $"Expected enrollment sample proof to accept three fresh distinct samples, got '{proof.Message}'.");
        Require(proof.SampleCount == 3, $"Expected proof sample count 3, got {proof.SampleCount}.");
        Require(proof.DistinctHashCount == 3, $"Expected three distinct sample hashes, got {proof.DistinctHashCount}.");
        Require(proof.Samples.All(sampleMetadata => sampleMetadata.ByteLength > 1024), "Enrollment proof should report useful sample byte lengths.");
        Require(proof.Samples.All(sampleMetadata => sampleMetadata.AgeSeconds <= 30), "Enrollment proof should report fresh sample ages.");
        Require(proof.Samples.Select(sampleMetadata => sampleMetadata.Sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3, "Enrollment proof should include unique sample hashes.");
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

    var commandStuffedWithBiometric = CallsignIdentityMatcher.Evaluate(
        "echo one open Notepad",
        0.95f,
        "echo one",
        biometric: FakeBiometric(accepted: true),
        requireBiometric: true);
    Require(!commandStuffedWithBiometric.Accepted, "Biometric proof must not allow callsign plus command in the identity turn.");
    Require(!session.TryVerifyIdentity(commandStuffedWithBiometric, "echo one", voiceEnrolled: true, requireBiometric: true, out var stuffedMessage),
        "Session should reject a command-stuffed identity turn even with accepted biometric proof.");
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected command-stuffed identity to stay in WaitingForIdentity, got {session.State}.");
    Require(!session.TryCaptureCommand("open Notepad", out _), "Command capture must stay blocked after command-stuffed identity.");
    Require(stuffedMessage.Contains("callsign", StringComparison.OrdinalIgnoreCase), $"Expected command-stuffed identity to ask for callsign only, got '{stuffedMessage}'.");

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

static void StartMenuAmbiguousResolutionRequiresConfirmation()
{
    var installed = new[]
    {
        "Calculator",
        "Notepad",
        "Notepad++",
        "Notepad Preview"
    };

    var exact = StartMenuLauncher.ResolveInstalledAppName("Notepad", installed);
    Require(exact is { IsResolved: true, IsAmbiguous: false }, "Exact app names should resolve without confirmation.");
    Require(exact.SelectedName == "Notepad", $"Expected exact Notepad match, got '{exact.SelectedName}'.");

    var alias = StartMenuLauncher.ResolveInstalledAppName("calc", installed);
    Require(alias is { IsResolved: true, IsAmbiguous: false }, "Common speech aliases should resolve without ambiguity when they map to one app.");
    Require(alias.SelectedName == "Calculator", $"Expected Calculator alias match, got '{alias.SelectedName}'.");

    var ambiguous = StartMenuLauncher.ResolveInstalledAppName("note", installed);
    Require(ambiguous is { IsResolved: false, IsAmbiguous: true }, "Broad fuzzy app names should require visible confirmation.");
    Require(ambiguous.SelectedName == null, "Ambiguous app names must not auto-select a launch target.");
    Require(ambiguous.Candidates.Count >= 2, "Ambiguous app names should surface multiple candidates.");
    Require(ambiguous.Candidates.Any(candidate => candidate.DisplayName == "Notepad"), "Ambiguous choices should include Notepad.");
    Require(ambiguous.Candidates.Any(candidate => candidate.DisplayName == "Notepad++"), "Ambiguous choices should include Notepad++.");

    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("choose app 1", out var firstChoice) && firstChoice == 1, "Voice app-choice parser should accept 'choose app 1'.");
    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("1", out var bareChoice) && bareChoice == 1, "Voice app-choice parser should accept a bare number during visible app-choice mode.");
    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("click 1", out var clickedChoice) && clickedChoice == 1, "Voice app-choice parser should accept click-number phrasing.");
    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("choose result 3", out var resultChoice) && resultChoice == 3, "Voice app-choice parser should accept result-number phrasing.");
    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("select option five", out var fifthChoice) && fifthChoice == 5, "Voice app-choice parser should accept word-number options.");
    Require(StartMenuLauncher.TryParseAppCandidateSelectionNumber("use result three", out var thirdChoice) && thirdChoice == 3, "Voice app-choice parser should accept result-number phrases.");
    Require(StartMenuLauncher.IsConfirmAppCandidateCommand("confirm app"), "App-choice helper should accept confirm app.");
    Require(StartMenuLauncher.IsNextAppCandidateCommand("next app choice"), "App-choice helper should accept next-choice navigation.");
    Require(StartMenuLauncher.IsPreviousAppCandidateCommand("previous app choice"), "App-choice helper should accept previous-choice navigation.");
    Require(StartMenuLauncher.IsClearAppCandidateCommand("clear app choices"), "App-choice helper should accept explicit clear wording.");
    Require(!StartMenuLauncher.TryParseAppCandidateSelectionNumber("open Notepad", out _), "Ordinary app-launch phrases must not be treated as app-choice confirmation.");
    Require(!StartMenuLauncher.TryParseAppCandidateSelectionNumber("open OneNote", out _), "Ordinary app names that begin with spoken number words must not be mistaken for app-choice confirmation.");
    Require(!StartMenuLauncher.TryParseAppCandidateSelectionNumber("choose app 6", out _), "Out-of-range app choices must be rejected.");

    var repoRoot = FindRepositoryRoot();
    var uiPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    var uiSource = File.ReadAllText(uiPath);
    Require(uiSource.Contains("LaunchWithResult(target)", StringComparison.OrdinalIgnoreCase), "UI launch path should use structured launch-path telemetry.");
    Require(uiSource.Contains("launchPath: launchResult.LaunchPath", StringComparison.OrdinalIgnoreCase), "Audit should record the actual launch path instead of always claiming Start menu search.");
    Require(uiSource.Contains("launchResult.IsVisibleStartMenuPath", StringComparison.OrdinalIgnoreCase), "UI status should distinguish visible Start menu launch from fallback launch.");

    var launcherPath = Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "StartMenuLauncher.cs");
    var launcherSource = File.ReadAllText(launcherPath);
    Require(launcherSource.Contains("StartMenuLaunchResult", StringComparison.OrdinalIgnoreCase), "Start menu launcher should expose structured launch-path telemetry.");
    Require(launcherSource.Contains("start-open:keybd-event-windows-key", StringComparison.OrdinalIgnoreCase), "Start menu launcher should retain the legacy Windows-key fallback for sessions where SendInput is blocked.");
    Require(launcherSource.Contains("start-menu:type-search-sendkeys", StringComparison.OrdinalIgnoreCase), "Start menu launcher should retain typed-search fallback evidence.");
    Require(launcherSource.Contains("WaitForStartMenuOrSearchSurface", StringComparison.OrdinalIgnoreCase), "Start menu launcher should verify the Start/Search surface before claiming the visible Start menu path.");
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

static void VisibleWindowSwitchingResolvesNamedMatchesAndAmbiguity()
{
    var ambiguous = new VisibleWindowSwitchResolution(
        RequestedName: "edge",
        NormalizedName: "edge",
        IsResolved: false,
        IsAmbiguous: true,
        SelectedCandidate: null,
        Candidates:
        [
            new VisibleWindowSwitchCandidate((nint)1, "Docs - Microsoft Edge", "msedge", false, 200, "contains"),
            new VisibleWindowSwitchCandidate((nint)2, "Mail - Microsoft Edge", "msedge", false, 200, "contains")
        ],
        Message: "Multiple open windows match 'edge'. Choose one before Callsign switches focus.");
    Require(ambiguous.IsAmbiguous, "Multiple matching windows should require visible choice confirmation.");
    Require(ambiguous.Candidates.Count == 2, $"Expected two visible window choices, got {ambiguous.Candidates.Count}.");
    Require(SystemControlService.TryParseVisibleWindowSelectionNumber("1", out var bareChoice) && bareChoice == 1, "Visible window-choice parser should accept a bare number.");
    Require(SystemControlService.TryParseVisibleWindowSelectionNumber("click 1", out var clickChoice) && clickChoice == 1, "Visible window-choice parser should accept click-number phrasing.");
    Require(SystemControlService.TryParseVisibleWindowSelectionNumber("choose window 2", out var chooseChoice) && chooseChoice == 2, "Visible window-choice parser should accept choose-window phrasing.");
    Require(SystemControlService.TryParseVisibleWindowSelectionNumber("choose result 3", out var resultChoice) && resultChoice == 3, "Visible window-choice parser should accept result-number phrasing.");
    Require(SystemControlService.IsConfirmVisibleWindowSelectionCommand("confirm window"), "Visible window-choice helper should accept confirm window.");
    Require(SystemControlService.IsNextVisibleWindowSelectionCommand("next window choice"), "Visible window-choice helper should accept next choice.");
    Require(SystemControlService.IsPreviousVisibleWindowSelectionCommand("previous window choice"), "Visible window-choice helper should accept previous choice.");
    Require(SystemControlService.IsClearVisibleWindowSelectionCommand("clear window choices"), "Visible window-choice helper should accept clear wording.");
    Require(SystemControlService.IsClearVisibleWindowSelectionCommand("cancel"), "Visible window-choice helper should accept cancel for dismissing choices.");
    Require(!SystemControlService.TryParseVisibleWindowSelectionNumber("switch to edge", out _), "Ordinary switch-to-app phrases must not be mistaken for a numbered window choice.");

    Require(AlphaCommandRouter.TryRoute("switch to edge", out var switchRoute), "Named app switching should route through the system command router.");
    Require(switchRoute.Kind == AlphaCommandKind.SystemControl, $"Expected SystemControl for named app switching, got {switchRoute.Kind}.");
    Require(switchRoute.Target == "system-switch-window:edge", $"Expected system-switch-window:edge target, got '{switchRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to notepad", out var goToRoute), "Go-to app switching should route through the system command router.");
    Require(goToRoute.Target == "system-switch-window:notepad", $"Expected system-switch-window:notepad target, got '{goToRoute.Target}'.");

    var voiceIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to edge", "Callsign", "echo one");
    Require(voiceIntent.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl for named app switching transcript, got {voiceIntent.Kind}.");
    Require(voiceIntent.Target == "system-switch-window:edge", $"Expected system-switch-window:edge intent target, got '{voiceIntent.Target}'.");
}

static void BrowserTargetResolution()
{
    Require(BrowserLaunchService.TryBuildTargetUri("https://example.com", out var directUri, out _), "Direct https URL should resolve.");
    Require(directUri?.Host == "example.com", "Direct URL should preserve host.");

    Require(BrowserLaunchService.TryBuildTargetUri("Callsign desktop assistant", out var searchUri, out _), "Search phrase should resolve.");
    Require(searchUri?.Host.Contains("bing", StringComparison.OrdinalIgnoreCase) == true, "Search phrase should route to the search engine.");

    Require(!BrowserLaunchService.TryBuildTargetUri(@"C:\temp\notes.txt", out _, out _), "Local file paths should not be treated as browser targets.");
    Require(!BrowserLaunchService.TryBuildTargetUri("javascript:alert(1)", out _, out var scriptReason), "Script URI schemes should not be browser targets.");
    Require(scriptReason.Contains("http/https", StringComparison.OrdinalIgnoreCase), $"Blocked script scheme should explain the web-only boundary, got '{scriptReason}'.");
    Require(!BrowserLaunchService.TryBuildTargetUri("file:///C:/temp/notes.txt", out _, out _), "File URI schemes should not be browser targets.");
    Require(!BrowserLaunchService.TryBuildTargetUri("ms-settings:privacy", out _, out _), "Settings URI schemes should use the settings command surface, not browser mode.");
    Require(BrowserLaunchService.TryBuildTargetUri("javascript:alert(1)", out var forcedSearchUri, out _, forceSearch: true), "Forced browser search should still allow scheme-like search text.");
    Require(forcedSearchUri?.Host.Contains("bing", StringComparison.OrdinalIgnoreCase) == true, "Forced scheme-like search text should route to the search engine.");
    Require(BrowserLaunchService.TryParseFindTextAction("browser-find-text:privacy policy", out var findText), "Browser find-text action should parse.");
    Require(findText == "privacy policy", $"Expected privacy policy find text, got '{findText}'.");
    Require(!BrowserLaunchService.TryParseFindTextAction("browser-find-text:", out _), "Empty browser find-text action should be rejected.");
    Require(BrowserLaunchService.TryParseAddressTextAction("browser-address-text:example.com", out var addressText), "Browser address-text action should parse.");
    Require(addressText == "example.com", $"Expected example.com address text, got '{addressText}'.");
    Require(!BrowserLaunchService.TryParseAddressTextAction("browser-address-text:", out _), "Empty browser address-text action should be rejected.");
    Require(!BrowserLaunchService.TryParseAddressTextAction("browser-address-text:hello\nworld", out _), "Multiline browser address-text action should be rejected.");
    Require(BrowserLaunchService.EscapeSendKeysText("a+b {test}") == "a{+}b {{}test{}}", "Browser find text should escape SendKeys metacharacters.");

    if (BrowserLaunchService.TryFindChrome(out var chromePath))
    {
        Require(Path.GetFileName(chromePath).Equals("chrome.exe", StringComparison.OrdinalIgnoreCase), "Chrome discovery should resolve chrome.exe.");
    }
}

static void BrowserActionExecutionCoversContinuousScrollingControls()
{
    var service = new BrowserLaunchService(dryRun: true);

    foreach (var (action, expectedMessage) in new[]
             {
                 ("browser-start-scroll-up", "Browser start scrolling up requested."),
                 ("browser-start-scroll-down", "Browser start scrolling down requested."),
                 ("browser-start-scroll-left", "Browser start scrolling left requested."),
                 ("browser-start-scroll-right", "Browser start scrolling right requested."),
                 ("browser-stop-scroll", "Browser stop scrolling requested.")
             })
    {
        Require(service.TryExecuteBrowserAction(action, out var message), $"Dry-run browser action should execute: {action}");
        Require(string.Equals(message, expectedMessage, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedMessage}' for '{action}', got '{message}'.");
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
        Require(report.Results[0].Rank == 1, $"File search results should be numbered from 1, got rank {report.Results[0].Rank}.");
        Require(report.Results[0].ToString().StartsWith("1.", StringComparison.OrdinalIgnoreCase), $"File search result text should display the rank, got '{report.Results[0]}'.");
        var description = FileSearchService.DescribeResult(report.Results[0], report.Results.Count);
        Require(description.Contains("Result 1", StringComparison.OrdinalIgnoreCase), $"File search description should include the visible rank, got '{description}'.");
        Require(description.Contains("alpha-notes.txt", StringComparison.OrdinalIgnoreCase), $"File search description should include the file name, got '{description}'.");
        if (report.SearchEngine == "built-in")
        {
            Require(report.Warnings.Any(warning => warning.Contains("fzf.exe", StringComparison.OrdinalIgnoreCase)),
                "Built-in fallback should report that fzf.exe was unavailable.");
        }

        var emptyReport = service.Search("does-not-exist", new[] { root }, maxResults: 10);
        Require(emptyReport.Results.Count == 0, "Non-matching file search should return no results.");

        var windowsRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windowsRoot))
        {
            var warnings = new List<string>();
            var allowedRoots = FileSearchService.FilterAllowedSearchRoots(new[] { root, windowsRoot }, warnings);
            Require(allowedRoots.Any(allowed => allowed.StartsWith(root, StringComparison.OrdinalIgnoreCase)), "Temp test root should remain searchable.");
            Require(!allowedRoots.Any(allowed => allowed.StartsWith(windowsRoot, StringComparison.OrdinalIgnoreCase)), "Windows root should be blocked from file search.");
            Require(warnings.Any(warning => warning.Contains("blocked", StringComparison.OrdinalIgnoreCase)), "Blocked search root should produce a visible warning.");
        }

        Require(!FileSearchService.IsBlockedOpenTarget(document), "Text documents should be safe open targets.");
        Require(FileSearchService.IsBlockedOpenTarget(Path.Combine(root, "installer.exe")), "Executable file results should be blocked from direct open.");
        Require(FileSearchService.IsBlockedOpenTarget(Path.Combine(root, "script.ps1")), "Script file results should be blocked from direct open.");
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
    Require(AlphaCommandRouter.TryRoute("find on page for privacy policy", out var browserFindTextRoute), "Browser find-text command should route.");
    Require(browserFindTextRoute.Kind == AlphaCommandKind.Browser, "Browser find-text should be browser kind.");
    Require(browserFindTextRoute.Target == "browser-find-text:privacy policy", $"Expected browser-find-text target, got '{browserFindTextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find privacy policy on this page", out var browserFindTextSuffixRoute), "Browser find-text suffix command should route.");
    Require(browserFindTextSuffixRoute.Target == "browser-find-text:privacy policy", $"Expected suffix browser-find-text target, got '{browserFindTextSuffixRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("type in address bar example dot com", out var browserAddressTextRoute), "Browser address-text command should route.");
    Require(browserAddressTextRoute.Kind == AlphaCommandKind.Browser, "Browser address-text should be browser kind.");
    Require(browserAddressTextRoute.Target == "browser-address-text:example.com", $"Expected browser-address-text target, got '{browserAddressTextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("type example dot com in the address bar", out var browserAddressTextSuffixRoute), "Browser address-text suffix command should route.");
    Require(browserAddressTextSuffixRoute.Target == "browser-address-text:example.com", $"Expected suffix browser-address-text target, got '{browserAddressTextSuffixRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to address bar and type example dot org", out var browserAddressBarTypeRoute), "Browser address-bar type command should route.");
    Require(browserAddressBarTypeRoute.Target == "browser-address-text:example.org", $"Expected address-bar type target, got '{browserAddressBarTypeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("search address bar for Callsign desktop assistant", out var browserAddressSearchRoute), "Browser address-bar search command should route.");
    Require(browserAddressSearchRoute.Target == "browser-address-text:callsign desktop assistant", $"Expected normalized address-bar search target, got '{browserAddressSearchRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser find", out var browserFindRoute), "Plain browser find command should route.");
    Require(browserFindRoute.Kind == AlphaCommandKind.Browser, "Plain browser find should be browser kind.");
    Require(browserFindRoute.Target == "browser-find", $"Expected browser-find target, got '{browserFindRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("repair wakeword", out var repairWakewordRoute), "Repair wakeword command should route.");
    Require(repairWakewordRoute.Kind == AlphaCommandKind.UiAction, "Repair wakeword should be a UI action.");
    Require(repairWakewordRoute.Target == "ui-repair-wakeword", $"Expected ui-repair-wakeword target, got '{repairWakewordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("train voice identity", out var trainVoiceIdentityRoute), "Train voice identity command should route.");
    Require(trainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Train voice identity should be a UI action.");
    Require(trainVoiceIdentityRoute.Target == "ui-train-voice-identity", $"Expected ui-train-voice-identity target, got '{trainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("add womprat to vocabulary", out var addVocabularyRoute), "Add-to-vocabulary command should route.");
    Require(addVocabularyRoute.Kind == AlphaCommandKind.UiAction, "Add-to-vocabulary should be a UI action.");
    Require(addVocabularyRoute.Target == "ui-add-vocabulary:womprat", $"Expected ui-add-vocabulary:womprat target, got '{addVocabularyRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("add project zephyr to dictation vocabulary", out var addVocabularyPhraseRoute), "Add phrase to dictation vocabulary command should route.");
    Require(addVocabularyPhraseRoute.Target == "ui-add-vocabulary:project zephyr", $"Expected ui-add-vocabulary:project zephyr target, got '{addVocabularyPhraseRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("add to dictionary support dot example", out var addDictionaryRoute), "Add-to-dictionary command should route.");
    Require(addDictionaryRoute.Target == "ui-add-vocabulary:support dot example", $"Expected ui-add-vocabulary:support dot example target, got '{addDictionaryRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("turn on automatic punctuation", out var automaticPunctuationOnRoute), "Automatic punctuation on command should route.");
    Require(automaticPunctuationOnRoute.Kind == AlphaCommandKind.UiAction, "Automatic punctuation on should be a UI action.");
    Require(automaticPunctuationOnRoute.Target == "ui-set-dictation-option:automatic-punctuation:on", $"Expected automatic punctuation on target, got '{automaticPunctuationOnRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("turn on fluid dictation", out var fluidDictationOnRoute), "Fluid dictation on command should route.");
    Require(fluidDictationOnRoute.Target == "ui-set-dictation-option:fluid-dictation:on", $"Expected fluid dictation on target, got '{fluidDictationOnRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("fluid dictation off", out var fluidDictationOffRoute), "Fluid dictation off command should route.");
    Require(fluidDictationOffRoute.Target == "ui-set-dictation-option:fluid-dictation:off", $"Expected fluid dictation off target, got '{fluidDictationOffRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("automatic punctuation off", out var automaticPunctuationOffRoute), "Automatic punctuation off command should route.");
    Require(automaticPunctuationOffRoute.Target == "ui-set-dictation-option:automatic-punctuation:off", $"Expected automatic punctuation off target, got '{automaticPunctuationOffRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("filter profanity", out var profanityFilterOnRoute), "Profanity filter on command should route.");
    Require(profanityFilterOnRoute.Target == "ui-set-dictation-option:profanity-filter:on", $"Expected profanity filter on target, got '{profanityFilterOnRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("do not filter profanity", out var profanityFilterOffRoute), "Profanity filter off command should route.");
    Require(profanityFilterOffRoute.Target == "ui-set-dictation-option:profanity-filter:off", $"Expected profanity filter off target, got '{profanityFilterOffRoute.Target}'.");
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
    Require(AlphaCommandRouter.TryRoute("move to next field", out var moveToNextFieldRoute), "Move to next field command should route.");
    Require(moveToNextFieldRoute.Kind == AlphaCommandKind.UiAction, "Move to next field should be a UI action.");
    Require(moveToNextFieldRoute.Target == "ui-next-control", $"Expected ui-next-control target, got '{moveToNextFieldRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("tab forward", out var tabForwardRoute), "Tab forward command should route through visible-control focus.");
    Require(tabForwardRoute.Kind == AlphaCommandKind.UiAction, "Tab forward should be a UI action.");
    Require(tabForwardRoute.Target == "ui-next-control", $"Expected ui-next-control target, got '{tabForwardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous control", out var previousControlRoute), "Previous control command should route.");
    Require(previousControlRoute.Kind == AlphaCommandKind.UiAction, "Previous control should be a UI action.");
    Require(previousControlRoute.Target == "ui-previous-control", $"Expected ui-previous-control target, got '{previousControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move to previous field", out var moveToPreviousFieldRoute), "Move to previous field command should route.");
    Require(moveToPreviousFieldRoute.Kind == AlphaCommandKind.UiAction, "Move to previous field should be a UI action.");
    Require(moveToPreviousFieldRoute.Target == "ui-previous-control", $"Expected ui-previous-control target, got '{moveToPreviousFieldRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("tab backward", out var tabBackwardRoute), "Tab backward command should route through visible-control focus.");
    Require(tabBackwardRoute.Kind == AlphaCommandKind.UiAction, "Tab backward should be a UI action.");
    Require(tabBackwardRoute.Target == "ui-previous-control", $"Expected ui-previous-control target, got '{tabBackwardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("activate control", out var activateControlRoute), "Activate control command should route.");
    Require(activateControlRoute.Kind == AlphaCommandKind.UiAction, "Activate control should be a UI action.");
    Require(activateControlRoute.Target == "ui-activate-control", $"Expected ui-activate-control target, got '{activateControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("what did you hear", out var readStatusRoute), "Status readback command should route.");
    Require(readStatusRoute.Kind == AlphaCommandKind.UiAction, "Status readback should be a UI action.");
    Require(readStatusRoute.Target == "ui-read-status", $"Expected ui-read-status target, got '{readStatusRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("stop status readback", out var stopStatusReadbackRoute), "Stop status readback command should route.");
    Require(stopStatusReadbackRoute.Kind == AlphaCommandKind.UiAction, "Stop status readback should be a UI action.");
    Require(stopStatusReadbackRoute.Target == "ui-stop-status-readback", $"Expected ui-stop-status-readback target, got '{stopStatusReadbackRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("clear recent speech", out var clearRecentSpeechRoute), "Clear recent speech command should route.");
    Require(clearRecentSpeechRoute.Kind == AlphaCommandKind.UiAction, "Clear recent speech should be a UI action.");
    Require(clearRecentSpeechRoute.Target == "ui-clear-recent-speech", $"Expected ui-clear-recent-speech target, got '{clearRecentSpeechRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press repair wakeword", out var pressRepairWakewordRoute), "Press repair wakeword command should route.");
    Require(pressRepairWakewordRoute.Kind == AlphaCommandKind.UiAction, "Press repair wakeword should be a UI action.");
    Require(pressRepairWakewordRoute.Target == "ui-activate-label:repair wakeword", $"Expected ui-activate-label:repair wakeword target, got '{pressRepairWakewordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the train voice identity button", out var clickTrainVoiceIdentityRoute), "Click train voice identity command should route.");
    Require(clickTrainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Click train voice identity should be a UI action.");
    Require(clickTrainVoiceIdentityRoute.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickTrainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click on the train voice identity button", out var clickOnTrainVoiceIdentityRoute), "Click on train voice identity command should route.");
    Require(clickOnTrainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Click on train voice identity should be a UI action.");
    Require(clickOnTrainVoiceIdentityRoute.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickOnTrainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("choose the train voice identity button", out var chooseTrainVoiceIdentityRoute), "Choose train voice identity command should route.");
    Require(chooseTrainVoiceIdentityRoute.Kind == AlphaCommandKind.UiAction, "Choose train voice identity should be a UI action.");
    Require(chooseTrainVoiceIdentityRoute.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{chooseTrainVoiceIdentityRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the train voice identity link", out var clickTrainVoiceIdentityLinkRoute), "Click train voice identity link command should route.");
    Require(clickTrainVoiceIdentityLinkRoute.Kind == AlphaCommandKind.UiAction, "Click train voice identity link should be a UI action.");
    Require(clickTrainVoiceIdentityLinkRoute.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickTrainVoiceIdentityLinkRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the voice mode radio button", out var clickVoiceModeRadioButtonRoute), "Click voice mode radio button command should route.");
    Require(clickVoiceModeRadioButtonRoute.Kind == AlphaCommandKind.UiAction, "Click voice mode radio button should be a UI action.");
    Require(clickVoiceModeRadioButtonRoute.Target == "ui-activate-label:voice mode", $"Expected ui-activate-label:voice mode target, got '{clickVoiceModeRadioButtonRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the settings menu item", out var clickSettingsMenuItemRoute), "Click settings menu item command should route.");
    Require(clickSettingsMenuItemRoute.Kind == AlphaCommandKind.UiAction, "Click settings menu item should be a UI action.");
    Require(clickSettingsMenuItemRoute.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{clickSettingsMenuItemRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("choose the settings option", out var chooseSettingsOptionRoute), "Choose settings option command should route.");
    Require(chooseSettingsOptionRoute.Kind == AlphaCommandKind.UiAction, "Choose settings option should be a UI action.");
    Require(chooseSettingsOptionRoute.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{chooseSettingsOptionRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the username text box", out var clickUsernameTextBoxRoute), "Click username text box command should route.");
    Require(clickUsernameTextBoxRoute.Kind == AlphaCommandKind.UiAction, "Click username text box should be a UI action.");
    Require(clickUsernameTextBoxRoute.Target == "ui-activate-label:username", $"Expected ui-activate-label:username target, got '{clickUsernameTextBoxRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the password edit box", out var clickPasswordEditBoxRoute), "Click password edit box command should route.");
    Require(clickPasswordEditBoxRoute.Kind == AlphaCommandKind.UiAction, "Click password edit box should be a UI action.");
    Require(clickPasswordEditBoxRoute.Target == "ui-activate-label:password", $"Expected ui-activate-label:password target, got '{clickPasswordEditBoxRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the project list item", out var clickProjectListItemRoute), "Click project list item command should route.");
    Require(clickProjectListItemRoute.Kind == AlphaCommandKind.UiAction, "Click project list item should be a UI action.");
    Require(clickProjectListItemRoute.Target == "ui-activate-label:project", $"Expected ui-activate-label:project target, got '{clickProjectListItemRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the navigation tree item", out var clickNavigationTreeItemRoute), "Click navigation tree item command should route.");
    Require(clickNavigationTreeItemRoute.Kind == AlphaCommandKind.UiAction, "Click navigation tree item should be a UI action.");
    Require(clickNavigationTreeItemRoute.Target == "ui-activate-label:navigation", $"Expected ui-activate-label:navigation target, got '{clickNavigationTreeItemRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the account row", out var clickAccountRowRoute), "Click account row command should route.");
    Require(clickAccountRowRoute.Kind == AlphaCommandKind.UiAction, "Click account row should be a UI action.");
    Require(clickAccountRowRoute.Target == "ui-activate-label:account", $"Expected ui-activate-label:account target, got '{clickAccountRowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the settings pane", out var clickSettingsPaneRoute), "Click settings pane command should route.");
    Require(clickSettingsPaneRoute.Kind == AlphaCommandKind.UiAction, "Click settings pane should be a UI action.");
    Require(clickSettingsPaneRoute.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{clickSettingsPaneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the status cell", out var clickStatusCellRoute), "Click status cell command should route.");
    Require(clickStatusCellRoute.Kind == AlphaCommandKind.UiAction, "Click status cell should be a UI action.");
    Require(clickStatusCellRoute.Target == "ui-activate-label:status", $"Expected ui-activate-label:status target, got '{clickStatusCellRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the document heading", out var clickDocumentHeadingRoute), "Click document heading command should route.");
    Require(clickDocumentHeadingRoute.Kind == AlphaCommandKind.UiAction, "Click document heading should be a UI action.");
    Require(clickDocumentHeadingRoute.Target == "ui-activate-label:document", $"Expected ui-activate-label:document target, got '{clickDocumentHeadingRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the account group", out var clickAccountGroupRoute), "Click account group command should route.");
    Require(clickAccountGroupRoute.Kind == AlphaCommandKind.UiAction, "Click account group should be a UI action.");
    Require(clickAccountGroupRoute.Target == "ui-activate-label:account", $"Expected ui-activate-label:account target, got '{clickAccountGroupRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click the inbox list box", out var clickInboxListBoxRoute), "Click inbox list box command should route.");
    Require(clickInboxListBoxRoute.Kind == AlphaCommandKind.UiAction, "Click inbox list box should be a UI action.");
    Require(clickInboxListBoxRoute.Target == "ui-activate-label:inbox", $"Expected ui-activate-label:inbox target, got '{clickInboxListBoxRoute.Target}'.");
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
    Require(AlphaCommandRouter.TryRoute("show numbers here", out var showNumbersHereRoute), "Show numbers here command should route.");
    Require(showNumbersHereRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNumbersHereRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show numbers everywhere", out var showNumbersEverywhereRoute), "Show numbers everywhere command should route.");
    Require(showNumbersEverywhereRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNumbersEverywhereRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show numbers on taskbar", out var showNumbersOnTaskbarRoute), "Show numbers on taskbar command should route.");
    Require(showNumbersOnTaskbarRoute.Target == "ui-show-visible-controls-taskbar", $"Expected ui-show-visible-controls-taskbar target, got '{showNumbersOnTaskbarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show numbers on notepad", out var showNumbersOnNotepadRoute), "Show numbers on notepad command should route.");
    Require(showNumbersOnNotepadRoute.Target == "ui-show-visible-controls-window:notepad", $"Expected ui-show-visible-controls-window:notepad target, got '{showNumbersOnNotepadRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show control numbers on visual studio code", out var showControlNumbersOnVsCodeRoute), "Show control numbers on visual studio code command should route.");
    Require(showControlNumbersOnVsCodeRoute.Target == "ui-show-visible-controls-window:visual studio code", $"Expected ui-show-visible-controls-window:visual studio code target, got '{showControlNumbersOnVsCodeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("number clickable controls", out var numberClickableControlsRoute), "Number clickable controls command should route.");
    Require(numberClickableControlsRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{numberClickableControlsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show control numbers", out var showControlNumbersRoute), "Show control numbers command should route.");
    Require(showControlNumbersRoute.Kind == AlphaCommandKind.UiAction, "Show control numbers should be a UI action.");
    Require(showControlNumbersRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showControlNumbersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show all controls", out var showAllControlsRoute), "Show all controls command should route.");
    Require(showAllControlsRoute.Kind == AlphaCommandKind.UiAction, "Show all controls should be a UI action.");
    Require(showAllControlsRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showAllControlsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show names", out var showNamesRoute), "Show names command should route.");
    Require(showNamesRoute.Kind == AlphaCommandKind.UiAction, "Show names should be a UI action.");
    Require(showNamesRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNamesRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show labels", out var showLabelsRoute), "Show labels command should route.");
    Require(showLabelsRoute.Kind == AlphaCommandKind.UiAction, "Show labels should be a UI action.");
    Require(showLabelsRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showLabelsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show all labels", out var showAllLabelsRoute), "Show all labels command should route.");
    Require(showAllLabelsRoute.Kind == AlphaCommandKind.UiAction, "Show all labels should be a UI action.");
    Require(showAllLabelsRoute.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showAllLabelsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide visible controls", out var hideVisibleControlsRoute), "Hide visible controls command should route.");
    Require(hideVisibleControlsRoute.Kind == AlphaCommandKind.UiAction, "Hide visible controls should be a UI action.");
    Require(hideVisibleControlsRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideVisibleControlsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide control numbers", out var hideControlNumbersRoute), "Hide control numbers command should route.");
    Require(hideControlNumbersRoute.Kind == AlphaCommandKind.UiAction, "Hide control numbers should be a UI action.");
    Require(hideControlNumbersRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideControlNumbersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide all controls", out var hideAllControlsRoute), "Hide all controls command should route.");
    Require(hideAllControlsRoute.Kind == AlphaCommandKind.UiAction, "Hide all controls should be a UI action.");
    Require(hideAllControlsRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideAllControlsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("cancel control numbers", out var cancelControlNumbersRoute), "Cancel control numbers command should route.");
    Require(cancelControlNumbersRoute.Kind == AlphaCommandKind.UiAction, "Cancel control numbers should be a UI action.");
    Require(cancelControlNumbersRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{cancelControlNumbersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("clear numbers", out var clearNumbersRoute), "Clear numbers command should route.");
    Require(clearNumbersRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{clearNumbersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide names", out var hideNamesRoute), "Hide names command should route.");
    Require(hideNamesRoute.Kind == AlphaCommandKind.UiAction, "Hide names should be a UI action.");
    Require(hideNamesRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideNamesRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide labels", out var hideLabelsRoute), "Hide labels command should route.");
    Require(hideLabelsRoute.Kind == AlphaCommandKind.UiAction, "Hide labels should be a UI action.");
    Require(hideLabelsRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideLabelsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide all labels", out var hideAllLabelsRoute), "Hide all labels command should route.");
    Require(hideAllLabelsRoute.Kind == AlphaCommandKind.UiAction, "Hide all labels should be a UI action.");
    Require(hideAllLabelsRoute.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideAllLabelsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show keyboard", out var showKeyboardRoute), "Show keyboard command should route.");
    Require(showKeyboardRoute.Kind == AlphaCommandKind.UiAction, "Show keyboard should be a UI action.");
    Require(showKeyboardRoute.Target == "ui-show-keyboard", $"Expected ui-show-keyboard target, got '{showKeyboardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open on screen keyboard", out var openOnScreenKeyboardRoute), "Open on-screen keyboard command should route.");
    Require(openOnScreenKeyboardRoute.Kind == AlphaCommandKind.UiAction, "Open on-screen keyboard should be a UI action.");
    Require(openOnScreenKeyboardRoute.Target == "ui-show-keyboard", $"Expected ui-show-keyboard target, got '{openOnScreenKeyboardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide keyboard", out var hideKeyboardRoute), "Hide keyboard command should route.");
    Require(hideKeyboardRoute.Kind == AlphaCommandKind.UiAction, "Hide keyboard should be a UI action.");
    Require(hideKeyboardRoute.Target == "ui-hide-keyboard", $"Expected ui-hide-keyboard target, got '{hideKeyboardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("cancel keyboard", out var cancelKeyboardRoute), "Cancel keyboard command should route.");
    Require(cancelKeyboardRoute.Kind == AlphaCommandKind.UiAction, "Cancel keyboard should be a UI action.");
    Require(cancelKeyboardRoute.Target == "ui-hide-keyboard", $"Expected ui-hide-keyboard target, got '{cancelKeyboardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show grid", out var showGridRoute), "Show grid command should route.");
    Require(showGridRoute.Kind == AlphaCommandKind.UiAction, "Show grid should be a UI action.");
    Require(showGridRoute.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show grid here", out var showGridHereRoute), "Show grid here command should route.");
    Require(showGridHereRoute.Target == "ui-show-mouse-grid-here", $"Expected ui-show-mouse-grid-here target, got '{showGridHereRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show grid everywhere", out var showGridEverywhereRoute), "Show grid everywhere command should route.");
    Require(showGridEverywhereRoute.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showGridEverywhereRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show window grid", out var showWindowGridRoute), "Show window grid command should route.");
    Require(showWindowGridRoute.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showWindowGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show mousegrid", out var showMousegridRoute), "Show mousegrid command should route.");
    Require(showMousegridRoute.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showMousegridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show numbered grid", out var showNumberedGridRoute), "Show numbered grid command should route.");
    Require(showNumberedGridRoute.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showNumberedGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("grid 5", out var gridFiveRoute), "Grid 5 command should route.");
    Require(gridFiveRoute.Target == "ui-select-mouse-grid-cell:5", $"Expected ui-select-mouse-grid-cell:5 target, got '{gridFiveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("grid bravo", out var gridBravoRoute), "Grid Bravo command should route.");
    Require(gridBravoRoute.Target == "ui-focus-mouse-grid-display:B", $"Expected ui-focus-mouse-grid-display:B target, got '{gridBravoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse grid alpha 114", out var mouseGridAlphaPathRoute), "Mouse grid Alpha path command should route.");
    Require(mouseGridAlphaPathRoute.Target == "ui-focus-mouse-grid-path:A:114", $"Expected ui-focus-mouse-grid-path:A:114 target, got '{mouseGridAlphaPathRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse grid 114", out var mouseGridShortcutPathRoute), "Mouse grid shortcut path command should route.");
    Require(mouseGridShortcutPathRoute.Target == "ui-focus-mouse-grid-shortcut-path:114", $"Expected ui-focus-mouse-grid-shortcut-path:114 target, got '{mouseGridShortcutPathRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse grid 1 1 4", out var mouseGridSpacedShortcutPathRoute), "Spaced mouse grid shortcut path command should route.");
    Require(mouseGridSpacedShortcutPathRoute.Target == "ui-focus-mouse-grid-shortcut-path:114", $"Expected spaced ui-focus-mouse-grid-shortcut-path:114 target, got '{mouseGridSpacedShortcutPathRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select cell third", out var selectThirdCellRoute), "Select cell third command should route.");
    Require(selectThirdCellRoute.Target == "ui-select-mouse-grid-cell:3", $"Expected ui-select-mouse-grid-cell:3 target, got '{selectThirdCellRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click grid 9", out var clickGridNineRoute), "Click grid 9 command should route.");
    Require(clickGridNineRoute.Target == "ui-click-mouse-grid-cell:9", $"Expected ui-click-mouse-grid-cell:9 target, got '{clickGridNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click cell one", out var clickCellOneRoute), "Click cell one command should route.");
    Require(clickCellOneRoute.Target == "ui-click-mouse-grid-cell:1", $"Expected ui-click-mouse-grid-cell:1 target, got '{clickCellOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag grid 1 to grid 9", out var dragGridRoute), "Drag grid command should route.");
    Require(dragGridRoute.Target == "ui-drag-mouse-grid:1:9", $"Expected ui-drag-mouse-grid:1:9 target, got '{dragGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag grid one to grid nine", out var spokenDragGridRoute), "Spoken-number drag grid command should route.");
    Require(spokenDragGridRoute.Target == "ui-drag-mouse-grid:1:9", $"Expected ui-drag-mouse-grid:1:9 target, got '{spokenDragGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag from cell one to cell ninth", out var dragCellRoute), "Drag from cell command should route.");
    Require(dragCellRoute.Target == "ui-drag-mouse-grid:1:9", $"Expected ui-drag-mouse-grid:1:9 target, got '{dragCellRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mark", out var markGridRoute), "Mark grid command should route.");
    Require(markGridRoute.Target == "ui-mark-mouse-grid", $"Expected ui-mark-mouse-grid target, got '{markGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mark four", out var markGridFourRoute), "Mark four command should route.");
    Require(markGridFourRoute.Target == "ui-mark-mouse-grid-cell:4", $"Expected ui-mark-mouse-grid-cell:4 target, got '{markGridFourRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("undo that", out var undoThatGridRoute), "Undo that command should route.");
    Require(undoThatGridRoute.Target == "ui-undo-mouse-grid", $"Expected ui-undo-mouse-grid target, got '{undoThatGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag", out var dragMarkedGridRoute), "Drag marked grid command should route.");
    Require(dragMarkedGridRoute.Target == "ui-drag-marked-mouse-grid", $"Expected ui-drag-marked-mouse-grid target, got '{dragMarkedGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hide grid", out var hideGridRoute), "Hide grid command should route.");
    Require(hideGridRoute.Target == "ui-hide-mouse-grid", $"Expected ui-hide-mouse-grid target, got '{hideGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("cancel mouse grid", out var cancelGridRoute), "Cancel mouse grid command should route.");
    Require(cancelGridRoute.Target == "ui-hide-mouse-grid", $"Expected ui-hide-mouse-grid target, got '{cancelGridRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click 3", out var clickNumberRoute), "Click 3 command should route.");
    Require(clickNumberRoute.Kind == AlphaCommandKind.UiAction, "Click 3 should be a UI action.");
    Require(clickNumberRoute.Target == "ui-activate-label:3", $"Expected ui-activate-label:3 target, got '{clickNumberRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("double click 3", out var doubleClickNumberRoute), "Double click 3 command should route.");
    Require(doubleClickNumberRoute.Kind == AlphaCommandKind.UiAction, "Double click 3 should be a UI action.");
    Require(doubleClickNumberRoute.Target == "ui-double-click-label:3", $"Expected ui-double-click-label:3 target, got '{doubleClickNumberRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("right click 3", out var rightClickNumberRoute), "Right click 3 command should route.");
    Require(rightClickNumberRoute.Kind == AlphaCommandKind.UiAction, "Right click 3 should be a UI action.");
    Require(rightClickNumberRoute.Target == "ui-right-click-label:3", $"Expected ui-right-click-label:3 target, got '{rightClickNumberRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("click one", out var clickOneRoute), "Click one command should route.");
    Require(clickOneRoute.Kind == AlphaCommandKind.UiAction, "Click one should be a UI action.");
    Require(clickOneRoute.Target == "ui-activate-label:1", $"Expected ui-activate-label:1 target, got '{clickOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("double click save as", out var doubleClickSaveAsRoute), "Double click label command should route.");
    Require(doubleClickSaveAsRoute.Target == "ui-double-click-label:save as", $"Expected ui-double-click-label:save as target, got '{doubleClickSaveAsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("right click save as", out var rightClickSaveAsRoute), "Right click label command should route.");
    Require(rightClickSaveAsRoute.Target == "ui-right-click-label:save as", $"Expected ui-right-click-label:save as target, got '{rightClickSaveAsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("tap second", out var tapSecondRoute), "Tap second command should route.");
    Require(tapSecondRoute.Target == "ui-activate-label:2", $"Expected ui-activate-label:2 target, got '{tapSecondRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("find file alpha-notes", out var fileRoute), "File search command should route.");
    Require(fileRoute.Kind == AlphaCommandKind.FileSearch, "File search command should be file-search kind.");
    Require(fileRoute.Target == "alpha-notes", $"File search target should be alpha-notes, got '{fileRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("search my pc for alpha notes", out var fileSearchPcRoute), "Search my PC file command should route.");
    Require(fileSearchPcRoute.Kind == AlphaCommandKind.FileSearch, "Search my PC file command should be file-search kind.");
    Require(fileSearchPcRoute.Target == "alpha notes", $"Expected alpha notes target, got '{fileSearchPcRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find a file called alpha notes", out var fileCalledRoute), "Find a file called command should route.");
    Require(fileCalledRoute.Kind == AlphaCommandKind.FileSearch, "Find a file called command should be file-search kind.");
    Require(fileCalledRoute.Target == "alpha notes", $"Expected alpha notes target, got '{fileCalledRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find folder named invoices", out var folderNamedRoute), "Find folder named command should route.");
    Require(folderNamedRoute.Kind == AlphaCommandKind.FileSearch, "Find folder named command should be file-search kind.");
    Require(folderNamedRoute.Target == "invoices", $"Expected invoices target, got '{folderNamedRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("search my folders for invoices", out var foldersRoute), "Search my folders command should route.");
    Require(foldersRoute.Kind == AlphaCommandKind.FileSearch, "Search my folders command should be file-search kind.");
    Require(foldersRoute.Target == "invoices", $"Expected invoices target, got '{foldersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open file result 2", out var openFileResultRoute), "Open file result command should route.");
    Require(openFileResultRoute.Kind == AlphaCommandKind.UiAction, "Open file result should be a UI action.");
    Require(openFileResultRoute.Target == "ui-open-file-result:2", $"Expected ui-open-file-result:2 target, got '{openFileResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open result 1", out var openResultRoute), "Open result command should route.");
    Require(openResultRoute.Kind == AlphaCommandKind.UiAction, "Open result should be a UI action.");
    Require(openResultRoute.Target == "ui-open-file-result:1", $"Expected ui-open-file-result:1 target, got '{openResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("reveal search result three", out var revealFileResultRoute), "Reveal file result command should route.");
    Require(revealFileResultRoute.Kind == AlphaCommandKind.UiAction, "Reveal file result should be a UI action.");
    Require(revealFileResultRoute.Target == "ui-reveal-file-result:3", $"Expected ui-reveal-file-result:3 target, got '{revealFileResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("reveal file result 1", out var revealFileResultOneRoute), "Reveal file result 1 command should route.");
    Require(revealFileResultOneRoute.Kind == AlphaCommandKind.UiAction, "Reveal file result 1 should be a UI action.");
    Require(revealFileResultOneRoute.Target == "ui-reveal-file-result:1", $"Expected ui-reveal-file-result:1 target, got '{revealFileResultOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show file result 1", out var showFileResultOneRoute), "Show file result 1 command should route.");
    Require(showFileResultOneRoute.Kind == AlphaCommandKind.UiAction, "Show file result 1 should be a UI action.");
    Require(showFileResultOneRoute.Target == "ui-reveal-file-result:1", $"Expected ui-reveal-file-result:1 target, got '{showFileResultOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select first result", out var selectFileResultRoute), "Select file result command should route.");
    Require(selectFileResultRoute.Kind == AlphaCommandKind.UiAction, "Select file result should be a UI action.");
    Require(selectFileResultRoute.Target == "ui-select-file-result:1", $"Expected ui-select-file-result:1 target, got '{selectFileResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open result eleventh", out var openEleventhResultRoute), "Open eleventh result command should route.");
    Require(openEleventhResultRoute.Target == "ui-open-file-result:11", $"Expected ui-open-file-result:11 target, got '{openEleventhResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open result twenty one", out var openTwentyOneResultRoute), "Open result twenty one command should route.");
    Require(openTwentyOneResultRoute.Target == "ui-open-file-result:21", $"Expected ui-open-file-result:21 target, got '{openTwentyOneResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("choose twentieth result", out var chooseTwentiethResultRoute), "Choose twentieth result command should route.");
    Require(chooseTwentiethResultRoute.Target == "ui-select-file-result:20", $"Expected ui-select-file-result:20 target, got '{chooseTwentiethResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("choose result thirty second", out var chooseThirtySecondResultRoute), "Choose result thirty second command should route.");
    Require(chooseThirtySecondResultRoute.Target == "ui-select-file-result:32", $"Expected ui-select-file-result:32 target, got '{chooseThirtySecondResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("reveal result thirty nine", out var revealThirtyNineResultRoute), "Reveal result thirty nine command should route.");
    Require(revealThirtyNineResultRoute.Target == "ui-reveal-file-result:39", $"Expected ui-reveal-file-result:39 target, got '{revealThirtyNineResultRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open containing folder for result 1", out var openContainingFolderRoute), "Open containing folder command should route.");
    Require(openContainingFolderRoute.Kind == AlphaCommandKind.UiAction, "Open containing folder should be a UI action.");
    Require(openContainingFolderRoute.Target == "ui-reveal-file-result:1", $"Expected ui-reveal-file-result:1 target, got '{openContainingFolderRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show containing folder for result 2", out var showContainingFolderRoute), "Show containing folder command should route.");
    Require(showContainingFolderRoute.Kind == AlphaCommandKind.UiAction, "Show containing folder should be a UI action.");
    Require(showContainingFolderRoute.Target == "ui-reveal-file-result:2", $"Expected ui-reveal-file-result:2 target, got '{showContainingFolderRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("volume up", out var systemVolumeUpRoute), "System volume up command should route.");
    Require(systemVolumeUpRoute.Kind == AlphaCommandKind.SystemControl, "Volume up command should be system-control kind.");
    Require(systemVolumeUpRoute.Target == "system-volume-up", $"Expected system-volume-up target, got '{systemVolumeUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("volume down", out var systemVolumeDownRoute), "System volume down command should route.");
    Require(systemVolumeDownRoute.Kind == AlphaCommandKind.SystemControl, "Volume down command should be system-control kind.");
    Require(systemVolumeDownRoute.Target == "system-volume-down", $"Expected system-volume-down target, got '{systemVolumeDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mute audio", out var systemMuteRoute), "System mute command should route.");
    Require(systemMuteRoute.Kind == AlphaCommandKind.SystemControl, "Mute command should be system-control kind.");
    Require(systemMuteRoute.Target == "system-volume-mute", $"Expected system-volume-mute target, got '{systemMuteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("play or pause", out var mediaPlayPauseRoute), "Media play/pause command should route.");
    Require(mediaPlayPauseRoute.Kind == AlphaCommandKind.SystemControl, "Media play/pause should be system-control kind.");
    Require(mediaPlayPauseRoute.Target == "system-media-play-pause", $"Expected system-media-play-pause target, got '{mediaPlayPauseRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next track", out var mediaNextRoute), "Media next track command should route.");
    Require(mediaNextRoute.Kind == AlphaCommandKind.SystemControl, "Media next track should be system-control kind.");
    Require(mediaNextRoute.Target == "system-media-next-track", $"Expected system-media-next-track target, got '{mediaNextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous song", out var mediaPreviousRoute), "Media previous track command should route.");
    Require(mediaPreviousRoute.Kind == AlphaCommandKind.SystemControl, "Media previous track should be system-control kind.");
    Require(mediaPreviousRoute.Target == "system-media-previous-track", $"Expected system-media-previous-track target, got '{mediaPreviousRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("stop playback", out var mediaStopRoute), "Media stop command should route.");
    Require(mediaStopRoute.Kind == AlphaCommandKind.SystemControl, "Media stop should be system-control kind.");
    Require(mediaStopRoute.Target == "system-media-stop", $"Expected system-media-stop target, got '{mediaStopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show desktop", out var showDesktopRoute), "System show desktop command should route.");
    Require(showDesktopRoute.Kind == AlphaCommandKind.SystemControl, "Show desktop command should be system-control kind.");
    Require(showDesktopRoute.Target == "system-show-desktop", $"Expected system-show-desktop target, got '{showDesktopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("minimize all windows", out var minimizeAllWindowsRoute), "Minimize all windows command should route.");
    Require(minimizeAllWindowsRoute.Kind == AlphaCommandKind.SystemControl, "Minimize all windows command should be system-control kind.");
    Require(minimizeAllWindowsRoute.Target == "system-show-desktop", $"Expected system-show-desktop target, got '{minimizeAllWindowsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch apps", out var switchAppsRoute), "Switch apps command should route.");
    Require(switchAppsRoute.Kind == AlphaCommandKind.SystemControl, "Switch apps command should be system-control kind.");
    Require(switchAppsRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{switchAppsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch application", out var switchApplicationRoute), "Switch application command should route.");
    Require(switchApplicationRoute.Kind == AlphaCommandKind.SystemControl, "Switch application command should be system-control kind.");
    Require(switchApplicationRoute.Target == "system-next-window", $"Expected system-next-window target, got '{switchApplicationRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("app switcher", out var appSwitcherRoute), "App switcher command should route.");
    Require(appSwitcherRoute.Kind == AlphaCommandKind.SystemControl, "App switcher command should be system-control kind.");
    Require(appSwitcherRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{appSwitcherRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next window", out var nextWindowRoute), "Next window command should route.");
    Require(nextWindowRoute.Kind == AlphaCommandKind.SystemControl, "Next window command should be system-control kind.");
    Require(nextWindowRoute.Target == "system-next-window", $"Expected system-next-window target, got '{nextWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next application", out var nextApplicationRoute), "Next application command should route.");
    Require(nextApplicationRoute.Target == "system-next-window", $"Expected system-next-window target, got '{nextApplicationRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch to the next window", out var nextWindowVerboseRoute), "Verbose next window command should route.");
    Require(nextWindowVerboseRoute.Kind == AlphaCommandKind.SystemControl, "Verbose next window command should be system-control kind.");
    Require(nextWindowVerboseRoute.Target == "system-next-window", $"Expected system-next-window target, got '{nextWindowVerboseRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch to the next app", out var nextAppRoute), "Next app command should route.");
    Require(nextAppRoute.Kind == AlphaCommandKind.SystemControl, "Next app command should be system-control kind.");
    Require(nextAppRoute.Target == "system-next-window", $"Expected system-next-window target, got '{nextAppRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous window", out var previousWindowRoute), "Previous window command should route.");
    Require(previousWindowRoute.Kind == AlphaCommandKind.SystemControl, "Previous window command should be system-control kind.");
    Require(previousWindowRoute.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("last app", out var lastAppRoute), "Last app command should route.");
    Require(lastAppRoute.Target == "system-previous-window", $"Expected system-previous-window target, got '{lastAppRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch to the previous window", out var previousWindowVerboseRoute), "Verbose previous window command should route.");
    Require(previousWindowVerboseRoute.Kind == AlphaCommandKind.SystemControl, "Verbose previous window command should be system-control kind.");
    Require(previousWindowVerboseRoute.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousWindowVerboseRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch to the previous app", out var previousAppRoute), "Previous app command should route.");
    Require(previousAppRoute.Kind == AlphaCommandKind.SystemControl, "Previous app command should be system-control kind.");
    Require(previousAppRoute.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousAppRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show open windows", out var taskViewRoute), "Task view command should route.");
    Require(taskViewRoute.Kind == AlphaCommandKind.SystemControl, "Task view command should be system-control kind.");
    Require(taskViewRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{taskViewRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("task view", out var taskViewAliasRoute), "Task view alias should route.");
    Require(taskViewAliasRoute.Kind == AlphaCommandKind.SystemControl, "Task view alias should be system-control kind.");
    Require(taskViewAliasRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{taskViewAliasRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open task view", out var openTaskViewRoute), "Open task view alias should route.");
    Require(openTaskViewRoute.Kind == AlphaCommandKind.SystemControl, "Open task view alias should be system-control kind.");
    Require(openTaskViewRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{openTaskViewRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show task view", out var showTaskViewRoute), "Show task view alias should route.");
    Require(showTaskViewRoute.Kind == AlphaCommandKind.SystemControl, "Show task view alias should be system-control kind.");
    Require(showTaskViewRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{showTaskViewRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show all windows", out var showAllWindowsRoute), "Show all windows alias should route.");
    Require(showAllWindowsRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{showAllWindowsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("window switcher", out var windowSwitcherRoute), "Window switcher alias should route.");
    Require(windowSwitcherRoute.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{windowSwitcherRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("switch windows", out var switchWindowsRoute), "Switch windows alias should route.");
    Require(switchWindowsRoute.Target == "system-next-window", $"Expected system-next-window target, got '{switchWindowsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("quick settings", out var quickSettingsRoute), "Quick Settings command should route.");
    Require(quickSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Quick Settings command should be system-control kind.");
    Require(quickSettingsRoute.Target == "system-open-quick-settings", $"Expected system-open-quick-settings target, got '{quickSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("notification center", out var notificationCenterRoute), "Notification Center command should route.");
    Require(notificationCenterRoute.Kind == AlphaCommandKind.SystemControl, "Notification Center command should be system-control kind.");
    Require(notificationCenterRoute.Target == "system-open-notification-center", $"Expected system-open-notification-center target, got '{notificationCenterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("emoji panel", out var emojiPanelRoute), "Emoji panel command should route.");
    Require(emojiPanelRoute.Kind == AlphaCommandKind.SystemControl, "Emoji panel command should be system-control kind.");
    Require(emojiPanelRoute.Target == "system-open-emoji-panel", $"Expected system-open-emoji-panel target, got '{emojiPanelRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show symbol picker", out var symbolPickerRoute), "Symbol picker command should route.");
    Require(symbolPickerRoute.Kind == AlphaCommandKind.SystemControl, "Symbol picker command should be system-control kind.");
    Require(symbolPickerRoute.Target == "system-open-emoji-panel", $"Expected system-open-emoji-panel target, got '{symbolPickerRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("clipboard history", out var clipboardHistoryRoute), "Clipboard history command should route.");
    Require(clipboardHistoryRoute.Kind == AlphaCommandKind.SystemControl, "Clipboard history command should be system-control kind.");
    Require(clipboardHistoryRoute.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{clipboardHistoryRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show clipboard panel", out var clipboardPanelRoute), "Clipboard panel command should route.");
    Require(clipboardPanelRoute.Kind == AlphaCommandKind.SystemControl, "Clipboard panel command should be system-control kind.");
    Require(clipboardPanelRoute.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{clipboardPanelRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show clipboard picker", out var clipboardPickerRoute), "Clipboard picker command should route.");
    Require(clipboardPickerRoute.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{clipboardPickerRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open clipboard", out var openClipboardRoute), "Open clipboard command should route to visible clipboard history.");
    Require(openClipboardRoute.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{openClipboardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("snipping toolbar", out var snippingToolbarRoute), "Snipping toolbar command should route.");
    Require(snippingToolbarRoute.Kind == AlphaCommandKind.SystemControl, "Snipping toolbar command should be system-control kind.");
    Require(snippingToolbarRoute.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{snippingToolbarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("take screenshot", out var takeScreenshotRoute), "Take screenshot command should route to snipping toolbar.");
    Require(takeScreenshotRoute.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{takeScreenshotRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show screenshot toolbar", out var showScreenshotToolbarRoute), "Show screenshot toolbar command should route.");
    Require(showScreenshotToolbarRoute.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{showScreenshotToolbarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open screenshot tools", out var openScreenshotToolsRoute), "Open screenshot tools command should route.");
    Require(openScreenshotToolsRoute.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{openScreenshotToolsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("project display", out var projectDisplayRoute), "Project display command should route.");
    Require(projectDisplayRoute.Kind == AlphaCommandKind.SystemControl, "Project display command should be system-control kind.");
    Require(projectDisplayRoute.Target == "system-open-project-display", $"Expected system-open-project-display target, got '{projectDisplayRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("wireless display", out var wirelessDisplayRoute), "Wireless display command should route.");
    Require(wirelessDisplayRoute.Kind == AlphaCommandKind.SystemControl, "Wireless display command should be system-control kind.");
    Require(wirelessDisplayRoute.Target == "system-open-cast-display", $"Expected system-open-cast-display target, got '{wirelessDisplayRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("new virtual desktop", out var newDesktopRoute), "New virtual desktop command should route.");
    Require(newDesktopRoute.Kind == AlphaCommandKind.SystemControl, "New virtual desktop command should be system-control kind.");
    Require(newDesktopRoute.Target == "system-new-virtual-desktop", $"Expected system-new-virtual-desktop target, got '{newDesktopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next desktop", out var nextDesktopRoute), "Next virtual desktop command should route.");
    Require(nextDesktopRoute.Kind == AlphaCommandKind.SystemControl, "Next virtual desktop command should be system-control kind.");
    Require(nextDesktopRoute.Target == "system-next-virtual-desktop", $"Expected system-next-virtual-desktop target, got '{nextDesktopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous desktop", out var previousDesktopRoute), "Previous virtual desktop command should route.");
    Require(previousDesktopRoute.Kind == AlphaCommandKind.SystemControl, "Previous virtual desktop command should be system-control kind.");
    Require(previousDesktopRoute.Target == "system-previous-virtual-desktop", $"Expected system-previous-virtual-desktop target, got '{previousDesktopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("task manager", out var taskManagerRoute), "Task manager command should route.");
    Require(taskManagerRoute.Kind == AlphaCommandKind.SystemControl, "Task manager command should be system-control kind.");
    Require(taskManagerRoute.Target == "system-open-task-manager", $"Expected system-open-task-manager target, got '{taskManagerRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("windows settings", out var settingsRoute), "Windows settings command should route.");
    Require(settingsRoute.Kind == AlphaCommandKind.SystemControl, "Windows settings command should be system-control kind.");
    Require(settingsRoute.Target == "system-open-settings", $"Expected system-open-settings target, got '{settingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open display settings", out var displaySettingsRoute), "Display settings command should route.");
    Require(displaySettingsRoute.Kind == AlphaCommandKind.SystemControl, "Display settings command should be system-control kind.");
    Require(displaySettingsRoute.Target == "system-open-display-settings", $"Expected system-open-display-settings target, got '{displaySettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open sound settings", out var soundSettingsRoute), "Sound settings command should route.");
    Require(soundSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Sound settings command should be system-control kind.");
    Require(soundSettingsRoute.Target == "system-open-sound-settings", $"Expected system-open-sound-settings target, got '{soundSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open bluetooth settings", out var bluetoothSettingsRoute), "Bluetooth settings command should route.");
    Require(bluetoothSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Bluetooth settings command should be system-control kind.");
    Require(bluetoothSettingsRoute.Target == "system-open-bluetooth-settings", $"Expected system-open-bluetooth-settings target, got '{bluetoothSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("wifi settings", out var wifiSettingsRoute), "Wi-Fi settings command should route.");
    Require(wifiSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Wi-Fi settings command should be system-control kind.");
    Require(wifiSettingsRoute.Target == "system-open-wifi-settings", $"Expected system-open-wifi-settings target, got '{wifiSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("accessibility settings", out var accessibilitySettingsRoute), "Accessibility settings command should route.");
    Require(accessibilitySettingsRoute.Kind == AlphaCommandKind.SystemControl, "Accessibility settings command should be system-control kind.");
    Require(accessibilitySettingsRoute.Target == "system-open-accessibility-settings", $"Expected system-open-accessibility-settings target, got '{accessibilitySettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("magnifier settings", out var magnifierSettingsRoute), "Magnifier settings command should route.");
    Require(magnifierSettingsRoute.Target == "system-open-magnifier-settings", $"Expected system-open-magnifier-settings target, got '{magnifierSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("zoom settings", out var zoomSettingsRoute), "Zoom settings command should route to Magnifier settings.");
    Require(zoomSettingsRoute.Target == "system-open-magnifier-settings", $"Expected system-open-magnifier-settings target, got '{zoomSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("narrator settings", out var narratorSettingsRoute), "Narrator settings command should route.");
    Require(narratorSettingsRoute.Target == "system-open-narrator-settings", $"Expected system-open-narrator-settings target, got '{narratorSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("captions settings", out var captionsSettingsRoute), "Captions settings command should route.");
    Require(captionsSettingsRoute.Target == "system-open-captions-settings", $"Expected system-open-captions-settings target, got '{captionsSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("live captions settings", out var liveCaptionsSettingsRoute), "Live captions settings command should route.");
    Require(liveCaptionsSettingsRoute.Target == "system-open-captions-settings", $"Expected system-open-captions-settings target, got '{liveCaptionsSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("speech settings", out var speechSettingsRoute), "Speech settings command should route.");
    Require(speechSettingsRoute.Target == "system-open-speech-settings", $"Expected system-open-speech-settings target, got '{speechSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("voice access settings", out var voiceAccessSettingsRoute), "Voice Access settings command should route to speech settings.");
    Require(voiceAccessSettingsRoute.Target == "system-open-speech-settings", $"Expected system-open-speech-settings target, got '{voiceAccessSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("voice typing settings", out var voiceTypingSettingsRoute), "Voice typing settings command should route to speech settings.");
    Require(voiceTypingSettingsRoute.Target == "system-open-speech-settings", $"Expected system-open-speech-settings target, got '{voiceTypingSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open magnifier", out var openMagnifierRoute), "Open magnifier command should route.");
    Require(openMagnifierRoute.Kind == AlphaCommandKind.SystemControl, "Open magnifier command should be system-control kind.");
    Require(openMagnifierRoute.Target == "system-open-magnifier", $"Expected system-open-magnifier target, got '{openMagnifierRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("magnifier zoom out", out var magnifierZoomOutRoute), "Magnifier zoom-out command should route.");
    Require(magnifierZoomOutRoute.Target == "system-magnifier-zoom-out", $"Expected system-magnifier-zoom-out target, got '{magnifierZoomOutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("close magnifier", out var closeMagnifierRoute), "Close magnifier command should route.");
    Require(closeMagnifierRoute.Target == "system-close-magnifier", $"Expected system-close-magnifier target, got '{closeMagnifierRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("power and battery settings", out var powerSettingsRoute), "Power and battery settings command should route.");
    Require(powerSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Power settings command should be system-control kind.");
    Require(powerSettingsRoute.Target == "system-open-power-settings", $"Expected system-open-power-settings target, got '{powerSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("installed apps settings", out var appsSettingsRoute), "Installed apps settings command should route.");
    Require(appsSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Apps settings command should be system-control kind.");
    Require(appsSettingsRoute.Target == "system-open-apps-settings", $"Expected system-open-apps-settings target, got '{appsSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("default apps settings", out var defaultAppsSettingsRoute), "Default apps settings command should route.");
    Require(defaultAppsSettingsRoute.Kind == AlphaCommandKind.SystemControl, "Default apps settings command should be system-control kind.");
    Require(defaultAppsSettingsRoute.Target == "system-open-default-apps-settings", $"Expected system-open-default-apps-settings target, got '{defaultAppsSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("date and time settings", out var dateTimeSettingsRoute), "Date and time settings command should route.");
    Require(dateTimeSettingsRoute.Target == "system-open-date-time-settings", $"Expected system-open-date-time-settings target, got '{dateTimeSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("notifications settings", out var notificationsSettingsRoute), "Notifications settings command should route.");
    Require(notificationsSettingsRoute.Target == "system-open-notifications-settings", $"Expected system-open-notifications-settings target, got '{notificationsSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("windows update settings", out var windowsUpdateSettingsRoute), "Windows Update settings command should route.");
    Require(windowsUpdateSettingsRoute.Target == "system-open-windows-update-settings", $"Expected system-open-windows-update-settings target, got '{windowsUpdateSettingsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("personalization settings", out var personalizationSettingsRoute), "Personalization settings command should route.");
    Require(personalizationSettingsRoute.Target == "system-open-personalization-settings", $"Expected system-open-personalization-settings target, got '{personalizationSettingsRoute.Target}'.");
    Require(!AlphaCommandRouter.TryRoute("open settings", out _), "Plain open settings should remain the Start menu launch path.");
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
    Require(AlphaCommandRouter.TryRoute("snap window left", out var snapLeftRoute), "Snap window left command should route.");
    Require(snapLeftRoute.Kind == AlphaCommandKind.SystemControl, "Snap left should be system-control kind.");
    Require(snapLeftRoute.Target == "system-snap-window-left", $"Expected system-snap-window-left target, got '{snapLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("snap right", out var snapRightRoute), "Snap right command should route.");
    Require(snapRightRoute.Kind == AlphaCommandKind.SystemControl, "Snap right should be system-control kind.");
    Require(snapRightRoute.Target == "system-snap-window-right", $"Expected system-snap-window-right target, got '{snapRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move window up", out var snapUpRoute), "Snap window up command should route.");
    Require(snapUpRoute.Kind == AlphaCommandKind.SystemControl, "Snap up should be system-control kind.");
    Require(snapUpRoute.Target == "system-snap-window-up", $"Expected system-snap-window-up target, got '{snapUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("dock window down", out var snapDownRoute), "Snap window down command should route.");
    Require(snapDownRoute.Kind == AlphaCommandKind.SystemControl, "Snap down should be system-control kind.");
    Require(snapDownRoute.Target == "system-snap-window-down", $"Expected system-snap-window-down target, got '{snapDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show snap layouts", out var snapLayoutsRoute), "Snap layouts command should route.");
    Require(snapLayoutsRoute.Kind == AlphaCommandKind.SystemControl, "Snap layouts should be system-control kind.");
    Require(snapLayoutsRoute.Target == "system-show-snap-layouts", $"Expected system-show-snap-layouts target, got '{snapLayoutsRoute.Target}'.");
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
    Require(AlphaCommandRouter.TryRoute("press space", out var spaceRoute), "Press space command should route.");
    Require(spaceRoute.Kind == AlphaCommandKind.SystemControl, "Press space command should be system-control kind.");
    Require(spaceRoute.Target == "system-press-space", $"Expected system-press-space target, got '{spaceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press delete", out var deleteRoute), "Press delete command should route.");
    Require(deleteRoute.Kind == AlphaCommandKind.SystemControl, "Press delete command should be system-control kind.");
    Require(deleteRoute.Target == "system-press-delete", $"Expected system-press-delete target, got '{deleteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press insert", out var insertRoute), "Press insert command should route.");
    Require(insertRoute.Kind == AlphaCommandKind.SystemControl, "Press insert command should be system-control kind.");
    Require(insertRoute.Target == "system-press-insert", $"Expected system-press-insert target, got '{insertRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press windows key", out var windowsKeyRoute), "Press Windows key command should route.");
    Require(windowsKeyRoute.Kind == AlphaCommandKind.SystemControl, "Press Windows key should be system-control kind.");
    Require(windowsKeyRoute.Target == "system-press-windows", $"Expected system-press-windows target, got '{windowsKeyRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("context menu key", out var contextMenuRoute), "Context menu key command should route.");
    Require(contextMenuRoute.Kind == AlphaCommandKind.SystemControl, "Context menu key should be system-control kind.");
    Require(contextMenuRoute.Target == "system-press-context-menu", $"Expected system-press-context-menu target, got '{contextMenuRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press caps lock", out var capsLockRoute), "Caps Lock command should route.");
    Require(capsLockRoute.Kind == AlphaCommandKind.SystemControl, "Caps Lock should be system-control kind.");
    Require(capsLockRoute.Target == "system-press-caps-lock", $"Expected system-press-caps-lock target, got '{capsLockRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press up arrow", out var upArrowRoute), "Press up arrow command should route.");
    Require(upArrowRoute.Kind == AlphaCommandKind.SystemControl, "Press up arrow should be system-control kind.");
    Require(upArrowRoute.Target == "system-press-up", $"Expected system-press-up target, got '{upArrowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("right arrow", out var rightArrowRoute), "Right arrow command should route.");
    Require(rightArrowRoute.Kind == AlphaCommandKind.SystemControl, "Right arrow should be system-control kind.");
    Require(rightArrowRoute.Target == "system-press-right", $"Expected system-press-right target, got '{rightArrowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press f5", out var functionFiveRoute), "Press F5 command should route.");
    Require(functionFiveRoute.Kind == AlphaCommandKind.SystemControl, "Press F5 command should be system-control kind.");
    Require(functionFiveRoute.Target == "system-press-f5", $"Expected system-press-f5 target, got '{functionFiveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("function key twelve", out var functionTwelveRoute), "Function key twelve command should route.");
    Require(functionTwelveRoute.Kind == AlphaCommandKind.SystemControl, "Function key twelve command should be system-control kind.");
    Require(functionTwelveRoute.Target == "system-press-f12", $"Expected system-press-f12 target, got '{functionTwelveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press 5", out var digitFiveRoute), "Press digit command should route.");
    Require(digitFiveRoute.Kind == AlphaCommandKind.SystemControl, "Press digit should be system-control kind.");
    Require(digitFiveRoute.Target == "system-press-digit:5", $"Expected system-press-digit:5 target, got '{digitFiveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("number key zero", out var digitZeroRoute), "Number key zero command should route.");
    Require(digitZeroRoute.Kind == AlphaCommandKind.SystemControl, "Number key zero should be system-control kind.");
    Require(digitZeroRoute.Target == "system-press-digit:0", $"Expected system-press-digit:0 target, got '{digitZeroRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press number nine", out var digitNineRoute), "Spoken digit command should route.");
    Require(digitNineRoute.Target == "system-press-digit:9", $"Expected system-press-digit:9 target, got '{digitNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press a", out var letterARoute), "Press letter command should route.");
    Require(letterARoute.Kind == AlphaCommandKind.SystemControl, "Press letter should be system-control kind.");
    Require(letterARoute.Target == "system-press-letter:a", $"Expected system-press-letter:a target, got '{letterARoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("letter key z", out var letterZRoute), "Letter key command should route.");
    Require(letterZRoute.Kind == AlphaCommandKind.SystemControl, "Letter key should be system-control kind.");
    Require(letterZRoute.Target == "system-press-letter:z", $"Expected system-press-letter:z target, got '{letterZRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press comma", out var commaRoute), "Press comma command should route.");
    Require(commaRoute.Kind == AlphaCommandKind.SystemControl, "Press comma should be system-control kind.");
    Require(commaRoute.Target == "system-press-symbol:comma", $"Expected system-press-symbol:comma target, got '{commaRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press question mark", out var questionRoute), "Press question mark command should route.");
    Require(questionRoute.Kind == AlphaCommandKind.SystemControl, "Press question mark should be system-control kind.");
    Require(questionRoute.Target == "system-press-symbol:question", $"Expected system-press-symbol:question target, got '{questionRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("symbol key at sign", out var atRoute), "Symbol key at sign command should route.");
    Require(atRoute.Kind == AlphaCommandKind.SystemControl, "Symbol key at sign should be system-control kind.");
    Require(atRoute.Target == "system-press-symbol:at", $"Expected system-press-symbol:at target, got '{atRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift tab", out var shiftTabRoute), "Press Shift Tab command should route.");
    Require(shiftTabRoute.Kind == AlphaCommandKind.SystemControl, "Press Shift Tab should be system-control kind.");
    Require(shiftTabRoute.Target == "system-press-chord:shift-tab", $"Expected system-press-chord:shift-tab target, got '{shiftTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift a", out var shiftARoute), "Press Shift A command should route through the generic Shift-letter path.");
    Require(shiftARoute.Kind == AlphaCommandKind.SystemControl, "Press Shift A should be system-control kind.");
    Require(shiftARoute.Target == "system-press-chord:shift-a", $"Expected system-press-chord:shift-a target, got '{shiftARoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift z", out var shiftZRoute), "Press Shift Z command should route through the generic Shift-letter path.");
    Require(shiftZRoute.Target == "system-press-chord:shift-z", $"Expected system-press-chord:shift-z target, got '{shiftZRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift 1", out var shiftOneRoute), "Press Shift 1 command should route through the generic Shift-digit path.");
    Require(shiftOneRoute.Target == "system-press-chord:shift-1", $"Expected system-press-chord:shift-1 target, got '{shiftOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift number nine", out var shiftNineRoute), "Press Shift number nine command should route through the generic Shift-digit path.");
    Require(shiftNineRoute.Target == "system-press-chord:shift-9", $"Expected system-press-chord:shift-9 target, got '{shiftNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control tab", out var controlTabRoute), "Press Control Tab command should route.");
    Require(controlTabRoute.Kind == AlphaCommandKind.SystemControl, "Press Control Tab should be system-control kind.");
    Require(controlTabRoute.Target == "system-press-chord:control-tab", $"Expected system-press-chord:control-tab target, got '{controlTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control c", out var controlCRoute), "Press Control C command should route.");
    Require(controlCRoute.Kind == AlphaCommandKind.SystemControl, "Press Control C should be system-control kind.");
    Require(controlCRoute.Target == "system-press-chord:control-c", $"Expected system-press-chord:control-c target, got '{controlCRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control v", out var controlVRoute), "Press Control V command should route.");
    Require(controlVRoute.Target == "system-press-chord:control-v", $"Expected system-press-chord:control-v target, got '{controlVRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control a", out var controlARoute), "Press Control A command should route.");
    Require(controlARoute.Target == "system-press-chord:control-a", $"Expected system-press-chord:control-a target, got '{controlARoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control s", out var controlSRoute), "Press Control S command should route.");
    Require(controlSRoute.Target == "system-press-chord:control-s", $"Expected system-press-chord:control-s target, got '{controlSRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control f", out var controlFRoute), "Press Control F command should route.");
    Require(controlFRoute.Target == "system-press-chord:control-f", $"Expected system-press-chord:control-f target, got '{controlFRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control r", out var controlRRoute), "Press Control R command should route through the generic Control-letter path.");
    Require(controlRRoute.Target == "system-press-chord:control-r", $"Expected system-press-chord:control-r target, got '{controlRRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control l", out var controlLRoute), "Press Control L command should route through the generic Control-letter path.");
    Require(controlLRoute.Target == "system-press-chord:control-l", $"Expected system-press-chord:control-l target, got '{controlLRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control w", out var controlWRoute), "Press Control W command should route through the generic Control-letter path.");
    Require(controlWRoute.Target == "system-press-chord:control-w", $"Expected system-press-chord:control-w target, got '{controlWRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control 1", out var controlOneRoute), "Press Control 1 command should route through the generic Control-digit path.");
    Require(controlOneRoute.Target == "system-press-chord:control-1", $"Expected system-press-chord:control-1 target, got '{controlOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control number nine", out var controlNineRoute), "Press Control number nine command should route through the generic Control-digit path.");
    Require(controlNineRoute.Target == "system-press-chord:control-9", $"Expected system-press-chord:control-9 target, got '{controlNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control plus", out var controlPlusRoute), "Press Control Plus command should route.");
    Require(controlPlusRoute.Target == "system-press-chord:control-plus", $"Expected system-press-chord:control-plus target, got '{controlPlusRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control minus", out var controlMinusRoute), "Press Control Minus command should route.");
    Require(controlMinusRoute.Target == "system-press-chord:control-minus", $"Expected system-press-chord:control-minus target, got '{controlMinusRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control zero", out var controlZeroRoute), "Press Control Zero command should route.");
    Require(controlZeroRoute.Target == "system-press-chord:control-zero", $"Expected system-press-chord:control-zero target, got '{controlZeroRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control shift end", out var controlShiftEndRoute), "Press Control Shift End command should route.");
    Require(controlShiftEndRoute.Kind == AlphaCommandKind.SystemControl, "Press Control Shift End should be system-control kind.");
    Require(controlShiftEndRoute.Target == "system-press-chord:control-shift-end", $"Expected system-press-chord:control-shift-end target, got '{controlShiftEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control shift t", out var controlShiftTRoute), "Press Control Shift T command should route through the generic Control-Shift-letter path.");
    Require(controlShiftTRoute.Kind == AlphaCommandKind.SystemControl, "Press Control Shift T should be system-control kind.");
    Require(controlShiftTRoute.Target == "system-press-chord:control-shift-t", $"Expected system-press-chord:control-shift-t target, got '{controlShiftTRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift control n", out var shiftControlNRoute), "Press Shift Control N command should route through the generic Control-Shift-letter path.");
    Require(shiftControlNRoute.Target == "system-press-chord:control-shift-n", $"Expected system-press-chord:control-shift-n target, got '{shiftControlNRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control shift 1", out var controlShiftOneRoute), "Press Control Shift 1 command should route through the generic Control-Shift-digit path.");
    Require(controlShiftOneRoute.Target == "system-press-chord:control-shift-1", $"Expected system-press-chord:control-shift-1 target, got '{controlShiftOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control shift number nine", out var controlShiftNineRoute), "Press Control Shift number nine command should route through the generic Control-Shift-digit path.");
    Require(controlShiftNineRoute.Target == "system-press-chord:control-shift-9", $"Expected system-press-chord:control-shift-9 target, got '{controlShiftNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt right", out var altRightRoute), "Press Alt Right command should route.");
    Require(altRightRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt Right should be system-control kind.");
    Require(altRightRoute.Target == "system-press-chord:alt-right", $"Expected system-press-chord:alt-right target, got '{altRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt up", out var altUpRoute), "Press Alt Up command should route.");
    Require(altUpRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt Up should be system-control kind.");
    Require(altUpRoute.Target == "system-press-chord:alt-up", $"Expected system-press-chord:alt-up target, got '{altUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt down", out var altDownRoute), "Press Alt Down command should route.");
    Require(altDownRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt Down should be system-control kind.");
    Require(altDownRoute.Target == "system-press-chord:alt-down", $"Expected system-press-chord:alt-down target, got '{altDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt shift tab", out var altShiftTabRoute), "Press Alt Shift Tab command should route.");
    Require(altShiftTabRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt Shift Tab should be system-control kind.");
    Require(altShiftTabRoute.Target == "system-press-chord:alt-shift-tab", $"Expected system-press-chord:alt-shift-tab target, got '{altShiftTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press shift alt tab", out var shiftAltTabRoute), "Press Shift Alt Tab command should route.");
    Require(shiftAltTabRoute.Target == "system-press-chord:alt-shift-tab", $"Expected system-press-chord:alt-shift-tab target, got '{shiftAltTabRoute.Target}'.");
    Require(!AlphaCommandRouter.TryRoute("press control alt delete", out _), "Control Alt Delete should not route as a safe keyboard parity chord.");
    Require(AlphaCommandRouter.TryRoute("press alt f", out var altFRoute), "Press Alt F command should route through the generic Alt-letter path.");
    Require(altFRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt F should be system-control kind.");
    Require(altFRoute.Target == "system-press-chord:alt-f", $"Expected system-press-chord:alt-f target, got '{altFRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt h", out var altHRoute), "Press Alt H command should route through the generic Alt-letter path.");
    Require(altHRoute.Target == "system-press-chord:alt-h", $"Expected system-press-chord:alt-h target, got '{altHRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt 1", out var altOneRoute), "Press Alt 1 command should route through the generic Alt-digit path.");
    Require(altOneRoute.Target == "system-press-chord:alt-1", $"Expected system-press-chord:alt-1 target, got '{altOneRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt number nine", out var altNineRoute), "Press Alt number nine command should route through the generic Alt-digit path.");
    Require(altNineRoute.Target == "system-press-chord:alt-9", $"Expected system-press-chord:alt-9 target, got '{altNineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control home", out var controlHomeRoute), "Press Control Home command should route.");
    Require(controlHomeRoute.Kind == AlphaCommandKind.SystemControl, "Press Control Home should be system-control kind.");
    Require(controlHomeRoute.Target == "system-press-chord:control-home", $"Expected system-press-chord:control-home target, got '{controlHomeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press control shift home", out var controlShiftHomeRoute), "Press Control Shift Home command should route.");
    Require(controlShiftHomeRoute.Kind == AlphaCommandKind.SystemControl, "Press Control Shift Home should be system-control kind.");
    Require(controlShiftHomeRoute.Target == "system-press-chord:control-shift-home", $"Expected system-press-chord:control-shift-home target, got '{controlShiftHomeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press alt left", out var altLeftRoute), "Press Alt Left command should route.");
    Require(altLeftRoute.Kind == AlphaCommandKind.SystemControl, "Press Alt Left should be system-control kind.");
    Require(AlphaCommandRouter.TryRoute("hold shift", out var holdShiftRoute), "Hold Shift command should route.");
    Require(holdShiftRoute.Kind == AlphaCommandKind.SystemControl, "Hold Shift should be system-control kind.");
    Require(holdShiftRoute.Target == "system-hold-modifier:shift", $"Expected system-hold-modifier:shift target, got '{holdShiftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press and hold control key", out var holdControlRoute), "Press-and-hold Control command should route.");
    Require(holdControlRoute.Target == "system-hold-modifier:control", $"Expected system-hold-modifier:control target, got '{holdControlRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("dismiss", out var dismissRoute), "Dismiss command should route.");
    Require(dismissRoute.Target == "system-press-escape", $"Expected dismiss to route to system-press-escape, got '{dismissRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("release alt", out var releaseAltRoute), "Release Alt command should route.");
    Require(releaseAltRoute.Target == "system-release-modifier:alt", $"Expected system-release-modifier:alt target, got '{releaseAltRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("release all modifiers", out var releaseModifiersRoute), "Release all modifiers command should route.");
    Require(releaseModifiersRoute.Target == "system-release-modifiers", $"Expected system-release-modifiers target, got '{releaseModifiersRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("release", out var releaseRoute), "Plain release command should route.");
    Require(releaseRoute.Target == "system-release-modifiers", $"Expected plain release to route to system-release-modifiers, got '{releaseRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press tab five times", out var repeatTabRoute), "Repeated Tab command should route.");
    Require(repeatTabRoute.Target == "system-repeat:system-press-tab:5", $"Expected repeated tab target, got '{repeatTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("press down three times", out var repeatDownRoute), "Repeated down-arrow command should route.");
    Require(repeatDownRoute.Target == "system-repeat:system-press-down:3", $"Expected repeated down target, got '{repeatDownRoute.Target}'.");
    Require(!AlphaCommandRouter.TryRoute("hold windows key", out _), "Held Windows key should not route as a safe held modifier.");
    Require(altLeftRoute.Target == "system-press-chord:alt-left", $"Expected system-press-chord:alt-left target, got '{altLeftRoute.Target}'.");
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
    Require(AlphaCommandRouter.TryRoute("tap", out var mouseTapRoute), "Tap command should route.");
    Require(mouseTapRoute.Target == "system-mouse-click", $"Expected tap to route to system-mouse-click, got '{mouseTapRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("double click", out var mouseDoubleClickRoute), "Double click command should route.");
    Require(mouseDoubleClickRoute.Kind == AlphaCommandKind.SystemControl, "Double click command should be system-control kind.");
    Require(mouseDoubleClickRoute.Target == "system-mouse-double-click", $"Expected system-mouse-double-click target, got '{mouseDoubleClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("triple click", out var mouseTripleClickRoute), "Triple click command should route.");
    Require(mouseTripleClickRoute.Kind == AlphaCommandKind.SystemControl, "Triple click command should be system-control kind.");
    Require(mouseTripleClickRoute.Target == "system-mouse-triple-click", $"Expected system-mouse-triple-click target, got '{mouseTripleClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("right click", out var mouseRightClickRoute), "Right click command should route.");
    Require(mouseRightClickRoute.Kind == AlphaCommandKind.SystemControl, "Right click command should be system-control kind.");
    Require(mouseRightClickRoute.Target == "system-mouse-right-click", $"Expected system-mouse-right-click target, got '{mouseRightClickRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("hold mouse", out var mouseButtonDownRoute), "Mouse button down command should route.");
    Require(mouseButtonDownRoute.Kind == AlphaCommandKind.SystemControl, "Mouse button down should be system-control kind.");
    Require(mouseButtonDownRoute.Target == "system-mouse-button-down", $"Expected system-mouse-button-down target, got '{mouseButtonDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("release mouse", out var mouseButtonUpRoute), "Mouse button up command should route.");
    Require(mouseButtonUpRoute.Kind == AlphaCommandKind.SystemControl, "Mouse button up should be system-control kind.");
    Require(mouseButtonUpRoute.Target == "system-mouse-button-up", $"Expected system-mouse-button-up target, got '{mouseButtonUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse scroll up", out var mouseScrollUpRoute), "Mouse scroll up command should route.");
    Require(mouseScrollUpRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll up command should be system-control kind.");
    Require(mouseScrollUpRoute.Target == "system-mouse-scroll-up", $"Expected system-mouse-scroll-up target, got '{mouseScrollUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse scroll down", out var mouseScrollDownRoute), "Mouse scroll down command should route.");
    Require(mouseScrollDownRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll down command should be system-control kind.");
    Require(mouseScrollDownRoute.Target == "system-mouse-scroll-down", $"Expected system-mouse-scroll-down target, got '{mouseScrollDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse scroll down a little", out var mouseScrollDownLittleRoute), "Mouse scroll down a little command should route.");
    Require(mouseScrollDownLittleRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll down a little should be system-control kind.");
    Require(mouseScrollDownLittleRoute.Target == "system-mouse-scroll-down", $"Expected system-mouse-scroll-down target, got '{mouseScrollDownLittleRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("scroll down a little", out var browserScrollDownLittleRoute), "Plain scroll down a little should route as browser/page scroll.");
    Require(browserScrollDownLittleRoute.Kind == AlphaCommandKind.Browser, "Plain scroll down a little should be browser kind.");
    Require(browserScrollDownLittleRoute.Target == "browser-scroll-down", $"Expected browser-scroll-down target, got '{browserScrollDownLittleRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("scroll left", out var mouseScrollLeftRoute), "Mouse scroll left command should route.");
    Require(mouseScrollLeftRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll left command should be system-control kind.");
    Require(mouseScrollLeftRoute.Target == "system-mouse-scroll-left", $"Expected system-mouse-scroll-left target, got '{mouseScrollLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("scroll right", out var mouseScrollRightRoute), "Mouse scroll right command should route.");
    Require(mouseScrollRightRoute.Kind == AlphaCommandKind.SystemControl, "Mouse scroll right command should be system-control kind.");
    Require(mouseScrollRightRoute.Target == "system-mouse-scroll-right", $"Expected system-mouse-scroll-right target, got '{mouseScrollRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse up", out var mouseMoveUpRoute), "Mouse move up command should route.");
    Require(mouseMoveUpRoute.Kind == AlphaCommandKind.SystemControl, "Mouse move up should be system-control kind.");
    Require(mouseMoveUpRoute.Target == "system-mouse-start-moving:up", $"Expected system-mouse-start-moving:up target, got '{mouseMoveUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse top left", out var mouseMoveTopLeftRoute), "Mouse move top-left command should route.");
    Require(mouseMoveTopLeftRoute.Target == "system-mouse-start-moving:top-left", $"Expected system-mouse-start-moving:top-left target, got '{mouseMoveTopLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse left five", out var mouseMoveLeftFiveRoute), "Fixed-distance mouse move should route.");
    Require(mouseMoveLeftFiveRoute.Target == "system-mouse-move-fixed:left:5", $"Expected system-mouse-move-fixed:left:5 target, got '{mouseMoveLeftFiveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("nudge up", out var nudgeUpRoute), "Nudge up command should route.");
    Require(nudgeUpRoute.Kind == AlphaCommandKind.SystemControl, "Nudge up should be system-control kind.");
    Require(nudgeUpRoute.Target == "system-mouse-move-up", $"Expected system-mouse-move-up target, got '{nudgeUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move faster", out var moveFasterRoute), "Move faster command should route.");
    Require(moveFasterRoute.Target == "system-mouse-move-faster", $"Expected system-mouse-move-faster target, got '{moveFasterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("slower", out var moveSlowerRoute), "Slower command should route.");
    Require(moveSlowerRoute.Target == "system-mouse-move-slower", $"Expected system-mouse-move-slower target, got '{moveSlowerRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("stop moving", out var stopMovingRoute), "Stop moving command should route.");
    Require(stopMovingRoute.Target == "system-mouse-stop-moving", $"Expected system-mouse-stop-moving target, got '{stopMovingRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse down", out var mouseMoveDownRoute), "Mouse move down command should route.");
    Require(mouseMoveDownRoute.Kind == AlphaCommandKind.SystemControl, "Mouse move down should be system-control kind.");
    Require(mouseMoveDownRoute.Target == "system-mouse-start-moving:down", $"Expected system-mouse-start-moving:down target, got '{mouseMoveDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("mouse down", out var mouseDownNudgeRoute), "Mouse down nudge command should still route.");
    Require(mouseDownNudgeRoute.Target == "system-mouse-move-down", $"Expected mouse down to remain a pointer nudge, got '{mouseDownNudgeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse left", out var mouseMoveLeftRoute), "Mouse move left command should route.");
    Require(mouseMoveLeftRoute.Kind == AlphaCommandKind.SystemControl, "Mouse move left should be system-control kind.");
    Require(mouseMoveLeftRoute.Target == "system-mouse-start-moving:left", $"Expected system-mouse-start-moving:left target, got '{mouseMoveLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("move mouse right", out var mouseMoveRightRoute), "Mouse move right command should route.");
    Require(mouseMoveRightRoute.Kind == AlphaCommandKind.SystemControl, "Mouse move right should be system-control kind.");
    Require(mouseMoveRightRoute.Target == "system-mouse-start-moving:right", $"Expected system-mouse-start-moving:right target, got '{mouseMoveRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag mouse up", out var mouseDragUpRoute), "Mouse drag up command should route.");
    Require(mouseDragUpRoute.Kind == AlphaCommandKind.SystemControl, "Mouse drag up should be system-control kind.");
    Require(mouseDragUpRoute.Target == "system-mouse-drag-direction:up", $"Expected system-mouse-drag-direction:up target, got '{mouseDragUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag mouse down", out var mouseDragDownRoute), "Mouse drag down command should route.");
    Require(mouseDragDownRoute.Kind == AlphaCommandKind.SystemControl, "Mouse drag down should be system-control kind.");
    Require(mouseDragDownRoute.Target == "system-mouse-drag-direction:down", $"Expected system-mouse-drag-direction:down target, got '{mouseDragDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag mouse left", out var mouseDragLeftRoute), "Mouse drag left command should route.");
    Require(mouseDragLeftRoute.Kind == AlphaCommandKind.SystemControl, "Mouse drag left should be system-control kind.");
    Require(mouseDragLeftRoute.Target == "system-mouse-drag-direction:left", $"Expected system-mouse-drag-direction:left target, got '{mouseDragLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag mouse right", out var mouseDragRightRoute), "Mouse drag right command should route.");
    Require(mouseDragRightRoute.Kind == AlphaCommandKind.SystemControl, "Mouse drag right should be system-control kind.");
    Require(mouseDragRightRoute.Target == "system-mouse-drag-direction:right", $"Expected system-mouse-drag-direction:right target, got '{mouseDragRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("drag mouse bottom right", out var mouseDragBottomRightRoute), "Diagonal mouse drag should route.");
    Require(mouseDragBottomRightRoute.Target == "system-mouse-drag-direction:bottom-right", $"Expected system-mouse-drag-direction:bottom-right target, got '{mouseDragBottomRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system copy", out var copyRoute), "Copy command should route.");
    Require(copyRoute.Kind == AlphaCommandKind.SystemControl, "Copy command should be system-control kind.");
    Require(copyRoute.Target == "system-copy", $"Expected system-copy target, got '{copyRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("copy", out var naturalCopyRoute), "Natural copy command should route.");
    Require(naturalCopyRoute.Kind == AlphaCommandKind.SystemControl, "Natural copy command should be system-control kind.");
    Require(naturalCopyRoute.Target == "system-copy", $"Expected system-copy target, got '{naturalCopyRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system paste", out var pasteRoute), "Paste command should route.");
    Require(pasteRoute.Kind == AlphaCommandKind.SystemControl, "Paste command should be system-control kind.");
    Require(pasteRoute.Target == "system-paste", $"Expected system-paste target, got '{pasteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("paste", out var naturalPasteRoute), "Natural paste command should route.");
    Require(naturalPasteRoute.Kind == AlphaCommandKind.SystemControl, "Natural paste command should be system-control kind.");
    Require(naturalPasteRoute.Target == "system-paste", $"Expected system-paste target, got '{naturalPasteRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system cut", out var cutRoute), "Cut command should route.");
    Require(cutRoute.Kind == AlphaCommandKind.SystemControl, "Cut command should be system-control kind.");
    Require(cutRoute.Target == "system-cut", $"Expected system-cut target, got '{cutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("cut", out var naturalCutRoute), "Natural cut command should route.");
    Require(naturalCutRoute.Kind == AlphaCommandKind.SystemControl, "Natural cut command should be system-control kind.");
    Require(naturalCutRoute.Target == "system-cut", $"Expected system-cut target, got '{naturalCutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select all", out var selectAllRoute), "Select all command should route.");
    Require(selectAllRoute.Kind == AlphaCommandKind.SystemControl, "Select all command should be system-control kind.");
    Require(selectAllRoute.Target == "system-select-all", $"Expected system-select-all target, got '{selectAllRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select all", out var naturalSelectAllRoute), "Natural select all command should route.");
    Require(naturalSelectAllRoute.Kind == AlphaCommandKind.SystemControl, "Natural select all command should be system-control kind.");
    Require(naturalSelectAllRoute.Target == "system-select-all", $"Expected system-select-all target, got '{naturalSelectAllRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system save", out var saveRoute), "Save command should route.");
    Require(saveRoute.Kind == AlphaCommandKind.SystemControl, "Save command should be system-control kind.");
    Require(saveRoute.Target == "system-save", $"Expected system-save target, got '{saveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("save", out var naturalSaveRoute), "Natural save command should route.");
    Require(naturalSaveRoute.Kind == AlphaCommandKind.SystemControl, "Natural save command should be system-control kind.");
    Require(naturalSaveRoute.Target == "system-save", $"Expected system-save target, got '{naturalSaveRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system undo", out var undoRoute), "Undo command should route.");
    Require(undoRoute.Kind == AlphaCommandKind.SystemControl, "Undo command should be system-control kind.");
    Require(undoRoute.Target == "system-undo", $"Expected system-undo target, got '{undoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("undo", out var naturalUndoRoute), "Natural undo command should route.");
    Require(naturalUndoRoute.Kind == AlphaCommandKind.SystemControl, "Natural undo command should be system-control kind.");
    Require(naturalUndoRoute.Target == "system-undo", $"Expected system-undo target, got '{naturalUndoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system redo", out var redoRoute), "Redo command should route.");
    Require(redoRoute.Kind == AlphaCommandKind.SystemControl, "Redo command should be system-control kind.");
    Require(redoRoute.Target == "system-redo", $"Expected system-redo target, got '{redoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("redo", out var naturalRedoRoute), "Natural redo command should route.");
    Require(naturalRedoRoute.Kind == AlphaCommandKind.SystemControl, "Natural redo command should be system-control kind.");
    Require(naturalRedoRoute.Target == "system-redo", $"Expected system-redo target, got '{naturalRedoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("bold", out var boldRoute), "Bold command should route.");
    Require(boldRoute.Kind == AlphaCommandKind.SystemControl, "Bold command should be system-control kind.");
    Require(boldRoute.Target == "system-bold", $"Expected system-bold target, got '{boldRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("bold that", out var boldThatRoute), "Bold-that command should route.");
    Require(boldThatRoute.Kind == AlphaCommandKind.SystemControl, "Bold-that command should be system-control kind.");
    Require(boldThatRoute.Target == "system-bold", $"Expected system-bold target, got '{boldThatRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("italic", out var italicRoute), "Italic command should route.");
    Require(italicRoute.Kind == AlphaCommandKind.SystemControl, "Italic command should be system-control kind.");
    Require(italicRoute.Target == "system-italic", $"Expected system-italic target, got '{italicRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("italicize that", out var italicizeThatRoute), "Italicize-that command should route.");
    Require(italicizeThatRoute.Kind == AlphaCommandKind.SystemControl, "Italicize-that command should be system-control kind.");
    Require(italicizeThatRoute.Target == "system-italic", $"Expected system-italic target, got '{italicizeThatRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("underline", out var underlineRoute), "Underline command should route.");
    Require(underlineRoute.Kind == AlphaCommandKind.SystemControl, "Underline command should be system-control kind.");
    Require(underlineRoute.Target == "system-underline", $"Expected system-underline target, got '{underlineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("underline that", out var underlineThatRoute), "Underline-that command should route.");
    Require(underlineThatRoute.Kind == AlphaCommandKind.SystemControl, "Underline-that command should be system-control kind.");
    Require(underlineThatRoute.Target == "system-underline", $"Expected system-underline target, got '{underlineThatRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system find", out var findRoute), "Find command should route.");
    Require(findRoute.Kind == AlphaCommandKind.SystemControl, "Find command should be system-control kind.");
    Require(findRoute.Target == "system-find", $"Expected system-find target, got '{findRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system new window", out var newWindowRoute), "New window command should route.");
    Require(newWindowRoute.Kind == AlphaCommandKind.SystemControl, "New window command should be system-control kind.");
    Require(newWindowRoute.Target == "system-new-window", $"Expected system-new-window target, got '{newWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("new document", out var newDocumentRoute), "New document command should route.");
    Require(newDocumentRoute.Kind == AlphaCommandKind.SystemControl, "New document command should be system-control kind.");
    Require(newDocumentRoute.Target == "system-new-document", $"Expected system-new-document target, got '{newDocumentRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open file", out var openFileRoute), "Open file command should route.");
    Require(openFileRoute.Kind == AlphaCommandKind.SystemControl, "Open file command should be system-control kind.");
    Require(openFileRoute.Target == "system-open-file", $"Expected system-open-file target, got '{openFileRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("print", out var printRoute), "Print command should route.");
    Require(printRoute.Kind == AlphaCommandKind.SystemControl, "Print command should be system-control kind.");
    Require(printRoute.Target == "system-print", $"Expected system-print target, got '{printRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("zoom in", out var systemZoomInRoute), "System zoom in command should route.");
    Require(systemZoomInRoute.Kind == AlphaCommandKind.SystemControl, "System zoom in should be system-control kind.");
    Require(systemZoomInRoute.Target == "system-zoom-in", $"Expected system-zoom-in target, got '{systemZoomInRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("zoom out", out var systemZoomOutRoute), "System zoom out command should route.");
    Require(systemZoomOutRoute.Kind == AlphaCommandKind.SystemControl, "System zoom out should be system-control kind.");
    Require(systemZoomOutRoute.Target == "system-zoom-out", $"Expected system-zoom-out target, got '{systemZoomOutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("reset zoom", out var systemZoomResetRoute), "System zoom reset command should route.");
    Require(systemZoomResetRoute.Kind == AlphaCommandKind.SystemControl, "System zoom reset should be system-control kind.");
    Require(systemZoomResetRoute.Target == "system-zoom-reset", $"Expected system-zoom-reset target, got '{systemZoomResetRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system close window", out var closeWindowRoute), "Close window command should route.");
    Require(closeWindowRoute.Kind == AlphaCommandKind.SystemControl, "Close window command should be system-control kind.");
    Require(closeWindowRoute.Target == "system-close-window", $"Expected system-close-window target, got '{closeWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("close active app", out var closeActiveAppRoute), "Close active app alias should route.");
    Require(closeActiveAppRoute.Kind == AlphaCommandKind.SystemControl, "Close active app should be system-control kind.");
    Require(closeActiveAppRoute.Target == "system-close-window", $"Expected system-close-window target, got '{closeActiveAppRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("previous character", out var movePreviousCharacterRoute), "Move previous character command should route.");
    Require(movePreviousCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Move previous character command should be system-control kind.");
    Require(movePreviousCharacterRoute.Target == "system-move-previous-character", $"Expected system-move-previous-character target, got '{movePreviousCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("next character", out var moveNextCharacterRoute), "Move next character command should route.");
    Require(moveNextCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Move next character command should be system-control kind.");
    Require(moveNextCharacterRoute.Target == "system-move-next-character", $"Expected system-move-next-character target, got '{moveNextCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select previous character", out var selectPreviousCharacterRoute), "Select previous character command should route.");
    Require(selectPreviousCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Select previous character command should be system-control kind.");
    Require(selectPreviousCharacterRoute.Target == "system-select-previous-character", $"Expected system-select-previous-character target, got '{selectPreviousCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select next character", out var selectNextCharacterRoute), "Select next character command should route.");
    Require(selectNextCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Select next character command should be system-control kind.");
    Require(selectNextCharacterRoute.Target == "system-select-next-character", $"Expected system-select-next-character target, got '{selectNextCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete previous character", out var deletePreviousCharacterRoute), "Delete previous character command should route.");
    Require(deletePreviousCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Delete previous character command should be system-control kind.");
    Require(deletePreviousCharacterRoute.Target == "system-delete-previous-character", $"Expected system-delete-previous-character target, got '{deletePreviousCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete next character", out var deleteNextCharacterRoute), "Delete next character command should route.");
    Require(deleteNextCharacterRoute.Kind == AlphaCommandKind.SystemControl, "Delete next character command should be system-control kind.");
    Require(deleteNextCharacterRoute.Target == "system-delete-next-character", $"Expected system-delete-next-character target, got '{deleteNextCharacterRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to line start", out var moveLineStartRoute), "Move line start command should route.");
    Require(moveLineStartRoute.Kind == AlphaCommandKind.SystemControl, "Move line start command should be system-control kind.");
    Require(moveLineStartRoute.Target == "system-move-line-start", $"Expected system-move-line-start target, got '{moveLineStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to line end", out var moveLineEndRoute), "Move line end command should route.");
    Require(moveLineEndRoute.Kind == AlphaCommandKind.SystemControl, "Move line end command should be system-control kind.");
    Require(moveLineEndRoute.Target == "system-move-line-end", $"Expected system-move-line-end target, got '{moveLineEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to previous line", out var movePreviousLineRoute), "Move previous line command should route.");
    Require(movePreviousLineRoute.Kind == AlphaCommandKind.SystemControl, "Move previous line command should be system-control kind.");
    Require(movePreviousLineRoute.Target == "system-move-previous-line", $"Expected system-move-previous-line target, got '{movePreviousLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to next line", out var moveNextLineRoute), "Move next line command should route.");
    Require(moveNextLineRoute.Kind == AlphaCommandKind.SystemControl, "Move next line command should be system-control kind.");
    Require(moveNextLineRoute.Target == "system-move-next-line", $"Expected system-move-next-line target, got '{moveNextLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select to line start", out var selectToLineStartRoute), "Select to line start command should route.");
    Require(selectToLineStartRoute.Kind == AlphaCommandKind.SystemControl, "Select to line start command should be system-control kind.");
    Require(selectToLineStartRoute.Target == "system-select-to-line-start", $"Expected system-select-to-line-start target, got '{selectToLineStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select to line end", out var selectToLineEndRoute), "Select to line end command should route.");
    Require(selectToLineEndRoute.Kind == AlphaCommandKind.SystemControl, "Select to line end command should be system-control kind.");
    Require(selectToLineEndRoute.Target == "system-select-to-line-end", $"Expected system-select-to-line-end target, got '{selectToLineEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select previous line", out var selectPreviousLineRoute), "Select previous line command should route.");
    Require(selectPreviousLineRoute.Kind == AlphaCommandKind.SystemControl, "Select previous line command should be system-control kind.");
    Require(selectPreviousLineRoute.Target == "system-select-previous-line", $"Expected system-select-previous-line target, got '{selectPreviousLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select next line", out var selectNextLineRoute), "Select next line command should route.");
    Require(selectNextLineRoute.Kind == AlphaCommandKind.SystemControl, "Select next line command should be system-control kind.");
    Require(selectNextLineRoute.Target == "system-select-next-line", $"Expected system-select-next-line target, got '{selectNextLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete to line start", out var deleteToLineStartRoute), "Delete to line start command should route.");
    Require(deleteToLineStartRoute.Kind == AlphaCommandKind.SystemControl, "Delete to line start command should be system-control kind.");
    Require(deleteToLineStartRoute.Target == "system-delete-to-line-start", $"Expected system-delete-to-line-start target, got '{deleteToLineStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete to line end", out var deleteToLineEndRoute), "Delete to line end command should route.");
    Require(deleteToLineEndRoute.Kind == AlphaCommandKind.SystemControl, "Delete to line end command should be system-control kind.");
    Require(deleteToLineEndRoute.Target == "system-delete-to-line-end", $"Expected system-delete-to-line-end target, got '{deleteToLineEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete previous line", out var deletePreviousLineRoute), "Delete previous line command should route.");
    Require(deletePreviousLineRoute.Kind == AlphaCommandKind.SystemControl, "Delete previous line command should be system-control kind.");
    Require(deletePreviousLineRoute.Target == "system-delete-previous-line", $"Expected system-delete-previous-line target, got '{deletePreviousLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete next line", out var deleteNextLineRoute), "Delete next line command should route.");
    Require(deleteNextLineRoute.Kind == AlphaCommandKind.SystemControl, "Delete next line command should be system-control kind.");
    Require(deleteNextLineRoute.Target == "system-delete-next-line", $"Expected system-delete-next-line target, got '{deleteNextLineRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system move previous word", out var movePreviousWordRoute), "Move previous word command should route.");
    Require(movePreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Move previous word command should be system-control kind.");
    Require(movePreviousWordRoute.Target == "system-move-previous-word", $"Expected system-move-previous-word target, got '{movePreviousWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to next word", out var goNextWordRoute), "Go to next word command should route.");
    Require(goNextWordRoute.Kind == AlphaCommandKind.SystemControl, "Go to next word command should be system-control kind.");
    Require(goNextWordRoute.Target == "system-move-next-word", $"Expected system-move-next-word target, got '{goNextWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system move next word", out var moveNextWordRoute), "Move next word command should route.");
    Require(moveNextWordRoute.Kind == AlphaCommandKind.SystemControl, "Move next word command should be system-control kind.");
    Require(moveNextWordRoute.Target == "system-move-next-word", $"Expected system-move-next-word target, got '{moveNextWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select previous word", out var selectPreviousWordRoute), "Select previous word command should route.");
    Require(selectPreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Select previous word command should be system-control kind.");
    Require(selectPreviousWordRoute.Target == "system-select-previous-word", $"Expected system-select-previous-word target, got '{selectPreviousWordRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select previous word", out var plainSelectPreviousWordRoute), "Plain select previous word command should route.");
    Require(plainSelectPreviousWordRoute.Kind == AlphaCommandKind.SystemControl, "Plain select previous word command should be system-control kind.");
    Require(plainSelectPreviousWordRoute.Target == "system-select-previous-word", $"Expected system-select-previous-word target, got '{plainSelectPreviousWordRoute.Target}'.");
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
    Require(AlphaCommandRouter.TryRoute("go to next paragraph", out var goNextParagraphRoute), "Go to next paragraph command should route.");
    Require(goNextParagraphRoute.Kind == AlphaCommandKind.SystemControl, "Go to next paragraph command should be system-control kind.");
    Require(goNextParagraphRoute.Target == "system-move-next-paragraph", $"Expected system-move-next-paragraph target, got '{goNextParagraphRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system select next sentence", out var selectNextSentenceRoute), "Select next sentence command should route.");
    Require(selectNextSentenceRoute.Kind == AlphaCommandKind.SystemControl, "Select next sentence command should be system-control kind.");
    Require(selectNextSentenceRoute.Target == "system-select-next-sentence", $"Expected system-select-next-sentence target, got '{selectNextSentenceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select next sentence", out var plainSelectNextSentenceRoute), "Plain select next sentence command should route.");
    Require(plainSelectNextSentenceRoute.Kind == AlphaCommandKind.SystemControl, "Plain select next sentence command should be system-control kind.");
    Require(plainSelectNextSentenceRoute.Target == "system-select-next-sentence", $"Expected system-select-next-sentence target, got '{plainSelectNextSentenceRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to paragraph start", out var moveParagraphStartRoute), "Move paragraph start command should route.");
    Require(moveParagraphStartRoute.Kind == AlphaCommandKind.SystemControl, "Move paragraph start command should be system-control kind.");
    Require(moveParagraphStartRoute.Target == "system-move-paragraph-start", $"Expected system-move-paragraph-start target, got '{moveParagraphStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to paragraph end", out var moveParagraphEndRoute), "Move paragraph end command should route.");
    Require(moveParagraphEndRoute.Kind == AlphaCommandKind.SystemControl, "Move paragraph end command should be system-control kind.");
    Require(moveParagraphEndRoute.Target == "system-move-paragraph-end", $"Expected system-move-paragraph-end target, got '{moveParagraphEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select to paragraph start", out var selectToParagraphStartRoute), "Select to paragraph start command should route.");
    Require(selectToParagraphStartRoute.Kind == AlphaCommandKind.SystemControl, "Select to paragraph start command should be system-control kind.");
    Require(selectToParagraphStartRoute.Target == "system-select-to-paragraph-start", $"Expected system-select-to-paragraph-start target, got '{selectToParagraphStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select to paragraph end", out var selectToParagraphEndRoute), "Select to paragraph end command should route.");
    Require(selectToParagraphEndRoute.Kind == AlphaCommandKind.SystemControl, "Select to paragraph end command should be system-control kind.");
    Require(selectToParagraphEndRoute.Target == "system-select-to-paragraph-end", $"Expected system-select-to-paragraph-end target, got '{selectToParagraphEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete to paragraph start", out var deleteToParagraphStartRoute), "Delete to paragraph start command should route.");
    Require(deleteToParagraphStartRoute.Kind == AlphaCommandKind.SystemControl, "Delete to paragraph start command should be system-control kind.");
    Require(deleteToParagraphStartRoute.Target == "system-delete-to-paragraph-start", $"Expected system-delete-to-paragraph-start target, got '{deleteToParagraphStartRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("delete to paragraph end", out var deleteToParagraphEndRoute), "Delete to paragraph end command should route.");
    Require(deleteToParagraphEndRoute.Kind == AlphaCommandKind.SystemControl, "Delete to paragraph end command should be system-control kind.");
    Require(deleteToParagraphEndRoute.Target == "system-delete-to-paragraph-end", $"Expected system-delete-to-paragraph-end target, got '{deleteToParagraphEndRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("system delete previous paragraph", out var deletePreviousParagraphRoute), "Delete previous paragraph command should route.");
    Require(deletePreviousParagraphRoute.Kind == AlphaCommandKind.SystemControl, "Delete previous paragraph command should be system-control kind.");
    Require(deletePreviousParagraphRoute.Target == "system-delete-previous-paragraph", $"Expected system-delete-previous-paragraph target, got '{deletePreviousParagraphRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("select previous paragraph", out var plainSelectPreviousParagraphRoute), "Plain select previous paragraph command should route.");
    Require(plainSelectPreviousParagraphRoute.Kind == AlphaCommandKind.SystemControl, "Plain select previous paragraph command should be system-control kind.");
    Require(plainSelectPreviousParagraphRoute.Target == "system-select-previous-paragraph", $"Expected system-select-previous-paragraph target, got '{plainSelectPreviousParagraphRoute.Target}'.");

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
    Require(AlphaCommandRouter.TryRoute("resume dictation", out var resumeDictationRoute), "Resume dictation command should route.");
    Require(resumeDictationRoute.Kind == AlphaCommandKind.Dictation, "Resume dictation should be dictation kind.");
    Require(AlphaCommandRouter.TryRoute("start typing", out var startTypingRoute), "Start typing command should route to dictation.");
    Require(startTypingRoute.Kind == AlphaCommandKind.Dictation, "Start typing should be dictation kind.");
    Require(AlphaCommandRouter.TryRoute("resume typing", out var resumeTypingRoute), "Resume typing command should route to dictation.");
    Require(resumeTypingRoute.Kind == AlphaCommandKind.Dictation, "Resume typing should be dictation kind.");
    Require(AlphaCommandRouter.TryRoute("type hello world", out var typeTextRoute), "Direct type-text command should route to dictation.");
    Require(typeTextRoute.Kind == AlphaCommandKind.Dictation, "Direct type-text command should be dictation kind.");
    Require(typeTextRoute.Target == AlphaCommandRouter.DictationInsertTextActionPrefix + "hello world", $"Expected direct dictation insert target, got '{typeTextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("insert text hello comma world", out var insertTextRoute), "Direct insert-text command should route to dictation.");
    Require(insertTextRoute.Target == AlphaCommandRouter.DictationInsertTextActionPrefix + "hello, world", $"Expected punctuation-normalized direct dictation target, got '{insertTextRoute.Target}'.");

    Require(!AlphaCommandRouter.TryRoute("open notepad", out _), "Plain app launch should remain a Start menu launch, not a special command route.");
    Require(AlphaCommandRouter.TryRoute("browser back", out var backRoute), "Browser back command should route.");
    Require(backRoute.Kind == AlphaCommandKind.Browser, "Browser back should route as browser kind.");
    Require(backRoute.Target == "browser-back", $"Expected browser-back target, got '{backRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser refresh", out var refreshRoute), "Browser refresh command should route.");
    Require(refreshRoute.Target == "browser-refresh", $"Expected browser-refresh target, got '{refreshRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser new tab", out var newTabRoute), "Browser new tab command should route.");
    Require(newTabRoute.Target == "browser-new-tab", $"Expected browser-new-tab target, got '{newTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser new window", out var browserNewWindowRoute), "Browser new window command should route.");
    Require(browserNewWindowRoute.Target == "browser-new-window", $"Expected browser-new-window target, got '{browserNewWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser private window", out var privateWindowRoute), "Browser private window command should route.");
    Require(privateWindowRoute.Target == "browser-private-window", $"Expected browser-private-window target, got '{privateWindowRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser incognito", out var incognitoRoute), "Browser incognito command should route.");
    Require(incognitoRoute.Target == "browser-private-window", $"Expected browser-private-window target, got '{incognitoRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser bookmark page", out var bookmarkPageRoute), "Browser bookmark page command should route.");
    Require(bookmarkPageRoute.Target == "browser-bookmark-page", $"Expected browser-bookmark-page target, got '{bookmarkPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("add to favorites", out var addToFavoritesRoute), "Add to favorites command should route.");
    Require(addToFavoritesRoute.Target == "browser-bookmark-page", $"Expected browser-bookmark-page target, got '{addToFavoritesRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser open bookmarks", out var openBookmarksRoute), "Browser open bookmarks command should route.");
    Require(openBookmarksRoute.Target == "browser-open-bookmarks", $"Expected browser-open-bookmarks target, got '{openBookmarksRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open favorites", out var openFavoritesRoute), "Open favorites command should route.");
    Require(openFavoritesRoute.Target == "browser-open-bookmarks", $"Expected browser-open-bookmarks target, got '{openFavoritesRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser bookmarks", out var bookmarksRoute), "Browser bookmarks command should route.");
    Require(bookmarksRoute.Target == "browser-open-bookmarks", $"Expected browser-open-bookmarks target, got '{bookmarksRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser save page", out var savePageRoute), "Browser save page command should route.");
    Require(savePageRoute.Target == "browser-save-page", $"Expected browser-save-page target, got '{savePageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser print page", out var printPageRoute), "Browser print page command should route.");
    Require(printPageRoute.Target == "browser-print-page", $"Expected browser-print-page target, got '{printPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser next tab", out var nextTabRoute), "Browser next tab command should route.");
    Require(nextTabRoute.Target == "browser-next-tab", $"Expected browser-next-tab target, got '{nextTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser previous tab", out var previousTabRoute), "Browser previous tab command should route.");
    Require(previousTabRoute.Target == "browser-previous-tab", $"Expected browser-previous-tab target, got '{previousTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser forward", out var forwardRoute), "Browser forward command should route.");
    Require(forwardRoute.Target == "browser-forward", $"Expected browser-forward target, got '{forwardRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser close tab", out var closeTabRoute), "Browser close tab command should route.");
    Require(closeTabRoute.Target == "browser-close-tab", $"Expected browser-close-tab target, got '{closeTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("reopen closed tab", out var reopenClosedTabRoute), "Browser reopen closed tab command should route.");
    Require(reopenClosedTabRoute.Target == "browser-reopen-closed-tab", $"Expected browser-reopen-closed-tab target, got '{reopenClosedTabRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser focus address bar", out var focusAddressBarRoute), "Browser focus address bar command should route.");
    Require(focusAddressBarRoute.Target == "browser-focus-address-bar", $"Expected browser-focus-address-bar target, got '{focusAddressBarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("url bar", out var urlBarRoute), "URL bar command should route.");
    Require(urlBarRoute.Target == "browser-focus-address-bar", $"Expected browser-focus-address-bar target, got '{urlBarRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser address bar search for example dot com", out var addressBarSearchRoute), "Browser address-bar search alias should route.");
    Require(addressBarSearchRoute.Target == "browser-address-text:example.com", $"Expected browser-address-text:example.com target, got '{addressBarSearchRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser home", out var browserHomeRoute), "Browser home command should route.");
    Require(browserHomeRoute.Target == "browser-home", $"Expected browser-home target, got '{browserHomeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser full screen", out var browserFullscreenRoute), "Browser full screen command should route.");
    Require(browserFullscreenRoute.Target == "browser-fullscreen", $"Expected browser-fullscreen target, got '{browserFullscreenRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser downloads", out var browserDownloadsRoute), "Browser downloads command should route.");
    Require(browserDownloadsRoute.Target == "browser-open-downloads", $"Expected browser-open-downloads target, got '{browserDownloadsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser history", out var browserHistoryRoute), "Browser history command should route.");
    Require(browserHistoryRoute.Target == "browser-open-history", $"Expected browser-open-history target, got '{browserHistoryRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find in page", out var findInPageRoute), "Browser find in page command should route.");
    Require(findInPageRoute.Target == "browser-find", $"Expected browser-find target, got '{findInPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open find box", out var openFindBoxRoute), "Open find box command should route.");
    Require(openFindBoxRoute.Target == "browser-find", $"Expected browser-find target, got '{openFindBoxRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser find", out var browserFindAliasRoute), "Browser find alias should route.");
    Require(browserFindAliasRoute.Kind == AlphaCommandKind.Browser, "Browser find alias should be browser kind.");
    Require(browserFindAliasRoute.Target == "browser-find", $"Expected browser-find target, got '{browserFindAliasRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser search in page", out var browserSearchInPageRoute), "Browser search in page alias should route.");
    Require(browserSearchInPageRoute.Kind == AlphaCommandKind.Browser, "Browser search in page alias should be browser kind.");
    Require(browserSearchInPageRoute.Target == "browser-find", $"Expected browser-find target, got '{browserSearchInPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find next", out var findNextRoute), "Browser find next command should route.");
    Require(findNextRoute.Target == "browser-find-next", $"Expected browser-find-next target, got '{findNextRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("find previous", out var findPreviousRoute), "Browser find previous command should route.");
    Require(findPreviousRoute.Target == "browser-find-previous", $"Expected browser-find-previous target, got '{findPreviousRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser scroll down", out var scrollDownRoute), "Browser scroll down command should route.");
    Require(scrollDownRoute.Target == "browser-scroll-down", $"Expected browser-scroll-down target, got '{scrollDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("start scrolling down", out var startScrollDownRoute), "Start scrolling down command should route.");
    Require(startScrollDownRoute.Target == "browser-start-scroll-down", $"Expected browser-start-scroll-down target, got '{startScrollDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser start scrolling left", out var startScrollLeftRoute), "Browser start scrolling left command should route.");
    Require(startScrollLeftRoute.Target == "browser-start-scroll-left", $"Expected browser-start-scroll-left target, got '{startScrollLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("stop scrolling", out var stopScrollingRoute), "Stop scrolling command should route.");
    Require(stopScrollingRoute.Target == "browser-stop-scroll", $"Expected browser-stop-scroll target, got '{stopScrollingRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("page down in browser", out var browserPageDownRoute), "Browser page-down alias should route.");
    Require(browserPageDownRoute.Kind == AlphaCommandKind.Browser, "Browser page-down alias should be browser kind.");
    Require(browserPageDownRoute.Target == "browser-scroll-down", $"Expected browser-scroll-down target, got '{browserPageDownRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("page up in browser", out var browserPageUpRoute), "Browser page-up alias should route.");
    Require(browserPageUpRoute.Kind == AlphaCommandKind.Browser, "Browser page-up alias should be browser kind.");
    Require(browserPageUpRoute.Target == "browser-scroll-up", $"Expected browser-scroll-up target, got '{browserPageUpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser scroll left", out var browserScrollLeftRoute), "Browser scroll left command should route.");
    Require(browserScrollLeftRoute.Target == "browser-scroll-left", $"Expected browser-scroll-left target, got '{browserScrollLeftRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("browser scroll right", out var browserScrollRightRoute), "Browser scroll right command should route.");
    Require(browserScrollRightRoute.Target == "browser-scroll-right", $"Expected browser-scroll-right target, got '{browserScrollRightRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("scroll to top", out var scrollTopRoute), "Browser scroll top command should route.");
    Require(scrollTopRoute.Target == "browser-scroll-top", $"Expected browser-scroll-top target, got '{scrollTopRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to top of page", out var goToTopOfPageRoute), "Go to top of page alias should route.");
    Require(goToTopOfPageRoute.Target == "browser-scroll-top", $"Expected browser-scroll-top target, got '{goToTopOfPageRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("go to bottom of page", out var goToBottomOfPageRoute), "Go to bottom of page alias should route.");
    Require(goToBottomOfPageRoute.Target == "browser-scroll-bottom", $"Expected browser-scroll-bottom target, got '{goToBottomOfPageRoute.Target}'.");
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

    Require(registry.TryExecute(execution, out var blockedResult), "Pack execution should match before policy blocks.");
    Require(!blockedResult.Succeeded, "Pack execution should fail closed when no identity context is supplied.");
    Require(blockedResult.AuditEvent?.Contains("fresh_identity_required", StringComparison.OrdinalIgnoreCase) == true, $"Expected fresh-identity audit event, got '{blockedResult.AuditEvent}'.");
    Require(blockedResult.PolicyDecision == CallsignPolicyDecision.RequireFreshIdentity, $"Expected fresh-identity policy decision, got {blockedResult.PolicyDecision}.");
    Require(blockedResult.PolicyApprovalRequirement == CallsignCommandApprovalRequirement.RequireFreshIdentity, $"Expected fresh-identity approval requirement, got {blockedResult.PolicyApprovalRequirement}.");
    Require(blockedResult.PolicyRiskTier == CallsignCommandRiskTier.Observe, $"Expected observe risk tier, got {blockedResult.PolicyRiskTier}.");

    Require(registry.TryExecute(execution, out var result, identityVerified: true, freshIdentityVerified: true), "Pack execution should succeed after identity policy proof.");
    Require(result.Succeeded, $"Pack execution should succeed, got message '{result.Message}'.");
    Require(result.Message.Contains("sample-pack", StringComparison.OrdinalIgnoreCase), $"Execution message should mention the pack, got '{result.Message}'.");
    Require(result.PolicyDecision == null, $"Successful pack execution should not report a policy block, got {result.PolicyDecision}.");
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

static void ExtensionPackImportDisablesCommunityDllByDefault()
{
    var registry = PackTestSupport.CreateRegistry();
    var sourceAssembly = typeof(SampleCommandPack).Assembly.Location;

    var import = registry.ImportPack(sourceAssembly);
    Require(import.Succeeded, $"Pack import should succeed: {import.Message}");
    Require(import.LoadStatus == CallsignPackLoadStatus.Disabled, $"Imported community pack should start disabled, got {import.LoadStatus}.");
    Require(!registry.TryResolve("sample pack echo hello", out _), "Imported community pack should not route until explicitly enabled.");
    Require(registry.GetPacks().Single(pack => pack.PackId == "sample-pack").CommandCount == 1, "Disabled imported pack should still expose its command metadata.");
    Require(registry.GetCommands().Any(command => command.PackId == "sample-pack" && command.CommandDisplayName == "Echo sample text"), "Disabled imported pack should remain visible in discovery.");
    var disabledDiscovery = CommandDiscoveryService.GetCommands(registry).Single(command => command.Phrase == "sample pack echo");
    Require(disabledDiscovery.LoadStatus == CallsignPackLoadStatus.Disabled, $"Disabled pack discovery should expose Disabled status, got {disabledDiscovery.LoadStatus}.");
    Require(disabledDiscovery.Availability.Contains("Disabled", StringComparison.OrdinalIgnoreCase), $"Disabled pack discovery should explain availability, got '{disabledDiscovery.Availability}'.");
    Require(disabledDiscovery.Availability.Contains("enabled from Packs", StringComparison.OrdinalIgnoreCase), $"Disabled pack discovery should tell users where to enable it, got '{disabledDiscovery.Availability}'.");
    var disabledPack = registry.GetPacks().Single(pack => pack.PackId == "sample-pack");
    var disabledReadiness = MainForm.FormatPackEnablementReadiness(disabledPack);
    Require(disabledReadiness.Contains("disabled for review", StringComparison.OrdinalIgnoreCase), $"Disabled pack readiness should explain review state, got '{disabledReadiness}'.");
    Require(disabledReadiness.Contains("Review tier, signature, risk, privacy, approval, and visibility", StringComparison.OrdinalIgnoreCase), $"Disabled pack readiness should name review fields, got '{disabledReadiness}'.");

    Require(!string.IsNullOrWhiteSpace(import.PackId), "Import result should include the placeholder pack id.");
    Require(registry.EnablePack(import.PackId!), "Imported pack should be enableable from the placeholder entry.");
    Require(registry.TryResolve("sample pack echo hello", out var resolution), "Enabled imported pack should route.");
    Require(resolution.PackId == "sample-pack", $"Expected loaded descriptor id sample-pack, got '{resolution.PackId}'.");
}

static void ExtensionPackImportMarksImportedPacksAndBuildsSplashManifest()
{
    var registry = PackTestSupport.CreateRegistry();
    var sourceAssembly = typeof(SampleCommandPack).Assembly.Location;

    var import = registry.ImportPack(sourceAssembly);
    Require(import.Succeeded, $"Pack import should succeed: {import.Message}");

    var importedPack = registry.GetPacks().Single(pack => pack.PackId == "sample-pack");
    Require(importedPack.WasImported, "Imported community pack should be marked as imported for the Packs surface.");

    var securitySummary = MainForm.FormatPackSecuritySummary(importedPack);
    Require(securitySummary.Contains("imported", StringComparison.OrdinalIgnoreCase), $"Expected imported state in security summary, got '{securitySummary}'.");

    var manifest = MainForm.BuildPackImportManifest(new[] { importedPack }, registry);
    Require(manifest.SplashSummary is not null && manifest.SplashSummary.Contains("Imported", StringComparison.OrdinalIgnoreCase), "Pack import manifest should include a visible summary.");
    Require(manifest.ExtensionPackChanges is { Count: 1 }, $"Expected one extension pack change, got {manifest.ExtensionPackChanges?.Count ?? 0}.");
    Require(manifest.ExtensionPackChanges![0].DisplayName == "Sample Pack", $"Expected pack change to describe the sample pack, got '{manifest.ExtensionPackChanges[0].DisplayName}'.");
    Require(manifest.ExtensionPackChanges[0].Summary.Contains("1 command", StringComparison.OrdinalIgnoreCase), $"Expected pack change summary to mention command count, got '{manifest.ExtensionPackChanges[0].Summary}'.");
    Require(manifest.AddedCommands is { Count: 1 }, $"Expected one added command, got {manifest.AddedCommands?.Count ?? 0}.");
    Require(manifest.AddedCommands![0].CommandId == "sample-echo", $"Expected added command id sample-echo, got '{manifest.AddedCommands[0].CommandId}'.");
    var preferredIndex = MainForm.FindPreferredPackIndex(
        new[]
        {
            importedPack with { PackId = "other-pack", DisplayName = "Other Pack" },
            importedPack
        },
        "sample-pack");
    Require(preferredIndex == 1, $"Expected preferred pack index 1, got {preferredIndex}.");

    using var splash = new UpdateSplashForm(manifest, isImportSplash: true);
    Require(splash.TitleText.Contains("Extension Pack Import", StringComparison.OrdinalIgnoreCase), $"Expected import splash title, got '{splash.TitleText}'.");
    Require(splash.SummaryText.Contains("Imported", StringComparison.OrdinalIgnoreCase), $"Expected splash summary to mention import, got '{splash.SummaryText}'.");
    Require(splash.DetailsText.Contains("Sample Pack", StringComparison.OrdinalIgnoreCase), $"Expected splash details to mention the imported pack, got '{splash.DetailsText}'.");
    Require(splash.DetailsText.Contains("sample-echo", StringComparison.OrdinalIgnoreCase), $"Expected splash details to mention the imported command, got '{splash.DetailsText}'.");
    Require(splash.NarrationText.Contains("extension pack import", StringComparison.OrdinalIgnoreCase), $"Expected import narration, got '{splash.NarrationText}'.");
}

static void ExtensionPackFolderImportExpandsDlls()
{
    var registry = PackTestSupport.CreateRegistry();
    var sourceAssembly = typeof(SampleCommandPack).Assembly.Location;

    var sourceDirectory = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "pack-folder", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(sourceDirectory);
    try
    {
        var nestedDirectory = Path.Combine(sourceDirectory, "nested");
        Directory.CreateDirectory(nestedDirectory);

        var sourceCopy = Path.Combine(nestedDirectory, Path.GetFileName(sourceAssembly));
        File.Copy(sourceAssembly, sourceCopy, overwrite: true);

        var expanded = CallsignCommandRegistry.ExpandImportablePackPaths(new[] { sourceDirectory });
        Require(expanded.Count == 1, $"Folder import should expand to one DLL, got {expanded.Count}.");
        Require(Path.GetFullPath(expanded[0]) == Path.GetFullPath(sourceCopy), "Expanded folder import should point to the nested pack DLL.");

        var import = registry.ImportPack(expanded[0]);
        Require(import.Succeeded, $"Expanded folder import should succeed: {import.Message}");
        Require(import.LoadStatus == CallsignPackLoadStatus.Disabled, "Expanded folder import should still default to disabled.");
    }
    finally
    {
        try
        {
            if (Directory.Exists(sourceDirectory))
                Directory.Delete(sourceDirectory, recursive: true);
        }
        catch
        {
        }
    }
}

static void ExtensionPackRemovalAndReimportWorksAsRollback()
{
    var registry = PackTestSupport.CreateRegistry();
    var sourceAssembly = Path.Combine(AppContext.BaseDirectory, "Callsign.SamplePack.dll");
    var sourceDirectory = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "packs-source", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        var sourceCopy = Path.Combine(sourceDirectory, Path.GetFileName(sourceAssembly));
        File.Copy(sourceAssembly, sourceCopy, overwrite: true);

        var import = registry.ImportPack(sourceCopy);
        Require(import.Succeeded, $"Initial pack import should succeed: {import.Message}");
        Require(!registry.TryResolve("sample pack echo hello", out _), "Imported community pack should start disabled.");

        Require(registry.RemovePack(import.PackId!, out var removeMessage, deleteAssemblyFile: false), $"Pack removal should succeed: {removeMessage}");
        Require(!registry.GetPacks().Any(pack => pack.PackId == "sample-pack"), "Removed pack should no longer appear in the registry.");
        Require(!registry.TryResolve("sample pack echo hello", out _), "Removed pack should not route.");

        var restoredSource = Path.Combine(sourceDirectory, "Callsign.SamplePack.Restored.dll");
        File.Copy(sourceCopy, restoredSource, overwrite: true);

        var reimport = registry.ImportPack(restoredSource, enableImmediately: true);
        Require(reimport.Succeeded, $"Re-import after removal should succeed: {reimport.Message}");
        Require(registry.TryResolve("sample pack echo hello", out var resolution), "Re-imported pack should route again.");
        Require(resolution.PackId == "sample-pack", $"Expected sample-pack id after rollback re-import, got '{resolution.PackId}'.");
    }
    finally
    {
        try
        {
            if (Directory.Exists(sourceDirectory))
                Directory.Delete(sourceDirectory, recursive: true);
        }
        catch
        {
        }
    }
}

static void ExtensionPackImportCanOverwriteInstalledCopy()
{
    var registry = PackTestSupport.CreateRegistry();
    var sourceAssembly = Path.Combine(AppContext.BaseDirectory, "Callsign.SamplePack.dll");

    var sourceDirectory = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "packs-source", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(sourceDirectory);
    try
    {
        var sourceCopy = Path.Combine(sourceDirectory, Path.GetFileName(sourceAssembly));
        File.Copy(sourceAssembly, sourceCopy, overwrite: true);

        var installedPath = Path.Combine(registry.PackRoot, Path.GetFileName(sourceCopy));
        Directory.CreateDirectory(registry.PackRoot);
        File.WriteAllBytes(installedPath, [0x43, 0x41, 0x4C, 0x4C, 0x53, 0x49, 0x47, 0x4E]);

        var import = registry.ImportPack(sourceCopy, enableImmediately: true, allowOverwrite: true);
        Require(import.Succeeded, $"Overwrite import should succeed: {import.Message}");
        Require(import.LoadStatus == CallsignPackLoadStatus.Loaded, $"Overwrite import should load immediately, got {import.LoadStatus}.");
        Require(File.ReadAllBytes(installedPath).SequenceEqual(File.ReadAllBytes(sourceCopy)), "Installed pack copy should be overwritten with the new source content.");
        Require(registry.TryResolve("sample pack echo hello", out var resolution), "Overwritten pack should route.");
        Require(resolution.PackId == "sample-pack", $"Expected sample-pack id after overwrite import, got '{resolution.PackId}'.");
    }
    finally
    {
        try
        {
            if (Directory.Exists(sourceDirectory))
                Directory.Delete(sourceDirectory, recursive: true);
        }
        catch
        {
        }
    }
}

static void ExtensionPackRegistryGatesPaidTiersByEntitlement()
{
    var freeOnlyPath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "free-only-packs", Guid.NewGuid().ToString("N"));
    var entitledPath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "entitled-packs", Guid.NewGuid().ToString("N"));

    try
    {
        var freeOnlyRegistry = new CallsignCommandRegistry(freeOnlyPath);
        freeOnlyRegistry.RegisterPack(new PaidSampleCommandPack());

        var gatedPack = freeOnlyRegistry.GetPacks().Single(pack => pack.PackId == "paid-sample-pack");
        Require(gatedPack.Tier == CallsignPackTier.Pro, $"Expected Pro pack tier, got {gatedPack.Tier}.");
        Require(gatedPack.LoadStatus == CallsignPackLoadStatus.EntitlementRequired, $"Expected entitlement-required status, got {gatedPack.LoadStatus}.");
        Require(gatedPack.Message.Contains("entitlement", StringComparison.OrdinalIgnoreCase), $"Expected entitlement message, got '{gatedPack.Message}'.");
        Require(gatedPack.CommandCount == 1, $"Expected gated pack metadata to expose command count, got {gatedPack.CommandCount}.");
        Require(!freeOnlyRegistry.TryResolve("paid sample action", out _), "Unentitled Pro pack should not route commands.");
        Require(!freeOnlyRegistry.EnablePack("paid-sample-pack"), "Unentitled Pro pack should not become enabled from the Packs UI.");
        var gatedDiscovery = CommandDiscoveryService.GetCommands(freeOnlyRegistry).Single(command => command.Phrase == "paid sample action");
        Require(gatedDiscovery.LoadStatus == CallsignPackLoadStatus.EntitlementRequired, $"Gated discovery should expose entitlement status, got {gatedDiscovery.LoadStatus}.");
        Require(gatedDiscovery.Availability.Contains("Entitlement required", StringComparison.OrdinalIgnoreCase), $"Gated discovery should explain entitlement availability, got '{gatedDiscovery.Availability}'.");
        Require(gatedDiscovery.Availability.Contains("Pro", StringComparison.OrdinalIgnoreCase), $"Gated discovery should name the required Pro tier, got '{gatedDiscovery.Availability}'.");
        Require(gatedDiscovery.Availability.Contains("will not route", StringComparison.OrdinalIgnoreCase), $"Gated discovery should say the command will not route, got '{gatedDiscovery.Availability}'.");
        var gatedDisplay = MainForm.FormatPackListDisplay(gatedPack);
        Require(gatedDisplay.Contains("entitlement required", StringComparison.OrdinalIgnoreCase), $"Expected pack list display to explain entitlement gate, got '{gatedDisplay}'.");
        var gatedSummary = MainForm.FormatPackSecuritySummary(gatedPack);
        Require(gatedSummary.Contains("tier=Pro", StringComparison.OrdinalIgnoreCase), $"Expected security summary to include Pro tier, got '{gatedSummary}'.");
        Require(gatedSummary.Contains("entitlement required", StringComparison.OrdinalIgnoreCase), $"Expected security summary to explain entitlement gate, got '{gatedSummary}'.");
        var gatedReadiness = MainForm.FormatPackEnablementReadiness(gatedPack);
        Require(gatedReadiness.Contains("blocked", StringComparison.OrdinalIgnoreCase), $"Expected gated readiness to say blocked, got '{gatedReadiness}'.");
        Require(gatedReadiness.Contains("Pro entitlement required", StringComparison.OrdinalIgnoreCase), $"Expected gated readiness to name Pro entitlement, got '{gatedReadiness}'.");
        Require(gatedReadiness.Contains("can route", StringComparison.OrdinalIgnoreCase), $"Expected gated readiness to explain routing, got '{gatedReadiness}'.");

        var entitledRegistry = new CallsignCommandRegistry(
            entitledPath,
            new CallsignEntitlementState(new[] { CallsignPackTier.Free, CallsignPackTier.Pro }));
        entitledRegistry.RegisterPack(new PaidSampleCommandPack());

        var entitledPack = entitledRegistry.GetPacks().Single(pack => pack.PackId == "paid-sample-pack");
        Require(entitledPack.LoadStatus == CallsignPackLoadStatus.Loaded, $"Expected entitled Pro pack to load, got {entitledPack.LoadStatus}.");
        Require(entitledRegistry.TryResolve("paid sample action", out var resolution), "Entitled Pro pack should route commands.");
        Require(resolution.Tier == CallsignPackTier.Pro, $"Expected Pro command resolution, got {resolution.Tier}.");
    }
    finally
    {
        TryDeleteDirectory(freeOnlyPath);
        TryDeleteDirectory(entitledPath);
    }
}

static void ExtensionPackRegistryRejectsInvalidMetadata()
{
    var registry = PackTestSupport.CreateRegistry();
    registry.RegisterPack(new InvalidMetadataCommandPack());

    var pack = registry.GetPacks().Single(pack => pack.PackId == "invalid-metadata-pack");
    Require(pack.LoadStatus == CallsignPackLoadStatus.InvalidPack, $"Expected invalid pack status, got {pack.LoadStatus}.");
    Require(pack.Message.Contains("metadata is invalid", StringComparison.OrdinalIgnoreCase), $"Expected metadata validation message, got '{pack.Message}'.");
    Require(pack.CommandCount == 1, $"Invalid pack metadata should still expose attempted command count, got {pack.CommandCount}.");
    Require(!registry.TryResolve("invalid metadata action", out _), "Invalid command-pack metadata should not route commands.");
    Require(!registry.EnablePack("invalid-metadata-pack"), "Invalid command-pack metadata should not become enabled from the Packs UI.");

    var discovery = CommandDiscoveryService.GetCommands(registry);
    Require(!discovery.Any(command => command.Phrase == "invalid metadata action"), "Invalid command-pack metadata should not expose runnable command discovery entries.");
    var display = MainForm.FormatPackListDisplay(pack);
    Require(display.Contains("InvalidPack", StringComparison.OrdinalIgnoreCase) || display.Contains("Invalid pack", StringComparison.OrdinalIgnoreCase), $"Pack display should expose invalid metadata status, got '{display}'.");
    var summary = MainForm.FormatPackSecuritySummary(pack);
    Require(summary.Contains("InvalidPack", StringComparison.OrdinalIgnoreCase) || summary.Contains("invalid", StringComparison.OrdinalIgnoreCase), $"Security summary should expose invalid metadata status, got '{summary}'.");
    var readiness = MainForm.FormatPackEnablementReadiness(pack);
    Require(readiness.Contains("blocked", StringComparison.OrdinalIgnoreCase), $"Invalid pack readiness should say blocked, got '{readiness}'.");
    Require(readiness.Contains("metadata is invalid", StringComparison.OrdinalIgnoreCase), $"Invalid pack readiness should explain metadata failure, got '{readiness}'.");
}

static void ExtensionPackRegistryGatesCommandTiersByEntitlement()
{
    var freeOnlyPath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "free-pack-paid-command", Guid.NewGuid().ToString("N"));
    var entitledPath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "advanced-command-entitled", Guid.NewGuid().ToString("N"));

    try
    {
        var freeOnlyRegistry = new CallsignCommandRegistry(freeOnlyPath);
        freeOnlyRegistry.RegisterPack(new MixedTierCommandPack());

        var freePack = freeOnlyRegistry.GetPacks().Single(pack => pack.PackId == "mixed-tier-pack");
        Require(freePack.Tier == CallsignPackTier.Free, $"Expected mixed-tier pack descriptor to stay Free, got {freePack.Tier}.");
        Require(freePack.LoadStatus == CallsignPackLoadStatus.Loaded, $"Expected Free pack container to load, got {freePack.LoadStatus}.");
        Require(!freeOnlyRegistry.TryResolve("advanced mixed action", out _), "Unentitled Advanced command inside a Free pack should not route.");

        var gatedCommand = freeOnlyRegistry.GetCommands().Single(command => command.CommandId == "advanced-mixed-action");
        Require(gatedCommand.Tier == CallsignPackTier.Advanced, $"Command resolution metadata should preserve Advanced tier, got {gatedCommand.Tier}.");
        Require(gatedCommand.LoadStatus == CallsignPackLoadStatus.EntitlementRequired, $"Unentitled command discovery should expose entitlement-required status, got {gatedCommand.LoadStatus}.");

        var gatedDiscovery = CommandDiscoveryService.GetCommands(freeOnlyRegistry).Single(command => command.Phrase == "advanced mixed action");
        Require(gatedDiscovery.Tier == CallsignPackTier.Advanced, $"Command discovery should preserve Advanced tier, got {gatedDiscovery.Tier}.");
        Require(gatedDiscovery.Availability.Contains("Entitlement required", StringComparison.OrdinalIgnoreCase), $"Command discovery should explain command-level entitlement gate, got '{gatedDiscovery.Availability}'.");
        Require(gatedDiscovery.Availability.Contains("Advanced", StringComparison.OrdinalIgnoreCase), $"Command discovery should name the required Advanced tier, got '{gatedDiscovery.Availability}'.");
        Require(gatedDiscovery.Availability.Contains("will not route", StringComparison.OrdinalIgnoreCase), $"Command discovery should say unentitled Advanced commands will not route, got '{gatedDiscovery.Availability}'.");

        var entitledRegistry = new CallsignCommandRegistry(
            entitledPath,
            new CallsignEntitlementState(new[] { CallsignPackTier.Free, CallsignPackTier.Advanced }));
        entitledRegistry.RegisterPack(new MixedTierCommandPack());

        Require(entitledRegistry.TryResolve("advanced mixed action", out var resolution), "Advanced-entitled registry should route the Advanced command inside a Free pack.");
        Require(resolution.Tier == CallsignPackTier.Advanced, $"Entitled command resolution should keep Advanced tier, got {resolution.Tier}.");
    }
    finally
    {
        TryDeleteDirectory(freeOnlyPath);
        TryDeleteDirectory(entitledPath);
    }
}

static void ExtensionPackRegistryRequiresSignaturesWhenDeclared()
{
    var packPath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "unsigned-pack", Guid.NewGuid().ToString("N"));

    try
    {
        var registry = new CallsignCommandRegistry(
            packPath,
            new CallsignEntitlementState(new[] { CallsignPackTier.Free, CallsignPackTier.Pro }));
        registry.RegisterPack(new PaidSampleCommandPack(signatureStatus: "dev"));

        var unsignedPack = registry.GetPacks().Single(pack => pack.PackId == "paid-sample-pack");
        Require(unsignedPack.RequiresSignature, "Unsigned test pack should declare that a signature is required.");
        Require(unsignedPack.SignatureStatus == "dev", $"Expected dev signature status, got '{unsignedPack.SignatureStatus}'.");
        Require(unsignedPack.LoadStatus == CallsignPackLoadStatus.SignatureRequired, $"Expected signature-required status, got {unsignedPack.LoadStatus}.");
        Require(unsignedPack.Message.Contains("signature", StringComparison.OrdinalIgnoreCase), $"Expected signature message, got '{unsignedPack.Message}'.");
        Require(!registry.TryResolve("paid sample action", out _), "Pack requiring a valid signature should not route commands when unsigned.");
        Require(!registry.EnablePack("paid-sample-pack"), "Pack requiring a valid signature should not become enabled from the Packs UI.");
        var unsignedDiscovery = CommandDiscoveryService.GetCommands(registry).Single(command => command.Phrase == "paid sample action");
        Require(unsignedDiscovery.LoadStatus == CallsignPackLoadStatus.SignatureRequired, $"Unsigned discovery should expose signature status, got {unsignedDiscovery.LoadStatus}.");
        Require(unsignedDiscovery.Availability.Contains("Signature required", StringComparison.OrdinalIgnoreCase), $"Unsigned discovery should explain signature availability, got '{unsignedDiscovery.Availability}'.");
        Require(unsignedDiscovery.Availability.Contains("will not route", StringComparison.OrdinalIgnoreCase), $"Unsigned discovery should say unsigned commands will not route, got '{unsignedDiscovery.Availability}'.");
        var unsignedDisplay = MainForm.FormatPackListDisplay(unsignedPack);
        Require(unsignedDisplay.Contains("signature required", StringComparison.OrdinalIgnoreCase), $"Expected pack list display to explain signature gate, got '{unsignedDisplay}'.");
        var unsignedSummary = MainForm.FormatPackSecuritySummary(unsignedPack);
        Require(unsignedSummary.Contains("signature=dev", StringComparison.OrdinalIgnoreCase), $"Expected security summary to include signature status, got '{unsignedSummary}'.");
        Require(unsignedSummary.Contains("valid signature required", StringComparison.OrdinalIgnoreCase), $"Expected security summary to explain signature gate, got '{unsignedSummary}'.");
        var unsignedReadiness = MainForm.FormatPackEnablementReadiness(unsignedPack);
        Require(unsignedReadiness.Contains("blocked", StringComparison.OrdinalIgnoreCase), $"Expected unsigned readiness to say blocked, got '{unsignedReadiness}'.");
        Require(unsignedReadiness.Contains("valid signed pack is required", StringComparison.OrdinalIgnoreCase), $"Expected unsigned readiness to explain signature gate, got '{unsignedReadiness}'.");
    }
    finally
    {
        TryDeleteDirectory(packPath);
    }
}

static void ExtensionPackExecutionEnforcesPolicyAtRegistryBoundary()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "policy-boundary", Guid.NewGuid().ToString("N"));
    try
    {
        var registry = new CallsignCommandRegistry(root, CallsignEntitlementState.AllTiers);
        registry.RegisterPack(new ApprovalRequiredCommandPack());

        Require(registry.TryResolve("approval sample action", out var resolution), "Approval-required command should resolve.");
        var execution = new CallsignCommandExecutionContext(
            resolution.PackId,
            resolution.CommandId,
            "approval sample action",
            "approval sample action",
            resolution.ArgumentText,
            "Echo One",
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Require(registry.TryExecute(execution, out var noIdentityResult), "Approval-required command should match before identity policy blocks.");
        Require(!noIdentityResult.Succeeded, "Approval-required command should not run without identity context.");
        Require(noIdentityResult.AuditEvent?.Contains("fresh_identity_required", StringComparison.OrdinalIgnoreCase) == true, $"Expected fresh-identity audit event, got '{noIdentityResult.AuditEvent}'.");
        Require(noIdentityResult.PolicyDecision == CallsignPolicyDecision.RequireFreshIdentity, $"Expected fresh-identity policy decision, got {noIdentityResult.PolicyDecision}.");
        Require(noIdentityResult.PolicyApprovalRequirement == CallsignCommandApprovalRequirement.RequireFreshIdentity, $"Expected fresh-identity approval requirement, got {noIdentityResult.PolicyApprovalRequirement}.");
        Require(noIdentityResult.PolicyRiskTier == CallsignCommandRiskTier.ExternalSideEffect, $"Expected external-side-effect risk tier, got {noIdentityResult.PolicyRiskTier}.");

        Require(registry.TryExecute(execution, out var needsApprovalResult, identityVerified: true, freshIdentityVerified: true), "Approval-required command should match before approval policy blocks.");
        Require(!needsApprovalResult.Succeeded, "Approval-required command should not run without explicit approval.");
        Require(needsApprovalResult.AuditEvent?.Contains("approval_required", StringComparison.OrdinalIgnoreCase) == true, $"Expected approval-required audit event, got '{needsApprovalResult.AuditEvent}'.");
        Require(needsApprovalResult.PolicyDecision == CallsignPolicyDecision.RequireApproval, $"Expected approval-required policy decision, got {needsApprovalResult.PolicyDecision}.");
        Require(needsApprovalResult.PolicyApprovalRequirement == CallsignCommandApprovalRequirement.RequireApproval, $"Expected require-approval policy metadata, got {needsApprovalResult.PolicyApprovalRequirement}.");
        Require(needsApprovalResult.PolicyRiskTier == CallsignCommandRiskTier.ExternalSideEffect, $"Expected external-side-effect risk metadata, got {needsApprovalResult.PolicyRiskTier}.");

        Require(registry.TryExecute(execution, out var approvedResult, identityVerified: true, freshIdentityVerified: true, approvalGranted: true), "Approved extension command should execute after identity and approval proof.");
        Require(approvedResult.Succeeded, $"Approved extension command should succeed, got '{approvedResult.Message}'.");
        Require(approvedResult.AuditEvent?.Contains("approval-pack:approval-sample-action", StringComparison.OrdinalIgnoreCase) == true, $"Expected pack execution audit event, got '{approvedResult.AuditEvent}'.");
        Require(approvedResult.PolicyDecision == null, $"Approved pack execution should not report a policy block, got {approvedResult.PolicyDecision}.");
    }
    finally
    {
        TryDeleteDirectory(root);
    }
}

static void VoiceShortcutsStorePersistsLocalShortcutDefinitions()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "voice-shortcuts-store", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new VoiceShortcutStore(root);
        var draft = store.CreateDraft() with
        {
            Title = "Toggle Address",
            WhenISay = "toggle address",
            Group = "Browser",
            Actions = new[]
            {
                new VoiceShortcutAction(VoiceShortcutActionKind.Command, "browser focus address bar"),
                new VoiceShortcutAction(VoiceShortcutActionKind.Wait, DurationMilliseconds: 500),
                new VoiceShortcutAction(VoiceShortcutActionKind.Command, "press control l")
            }
        };

        var saved = store.Save(draft);
        Require(saved.Succeeded, $"Voice shortcut save should succeed: {saved.Message}");
        Require(saved.Shortcut != null, "Voice shortcut save should return the saved shortcut.");

        var shortcuts = store.GetShortcuts();
        Require(shortcuts.Count == 1, $"Expected one saved voice shortcut, got {shortcuts.Count}.");
        var shortcut = shortcuts[0];
        Require(shortcut.Title == "Toggle Address", $"Expected saved title, got '{shortcut.Title}'.");
        Require(shortcut.WhenISay == "toggle address", $"Expected saved phrase, got '{shortcut.WhenISay}'.");
        Require(shortcut.Group == "Browser", $"Expected saved group, got '{shortcut.Group}'.");
        Require(shortcut.Actions.Count == 3, $"Expected three saved shortcut actions, got {shortcut.Actions.Count}.");
        Require(shortcut.Actions[1].Kind == VoiceShortcutActionKind.Wait, $"Expected wait action in slot two, got {shortcut.Actions[1].Kind}.");
        Require(shortcut.Actions[1].DurationMilliseconds == 500, $"Expected wait duration 500 ms, got {shortcut.Actions[1].DurationMilliseconds}.");
    }
    finally
    {
        TryDeleteDirectory(root);
    }
}

static void VoiceShortcutsPackExposesEnabledShortcutsAndFollowUpSteps()
{
    var registry = PackTestSupport.CreateRegistry();
    var enabledShortcut = new VoiceShortcutDefinition(
        ShortcutId: "shortcut-toggle-address",
        Title: "Toggle Address",
        WhenISay: "toggle address",
        Group: "Browser",
        Enabled: true,
        Actions: new[]
        {
            new VoiceShortcutAction(VoiceShortcutActionKind.Command, "browser focus address bar"),
            new VoiceShortcutAction(VoiceShortcutActionKind.Wait, DurationMilliseconds: 750),
            new VoiceShortcutAction(VoiceShortcutActionKind.Command, "press control l")
        },
        CreatedUtc: DateTimeOffset.UtcNow,
        UpdatedUtc: DateTimeOffset.UtcNow);
    var disabledShortcut = enabledShortcut with
    {
        ShortcutId = "shortcut-disabled",
        Title = "Disabled Shortcut",
        WhenISay = "disabled shortcut",
        Enabled = false
    };

    registry.RegisterPack(new VoiceShortcutCommandPack(new[] { enabledShortcut, disabledShortcut }));

    Require(registry.TryResolve("toggle address", out var resolution), "Enabled voice shortcut should resolve through the pack registry.");
    Require(resolution.PackId == VoiceShortcutConstants.PackId, $"Expected voice-shortcuts pack id, got '{resolution.PackId}'.");
    Require(resolution.CommandId == "shortcut-toggle-address", $"Expected toggle-address command id, got '{resolution.CommandId}'.");
    Require(!registry.TryResolve("disabled shortcut", out _), "Disabled voice shortcut should not resolve.");

    var commands = CommandDiscoveryService.GetCommands(registry);
    Require(commands.Any(command => command.Category == "Voice shortcuts" && command.Phrase == "open voice shortcuts"), "Discovery should include the built-in voice-shortcuts management surface.");
    Require(commands.Any(command => command.Category == "Voice shortcuts" && command.Phrase == "toggle address" && command.Source.Contains("Voice Shortcuts", StringComparison.OrdinalIgnoreCase)), "Discovery should include enabled local voice shortcuts.");

    var execution = new CallsignCommandExecutionContext(
        resolution.PackId,
        resolution.CommandId,
        "toggle address",
        "toggle address",
        resolution.ArgumentText,
        "Echo One",
        DateTimeOffset.UtcNow,
        CancellationToken.None);
    Require(registry.TryExecute(execution, out var result, identityVerified: true, freshIdentityVerified: true), "Voice shortcut should execute through the registry.");
    Require(result.Succeeded, $"Voice shortcut pack execution should succeed, got '{result.Message}'.");
    Require(result.FollowUpSteps?.Count == 3, $"Expected three follow-up steps, got {result.FollowUpSteps?.Count ?? 0}.");
    Require(result.FollowUpSteps?[0].Kind == CallsignFollowUpStepKind.Command, $"Expected first follow-up step to be a command, got {result.FollowUpSteps?[0].Kind}.");
    Require(result.FollowUpSteps?[1].Kind == CallsignFollowUpStepKind.Wait, $"Expected second follow-up step to be a wait, got {result.FollowUpSteps?[1].Kind}.");
    Require(result.FollowUpSteps?[1].DurationMilliseconds == 750, $"Expected wait duration 750 ms, got {result.FollowUpSteps?[1].DurationMilliseconds}.");
}

static void VoiceShortcutsSurfaceIsWiredIntoRoutingAndDiscovery()
{
    Require(AlphaCommandRouter.TryRouteUiNavigation("open voice shortcuts", out var shortcutsTab), "Open voice shortcuts should route to the Shortcuts tab.");
    Require(shortcutsTab == "Shortcuts", $"Expected Shortcuts tab target, got '{shortcutsTab}'.");
    Require(AlphaCommandRouter.TryRoute("save voice shortcut", out var saveShortcutRoute), "Save voice shortcut should route.");
    Require(saveShortcutRoute.Target == "ui-save-voice-shortcut", $"Expected ui-save-voice-shortcut target, got '{saveShortcutRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("add voice shortcut wait action", out var addWaitRoute), "Add voice shortcut wait action should route.");
    Require(addWaitRoute.Target == "ui-add-voice-shortcut-wait-action", $"Expected ui-add-voice-shortcut-wait-action target, got '{addWaitRoute.Target}'.");

    var repoRoot = FindRepositoryRoot();
    var mainFormSource = File.ReadAllText(Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("BuildShortcutsTab()", StringComparison.OrdinalIgnoreCase), "MainForm should build a visible Shortcuts tab.");
    Require(mainFormSource.Contains("ShowShortcutsTab()", StringComparison.OrdinalIgnoreCase), "MainForm should expose a voice-shortcuts tab helper.");
    Require(mainFormSource.Contains("Voice shortcuts let you save a spoken phrase", StringComparison.OrdinalIgnoreCase), "Shortcuts tab should explain saved spoken-phrase behavior.");
    Require(mainFormSource.Contains("AccessibleName = \"Voice shortcuts safety\"", StringComparison.OrdinalIgnoreCase), "Shortcuts tab safety line should expose an accessible name.");
    Require(mainFormSource.Contains("voice shortcuts compose existing visible Callsign commands", StringComparison.OrdinalIgnoreCase), "Shortcuts tab should explain that shortcuts compose existing visible commands.");
    Require(mainFormSource.Contains("requires wake, identity, policy, visibility, audit, and any paid entitlement gates", StringComparison.OrdinalIgnoreCase), "Shortcuts tab should explain that shortcuts cannot bypass Callsign gates.");
    Require(mainFormSource.Contains("bounded waits do not add new privileges", StringComparison.OrdinalIgnoreCase), "Shortcuts tab should explain bounded waits do not grant privileges.");
    Require(mainFormSource.Contains("Voice phrase: save voice shortcut.", StringComparison.OrdinalIgnoreCase), "Shortcuts save button should expose its spoken phrase.");
}

static void AlphaAuditLogWritesCorrelationAndVerificationFields()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "audit", Guid.NewGuid().ToString("N"));
    try
    {
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "Echo One",
            DisplayName = "Echo One"
        };
        store.Save(profile);

        var audit = new AlphaAuditLog(store);
        Require(
            audit.TryRecordCommand(
                profile,
                eventType: "alpha.command_execution",
                actionName: "visible_control",
                status: "succeeded",
                out var warning,
                commandFamily: "UiAction",
                actionTarget: "ui-show-visible-controls",
                details: "policy_allowed:ui-show-visible-controls",
                success: true,
                correlationId: "task_123.step_001",
                verificationMethod: "visible_status",
                verificationSummary: "Visible controls overlay shown."),
            $"Audit write should succeed: {warning}");

        var auditPath = Path.Combine(store.ResolveCallsSignFolder(profile.Callsign), "alpha-audit.jsonl");
        Require(File.Exists(auditPath), $"Audit file should exist at {auditPath}.");
        var line = File.ReadLines(auditPath).Single();
        using var json = JsonDocument.Parse(line);
        var rootElement = json.RootElement;
        Require(rootElement.GetProperty("correlation_id").GetString() == "task_123.step_001", "Audit should record the supplied correlation id.");
        Require(rootElement.GetProperty("event_type").GetString() == "alpha.command_execution", "Audit should record the event type.");
        Require(rootElement.GetProperty("action_name").GetString() == "visible_control", "Audit should record the action name.");
        var verification = rootElement.GetProperty("verification");
        Require(verification.GetProperty("performed").GetBoolean(), "Audit verification should be marked performed.");
        Require(verification.GetProperty("method").GetString() == "visible_status", "Audit should record the verification method.");
        Require(verification.GetProperty("summary").GetString() == "Visible controls overlay shown.", "Audit should record the verification summary.");

        Require(
            audit.TryRecordStartMenuLaunch(
                profile,
                "Notepad",
                out warning,
                launchPath: "start-menu-search",
                visibleStartMenuPath: true),
            $"Start menu audit write should succeed: {warning}");
        var launchLine = File.ReadLines(auditPath).Last();
        using var launchJson = JsonDocument.Parse(launchLine);
        var launchRoot = launchJson.RootElement;
        Require(launchRoot.GetProperty("launch_path").GetString() == "start-menu-search", "Start menu audit should record the actual launch path.");
        var launchVerification = launchRoot.GetProperty("verification");
        Require(launchVerification.GetProperty("performed").GetBoolean(), "Start menu audit verification should be marked performed.");
        Require(launchVerification.GetProperty("method").GetString() == "visible_start_menu", "Start menu audit should record visible Start menu verification.");
        Require(launchVerification.GetProperty("summary").GetString()?.Contains("visible Start menu", StringComparison.OrdinalIgnoreCase) == true, "Start menu audit should summarize visible launch proof.");
    }
    finally
    {
        TryDeleteDirectory(root);
    }
}

static void CommandPolicyEvaluatesParityMetadata()
{
    var localCommand = new CallsignCommandDefinition(
        CommandId: "parity-show-numbers",
        DisplayName: "Show numbers",
        VoicePhrases: new[] { "show numbers" },
        Description: "Shows numbered visible controls.",
        Kind: CallsignCommandKind.UiAction,
        Tier: CallsignPackTier.Free,
        RiskTier: CallsignCommandRiskTier.LocalReversible,
        Category: "Visible control",
        PrivacyImpact: CallsignCommandPrivacyImpact.UiText,
        HelpText: "Say a number to activate a visible control.",
        Examples: new[] { "show numbers", "click 1" },
        VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus);

    var noIdentity = CallsignCommandPolicy.Evaluate(localCommand, identityVerified: false);
    Require(noIdentity.Decision == CallsignPolicyDecision.RequireFreshIdentity, $"Expected fresh identity decision, got {noIdentity.Decision}.");

    var allowed = CallsignCommandPolicy.Evaluate(localCommand, identityVerified: true);
    Require(allowed.Decision == CallsignPolicyDecision.Allow, $"Expected allow decision, got {allowed.Decision}.");
    Require(allowed.VisibleActionRequired, "Visible control commands should require visible action.");

    var visibleRequiredCommand = localCommand with
    {
        CommandId = "parity-visible-required",
        VisibleAction = false,
        VisibilityRequirement = CallsignCommandVisibilityRequirement.VisibleRequired
    };
    var visibleRequired = CallsignCommandPolicy.Evaluate(visibleRequiredCommand, identityVerified: true);
    Require(visibleRequired.Decision == CallsignPolicyDecision.Allow, $"Expected allow decision for visible-required command, got {visibleRequired.Decision}.");
    Require(visibleRequired.VisibleActionRequired, "VisibleRequired commands should require a visible surface even if VisibleAction metadata is false.");

    var visiblePreferredCommand = localCommand with
    {
        CommandId = "parity-visible-preferred",
        VisibleAction = false,
        VisibilityRequirement = CallsignCommandVisibilityRequirement.VisiblePreferred
    };
    var visiblePreferred = CallsignCommandPolicy.Evaluate(visiblePreferredCommand, identityVerified: true);
    Require(!visiblePreferred.VisibleActionRequired, "VisiblePreferred commands with VisibleAction=false should not be promoted to visible-required.");

    var freshIdentityCommand = localCommand with
    {
        CommandId = "parity-dictation-start",
        DisplayName = "Start dictation",
        Kind = CallsignCommandKind.Dictation,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.UiText,
        ApprovalRequirement = CallsignCommandApprovalRequirement.RequireFreshIdentity
    };
    var staleIdentity = CallsignCommandPolicy.Evaluate(freshIdentityCommand, identityVerified: true, freshIdentityVerified: false);
    Require(staleIdentity.Decision == CallsignPolicyDecision.RequireFreshIdentity, $"Expected fresh identity decision for dictation, got {staleIdentity.Decision}.");
    var freshIdentity = CallsignCommandPolicy.Evaluate(freshIdentityCommand, identityVerified: true, freshIdentityVerified: true);
    Require(freshIdentity.Decision == CallsignPolicyDecision.Allow, $"Expected allow decision for fresh dictation, got {freshIdentity.Decision}.");

    var clipboardHistoryCommand = localCommand with
    {
        CommandId = "parity-clipboard-history",
        DisplayName = "Clipboard history",
        Kind = CallsignCommandKind.SystemControl,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.Clipboard,
        ApprovalRequirement = CallsignCommandApprovalRequirement.RequireApproval
    };
    var clipboardHistory = CallsignCommandPolicy.Evaluate(clipboardHistoryCommand, identityVerified: true, freshIdentityVerified: true);
    Require(clipboardHistory.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for clipboard history, got {clipboardHistory.Decision}.");

    var implicitClipboardCommand = localCommand with
    {
        CommandId = "advanced-clipboard-read",
        DisplayName = "Read clipboard",
        Tier = CallsignPackTier.Advanced,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.Clipboard,
        ApprovalRequirement = CallsignCommandApprovalRequirement.None
    };
    var implicitClipboard = CallsignCommandPolicy.Evaluate(implicitClipboardCommand, identityVerified: true, freshIdentityVerified: true);
    Require(implicitClipboard.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for clipboard privacy impact, got {implicitClipboard.Decision}.");

    var snippingToolbarCommand = localCommand with
    {
        CommandId = "parity-snipping-toolbar",
        DisplayName = "Snipping toolbar",
        Kind = CallsignCommandKind.SystemControl,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.ScreenshotOrOcr,
        ApprovalRequirement = CallsignCommandApprovalRequirement.RequireApproval
    };
    var snippingToolbar = CallsignCommandPolicy.Evaluate(snippingToolbarCommand, identityVerified: true, freshIdentityVerified: true);
    Require(snippingToolbar.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for snipping toolbar, got {snippingToolbar.Decision}.");

    var implicitScreenshotCommand = localCommand with
    {
        CommandId = "advanced-screen-analysis",
        DisplayName = "Screen analysis",
        Tier = CallsignPackTier.Advanced,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.ScreenshotOrOcr,
        ApprovalRequirement = CallsignCommandApprovalRequirement.None
    };
    var implicitScreenshot = CallsignCommandPolicy.Evaluate(implicitScreenshotCommand, identityVerified: true, freshIdentityVerified: true);
    Require(implicitScreenshot.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for screenshot/OCR privacy impact, got {implicitScreenshot.Decision}.");

    var fileContentsCommand = localCommand with
    {
        CommandId = "advanced-file-summary",
        DisplayName = "Summarize file",
        Tier = CallsignPackTier.Advanced,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.FileContents,
        ApprovalRequirement = CallsignCommandApprovalRequirement.None
    };
    var fileContents = CallsignCommandPolicy.Evaluate(fileContentsCommand, identityVerified: true, freshIdentityVerified: true);
    Require(fileContents.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for file-content privacy impact, got {fileContents.Decision}.");

    var externalDataCommand = localCommand with
    {
        CommandId = "advanced-external-data",
        DisplayName = "External data lookup",
        Tier = CallsignPackTier.Advanced,
        RiskTier = CallsignCommandRiskTier.LocalStateChange,
        PrivacyImpact = CallsignCommandPrivacyImpact.ExternalData,
        ApprovalRequirement = CallsignCommandApprovalRequirement.None
    };
    var externalData = CallsignCommandPolicy.Evaluate(externalDataCommand, identityVerified: true, freshIdentityVerified: true);
    Require(externalData.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for external-data privacy impact, got {externalData.Decision}.");

    var backgroundAllowedCommand = localCommand with
    {
        CommandId = "advanced-background-diagnostic",
        DisplayName = "Background diagnostic",
        Tier = CallsignPackTier.Advanced,
        RiskTier = CallsignCommandRiskTier.LocalReversible,
        VisibilityRequirement = CallsignCommandVisibilityRequirement.BackgroundAllowedWithApproval,
        ApprovalRequirement = CallsignCommandApprovalRequirement.None
    };
    var backgroundAllowed = CallsignCommandPolicy.Evaluate(backgroundAllowedCommand, identityVerified: true, freshIdentityVerified: true);
    Require(backgroundAllowed.Decision == CallsignPolicyDecision.RequireApproval, $"Expected approval decision for background-allowed command, got {backgroundAllowed.Decision}.");
    Require(backgroundAllowed.VisibleActionRequired, "Background-allowed commands should still require a visible approval surface.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("clipboard-history", StringComparison.OrdinalIgnoreCase), "Built-in clipboard history commands should be tagged for clipboard privacy and approval.");
    Require(mainFormSource.Contains("snipping-toolbar", StringComparison.OrdinalIgnoreCase), "Built-in snipping toolbar commands should be tagged for screenshot privacy and approval.");
    Require(mainFormSource.Contains("CallsignCommandApprovalRequirement.RequireApproval", StringComparison.OrdinalIgnoreCase), "Built-in approval mapping should remain present for sensitive system commands.");

    var blockedCommand = localCommand with
    {
        CommandId = "dangerous-shell",
        RiskTier = CallsignCommandRiskTier.DangerousOrBlocked,
        ApprovalRequirement = CallsignCommandApprovalRequirement.Blocked
    };
    var blocked = CallsignCommandPolicy.Evaluate(blockedCommand, identityVerified: true, freshIdentityVerified: true);
    Require(blocked.Decision == CallsignPolicyDecision.BlockedDangerousAction, $"Expected blocked decision, got {blocked.Decision}.");
}

static void UpdateManifestCarriesSplashCommandChanges()
{
    var manifest = new CallsignUpdateManifest(
        Version: "1.4.0a",
        InstallerUrl: "https://example.invalid/downloads/Callsign-Setup.exe",
        InstallerSha256: new string('a', 64),
        InstallerSizeBytes: 123456,
        ReleaseNotes: "Voice Access parity hardening.",
        AddedCommands:
        [
            new CallsignUpdateCommandChange(
                CommandId: "parity-grid",
                DisplayName: "Show grid",
                Category: "Visible control",
                Summary: "Adds a mouse targeting grid.")
        ],
        SplashSummary: "New visible control commands are available.");

    Require(manifest.AddedCommands?.Count == 1, "Manifest should carry newly added commands for the update splash.");
    Require(manifest.SplashSummary?.Contains("visible control", StringComparison.OrdinalIgnoreCase) == true, "Manifest splash summary should describe new features.");
    Require(manifest.InstallerSha256.Length == 64, "Manifest should carry a SHA-256 installer hash.");
}

static void UpdateSplashPresentsManifestDetails()
{
    var manifest = new CallsignUpdateManifest(
        Version: "1.4.0a",
        InstallerUrl: "https://example.invalid/downloads/Callsign-Setup.exe",
        InstallerSha256: new string('a', 64),
        InstallerSizeBytes: 123456,
        ReleaseNotes: "Voice Access parity hardening.",
        AddedCommands:
        [
            new CallsignUpdateCommandChange(
                CommandId: "show-numbers",
                DisplayName: "Show numbers",
                Category: "Visible control",
                Summary: "Adds numbered overlays for visible controls.",
                Tier: CallsignPackTier.Free)
        ],
        ChangedCommands:
        [
            new CallsignUpdateCommandChange(
                CommandId: "browser-refresh",
                DisplayName: "Browser refresh",
                Category: "Browser",
                Summary: "Adds browser page control updates.",
                Tier: CallsignPackTier.Free)
        ],
        ExtensionPackChanges:
        [
            new CallsignUpdateExtensionChange(
                PackId: "sample-pack",
                DisplayName: "Sample Pack",
                Version: "1.0.1",
                Tier: CallsignPackTier.Free,
                Summary: "Community pack import defaults to disabled.",
                SignatureStatus: "signed")
        ],
        SplashSummary: "New visible control commands are available.",
        PublishedUtc: DateTimeOffset.UtcNow);

    using var splash = new UpdateSplashForm(manifest);
    RequireVisualContract(splash.VisualStyleName, "update splash");
    Require(splash.TitleText.Contains("1.4.0a", StringComparison.OrdinalIgnoreCase), $"Expected update splash title to include version, got '{splash.TitleText}'.");
    Require(splash.CloseGlyphText == "\u00D7", $"Expected update splash close glyph to be a clean multiply sign, got '{splash.CloseGlyphText}'.");
    Require(splash.SurfaceAccessibleName.Contains("update splash", StringComparison.OrdinalIgnoreCase), $"Expected update splash surface accessibility name, got '{splash.SurfaceAccessibleName}'.");
    Require(splash.SurfaceAccessibleDescription.Contains("extension-pack commands", StringComparison.OrdinalIgnoreCase), $"Expected update splash surface accessibility description, got '{splash.SurfaceAccessibleDescription}'.");
    Require(splash.PanelAccessibleName.Contains("Update splash surface", StringComparison.OrdinalIgnoreCase), $"Expected update splash panel accessibility name, got '{splash.PanelAccessibleName}'.");
    Require(splash.TitleAccessibleName.Contains("title", StringComparison.OrdinalIgnoreCase), $"Expected update splash title accessibility name, got '{splash.TitleAccessibleName}'.");
    Require(splash.SubtitleAccessibleName.Contains("published", StringComparison.OrdinalIgnoreCase), $"Expected update splash subtitle accessibility name, got '{splash.SubtitleAccessibleName}'.");
    Require(splash.SummaryAccessibleName.Contains("summary", StringComparison.OrdinalIgnoreCase), $"Expected update splash summary accessibility name, got '{splash.SummaryAccessibleName}'.");
    Require(splash.CueAccessibleName.Contains("voice cue", StringComparison.OrdinalIgnoreCase), $"Expected update splash cue accessibility name, got '{splash.CueAccessibleName}'.");
    Require(splash.CueAccessibleDescription.Contains("policy", StringComparison.OrdinalIgnoreCase), $"Expected update splash cue accessibility description to mention policy, got '{splash.CueAccessibleDescription}'.");
    Require(splash.DetailsAccessibleName.Contains("details", StringComparison.OrdinalIgnoreCase), $"Expected update splash details accessibility name, got '{splash.DetailsAccessibleName}'.");
    Require(splash.DetailsAccessibleDescription.Contains("extension-pack command changes", StringComparison.OrdinalIgnoreCase), $"Expected update splash details accessibility description, got '{splash.DetailsAccessibleDescription}'.");
    Require(splash.CloseButtonAccessibleName.Contains("Close update splash", StringComparison.OrdinalIgnoreCase), $"Expected update splash close button accessibility name, got '{splash.CloseButtonAccessibleName}'.");
    var splashButtons = EnumerateControls(splash).OfType<Button>().ToList();
    Require(ReferenceEquals(splash.AcceptButton, splashButtons.FirstOrDefault(control => string.Equals(control.AccessibleName, "Close update splash", StringComparison.OrdinalIgnoreCase))), "Expected Enter to dismiss the update splash.");
    Require(ReferenceEquals(splash.CancelButton, splashButtons.FirstOrDefault(control => string.Equals(control.AccessibleName, "Close update splash", StringComparison.OrdinalIgnoreCase))), "Expected Escape to dismiss the update splash.");
    Require(splash.SummaryText.Contains("visible control", StringComparison.OrdinalIgnoreCase), $"Expected update splash summary to reflect manifest summary, got '{splash.SummaryText}'.");
    Require(splash.NarrationText.Contains("Callsign update 1.4.0a", StringComparison.OrdinalIgnoreCase), $"Expected update splash narration to include the version, got '{splash.NarrationText}'.");
    Require(splash.NarrationText.Contains("visible control commands", StringComparison.OrdinalIgnoreCase), $"Expected update splash narration to include the manifest summary, got '{splash.NarrationText}'.");
    Require(splash.NarrationText.Contains("Added 1", StringComparison.OrdinalIgnoreCase), $"Expected update splash narration to include added count, got '{splash.NarrationText}'.");
    Require(splash.NarrationText.Contains("Pack changes 1", StringComparison.OrdinalIgnoreCase), $"Expected update splash narration to include pack-change count, got '{splash.NarrationText}'.");
    Require(splash.CueText.Contains("dismiss update splash", StringComparison.OrdinalIgnoreCase), $"Expected update splash cue to include spoken dismissal phrases, got '{splash.CueText}'.");
    Require(splash.CueText.Contains("policy and entitlement", StringComparison.OrdinalIgnoreCase), $"Expected update splash cue to preserve policy and entitlement gates, got '{splash.CueText}'.");
    Require(splash.DetailsText.Contains("show-numbers", StringComparison.OrdinalIgnoreCase), $"Expected update splash details to include added command info, got '{splash.DetailsText}'.");
    Require(splash.DetailsText.Contains("- show-numbers", StringComparison.OrdinalIgnoreCase), $"Expected update splash details to use ASCII bullets, got '{splash.DetailsText}'.");
    Require(splash.DetailsText.Contains("sample-pack", StringComparison.OrdinalIgnoreCase), $"Expected update splash details to include pack change info, got '{splash.DetailsText}'.");
}

static void UpdateCheckFailureDoesNotAdvanceDueWindow()
{
    Console.WriteLine("BEGIN update failure test");
    using var client = new HttpClient(new StaticResponseHandler(HttpStatusCode.InternalServerError, "{\"error\":\"not ready\"}"));
    var statePath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.Update", Guid.NewGuid().ToString("N"), "updates-state.json");
    var service = new UpdateCheckService(serverUrl: "http://localhost:5087", checkInterval: TimeSpan.FromHours(25), httpClient: client, statePath: statePath);
    SetField(service, "_installedExecutablePath", string.Empty);
    Require(service.IsCheckDue(DateTimeOffset.UtcNow), "Fresh update service should treat the check as due.");

    var result = service.CheckForUpdateAsync(force: true, attemptInstall: false).GetAwaiter().GetResult();

    Require(!result.Succeeded, $"Expected failed update check against a local error response, got success message '{result.Message}'.");
    Require(service.IsCheckDue(DateTimeOffset.UtcNow), "A failed update check should not postpone the next due window.");
    Console.WriteLine("END update failure test");
}

static void UpdateCheckSuccessAdvancesDueWindow()
{
    Console.WriteLine("BEGIN update success test");
    var manifestJson = """
    {
      "version": "9.9.9a",
      "installerUrl": "",
      "installerSha256": "",
      "installerSizeBytes": 0,
      "notes": "Voice Access parity hardening.",
      "title": "Voice Access parity hardening.",
      "artifactUrl": "",
      "sizeBytes": 0,
      "publishedUtc": "2026-06-24T12:00:00+00:00"
    }
    """;

    using var client = new HttpClient(new StaticResponseHandler(HttpStatusCode.OK, manifestJson));
    var statePath = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.Update", Guid.NewGuid().ToString("N"), "updates-state.json");
    var service = new UpdateCheckService(serverUrl: "http://localhost:5087", checkInterval: TimeSpan.FromHours(25), httpClient: client, statePath: statePath);
    SetField(service, "_installedExecutablePath", string.Empty);
    Require(service.IsCheckDue(DateTimeOffset.UtcNow), "Fresh update service should treat the check as due.");

    var result = service.CheckForUpdateAsync(force: true, attemptInstall: false).GetAwaiter().GetResult();

    Require(result.Succeeded, $"Expected update check to succeed against the local manifest server, got '{result.Message}'.");
    Require(result.UpdateAvailable, "Expected the local manifest to be treated as an available update.");
    Require(result.Manifest != null, "Expected the local manifest to be captured in the result.");
    Require(string.Equals(result.Manifest!.Version, "9.9.9a", StringComparison.OrdinalIgnoreCase), $"Expected the manifest version to round-trip, got '{result.Manifest.Version}'.");
    Require(!service.IsCheckDue(DateTimeOffset.UtcNow), "A successful update check should advance the due window.");
    Console.WriteLine("END update success test");
}

static void UpdateTimerUsesTwentyFiveHourCadenceAndStartupForcesCheck()
{
    using var form = new MainForm();
    var timerField = typeof(MainForm).GetField("_updateCheckTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    Require(timerField != null, "MainForm should expose the update-check timer field.");

    var timer = timerField!.GetValue(form) as System.Windows.Forms.Timer;
    Require(timer != null, "MainForm should build the update-check timer during construction.");
    Require(timer!.Interval == (int)UpdateCheckService.DefaultCheckInterval.TotalMilliseconds, $"Expected update timer interval to match the 25-hour cadence, got {timer.Interval}.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("_updateCheckTimer.Tick += async (_, _) => await CheckForUpdatesAsync(force: false);", StringComparison.OrdinalIgnoreCase), "MainForm should run periodic update checks without forcing installs.");
    Require(mainFormSource.Contains("_ = CheckForUpdatesAsync(force: true, attemptInstall: true);", StringComparison.OrdinalIgnoreCase), "MainForm should force an update check on startup so it phones home when launched.");
    Require(mainFormSource.Contains("BuildUpdatesTab()", StringComparison.OrdinalIgnoreCase), "MainForm should expose an Updates tab for visible update state.");
    Require(mainFormSource.Contains("RefreshUpdatesPanel()", StringComparison.OrdinalIgnoreCase), "MainForm should refresh the visible updates panel after checks.");
}

static void UpdateCheckServiceStatusIncludesCadenceAndNextDue()
{
    var statePath = Path.Combine(Path.GetTempPath(), $"callsign-update-state-{Guid.NewGuid():N}.json");
    var downloadPath = Path.Combine(Path.GetTempPath(), $"callsign-update-downloads-{Guid.NewGuid():N}");

    try
    {
        var client = new HttpClient(new StaticResponseHandler(HttpStatusCode.OK, "{}"));
        var service = new UpdateCheckService("stable", "https://updates.example.test", TimeSpan.FromHours(25), client, statePath);

        var status = service.DescribeStatus(DateTimeOffset.UtcNow);
        Require(status.Contains("Server https://updates.example.test", StringComparison.OrdinalIgnoreCase), $"Expected server URL in update status, got '{status}'.");
        Require(status.Contains("cadence every 25 hours", StringComparison.OrdinalIgnoreCase), $"Expected cadence in update status, got '{status}'.");
        Require(status.Contains("next due", StringComparison.OrdinalIgnoreCase), $"Expected next-due information in update status, got '{status}'.");
    }
    finally
    {
        if (File.Exists(statePath))
            File.Delete(statePath);
    }
}

static void StartupWalkthroughPresentsCleanInstallSteps()
{
    using var walkthrough = new StartupWalkthroughForm(_ => { });

    RequireVisualContract(walkthrough.VisualStyleName, "startup walkthrough");
    Require(walkthrough.FormAccessibleName.Contains("first-run walkthrough", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough form accessibility metadata, got '{walkthrough.FormAccessibleName}'.");
    Require(walkthrough.FormAccessibleDescription.Contains("clean-install walkthrough", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough form accessibility description, got '{walkthrough.FormAccessibleDescription}'.");
    Require(walkthrough.SurfaceAccessibleName.Contains("startup walkthrough", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough surface accessibility metadata, got '{walkthrough.SurfaceAccessibleName}'.");
    Require(walkthrough.TitleAccessibleName.Contains("title", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough title accessibility metadata, got '{walkthrough.TitleAccessibleName}'.");
    Require(walkthrough.CloseButtonAccessibleName.Contains("Close startup walkthrough", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough close-button accessibility metadata, got '{walkthrough.CloseButtonAccessibleName}'.");
    Require(walkthrough.CloseButtonText == "\u00D7", $"Expected walkthrough close glyph to be a clean multiply sign, got '{walkthrough.CloseButtonText}'.");
    Require(walkthrough.SummaryAccessibleName.Contains("summary", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough summary accessibility metadata, got '{walkthrough.SummaryAccessibleName}'.");
    Require(walkthrough.SafetyAccessibleName.Contains("safety and tier summary", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier accessibility metadata, got '{walkthrough.SafetyAccessibleName}'.");
    Require(walkthrough.SafetyAccessibleDescription.Contains("Free parity core", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier description to explain the Free parity core, got '{walkthrough.SafetyAccessibleDescription}'.");
    Require(walkthrough.StatusAccessibleName.Contains("status", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough status accessibility metadata, got '{walkthrough.StatusAccessibleName}'.");
    Require(walkthrough.StepsAccessibleName.Contains("steps", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps accessibility metadata, got '{walkthrough.StepsAccessibleName}'.");
    Require(walkthrough.StepsAccessibleDescription.Contains("voice enrollment", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps accessibility description to mention voice enrollment, got '{walkthrough.StepsAccessibleDescription}'.");
    Require(walkthrough.TitleText.Contains("Start with Callsign", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough title to frame the clean install, got '{walkthrough.TitleText}'.");
    Require(walkthrough.SummaryText.Contains("clean install", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough summary to mention clean install, got '{walkthrough.SummaryText}'.");
    Require(walkthrough.SafetyText.Contains("Free alpha core", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier text to name the Free alpha core, got '{walkthrough.SafetyText}'.");
    Require(walkthrough.SafetyText.Contains("Voice Access parity", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier text to name Voice Access parity, got '{walkthrough.SafetyText}'.");
    Require(walkthrough.SafetyText.Contains("stop listening", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier text to name the visible stop path, got '{walkthrough.SafetyText}'.");
    Require(walkthrough.SafetyText.Contains("Community, Pro, and Advanced packs", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier text to explain extension pack gates, got '{walkthrough.SafetyText}'.");
    Require(walkthrough.SafetyText.Contains("policy-gated", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough safety/tier text to explain policy gating, got '{walkthrough.SafetyText}'.");
    Require(walkthrough.StepsText.Contains("Create or pick a callsign profile", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps to cover profile creation, got '{walkthrough.StepsText}'.");
    Require(walkthrough.StepsText.Contains("Record at least three voice samples", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps to cover enrollment, got '{walkthrough.StepsText}'.");
    Require(walkthrough.StepsText.Contains("visible wake overlay", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps to cover the wake overlay, got '{walkthrough.StepsText}'.");
    Require(walkthrough.StepsText.Contains("Launch an installed app", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps to cover app launch, got '{walkthrough.StepsText}'.");
    Require(walkthrough.StepsText.Contains("Open Shortcuts", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough steps to cover local voice shortcuts, got '{walkthrough.StepsText}'.");
    Require(walkthrough.AccountButtonText.Contains("Open Account", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer an Account jump button, got '{walkthrough.AccountButtonText}'.");
    Require(walkthrough.VoiceButtonText.Contains("Open Voice", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a Voice jump button, got '{walkthrough.VoiceButtonText}'.");
    Require(walkthrough.SessionButtonText.Contains("Open Session", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a Session jump button, got '{walkthrough.SessionButtonText}'.");
    Require(walkthrough.ShortcutsButtonText.Contains("Open Shortcuts", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a Shortcuts jump button, got '{walkthrough.ShortcutsButtonText}'.");
    Require(walkthrough.PacksButtonText.Contains("Open Packs", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a Packs jump button, got '{walkthrough.PacksButtonText}'.");
    Require(walkthrough.AccountButtonAccessibleName.Contains("Open Account", StringComparison.OrdinalIgnoreCase), $"Expected Account button accessibility name, got '{walkthrough.AccountButtonAccessibleName}'.");
    Require(walkthrough.VoiceButtonAccessibleName.Contains("Open Voice", StringComparison.OrdinalIgnoreCase), $"Expected Voice button accessibility name, got '{walkthrough.VoiceButtonAccessibleName}'.");
    Require(walkthrough.SessionButtonAccessibleName.Contains("Open Session", StringComparison.OrdinalIgnoreCase), $"Expected Session button accessibility name, got '{walkthrough.SessionButtonAccessibleName}'.");
    Require(walkthrough.ShortcutsButtonAccessibleName.Contains("Open Shortcuts", StringComparison.OrdinalIgnoreCase), $"Expected Shortcuts button accessibility name, got '{walkthrough.ShortcutsButtonAccessibleName}'.");
    Require(walkthrough.PacksButtonAccessibleName.Contains("Open Packs", StringComparison.OrdinalIgnoreCase), $"Expected Packs button accessibility name, got '{walkthrough.PacksButtonAccessibleName}'.");
    Require(walkthrough.ContinueButtonText.Contains("Continue", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a continue button, got '{walkthrough.ContinueButtonText}'.");
    Require(walkthrough.RemindLaterButtonText.Contains("Remind", StringComparison.OrdinalIgnoreCase), $"Expected walkthrough to offer a remind-later button, got '{walkthrough.RemindLaterButtonText}'.");
    Require(walkthrough.ContinueAccessibleName.Contains("Continue", StringComparison.OrdinalIgnoreCase), $"Expected Continue button accessibility name, got '{walkthrough.ContinueAccessibleName}'.");
    Require(walkthrough.RemindLaterAccessibleName.Contains("Remind", StringComparison.OrdinalIgnoreCase), $"Expected remind-later button accessibility name, got '{walkthrough.RemindLaterAccessibleName}'.");
    var walkthroughButtons = EnumerateControls(walkthrough).OfType<Button>().ToList();
    Require(ReferenceEquals(walkthrough.AcceptButton, walkthroughButtons.FirstOrDefault(control => string.Equals(control.Text, "Continue to Callsign", StringComparison.OrdinalIgnoreCase))), "Expected Enter to trigger the walkthrough continue action.");
    Require(ReferenceEquals(walkthrough.CancelButton, walkthroughButtons.FirstOrDefault(control => string.Equals(control.Text, "Remind me later", StringComparison.OrdinalIgnoreCase))), "Expected Escape to trigger the walkthrough dismiss action.");
}

static void StartupWalkthroughIsReachableFromAccountTab()
{
    using var form = new MainForm();
    var tabsField = typeof(MainForm).GetField("_tabs", BindingFlags.Instance | BindingFlags.NonPublic);
    Require(tabsField != null, "MainForm should expose the tab control field for walkthrough discovery.");
    var tabs = tabsField!.GetValue(form) as TabControl;
    Require(tabs != null, "MainForm should build the tab control during construction.");

    var accountTab = tabs!.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals(page.Text, "Account", StringComparison.OrdinalIgnoreCase));
    Require(accountTab != null, "MainForm should include the Account tab.");

    var gettingStartedButton = EnumerateControls(accountTab!)
        .OfType<Button>()
        .FirstOrDefault(control => string.Equals(control.Text, "Getting Started", StringComparison.OrdinalIgnoreCase));

    if (gettingStartedButton is not Button button)
    {
        var allButtons = EnumerateControls(accountTab!)
            .OfType<Button>()
            .Select(control => control.Text)
            .ToArray();
        Require(false, $"Expected a Getting Started button on the Account tab, but found: {string.Join(", ", allButtons)}");
        return;
    }

    Require(button.Enabled, "Getting Started button should be enabled on the Account tab.");
}

static void AlphaV1ChecklistVerifiesWalkthroughArtifacts()
{
    var repoRoot = FindRepositoryRoot();
    var scriptPath = Path.Combine(repoRoot, "scripts", "alpha_v1_checklist.ps1");
    Require(File.Exists(scriptPath), $"Could not find alpha v1 checklist at {scriptPath}.");

    var source = File.ReadAllText(scriptPath);
    Require(source.Contains("[switch]$Verify", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should expose a verification mode.");
    Require(source.Contains("[switch]$RunSmoke", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should optionally run the alpha smoke suite.");
    Require(source.Contains("alpha-v1-walkthrough-evidence.json", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should write structured walkthrough evidence.");
    Require(source.Contains("StartupWalkthroughForm.cs", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should verify the walkthrough source exists.");
    Require(source.Contains("macOS Voice Control", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should verify the macOS visual target.");
    Require(source.Contains("Start search", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should verify Start search launch guidance.");
    Require(source.Contains("manual_checks_remaining", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should preserve manual clean-install evidence requirements.");
    Require(source.Contains("public website installer", StringComparison.OrdinalIgnoreCase), "Alpha v1 checklist should keep public website installer verification explicit.");
}

static void VoiceAccessParityEvidenceScriptPreservesReleaseGates()
{
    var repoRoot = FindRepositoryRoot();
    var scriptPath = Path.Combine(repoRoot, "scripts", "voice_access_parity_evidence.ps1");
    Require(File.Exists(scriptPath), $"Could not find Voice Access parity evidence script at {scriptPath}.");

    var source = File.ReadAllText(scriptPath);
    Require(source.Contains("VOICE_ACCESS_PARITY_MATRIX.md", StringComparison.OrdinalIgnoreCase), "Parity evidence script should read the canonical parity matrix.");
    Require(source.Contains("TEST_PLAN.md", StringComparison.OrdinalIgnoreCase), "Parity evidence script should read the release test plan.");
    Require(source.Contains("voice-access-parity.html", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify the generated public parity page.");
    Require(source.Contains("voice-ux.html", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify the generated Voice UX page.");
    Require(source.Contains("tier-architecture.html", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify the generated tier architecture page.");
    Require(source.Contains("security-model.html", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify the generated security model page.");
    Require(source.Contains("Callsign-Setup.exe", StringComparison.OrdinalIgnoreCase), "Parity evidence script should include installer proof.");
    Require(source.Contains("[switch]$RequireManualEvidence", StringComparison.OrdinalIgnoreCase), "Parity evidence script should support a hard manual-evidence gate.");
    Require(source.Contains("[switch]$WriteManualEvidenceTemplate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should write a manual-evidence template.");
    Require(source.Contains("ManualEvidenceTemplatePath", StringComparison.OrdinalIgnoreCase), "Parity evidence script should allow choosing the manual-evidence template path.");
    Require(source.Contains("[switch]$RunSmoke", StringComparison.OrdinalIgnoreCase), "Parity evidence script should optionally run the alpha smoke suite.");
    Require(source.Contains("Test-IsoUtcTimestamp", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate manual evidence timestamps.");
    Require(source.Contains("Test-InstallerDownloadUrl", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the website installer URL.");
    Require(source.Contains("Manual evidence schema is supported", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the manual evidence schema.");
    Require(source.Contains("Manual evidence generated timestamp is valid", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the manual evidence generation timestamp.");
    Require(source.Contains("Manual evidence website download URL targets Callsign installer", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require a Callsign installer download URL.");
    Require(source.Contains("Generated parity page includes category", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated page category coverage.");
    Require(source.Contains("Generated parity page includes visible-status audit contract", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated page audit contract coverage.");
    Require(source.Contains("Generated parity page includes voice-control audit contract", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated page voice-control audit coverage.");
    Require(source.Contains("Generated parity page includes command-level entitlement gating", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated parity page entitlement coverage.");
    Require(source.Contains("Generated Voice UX page includes shared visual contract", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated visual contract coverage.");
    Require(source.Contains("Generated Voice UX page includes macOS Voice Control target", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated visual target coverage.");
    Require(source.Contains("Shared visual style source exists", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify the shared visual style source exists.");
    Require(source.Contains("Shared visual style defines contrast evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify concrete visual contrast tokens.");
    Require(source.Contains("Shared visual style defines translucency evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify concrete visual opacity tokens.");
    Require(source.Contains("Shared visual style defines compact radius evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify concrete visual radius tokens.");
    Require(source.Contains("Shared visual style defines stop-visible evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify visible stop/cancel visual tokens.");
    Require(source.Contains("Generated Voice UX page includes contrast evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX contrast token coverage.");
    Require(source.Contains("Generated Voice UX page includes translucency range evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX opacity token coverage.");
    Require(source.Contains("Generated Voice UX page includes compact radius evidence token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX radius token coverage.");
    Require(source.Contains("Generated Voice UX page includes visible stop affordance token", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX stop affordance coverage.");
    Require(source.Contains("Generated tier page includes command-level entitlement gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier entitlement coverage.");
    Require(source.Contains("Generated security page includes command-level entitlement gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security entitlement coverage.");
    Require(source.Contains("Generated tier page includes invalid command metadata gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier invalid-metadata coverage.");
    Require(source.Contains("Generated security page includes invalid command metadata gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security invalid-metadata coverage.");
    Require(source.Contains("InvalidPack", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require invalid command metadata to be documented.");
    Require(source.Contains("Generated tier page includes registry execution policy gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier registry execution policy coverage.");
    Require(source.Contains("Generated security page includes registry execution policy gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security registry execution policy coverage.");
    Require(source.Contains("Generated tier page includes structured policy outcome metadata", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier structured policy metadata coverage.");
    Require(source.Contains("Generated security page includes structured policy outcome metadata", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security structured policy metadata coverage.");
    Require(source.Contains("PolicyDecision", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require structured policy decision metadata.");
    Require(source.Contains("PolicyApprovalRequirement", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require structured policy approval metadata.");
    Require(source.Contains("PolicyRiskTier", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require structured policy risk metadata.");
    Require(source.Contains("Generated tier page includes paid discovery non-routing status", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier paid-discovery non-routing coverage.");
    Require(source.Contains("Generated Voice UX page includes gated discovery non-routing status", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX gated-discovery coverage.");
    Require(source.Contains("Generated Voice UX page includes command availability column contract", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated Voice UX availability-column coverage.");
    Require(source.Contains("Generated tier page includes background approval gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier background approval coverage.");
    Require(source.Contains("Generated security page includes background approval gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security background approval coverage.");
    Require(source.Contains("Generated tier page includes visible-required gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier visible-required coverage.");
    Require(source.Contains("Generated security page includes visible-required gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security visible-required coverage.");
    Require(source.Contains("Generated tier page includes high-impact privacy approval gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated tier privacy approval coverage.");
    Require(source.Contains("Generated security page includes high-impact privacy approval gate", StringComparison.OrdinalIgnoreCase), "Parity evidence script should verify generated security privacy approval coverage.");
    Require(source.Contains("CallsignVisualStyle", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require the shared visual contract name.");
    Require(source.Contains("bundled inside a Free pack", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve command-level pack boundary proof.");
    Require(source.Contains("paid-tier command may route", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve command-level security boundary proof.");
    Require(source.Contains("will not route", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve paid-discovery non-routing proof.");
    Require(source.Contains("dedicated availability column", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve command-palette availability-column proof.");
    Require(source.Contains("BackgroundAllowedWithApproval", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the background approval policy proof.");
    Require(source.Contains("VisibleRequired", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the visible-required policy proof.");
    Require(source.Contains("Clipboard", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the clipboard privacy approval proof.");
    Require(source.Contains("FileContents", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the file-content privacy approval proof.");
    Require(source.Contains("ScreenshotOrOcr", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the screenshot/OCR privacy approval proof.");
    Require(source.Contains("ExternalData", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve the external-data privacy approval proof.");
    Require(source.Contains("Manual parity evidence has operator", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence operators.");
    Require(source.Contains("Manual parity evidence has environment", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence environments.");
    Require(source.Contains("Manual parity evidence has notes", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence notes.");
    Require(source.Contains("Manual parity evidence has description", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence descriptions.");
    Require(source.Contains("Manual parity evidence description matches canonical prompt", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence descriptions to match the canonical template.");
    Require(source.Contains("Manual parity evidence has evidence_command", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence walkthrough commands.");
    Require(source.Contains("Manual parity evidence command matches canonical prompt", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence walkthrough commands to match the canonical template.");
    Require(source.Contains("Manual parity evidence has expected_result", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence expected-result targets.");
    Require(source.Contains("Manual parity evidence expected_result matches canonical proof target", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require expected results to match the canonical proof target.");
    Require(source.Contains("Manual parity evidence has observed_result for passed check", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require observed results for passed manual checks.");
    Require(source.Contains("Manual parity evidence has artifact references for passed check", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require artifact references for passed manual checks.");
    Require(source.Contains("Manual parity evidence artifact references are valid", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate artifact references for passed manual checks.");
    Require(source.Contains("Manual evidence check ids are unique", StringComparison.OrdinalIgnoreCase), "Parity evidence script should reject duplicate manual evidence check ids.");
    Require(source.Contains("Manual parity evidence categories match canonical matrix coverage", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual evidence category coverage to match the canonical matrix.");
    Require(source.Contains("\"test_machine\"", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require test machine metadata.");
    Require(source.Contains("\"windows_version\"", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require Windows version metadata.");
    Require(source.Contains("\"callsign_version\"", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require Callsign version metadata.");
    Require(source.Contains("public website serves the latest installer", StringComparison.OrdinalIgnoreCase), "Parity evidence script should preserve website installer proof as a release requirement.");
    Require(source.Contains("Manual parity evidence supplied", StringComparison.OrdinalIgnoreCase), "Parity evidence script should explicitly report whether manual evidence was supplied.");
    Require(source.Contains("release_ready", StringComparison.OrdinalIgnoreCase), "Parity evidence script should distinguish local evidence checks from release-ready parity proof.");
    Require(source.Contains("release_blockers", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report blockers that prevent a release parity claim.");
    Require(source.Contains("canonical_manual_evidence", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report canonical manual evidence map coverage.");
    Require(source.Contains("Canonical manual evidence map covers every parity category", StringComparison.OrdinalIgnoreCase), "Parity evidence script should fail when the manual evidence template misses a parity category.");
    Require(source.Contains("categories_covered", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report manual parity categories covered.");
    Require(source.Contains("categories_missing", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report manual parity categories still missing.");
    Require(source.Contains("Manual/live category proof missing", StringComparison.OrdinalIgnoreCase), "Parity evidence script should block release-ready parity for missing category proof.");
    Require(source.Contains("Manual/live parity evidence was not supplied", StringComparison.OrdinalIgnoreCase), "Parity evidence script should block release-ready parity when live manual evidence is missing.");
    Require(source.Contains("callsign.voice_access_parity.manual_evidence.v1", StringComparison.OrdinalIgnoreCase), "Parity evidence script should version the manual evidence schema.");
    Require(source.Contains("requiredManualEvidenceChecks", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require named manual evidence checks.");
    Require(source.Contains("clean_install_public_installer", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require clean-install website evidence.");
    Require(source.Contains("browser_edge_or_chrome_navigation", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require browser walkthrough evidence.");
    Require(source.Contains("help_command_discovery_palette", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require command discovery walkthrough evidence.");
    Require(source.Contains("community_extension_import_manage", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require community extension walkthrough evidence.");
    Require(source.Contains("update_splash_manifest_walkthrough", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require update splash walkthrough evidence.");
    Require(source.Contains("apple_style_visual_polish_walkthrough", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require manual Apple-style visual polish walkthrough evidence.");
    Require(source.Contains("Apple Voice Control-style visual polish walkthrough", StringComparison.OrdinalIgnoreCase), "Parity evidence script should name the visual polish manual evidence check.");
    Require(source.Contains("wake overlay, visible-controls HUD, mouse grid, keyboard overlay, command palette, correction chooser, update splash, and startup walkthrough", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require live visual proof across the core visible surfaces.");
    Require(source.Contains("compact translucent surfaces", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require visual polish artifacts to cover compact translucent surfaces.");
    Require(source.Contains("visible stop/cancel/status affordances", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require visual polish artifacts to cover stop/cancel/status affordances.");
    Require(source.Contains("public_website_installer_hash_match", StringComparison.OrdinalIgnoreCase), "Parity evidence script should require public installer hash evidence.");
    Require(source.Contains("local_installer_sha256", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the local installer hash from manual evidence.");
    Require(source.Contains("website_installer_sha256", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the website installer hash from manual evidence.");
    Require(source.Contains("local_installer_size_bytes", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the local installer size from manual evidence.");
    Require(source.Contains("website_installer_size_bytes", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate the website installer size from manual evidence.");
    Require(source.Contains("manualEvidenceDescriptions", StringComparison.OrdinalIgnoreCase), "Parity evidence script should keep named remaining manual evidence descriptions.");
    Require(source.Contains("manualEvidenceCommands", StringComparison.OrdinalIgnoreCase), "Parity evidence script should keep named manual walkthrough commands.");
    Require(source.Contains("manualEvidenceExpectedResults", StringComparison.OrdinalIgnoreCase), "Parity evidence script should keep named manual proof targets.");
    Require(source.Contains("manualEvidenceCategories", StringComparison.OrdinalIgnoreCase), "Parity evidence script should map manual walkthroughs to matrix categories.");
    Require(source.Contains("evidence_command", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include a per-check walkthrough command.");
    Require(source.Contains("expected_result", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include a per-check proof target.");
    Require(source.Contains("observed_result", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include a per-check observed result.");
    Require(source.Contains("artifacts", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include per-check artifact references.");
    Require(source.Contains("Test-ArtifactReference", StringComparison.OrdinalIgnoreCase), "Parity evidence script should validate artifact reference shape.");
    Require(source.Contains("Get-DuplicateManualEvidenceCheckIds", StringComparison.OrdinalIgnoreCase), "Parity evidence script should detect duplicated manual evidence check ids.");
    Require(source.Contains("parity_categories", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include per-check matrix categories.");
    Require(source.Contains("missing an evidence_command walkthrough prompt", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report missing walkthrough prompts.");
    Require(source.Contains("missing an expected_result proof target", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report missing expected-result proof targets.");
    Require(source.Contains("marked passed but is missing an observed_result", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report passed checks without observed results.");
    Require(source.Contains("marked passed but has no artifact references", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report passed checks without artifacts.");
    Require(source.Contains("marked passed but has invalid artifact references", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report passed checks with invalid artifacts.");
    Require(source.Contains("Manual evidence contains duplicate check ids", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report duplicate manual evidence check ids as blockers.");
    Require(source.Contains("description does not match the canonical template", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report stale manual evidence descriptions.");
    Require(source.Contains("evidence_command does not match the canonical template", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report stale manual walkthrough prompts.");
    Require(source.Contains("expected_result does not match the canonical proof target", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report stale expected-result proof targets.");
    Require(source.Contains("show numbers/show grid", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include visible-controls walkthrough instructions.");
    Require(source.Contains("Compare SHA-256 and size", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include public installer hash comparison instructions.");
    Require(source.Contains("Say what can I say", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include command discovery walkthrough instructions.");
    Require(source.Contains("Import a community DLL or folder", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include community extension walkthrough instructions.");
    Require(source.Contains("Load an update manifest", StringComparison.OrdinalIgnoreCase), "Parity evidence template should include update splash walkthrough instructions.");
    Require(source.Contains("Manual evidence local installer hash does not match the current Callsign-Setup.exe", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report remaining hash mismatch proof.");
    Require(source.Contains("Manual evidence website installer hash does not match the current Callsign-Setup.exe", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report website hash mismatch proof.");
    Require(source.Contains("Manual evidence local installer size does not match the current Callsign-Setup.exe", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report local size mismatch proof.");
    Require(source.Contains("Manual evidence website installer size does not match the current Callsign-Setup.exe", StringComparison.OrdinalIgnoreCase), "Parity evidence script should report website size mismatch proof.");
    Require(source.Contains("Clean install from the public website installer", StringComparison.OrdinalIgnoreCase), "Parity evidence script should keep clean-install manual proof explicit.");
    Require(source.Contains("Voice access controls", StringComparison.OrdinalIgnoreCase), "Parity evidence script should enumerate the Voice Access controls category.");
    Require(source.Contains("Correction alternatives", StringComparison.OrdinalIgnoreCase), "Parity evidence script should enumerate dictation correction parity.");
    Require(source.Contains("Safe system/settings control", StringComparison.OrdinalIgnoreCase), "Parity evidence script should enumerate safe settings parity.");
}

static void VoiceTabExplainsEnrollmentNextStepsAndFailures()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.VoiceTabSetup", Guid.NewGuid().ToString("N"));
    var store = new ProfileStore(root);
    UserProfile? profile = null;
    try
    {
        using var form = new MainForm(store);
        var activeProfileField = typeof(MainForm).GetField("_activeProfile", BindingFlags.Instance | BindingFlags.NonPublic);
        var refreshVoicePanelMethod = typeof(MainForm).GetMethod("RefreshVoicePanel", BindingFlags.Instance | BindingFlags.NonPublic);
        var nextStepProperty = typeof(MainForm).GetProperty("VoiceNextStepText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var failureProperty = typeof(MainForm).GetProperty("VoiceFailureText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Require(activeProfileField != null, "MainForm should expose active profile state for voice panel refresh.");
        Require(refreshVoicePanelMethod != null, "MainForm should expose a voice panel refresh method.");
        Require(nextStepProperty != null, "MainForm should expose voice next-step text.");
        Require(failureProperty != null, "MainForm should expose voice failure text.");

        activeProfileField!.SetValue(form, null);
        refreshVoicePanelMethod!.Invoke(form, null);
        var noProfileNextStep = nextStepProperty!.GetValue(form)?.ToString() ?? string.Empty;
        var noProfileFailure = failureProperty!.GetValue(form)?.ToString() ?? string.Empty;
        Require(noProfileNextStep.Contains("create or pick a profile", StringComparison.OrdinalIgnoreCase), $"Expected voice tab to explain the first step, got '{noProfileNextStep}'.");
        Require(noProfileFailure.Contains("none yet", StringComparison.OrdinalIgnoreCase), $"Expected voice tab to show a neutral failure state, got '{noProfileFailure}'.");

        profile = new UserProfile
        {
            Callsign = "alpha",
            Settings =
            {
                VoiceSamplesRequired = 3,
                VoiceSamplesRecorded = 0,
                VoiceEnrollmentStatus = "Not activated"
            }
        };
        store.Save(profile);
        for (var index = 1; index <= 3; index++)
        {
            var path = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, index);
            WriteTone(path, 180 + (index * 10), 0.40);
        }

        activeProfileField.SetValue(form, profile);
        refreshVoicePanelMethod.Invoke(form, null);

        var sampleNextStep = nextStepProperty.GetValue(form)?.ToString() ?? string.Empty;
        var sampleFailure = failureProperty.GetValue(form)?.ToString() ?? string.Empty;
        Require(sampleNextStep.Contains("enroll voice identity", StringComparison.OrdinalIgnoreCase) || sampleNextStep.Contains("record 3 more fresh sample", StringComparison.OrdinalIgnoreCase),
            $"Expected voice tab to explain missing samples or enrollment readiness, got '{sampleNextStep}'.");
        Require(sampleFailure.Contains("not enough samples yet", StringComparison.OrdinalIgnoreCase) || sampleFailure.Contains("identity runtime", StringComparison.OrdinalIgnoreCase) || sampleFailure.Contains("model cache", StringComparison.OrdinalIgnoreCase),
            $"Expected voice tab to show a useful blocker, got '{sampleFailure}'.");

        profile.Settings.VoiceSamplesRecorded = 3;
        profile.Settings.VoiceEnrollmentStatus = "pyannote setup required";
        store.Save(profile);
        activeProfileField.SetValue(form, profile);
        refreshVoicePanelMethod.Invoke(form, null);

        var runtimeNextStep = nextStepProperty.GetValue(form)?.ToString() ?? string.Empty;
        var runtimeFailure = failureProperty.GetValue(form)?.ToString() ?? string.Empty;
        Require(runtimeNextStep.Contains("enroll voice identity", StringComparison.OrdinalIgnoreCase) || runtimeNextStep.Contains("voice identity is ready", StringComparison.OrdinalIgnoreCase),
            $"Expected voice tab to explain enrollment readiness, got '{runtimeNextStep}'.");
        Require(runtimeFailure.Contains("identity runtime", StringComparison.OrdinalIgnoreCase) || runtimeFailure.Contains("model cache", StringComparison.OrdinalIgnoreCase),
            $"Expected voice tab to identify the identity runtime or model cache blocker, got '{runtimeFailure}'.");

        for (var index = 1; index <= 3; index++)
        {
            var path = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, index);
            WriteTone(path, 260, 0.40);
        }

        var duplicateResult = new VoiceBiometricVerificationService().EnrollFreshSamples(store, profile, VoiceBiometricVerificationService.GetEnrollmentSamplePaths(store, profile).Take(3));
        Require(!duplicateResult.Accepted && duplicateResult.RejectReason == "pyannote_sample_set_not_distinct", $"Expected duplicate sample proof failure, got {duplicateResult.RejectReason}.");
        profile.Settings.VoiceSamplesRecorded = 3;
        profile.Settings.VoiceEnrollmentStatus = duplicateResult.Message;
        store.Save(profile);
        activeProfileField.SetValue(form, profile);
        refreshVoicePanelMethod.Invoke(form, null);

        var duplicateFailure = failureProperty.GetValue(form)?.ToString() ?? string.Empty;
        Require(duplicateFailure.Contains("duplicate voice samples", StringComparison.OrdinalIgnoreCase), $"Expected voice tab to identify duplicate sample proof failure, got '{duplicateFailure}'.");
    }
    finally
    {
        if (profile != null)
            VoiceBiometricVerificationService.ResetEnrollmentArtifacts(store, profile);
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static IEnumerable<Control> EnumerateControls(Control root)
{
    foreach (Control child in root.Controls)
    {
        yield return child;
        foreach (var descendant in EnumerateControls(child))
            yield return descendant;
    }
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
    var nextWindowVerbose = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to the next window", "Callsign", "echo one");
    Require(nextWindowVerbose.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {nextWindowVerbose.Kind}.");
    Require(nextWindowVerbose.Target == "system-next-window", $"Expected system-next-window target, got '{nextWindowVerbose.Target}'.");
    var nextApp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to the next app", "Callsign", "echo one");
    Require(nextApp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {nextApp.Kind}.");
    Require(nextApp.Target == "system-next-window", $"Expected system-next-window target, got '{nextApp.Target}'.");

    var minimizeAllWindows = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one minimize all windows", "Callsign", "echo one");
    Require(minimizeAllWindows.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {minimizeAllWindows.Kind}.");
    Require(minimizeAllWindows.Target == "system-show-desktop", $"Expected system-show-desktop target, got '{minimizeAllWindows.Target}'.");

    var switchApps = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch apps", "Callsign", "echo one");
    Require(switchApps.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {switchApps.Kind}.");
    Require(switchApps.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{switchApps.Target}'.");

    var taskView = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one task view", "Callsign", "echo one");
    Require(taskView.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {taskView.Kind}.");
    Require(taskView.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{taskView.Target}'.");

    var openTaskView = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open task view", "Callsign", "echo one");
    Require(openTaskView.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {openTaskView.Kind}.");
    Require(openTaskView.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{openTaskView.Target}'.");

    var showTaskView = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show task view", "Callsign", "echo one");
    Require(showTaskView.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {showTaskView.Kind}.");
    Require(showTaskView.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{showTaskView.Target}'.");
    var showAllWindows = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show all windows", "Callsign", "echo one");
    Require(showAllWindows.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {showAllWindows.Kind}.");
    Require(showAllWindows.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{showAllWindows.Target}'.");
    var windowSwitcher = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one window switcher", "Callsign", "echo one");
    Require(windowSwitcher.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {windowSwitcher.Kind}.");
    Require(windowSwitcher.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{windowSwitcher.Target}'.");

    var taskSwitcher = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one task switcher", "Callsign", "echo one");
    Require(taskSwitcher.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {taskSwitcher.Kind}.");
    Require(taskSwitcher.Target == "system-open-task-view", $"Expected system-open-task-view target, got '{taskSwitcher.Target}'.");

    var quickSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one quick settings", "Callsign", "echo one");
    Require(quickSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {quickSettings.Kind}.");
    Require(quickSettings.Target == "system-open-quick-settings", $"Expected system-open-quick-settings target, got '{quickSettings.Target}'.");

    var notificationCenter = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one notification center", "Callsign", "echo one");
    Require(notificationCenter.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {notificationCenter.Kind}.");
    Require(notificationCenter.Target == "system-open-notification-center", $"Expected system-open-notification-center target, got '{notificationCenter.Target}'.");

    var emojiPanel = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one emoji panel", "Callsign", "echo one");
    Require(emojiPanel.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {emojiPanel.Kind}.");
    Require(emojiPanel.Target == "system-open-emoji-panel", $"Expected system-open-emoji-panel target, got '{emojiPanel.Target}'.");

    var clipboardHistory = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one clipboard history", "Callsign", "echo one");
    Require(clipboardHistory.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {clipboardHistory.Kind}.");
    Require(clipboardHistory.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{clipboardHistory.Target}'.");
    var clipboardPicker = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show clipboard picker", "Callsign", "echo one");
    Require(clipboardPicker.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {clipboardPicker.Kind}.");
    Require(clipboardPicker.Target == "system-open-clipboard-history", $"Expected system-open-clipboard-history target, got '{clipboardPicker.Target}'.");

    var snippingToolbar = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one snipping toolbar", "Callsign", "echo one");
    Require(snippingToolbar.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {snippingToolbar.Kind}.");
    Require(snippingToolbar.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{snippingToolbar.Target}'.");
    var screenshotTools = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open screenshot tools", "Callsign", "echo one");
    Require(screenshotTools.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {screenshotTools.Kind}.");
    Require(screenshotTools.Target == "system-open-snipping-toolbar", $"Expected system-open-snipping-toolbar target, got '{screenshotTools.Target}'.");

    var projectDisplay = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one project display", "Callsign", "echo one");
    Require(projectDisplay.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {projectDisplay.Kind}.");
    Require(projectDisplay.Target == "system-open-project-display", $"Expected system-open-project-display target, got '{projectDisplay.Target}'.");

    var castDisplay = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one cast display", "Callsign", "echo one");
    Require(castDisplay.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {castDisplay.Kind}.");
    Require(castDisplay.Target == "system-open-cast-display", $"Expected system-open-cast-display target, got '{castDisplay.Target}'.");

    var previousWindowVerbose = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to the previous window", "Callsign", "echo one");
    Require(previousWindowVerbose.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {previousWindowVerbose.Kind}.");
    Require(previousWindowVerbose.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousWindowVerbose.Target}'.");
    var previousApp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to the previous app", "Callsign", "echo one");
    Require(previousApp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {previousApp.Kind}.");
    Require(previousApp.Target == "system-previous-window", $"Expected system-previous-window target, got '{previousApp.Target}'.");

    var minimizeWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one minimize window", "Callsign", "echo one");
    Require(minimizeWindow.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {minimizeWindow.Kind}.");
    Require(minimizeWindow.Target == "system-minimize-window", $"Expected system-minimize-window target, got '{minimizeWindow.Target}'.");

    var pressEnter = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press enter", "Callsign", "echo one");
    Require(pressEnter.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressEnter.Kind}.");
    Require(pressEnter.Target == "system-press-enter", $"Expected system-press-enter target, got '{pressEnter.Target}'.");

    var pressSpace = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press space", "Callsign", "echo one");
    Require(pressSpace.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressSpace.Kind}.");
    Require(pressSpace.Target == "system-press-space", $"Expected system-press-space target, got '{pressSpace.Target}'.");

    var pressFunctionKey = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one function key twelve", "Callsign", "echo one");
    Require(pressFunctionKey.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressFunctionKey.Kind}.");
    Require(pressFunctionKey.Target == "system-press-f12", $"Expected system-press-f12 target, got '{pressFunctionKey.Target}'.");

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

    var goNextWord = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one go to next word", "Callsign", "echo one");
    Require(goNextWord.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {goNextWord.Kind}.");
    Require(goNextWord.Target == "system-move-next-word", $"Expected system-move-next-word target, got '{goNextWord.Target}'.");

    var movePreviousSentence = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one system move previous sentence", "Callsign", "echo one");
    Require(movePreviousSentence.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {movePreviousSentence.Kind}.");
    Require(movePreviousSentence.Target == "system-move-previous-sentence", $"Expected system-move-previous-sentence target, got '{movePreviousSentence.Target}'.");

    var goNextParagraph = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one go to next paragraph", "Callsign", "echo one");
    Require(goNextParagraph.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {goNextParagraph.Kind}.");
    Require(goNextParagraph.Target == "system-move-next-paragraph", $"Expected system-move-next-paragraph target, got '{goNextParagraph.Target}'.");
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

    var overlaySequence = new[]
    {
        OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForIdentity, "echo one", transcriptConfidence: 0.93f),
        OverlayReadoutFormatter.FormatReadout(AlphaSessionState.WaitingForCommand, verifiedCallsign: "echo one", speechActive: true),
        OverlayReadoutFormatter.FormatReadout(AlphaSessionState.ReadyToLaunch, pendingCommand: "open notepad"),
        OverlayReadoutFormatter.FormatReadout(AlphaSessionState.Launching, pendingApp: "Notepad")
    };
    Require(overlaySequence[0] == "Heard: echo one (93 %)" || overlaySequence[0] == "Heard: echo one (93%)", $"Expected identity transcript with confidence, got '{overlaySequence[0]}'.");
    Require(overlaySequence[1] == "Hearing your command...", $"Expected command listening readout, got '{overlaySequence[1]}'.");
    Require(overlaySequence[2] == "Command: open notepad", $"Expected ready command readout, got '{overlaySequence[2]}'.");
    Require(overlaySequence[3] == "Launching Notepad...", $"Expected launching readout, got '{overlaySequence[3]}'.");
}

static void WakeOverlayReadoutUpdates()
{
    using var overlay = new WakeOverlayForm();
    Require(overlay.IsReady, "Wake overlay should load the bundled callsign.gif asset.");
    Require(overlay.IsTopMostOverlay, "Wake overlay should be topmost so the wake cue appears above normal windows.");
    Require(overlay.IsNonActivatingOverlay, "Wake overlay should not steal focus when shown.");
    Require(overlay.UsesNoActivateClickThroughStyles, "Wake overlay should use no-activate click-through tool-window styles.");
    Require(overlay.WindowBehaviorSummary.Contains("Topmost no-activate", StringComparison.OrdinalIgnoreCase), $"Expected overlay behavior summary, got '{overlay.WindowBehaviorSummary}'.");
    RequireVisualContract(overlay.VisualStyleName, "wake overlay");
    Require(overlay.TitleAccessibleName.Contains("Wake overlay", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay title accessibility metadata, got '{overlay.TitleAccessibleName}'.");
    Require(overlay.PhaseAccessibleName.Contains("phase", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay phase accessibility metadata, got '{overlay.PhaseAccessibleName}'.");
    Require(overlay.ReadoutAccessibleName.Contains("readout", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay readout accessibility metadata, got '{overlay.ReadoutAccessibleName}'.");
    Require(overlay.SafetyText.Contains("stop, cancel, stop listening, or reset session", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay safety text to include escape phrases, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyText.Contains("Commands stay blocked until identity is confirmed", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay safety text to explain identity gate, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyAccessibleName.Contains("Wake overlay safety", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay safety accessibility metadata, got '{overlay.SafetyAccessibleName}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("escape phrases", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay safety accessibility description to mention escape phrases, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("identity is confirmed", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay safety accessibility description to mention identity gate, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.TranscriptAccessibleName.Contains("transcript", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay transcript accessibility metadata, got '{overlay.TranscriptAccessibleName}'.");
    Require(overlay.ActivityAccessibleName.Contains("microphone", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay activity accessibility metadata, got '{overlay.ActivityAccessibleName}'.");
    Require(overlay.AuthorityAccessibleDescription.Contains("runtime", StringComparison.OrdinalIgnoreCase), $"Expected wake overlay authority accessibility description, got '{overlay.AuthorityAccessibleDescription}'.");

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
    Require(overlay.SubtitleText.Contains("click 2", StringComparison.OrdinalIgnoreCase), $"Expected subtitle to mention the focused number, got '{overlay.SubtitleText}'.");
    Require(overlay.SubtitleText.Contains("double click 2", StringComparison.OrdinalIgnoreCase), $"Expected subtitle to mention double click for the focused number, got '{overlay.SubtitleText}'.");
    Require(overlay.SubtitleText.Contains("right click Voice", StringComparison.OrdinalIgnoreCase), $"Expected subtitle to mention right click for the focused label, got '{overlay.SubtitleText}'.");
    Require(overlay.CueText.Contains("Hearing your command", StringComparison.OrdinalIgnoreCase), $"Expected cue label to show live voice cue, got '{overlay.CueText}'.");
    Require(overlay.HeardText.Contains("open notepad", StringComparison.OrdinalIgnoreCase), $"Expected heard label to show transcript, got '{overlay.HeardText}'.");
    Require(overlay.TargetSummaryText.Contains("3 controls numbered", StringComparison.OrdinalIgnoreCase), $"Expected target summary to show the numbered-control count, got '{overlay.TargetSummaryText}'.");
    Require(overlay.TargetSummaryText.Contains("double click", StringComparison.OrdinalIgnoreCase), $"Expected target summary to mention double click, got '{overlay.TargetSummaryText}'.");
    Require(overlay.TargetSummaryText.Contains("right click", StringComparison.OrdinalIgnoreCase), $"Expected target summary to mention right click, got '{overlay.TargetSummaryText}'.");
    Require(overlay.SafetyText.Contains("numbers act only on visible targets", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety text to explain visible targets, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyText.Contains("Hide or cancel exits without clicking", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety text to explain hide/cancel escape, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyText.Contains("mouse grid", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety text to explain mouse-grid fallback, got '{overlay.SafetyText}'.");
    Require(overlay.ItemsText.Contains("2. Voice (focused)", StringComparison.OrdinalIgnoreCase), $"Expected items list to mark the focused control, got '{overlay.ItemsText}'.");
    RequireVisualContract(overlay.VisualStyleName, "visible-controls overlay");
    Require(overlay.HudBounds.Width <= 430 && overlay.HudBounds.Height <= 360, $"Expected compact HUD bounds, got {overlay.HudBounds}.");
    Require(overlay.OverlayAccessibleName.Contains("Visible controls overlay", StringComparison.OrdinalIgnoreCase), $"Expected overlay accessible name, got '{overlay.OverlayAccessibleName}'.");
    Require(overlay.OverlayAccessibleDescription.Contains("numbered control overlay", StringComparison.OrdinalIgnoreCase), $"Expected overlay accessible description, got '{overlay.OverlayAccessibleDescription}'.");
    Require(overlay.HudAccessibleName.Contains("Visible controls", StringComparison.OrdinalIgnoreCase), $"Expected HUD accessible name, got '{overlay.HudAccessibleName}'.");
    Require(overlay.CueAccessibleName.Contains("voice cue", StringComparison.OrdinalIgnoreCase), $"Expected cue accessible name, got '{overlay.CueAccessibleName}'.");
    Require(overlay.CueAccessibleDescription.Contains("spoken targeting cue", StringComparison.OrdinalIgnoreCase), $"Expected cue accessible description, got '{overlay.CueAccessibleDescription}'.");
    Require(overlay.HeardAccessibleName.Contains("transcript", StringComparison.OrdinalIgnoreCase), $"Expected heard accessible name, got '{overlay.HeardAccessibleName}'.");
    Require(overlay.HeardAccessibleDescription.Contains("what Callsign heard", StringComparison.OrdinalIgnoreCase), $"Expected heard accessible description, got '{overlay.HeardAccessibleDescription}'.");
    Require(overlay.FocusAccessibleName.Contains("focused target", StringComparison.OrdinalIgnoreCase), $"Expected focus accessible name, got '{overlay.FocusAccessibleName}'.");
    Require(overlay.FocusAccessibleDescription.Contains("currently focused", StringComparison.OrdinalIgnoreCase), $"Expected focus accessible description, got '{overlay.FocusAccessibleDescription}'.");
    Require(overlay.SafetyAccessibleName.Contains("Visible controls safety", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety accessible name, got '{overlay.SafetyAccessibleName}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("visible targets", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety accessible description to mention visible targets, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("mouse grid", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls safety accessible description to mention mouse-grid fallback, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.TargetsAccessibleName.Contains("numbered targets", StringComparison.OrdinalIgnoreCase), $"Expected numbered-targets accessible name, got '{overlay.TargetsAccessibleName}'.");
    Require(overlay.TargetsAccessibleDescription.Contains("click, double-click, or right-click", StringComparison.OrdinalIgnoreCase), $"Expected numbered-targets accessible description, got '{overlay.TargetsAccessibleDescription}'.");
    Require(overlay.SummaryAccessibleDescription.Contains("visible number or label", StringComparison.OrdinalIgnoreCase), $"Expected summary accessible description, got '{overlay.SummaryAccessibleDescription}'.");
    Require(overlay.CloseButtonAccessibleName.Contains("Close visible controls overlay", StringComparison.OrdinalIgnoreCase), $"Expected visible-controls close button accessibility name, got '{overlay.CloseButtonAccessibleName}'.");
    Require(overlay.CloseButtonText == "\u00D7", $"Expected visible-controls close glyph to be a clean multiply sign, got '{overlay.CloseButtonText}'.");

    var badgeBounds = VisibleControlsOverlayForm.CalculateBadgeBounds(new Rectangle(10, 10, 100, 30), new Rectangle(0, 0, 800, 600), focused: false);
    Require(badgeBounds.Width == 32 && badgeBounds.Height == 32, $"Expected normal badge to be 32x32, got {badgeBounds}.");
    Require(badgeBounds.Left >= 0 && badgeBounds.Top >= 0, $"Expected badge to stay on screen, got {badgeBounds}.");
    var focusedBadgeBounds = VisibleControlsOverlayForm.CalculateBadgeBounds(new Rectangle(10, 10, 100, 30), new Rectangle(0, 0, 800, 600), focused: true);
    Require(focusedBadgeBounds.Width == 36 && focusedBadgeBounds.Height == 36, $"Expected focused badge to be 36x36, got {focusedBadgeBounds}.");

    var hudBounds = VisibleControlsOverlayForm.CalculateHudBounds(new Rectangle(0, 0, 800, 600), new Size(430, 360));
    Require(hudBounds.Right <= 800 && hudBounds.Top == 18, $"Expected HUD to sit compactly in the viewport, got {hudBounds}.");
    Require(VisibleControlsOverlayForm.FormatTargetSummary(0, 0).Contains("No visible controls", StringComparison.OrdinalIgnoreCase), "Empty overlay target summary should be clear.");
    Require(VisibleControlsOverlayForm.FormatTargetSummary(1, 0).Contains("1 control numbered", StringComparison.OrdinalIgnoreCase), "Single-control target summary should use singular copy.");
    Require(VisibleControlsOverlayForm.FormatTargetSummary(1, 0).Contains("right click one", StringComparison.OrdinalIgnoreCase), "Single-control target summary should teach right-click by spoken number.");
}

static void DesktopVisibleControlsNormalizeLabels()
{
    Require(DesktopVisibleControlService.LabelsMatch("Save as...", "save as"), "Desktop UIA labels should ignore punctuation-like spacing.");
    Require(DesktopVisibleControlService.LabelsMatch("Search_Box", "search box"), "Desktop UIA labels should normalize underscores.");
    Require(DesktopVisibleControlService.LabelsMatch("Open-File", "open file"), "Desktop UIA labels should normalize hyphens.");
    Require(DesktopVisibleControlService.LabelsMatch("Save & Close", "save and close"), "Desktop UIA labels should normalize ampersands.");
    Require(DesktopVisibleControlService.LabelsMatch("Read/Write", "read write"), "Desktop UIA labels should normalize slashes.");
    Require(DesktopVisibleControlService.LabelsMatch("Don't Save", "dont save"), "Desktop UIA labels should normalize apostrophes.");
    Require(DesktopVisibleControlService.LabelsMatch("\"Quoted\" Label", "quoted label"), "Desktop UIA labels should normalize quotes.");
    Require(!DesktopVisibleControlService.LabelsMatch("Delete", "save"), "Desktop UIA labels should not match unrelated commands.");
}

static void DesktopVisibleControlsExposeTaskbarCaptureSupport()
{
    var serviceSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "DesktopVisibleControlService.cs"));
    Require(serviceSource.Contains("TryCaptureTaskbar(", StringComparison.Ordinal), "Desktop visible controls service should expose taskbar capture.");
    Require(serviceSource.Contains("Shell_TrayWnd", StringComparison.Ordinal), "Desktop visible controls service should target the Windows taskbar window class.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("Opened taskbar visible controls summary.", StringComparison.Ordinal), "MainForm should surface taskbar visible-controls status.");
    Require(mainFormSource.Contains("Visible controls for Taskbar", StringComparison.Ordinal), "MainForm should build a dedicated taskbar visible-controls summary.");
}

static void DesktopVisibleControlsExposeNamedWindowCaptureSupport()
{
    var serviceSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "DesktopVisibleControlService.cs"));
    Require(serviceSource.Contains("TryCaptureNamedWindow(", StringComparison.Ordinal), "Desktop visible controls service should expose named-window capture.");
    Require(serviceSource.Contains("EnumWindows(", StringComparison.Ordinal), "Desktop visible controls service should enumerate top-level windows for named-window capture.");

    var routerSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "AlphaCommandRouter.cs"));
    Require(routerSource.Contains("ui-show-visible-controls-window:", StringComparison.Ordinal), "Visible-controls routing should expose a named-window action prefix.");
    Require(routerSource.Contains("show numbers on", StringComparison.OrdinalIgnoreCase), "Visible-controls routing should parse show-numbers-on window commands.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("Opened visible controls summary for", StringComparison.Ordinal), "MainForm should surface named-window visible-controls status.");
    Require(mainFormSource.Contains("Visible controls for", StringComparison.Ordinal), "MainForm should build a named-window visible-controls summary.");
}

static void MouseGridSupportsCurrentWindowScope()
{
    var serviceSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "DesktopVisibleControlService.cs"));
    Require(serviceSource.Contains("TryGetForegroundWindowBounds(", StringComparison.Ordinal), "Desktop visible controls service should expose foreground-window bounds for scoped mouse grid.");

    var routerSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "AlphaCommandRouter.cs"));
    Require(routerSource.Contains("show grid here", StringComparison.OrdinalIgnoreCase), "Mouse-grid routing should include show-grid-here.");
    Require(routerSource.Contains("ui-show-mouse-grid-here", StringComparison.Ordinal), "Mouse-grid routing should expose a dedicated current-window grid action.");
    Require(routerSource.Contains("ui-focus-mouse-grid-shortcut-path:", StringComparison.Ordinal), "Mouse-grid routing should expose a current-scope shortcut path action.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("MouseGridScope.CurrentWindow", StringComparison.Ordinal), "MainForm should support a current-window mouse-grid scope.");
    Require(mainFormSource.Contains("Mouse grid shown for", StringComparison.Ordinal), "MainForm should report window-scoped mouse-grid status.");
    Require(mainFormSource.Contains("FocusMouseGridShortcutPath(", StringComparison.Ordinal), "MainForm should support current-scope mouse-grid shortcut paths.");
}

static void MouseGridSupportsMarkedDragAndUndo()
{
    var routerSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "Services", "AlphaCommandRouter.cs"));
    Require(routerSource.Contains("ui-undo-mouse-grid", StringComparison.Ordinal), "Mouse-grid routing should expose an undo action.");
    Require(routerSource.Contains("ui-mark-mouse-grid", StringComparison.Ordinal), "Mouse-grid routing should expose a mark action.");
    Require(routerSource.Contains("ui-drag-marked-mouse-grid", StringComparison.Ordinal), "Mouse-grid routing should expose a marked-drag action.");
    Require(routerSource.Contains("undo that", StringComparison.OrdinalIgnoreCase), "Mouse-grid routing should include undo-that compatibility.");

    var overlaySource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MouseGridOverlayForm.cs"));
    Require(overlaySource.Contains("A drag start is marked", StringComparison.Ordinal), "Mouse grid overlay should explain when a drag start is marked.");
    Require(overlaySource.Contains("public bool Undo()", StringComparison.Ordinal), "Mouse grid overlay should support reverting to the previous state.");
    Require(overlaySource.Contains("SetMarkedPoint", StringComparison.Ordinal), "Mouse grid overlay should support visible marked drag starts.");

    var mainFormSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("MarkMouseGrid(", StringComparison.Ordinal), "MainForm should support marking a mouse-grid drag start.");
    Require(mainFormSource.Contains("DragMarkedMouseGrid()", StringComparison.Ordinal), "MainForm should support dragging to a marked destination.");
    Require(mainFormSource.Contains("Mouse grid marked", StringComparison.Ordinal), "MainForm should surface mouse-grid mark status.");
}

static void DesktopVisibleControlsPrioritizeActionableTargets()
{
    var root = AutomationElement.RootElement ?? throw new InvalidOperationException("UI Automation root element was not available.");
    var actionable = new DesktopVisibleControlEntry(
        1,
        "Action button",
        new Rectangle(10, 120, 100, 32),
        "button",
        "actionButton",
        true,
        true,
        true,
        root);
    var passive = new DesktopVisibleControlEntry(
        2,
        "Passive label",
        new Rectangle(10, 20, 100, 32),
        "text",
        "passiveLabel",
        false,
        true,
        false,
        root);

    var prioritized = DesktopVisibleControlService.PrioritizeEntries([passive, actionable]);
    Require(prioritized[0].Label == "Action button", $"Expected actionable control first, got '{prioritized[0].Label}'.");
    Require(prioritized[0].Number == 1 || prioritized[0].Number == 2, "Prioritized entry should remain numberable.");
    Require(prioritized[0].IsActionable, "Prioritized entry should be actionable.");
    Require(prioritized[1].Label == "Passive label", $"Expected passive control second, got '{prioritized[1].Label}'.");
}

static void MouseGridOverlayCalculatesNumberedCells()
{
    using var overlay = new MouseGridOverlayForm();
    RequireVisualContract(overlay.VisualStyleName, "mouse grid overlay");
    Require(overlay.CueText.Contains("drag grid", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid cue to describe drag commands, got '{overlay.CueText}'.");
    Require(overlay.OverlayAccessibleName.Contains("Mouse grid overlay", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid overlay accessible name, got '{overlay.OverlayAccessibleName}'.");
    Require(overlay.OverlayAccessibleDescription.Contains("spoken mouse targeting", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid overlay accessible description, got '{overlay.OverlayAccessibleDescription}'.");
    Require(overlay.CueAccessibleName.Contains("Mouse grid", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid cue accessible name, got '{overlay.CueAccessibleName}'.");
    Require(overlay.CueAccessibleDescription.Contains("targeting", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid cue accessible description, got '{overlay.CueAccessibleDescription}'.");
    Require(overlay.SafetyText.Contains("visible pointer actions only", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid safety text to explain visible pointer actions, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyText.Contains("hide grid or cancel exits without acting", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid safety text to explain the escape path, got '{overlay.SafetyText}'.");
    Require(overlay.SafetyAccessibleName.Contains("Mouse grid safety", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid safety accessible name, got '{overlay.SafetyAccessibleName}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("refined or undone before click or drag", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid safety accessibility to mention refine and undo, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.SafetyAccessibleDescription.Contains("exits without acting", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid safety accessibility to mention exit without acting, got '{overlay.SafetyAccessibleDescription}'.");
    Require(overlay.CloseButtonAccessibleName.Contains("Close mouse grid overlay", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid close-button accessibility name, got '{overlay.CloseButtonAccessibleName}'.");
    Require(overlay.CloseButtonText == "\u00D7", $"Expected mouse grid close glyph to be a clean multiply sign, got '{overlay.CloseButtonText}'.");

    var bounds = new Rectangle(0, 0, 900, 600);
    Require(MouseGridOverlayForm.CalculateCellBounds(bounds, 1) == new Rectangle(0, 0, 300, 200), "Grid cell 1 should be top-left.");
    Require(MouseGridOverlayForm.CalculateCellBounds(bounds, 5) == new Rectangle(300, 200, 300, 200), "Grid cell 5 should be centered.");
    Require(MouseGridOverlayForm.CalculateCellBounds(bounds, 9) == new Rectangle(600, 400, 300, 200), "Grid cell 9 should be bottom-right.");
    Require(MouseGridOverlayForm.CalculateCellBounds(bounds, 0).IsEmpty, "Invalid grid cell should be empty.");
    Require(MouseGridOverlayForm.CalculateCellCenter(bounds, 5) == new Point(450, 300), "Grid cell center should be the center of cell 5.");
    Require(MouseGridOverlayForm.CalculateCellCenter(bounds, 0) == Point.Empty, "Invalid grid cell center should be empty.");

    var unevenBounds = new Rectangle(10, 20, 100, 100);
    var bottomRight = MouseGridOverlayForm.CalculateCellBounds(unevenBounds, 9);
    Require(bottomRight.Right == unevenBounds.Right && bottomRight.Bottom == unevenBounds.Bottom, "Bottom-right cell should absorb remainder pixels.");

    var virtualBounds = new Rectangle(0, 0, 1800, 600);
    var displayRegions = MouseGridOverlayForm.CreateDisplayRegions(
        virtualBounds,
        [new Rectangle(0, 0, 900, 600), new Rectangle(900, 0, 900, 600)]);
    Require(displayRegions.Count == 2, $"Expected two display regions, got {displayRegions.Count}.");
    Require(displayRegions[0].Identifier == "A", $"Expected first display identifier A, got '{displayRegions[0].Identifier}'.");
    Require(displayRegions[1].Identifier == "B", $"Expected second display identifier B, got '{displayRegions[1].Identifier}'.");
    Require(MouseGridOverlayForm.TryNormalizeDisplayIdentifier("Bravo", out var normalizedDisplayIdentifier), "Expected Bravo display identifier to normalize.");
    Require(normalizedDisplayIdentifier == "B", $"Expected normalized display identifier B, got '{normalizedDisplayIdentifier}'.");
    Require(MouseGridOverlayForm.ResolveDisplayBounds(displayRegions, "B") == new Rectangle(900, 0, 900, 600), "Display B should resolve to the second display bounds.");
    Require(MouseGridOverlayForm.CalculateGridPathBounds(new Rectangle(0, 0, 900, 600), "114") == new Rectangle(0, 22, 33, 22), "Mouse grid display shortcut 114 should resolve to the left-middle subcell.");

    overlay.ShowGrid(virtualBounds, displayRegions);
    Require(overlay.CueText.Contains("Alpha", StringComparison.OrdinalIgnoreCase), $"Expected multi-display cue to mention Alpha, got '{overlay.CueText}'.");
    var focusedDisplayBounds = overlay.FocusDisplay("B");
    Require(focusedDisplayBounds == new Rectangle(900, 0, 900, 600), $"Expected focused display bounds for B, got {focusedDisplayBounds}.");
    Require(overlay.FocusedDisplayIdentifier == "B", $"Expected overlay to track focused display B, got '{overlay.FocusedDisplayIdentifier}'.");

    overlay.ShowGrid(bounds);
    Require(overlay.CueAccessibleDescription.Contains("before a cell is refined", StringComparison.OrdinalIgnoreCase), $"Expected mouse grid cue accessibility to describe the unrefined grid, got '{overlay.CueAccessibleDescription}'.");
    var refined = overlay.RefineToCell(5);
    Require(refined == new Rectangle(300, 200, 300, 200), $"Expected refined cell 5 bounds, got {refined}.");
    Require(overlay.FocusedCellNumber == 5, $"Expected focused cell tracking to record 5, got '{overlay.FocusedCellNumber}'.");
    Require(overlay.CueText.Contains("refined to 5", StringComparison.OrdinalIgnoreCase), $"Expected refined cue text, got '{overlay.CueText}'.");
    Require(overlay.CueText.Contains("drag grid", StringComparison.OrdinalIgnoreCase), $"Expected refined cue to keep drag commands visible, got '{overlay.CueText}'.");
    Require(overlay.CueAccessibleDescription.Contains("refined to cell 5", StringComparison.OrdinalIgnoreCase), $"Expected refined mouse grid cue accessibility to name cell 5, got '{overlay.CueAccessibleDescription}'.");
    Require(overlay.CueAccessibleDescription.Contains("drag grid 1 to grid 9", StringComparison.OrdinalIgnoreCase), $"Expected refined mouse grid cue accessibility to keep drag instructions, got '{overlay.CueAccessibleDescription}'.");
    Require(overlay.CanUndo, "Refining the mouse grid should create an undoable state.");
    Require(overlay.Undo(), "Refined mouse grid should undo to the previous state.");
    Require(overlay.FocusedCellNumber == null, $"Expected undo to clear the focused cell, got '{overlay.FocusedCellNumber}'.");
    Require(overlay.GridBounds == bounds, $"Expected undo to restore the previous bounds, got {overlay.GridBounds}.");
    overlay.SetMarkedPoint(new Point(450, 300));
    Require(overlay.MarkedPoint == new Point(450, 300), $"Expected marked mouse-grid point to be tracked, got '{overlay.MarkedPoint}'.");
    Require(overlay.CueAccessibleDescription.Contains("marked drag start", StringComparison.OrdinalIgnoreCase), $"Expected marked mouse-grid cue accessibility, got '{overlay.CueAccessibleDescription}'.");
    Require(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Callsign.UI", "MainForm.cs")).Contains("var center = GetRectangleCenter(cellBounds);", StringComparison.Ordinal), "Mouse grid click should target the selected cell bounds, not a nested cell after refinement.");
}

static void KeyboardOverlayPresentsVisibleKeys()
{
    try
    {
        using var overlay = new KeyboardOverlayForm();
        RequireVisualContract(overlay.VisualStyleName, "keyboard overlay");
        Require(overlay.CueText.Contains("press Space", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue to describe key commands, got '{overlay.CueText}'.");
        Require(overlay.CueText.Contains("press F5", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue to describe function-key commands, got '{overlay.CueText}'.");
        Require(overlay.CueText.Contains("hold Shift", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue to describe held modifiers, got '{overlay.CueText}'.");
        Require(overlay.CueText.Contains("release all modifiers", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue to describe modifier release safety, got '{overlay.CueText}'.");
        Require(overlay.OverlayAccessibleName.Contains("Keyboard overlay", StringComparison.OrdinalIgnoreCase), $"Expected keyboard overlay accessible name, got '{overlay.OverlayAccessibleName}'.");
        Require(overlay.OverlayAccessibleDescription.Contains("on-screen keyboard", StringComparison.OrdinalIgnoreCase), $"Expected keyboard overlay accessible description, got '{overlay.OverlayAccessibleDescription}'.");
        Require(overlay.CueAccessibleName.Contains("Keyboard overlay", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue accessible name, got '{overlay.CueAccessibleName}'.");
        Require(overlay.CueAccessibleDescription.Contains("spoken keyboard commands", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue accessible description, got '{overlay.CueAccessibleDescription}'.");
        Require(overlay.CueAccessibleDescription.Contains("held modifiers", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue accessible description to mention held modifiers, got '{overlay.CueAccessibleDescription}'.");
        Require(overlay.CueAccessibleDescription.Contains("release all modifiers", StringComparison.OrdinalIgnoreCase), $"Expected keyboard cue accessible description to mention modifier release safety, got '{overlay.CueAccessibleDescription}'.");
        Require(overlay.SafetyText.Contains("visible foreground app only", StringComparison.OrdinalIgnoreCase), $"Expected keyboard safety text to describe foreground targeting, got '{overlay.SafetyText}'.");
        Require(overlay.SafetyText.Contains("release all modifiers", StringComparison.OrdinalIgnoreCase), $"Expected keyboard safety text to name the release-all safety command, got '{overlay.SafetyText}'.");
        Require(overlay.SafetyAccessibleName.Contains("Keyboard overlay safety", StringComparison.OrdinalIgnoreCase), $"Expected keyboard safety accessible name, got '{overlay.SafetyAccessibleName}'.");
        Require(overlay.SafetyAccessibleDescription.Contains("visible foreground app only", StringComparison.OrdinalIgnoreCase), $"Expected keyboard safety accessible description to mention visible foreground targeting, got '{overlay.SafetyAccessibleDescription}'.");
        Require(overlay.SafetyAccessibleDescription.Contains("Shift, Control, or Alt", StringComparison.OrdinalIgnoreCase), $"Expected keyboard safety accessible description to name bounded held modifiers, got '{overlay.SafetyAccessibleDescription}'.");
        Require(overlay.CloseButtonAccessibleName.Contains("Close keyboard overlay", StringComparison.OrdinalIgnoreCase), $"Expected keyboard close-button accessibility name, got '{overlay.CloseButtonAccessibleName}'.");
        Require(overlay.CloseButtonText == "\u00D7", $"Expected keyboard close glyph to be a clean multiply sign, got '{overlay.CloseButtonText}'.");
        Require(overlay.Keys.Any(key => key.Label == "A"), "Keyboard overlay should include letter keys.");
        Require(overlay.Keys.Any(key => key.Label == "Space" && key.ColumnSpan >= 4), "Keyboard overlay should include a wide Space key.");
        Require(overlay.Keys.Any(key => key.Label == "Enter"), "Keyboard overlay should include Enter.");
        Require(overlay.Keys.Any(key => key.Label == "Backspace"), "Keyboard overlay should include Backspace.");
        Require(overlay.Keys.Any(key => key.Label == "F1"), "Keyboard overlay should include function keys.");
        Require(overlay.Keys.Any(key => key.Label == "F12"), "Keyboard overlay should include F12.");
        Require(overlay.Keys.Any(key => key.Label == "Up"), "Keyboard overlay should include arrow keys.");
        Require(overlay.Keys.Any(key => key.Label == "Left"), "Keyboard overlay should include left arrow key.");

        var screenBounds = new Rectangle(0, 0, 1920, 1080);
        var overlayBounds = KeyboardOverlayForm.CalculateOverlayBounds(screenBounds);
        Require(overlayBounds.Width <= 980, $"Keyboard overlay should cap width, got {overlayBounds.Width}.");
        Require(overlayBounds.Bottom <= screenBounds.Bottom, "Keyboard overlay should fit inside the screen bounds.");
        Require(overlayBounds.Top > screenBounds.Height / 2, "Keyboard overlay should sit near the bottom of the screen.");

        var keys = KeyboardOverlayForm.BuildKeys();
        var aKey = keys.First(key => key.Label == "A");
        var spaceKey = keys.First(key => key.Label == "Space");
        var keyboardBounds = new Rectangle(16, 70, 900, 220);
        var aBounds = KeyboardOverlayForm.CalculateKeyBounds(keyboardBounds, aKey);
        var spaceBounds = KeyboardOverlayForm.CalculateKeyBounds(keyboardBounds, spaceKey);
        Require(!aBounds.IsEmpty, "A key bounds should be calculable.");
        Require(spaceBounds.Width > aBounds.Width * 2, "Space key should be visibly wider than letter keys.");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Keyboard overlay smoke failed:\n" + ex, ex);
    }
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
        LastWakeTransitionSource: AlphaSessionStateMachine.AudioWakeDetectorSource,
        RuntimeAuthorityStatus: "authoritative-user-runtime",
        IsAuthoritativeUserRuntime: true,
        ServiceDictationHistory: new[] { "Hello world", "Open Notepad" });

    var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
    var roundTripped = System.Text.Json.JsonSerializer.Deserialize<RuntimeStateSnapshot>(json)
        ?? throw new InvalidOperationException("Runtime state snapshot did not deserialize.");

    Require(roundTripped.OverlayReadout == "Command: open notepad", "Overlay readout should survive snapshot serialization.");
    Require(roundTripped.LastWakeTransitionSource == AlphaSessionStateMachine.AudioWakeDetectorSource, "Wake transition source should survive snapshot serialization.");
    var recentTranscriptHistory = roundTripped.RecentTranscriptHistory ?? throw new InvalidOperationException("Recent transcript history should survive snapshot serialization.");
    Require(recentTranscriptHistory.Count == 2, "Recent transcript history should survive snapshot serialization.");
    Require(recentTranscriptHistory[0] == "[rehearsal] Callsign womprat", "First transcript history item should be preserved.");
    Require(recentTranscriptHistory[1] == "[rehearsal] open Notepad", "Second transcript history item should be preserved.");
    var serviceDictationHistory = roundTripped.ServiceDictationHistory ?? throw new InvalidOperationException("Service dictation history should survive snapshot serialization.");
    Require(serviceDictationHistory.Count == 2, "Service dictation history should survive snapshot serialization.");
    Require(serviceDictationHistory[0] == "Hello world", "First dictation history item should be preserved.");
    Require(serviceDictationHistory[1] == "Open Notepad", "Second dictation history item should be preserved.");
    Require(roundTripped.RuntimeAuthorityStatus == "authoritative-user-runtime", "Runtime authority status should survive snapshot serialization.");
    Require(roundTripped.IsAuthoritativeUserRuntime == true, "Runtime authority flag should survive snapshot serialization.");

    var repoRoot = FindRepositoryRoot();
    var runtimeControlSource = File.ReadAllText(Path.Combine(repoRoot, "src", "Callsign.UI", "Services", "RuntimeControlFiles.cs"));
    var runtimeWorkerSource = File.ReadAllText(Path.Combine(repoRoot, "src", "Callsign.Service", "CallsignRuntimeWorker.cs"));
    Require(runtimeControlSource.Contains("ClearTranscriptHistoryRequestPath", StringComparison.OrdinalIgnoreCase), "Runtime controls should expose a transcript-history clear request path.");
    Require(runtimeControlSource.Contains("RequestClearTranscriptHistory", StringComparison.OrdinalIgnoreCase), "Runtime controls should allow the UI to request transcript-history clearing.");
    Require(runtimeControlSource.Contains("TryConsumeClearTranscriptHistoryRequest", StringComparison.OrdinalIgnoreCase), "Runtime controls should allow the service to consume transcript-history clear requests.");
    Require(runtimeWorkerSource.Contains("ConsumeClearTranscriptHistoryRequest", StringComparison.OrdinalIgnoreCase), "User runtime should consume transcript-history clear requests.");
    Require(runtimeWorkerSource.Contains("_recentTranscriptHistory.Clear()", StringComparison.OrdinalIgnoreCase), "User runtime should clear recent transcript history.");
    Require(runtimeWorkerSource.Contains("_lastTranscriptText = null", StringComparison.OrdinalIgnoreCase), "User runtime should clear the last transcript text.");
}

static void RuntimeStateMonitorReadsControlledRuntimeState()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    var statePath = Path.Combine(root, "state.json");

    try
    {
        var snapshot = new RuntimeStateSnapshot(
            ServiceState: "Running",
            RuntimeRole: "user-runtime",
            StatusMessage: "Listening",
            ActiveCallsign: "echo one",
            VerifiedCallsign: "echo one",
            PendingCommand: "open notepad",
            PendingApp: "Notepad",
            LastLaunchedApp: "Notepad",
            IsListening: true,
            ModeDescription: "Voice listening",
            UpdatedUtc: DateTime.UtcNow,
            SessionState: "WaitingForCommand",
            CanHearAudio: true,
            RuntimeAuthorityStatus: "authoritative-user-runtime",
            IsAuthoritativeUserRuntime: true);

        File.WriteAllText(statePath, System.Text.Json.JsonSerializer.Serialize(snapshot));

        using var monitor = new RuntimeStateMonitor(statePath);
        var read = monitor.Read() ?? throw new InvalidOperationException("Runtime state monitor did not read the controlled state file.");

        Require(read.ActiveCallsign == "echo one", "Runtime state monitor should read the active callsign.");
        Require(read.PendingApp == "Notepad", "Runtime state monitor should read the pending app.");
        Require(read.CanHearAudio == true, "Runtime state monitor should preserve the hearing-audio flag.");
        Require(read.RuntimeAuthorityStatus == "authoritative-user-runtime", "Runtime state monitor should preserve the authority status.");
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
        }
    }
}

static void RuntimeMicStatusFormatterExplainsAuthoritativeAudio()
{
    var authoritativeSnapshot = new RuntimeStateSnapshot(
        ServiceState: "Running",
        RuntimeRole: "user-runtime",
        StatusMessage: "Listening",
        ActiveCallsign: "echo one",
        VerifiedCallsign: "echo one",
        PendingCommand: null,
        PendingApp: null,
        LastLaunchedApp: null,
        IsListening: true,
        ModeDescription: "Voice listening",
        UpdatedUtc: DateTime.UtcNow,
        SessionState: "WaitingForCommand",
        LastMicrophoneLevelState: "green",
        CanHearAudio: true,
        RuntimeAuthorityStatus: "authoritative-user-runtime");

    var nonAuthoritativeSnapshot = authoritativeSnapshot with
    {
        RuntimeAuthorityStatus = "standby-user-runtime",
        CanHearAudio = true
    };

    var quietSnapshot = authoritativeSnapshot with
    {
        CanHearAudio = false,
        SecondsSinceLastAudioPacket = 1.0
    };

    Require(RuntimeStatusFormatter.FormatMicLevel(authoritativeSnapshot).Contains("Authoritative runtime is hearing audio", StringComparison.OrdinalIgnoreCase), "Authoritative runtime should explain that it is hearing audio.");
    Require(RuntimeStatusFormatter.FormatMicLevel(nonAuthoritativeSnapshot).Contains("Microphone level: green", StringComparison.OrdinalIgnoreCase), "Non-authoritative runtime should fall back to the mic level summary.");
    Require(RuntimeStatusFormatter.FormatMicLevel(quietSnapshot).Contains("below the active threshold", StringComparison.OrdinalIgnoreCase), "Quiet runtime should explain that speech is below threshold.");
}

static void RuntimeHearingProofFormatterShowsMicAndPacketState()
{
    var hearingSnapshot = new RuntimeStateSnapshot(
        ServiceState: "Running",
        RuntimeRole: "user-runtime",
        StatusMessage: "Listening",
        ActiveCallsign: "echo one",
        VerifiedCallsign: "echo one",
        PendingCommand: null,
        PendingApp: null,
        LastLaunchedApp: null,
        IsListening: true,
        ModeDescription: "Voice listening",
        UpdatedUtc: DateTime.UtcNow,
        SessionState: "WaitingForCommand",
        ActiveMicrophoneDeviceName: "Headset Microphone",
        CanHearAudio: true,
        SecondsSinceLastAudioPacket: 0.4,
        RuntimeAuthorityStatus: "authoritative-user-runtime");

    var proof = RuntimeStatusFormatter.FormatHearingProof(hearingSnapshot);
    Require(proof.Contains("CanHearAudio=true", StringComparison.Ordinal), $"Expected hearing proof to expose CanHearAudio=true, got '{proof}'.");
    Require(proof.Contains("Headset Microphone", StringComparison.Ordinal), $"Expected hearing proof to expose active microphone, got '{proof}'.");
    Require(proof.Contains("packet age=0.4s", StringComparison.Ordinal), $"Expected hearing proof to expose packet age, got '{proof}'.");
    Require(proof.Contains("recent audio packets", StringComparison.OrdinalIgnoreCase), $"Expected hearing proof to identify recent packets, got '{proof}'.");
    Require(proof.Contains("authoritative-user-runtime", StringComparison.OrdinalIgnoreCase), $"Expected hearing proof to expose runtime authority, got '{proof}'.");

    var silentProof = RuntimeStatusFormatter.FormatHearingProof(hearingSnapshot with
    {
        CanHearAudio = false,
        SecondsSinceLastAudioPacket = 6.2
    });
    Require(silentProof.Contains("CanHearAudio=false", StringComparison.Ordinal), $"Expected silent proof to expose CanHearAudio=false, got '{silentProof}'.");
    Require(silentProof.Contains("no recent audio packets", StringComparison.OrdinalIgnoreCase), $"Expected silent proof to identify stale packets, got '{silentProof}'.");
}

static void RuntimeAuthorityFormatterExplainsListenerOwnership()
{
    var listeningAuthoritative = new RuntimeStateSnapshot(
        ServiceState: "Running",
        RuntimeRole: "user-runtime",
        StatusMessage: "Listening",
        ActiveCallsign: null,
        VerifiedCallsign: null,
        PendingCommand: null,
        PendingApp: null,
        LastLaunchedApp: null,
        IsListening: true,
        ModeDescription: "Voice listening",
        UpdatedUtc: DateTime.UtcNow,
        SessionState: "Idle",
        RuntimeAuthorityStatus: "authoritative-user-runtime",
        CanHearAudio: true);

    var quietService = listeningAuthoritative with
    {
        RuntimeAuthorityStatus = null,
        CanHearAudio = false,
        IsListening = true
    };

    var idleService = listeningAuthoritative with
    {
        RuntimeAuthorityStatus = null,
        IsListening = false
    };

    Require(RuntimeStatusFormatter.FormatAuthority(listeningAuthoritative, isListening: false, usingLocalPreviewListener: false).Contains("hearing audio", StringComparison.OrdinalIgnoreCase), "Authority formatter should call out the authoritative runtime.");
    Require(RuntimeStatusFormatter.FormatAuthority(quietService, isListening: false, usingLocalPreviewListener: false).Contains("running but silent", StringComparison.OrdinalIgnoreCase), "Authority formatter should explain a quiet background service.");
    Require(RuntimeStatusFormatter.FormatAuthority(idleService, isListening: false, usingLocalPreviewListener: false).Contains("idle", StringComparison.OrdinalIgnoreCase), "Authority formatter should explain an idle background service.");
    Require(RuntimeStatusFormatter.FormatAuthority(null, isListening: true, usingLocalPreviewListener: true).Contains("Local preview listener", StringComparison.OrdinalIgnoreCase), "Authority formatter should call out the local preview listener.");
}

static void RuntimeOwnershipEvaluatorExplainsDuplicateRuntimes()
{
    var startedUtc = DateTime.UtcNow.AddMinutes(-3);
    var authoritativeSnapshot = new RuntimeStateSnapshot(
        ServiceState: "Running",
        RuntimeRole: "user-runtime",
        StatusMessage: "Listening",
        ActiveCallsign: null,
        VerifiedCallsign: null,
        PendingCommand: null,
        PendingApp: null,
        LastLaunchedApp: null,
        IsListening: true,
        ModeDescription: "Voice listening",
        UpdatedUtc: DateTime.UtcNow,
        SessionState: "Idle",
        CanHearAudio: true,
        RuntimeAuthorityStatus: "authoritative-user-runtime",
        CurrentProcessId: 4242,
        ProcessStartedUtc: startedUtc);

    var nonAuthoritativeSnapshot = authoritativeSnapshot with
    {
        CanHearAudio = false,
        RuntimeAuthorityStatus = "standby-user-runtime"
    };

    var authoritative = RuntimeOwnershipService.EvaluateStart(runtimeExeExists: true, authoritativeSnapshot, runningProcessCount: 1);
    Require(authoritative.State == UserRuntimeOwnershipState.AlreadyRunningAuthoritative, $"Expected authoritative duplicate detection, got {authoritative.State}.");
    Require(authoritative.Message.Contains("already authoritative", StringComparison.OrdinalIgnoreCase), $"Expected authoritative duplicate message, got '{authoritative.Message}'.");

    var nonAuthoritative = RuntimeOwnershipService.EvaluateStart(runtimeExeExists: true, nonAuthoritativeSnapshot, runningProcessCount: 1);
    Require(nonAuthoritative.State == UserRuntimeOwnershipState.AlreadyRunningNonAuthoritative, $"Expected non-authoritative duplicate detection, got {nonAuthoritative.State}.");
    Require(nonAuthoritative.Message.Contains("not hearing microphone audio yet", StringComparison.OrdinalIgnoreCase), $"Expected non-authoritative duplicate message, got '{nonAuthoritative.Message}'.");

    var started = RuntimeOwnershipService.EvaluateStart(runtimeExeExists: true, null, runningProcessCount: 0);
    Require(started.State == UserRuntimeOwnershipState.Started, $"Expected start decision, got {started.State}.");
    Require(started.Message.Contains("Requested background user runtime start", StringComparison.OrdinalIgnoreCase), $"Expected start message, got '{started.Message}'.");

    var ownershipProof = RuntimeStatusFormatter.FormatOwnershipProof(authoritativeSnapshot, runningServiceProcessCount: 2);
    Require(ownershipProof.Contains("authoritative-user-runtime", StringComparison.OrdinalIgnoreCase), $"Expected ownership proof to expose authority, got '{ownershipProof}'.");
    Require(ownershipProof.Contains("role=user-runtime", StringComparison.OrdinalIgnoreCase), $"Expected ownership proof to expose role, got '{ownershipProof}'.");
    Require(ownershipProof.Contains("PID=4242", StringComparison.Ordinal), $"Expected ownership proof to expose PID, got '{ownershipProof}'.");
    Require(ownershipProof.Contains("process count=2", StringComparison.OrdinalIgnoreCase), $"Expected ownership proof to expose process count, got '{ownershipProof}'.");
    Require(ownershipProof.Contains("Local\\Callsign.UserRuntime", StringComparison.Ordinal), $"Expected ownership proof to mention duplicate-runtime mutex, got '{ownershipProof}'.");

    var missingProof = RuntimeStatusFormatter.FormatOwnershipProof(null, runningServiceProcessCount: 0);
    Require(missingProof.Contains("no service snapshot", StringComparison.OrdinalIgnoreCase), $"Expected missing ownership proof to identify missing snapshot, got '{missingProof}'.");
}

static void DictationVoiceActionsRecognized()
{
    foreach (var (phrase, expectedAction) in new[]
             {
                 ("copy dictation", DictationVoiceAction.Copy),
                 ("read dictation", DictationVoiceAction.ReadBack),
                 ("read that back", DictationVoiceAction.ReadBack),
                 ("speak text", DictationVoiceAction.ReadBack),
                 ("stop reading", DictationVoiceAction.StopReadBack),
                 ("stop readback", DictationVoiceAction.StopReadBack),
                 ("stop speaking", DictationVoiceAction.StopReadBack),
                 ("paste dictated text", DictationVoiceAction.Paste),
                 ("clear text", DictationVoiceAction.Clear),
                 ("delete all", DictationVoiceAction.Clear),
                 ("select all", DictationVoiceAction.SelectAll),
                 ("highlight all", DictationVoiceAction.SelectAll),
                 ("cut text", DictationVoiceAction.Cut),
                 ("undo that", DictationVoiceAction.Undo),
                 ("revert", DictationVoiceAction.Undo),
                 ("redo that", DictationVoiceAction.Redo),
                 ("select that", DictationVoiceAction.SelectThat),
                 ("highlight last phrase", DictationVoiceAction.SelectThat),
                 ("delete that", DictationVoiceAction.DeleteThat),
                 ("scratch that", DictationVoiceAction.DeleteThat),
                 ("go to start", DictationVoiceAction.GoToStart),
                 ("go to beginning", DictationVoiceAction.GoToStart),
                 ("go to beginning of text", DictationVoiceAction.GoToStart),
                 ("beginning of dictation", DictationVoiceAction.GoToStart),
                 ("go to end", DictationVoiceAction.GoToEnd),
                 ("go to end of text", DictationVoiceAction.GoToEnd),
                 ("end of dictation", DictationVoiceAction.GoToEnd),
                 ("select to start", DictationVoiceAction.SelectToStart),
                 ("select to beginning of text", DictationVoiceAction.SelectToStart),
                 ("select to end", DictationVoiceAction.SelectToEnd),
                 ("select to end of dictation", DictationVoiceAction.SelectToEnd),
                 ("delete to start", DictationVoiceAction.DeleteToStart),
                 ("delete to beginning of text", DictationVoiceAction.DeleteToStart),
                 ("delete to end", DictationVoiceAction.DeleteToEnd),
                 ("delete to end of dictation", DictationVoiceAction.DeleteToEnd),
                 ("go to line start", DictationVoiceAction.GoToLineStart),
                 ("go to line end", DictationVoiceAction.GoToLineEnd),
                 ("go to previous line", DictationVoiceAction.GoToPreviousLine),
                 ("go to next line", DictationVoiceAction.GoToNextLine),
                 ("select to line start", DictationVoiceAction.SelectToLineStart),
                 ("select to line end", DictationVoiceAction.SelectToLineEnd),
                 ("delete to line start", DictationVoiceAction.DeleteToLineStart),
                 ("delete to line end", DictationVoiceAction.DeleteToLineEnd),
                 ("select previous line", DictationVoiceAction.SelectPreviousLine),
                 ("select next line", DictationVoiceAction.SelectNextLine),
                 ("delete previous line", DictationVoiceAction.DeletePreviousLine),
                 ("delete next line", DictationVoiceAction.DeleteNextLine),
                 ("go to paragraph start", DictationVoiceAction.GoToParagraphStart),
                 ("go to paragraph end", DictationVoiceAction.GoToParagraphEnd),
                 ("select to paragraph start", DictationVoiceAction.SelectToParagraphStart),
                 ("select to paragraph end", DictationVoiceAction.SelectToParagraphEnd),
                 ("delete to paragraph start", DictationVoiceAction.DeleteToParagraphStart),
                 ("delete to paragraph end", DictationVoiceAction.DeleteToParagraphEnd),
                 ("new line", DictationVoiceAction.NewLine),
                 ("new paragraph", DictationVoiceAction.NewParagraph),
                 ("new sentence", DictationVoiceAction.NewSentence),
                 ("tab", DictationVoiceAction.Tab),
                 ("delete last word", DictationVoiceAction.DeleteLastWord),
                 ("go to previous word", DictationVoiceAction.GoToPreviousWord),
                 ("go to next word", DictationVoiceAction.GoToNextWord),
                 ("select previous word", DictationVoiceAction.SelectPreviousWord),
                 ("select next word", DictationVoiceAction.SelectNextWord),
                 ("delete previous word", DictationVoiceAction.DeletePreviousWord),
                 ("delete next word", DictationVoiceAction.DeleteNextWord),
                 ("select previous character", DictationVoiceAction.SelectPreviousCharacter),
                 ("select next character", DictationVoiceAction.SelectNextCharacter),
                 ("delete previous character", DictationVoiceAction.DeletePreviousCharacter),
                 ("delete next character", DictationVoiceAction.DeleteNextCharacter),
                 ("go to previous sentence", DictationVoiceAction.GoToPreviousSentence),
                 ("go to next sentence", DictationVoiceAction.GoToNextSentence),
                 ("select previous sentence", DictationVoiceAction.SelectPreviousSentence),
                 ("select next sentence", DictationVoiceAction.SelectNextSentence),
                 ("delete previous sentence", DictationVoiceAction.DeletePreviousSentence),
                 ("delete next sentence", DictationVoiceAction.DeleteNextSentence),
                 ("go to previous paragraph", DictationVoiceAction.GoToPreviousParagraph),
                 ("go to next paragraph", DictationVoiceAction.GoToNextParagraph),
                 ("select previous paragraph", DictationVoiceAction.SelectPreviousParagraph),
                 ("select next paragraph", DictationVoiceAction.SelectNextParagraph),
                 ("delete previous paragraph", DictationVoiceAction.DeletePreviousParagraph),
                 ("delete next paragraph", DictationVoiceAction.DeleteNextParagraph),
                 ("comma", DictationVoiceAction.Comma),
                 ("period", DictationVoiceAction.Period),
                 ("question mark", DictationVoiceAction.QuestionMark),
                 ("exclamation", DictationVoiceAction.ExclamationMark),
                 ("exclamation point", DictationVoiceAction.ExclamationMark),
                 ("semicolon", DictationVoiceAction.Semicolon),
                 ("semi colon", DictationVoiceAction.Semicolon),
                 ("colon", DictationVoiceAction.Colon),
                 ("apostrophe", DictationVoiceAction.Apostrophe),
                 ("quote", DictationVoiceAction.Quote),
                 ("double quote", DictationVoiceAction.Quote),
                 ("quote that", DictationVoiceAction.QuoteThat),
                 ("put that in quotes", DictationVoiceAction.QuoteThat),
                 ("open parenthesis", DictationVoiceAction.OpenParenthesis),
                 ("open parentheses", DictationVoiceAction.OpenParenthesis),
                 ("left paren", DictationVoiceAction.OpenParenthesis),
                 ("close parenthesis", DictationVoiceAction.CloseParenthesis),
                 ("close parentheses", DictationVoiceAction.CloseParenthesis),
                 ("right paren", DictationVoiceAction.CloseParenthesis),
                 ("parenthesize that", DictationVoiceAction.ParenthesizeThat),
                 ("put that in parentheses", DictationVoiceAction.ParenthesizeThat),
                 ("open bracket", DictationVoiceAction.OpenBracket),
                 ("open square bracket", DictationVoiceAction.OpenBracket),
                 ("right bracket", DictationVoiceAction.CloseBracket),
                 ("close square bracket", DictationVoiceAction.CloseBracket),
                 ("bracket that", DictationVoiceAction.BracketThat),
                 ("put that in brackets", DictationVoiceAction.BracketThat),
                 ("open brace", DictationVoiceAction.OpenBrace),
                 ("right curly brace", DictationVoiceAction.CloseBrace),
                 ("brace that", DictationVoiceAction.BraceThat),
                 ("put that in braces", DictationVoiceAction.BraceThat),
                 ("hyphen", DictationVoiceAction.Hyphen),
                 ("dash", DictationVoiceAction.Dash),
                 ("slash", DictationVoiceAction.Slash),
                 ("backslash", DictationVoiceAction.Backslash),
                 ("back slash", DictationVoiceAction.Backslash),
                 ("pipe", DictationVoiceAction.Pipe),
                 ("vertical bar", DictationVoiceAction.Pipe),
                 ("backtick", DictationVoiceAction.Grave),
                 ("tilde", DictationVoiceAction.Tilde),
                 ("underscore", DictationVoiceAction.Underscore),
                 ("plus sign", DictationVoiceAction.Plus),
                 ("equals sign", DictationVoiceAction.Equals),
                 ("number sign", DictationVoiceAction.Hash),
                 ("dollar sign", DictationVoiceAction.Dollar),
                 ("ampersand", DictationVoiceAction.Ampersand),
                 ("percent sign", DictationVoiceAction.Percent),
                 ("caret", DictationVoiceAction.Caret),
                 ("asterisk", DictationVoiceAction.Asterisk),
                 ("at sign", DictationVoiceAction.AtSign),
                 ("space", DictationVoiceAction.Space),
                 ("space bar", DictationVoiceAction.Space),
                 ("no space that", DictationVoiceAction.NoSpaceThat),
                 ("remove space before previous word", DictationVoiceAction.NoSpaceThat)
             })
    {
        Require(AlphaVoiceTranscriptParser.ParseDictationVoiceAction(phrase) == expectedAction, $"Expected dictation action {expectedAction} for '{phrase}'.");
    }

    var repoRoot = FindRepositoryRoot();
    var mainFormSource = File.ReadAllText(Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("TryGetLastDictationPhraseSpan", StringComparison.OrdinalIgnoreCase), "Dictation should select/delete the last phrase from the visible review buffer.");
    Require(mainFormSource.Contains("ReadDictationTextAloud", StringComparison.OrdinalIgnoreCase), "MainForm should read reviewed dictation aloud locally.");
    Require(mainFormSource.Contains("StopDictationReadback", StringComparison.OrdinalIgnoreCase), "MainForm should stop local dictation readback visibly.");
    Require(mainFormSource.Contains("SpeechSynthesizer", StringComparison.OrdinalIgnoreCase), "Dictation readback should use local System.Speech synthesis.");
    Require(mainFormSource.Contains("DictationVoiceAction.ReadBack", StringComparison.OrdinalIgnoreCase), "MainForm should execute read-back dictation commands.");
    Require(mainFormSource.Contains("DictationVoiceAction.StopReadBack", StringComparison.OrdinalIgnoreCase), "MainForm should execute stop-readback dictation commands.");
    Require(mainFormSource.Contains("AccessibleName = \"Dictation read aloud\"", StringComparison.OrdinalIgnoreCase), "Dictation readback should be exposed as a visible accessible control.");
    Require(mainFormSource.Contains("AccessibleName = \"Dictation stop reading\"", StringComparison.OrdinalIgnoreCase), "Dictation readback stop should be exposed as a visible accessible control.");
    Require(mainFormSource.Contains("ReadDictationTextAloud, \"read the dictated text aloud\", captureUndoSnapshot: false", StringComparison.OrdinalIgnoreCase), "Dictation readback should not overwrite review-buffer undo snapshots.");
    Require(mainFormSource.Contains("StopDictationReadback, \"stop reading the dictated text aloud\", captureUndoSnapshot: false", StringComparison.OrdinalIgnoreCase), "Dictation readback stop should not overwrite review-buffer undo snapshots.");
    Require(mainFormSource.Contains("DictationVoiceAction.SelectThat", StringComparison.OrdinalIgnoreCase), "MainForm should execute select-that dictation commands.");
    Require(mainFormSource.Contains("DictationVoiceAction.DeleteThat", StringComparison.OrdinalIgnoreCase), "MainForm should execute delete-that dictation commands.");
    Require(mainFormSource.Contains("GetCurrentParagraphSpan", StringComparison.OrdinalIgnoreCase), "Dictation should select/delete paragraph spans from the visible review buffer.");
    Require(mainFormSource.Contains("DictationVoiceAction.SelectPreviousParagraph", StringComparison.OrdinalIgnoreCase), "MainForm should execute select-previous-paragraph dictation commands.");
    Require(mainFormSource.Contains("DictationVoiceAction.DeleteNextParagraph", StringComparison.OrdinalIgnoreCase), "MainForm should execute delete-next-paragraph dictation commands.");
    Require(mainFormSource.Contains("InsertDictationSpace", StringComparison.OrdinalIgnoreCase), "MainForm should insert explicit dictation spaces into the visible review buffer.");
    Require(mainFormSource.Contains("RemoveSpaceBeforeLastDictationPhrase", StringComparison.OrdinalIgnoreCase), "MainForm should remove spacing before the last dictated phrase in the visible review buffer.");
    Require(mainFormSource.Contains("SelectPreviousDictationCharacter", StringComparison.OrdinalIgnoreCase), "MainForm should select previous dictated characters in the visible review buffer.");
    Require(mainFormSource.Contains("DeleteNextDictationCharacter", StringComparison.OrdinalIgnoreCase), "MainForm should delete next dictated characters in the visible review buffer.");
    Require(mainFormSource.Contains("GetLineTextSpan", StringComparison.OrdinalIgnoreCase), "MainForm should compute deterministic line spans in the visible review buffer.");
    Require(mainFormSource.Contains("DeleteNextDictationLine", StringComparison.OrdinalIgnoreCase), "MainForm should delete next dictated lines in the visible review buffer.");
    Require(mainFormSource.Contains("WrapSelectedOrLastDictationPhrase", StringComparison.OrdinalIgnoreCase), "MainForm should wrap selected or recent dictation with paired punctuation.");
    Require(mainFormSource.Contains("ApplyDictationCasingCommand", StringComparison.OrdinalIgnoreCase), "MainForm should support persistent casing commands for newly dictated text.");
    Require(mainFormSource.Contains("DictationReviewTextService.FormatReviewedSegment", StringComparison.OrdinalIgnoreCase), "MainForm should format dictated text before it reaches the visible review buffer.");
    Require(mainFormSource.Contains("InsertDictationSentenceBreak", StringComparison.OrdinalIgnoreCase), "MainForm should insert visible sentence breaks in the review buffer.");
    Require(mainFormSource.Contains("InsertDictationTab", StringComparison.OrdinalIgnoreCase), "MainForm should insert tabs in the visible review buffer.");
    Require(mainFormSource.Contains("_dictationUndoSnapshot", StringComparison.OrdinalIgnoreCase), "Dictation should keep explicit undo snapshots for review-buffer recovery.");
    Require(mainFormSource.Contains("captureUndoSnapshot: false", StringComparison.OrdinalIgnoreCase), "Dictation undo/redo commands should not overwrite their own recovery snapshots.");
    foreach (var phrase in new[] { "pause dictation", "pause typing", "pause voice typing", "pause taking dictation" })
        Require(AlphaVoiceTranscriptParser.IsPauseDictationCommand(phrase), $"Pause dictation phrase should be recognized: {phrase}");
    foreach (var phrase in new[] { "stop dictation", "stop taking dictation", "stop typing", "stop voice typing", "finish typing" })
        Require(AlphaVoiceTranscriptParser.IsStopDictationCommand(phrase), $"Stop dictation phrase should be recognized: {phrase}");
    Require(AlphaVoiceTranscriptParser.IsPauseDictationCommand("pause typing"), "Pause typing phrase should be recognized.");
    Require(mainFormSource.Contains("PauseDictation();", StringComparison.OrdinalIgnoreCase), "Active dictation should pause visibly without treating pause as dictated text.");
}

static void DictationSpellingCommandsRecognized()
{
    Require(AlphaVoiceTranscriptParser.TryParseDictationInsertTextCommand("type hello world", out var plainTypeText), "Plain type-text command should be recognized.");
    Require(plainTypeText == "hello world", $"Expected hello world, got '{plainTypeText}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationInsertTextCommand("insert text hello comma world", out var punctuationTypeText), "Plain insert-text command should recognize punctuation words.");
    Require(punctuationTypeText == "hello, world", $"Expected hello, world, got '{punctuationTypeText}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationInsertTextCommand("dictate alpha new line bravo", out var multilineTypeText), "Plain dictate-text command should recognize new-line words.");
    Require(multilineTypeText == $"alpha{Environment.NewLine}bravo", $"Expected alpha/newline/bravo, got '{multilineTypeText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell alpha bravo charlie", out var natoSpelling), "NATO spelling command should be recognized.");
    var nato = natoSpelling ?? throw new InvalidOperationException("NATO spelling command should be recognized.");
    Require(nato.Text == "abc", $"Expected abc, got '{nato.Text}'.");
    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell it out w o m p r a t", out var spelledOut), "Spell it out command should be recognized.");
    var spelled = spelledOut ?? throw new InvalidOperationException("Spell it out command should be recognized.");
    Require(spelled.Text == "womprat", $"Expected womprat, got '{spelled.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("type letter w o m p r a t", out var letterSpelling), "Letter spelling command should be recognized.");
    var letter = letterSpelling ?? throw new InvalidOperationException("Letter spelling command should be recognized.");
    Require(letter.Text == "womprat", $"Expected womprat, got '{letter.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("insert alpha underscore one", out var symbolSpelling), "Symbol spelling command should be recognized.");
    var symbol = symbolSpelling ?? throw new InvalidOperationException("Symbol spelling command should be recognized.");
    Require(symbol.Text == "a_1", $"Expected a_1, got '{symbol.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell support at sign example dot com", out var emailSpelling), "Multi-word symbol spelling command should be recognized.");
    var email = emailSpelling ?? throw new InvalidOperationException("Multi-word symbol spelling command should be recognized.");
    Require(email.Text == "support@example.com", $"Expected support@example.com, got '{email.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("insert open bracket alpha close bracket plus sign one", out var bracketSpelling), "Bracket and operator spelling command should be recognized.");
    var bracket = bracketSpelling ?? throw new InvalidOperationException("Bracket and operator spelling command should be recognized.");
    Require(bracket.Text == "[a]+1", $"Expected [a]+1, got '{bracket.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell capital alpha bravo cap letter charlie", out var capitalSpelling), "Capitalized spelling command should be recognized.");
    var capital = capitalSpelling ?? throw new InvalidOperationException("Capitalized spelling command should be recognized.");
    Require(capital.Text == "AbC", $"Expected AbC, got '{capital.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell capital alpha lowercase bravo lower case letter charlie digit five number six", out var qualifiedSpelling), "Qualified letter and digit spelling command should be recognized.");
    var qualified = qualifiedSpelling ?? throw new InvalidOperationException("Qualified letter and digit spelling command should be recognized.");
    Require(qualified.Text == "Abc56", $"Expected Abc56, got '{qualified.Text}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand("spell womprat", out var wordSpelling), "Single-word spelling command should be recognized.");
    var word = wordSpelling ?? throw new InvalidOperationException("Single-word spelling command should be recognized.");
    Require(word.Text == "womprat", $"Expected womprat, got '{word.Text}'.");

    var repoRoot = FindRepositoryRoot();
    var mainFormSource = File.ReadAllText(Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs"));
    Require(mainFormSource.Contains("DictationInsertTextActionPrefix", StringComparison.OrdinalIgnoreCase), "Verified direct type-text commands should execute through the dictation insert target.");
    Require(mainFormSource.Contains("AppendDictationTranscript(insertedText)", StringComparison.OrdinalIgnoreCase), "Verified direct type-text commands should append only to the visible dictation review buffer.");
}

static void DictationTargetTextCommandsRecognized()
{
    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("select privacy policy", out var selectCommand), "Target-text select command should be recognized.");
    var select = selectCommand ?? throw new InvalidOperationException("Target-text select command should be recognized.");
    Require(select.Action == DictationTargetTextAction.Select, "Target-text select command should use Select action.");
    Require(select.TargetText == "privacy policy", $"Expected privacy policy target, got '{select.TargetText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("delete the phrase privacy policy", out var deleteCommand), "Target-text delete command should be recognized.");
    var delete = deleteCommand ?? throw new InvalidOperationException("Target-text delete command should be recognized.");
    Require(delete.Action == DictationTargetTextAction.Delete, "Target-text delete command should use Delete action.");
    Require(delete.TargetText == "privacy policy", $"Expected privacy policy delete target, got '{delete.TargetText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("replace privacy policy with safety notes", out var replaceCommand), "Target-text replace command should be recognized.");
    var replace = replaceCommand ?? throw new InvalidOperationException("Target-text replace command should be recognized.");
    Require(replace.Action == DictationTargetTextAction.Replace, "Target-text replace command should use Replace action.");
    Require(replace.TargetText == "privacy policy", $"Expected privacy policy replace target, got '{replace.TargetText}'.");
    Require(replace.ReplacementText == "safety notes", $"Expected safety notes replacement, got '{replace.ReplacementText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("fix privacy policy with safety notes", out var fixCommand), "Target-text fix command should be recognized.");
    var fix = fixCommand ?? throw new InvalidOperationException("Target-text fix command should be recognized.");
    Require(fix.Action == DictationTargetTextAction.Replace, "Target-text fix command should use Replace action.");
    Require(fix.TargetText == "privacy policy", $"Expected privacy policy fix target, got '{fix.TargetText}'.");
    Require(fix.ReplacementText == "safety notes", $"Expected safety notes fix replacement, got '{fix.ReplacementText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("go before privacy policy", out var moveBeforeCommand), "Target-text move-before command should be recognized.");
    var moveBefore = moveBeforeCommand ?? throw new InvalidOperationException("Target-text move-before command should be recognized.");
    Require(moveBefore.Action == DictationTargetTextAction.MoveBefore, "Target-text move-before command should use MoveBefore action.");
    Require(moveBefore.TargetText == "privacy policy", $"Expected privacy policy move-before target, got '{moveBefore.TargetText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("move after the phrase privacy policy", out var moveAfterCommand), "Target-text move-after command should be recognized.");
    var moveAfter = moveAfterCommand ?? throw new InvalidOperationException("Target-text move-after command should be recognized.");
    Require(moveAfter.Action == DictationTargetTextAction.MoveAfter, "Target-text move-after command should use MoveAfter action.");
    Require(moveAfter.TargetText == "privacy policy", $"Expected privacy policy move-after target, got '{moveAfter.TargetText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("select from privacy to section", out var rangeSelectCommand), "Target-text range select command should be recognized.");
    var rangeSelect = rangeSelectCommand ?? throw new InvalidOperationException("Target-text range select command should be recognized.");
    Require(rangeSelect.Action == DictationTargetTextAction.Select, "Target-text range select should use Select action.");
    Require(rangeSelect.TargetText == "privacy", $"Expected privacy range start, got '{rangeSelect.TargetText}'.");
    Require(rangeSelect.EndText == "section", $"Expected section range end, got '{rangeSelect.EndText}'.");

    Require(AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("replace from privacy to section with safety notes", out var rangeReplaceCommand), "Target-text range replace command should be recognized.");
    var rangeReplace = rangeReplaceCommand ?? throw new InvalidOperationException("Target-text range replace command should be recognized.");
    Require(rangeReplace.Action == DictationTargetTextAction.Replace, "Target-text range replace should use Replace action.");
    Require(rangeReplace.TargetText == "privacy", $"Expected privacy range replace start, got '{rangeReplace.TargetText}'.");
    Require(rangeReplace.EndText == "section", $"Expected section range replace end, got '{rangeReplace.EndText}'.");
    Require(rangeReplace.ReplacementText == "safety notes", $"Expected safety notes range replacement, got '{rangeReplace.ReplacementText}'.");

    Require(!AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("select previous word", out _), "Scoped selection commands must keep their existing parser path.");
    Require(!AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand("replace previous word with ready", out _), "Scoped replacement commands must keep their existing parser path.");

    Require(DictationTargetTextService.TryFindPhraseSpan("Open the Privacy, Policy section.", "privacy policy", out var start, out var length), "Target-text matching should tolerate punctuation between words.");
    Require(start == 9 && length == "Privacy, Policy".Length, $"Expected punctuation-tolerant span 9/{ "Privacy, Policy".Length }, got {start}/{length}.");
    Require(DictationTargetTextService.TryFindPhraseRangeSpan("Open the Privacy, Policy section today.", "privacy", "section", out var rangeStart, out var rangeLength), "Target-text range matching should find text from start phrase through end phrase.");
    Require(rangeStart == 9 && rangeLength == "Privacy, Policy section".Length, $"Expected range span 9/{ "Privacy, Policy section".Length }, got {rangeStart}/{rangeLength}.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy, Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.Select, "privacy policy"),
        out var selected), "Target-text select should apply.");
    Require(selected.Text == "Open the Privacy, Policy section.", "Target-text select should not change text.");
    Require(selected.SelectionStart == 9 && selected.SelectionLength == "Privacy, Policy".Length, "Target-text select should select the matched phrase.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.Delete, "privacy policy"),
        out var deleted), "Target-text delete should apply.");
    Require(deleted.Text == "Open the  section.", $"Expected phrase deletion, got '{deleted.Text}'.");
    Require(deleted.SelectionStart == 9 && deleted.SelectionLength == 0, "Target-text delete should leave the caret at the deletion point.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.Replace, "privacy policy", "safety notes"),
        out var replaced), "Target-text replace should apply.");
    Require(replaced.Text == "Open the safety notes section.", $"Expected phrase replacement, got '{replaced.Text}'.");
    Require(replaced.SelectionStart == 9 && replaced.SelectionLength == "safety notes".Length, "Target-text replace should select the replacement text.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.MoveBefore, "privacy policy"),
        out var movedBefore), "Target-text move-before should apply.");
    Require(movedBefore.Text == "Open the Privacy Policy section.", "Target-text move-before should not change text.");
    Require(movedBefore.SelectionStart == 9 && movedBefore.SelectionLength == 0, "Target-text move-before should place the caret before the matched phrase.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.MoveAfter, "privacy policy"),
        out var movedAfter), "Target-text move-after should apply.");
    Require(movedAfter.Text == "Open the Privacy Policy section.", "Target-text move-after should not change text.");
    Require(movedAfter.SelectionStart == 9 + "Privacy Policy".Length && movedAfter.SelectionLength == 0, "Target-text move-after should place the caret after the matched phrase.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section today.",
        new DictationTargetTextCommand(DictationTargetTextAction.Select, "privacy", EndText: "section"),
        out var rangeSelected), "Target-text range select should apply.");
    Require(rangeSelected.Text == "Open the Privacy Policy section today.", "Target-text range select should not change text.");
    Require(rangeSelected.SelectionStart == 9 && rangeSelected.SelectionLength == "Privacy Policy section".Length, "Target-text range select should select from start through end phrase.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section today.",
        new DictationTargetTextCommand(DictationTargetTextAction.Delete, "privacy", EndText: "section"),
        out var rangeDeleted), "Target-text range delete should apply.");
    Require(rangeDeleted.Text == "Open the  today.", $"Expected range deletion, got '{rangeDeleted.Text}'.");

    Require(DictationTargetTextService.TryApply(
        "Open the Privacy Policy section today.",
        new DictationTargetTextCommand(DictationTargetTextAction.Replace, "privacy", "safety notes", "section"),
        out var rangeReplaced), "Target-text range replace should apply.");
    Require(rangeReplaced.Text == "Open the safety notes today.", $"Expected range replacement, got '{rangeReplaced.Text}'.");
    Require(rangeReplaced.SelectionStart == 9 && rangeReplaced.SelectionLength == "safety notes".Length, "Target-text range replace should select the replacement text.");

    Require(!DictationTargetTextService.TryApply(
        "Open the Privacy Policy section.",
        new DictationTargetTextCommand(DictationTargetTextAction.Select, "missing phrase"),
        out _), "Target-text commands should fail when the phrase is not in the review buffer.");
}

static void DictationFormattingCommandsRecognized()
{
    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("capitalize previous word", out var capitalizeWord),
        "Capitalize previous word command should be recognized.");
    var capitalize = capitalizeWord ?? throw new InvalidOperationException("Capitalize previous word command should be recognized.");
    Require(capitalize.Scope == DictationReplacementScope.PreviousWord, "Capitalize previous word should target the previous word.");
    Require(capitalize.Format == DictationTextFormat.SentenceCase, "Capitalize previous word should use sentence case.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("uppercase previous sentence", out var uppercaseSentence),
        "Uppercase previous sentence command should be recognized.");
    var uppercase = uppercaseSentence ?? throw new InvalidOperationException("Uppercase previous sentence command should be recognized.");
    Require(uppercase.Scope == DictationReplacementScope.PreviousSentence, "Uppercase previous sentence should target the previous sentence.");
    Require(uppercase.Format == DictationTextFormat.Uppercase, "Uppercase previous sentence should use uppercase.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("lowercase all text", out var lowercaseAll),
        "Lowercase all text command should be recognized.");
    var lowercase = lowercaseAll ?? throw new InvalidOperationException("Lowercase all text command should be recognized.");
    Require(lowercase.Scope == DictationReplacementScope.AllText, "Lowercase all text should target the whole review buffer.");
    Require(lowercase.Format == DictationTextFormat.Lowercase, "Lowercase all text should use lowercase.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("title case previous paragraph", out var titleParagraph),
        "Title case previous paragraph command should be recognized.");
    var titleCase = titleParagraph ?? throw new InvalidOperationException("Title case previous paragraph command should be recognized.");
    Require(titleCase.Scope == DictationReplacementScope.PreviousParagraph, "Title case previous paragraph should target the previous paragraph.");
    Require(titleCase.Format == DictationTextFormat.TitleCase, "Title case previous paragraph should use title case.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("make that uppercase", out var makeThatUppercase),
        "Make-that uppercase command should be recognized.");
    var makeUppercase = makeThatUppercase ?? throw new InvalidOperationException("Make-that uppercase command should be recognized.");
    Require(makeUppercase.Scope == DictationReplacementScope.PreviousWord, "Make-that uppercase should target the previous word.");
    Require(makeUppercase.Format == DictationTextFormat.Uppercase, "Make-that uppercase should use uppercase.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("make previous sentence lower case", out var makeSentenceLowercase),
        "Make previous sentence lower-case command should be recognized.");
    var makeLowercase = makeSentenceLowercase ?? throw new InvalidOperationException("Make previous sentence lower-case command should be recognized.");
    Require(makeLowercase.Scope == DictationReplacementScope.PreviousSentence, "Make previous sentence lower-case should target the previous sentence.");
    Require(makeLowercase.Format == DictationTextFormat.Lowercase, "Make previous sentence lower-case should use lowercase.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationFormatCommand("make all text title case", out var makeAllTitleCase),
        "Make all text title-case command should be recognized.");
    var makeTitleCase = makeAllTitleCase ?? throw new InvalidOperationException("Make all text title-case command should be recognized.");
    Require(makeTitleCase.Scope == DictationReplacementScope.AllText, "Make all text title-case should target the whole review buffer.");
    Require(makeTitleCase.Format == DictationTextFormat.TitleCase, "Make all text title-case should use title case.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCasingCommand("caps on", out var capsOn),
        "Caps-on mode command should be recognized.");
    Require(capsOn?.Mode == DictationCasingMode.Caps, "Caps-on should set sentence-style casing for new dictation.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCasingCommand("all caps on", out var allCapsOn),
        "All-caps mode command should be recognized.");
    Require(allCapsOn?.Mode == DictationCasingMode.AllCaps, "All-caps-on should set uppercase casing for new dictation.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCasingCommand("no caps on", out var noCapsOn),
        "No-caps mode command should be recognized.");
    Require(noCapsOn?.Mode == DictationCasingMode.NoCaps, "No-caps-on should set lowercase casing for new dictation.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCasingCommand("caps off", out var capsOff),
        "Caps-off mode command should be recognized.");
    Require(capsOff?.Mode == DictationCasingMode.Default, "Caps-off should return new dictation to default casing.");

    Require(
        DictationFormattingService.TryApply(
            "please open notepad",
            new DictationFormatCommand(DictationReplacementScope.PreviousWord, DictationTextFormat.SentenceCase),
            out var capitalizedWord),
        "Formatting service should capitalize the previous word.");
    Require(capitalizedWord.Text == "please open Notepad", $"Expected please open Notepad, got '{capitalizedWord.Text}'.");
    Require(capitalizedWord.SelectionStart == "please open ".Length && capitalizedWord.SelectionLength == "Notepad".Length, "Formatted word should remain selected for review.");

    Require(
        DictationFormattingService.TryApply(
            "ready. launch app",
            new DictationFormatCommand(DictationReplacementScope.PreviousSentence, DictationTextFormat.Uppercase),
            out var uppercaseResult),
        "Formatting service should uppercase the previous sentence.");
    Require(uppercaseResult.Text == "ready. LAUNCH APP", $"Expected ready. LAUNCH APP, got '{uppercaseResult.Text}'.");

    Require(
        DictationFormattingService.TryApply(
            "LOUD TEXT",
            new DictationFormatCommand(DictationReplacementScope.AllText, DictationTextFormat.Lowercase),
            out var lowercaseResult),
        "Formatting service should lowercase all reviewed text.");
    Require(lowercaseResult.Text == "LOUD TEXT".ToLowerInvariant(), $"Expected loud text, got '{lowercaseResult.Text}'.");
}

static void DictationCorrectionAlternativesRecognized()
{
    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("correct previous word", out var wordCorrection),
        "Previous-word correction alternatives should be recognized.");
    var word = wordCorrection ?? throw new InvalidOperationException("Previous-word correction alternatives should be recognized.");
    Require(word.Action == DictationCorrectionVoiceAction.ShowAlternatives, "Previous-word correction should show alternatives.");
    Require(word.Scope == DictationReplacementScope.PreviousWord, "Previous-word correction should target the previous word.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("fix that", out var fixThatCorrection),
        "Fix-that correction alternatives should be recognized.");
    var fixThat = fixThatCorrection ?? throw new InvalidOperationException("Fix-that correction alternatives should be recognized.");
    Require(fixThat.Action == DictationCorrectionVoiceAction.ShowAlternatives, "Fix-that correction should show alternatives.");
    Require(fixThat.Scope == DictationReplacementScope.PreviousSentence, "Fix-that correction should target the previous sentence.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("correct all text", out var allTextCorrection),
        "All-text correction alternatives should be recognized.");
    var allText = allTextCorrection ?? throw new InvalidOperationException("All-text correction alternatives should be recognized.");
    Require(allText.Action == DictationCorrectionVoiceAction.ShowAlternatives, "All-text correction should show alternatives.");
    Require(allText.Scope == DictationReplacementScope.AllText, "All-text correction should target the whole review buffer.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("choose correction two", out var chooseCorrection),
        "Correction choice command should be recognized.");
    var choice = chooseCorrection ?? throw new InvalidOperationException("Correction choice command should be recognized.");
    Require(choice.Action == DictationCorrectionVoiceAction.ChooseAlternative, "Correction choice should choose an alternative.");
    Require(choice.ChoiceNumber == 2, "Spoken correction choice should parse to a number.");
    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("choose one", out var chooseOneCorrection),
        "Natural choose-one correction command should be recognized.");
    var chooseOne = chooseOneCorrection ?? throw new InvalidOperationException("Choose-one correction command should be recognized.");
    Require(chooseOne.Action == DictationCorrectionVoiceAction.ChooseAlternative, "Choose-one correction should choose an alternative.");
    Require(chooseOne.ChoiceNumber == 1, $"Expected correction choice 1, got {chooseOne.ChoiceNumber}.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("next correction", out var nextCorrection),
        "Next correction command should be recognized.");
    var next = nextCorrection ?? throw new InvalidOperationException("Next correction command should be recognized.");
    Require(next.Action == DictationCorrectionVoiceAction.NextAlternative, "Next correction should navigate alternatives.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("previous correction", out var previousCorrection),
        "Previous correction command should be recognized.");
    var previous = previousCorrection ?? throw new InvalidOperationException("Previous correction command should be recognized.");
    Require(previous.Action == DictationCorrectionVoiceAction.PreviousAlternative, "Previous correction should navigate alternatives.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("accept correction", out var acceptCorrection),
        "Accept correction command should be recognized.");
    var accept = acceptCorrection ?? throw new InvalidOperationException("Accept correction command should be recognized.");
    Require(accept.Action == DictationCorrectionVoiceAction.AcceptCurrentAlternative, "Accept correction should apply the selected alternative.");
    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("accept that", out var acceptThatCorrection),
        "Natural accept-that correction command should be recognized.");
    var acceptThat = acceptThatCorrection ?? throw new InvalidOperationException("Accept-that correction command should be recognized.");
    Require(acceptThat.Action == DictationCorrectionVoiceAction.AcceptCurrentAlternative, "Accept-that correction should apply the selected alternative.");

    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("cancel correction", out var cancelCorrection),
        "Cancel correction command should be recognized.");
    var cancel = cancelCorrection ?? throw new InvalidOperationException("Cancel correction command should be recognized.");
    Require(cancel.Action == DictationCorrectionVoiceAction.CancelAlternatives, "Cancel correction should cancel alternatives.");
    Require(
        AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand("close correction", out var closeCorrection),
        "Close correction command should be recognized.");
    var close = closeCorrection ?? throw new InvalidOperationException("Close correction command should be recognized.");
    Require(close.Action == DictationCorrectionVoiceAction.CancelAlternatives, "Close correction should dismiss alternatives.");

    var wordSession = DictationCorrectionService.CreateSession("please open note pad", DictationReplacementScope.PreviousWord);
    Require(wordSession.Choices.Count >= 2, "Previous-word correction should produce alternatives.");
    Require(wordSession.Choices[0].Text == "pad", "First correction choice should keep the original word.");

    var phraseSession = DictationCorrectionService.CreateSession("launch app. note pad", DictationReplacementScope.PreviousSentence);
    Require(phraseSession.Choices.Any(choice => choice.Text == "notepad"), "Previous-sentence correction should include a joined-words alternative.");
    var joinedChoice = phraseSession.Choices.First(choice => choice.Text == "notepad");
    Require(
        DictationCorrectionService.TryApplyChoice("launch app. note pad", joinedChoice, out var correctedText, out var selectionStart),
        "Correction choice should apply to the reviewed dictation text.");
    Require(correctedText == "launch app. notepad", $"Expected launch app. notepad, got '{correctedText}'.");
    Require(selectionStart == correctedText.Length, "Correction selection should move to the end of the replacement.");

    var allTextSession = DictationCorrectionService.CreateSession("LAUNCH NOTE PAD", DictationReplacementScope.AllText);
    Require(allTextSession.Choices.Any(choice => choice.Text == "launch note pad"), "All-text correction should include a lowercase whole-buffer alternative.");
    var lowercaseAll = allTextSession.Choices.First(choice => choice.Text == "launch note pad");
    Require(
        DictationCorrectionService.TryApplyChoice("LAUNCH NOTE PAD", lowercaseAll, out var allTextCorrected, out var allTextSelectionStart),
        "All-text correction choice should apply to the reviewed dictation text.");
    Require(allTextCorrected == "launch note pad", $"Expected launch note pad, got '{allTextCorrected}'.");
    Require(allTextSelectionStart == allTextCorrected.Length, "All-text correction selection should move to the end of the replacement.");

    using var form = new DictationCorrectionForm();
    form.ShowCorrections(null!, allTextSession.Choices, DictationReplacementScope.AllText);
    RequireVisualContract(form.VisualStyleName, "correction surface");
    Require(form.SurfaceAccessibleName.Contains("Dictation correction alternatives", StringComparison.OrdinalIgnoreCase), $"Expected correction surface accessible name, got '{form.SurfaceAccessibleName}'.");
    Require(form.SurfaceAccessibleDescription.Contains("numbered alternatives", StringComparison.OrdinalIgnoreCase), $"Expected correction surface accessible description, got '{form.SurfaceAccessibleDescription}'.");
    Require(form.PanelAccessibleName.Contains("Dictation correction surface", StringComparison.OrdinalIgnoreCase), $"Expected correction panel accessible name, got '{form.PanelAccessibleName}'.");
    Require(form.TitleAccessibleName.Contains("correction title", StringComparison.OrdinalIgnoreCase), $"Expected correction title accessible name, got '{form.TitleAccessibleName}'.");
    Require(form.CloseButtonAccessibleName.Contains("Close correction alternatives", StringComparison.OrdinalIgnoreCase), $"Expected correction close-button accessibility name, got '{form.CloseButtonAccessibleName}'.");
    Require(form.CloseButtonText == "\u00D7", $"Expected correction close glyph to be a clean multiply sign, got '{form.CloseButtonText}'.");
    Require(form.ScopeAccessibleName.Contains("correction scope", StringComparison.OrdinalIgnoreCase), $"Expected correction scope accessible name, got '{form.ScopeAccessibleName}'.");
    Require(form.SummaryAccessibleName.Contains("correction summary", StringComparison.OrdinalIgnoreCase), $"Expected correction summary accessible name, got '{form.SummaryAccessibleName}'.");
    Require(form.CueAccessibleName.Contains("correction voice cue", StringComparison.OrdinalIgnoreCase), $"Expected correction cue accessible name, got '{form.CueAccessibleName}'.");
    Require(form.CueAccessibleDescription.Contains("accepting", StringComparison.OrdinalIgnoreCase), $"Expected correction cue accessible description, got '{form.CueAccessibleDescription}'.");
    Require(form.SafetyAccessibleName.Contains("correction safety", StringComparison.OrdinalIgnoreCase), $"Expected correction safety accessible name, got '{form.SafetyAccessibleName}'.");
    Require(form.SafetyAccessibleDescription.Contains("leaves the reviewed dictation text unchanged", StringComparison.OrdinalIgnoreCase), $"Expected correction safety accessible description, got '{form.SafetyAccessibleDescription}'.");
    Require(form.ChoicesAccessibleName.Contains("correction alternatives", StringComparison.OrdinalIgnoreCase), $"Expected correction choices accessible name, got '{form.ChoicesAccessibleName}'.");
    Require(form.ChoicesAccessibleDescription.Contains("chosen by voice", StringComparison.OrdinalIgnoreCase), $"Expected correction choices accessible description, got '{form.ChoicesAccessibleDescription}'.");
    Require(form.ChoiceCount == allTextSession.Choices.Count, $"Expected choice count to match session choices, got {form.ChoiceCount}.");
    Require(form.CueText.Contains("choose correction", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to explain spoken choice commands, got '{form.CueText}'.");
    Require(form.CueText.Contains("next correction", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to explain next command, got '{form.CueText}'.");
    Require(form.CueText.Contains("previous correction", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to explain previous command, got '{form.CueText}'.");
    Require(form.CueText.Contains("accept correction", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to explain accept command, got '{form.CueText}'.");
    Require(form.CueText.Contains("close correction", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to explain close command, got '{form.CueText}'.");
    Require(!form.CueText.Contains("Â", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to avoid mojibake characters, got '{form.CueText}'.");
    Require(form.CueText.Contains("|", StringComparison.OrdinalIgnoreCase), $"Expected correction cue to use plain ASCII separators, got '{form.CueText}'.");
    Require(form.SafetyText.Contains("choose or accept replaces reviewed text", StringComparison.OrdinalIgnoreCase), $"Expected correction safety text to explain replacement, got '{form.SafetyText}'.");
    Require(form.SafetyText.Contains("close or cancel leaves the review buffer unchanged", StringComparison.OrdinalIgnoreCase), $"Expected correction safety text to explain cancel behavior, got '{form.SafetyText}'.");
    Require(form.ScopeText.Contains("all dictated text", StringComparison.OrdinalIgnoreCase), $"Expected correction scope to be visible, got '{form.ScopeText}'.");
    Require(form.SelectedChoiceNumber == "1", $"Expected first correction choice to be selected by default, got '{form.SelectedChoiceNumber}'.");
    Require(form.SelectedChoiceText.Contains("launch note pad", StringComparison.OrdinalIgnoreCase), $"Expected selected correction text to be visible, got '{form.SelectedChoiceText}'.");
    Require(form.MoveSelectionByVoice(1), "Voice correction navigation should move to the next alternative.");
    Require(form.SelectedChoiceNumber == "2", $"Expected second correction choice after next command, got '{form.SelectedChoiceNumber}'.");
    Require(form.MoveSelectionByVoice(-1), "Voice correction navigation should move to the previous alternative.");
    Require(form.SelectedChoiceNumber == "1", $"Expected first correction choice after previous command, got '{form.SelectedChoiceNumber}'.");
    Require(form.HudSize.Width <= 720 && form.HudSize.Height <= 480, $"Expected compact correction HUD size, got {form.HudSize}.");
    Require(
        form.SummaryText.Contains("selected", StringComparison.OrdinalIgnoreCase)
        || form.SummaryText.Contains("alternative", StringComparison.OrdinalIgnoreCase),
        $"Expected correction summary text to describe the choices, got '{form.SummaryText}'.");
}

static void DictationPasteBlocksSensitiveTargets()
{
    Require(
        DictationTargetSafetyService.IsSensitiveTarget(new DictationTargetInfo("Example.com - Password reset", "chrome"), out var passwordReason),
        "Password-like foreground titles should block dictation paste.");
    Require(passwordReason.Contains("password", StringComparison.OrdinalIgnoreCase), $"Expected password reason, got '{passwordReason}'.");

    Require(
        DictationTargetSafetyService.IsSensitiveTarget(new DictationTargetInfo("Secure checkout - credit card", "msedge"), out var paymentReason),
        "Payment-like foreground titles should block dictation paste.");
    Require(paymentReason.Contains("credit card", StringComparison.OrdinalIgnoreCase), $"Expected payment reason, got '{paymentReason}'.");

    Require(
        DictationTargetSafetyService.IsSensitiveTarget(new DictationTargetInfo("Vault", "Bitwarden"), out var processReason),
        "Password manager process names should block dictation paste.");
    Require(processReason.Contains("Bitwarden", StringComparison.OrdinalIgnoreCase), $"Expected process reason, got '{processReason}'.");

    Require(
        !DictationTargetSafetyService.IsSensitiveTarget(new DictationTargetInfo("Untitled - Notepad", "notepad"), out _),
        "Normal editor targets should not block dictation paste.");

    var repoRoot = FindRepositoryRoot();
    var uiPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    var source = File.ReadAllText(uiPath);
    Require(source.Contains("TryAllowDictationPasteTarget(out var blockedMessage)", StringComparison.OrdinalIgnoreCase), "Paste dictation should check target safety before sending text.");
    Require(source.Contains("Dictation paste blocked", StringComparison.OrdinalIgnoreCase), "Paste dictation should explain sensitive-target blocks visibly.");
}

static void VoiceHelpCommandRoutesSetupHelp()
{
    Require(AlphaCommandRouter.TryRoute("voice help", out var voiceHelpRoute), "Voice help command should route.");
    Require(voiceHelpRoute.Kind == AlphaCommandKind.UiAction, "Voice help should be a UI action.");
    Require(voiceHelpRoute.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{voiceHelpRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("getting started", out var walkthroughRoute), "Getting started command should route.");
    Require(walkthroughRoute.Kind == AlphaCommandKind.UiAction, "Getting started should be a UI action.");
    Require(walkthroughRoute.Target == "ui-getting-started", $"Expected ui-getting-started target, got '{walkthroughRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("what can i say", out var whatCanISayRoute), "What can I say command should route.");
    Require(whatCanISayRoute.Kind == AlphaCommandKind.UiAction, "What can I say should be a UI action.");
    Require(whatCanISayRoute.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{whatCanISayRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show all commands", out var showAllCommandsRoute), "Show all commands should route.");
    Require(showAllCommandsRoute.Target == "ui-voice-help", $"Expected ui-voice-help for show all commands, got '{showAllCommandsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("show command list", out var showCommandListRoute), "Show command list should route.");
    Require(showCommandListRoute.Target == "ui-voice-help", $"Expected ui-voice-help for show command list, got '{showCommandListRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open voice access help", out var openVoiceAccessHelpRoute), "Open voice access help should route.");
    Require(openVoiceAccessHelpRoute.Target == "ui-voice-help", $"Expected ui-voice-help for open voice access help, got '{openVoiceAccessHelpRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("open voice access guide", out var openVoiceAccessGuideRoute), "Open voice access guide should route.");
    Require(openVoiceAccessGuideRoute.Target == "ui-getting-started", $"Expected ui-getting-started for open voice access guide, got '{openVoiceAccessGuideRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("close commands", out var closeCommandsRoute), "Close commands command should route.");
    Require(closeCommandsRoute.Kind == AlphaCommandKind.UiAction, "Close commands should be a UI action.");
    Require(closeCommandsRoute.Target == "ui-hide-command-palette", $"Expected ui-hide-command-palette target, got '{closeCommandsRoute.Target}'.");
    Require(AlphaCommandRouter.TryRouteUiNavigation("open voice access settings", out var voiceAccessSettingsNavigationTarget), "Open voice access settings should route to the visible Voice surface.");
    Require(voiceAccessSettingsNavigationTarget == "Voice", $"Expected Voice navigation target for open voice access settings, got '{voiceAccessSettingsNavigationTarget}'.");

    Require(AlphaCommandRouter.TryRoute("dismiss update splash", out var dismissUpdateSplashRoute), "Dismiss update splash command should route.");
    Require(dismissUpdateSplashRoute.Kind == AlphaCommandKind.UiAction, "Dismiss update splash should be a UI action.");
    Require(dismissUpdateSplashRoute.Target == "ui-hide-update-splash", $"Expected ui-hide-update-splash target, got '{dismissUpdateSplashRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("commands only mode", out var commandsOnlyModeRoute), "Commands-only mode command should route.");
    Require(commandsOnlyModeRoute.Kind == AlphaCommandKind.UiAction, "Commands-only mode should be a UI action.");
    Require(commandsOnlyModeRoute.Target == "ui-set-voice-mode:commands", $"Expected ui-set-voice-mode:commands target, got '{commandsOnlyModeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("start command mode", out var startCommandModeRoute), "Start command mode should route.");
    Require(startCommandModeRoute.Target == "ui-set-voice-mode:commands", $"Expected ui-set-voice-mode:commands target, got '{startCommandModeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("turn off dictation mode", out var turnOffDictationModeRoute), "Turn off dictation mode should route to commands-only mode.");
    Require(turnOffDictationModeRoute.Target == "ui-set-voice-mode:commands", $"Expected ui-set-voice-mode:commands target, got '{turnOffDictationModeRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("dictation mode", out var dictationModeRoute), "Dictation mode command should route.");
    Require(dictationModeRoute.Kind == AlphaCommandKind.UiAction, "Dictation mode should be a UI action.");
    Require(dictationModeRoute.Target == "ui-set-voice-mode:dictation", $"Expected ui-set-voice-mode:dictation target, got '{dictationModeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("start dictation mode", out var startDictationModeRoute), "Start dictation mode should route.");
    Require(startDictationModeRoute.Target == "ui-set-voice-mode:dictation", $"Expected ui-set-voice-mode:dictation target, got '{startDictationModeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("typing mode", out var typingModeRoute), "Typing mode should route.");
    Require(typingModeRoute.Target == "ui-set-voice-mode:dictation", $"Expected ui-set-voice-mode:dictation target, got '{typingModeRoute.Target}'.");

    Require(AlphaCommandRouter.TryRoute("default mode", out var defaultModeRoute), "Default mode command should route.");
    Require(defaultModeRoute.Kind == AlphaCommandKind.UiAction, "Default mode should be a UI action.");
    Require(defaultModeRoute.Target == "ui-set-voice-mode:default", $"Expected ui-set-voice-mode:default target, got '{defaultModeRoute.Target}'.");
    Require(AlphaCommandRouter.TryRoute("commands plus dictation mode", out var commandsPlusDictationModeRoute), "Commands plus dictation mode should route.");
    Require(commandsPlusDictationModeRoute.Target == "ui-set-voice-mode:default", $"Expected ui-set-voice-mode:default target, got '{commandsPlusDictationModeRoute.Target}'.");

    foreach (var phrase in new[] { "voice access wake up", "wake up", "microphone on", "unmute microphone" })
    {
        Require(AlphaCommandRouter.TryRoute(phrase, out var startListeningRoute), $"Voice Access-style start-listening phrase should route: {phrase}");
        Require(startListeningRoute.Kind == AlphaCommandKind.UiAction, $"Start-listening phrase should be a UI action: {phrase}");
        Require(startListeningRoute.Target == "ui-start-listening", $"Expected ui-start-listening for '{phrase}', got '{startListeningRoute.Target}'.");
    }

    foreach (var phrase in new[] { "voice access sleep", "go to sleep", "turn off microphone", "turn off voice access", "stop voice access", "close voice access", "exit voice access", "quit voice access", "mute microphone", "microphone off" })
    {
        Require(AlphaCommandRouter.TryRoute(phrase, out var stopListeningRoute), $"Voice Access-style stop-listening phrase should route: {phrase}");
        Require(stopListeningRoute.Kind == AlphaCommandKind.UiAction, $"Stop-listening phrase should be a UI action: {phrase}");
        Require(stopListeningRoute.Target == "ui-stop-listening", $"Expected ui-stop-listening for '{phrase}', got '{stopListeningRoute.Target}'.");
        Require(AlphaVoiceTranscriptParser.IsStopListeningCommand(phrase), $"Stop-listening phrase should be accepted by transcript-level listener guard: {phrase}");
    }

    foreach (var phrase in new[] { "stop", "stop now", "pause", "cancel", "never mind" })
    {
        Require(AlphaCommandRouter.TryRoute(phrase, out var cancelRoute), $"Urgent session-safety phrase should route: {phrase}");
        Require(cancelRoute.Kind == AlphaCommandKind.UiAction, $"Urgent session-safety phrase should be a UI action: {phrase}");
        Require(cancelRoute.Target == "ui-cancel-session", $"Expected ui-cancel-session for '{phrase}', got '{cancelRoute.Target}'.");
        Require(AlphaVoiceTranscriptParser.IsCancelCommand(phrase), $"Urgent session-safety phrase should be recognized before command execution: {phrase}");
    }

    foreach (var phrase in new[] { "reset session", "restart session", "clear session" })
    {
        Require(AlphaCommandRouter.TryRoute(phrase, out var resetRoute), $"Reset-session phrase should route: {phrase}");
        Require(resetRoute.Kind == AlphaCommandKind.UiAction, $"Reset-session phrase should be a UI action: {phrase}");
        Require(resetRoute.Target == "ui-reset-session", $"Expected ui-reset-session for '{phrase}', got '{resetRoute.Target}'.");
    }

    Require(AlphaCommandRouter.TryRoute("close active app", out var closeWindowRoute), "Close active app command should route.");
    Require(closeWindowRoute.Kind == AlphaCommandKind.SystemControl, "Close active app should be a system-control action.");
    Require(closeWindowRoute.Target == "system-close-window", $"Expected system-close-window target, got '{closeWindowRoute.Target}'.");

    var parsed = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one what can I say", "Callsign", "echo one");
    Require(parsed.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {parsed.Kind}.");
    Require(parsed.Target == "ui-voice-help", $"Expected ui-voice-help target, got '{parsed.Target}'.");

    var settingsIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open voice access settings", "Callsign", "echo one");
    Require(settingsIntent.Kind == AlphaVoiceIntentKind.UiNavigation, $"Expected UiNavigation for settings intent, got {settingsIntent.Kind}.");
    Require(settingsIntent.Target == "Voice", $"Expected Voice target for settings intent, got '{settingsIntent.Target}'.");

    var guideIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open voice access guide", "Callsign", "echo one");
    Require(guideIntent.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for guide intent, got {guideIntent.Kind}.");
    Require(guideIntent.Target == "ui-getting-started", $"Expected ui-getting-started target for guide intent, got '{guideIntent.Target}'.");

    var sleepIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one go to sleep", "Callsign", "echo one");
    Require(sleepIntent.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for sleep intent, got {sleepIntent.Kind}.");
    Require(sleepIntent.Target == "ui-stop-listening", $"Expected ui-stop-listening sleep target, got '{sleepIntent.Target}'.");
    var stopNowIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one stop now", "Callsign", "echo one");
    Require(stopNowIntent.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for stop-now intent, got {stopNowIntent.Kind}.");
    Require(stopNowIntent.Target == "ui-cancel-session", $"Expected ui-cancel-session stop-now target, got '{stopNowIntent.Target}'.");
    var resetIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one reset session", "Callsign", "echo one");
    Require(resetIntent.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for reset-session intent, got {resetIntent.Kind}.");
    Require(resetIntent.Target == "ui-reset-session", $"Expected ui-reset-session target, got '{resetIntent.Target}'.");

    var parsedMode = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one switch to commands only mode", "Callsign", "echo one");
    Require(parsedMode.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for voice mode, got {parsedMode.Kind}.");
    Require(parsedMode.Target == "ui-set-voice-mode:commands", $"Expected ui-set-voice-mode:commands target, got '{parsedMode.Target}'.");
    var startCommandMode = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start command mode", "Callsign", "echo one");
    Require(startCommandMode.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for start command mode, got {startCommandMode.Kind}.");
    Require(startCommandMode.Target == "ui-set-voice-mode:commands", $"Expected ui-set-voice-mode:commands target, got '{startCommandMode.Target}'.");
    var startDictationMode = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start dictation mode", "Callsign", "echo one");
    Require(startDictationMode.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for start dictation mode, got {startDictationMode.Kind}.");
    Require(startDictationMode.Target == "ui-set-voice-mode:dictation", $"Expected ui-set-voice-mode:dictation target, got '{startDictationMode.Target}'.");
    var defaultPlusDictationMode = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one commands plus dictation mode", "Callsign", "echo one");
    Require(defaultPlusDictationMode.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction for commands plus dictation mode, got {defaultPlusDictationMode.Kind}.");
    Require(defaultPlusDictationMode.Target == "ui-set-voice-mode:default", $"Expected ui-set-voice-mode:default target, got '{defaultPlusDictationMode.Target}'.");
}

static void CommandDiscoveryListsBuiltInAndExtensionCommands()
{
    var registry = PackTestSupport.CreateRegistry();
    registry.RegisterPack(new SampleCommandPack());

    var commands = CommandDiscoveryService.GetCommands(registry);
    Require(commands.Any(command => command.Phrase == "what can I say"), "Command discovery should include the help command.");
    Require(commands.Any(command => command.Category == "Help" && command.Phrase == "what can I say" && command.Examples?.Contains("close commands") == true && command.Examples.Contains("cancel commands")), "Command discovery should include command-palette dismissal commands.");
    Require(commands.Any(command => command.Category == "Updates" && command.Phrase == "close update splash" && command.Examples?.Contains("dismiss update splash") == true && command.Examples.Contains("cancel update splash")), "Command discovery should include update-splash dismissal commands.");
    Require(commands.Any(command => command.Phrase == "show numbers"), "Command discovery should include visible control numbers.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("double click 1") == true && command.Examples.Contains("right click save")), "Command discovery should include visible-control double-click and right-click commands.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("click number one") == true && command.Examples.Contains("choose item twenty one")), "Command discovery should include natural numbered visible-control phrases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("double click control twelve") == true && command.Examples.Contains("right click option third")), "Command discovery should include natural numbered mouse-action phrases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("cancel control numbers") == true), "Command discovery should include visible-control cancel commands.");
    Require(commands.Any(command => command.Phrase == "show grid"), "Command discovery should include mouse grid commands.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show mouse grid") == true), "Command discovery should include mouse grid aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("mouse grid") == true), "Command discovery should include mouse grid shorthand.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("open grid") == true), "Command discovery should include mouse grid open aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("hide mouse grid") == true), "Command discovery should include mouse grid hide aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("cancel mouse grid") == true), "Command discovery should include mouse grid cancel aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show mousegrid") == true), "Command discovery should include mousegrid compatibility aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show numbered grid") == true), "Command discovery should include numbered-grid aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("grid five") == true && command.Examples.Contains("select cell third")), "Command discovery should include natural mouse-grid cell selection phrases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("click cell one") == true && command.Examples.Contains("drag from cell one to cell ninth")), "Command discovery should include natural mouse-grid click and drag phrases.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "show keyboard" && command.Examples?.Contains("show on screen keyboard") == true), "Command discovery should include on-screen keyboard aliases.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "show keyboard" && command.Examples?.Contains("hide keyboard") == true), "Command discovery should include keyboard hide aliases.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "show keyboard" && command.Examples?.Contains("cancel keyboard") == true), "Command discovery should include keyboard cancel aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "click" && command.Examples?.Contains("double click") == true), "Command discovery should include mouse click aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "click" && command.Examples?.Contains("tap") == true), "Command discovery should include tap as a mouse click alias.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "click" && command.Examples?.Contains("triple click") == true), "Command discovery should include mouse triple-click aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "click" && command.Examples?.Contains("right click") == true), "Command discovery should include mouse right-click aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "mouse scroll up" && command.Examples?.Contains("scroll left") == true), "Command discovery should include mouse scroll aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "mouse scroll up" && command.Examples?.Contains("mouse scroll down a little") == true), "Command discovery should include bounded mouse scroll aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "move mouse up" && command.Examples?.Contains("move mouse right") == true), "Command discovery should include mouse nudge aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "move mouse up" && command.Examples?.Contains("move mouse top left") == true), "Command discovery should include diagonal continuous mouse movement examples.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "move mouse up" && command.Examples?.Contains("move mouse left five") == true), "Command discovery should include fixed-distance mouse movement examples.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "move mouse up" && command.Examples?.Contains("move faster") == true && command.Examples.Contains("stop moving")), "Command discovery should include mouse motion speed and stop commands.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "move mouse up" && command.Examples?.Contains("nudge up") == true), "Command discovery should include short pointer nudge aliases.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("dismiss") == true), "Command discovery should include dismiss as an escape-key alias.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press tab five times") == true && command.Examples.Contains("press down three times")), "Command discovery should include repeated single-key press examples.");
    Require(commands.Any(command => command.Category == "File results" && command.Phrase == "open file result 1" && command.Examples?.Contains("open result twenty one") == true), "Command discovery should include compound open-result number phrases.");
    Require(commands.Any(command => command.Category == "File results" && command.Phrase == "open file result 1" && command.Examples?.Contains("choose result thirty second") == true && command.Examples.Contains("reveal result thirty nine")), "Command discovery should include compound select/reveal result number phrases.");
    Require(commands.Any(command => command.Phrase == "search this page for privacy policy"), "Command discovery should include browser find-text commands.");
    Require(commands.Any(command => command.Category == "Browser navigation" && command.Phrase == "browser back" && command.Examples?.Contains("url bar") == true), "Command discovery should include URL bar aliases.");
    Require(commands.Any(command => command.Category == "Browser search" && command.Phrase == "search this page for privacy policy" && command.Examples?.Contains("open find box") == true), "Command discovery should include browser find-box aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("start scrolling down") == true && command.Examples.Contains("stop scrolling")), "Command discovery should include continuous browser scrolling aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("switch application") == true), "Command discovery should include switch-application aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("last app") == true), "Command discovery should include last-app aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("switch to edge") == true && command.Examples.Contains("go to notepad")), "Command discovery should include named app/window switching aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("choose window 1") == true && command.Examples.Contains("confirm window")), "Command discovery should include visible numbered window-choice commands.");
    Require(commands.Any(command => command.Category == "Dictation"), "Command discovery should include dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("delete next word") == true), "Command discovery should include forward dictation edit commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("select that") == true), "Command discovery should include select-that dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("delete that") == true), "Command discovery should include delete-that dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("scratch that") == true), "Command discovery should include scratch-that dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("delete all") == true), "Command discovery should include delete-all dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("tab") == true && command.Examples.Contains("new sentence")), "Command discovery should include tab and new-sentence dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("exclamation") == true && command.Examples.Contains("semi colon")), "Command discovery should include natural punctuation aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("open parentheses") == true && command.Examples.Contains("open square bracket")), "Command discovery should include natural bracket punctuation aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("space") == true && command.Examples.Contains("no space that")), "Command discovery should include dictation spacing controls.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("select previous character") == true && command.Examples.Contains("delete next character")), "Command discovery should include character-level dictation edit commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to previous line") == true && command.Examples.Contains("delete next line")), "Command discovery should include line-level dictation edit commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("pause dictation") == true && command.Examples.Contains("resume dictation")), "Command discovery should include pause/resume dictation controls.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("start typing") == true && command.Examples.Contains("resume typing")), "Command discovery should include natural typing aliases for dictation.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("stop taking dictation") == true && command.Examples.Contains("stop voice typing")), "Command discovery should include natural stop-typing dictation aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("select previous paragraph") == true && command.Examples.Contains("delete next paragraph")), "Command discovery should include paragraph selection and deletion dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to next word") == true), "Command discovery should include word movement dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to next sentence") == true), "Command discovery should include sentence movement dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to next paragraph") == true), "Command discovery should include paragraph movement dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to start") == true), "Command discovery should include start-of-text dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to beginning of text") == true && command.Examples.Contains("select to beginning of text")), "Command discovery should include beginning-of-text dictation aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to end of text") == true && command.Examples.Contains("delete to end of dictation")), "Command discovery should include end-of-text dictation aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go before privacy policy") == true && command.Examples.Contains("move after the phrase privacy policy")), "Command discovery should include target-text cursor movement aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("type hello world") == true), "Command discovery should include direct reviewed type-text commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("insert text hello comma world") == true), "Command discovery should include direct reviewed insert-text commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("read dictation") == true && command.Examples.Contains("read that back") && command.Examples.Contains("speak text")), "Command discovery should include local dictation readback commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("stop reading") == true && command.Examples.Contains("stop readback") && command.Examples.Contains("stop speaking")), "Command discovery should include local dictation readback stop commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Description.Contains("visible review surface", StringComparison.OrdinalIgnoreCase) && command.Description.Contains("read aloud locally", StringComparison.OrdinalIgnoreCase) && command.Description.Contains("stopped", StringComparison.OrdinalIgnoreCase) && command.Description.Contains("copied", StringComparison.OrdinalIgnoreCase) && command.Description.Contains("pasted", StringComparison.OrdinalIgnoreCase)), "Command discovery should explain that direct dictated text remains in the visible review surface before local readback/stop/copy/paste.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to line start") == true), "Command discovery should include line-start dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("go to line end") == true), "Command discovery should include line-end dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("select to line start") == true), "Command discovery should include line-selection dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("delete to line start") == true), "Command discovery should include line-deletion dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("select all") == true), "Command discovery should include select-all dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out"), "Command discovery should include dictation spelling commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out" && command.Examples?.Contains("spell alpha bravo charlie") == true), "Command discovery should include NATO spelling dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out" && command.Examples?.Contains("spell capital alpha bravo cap letter charlie") == true), "Command discovery should include capitalized spelling examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out" && command.Examples?.Contains("spell capital alpha lowercase bravo lower case letter charlie digit five number six") == true), "Command discovery should include qualified lowercase and digit spelling examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out" && command.Examples?.Contains("insert alpha underscore one") == true), "Command discovery should include symbol spelling dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "spell it out" && command.Examples?.Contains("spell support at sign example dot com") == true), "Command discovery should include multi-word symbol spelling examples.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show what i can click") == true), "Command discovery should include friendly visible-controls aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show clickable controls") == true), "Command discovery should include clickable visible-controls aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show numbers here") == true), "Command discovery should include show-numbers-here aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show numbers everywhere") == true), "Command discovery should include show-numbers-everywhere aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show numbers on notepad") == true), "Command discovery should include named-window visible-controls aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("show numbers on taskbar") == true), "Command discovery should include taskbar visible-controls aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("number clickable controls") == true), "Command discovery should include number-clickable-controls aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "show numbers" && command.Examples?.Contains("clear numbers") == true), "Command discovery should include clear-numbers aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show grid here") == true), "Command discovery should include show-grid-here aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show grid everywhere") == true), "Command discovery should include show-grid-everywhere aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("show window grid") == true), "Command discovery should include show-window-grid aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("grid bravo") == true), "Command discovery should include display-selection mouse-grid aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("mouse grid a 114") == true), "Command discovery should include display-path mouse-grid aliases.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("mouse grid 114") == true), "Command discovery should include current-scope mouse-grid shortcut paths.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("mouse grid 1 1 4") == true), "Command discovery should include spaced current-scope mouse-grid shortcut paths.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("mark four") == true), "Command discovery should include mouse-grid mark commands.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("undo that") == true), "Command discovery should include mouse-grid undo commands.");
    Require(commands.Any(command => command.Category == "Mouse grid" && command.Phrase == "show grid" && command.Examples?.Contains("drag") == true), "Command discovery should include marked mouse-grid drag commands.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "next control" && command.Examples?.Contains("activate control") == true), "Command discovery should include visible-control navigation aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "next control" && command.Examples?.Contains("move to next field") == true && command.Examples.Contains("tab forward")), "Command discovery should include natural forward field-navigation aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "next control" && command.Examples?.Contains("move to previous field") == true && command.Examples.Contains("tab backward")), "Command discovery should include natural backward field-navigation aliases.");
    Require(commands.Any(command => command.Category == "Visible controls" && command.Phrase == "next control" && command.Examples?.Contains("click selected control") == true), "Command discovery should include selected-control activation aliases.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "getting started" && command.Examples?.Contains("open getting started") == true), "Command discovery should include getting-started walkthrough commands.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "open account" && command.Examples?.Contains("open voice tab") == true), "Command discovery should include direct setup tab navigation.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "open account" && command.Examples?.Contains("open system") == true), "Command discovery should include the System setup tab.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "open account" && command.Examples?.Contains("manage packs") == true), "Command discovery should include command-pack management navigation.");
    Require(commands.Any(command => command.Category == "Profile setup" && command.Phrase == "create account" && command.Examples?.Contains("save profile") == true), "Command discovery should include save-profile setup commands.");
    Require(commands.Any(command => command.Category == "Profile setup" && command.Phrase == "create account" && command.Examples?.Contains("delete profile") == true), "Command discovery should include delete-profile setup commands.");
    Require(commands.Any(command => command.Category == "Profile setup" && command.Phrase == "repair wake word" && command.Examples?.Contains("fix wake word") == true), "Command discovery should include wake-word repair commands.");
    Require(commands.Any(command => command.Category == "Profile setup" && command.Phrase == "train voice identity" && command.Examples?.Contains("voice identity training") == true), "Command discovery should include voice identity training commands.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "open account" && command.Examples?.Contains("open voice access settings") == true), "Command discovery should include visible voice access settings navigation.");
    Require(commands.Any(command => command.Category == "Navigation" && command.Phrase == "getting started" && command.Examples?.Contains("open voice access guide") == true), "Command discovery should include the Voice Access guide alias.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "start listening" && command.Examples?.Contains("voice access wake up") == true && command.Examples.Contains("turn off microphone") && command.Examples.Contains("unmute microphone")), "Command discovery should include Voice Access-style wake-up and microphone listener controls.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "start listening" && command.Examples?.Contains("go to sleep") == true), "Command discovery should include Voice Access-style sleep listener controls.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "start listening" && command.Description.Contains("audio detector", StringComparison.OrdinalIgnoreCase)), "Runtime command discovery should preserve Callsign's audio-detector wake boundary.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "start listening" && command.Examples?.Contains("stop voice") == true && command.Examples.Contains("close voice access") && command.Examples.Contains("exit voice access")), "Command discovery should include runtime listener stop commands and Voice Access exit aliases.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "commands only mode" && command.Examples?.Contains("dictation mode") == true), "Command discovery should include Voice Access-style dictation mode switching.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "commands only mode" && command.Examples?.Contains("start command mode") == true && command.Examples.Contains("start dictation mode")), "Command discovery should include natural start-mode aliases.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "commands only mode" && command.Examples?.Contains("commands plus dictation mode") == true), "Command discovery should include default-mode plus-dictation aliases.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "commands only mode" && command.Examples?.Contains("pause dictation") == true && command.Examples.Contains("resume typing")), "Command discovery should include pause/resume typing in runtime controls.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "what did you hear" && command.Examples?.Contains("read status") == true && command.Examples.Contains("stop status readback")), "Command discovery should include visible status readback and stop-readback commands.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "what did you hear" && command.Examples?.Contains("clear recent speech") == true && command.Examples.Contains("clear speech history")), "Command discovery should include recent-speech clear commands.");
    Require(commands.Any(command => command.Category == "Runtime" && command.Phrase == "commands only mode" && command.Examples?.Contains("default mode") == true), "Command discovery should include Voice Access-style default mode switching.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "add to vocabulary" && command.Examples?.Contains("add womprat to vocabulary") == true && command.Examples.Contains("add project zephyr to dictation vocabulary")), "Command discovery should include local dictation vocabulary commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "fluid dictation on" && command.Examples?.Contains("turn on fluid dictation") == true && command.Examples.Contains("revert")), "Command discovery should include local fluid dictation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "automatic punctuation on" && command.Examples?.Contains("turn on automatic punctuation") == true && command.Examples.Contains("do not filter profanity")), "Command discovery should include local dictation option commands.");
    Require(commands.Any(command => command.Category == "Diagnostics" && command.Phrase == "open logs folder" && command.Examples?.Contains("open data folder") == true), "Command discovery should include local data folder commands.");
    Require(commands.Any(command => command.Category == "Diagnostics" && command.Phrase == "open logs folder" && command.Examples?.Contains("open app folder") == true), "Command discovery should include local app folder commands.");
    Require(commands.Any(command => command.Category == "Session safety" && command.Phrase == "cancel" && command.Examples?.Contains("cancel session") == true), "Command discovery should include cancel-session commands.");
    Require(commands.Any(command => command.Category == "Session safety" && command.Phrase == "cancel" && command.Examples?.Contains("stop") == true && command.Examples.Contains("stop now") && command.Examples.Contains("pause")), "Command discovery should include urgent stop and pause safety aliases.");
    Require(commands.Any(command => command.Category == "Session safety" && command.Phrase == "stop listening" && command.Examples?.Contains("stop voice") == true && command.Examples.Contains("turn off voice access") && command.Examples.Contains("quit voice access")), "Command discovery should include stop-listening commands and Voice Access exit aliases.");
    Require(commands.Any(command => command.Category == "Help" && command.Phrase == "what can I say" && command.Examples?.Contains("show all commands") == true && command.Examples.Contains("show command list") && command.Examples.Contains("open voice access help")), "Command discovery should include the Voice Access command list and help aliases.");
    Require(commands.Any(command => command.Category == "Session safety" && command.Phrase == "reset session" && command.Examples?.Contains("start over") == true), "Command discovery should include reset-session commands.");
    Require(commands.Any(command => command.Category == "Session safety" && command.Phrase == "reset session" && command.Examples?.Contains("restart session") == true && command.Examples.Contains("clear session")), "Command discovery should include natural reset-session aliases.");
    Require(commands.Any(command => command.Phrase == "capitalize previous word"), "Command discovery should include dictation formatting commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("fix privacy policy with safety notes") == true), "Command discovery should include natural fix target-text commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "start dictation" && command.Examples?.Contains("fix previous word with hello") == true), "Command discovery should include natural fix scoped replacement commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "capitalize previous word" && command.Examples?.Contains("select previous word") == true), "Command discovery should include selection-aware dictation formatting commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "capitalize previous word" && command.Examples?.Contains("make that uppercase") == true && command.Examples.Contains("make all text title case")), "Command discovery should include natural make-that dictation formatting commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "capitalize previous word" && command.Examples?.Contains("caps on") == true && command.Examples.Contains("all caps on") && command.Examples.Contains("no caps on") && command.Examples.Contains("caps off")), "Command discovery should include persistent dictation casing modes.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote"), "Command discovery should include dictation symbol commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("backslash") == true), "Command discovery should include expanded dictation symbol examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("comma") == true), "Command discovery should include punctuation dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("full stop") == true), "Command discovery should include punctuation synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("left paren") == true), "Command discovery should include parenthesis synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("open parentheses") == true), "Command discovery should include plural open-parentheses synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("right paren") == true), "Command discovery should include closing parenthesis synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("close parentheses") == true), "Command discovery should include plural close-parentheses synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("minus") == true), "Command discovery should include minus/dash synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("semicolon") == true), "Command discovery should include semicolon synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("semi colon") == true), "Command discovery should include spaced semicolon synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("colon") == true), "Command discovery should include colon synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("equal sign") == true), "Command discovery should include equals synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("at symbol") == true), "Command discovery should include at-symbol synonyms.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("question mark") == true), "Command discovery should include question-mark dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("exclamation") == true), "Command discovery should include exclamation dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("plus sign") == true), "Command discovery should include direct plus-sign dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("open bracket") == true), "Command discovery should include bracket dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("open square bracket") == true && command.Examples.Contains("close square bracket")), "Command discovery should include square-bracket dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("close brace") == true), "Command discovery should include brace dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("pipe") == true), "Command discovery should include pipe dictation examples.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "quote" && command.Examples?.Contains("quote that") == true && command.Examples.Contains("parenthesize that") && command.Examples.Contains("bracket that")), "Command discovery should include paired punctuation wrapping commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("correct all text") == true), "Command discovery should include all-text correction commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("fix that") == true && command.Examples.Contains("fix all text")), "Command discovery should include natural fix correction commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("show corrections") == true), "Command discovery should include correction mode discovery commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("show correction alternatives") == true), "Command discovery should include correction alternative discovery commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("next correction") == true && command.Examples.Contains("previous correction")), "Command discovery should include correction navigation commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("accept correction") == true), "Command discovery should include selected correction accept commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("accept that") == true && command.Examples.Contains("use that")), "Command discovery should include natural selected-correction accept aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("choose correction 1") == true), "Command discovery should include correction selection commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("choose correction 6") == true), "Command discovery should include correction range commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("choose one") == true && command.Examples.Contains("pick option two") && command.Examples.Contains("use alternative 3")), "Command discovery should include natural correction-number selection aliases.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("cancel correction") == true), "Command discovery should include correction cancel commands.");
    Require(commands.Any(command => command.Category == "Dictation" && command.Phrase == "correct previous word" && command.Examples?.Contains("close correction") == true && command.Examples.Contains("dismiss correction")), "Command discovery should include correction close commands.");
    Require(commands.Any(command => command.Category == "Browser navigation" && command.Phrase == "browser back" && command.Examples?.Contains("browser home") == true), "Command discovery should include browser home navigation commands.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("browser new window") == true), "Command discovery should include browser window commands.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("open new tab") == true), "Command discovery should include natural browser new-tab aliases.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("browser private window") == true), "Command discovery should include browser private window commands.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("new private window") == true && command.Examples.Contains("open incognito window")), "Command discovery should include natural private/incognito browser aliases.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("reopen closed tab") == true), "Command discovery should include browser tab recovery commands.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("undo close tab") == true), "Command discovery should include undo-close-tab browser aliases.");
    Require(commands.Any(command => command.Category == "Browser utilities" && command.Phrase == "browser open bookmarks" && command.Examples?.Contains("browser favorites") == true), "Command discovery should include browser favorites commands.");
    Require(commands.Any(command => command.Category == "Browser utilities" && command.Phrase == "browser open bookmarks" && command.Examples?.Contains("browser downloads") == true), "Command discovery should include browser downloads commands.");
    Require(commands.Any(command => command.Category == "Browser utilities" && command.Phrase == "browser open bookmarks" && command.Examples?.Contains("show downloads") == true), "Command discovery should include natural browser downloads aliases.");
    Require(commands.Any(command => command.Category == "Browser utilities" && command.Phrase == "browser open bookmarks" && command.Examples?.Contains("browser history") == true), "Command discovery should include browser history commands.");
    Require(commands.Any(command => command.Category == "Browser utilities" && command.Phrase == "browser open bookmarks" && command.Examples?.Contains("show history") == true), "Command discovery should include natural browser history aliases.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "launch app" && command.Examples?.Contains("launch Notepad") == true), "Command discovery should include visible app-launch commands.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "launch app" && command.Examples?.Contains("start Paint") == true), "Command discovery should include app-launch synonyms.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "choose app 1" && command.Examples?.Contains("confirm app") == true), "Command discovery should include ambiguous app-confirmation commands.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "choose app 1" && command.Examples?.Contains("choose app 5") == true), "Command discovery should include numbered app-choice commands.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "choose app 1" && command.Examples?.Contains("1") == true && command.Examples.Contains("click 1")), "Command discovery should include bare-number and click-number app-choice commands.");
    Require(commands.Any(command => command.Category == "App launch" && command.Phrase == "choose app 1" && command.Examples?.Contains("choose result 1") == true && command.Examples.Contains("next app choice") && command.Examples.Contains("clear app choices")), "Command discovery should include result-style, navigation, and clear app-choice commands.");
    Require(commands.Any(command => command.Category == "Browser open" && command.Phrase == "browser open example.com" && command.Examples?.Contains("browser search callsign") == true), "Command discovery should include browser open commands.");
    Require(commands.Any(command => command.Category == "Browser open" && command.Phrase == "browser open example.com" && command.Examples?.Contains("type in address bar example.com") == true && command.Examples.Contains("search address bar for callsign")), "Command discovery should include browser address-bar typing commands.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("search my documents for budget") == true), "Command discovery should include file search commands.");
    Require(commands.Any(command => command.Category == "File results" && command.Phrase == "open file result 1" && command.Examples?.Contains("reveal file result 1") == true), "Command discovery should include file result utility commands.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("browser downloads") == true), "Command discovery should include browser utility commands.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("bookmark this page") == true && command.Examples.Contains("add bookmark")), "Command discovery should include natural bookmark-page aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("browser scroll left") == true), "Command discovery should include browser horizontal scroll commands.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("scroll down a little") == true), "Command discovery should include natural browser page scroll aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("page down in browser") == true), "Command discovery should include explicit browser page-down aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("go to top of page") == true), "Command discovery should include natural browser top-of-page aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("go to bottom of page") == true), "Command discovery should include natural browser bottom-of-page aliases.");
    Require(commands.Any(command => command.Category == "Browser view" && command.Phrase == "browser full screen" && command.Examples?.Contains("browser zoom reset") == true), "Command discovery should include browser fullscreen and zoom commands.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("browser save page") == true), "Command discovery should include browser save-page aliases.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("browser print page") == true), "Command discovery should include browser print-page aliases.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("browser close tab") == true), "Command discovery should include browser close-tab aliases.");
    Require(commands.Any(command => command.Category == "Browser tabs" && command.Phrase == "browser new tab" && command.Examples?.Contains("reopen closed tab") == true), "Command discovery should include browser restore-tab aliases.");
    Require(commands.Any(command => command.Category == "Browser page" && command.Phrase == "browser bookmark page" && command.Examples?.Contains("browser history") == true), "Command discovery should include browser history aliases.");
    Require(commands.Any(command => command.Category == "Browser navigation" && command.Phrase == "browser back" && command.Examples?.Contains("focus address bar") == true), "Command discovery should include browser focus-address-bar aliases.");
    Require(commands.Any(command => command.Category == "Browser navigation" && command.Phrase == "browser back" && command.Examples?.Contains("type in address bar example.com") == true), "Command discovery should include visible address-bar target entry.");
    Require(commands.Any(command => command.Category == "Files tab" && command.Phrase == "open files tab"), "Command discovery should include files tab navigation.");
    Require(commands.Any(command => command.Category == "Files tab" && command.Phrase == "open files tab" && command.Examples?.Contains("show files tab") == true), "Command discovery should include files tab aliases.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "hold mouse"), "Command discovery should include mouse button drag commands.");
    Require(commands.Any(command => command.Category == "Mouse" && command.Phrase == "drag mouse right"), "Command discovery should include direct mouse drag commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space"), "Command discovery should include keyboard keypress commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press Windows key") == true), "Command discovery should include Windows key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("context menu key") == true), "Command discovery should include context menu key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press enter") == true), "Command discovery should include enter key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press tab") == true), "Command discovery should include tab key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press delete") == true), "Command discovery should include delete key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press home") == true), "Command discovery should include home key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("page up") == true), "Command discovery should include page-up key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press up arrow") == true && command.Examples.Contains("right arrow")), "Command discovery should include arrow key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press 5") == true), "Command discovery should include digit key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press A") == true), "Command discovery should include letter key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press question mark") == true), "Command discovery should include symbol key commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press shift tab") == true), "Command discovery should include safe modifier chord commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press shift a") == true && command.Examples.Contains("press shift 1")), "Command discovery should include safe natural Shift-key shortcut commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press control c") == true && command.Examples.Contains("press control v") && command.Examples.Contains("press control r") && command.Examples.Contains("press control 1") && command.Examples.Contains("press control zero")), "Command discovery should include safe natural Control-key shortcut commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press control shift t") == true && command.Examples.Contains("press control shift 1")), "Command discovery should include safe natural Control-Shift shortcut commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "press space" && command.Examples?.Contains("press alt shift tab") == true && command.Examples.Contains("press alt f") && command.Examples.Contains("press alt 1")), "Command discovery should include safe natural Alt-key access commands.");
    Require(commands.Any(command => command.Category == "Keyboard" && command.Phrase == "hold shift" && command.Examples?.Contains("release all modifiers") == true), "Command discovery should include held modifier key commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("bold") == true), "Command discovery should include editing formatting shortcuts.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("bold that") == true && command.Examples.Contains("italicize that") && command.Examples.Contains("underline that")), "Command discovery should include selected-text formatting phrases.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("make that bold") == true && command.Examples.Contains("make that italic") && command.Examples.Contains("make that underlined")), "Command discovery should include natural make-that formatting phrases.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("print") == true), "Command discovery should include document shortcut commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("select previous character") == true && command.Examples.Contains("delete next character")), "Command discovery should include active-app character-level editing commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("go to line start") == true && command.Examples.Contains("delete next line")), "Command discovery should include active-app line-level editing commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("select previous word") == true && command.Examples.Contains("delete next sentence")), "Command discovery should include active-app word and sentence editing commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("go to paragraph start") == true && command.Examples.Contains("delete to paragraph end")), "Command discovery should include active-app paragraph boundary commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("go to next word") == true), "Command discovery should include natural text navigation commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("go to previous word") == true), "Command discovery should include reverse word navigation commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("go to previous paragraph") == true), "Command discovery should include reverse paragraph navigation commands.");
    Require(commands.Any(command => command.Category == "Editing" && command.Phrase == "copy" && command.Examples?.Contains("zoom in") == true), "Command discovery should include active-app zoom shortcuts.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "volume up" && command.Examples?.Contains("volume down") == true), "Command discovery should include volume control aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "volume down" && command.Examples?.Contains("decrease volume") == true), "Command discovery should include volume-down aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "mute volume" && command.Examples?.Contains("mute audio") == true), "Command discovery should include mute aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "play or pause" && command.Examples?.Contains("play media") == true), "Command discovery should include media play aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "play or pause" && command.Examples?.Contains("stop playback") == true), "Command discovery should include media stop aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("power and battery settings") == true), "Command discovery should include expanded safe settings surfaces.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("open sound settings") == true), "Command discovery should include sound settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("open bluetooth settings") == true), "Command discovery should include bluetooth settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("open wifi settings") == true), "Command discovery should include wifi settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("open keyboard settings") == true), "Command discovery should include keyboard settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("magnifier settings") == true), "Command discovery should include magnifier settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("zoom settings") == true), "Command discovery should include natural zoom settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("narrator settings") == true), "Command discovery should include narrator settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("screen reader settings") == true), "Command discovery should include screen reader settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("captions settings") == true), "Command discovery should include captions settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("live captions settings") == true), "Command discovery should include live captions settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("speech settings") == true), "Command discovery should include speech settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("voice access settings") == true && command.Examples.Contains("voice typing settings")), "Command discovery should include voice access and voice typing settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("installed apps settings") == true), "Command discovery should include installed apps settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("default apps settings") == true), "Command discovery should include default-app settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("date and time settings") == true), "Command discovery should include date/time settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open display settings" && command.Examples?.Contains("windows update settings") == true), "Command discovery should include Windows Update settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open magnifier" && command.Examples?.Contains("magnifier zoom out") == true), "Command discovery should include magnifier zoom aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "open magnifier" && command.Examples?.Contains("close magnifier") == true), "Command discovery should include close-magnifier aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show desktop" && command.Examples?.Contains("minimize all windows") == true), "Command discovery should include desktop-hide aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("switch apps") == true), "Command discovery should include app-switching aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("switch windows") == true), "Command discovery should include switch-windows aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("task switcher") == true), "Command discovery should include task-switcher aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("window switcher") == true), "Command discovery should include window-switcher aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("task view") == true), "Command discovery should include task view aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("show all windows") == true), "Command discovery should include show-all-windows aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("quick settings") == true), "Command discovery should include Quick Settings aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("notification center") == true), "Command discovery should include Notification Center aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("project display") == true), "Command discovery should include project display aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("wireless display") == true), "Command discovery should include wireless display aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("emoji panel") == true), "Command discovery should include emoji panel aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("symbol picker") == true), "Command discovery should include symbol picker aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "clipboard history" && command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireApproval), "Command discovery should show clipboard history as approval-gated.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "clipboard history" && command.Examples?.Contains("show clipboard panel") == true), "Command discovery should include clipboard panel aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "clipboard history" && command.Examples?.Contains("open clipboard") == true && command.Examples.Contains("show clipboard picker")), "Command discovery should include natural visible clipboard surface aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snipping toolbar" && command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireApproval), "Command discovery should show snipping toolbar as approval-gated.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snipping toolbar" && command.Examples?.Contains("take screenshot") == true), "Command discovery should include screenshot toolbar aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snipping toolbar" && command.Examples?.Contains("show screenshot toolbar") == true && command.Examples.Contains("open screenshot tools")), "Command discovery should include natural visible screenshot-tool aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "show open windows" && command.Examples?.Contains("previous desktop") == true), "Command discovery should include virtual desktop navigation aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snap window left" && command.Examples?.Contains("snap up") == true), "Command discovery should include snap-direction aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snap window left" && command.Examples?.Contains("minimize window") == true), "Command discovery should include window minimize aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snap window left" && command.Examples?.Contains("maximize window") == true), "Command discovery should include window maximize aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snap window left" && command.Examples?.Contains("restore window") == true), "Command discovery should include window restore aliases.");
    Require(commands.Any(command => command.Category == "Browser search" && command.Phrase == "search this page for privacy policy" && command.Examples?.Contains("browser find") == true), "Command discovery should include browser find aliases.");
    Require(commands.Any(command => command.Category == "Browser search" && command.Phrase == "search this page for privacy policy" && command.Examples?.Contains("browser search in page") == true), "Command discovery should include browser search-in-page aliases.");
    Require(commands.Any(command => command.Category == "Browser search" && command.Phrase == "search this page for privacy policy" && command.Examples?.Contains("find privacy policy on this page") == true), "Command discovery should include natural browser find suffix aliases.");
    Require(commands.Any(command => command.Category == "Browser open" && command.Phrase == "browser open example.com" && command.Examples?.Contains("type example.com in the address bar") == true), "Command discovery should include natural address-bar suffix aliases.");
    Require(commands.Any(command => command.Category == "Browser open" && command.Phrase == "browser open example.com" && command.Examples?.Contains("go to address bar and type example.com") == true), "Command discovery should include address-bar type aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("open result 1") == true), "Command discovery should include open-result aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("find files named invoice") == true), "Command discovery should include named file-search aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("find folder named invoices") == true), "Command discovery should include named folder-search aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("search my folders for invoices") == true && command.Examples.Contains("look in folders for receipts")), "Command discovery should include folder search aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("select first result") == true), "Command discovery should include numbered result-selection aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("open second result") == true), "Command discovery should include ordinal open-result aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("reveal file result 1") == true), "Command discovery should include reveal-result aliases.");
    Require(commands.Any(command => command.Category == "File search" && command.Phrase == "search my files for budget" && command.Examples?.Contains("show result folder 2") == true), "Command discovery should include folder-reveal aliases.");
    Require(commands.Any(command => command.Category == "File results" && command.Phrase == "open file result 1" && command.Examples?.Contains("open folder result 2") == true), "Command discovery should include folder-result open aliases.");
    Require(commands.Any(command => command.Category == "File results" && command.Phrase == "open file result 1" && command.Examples?.Contains("open containing folder for result 1") == true && command.Examples.Contains("show containing folder for result 2")), "Command discovery should include containing-folder result aliases.");
    Require(commands.Any(command => command.Category == "System" && command.Phrase == "snap window left" && command.Examples?.Contains("close active app") == true), "Command discovery should include visible close-window aliases.");
    Require(commands.Any(command => command.Source.Contains("Built-in Free core", StringComparison.OrdinalIgnoreCase) && command.Phrase == "Callsign"), "Command discovery should label the free core as the built-in source.");
    Require(commands.Any(command => command.Source.Contains("Sample Pack", StringComparison.OrdinalIgnoreCase) && command.Phrase == "sample pack echo"), "Command discovery should include extension pack commands.");
    Require(commands.Any(command => command.Source.Contains("Sample Pack", StringComparison.OrdinalIgnoreCase) && command.Source.Contains("Free", StringComparison.OrdinalIgnoreCase) && command.Source.Contains("Available", StringComparison.OrdinalIgnoreCase)), "Command discovery should include extension pack tier and availability.");
    Require(commands.Any(command => command.Phrase == "show numbers" && command.VoicePhrases?.Contains("show control numbers") == true), "Command discovery should include visible control aliases.");
    Require(commands.Any(command => command.Source.Contains("Sample Pack", StringComparison.OrdinalIgnoreCase) && command.Phrase == "sample pack echo" && command.VoicePhrases?.Contains("sample pack say") == true), "Command discovery should include extension pack aliases.");

    var helpText = CommandDiscoveryService.BuildHelpText(registry);
    Require(helpText.Contains("Callsign command palette", StringComparison.OrdinalIgnoreCase), "Help text should identify the command palette.");
    Require(helpText.Contains("Try getting started to reopen the setup walkthrough.", StringComparison.OrdinalIgnoreCase), "Help text should surface the getting-started walkthrough.");
    Require(helpText.Contains("tier:free", StringComparison.OrdinalIgnoreCase), "Help text should surface structured tier search tips.");
    Require(helpText.Contains("source:Built-in Free core", StringComparison.OrdinalIgnoreCase), "Help text should surface built-in free-core source search tips.");
    Require(helpText.Contains("status:disabled", StringComparison.OrdinalIgnoreCase), "Help text should surface structured status search tips.");
    Require(helpText.Contains("source:Sample Pack", StringComparison.OrdinalIgnoreCase), "Help text should surface source search tips.");
    Require(helpText.Contains("sample pack echo", StringComparison.OrdinalIgnoreCase), "Help text should include extension command phrases.");
    Require(helpText.Contains("show numbers on notepad", StringComparison.OrdinalIgnoreCase), "Help text should include named-window visible-controls aliases.");
    Require(helpText.Contains("show numbers on taskbar", StringComparison.OrdinalIgnoreCase), "Help text should include built-in aliases.");
    Require(helpText.Contains("mouse grid 114", StringComparison.OrdinalIgnoreCase), "Help text should include current-scope mouse-grid shortcut aliases.");
    Require(helpText.Contains("mark four", StringComparison.OrdinalIgnoreCase), "Help text should include mouse-grid mark aliases.");
    Require(helpText.Contains("undo that", StringComparison.OrdinalIgnoreCase), "Help text should include mouse-grid undo aliases.");
    Require(helpText.Contains("Try:", StringComparison.OrdinalIgnoreCase), "Help text should include compact example hints.");
    Require(helpText.Contains("find privacy policy on this page", StringComparison.OrdinalIgnoreCase), "Help text should include natural browser find examples.");
    Require(helpText.Contains("start scrolling down", StringComparison.OrdinalIgnoreCase), "Help text should include continuous browser scrolling examples.");
    Require(helpText.Contains("fix previous word with hello", StringComparison.OrdinalIgnoreCase), "Help text should include natural dictation fix examples.");
    Require(helpText.Contains("nudge up", StringComparison.OrdinalIgnoreCase), "Help text should include short pointer nudge examples.");
    Require(helpText.Contains("dismiss", StringComparison.OrdinalIgnoreCase), "Help text should include dismiss as an escape-key example.");
    Require(helpText.Contains("press tab five times", StringComparison.OrdinalIgnoreCase), "Help text should include repeated keypress examples.");
    Require(helpText.Contains("cancel: stop the current session", StringComparison.OrdinalIgnoreCase), "Help text should include cancel as a visible safety command.");
    Require(helpText.Contains("stop listening: stop voice capture", StringComparison.OrdinalIgnoreCase), "Help text should include stop-listening as a visible safety command.");
    Require(helpText.Contains("reset session: return to idle", StringComparison.OrdinalIgnoreCase), "Help text should include reset-session as a visible safety command.");
}

static void CommandPaletteFiltersCommandsWithStatus()
{
    var registry = PackTestSupport.CreateRegistry();
    registry.RegisterPack(new SampleCommandPack());
    registry.RegisterPack(new FreshIdentityCommandPack());
    registry.RegisterPack(new ExternalSideEffectCommandPack());
    registry.RegisterPack(new BlockedCommandPack());
    var commands = CommandDiscoveryService.GetCommands(registry);

    using var palette = new CommandPaletteForm();
    palette.ShowPalette(null!, commands);
    Require(palette.VisibleCommandCount == commands.Count, $"Expected all commands to be visible, got {palette.VisibleCommandCount} of {commands.Count}.");
    Require(palette.VisibleCategoryCount > 1, $"Expected grouped categories in the palette, got {palette.VisibleCategoryCount}.");
    Require(palette.ResultSummaryText.Contains("available", StringComparison.OrdinalIgnoreCase), $"Expected available-count status, got '{palette.ResultSummaryText}'.");
    Require(palette.ScopeSummaryText.Contains("Scope:", StringComparison.OrdinalIgnoreCase), $"Expected palette scope summary, got '{palette.ScopeSummaryText}'.");
    Require(palette.ScopeSummaryText.Contains("Free", StringComparison.OrdinalIgnoreCase), $"Expected palette scope summary to mention Free counts, got '{palette.ScopeSummaryText}'.");
    Require(palette.ScopeSummaryText.Contains("Pro", StringComparison.OrdinalIgnoreCase), $"Expected palette scope summary to mention Pro counts, got '{palette.ScopeSummaryText}'.");
    Require(palette.ScopeSummaryText.Contains("Advanced", StringComparison.OrdinalIgnoreCase), $"Expected palette scope summary to mention Advanced counts, got '{palette.ScopeSummaryText}'.");
    RequireVisualContract(palette.VisualStyleName, "command palette");
    Require(palette.SurfaceAccessibleName.Contains("command palette", StringComparison.OrdinalIgnoreCase), $"Expected palette surface accessibility metadata, got '{palette.SurfaceAccessibleName}'.");
    Require(palette.SurfaceAccessibleDescription.Contains("tier", StringComparison.OrdinalIgnoreCase), $"Expected palette surface accessibility description to include tier context, got '{palette.SurfaceAccessibleDescription}'.");
    Require(palette.SurfaceAccessibleDescription.Contains("availability", StringComparison.OrdinalIgnoreCase), $"Expected palette surface accessibility description to include availability, got '{palette.SurfaceAccessibleDescription}'.");
    Require(palette.SurfaceAccessibleDescription.Contains("risk", StringComparison.OrdinalIgnoreCase), $"Expected palette surface accessibility description to include risk context, got '{palette.SurfaceAccessibleDescription}'.");
    Require(palette.TitleAccessibleName.Contains("Command palette", StringComparison.OrdinalIgnoreCase), $"Expected palette title accessibility metadata, got '{palette.TitleAccessibleName}'.");
    Require(palette.CloseButtonAccessibleName.Contains("Close command palette", StringComparison.OrdinalIgnoreCase), $"Expected palette close-button accessibility metadata, got '{palette.CloseButtonAccessibleName}'.");
    Require(palette.CloseButtonText == "\u00D7", $"Expected palette close glyph to use a clean multiply sign, got '{palette.CloseButtonText}'.");
    Require(ReferenceEquals(palette.CancelButton, EnumerateControls(palette).OfType<Button>().FirstOrDefault(control => string.Equals(control.AccessibleName, "Close command palette", StringComparison.OrdinalIgnoreCase))), "Expected Escape to dismiss the command palette.");
    Require(palette.SubtitleAccessibleName.Contains("session", StringComparison.OrdinalIgnoreCase), $"Expected palette subtitle accessibility metadata, got '{palette.SubtitleAccessibleName}'.");
    Require(palette.QuickFiltersAccessibleName.Contains("quick filters", StringComparison.OrdinalIgnoreCase), $"Expected palette quick-filter accessibility metadata, got '{palette.QuickFiltersAccessibleName}'.");
    Require(palette.QuickFiltersAccessibleDescription.Contains("Free commands", StringComparison.OrdinalIgnoreCase), $"Expected palette quick-filter description to mention Free commands, got '{palette.QuickFiltersAccessibleDescription}'.");
    Require(palette.QuickFiltersAccessibleDescription.Contains("extension commands", StringComparison.OrdinalIgnoreCase), $"Expected palette quick-filter description to mention extension commands, got '{palette.QuickFiltersAccessibleDescription}'.");
    Require(palette.QuickFilterTexts.Contains("All", StringComparison.OrdinalIgnoreCase), $"Expected All quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Free", StringComparison.OrdinalIgnoreCase), $"Expected Free quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Launch", StringComparison.OrdinalIgnoreCase), $"Expected Launch quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Navigate", StringComparison.OrdinalIgnoreCase), $"Expected Navigate quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Profile", StringComparison.OrdinalIgnoreCase), $"Expected Profile quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Runtime", StringComparison.OrdinalIgnoreCase), $"Expected Runtime quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Updates", StringComparison.OrdinalIgnoreCase), $"Expected Updates quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase), $"Expected Diagnostics quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Help", StringComparison.OrdinalIgnoreCase), $"Expected Help quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("System", StringComparison.OrdinalIgnoreCase), $"Expected System quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Browser", StringComparison.OrdinalIgnoreCase), $"Expected Browser quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Files", StringComparison.OrdinalIgnoreCase), $"Expected Files quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Keyboard", StringComparison.OrdinalIgnoreCase), $"Expected Keyboard quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Mouse", StringComparison.OrdinalIgnoreCase), $"Expected Mouse quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Visible Controls", StringComparison.OrdinalIgnoreCase), $"Expected Visible Controls quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Settings", StringComparison.OrdinalIgnoreCase), $"Expected Settings quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Media", StringComparison.OrdinalIgnoreCase), $"Expected Media quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Window", StringComparison.OrdinalIgnoreCase), $"Expected Window quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Editing", StringComparison.OrdinalIgnoreCase), $"Expected Editing quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Approval", StringComparison.OrdinalIgnoreCase), $"Expected Approval quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Fresh ID", StringComparison.OrdinalIgnoreCase), $"Expected Fresh ID quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("No Approval", StringComparison.OrdinalIgnoreCase), $"Expected No Approval quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Risk", StringComparison.OrdinalIgnoreCase), $"Expected Risk quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Observe", StringComparison.OrdinalIgnoreCase), $"Expected Observe quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Local", StringComparison.OrdinalIgnoreCase), $"Expected Local quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("External", StringComparison.OrdinalIgnoreCase), $"Expected External quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Blocked", StringComparison.OrdinalIgnoreCase), $"Expected Blocked quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Safety", StringComparison.OrdinalIgnoreCase), $"Expected Safety quick filter, got '{palette.QuickFilterTexts}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "All", StringComparison.OrdinalIgnoreCase), $"Expected All to be the default selected filter, got '{palette.ActiveQuickFilterText}'.");
    Require(palette.QuickFilterTexts.Contains("Pro", StringComparison.OrdinalIgnoreCase), $"Expected Pro quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Advanced", StringComparison.OrdinalIgnoreCase), $"Expected Advanced quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Disabled", StringComparison.OrdinalIgnoreCase), $"Expected Disabled quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Gated", StringComparison.OrdinalIgnoreCase), $"Expected Gated quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Built-in", StringComparison.OrdinalIgnoreCase), $"Expected Built-in quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Dictation", StringComparison.OrdinalIgnoreCase), $"Expected Dictation quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Visible", StringComparison.OrdinalIgnoreCase), $"Expected Visible quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.QuickFilterTexts.Contains("Extensions", StringComparison.OrdinalIgnoreCase), $"Expected Extensions quick filter, got '{palette.QuickFilterTexts}'.");
    Require(palette.SearchAccessibleName.Contains("Command search", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility metadata, got '{palette.SearchAccessibleName}'.");
    Require(palette.SearchAccessibleDescription.Contains("tiers", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description to include tier metadata, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.SearchAccessibleDescription.Contains("extension sources", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.SearchAccessibleDescription.Contains("tier:pro", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description to mention structured tier filters, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.SearchAccessibleDescription.Contains("status:disabled", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description to mention structured status filters, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.SearchAccessibleDescription.Contains("category", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description to mention category filters, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.SearchAccessibleDescription.Contains("settings", StringComparison.OrdinalIgnoreCase), $"Expected palette search accessibility description to mention settings filters, got '{palette.SearchAccessibleDescription}'.");
    Require(palette.ResultAccessibleName.Contains("result", StringComparison.OrdinalIgnoreCase), $"Expected palette result accessibility metadata, got '{palette.ResultAccessibleName}'.");
    Require(palette.SafetyAccessibleName.Contains("safety", StringComparison.OrdinalIgnoreCase), $"Expected palette safety accessibility metadata, got '{palette.SafetyAccessibleName}'.");
    Require(palette.DetailsAccessibleName.Contains("details", StringComparison.OrdinalIgnoreCase), $"Expected palette details accessibility metadata, got '{palette.DetailsAccessibleName}'.");
    Require(palette.DetailsAccessibleDescription.Contains("tier", StringComparison.OrdinalIgnoreCase), $"Expected palette details accessibility description to include tier context, got '{palette.DetailsAccessibleDescription}'.");
    Require(palette.DetailsAccessibleDescription.Contains("availability", StringComparison.OrdinalIgnoreCase), $"Expected palette details accessibility description to include availability context, got '{palette.DetailsAccessibleDescription}'.");
    Require(palette.DetailsAccessibleDescription.Contains("approval", StringComparison.OrdinalIgnoreCase), $"Expected palette details accessibility description to include approval context, got '{palette.DetailsAccessibleDescription}'.");
    Require(palette.ResultsAccessibleName.Contains("results", StringComparison.OrdinalIgnoreCase), $"Expected palette results accessibility metadata, got '{palette.ResultsAccessibleName}'.");
    Require(palette.ResultsAccessibleDescription.Contains("tier", StringComparison.OrdinalIgnoreCase), $"Expected palette results accessibility description to include tier context, got '{palette.ResultsAccessibleDescription}'.");
    Require(palette.ResultsAccessibleDescription.Contains("availability", StringComparison.OrdinalIgnoreCase), $"Expected palette results accessibility description to include availability context, got '{palette.ResultsAccessibleDescription}'.");
    Require(palette.ResultsAccessibleDescription.Contains("approval", StringComparison.OrdinalIgnoreCase), $"Expected palette results accessibility description to include approval context, got '{palette.ResultsAccessibleDescription}'.");
    Require(palette.DetailsText.Contains("Select a command", StringComparison.OrdinalIgnoreCase) || palette.DetailsText.Contains("command", StringComparison.OrdinalIgnoreCase), $"Expected palette details text to be populated, got '{palette.DetailsText}'.");
    Require(palette.DetailsText.Contains("aliases:", StringComparison.OrdinalIgnoreCase), $"Expected palette details to show aliases, got '{palette.DetailsText}'.");
    Require(palette.DetailsText.Contains("Free", StringComparison.OrdinalIgnoreCase), $"Expected palette details to show tier metadata, got '{palette.DetailsText}'.");
    Require(palette.DetailsText.Contains("Available", StringComparison.OrdinalIgnoreCase), $"Expected palette details to show availability, got '{palette.DetailsText}'.");
    Require(string.Equals(palette.FirstVisibleTierText, "Free", StringComparison.OrdinalIgnoreCase), $"Expected visible tier column to show Free, got '{palette.FirstVisibleTierText}'.");
    Require(string.Equals(palette.FirstVisibleAvailabilityText, "Available", StringComparison.OrdinalIgnoreCase), $"Expected visible availability column to show Available, got '{palette.FirstVisibleAvailabilityText}'.");
    Require(palette.SafetySummaryText.Contains("cancel", StringComparison.OrdinalIgnoreCase), $"Expected palette safety summary to mention cancel, got '{palette.SafetySummaryText}'.");
    Require(palette.SafetySummaryText.Contains("stop listening", StringComparison.OrdinalIgnoreCase), $"Expected palette safety summary to mention stop listening, got '{palette.SafetySummaryText}'.");
    Require(palette.SafetySummaryText.Contains("reset session", StringComparison.OrdinalIgnoreCase), $"Expected palette safety summary to mention reset session, got '{palette.SafetySummaryText}'.");
    Require(!string.IsNullOrWhiteSpace(palette.SelectedCommandPhrase), "Palette should select the first visible command by default.");

    palette.SetSearchText("dictation");
    Require(palette.VisibleCommandCount > 0, "Filtering for dictation should keep dictation commands visible.");
    Require(palette.VisibleCommandCount < commands.Count, "Filtering for dictation should reduce visible command count.");
    Require(palette.ResultSummaryText.Contains("match", StringComparison.OrdinalIgnoreCase), $"Expected filtered-match status, got '{palette.ResultSummaryText}'.");
    Require(palette.DetailsText.Contains("dictation", StringComparison.OrdinalIgnoreCase), $"Expected details text to follow the filtered command, got '{palette.DetailsText}'.");
    Require(palette.DetailsText.Contains("Try:", StringComparison.OrdinalIgnoreCase), $"Expected details text to show compact examples, got '{palette.DetailsText}'.");

    palette.SetSearchText("safety");
    Require(palette.VisibleCommandCount > 0, "Filtering for the Safety quick-filter token should keep stop/cancel/reset commands visible.");
    Require(
        palette.DetailsText.Contains("cancel", StringComparison.OrdinalIgnoreCase)
        || palette.DetailsText.Contains("stop", StringComparison.OrdinalIgnoreCase)
        || palette.DetailsText.Contains("reset", StringComparison.OrdinalIgnoreCase)
        || palette.DetailsText.Contains("dismiss", StringComparison.OrdinalIgnoreCase),
        $"Expected safety-filter details to mention a stop/cancel/reset-style command, got '{palette.DetailsText}'.");

    palette.SetSearchText("visible controls");
    Require(palette.VisibleCommandCount > 0, "Filtering for the Visible quick-filter token should keep visible-control commands visible.");
    Require(palette.DetailsText.Contains("visible", StringComparison.OrdinalIgnoreCase), $"Expected visible-filter details to mention visible commands, got '{palette.DetailsText}'.");

    palette.SetSearchText("fix previous word with hello");
    Require(palette.DetailsText.Contains("fix previous word with hello", StringComparison.OrdinalIgnoreCase), $"Expected details text to surface representative late dictation examples, got '{palette.DetailsText}'.");
    Require(palette.SelectedCommandPhrase?.Contains("dictation", StringComparison.OrdinalIgnoreCase) == true, $"Expected selected command to follow the filtered command, got '{palette.SelectedCommandPhrase}'.");

    palette.SetSearchText("add womprat to vocabulary");
    Require(palette.VisibleCommandCount == 1, $"Expected one vocabulary command result, got {palette.VisibleCommandCount}.");
    Require(palette.DetailsText.Contains("add womprat to vocabulary", StringComparison.OrdinalIgnoreCase), $"Expected details text to show local vocabulary examples, got '{palette.DetailsText}'.");

    palette.SetSearchText("find privacy policy on this page");
    Require(palette.VisibleCommandCount == 1, $"Expected one natural browser-find result, got {palette.VisibleCommandCount}.");
    Require(palette.DetailsText.Contains("find privacy policy on this page", StringComparison.OrdinalIgnoreCase), $"Expected details text to show natural browser find examples, got '{palette.DetailsText}'.");

    palette.SetSearchText("sample pack echo");
    Require(palette.VisibleCommandCount == 1, $"Expected one sample-pack result, got {palette.VisibleCommandCount}.");

    palette.SetSearchText("sample pack say");
    Require(palette.VisibleCommandCount == 1, $"Expected one sample-pack alias result, got {palette.VisibleCommandCount}.");
    Require(palette.DetailsText.Contains("sample pack say", StringComparison.OrdinalIgnoreCase), $"Expected alias details to show the alternate phrase, got '{palette.DetailsText}'.");

    palette.SetSearchText("start taking dictation");
    Require(palette.VisibleCommandCount == 1, $"Expected one start-taking-dictation result, got {palette.VisibleCommandCount}.");
    Require(
        string.Equals(palette.FirstVisibleApprovalText, "Approval", StringComparison.OrdinalIgnoreCase),
        $"Expected dictation command approval metadata to remain visible, got '{palette.FirstVisibleApprovalText}'.");

    palette.SetSearchText("approval");
    Require(palette.VisibleCommandCount > 0, "Filtering for approval should find approval-gated commands.");
    Require(palette.ResultSummaryText.Contains("match", StringComparison.OrdinalIgnoreCase), $"Expected approval filtered-match status, got '{palette.ResultSummaryText}'.");
    Require(palette.FirstVisibleApprovalText?.Contains("Approval", StringComparison.OrdinalIgnoreCase) == true, $"Expected approval filter to surface approval metadata, got '{palette.FirstVisibleApprovalText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Approval", StringComparison.OrdinalIgnoreCase), $"Expected Approval chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("local change");
    Require(palette.VisibleCommandCount > 0, "Filtering for local change should find local-state-change commands by formatted risk text.");
    Require(palette.DetailsText.Contains("Local change", StringComparison.OrdinalIgnoreCase), $"Expected local-change filter to surface risk metadata, got '{palette.DetailsText}'.");

    palette.SetSearchText("approval:fresh");
    Require(palette.VisibleCommandCount > 0, "Filtering for fresh identity should find fresh-identity approval commands.");
    Require(palette.FirstVisibleApprovalText?.Contains("Fresh ID", StringComparison.OrdinalIgnoreCase) == true, $"Expected fresh-identity filter to surface fresh ID metadata, got '{palette.FirstVisibleApprovalText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Fresh ID", StringComparison.OrdinalIgnoreCase), $"Expected Fresh ID chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("approval:none");
    Require(palette.VisibleCommandCount > 0, "Filtering for approval:none should keep approval-free commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "No Approval", StringComparison.OrdinalIgnoreCase), $"Expected No Approval chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:system");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:system should keep system commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "System", StringComparison.OrdinalIgnoreCase), $"Expected System chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:browser tabs");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:browser tabs should keep browser commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Browser", StringComparison.OrdinalIgnoreCase), $"Expected Browser chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:files tab");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:files tab should keep file commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Files", StringComparison.OrdinalIgnoreCase), $"Expected Files chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:keyboard");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:keyboard should keep keyboard commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Keyboard", StringComparison.OrdinalIgnoreCase), $"Expected Keyboard chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:mouse grid");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:mouse grid should keep mouse commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Mouse", StringComparison.OrdinalIgnoreCase), $"Expected Mouse chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:visible controls");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:visible controls should keep visibility-related commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Visible", StringComparison.OrdinalIgnoreCase), $"Expected Visible chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("visible controls");
    Require(palette.VisibleCommandCount > 0, "Filtering for visible controls should keep visible-control commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Visible Controls", StringComparison.OrdinalIgnoreCase), $"Expected Visible Controls chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:app launch");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:app launch should keep app-launch commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Launch", StringComparison.OrdinalIgnoreCase), $"Expected Launch chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:navigation");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:navigation should keep navigation commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Navigate", StringComparison.OrdinalIgnoreCase), $"Expected Navigate chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:profile setup");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:profile setup should keep profile commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Profile", StringComparison.OrdinalIgnoreCase), $"Expected Profile chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:runtime");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:runtime should keep runtime commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Runtime", StringComparison.OrdinalIgnoreCase), $"Expected Runtime chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:updates");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:updates should keep update commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Updates", StringComparison.OrdinalIgnoreCase), $"Expected Updates chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:diagnostics");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:diagnostics should keep diagnostics commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Diagnostics", StringComparison.OrdinalIgnoreCase), $"Expected Diagnostics chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:help");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:help should keep help commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Help", StringComparison.OrdinalIgnoreCase), $"Expected Help chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("settings");
    Require(palette.VisibleCommandCount > 0, "Filtering for settings should keep settings commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Settings", StringComparison.OrdinalIgnoreCase), $"Expected Settings chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("media");
    Require(palette.VisibleCommandCount > 0, "Filtering for media should keep media commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Media", StringComparison.OrdinalIgnoreCase), $"Expected Media chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("window");
    Require(palette.VisibleCommandCount > 0, "Filtering for window should keep window commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Window", StringComparison.OrdinalIgnoreCase), $"Expected Window chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:editing");
    Require(palette.VisibleCommandCount > 0, "Filtering for category:editing should keep editing commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Editing", StringComparison.OrdinalIgnoreCase), $"Expected Editing chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("risk:observe");
    Require(palette.VisibleCommandCount > 0, "Filtering for risk:observe should keep observe-only commands visible.");
    Require(palette.DetailsText.Contains("Observe", StringComparison.OrdinalIgnoreCase), $"Expected observe-risk filter to surface observe metadata, got '{palette.DetailsText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Observe", StringComparison.OrdinalIgnoreCase), $"Expected Observe chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("risk:external");
    Require(palette.VisibleCommandCount > 0, "Filtering for risk:external should keep external-side-effect commands visible.");
    Require(palette.DetailsText.Contains("External", StringComparison.OrdinalIgnoreCase), $"Expected external-risk filter to surface external metadata, got '{palette.DetailsText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "External", StringComparison.OrdinalIgnoreCase), $"Expected External chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("risk:blocked");
    Require(palette.VisibleCommandCount > 0, "Filtering for risk:blocked should keep blocked commands visible.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Blocked", StringComparison.OrdinalIgnoreCase), $"Expected Blocked chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("status:available");
    Require(palette.VisibleCommandCount > 0, "Filtering for status:available should keep available commands visible.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("Available", StringComparison.OrdinalIgnoreCase) == true, $"Expected available-status filter to show available metadata, got '{palette.FirstVisibleAvailabilityText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Available", StringComparison.OrdinalIgnoreCase), $"Expected Available chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    var gatedRegistry = PackTestSupport.CreateRegistry();
    gatedRegistry.RegisterPack(new PaidSampleCommandPack());
    var gatedCommands = CommandDiscoveryService.GetCommands(gatedRegistry);
    palette.ShowPalette(null!, gatedCommands);
    palette.SetSearchText("paid sample action");
    Require(palette.VisibleCommandCount == 1, $"Expected one gated paid-pack result, got {palette.VisibleCommandCount}.");
    Require(string.Equals(palette.FirstVisibleTierText, "Pro", StringComparison.OrdinalIgnoreCase), $"Expected visible tier column to show Pro, got '{palette.FirstVisibleTierText}'.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("Pro entitlement required", StringComparison.OrdinalIgnoreCase) == true, $"Expected visible availability column to show Pro entitlement gate, got '{palette.FirstVisibleAvailabilityText}'.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("will not route", StringComparison.OrdinalIgnoreCase) == true, $"Expected visible availability column to say gated commands will not route, got '{palette.FirstVisibleAvailabilityText}'.");
    Require(palette.DetailsText.Contains("Pro entitlement required", StringComparison.OrdinalIgnoreCase), $"Expected palette details to show the Pro entitlement gate, got '{palette.DetailsText}'.");
    Require(palette.DetailsText.Contains("will not route", StringComparison.OrdinalIgnoreCase), $"Expected palette details to say unentitled commands will not route, got '{palette.DetailsText}'.");

    palette.SetSearchText("extension");
    Require(palette.VisibleCommandCount > 0, $"Expected extension quick-filter token to surface extension commands, got {palette.VisibleCommandCount}.");
    Require(palette.VisibleCommandCount < commands.Count, $"Expected extension quick-filter token to narrow the list, got {palette.VisibleCommandCount} of {commands.Count}.");
    Require(palette.DetailsText.Contains("Paid Sample Pack", StringComparison.OrdinalIgnoreCase), $"Expected extension filter to surface pack source details, got '{palette.DetailsText}'.");

    palette.SetSearchText("tier:pro");
    Require(palette.VisibleCommandCount == 1, $"Expected structured Pro tier filter to find the gated paid-pack result, got {palette.VisibleCommandCount}.");
    Require(palette.FirstVisibleTierText?.Equals("Pro", StringComparison.OrdinalIgnoreCase) == true, $"Expected structured Pro tier filter to show Pro tier, got '{palette.FirstVisibleTierText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Pro", StringComparison.OrdinalIgnoreCase), $"Expected Pro chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("status:gated");
    Require(palette.VisibleCommandCount == 1, $"Expected structured gated-status filter to find the gated paid-pack result, got {palette.VisibleCommandCount}.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("will not route", StringComparison.OrdinalIgnoreCase) == true, $"Expected gated-status filter to show routing gate, got '{palette.FirstVisibleAvailabilityText}'.");

    palette.SetSearchText("Pro entitlement");
    Require(palette.VisibleCommandCount == 1, $"Expected Pro entitlement search to find the gated paid-pack result, got {palette.VisibleCommandCount}.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("Pro entitlement required", StringComparison.OrdinalIgnoreCase) == true, $"Expected Pro entitlement search to show the entitlement gate, got '{palette.FirstVisibleAvailabilityText}'.");

    var disabledRegistry = PackTestSupport.CreateRegistry();
    disabledRegistry.ImportPack(typeof(SampleCommandPack).Assembly.Location);
    palette.ShowPalette(null!, CommandDiscoveryService.GetCommands(disabledRegistry));
    palette.SetSearchText("status:disabled");
    Require(palette.VisibleCommandCount == 1, $"Expected structured disabled-status filter to find the imported pack, got {palette.VisibleCommandCount}.");
    Require(palette.FirstVisibleAvailabilityText?.Contains("Disabled", StringComparison.OrdinalIgnoreCase) == true, $"Expected disabled-status filter to show the disabled gate, got '{palette.FirstVisibleAvailabilityText}'.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Disabled", StringComparison.OrdinalIgnoreCase), $"Expected Disabled chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("source:built-in");
    Require(palette.VisibleCommandCount > 0, "Expected built-in source filter to keep built-in commands visible.");
    Require(palette.VisibleCommandCount < commands.Count, "Expected built-in source filter to narrow the list.");
    Require(string.Equals(palette.ActiveQuickFilterText, "Built-in", StringComparison.OrdinalIgnoreCase), $"Expected Built-in chip to be highlighted, got '{palette.ActiveQuickFilterText}'.");

    palette.SetSearchText("category:system");
    Require(palette.VisibleCommandCount > 0, "Expected category:system filter to keep system commands visible.");
    Require(palette.DetailsText.Contains("System", StringComparison.OrdinalIgnoreCase), $"Expected system category filter to surface system details, got '{palette.DetailsText}'.");

    palette.SetSearchText("category:browser");
    Require(palette.VisibleCommandCount > 0, "Expected category:browser filter to keep browser commands visible.");
    Require(palette.DetailsText.Contains("Browser", StringComparison.OrdinalIgnoreCase), $"Expected browser category filter to surface browser details, got '{palette.DetailsText}'.");

    palette.SetSearchText("category:dictation");
    Require(palette.VisibleCommandCount > 0, "Expected category:dictation filter to keep dictation commands visible.");
    Require(palette.DetailsText.Contains("Dictation", StringComparison.OrdinalIgnoreCase), $"Expected dictation category filter to surface dictation details, got '{palette.DetailsText}'.");

    palette.ShowPalette(null!, gatedCommands);
    palette.SetSearchText("will not route");
    Require(palette.VisibleCommandCount == 1, $"Expected will-not-route search to find the gated paid-pack result, got {palette.VisibleCommandCount}.");
    Require(palette.DetailsText.Contains("will not route", StringComparison.OrdinalIgnoreCase), $"Expected will-not-route search to keep routing status visible, got '{palette.DetailsText}'.");
}

static void VerifiedSessionRoutesBuiltInParityFamilies()
{
    var repoRoot = FindRepositoryRoot();
    var uiPath = Path.Combine(repoRoot, "src", "Callsign.UI", "MainForm.cs");
    Require(File.Exists(uiPath), $"Could not find UI source at {uiPath}.");

    var source = File.ReadAllText(uiPath);
    var waitingForCommandStart = source.IndexOf("if (_session.State == AlphaSessionState.WaitingForCommand)", StringComparison.OrdinalIgnoreCase);
    Require(waitingForCommandStart >= 0, "Main form should have a verified command-session branch.");
    var waitingForCommandSource = source[waitingForCommandStart..];

    var systemIntentCheck = waitingForCommandSource.IndexOf("uiNavigationIntent.Kind == AlphaVoiceIntentKind.SystemControl", StringComparison.OrdinalIgnoreCase);
    var fileIntentCheck = waitingForCommandSource.IndexOf("uiNavigationIntent.Kind == AlphaVoiceIntentKind.FileSearch", StringComparison.OrdinalIgnoreCase);
    var dictationIntentCheck = waitingForCommandSource.IndexOf("uiNavigationIntent.Kind == AlphaVoiceIntentKind.Dictation", StringComparison.OrdinalIgnoreCase);
    var startMenuFallback = waitingForCommandSource.IndexOf("CaptureCommand();", StringComparison.OrdinalIgnoreCase);

    Require(systemIntentCheck >= 0, "Verified command branch should handle system-control intents.");
    Require(fileIntentCheck >= 0, "Verified command branch should handle file-search intents.");
    Require(dictationIntentCheck >= 0, "Verified command branch should handle dictation intents.");
    Require(startMenuFallback >= 0, "Verified command branch should still keep the Start menu launch fallback.");
    Require(systemIntentCheck < startMenuFallback, "System-control intents should execute before app-launch fallback.");
    Require(fileIntentCheck < startMenuFallback, "File-search intents should execute before app-launch fallback.");
    Require(dictationIntentCheck < startMenuFallback, "Dictation intents should execute before app-launch fallback.");

    Require(source.Contains("private bool TryAuthorizeBuiltInIntent(AlphaVoiceIntent intent", StringComparison.OrdinalIgnoreCase), "Built-in parity commands should share a policy authorization helper.");
    Require(source.Contains("CallsignCommandPolicy.Evaluate(definition, identityVerified, freshIdentity)", StringComparison.OrdinalIgnoreCase), "Built-in parity command authorization should use the policy engine.");
    Require(source.Contains("AuditBuiltInPolicyDecision(intent, \"allowed\"", StringComparison.OrdinalIgnoreCase), "Built-in parity commands should audit allowed policy decisions.");
    Require(source.Contains("verificationMethod: \"policy_evaluation\"", StringComparison.OrdinalIgnoreCase), "Built-in policy audit records should include policy-evaluation verification metadata.");
    Require(source.Contains("Built-in command passed Callsign policy evaluation", StringComparison.OrdinalIgnoreCase), "Built-in policy audit records should summarize allowed policy verification.");
    Require(source.Contains("Built-in command was blocked by Callsign policy evaluation", StringComparison.OrdinalIgnoreCase), "Built-in policy audit records should summarize blocked policy verification.");
    Require(source.Contains("AccessibleName = \"Account save\"", StringComparison.OrdinalIgnoreCase), "Account save button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Account train voice identity\"", StringComparison.OrdinalIgnoreCase), "Account voice-training button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice record sample\"", StringComparison.OrdinalIgnoreCase), "Voice record button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice reset\"", StringComparison.OrdinalIgnoreCase), "Voice reset button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Session start listening\"", StringComparison.OrdinalIgnoreCase), "Session start-listening button should expose a spoken-label accessible name.");
    Require(source.Contains("voice access wake up, wake up, unmute microphone", StringComparison.OrdinalIgnoreCase), "Session start-listening button should expose Voice Access-style wake aliases.");
    Require(source.Contains("voice access sleep, go to sleep, turn off microphone, turn off voice access, stop voice access, close voice access, exit voice access, quit voice access, mute microphone", StringComparison.OrdinalIgnoreCase), "Session stop-listening button should expose Voice Access-style sleep and exit aliases.");
    Require(source.Contains("AccessibleName = \"Voice mode commands only\"", StringComparison.OrdinalIgnoreCase), "Voice mode commands-only control should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice mode dictation only\"", StringComparison.OrdinalIgnoreCase), "Voice mode dictation-only control should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice mode default\"", StringComparison.OrdinalIgnoreCase), "Voice mode default control should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: commands only mode, start command mode, turn off dictation mode.", StringComparison.OrdinalIgnoreCase), "Voice mode commands-only control should expose its spoken phrases.");
    Require(source.Contains("Voice phrases: default mode, commands and dictation mode, commands plus dictation mode.", StringComparison.OrdinalIgnoreCase), "Voice mode default control should expose its spoken phrases.");
    Require(source.Contains("AccessibleName = \"Session verify callsign\"", StringComparison.OrdinalIgnoreCase), "Session verify button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: confirm app, 1, click 1, choose result one.", StringComparison.OrdinalIgnoreCase), "Session confirm-app button should expose numbered app-choice phrases.");
    Require(source.Contains("Visible numbered app choices for ambiguous launch requests.", StringComparison.OrdinalIgnoreCase), "Session app-choice list should describe the visible numbered-choice mode.");
    Require(source.Contains("next app choice", StringComparison.OrdinalIgnoreCase) && source.Contains("previous app choice", StringComparison.OrdinalIgnoreCase), "Session app-choice flow should surface spoken next/previous navigation.");
    Require(source.Contains("AccessibleName = \"Session cancel\"", StringComparison.OrdinalIgnoreCase), "Session cancel button should expose a spoken-label accessible name.");
    Require(source.Contains("Browser open request was shown in the visible Browser tab status.", StringComparison.OrdinalIgnoreCase), "Browser open audit should summarize visible success status.");
    Require(source.Contains("Browser open failure was shown in the visible Browser tab status.", StringComparison.OrdinalIgnoreCase), "Browser open audit should summarize visible failure status.");
    Require(source.Contains("Browser action request was shown in the visible Browser tab status.", StringComparison.OrdinalIgnoreCase), "Browser action audit should summarize visible success status.");
    Require(source.Contains("Browser action failure was shown in the visible Browser tab status.", StringComparison.OrdinalIgnoreCase), "Browser action audit should summarize visible failure status.");
    Require(source.Contains("AccessibleName = \"Browser safety\"", StringComparison.OrdinalIgnoreCase), "Browser safety line should expose a spoken-label accessible name.");
    Require(source.Contains("browser targets are web-only", StringComparison.OrdinalIgnoreCase), "Browser safety line should explain the web-only target boundary.");
    Require(source.Contains("file, script, settings, installer, and app schemes are blocked here", StringComparison.OrdinalIgnoreCase), "Browser safety line should explain blocked non-web schemes.");
    Require(source.Contains("do not inspect page contents or run hidden scripts", StringComparison.OrdinalIgnoreCase), "Browser safety line should explain no hidden page inspection or script execution.");
    Require(source.Contains("AccessibleName = \"Browser open or search\"", StringComparison.OrdinalIgnoreCase), "Browser open/search button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser address bar text\"", StringComparison.OrdinalIgnoreCase), "Browser address-text input should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser find text\"", StringComparison.OrdinalIgnoreCase), "Browser find-text input should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser private window\"", StringComparison.OrdinalIgnoreCase), "Browser private-window button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser reopen closed tab\"", StringComparison.OrdinalIgnoreCase), "Browser reopen-closed-tab button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser home\"", StringComparison.OrdinalIgnoreCase), "Browser home button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser downloads\"", StringComparison.OrdinalIgnoreCase), "Browser downloads button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser history\"", StringComparison.OrdinalIgnoreCase), "Browser history button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser send address bar text\"", StringComparison.OrdinalIgnoreCase), "Browser address-text button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: type in address bar example.com, search address bar for callsign.", StringComparison.OrdinalIgnoreCase), "Browser address-text button should expose spoken address-bar phrases.");
    Require(source.Contains("AccessibleName = \"Browser find page text\"", StringComparison.OrdinalIgnoreCase), "Browser find-text button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: search this page for privacy policy, find privacy policy on this page.", StringComparison.OrdinalIgnoreCase), "Browser find-text button should expose spoken page-find phrases.");
    Require(source.Contains("AccessibleName = \"Browser start scrolling up\"", StringComparison.OrdinalIgnoreCase), "Browser start-scrolling-up button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser start scrolling down\"", StringComparison.OrdinalIgnoreCase), "Browser start-scrolling-down button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser start scrolling left\"", StringComparison.OrdinalIgnoreCase), "Browser start-scrolling-left button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser start scrolling right\"", StringComparison.OrdinalIgnoreCase), "Browser start-scrolling-right button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser stop scrolling\"", StringComparison.OrdinalIgnoreCase), "Browser stop-scrolling button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser scroll left\"", StringComparison.OrdinalIgnoreCase), "Browser scroll-left button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser scroll right\"", StringComparison.OrdinalIgnoreCase), "Browser scroll-right button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Browser full screen\"", StringComparison.OrdinalIgnoreCase), "Browser full-screen button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: browser close tab, close browser tab.", StringComparison.OrdinalIgnoreCase), "Browser close-tab button should expose close-tab phrases.");
    Require(source.Contains("AccessibleName = \"Browser scroll to bottom\"", StringComparison.OrdinalIgnoreCase), "Browser scroll-bottom button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: browser zoom reset.", StringComparison.OrdinalIgnoreCase), "Browser zoom-reset button should expose its spoken phrase.");
    Require(source.Contains("System action failure was shown in the visible System tab status.", StringComparison.OrdinalIgnoreCase), "System action execution audit should summarize visible failure status.");
    Require(source.Contains("System action request was shown in the visible System tab status", StringComparison.OrdinalIgnoreCase), "System action execution audit should summarize visible success status.");
    Require(source.Contains("AccessibleName = \"System safety\"", StringComparison.OrdinalIgnoreCase), "System safety line should expose a spoken-label accessible name.");
    Require(source.Contains("system commands stay visible and reversible where possible", StringComparison.OrdinalIgnoreCase), "System safety line should explain the visible reversible-action boundary.");
    Require(source.Contains("does not toggle settings, read clipboard contents, capture screenshots, force-kill apps, or act in hidden windows", StringComparison.OrdinalIgnoreCase), "System safety line should explain blocked hidden or privacy-sensitive behavior.");
    Require(source.Contains("AccessibleName = \"System volume up\"", StringComparison.OrdinalIgnoreCase), "System volume button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: play or pause, play media, pause media.", StringComparison.OrdinalIgnoreCase), "System media button should expose spoken media phrases.");
    Require(source.Contains("Voice phrases: stop media, stop playback.", StringComparison.OrdinalIgnoreCase), "System stop-media button should expose spoken stop phrases.");
    Require(source.Contains("AccessibleName = \"System task view\"", StringComparison.OrdinalIgnoreCase), "System task-view button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System quick settings\"", StringComparison.OrdinalIgnoreCase), "System Quick Settings button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: notification center, show notifications.", StringComparison.OrdinalIgnoreCase), "System Notification Center button should expose spoken notification phrases.");
    Require(source.Contains("AccessibleName = \"System emoji panel\"", StringComparison.OrdinalIgnoreCase), "System emoji-panel button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: clipboard history, open clipboard, show clipboard picker.", StringComparison.OrdinalIgnoreCase), "System clipboard-history button should expose spoken clipboard phrases.");
    Require(source.Contains("AccessibleName = \"System snipping toolbar\"", StringComparison.OrdinalIgnoreCase), "System snipping-toolbar button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: snipping toolbar, show screenshot toolbar, open screenshot tools.", StringComparison.OrdinalIgnoreCase), "System snipping-toolbar button should expose spoken screenshot-tool phrases.");
    Require(source.Contains("AccessibleName = \"System project display\"", StringComparison.OrdinalIgnoreCase), "System project-display button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: cast display, wireless display.", StringComparison.OrdinalIgnoreCase), "System cast-display button should expose spoken cast phrases.");
    Require(source.Contains("Voice phrases: next window, switch to the next app.", StringComparison.OrdinalIgnoreCase), "System next-window button should expose spoken switching phrases.");
    Require(source.Contains("Voice phrases: switch to Edge, go to Notepad.", StringComparison.OrdinalIgnoreCase), "System named-window switch button should expose spoken app-switch phrases.");
    Require(source.Contains("Visible numbered window choices for app switching.", StringComparison.OrdinalIgnoreCase), "System window-choice list should describe the visible numbered-choice mode.");
    Require(source.Contains("AccessibleName = \"System snap window left\"", StringComparison.OrdinalIgnoreCase), "System snap button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: show snap layouts.", StringComparison.OrdinalIgnoreCase), "System snap-layouts button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System display settings\"", StringComparison.OrdinalIgnoreCase), "System display settings button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: open sound settings.", StringComparison.OrdinalIgnoreCase), "System sound settings button should expose its spoken phrase.");
    Require(source.Contains("Voice phrases: open network settings, open wifi settings.", StringComparison.OrdinalIgnoreCase), "System network settings button should expose network and Wi-Fi phrases.");
    Require(source.Contains("AccessibleName = \"System accessibility settings\"", StringComparison.OrdinalIgnoreCase), "System accessibility settings button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System magnifier settings\"", StringComparison.OrdinalIgnoreCase), "System magnifier-settings button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: narrator settings, screen reader settings.", StringComparison.OrdinalIgnoreCase), "System narrator-settings button should expose spoken narrator aliases.");
    Require(source.Contains("Voice phrases: captions settings, live captions settings.", StringComparison.OrdinalIgnoreCase), "System captions-settings button should expose spoken captions aliases.");
    Require(source.Contains("Voice phrases: speech settings, voice access settings, voice typing settings, dictation settings.", StringComparison.OrdinalIgnoreCase), "System speech-settings button should expose spoken speech aliases.");
    Require(source.Contains("AccessibleName = \"System mouse settings\"", StringComparison.OrdinalIgnoreCase), "System mouse-settings button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System keyboard settings\"", StringComparison.OrdinalIgnoreCase), "System keyboard-settings button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System privacy settings\"", StringComparison.OrdinalIgnoreCase), "System privacy-settings button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: power and battery settings, open power and battery settings.", StringComparison.OrdinalIgnoreCase), "System power-settings button should expose spoken power aliases.");
    Require(source.Contains("AccessibleName = \"System installed apps settings\"", StringComparison.OrdinalIgnoreCase), "System installed-apps button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: default apps settings, open default apps settings.", StringComparison.OrdinalIgnoreCase), "System default-apps button should expose spoken default-app aliases.");
    Require(source.Contains("Voice phrases: date and time settings, open date and time settings.", StringComparison.OrdinalIgnoreCase), "System date-time button should expose spoken date/time aliases.");
    Require(source.Contains("Voice phrases: notifications settings, open notifications settings.", StringComparison.OrdinalIgnoreCase), "System notifications-settings button should expose spoken notifications aliases.");
    Require(source.Contains("AccessibleName = \"System Windows Update settings\"", StringComparison.OrdinalIgnoreCase), "System Windows Update button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: personalization settings, open personalization settings.", StringComparison.OrdinalIgnoreCase), "System personalization button should expose spoken personalization aliases.");
    Require(source.Contains("AccessibleName = \"System open magnifier\"", StringComparison.OrdinalIgnoreCase), "System open-magnifier button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: magnifier zoom out.", StringComparison.OrdinalIgnoreCase), "System magnifier-zoom-out button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System close magnifier\"", StringComparison.OrdinalIgnoreCase), "System close-magnifier button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System press enter\"", StringComparison.OrdinalIgnoreCase), "System Enter button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System new virtual desktop\"", StringComparison.OrdinalIgnoreCase), "System new-virtual-desktop button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System open Task Manager\"", StringComparison.OrdinalIgnoreCase), "System Task Manager button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: press windows key, windows key.", StringComparison.OrdinalIgnoreCase), "System Windows-key button should expose spoken Windows-key aliases.");
    Require(source.Contains("Voice phrases: press context menu, context menu key.", StringComparison.OrdinalIgnoreCase), "System context-menu button should expose spoken context-menu aliases.");
    Require(source.Contains("AccessibleName = \"System press Caps Lock\"", StringComparison.OrdinalIgnoreCase), "System Caps Lock button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System press Home\"", StringComparison.OrdinalIgnoreCase), "System Home button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System press End\"", StringComparison.OrdinalIgnoreCase), "System End button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: press page up, page up.", StringComparison.OrdinalIgnoreCase), "System Page Up button should expose spoken page-up aliases.");
    Require(source.Contains("Voice phrases: press page down, page down.", StringComparison.OrdinalIgnoreCase), "System Page Down button should expose spoken page-down aliases.");
    Require(source.Contains("Voice phrases: press up arrow, up arrow.", StringComparison.OrdinalIgnoreCase), "System up-arrow button should expose spoken arrow phrases.");
    Require(source.Contains("AccessibleName = \"System mouse click\"", StringComparison.OrdinalIgnoreCase), "System mouse click button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: release mouse, release mouse button.", StringComparison.OrdinalIgnoreCase), "System release-mouse button should expose spoken release phrases.");
    Require(source.Contains("Voice phrases: mouse scroll up, mouse scroll up a little.", StringComparison.OrdinalIgnoreCase), "System mouse scroll button should expose spoken scroll phrases.");
    Require(source.Contains("Voice phrases: mouse scroll right, scroll right.", StringComparison.OrdinalIgnoreCase), "System horizontal scroll button should expose spoken scroll phrases.");
    Require(source.Contains("Voice phrases: move mouse left, nudge left.", StringComparison.OrdinalIgnoreCase), "System mouse-left button should expose spoken nudge-left aliases.");
    Require(source.Contains("Voice phrases: move mouse right, nudge right.", StringComparison.OrdinalIgnoreCase), "System mouse-right button should expose spoken nudge-right aliases.");
    Require(source.Contains("AccessibleName = \"System move mouse right\"", StringComparison.OrdinalIgnoreCase), "System mouse nudge button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: move mouse up, nudge up.", StringComparison.OrdinalIgnoreCase), "System mouse nudge button should expose short spoken nudge phrases.");
    Require(source.Contains("AccessibleName = \"System drag mouse right\"", StringComparison.OrdinalIgnoreCase), "System mouse drag button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"System copy\"", StringComparison.OrdinalIgnoreCase), "System copy button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: bold, bold that.", StringComparison.OrdinalIgnoreCase), "System bold button should expose formatting phrases.");
    Require(source.Contains("Voice phrase: new document.", StringComparison.OrdinalIgnoreCase), "System new-document button should expose its spoken phrase.");
    Require(source.Contains("Voice phrase: print.", StringComparison.OrdinalIgnoreCase), "System print button should expose its spoken phrase.");
    Require(source.Contains("Voice phrase: reset zoom.", StringComparison.OrdinalIgnoreCase), "System zoom-reset button should expose its spoken phrase.");
    Require(source.Contains("Voice phrases: close this window, close active app.", StringComparison.OrdinalIgnoreCase), "System close-window button should expose close-window phrases.");
    Require(source.Contains("AccessibleName = \"System move previous character\"", StringComparison.OrdinalIgnoreCase), "System previous-character button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: select next character.", StringComparison.OrdinalIgnoreCase), "System next-character selection button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System move line start\"", StringComparison.OrdinalIgnoreCase), "System line-start button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: select to line end.", StringComparison.OrdinalIgnoreCase), "System select-to-line-end button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System move previous line\"", StringComparison.OrdinalIgnoreCase), "System previous-line button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: delete next line.", StringComparison.OrdinalIgnoreCase), "System next-line deletion button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System move previous word\"", StringComparison.OrdinalIgnoreCase), "System previous-word button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: select next word.", StringComparison.OrdinalIgnoreCase), "System next-word selection button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System move previous sentence\"", StringComparison.OrdinalIgnoreCase), "System previous-sentence button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: delete next sentence.", StringComparison.OrdinalIgnoreCase), "System next-sentence deletion button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"System move previous paragraph\"", StringComparison.OrdinalIgnoreCase), "System previous-paragraph button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: delete next paragraph.", StringComparison.OrdinalIgnoreCase), "System next-paragraph deletion button should expose its spoken phrase.");
    Require(source.Contains("File search results were shown in the visible Files tab status.", StringComparison.OrdinalIgnoreCase), "File search audit should summarize visible result status.");
    Require(source.Contains("File result selection was shown in the visible Files tab status.", StringComparison.OrdinalIgnoreCase), "File result selection audit should summarize visible status.");
    Require(source.Contains("File result open request was shown in the visible Files tab status.", StringComparison.OrdinalIgnoreCase), "File result open audit should summarize visible success status.");
    Require(source.Contains("File result reveal request was shown in the visible Files tab status.", StringComparison.OrdinalIgnoreCase), "File result reveal audit should summarize visible success status.");
    Require(source.Contains("AccessibleName = \"Files search\"", StringComparison.OrdinalIgnoreCase), "Files search button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Files search safety\"", StringComparison.OrdinalIgnoreCase), "Files safety line should expose a spoken-label accessible name.");
    Require(source.Contains("file search stays in common user folders and Callsign data", StringComparison.OrdinalIgnoreCase), "Files safety line should explain allowed search scope.");
    Require(source.Contains("Results are shown before action", StringComparison.OrdinalIgnoreCase), "Files safety line should explain visible review before action.");
    Require(source.Contains("executable or script-like files are blocked from direct open", StringComparison.OrdinalIgnoreCase), "Files safety line should explain blocked executable/script direct-open behavior.");
    Require(source.Contains("AccessibleName = \"Files result number\"", StringComparison.OrdinalIgnoreCase), "Files result-number picker should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: select result 1, select first result, choose result thirty second.", StringComparison.OrdinalIgnoreCase), "Files numbered selection button should expose its spoken phrases.");
    Require(source.Contains("AccessibleName = \"Files open selected result\"", StringComparison.OrdinalIgnoreCase), "Files open-result button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: reveal selected file.", StringComparison.OrdinalIgnoreCase), "Files reveal-result button should expose its spoken phrase.");
    Require(source.Contains("AccessibleName = \"Files open result number\"", StringComparison.OrdinalIgnoreCase), "Files numbered open button should expose a spoken-label accessible name.");
    Require(source.Contains("open containing folder for result 1, show containing folder for result 2", StringComparison.OrdinalIgnoreCase), "Files numbered reveal button should expose containing-folder aliases.");
    Require(source.Contains("commandFamily: \"visible_ui\"", StringComparison.OrdinalIgnoreCase), "Visible UI actions should record audit command family.");
    Require(source.Contains("Visible controls overlay state was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Visible-controls overlay audit should summarize visible status.");
    Require(source.Contains("Visible numbered control activation was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Visible numbered-control audit should summarize visible status.");
    Require(source.Contains("Mouse grid selection was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Mouse grid selection audit should summarize visible status.");
    Require(source.Contains("Mouse grid click was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Mouse grid click audit should summarize visible status.");
    Require(source.Contains("Mouse grid drag was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Mouse grid drag audit should summarize visible status.");
    Require(source.Contains("commandFamily: \"dictation\"", StringComparison.OrdinalIgnoreCase), "Dictation actions should record audit command family.");
    Require(source.Contains("Dictation start was shown in the visible Dictation review surface.", StringComparison.OrdinalIgnoreCase), "Dictation start audit should summarize visible review status.");
    Require(source.Contains("Dictation copy was shown in the visible Dictation review surface.", StringComparison.OrdinalIgnoreCase), "Dictation copy audit should summarize visible review status.");
    Require(source.Contains("Dictation paste block was shown in the visible Dictation review surface.", StringComparison.OrdinalIgnoreCase), "Dictation paste block audit should summarize visible review status.");
    Require(source.Contains("Dictation correction alternatives were shown in the visible correction HUD.", StringComparison.OrdinalIgnoreCase), "Dictation correction audit should summarize visible HUD status.");
    Require(source.Contains("Dictation formatting was shown in the visible Dictation review surface.", StringComparison.OrdinalIgnoreCase), "Dictation formatting audit should summarize visible review status.");
    Require(source.Contains("Dictation replacement was shown in the visible Dictation review surface.", StringComparison.OrdinalIgnoreCase), "Dictation replacement audit should summarize visible review status.");
    Require(source.Contains("AccessibleName = \"Dictation start\"", StringComparison.OrdinalIgnoreCase), "Dictation start button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation review safety\"", StringComparison.OrdinalIgnoreCase), "Dictation review safety line should expose an accessible name.");
    Require(source.Contains("dictated text stays in Callsign's review buffer until you copy or paste it", StringComparison.OrdinalIgnoreCase), "Dictation review safety line should explain the review buffer boundary.");
    Require(source.Contains("Paste into sensitive targets is blocked", StringComparison.OrdinalIgnoreCase), "Dictation review safety line should explain sensitive-target paste blocking.");
    Require(source.Contains("readback is local and stop reading leaves text unchanged", StringComparison.OrdinalIgnoreCase), "Dictation review safety line should explain local readback and stop behavior.");
    Require(source.Contains("AccessibleName = \"Dictation paste into active app\"", StringComparison.OrdinalIgnoreCase), "Dictation paste button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation select to line end\"", StringComparison.OrdinalIgnoreCase), "Dictation line selection button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation paragraph start\"", StringComparison.OrdinalIgnoreCase), "Dictation paragraph-start button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation delete to paragraph end\"", StringComparison.OrdinalIgnoreCase), "Dictation paragraph deletion button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation replace previous sentence\"", StringComparison.OrdinalIgnoreCase), "Dictation replacement button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation replace all\"", StringComparison.OrdinalIgnoreCase), "Dictation replace-all button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Dictation question mark\"", StringComparison.OrdinalIgnoreCase), "Dictation question-mark button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: period, full stop.", StringComparison.OrdinalIgnoreCase), "Dictation period button should expose period and full-stop phrases.");
    Require(source.Contains("Voice phrases: exclamation, exclamation mark, exclamation point.", StringComparison.OrdinalIgnoreCase), "Dictation exclamation button should expose natural exclamation phrases.");
    Require(source.Contains("Voice phrases: semicolon, semi colon.", StringComparison.OrdinalIgnoreCase), "Dictation semicolon button should expose compact and spaced semicolon phrases.");
    Require(source.Contains("AccessibleName = \"Dictation open parenthesis\"", StringComparison.OrdinalIgnoreCase), "Dictation open-parenthesis button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrases: open parenthesis, open parentheses.", StringComparison.OrdinalIgnoreCase), "Dictation open-parenthesis button should expose singular and plural phrases.");
    Require(source.Contains("Voice phrases: close parenthesis, close parentheses.", StringComparison.OrdinalIgnoreCase), "Dictation close-parenthesis button should expose singular and plural phrases.");
    Require(source.Contains("AccessibleName = \"Dictation at sign\"", StringComparison.OrdinalIgnoreCase), "Dictation at-sign button should expose a spoken-label accessible name.");
    Require(source.Contains("commandFamily: \"help_discovery\"", StringComparison.OrdinalIgnoreCase), "Help and command-discovery actions should record audit command family.");
    Require(source.Contains("Command palette was shown in the visible help surface.", StringComparison.OrdinalIgnoreCase), "Command palette audit should summarize visible help status.");
    Require(source.Contains("Command palette dismissal was shown in the visible help surface.", StringComparison.OrdinalIgnoreCase), "Command palette dismissal audit should summarize visible help status.");
    Require(source.Contains("Current status readback was shown in the visible status surface and used local speech synthesis.", StringComparison.OrdinalIgnoreCase), "Status readback audit should summarize visible local speech status.");
    Require(source.Contains("Status readback stop was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Status readback stop audit should summarize visible status.");
    Require(source.Contains("ClearRecentSpeechHistory", StringComparison.OrdinalIgnoreCase), "MainForm should expose a visible recent-speech clear action.");
    Require(source.Contains("RequestClearTranscriptHistory", StringComparison.OrdinalIgnoreCase), "MainForm should request user-runtime transcript history clearing.");
    Require(source.Contains("Recent speech history clear was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Recent-speech clear audit should summarize visible status.");
    Require(source.Contains("Getting Started walkthrough was shown in the visible help surface.", StringComparison.OrdinalIgnoreCase), "Startup walkthrough audit should summarize visible help status.");
    Require(source.Contains("Extension pack management was shown in the visible help surface.", StringComparison.OrdinalIgnoreCase), "Pack-management help audit should summarize visible help status.");
    Require(source.Contains("commandFamily: \"extension_pack\"", StringComparison.OrdinalIgnoreCase), "Extension pack UI actions should record audit command family.");
    Require(source.Contains("Update splash was shown in the visible update surface.", StringComparison.OrdinalIgnoreCase), "Update splash audit should summarize visible update status.");
    Require(source.Contains("Update splash dismissal was shown in the visible update surface.", StringComparison.OrdinalIgnoreCase), "Update splash dismissal audit should summarize visible update status.");
    Require(source.Contains("Extension pack import was shown in the visible Packs surface with review-before-enable status.", StringComparison.OrdinalIgnoreCase), "Extension pack import audit should summarize disabled-by-default review status.");
    Require(source.Contains("Extension pack enablement change was shown in the visible Packs surface.", StringComparison.OrdinalIgnoreCase), "Extension pack enable/disable audit should summarize visible status.");
    Require(source.Contains("Extension pack removal was shown in the visible Packs surface.", StringComparison.OrdinalIgnoreCase), "Extension pack removal audit should summarize visible status.");
    Require(source.Contains("AccessibleName = \"Selected pack summary\"", StringComparison.OrdinalIgnoreCase), "Packs selected-pack summary should expose a spoken-label accessible name.");
    Require(source.Contains("tier, load status, source, signature status, import status, and command gate", StringComparison.OrdinalIgnoreCase), "Packs selected-pack summary should expose explicit safety fields.");
    Require(source.Contains("AccessibleName = \"Pack enablement readiness\"", StringComparison.OrdinalIgnoreCase), "Packs enablement readiness should expose a spoken-label accessible name.");
    Require(source.Contains("is disabled for review, or is blocked by signature, entitlement, invalid metadata, or missing files", StringComparison.OrdinalIgnoreCase), "Packs enablement readiness should explain enablement blockers.");
    Require(source.Contains("FormatPackEnablementReadiness(packItem.Pack)", StringComparison.OrdinalIgnoreCase), "Packs selected-pack refresh should show enablement readiness.");
    Require(source.Contains("AccessibleName = \"Packs drop zone\"", StringComparison.OrdinalIgnoreCase), "Packs drop zone should expose a spoken-label accessible name.");
    Require(source.Contains("Visible drag-and-drop target for community command pack DLL files or folders", StringComparison.OrdinalIgnoreCase), "Packs drop zone should describe community DLL/folder import.");
    Require(source.Contains("Dropped packs are imported disabled by default", StringComparison.OrdinalIgnoreCase), "Packs drop zone should explain disabled-by-default review.");
    Require(source.Contains("_packsDropZoneLabel.AllowDrop = true", StringComparison.OrdinalIgnoreCase), "Packs drop zone should accept drag-and-drop files.");
    Require(source.Contains("PacksDrop(_packsDropZoneLabel", StringComparison.OrdinalIgnoreCase), "Packs drop zone should route dropped files through the pack import handler.");
    Require(source.Contains("Installed pack rows show display name, version, tier, load status, and high-level entitlement or signature gates.", StringComparison.OrdinalIgnoreCase), "Packs list should describe visible pack-row metadata.");
    Require(source.Contains("Shows the selected pack security summary plus command metadata including risk, privacy, approval, and visibility.", StringComparison.OrdinalIgnoreCase), "Packs commands list should describe command-level safety metadata.");
    Require(source.Contains("AccessibleName = \"Packs import pack\"", StringComparison.OrdinalIgnoreCase), "Packs import button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Packs import folder\"", StringComparison.OrdinalIgnoreCase), "Packs folder-import button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Packs enable selected pack\"", StringComparison.OrdinalIgnoreCase), "Packs enable button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Packs disable selected pack\"", StringComparison.OrdinalIgnoreCase), "Packs disable button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Packs remove selected pack\"", StringComparison.OrdinalIgnoreCase), "Packs remove button should expose a spoken-label accessible name.");
    Require(source.Contains("Voice phrase: open packs folder.", StringComparison.OrdinalIgnoreCase), "Packs open-folder button should expose its spoken phrase.");
    Require(source.Contains("commandFamily: \"voice_control\"", StringComparison.OrdinalIgnoreCase), "Voice control actions should record audit command family.");
    Require(source.Contains("Voice access mode change was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Voice mode-change audit should summarize visible status.");
    Require(source.Contains("Voice listening start was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Voice listening start audit should summarize visible status.");
    Require(source.Contains("Voice listening stop was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Voice listening stop audit should summarize visible status.");
    Require(source.Contains("Voice session cancel was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Voice cancel audit should summarize visible status.");
    Require(source.Contains("Voice session reset was shown in the visible status surface.", StringComparison.OrdinalIgnoreCase), "Voice reset audit should summarize visible status.");
    Require(source.Contains("var correlationId = $\"extension_", StringComparison.OrdinalIgnoreCase), "Extension command audit records should share a per-command correlation id.");
    Require(source.Contains("verificationMethod: \"registry_resolution\"", StringComparison.OrdinalIgnoreCase), "Extension command audit records should mark registry-resolution verification.");
    Require(source.Contains("No extension command matched the normalized spoken phrase.", StringComparison.OrdinalIgnoreCase), "Extension command audit records should summarize unmatched registry resolution.");
    Require(source.Contains("Extension command was blocked by Callsign policy before execution.", StringComparison.OrdinalIgnoreCase), "Extension command audit records should summarize policy blocking.");
    Require(source.Contains("verificationMethod: \"user_approval\"", StringComparison.OrdinalIgnoreCase), "Extension command audit records should mark user approval verification when approval is denied.");
    Require(source.Contains("verificationMethod: \"pack_execution\"", StringComparison.OrdinalIgnoreCase), "Extension command audit records should mark pack-execution verification.");
    Require(source.Contains("FormatExtensionVerificationSummary", StringComparison.OrdinalIgnoreCase), "Extension command audit records should summarize pack execution verification strategy.");
    Require(source.Contains("ExecuteSystemAction(intent.Target", StringComparison.OrdinalIgnoreCase), "System-control intents should execute through the visible system handler.");
    Require(source.Contains("ExecuteSystemShellSurfaceAction", StringComparison.OrdinalIgnoreCase), "System shell-surface buttons should execute through the visible policy-aware shell handler.");
    Require(source.Contains("SearchFiles();", StringComparison.OrdinalIgnoreCase), "File-search intents should execute through the visible file search handler.");
    Require(source.Contains("StartDictation();", StringComparison.OrdinalIgnoreCase), "Dictation intents should execute through the visible dictation review handler.");
}

static void SystemControlDryRunCoversAppSwitchingAndWindowManagement()
{
    var service = new SystemControlService(dryRun: true);

    foreach (var (action, expectedMessage) in new[]
             {
                 ("system-next-window", "Next window requested."),
                 ("system-previous-window", "Previous window requested."),
                 ("system-open-task-view", "Task view requested."),
                 ("system-open-quick-settings", "Quick Settings requested."),
                 ("system-open-notification-center", "Notification Center requested."),
                 ("system-open-emoji-panel", "Emoji panel requested."),
                 ("system-open-clipboard-history", "Clipboard history requested."),
                 ("system-open-snipping-toolbar", "Snipping toolbar requested."),
                 ("system-open-project-display", "Project display requested."),
                 ("system-open-cast-display", "Cast display requested."),
                 ("system-new-virtual-desktop", "New virtual desktop requested."),
                 ("system-next-virtual-desktop", "Next virtual desktop requested."),
                 ("system-previous-virtual-desktop", "Previous virtual desktop requested."),
                 ("system-minimize-window", "Minimize window requested."),
                 ("system-maximize-window", "Maximize window requested."),
                 ("system-restore-window", "Restore window requested."),
                 ("system-snap-window-left", "Snap window left requested."),
                 ("system-snap-window-right", "Snap window right requested."),
                 ("system-snap-window-up", "Snap window up requested."),
                 ("system-snap-window-down", "Snap window down requested."),
                 ("system-show-snap-layouts", "Snap layouts requested."),
                 ("system-open-task-manager", "Task Manager requested."),
                 ("system-show-desktop", "Show desktop requested."),
                 ("system-close-window", "Close window requested.")
             })
    {
        Require(service.TryExecute(action, out var message), $"Dry-run system control should execute: {action}");
        Require(string.Equals(message, expectedMessage, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedMessage}' for '{action}', got '{message}'.");
    }
}

static void SystemControlDryRunCoversMouseAndScrolling()
{
    var service = new SystemControlService(dryRun: true);

    foreach (var (action, expectedMessage) in new[]
             {
                 ("system-mouse-click", "Mouse click requested."),
                 ("system-mouse-double-click", "Mouse double-click requested."),
                 ("system-mouse-triple-click", "Mouse triple-click requested."),
                 ("system-mouse-right-click", "Mouse right-click requested."),
                 ("system-mouse-button-down", "Mouse button down requested."),
                 ("system-mouse-button-up", "Mouse button up requested."),
                 ("system-mouse-scroll-up", "Mouse scroll up requested."),
                 ("system-mouse-scroll-down", "Mouse scroll down requested."),
                 ("system-mouse-scroll-left", "Mouse scroll left requested."),
                 ("system-mouse-scroll-right", "Mouse scroll right requested."),
                 ("system-mouse-start-moving:up", "Mouse move up requested."),
                 ("system-mouse-start-moving:top-left", "Mouse move top left requested."),
                 ("system-mouse-stop-moving", "Mouse stop moving requested."),
                 ("system-mouse-move-faster", "Mouse move faster requested."),
                 ("system-mouse-move-slower", "Mouse move slower requested."),
                 ("system-mouse-move-fixed:left:5", "Mouse move left 5 requested."),
                 ("system-mouse-move-up", "Mouse move up requested."),
                 ("system-mouse-move-down", "Mouse move down requested."),
                 ("system-mouse-move-left", "Mouse move left requested."),
                 ("system-mouse-move-right", "Mouse move right requested."),
                 ("system-mouse-drag-direction:up", "Mouse drag up requested."),
                 ("system-mouse-drag-direction:down", "Mouse drag down requested."),
                 ("system-mouse-drag-direction:left", "Mouse drag left requested."),
                 ("system-mouse-drag-direction:right", "Mouse drag right requested."),
                 ("system-mouse-drag-direction:bottom-right", "Mouse drag bottom right requested.")
             })
    {
        Require(service.TryExecute(action, out var message), $"Dry-run mouse control should execute: {action}");
        Require(string.Equals(message, expectedMessage, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedMessage}' for '{action}', got '{message}'.");
    }
}

static void SystemControlDryRunCoversKeyboardCommands()
{
    var service = new SystemControlService(dryRun: true);

    foreach (var (action, expectedMessage) in new[]
             {
                 ("system-press-enter", "Enter requested."),
                 ("system-press-tab", "Tab requested."),
                 ("system-repeat:system-press-tab:5", "Tab 5 times requested."),
                 ("system-repeat:system-press-down:3", "Down arrow 3 times requested."),
                 ("system-press-escape", "Escape requested."),
                 ("system-press-backspace", "Backspace requested."),
                 ("system-press-space", "Space requested."),
                 ("system-press-delete", "Delete requested."),
                 ("system-press-insert", "Insert requested."),
                 ("system-press-windows", "Windows key requested."),
                 ("system-press-context-menu", "Context menu key requested."),
                 ("system-press-caps-lock", "Caps Lock requested."),
                 ("system-press-up", "Up arrow requested."),
                 ("system-press-down", "Down arrow requested."),
                 ("system-press-left", "Left arrow requested."),
                 ("system-press-right", "Right arrow requested."),
                 ("system-press-home", "Home requested."),
                 ("system-press-end", "End requested."),
                 ("system-page-up", "Page up requested."),
                 ("system-page-down", "Page down requested."),
                 ("system-press-f5", "F5 requested."),
                 ("system-press-f12", "F12 requested."),
                 ("system-press-digit:0", "Digit 0 requested."),
                 ("system-press-digit:5", "Digit 5 requested."),
                 ("system-press-letter:a", "Letter A requested."),
                 ("system-press-letter:z", "Letter Z requested."),
                 ("system-press-symbol:comma", "Comma requested."),
                 ("system-press-symbol:question", "Question mark requested."),
                 ("system-press-symbol:at", "At sign requested."),
                 ("system-press-chord:shift-tab", "Shift Tab requested."),
                 ("system-press-chord:shift-a", "Shift A requested."),
                 ("system-press-chord:shift-z", "Shift Z requested."),
                 ("system-press-chord:shift-1", "Shift 1 requested."),
                 ("system-press-chord:shift-9", "Shift 9 requested."),
                 ("system-press-chord:control-tab", "Control Tab requested."),
                 ("system-press-chord:control-shift-tab", "Control Shift Tab requested."),
                 ("system-press-chord:control-a", "Control A requested."),
                 ("system-press-chord:control-b", "Control B requested."),
                 ("system-press-chord:control-c", "Control C requested."),
                 ("system-press-chord:control-f", "Control F requested."),
                 ("system-press-chord:control-i", "Control I requested."),
                 ("system-press-chord:control-l", "Control L requested."),
                 ("system-press-chord:control-n", "Control N requested."),
                 ("system-press-chord:control-o", "Control O requested."),
                 ("system-press-chord:control-p", "Control P requested."),
                 ("system-press-chord:control-r", "Control R requested."),
                 ("system-press-chord:control-s", "Control S requested."),
                 ("system-press-chord:control-u", "Control U requested."),
                 ("system-press-chord:control-v", "Control V requested."),
                 ("system-press-chord:control-w", "Control W requested."),
                 ("system-press-chord:control-x", "Control X requested."),
                 ("system-press-chord:control-y", "Control Y requested."),
                 ("system-press-chord:control-z", "Control Z requested."),
                 ("system-press-chord:control-1", "Control 1 requested."),
                 ("system-press-chord:control-9", "Control 9 requested."),
                 ("system-press-chord:control-plus", "Control Plus requested."),
                 ("system-press-chord:control-minus", "Control Minus requested."),
                 ("system-press-chord:control-zero", "Control Zero requested."),
                 ("system-press-chord:alt-left", "Alt Left requested."),
                 ("system-press-chord:alt-right", "Alt Right requested."),
                 ("system-press-chord:alt-up", "Alt Up requested."),
                 ("system-press-chord:alt-down", "Alt Down requested."),
                 ("system-press-chord:alt-shift-tab", "Alt Shift Tab requested."),
                 ("system-press-chord:alt-f", "Alt F requested."),
                 ("system-press-chord:alt-h", "Alt H requested."),
                 ("system-press-chord:alt-1", "Alt 1 requested."),
                 ("system-press-chord:alt-9", "Alt 9 requested."),
                 ("system-press-chord:control-home", "Control Home requested."),
                 ("system-press-chord:control-end", "Control End requested."),
                 ("system-press-chord:control-shift-home", "Control Shift Home requested."),
                 ("system-press-chord:control-shift-end", "Control Shift End requested."),
                 ("system-press-chord:control-shift-t", "Control Shift T requested."),
                 ("system-press-chord:control-shift-n", "Control Shift N requested."),
                 ("system-press-chord:control-shift-1", "Control Shift 1 requested."),
                 ("system-press-chord:control-shift-9", "Control Shift 9 requested."),
                 ("system-hold-modifier:shift", "Shift held."),
                 ("system-hold-modifier:control", "Control held."),
                 ("system-hold-modifier:alt", "Alt held."),
                 ("system-release-modifier:shift", "Shift released."),
                 ("system-release-modifier:control", "Control released."),
                 ("system-release-modifier:alt", "Alt released."),
                 ("system-release-modifiers", "All held modifier keys released."),
                 ("system-move-previous-character", "Move previous character requested."),
                 ("system-move-next-character", "Move next character requested."),
                 ("system-select-previous-character", "Select previous character requested."),
                 ("system-select-next-character", "Select next character requested."),
                 ("system-delete-previous-character", "Delete previous character requested."),
                 ("system-delete-next-character", "Delete next character requested."),
                 ("system-move-line-start", "Move to line start requested."),
                 ("system-move-line-end", "Move to line end requested."),
                 ("system-move-previous-line", "Move previous line requested."),
                 ("system-move-next-line", "Move next line requested."),
                 ("system-select-to-line-start", "Select to line start requested."),
                 ("system-select-to-line-end", "Select to line end requested."),
                 ("system-select-previous-line", "Select previous line requested."),
                 ("system-select-next-line", "Select next line requested."),
                 ("system-delete-to-line-start", "Delete to line start requested."),
                 ("system-delete-to-line-end", "Delete to line end requested."),
                 ("system-delete-previous-line", "Delete previous line requested."),
                 ("system-delete-next-line", "Delete next line requested."),
                 ("system-move-paragraph-start", "Move to paragraph start requested."),
                 ("system-move-paragraph-end", "Move to paragraph end requested."),
                 ("system-select-to-paragraph-start", "Select to paragraph start requested."),
                 ("system-select-to-paragraph-end", "Select to paragraph end requested."),
                 ("system-delete-to-paragraph-start", "Delete to paragraph start requested."),
                 ("system-delete-to-paragraph-end", "Delete to paragraph end requested."),
                 ("system-copy", "Copy requested."),
                 ("system-paste", "Paste requested."),
                 ("system-cut", "Cut requested."),
                 ("system-select-all", "Select all requested."),
                 ("system-save", "Save requested."),
                 ("system-undo", "Undo requested."),
                 ("system-redo", "Redo requested."),
                 ("system-bold", "Bold requested."),
                 ("system-italic", "Italic requested."),
                 ("system-underline", "Underline requested."),
                 ("system-new-window", "New window requested."),
                 ("system-new-document", "New document requested."),
                 ("system-open-file", "Open file dialog requested."),
                 ("system-print", "Print dialog requested."),
                 ("system-find", "Find requested."),
                 ("system-zoom-in", "Zoom in requested."),
                 ("system-zoom-out", "Zoom out requested."),
                 ("system-zoom-reset", "Zoom reset requested.")
             })
    {
        Require(service.TryExecute(action, out var message), $"Dry-run keyboard control should execute: {action}");
        Require(string.Equals(message, expectedMessage, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedMessage}' for '{action}', got '{message}'.");
    }

    Require(!service.TryExecute("system-press-chord:control-alt-delete", out _), "Control Alt Delete should not execute as a safe keyboard parity chord.");
    Require(!service.TryExecute("system-hold-modifier:windows", out _), "Windows key should not execute as a held modifier.");
}

static void SystemControlDryRunCoversSafeSettings()
{
    var service = new SystemControlService(dryRun: true);

    foreach (var (action, expectedMessage) in new[]
             {
                 ("system-open-settings", "Windows Settings requested."),
                 ("system-open-display-settings", "Display settings requested."),
                 ("system-open-sound-settings", "Sound settings requested."),
                 ("system-open-bluetooth-settings", "Bluetooth settings requested."),
                 ("system-open-wifi-settings", "Wi-Fi settings requested."),
                 ("system-open-network-settings", "Network settings requested."),
                 ("system-open-accessibility-settings", "Accessibility settings requested."),
                 ("system-open-magnifier-settings", "Magnifier settings requested."),
                 ("system-open-narrator-settings", "Narrator settings requested."),
                 ("system-open-captions-settings", "Captions settings requested."),
                 ("system-open-speech-settings", "Speech settings requested."),
                 ("system-open-magnifier", "Magnifier requested."),
                 ("system-magnifier-zoom-out", "Magnifier zoom out requested."),
                 ("system-close-magnifier", "Close magnifier requested."),
                 ("system-open-mouse-settings", "Mouse settings requested."),
                 ("system-open-keyboard-settings", "Keyboard settings requested."),
                 ("system-open-privacy-settings", "Privacy settings requested."),
                 ("system-open-power-settings", "Power settings requested."),
                 ("system-open-apps-settings", "Apps settings requested."),
                 ("system-open-default-apps-settings", "Default apps settings requested."),
                 ("system-open-date-time-settings", "Date and time settings requested."),
                 ("system-open-notifications-settings", "Notifications settings requested."),
                 ("system-open-windows-update-settings", "Windows Update settings requested."),
                 ("system-open-personalization-settings", "Personalization settings requested.")
             })
    {
        Require(service.TryExecute(action, out var message), $"Dry-run safe settings should execute: {action}");
        Require(string.Equals(message, expectedMessage, StringComparison.OrdinalIgnoreCase), $"Expected '{expectedMessage}' for '{action}', got '{message}'.");
    }
}

static void ScriptedVoiceIntentsCoverAlphaActions()
{
    var launch = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open note pad", "Callsign", "echo one");
    Require(launch.ContainsCallsign, "Launch transcript should contain callsign.");
    Require(launch.Kind == AlphaVoiceIntentKind.StartMenuLaunch, $"Expected StartMenuLaunch, got {launch.Kind}.");
    Require(launch.Target == "Notepad", $"Expected Notepad alias target, got '{launch.Target}'.");

    var launched = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one launch app note pad", "Callsign", "echo one");
    Require(launched.ContainsCallsign, "Launch app transcript should contain callsign.");
    Require(launched.Kind == AlphaVoiceIntentKind.StartMenuLaunch, $"Expected StartMenuLaunch for launch app, got {launched.Kind}.");
    Require(launched.Target == "Notepad", $"Expected Notepad target for launch app, got '{launched.Target}'.");

    Require(AlphaVoiceTranscriptParser.InferAppName("launch app note pad") == "note pad", "Launch app prefix should strip cleanly before Start menu resolution.");
    Require(AlphaVoiceTranscriptParser.InferAppName("launch application calculator") == "calculator", "Launch application prefix should parse the app name.");
    Require(AlphaVoiceTranscriptParser.InferAppName("open app called settings") == "settings", "Open app called prefix should parse the app name.");

    var dictation = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start dictation", "Callsign", "echo one");
    Require(dictation.ContainsCallsign, "Dictation transcript should contain callsign.");
    Require(dictation.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation, got {dictation.Kind}.");
    var resumeDictation = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one resume dictation", "Callsign", "echo one");
    Require(resumeDictation.ContainsCallsign, "Resume dictation transcript should contain callsign.");
    Require(resumeDictation.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation, got {resumeDictation.Kind}.");
    var startTyping = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start typing", "Callsign", "echo one");
    Require(startTyping.ContainsCallsign, "Start typing transcript should contain callsign.");
    Require(startTyping.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation for start typing, got {startTyping.Kind}.");
    var typeText = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one type hello world", "Callsign", "echo one");
    Require(typeText.ContainsCallsign, "Direct type-text transcript should contain callsign.");
    Require(typeText.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation for direct type text, got {typeText.Kind}.");
    Require(typeText.Target == AlphaCommandRouter.DictationInsertTextActionPrefix + "hello world", $"Expected direct type text target, got '{typeText.Target}'.");
    var insertText = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one insert text hello comma world", "Callsign", "echo one");
    Require(insertText.Kind == AlphaVoiceIntentKind.Dictation, $"Expected Dictation for direct insert text, got {insertText.Kind}.");
    Require(insertText.Target == AlphaCommandRouter.DictationInsertTextActionPrefix + "hello, world", $"Expected punctuation-normalized insert text target, got '{insertText.Target}'.");
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
    var clickOnTrainVoiceIdentity = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click on the train voice identity button", "Callsign", "echo one");
    Require(clickOnTrainVoiceIdentity.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickOnTrainVoiceIdentity.Kind}.");
    Require(clickOnTrainVoiceIdentity.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickOnTrainVoiceIdentity.Target}'.");
    var chooseTrainVoiceIdentity = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one choose the train voice identity button", "Callsign", "echo one");
    Require(chooseTrainVoiceIdentity.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {chooseTrainVoiceIdentity.Kind}.");
    Require(chooseTrainVoiceIdentity.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{chooseTrainVoiceIdentity.Target}'.");
    var clickTrainVoiceIdentityLink = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the train voice identity link", "Callsign", "echo one");
    Require(clickTrainVoiceIdentityLink.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickTrainVoiceIdentityLink.Kind}.");
    Require(clickTrainVoiceIdentityLink.Target == "ui-activate-label:train voice identity", $"Expected ui-activate-label:train voice identity target, got '{clickTrainVoiceIdentityLink.Target}'.");
    var clickVoiceModeRadioButton = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the voice mode radio button", "Callsign", "echo one");
    Require(clickVoiceModeRadioButton.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickVoiceModeRadioButton.Kind}.");
    Require(clickVoiceModeRadioButton.Target == "ui-activate-label:voice mode", $"Expected ui-activate-label:voice mode target, got '{clickVoiceModeRadioButton.Target}'.");
    var clickSettingsMenuItem = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the settings menu item", "Callsign", "echo one");
    Require(clickSettingsMenuItem.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickSettingsMenuItem.Kind}.");
    Require(clickSettingsMenuItem.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{clickSettingsMenuItem.Target}'.");
    var chooseSettingsOption = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one choose the settings option", "Callsign", "echo one");
    Require(chooseSettingsOption.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {chooseSettingsOption.Kind}.");
    Require(chooseSettingsOption.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{chooseSettingsOption.Target}'.");
    var clickUsernameTextBox = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the username text box", "Callsign", "echo one");
    Require(clickUsernameTextBox.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickUsernameTextBox.Kind}.");
    Require(clickUsernameTextBox.Target == "ui-activate-label:username", $"Expected ui-activate-label:username target, got '{clickUsernameTextBox.Target}'.");
    var clickPasswordEditBox = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the password edit box", "Callsign", "echo one");
    Require(clickPasswordEditBox.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickPasswordEditBox.Kind}.");
    Require(clickPasswordEditBox.Target == "ui-activate-label:password", $"Expected ui-activate-label:password target, got '{clickPasswordEditBox.Target}'.");
    var clickProjectListItem = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the project list item", "Callsign", "echo one");
    Require(clickProjectListItem.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickProjectListItem.Kind}.");
    Require(clickProjectListItem.Target == "ui-activate-label:project", $"Expected ui-activate-label:project target, got '{clickProjectListItem.Target}'.");
    var clickNavigationTreeItem = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the navigation tree item", "Callsign", "echo one");
    Require(clickNavigationTreeItem.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickNavigationTreeItem.Kind}.");
    Require(clickNavigationTreeItem.Target == "ui-activate-label:navigation", $"Expected ui-activate-label:navigation target, got '{clickNavigationTreeItem.Target}'.");
    var clickAccountRow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the account row", "Callsign", "echo one");
    Require(clickAccountRow.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickAccountRow.Kind}.");
    Require(clickAccountRow.Target == "ui-activate-label:account", $"Expected ui-activate-label:account target, got '{clickAccountRow.Target}'.");
    var clickSettingsPane = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the settings pane", "Callsign", "echo one");
    Require(clickSettingsPane.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickSettingsPane.Kind}.");
    Require(clickSettingsPane.Target == "ui-activate-label:settings", $"Expected ui-activate-label:settings target, got '{clickSettingsPane.Target}'.");
    var clickStatusCell = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the status cell", "Callsign", "echo one");
    Require(clickStatusCell.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickStatusCell.Kind}.");
    Require(clickStatusCell.Target == "ui-activate-label:status", $"Expected ui-activate-label:status target, got '{clickStatusCell.Target}'.");
    var clickDocumentHeading = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the document heading", "Callsign", "echo one");
    Require(clickDocumentHeading.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickDocumentHeading.Kind}.");
    Require(clickDocumentHeading.Target == "ui-activate-label:document", $"Expected ui-activate-label:document target, got '{clickDocumentHeading.Target}'.");
    var clickAccountGroup = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the account group", "Callsign", "echo one");
    Require(clickAccountGroup.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickAccountGroup.Kind}.");
    Require(clickAccountGroup.Target == "ui-activate-label:account", $"Expected ui-activate-label:account target, got '{clickAccountGroup.Target}'.");
    var clickInboxListBox = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click the inbox list box", "Callsign", "echo one");
    Require(clickInboxListBox.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickInboxListBox.Kind}.");
    Require(clickInboxListBox.Target == "ui-activate-label:inbox", $"Expected ui-activate-label:inbox target, got '{clickInboxListBox.Target}'.");
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
    var chooseTwentiethResult = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one choose twentieth result", "Callsign", "echo one");
    Require(chooseTwentiethResult.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {chooseTwentiethResult.Kind}.");
    Require(chooseTwentiethResult.Target == "ui-select-file-result:20", $"Expected ui-select-file-result:20 target, got '{chooseTwentiethResult.Target}'.");
    var clickSystemVolumeUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click system volume up", "Callsign", "echo one");
    Require(clickSystemVolumeUp.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickSystemVolumeUp.Kind}.");
    Require(clickSystemVolumeUp.Target == "ui-activate-label:system volume up", $"Expected ui-activate-label:system volume up target, got '{clickSystemVolumeUp.Target}'.");
    var showNumbers = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show numbers", "Callsign", "echo one");
    Require(showNumbers.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showNumbers.Kind}.");
    Require(showNumbers.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNumbers.Target}'.");
    var showNumbersOnTaskbar = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show numbers on taskbar", "Callsign", "echo one");
    Require(showNumbersOnTaskbar.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showNumbersOnTaskbar.Kind}.");
    Require(showNumbersOnTaskbar.Target == "ui-show-visible-controls-taskbar", $"Expected ui-show-visible-controls-taskbar target, got '{showNumbersOnTaskbar.Target}'.");
    var showNumbersOnNotepad = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show numbers on notepad", "Callsign", "echo one");
    Require(showNumbersOnNotepad.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showNumbersOnNotepad.Kind}.");
    Require(showNumbersOnNotepad.Target == "ui-show-visible-controls-window:notepad", $"Expected ui-show-visible-controls-window:notepad target, got '{showNumbersOnNotepad.Target}'.");
    var showControlNumbers = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show control numbers", "Callsign", "echo one");
    Require(showControlNumbers.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showControlNumbers.Kind}.");
    Require(showControlNumbers.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showControlNumbers.Target}'.");
    var showAllControls = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show all controls", "Callsign", "echo one");
    Require(showAllControls.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showAllControls.Kind}.");
    Require(showAllControls.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showAllControls.Target}'.");
    var showNames = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show names", "Callsign", "echo one");
    Require(showNames.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showNames.Kind}.");
    Require(showNames.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showNames.Target}'.");
    var showLabels = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show labels", "Callsign", "echo one");
    Require(showLabels.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showLabels.Kind}.");
    Require(showLabels.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showLabels.Target}'.");
    var showAllLabels = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show all labels", "Callsign", "echo one");
    Require(showAllLabels.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showAllLabels.Kind}.");
    Require(showAllLabels.Target == "ui-show-visible-controls", $"Expected ui-show-visible-controls target, got '{showAllLabels.Target}'.");
    var hideVisibleControls = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide visible controls", "Callsign", "echo one");
    Require(hideVisibleControls.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideVisibleControls.Kind}.");
    Require(hideVisibleControls.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideVisibleControls.Target}'.");
    var hideControlNumbers = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide control numbers", "Callsign", "echo one");
    Require(hideControlNumbers.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideControlNumbers.Kind}.");
    Require(hideControlNumbers.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideControlNumbers.Target}'.");
    var hideAllControls = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide all controls", "Callsign", "echo one");
    Require(hideAllControls.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideAllControls.Kind}.");
    Require(hideAllControls.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideAllControls.Target}'.");
    var hideNames = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide names", "Callsign", "echo one");
    Require(hideNames.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideNames.Kind}.");
    Require(hideNames.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideNames.Target}'.");
    var hideLabels = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide labels", "Callsign", "echo one");
    Require(hideLabels.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideLabels.Kind}.");
    Require(hideLabels.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideLabels.Target}'.");
    var hideAllLabels = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one hide all labels", "Callsign", "echo one");
    Require(hideAllLabels.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {hideAllLabels.Kind}.");
    Require(hideAllLabels.Target == "ui-hide-visible-controls", $"Expected ui-hide-visible-controls target, got '{hideAllLabels.Target}'.");
    var showGrid = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show grid", "Callsign", "echo one");
    Require(showGrid.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showGrid.Kind}.");
    Require(showGrid.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showGrid.Target}'.");
    var showGridHere = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show grid here", "Callsign", "echo one");
    Require(showGridHere.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showGridHere.Kind}.");
    Require(showGridHere.Target == "ui-show-mouse-grid-here", $"Expected ui-show-mouse-grid-here target, got '{showGridHere.Target}'.");
    var showGridEverywhere = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show grid everywhere", "Callsign", "echo one");
    Require(showGridEverywhere.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showGridEverywhere.Kind}.");
    Require(showGridEverywhere.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showGridEverywhere.Target}'.");
    var showWindowGrid = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one show window grid", "Callsign", "echo one");
    Require(showWindowGrid.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {showWindowGrid.Kind}.");
    Require(showWindowGrid.Target == "ui-show-mouse-grid", $"Expected ui-show-mouse-grid target, got '{showWindowGrid.Target}'.");
    var mouseGridShortcutPath = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mouse grid 114", "Callsign", "echo one");
    Require(mouseGridShortcutPath.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {mouseGridShortcutPath.Kind}.");
    Require(mouseGridShortcutPath.Target == "ui-focus-mouse-grid-shortcut-path:114", $"Expected ui-focus-mouse-grid-shortcut-path:114 target, got '{mouseGridShortcutPath.Target}'.");
    var mouseGridSpacedShortcutPath = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mouse grid 1 1 4", "Callsign", "echo one");
    Require(mouseGridSpacedShortcutPath.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {mouseGridSpacedShortcutPath.Kind}.");
    Require(mouseGridSpacedShortcutPath.Target == "ui-focus-mouse-grid-shortcut-path:114", $"Expected spaced ui-focus-mouse-grid-shortcut-path:114 target, got '{mouseGridSpacedShortcutPath.Target}'.");
    var undoThatGrid = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one undo that", "Callsign", "echo one");
    Require(undoThatGrid.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {undoThatGrid.Kind}.");
    Require(undoThatGrid.Target == "ui-undo-mouse-grid", $"Expected ui-undo-mouse-grid target, got '{undoThatGrid.Target}'.");
    var markGrid = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mark", "Callsign", "echo one");
    Require(markGrid.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {markGrid.Kind}.");
    Require(markGrid.Target == "ui-mark-mouse-grid", $"Expected ui-mark-mouse-grid target, got '{markGrid.Target}'.");
    var markGridFour = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mark four", "Callsign", "echo one");
    Require(markGridFour.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {markGridFour.Kind}.");
    Require(markGridFour.Target == "ui-mark-mouse-grid-cell:4", $"Expected ui-mark-mouse-grid-cell:4 target, got '{markGridFour.Target}'.");
    var dragMarkedGrid = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one drag", "Callsign", "echo one");
    Require(dragMarkedGrid.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {dragMarkedGrid.Kind}.");
    Require(dragMarkedGrid.Target == "ui-drag-marked-mouse-grid", $"Expected ui-drag-marked-mouse-grid target, got '{dragMarkedGrid.Target}'.");
    var clickThree = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click 3", "Callsign", "echo one");
    Require(clickThree.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickThree.Kind}.");
    Require(clickThree.Target == "ui-activate-label:3", $"Expected ui-activate-label:3 target, got '{clickThree.Target}'.");
    var clickOne = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click one", "Callsign", "echo one");
    Require(clickOne.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickOne.Kind}.");
    Require(clickOne.Target == "ui-activate-label:1", $"Expected ui-activate-label:1 target, got '{clickOne.Target}'.");
    var clickNumberOne = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one click number one", "Callsign", "echo one");
    Require(clickNumberOne.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {clickNumberOne.Kind}.");
    Require(clickNumberOne.Target == "ui-activate-label:1", $"Expected ui-activate-label:1 target, got '{clickNumberOne.Target}'.");
    var chooseItemTwentyOne = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one choose item twenty one", "Callsign", "echo one");
    Require(chooseItemTwentyOne.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {chooseItemTwentyOne.Kind}.");
    Require(chooseItemTwentyOne.Target == "ui-activate-label:21", $"Expected ui-activate-label:21 target, got '{chooseItemTwentyOne.Target}'.");
    var doubleClickOne = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one double click one", "Callsign", "echo one");
    Require(doubleClickOne.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {doubleClickOne.Kind}.");
    Require(doubleClickOne.Target == "ui-double-click-label:1", $"Expected ui-double-click-label:1 target, got '{doubleClickOne.Target}'.");
    var doubleClickControlTwelve = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one double click control twelve", "Callsign", "echo one");
    Require(doubleClickControlTwelve.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {doubleClickControlTwelve.Kind}.");
    Require(doubleClickControlTwelve.Target == "ui-double-click-label:12", $"Expected ui-double-click-label:12 target, got '{doubleClickControlTwelve.Target}'.");
    var rightClickOne = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one right click one", "Callsign", "echo one");
    Require(rightClickOne.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {rightClickOne.Kind}.");
    Require(rightClickOne.Target == "ui-right-click-label:1", $"Expected ui-right-click-label:1 target, got '{rightClickOne.Target}'.");
    var rightClickOptionThird = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one right click option third", "Callsign", "echo one");
    Require(rightClickOptionThird.Kind == AlphaVoiceIntentKind.UiAction, $"Expected UiAction, got {rightClickOptionThird.Kind}.");
    Require(rightClickOptionThird.Target == "ui-right-click-label:3", $"Expected ui-right-click-label:3 target, got '{rightClickOptionThird.Target}'.");
    var volumeUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one volume up", "Callsign", "echo one");
    Require(volumeUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {volumeUp.Kind}.");
    Require(volumeUp.Target == "system-volume-up", $"Expected system-volume-up target, got '{volumeUp.Target}'.");
    var volumeDown = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one volume down", "Callsign", "echo one");
    Require(volumeDown.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {volumeDown.Kind}.");
    Require(volumeDown.Target == "system-volume-down", $"Expected system-volume-down target, got '{volumeDown.Target}'.");
    var muteVolume = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mute volume", "Callsign", "echo one");
    Require(muteVolume.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {muteVolume.Kind}.");
    Require(muteVolume.Target == "system-volume-mute", $"Expected system-volume-mute target, got '{muteVolume.Target}'.");
    var playMedia = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one play media", "Callsign", "echo one");
    Require(playMedia.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {playMedia.Kind}.");
    Require(playMedia.Target == "system-media-play-pause", $"Expected system-media-play-pause target, got '{playMedia.Target}'.");
    var taskManager = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one task manager", "Callsign", "echo one");
    Require(taskManager.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {taskManager.Kind}.");
    Require(taskManager.Target == "system-open-task-manager", $"Expected system-open-task-manager target, got '{taskManager.Target}'.");
    var openMagnifier = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one open magnifier", "Callsign", "echo one");
    Require(openMagnifier.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {openMagnifier.Kind}.");
    Require(openMagnifier.Target == "system-open-magnifier", $"Expected system-open-magnifier target, got '{openMagnifier.Target}'.");
    var closeMagnifier = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one close magnifier", "Callsign", "echo one");
    Require(closeMagnifier.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {closeMagnifier.Kind}.");
    Require(closeMagnifier.Target == "system-close-magnifier", $"Expected system-close-magnifier target, got '{closeMagnifier.Target}'.");
    var powerSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one power and battery settings", "Callsign", "echo one");
    Require(powerSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {powerSettings.Kind}.");
    Require(powerSettings.Target == "system-open-power-settings", $"Expected system-open-power-settings target, got '{powerSettings.Target}'.");
    var narratorSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one narrator settings", "Callsign", "echo one");
    Require(narratorSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {narratorSettings.Kind}.");
    Require(narratorSettings.Target == "system-open-narrator-settings", $"Expected system-open-narrator-settings target, got '{narratorSettings.Target}'.");
    var screenReaderSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one screen reader settings", "Callsign", "echo one");
    Require(screenReaderSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {screenReaderSettings.Kind}.");
    Require(screenReaderSettings.Target == "system-open-narrator-settings", $"Expected system-open-narrator-settings target, got '{screenReaderSettings.Target}'.");
    var captionsSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one captions settings", "Callsign", "echo one");
    Require(captionsSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {captionsSettings.Kind}.");
    Require(captionsSettings.Target == "system-open-captions-settings", $"Expected system-open-captions-settings target, got '{captionsSettings.Target}'.");
    var liveCaptionsSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one live captions settings", "Callsign", "echo one");
    Require(liveCaptionsSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {liveCaptionsSettings.Kind}.");
    Require(liveCaptionsSettings.Target == "system-open-captions-settings", $"Expected system-open-captions-settings target, got '{liveCaptionsSettings.Target}'.");
    var voiceAccessSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one voice access settings", "Callsign", "echo one");
    Require(voiceAccessSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {voiceAccessSettings.Kind}.");
    Require(voiceAccessSettings.Target == "system-open-speech-settings", $"Expected system-open-speech-settings target, got '{voiceAccessSettings.Target}'.");
    var appsSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one installed apps settings", "Callsign", "echo one");
    Require(appsSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {appsSettings.Kind}.");
    Require(appsSettings.Target == "system-open-apps-settings", $"Expected system-open-apps-settings target, got '{appsSettings.Target}'.");
    var defaultAppsSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one default apps settings", "Callsign", "echo one");
    Require(defaultAppsSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {defaultAppsSettings.Kind}.");
    Require(defaultAppsSettings.Target == "system-open-default-apps-settings", $"Expected system-open-default-apps-settings target, got '{defaultAppsSettings.Target}'.");
    var dateTimeSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one date and time settings", "Callsign", "echo one");
    Require(dateTimeSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {dateTimeSettings.Kind}.");
    Require(dateTimeSettings.Target == "system-open-date-time-settings", $"Expected system-open-date-time-settings target, got '{dateTimeSettings.Target}'.");
    var windowsUpdateSettings = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one windows update settings", "Callsign", "echo one");
    Require(windowsUpdateSettings.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {windowsUpdateSettings.Kind}.");
    Require(windowsUpdateSettings.Target == "system-open-windows-update-settings", $"Expected system-open-windows-update-settings target, got '{windowsUpdateSettings.Target}'.");
    var restoreWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one restore window", "Callsign", "echo one");
    Require(restoreWindow.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {restoreWindow.Kind}.");
    Require(restoreWindow.Target == "system-restore-window", $"Expected system-restore-window target, got '{restoreWindow.Target}'.");
    var closeActiveApp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one close active app", "Callsign", "echo one");
    Require(closeActiveApp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {closeActiveApp.Kind}.");
    Require(closeActiveApp.Target == "system-close-window", $"Expected system-close-window target, got '{closeActiveApp.Target}'.");
    var pressTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press tab", "Callsign", "echo one");
    Require(pressTab.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressTab.Kind}.");
    Require(pressTab.Target == "system-press-tab", $"Expected system-press-tab target, got '{pressTab.Target}'.");
    var pressDelete = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press delete", "Callsign", "echo one");
    Require(pressDelete.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressDelete.Kind}.");
    Require(pressDelete.Target == "system-press-delete", $"Expected system-press-delete target, got '{pressDelete.Target}'.");
    var pressF5 = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press f5", "Callsign", "echo one");
    Require(pressF5.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {pressF5.Kind}.");
    Require(pressF5.Target == "system-press-f5", $"Expected system-press-f5 target, got '{pressF5.Target}'.");
    var dismiss = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one dismiss", "Callsign", "echo one");
    Require(dismiss.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {dismiss.Kind}.");
    Require(dismiss.Target == "system-press-escape", $"Expected system-press-escape target, got '{dismiss.Target}'.");
    var repeatTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one press tab five times", "Callsign", "echo one");
    Require(repeatTab.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {repeatTab.Kind}.");
    Require(repeatTab.Target == "system-repeat:system-press-tab:5", $"Expected system-repeat:system-press-tab:5 target, got '{repeatTab.Target}'.");
    var home = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one home key", "Callsign", "echo one");
    Require(home.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {home.Kind}.");
    Require(home.Target == "system-press-home", $"Expected system-press-home target, got '{home.Target}'.");
    var mouseScrollUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one mouse scroll up", "Callsign", "echo one");
    Require(mouseScrollUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {mouseScrollUp.Kind}.");
    Require(mouseScrollUp.Target == "system-mouse-scroll-up", $"Expected system-mouse-scroll-up target, got '{mouseScrollUp.Target}'.");
    var moveMouseUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one move mouse up", "Callsign", "echo one");
    Require(moveMouseUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {moveMouseUp.Kind}.");
    Require(moveMouseUp.Target == "system-mouse-start-moving:up", $"Expected system-mouse-start-moving:up target, got '{moveMouseUp.Target}'.");
    var moveMouseTopLeft = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one move mouse top left", "Callsign", "echo one");
    Require(moveMouseTopLeft.Target == "system-mouse-start-moving:top-left", $"Expected system-mouse-start-moving:top-left target, got '{moveMouseTopLeft.Target}'.");
    var moveMouseLeftFive = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one move mouse left five", "Callsign", "echo one");
    Require(moveMouseLeftFive.Target == "system-mouse-move-fixed:left:5", $"Expected system-mouse-move-fixed:left:5 target, got '{moveMouseLeftFive.Target}'.");
    var nudgeUp = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one nudge up", "Callsign", "echo one");
    Require(nudgeUp.Kind == AlphaVoiceIntentKind.SystemControl, $"Expected SystemControl, got {nudgeUp.Kind}.");
    Require(nudgeUp.Target == "system-mouse-move-up", $"Expected system-mouse-move-up target, got '{nudgeUp.Target}'.");
    var moveFaster = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one move faster", "Callsign", "echo one");
    Require(moveFaster.Target == "system-mouse-move-faster", $"Expected system-mouse-move-faster target, got '{moveFaster.Target}'.");
    var stopMoving = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one stop moving", "Callsign", "echo one");
    Require(stopMoving.Target == "system-mouse-stop-moving", $"Expected system-mouse-stop-moving target, got '{stopMoving.Target}'.");
    var tripleClick = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one triple click", "Callsign", "echo one");
    Require(tripleClick.Target == "system-mouse-triple-click", $"Expected system-mouse-triple-click target, got '{tripleClick.Target}'.");
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
    Require(AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand("fix previous word with ready", out var fixWord) && fixWord is not null, "Fix previous word should be recognized.");
    Require(fixWord!.Scope == DictationReplacementScope.PreviousWord, "Fix previous word should map to previous word scope.");
    Require(fixWord.ReplacementText == "ready", $"Expected fix replacement text ready, got '{fixWord.ReplacementText}'.");
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

    var launchBrowser = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one launch browser example.com", "Callsign", "echo one");
    Require(launchBrowser.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {launchBrowser.Kind}.");
    Require(launchBrowser.Target == "example.com", $"Expected launch-browser target example.com, got '{launchBrowser.Target}'.");

    var browserOpenIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser open example.com", "Callsign", "echo one");
    Require(browserOpenIntent.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserOpenIntent.Kind}.");
    Require(browserOpenIntent.Target == "example.com", $"Expected browser-open target example.com, got '{browserOpenIntent.Target}'.");

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

    var browserNewWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser new window", "Callsign", "echo one");
    Require(browserNewWindow.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserNewWindow.Kind}.");
    Require(browserNewWindow.Target == "browser-new-window", $"Expected browser-new-window target, got '{browserNewWindow.Target}'.");

    var browserPrivateWindow = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser private window", "Callsign", "echo one");
    Require(browserPrivateWindow.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserPrivateWindow.Kind}.");
    Require(browserPrivateWindow.Target == "browser-private-window", $"Expected browser-private-window target, got '{browserPrivateWindow.Target}'.");

    var browserIncognito = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser incognito", "Callsign", "echo one");
    Require(browserIncognito.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserIncognito.Kind}.");
    Require(browserIncognito.Target == "browser-private-window", $"Expected browser-private-window target, got '{browserIncognito.Target}'.");

    var browserBookmarkPage = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser bookmark page", "Callsign", "echo one");
    Require(browserBookmarkPage.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserBookmarkPage.Kind}.");
    Require(browserBookmarkPage.Target == "browser-bookmark-page", $"Expected browser-bookmark-page target, got '{browserBookmarkPage.Target}'.");

    var browserFavorites = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one add to favorites", "Callsign", "echo one");
    Require(browserFavorites.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFavorites.Kind}.");
    Require(browserFavorites.Target == "browser-bookmark-page", $"Expected browser-bookmark-page target, got '{browserFavorites.Target}'.");

    var browserBookmarks = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser bookmarks", "Callsign", "echo one");
    Require(browserBookmarks.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserBookmarks.Kind}.");
    Require(browserBookmarks.Target == "browser-open-bookmarks", $"Expected browser-open-bookmarks target, got '{browserBookmarks.Target}'.");

    var browserSavePage = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one save this page", "Callsign", "echo one");
    Require(browserSavePage.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserSavePage.Kind}.");
    Require(browserSavePage.Target == "browser-save-page", $"Expected browser-save-page target, got '{browserSavePage.Target}'.");

    var browserPrintPage = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one print page", "Callsign", "echo one");
    Require(browserPrintPage.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserPrintPage.Kind}.");
    Require(browserPrintPage.Target == "browser-print-page", $"Expected browser-print-page target, got '{browserPrintPage.Target}'.");

    var browserNextTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser next tab", "Callsign", "echo one");
    Require(browserNextTab.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserNextTab.Kind}.");
    Require(browserNextTab.Target == "browser-next-tab", $"Expected browser-next-tab target, got '{browserNextTab.Target}'.");

    var browserPreviousTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser previous tab", "Callsign", "echo one");
    Require(browserPreviousTab.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserPreviousTab.Kind}.");
    Require(browserPreviousTab.Target == "browser-previous-tab", $"Expected browser-previous-tab target, got '{browserPreviousTab.Target}'.");

    var browserReopenClosedTab = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one reopen closed tab", "Callsign", "echo one");
    Require(browserReopenClosedTab.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserReopenClosedTab.Kind}.");
    Require(browserReopenClosedTab.Target == "browser-reopen-closed-tab", $"Expected browser-reopen-closed-tab target, got '{browserReopenClosedTab.Target}'.");

    var browserFocusAddressBar = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser focus address bar", "Callsign", "echo one");
    Require(browserFocusAddressBar.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFocusAddressBar.Kind}.");
    Require(browserFocusAddressBar.Target == "browser-focus-address-bar", $"Expected browser-focus-address-bar target, got '{browserFocusAddressBar.Target}'.");

    var browserHome = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser home", "Callsign", "echo one");
    Require(browserHome.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserHome.Kind}.");
    Require(browserHome.Target == "browser-home", $"Expected browser-home target, got '{browserHome.Target}'.");

    var browserFullscreen = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser full screen", "Callsign", "echo one");
    Require(browserFullscreen.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFullscreen.Kind}.");
    Require(browserFullscreen.Target == "browser-fullscreen", $"Expected browser-fullscreen target, got '{browserFullscreen.Target}'.");

    var browserDownloads = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser downloads", "Callsign", "echo one");
    Require(browserDownloads.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserDownloads.Kind}.");
    Require(browserDownloads.Target == "browser-open-downloads", $"Expected browser-open-downloads target, got '{browserDownloads.Target}'.");

    var browserHistory = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser history", "Callsign", "echo one");
    Require(browserHistory.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserHistory.Kind}.");
    Require(browserHistory.Target == "browser-open-history", $"Expected browser-open-history target, got '{browserHistory.Target}'.");

    var browserFindText = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one search this page for privacy policy", "Callsign", "echo one");
    Require(browserFindText.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFindText.Kind}.");
    Require(browserFindText.Target == "browser-find-text:privacy policy", $"Expected browser-find-text:privacy policy target, got '{browserFindText.Target}'.");

    var browserFindTextSuffix = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one find privacy policy on this page", "Callsign", "echo one");
    Require(browserFindTextSuffix.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFindTextSuffix.Kind}.");
    Require(browserFindTextSuffix.Target == "browser-find-text:privacy policy", $"Expected suffix browser-find-text:privacy policy target, got '{browserFindTextSuffix.Target}'.");

    var browserAddressText = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one type in address bar example dot com", "Callsign", "echo one");
    Require(browserAddressText.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserAddressText.Kind}.");
    Require(browserAddressText.Target == "browser-address-text:example.com", $"Expected browser-address-text:example.com target, got '{browserAddressText.Target}'.");

    var browserAddressTextSuffix = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one type example dot com in the address bar", "Callsign", "echo one");
    Require(browserAddressTextSuffix.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserAddressTextSuffix.Kind}.");
    Require(browserAddressTextSuffix.Target == "browser-address-text:example.com", $"Expected suffix browser-address-text:example.com target, got '{browserAddressTextSuffix.Target}'.");

    var browserOpen = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser open example.com", "Callsign", "echo one");
    Require(browserOpen.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserOpen.Kind}.");
    Require(browserOpen.Target == "example.com", $"Expected browser-open target example.com, got '{browserOpen.Target}'.");

    var browserFindAlias = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser find", "Callsign", "echo one");
    Require(browserFindAlias.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserFindAlias.Kind}.");
    Require(browserFindAlias.Target == "browser-find", $"Expected browser-find target, got '{browserFindAlias.Target}'.");

    var browserSearchInPage = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser search in page", "Callsign", "echo one");
    Require(browserSearchInPage.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserSearchInPage.Kind}.");
    Require(browserSearchInPage.Target == "browser-find", $"Expected browser-find target, got '{browserSearchInPage.Target}'.");

    var browserScrollLeft = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser scroll left", "Callsign", "echo one");
    Require(browserScrollLeft.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserScrollLeft.Kind}.");
    Require(browserScrollLeft.Target == "browser-scroll-left", $"Expected browser-scroll-left target, got '{browserScrollLeft.Target}'.");

    var startScrollingDown = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one start scrolling down", "Callsign", "echo one");
    Require(startScrollingDown.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {startScrollingDown.Kind}.");
    Require(startScrollingDown.Target == "browser-start-scroll-down", $"Expected browser-start-scroll-down target, got '{startScrollingDown.Target}'.");

    var stopScrolling = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one stop scrolling", "Callsign", "echo one");
    Require(stopScrolling.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {stopScrolling.Kind}.");
    Require(stopScrolling.Target == "browser-stop-scroll", $"Expected browser-stop-scroll target, got '{stopScrolling.Target}'.");

    var browserScrollRight = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one browser scroll right", "Callsign", "echo one");
    Require(browserScrollRight.Kind == AlphaVoiceIntentKind.Browser, $"Expected Browser, got {browserScrollRight.Kind}.");
    Require(browserScrollRight.Target == "browser-scroll-right", $"Expected browser-scroll-right target, got '{browserScrollRight.Target}'.");

    var fileSearchFiles = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one search my files for alpha notes", "Callsign", "echo one");
    Require(fileSearchFiles.Kind == AlphaVoiceIntentKind.FileSearch, $"Expected FileSearch, got {fileSearchFiles.Kind}.");
    Require(fileSearchFiles.Target == "alpha notes", $"Expected file-search target alpha notes, got '{fileSearchFiles.Target}'.");

    var fileSearchDocuments = AlphaVoiceIntentParser.ParseVerifiedTranscript("Callsign echo one search my documents for budget", "Callsign", "echo one");
    Require(fileSearchDocuments.Kind == AlphaVoiceIntentKind.FileSearch, $"Expected FileSearch, got {fileSearchDocuments.Kind}.");
    Require(fileSearchDocuments.Target == "budget", $"Expected file-search target budget, got '{fileSearchDocuments.Target}'.");

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

static void WakeTransitionSourceDistinguishesAudioFromScriptedControl()
{
    var session = new AlphaSessionStateMachine();
    Require(session.LastWakeTransitionSource == null, "Fresh sessions should not report a wake transition source.");

    session.DetectWakeWord(AlphaSessionStateMachine.AudioWakeDetectorSource);
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected audio wake to enter WaitingForIdentity, got {session.State}.");
    Require(session.LastWakeTransitionSource == AlphaSessionStateMachine.AudioWakeDetectorSource, $"Expected audio wake source, got '{session.LastWakeTransitionSource}'.");

    session.Reset();
    session.DetectWakeWord(AlphaSessionStateMachine.ScriptedTranscriptControlSource);
    Require(session.State == AlphaSessionState.WaitingForIdentity, $"Expected scripted wake control to enter WaitingForIdentity, got {session.State}.");
    Require(session.LastWakeTransitionSource == AlphaSessionStateMachine.ScriptedTranscriptControlSource, $"Expected scripted wake source, got '{session.LastWakeTransitionSource}'.");

    session.Cancel("User cancelled.");
    Require(session.LastWakeTransitionSource == null, "Cancelled sessions should clear wake transition source.");

    session.DetectWakeWord("  custom-wake-source  ");
    Require(session.LastWakeTransitionSource == "custom-wake-source", $"Expected trimmed custom wake source, got '{session.LastWakeTransitionSource}'.");
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
    Require(source.Contains("DetectWakeWord(AlphaSessionStateMachine.AudioWakeDetectorSource)", StringComparison.OrdinalIgnoreCase), "Service worker wake event should label audio detector wake transitions.");
    Require(source.Contains("DetectWakeWord(AlphaSessionStateMachine.ScriptedTranscriptControlSource)", StringComparison.OrdinalIgnoreCase), "Service worker scripted control path should label non-audio wake transitions separately.");
    Require(source.Contains("LastWakeTransitionSource: _session.LastWakeTransitionSource", StringComparison.OrdinalIgnoreCase), "Runtime snapshots should expose the session wake transition source.");
    Require(source.Contains("_session.TryVerifyIdentity(result, profile.Callsign", StringComparison.OrdinalIgnoreCase), "Service worker should advance identity only through the result-aware identity gate.");
    var identityHandlerStart = source.IndexOf("private void HandleIdentityTranscript", StringComparison.OrdinalIgnoreCase);
    var executeCommandStart = source.IndexOf("private void ExecuteVerifiedCommand", StringComparison.OrdinalIgnoreCase);
    Require(identityHandlerStart >= 0, "Service worker should have an identity transcript handler.");
    Require(executeCommandStart > identityHandlerStart, "Service command execution should be separate from identity handling.");
    var identityHandlerSource = source[identityHandlerStart..executeCommandStart];
    Require(!identityHandlerSource.Contains("ExecuteVerifiedCommand", StringComparison.OrdinalIgnoreCase), "Identity handler must not execute commands from the identity utterance.");
    Require(!identityHandlerSource.Contains("DetectWakeWord", StringComparison.OrdinalIgnoreCase), "Identity handler must not promote transcript text into a wake transition.");
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
    var detectCall = handlerSource.IndexOf("_session.DetectWakeWord(AlphaSessionStateMachine.AudioWakeDetectorSource);", StringComparison.OrdinalIgnoreCase);
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
    var constructorStart = uiSource.IndexOf("public MainForm(", StringComparison.OrdinalIgnoreCase);
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
    Require(source.Contains("[double]$Threshold = 0", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should default to app-style sensitivity thresholds instead of legacy 0.55.");
    Require(source.Contains("[string]$Sensitivity = \"More responsive\"", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should default to the More responsive sensitivity.");
    Require(source.Contains("Resolve-CallsignWakeThreshold", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should use the same threshold family as the app.");
    Require(source.Contains("Effective threshold", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should print the effective threshold used for detection.");
    Require(source.Contains("Margin", StringComparison.OrdinalIgnoreCase), "Packaged wake test helper should print score margin for reliability evidence.");
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
    Require(source.Contains("AccessibleName = \"Voice identity record sample\"", StringComparison.OrdinalIgnoreCase), "Voice training record button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice identity record wake sample\"", StringComparison.OrdinalIgnoreCase), "Voice training wake-sample button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice identity enroll\"", StringComparison.OrdinalIgnoreCase), "Voice training enroll button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice identity calibrate wakeword\"", StringComparison.OrdinalIgnoreCase), "Voice training wake-calibration button should expose a spoken-label accessible name.");
    Require(source.Contains("AccessibleName = \"Voice identity repair runtime\"", StringComparison.OrdinalIgnoreCase), "Voice training repair-runtime button should expose a spoken-label accessible name.");
    Require(source.Contains("TryScoreWakeWordSampleAsync", StringComparison.OrdinalIgnoreCase), "Voice training form should call the wake scoring helper.");
    Require(source.Contains("ApplyWakeCalibration", StringComparison.OrdinalIgnoreCase), "Voice training form should apply a profile-specific wake threshold.");
    Require(source.Contains("GetRecordedWakeSamplePaths", StringComparison.OrdinalIgnoreCase), "Voice training form should maintain dedicated wake samples.");
}

static void VoiceIdentityTrainingSurfaceExplainsNextStepsAndFailures()
{
    var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke.IdentityTraining", Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(root);
        var store = new ProfileStore(root);
        var profile = new UserProfile
        {
            Callsign = "alpha",
            Settings =
            {
                VoiceSamplesRequired = 3,
                VoiceSamplesRecorded = 0,
                VoiceEnrollmentStatus = "Not activated"
            }
        };
        store.Save(profile);

        using var emptyForm = new VoiceIdentityTrainingForm(store, profile, new VoiceCommandService());
        Require(emptyForm.NextStepText.Contains("record 3 more fresh sample", StringComparison.OrdinalIgnoreCase), $"Expected empty form to explain sample count, got '{emptyForm.NextStepText}'.");
        Require(emptyForm.FailureText.Contains("not enough samples yet", StringComparison.OrdinalIgnoreCase), $"Expected empty form to call out missing samples, got '{emptyForm.FailureText}'.");
        Require(ReferenceEquals(emptyForm.CancelButton, emptyForm.Controls.OfType<Control>().SelectMany(control => control.Controls.OfType<Button>()).FirstOrDefault(control => string.Equals(control.AccessibleName, "Voice identity close", StringComparison.OrdinalIgnoreCase))), "Expected Escape to dismiss the voice identity training dialog.");

        for (var index = 1; index <= 3; index++)
        {
            var path = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, index);
            WriteTone(path, 180 + (index * 10), 0.40);
        }

        profile.Settings.VoiceSamplesRecorded = 3;
        profile.Settings.VoiceEnrollmentStatus = "pyannote setup required";
        profile.Settings.VoiceEnrolledUtc = null;
        store.Save(profile);

        using var runtimeForm = new VoiceIdentityTrainingForm(store, profile, new VoiceCommandService());
        Require(runtimeForm.NextStepText.Contains("enroll voice identity", StringComparison.OrdinalIgnoreCase) || runtimeForm.NextStepText.Contains("voice identity is enrolled", StringComparison.OrdinalIgnoreCase),
            $"Expected enrollment-ready form to explain the next action, got '{runtimeForm.NextStepText}'.");
        Require(runtimeForm.FailureText.Contains("identity runtime", StringComparison.OrdinalIgnoreCase) || runtimeForm.FailureText.Contains("model cache", StringComparison.OrdinalIgnoreCase),
            $"Expected enrollment-ready form to identify the runtime or model blocker, got '{runtimeForm.FailureText}'.");
        Require(ReferenceEquals(runtimeForm.CancelButton, runtimeForm.Controls.OfType<Control>().SelectMany(control => control.Controls.OfType<Button>()).FirstOrDefault(control => string.Equals(control.AccessibleName, "Voice identity close", StringComparison.OrdinalIgnoreCase))), "Expected Escape to dismiss the enrollment-ready voice identity training dialog.");

        for (var index = 1; index <= 3; index++)
        {
            var path = VoiceBiometricVerificationService.GetEnrollmentSamplePath(store, profile, index);
            WriteTone(path, 260, 0.40);
        }

        var duplicateResult = new VoiceBiometricVerificationService().EnrollFreshSamples(store, profile, VoiceBiometricVerificationService.GetEnrollmentSamplePaths(store, profile).Take(3));
        Require(!duplicateResult.Accepted && duplicateResult.RejectReason == "pyannote_sample_set_not_distinct", $"Expected duplicate sample proof failure, got {duplicateResult.RejectReason}.");
        profile.Settings.VoiceSamplesRecorded = 3;
        profile.Settings.VoiceEnrollmentStatus = duplicateResult.Message;
        store.Save(profile);

        using var duplicateForm = new VoiceIdentityTrainingForm(store, profile, new VoiceCommandService());
        Require(duplicateForm.FailureText.Contains("duplicate voice samples", StringComparison.OrdinalIgnoreCase),
            $"Expected training form to identify duplicate sample proof failure, got '{duplicateForm.FailureText}'.");
        Require(ReferenceEquals(duplicateForm.CancelButton, duplicateForm.Controls.OfType<Control>().SelectMany(control => control.Controls.OfType<Button>()).FirstOrDefault(control => string.Equals(control.AccessibleName, "Voice identity close", StringComparison.OrdinalIgnoreCase))), "Expected Escape to dismiss the duplicate-sample voice identity training dialog.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
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

    var before = GetLaunchProcessIds(appName);
    var launcher = new StartMenuLauncher();
    var launchResult = launcher.LaunchWithResult(appName);
    if (!launchResult.Succeeded)
    {
        Console.WriteLine($"FAIL: {launchResult.Message}");
        Console.WriteLine($"INFO: LaunchPath={launchResult.LaunchPath}; StartMenuOpened={launchResult.StartMenuOpened}; ShellFallbackUsed={launchResult.ShellFallbackUsed}; Steps={string.Join(" > ", launchResult.Steps)}");
        return 1;
    }

    Console.WriteLine($"INFO: {launchResult.Message}");
    Console.WriteLine($"INFO: LaunchPath={launchResult.LaunchPath}; StartMenuOpened={launchResult.StartMenuOpened}; ShellFallbackUsed={launchResult.ShellFallbackUsed}; Steps={string.Join(" > ", launchResult.Steps)}");
    var launched = new List<Process>();
    for (var attempt = 0; attempt < 12 && launched.Count == 0; attempt++)
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
        launched = GetLaunchProcesses(appName, before);
    }

    if (launched.Count == 0)
    {
        var existing = GetLaunchProcesses(appName, before: null);
        if (before.Count > 0 && existing.Count > 0)
        {
            Console.WriteLine($"PASS: '{appName}' was already running and remained available after the Start menu launch path.");
            foreach (var process in existing)
                process.Dispose();
            return launchResult.IsVisibleStartMenuPath
                ? 0
                : FailVisibleStartMenuEvidence(launchResult);
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

    return launchResult.IsVisibleStartMenuPath
        ? 0
        : FailVisibleStartMenuEvidence(launchResult);
}

static int FailVisibleStartMenuEvidence(StartMenuLaunchResult launchResult)
{
    Console.WriteLine($"FAIL: Live launch succeeded through '{launchResult.LaunchPath}', but this does not prove the visible Start menu flow required by v1.0 row 4.3.");
    return 1;
}

static HashSet<int> GetLaunchProcessIds(string appName) =>
    GetLaunchProcesses(appName, before: null)
        .Select(process => process.Id)
        .ToHashSet();

static List<Process> GetLaunchProcesses(string appName, HashSet<int>? before)
{
    var names = GetLaunchProcessNames(appName);
    var titleTerms = GetLaunchTitleTerms(appName);
    var processes = new List<Process>();
    foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        processes.AddRange(Process.GetProcessesByName(name)
            .Where(process => before is null || !before.Contains(process.Id)));
    }

    processes.AddRange(Process.GetProcesses()
        .Where(process => before is null || !before.Contains(process.Id))
        .Where(process =>
        {
            try
            {
                return !string.IsNullOrWhiteSpace(process.MainWindowTitle)
                    && titleTerms.Any(term => process.MainWindowTitle.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }));

    return processes
        .GroupBy(process => process.Id)
        .Select(group => group.First())
        .ToList();
}

static IReadOnlyList<string> GetLaunchProcessNames(string appName)
{
    var normalized = StartMenuLauncher.ResolveAppName(appName);
    return normalized.ToLowerInvariant() switch
    {
        "notepad" => ["notepad"],
        "calculator" => ["calc", "calculatorapp", "windowscalculator", "applicationframehost"],
        "settings" => ["systemsettings", "applicationframehost"],
        _ => [Path.GetFileNameWithoutExtension(appName.Trim())]
    };
}

static IReadOnlyList<string> GetLaunchTitleTerms(string appName)
{
    var normalized = StartMenuLauncher.ResolveAppName(appName);
    return normalized.ToLowerInvariant() switch
    {
        "notepad" => ["notepad"],
        "calculator" => ["calculator"],
        "settings" => ["settings"],
        _ => [normalized]
    };
}

static int LiveBrowser(string commandOrTarget)
{
    Console.WriteLine();
    if (commandOrTarget.Contains(';', StringComparison.Ordinal))
    {
        return LiveBrowserSequence(commandOrTarget);
    }

    Console.WriteLine($"LIVE: opening browser target '{commandOrTarget}'.");

    return RunBrowserStep(commandOrTarget);
}

static int LiveBrowserSequence(string sequence)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: running browser sequence '{sequence}'.");

    var steps = sequence
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var step in steps)
    {
        var exitCode = RunBrowserStep(step);
        if (exitCode != 0)
        {
            Console.WriteLine($"FAIL: Browser sequence stopped at step '{step}'.");
            return exitCode;
        }
    }

    return 0;
}

static int RunBrowserStep(string commandOrTarget)
{
    Console.WriteLine($"LIVE: opening browser target '{commandOrTarget}'.");

    var service = new BrowserLaunchService();
    var intent = AlphaVoiceIntentParser.ParseVerifiedTranscript($"Callsign echo one {commandOrTarget}", "Callsign", "echo one");
    var target = commandOrTarget;
    var browserTarget = BrowserOpenTarget.Default;
    var browserAction = string.Empty;

    if (intent.Kind == AlphaVoiceIntentKind.Browser)
    {
        if (intent.Target.StartsWith("browser-", StringComparison.OrdinalIgnoreCase))
        {
            browserAction = intent.Target;
            target = "https://example.com";
        }
        else
        {
            target = intent.Target;
            browserTarget = intent.BrowserTarget;
        }
    }

    if (service.TryOpen(target, out var message, out var targetUri, browserTarget: browserTarget))
    {
        Console.WriteLine($"PASS: {message}");
        Console.WriteLine($"INFO: Browser URI: {targetUri}");
        if (!string.IsNullOrWhiteSpace(browserAction))
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            if (service.TryExecuteBrowserAction(browserAction, out var actionMessage))
            {
                Console.WriteLine($"PASS: {actionMessage}");
                return 0;
            }

            Console.WriteLine($"FAIL: {actionMessage}");
            return 1;
        }

        return 0;
    }

    Console.WriteLine($"FAIL: {message}");
    return 1;
}

static int LiveFileSearch(string query)
{
    Console.WriteLine();
    Console.WriteLine($"LIVE: searching files for '{query}' and opening the best result in Explorer.");

    var fixtureRoot = Path.Combine(Path.GetTempPath(), "Callsign", "SmokeSearch");
    Directory.CreateDirectory(fixtureRoot);
    var fixtureName = CreateSearchFixtureName(query);
    var fixturePath = Path.Combine(fixtureRoot, fixtureName);
    var service = new FileSearchService();

    try
    {
        File.WriteAllText(fixturePath, $"Callsign smoke fixture for '{query}' created at {DateTime.UtcNow:o}.");

        var report = service.Search(query, new[] { fixtureRoot }, maxResults: 10);
        foreach (var warning in report.Warnings)
            Console.WriteLine($"WARN: {warning}");

        if (report.Results.Count == 0)
        {
            Console.WriteLine("FAIL: No file or folder results were found.");
            return 1;
        }

        var best = report.Results[0];
        Console.WriteLine($"INFO: Best result: {best.FullPath}");
        if (service.TryReveal(best, out var message))
        {
            Console.WriteLine($"PASS: {message}");
            return 0;
        }

        Console.WriteLine($"FAIL: {message}");
        return 1;
    }
    finally
    {
        try
        {
            if (File.Exists(fixturePath))
                File.Delete(fixturePath);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
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
    var fixtureRoot = Path.Combine(Path.GetTempPath(), "Callsign", "SmokeSearch");
    Directory.CreateDirectory(fixtureRoot);
    var fixturePath = Path.Combine(fixtureRoot, CreateSearchFixtureName(query));
    var service = new FileSearchService();

    try
    {
        File.WriteAllText(fixturePath, $"Callsign scripted search fixture for '{query}' created at {DateTime.UtcNow:o}.");

        var report = service.Search(query, new[] { fixtureRoot }, maxResults: 10);
        foreach (var warning in report.Warnings)
            Console.WriteLine($"WARN: {warning}");

        if (report.Results.Count == 0)
        {
            Console.WriteLine($"FAIL: No file or folder results matched '{query}'.");
            return 1;
        }

        if (!service.TryReveal(report.Results[0], out var message))
        {
            Console.WriteLine($"FAIL: {message}");
            return 1;
        }

        session.CompleteLaunch();
        Console.WriteLine($"PASS: Scripted gated session opened file-search result in Explorer. {message}");
        return 0;
    }
    finally
    {
        try
        {
            if (File.Exists(fixturePath))
                File.Delete(fixturePath);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}

static string CreateSearchFixtureName(string query)
{
    var sanitized = new string(query
        .Where(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) || character is '-' or '_')
        .ToArray())
        .Trim();

    if (string.IsNullOrWhiteSpace(sanitized))
        sanitized = "callsign-smoke-search";

    sanitized = sanitized.Replace(' ', '-').Replace('_', '-');
    return $"{sanitized}-fixture.txt";
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

static void SharedVisualStyleDefinesEvidenceTokens()
{
    Require(CallsignVisualStyle.DescribeSurface("wake overlay").Contains(CallsignVisualStyle.TargetName, StringComparison.OrdinalIgnoreCase), "Visual style description should name the macOS Voice Control target.");
    Require(CallsignVisualStyle.DescribeSurface("wake overlay").Contains(CallsignVisualStyle.EvidenceMarker, StringComparison.OrdinalIgnoreCase), "Visual style description should include concrete evidence tokens.");
    Require(CallsignVisualStyle.CompactRadius >= 16, "Compact surface radius should preserve soft, Apple-style rounded HUD geometry.");
    Require(CallsignVisualStyle.LargeSurfaceRadius <= 28, "Large surface radius should stay compact instead of drifting into oversized card styling.");
    Require(CallsignVisualStyle.IsAcceptedSurfaceOpacity(0.86), "Minimum overlay opacity should be accepted.");
    Require(CallsignVisualStyle.IsAcceptedSurfaceOpacity(0.985), "High but translucent surface opacity should be accepted.");
    Require(!CallsignVisualStyle.IsAcceptedSurfaceOpacity(1.0), "Fully opaque surfaces should not satisfy the shared translucent overlay contract.");
    Require(CallsignVisualStyle.HasAccessibleTextContrast(CallsignVisualStyle.PrimaryText, CallsignVisualStyle.SurfaceBackground), "Primary text should meet accessible contrast on the shared surface background.");
    Require(CallsignVisualStyle.HasAccessibleTextContrast(CallsignVisualStyle.SecondaryText, CallsignVisualStyle.SurfaceBackground), "Secondary text should meet accessible contrast on the shared surface background.");
    Require(CallsignVisualStyle.HasAccessibleTextContrast(CallsignVisualStyle.OverlayText, CallsignVisualStyle.OverlayPanel), "Overlay text should meet accessible contrast on the translucent panel color.");
    Require(CallsignVisualStyle.SurfacePrinciples.Contains("non-activating", StringComparison.OrdinalIgnoreCase), "Shared visual principles should keep overlays non-activating.");
    Require(CallsignVisualStyle.SurfacePrinciples.Contains("visible-status", StringComparison.OrdinalIgnoreCase), "Shared visual principles should keep status visible.");
    Require(CallsignVisualStyle.EvidenceMarker.Contains("stop-visible", StringComparison.OrdinalIgnoreCase), "Visual evidence marker should require a visible stop/cancel affordance.");
}

static void RequireVisualContract(string visualStyleName, string surfaceName)
{
    Require(visualStyleName.Contains(CallsignVisualStyle.TargetName, StringComparison.OrdinalIgnoreCase), $"Expected {surfaceName} visual style to target {CallsignVisualStyle.TargetName}, got '{visualStyleName}'.");
    foreach (var principle in CallsignVisualStyle.SurfacePrinciples.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        Require(visualStyleName.Contains(principle, StringComparison.OrdinalIgnoreCase), $"Expected {surfaceName} visual style to include '{principle}', got '{visualStyleName}'.");
    foreach (var token in CallsignVisualStyle.EvidenceMarker.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        Require(visualStyleName.Contains(token, StringComparison.OrdinalIgnoreCase), $"Expected {surfaceName} visual style to include evidence token '{token}', got '{visualStyleName}'.");
}

static void TryDeleteDirectory(string path)
{
    try
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
    catch
    {
    }
}

static void SetField(object target, string fieldName, object? value)
{
    var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    if (field == null)
        throw new InvalidOperationException($"Missing field {fieldName} on {target.GetType().Name}.");

    field.SetValue(target, value);
}

sealed class StaticResponseHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public StaticResponseHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            RequestMessage = request
        };

        return Task.FromResult(response);
    }
}

sealed class PaidSampleCommandPack : ICallsignCommandPack
{
    private readonly string _signatureStatus;

    public PaidSampleCommandPack(string signatureStatus = "signed")
    {
        _signatureStatus = signatureStatus;
        Descriptor = new CallsignPackDescriptor(
            PackId: "paid-sample-pack",
            DisplayName: "Paid Sample Pack",
            Version: "1.0.0",
            Tier: CallsignPackTier.Pro,
            Description: "Smoke-test Pro extension pack.",
            SignatureStatus: _signatureStatus,
            IsCommunity: false,
            RequiresSignature: true);
    }

    private static readonly CallsignCommandDefinition[] PaidCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "paid-sample-action",
            DisplayName: "Paid sample action",
            VoicePhrases: new[] { "paid sample action" },
            Description: "Smoke-test command used to prove paid-tier entitlement gating.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Pro,
            RiskTier: CallsignCommandRiskTier.LocalReversible,
            Category: "Paid sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "paid sample action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; }

    public IReadOnlyList<CallsignCommandDefinition> Commands => PaidCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(true, "Paid sample action executed."));
}

sealed class MixedTierCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] MixedTierCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "advanced-mixed-action",
            DisplayName: "Advanced mixed action",
            VoicePhrases: new[] { "advanced mixed action" },
            Description: "Smoke-test command used to prove command-level entitlement gating.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Advanced,
            RiskTier: CallsignCommandRiskTier.LocalReversible,
            Category: "Advanced sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "advanced mixed action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "mixed-tier-pack",
        DisplayName: "Mixed Tier Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test Free pack that contains a paid-tier command.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => MixedTierCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(true, "Advanced mixed action executed."));
}

sealed class InvalidMetadataCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] InvalidCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "invalid-metadata-action",
            DisplayName: "Invalid metadata action",
            VoicePhrases: new[] { "invalid metadata action" },
            Description: "Smoke-test command used to prove invalid metadata cannot route.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.LocalReversible,
            Category: null!,
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            HelpText: "This command intentionally omits required category metadata.",
            Examples: new[] { "invalid metadata action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "invalid-metadata-pack",
        DisplayName: "Invalid Metadata Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test pack with invalid command metadata.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => InvalidCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(true, "Invalid metadata action should never execute."));
}

sealed class ApprovalRequiredCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] ApprovalCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "approval-sample-action",
            DisplayName: "Approval sample action",
            VoicePhrases: new[] { "approval sample action" },
            Description: "Smoke-test command used to prove registry execution cannot bypass policy.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.ExternalSideEffect,
            Category: "Policy sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            ApprovalRequirement: CallsignCommandApprovalRequirement.RequireApproval,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "approval sample action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "approval-pack",
        DisplayName: "Approval Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test pack with an approval-required command.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => ApprovalCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(true, "Approval sample action executed.", AuditEvent: $"approval-pack:{context.CommandId}"));
}

sealed class BlockedCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] BlockedCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "blocked-sample-action",
            DisplayName: "Blocked sample action",
            VoicePhrases: new[] { "blocked sample action" },
            Description: "Smoke-test command used to prove the palette can surface blocked-risk commands.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.DangerousOrBlocked,
            Category: "Policy sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            ApprovalRequirement: CallsignCommandApprovalRequirement.Blocked,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "blocked sample action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "blocked-pack",
        DisplayName: "Blocked Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test pack with a blocked-risk command.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => BlockedCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(false, "Blocked sample action should never execute."));
}

sealed class FreshIdentityCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] FreshIdentityCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "fresh-identity-sample-action",
            DisplayName: "Fresh identity sample action",
            VoicePhrases: new[] { "fresh identity sample action" },
            Description: "Smoke-test command used to prove the palette can surface fresh-identity requirements.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.Observe,
            Category: "Policy sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            ApprovalRequirement: CallsignCommandApprovalRequirement.RequireFreshIdentity,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "fresh identity sample action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "fresh-identity-pack",
        DisplayName: "Fresh Identity Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test pack with a fresh-identity command.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => FreshIdentityCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(false, "Fresh identity sample action should never execute."));
}

sealed class ExternalSideEffectCommandPack : ICallsignCommandPack
{
    private static readonly CallsignCommandDefinition[] ExternalCommands =
    [
        new CallsignCommandDefinition(
            CommandId: "external-side-effect-sample-action",
            DisplayName: "External side effect sample action",
            VoicePhrases: new[] { "external side effect sample action" },
            Description: "Smoke-test command used to prove the palette can surface external-side-effect risk.",
            Kind: CallsignCommandKind.Extension,
            Tier: CallsignPackTier.Free,
            RiskTier: CallsignCommandRiskTier.ExternalSideEffect,
            Category: "Policy sample",
            PrivacyImpact: CallsignCommandPrivacyImpact.None,
            ApprovalRequirement: CallsignCommandApprovalRequirement.RequireApproval,
            HelpText: "Used by alpha smoke tests only.",
            Examples: new[] { "external side effect sample action" },
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus)
    ];

    public CallsignPackDescriptor Descriptor { get; } = new(
        PackId: "external-side-effect-pack",
        DisplayName: "External Side Effect Pack",
        Version: "1.0.0",
        Tier: CallsignPackTier.Free,
        Description: "Smoke-test pack with an external-side-effect command.",
        SignatureStatus: "signed",
        IsCommunity: false,
        RequiresSignature: false);

    public IReadOnlyList<CallsignCommandDefinition> Commands => ExternalCommands;

    public ValueTask<CallsignCommandExecutionResult> ExecuteAsync(CallsignCommandExecutionContext context) =>
        ValueTask.FromResult(new CallsignCommandExecutionResult(false, "External side effect sample action should never execute."));
}
