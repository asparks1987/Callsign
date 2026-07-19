using System.Globalization;
using NAudio.Wave;

namespace Callsign.UI.Services;

public sealed record VoiceSampleQuality(
    bool Accepted,
    string State,
    string Message,
    double Peak,
    double Rms,
    double DurationSeconds,
    double ClippingRatio,
    double ZeroCrossingRate);

public static class VoiceSampleQualityAnalyzer
{
    public static VoiceSampleQuality Analyze(string samplePath)
    {
        if (string.IsNullOrWhiteSpace(samplePath) || !File.Exists(samplePath))
            return Reject("Too quiet", "No sample file was captured.");

        try
        {
            using var reader = new AudioFileReader(samplePath);
            var channels = Math.Max(1, reader.WaveFormat.Channels);
            var buffer = new float[reader.WaveFormat.SampleRate * channels];
            var mono = new List<float>();
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var index = 0; index < read; index += channels)
                {
                    double sum = 0;
                    for (var channel = 0; channel < channels && index + channel < read; channel++)
                        sum += buffer[index + channel];
                    mono.Add((float)(sum / channels));
                }
            }

            var duration = reader.TotalTime.TotalSeconds;
            if (duration < 0.65)
                return Reject("Too quiet", "Sample is too short. Hold record a little longer.", durationSeconds: duration);
            if (mono.Count == 0)
                return Reject("Too quiet", "Sample is too quiet or silent.", durationSeconds: duration);

            var peak = 0.0;
            var sumSquares = 0.0;
            var clipped = 0;
            for (var index = 0; index < mono.Count; index++)
            {
                var absolute = Math.Abs(mono[index]);
                peak = Math.Max(peak, absolute);
                sumSquares += absolute * absolute;
                if (absolute >= 0.985)
                    clipped++;
            }

            var rms = Math.Sqrt(sumSquares / mono.Count);
            var clippingRatio = clipped / (double)mono.Count;
            var zeroCrossingRate = ComputeZeroCrossingRate(mono);
            if (peak < 0.015 || rms < 0.004)
            {
                return Reject(
                    "Too quiet",
                    "Sample is too quiet or silent.",
                    peak,
                    rms,
                    duration,
                    clippingRatio,
                    zeroCrossingRate);
            }

            if (peak > 0.98 || clippingRatio > 0.005)
            {
                return Reject(
                    "Clipping",
                    "Sample is clipping. Lower microphone gain and try again.",
                    peak,
                    rms,
                    duration,
                    clippingRatio,
                    zeroCrossingRate);
            }

            if (zeroCrossingRate > 0.32 && rms > 0.020)
            {
                return Reject(
                    "Noisy",
                    "Sample has excessive broadband noise. Move closer to the microphone or reduce background noise.",
                    peak,
                    rms,
                    duration,
                    clippingRatio,
                    zeroCrossingRate);
            }

            return new VoiceSampleQuality(
                true,
                "Good",
                $"Clean sample: {duration:0.0}s, peak {peak:0.00}, RMS {rms.ToString("0.000", CultureInfo.CurrentCulture)}.",
                peak,
                rms,
                duration,
                clippingRatio,
                zeroCrossingRate);
        }
        catch (Exception ex)
        {
            return Reject("Too quiet", $"Sample could not be read: {ex.Message}");
        }
    }

    private static VoiceSampleQuality Reject(
        string state,
        string message,
        double peak = 0,
        double rms = 0,
        double durationSeconds = 0,
        double clippingRatio = 0,
        double zeroCrossingRate = 0) =>
        new(false, state, message, peak, rms, durationSeconds, clippingRatio, zeroCrossingRate);

    private static double ComputeZeroCrossingRate(IReadOnlyList<float> samples)
    {
        if (samples.Count < 2)
            return 0;

        var crossings = 0;
        for (var index = 1; index < samples.Count; index++)
        {
            if ((samples[index - 1] < 0 && samples[index] >= 0)
                || (samples[index - 1] >= 0 && samples[index] < 0))
            {
                crossings++;
            }
        }

        return crossings / (double)(samples.Count - 1);
    }
}
