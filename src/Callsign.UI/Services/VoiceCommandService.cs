using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Recognition;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Callsign.UI.Services;

public sealed class VoiceCommandService : IDisposable
{
    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;
    private const int SegmentSilenceMilliseconds = 850;
    private const int MinimumSegmentMilliseconds = 300;
    private const double SpeechEnergyThreshold = 0.010;

    private readonly object _gate = new();
    private readonly SemaphoreSlim _segmentSignal = new(0);
    private readonly ConcurrentQueue<CapturedSegment> _segments = new();
    private readonly string _modelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Models",
        "ggml-base.bin");

    private WaveInEvent? _waveIn;
    private WaveFileWriter? _currentSegmentWriter;
    private FileStream? _currentSegmentStream;
    private CancellationTokenSource? _cts;
    private Task? _segmentPumpTask;
    private ICommandTranscriber? _transcriber;
    private string? _startupWarning;
    private bool _isInitializing;
    private DateTime _currentSegmentStartedUtc;
    private DateTime _lastSpeechUtc;
    private int _segmentId;
    private string _languageCode = "en-US";
    private string _wakeWord = "Callsign";
    private string _callsign = string.Empty;

    public event EventHandler<VoiceTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<VoiceRecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler? ListeningStateChanged;

    public bool IsListening { get; private set; }
    public string? LastStartupWarning { get; private set; }
    public string CurrentModeDescription { get; private set; } = "Offline recognition is stopped.";

    public void Start(string languageCode, string wakeWord, string callsign)
    {
        if (IsListening)
            return;

        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en-US" : languageCode;
        _wakeWord = string.IsNullOrWhiteSpace(wakeWord) ? "Callsign" : wakeWord.Trim();
        _callsign = string.IsNullOrWhiteSpace(callsign) ? string.Empty : callsign.Trim();
        _startupWarning = null;
        _isInitializing = true;

        try
        {
            _cts = new CancellationTokenSource();
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, Channels),
                BufferMilliseconds = 50
            };
            _waveIn.DataAvailable += WaveInDataAvailable;
            _waveIn.RecordingStopped += WaveInRecordingStopped;
            _waveIn.StartRecording();

            _segmentPumpTask = Task.Run(() => PumpSegmentsAsync(_cts.Token));
            IsListening = true;
            CurrentModeDescription = "Listening with local transcription.";
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
            _ = Task.Run(() => InitializeTranscriberAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            DisposeEngine();
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs(GetReadableMicStartMessage(ex)));
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public void Stop()
    {
        if (!IsListening && _waveIn == null && _segmentPumpTask == null)
            return;

        try
        {
            _cts?.Cancel();
        }
        catch (Exception ex)
        {
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs($"Unable to stop microphone listener cleanly: {ex.Message}"));
        }

        lock (_gate)
        {
            FinalizeCurrentSegment(commit: false);
        }

        DisposeEngine();
        ListeningStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void WaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        CapturedSegment? readySegment = null;

        lock (_gate)
        {
            if (_cts?.IsCancellationRequested == true)
                return;

            var now = DateTime.UtcNow;
            var speechEnergy = GetSpeechEnergy(e.Buffer, e.BytesRecorded);
            var speechDetected = speechEnergy >= SpeechEnergyThreshold;

            if (speechDetected && _currentSegmentWriter == null)
            {
                StartCurrentSegment(now);
            }

            if (_currentSegmentWriter == null)
                return;

            _currentSegmentWriter.Write(e.Buffer, 0, e.BytesRecorded);
            _currentSegmentWriter.Flush();

            if (speechDetected)
                _lastSpeechUtc = now;

            var elapsedSinceLastSpeech = now - _lastSpeechUtc;
            var segmentAge = now - _currentSegmentStartedUtc;
            if (segmentAge.TotalMilliseconds >= MinimumSegmentMilliseconds
                && elapsedSinceLastSpeech.TotalMilliseconds >= SegmentSilenceMilliseconds)
            {
                readySegment = FinalizeCurrentSegment(commit: true);
            }
        }

        if (readySegment != null)
            EnqueueSegment(readySegment);
    }

    private void WaveInRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs($"Speech capture stopped: {e.Exception.Message}"));
    }

    private async Task PumpSegmentsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _segmentSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            while (_segments.TryDequeue(out var segment))
            {
                try
                {
                    var transcript = await TranscribeSegmentAsync(segment, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(transcript.Transcript))
                        continue;

                    TranscriptReceived?.Invoke(this, new VoiceTranscriptEventArgs(transcript.Transcript, transcript.Confidence));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs(ex.Message));
                }
                finally
                {
                    TryDeleteSegmentFile(segment.Path);
                }
            }
        }
    }

    private async Task InitializeTranscriberAsync(CancellationToken cancellationToken)
    {
        if (_transcriber != null || cancellationToken.IsCancellationRequested)
            return;

        try
        {
            _transcriber = await WhisperCommandTranscriber.TryCreateAsync(
                _modelPath,
                _languageCode,
                warning => AppendStartupWarning(warning),
                cancellationToken).ConfigureAwait(false);

            CurrentModeDescription = _transcriber.ModeDescription;
            LastStartupWarning = _startupWarning;
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
            if (!string.IsNullOrWhiteSpace(_startupWarning))
                RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs(_startupWarning));
        }
        catch (Exception ex)
        {
            AppendStartupWarning($"Local Whisper transcription was unavailable. Falling back to compatibility mode. {ex.Message}");
            CurrentModeDescription = "Compatibility transcription fallback.";
            _transcriber = new SystemSpeechCommandTranscriber(_languageCode, warning => AppendStartupWarning(warning));
            LastStartupWarning = _startupWarning;
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
            RecognitionError?.Invoke(this, new VoiceRecognitionErrorEventArgs(_startupWarning ?? ex.Message));
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async Task<VoiceRecognitionResult> TranscribeSegmentAsync(CapturedSegment segment, CancellationToken cancellationToken)
    {
        var transcriber = await EnsureTranscriberAsync(cancellationToken).ConfigureAwait(false);
        return await transcriber.TranscribeAsync(segment.Path, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ICommandTranscriber> EnsureTranscriberAsync(CancellationToken cancellationToken)
    {
        if (_transcriber != null)
            return _transcriber;

        if (!_isInitializing)
        {
            _isInitializing = true;
            _ = InitializeTranscriberAsync(cancellationToken);
        }

        while (_transcriber == null && !cancellationToken.IsCancellationRequested)
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        return _transcriber ?? new SystemSpeechCommandTranscriber(_languageCode, warning => AppendStartupWarning(warning));
    }

    private void EnqueueSegment(CapturedSegment segment)
    {
        _segments.Enqueue(segment);
        _segmentSignal.Release();
    }

    private void StartCurrentSegment(DateTime nowUtc)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Logs",
            "segments");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"segment-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Interlocked.Increment(ref _segmentId)}.wav");
        _currentSegmentStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        _currentSegmentWriter = new WaveFileWriter(_currentSegmentStream, new WaveFormat(SampleRate, BitsPerSample, Channels));
        _currentSegmentStartedUtc = nowUtc;
        _lastSpeechUtc = nowUtc;
    }

    private CapturedSegment? FinalizeCurrentSegment(bool commit)
    {
        if (_currentSegmentWriter == null || _currentSegmentStream == null)
            return null;

        var path = _currentSegmentStream.Name;
        var startedUtc = _currentSegmentStartedUtc;
        var duration = DateTime.UtcNow - startedUtc;

        try
        {
            _currentSegmentWriter.Dispose();
            _currentSegmentStream.Dispose();
        }
        finally
        {
            _currentSegmentWriter = null;
            _currentSegmentStream = null;
        }

        if (!commit || duration.TotalMilliseconds < MinimumSegmentMilliseconds)
        {
            TryDeleteSegmentFile(path);
            return null;
        }

        return new CapturedSegment(path, startedUtc, duration);
    }

    private void DisposeEngine()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= WaveInDataAvailable;
            _waveIn.RecordingStopped -= WaveInRecordingStopped;
            try
            {
                _waveIn.StopRecording();
            }
            catch
            {
                // Best-effort shutdown only.
            }

            _waveIn.Dispose();
            _waveIn = null;
        }

        _cts?.Dispose();
        _cts = null;

        if (_segmentPumpTask != null)
        {
            try
            {
                _segmentPumpTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Best-effort shutdown only.
            }

            _segmentPumpTask = null;
        }

        if (_currentSegmentWriter != null || _currentSegmentStream != null)
        {
            lock (_gate)
            {
                FinalizeCurrentSegment(commit: false);
            }
        }

        while (_segments.TryDequeue(out var segment))
            TryDeleteSegmentFile(segment.Path);

        IsListening = false;
        CurrentModeDescription = "Offline recognition is stopped.";
        LastStartupWarning = _startupWarning;
    }

    private static double GetSpeechEnergy(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded < 2)
            return 0;

        var sampleCount = bytesRecorded / 2;
        if (sampleCount == 0)
            return 0;

        double total = 0;
        for (var index = 0; index < sampleCount; index++)
        {
            var sample = BitConverter.ToInt16(buffer, index * 2);
            total += Math.Abs(sample) / 32768.0;
        }

        return total / sampleCount;
    }

    private void AppendStartupWarning(string message)
    {
        _startupWarning = string.IsNullOrWhiteSpace(_startupWarning)
            ? message
            : $"{_startupWarning} {message}";
        LastStartupWarning = _startupWarning;
    }

    private static void TryDeleteSegmentFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static string GetReadableMicStartMessage(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        if (message.Contains("access is denied") || message.Contains("permission"))
            return "Unable to start microphone listener: microphone permission was denied or is unavailable.";

        if (message.Contains("no default capture device") || message.Contains("device not found") || message.Contains("wavein"))
            return "Unable to start microphone listener: no microphone device was found.";

        return $"Unable to start microphone listener: {ex.Message}";
    }

    private sealed record CapturedSegment(string Path, DateTime StartedUtc, TimeSpan Duration);
}

public sealed class VoiceRecognitionSettings
{
    public string Mode { get; set; } = "Local";
    public string? InputDeviceId { get; set; }
    public string? ModelPath { get; set; }
    public string LanguageCode { get; set; } = "en-US";
    public double WakeThreshold { get; set; } = 0.010;
    public double CommandConfidenceThreshold { get; set; } = 0.65;
    public bool CloudOptIn { get; set; }
    public bool UseVoiceActivityDetection { get; set; } = true;
    public bool UseNoiseSuppression { get; set; }
}

public sealed class VoiceRecognitionResult
{
    public required string Transcript { get; init; }
    public required float Confidence { get; init; }
    public required string Engine { get; init; }
    public required TimeSpan Latency { get; init; }
    public IReadOnlyList<string> Alternatives { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string? MatchedApp { get; init; }
    public bool NeedsConfirmation { get; init; }
}

public sealed class VoiceRecognitionDiagnostics
{
    private readonly Queue<string> _recent = new();
    private readonly int _capacity;

    public VoiceRecognitionDiagnostics(int capacity = 20)
    {
        _capacity = Math.Max(1, capacity);
    }

    public IReadOnlyList<string> Recent
    {
        get
        {
            lock (_recent)
            {
                return _recent.ToArray();
            }
        }
    }

    public void Add(string message)
    {
        lock (_recent)
        {
            _recent.Enqueue($"{DateTime.UtcNow:O} {message}");
            while (_recent.Count > _capacity)
                _recent.Dequeue();
        }
    }
}

internal interface ICommandTranscriber
{
    string ModeDescription { get; }
    Task<VoiceRecognitionResult> TranscribeAsync(string wavPath, CancellationToken cancellationToken);
}

internal sealed class WhisperCommandTranscriber : ICommandTranscriber, IAsyncDisposable
{
    private readonly string _modelPath;
    private readonly string _languageCode;
    private readonly Action<string> _appendWarning;
    private WhisperFactory? _factory;

    public string ModeDescription => "Local Whisper transcription.";

    private WhisperCommandTranscriber(string modelPath, string languageCode, Action<string> appendWarning)
    {
        _modelPath = modelPath;
        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en-US" : languageCode;
        _appendWarning = appendWarning;
    }

    public static async Task<ICommandTranscriber> TryCreateAsync(
        string modelPath,
        string languageCode,
        Action<string> appendWarning,
        CancellationToken cancellationToken)
    {
        var transcriber = new WhisperCommandTranscriber(modelPath, languageCode, appendWarning);
        await transcriber.EnsureModelAsync(cancellationToken).ConfigureAwait(false);
        return transcriber;
    }

    public async Task<VoiceRecognitionResult> TranscribeAsync(string wavPath, CancellationToken cancellationToken)
    {
        await EnsureModelAsync(cancellationToken).ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        using var fileStream = File.OpenRead(wavPath);
        using var processor = _factory!.CreateBuilder()
            .WithLanguage(_languageCode)
            .Build();

        var segments = new List<string>();
        await foreach (var result in processor.ProcessAsync(fileStream).WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
                segments.Add(result.Text.Trim());
        }

        var transcript = string.Join(" ", segments).Trim();
        return new VoiceRecognitionResult
        {
            Transcript = transcript,
            Confidence = string.IsNullOrWhiteSpace(transcript) ? 0f : 0.92f,
            Engine = "Whisper",
            Latency = sw.Elapsed,
            Alternatives = Array.Empty<string>(),
            Warnings = Array.Empty<string>(),
            NeedsConfirmation = false
        };
    }

    public ValueTask DisposeAsync()
    {
        _factory?.Dispose();
        _factory = null;
        return ValueTask.CompletedTask;
    }

    private async Task EnsureModelAsync(CancellationToken cancellationToken)
    {
        if (_factory != null)
            return;

        var directory = Path.GetDirectoryName(_modelPath)
            ?? throw new InvalidOperationException("Model path could not be resolved.");
        Directory.CreateDirectory(directory);

        var repairedModel = false;
        while (true)
        {
            try
            {
                if (!File.Exists(_modelPath))
                {
                    _appendWarning("Local Whisper model is missing. Downloading the base model now.");
                    await DownloadModelAsync(cancellationToken).ConfigureAwait(false);
                }

                _factory = WhisperFactory.FromPath(_modelPath);
                return;
            }
            catch (Exception) when (!repairedModel)
            {
                repairedModel = true;
                _appendWarning("Local Whisper model could not be loaded cleanly. Re-downloading a fresh copy.");
                TryDeleteFile(_modelPath);
                TryDeleteFile(_modelPath + ".download");
            }
        }
    }

    private async Task DownloadModelAsync(CancellationToken cancellationToken)
    {
        var tempPath = _modelPath + ".download";
        TryDeleteFile(tempPath);

        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base).ConfigureAwait(false);
        await using (var fileWriter = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await modelStream.CopyToAsync(fileWriter, cancellationToken).ConfigureAwait(false);
            await fileWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        TryDeleteFile(_modelPath);
        File.Move(tempPath, _modelPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}

internal sealed class SystemSpeechCommandTranscriber : ICommandTranscriber
{
    private readonly string _languageCode;
    private readonly Action<string> _appendWarning;

    public SystemSpeechCommandTranscriber(string languageCode, Action<string> appendWarning)
    {
        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en-US" : languageCode;
        _appendWarning = appendWarning;
    }

    public string ModeDescription => "Compatibility System.Speech transcription.";

    public Task<VoiceRecognitionResult> TranscribeAsync(string wavPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var recognizer = new SpeechRecognitionEngine(CultureInfo.GetCultureInfo(_languageCode));
            recognizer.LoadGrammar(new DictationGrammar());
            recognizer.SetInputToWaveFile(wavPath);
            var result = recognizer.Recognize(TimeSpan.FromSeconds(20));
            if (result == null)
            {
                _appendWarning("Compatibility recognizer produced no transcript.");
                return Task.FromResult(new VoiceRecognitionResult
                {
                    Transcript = string.Empty,
                    Confidence = 0f,
                    Engine = "System.Speech",
                    Latency = TimeSpan.Zero,
                    Alternatives = Array.Empty<string>(),
                    Warnings = new[] { "No transcript produced." },
                    NeedsConfirmation = true
                });
            }

            return Task.FromResult(new VoiceRecognitionResult
            {
                Transcript = result.Text?.Trim() ?? string.Empty,
                Confidence = result.Confidence,
                Engine = "System.Speech",
                Latency = TimeSpan.Zero,
                Alternatives = Array.Empty<string>(),
                Warnings = Array.Empty<string>(),
                NeedsConfirmation = result.Confidence < 0.70f
            });
        }
        catch (Exception ex)
        {
            _appendWarning($"Compatibility recognizer failed: {ex.Message}");
            return Task.FromResult(new VoiceRecognitionResult
            {
                Transcript = string.Empty,
                Confidence = 0f,
                Engine = "System.Speech",
                Latency = TimeSpan.Zero,
                Alternatives = Array.Empty<string>(),
                Warnings = new[] { ex.Message },
                NeedsConfirmation = true
            });
        }
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
