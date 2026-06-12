using NAudio.Wave;

namespace Callsign.UI.Services;

public sealed class VoiceSampleCaptureService : IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;

    public bool IsRecording => _waveIn != null;

    public string? CurrentSamplePath { get; private set; }

    public void Start(string samplePath)
    {
        if (IsRecording)
            throw new InvalidOperationException("A voice sample is already being recorded.");

        var directory = Path.GetDirectoryName(samplePath)
            ?? throw new InvalidOperationException("Sample path could not be resolved.");
        Directory.CreateDirectory(directory);

        CurrentSamplePath = samplePath;
        _writer = null;
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16_000, 1),
            BufferMilliseconds = 100,
            NumberOfBuffers = 3
        };
        _writer = new WaveFileWriter(samplePath, _waveIn.WaveFormat);
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
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        _writer?.Flush();
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
    }
}
