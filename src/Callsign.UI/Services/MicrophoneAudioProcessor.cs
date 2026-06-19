using System.Buffers.Binary;
using System.Globalization;
using System.Linq;
using Callsign.UI.Models;
using NAudio.Wave;

namespace Callsign.UI.Services;

public sealed record MicrophoneAudioSettings(
    double VoiceInputGainDb = 12,
    bool VoiceAutoGainEnabled = true,
    double VoiceTargetRms = 0.08,
    bool VoiceAdaptiveSpeechThresholdEnabled = true)
{
    public static MicrophoneAudioSettings From(UserSettings settings) =>
        new(
            settings.VoiceInputGainDb,
            settings.VoiceAutoGainEnabled,
            settings.VoiceTargetRms,
            settings.VoiceAdaptiveSpeechThresholdEnabled);
}

public sealed record MicrophoneAudioSnapshot(
    double RawRms,
    double RawPeak,
    double ProcessedRms,
    double ProcessedPeak,
    double AppliedGainDb,
    double NoiseFloorRms,
    double ClippingRatio,
    double SpeechThresholdRms,
    double RecommendedGainDb,
    string LevelState,
    IReadOnlyList<string> Warnings,
    DateTime Utc)
{
    public static MicrophoneAudioSnapshot Silent(DateTime utc) =>
        new(0, 0, 0, 0, 0, 0.004, 0, 0.018, 12, "Too quiet", Array.Empty<string>(), utc);
}

public sealed record MicrophoneAudioProcessingResult(
    byte[] ProcessedBuffer,
    int BytesRecorded,
    MicrophoneAudioSnapshot Snapshot,
    bool SpeechDetected);

public sealed class MicrophoneAudioProcessor
{
    public static readonly WaveFormat OutputWaveFormat = new(16_000, 16, 1);

    private const double MaxGainDb = 36.0;
    private const double MaxBoostDb = 30.0;
    private const double MaxPeak = 0.98;
    private const double MinSpeechThreshold = 0.003;
    private const double MinNoiseFloor = 0.0005;

    private readonly object _gate = new();
    private readonly MicrophoneAudioSettings _settings;
    private double _noiseFloorRms;

    public MicrophoneAudioProcessor(MicrophoneAudioSettings settings)
    {
        _settings = settings;
        _noiseFloorRms = Math.Max(MinNoiseFloor, settings.VoiceTargetRms * 0.05);
        LastSnapshot = MicrophoneAudioSnapshot.Silent(DateTime.UtcNow);
    }

    public MicrophoneAudioSnapshot LastSnapshot { get; private set; }

    public MicrophoneAudioProcessingResult ProcessBuffer(ReadOnlySpan<byte> pcm16, int bytesRecorded, DateTime utcNow)
    {
        return ProcessSamples(DecodePcm16(pcm16, bytesRecorded), utcNow);
    }

    public MicrophoneAudioProcessingResult ProcessBuffer(ReadOnlySpan<byte> buffer, int bytesRecorded, WaveFormat inputFormat, DateTime utcNow)
    {
        var monoSamples = DecodeToMonoSamples(buffer, bytesRecorded, inputFormat);
        if (inputFormat.SampleRate != OutputWaveFormat.SampleRate)
            monoSamples = ResampleLinear(monoSamples, inputFormat.SampleRate, OutputWaveFormat.SampleRate);

        return ProcessSamples(monoSamples, utcNow);
    }

    public static byte[] ConvertToWakePcm16(ReadOnlySpan<byte> buffer, int bytesRecorded, WaveFormat inputFormat)
    {
        var monoSamples = DecodeToMonoSamples(buffer, bytesRecorded, inputFormat);
        if (inputFormat.SampleRate != OutputWaveFormat.SampleRate)
            monoSamples = ResampleLinear(monoSamples, inputFormat.SampleRate, OutputWaveFormat.SampleRate);

        return ToPcm16(monoSamples);
    }

    private MicrophoneAudioProcessingResult ProcessSamples(double[] rawSamples, DateTime utcNow)
    {
        lock (_gate)
        {
            if (rawSamples.Length <= 0)
            {
                LastSnapshot = MicrophoneAudioSnapshot.Silent(utcNow);
                return new MicrophoneAudioProcessingResult(Array.Empty<byte>(), 0, LastSnapshot, false);
            }

            var sampleCount = rawSamples.Length;
            var rawPeak = 0.0;
            for (var index = 0; index < sampleCount; index++)
                rawPeak = Math.Max(rawPeak, Math.Abs(rawSamples[index]));

            var rawRms = ComputeRms(rawSamples);
            var clippingRatio = rawSamples.Count(sample => Math.Abs(sample) >= 0.999) / (double)sampleCount;
            var effectiveGainDb = ComputeEffectiveGainDb(rawRms);
            var gainScalar = Math.Pow(10, effectiveGainDb / 20.0);
            var processedSamples = new double[sampleCount];
            var processedPeak = 0.0;
            for (var index = 0; index < sampleCount; index++)
            {
                var value = rawSamples[index] * gainScalar;
                value = Math.Clamp(value, -MaxPeak, MaxPeak);
                processedSamples[index] = value;
                processedPeak = Math.Max(processedPeak, Math.Abs(value));
            }

            var processedRms = ComputeRms(processedSamples);
            var speechThreshold = ComputeSpeechThreshold(gainScalar);
            var speechDetected = processedRms >= speechThreshold;
            if (!speechDetected)
                UpdateNoiseFloor(rawRms);
            else
                _noiseFloorRms = Math.Max(MinNoiseFloor, (_noiseFloorRms * 0.985) + (rawRms * 0.015));

            var warnings = BuildWarnings(rawRms, processedRms, processedPeak, clippingRatio, effectiveGainDb, speechThreshold);
            var snapshot = new MicrophoneAudioSnapshot(
                RawRms: rawRms,
                RawPeak: rawPeak,
                ProcessedRms: processedRms,
                ProcessedPeak: processedPeak,
                AppliedGainDb: effectiveGainDb,
                NoiseFloorRms: _noiseFloorRms,
                ClippingRatio: clippingRatio,
                SpeechThresholdRms: speechThreshold,
                RecommendedGainDb: ComputeRecommendedGainDb(rawRms),
                LevelState: GetLevelState(rawRms, processedRms, clippingRatio),
                Warnings: warnings,
                Utc: utcNow);

            LastSnapshot = snapshot;
            var processedBuffer = ToPcm16(processedSamples);
            return new MicrophoneAudioProcessingResult(processedBuffer, processedBuffer.Length, snapshot, speechDetected);
        }
    }

    public MicrophoneAudioSnapshot NormalizeWaveFileInPlace(string wavPath)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
                throw new FileNotFoundException("The WAV file to normalize was not found.", wavPath);

            using var reader = new WaveFileReader(wavPath);
            if (reader.WaveFormat.Channels < 1)
                throw new InvalidOperationException("Only WAV files with at least one audio channel can be normalized.");

            var input = new byte[checked((int)reader.Length)];
            var read = 0;
            while (read < input.Length)
            {
                var bytesRead = reader.Read(input, read, input.Length - read);
                if (bytesRead <= 0)
                    break;
                read += bytesRead;
            }

            var result = ProcessBuffer(input, read, reader.WaveFormat, DateTime.UtcNow);
            var tempPath = $"{wavPath}.{Environment.ProcessId}.normalized";
            try
            {
                using (var writer = new WaveFileWriter(tempPath, OutputWaveFormat))
                {
                    writer.Write(result.ProcessedBuffer, 0, result.BytesRecorded);
                }

                ReplaceFile(tempPath, wavPath);
                return result.Snapshot;
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }
    }

    private double ComputeEffectiveGainDb(double rawRms)
    {
        var gainDb = _settings.VoiceInputGainDb;
        if (_settings.VoiceAutoGainEnabled && rawRms > 0)
        {
            var target = Math.Max(MinNoiseFloor, _settings.VoiceTargetRms);
            var boostDb = 20.0 * Math.Log10(target / Math.Max(rawRms, MinNoiseFloor));
            boostDb = Math.Clamp(boostDb, 0, MaxBoostDb);
            gainDb += boostDb;
        }

        return Math.Clamp(gainDb, 0, MaxGainDb);
    }

    private double ComputeSpeechThreshold(double gainScalar)
    {
        if (!_settings.VoiceAdaptiveSpeechThresholdEnabled)
            return 0.010;

        var threshold = Math.Max(MinSpeechThreshold, _noiseFloorRms * gainScalar * 1.25);
        return Math.Clamp(threshold, MinSpeechThreshold, 0.05);
    }

    private void UpdateNoiseFloor(double rawRms)
    {
        var sample = Math.Max(MinNoiseFloor, rawRms);
        _noiseFloorRms = (_noiseFloorRms * 0.92) + (sample * 0.08);
        _noiseFloorRms = Math.Max(MinNoiseFloor, _noiseFloorRms);
    }

    private double ComputeRecommendedGainDb(double rawRms)
    {
        if (rawRms <= 0)
            return _settings.VoiceInputGainDb;

        var target = Math.Max(MinNoiseFloor, _settings.VoiceTargetRms);
        var recommended = 20.0 * Math.Log10(target / Math.Max(rawRms, MinNoiseFloor));
        return Math.Clamp(recommended, 0, MaxGainDb);
    }

    private static string GetLevelState(double rawRms, double processedRms, double clippingRatio)
    {
        if (clippingRatio > 0.01 || processedRms >= 0.97)
            return "Clipping";

        if (rawRms < 0.0005 || processedRms < MinSpeechThreshold)
            return "Too quiet";

        return "Good";
    }

    private static IReadOnlyList<string> BuildWarnings(
        double rawRms,
        double processedRms,
        double processedPeak,
        double clippingRatio,
        double appliedGainDb,
        double speechThreshold)
    {
        var warnings = new List<string>();
        if (clippingRatio > 0.01)
            warnings.Add("Input clipping detected; limiter protected the audio.");
        if (processedPeak >= 0.97)
            warnings.Add("Boosted microphone audio was capped to prevent clipping.");
        if (rawRms < 0.0005)
            warnings.Add("Microphone input is very quiet.");
        if (processedRms < speechThreshold)
            warnings.Add("Speech level is still below the adaptive threshold.");
        if (appliedGainDb > 0)
            warnings.Add($"Callsign boosted this microphone by {appliedGainDb:0.0} dB.");
        return warnings;
    }

    private static double ComputeRms(ReadOnlySpan<double> samples)
    {
        if (samples.Length == 0)
            return 0;

        double sumSquares = 0;
        for (var index = 0; index < samples.Length; index++)
            sumSquares += samples[index] * samples[index];

        return Math.Sqrt(sumSquares / samples.Length);
    }

    private static double[] DecodePcm16(ReadOnlySpan<byte> pcm16, int bytesRecorded)
    {
        if (bytesRecorded <= 0 || pcm16.Length < 2)
            return Array.Empty<double>();

        var sampleCount = bytesRecorded / 2;
        var samples = new double[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(pcm16.Slice(index * 2, 2));
            samples[index] = sample / 32768.0;
        }

        return samples;
    }

    private static double[] DecodeToMonoSamples(ReadOnlySpan<byte> buffer, int bytesRecorded, WaveFormat format)
    {
        if (bytesRecorded <= 0 || buffer.Length == 0)
            return Array.Empty<double>();

        if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16 && format.Channels == 1)
            return DecodePcm16(buffer, bytesRecorded);

        var channels = Math.Max(1, format.Channels);
        var bytesPerSample = Math.Max(1, format.BitsPerSample / 8);
        var bytesPerFrame = Math.Max(1, bytesPerSample * channels);
        var frameCount = bytesRecorded / bytesPerFrame;
        if (frameCount <= 0)
            return Array.Empty<double>();

        var samples = new double[frameCount];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var sum = 0.0;
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = (frame * bytesPerFrame) + (channel * bytesPerSample);
                sum += DecodeSample(buffer.Slice(offset, bytesPerSample), format);
            }

            samples[frame] = Math.Clamp(sum / channels, -1.0, 1.0);
        }

        return samples;
    }

    private static double DecodeSample(ReadOnlySpan<byte> sampleBytes, WaveFormat format)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32 && sampleBytes.Length >= 4)
            return Math.Clamp(BitConverter.ToSingle(sampleBytes), -1.0, 1.0);

        if (format.Encoding == WaveFormatEncoding.Pcm)
        {
            if (format.BitsPerSample == 16 && sampleBytes.Length >= 2)
                return BinaryPrimitives.ReadInt16LittleEndian(sampleBytes[..2]) / 32768.0;

            if (format.BitsPerSample == 24 && sampleBytes.Length >= 3)
            {
                var value = sampleBytes[0] | (sampleBytes[1] << 8) | (sampleBytes[2] << 16);
                if ((value & 0x800000) != 0)
                    value |= unchecked((int)0xFF000000);
                return value / 8388608.0;
            }

            if (format.BitsPerSample == 32 && sampleBytes.Length >= 4)
                return BinaryPrimitives.ReadInt32LittleEndian(sampleBytes[..4]) / 2147483648.0;
        }

        return 0;
    }

    private static double[] ResampleLinear(double[] samples, int inputRate, int outputRate)
    {
        if (samples.Length == 0 || inputRate <= 0 || outputRate <= 0 || inputRate == outputRate)
            return samples;

        var outputLength = Math.Max(1, (int)Math.Round(samples.Length * (outputRate / (double)inputRate)));
        var output = new double[outputLength];
        var scale = inputRate / (double)outputRate;
        for (var index = 0; index < output.Length; index++)
        {
            var source = index * scale;
            var left = (int)Math.Floor(source);
            var right = Math.Min(samples.Length - 1, left + 1);
            var fraction = source - left;
            output[index] = samples[Math.Min(left, samples.Length - 1)] * (1 - fraction) + samples[right] * fraction;
        }

        return output;
    }

    private static byte[] ToPcm16(ReadOnlySpan<double> samples)
    {
        var buffer = new byte[samples.Length * 2];
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = (short)Math.Clamp(Math.Round(samples[index] * short.MaxValue), short.MinValue, short.MaxValue);
            var bytes = BitConverter.GetBytes(sample);
            buffer[index * 2] = bytes[0];
            buffer[index * 2 + 1] = bytes[1];
        }

        return buffer;
    }

    private static void ReplaceFile(string tempPath, string targetPath)
    {
        try
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);

            File.Move(tempPath, targetPath);
        }
        catch (IOException ex)
        {
            throw new IOException($"Unable to update normalized audio file '{targetPath}'.", ex);
        }
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
