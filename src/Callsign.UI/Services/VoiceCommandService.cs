using System.Globalization;
using System.Speech.Recognition;

namespace Callsign.UI.Services;

public sealed class VoiceCommandService : IDisposable
{
    private SpeechRecognitionEngine? _engine;
    private string? _startupWarning;
    private bool _grammarLoaded;

    public event EventHandler<VoiceTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<VoiceRecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler? ListeningStateChanged;

    public bool IsListening { get; private set; }
    public string? LastStartupWarning { get; private set; }

    public void Start(string languageCode, string wakeWord, string callsign)
    {
        if (IsListening)
            return;

        try
        {
            _grammarLoaded = false;
            _engine = CreateEngine(languageCode);
            TryLoadAlphaCommandGrammar(_engine, wakeWord, callsign);
            TryLoadDictationGrammar(_engine);
            if (!_grammarLoaded)
            {
                throw new InvalidOperationException("No usable speech recognition grammar was available.");
            }
            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += SpeechRecognized;
            _engine.RecognizeCompleted += RecognizeCompleted;
            _engine.RecognizeAsync(RecognizeMode.Multiple);
            IsListening = true;
            LastStartupWarning = _startupWarning;
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
            if (!string.IsNullOrWhiteSpace(_startupWarning))
                RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs(_startupWarning));
        }
        catch (Exception ex)
        {
            DisposeEngine();
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs($"Unable to start microphone listener: {ex.Message}"));
        }
    }

    public void Stop()
    {
        if (!IsListening && _engine == null)
            return;

        try
        {
            _engine?.RecognizeAsyncCancel();
        }
        catch (Exception ex)
        {
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs($"Unable to stop microphone listener cleanly: {ex.Message}"));
        }

        DisposeEngine();
        ListeningStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var result = e.Result;
        if (result is null)
            return;

        var text = result.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        TranscriptReceived?.Invoke(this, new VoiceTranscriptEventArgs(text, result.Confidence));
    }

    private SpeechRecognitionEngine CreateEngine(string languageCode)
    {
        _startupWarning = null;
        var requestedLanguage = string.IsNullOrWhiteSpace(languageCode) ? "en-US" : languageCode;
        try
        {
            return new SpeechRecognitionEngine(CultureInfo.GetCultureInfo(requestedLanguage));
        }
        catch (Exception)
        {
            AppendStartupWarning($"No '{requestedLanguage}' recognizer was available. Using the Windows default speech recognizer.");
            return new SpeechRecognitionEngine();
        }
    }

    private void TryLoadAlphaCommandGrammar(SpeechRecognitionEngine engine, string wakeWord, string callsign)
    {
        try
        {
            LoadAlphaCommandGrammar(engine, wakeWord, callsign);
            _grammarLoaded = true;
        }
        catch (Exception ex)
        {
            AppendStartupWarning($"Personalized launch grammar was unavailable. Dictation fallback is active. {ex.Message}");
        }
    }

    private void TryLoadDictationGrammar(SpeechRecognitionEngine engine)
    {
        try
        {
            engine.LoadGrammar(new DictationGrammar());
            _grammarLoaded = true;
        }
        catch (Exception ex)
        {
            AppendStartupWarning($"Dictation fallback was unavailable. Personalized launch grammar may still work. {ex.Message}");
        }
    }

    private static void LoadAlphaCommandGrammar(SpeechRecognitionEngine engine, string wakeWord, string callsign)
    {
        var wakeChoices = new Choices(DistinctGrammarChoices(
            NormalizeGrammarPhrase(wakeWord),
            "callsign",
            "call sign"));
        var callsignChoices = new Choices(NormalizeGrammarPhrase(callsign));
        var actionChoices = new Choices(DistinctGrammarChoices(
            "open",
            "open up",
            "open app",
            "open application",
            "open the app",
            "open the application",
            "open the app called",
            "open the application called",
            "launch",
            "launch app",
            "launch application",
            "launch the app",
            "launch the application",
            "launch the app called",
            "launch the application called",
            "start",
            "run"));

        var builder = new GrammarBuilder
        {
            Culture = engine.RecognizerInfo.Culture
        };
        builder.Append(wakeChoices);
        builder.Append(callsignChoices);
        builder.Append(actionChoices);
        builder.AppendDictation();

        engine.LoadGrammar(new Grammar(builder)
        {
            Name = "Callsign alpha launch command"
        });
    }

    private static string NormalizeGrammarPhrase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "callsign";

        return string.Join(
            ' ',
            value.ToLowerInvariant()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string[] DistinctGrammarChoices(params string[] choices) =>
        choices
            .Where(choice => !string.IsNullOrWhiteSpace(choice))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private void AppendStartupWarning(string message)
    {
        _startupWarning = string.IsNullOrWhiteSpace(_startupWarning)
            ? message
            : $"{_startupWarning} {message}";
    }

    private void RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null)
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs($"Speech recognition stopped: {e.Error.Message}"));
    }

    private void DisposeEngine()
    {
        if (_engine == null)
        {
            IsListening = false;
            return;
        }

        _engine.SpeechRecognized -= SpeechRecognized;
        _engine.RecognizeCompleted -= RecognizeCompleted;
        _engine.Dispose();
        _engine = null;
        IsListening = false;
        LastStartupWarning = null;
    }
}

public sealed class VoiceTranscriptEventArgs(string text, float confidence) : EventArgs
{
    public string Text { get; } = text;
    public float Confidence { get; } = confidence;
}

public sealed class VoiceRecognitionErrorEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
