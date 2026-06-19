using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Speech.Recognition;
using Callsign.UI.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Callsign.UI.Services;

public sealed class VoiceCommandService : IDisposable
{
    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;
    private const int MinimumSegmentMilliseconds = 200;
    private const int DefaultSegmentSilenceMilliseconds = 200;
    private const double DefaultWakeDetectionThreshold = 0.01;

    private readonly object _gate = new();
    private readonly SemaphoreSlim _segmentSignal = new(0);
    private readonly ConcurrentQueue<CapturedSegment> _segments = new();
    private readonly string _modelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Models",
        "ggml-base.bin");
    private readonly string _openWakeWordModelPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Models",
        "callsign.onnx");
    private readonly string _openWakeWordRuntimeRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Callsign",
        "Runtime",
        "openwakeword");
    private readonly string _openWakeWordRuntimePythonPath;

    private IWaveIn? _waveIn;
    private WaveFileWriter? _currentSegmentWriter;
    private FileStream? _currentSegmentStream;
    private CancellationTokenSource? _cts;
    private Task? _segmentPumpTask;
    private ICommandTranscriber? _transcriber;
    private IWakeWordDetector? _wakeWordDetector;
    private string? _startupWarning;
    private bool _isInitializing;
    private DateTime _currentSegmentStartedUtc;
    private DateTime _lastSpeechUtc;
    private int _segmentId;
    private string _languageCode = "en-US";
    private string _wakeWord = "Callsign";
    private string _callsign = string.Empty;
    private double _wakeDetectionThreshold = DefaultWakeDetectionThreshold;
    private bool _wakeDiagnosticsEnabled;
    private MicrophoneAudioProcessor _microphoneProcessor = new(new MicrophoneAudioSettings());
    private MicrophoneAudioSnapshot? _lastMicrophoneSnapshot;
    private WaveFormat _captureFormat = MicrophoneAudioProcessor.OutputWaveFormat;
    private HashSet<string>? _currentSegmentWarnings;
    private string? _activeCaptureDeviceName;
    private DateTime? _lastAudioPacketUtc;
    private int _segmentSilenceMilliseconds = DefaultSegmentSilenceMilliseconds;
    private readonly VoiceRecognitionDiagnostics _wakeDiagnostics = new();
    private bool _isSpeechActive;
    private DateTime? _lastSpeechActivityUtc;
    private readonly Queue<byte[]> _wakeWindowChunks = new();
    private int _wakeWindowBytes;
    private int _wakeEvaluationInFlight;
    private DateTime _lastWakeEvaluationUtc = DateTime.MinValue;
    private DateTime _lastWakeWordDetectedUtc = DateTime.MinValue;
    private const int WakeWindowMilliseconds = 1920;
    private static readonly TimeSpan WakeEvaluationCooldown = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan WakeWordRepeatCooldown = TimeSpan.FromSeconds(2);

    public VoiceCommandService()
    {
        _openWakeWordRuntimePythonPath = Path.Combine(_openWakeWordRuntimeRoot, "venv", "Scripts", "python.exe");
    }

    public event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;
    public event EventHandler<WakeWordDetectedEventArgs>? WakeWordEvaluated;
    public event EventHandler<VoiceTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<VoiceRecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler? ListeningStateChanged;
    public event EventHandler? SpeechActivityChanged;

    public bool IsListening { get; private set; }
    public string? LastStartupWarning { get; private set; }
    public string CurrentModeDescription { get; private set; } = "Offline recognition is stopped.";
    public string CurrentWakeWordEngine => _wakeWordDetector?.EngineName ?? "No wake detector";
    public WakeWordDetectionResult? LastWakeWordDetection { get; private set; }
    public MicrophoneAudioSnapshot? CurrentAudioTelemetry => _lastMicrophoneSnapshot;
    public string? ActiveCaptureDeviceName => _activeCaptureDeviceName;
    public DateTime? LastAudioPacketUtc => _lastAudioPacketUtc;
    public bool IsSpeechActive => _isSpeechActive;
    public DateTime? LastSpeechActivityUtc => _lastSpeechActivityUtc;
    public IReadOnlyList<string> WakeDiagnostics => _wakeDiagnostics.Recent;

    public async Task<double?> TryScoreWakeWordSampleAsync(string wavPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
            return null;

        if (!TryEnsureWakeWordDetector())
            return null;

        var result = await ScoreWakeWordFromPathAsync(
            wavPath,
            Array.Empty<string>(),
            requestKeepCandidateWindow: false,
            cancellationToken).ConfigureAwait(false);
        return result.Score;
    }

    public void Start(
        string languageCode,
        string wakeWord,
        string callsign,
        double? wakeThreshold = null,
        string? wakeSensitivity = null,
        bool wakeDiagnosticsEnabled = false,
        MicrophoneAudioSettings? microphoneSettings = null,
        int? segmentSilenceMilliseconds = null)
    {
        if (IsListening)
            return;

        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en-US" : languageCode;
        _wakeWord = string.IsNullOrWhiteSpace(wakeWord) ? "Callsign" : wakeWord.Trim();
        _callsign = string.IsNullOrWhiteSpace(callsign) ? string.Empty : callsign.Trim();
        _wakeDetectionThreshold = ResolveWakeThreshold(wakeThreshold, wakeSensitivity);
        _wakeDiagnosticsEnabled = wakeDiagnosticsEnabled;
        _microphoneProcessor = new MicrophoneAudioProcessor(microphoneSettings ?? new MicrophoneAudioSettings());
        _segmentSilenceMilliseconds = Math.Clamp(segmentSilenceMilliseconds ?? DefaultSegmentSilenceMilliseconds, 150, 2000);
        _lastMicrophoneSnapshot = _microphoneProcessor.LastSnapshot;
        _startupWarning = null;
        if (OpenWakeWordPythonWakeWordDetector.TryCreate(_openWakeWordModelPath, _openWakeWordRuntimePythonPath, out var openWakeWordDetector, out var wakeWarning))
        {
            _wakeWordDetector = openWakeWordDetector;
        }
        else
        {
            _wakeWordDetector = new OpenWakeWordUnavailableWakeWordDetector(wakeWarning);
            AppendStartupWarning(wakeWarning);
        }

        _isInitializing = true;

        try
        {
            _cts = new CancellationTokenSource();
            _waveIn = CreateMicrophoneCapture(out _activeCaptureDeviceName);
            _captureFormat = _waveIn.WaveFormat;
            _waveIn.DataAvailable += WaveInDataAvailable;
            _waveIn.RecordingStopped += WaveInRecordingStopped;
            _waveIn.StartRecording();

            _segmentPumpTask = Task.Run(() => PumpSegmentsAsync(_cts.Token));
            IsListening = true;
            UpdateCurrentModeDescription();
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
            _ = Task.Run(() => WarmUpWakeWordDetectorAsync(_cts.Token));
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
        byte[]? wakeWindowSnapshot = null;

        lock (_gate)
        {
            if (_cts?.IsCancellationRequested == true)
                return;

            var now = DateTime.UtcNow;
            _lastAudioPacketUtc = now;
            var processed = _microphoneProcessor.ProcessBuffer(e.Buffer, e.BytesRecorded, _captureFormat, now);
            _lastMicrophoneSnapshot = processed.Snapshot;
            var speechDetected = processed.SpeechDetected;
            var audioWarnings = processed.Snapshot.Warnings;
            var speechActivityChanged = false;
            var wakeFrame = MicrophoneAudioProcessor.ConvertToWakePcm16(e.Buffer, e.BytesRecorded, _captureFormat);
            wakeWindowSnapshot = UpdateWakeWindowLocked(wakeFrame, now);

            if (speechDetected && _currentSegmentWriter == null)
            {
                StartCurrentSegment(now);
                speechActivityChanged = UpdateSpeechActivityStateLocked(now);
            }

            if (_currentSegmentWriter == null)
            {
                if (_isSpeechActive && UpdateSpeechActivityStateLocked(now))
                    speechActivityChanged = true;

                if (speechActivityChanged)
                    SpeechActivityChanged?.Invoke(this, EventArgs.Empty);

                return;
            }

            foreach (var warning in audioWarnings)
                _currentSegmentWarnings?.Add(warning);

            _currentSegmentWriter.Write(processed.ProcessedBuffer, 0, processed.BytesRecorded);
            _currentSegmentWriter.Flush();

            if (speechDetected)
            {
                _lastSpeechUtc = now;
                if (UpdateSpeechActivityStateLocked(now))
                    speechActivityChanged = true;
            }

            var elapsedSinceLastSpeech = now - _lastSpeechUtc;
            var segmentAge = now - _currentSegmentStartedUtc;
            if (segmentAge.TotalMilliseconds >= MinimumSegmentMilliseconds
                && elapsedSinceLastSpeech.TotalMilliseconds >= _segmentSilenceMilliseconds)
            {
                readySegment = FinalizeCurrentSegment(commit: true);
                if (UpdateSpeechActivityStateLocked(now))
                    speechActivityChanged = true;
            }

            if (speechActivityChanged)
                SpeechActivityChanged?.Invoke(this, EventArgs.Empty);
        }

        if (readySegment != null)
            EnqueueSegment(readySegment);

        if (wakeWindowSnapshot != null)
            EvaluateWakeWindowAsync(wakeWindowSnapshot, cancellationToken: CancellationToken.None);
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
                    var wakeResult = await DetectWakeWordFromAudioAsync(segment, cancellationToken).ConfigureAwait(false);
                    if (wakeResult.Detected)
                        PublishWakeWordDetection(wakeResult);

                    var transcript = await TranscribeSegmentAsync(segment, cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(transcript.Transcript))
                        continue;

                    TranscriptReceived?.Invoke(
                        this,
                        new VoiceTranscriptEventArgs(
                            transcript.Transcript,
                            transcript.Confidence,
                            segment.Path,
                            segment.AudioQualityWarnings));
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
                    if (!_wakeDiagnosticsEnabled)
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

            _startupWarning = null;
            UpdateCurrentModeDescription();
            LastStartupWarning = null;
            ListeningStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AppendStartupWarning($"Local Whisper transcription was unavailable. Falling back to compatibility mode. {ex.Message}");
            _transcriber = new SystemSpeechCommandTranscriber(_languageCode, warning => AppendStartupWarning(warning));
            UpdateCurrentModeDescription();
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

    private async Task<WakeWordDetectionResult> DetectWakeWordFromAudioAsync(CapturedSegment segment, CancellationToken cancellationToken)
    {
        return await DetectWakeWordFromPathAsync(segment.Path, segment.AudioQualityWarnings, requestKeepCandidateWindow: _wakeDiagnosticsEnabled, cancellationToken).ConfigureAwait(false);
    }

    private async Task<WakeWordDetectionResult> DetectWakeWordFromPathAsync(
        string wavPath,
        IReadOnlyList<string> audioQualityWarnings,
        bool requestKeepCandidateWindow,
        CancellationToken cancellationToken)
    {
        var detector = _wakeWordDetector ?? new OpenWakeWordUnavailableWakeWordDetector("openWakeWord wake detection is not initialized.");
        var result = await detector.DetectAsync(
            new WakeWordDetectionRequest(
                wavPath,
                _wakeWord,
                _languageCode,
                _wakeDetectionThreshold,
                BuildWakePhrases(_wakeWord),
                audioQualityWarnings,
                requestKeepCandidateWindow),
            cancellationToken).ConfigureAwait(false);

        RememberWakeWordDetection(result);
        WakeWordEvaluated?.Invoke(this, new WakeWordDetectedEventArgs(result));
        return result;
    }

    private async Task<WakeWordDetectionResult> ScoreWakeWordFromPathAsync(
        string wavPath,
        IReadOnlyList<string> audioQualityWarnings,
        bool requestKeepCandidateWindow,
        CancellationToken cancellationToken)
    {
        if (!TryEnsureWakeWordDetector())
        {
            return new WakeWordDetectionResult
            {
                Detected = false,
                Score = 0,
                Threshold = _wakeDetectionThreshold,
                Engine = "openWakeWord unavailable",
                AudioQualityWarnings = audioQualityWarnings,
                TimestampUtc = DateTime.UtcNow,
                CandidateWindowPath = requestKeepCandidateWindow ? wavPath : null
            };
        }

        var detector = _wakeWordDetector!;
        return await detector.DetectAsync(
            new WakeWordDetectionRequest(
                wavPath,
                _wakeWord,
                _languageCode,
                _wakeDetectionThreshold,
                BuildWakePhrases(_wakeWord),
                audioQualityWarnings,
                requestKeepCandidateWindow),
            cancellationToken).ConfigureAwait(false);
    }

    private bool TryEnsureWakeWordDetector()
    {
        if (_wakeWordDetector != null)
            return true;

        if (OpenWakeWordPythonWakeWordDetector.TryCreate(_openWakeWordModelPath, _openWakeWordRuntimePythonPath, out var detector, out _))
        {
            _wakeWordDetector = detector;
            return true;
        }

        return false;
    }

    private async Task WarmUpWakeWordDetectorAsync(CancellationToken cancellationToken)
    {
        if (!TryEnsureWakeWordDetector() || _wakeWordDetector is not OpenWakeWordPythonWakeWordDetector)
            return;

        try
        {
            var warmupPath = await WriteWakeWarmupSampleAsync(cancellationToken).ConfigureAwait(false);
            if (warmupPath == null)
                return;

            try
            {
                await _wakeWordDetector.DetectAsync(
                    new WakeWordDetectionRequest(
                        warmupPath,
                        _wakeWord,
                        _languageCode,
                        1.0,
                        BuildWakePhrases(_wakeWord),
                        Array.Empty<string>(),
                        KeepCandidateWindow: false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Warmup is best-effort only.
            }
            finally
            {
                TryDeleteSegmentFile(warmupPath);
            }
        }
        catch
        {
            // Warmup is best-effort only.
        }
    }

    private static async Task<string?> WriteWakeWarmupSampleAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "wake-warmup");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"wake-warmup-{Guid.NewGuid():N}.wav");
        var bytesPerSecond = MicrophoneAudioProcessor.OutputWaveFormat.AverageBytesPerSecond;
        var warmupBytes = Math.Max(1, (int)(bytesPerSecond * 0.24));
        var buffer = new byte[warmupBytes];
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, useAsync: true);
        using var writer = new WaveFileWriter(stream, MicrophoneAudioProcessor.OutputWaveFormat);
        writer.Write(buffer, 0, buffer.Length);
        writer.Flush();
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return path;
    }

    private void EvaluateWakeWindowAsync(byte[] wakeWindowSnapshot, CancellationToken cancellationToken)
    {
        if (wakeWindowSnapshot.Length == 0)
            return;

        if (Interlocked.CompareExchange(ref _wakeEvaluationInFlight, 1, 0) != 0)
            return;

        var now = DateTime.UtcNow;
        if (now - _lastWakeWordDetectedUtc < WakeWordRepeatCooldown)
        {
            Interlocked.Exchange(ref _wakeEvaluationInFlight, 0);
            return;
        }

        if (now - _lastWakeEvaluationUtc < WakeEvaluationCooldown)
        {
            Interlocked.Exchange(ref _wakeEvaluationInFlight, 0);
            return;
        }

        _lastWakeEvaluationUtc = now;
        _ = Task.Run(async () =>
        {
            try
            {
                var tempPath = await WriteWakeWindowSnapshotAsync(wakeWindowSnapshot, cancellationToken).ConfigureAwait(false);
                if (tempPath == null)
                    return;

                try
                {
                    var wakeResult = await DetectWakeWordFromPathAsync(
                        tempPath,
                        Array.Empty<string>(),
                        requestKeepCandidateWindow: false,
                        cancellationToken).ConfigureAwait(false);
                    if (wakeResult.Detected)
                        PublishWakeWordDetection(wakeResult);
                }
                finally
                {
                    TryDeleteSegmentFile(tempPath);
                }
            }
            catch
            {
                // Best-effort wake evaluation only.
            }
            finally
            {
                Interlocked.Exchange(ref _wakeEvaluationInFlight, 0);
            }
        }, cancellationToken);
    }

    private static async Task<string?> WriteWakeWindowSnapshotAsync(byte[] wakeWindowSnapshot, CancellationToken cancellationToken)
    {
        var wakeWindowBytes = Math.Max(1, WakeWindowMilliseconds * MicrophoneAudioProcessor.OutputWaveFormat.AverageBytesPerSecond / 1000);
        if (wakeWindowSnapshot.Length < wakeWindowBytes / 16)
            return null;

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "wake-window");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"wake-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.wav");
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, useAsync: true);
        using var writer = new WaveFileWriter(stream, MicrophoneAudioProcessor.OutputWaveFormat);
        writer.Write(wakeWindowSnapshot, 0, wakeWindowSnapshot.Length);
        writer.Flush();
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return path;
    }

    private byte[]? UpdateWakeWindowLocked(byte[] wakeFrame, DateTime now)
    {
        if (wakeFrame.Length <= 0)
            return null;

        if (_lastWakeWordDetectedUtc != DateTime.MinValue && now - _lastWakeWordDetectedUtc < WakeWordRepeatCooldown)
            return null;

        var chunk = new byte[wakeFrame.Length];
        Buffer.BlockCopy(wakeFrame, 0, chunk, 0, wakeFrame.Length);
        _wakeWindowChunks.Enqueue(chunk);
        _wakeWindowBytes += chunk.Length;

        var maxBytes = MicrophoneAudioProcessor.OutputWaveFormat.AverageBytesPerSecond * WakeWindowMilliseconds / 1000;
        while (_wakeWindowBytes > maxBytes && _wakeWindowChunks.Count > 0)
        {
            var removed = _wakeWindowChunks.Dequeue();
            _wakeWindowBytes -= removed.Length;
        }

        if (_wakeWindowBytes < maxBytes / 8)
            return null;

        var snapshot = new byte[_wakeWindowBytes];
        var offset = 0;
        foreach (var wakeChunk in _wakeWindowChunks)
        {
            Buffer.BlockCopy(wakeChunk, 0, snapshot, offset, wakeChunk.Length);
            offset += wakeChunk.Length;
        }

        return snapshot;
    }

    private void PublishWakeWordDetection(WakeWordDetectionResult result)
    {
        _lastWakeWordDetectedUtc = DateTime.UtcNow;
        ClearWakeWindow();
        WakeWordDetected?.Invoke(this, new WakeWordDetectedEventArgs(result));
    }

    private void RememberWakeWordDetection(WakeWordDetectionResult result)
    {
        LastWakeWordDetection = result;
        var verdict = result.Detected ? "detected" : "rejected";
        _wakeDiagnostics.Add(
            $"Wake {verdict}: engine={result.Engine}; score={result.Score:0.000}; threshold={result.Threshold:0.000}; warnings={string.Join("|", result.AudioQualityWarnings)}");
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
        _currentSegmentWriter = new WaveFileWriter(_currentSegmentStream, MicrophoneAudioProcessor.OutputWaveFormat);
        _currentSegmentStartedUtc = nowUtc;
        _lastSpeechUtc = nowUtc;
        _currentSegmentWarnings = [];
    }

    private CapturedSegment? FinalizeCurrentSegment(bool commit)
    {
        if (_currentSegmentWriter == null || _currentSegmentStream == null)
            return null;

        var path = _currentSegmentStream.Name;
        var startedUtc = _currentSegmentStartedUtc;
        var duration = DateTime.UtcNow - startedUtc;
        var warnings = _currentSegmentWarnings?.ToArray() ?? Array.Empty<string>();

        try
        {
            _currentSegmentWriter.Dispose();
            _currentSegmentStream.Dispose();
        }
        finally
        {
            _currentSegmentWriter = null;
            _currentSegmentStream = null;
            _currentSegmentWarnings = null;
        }

        if (!commit || duration.TotalMilliseconds < MinimumSegmentMilliseconds)
        {
            TryDeleteSegmentFile(path);
            return null;
        }

        try
        {
            _microphoneProcessor.NormalizeWaveFileInPlace(path);
        }
        catch (Exception ex)
        {
            _wakeDiagnostics.Add($"Audio normalization failed for {Path.GetFileName(path)}: {ex.Message}");
        }

        return new CapturedSegment(path, startedUtc, duration, warnings);
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
        _wakeWordDetector = null;
        _activeCaptureDeviceName = null;
        _lastAudioPacketUtc = null;
        _isSpeechActive = false;
        _lastSpeechActivityUtc = null;

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

        ClearWakeWindow();

        IsListening = false;
        CurrentModeDescription = "Offline recognition is stopped.";
        LastStartupWarning = _startupWarning;
        SpeechActivityChanged?.Invoke(this, EventArgs.Empty);
    }

    public static double ResolveWakeThreshold(double? threshold, string? wakeSensitivity = null)
    {
        var sensitivityThreshold = wakeSensitivity?.Trim().ToLowerInvariant() switch
        {
            "more responsive" => 0.01,
            "balanced" => 0.02,
            "fewer false wakes" => 0.04,
            _ => DefaultWakeDetectionThreshold
        };

        var value = threshold.GetValueOrDefault(sensitivityThreshold);
        if (value <= 0
            || Math.Abs(value - 0.55) < 0.0001
            || Math.Abs(value - 0.50) < 0.0001
            || Math.Abs(value - 0.35) < 0.0001
            || Math.Abs(value - 0.42) < 0.0001)
            value = sensitivityThreshold;

        return Math.Clamp(value, 0.01, 0.95);
    }

    public static double? ComputeCalibratedWakeThreshold(double score)
    {
        if (double.IsNaN(score) || double.IsInfinity(score) || score < 0.05)
            return null;

        return Math.Clamp(score * 0.30, 0.01, 0.06);
    }

    public static void ApplyWakeCalibration(UserSettings settings, double score, int sampleCount, string? sourceSampleName = null)
    {
        var calibratedThreshold = ComputeCalibratedWakeThreshold(score);
        if (!calibratedThreshold.HasValue)
            throw new ArgumentException("Wake calibration score is too small to trust.", nameof(score));

        settings.VoiceWakeThreshold = calibratedThreshold.Value;
        settings.VoiceWakeSensitivity = "More responsive";
        settings.VoiceWakeCalibrationVersion = "streaming-frame-v1";
        settings.VoiceWakeCalibrationSampleCount = Math.Max(1, sampleCount);
        settings.VoiceWakeCalibratedUtc = DateTime.UtcNow;
        settings.VoiceWakeCalibrationSource = string.IsNullOrWhiteSpace(sourceSampleName) ? null : sourceSampleName;
    }

    private static IReadOnlyList<string> BuildWakePhrases(string wakeWord)
    {
        var phrases = new[]
        {
            wakeWord,
            "Callsign",
            "call sign",
            "call-sign",
            "callsign wake",
            "call sign wake"
        };

        return phrases
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Select(phrase => phrase.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IWaveIn CreateMicrophoneCapture(out string? deviceName)
    {
        deviceName = null;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = TryGetDefaultCaptureDevice(enumerator, Role.Communications)
                ?? TryGetDefaultCaptureDevice(enumerator, Role.Console)
                ?? TryGetDefaultCaptureDevice(enumerator, Role.Multimedia);

            if (device != null)
            {
                deviceName = device.FriendlyName;
                return new WasapiCapture(device);
            }
        }
        catch
        {
            // Fall back to WaveIn below so Callsign can still run on older machines.
        }

        deviceName = "Default microphone capture";
        return new WaveInEvent
        {
            WaveFormat = MicrophoneAudioProcessor.OutputWaveFormat,
            BufferMilliseconds = 50,
            NumberOfBuffers = 4
        };
    }

    private static MMDevice? TryGetDefaultCaptureDevice(MMDeviceEnumerator enumerator, Role role)
    {
        try
        {
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, role);
            return device.State == DeviceState.Active ? device : null;
        }
        catch
        {
            return null;
        }
    }

    private void AppendStartupWarning(string message)
    {
        _startupWarning = string.IsNullOrWhiteSpace(_startupWarning)
            ? message
            : $"{_startupWarning} {message}";
        LastStartupWarning = _startupWarning;
    }

    private void UpdateCurrentModeDescription()
    {
        var wakeEngine = CurrentWakeWordEngine;
        var transcription = _transcriber?.ModeDescription ?? "transcription initializing";
        CurrentModeDescription = $"{wakeEngine} wake detection + {transcription}";
    }

    private bool UpdateSpeechActivityStateLocked(DateTime utcNow)
    {
        var speechActive = _currentSegmentWriter != null;
        if (_isSpeechActive == speechActive)
            return false;

        _isSpeechActive = speechActive;
        _lastSpeechActivityUtc = utcNow;
        return true;
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

    private void ClearWakeWindow()
    {
        lock (_gate)
        {
            _wakeWindowChunks.Clear();
            _wakeWindowBytes = 0;
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

    private sealed record CapturedSegment(
        string Path,
        DateTime StartedUtc,
        TimeSpan Duration,
        IReadOnlyList<string> AudioQualityWarnings);
}

public sealed class VoiceRecognitionSettings
{
    public string Mode { get; set; } = "Local";
    public string? InputDeviceId { get; set; }
    public string? ModelPath { get; set; }
    public string LanguageCode { get; set; } = "en-US";
    public double WakeThreshold { get; set; } = 0;
    public string WakeSensitivity { get; set; } = "More responsive";
    public double CommandConfidenceThreshold { get; set; } = 0.65;
    public bool CloudOptIn { get; set; }
    public bool WakeDiagnosticsEnabled { get; set; }
    public bool UseVoiceActivityDetection { get; set; } = true;
    public bool UseNoiseSuppression { get; set; }
}

public sealed class WakeWordDetectionResult
{
    public required bool Detected { get; init; }
    public required double Score { get; init; }
    public required double Threshold { get; init; }
    public required string Engine { get; init; }
    public IReadOnlyList<string> AudioQualityWarnings { get; init; } = Array.Empty<string>();
    public required DateTime TimestampUtc { get; init; }
    public string? CandidateWindowPath { get; init; }
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

internal sealed record WakeWordDetectionRequest(
    string WavPath,
    string WakeWord,
    string LanguageCode,
    double Threshold,
    IReadOnlyList<string> WakePhrases,
    IReadOnlyList<string> AudioQualityWarnings,
    bool KeepCandidateWindow);

internal interface IWakeWordDetector
{
    string EngineName { get; }
    Task<WakeWordDetectionResult> DetectAsync(WakeWordDetectionRequest request, CancellationToken cancellationToken);
}

internal sealed class OpenWakeWordPythonWakeWordDetector : IWakeWordDetector
{
    private readonly string _pythonCommand;
    private readonly string[] _pythonPrefixArgs;
    private readonly string _modelPath;
    private readonly string _scriptPath;

    private OpenWakeWordPythonWakeWordDetector(string pythonCommand, string[] pythonPrefixArgs, string modelPath, string scriptPath)
    {
        _pythonCommand = pythonCommand;
        _pythonPrefixArgs = pythonPrefixArgs;
        _modelPath = modelPath;
        _scriptPath = scriptPath;
    }

    public string EngineName => "openWakeWord";

    public static bool TryCreate(string modelPath, string bundledPythonPath, out IWakeWordDetector detector, out string warning)
    {
        detector = new OpenWakeWordUnavailableWakeWordDetector("openWakeWord wake detection is not initialized.");
        if (!File.Exists(modelPath))
        {
            warning = "openWakeWord wake detection is required, but the installed Callsign wake model is missing. Use Repair Wakeword to restore it.";
            return false;
        }

        if (!TryFindBundledPythonWithOpenWakeWord(bundledPythonPath, out var pythonCommand, out var prefixArgs, out warning))
            return false;

        var scriptPath = EnsureDetectorScript();
        detector = new OpenWakeWordPythonWakeWordDetector(pythonCommand, prefixArgs, modelPath, scriptPath);
        warning = string.Empty;
        return true;
    }

    public async Task<WakeWordDetectionResult> DetectAsync(WakeWordDetectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (var arg in _pythonPrefixArgs)
                startInfo.ArgumentList.Add(arg);
            startInfo.ArgumentList.Add(_scriptPath);
            startInfo.ArgumentList.Add(request.WavPath);
            startInfo.ArgumentList.Add(_modelPath);
            startInfo.ArgumentList.Add(request.Threshold.ToString(System.Globalization.CultureInfo.InvariantCulture));

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Python process could not be started for openWakeWord.");

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"openWakeWord exited with code {process.ExitCode}." : error.Trim());

            var prediction = JsonSerializer.Deserialize<OpenWakeWordPrediction>(output);
            var score = prediction?.Score ?? 0;
            var warnings = request.AudioQualityWarnings;
            return new WakeWordDetectionResult
            {
                Detected = score >= request.Threshold,
                Score = score,
                Threshold = request.Threshold,
                Engine = EngineName,
                AudioQualityWarnings = warnings,
                TimestampUtc = DateTime.UtcNow,
                CandidateWindowPath = request.KeepCandidateWindow ? request.WavPath : null
            };
        }
        catch (Exception ex)
        {
            var warnings = request.AudioQualityWarnings
                .Concat(new[] { $"openWakeWord detection failed: {ex.Message}" })
                .ToArray();

            return new WakeWordDetectionResult
            {
                Detected = false,
                Score = 0,
                Threshold = request.Threshold,
                Engine = EngineName,
                AudioQualityWarnings = warnings,
                TimestampUtc = DateTime.UtcNow,
                CandidateWindowPath = request.KeepCandidateWindow ? request.WavPath : null
            };
        }
    }

    private static bool TryFindBundledPythonWithOpenWakeWord(string bundledPythonPath, out string pythonCommand, out string[] prefixArgs, out string warning)
    {
        if (File.Exists(bundledPythonPath))
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = bundledPythonPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add("import pathlib, openwakeword, onnxruntime, numpy; root=pathlib.Path(openwakeword.__file__).parent/'resources'/'models'; missing=[name for name in ['melspectrogram.onnx','embedding_model.onnx'] if not (root/name).exists()]; raise SystemExit(1 if missing else 0)");

                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Bundled openWakeWord Python runtime could not be started.");

                if (process.WaitForExit(5000) && process.ExitCode == 0)
                {
                    pythonCommand = bundledPythonPath;
                    prefixArgs = [];
                    warning = string.Empty;
                    return true;
                }
            }
            catch
            {
                // Use the failure path below to surface a repair hint.
            }
        }

        pythonCommand = string.Empty;
        prefixArgs = [];
        warning = "openWakeWord wake detection is required, but the installed Python runtime or openWakeWord feature models were not ready. Use Repair Wakeword to restore the local environment.";
        return false;
    }

    private static string EnsureDetectorScript()
    {
        var scriptDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "openwakeword");
        Directory.CreateDirectory(scriptDir);
        var scriptPath = Path.Combine(scriptDir, "openwakeword_detect.py");
        File.WriteAllText(scriptPath, OpenWakeWordDetectorScript);
        return scriptPath;
    }

    private sealed record OpenWakeWordPrediction(double Score, string? Label);

    private const string OpenWakeWordDetectorScript = """
import json
import wave
import sys

import numpy as np
from openwakeword.model import Model

wav_path = sys.argv[1]
model_path = sys.argv[2]
threshold = float(sys.argv[3])

model = Model(
    wakeword_models=[model_path],
    inference_framework="onnx",
    vad_threshold=0.0,
    enable_speex_noise_suppression=False,
)
frame_milliseconds = 80
hop_milliseconds = 20
frame_size = int(16000 * frame_milliseconds / 1000)
hop_size = int(16000 * hop_milliseconds / 1000)
buffer = bytearray()
best_label = None
best_score = 0.0


def maybe_update_prediction(predictions):
    global best_label, best_score

    if isinstance(predictions, dict):
        for label, score in predictions.items():
            if isinstance(score, dict):
                maybe_update_prediction(score)
                continue

            try:
                score_value = float(score)
            except Exception:
                continue

            if score_value > best_score:
                best_score = score_value
                best_label = label
        return

    if isinstance(predictions, (list, tuple)):
        for nested in predictions:
            maybe_update_prediction(nested)
        return

    if hasattr(predictions, "tolist"):
        maybe_update_prediction(predictions.tolist())
        return

    try:
        score_value = float(predictions)
    except Exception:
        return

    if score_value > best_score:
        best_score = score_value


with wave.open(wav_path, "rb") as wav_file:
    total_frames = wav_file.getnframes()
    remaining_frames = total_frames
    sample_width = wav_file.getsampwidth()
    channel_count = wav_file.getnchannels()
    target_bytes = frame_size * sample_width * channel_count
    hop_bytes = hop_size * sample_width * channel_count

    while remaining_frames > 0:
        raw_frame = wav_file.readframes(min(hop_size, remaining_frames))
        remaining_frames -= min(hop_size, remaining_frames)

        if not raw_frame:
            break

        buffer.extend(raw_frame)

        while len(buffer) >= target_bytes:
            raw_frame = bytes(buffer[:target_bytes])
            del buffer[:hop_bytes]

            frame = np.frombuffer(raw_frame, dtype=np.int16)
            predictions = model.predict(frame)
            maybe_update_prediction(predictions)

        if best_score >= threshold:
            break

    if buffer and best_score < threshold:
        if len(buffer) < target_bytes:
            buffer.extend(b"\0" * (target_bytes - len(buffer)))

        frame = np.frombuffer(bytes(buffer[:target_bytes]), dtype=np.int16)
        predictions = model.predict(frame)
        maybe_update_prediction(predictions)


print(json.dumps({"Score": best_score, "Label": best_label}))
""";
}

internal sealed class OpenWakeWordUnavailableWakeWordDetector : IWakeWordDetector
{
    private readonly string _reason;

    public OpenWakeWordUnavailableWakeWordDetector(string reason)
    {
        _reason = string.IsNullOrWhiteSpace(reason)
            ? "openWakeWord wake detection is unavailable."
            : reason;
    }

    public string EngineName => "openWakeWord unavailable";

    public Task<WakeWordDetectionResult> DetectAsync(WakeWordDetectionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = request.AudioQualityWarnings
            .Concat(new[] { _reason })
            .ToArray();

        return Task.FromResult(new WakeWordDetectionResult
        {
            Detected = false,
            Score = 0,
            Threshold = request.Threshold,
            Engine = EngineName,
            AudioQualityWarnings = warnings,
            TimestampUtc = DateTime.UtcNow,
            CandidateWindowPath = request.KeepCandidateWindow ? request.WavPath : null
        });
    }
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
        try
        {
            return await TranscribeOnceAsync(wavPath, cancellationToken, sw).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsModelLoadFailure(ex))
        {
            _appendWarning("Local Whisper model could not be used cleanly. Re-downloading a fresh copy and retrying once.");
            ResetFactoryAndModel();
            await EnsureModelAsync(cancellationToken).ConfigureAwait(false);
            return await TranscribeOnceAsync(wavPath, cancellationToken, sw).ConfigureAwait(false);
        }
    }

    private async Task<VoiceRecognitionResult> TranscribeOnceAsync(
        string wavPath,
        CancellationToken cancellationToken,
        Stopwatch sw)
    {
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
                    _appendWarning($"Local Whisper model is missing at '{_modelPath}'. Downloading the base model now.");
                    await DownloadModelAsync(cancellationToken).ConfigureAwait(false);
                }

                _factory = WhisperFactory.FromPath(_modelPath);
                return;
            }
            catch (Exception) when (!repairedModel)
            {
                repairedModel = true;
                _appendWarning($"Local Whisper model could not be loaded cleanly from '{_modelPath}'. Re-downloading a fresh copy.");
                TryDeleteFile(_modelPath);
                TryDeleteFile(_modelPath + ".download");
            }
        }
    }

    private void ResetFactoryAndModel()
    {
        _factory?.Dispose();
        _factory = null;
        TryDeleteFile(_modelPath);
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

    private static bool IsModelLoadFailure(Exception ex)
    {
        var message = ex.Message ?? string.Empty;
        return message.Contains("Failed to load the whisper model", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Failed to load native whisper library", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot load the library on this platform", StringComparison.OrdinalIgnoreCase);
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

public sealed class VoiceTranscriptEventArgs(
    string text,
    float confidence,
    string? capturedAudioPath = null,
    IReadOnlyList<string>? audioQualityWarnings = null) : EventArgs
{
    public string Text { get; } = text;
    public float Confidence { get; } = confidence;
    public string? CapturedAudioPath { get; } = capturedAudioPath;
    public IReadOnlyList<string> AudioQualityWarnings { get; } = audioQualityWarnings ?? Array.Empty<string>();
}

public sealed class WakeWordDetectedEventArgs(WakeWordDetectionResult result) : EventArgs
{
    public WakeWordDetectionResult Result { get; } = result;
}

public sealed class VoiceRecognitionErrorEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
