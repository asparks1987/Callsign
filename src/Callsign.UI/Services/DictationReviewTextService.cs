using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Callsign.UI.Services;

public static partial class DictationReviewTextService
{
    [GeneratedRegex(@"\b(?:asshole|bastard|bitch|bullshit|damn|fuck|fucking|hell|shit)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex ProfanityRegex();
    [GeneratedRegex(@"\b(?:um|uh|er|ah)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex FillerWordRegex();
    [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex MultiSpaceRegex();
    [GeneratedRegex(@"\bi\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex StandalonePronounRegex();

    public static string AppendReviewedText(
        string? existingText,
        string transcript,
        DictationCasingMode casingMode,
        bool fluidDictationEnabled,
        bool automaticPunctuationEnabled,
        bool profanityFilterEnabled)
    {
        var reviewedText = existingText ?? string.Empty;
        var displaySegment = FormatReviewedSegment(
            transcript,
            casingMode,
            fluidDictationEnabled,
            automaticPunctuationEnabled,
            profanityFilterEnabled,
            ShouldTreatNextSegmentAsSentenceStart(reviewedText));
        if (string.IsNullOrWhiteSpace(displaySegment))
            return reviewedText;

        var builder = new StringBuilder(reviewedText);
        if (builder.Length > 0 && NeedsSeparator(builder))
            builder.Append(' ');

        builder.Append(displaySegment);
        return builder.ToString();
    }

    public static string BuildReviewedText(
        IEnumerable<string>? transcripts,
        DictationCasingMode casingMode,
        bool fluidDictationEnabled,
        bool automaticPunctuationEnabled,
        bool profanityFilterEnabled)
    {
        if (transcripts == null)
            return string.Empty;

        var reviewedText = string.Empty;
        foreach (var transcript in transcripts)
        {
            reviewedText = AppendReviewedText(
                reviewedText,
                transcript,
                casingMode,
                fluidDictationEnabled,
                automaticPunctuationEnabled,
                profanityFilterEnabled);
        }

        return reviewedText;
    }

    public static string FormatReviewedSegment(
        string transcript,
        DictationCasingMode casingMode,
        bool fluidDictationEnabled,
        bool automaticPunctuationEnabled,
        bool profanityFilterEnabled,
        bool startOfSentence)
    {
        var normalized = transcript.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (fluidDictationEnabled)
            normalized = ApplyFluidDictationCleanup(normalized);

        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var usesSentenceShaping = automaticPunctuationEnabled || fluidDictationEnabled;
        var displayText = ApplyCasing(normalized, casingMode, usesSentenceShaping, startOfSentence);
        if (fluidDictationEnabled)
            displayText = CapitalizeSentenceBoundaries(displayText, startOfSentence);

        if (usesSentenceShaping && !EndsWithSentenceTerminator(displayText))
            displayText += ".";

        if (profanityFilterEnabled)
            displayText = ProfanityRegex().Replace(displayText, static match => MaskProfanity(match.Value));

        return displayText;
    }

    public static bool ShouldTreatNextSegmentAsSentenceStart(string? existingText)
    {
        if (string.IsNullOrWhiteSpace(existingText))
            return true;

        var trimmed = existingText.TrimEnd();
        if (trimmed.Length == 0)
            return true;

        var lastCharacter = trimmed[^1];
        return lastCharacter is '.' or '!' or '?' or '\n';
    }

    private static string ApplyFluidDictationCleanup(string text)
    {
        var normalized = text;
        normalized = FillerWordRegex().Replace(normalized, " ");
        normalized = normalized.Replace(" ,", ",", StringComparison.Ordinal)
            .Replace(" .", ".", StringComparison.Ordinal)
            .Replace(" !", "!", StringComparison.Ordinal)
            .Replace(" ?", "?", StringComparison.Ordinal)
            .Replace(" ;", ";", StringComparison.Ordinal)
            .Replace(" :", ":", StringComparison.Ordinal);
        normalized = StandalonePronounRegex().Replace(normalized, "I");
        normalized = MultiSpaceRegex().Replace(normalized, " ").Trim();
        return normalized;
    }

    private static string ApplyCasing(
        string text,
        DictationCasingMode casingMode,
        bool automaticPunctuationEnabled,
        bool startOfSentence) =>
        casingMode switch
        {
            DictationCasingMode.Caps => ToSentenceCase(text),
            DictationCasingMode.AllCaps => text.ToUpper(CultureInfo.CurrentCulture),
            DictationCasingMode.NoCaps => text.ToLower(CultureInfo.CurrentCulture),
            _ when automaticPunctuationEnabled && startOfSentence => ToSentenceCase(text),
            _ => text
        };

    private static string CapitalizeSentenceBoundaries(string text, bool startOfSentence)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var builder = new StringBuilder(text.Length);
        var capitalizeNext = startOfSentence;
        foreach (var character in text)
        {
            if (capitalizeNext && char.IsLetter(character))
            {
                builder.Append(char.ToUpper(character, CultureInfo.CurrentCulture));
                capitalizeNext = false;
                continue;
            }

            builder.Append(character);
            if (character is '.' or '!' or '?')
                capitalizeNext = true;
            else if (!char.IsWhiteSpace(character))
                capitalizeNext = false;
        }

        return builder.ToString();
    }

    private static bool EndsWithSentenceTerminator(string text)
    {
        for (var index = text.Length - 1; index >= 0; index--)
        {
            var character = text[index];
            if (char.IsWhiteSpace(character))
                continue;

            return character is '.' or '!' or '?';
        }

        return false;
    }

    private static bool NeedsSeparator(StringBuilder builder)
    {
        if (builder.Length == 0)
            return false;

        var lastCharacter = builder[^1];
        return !char.IsWhiteSpace(lastCharacter);
    }

    private static string ToSentenceCase(string text)
    {
        var lower = text.ToLower(CultureInfo.CurrentCulture);
        for (var index = 0; index < lower.Length; index++)
        {
            if (!char.IsLetter(lower[index]))
                continue;

            return lower[..index]
                + char.ToUpper(lower[index], CultureInfo.CurrentCulture)
                + lower[(index + 1)..];
        }

        return lower;
    }

    private static string MaskProfanity(string value)
    {
        if (value.Length <= 2)
            return new string('*', value.Length);

        return value[0] + new string('*', value.Length - 2) + value[^1];
    }
}
