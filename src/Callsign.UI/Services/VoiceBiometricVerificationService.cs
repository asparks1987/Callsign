using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Callsign.UI.Models;
using NAudio.Wave;

namespace Callsign.UI.Services;

public interface IVoiceBiometricVerifier
{
    VoiceBiometricVerificationResult Verify(
        ProfileStore profileStore,
        UserProfile profile,
        string? candidateSamplePath,
        double? threshold = null,
        TimeSpan? maxCandidateAge = null);
}

public sealed record VoiceBiometricVerificationResult(
    bool Accepted,
    double Score,
    double Threshold,
    string Engine,
    string? RejectReason,
    string? EnrollmentSamplePath,
    string? CandidateSamplePath,
    double? Distance = null,
    double? NearMatchThreshold = null,
    string? EnrollmentEmbeddingPath = null);

public sealed record VoiceBiometricEnrollmentResult(
    bool Accepted,
    string Engine,
    string? RejectReason,
    string? EnrollmentEmbeddingPath,
    int SamplesEnrolled,
    string Message);

public sealed record VoiceEnrollmentSampleMetadata(
    string Path,
    string FileName,
    long ByteLength,
    DateTime LastWriteUtc,
    double AgeSeconds,
    string Sha256,
    string QualityState,
    string QualityMessage,
    double Peak,
    double Rms,
    double DurationSeconds,
    double ClippingRatio,
    double ZeroCrossingRate);

public sealed record VoiceEnrollmentSampleProof(
    DateTime UpdatedUtc,
    bool Accepted,
    string? RejectReason,
    string Message,
    int RequiredSamples,
    int SampleCount,
    int DistinctHashCount,
    IReadOnlyList<VoiceEnrollmentSampleMetadata> Samples);

public sealed class VoiceBiometricVerificationService : IVoiceBiometricVerifier
{
    public const double DefaultThreshold = 0.72;
    public const double DefaultNearMatchThreshold = 0.86;
    private const string HeuristicEngineName = "local-open-source-naudio-voiceprint";
    private const string PyannoteEngineName = "pyannote/embedding";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public VoiceBiometricEnrollmentResult EnrollFreshSamples(ProfileStore profileStore, UserProfile profile, IEnumerable<string> samplePaths)
    {
        var sampleProof = BuildEnrollmentSampleProof(samplePaths, accepted: false, rejectReason: null, message: "Validating voice identity samples.");
        WriteEnrollmentSampleProof(profileStore, profile, sampleProof);
        if (sampleProof.SampleCount < 3)
        {
            sampleProof = sampleProof with
            {
                RejectReason = "pyannote_sample_set_too_small",
                Message = "Collect at least 3 fresh voice samples before enrolling."
            };
            WriteEnrollmentSampleProof(profileStore, profile, sampleProof);
            return new VoiceBiometricEnrollmentResult(
                false,
                PyannoteEngineName,
                "pyannote_sample_set_too_small",
                GetEnrollmentEmbeddingPath(profileStore, profile),
                sampleProof.SampleCount,
                "Collect at least 3 fresh voice samples before enrolling.");
        }

        if (sampleProof.DistinctHashCount < sampleProof.SampleCount)
        {
            sampleProof = sampleProof with
            {
                RejectReason = "pyannote_sample_set_not_distinct",
                Message = "Record 3 different fresh voice samples before enrolling."
            };
            WriteEnrollmentSampleProof(profileStore, profile, sampleProof);
            return new VoiceBiometricEnrollmentResult(
                false,
                PyannoteEngineName,
                "pyannote_sample_set_not_distinct",
                GetEnrollmentEmbeddingPath(profileStore, profile),
                sampleProof.SampleCount,
                "Record 3 different fresh voice samples before enrolling.");
        }

        foreach (var sample in sampleProof.Samples)
        {
            if (ValidateEnrollmentSample(sample.Path, out var validationError) != null)
            {
                sampleProof = sampleProof with
                {
                    RejectReason = "pyannote_sample_invalid",
                    Message = validationError!
                };
                WriteEnrollmentSampleProof(profileStore, profile, sampleProof);
                return new VoiceBiometricEnrollmentResult(
                    false,
                    PyannoteEngineName,
                    "pyannote_sample_invalid",
                    GetEnrollmentEmbeddingPath(profileStore, profile),
                    0,
                    validationError!);
            }
        }

        sampleProof = sampleProof with
        {
            Accepted = true,
            RejectReason = null,
            Message = $"Validated {sampleProof.SampleCount} fresh distinct voice sample(s)."
        };
        WriteEnrollmentSampleProof(profileStore, profile, sampleProof);

        var embeddings = new List<double[]>();
        string? modelId = null;
        foreach (var sample in sampleProof.Samples)
        {
            if (!TryEmbedWithPyannote(sample.Path, out var embedding, out var sampleModelId, out var error))
            {
                return new VoiceBiometricEnrollmentResult(
                    false,
                    PyannoteEngineName,
                    "pyannote_runtime_not_ready",
                    GetEnrollmentEmbeddingPath(profileStore, profile),
                    0,
                    error);
            }

            embeddings.Add(embedding);
            modelId ??= sampleModelId;
        }

        var averaged = AverageEmbeddings(null, embeddings);
        var updated = new VoiceIdentityProfile
        {
            Engine = PyannoteEngineName,
            ModelId = string.IsNullOrWhiteSpace(modelId) ? PyannoteEngineName : modelId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            SamplesEnrolled = embeddings.Count,
            Threshold = NormalizeThreshold(profile.Settings.VoiceBiometricThreshold),
            NearMatchThreshold = NormalizeNearMatchThreshold(profile.Settings.VoiceBiometricNearMatchThreshold, profile.Settings.VoiceBiometricThreshold),
            Embedding = averaged
        };

        SaveIdentity(profileStore, profile, updated);
        return new VoiceBiometricEnrollmentResult(
            true,
            PyannoteEngineName,
            null,
            GetEnrollmentEmbeddingPath(profileStore, profile),
            updated.SamplesEnrolled,
            $"pyannote voice identity enrolled with {updated.SamplesEnrolled} fresh sample(s).");
    }

    public VoiceBiometricEnrollmentResult EnrollLatestSample(ProfileStore profileStore, UserProfile profile, string samplePath)
    {
        return EnrollFreshSamples(profileStore, profile, new[] { samplePath });
    }

    public static bool IsSampleProofRejectReason(string? rejectReason) =>
        !string.IsNullOrWhiteSpace(rejectReason)
        && (rejectReason.StartsWith("pyannote_sample_", StringComparison.OrdinalIgnoreCase)
            || rejectReason.StartsWith("sample_proof_", StringComparison.OrdinalIgnoreCase));

    public static string DescribeEnrollmentFailureType(string? rejectReason, string? message, VoiceEnrollmentSampleProof? proof = null)
    {
        if (string.Equals(rejectReason, "pyannote_runtime_not_ready", StringComparison.OrdinalIgnoreCase))
        {
            return (message ?? string.Empty).Contains("model cache", StringComparison.OrdinalIgnoreCase)
                ? "Failure type: model cache."
                : "Failure type: identity runtime or model cache.";
        }

        if (string.Equals(rejectReason, "pyannote_enrollment_missing", StringComparison.OrdinalIgnoreCase))
            return "Failure type: identity runtime.";

        if (string.Equals(rejectReason, "pyannote_sample_set_too_small", StringComparison.OrdinalIgnoreCase))
            return "Failure type: not enough samples yet.";

        if (string.Equals(rejectReason, "pyannote_sample_set_not_distinct", StringComparison.OrdinalIgnoreCase))
            return "Failure type: duplicate voice samples; re-record 3 different fresh samples.";

        if (string.Equals(rejectReason, "pyannote_sample_invalid", StringComparison.OrdinalIgnoreCase))
            return "Failure type: sample quality or freshness.";

        if (proof is { Accepted: false } && IsSampleProofRejectReason(proof.RejectReason))
            return DescribeEnrollmentFailureType(proof.RejectReason, proof.Message, null);

        var text = message ?? string.Empty;
        if (text.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            return "Failure type: service timeout.";

        if (text.Contains("microphone", StringComparison.OrdinalIgnoreCase))
            return "Failure type: microphone.";

        if (text.Contains("model cache", StringComparison.OrdinalIgnoreCase))
            return "Failure type: model cache.";

        return "Failure type: service.";
    }

    public VoiceBiometricVerificationResult Verify(
        ProfileStore profileStore,
        UserProfile profile,
        string? candidateSamplePath,
        double? threshold = null,
        TimeSpan? maxCandidateAge = null)
    {
        var identity = LoadIdentity(profileStore, profile);
        if (identity is { Embedding.Length: > 0 })
        {
            return VerifyPyannoteIdentity(
                profileStore,
                profile,
                identity,
                candidateSamplePath,
                threshold,
                maxCandidateAge);
        }

        if (profile.Settings.VoiceBiometricRequired)
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                NormalizeThreshold(threshold),
                PyannoteEngineName,
                "pyannote_enrollment_missing",
                null,
                candidateSamplePath,
                Distance: 1,
                NearMatchThreshold: NormalizeNearMatchThreshold(profile.Settings.VoiceBiometricNearMatchThreshold, threshold),
                EnrollmentEmbeddingPath: GetEnrollmentEmbeddingPath(profileStore, profile));
        }

        return Verify(GetLatestEnrollmentSamplePath(profileStore, profile), candidateSamplePath, threshold, maxCandidateAge);
    }

    public VoiceBiometricVerificationResult Verify(
        string? enrollmentSamplePath,
        string? candidateSamplePath,
        double? threshold = null,
        TimeSpan? maxCandidateAge = null)
    {
        var normalizedThreshold = NormalizeThreshold(threshold);
        if (string.IsNullOrWhiteSpace(enrollmentSamplePath) || !File.Exists(enrollmentSamplePath))
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                normalizedThreshold,
                HeuristicEngineName,
                "biometric_enrollment_missing",
                enrollmentSamplePath,
                candidateSamplePath,
                Distance: 1);
        }

        if (string.IsNullOrWhiteSpace(candidateSamplePath) || !File.Exists(candidateSamplePath))
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                normalizedThreshold,
                HeuristicEngineName,
                "biometric_candidate_missing",
                enrollmentSamplePath,
                candidateSamplePath,
                Distance: 1);
        }

        var replay = ValidateCandidateFreshness(enrollmentSamplePath, candidateSamplePath, maxCandidateAge, normalizedThreshold, HeuristicEngineName, null);
        if (replay != null)
            return replay;

        try
        {
            var enrolled = Voiceprint.Extract(enrollmentSamplePath);
            var candidate = Voiceprint.Extract(candidateSamplePath);
            if (!enrolled.IsUsable || !candidate.IsUsable)
            {
                return new VoiceBiometricVerificationResult(
                    false,
                    0,
                    normalizedThreshold,
                    HeuristicEngineName,
                    "biometric_audio_unusable",
                    enrollmentSamplePath,
                    candidateSamplePath,
                    Distance: 1);
            }

            var score = Voiceprint.Score(enrolled, candidate);
            return new VoiceBiometricVerificationResult(
                score >= normalizedThreshold,
                score,
                normalizedThreshold,
                HeuristicEngineName,
                score >= normalizedThreshold ? null : "biometric_mismatch",
                enrollmentSamplePath,
                candidateSamplePath,
                Distance: 1 - score);
        }
        catch (Exception ex)
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                normalizedThreshold,
                HeuristicEngineName,
                $"biometric_error: {ex.Message}",
                enrollmentSamplePath,
                candidateSamplePath,
                Distance: 1);
        }
    }

    public static string GetLatestEnrollmentSamplePath(ProfileStore profileStore, UserProfile profile)
    {
        return Path.Combine(profileStore.ResolveCallsSignFolder(profile.Callsign), "voice-samples", "latest.wav");
    }

    public static string GetEnrollmentSampleFolder(ProfileStore profileStore, UserProfile profile) =>
        Path.Combine(profileStore.ResolveCallsSignFolder(profile.Callsign), "voice-samples");

    public static string GetEnrollmentSamplePath(ProfileStore profileStore, UserProfile profile, int sampleIndex)
    {
        var folder = GetEnrollmentSampleFolder(profileStore, profile);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"sample-{sampleIndex:000}.wav");
    }

    public static IReadOnlyList<string> GetEnrollmentSamplePaths(ProfileStore profileStore, UserProfile profile)
    {
        var folder = GetEnrollmentSampleFolder(profileStore, profile);
        if (!Directory.Exists(folder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(folder, "sample-*.wav", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string GetIdentityFolder(ProfileStore profileStore, UserProfile profile) =>
        Path.Combine(profileStore.ResolveCallsSignFolder(profile.Callsign), "voice-identity");

    public static string GetEnrollmentEmbeddingPath(ProfileStore profileStore, UserProfile profile) =>
        Path.Combine(GetIdentityFolder(profileStore, profile), "embedding.json");

    public static string GetIdentityMetadataPath(ProfileStore profileStore, UserProfile profile) =>
        Path.Combine(GetIdentityFolder(profileStore, profile), "voice-identity.json");

    public static string GetEnrollmentSampleProofPath(ProfileStore profileStore, UserProfile profile) =>
        Path.Combine(GetIdentityFolder(profileStore, profile), "enrollment-samples.json");

    public static void ResetEnrollmentArtifacts(ProfileStore profileStore, UserProfile profile)
    {
        DeleteFolderIfExists(GetEnrollmentSampleFolder(profileStore, profile));
        DeleteFolderIfExists(GetIdentityFolder(profileStore, profile));
    }

    public static VoiceEnrollmentSampleProof ReadEnrollmentSampleProof(ProfileStore profileStore, UserProfile profile)
    {
        var path = GetEnrollmentSampleProofPath(profileStore, profile);
        if (!File.Exists(path))
            return new VoiceEnrollmentSampleProof(DateTime.UtcNow, false, "sample_proof_missing", "No enrollment sample proof has been written.", 3, 0, 0, []);

        try
        {
            return JsonSerializer.Deserialize<VoiceEnrollmentSampleProof>(File.ReadAllText(path), JsonOptions)
                ?? new VoiceEnrollmentSampleProof(DateTime.UtcNow, false, "sample_proof_invalid", "Enrollment sample proof could not be read.", 3, 0, 0, []);
        }
        catch
        {
            return new VoiceEnrollmentSampleProof(DateTime.UtcNow, false, "sample_proof_invalid", "Enrollment sample proof could not be read.", 3, 0, 0, []);
        }
    }

    private VoiceBiometricVerificationResult VerifyPyannoteIdentity(
        ProfileStore profileStore,
        UserProfile profile,
        VoiceIdentityProfile identity,
        string? candidateSamplePath,
        double? threshold,
        TimeSpan? maxCandidateAge)
    {
        var normalizedThreshold = NormalizeThreshold(threshold ?? identity.Threshold);
        var nearThreshold = NormalizeNearMatchThreshold(profile.Settings.VoiceBiometricNearMatchThreshold, normalizedThreshold);
        if (string.IsNullOrWhiteSpace(candidateSamplePath) || !File.Exists(candidateSamplePath))
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                normalizedThreshold,
                PyannoteEngineName,
                "biometric_candidate_missing",
                null,
                candidateSamplePath,
                Distance: 1,
                NearMatchThreshold: nearThreshold,
                EnrollmentEmbeddingPath: GetEnrollmentEmbeddingPath(profileStore, profile));
        }

        var replay = ValidateCandidateFreshness(null, candidateSamplePath, maxCandidateAge, normalizedThreshold, PyannoteEngineName, GetEnrollmentEmbeddingPath(profileStore, profile));
        if (replay != null)
            return replay;

        if (!TryEmbedWithPyannote(candidateSamplePath, out var candidateEmbedding, out _, out var error))
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                normalizedThreshold,
                PyannoteEngineName,
                "pyannote_runtime_not_ready",
                null,
                candidateSamplePath,
                Distance: 1,
                NearMatchThreshold: nearThreshold,
                EnrollmentEmbeddingPath: GetEnrollmentEmbeddingPath(profileStore, profile));
        }

        var score = Cosine(identity.Embedding, candidateEmbedding);
        return new VoiceBiometricVerificationResult(
            score >= normalizedThreshold,
            score,
            normalizedThreshold,
            PyannoteEngineName,
            score >= normalizedThreshold ? null : "biometric_mismatch",
            null,
            candidateSamplePath,
            Distance: 1 - score,
            NearMatchThreshold: nearThreshold,
            EnrollmentEmbeddingPath: GetEnrollmentEmbeddingPath(profileStore, profile));
    }

    private static VoiceBiometricVerificationResult? ValidateCandidateFreshness(
        string? enrollmentSamplePath,
        string candidateSamplePath,
        TimeSpan? maxCandidateAge,
        double threshold,
        string engine,
        string? embeddingPath)
    {
        if (!string.IsNullOrWhiteSpace(enrollmentSamplePath)
            && Path.GetFullPath(enrollmentSamplePath).Equals(Path.GetFullPath(candidateSamplePath), StringComparison.OrdinalIgnoreCase))
        {
            return new VoiceBiometricVerificationResult(
                false,
                0,
                threshold,
                engine,
                "biometric_replay_enrollment_sample",
                enrollmentSamplePath,
                candidateSamplePath,
                Distance: 1,
                EnrollmentEmbeddingPath: embeddingPath);
        }

        if (maxCandidateAge.HasValue)
        {
            var candidateAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(candidateSamplePath);
            if (candidateAge < TimeSpan.Zero || candidateAge > maxCandidateAge.Value)
            {
                return new VoiceBiometricVerificationResult(
                    false,
                    0,
                    threshold,
                    engine,
                    "biometric_candidate_stale",
                    enrollmentSamplePath,
                    candidateSamplePath,
                    Distance: 1,
                    EnrollmentEmbeddingPath: embeddingPath);
            }
        }

        return null;
    }

    private static string? ValidateEnrollmentSample(string samplePath, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(samplePath) || !File.Exists(samplePath))
        {
            error = "A required voice sample is missing.";
            return error;
        }

        var sampleInfo = new FileInfo(samplePath);
        if (sampleInfo.Length < 1024)
        {
            error = "A required voice sample is too small to fingerprint.";
            return error;
        }

        var age = DateTime.UtcNow - sampleInfo.LastWriteTimeUtc;
        if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(30))
        {
            error = $"A required voice sample is stale ({age.TotalMinutes:0.0} minutes old). Re-record the voice samples.";
            return error;
        }

        var quality = VoiceSampleQualityAnalyzer.Analyze(samplePath);
        if (!quality.Accepted)
        {
            error = quality.Message;
            return error;
        }

        return null;
    }

    private static VoiceIdentityProfile? LoadIdentity(ProfileStore profileStore, UserProfile profile)
    {
        var path = GetEnrollmentEmbeddingPath(profileStore, profile);
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<VoiceIdentityProfile>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveIdentity(ProfileStore profileStore, UserProfile profile, VoiceIdentityProfile identity)
    {
        var folder = GetIdentityFolder(profileStore, profile);
        Directory.CreateDirectory(folder);
        File.WriteAllText(GetEnrollmentEmbeddingPath(profileStore, profile), JsonSerializer.Serialize(identity, JsonOptions));
        var sampleProof = ReadEnrollmentSampleProof(profileStore, profile);
        var metadata = new
        {
            identity.Engine,
            identity.ModelId,
            identity.CreatedUtc,
            identity.UpdatedUtc,
            identity.SamplesEnrolled,
            identity.Threshold,
            identity.NearMatchThreshold,
            SampleProof = sampleProof
        };
        File.WriteAllText(GetIdentityMetadataPath(profileStore, profile), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private static VoiceEnrollmentSampleProof BuildEnrollmentSampleProof(
        IEnumerable<string> samplePaths,
        bool accepted,
        string? rejectReason,
        string message)
    {
        var samples = new List<VoiceEnrollmentSampleMetadata>();
        foreach (var samplePath in ResolveDistinctEnrollmentSamples(samplePaths))
        {
            if (!File.Exists(samplePath))
                continue;

            var info = new FileInfo(samplePath);
            var lastWriteUtc = info.LastWriteTimeUtc;
            var ageSeconds = Math.Max(0, (DateTime.UtcNow - lastWriteUtc).TotalSeconds);
            var quality = VoiceSampleQualityAnalyzer.Analyze(samplePath);
            samples.Add(new VoiceEnrollmentSampleMetadata(
                Path.GetFullPath(samplePath),
                info.Name,
                info.Length,
                lastWriteUtc,
                ageSeconds,
                ComputeSha256(samplePath),
                quality.State,
                quality.Message,
                quality.Peak,
                quality.Rms,
                quality.DurationSeconds,
                quality.ClippingRatio,
                quality.ZeroCrossingRate));
        }

        var distinctHashCount = samples
            .Select(sample => sample.Sha256)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        return new VoiceEnrollmentSampleProof(
            DateTime.UtcNow,
            accepted,
            rejectReason,
            message,
            3,
            samples.Count,
            distinctHashCount,
            samples);
    }

    private static void WriteEnrollmentSampleProof(ProfileStore profileStore, UserProfile profile, VoiceEnrollmentSampleProof proof)
    {
        var folder = GetIdentityFolder(profileStore, profile);
        Directory.CreateDirectory(folder);
        File.WriteAllText(GetEnrollmentSampleProofPath(profileStore, profile), JsonSerializer.Serialize(proof, JsonOptions));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static double[] AverageEmbeddings(double[]? existing, IReadOnlyList<double[]> nextEmbeddings)
    {
        var allEmbeddings = new List<double[]>();
        if (existing is { Length: > 0 })
            allEmbeddings.Add(existing);

        foreach (var embedding in nextEmbeddings)
        {
            if (embedding is { Length: > 0 })
                allEmbeddings.Add(embedding);
        }

        if (allEmbeddings.Count == 0)
            return [];

        var length = allEmbeddings.Min(embedding => embedding.Length);
        var averaged = new double[length];
        for (var index = 0; index < length; index++)
        {
            double sum = 0;
            foreach (var embedding in allEmbeddings)
                sum += embedding[index];
            averaged[index] = sum / allEmbeddings.Count;
        }
        return NormalizeVector(averaged);
    }

    private static IEnumerable<string> ResolveDistinctEnrollmentSamples(IEnumerable<string> samplePaths)
    {
        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var samplePath in samplePaths)
        {
            if (string.IsNullOrWhiteSpace(samplePath))
                continue;

            var fullPath = Path.GetFullPath(samplePath);
            if (!File.Exists(fullPath))
                continue;

            distinct.Add(fullPath);
        }

        return distinct.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryEmbedWithPyannote(string wavPath, out double[] embedding, out string? modelId, out string error)
    {
        embedding = [];
        modelId = null;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
        {
            error = "Voice sample is missing.";
            return false;
        }

        var sampleInfo = new FileInfo(wavPath);
        if (sampleInfo.Length < 1024)
        {
            error = "Voice sample is too small to fingerprint. Record a clean sample and try again.";
            return false;
        }

        var pythonPath = GetPyannotePythonPath();
        if (!File.Exists(pythonPath))
        {
            error = "pyannote runtime not ready. Next: choose Train Voice Identity, then Repair Identity Runtime if prompted.";
            return false;
        }

        var modelCachePath = GetPyannoteModelCachePath();
        if (!PyannoteModelCacheLooksReady(modelCachePath))
        {
            error = "pyannote model cache is missing or incomplete. Next: choose Repair Identity Runtime or reinstall Callsign.";
            return false;
        }

        var scriptPath = EnsurePyannoteEmbeddingScript();
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add(scriptPath);
            process.StartInfo.ArgumentList.Add(wavPath);
            process.StartInfo.Environment["HF_HOME"] = modelCachePath;
            process.StartInfo.Environment["HUGGINGFACE_HUB_CACHE"] = modelCachePath;
            process.StartInfo.Environment["HF_HUB_OFFLINE"] = "1";
            process.StartInfo.Environment["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1";
            process.StartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            process.StartInfo.Environment["PYTHONWARNINGS"] = "ignore";

            if (!process.Start())
            {
                error = "pyannote embedding process could not be started.";
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000))
            {
                TryKillProcessTree(process);
                error = "pyannote embedding timed out after 120 seconds. Next: Repair Identity Runtime, then try enrollment again.";
                return false;
            }

            var output = outputTask.GetAwaiter().GetResult();
            var stderr = errorTask.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stderr)
                    ? $"pyannote embedding failed with exit code {process.ExitCode}."
                    : stderr.Trim();
                return false;
            }

            var result = JsonSerializer.Deserialize<PyannoteEmbeddingResult>(output, JsonOptions);
            if (result?.Embedding is not { Length: > 0 })
            {
                error = "pyannote produced no embedding.";
                return false;
            }

            embedding = NormalizeVector(result.Embedding);
            modelId = result.ModelId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetPyannotePythonPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "pyannote",
            "venv",
            "Scripts",
            "python.exe");
    }

    private static string GetPyannoteModelCachePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "pyannote",
            "hub");
    }

    private static bool PyannoteModelCacheLooksReady(string modelCachePath)
    {
        if (!Directory.Exists(modelCachePath))
            return false;

        var modelRoot = Path.Combine(modelCachePath, "models--pyannote--embedding");
        if (!Directory.Exists(modelRoot))
            return false;

        try
        {
            return Directory.EnumerateFiles(modelRoot, "pytorch_model.bin", SearchOption.AllDirectories).Any()
                || Directory.EnumerateFiles(modelRoot, "model.safetensors", SearchOption.AllDirectories).Any();
        }
        catch
        {
            return false;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private static void DeleteFolderIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static string EnsurePyannoteEmbeddingScript()
    {
        var runtimeRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Runtime",
            "pyannote");
        Directory.CreateDirectory(runtimeRoot);
        var scriptPath = Path.Combine(runtimeRoot, "pyannote_embed.py");
        File.WriteAllText(scriptPath, PyannoteEmbeddingScript);
        return scriptPath;
    }

    private static double NormalizeThreshold(double? threshold)
    {
        var value = threshold.GetValueOrDefault(DefaultThreshold);
        return Math.Clamp(value <= 0 ? DefaultThreshold : value, 0.50, 0.98);
    }

    private static double NormalizeNearMatchThreshold(double? nearMatchThreshold, double? normalThreshold)
    {
        var normal = NormalizeThreshold(normalThreshold);
        var near = nearMatchThreshold.GetValueOrDefault(DefaultNearMatchThreshold);
        return Math.Clamp(near <= 0 ? DefaultNearMatchThreshold : near, normal, 0.99);
    }

    private static double Cosine(double[] left, double[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0)
            return 0;

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;
        for (var index = 0; index < length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        if (leftNorm <= 0 || rightNorm <= 0)
            return 0;

        return Math.Clamp(dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm)), 0, 1);
    }

    private static double[] NormalizeVector(double[] vector)
    {
        var norm = Math.Sqrt(vector.Sum(value => value * value));
        if (norm <= 0)
            return vector;

        return vector.Select(value => value / norm).ToArray();
    }

    private sealed class VoiceIdentityProfile
    {
        public string Engine { get; set; } = PyannoteEngineName;
        public string ModelId { get; set; } = PyannoteEngineName;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public int SamplesEnrolled { get; set; }
        public double Threshold { get; set; } = DefaultThreshold;
        public double NearMatchThreshold { get; set; } = DefaultNearMatchThreshold;
        public double[] Embedding { get; set; } = [];
    }

    private sealed class PyannoteEmbeddingResult
    {
        public string? ModelId { get; set; }
        public double[] Embedding { get; set; } = [];
    }

    private const string PyannoteEmbeddingScript = """
import json
import os
import sys
import warnings

import numpy as np
import torch
from pyannote.audio import Model
import soundfile as sf
from scipy import signal

warnings.filterwarnings("ignore")
runtime_root = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Callsign", "Runtime", "pyannote")
model_cache = os.path.join(runtime_root, "hub")
os.environ.setdefault("HF_HOME", model_cache)
os.environ.setdefault("HUGGINGFACE_HUB_CACHE", model_cache)
os.environ.setdefault("HF_HUB_OFFLINE", "1")
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")

MODEL_ID = "pyannote/embedding"
wav_path = sys.argv[1]
token = os.environ.get("HF_TOKEN") or os.environ.get("HUGGINGFACE_TOKEN")
model = Model.from_pretrained(
    MODEL_ID,
    token=token,
    cache_dir=model_cache,
    strict=False,
    local_files_only=not bool(token),
)
samples, sample_rate = sf.read(wav_path, dtype="float32", always_2d=True)
samples = samples.mean(axis=1)
if sample_rate != 16000:
    samples = signal.resample_poly(samples, 16000, sample_rate).astype("float32")
waveform = torch.from_numpy(samples).float().unsqueeze(0)
with torch.no_grad():
    embedding = model(waveform[None])
array = embedding.detach().cpu().numpy()
array = np.asarray(array).reshape(-1)
norm = np.linalg.norm(array)
if norm > 0:
    array = array / norm
print(json.dumps({"ModelId": MODEL_ID, "Embedding": array.astype(float).tolist()}))
""";

    private sealed class Voiceprint
    {
        private readonly double[] _features;
        private readonly double _durationSeconds;

        private Voiceprint(double[] features, double durationSeconds)
        {
            _features = features;
            _durationSeconds = durationSeconds;
            IsUsable = features.Length > 0 && durationSeconds >= 0.20;
        }

        public bool IsUsable { get; }

        public static Voiceprint Extract(string wavPath)
        {
            var samples = ReadMonoSamples(wavPath);
            var trimmed = TrimSilence(samples);
            if (trimmed.Length == 0)
                return new Voiceprint([], 0);

            NormalizeRms(trimmed);
            var durationSeconds = trimmed.Length / 16000.0;
            var features = BuildFeatures(trimmed);
            return new Voiceprint(features, durationSeconds);
        }

        public static double Score(Voiceprint enrolled, Voiceprint candidate)
        {
            var cosine = Cosine(enrolled._features, candidate._features);
            var durationRatio = Math.Min(enrolled._durationSeconds, candidate._durationSeconds)
                / Math.Max(enrolled._durationSeconds, candidate._durationSeconds);
            var durationScore = Math.Clamp(durationRatio, 0, 1);
            return Math.Clamp((cosine * 0.82) + (durationScore * 0.18), 0, 1);
        }

        private static float[] ReadMonoSamples(string wavPath)
        {
            using var reader = new AudioFileReader(wavPath);
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

            return mono.ToArray();
        }

        private static float[] TrimSilence(float[] samples)
        {
            const float threshold = 0.012f;
            var start = 0;
            while (start < samples.Length && Math.Abs(samples[start]) < threshold)
                start++;

            var end = samples.Length - 1;
            while (end > start && Math.Abs(samples[end]) < threshold)
                end--;

            if (end <= start)
                return [];

            var result = new float[end - start + 1];
            Array.Copy(samples, start, result, 0, result.Length);
            return result;
        }

        private static void NormalizeRms(float[] samples)
        {
            var rms = Math.Sqrt(samples.Select(sample => sample * sample).Average());
            if (rms <= 0.0001)
                return;

            var gain = 0.18 / rms;
            for (var index = 0; index < samples.Length; index++)
                samples[index] = (float)Math.Clamp(samples[index] * gain, -1, 1);
        }

        private static double[] BuildFeatures(float[] samples)
        {
            var features = new List<double>
            {
                samples.Select(Math.Abs).Average(),
                StandardDeviation(samples.Select(sample => (double)Math.Abs(sample))),
                ZeroCrossingRate(samples)
            };

            AddEnvelopeFeatures(samples, features, bins: 16);
            AddAutocorrelationFeatures(samples, features, lags: [40, 53, 67, 80, 100, 133, 160, 200, 267, 320, 400, 533]);
            return features.ToArray();
        }

        private static void AddEnvelopeFeatures(float[] samples, List<double> features, int bins)
        {
            for (var bin = 0; bin < bins; bin++)
            {
                var start = bin * samples.Length / bins;
                var end = Math.Max(start + 1, (bin + 1) * samples.Length / bins);
                var sum = 0.0;
                for (var index = start; index < end; index++)
                    sum += Math.Abs(samples[index]);
                features.Add(sum / (end - start));
            }
        }

        private static void AddAutocorrelationFeatures(float[] samples, List<double> features, int[] lags)
        {
            foreach (var lag in lags)
            {
                if (samples.Length <= lag)
                {
                    features.Add(0);
                    continue;
                }

                double numerator = 0;
                double denominator = 0;
                for (var index = lag; index < samples.Length; index++)
                {
                    numerator += samples[index] * samples[index - lag];
                    denominator += samples[index] * samples[index];
                }

                features.Add(denominator <= 0 ? 0 : numerator / denominator);
            }
        }

        private static double ZeroCrossingRate(float[] samples)
        {
            if (samples.Length < 2)
                return 0;

            var crossings = 0;
            for (var index = 1; index < samples.Length; index++)
            {
                if ((samples[index - 1] < 0 && samples[index] >= 0) || (samples[index - 1] >= 0 && samples[index] < 0))
                    crossings++;
            }

            return (double)crossings / (samples.Length - 1);
        }

        private static double StandardDeviation(IEnumerable<double> values)
        {
            var array = values.ToArray();
            if (array.Length == 0)
                return 0;

            var average = array.Average();
            return Math.Sqrt(array.Select(value => Math.Pow(value - average, 2)).Average());
        }
    }
}
