namespace Callsign.UI.Services;

public sealed record CallsignIdentityResult(
    bool Accepted,
    string? MatchedVariant,
    double Confidence,
    string? RejectReason,
    string? RetryPrompt,
    string Transcript,
    VoiceBiometricVerificationResult? Biometric = null);

public static class CallsignIdentityMatcher
{
    public const double DefaultConfidenceThreshold = 0.65;
    public const double DefaultNearMatchBiometricThreshold = 0.86;

    public static CallsignIdentityResult Evaluate(
        string transcript,
        float transcriptConfidence,
        string enrolledCallsign,
        IEnumerable<string>? aliases = null,
        double confidenceThreshold = DefaultConfidenceThreshold,
        VoiceBiometricVerificationResult? biometric = null,
        bool requireBiometric = false,
        double nearMatchBiometricThreshold = DefaultNearMatchBiometricThreshold)
    {
        var normalizedTranscript = AlphaVoiceTranscriptParser.NormalizeSpeechText(transcript);
        var variants = BuildAllowedVariants(enrolledCallsign, aliases).ToArray();
        var confidence = Math.Clamp(transcriptConfidence, 0f, 1f);
        var retryPrompt = "Say your callsign again.";

        if (confidence < confidenceThreshold)
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: "identity_confidence_low",
                RetryPrompt: retryPrompt,
                Transcript: transcript,
                Biometric: biometric);
        }

        if (requireBiometric && biometric == null)
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: "identity_biometric_unavailable",
                RetryPrompt: "Say your callsign again.",
                Transcript: transcript,
                Biometric: biometric);
        }

        if (requireBiometric && biometric?.Accepted != true)
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: biometric?.RejectReason ?? "identity_biometric_mismatch",
                RetryPrompt: "Say your callsign again.",
                Transcript: transcript,
                Biometric: biometric);
        }

        if (string.IsNullOrWhiteSpace(normalizedTranscript))
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: "identity_empty",
                RetryPrompt: retryPrompt,
                Transcript: transcript,
                Biometric: biometric);
        }

        foreach (var variant in variants)
        {
            if (IsExactIdentity(normalizedTranscript, variant))
            {
                return new CallsignIdentityResult(
                    Accepted: true,
                    MatchedVariant: variant,
                    Confidence: confidence,
                    RejectReason: null,
                    RetryPrompt: null,
                    Transcript: transcript,
                    Biometric: biometric);
            }
        }

        var normalizedNearMatchBiometricThreshold = Math.Clamp(
            nearMatchBiometricThreshold <= 0 ? DefaultNearMatchBiometricThreshold : nearMatchBiometricThreshold,
            biometric?.Threshold ?? VoiceBiometricVerificationService.DefaultThreshold,
            0.99);

        if (biometric?.Accepted == true && biometric.Score >= normalizedNearMatchBiometricThreshold)
        {
            var nearVariant = variants.FirstOrDefault(variant => IsNearIdentity(normalizedTranscript, variant));
            if (!string.IsNullOrWhiteSpace(nearVariant))
            {
                return new CallsignIdentityResult(
                    Accepted: true,
                    MatchedVariant: nearVariant,
                    Confidence: confidence,
                    RejectReason: null,
                    RetryPrompt: null,
                    Transcript: transcript,
                    Biometric: biometric);
            }
        }

        if (biometric?.Accepted == true
            && biometric.Score < normalizedNearMatchBiometricThreshold
            && variants.Any(variant => IsNearIdentity(normalizedTranscript, variant)))
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: "identity_near_match_biometric_too_weak",
                RetryPrompt: "Say your callsign again.",
                Transcript: transcript,
                Biometric: biometric);
        }

        if (variants.Any(variant => ContainsIdentityWithExtraWords(normalizedTranscript, variant)))
        {
            return new CallsignIdentityResult(
                Accepted: false,
                MatchedVariant: null,
                Confidence: confidence,
                RejectReason: "identity_ambiguous_extra_words",
                RetryPrompt: "Say only your callsign.",
                Transcript: transcript,
                Biometric: biometric);
        }

        return new CallsignIdentityResult(
            Accepted: false,
            MatchedVariant: null,
            Confidence: confidence,
            RejectReason: "identity_mismatch",
            RetryPrompt: null,
            Transcript: transcript,
            Biometric: biometric);
    }

    public static IReadOnlyList<string> BuildAllowedVariants(string enrolledCallsign, IEnumerable<string>? aliases = null)
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddVariant(enrolledCallsign);

        if (aliases != null)
        {
            foreach (var alias in aliases)
                AddVariant(alias);
        }

        return variants
            .Where(variant => !string.IsNullOrWhiteSpace(variant))
            .OrderBy(variant => variant, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void AddVariant(string? value)
        {
            var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            variants.Add(normalized);
            variants.Add(normalized.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static bool IsExactIdentity(string normalizedTranscript, string normalizedVariant)
    {
        if (string.Equals(normalizedTranscript, normalizedVariant, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(
            RemoveSpeechSeparators(normalizedTranscript),
            RemoveSpeechSeparators(normalizedVariant),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsIdentityWithExtraWords(string normalizedTranscript, string normalizedVariant)
    {
        if (IsExactIdentity(normalizedTranscript, normalizedVariant))
            return false;

        var transcriptWithBoundaries = $" {normalizedTranscript} ";
        var variantWithBoundaries = $" {normalizedVariant} ";
        return transcriptWithBoundaries.Contains(variantWithBoundaries, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNearIdentity(string normalizedTranscript, string normalizedVariant)
    {
        var transcript = RemoveSpeechSeparators(normalizedTranscript);
        var variant = RemoveSpeechSeparators(normalizedVariant);
        if (transcript.Length < 3 || variant.Length < 3)
            return false;

        var distance = LevenshteinDistance(transcript, variant);
        var maxLength = Math.Max(transcript.Length, variant.Length);
        return distance <= 1 || (maxLength >= 6 && distance <= 2);
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var costs = new int[right.Length + 1];
        for (var index = 0; index <= right.Length; index++)
            costs[index] = index;

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            var previous = costs[0];
            costs[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var current = costs[rightIndex];
                var substitution = previous + (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1);
                costs[rightIndex] = Math.Min(
                    Math.Min(costs[rightIndex] + 1, costs[rightIndex - 1] + 1),
                    substitution);
                previous = current;
            }
        }

        return costs[right.Length];
    }

    private static string RemoveSpeechSeparators(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit));
}
