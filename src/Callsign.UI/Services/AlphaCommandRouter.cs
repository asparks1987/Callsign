namespace Callsign.UI.Services;

public enum AlphaCommandKind
{
    None,
    Browser,
    FileSearch,
    Dictation,
    SystemControl,
    UiAction
}

public sealed record AlphaCommandRoute(
    AlphaCommandKind Kind,
    string Target,
    BrowserOpenTarget BrowserTarget = BrowserOpenTarget.Default);

public static class AlphaCommandRouter
{
    public static bool TryRouteUiNavigation(string command, out string target)
    {
        var navigationMap = new (string[] Prefixes, string Target)[]
        {
            (UiNextPrefixes, "Next"),
            (UiPreviousPrefixes, "Previous"),
            (UiAccountPrefixes, "Account"),
            (UiVoicePrefixes, "Voice"),
            (UiSessionPrefixes, "Session"),
            (UiDictationPrefixes, "Dictation"),
            (UiBrowserPrefixes, "Browser"),
            (UiFilesPrefixes, "Files"),
            (UiSystemPrefixes, "System")
        };

        foreach (var (prefixes, mappedTarget) in navigationMap)
        {
            if (TryStripAnyPrefix(command, prefixes, out _))
            {
                target = mappedTarget;
                return true;
            }
        }

        target = string.Empty;
        return false;
    }

    public static bool TryRoute(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryStripAnyPrefix(command, UiRepairWakewordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRepairWakeword);
            return true;
        }

        if (TryStripAnyPrefix(command, UiTrainVoiceIdentityPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionTrainVoiceIdentity);
            return true;
        }

        if (TryStripAnyPrefix(command, UiCreateAccountPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCreateAccount);
            return true;
        }

        if (TryStripAnyPrefix(command, UiSaveAccountPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionSaveAccount);
            return true;
        }

        if (TryStripAnyPrefix(command, UiDeleteAccountPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionDeleteAccount);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenDataFolderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenDataFolder);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenLogsFolderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenLogsFolder);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenAppFolderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenAppFolder);
            return true;
        }

        if (TryStripAnyPrefix(command, UiStartListeningPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStartListening);
            return true;
        }

        if (TryStripAnyPrefix(command, UiStopListeningPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStopListening);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelp);
            return true;
        }

        if (TryStripAnyPrefix(command, UiShowVisibleControlsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowVisibleControls);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideVisibleControlsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideVisibleControls);
            return true;
        }

        if (TryStripAnyPrefix(command, UiNextControlPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionNextControl);
            return true;
        }

        if (TryStripAnyPrefix(command, UiPreviousControlPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionPreviousControl);
            return true;
        }

        if (TryStripAnyPrefix(command, UiActivateControlPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionActivateControl);
            return true;
        }

        if (TryStripAnyPrefix(command, UiActivateVoicePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStartListening);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserBackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionBack);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserForwardPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionForward);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserRefreshPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionRefresh);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserNewTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionNewTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserCloseTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionCloseTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserFocusAddressBarPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFocusAddressBar);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserFindPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFind);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserFindNextPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFindNext);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserFindPreviousPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFindPrevious);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserScrollUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollUp);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserScrollDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollDown);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserScrollTopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollTop);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserScrollBottomPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollBottom);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserZoomInPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionZoomIn);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserZoomOutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionZoomOut);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserZoomResetPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionZoomReset);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemVolumeUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionVolumeUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemVolumeDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionVolumeDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemVolumeMutePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionVolumeMute);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemShowDesktopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionShowDesktop);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNextWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNextWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPreviousWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPreviousWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemTaskManagerPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenTaskManager);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMinimizeWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMinimizeWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMaximizeWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMaximizeWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemRestoreWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionRestoreWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressEnterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressEnter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressTab);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressEscapePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressEscape);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressBackspacePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressBackspace);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressRight);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressHomePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressHome);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPageUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPageUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPageDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPageDown);
            return true;
        }

        if (TryRouteVisibleControlLabelAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMouseClickPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseClick);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseDoubleClickPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseDoubleClick);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseRightClickPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseRightClick);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseScrollUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseScrollUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseScrollDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseScrollDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopy);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPastePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPaste);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCut);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectAllPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectAll);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSavePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSave);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemUndoPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionUndo);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemRedoPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionRedo);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemFindPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFind);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNewWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNewWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCloseWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCloseWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMovePreviousWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMovePreviousWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveNextWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveNextWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectPreviousWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectPreviousWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectNextWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectNextWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeletePreviousWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeletePreviousWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteNextWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteNextWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMovePreviousSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMovePreviousSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveNextSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveNextSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectPreviousSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectPreviousSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectNextSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectNextSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeletePreviousSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeletePreviousSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteNextSentencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteNextSentence);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMovePreviousParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMovePreviousParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveNextParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveNextParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectPreviousParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectPreviousParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectNextParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectNextParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeletePreviousParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeletePreviousParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteNextParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteNextParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, ChromeBrowserPrefixes, out var chromeTarget))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, TrimBrowserTargetLeadIn(chromeTarget), BrowserOpenTarget.Chrome);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserPrefixes, out var browserTarget))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, TrimBrowserTargetLeadIn(browserTarget));
            return true;
        }

        if (TryStripAnyPrefix(command, FileSearchPrefixes, out var fileSearchQuery))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.FileSearch, fileSearchQuery);
            return true;
        }

        if (TryStripAnyPrefix(command, DictationPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Dictation, string.Empty);
            return true;
        }

        return false;
    }

    private static string TrimBrowserTargetLeadIn(string target)
    {
        var trimmed = target.Trim();
        foreach (var prefix in new[] { "to ", "for ", "search for ", "look up " })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return NormalizeSpokenWebTarget(trimmed[prefix.Length..].Trim());
        }

        return NormalizeSpokenWebTarget(trimmed);
    }

    private static string NormalizeSpokenWebTarget(string target)
    {
        var normalized = target.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = normalized
            .Replace(" dot ", ".", StringComparison.OrdinalIgnoreCase)
            .Replace(" point ", ".", StringComparison.OrdinalIgnoreCase);

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 2 && IsCommonTopLevelDomain(tokens[1]))
            return $"{tokens[0]}.{tokens[1]}";

        return normalized;
    }

    private static bool IsCommonTopLevelDomain(string token) =>
        token.Equals("com", StringComparison.OrdinalIgnoreCase)
        || token.Equals("org", StringComparison.OrdinalIgnoreCase)
        || token.Equals("net", StringComparison.OrdinalIgnoreCase)
        || token.Equals("io", StringComparison.OrdinalIgnoreCase)
        || token.Equals("ai", StringComparison.OrdinalIgnoreCase)
        || token.Equals("dev", StringComparison.OrdinalIgnoreCase)
        || token.Equals("gov", StringComparison.OrdinalIgnoreCase)
        || token.Equals("edu", StringComparison.OrdinalIgnoreCase);

    private static bool TryStripAnyPrefix(string command, IReadOnlyList<string> prefixes, out string remainder)
    {
        var trimmed = command.Trim();
        foreach (var prefix in prefixes)
        {
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            remainder = trimmed[prefix.Length..].Trim();
            return true;
        }

        remainder = string.Empty;
        return false;
    }

    private static bool TryRouteVisibleControlLabelAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        foreach (var prefix in UiVisibleControlLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionActivateLabelPrefix}{normalizedLabel}");
            return true;
        }

        return false;
    }

    private static string NormalizeVisibleControlLabel(string value)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = TrimSpeechWords(normalized, VisibleControlLabelLeadingWords, trimFromStart: true);
        normalized = TrimSpeechWords(normalized, VisibleControlLabelTrailingWords, trimFromStart: false);
        return normalized;
    }

    private static string TrimSpeechWords(string value, IReadOnlyCollection<string> words, bool trimFromStart)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var word in words)
            {
                if (trimFromStart)
                {
                    if (!normalized.StartsWith($"{word} ", StringComparison.OrdinalIgnoreCase))
                        continue;

                    normalized = normalized[word.Length..].Trim();
                    changed = true;
                    break;
                }

                if (!normalized.EndsWith($" {word}", StringComparison.OrdinalIgnoreCase))
                    continue;

                normalized = normalized[..^word.Length].Trim();
                changed = true;
                break;
            }
        }

        return normalized;
    }

    private static bool IsListeningLabel(string normalizedLabel) =>
        string.Equals(normalizedLabel, "voice", StringComparison.OrdinalIgnoreCase)
        || string.Equals(normalizedLabel, "voice control", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] BrowserPrefixes =
    [
        "open website ",
        "open web site ",
        "open browser ",
        "browse to ",
        "go to ",
        "search web for ",
        "search the web for ",
        "google ",
        "browser search "
    ];

    private static readonly string[] BrowserBackPrefixes =
    [
        "browser back",
        "back browser",
        "back page",
        "browser previous page",
        "go back in browser",
        "go browser back"
    ];

    private static readonly string[] BrowserForwardPrefixes =
    [
        "browser forward",
        "forward browser",
        "next page",
        "browser next page",
        "go forward in browser",
        "go browser forward"
    ];

    private static readonly string[] BrowserRefreshPrefixes =
    [
        "browser refresh",
        "refresh browser",
        "refresh page",
        "reload browser",
        "reload page"
    ];

    private static readonly string[] BrowserNewTabPrefixes =
    [
        "browser new tab",
        "new browser tab",
        "new tab",
        "open new tab"
    ];

    private static readonly string[] BrowserCloseTabPrefixes =
    [
        "browser close tab",
        "close browser tab",
        "close tab"
    ];

    private static readonly string[] BrowserFocusAddressBarPrefixes =
    [
        "browser focus address bar",
        "browser address bar",
        "focus address bar",
        "open address bar",
        "go to address bar"
    ];

    private static readonly string[] BrowserFindPrefixes =
    [
        "browser find",
        "find in page",
        "find on page",
        "search in page",
        "search page",
        "browser search in page",
        "find text",
        "browser find text"
    ];

    private static readonly string[] BrowserFindNextPrefixes =
    [
        "browser find next",
        "find next",
        "next match",
        "next result",
        "find again",
        "browser next search result"
    ];

    private static readonly string[] BrowserFindPreviousPrefixes =
    [
        "browser find previous",
        "find previous",
        "previous match",
        "previous result",
        "browser previous search result"
    ];

    private static readonly string[] BrowserScrollUpPrefixes =
    [
        "browser scroll up",
        "scroll up",
        "browser page up",
        "scroll page up",
        "move up page"
    ];

    private static readonly string[] BrowserScrollDownPrefixes =
    [
        "browser scroll down",
        "scroll down",
        "browser page down",
        "scroll page down",
        "move down page"
    ];

    private static readonly string[] BrowserScrollTopPrefixes =
    [
        "browser scroll top",
        "scroll to top",
        "scroll top",
        "top of page",
        "browser top",
        "go to top"
    ];

    private static readonly string[] BrowserScrollBottomPrefixes =
    [
        "browser scroll bottom",
        "scroll to bottom",
        "scroll bottom",
        "bottom of page",
        "browser bottom",
        "go to bottom"
    ];

    private static readonly string[] BrowserZoomInPrefixes =
    [
        "browser zoom in",
        "zoom in",
        "browser bigger",
        "make bigger",
        "browser larger"
    ];

    private static readonly string[] BrowserZoomOutPrefixes =
    [
        "browser zoom out",
        "zoom out",
        "browser smaller",
        "make smaller",
        "browser smaller text"
    ];

    private static readonly string[] BrowserZoomResetPrefixes =
    [
        "browser zoom reset",
        "reset zoom",
        "browser reset zoom",
        "zoom reset",
        "browser normal size",
        "browser actual size"
    ];

    private static readonly string[] SystemVolumeUpPrefixes =
    [
        "volume up",
        "turn volume up",
        "increase volume",
        "louder",
        "system volume up"
    ];

    private static readonly string[] SystemVolumeDownPrefixes =
    [
        "volume down",
        "turn volume down",
        "decrease volume",
        "quieter",
        "system volume down"
    ];

    private static readonly string[] SystemVolumeMutePrefixes =
    [
        "mute volume",
        "mute sound",
        "turn volume mute",
        "system mute",
        "system mute volume",
        "mute audio"
    ];

    private static readonly string[] SystemShowDesktopPrefixes =
    [
        "show desktop",
        "go to desktop",
        "desktop",
        "system show desktop"
    ];

    private static readonly string[] SystemNextWindowPrefixes =
    [
        "next window",
        "switch window",
        "cycle window",
        "system next window"
    ];

    private static readonly string[] SystemPreviousWindowPrefixes =
    [
        "previous window",
        "back window",
        "switch back window",
        "system previous window"
    ];

    private static readonly string[] SystemTaskManagerPrefixes =
    [
        "task manager",
        "open task manager",
        "show task manager",
        "system task manager"
    ];

    private static readonly string[] SystemMinimizeWindowPrefixes =
    [
        "minimize window",
        "minimise window",
        "minimize this window",
        "minimise this window",
        "system minimize window"
    ];

    private static readonly string[] SystemMaximizeWindowPrefixes =
    [
        "maximize window",
        "maximise window",
        "maximize this window",
        "maximise this window",
        "system maximize window"
    ];

    private static readonly string[] SystemRestoreWindowPrefixes =
    [
        "restore window",
        "restore this window",
        "system restore window"
    ];

    private static readonly string[] SystemPressEnterPrefixes =
    [
        "press enter",
        "hit enter",
        "enter key",
        "system press enter"
    ];

    private static readonly string[] SystemPressTabPrefixes =
    [
        "press tab",
        "hit tab",
        "tab key",
        "system press tab"
    ];

    private static readonly string[] SystemPressEscapePrefixes =
    [
        "press escape",
        "hit escape",
        "escape key",
        "system press escape"
    ];

    private static readonly string[] SystemPressBackspacePrefixes =
    [
        "press backspace",
        "hit backspace",
        "backspace key",
        "system press backspace"
    ];

    private static readonly string[] SystemPressUpPrefixes =
    [
        "press up",
        "press up arrow",
        "up arrow",
        "system press up"
    ];

    private static readonly string[] SystemPressDownPrefixes =
    [
        "press down",
        "press down arrow",
        "down arrow",
        "system press down"
    ];

    private static readonly string[] SystemPressLeftPrefixes =
    [
        "press left",
        "press left arrow",
        "left arrow",
        "system press left"
    ];

    private static readonly string[] SystemPressRightPrefixes =
    [
        "press right",
        "press right arrow",
        "right arrow",
        "system press right"
    ];

    private static readonly string[] SystemPressHomePrefixes =
    [
        "press home",
        "home key",
        "go to home",
        "system press home"
    ];

    private static readonly string[] SystemPressEndPrefixes =
    [
        "press end",
        "end key",
        "go to end",
        "system press end"
    ];

    private static readonly string[] SystemPageUpPrefixes =
    [
        "page up",
        "press page up",
        "go page up",
        "system page up"
    ];

    private static readonly string[] SystemPageDownPrefixes =
    [
        "page down",
        "press page down",
        "go page down",
        "system page down"
    ];

    private static readonly string[] SystemMouseClickPrefixes =
    [
        "click",
        "mouse click",
        "left click",
        "system click"
    ];

    private static readonly string[] SystemMouseDoubleClickPrefixes =
    [
        "double click",
        "mouse double click",
        "double tap",
        "system double click"
    ];

    private static readonly string[] SystemMouseRightClickPrefixes =
    [
        "right click",
        "mouse right click",
        "context click",
        "system right click"
    ];

    private static readonly string[] SystemMouseScrollUpPrefixes =
    [
        "mouse scroll up",
        "scroll wheel up",
        "wheel up",
        "system mouse scroll up"
    ];

    private static readonly string[] SystemMouseScrollDownPrefixes =
    [
        "mouse scroll down",
        "scroll wheel down",
        "wheel down",
        "system mouse scroll down"
    ];

    private static readonly string[] SystemCopyPrefixes =
    [
        "system copy",
        "copy selection",
        "copy selected text"
    ];

    private static readonly string[] SystemPastePrefixes =
    [
        "system paste",
        "paste clipboard",
        "paste selection"
    ];

    private static readonly string[] SystemCutPrefixes =
    [
        "system cut",
        "cut selection",
        "cut selected text"
    ];

    private static readonly string[] SystemSelectAllPrefixes =
    [
        "system select all",
        "select all text",
        "highlight all text"
    ];

    private static readonly string[] SystemSavePrefixes =
    [
        "system save",
        "save file",
        "save document",
        "save it"
    ];

    private static readonly string[] SystemUndoPrefixes =
    [
        "system undo",
        "undo last change",
        "undo it"
    ];

    private static readonly string[] SystemRedoPrefixes =
    [
        "system redo",
        "redo last change",
        "redo it"
    ];

    private static readonly string[] SystemFindPrefixes =
    [
        "system find",
        "find in app",
        "find text",
        "search in app"
    ];

    private static readonly string[] SystemNewWindowPrefixes =
    [
        "system new window",
        "new window",
        "open new window"
    ];

    private static readonly string[] SystemCloseWindowPrefixes =
    [
        "system close window",
        "close window",
        "close this window"
    ];

    private static readonly string[] SystemMovePreviousWordPrefixes =
    [
        "system move previous word",
        "move previous word",
        "previous word",
        "go previous word"
    ];

    private static readonly string[] SystemMoveNextWordPrefixes =
    [
        "system move next word",
        "move next word",
        "next word",
        "go next word"
    ];

    private static readonly string[] SystemSelectPreviousWordPrefixes =
    [
        "system select previous word",
        "select previous word",
        "highlight previous word"
    ];

    private static readonly string[] SystemSelectNextWordPrefixes =
    [
        "system select next word",
        "select next word",
        "highlight next word"
    ];

    private static readonly string[] SystemDeletePreviousWordPrefixes =
    [
        "system delete previous word",
        "delete previous word",
        "remove previous word"
    ];

    private static readonly string[] SystemDeleteNextWordPrefixes =
    [
        "system delete next word",
        "delete next word",
        "remove next word"
    ];

    private static readonly string[] SystemMovePreviousSentencePrefixes =
    [
        "system move previous sentence",
        "move previous sentence",
        "previous sentence"
    ];

    private static readonly string[] SystemMoveNextSentencePrefixes =
    [
        "system move next sentence",
        "move next sentence",
        "next sentence"
    ];

    private static readonly string[] SystemSelectPreviousSentencePrefixes =
    [
        "system select previous sentence",
        "select previous sentence",
        "highlight previous sentence"
    ];

    private static readonly string[] SystemSelectNextSentencePrefixes =
    [
        "system select next sentence",
        "select next sentence",
        "highlight next sentence"
    ];

    private static readonly string[] SystemDeletePreviousSentencePrefixes =
    [
        "system delete previous sentence",
        "delete previous sentence",
        "remove previous sentence"
    ];

    private static readonly string[] SystemDeleteNextSentencePrefixes =
    [
        "system delete next sentence",
        "delete next sentence",
        "remove next sentence"
    ];

    private static readonly string[] SystemMovePreviousParagraphPrefixes =
    [
        "system move previous paragraph",
        "move previous paragraph",
        "previous paragraph"
    ];

    private static readonly string[] SystemMoveNextParagraphPrefixes =
    [
        "system move next paragraph",
        "move next paragraph",
        "next paragraph"
    ];

    private static readonly string[] SystemSelectPreviousParagraphPrefixes =
    [
        "system select previous paragraph",
        "select previous paragraph",
        "highlight previous paragraph"
    ];

    private static readonly string[] SystemSelectNextParagraphPrefixes =
    [
        "system select next paragraph",
        "select next paragraph",
        "highlight next paragraph"
    ];

    private static readonly string[] SystemDeletePreviousParagraphPrefixes =
    [
        "system delete previous paragraph",
        "delete previous paragraph",
        "remove previous paragraph"
    ];

    private static readonly string[] SystemDeleteNextParagraphPrefixes =
    [
        "system delete next paragraph",
        "delete next paragraph",
        "remove next paragraph"
    ];

    private const string BrowserActionBack = "browser-back";
    private const string BrowserActionForward = "browser-forward";
    private const string BrowserActionRefresh = "browser-refresh";
    private const string BrowserActionNewTab = "browser-new-tab";
    private const string BrowserActionCloseTab = "browser-close-tab";
    private const string BrowserActionFocusAddressBar = "browser-focus-address-bar";
    private const string BrowserActionFind = "browser-find";
    private const string BrowserActionFindNext = "browser-find-next";
    private const string BrowserActionFindPrevious = "browser-find-previous";
    private const string BrowserActionScrollUp = "browser-scroll-up";
    private const string BrowserActionScrollDown = "browser-scroll-down";
    private const string BrowserActionScrollTop = "browser-scroll-top";
    private const string BrowserActionScrollBottom = "browser-scroll-bottom";
    private const string BrowserActionZoomIn = "browser-zoom-in";
    private const string BrowserActionZoomOut = "browser-zoom-out";
    private const string BrowserActionZoomReset = "browser-zoom-reset";
    private const string SystemActionVolumeUp = "system-volume-up";
    private const string SystemActionVolumeDown = "system-volume-down";
    private const string SystemActionVolumeMute = "system-volume-mute";
    private const string SystemActionShowDesktop = "system-show-desktop";
    private const string SystemActionNextWindow = "system-next-window";
    private const string SystemActionPreviousWindow = "system-previous-window";
    private const string SystemActionOpenTaskManager = "system-open-task-manager";
    private const string SystemActionMinimizeWindow = "system-minimize-window";
    private const string SystemActionMaximizeWindow = "system-maximize-window";
    private const string SystemActionRestoreWindow = "system-restore-window";
    private const string SystemActionPressEnter = "system-press-enter";
    private const string SystemActionPressTab = "system-press-tab";
    private const string SystemActionPressEscape = "system-press-escape";
    private const string SystemActionPressBackspace = "system-press-backspace";
    private const string SystemActionPressUp = "system-press-up";
    private const string SystemActionPressDown = "system-press-down";
    private const string SystemActionPressLeft = "system-press-left";
    private const string SystemActionPressRight = "system-press-right";
    private const string SystemActionPressHome = "system-press-home";
    private const string SystemActionPressEnd = "system-press-end";
    private const string SystemActionPageUp = "system-page-up";
    private const string SystemActionPageDown = "system-page-down";
    private const string SystemActionMouseClick = "system-mouse-click";
    private const string SystemActionMouseDoubleClick = "system-mouse-double-click";
    private const string SystemActionMouseRightClick = "system-mouse-right-click";
    private const string SystemActionMouseScrollUp = "system-mouse-scroll-up";
    private const string SystemActionMouseScrollDown = "system-mouse-scroll-down";
    private const string SystemActionCopy = "system-copy";
    private const string SystemActionPaste = "system-paste";
    private const string SystemActionCut = "system-cut";
    private const string SystemActionSelectAll = "system-select-all";
    private const string SystemActionSave = "system-save";
    private const string SystemActionUndo = "system-undo";
    private const string SystemActionRedo = "system-redo";
    private const string SystemActionFind = "system-find";
    private const string SystemActionNewWindow = "system-new-window";
    private const string SystemActionCloseWindow = "system-close-window";
    private const string SystemActionMovePreviousWord = "system-move-previous-word";
    private const string SystemActionMoveNextWord = "system-move-next-word";
    private const string SystemActionSelectPreviousWord = "system-select-previous-word";
    private const string SystemActionSelectNextWord = "system-select-next-word";
    private const string SystemActionDeletePreviousWord = "system-delete-previous-word";
    private const string SystemActionDeleteNextWord = "system-delete-next-word";
    private const string SystemActionMovePreviousSentence = "system-move-previous-sentence";
    private const string SystemActionMoveNextSentence = "system-move-next-sentence";
    private const string SystemActionSelectPreviousSentence = "system-select-previous-sentence";
    private const string SystemActionSelectNextSentence = "system-select-next-sentence";
    private const string SystemActionDeletePreviousSentence = "system-delete-previous-sentence";
    private const string SystemActionDeleteNextSentence = "system-delete-next-sentence";
    private const string SystemActionMovePreviousParagraph = "system-move-previous-paragraph";
    private const string SystemActionMoveNextParagraph = "system-move-next-paragraph";
    private const string SystemActionSelectPreviousParagraph = "system-select-previous-paragraph";
    private const string SystemActionSelectNextParagraph = "system-select-next-paragraph";
    private const string SystemActionDeletePreviousParagraph = "system-delete-previous-paragraph";
    private const string SystemActionDeleteNextParagraph = "system-delete-next-paragraph";
    private const string UiActionRepairWakeword = "ui-repair-wakeword";
    private const string UiActionTrainVoiceIdentity = "ui-train-voice-identity";
    private const string UiActionCreateAccount = "ui-create-account";
    private const string UiActionSaveAccount = "ui-save-account";
    private const string UiActionDeleteAccount = "ui-delete-account";
    private const string UiActionOpenDataFolder = "ui-open-data-folder";
    private const string UiActionOpenLogsFolder = "ui-open-logs-folder";
    private const string UiActionOpenAppFolder = "ui-open-app-folder";
    private const string UiActionStartListening = "ui-start-listening";
    private const string UiActionStopListening = "ui-stop-listening";
    private const string UiActionVoiceHelp = "ui-voice-help";
    private const string UiActionShowVisibleControls = "ui-show-visible-controls";
    private const string UiActionHideVisibleControls = "ui-hide-visible-controls";
    private const string UiActionNextControl = "ui-next-control";
    private const string UiActionPreviousControl = "ui-previous-control";
    private const string UiActionActivateControl = "ui-activate-control";

    private static readonly string[] ChromeBrowserPrefixes =
    [
        "open chrome to ",
        "open chrome ",
        "open google chrome to ",
        "open google chrome ",
        "chrome open ",
        "chrome search for ",
        "google chrome search for ",
        "search chrome for ",
        "search google chrome for ",
        "google chrome ",
        "google crome ",
        "open crome ",
        "crome search for ",
        "browse chrome to "
    ];

    private static readonly string[] FileSearchPrefixes =
    [
        "find files named ",
        "find file named ",
        "find folder named ",
        "find a file called ",
        "find a folder called ",
        "find something called ",
        "find file ",
        "find files ",
        "find the file ",
        "find my file ",
        "find folder ",
        "find folders ",
        "find the folder ",
        "look for file ",
        "look for folder ",
        "search for file ",
        "search for folder ",
        "search files for ",
        "search file for ",
        "search folders for ",
        "search folder for ",
        "search my pc for ",
        "search this pc for ",
        "search my computer for ",
        "find document ",
        "open file search for ",
        "search explorer for ",
        "search files called ",
        "search folders called "
    ];

    private static readonly string[] DictationPrefixes =
    [
        "start dictation",
        "begin dictation",
        "dictation",
        "take dictation"
    ];

    private static readonly string[] UiRepairWakewordPrefixes =
    [
        "repair wakeword",
        "repair wake word",
        "fix wakeword",
        "fix wake word",
        "wakeword repair",
        "wake word repair"
    ];

    private static readonly string[] UiTrainVoiceIdentityPrefixes =
    [
        "train voice identity",
        "train identity",
        "repair identity runtime",
        "repair identity",
        "voice identity training",
        "open train voice identity",
        "show train voice identity"
    ];

    private static readonly string[] UiCreateAccountPrefixes =
    [
        "create new account",
        "new account",
        "create account",
        "open new account"
    ];

    private static readonly string[] UiSaveAccountPrefixes =
    [
        "save account",
        "save current account",
        "save profile"
    ];

    private static readonly string[] UiDeleteAccountPrefixes =
    [
        "delete account",
        "delete current account",
        "remove account",
        "delete profile"
    ];

    private static readonly string[] UiOpenDataFolderPrefixes =
    [
        "open data folder",
        "show data folder",
        "open profile folder",
        "show profile folder",
        "open profile data",
        "show profile data"
    ];

    private static readonly string[] UiOpenLogsFolderPrefixes =
    [
        "open logs folder",
        "show logs folder",
        "open log folder",
        "show log folder"
    ];

    private static readonly string[] UiOpenAppFolderPrefixes =
    [
        "open app folder",
        "show app folder",
        "open application folder",
        "show application folder",
        "open app directory",
        "show app directory"
    ];

    private static readonly string[] UiStartListeningPrefixes =
    [
        "start listening",
        "start voice",
        "listen",
        "begin listening"
    ];

    private static readonly string[] UiStopListeningPrefixes =
    [
        "stop listening",
        "stop voice",
        "end listening"
    ];

    private static readonly string[] UiVoiceHelpPrefixes =
    [
        "voice help",
        "what can i say",
        "what can i do",
        "show voice help",
        "show commands",
        "list commands",
        "help"
    ];

    private static readonly string[] UiShowVisibleControlsPrefixes =
    [
        "show numbers",
        "show visible controls",
        "show controls",
        "show what i can click",
        "show clickable controls"
    ];

    private static readonly string[] UiHideVisibleControlsPrefixes =
    [
        "hide visible controls",
        "hide numbers",
        "close visible controls",
        "dismiss visible controls"
    ];

    private static readonly string[] UiNextControlPrefixes =
    [
        "next control",
        "next field",
        "next button",
        "next item",
        "go to next control",
        "move to next control",
        "focus next control"
    ];

    private static readonly string[] UiPreviousControlPrefixes =
    [
        "previous control",
        "previous field",
        "previous button",
        "previous item",
        "go to previous control",
        "move to previous control",
        "focus previous control",
        "back control"
    ];

    private static readonly string[] UiActivateControlPrefixes =
    [
        "activate control",
        "click control",
        "press control",
        "activate selected control",
        "press selected control",
        "click selected control",
        "activate button",
        "click button",
        "press button",
        "activate current control",
        "click current control",
        "press current control"
    ];

    private static readonly string[] UiActivateVoicePrefixes =
    [
        "activate voice control",
        "enable voice"
    ];

    private static readonly string[] UiVisibleControlLabelPrefixes =
    [
        "press ",
        "click ",
        "activate ",
        "tap "
    ];

    private static readonly string[] UiAccountPrefixes =
    [
        "open account",
        "show account",
        "go to account",
        "switch to account",
        "account tab",
        "open account tab",
        "show account tab"
    ];

    private static readonly string[] UiVoicePrefixes =
    [
        "open voice",
        "show voice",
        "go to voice",
        "switch to voice",
        "voice tab",
        "open voice tab",
        "show voice tab"
    ];

    private static readonly string[] UiSessionPrefixes =
    [
        "open session",
        "show session",
        "go to session",
        "switch to session",
        "session tab",
        "open session tab",
        "show session tab"
    ];

    private static readonly string[] UiDictationPrefixes =
    [
        "open dictation",
        "show dictation",
        "go to dictation",
        "switch to dictation",
        "dictation tab",
        "open dictation tab",
        "show dictation tab"
    ];

    private static readonly string[] UiBrowserPrefixes =
    [
        "open browser",
        "show browser",
        "go to browser",
        "switch to browser",
        "browser tab",
        "open browser tab",
        "show browser tab"
    ];

    private static readonly string[] UiFilesPrefixes =
    [
        "open files",
        "show files",
        "go to files",
        "switch to files",
        "files tab",
        "open files tab",
        "show files tab"
    ];

    private static readonly string[] UiSystemPrefixes =
    [
        "open system",
        "show system",
        "go to system",
        "switch to system",
        "system tab",
        "open system tab",
        "show system tab"
    ];

    private static readonly string[] UiNextPrefixes =
    [
        "next tab",
        "go to next tab",
        "switch to next tab",
        "move to next tab",
        "next page",
        "show next tab"
    ];

    private static readonly string[] UiPreviousPrefixes =
    [
        "previous tab",
        "go to previous tab",
        "switch to previous tab",
        "move to previous tab",
        "back tab",
        "go back",
        "previous page",
        "show previous tab"
    ];

    private const string UiActionActivateLabelPrefix = "ui-activate-label:";

    private static readonly string[] VisibleControlLabelLeadingWords =
    [
        "the",
        "a",
        "an",
        "my",
        "this",
        "that",
        "current"
    ];

    private static readonly string[] VisibleControlLabelTrailingWords =
    [
        "button",
        "control",
        "field",
        "item",
        "tab",
        "page"
    ];
}
