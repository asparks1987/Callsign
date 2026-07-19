using System.Text.RegularExpressions;

namespace Callsign.UI.Services;

public sealed record DictationTargetTextResult(
    string Text,
    int SelectionStart,
    int SelectionLength,
    bool Changed,
    string MatchedText);

public static class DictationTargetTextService
{
    public static bool TryApply(string text, DictationTargetTextCommand command, out DictationTargetTextResult result)
    {
        result = new DictationTargetTextResult(text, 0, 0, false, string.Empty);
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(command.TargetText))
            return false;

        var start = 0;
        var length = 0;
        var foundSpan = !string.IsNullOrWhiteSpace(command.EndText)
            ? TryFindPhraseRangeSpan(text, command.TargetText, command.EndText, out start, out length)
            : TryFindPhraseSpan(text, command.TargetText, out start, out length);
        if (!foundSpan)
            return false;

        var matchedText = text.Substring(start, length);
        switch (command.Action)
        {
            case DictationTargetTextAction.Select:
                result = new DictationTargetTextResult(text, start, length, false, matchedText);
                return true;
            case DictationTargetTextAction.MoveBefore:
                result = new DictationTargetTextResult(text, start, 0, false, matchedText);
                return true;
            case DictationTargetTextAction.MoveAfter:
                result = new DictationTargetTextResult(text, start + length, 0, false, matchedText);
                return true;
            case DictationTargetTextAction.InsertBefore:
                return TryInsertNearTarget(text, start, command.ReplacementText, true, matchedText, out result);
            case DictationTargetTextAction.InsertAfter:
                return TryInsertNearTarget(text, start + length, command.ReplacementText, false, matchedText, out result);
            case DictationTargetTextAction.Delete:
                result = new DictationTargetTextResult(text.Remove(start, length), start, 0, true, matchedText);
                return true;
            case DictationTargetTextAction.Replace:
                if (string.IsNullOrWhiteSpace(command.ReplacementText))
                    return false;

                var replacement = command.ReplacementText.Trim();
                result = new DictationTargetTextResult(
                    text.Remove(start, length).Insert(start, replacement),
                    start,
                    replacement.Length,
                    true,
                    matchedText);
                return true;
            default:
                return false;
        }
    }

    private static bool TryInsertNearTarget(
        string text,
        int insertionIndex,
        string insertionText,
        bool beforeTarget,
        string matchedText,
        out DictationTargetTextResult result)
    {
        result = new DictationTargetTextResult(text, 0, 0, false, matchedText);
        if (string.IsNullOrWhiteSpace(insertionText))
            return false;

        var insertion = BuildContextualInsertion(text, insertionIndex, insertionText.Trim(), beforeTarget);
        var insertedText = text.Insert(insertionIndex, insertion);
        var selectionStart = insertionIndex + Math.Max(0, insertion.IndexOf(insertionText.Trim(), StringComparison.Ordinal));
        result = new DictationTargetTextResult(
            insertedText,
            selectionStart,
            insertionText.Trim().Length,
            true,
            matchedText);
        return true;
    }

    private static string BuildContextualInsertion(string text, int insertionIndex, string insertion, bool beforeTarget)
    {
        if (string.IsNullOrEmpty(insertion) || IsPunctuationOnly(insertion))
            return insertion;

        var prefix = string.Empty;
        var suffix = string.Empty;
        if (!beforeTarget && insertionIndex > 0 && !char.IsWhiteSpace(text[insertionIndex - 1]))
            prefix = " ";

        if (beforeTarget && insertionIndex < text.Length && !char.IsWhiteSpace(text[insertionIndex]))
            suffix = " ";

        return prefix + insertion + suffix;
    }

    private static bool IsPunctuationOnly(string value)
    {
        foreach (var character in value)
        {
            if (!char.IsPunctuation(character) && !char.IsSymbol(character))
                return false;
        }

        return value.Length > 0;
    }

    public static bool TryFindPhraseSpan(string text, string phrase, out int start, out int length)
    {
        start = 0;
        length = 0;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
            return false;

        var targetWords = ExtractWords(phrase).Select(word => word.Value).ToArray();
        if (targetWords.Length == 0)
            return false;

        var words = ExtractWords(text).ToArray();
        if (words.Length < targetWords.Length)
            return false;

        for (var index = 0; index <= words.Length - targetWords.Length; index++)
        {
            var matched = true;
            for (var offset = 0; offset < targetWords.Length; offset++)
            {
                if (!string.Equals(words[index + offset].Value, targetWords[offset], StringComparison.OrdinalIgnoreCase))
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
                continue;

            start = words[index].Start;
            var end = words[index + targetWords.Length - 1].Start + words[index + targetWords.Length - 1].Length;
            length = end - start;
            return true;
        }

        return false;
    }

    public static bool TryFindPhraseRangeSpan(string text, string startPhrase, string endPhrase, out int start, out int length)
    {
        start = 0;
        length = 0;
        if (string.IsNullOrWhiteSpace(text)
            || string.IsNullOrWhiteSpace(startPhrase)
            || string.IsNullOrWhiteSpace(endPhrase))
        {
            return false;
        }

        var startWords = ExtractWords(startPhrase).Select(word => word.Value).ToArray();
        var endWords = ExtractWords(endPhrase).Select(word => word.Value).ToArray();
        if (startWords.Length == 0 || endWords.Length == 0)
            return false;

        var words = ExtractWords(text).ToArray();
        if (words.Length < startWords.Length || words.Length < endWords.Length)
            return false;

        for (var startIndex = 0; startIndex <= words.Length - startWords.Length; startIndex++)
        {
            if (!WordsMatchAt(words, startWords, startIndex))
                continue;

            var endSearchStart = startIndex + startWords.Length;
            if (SamePhrase(startWords, endWords))
                endSearchStart = startIndex + startWords.Length;

            for (var endIndex = endSearchStart; endIndex <= words.Length - endWords.Length; endIndex++)
            {
                if (!WordsMatchAt(words, endWords, endIndex))
                    continue;

                start = words[startIndex].Start;
                var end = words[endIndex + endWords.Length - 1].Start + words[endIndex + endWords.Length - 1].Length;
                length = end - start;
                return length > 0;
            }
        }

        return false;
    }

    private static bool WordsMatchAt(IReadOnlyList<DictationWordSpan> words, IReadOnlyList<string> targetWords, int index)
    {
        if (index < 0 || index + targetWords.Count > words.Count)
            return false;

        for (var offset = 0; offset < targetWords.Count; offset++)
        {
            if (!string.Equals(words[index + offset].Value, targetWords[offset], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool SamePhrase(IReadOnlyList<string> left, IReadOnlyList<string> right)
        => left.Count == right.Count && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<DictationWordSpan> ExtractWords(string text)
    {
        foreach (Match match in Regex.Matches(text, "[A-Za-z0-9]+"))
            yield return new DictationWordSpan(match.Value.ToLowerInvariant(), match.Index, match.Length);
    }

    private sealed record DictationWordSpan(string Value, int Start, int Length);
}
