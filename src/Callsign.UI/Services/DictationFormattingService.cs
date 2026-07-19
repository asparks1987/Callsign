using System.Globalization;

namespace Callsign.UI.Services;

public sealed record DictationFormatResult(
    string Text,
    int SelectionStart,
    int SelectionLength);

public static class DictationFormattingService
{
    public static bool TryApply(string text, DictationFormatCommand command, out DictationFormatResult result)
    {
        result = new DictationFormatResult(text, 0, 0);
        var (start, length) = GetSpan(text, command);
        if (length <= 0 || start < 0 || start + length > text.Length)
            return false;

        var original = text.Substring(start, length);
        var formatted = ApplyFormat(original, command.Format);
        if (string.IsNullOrWhiteSpace(formatted))
            return false;

        var updatedText = text.Remove(start, length).Insert(start, formatted);
        result = new DictationFormatResult(updatedText, start, formatted.Length);
        return true;
    }

    public static string FormatDescription(DictationFormatCommand command)
    {
        var format = command.Format switch
        {
            DictationTextFormat.SentenceCase => "capitalize",
            DictationTextFormat.TitleCase => "title case",
            DictationTextFormat.Uppercase => "uppercase",
            DictationTextFormat.Lowercase => "lowercase",
            _ => "format"
        };

        var scope = !string.IsNullOrWhiteSpace(command.TargetText)
            ? $"'{command.TargetText}'"
            : command.Scope switch
        {
            DictationReplacementScope.PreviousSentence => "the previous sentence",
            DictationReplacementScope.PreviousParagraph => "the previous paragraph",
            DictationReplacementScope.AllText => "all dictated text",
            _ => "the previous word"
        };

        return $"{format} {scope}";
    }

    private static string ApplyFormat(string text, DictationTextFormat format) =>
        format switch
        {
            DictationTextFormat.SentenceCase => ToSentenceCase(text),
            DictationTextFormat.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower(CultureInfo.CurrentCulture)),
            DictationTextFormat.Uppercase => text.ToUpper(CultureInfo.CurrentCulture),
            DictationTextFormat.Lowercase => text.ToLower(CultureInfo.CurrentCulture),
            _ => text
        };

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

    private static (int Start, int Length) GetSpan(string text, DictationFormatCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.TargetText))
        {
            return DictationTargetTextService.TryFindPhraseSpan(text, command.TargetText, out var start, out var length)
                ? (start, length)
                : (0, 0);
        }

        return command.Scope switch
        {
            DictationReplacementScope.PreviousSentence => GetLastSentenceSpan(text),
            DictationReplacementScope.PreviousParagraph => GetCurrentParagraphSpan(text),
            DictationReplacementScope.AllText => string.IsNullOrWhiteSpace(text) ? (0, 0) : (0, text.TrimEnd().Length),
            _ => GetLastWordSpan(text)
        };
    }

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
