using System.Globalization;

namespace Callsign.UI.Services;

public sealed record DictationCorrectionChoice(
    int Number,
    string Text,
    string Label,
    int Start,
    int Length);

public sealed record DictationCorrectionSession(
    DictationReplacementScope Scope,
    string OriginalText,
    IReadOnlyList<DictationCorrectionChoice> Choices);

public static class DictationCorrectionService
{
    public static DictationCorrectionSession CreateSession(string text, DictationReplacementScope scope)
    {
        var (start, length) = GetSpan(text, scope);
        if (length <= 0)
            return new DictationCorrectionSession(scope, string.Empty, []);

        var original = text.Substring(start, length).Trim();
        if (string.IsNullOrWhiteSpace(original))
            return new DictationCorrectionSession(scope, string.Empty, []);

        var choices = new List<DictationCorrectionChoice>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        AddChoice(choices, seen, original, "Keep as heard", start, length);
        AddChoice(choices, seen, ToSentenceCase(original), "Sentence case", start, length);
        AddChoice(choices, seen, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(original.ToLower(CultureInfo.CurrentCulture)), "Title case", start, length);
        AddChoice(choices, seen, original.ToLower(CultureInfo.CurrentCulture), "Lowercase", start, length);
        AddChoice(choices, seen, original.ToUpper(CultureInfo.CurrentCulture), "Uppercase", start, length);

        if (original.Contains(' ', StringComparison.Ordinal))
        {
            AddChoice(choices, seen, string.Concat(original.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)), "Joined words", start, length);
            AddChoice(choices, seen, string.Join("-", original.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)), "Hyphenated", start, length);
        }

        var withoutTrailingPunctuation = original.TrimEnd('.', '!', '?', ',', ';', ':');
        if (!string.Equals(withoutTrailingPunctuation, original, StringComparison.Ordinal))
            AddChoice(choices, seen, withoutTrailingPunctuation, "Without trailing punctuation", start, length);

        return new DictationCorrectionSession(scope, original, choices.Take(6).ToArray());
    }

    public static bool TryApplyChoice(string currentText, DictationCorrectionChoice choice, out string updatedText, out int selectionStart)
    {
        updatedText = currentText;
        selectionStart = 0;
        if (choice.Start < 0 || choice.Length <= 0 || choice.Start + choice.Length > currentText.Length)
            return false;

        updatedText = currentText.Remove(choice.Start, choice.Length).Insert(choice.Start, choice.Text);
        selectionStart = choice.Start + choice.Text.Length;
        return true;
    }

    private static void AddChoice(
        List<DictationCorrectionChoice> choices,
        HashSet<string> seen,
        string text,
        string label,
        int start,
        int length)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            return;

        choices.Add(new DictationCorrectionChoice(choices.Count + 1, normalized, label, start, length));
    }

    private static string ToSentenceCase(string text)
    {
        var lower = text.ToLower(CultureInfo.CurrentCulture);
        for (var index = 0; index < lower.Length; index++)
        {
            if (!char.IsLetter(lower[index]))
                continue;

            return lower[..index] + char.ToUpper(lower[index], CultureInfo.CurrentCulture) + lower[(index + 1)..];
        }

        return lower;
    }

    private static (int Start, int Length) GetSpan(string text, DictationReplacementScope scope) =>
        scope switch
        {
            DictationReplacementScope.PreviousSentence => GetLastSentenceSpan(text),
            DictationReplacementScope.PreviousParagraph => GetCurrentParagraphSpan(text),
            DictationReplacementScope.AllText => string.IsNullOrWhiteSpace(text) ? (0, 0) : (0, text.TrimEnd().Length),
            _ => GetLastWordSpan(text)
        };

    private static (int Start, int Length) GetLastWordSpan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0);

        var trimmed = text.TrimEnd();
        var end = trimmed.Length;
        var start = end;
        while (start > 0 && !char.IsWhiteSpace(trimmed[start - 1]))
            start--;

        return start >= end ? (0, 0) : (start, end - start);
    }

    private static (int Start, int Length) GetLastSentenceSpan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0);

        var trimmed = text.TrimEnd();
        var end = trimmed.Length;
        var sentenceStart = 0;
        var searchStart = end > 0 && trimmed[end - 1] is ('.' or '!' or '?') ? end - 2 : end - 1;
        for (var index = searchStart; index >= 0; index--)
        {
            if (trimmed[index] is not ('.' or '!' or '?'))
                continue;

            sentenceStart = index + 1;
            while (sentenceStart < end && char.IsWhiteSpace(trimmed[sentenceStart]))
                sentenceStart++;
            break;
        }

        return sentenceStart >= end ? (0, 0) : (sentenceStart, end - sentenceStart);
    }

    private static (int Start, int Length) GetCurrentParagraphSpan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0);

        var end = text.TrimEnd().Length;
        var start = text.LastIndexOf(Environment.NewLine + Environment.NewLine, Math.Max(0, end - 1), StringComparison.Ordinal);
        start = start < 0 ? 0 : start + (Environment.NewLine + Environment.NewLine).Length;
        return start >= end ? (0, 0) : (start, end - start);
    }
}
