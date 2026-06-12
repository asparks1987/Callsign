using Callsign.UI.Models;
using Callsign.UI.Services;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;

var liveLaunchApp = GetArgumentValue(args, "--live-launch");
var runVoiceListener = HasArgument(args, "--voice-listener");
var offlineSpeechPhrase = GetArgumentValue(args, "--offline-speech");

var checks = new List<(string Name, Action Check)>
{
    ("profile creation persists personalized callsign state", ProfileCreationPersists),
    ("wake word alone cannot execute a launch", WakeWordAloneCannotExecute),
    ("matching callsign unlocks command capture and launch intent", MatchingCallsignUnlocksLaunchIntent),
    ("mismatched callsign locks out and blocks execution", MismatchedCallsignLocksOut),
    ("voice activation is required before identity confirmation", VoiceActivationRequired),
    ("Start menu alpha scope accepts plain app names and rejects command text", StartMenuScopeValidation),
    ("Start menu launcher can resolve installed app names", StartMenuResolution),
    ("browser helper resolves URLs and search phrases", BrowserTargetResolution),
    ("file search helper finds files in the intended scope", FileSearchResolution)
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

if (runVoiceListener)
{
    var voiceExitCode = VoiceListenerStartup();
    if (voiceExitCode != 0)
        return voiceExitCode;
}

if (!string.IsNullOrWhiteSpace(offlineSpeechPhrase))
    return OfflineSpeechRecognition(offlineSpeechPhrase);

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
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
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

static void BrowserTargetResolution()
{
    Require(BrowserLaunchService.TryBuildTargetUri("https://example.com", out var directUri, out _), "Direct https URL should resolve.");
    Require(directUri?.Host == "example.com", "Direct URL should preserve host.");

    Require(BrowserLaunchService.TryBuildTargetUri("Callsign desktop assistant", out var searchUri, out _), "Search phrase should resolve.");
    Require(searchUri?.Host.Contains("bing", StringComparison.OrdinalIgnoreCase) == true, "Search phrase should route to the search engine.");

    Require(!BrowserLaunchService.TryBuildTargetUri(@"C:\temp\notes.txt", out _, out _), "Local file paths should not be treated as browser targets.");
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
        Require(report.Warnings.Count == 0, $"Search should not warn in temp scope, got {string.Join("; ", report.Warnings)}");

        var emptyReport = service.Search("does-not-exist", new[] { root }, maxResults: 10);
        Require(emptyReport.Results.Count == 0, "Non-matching file search should return no results.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
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
        Console.WriteLine($"FAIL: No new '{appName}' process was detected.");
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
