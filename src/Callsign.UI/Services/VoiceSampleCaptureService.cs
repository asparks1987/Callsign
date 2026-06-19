using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Callsign.UI.Services;

public sealed class VoiceSampleCaptureService : IDisposable
{
    private IWaveIn? _waveIn;
    private WaveFileWriter? _writer;
    private MicrophoneAudioProcessor? _processor;
    private WaveFormat _captureFormat = MicrophoneAudioProcessor.OutputWaveFormat;

    public bool IsRecording => _waveIn != null;

    public string? CurrentSamplePath { get; private set; }
    public MicrophoneAudioSnapshot? LastTelemetry { get; private set; }

    public void Start(string samplePath, MicrophoneAudioSettings? audioSettings = null)
    {
        if (IsRecording)
            throw new InvalidOperationException("A voice sample is already being recorded.");

        var directory = Path.GetDirectoryName(samplePath)
            ?? throw new InvalidOperationException("Sample path could not be resolved.");
        Directory.CreateDirectory(directory);

        CurrentSamplePath = samplePath;
        _processor = new MicrophoneAudioProcessor(audioSettings ?? new MicrophoneAudioSettings());
        LastTelemetry = _processor.LastSnapshot;
        _writer = null;
        _waveIn = CreateMicrophoneCapture();
        _captureFormat = _waveIn.WaveFormat;
        _writer = new WaveFileWriter(samplePath, MicrophoneAudioProcessor.OutputWaveFormat);
        _waveIn.DataAvailable += WaveInDataAvailable;
        _waveIn.RecordingStopped += WaveInRecordingStopped;
        _waveIn.StartRecording();
    }

    public void Stop()
    {
        if (!IsRecording)
            return;

        try
        {
            _waveIn?.StopRecording();
        }
        finally
        {
            DisposeCurrentRecording();
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void WaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_writer == null || _processor == null)
            return;

        var processed = _processor.ProcessBuffer(e.Buffer, e.BytesRecorded, _captureFormat, DateTime.UtcNow);
        LastTelemetry = processed.Snapshot;
        _writer.Write(processed.ProcessedBuffer, 0, processed.BytesRecorded);
        _writer.Flush();
    }

    private void WaveInRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // Stop/cleanup happens in the owning form; swallow the callback error here so
        // the UI can decide how to surface recording problems.
        _ = e.Exception;
    }

    private void DisposeCurrentRecording()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= WaveInDataAvailable;
            _waveIn.RecordingStopped -= WaveInRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _writer?.Dispose();
        _writer = null;

        if (!string.IsNullOrWhiteSpace(CurrentSamplePath) && File.Exists(CurrentSamplePath) && _processor != null)
        {
            try
            {
                LastTelemetry = _processor.NormalizeWaveFileInPlace(CurrentSamplePath);
            }
            catch
            {
                // Best-effort normalization only.
            }
        }

        CurrentSamplePath = null;
    }

    private static IWaveIn CreateMicrophoneCapture()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = TryGetDefaultCaptureDevice(enumerator, Role.Communications)
                ?? TryGetDefaultCaptureDevice(enumerator, Role.Console)
                ?? TryGetDefaultCaptureDevice(enumerator, Role.Multimedia);

            if (device != null)
                return new WasapiCapture(device);
        }
        catch
        {
            // Fall back to the older WaveIn path below.
        }

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
}
