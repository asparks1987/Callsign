using System.Globalization;
using System.Text.RegularExpressions;

namespace Callsign.UI.Services;

public enum DictationVoiceAction
{
    None,
    Copy,
    ReadBack,
    StopReadBack,
    Paste,
    Clear,
    SelectAll,
    Cut,
    Undo,
    Redo,
    SelectThat,
    DeleteThat,
    GoToStart,
    GoToEnd,
    SelectToStart,
    SelectToEnd,
    DeleteToStart,
    DeleteToEnd,
    GoToLineStart,
    GoToLineEnd,
    GoToPreviousLine,
    GoToNextLine,
    SelectToLineStart,
    SelectToLineEnd,
    DeleteToLineStart,
    DeleteToLineEnd,
    SelectPreviousLine,
    SelectNextLine,
    DeletePreviousLine,
    DeleteNextLine,
    GoToParagraphStart,
    GoToParagraphEnd,
    SelectToParagraphStart,
    SelectToParagraphEnd,
    DeleteToParagraphStart,
    DeleteToParagraphEnd,
    NewLine,
    NewParagraph,
    NewSentence,
    Tab,
    DeleteLastWord,
    GoToPreviousWord,
    GoToNextWord,
    SelectPreviousWord,
    SelectNextWord,
    DeletePreviousWord,
    DeleteNextWord,
    SelectPreviousCharacter,
    SelectNextCharacter,
    DeletePreviousCharacter,
    DeleteNextCharacter,
    GoToPreviousSentence,
    GoToNextSentence,
    SelectPreviousSentence,
    SelectNextSentence,
    DeletePreviousSentence,
    DeleteNextSentence,
    GoToPreviousParagraph,
    GoToNextParagraph,
    SelectPreviousParagraph,
    SelectNextParagraph,
    DeletePreviousParagraph,
    DeleteNextParagraph,
    Comma,
    Period,
    QuestionMark,
    ExclamationMark,
    Semicolon,
    Colon,
    Apostrophe,
    Quote,
    QuoteThat,
    OpenParenthesis,
    CloseParenthesis,
    ParenthesizeThat,
    OpenBracket,
    CloseBracket,
    BracketThat,
    OpenBrace,
    CloseBrace,
    BraceThat,
    Hyphen,
    Dash,
    Slash,
    Backslash,
    Pipe,
    Grave,
    Tilde,
    Underscore,
    Plus,
    Equals,
    Hash,
    Dollar,
    Ampersand,
    Percent,
    Caret,
    Asterisk,
    AtSign,
    Space,
    NoSpaceThat
}

public enum DictationReplacementScope
{
    None,
    PreviousWord,
    PreviousSentence,
    PreviousParagraph,
    AllText
}

public enum DictationCorrectionVoiceAction
{
    None,
    ShowAlternatives,
    ChooseAlternative,
    PreviousAlternative,
    NextAlternative,
    AcceptCurrentAlternative,
    CancelAlternatives
}

public enum DictationTextFormat
{
    None,
    SentenceCase,
    TitleCase,
    Uppercase,
    Lowercase
}

public enum DictationCasingMode
{
    Default,
    Caps,
    AllCaps,
    NoCaps
}

public enum DictationTargetTextAction
{
    None,
    Select,
    Delete,
    Replace,
    MoveBefore,
    MoveAfter,
    InsertBefore,
    InsertAfter
}

public sealed record DictationReplacementCommand(DictationReplacementScope Scope, string ReplacementText);
public sealed record DictationSpellingCommand(string Text);
public sealed record DictationTargetTextCommand(
    DictationTargetTextAction Action,
    string TargetText,
    string ReplacementText = "",
    string EndText = "");
public sealed record DictationCorrectionCommand(DictationCorrectionVoiceAction Action, DictationReplacementScope Scope, int ChoiceNumber = 0);
public sealed record DictationFormatCommand(DictationReplacementScope Scope, DictationTextFormat Format, string TargetText = "");
public sealed record DictationCasingCommand(DictationCasingMode Mode);

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
        command = RemoveLeadingSpeechPhrase(command, wakeWord);
        command = RemoveLeadingSpeechPhrase(command, "call sign");
        command = RemoveLeadingSpeechPhrase(command, "paul sign");
        command = RemoveLeadingSpeechPhrase(command, "wall sign");
        command = RemoveLeadingSpeechPhrase(command, "cold sign");
        command = RemoveLeadingSpeechPhrase(command, "call science");
        command = RemoveLeadingSpeechPhrase(command, callsign);
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
            or "stop"
            or "callsign stop"
            or "call sign stop"
            or "stop now"
            or "callsign stop now"
            or "call sign stop now"
            or "pause"
            or "callsign pause"
            or "call sign pause"
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
        return normalized is "stop listening"
            or "callsign stop listening"
            or "call sign stop listening"
            or "stop voice"
            or "callsign stop voice"
            or "call sign stop voice"
            or "voice access sleep"
            or "go to sleep"
            or "turn off microphone"
            or "turn off mic"
            or "turn off voice access"
            or "stop voice access"
            or "close voice access"
            or "exit voice access"
            or "quit voice access"
            or "mute microphone"
            or "mute mic"
            or "microphone off"
            or "mic off";
    }

    public static bool IsStopDictationCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "stop dictation"
            or "stop taking dictation"
            or "stop typing"
            or "stop voice typing"
            or "callsign stop dictation"
            or "call sign stop dictation"
            or "end dictation"
            or "end typing"
            or "finish dictation"
            or "finish typing";
    }

    public static bool IsPauseDictationCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "pause dictation"
            or "pause voice dictation"
            or "pause typing"
            or "pause voice typing"
            or "pause taking dictation"
            or "hold dictation"
            or "hold typing"
            or "callsign pause dictation"
            or "call sign pause dictation";
    }

    public static bool TryParseDictationCasingCommand(string transcript, out DictationCasingCommand? command)
    {
        command = null;
        var normalized = NormalizeSpeechText(transcript);
        var mode = normalized switch
        {
            "caps on" or "capitalize on" or "capital letters on" => DictationCasingMode.Caps,
            "all caps on" or "uppercase on" or "upper case on" => DictationCasingMode.AllCaps,
            "no caps on" or "lowercase on" or "lower case on" => DictationCasingMode.NoCaps,
            "caps off" or "all caps off" or "no caps off" or "capitalize off" or "uppercase off" or "upper case off" or "lowercase off" or "lower case off" or "normal caps" or "normal case" or "default case" => DictationCasingMode.Default,
            _ => DictationCasingMode.Default
        };

        if (mode == DictationCasingMode.Default
            && normalized is not ("caps off" or "all caps off" or "no caps off" or "capitalize off" or "uppercase off" or "upper case off" or "lowercase off" or "lower case off" or "normal caps" or "normal case" or "default case"))
            return false;

        command = new DictationCasingCommand(mode);
        return true;
    }

    public static DictationVoiceAction ParseDictationVoiceAction(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized switch
        {
            "copy dictation" or "copy dictated text" or "copy dictation text" or "copy that" or "copy text" or "copy transcript" or "copy the text" => DictationVoiceAction.Copy,
            "read back" or "read that back" or "read dictation" or "read dictated text" or "read dictation text" or "read the text" or "speak dictation" or "speak dictated text" or "speak text" => DictationVoiceAction.ReadBack,
            "stop reading" or "stop readback" or "stop reading dictation" or "stop speaking" or "stop speaking text" or "cancel readback" or "silence readback" => DictationVoiceAction.StopReadBack,
            "paste dictation" or "paste dictated text" or "paste dictation text" or "paste that" or "paste text" or "paste transcript" or "paste the text" => DictationVoiceAction.Paste,
            "clear dictation" or "clear dictated text" or "clear dictation text" or "clear that" or "clear text" or "erase text" or "delete text" or "delete all" or "delete everything" or "clear all" => DictationVoiceAction.Clear,
            "select all" or "select all dictation" or "select all text" or "highlight all" or "select everything" => DictationVoiceAction.SelectAll,
            "cut dictation" or "cut dictated text" or "cut dictation text" or "cut text" or "cut that" or "cut transcript" => DictationVoiceAction.Cut,
            "undo dictation" or "undo dictated text" or "undo that" or "undo text" or "revert that" or "revert" => DictationVoiceAction.Undo,
            "redo dictation" or "redo dictated text" or "redo that" or "redo text" or "do that again" => DictationVoiceAction.Redo,
            "select that" or "select last phrase" or "select previous phrase" or "highlight that" or "highlight last phrase" => DictationVoiceAction.SelectThat,
            "delete that" or "delete last phrase" or "delete previous phrase" or "remove that" or "scratch that" => DictationVoiceAction.DeleteThat,
            "go to start" or "move to start" or "start of dictation" or "go to the start" or "move to the start" or "go to beginning" or "go to the beginning" or "go to beginning of text" or "go to beginning of dictation" or "beginning of text" or "beginning of dictation" => DictationVoiceAction.GoToStart,
            "go to end" or "move to end" or "end of dictation" or "go to the end" or "move to the end" or "go to end of text" or "go to end of dictation" or "end of text" => DictationVoiceAction.GoToEnd,
            "select to start" or "select from here to start" or "select from here to the start" or "select to the beginning" or "select to beginning" or "select to beginning of text" or "select to beginning of dictation" => DictationVoiceAction.SelectToStart,
            "select to end" or "select from here to end" or "select from here to the end" or "select to the finish" or "select to end of text" or "select to end of dictation" => DictationVoiceAction.SelectToEnd,
            "delete to start" or "delete from here to start" or "delete from here to the start" or "delete to the beginning" or "delete to beginning" or "delete to beginning of text" or "delete to beginning of dictation" => DictationVoiceAction.DeleteToStart,
            "delete to end" or "delete from here to end" or "delete from here to the end" or "delete to the finish" or "delete to end of text" or "delete to end of dictation" => DictationVoiceAction.DeleteToEnd,
            "go to line start" or "move to line start" or "start of line" or "go to the start of the line" or "move to the start of the line" or "line start" => DictationVoiceAction.GoToLineStart,
            "go to line end" or "move to line end" or "end of line" or "go to the end of the line" or "move to the end of the line" or "line end" => DictationVoiceAction.GoToLineEnd,
            "go to previous line" or "move to previous line" or "previous line" or "go to last line" or "move to last line" or "last line" => DictationVoiceAction.GoToPreviousLine,
            "go to next line" or "move to next line" => DictationVoiceAction.GoToNextLine,
            "select to line start" or "select to the start of the line" or "select from here to the start of the line" => DictationVoiceAction.SelectToLineStart,
            "select to line end" or "select to the end of the line" or "select from here to the end of the line" => DictationVoiceAction.SelectToLineEnd,
            "delete to line start" or "delete to the start of the line" or "delete from here to the start of the line" => DictationVoiceAction.DeleteToLineStart,
            "delete to line end" or "delete to the end of the line" or "delete from here to the end of the line" => DictationVoiceAction.DeleteToLineEnd,
            "select previous line" or "select the previous line" or "highlight previous line" or "select last line" or "select the last line" or "highlight last line" => DictationVoiceAction.SelectPreviousLine,
            "select next line" or "select the next line" or "highlight next line" => DictationVoiceAction.SelectNextLine,
            "delete previous line" or "remove previous line" or "backspace previous line" or "delete last line" or "remove last line" or "backspace last line" => DictationVoiceAction.DeletePreviousLine,
            "delete next line" or "remove next line" => DictationVoiceAction.DeleteNextLine,
            "go to paragraph start" or "move to paragraph start" or "start of paragraph" or "go to the start of the paragraph" or "move to the start of the paragraph" => DictationVoiceAction.GoToParagraphStart,
            "go to paragraph end" or "move to paragraph end" or "end of paragraph" or "go to the end of the paragraph" or "move to the end of the paragraph" => DictationVoiceAction.GoToParagraphEnd,
            "select to paragraph start" or "select to the start of the paragraph" or "select from here to the start of the paragraph" => DictationVoiceAction.SelectToParagraphStart,
            "select to paragraph end" or "select to the end of the paragraph" or "select from here to the end of the paragraph" => DictationVoiceAction.SelectToParagraphEnd,
            "delete to paragraph start" or "delete to the start of the paragraph" or "delete from here to the start of the paragraph" => DictationVoiceAction.DeleteToParagraphStart,
            "delete to paragraph end" or "delete to the end of the paragraph" or "delete from here to the end of the paragraph" => DictationVoiceAction.DeleteToParagraphEnd,
            "new line" or "new dictation line" or "line break" or "next line" => DictationVoiceAction.NewLine,
            "new paragraph" or "paragraph break" or "new dictation paragraph" => DictationVoiceAction.NewParagraph,
            "new sentence" or "sentence break" or "end sentence" or "end the sentence" => DictationVoiceAction.NewSentence,
            "tab" or "tab key" or "insert tab" => DictationVoiceAction.Tab,
            "delete last word" or "backspace word" or "remove last word" => DictationVoiceAction.DeleteLastWord,
            "go to previous word" or "move to previous word" or "previous word" or "go to last word" or "move to last word" or "last word" => DictationVoiceAction.GoToPreviousWord,
            "go to next word" or "move to next word" or "next word" => DictationVoiceAction.GoToNextWord,
            "select previous word" or "select the previous word" or "highlight previous word" or "select last word" or "select the last word" or "highlight last word" => DictationVoiceAction.SelectPreviousWord,
            "select next word" or "select the next word" or "highlight next word" => DictationVoiceAction.SelectNextWord,
            "delete previous word" or "remove previous word" or "backspace previous word" or "delete last word" or "remove last word" or "backspace last word" => DictationVoiceAction.DeletePreviousWord,
            "delete next word" or "remove next word" => DictationVoiceAction.DeleteNextWord,
            "select previous character" or "select the previous character" or "select previous letter" or "highlight previous character" or "select last character" or "select last letter" or "highlight last character" => DictationVoiceAction.SelectPreviousCharacter,
            "select next character" or "select the next character" or "select next letter" or "highlight next character" => DictationVoiceAction.SelectNextCharacter,
            "delete previous character" or "remove previous character" or "backspace character" or "delete previous letter" or "backspace letter" or "delete last character" or "delete last letter" or "remove last character" => DictationVoiceAction.DeletePreviousCharacter,
            "delete next character" or "remove next character" or "delete next letter" => DictationVoiceAction.DeleteNextCharacter,
            "go to previous sentence" or "move to previous sentence" or "previous sentence" or "go to last sentence" or "move to last sentence" or "last sentence" => DictationVoiceAction.GoToPreviousSentence,
            "go to next sentence" or "move to next sentence" or "next sentence" => DictationVoiceAction.GoToNextSentence,
            "select previous sentence" or "select the previous sentence" or "highlight previous sentence" or "select last sentence" or "select the last sentence" or "highlight last sentence" => DictationVoiceAction.SelectPreviousSentence,
            "select next sentence" or "select the next sentence" or "highlight next sentence" => DictationVoiceAction.SelectNextSentence,
            "delete previous sentence" or "remove previous sentence" or "backspace previous sentence" or "delete last sentence" or "remove last sentence" or "backspace last sentence" => DictationVoiceAction.DeletePreviousSentence,
            "delete next sentence" or "remove next sentence" => DictationVoiceAction.DeleteNextSentence,
            "go to previous paragraph" or "move to previous paragraph" or "previous paragraph" or "go to last paragraph" or "move to last paragraph" or "last paragraph" => DictationVoiceAction.GoToPreviousParagraph,
            "go to next paragraph" or "move to next paragraph" or "next paragraph" => DictationVoiceAction.GoToNextParagraph,
            "select previous paragraph" or "select the previous paragraph" or "highlight previous paragraph" or "select previous section" or "select last paragraph" or "select the last paragraph" or "highlight last paragraph" or "select last section" => DictationVoiceAction.SelectPreviousParagraph,
            "select next paragraph" or "select the next paragraph" or "highlight next paragraph" or "select next section" => DictationVoiceAction.SelectNextParagraph,
            "delete previous paragraph" or "remove previous paragraph" or "backspace previous paragraph" or "delete previous section" or "delete last paragraph" or "remove last paragraph" or "backspace last paragraph" or "delete last section" => DictationVoiceAction.DeletePreviousParagraph,
            "delete next paragraph" or "remove next paragraph" or "delete next section" => DictationVoiceAction.DeleteNextParagraph,
            "comma" or "comma please" => DictationVoiceAction.Comma,
            "period" or "full stop" or "dot" or "period please" => DictationVoiceAction.Period,
            "question mark" or "question" => DictationVoiceAction.QuestionMark,
            "exclamation" or "exclamation mark" or "exclamation point" or "bang" => DictationVoiceAction.ExclamationMark,
            "semicolon" or "semi colon" => DictationVoiceAction.Semicolon,
            "colon" => DictationVoiceAction.Colon,
            "apostrophe" or "single quote" => DictationVoiceAction.Apostrophe,
            "quote" or "double quote" or "quotation mark" or "open quote" or "close quote" => DictationVoiceAction.Quote,
            "quote that" or "quote selection" or "put that in quotes" or "put selection in quotes" => DictationVoiceAction.QuoteThat,
            "open parenthesis" or "open parentheses" or "left parenthesis" or "left parentheses" or "open paren" or "left paren" => DictationVoiceAction.OpenParenthesis,
            "close parenthesis" or "close parentheses" or "right parenthesis" or "right parentheses" or "close paren" or "right paren" => DictationVoiceAction.CloseParenthesis,
            "parenthesize that" or "parenthesize selection" or "put that in parentheses" or "put selection in parentheses" => DictationVoiceAction.ParenthesizeThat,
            "open bracket" or "left bracket" or "open square bracket" or "left square bracket" => DictationVoiceAction.OpenBracket,
            "close bracket" or "right bracket" or "close square bracket" or "right square bracket" => DictationVoiceAction.CloseBracket,
            "bracket that" or "bracket selection" or "put that in brackets" or "put selection in brackets" => DictationVoiceAction.BracketThat,
            "open brace" or "left brace" or "open curly brace" or "left curly brace" => DictationVoiceAction.OpenBrace,
            "close brace" or "right brace" or "close curly brace" or "right curly brace" => DictationVoiceAction.CloseBrace,
            "brace that" or "brace selection" or "put that in braces" or "put selection in braces" or "put that in curly braces" => DictationVoiceAction.BraceThat,
            "hyphen" => DictationVoiceAction.Hyphen,
            "dash" or "em dash" => DictationVoiceAction.Dash,
            "slash" or "forward slash" => DictationVoiceAction.Slash,
            "backslash" or "back slash" => DictationVoiceAction.Backslash,
            "pipe" or "vertical bar" => DictationVoiceAction.Pipe,
            "grave" or "grave accent" or "backtick" or "back tick" => DictationVoiceAction.Grave,
            "tilde" => DictationVoiceAction.Tilde,
            "underscore" or "under score" => DictationVoiceAction.Underscore,
            "plus" or "plus sign" => DictationVoiceAction.Plus,
            "equals" or "equal sign" or "equals sign" => DictationVoiceAction.Equals,
            "hash" or "pound" or "number sign" => DictationVoiceAction.Hash,
            "dollar" or "dollar sign" => DictationVoiceAction.Dollar,
            "ampersand" or "and sign" => DictationVoiceAction.Ampersand,
            "percent" or "percent sign" => DictationVoiceAction.Percent,
            "caret" => DictationVoiceAction.Caret,
            "asterisk" or "star" => DictationVoiceAction.Asterisk,
            "at sign" or "at symbol" => DictationVoiceAction.AtSign,
            "space" or "space bar" or "insert space" => DictationVoiceAction.Space,
            "no space that" or "no space" or "remove space" or "delete space" or "remove space before that" or "remove space before previous word" or "join that" or "join previous word" => DictationVoiceAction.NoSpaceThat,
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
            (@"^\s*(?:replace|change|correct|fix)\s+(?:the previous word|previous word|last word|that word)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousWord),
            (@"^\s*(?:replace|change|correct|fix)\s+(?:that|this|that sentence|this sentence|the previous sentence|previous sentence|last sentence|the previous phrase|previous phrase|last phrase)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousSentence),
            (@"^\s*(?:replace|change|correct|fix)\s+(?:that|this|that paragraph|this paragraph|the previous paragraph|previous paragraph|last paragraph|that section|this section|the previous section|previous section|last section)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.PreviousParagraph),
            (@"^\s*(?:replace|change|correct|fix)\s+(?:all|all text|everything|the text|dictation text)\s+(?:with|to)\s+(?<text>.+?)\s*$", DictationReplacementScope.AllText)
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

            var replacementText = ConvertDictationReplacementText(match.Groups["text"].Value.Trim());
            if (string.IsNullOrWhiteSpace(replacementText))
                return false;

            command = new DictationReplacementCommand(scope, replacementText);
            return true;
        }

        return false;
    }

    public static bool TryParseDictationTargetTextCommand(string transcript, out DictationTargetTextCommand? command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var normalized = NormalizeSpeechText(transcript);
        var moveMatch = Regex.Match(
            normalized,
            @"^\s*(?:go|move|put cursor|place cursor)\s+(?<position>before|after)\s+(?:the\s+)?(?:(?:word|words|phrase|text)\s+)?(?<target>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (moveMatch.Success)
        {
            var target = moveMatch.Groups["target"].Value.Trim();
            if (IsValidDictationTargetText(target))
            {
                var position = moveMatch.Groups["position"].Value.Trim();
                var moveAction = position.Equals("before", StringComparison.OrdinalIgnoreCase)
                    ? DictationTargetTextAction.MoveBefore
                    : DictationTargetTextAction.MoveAfter;
                command = new DictationTargetTextCommand(moveAction, target);
                return true;
            }
        }

        var insertMatch = Regex.Match(
            normalized,
            @"^\s*(?:insert|add|put)\s+(?<text>.+?)\s+(?<position>before|after)\s+(?:the\s+)?(?:(?:word|words|phrase|text)\s+)?(?<target>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (insertMatch.Success)
        {
            var target = insertMatch.Groups["target"].Value.Trim();
            var insertion = insertMatch.Groups["text"].Value.Trim();
            if (IsValidDictationTargetText(target) && !string.IsNullOrWhiteSpace(insertion))
            {
                var position = insertMatch.Groups["position"].Value.Trim();
                var insertAction = position.Equals("before", StringComparison.OrdinalIgnoreCase)
                    ? DictationTargetTextAction.InsertBefore
                    : DictationTargetTextAction.InsertAfter;
                var insertionText = TryConvertLiteralOrNumeralDictationText(insertion, out var literalOrNumeralText)
                    ? literalOrNumeralText
                    : ConvertPlainDictationText(insertion);
                command = new DictationTargetTextCommand(insertAction, target, insertionText);
                return true;
            }
        }

        var rangeReplaceMatch = Regex.Match(
            normalized,
            @"^\s*(?:replace|change|correct|fix)\s+from\s+(?<start>.+?)\s+to\s+(?<end>.+?)\s+with\s+(?<replacement>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (rangeReplaceMatch.Success)
        {
            var startText = rangeReplaceMatch.Groups["start"].Value.Trim();
            var endText = rangeReplaceMatch.Groups["end"].Value.Trim();
            var replacement = ConvertDictationReplacementText(rangeReplaceMatch.Groups["replacement"].Value.Trim());
            if (IsValidDictationTargetText(startText)
                && IsValidDictationTargetText(endText)
                && !string.IsNullOrWhiteSpace(replacement))
            {
                command = new DictationTargetTextCommand(DictationTargetTextAction.Replace, startText, replacement, endText);
                return true;
            }
        }

        var rangeActionMatch = Regex.Match(
            normalized,
            @"^\s*(?<action>select|highlight|delete|remove)\s+from\s+(?<start>.+?)\s+to\s+(?<end>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (rangeActionMatch.Success)
        {
            var startText = rangeActionMatch.Groups["start"].Value.Trim();
            var endText = rangeActionMatch.Groups["end"].Value.Trim();
            if (IsValidDictationTargetText(startText) && IsValidDictationTargetText(endText))
            {
                var rangeActionText = rangeActionMatch.Groups["action"].Value.Trim();
                var rangeAction = rangeActionText is "select" or "highlight"
                    ? DictationTargetTextAction.Select
                    : DictationTargetTextAction.Delete;
                command = new DictationTargetTextCommand(rangeAction, startText, EndText: endText);
                return true;
            }
        }

        var replaceMatch = Regex.Match(
            normalized,
            @"^\s*(?:replace|change|correct|fix)\s+(?:the\s+)?(?:(?:word|words|phrase|text)\s+)?(?<target>.+?)\s+(?:with|to)\s+(?<replacement>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (replaceMatch.Success)
        {
            var target = replaceMatch.Groups["target"].Value.Trim();
            var replacement = ConvertDictationReplacementText(replaceMatch.Groups["replacement"].Value.Trim());
            if (IsValidDictationTargetText(target) && !string.IsNullOrWhiteSpace(replacement))
            {
                command = new DictationTargetTextCommand(DictationTargetTextAction.Replace, target, replacement);
                return true;
            }
        }

        var actionMatch = Regex.Match(
            normalized,
            @"^\s*(?<action>select|highlight|delete|remove)\s+(?:the\s+)?(?:(?:word|words|phrase|text)\s+)?(?<target>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!actionMatch.Success)
            return false;

        var targetText = actionMatch.Groups["target"].Value.Trim();
        if (!IsValidDictationTargetText(targetText))
            return false;

        var actionText = actionMatch.Groups["action"].Value.Trim();
        var action = actionText is "select" or "highlight"
            ? DictationTargetTextAction.Select
            : DictationTargetTextAction.Delete;
        command = new DictationTargetTextCommand(action, targetText);
        return true;
    }

    private static string ConvertDictationReplacementText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = NormalizeSpeechText(value);
        return TryConvertLiteralOrNumeralDictationText(normalized, out var literalOrNumeralText)
            ? literalOrNumeralText
            : ConvertPlainDictationText(normalized);
    }

    public static bool TryParseDictationCorrectionCommand(string transcript, out DictationCorrectionCommand? command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var normalized = NormalizeSpeechText(transcript);
        if (normalized is "cancel correction"
            or "cancel corrections"
            or "hide correction"
            or "hide corrections"
            or "dismiss correction"
            or "dismiss corrections"
            or "close correction"
            or "close corrections")
        {
            command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.CancelAlternatives, DictationReplacementScope.None);
            return true;
        }

        if (normalized is "previous correction"
            or "previous correction alternative"
            or "previous alternative"
            or "previous choice"
            or "correction previous")
        {
            command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.PreviousAlternative, DictationReplacementScope.None);
            return true;
        }

        if (normalized is "next correction"
            or "next correction alternative"
            or "next alternative"
            or "next choice"
            or "correction next")
        {
            command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.NextAlternative, DictationReplacementScope.None);
            return true;
        }

        if (normalized is "accept correction"
            or "apply correction"
            or "accept that"
            or "apply that"
            or "use that"
            or "accept selected correction"
            or "apply selected correction"
            or "choose selected correction"
            or "use selected correction"
            or "accept current correction"
            or "apply current correction"
            or "use current correction")
        {
            command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.AcceptCurrentAlternative, DictationReplacementScope.None);
            return true;
        }

        var chooseMatch = Regex.Match(
            normalized,
            @"^\s*(?:choose|pick|select|use)\s+(?:(?:correction|option|choice|alternative)\s+)?(?<number>one|two|three|four|five|six|seven|eight|nine|\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (chooseMatch.Success && TryParseSpokenNumber(chooseMatch.Groups["number"].Value, out var choiceNumber))
        {
            command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.ChooseAlternative, DictationReplacementScope.None, choiceNumber);
            return true;
        }

        var scope = normalized switch
        {
            "show corrections" or "show correction alternatives" or "correction alternatives" or "suggest corrections" => DictationReplacementScope.PreviousWord,
            "correct previous word" or "correct the previous word" or "correct last word" or "correct the last word" or "correct that word" or "fix previous word" or "fix the previous word" or "fix last word" or "fix the last word" or "fix that word" => DictationReplacementScope.PreviousWord,
            "correct that" or "correct this" or "correct previous sentence" or "correct the previous sentence" or "correct last sentence" or "correct the last sentence" or "correct previous phrase" or "correct the previous phrase" or "fix that" or "fix this" or "fix previous sentence" or "fix the previous sentence" or "fix last sentence" or "fix the last sentence" or "fix previous phrase" or "fix the previous phrase" => DictationReplacementScope.PreviousSentence,
            "correct previous paragraph" or "correct the previous paragraph" or "correct last paragraph" or "correct the last paragraph" or "correct previous section" or "correct the previous section" or "fix previous paragraph" or "fix the previous paragraph" or "fix last paragraph" or "fix the last paragraph" or "fix previous section" or "fix the previous section" => DictationReplacementScope.PreviousParagraph,
            "correct all" or "correct all text" or "correct everything" or "correct dictation" or "correct dictation text" or "fix all" or "fix all text" or "fix everything" or "fix dictation" or "fix dictation text" or "show corrections for all text" or "show alternatives for all text" => DictationReplacementScope.AllText,
            _ => DictationReplacementScope.None
        };

        if (scope == DictationReplacementScope.None)
            return false;

        command = new DictationCorrectionCommand(DictationCorrectionVoiceAction.ShowAlternatives, scope);
        return true;
    }

    public static bool TryParseDictationFormatCommand(string transcript, out DictationFormatCommand? command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var normalized = NormalizeSpeechText(transcript);
        if (TryParseMakeFormatCommand(normalized, out command))
            return true;

        var format = normalized switch
        {
            var value when value.StartsWith("capitalize ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.SentenceCase,
            var value when value.StartsWith("title case ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.TitleCase,
            var value when value.StartsWith("uppercase ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Uppercase,
            var value when value.StartsWith("upper case ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Uppercase,
            var value when value.StartsWith("all caps ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Uppercase,
            var value when value.StartsWith("lowercase ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Lowercase,
            var value when value.StartsWith("lower case ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Lowercase,
            var value when value.StartsWith("no caps ", StringComparison.OrdinalIgnoreCase) => DictationTextFormat.Lowercase,
            _ => DictationTextFormat.None
        };

        if (format == DictationTextFormat.None)
            return false;

        var target = normalized;
        foreach (var prefix in new[]
                 {
                     "capitalize ",
                     "title case ",
                     "uppercase ",
                     "upper case ",
                     "all caps ",
                     "lowercase ",
                     "lower case ",
                     "no caps "
                 })
        {
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            target = target[prefix.Length..].Trim();
                break;
        }

        var scope = ParseDictationFormatScope(target);

        if (scope == DictationReplacementScope.None)
        {
            target = StripOptionalDictationTargetPrefix(target);
            if (!IsValidDictationTargetText(target))
                return false;

            command = new DictationFormatCommand(DictationReplacementScope.None, format, target);
            return true;
        }

        command = new DictationFormatCommand(scope, format);
        return true;
    }

    private static bool TryParseMakeFormatCommand(string normalized, out DictationFormatCommand? command)
    {
        command = null;
        const string prefix = "make ";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var target = normalized[prefix.Length..].Trim();
        foreach (var (suffix, format) in new[]
                 {
                     ("title case", DictationTextFormat.TitleCase),
                     ("sentence case", DictationTextFormat.SentenceCase),
                     ("capitalized", DictationTextFormat.SentenceCase),
                     ("capitalize", DictationTextFormat.SentenceCase),
                     ("upper case", DictationTextFormat.Uppercase),
                     ("uppercase", DictationTextFormat.Uppercase),
                     ("all caps", DictationTextFormat.Uppercase),
                     ("lower case", DictationTextFormat.Lowercase),
                     ("lowercase", DictationTextFormat.Lowercase),
                     ("no caps", DictationTextFormat.Lowercase)
                 })
        {
            if (!target.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var formatTarget = target[..^suffix.Length].Trim();
            var scope = ParseDictationFormatScope(formatTarget);
            if (scope == DictationReplacementScope.None)
            {
                formatTarget = StripOptionalDictationTargetPrefix(formatTarget);
                if (!IsValidDictationTargetText(formatTarget))
                    return false;

                command = new DictationFormatCommand(DictationReplacementScope.None, format, formatTarget);
                return true;
            }

            command = new DictationFormatCommand(scope, format);
            return true;
        }

        return false;
    }

    private static DictationReplacementScope ParseDictationFormatScope(string target) =>
        target switch
        {
            "that" or "this" or "previous word" or "the previous word" or "last word" or "the last word" or "that word" or "this word" => DictationReplacementScope.PreviousWord,
            "previous sentence" or "the previous sentence" or "last sentence" or "the last sentence" or "that sentence" or "this sentence" or "previous phrase" or "the previous phrase" => DictationReplacementScope.PreviousSentence,
            "previous paragraph" or "the previous paragraph" or "last paragraph" or "the last paragraph" or "that paragraph" or "this paragraph" or "previous section" or "the previous section" => DictationReplacementScope.PreviousParagraph,
            "all" or "all text" or "everything" or "dictation" or "dictation text" or "the text" => DictationReplacementScope.AllText,
            _ => DictationReplacementScope.None
        };

    private static string StripOptionalDictationTargetPrefix(string target)
    {
        foreach (var prefix in new[] { "the word ", "the words ", "the phrase ", "the text ", "word ", "words ", "phrase ", "text " })
        {
            if (target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return target[prefix.Length..].Trim();
        }

        return target.Trim();
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
            "spell that out",
            "spell that",
            "spell this out",
            "spell this",
            "spell word",
            "spell the word",
            "spell phrase",
            "spell the phrase",
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

    public static bool TryParseDictationInsertTextCommand(string transcript, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(transcript))
            return false;

        var normalized = NormalizeSpeechText(transcript);
        if (TryConvertLiteralOrNumeralDictationText(normalized, out text))
            return true;

        var prefixes = new[]
        {
            "type the words ",
            "type text ",
            "type ",
            "write the words ",
            "write text ",
            "write ",
            "insert the words ",
            "insert text ",
            "dictate the words ",
            "dictate text ",
            "dictate "
        };

        foreach (var prefix in prefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = normalized[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(remainder))
                return false;

            if (prefix.Equals("type ", StringComparison.OrdinalIgnoreCase)
                && (remainder.StartsWith("letter ", StringComparison.OrdinalIgnoreCase)
                    || remainder.StartsWith("letters ", StringComparison.OrdinalIgnoreCase)
                    || remainder.StartsWith("character ", StringComparison.OrdinalIgnoreCase)
                    || remainder.StartsWith("characters ", StringComparison.OrdinalIgnoreCase)
                    || remainder.StartsWith("out ", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            text = TryConvertLiteralOrNumeralDictationText(remainder, out var literalOrNumeralText)
                ? literalOrNumeralText
                : ConvertPlainDictationText(remainder);
            return !string.IsNullOrWhiteSpace(text);
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
            "launch app called ",
            "launch app named ",
            "launch application ",
            "launch app ",
            "launch the application ",
            "launch the app ",
            "open the application called ",
            "open the application named ",
            "open the app called ",
            "open the app named ",
            "open app called ",
            "open app named ",
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

    private static bool IsValidDictationTargetText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return NormalizeSpeechText(value) is not (
            "that" or "this" or "all" or "all text" or "everything" or "dictation" or "dictation text" or "the text"
            or "previous word" or "the previous word" or "last word" or "the last word" or "that word" or "this word"
            or "previous sentence" or "the previous sentence" or "last sentence" or "the last sentence" or "that sentence" or "this sentence"
            or "previous phrase" or "the previous phrase" or "last phrase" or "the last phrase"
            or "previous paragraph" or "the previous paragraph" or "last paragraph" or "the last paragraph" or "that paragraph" or "this paragraph"
            or "previous section" or "the previous section" or "last section" or "the last section");
    }

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

    private static string RemoveLeadingSpeechPhrase(string transcript, string phrase)
    {
        var normalizedPhrase = NormalizeSpeechText(phrase);
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
            return transcript;

        if (string.Equals(transcript, normalizedPhrase, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var prefix = normalizedPhrase + " ";
        return transcript.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? transcript[prefix.Length..].Trim()
            : transcript;
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
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (IsSpellingFillerWord(token))
                continue;

            if (!TryMapSpellingTokenSequence(tokens, index, out var mapped, out var consumed))
                return false;

            builder.Append(mapped);
            index += consumed - 1;
        }

        spelledText = builder.ToString();
        return spelledText.Length > 0;
    }

    private static bool IsSpellingFillerWord(string token) =>
        token is "spell" or "type" or "insert" or "dictate" or "out" or "please";

    private static bool TryConvertLiteralOrNumeralDictationText(string value, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = NormalizeSpeechText(value);
        foreach (var prefix in new[] { "literal ", "say literal ", "type literal ", "insert literal " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = normalized[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(remainder))
                return false;

            text = remainder;
            return true;
        }

        foreach (var prefix in new[] { "numeral ", "number ", "say numeral ", "type numeral ", "insert numeral ", "type number ", "insert number " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = normalized[prefix.Length..].Trim();
            if (TryParseDictationNumeral(remainder, out var number))
            {
                text = number.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool TryParseDictationNumeral(string value, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            number = parsed;
            return true;
        }

        var tokens = NormalizeSpeechText(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return false;

        var total = 0;
        var current = 0;
        var consumedNumber = false;
        foreach (var token in tokens)
        {
            if (token == "and")
                continue;

            if (TryParseSmallNumberWord(token, out var small))
            {
                current += small;
                consumedNumber = true;
                continue;
            }

            if (TryParseTensNumberWord(token, out var tens))
            {
                current += tens;
                consumedNumber = true;
                continue;
            }

            if (token == "hundred")
            {
                if (current == 0)
                    current = 1;

                current *= 100;
                consumedNumber = true;
                continue;
            }

            return false;
        }

        number = total + current;
        return consumedNumber && number >= 0;
    }

    private static bool TryParseSmallNumberWord(string token, out int number)
    {
        number = token switch
        {
            "zero" or "oh" => 0,
            "one" => 1,
            "two" or "to" or "too" => 2,
            "three" => 3,
            "four" or "for" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            "ten" => 10,
            "eleven" => 11,
            "twelve" => 12,
            "thirteen" => 13,
            "fourteen" => 14,
            "fifteen" => 15,
            "sixteen" => 16,
            "seventeen" => 17,
            "eighteen" => 18,
            "nineteen" => 19,
            _ => -1
        };

        return number >= 0;
    }

    private static bool TryParseTensNumberWord(string token, out int number)
    {
        number = token switch
        {
            "twenty" => 20,
            "thirty" => 30,
            "forty" => 40,
            "fifty" => 50,
            "sixty" => 60,
            "seventy" => 70,
            "eighty" => 80,
            "ninety" => 90,
            _ => -1
        };

        return number >= 0;
    }

    private static string ConvertPlainDictationText(string value)
    {
        var tokens = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return string.Empty;

        var words = new List<string>();
        for (var index = 0; index < tokens.Length; index++)
        {
            if (TryMapPlainDictationTokenSequence(tokens, index, out var mapped, out var consumed))
            {
                words.Add(mapped);
                index += consumed - 1;
                continue;
            }

            words.Add(tokens[index]);
        }

        return string.Join(" ", words)
            .Replace($" {Environment.NewLine} ", Environment.NewLine, StringComparison.Ordinal)
            .Replace($" {Environment.NewLine}", Environment.NewLine, StringComparison.Ordinal)
            .Replace($"{Environment.NewLine} ", Environment.NewLine, StringComparison.Ordinal)
            .Replace(" ,", ",", StringComparison.Ordinal)
            .Replace(" .", ".", StringComparison.Ordinal)
            .Replace(" !", "!", StringComparison.Ordinal)
            .Replace(" ?", "?", StringComparison.Ordinal)
            .Replace(" :", ":", StringComparison.Ordinal)
            .Replace(" ;", ";", StringComparison.Ordinal);
    }

    private static bool TryMapPlainDictationTokenSequence(string[] tokens, int index, out string mapped, out int consumed)
    {
        mapped = string.Empty;
        consumed = 1;

        if (index + 1 < tokens.Length)
        {
            var phrase = $"{tokens[index]} {tokens[index + 1]}";
            mapped = phrase switch
            {
                "new line" => Environment.NewLine,
                "new paragraph" => Environment.NewLine + Environment.NewLine,
                "new sentence" => ".",
                "full stop" => ".",
                "question mark" => "?",
                "exclamation point" or "exclamation mark" => "!",
                "at sign" or "at symbol" => "@",
                "plus sign" => "+",
                "equals sign" or "equal sign" => "=",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(mapped))
            {
                consumed = 2;
                return true;
            }
        }

        mapped = tokens[index] switch
        {
            "comma" => ",",
            "period" => ".",
            "dot" => ".",
            "colon" => ":",
            "semicolon" => ";",
            "dash" or "hyphen" => "-",
            _ => string.Empty
        };

        return !string.IsNullOrEmpty(mapped);
    }

    private static bool TryMapSpellingTokenSequence(string[] tokens, int index, out string mapped, out int consumed)
    {
        mapped = string.Empty;
        consumed = 1;

        if (TryMapCapitalizedSpellingToken(tokens, index, out mapped, out consumed))
            return true;

        if (TryMapLowercaseSpellingToken(tokens, index, out mapped, out consumed))
            return true;

        if (index + 1 < tokens.Length)
        {
            var phrase = $"{tokens[index]} {tokens[index + 1]}";
            mapped = phrase switch
            {
                "at sign" or "at symbol" => "@",
                "and sign" => "&",
                "number sign" or "pound sign" or "hash sign" => "#",
                "dollar sign" => "$",
                "percent sign" => "%",
                "plus sign" => "+",
                "equal sign" or "equals sign" => "=",
                "under score" => "_",
                "back slash" => "\\",
                "forward slash" => "/",
                "single quote" => "'",
                "double quote" => "\"",
                "open parenthesis" or "left parenthesis" or "open paren" or "left paren" => "(",
                "close parenthesis" or "right parenthesis" or "close paren" or "right paren" => ")",
                "open bracket" or "left bracket" => "[",
                "close bracket" or "right bracket" => "]",
                "open brace" or "left brace" => "{",
                "close brace" or "right brace" => "}",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(mapped))
            {
                consumed = 2;
                return true;
            }
        }

        if (TryMapDigitSpellingToken(tokens, index, out mapped, out consumed))
            return true;

        return TryMapSpellingToken(tokens[index], out mapped);
    }

    private static bool TryMapCapitalizedSpellingToken(string[] tokens, int index, out string mapped, out int consumed)
    {
        mapped = string.Empty;
        consumed = 1;

        if (index >= tokens.Length || tokens[index] is not ("capital" or "cap" or "uppercase" or "upper"))
            return false;

        var tokenIndex = index + 1;
        if (tokenIndex < tokens.Length && tokens[tokenIndex] is "letter" or "character")
            tokenIndex++;

        if (tokenIndex >= tokens.Length || !TryMapSpellingToken(tokens[tokenIndex], out var next) || next.Length != 1 || !char.IsLetter(next[0]))
            return false;

        mapped = next.ToUpperInvariant();
        consumed = tokenIndex - index + 1;
        return true;
    }

    private static bool TryMapLowercaseSpellingToken(string[] tokens, int index, out string mapped, out int consumed)
    {
        mapped = string.Empty;
        consumed = 1;

        if (index >= tokens.Length || tokens[index] is not ("lowercase" or "lower"))
            return false;

        var tokenIndex = index + 1;
        if (tokens[index] == "lower" && tokenIndex < tokens.Length && tokens[tokenIndex] == "case")
            tokenIndex++;

        if (tokenIndex < tokens.Length && tokens[tokenIndex] is "letter" or "character")
            tokenIndex++;

        if (tokenIndex >= tokens.Length || !TryMapSpellingToken(tokens[tokenIndex], out var next) || next.Length != 1 || !char.IsLetter(next[0]))
            return false;

        mapped = next.ToLowerInvariant();
        consumed = tokenIndex - index + 1;
        return true;
    }

    private static bool TryMapDigitSpellingToken(string[] tokens, int index, out string mapped, out int consumed)
    {
        mapped = string.Empty;
        consumed = 1;

        if (index + 1 >= tokens.Length || tokens[index] is not ("digit" or "number"))
            return false;

        if (!TryMapSpellingToken(tokens[index + 1], out var next) || next.Length != 1 || !char.IsDigit(next[0]))
            return false;

        mapped = next;
        consumed = 2;
        return true;
    }

    private static bool TryParseSpokenNumber(string token, out int value)
    {
        value = 0;
        if (int.TryParse(token, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = token switch
        {
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            _ => 0
        };

        return value > 0;
    }

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
            "hash" or "pound" => "#",
            "dollar" => "$",
            "plus" => "+",
            "equals" or "equal" => "=",
            "colon" => ":",
            "semicolon" => ";",
            "comma" => ",",
            "question" => "?",
            "exclamation" => "!",
            "caret" => "^",
            "asterisk" or "star" => "*",
            "tilde" => "~",
            "backtick" or "grave" => "`",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(mapped))
            return true;

        if (Regex.IsMatch(token, @"^[a-z0-9]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
        {
            mapped = token;
            return true;
        }

        return false;
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
