using System.Text.RegularExpressions;
using Callsign.UI.Models;

namespace Callsign.UI.Services;

public enum DictationVocabularyAddStatus
{
    Added,
    AlreadyExists,
    Invalid
}

public sealed record DictationVocabularyAddResult(
    DictationVocabularyAddStatus Status,
    string Word,
    int Count,
    string Message);

public static class DictationVocabularyService
{
    public static DictationVocabularyAddResult Add(UserProfile profile, string phrase)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Settings ??= new UserSettings();
        profile.Settings.DictationVocabulary ??= [];

        var normalized = NormalizeEntry(phrase);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new DictationVocabularyAddResult(
                DictationVocabularyAddStatus.Invalid,
                string.Empty,
                profile.Settings.DictationVocabulary.Count,
                "Vocabulary entry was empty or unsafe.");
        }

        if (profile.Settings.DictationVocabulary.Any(entry => string.Equals(entry, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return new DictationVocabularyAddResult(
                DictationVocabularyAddStatus.AlreadyExists,
                normalized,
                profile.Settings.DictationVocabulary.Count,
                $"'{normalized}' is already in the local dictation vocabulary.");
        }

        profile.Settings.DictationVocabulary.Add(normalized);
        profile.Settings.DictationVocabulary = profile.Settings.DictationVocabulary
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DictationVocabularyAddResult(
            DictationVocabularyAddStatus.Added,
            normalized,
            profile.Settings.DictationVocabulary.Count,
            $"Added '{normalized}' to the local dictation vocabulary.");
    }

    public static string NormalizeEntry(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return string.Empty;

        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(phrase)
            .Replace(" dash ", "-", StringComparison.OrdinalIgnoreCase)
            .Replace(" hyphen ", "-", StringComparison.OrdinalIgnoreCase)
            .Replace(" underscore ", "_", StringComparison.OrdinalIgnoreCase)
            .Trim();

        normalized = Regex.Replace(
            normalized,
            @"\s+",
            " ",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        if (normalized.Length is < 2 or > 80)
            return string.Empty;

        return Regex.IsMatch(
            normalized,
            @"^[a-z0-9][a-z0-9 '\-_\.]{0,78}[a-z0-9]$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100))
            ? normalized
            : string.Empty;
    }
}
