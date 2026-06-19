using System.Text.RegularExpressions;

namespace Callsign.UI.Services;

public enum DictationVoiceAction
{
    None,
    Copy,
    Paste,
    Clear,
    SelectAll,
    Cut,
    Undo,
    Redo,
    GoToStart,
    GoToEnd,
    SelectToStart,
    SelectToEnd,
    DeleteToStart,
    DeleteToEnd,
    GoToLineStart,
    GoToLineEnd,
    SelectToLineStart,
    SelectToLineEnd,
    DeleteToLineStart,
    DeleteToLineEnd,
    GoToParagraphStart,
    GoToParagraphEnd,
    SelectToParagraphStart,
    SelectToParagraphEnd,
    DeleteToParagraphStart,
    DeleteToParagraphEnd,
    NewLine,
    NewParagraph,
    DeleteLastWord,
    SelectPreviousWord,
    SelectNextWord,
    DeletePreviousWord,
    SelectPreviousSentence,
    SelectNextSentence,
    DeletePreviousSentence,
    Comma,
    Period,
    QuestionMark,
    ExclamationMark,
    Semicolon,
    Colon,
    Apostrophe
}

public enum DictationReplacementScope
{
    None,
    PreviousWord,
    PreviousSentence,
    PreviousParagraph,
    AllText
}

public sealed record DictationReplacementCommand(DictationReplacementScope Scope, string ReplacementText);
public sealed record DictationSpellingCommand(string Text);

public static class AlphaVoiceTranscriptParser
{
    public static bool ContainsSpeechPhrase(string transcript, string phrase)
    {
        var normalizedTranscript = NormalizeSpeechText(transcript);
        var normalizedPhrase = NormalizeSpeechText(phrase);
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
            return false;

        if (ContainsWithBoundaries(normalizedTranscript, normalizedPhrase))
            return true;

        var compactTranscript = RemoveSpeechSeparators(normalizedTranscript);
        var compactPhrase = RemoveSpeechSeparators(normalizedPhrase);
        return compactTranscript.Contains(compactPhrase, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsWakeWord(string transcript, string wakeWord) =>
        ContainsSpeechPhrase(transcript, wakeWord)
        || ContainsSpeechPhrase(transcript, "call sign")
        || ContainsSpeechPhrase(transcript, "paul sign")
        || ContainsSpeechPhrase(transcript, "wall sign")
        || ContainsSpeechPhrase(transcript, "cold sign")
        || ContainsSpeechPhrase(transcript, "call science");

    public static string ExtractCommandFromTranscript(string transcript, string wakeWord, string callsign)
    {
        var command = NormalizeSpeechText(transcript);
        command = RemoveFirstSpeechPhrase(command, wakeWord);
        command = RemoveFirstSpeechPhrase(command, "call sign");
        command = RemoveFirstSpeechPhrase(command, "paul sign");
        command = RemoveFirstSpeechPhrase(command, "wall sign");
        command = RemoveFirstSpeechPhrase(command, "cold sign");
        command = RemoveFirstSpeechPhrase(command, "call science");
        command = RemoveFirstSpeechPhrase(command, callsign);
        return command.Trim();
    }

    public static string NormalizeLaunchCommand(string command)
    {
        var normalized = command.Trim();
        var prefixes = new[]
        {
            "to open ",
            "to launch ",
            "to start ",
            "to run ",
            "to ",
            "please ",
            "can you ",
            "could you ",
            "would you ",
            "i want you to "
        };

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var prefix in prefixes)
            {
                if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                normalized = normalized[prefix.Length..].Trim();
                changed = true;
                break;
            }
        }

        var trim = true;
        while (trim)
        {
            trim = false;
            foreach (var suffix in new[]
                     {
                         " please please",
                         " please",
                         " thanks",
                         " thank you"
                     })
            {
                if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    continue;

                normalized = normalized[..^suffix.Length].Trim();
                trim = true;
                break;
            }
        }

        if (normalized is "please" or "thanks" or "thank you")
            return string.Empty;

        return normalized;
    }

    public static bool IsCancelCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "cancel"
            or "callsign cancel"
            or "call sign cancel"
            or "cancel session"
            or "callsign cancel session"
            or "call sign cancel session"
            or "never mind"
            or "callsign never mind"
            or "call sign never mind"
            or "nevermind"
            or "callsign nevermind"
            or "call sign nevermind"
            or "stop command"
            or "callsign stop command"
            or "call sign stop command";
    }

    public static bool IsStopListeningCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "stop listening" or "callsign stop listening" or "call sign stop listening";
    }

    public static bool IsStopDictationCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "stop dictation"
            or "callsign stop dictation"
            or "call sign stop dictation"
            or "end dictation"
            or "finish dictation";
    }

    public static DictationVoiceAction ParseDictationVoiceAction(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized switch
        {
            "copy dictation" or "copy dictated text" or "copy dictation text" or "copy that" or "copy text" or "copy transcript" or "copy the text" => DictationVoiceAction.Copy,
            "paste dictation" or "paste dictated text" or "paste dictation text" or "paste that" or "paste text" or "paste transcript" or "paste the text" => DictationVoiceAction.Paste,
            "clear dictation" or "clear dictated text" or "clear dictation text" or "clear that" or "clear text" or "erase text" or "delete text" => DictationVoiceAction.Clear,
            "select all" or "select all dictation" or "select all text" or "highlight all" or "select everything" => DictationVoiceAction.SelectAll,
            "cut dictation" or "cut dictated text" or "cut dictation text" or "cut text" or "cut that" or "cut transcript" => DictationVoiceAction.Cut,
            "undo dictation" or "undo dictated text" or "undo that" or "undo text" or "revert that" => DictationVoiceAction.Undo,
            "redo dictation" or "redo dictated text" or "redo that" or "redo text" or "do that again" => DictationVoiceAction.Redo,
            "go to start" or "move to start" or "start of dictation" or "go to the start" or "move to the start" => DictationVoiceAction.GoToStart,
            "go to end" or "move to end" or "end of dictation" or "go to the end" or "move to the end" => DictationVoiceAction.GoToEnd,
            "select to start" or "select from here to start" or "select from here to the start" or "select to the beginning" => DictationVoiceAction.SelectToStart,
            "select to end" or "select from here to end" or "select from here to the end" or "select to the finish" => DictationVoiceAction.SelectToEnd,
            "delete to start" or "delete from here to start" or "delete from here to the start" or "delete to the beginning" => DictationVoiceAction.DeleteToStart,
            "delete to end" or "delete from here to end" or "delete from here to the end" or "delete to the finish" => DictationVoiceAction.DeleteToEnd,
            "go to line start" or "move to line start" or "start of line" or "go to the start of the line" or "move to the start of the line" or "line start" => DictationVoiceAction.GoToLineStart,
            "go to line end" or "move to line end" or "end of line" or "go to the end of the line" or "move to the end of the line" or "line end" => DictationVoiceAction.GoToLineEnd,
            "select to line start" or "select to the start of the line" or "select from here to the start of the line" => DictationVoiceAction.SelectToLineStart,
            "select to line end" or "select to the end of the line" or "select from here to the end of the line" => DictationVoiceAction.SelectToLineEnd,
            "delete to line start" or "delete to the start of the line" or "delete from here to the start of the line" => DictationVoiceAction.DeleteToLineStart,
            "delete to line end" or "delete to the end of the line" or "delete from here to the end of the line" => DictationVoiceAction.DeleteToLineEnd,
            "go to paragraph start" or "move to paragraph start" or "start of paragraph" or "go to the start of the paragraph" or "move to the start of the paragraph" => DictationVoiceAction.GoToParagraphStart,
            "go to paragraph end" or "move to paragraph end" or "end of paragraph" or "go to the end of the paragraph" or "move to the end of the paragraph" => DictationVoiceAction.GoToParagraphEnd,
            "select to paragraph start" or "select to the start of the paragraph" or "select from here to the start of the paragraph" => DictationVoiceAction.SelectToParagraphStart,
            "select to paragraph end" or "select to the end of the paragraph" or "select from here to the end of the paragraph" => DictationVoiceAction.SelectToParagraphEnd,
            "delete to paragraph start" or "delete to the start of the paragraph" or "delete from here to the start of the paragraph" => DictationVoiceAction.DeleteToParagraphStart,
            "delete to paragraph end" or "delete to the end of the paragraph" or "delete from here to the end of the paragraph" => DictationVoiceAction.DeleteToParagraphEnd,
            "new line" or "new dictation line" or "line break" or "next line" => DictationVoiceAction.NewLine,
            "new paragraph" or "paragraph break" or "new dictation paragraph" => DictationVoiceAction.NewParagraph,
            "delete last word" or "backspace word" or "remove last word" => DictationVoiceAction.DeleteLastWord,
            "select previous word" or "select the previous word" or "highlight previous word" => DictationVoiceAction.SelectPreviousWord,
            "select next word" or "select the next word" or "highlight next word" => DictationVoiceAction.SelectNextWord,
            "delete previous word" or "remove previous word" or "backspace previous word" => DictationVoiceAction.DeletePreviousWord,
            "select previous sentence" or "select the previous sentence" or "highlight previous sentence" => DictationVoiceAction.SelectPreviousSentence,
            "select next sentence" or "select the next sentence" or "highlight next sentence" => DictationVoiceAction.SelectNextSentence,
            "delete previous sentence" or "remove previous sentence" or "backspace previous sentence" => DictationVoiceAction.DeletePreviousSentence,
            "comma" or "comma please" => DictationVoiceAction.Comma,
            "period" or "full stop" or "dot" or "period please" => DictationVoiceAction.Period,
            "question mark" or "question" => DictationVoiceAction.QuestionMark,
            "exclamation mark" or "exclamation point" or "bang" => DictationVoiceAction.ExclamationMark,
            "semicolon" => DictationVoiceAction.Semicolon,
            "colon" => DictationVoiceAction.Colon,
            "apostrophe" or "single quote" => DictationVoiceAction.Apostrophe,
            _ => DictationVoiceAction.None
        };
    }

    public static bool TryParseDictationReplacementCommand(string transcript, out DictationReplacementCommand? command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var patterns = new (string Pattern, DictationReplacementScope Scope)[]
        {
            (@"^\s*(?:replace|change|correct)\s+(?:the previous word|previous word|last word|that word)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousWord),
            (@"^\s*(?:replace|change|correct)\s+(?:that|this|that sentence|this sentence|the previous sentence|previous sentence|last sentence|the previous phrase|previous phrase|last phrase)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousSentence),
            (@"^\s*(?:replace|change|correct)\s+(?:that|this|that paragraph|this paragraph|the previous paragraph|previous paragraph|last paragraph|that section|this section|the previous section|previous section|last section)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousParagraph),
            (@"^\s*(?:replace|change|correct)\s+(?:all|everything|the text|dictation text)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.AllText)
        };

        foreach (var (pattern, scope) in patterns)
        {
            var match = Regex.Match(
                transcript,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));

            if (!match.Success)
                continue;

            var replacementText = match.Groups["text"].Value.Trim();
            if (string.IsNullOrWhiteSpace(replacementText))
                return false;

            command = new DictationReplacementCommand(scope, replacementText);
            return true;
        }

        return false;
    }

    public static bool TryParseDictationSpellingCommand(string transcript, out DictationSpellingCommand? command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var normalized = NormalizeSpeechText(transcript);
        var prefixes = new[]
        {
            "spell out",
            "spell it out",
            "spell",
            "type out",
            "type",
            "insert",
            "dictate out",
            "dictate"
        };

        var matchedPrefix = string.Empty;
        foreach (var prefix in prefixes)
        {
            if (normalized.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                matchedPrefix = prefix;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(matchedPrefix))
            return false;

        var remainder = normalized[matchedPrefix.Length..].Trim();
        remainder = RemoveLeadingSpellingWords(remainder);
        if (string.IsNullOrWhiteSpace(remainder))
            return false;

        if (TryBuildSpelledText(remainder, out var spelledText))
        {
            command = new DictationSpellingCommand(spelledText);
            return true;
        }

        return false;
    }

    public static string? InferAppName(string command)
    {
        var normalized = command.Trim();
        var prefixes = new[]
        {
            "launch the application called ",
            "launch the application named ",
            "launch the app called ",
            "launch the app named ",
            "launch application ",
            "launch app ",
            "launch the application ",
            "launch the app ",
            "open the application called ",
            "open the application named ",
            "open the app called ",
            "open the app named ",
            "open application ",
            "open app ",
            "open the application ",
            "open the app ",
            "open up ",
            "open up the app ",
            "open up the application ",
            "launch ",
            "open ",
            "start ",
            "run "
        };
        foreach (var prefix in prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return TrimPoliteSuffix(normalized[prefix.Length..].Trim());
        }

        return TrimPoliteSuffix(normalized);
    }

    public static string NormalizeSpeechText(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

    private static bool ContainsWithBoundaries(string transcript, string phrase)
    {
        var normalizedTranscript = $" {transcript} ";
        var normalizedPhrase = $" {phrase} ";
        return normalizedTranscript.Contains(normalizedPhrase, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveSpeechSeparators(string value) =>
        Regex.Replace(value, @"\s+", string.Empty);

    private static string RemoveSpeechPhrase(string transcript, string phrase)
    {
        var normalizedPhrase = NormalizeSpeechText(phrase);
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
            return transcript;

        return Regex.Replace(
            $" {transcript} ",
            $@"\s{Regex.Escape(normalizedPhrase)}\s",
            " ",
            RegexOptions.IgnoreCase).Trim();
    }

    private static string RemoveFirstSpeechPhrase(string transcript, string phrase)
    {
        var normalizedPhrase = NormalizeSpeechText(phrase);
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
            return transcript;

        var regex = new Regex(
            $@"\s{Regex.Escape(normalizedPhrase)}\s",
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
        return regex.Replace(
            $" {transcript} ",
            " ",
            1).Trim();
    }

    private static string RemoveLeadingSpellingWords(string value)
    {
        var normalized = value.Trim();
        var prefixes = new[]
        {
            "the letters ",
            "the letter ",
            "letters ",
            "letter ",
            "the characters ",
            "the character ",
            "characters ",
            "character ",
            "out "
        };

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var prefix in prefixes)
            {
                if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                normalized = normalized[prefix.Length..].Trim();
                changed = true;
                break;
            }
        }

        return normalized;
    }

    private static bool TryBuildSpelledText(string value, out string spelledText)
    {
        spelledText = string.Empty;
        var tokens = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
            return false;

        if (tokens.Length == 1)
        {
            if (TryMapSpellingToken(tokens[0], out var singleToken))
            {
                spelledText = singleToken;
                return true;
            }

            spelledText = tokens[0];
            return true;
        }

        var builder = new System.Text.StringBuilder();
        foreach (var token in tokens)
        {
            if (IsSpellingFillerWord(token))
                continue;

            if (!TryMapSpellingToken(token, out var mapped))
                return false;

            builder.Append(mapped);
        }

        spelledText = builder.ToString();
        return spelledText.Length > 0;
    }

    private static bool IsSpellingFillerWord(string token) =>
        token is "spell" or "type" or "insert" or "dictate" or "out" or "please";

    private static bool TryMapSpellingToken(string token, out string mapped)
    {
        mapped = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.Length == 1 && char.IsLetterOrDigit(token[0]))
        {
            mapped = token;
            return true;
        }

        mapped = token switch
        {
            "alpha" => "a",
            "bravo" => "b",
            "charlie" => "c",
            "delta" => "d",
            "echo" => "e",
            "foxtrot" => "f",
            "golf" => "g",
            "hotel" => "h",
            "india" => "i",
            "juliet" => "j",
            "kilo" => "k",
            "lima" => "l",
            "mike" => "m",
            "november" => "n",
            "oscar" => "o",
            "papa" => "p",
            "quebec" => "q",
            "romeo" => "r",
            "sierra" => "s",
            "tango" => "t",
            "uniform" => "u",
            "victor" => "v",
            "whiskey" => "w",
            "xray" => "x",
            "x" => "x",
            "yankee" => "y",
            "zulu" => "z",
            "zero" or "oh" => "0",
            "one" => "1",
            "two" => "2",
            "three" => "3",
            "four" => "4",
            "five" => "5",
            "six" => "6",
            "seven" => "7",
            "eight" => "8",
            "nine" => "9",
            "space" => " ",
            "dash" or "hyphen" or "minus" => "-",
            "underscore" => "_",
            "period" or "dot" => ".",
            "slash" => "/",
            "backslash" => "\\",
            "apostrophe" or "quote" or "singlequote" => "'",
            "at" => "@",
            "ampersand" => "&",
            "percent" => "%",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(mapped);
    }

    private static string TrimPoliteSuffix(string value)
    {
        foreach (var suffix in new[] { " please", " thanks", " thank you" })
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return value[..^suffix.Length].Trim();
        }

        return value;
    }
}
