using System.Text.RegularExpressions;

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
            (UiShortcutsPrefixes, "Shortcuts"),
            (UiPlansPrefixes, "Plans"),
            (UiUpdatesPrefixes, "Updates"),
            (UiBrowserPrefixes, "Browser"),
            (UiPacksPrefixes, "Packs"),
            (UiFilesPrefixes, "Files"),
            (UiSystemPrefixes, "System")
        };

        foreach (var (prefixes, mappedTarget) in navigationMap)
        {
            if (TryMatchWholeCommand(command, prefixes))
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

        if (TryMatchWholeCommand(command, SystemSelectAllPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectAll);
            return true;
        }

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

        if (TryStripAnyPrefix(command, UiGettingStartedPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionGettingStarted);
            return true;
        }

        if (TryStripAnyPrefix(command, UiPacksPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenPacks);
            return true;
        }

        if (TryStripAnyPrefix(command, UiImportPackFolderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionImportPackFolder);
            return true;
        }

        if (TryStripAnyPrefix(command, UiImportPackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionImportPack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiUpdatePackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionUpdatePack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiRollbackPackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRollbackPack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiRefreshPacksPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRefreshPacks);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenPacksFolderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenPacksFolder);
            return true;
        }

        if (TryStripAnyPrefix(command, UiEnableSelectedPackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionEnableSelectedPack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiDisableSelectedPackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionDisableSelectedPack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiRemoveSelectedPackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRemoveSelectedPack);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenShortcutsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenShortcuts);
            return true;
        }

        if (TryStripAnyPrefix(command, UiNewVoiceShortcutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionNewVoiceShortcut);
            return true;
        }

        if (TryStripAnyPrefix(command, UiSaveVoiceShortcutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionSaveVoiceShortcut);
            return true;
        }

        if (TryStripAnyPrefix(command, UiDeleteVoiceShortcutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionDeleteVoiceShortcut);
            return true;
        }

        if (TryStripAnyPrefix(command, UiEnableVoiceShortcutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionEnableVoiceShortcut);
            return true;
        }

        if (TryStripAnyPrefix(command, UiDisableVoiceShortcutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionDisableVoiceShortcut);
            return true;
        }

        if (TryStripAnyPrefix(command, UiAddVoiceShortcutCommandActionPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionAddVoiceShortcutCommandAction);
            return true;
        }

        if (TryStripAnyPrefix(command, UiAddVoiceShortcutWaitActionPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionAddVoiceShortcutWaitAction);
            return true;
        }

        if (TryStripAnyPrefix(command, UiRemoveVoiceShortcutActionPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRemoveVoiceShortcutAction);
            return true;
        }

        if (TryStripAnyPrefix(command, UiStartListeningPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStartListening);
            return true;
        }

        if (TryStripAnyPrefix(command, UiRestartListeningPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionRestartListening);
            return true;
        }

        if (TryStripAnyPrefix(command, UiStopListeningPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStopListening);
            return true;
        }

        if (TryRouteVocabularyAction(command, out route))
            return true;

        if (TryMatchWholeCommand(command, UiCancelSessionPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCancelSession);
            return true;
        }

        if (TryMatchWholeCommand(command, UiResetSessionPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionResetSession);
            return true;
        }

        if (TryMatchWholeCommand(command, UiBlockedExternalSideEffectPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionBlockedExternalSideEffect);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelp);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpSystemPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpSystem);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpBrowserPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpBrowser);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpDictationPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpDictation);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpVisiblePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpVisible);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpExtensionsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpExtensions);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpSafetyPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpSafety);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpFreePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpFree);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpPaidPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpPaid);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpProPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpPro);
            return true;
        }

        if (TryStripAnyPrefix(command, UiVoiceHelpAdvancedPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionVoiceHelpAdvanced);
            return true;
        }

        if (TryStripAnyPrefix(command, UiCheckUpdatesPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCheckUpdatesNow);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadUpdateSummaryAgainPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadUpdateSummaryAgain);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadImportSummaryAgainPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadImportSummaryAgain);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadWatchStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadWatchStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenReleaseProofPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenReleaseProof);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadReleaseProofPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadReleaseProof);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadVoiceModeStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadVoiceModeStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenManualEvidenceChecklistPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenManualEvidenceChecklist);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadEvidenceHeaderPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadEvidenceHeader);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenManualEvidencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenManualEvidence);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenReleaseEvidencePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenReleaseEvidence);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadProofNotesStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadProofNotesStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiCreateAllProofNotesPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCreateAllProofNotes);
            return true;
        }

        if (TryStripAnyPrefix(command, UiCreateEvidenceDraftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCreateEvidenceDraft);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenEvidenceDraftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenEvidenceDraft);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadEvidenceDraftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadEvidenceDraft);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenProofNotesPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenProofNotes);
            return true;
        }

        if (TryStripAnyPrefix(command, UiOpenInstallerPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionOpenInstaller);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadUpdatesStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadUpdatesStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadCheckInStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadCheckInStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadEvidenceStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadEvidenceStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadReleaseBlockersPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadReleaseBlockers);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadNextProofPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadNextProof);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadReleaseGatesPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadReleaseGates);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadNextProofInstructionsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadNextProofInstructions);
            return true;
        }

        if (TryStripAnyPrefix(command, UiCreateNextProofNotePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionCreateNextProofNote);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadVisualStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadVisualStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadRestartProofPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadRestartProof);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadPlansStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadPlansStatus);
            return true;
        }

        if (TryMatchWholeCommand(command, UiReadClockStatusPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadClockStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiReadStatusPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionReadStatus);
            return true;
        }

        if (TryStripAnyPrefix(command, UiStopStatusReadbackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionStopStatusReadback);
            return true;
        }

        if (TryStripAnyPrefix(command, UiClearRecentSpeechPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionClearRecentSpeech);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideCommandPalettePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideCommandPalette);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideImportSplashPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideImportSplash);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideUpdateSplashPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideUpdateSplash);
            return true;
        }

        if (TryRouteVoiceModeAction(command, out route))
            return true;

        if (TryRouteDictationOptionAction(command, out route))
            return true;

        if (TryRouteVisibleControlAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, UiShowKeyboardPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowKeyboard);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideKeyboardPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideKeyboard);
            return true;
        }

        if (TryRouteMouseGridAction(command, out route))
            return true;

        if (TryRouteFileResultAction(command, out route))
            return true;

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

        if (TryStripAnyPrefix(command, BrowserStopLoadingPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStopLoading);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserNewTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionNewTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserNewWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionNewWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserPrivateWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionPrivateWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserBookmarkPagePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionBookmarkPage);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserOpenBookmarksPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionOpenBookmarks);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserSavePagePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionSavePage);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserPrintPagePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionPrintPage);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserNextTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionNextTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserPreviousTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionPreviousTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserMoveTabLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionMoveTabLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserMoveTabRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionMoveTabRight);
            return true;
        }

        if (TryRouteBrowserSelectTab(command, out route))
            return true;

        if (TryStripAnyPrefix(command, BrowserCloseTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionCloseTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserReopenClosedTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionReopenClosedTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserDuplicateTabPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionDuplicateTab);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserCopyAddressPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionCopyAddress);
            return true;
        }

        if (TryRouteBrowserAddressTextAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, BrowserFocusAddressBarPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFocusAddressBar);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserHomePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionHome);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserFullscreenPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionFullscreen);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserShowNumbersPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowVisibleControls);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserShowGridPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowMouseGridHere);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserHideOverlaysPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideBrowserOverlays);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserDownloadsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionOpenDownloads);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserHistoryPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionOpenHistory);
            return true;
        }

        if (TryRouteAppScopedFindAction(command, out route))
            return true;

        if (TryRouteBrowserFindTextAction(command, out route))
            return true;

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

        if (TryStripAnyPrefix(command, BrowserStartScrollUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStartScrollUp);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserStartScrollDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStartScrollDown);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserStartScrollLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStartScrollLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserStartScrollRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStartScrollRight);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserStopScrollPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionStopScroll);
            return true;
        }

        if (TryRouteBrowserPageScrollCount(command, out route))
            return true;

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

        if (TryStripAnyPrefix(command, BrowserScrollLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserScrollRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionScrollRight);
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

        if (TryMatchWholeCommand(command, BrowserZoomInPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionZoomIn);
            return true;
        }

        if (TryMatchWholeCommand(command, BrowserZoomOutPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, BrowserActionZoomOut);
            return true;
        }

        if (TryMatchWholeCommand(command, BrowserZoomResetPrefixes))
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

        if (TryStripAnyPrefix(command, SystemMediaPlayPausePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMediaPlayPause);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMediaNextTrackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMediaNextTrack);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMediaPreviousTrackPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMediaPreviousTrack);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMediaStopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMediaStop);
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

        if (TryStripAnyPrefix(command, SystemTaskViewPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenTaskView);
            return true;
        }

        if (TryRouteVisibleFieldLabelAction(command, out route))
            return true;

        if (TryRouteModifierChordAction(command, out route))
            return true;

        if (TryParseNamedWindowSwitchCommand(command, out var requestedWindow))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionSwitchWindowPrefix}{requestedWindow}");
            return true;
        }

        if (TryStripAnyPrefix(command, SystemQuickSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenQuickSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNotificationCenterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenNotificationCenter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemEmojiPanelPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenEmojiPanel);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemClipboardHistoryPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenClipboardHistory);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSnippingToolbarPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenSnippingToolbar);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemProjectDisplayPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenProjectDisplay);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCastDisplayPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenCastDisplay);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNewVirtualDesktopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNewVirtualDesktop);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNextVirtualDesktopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNextVirtualDesktop);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPreviousVirtualDesktopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPreviousVirtualDesktop);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemTaskManagerPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenTaskManager);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDisplaySettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenDisplaySettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSoundSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenSoundSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemBluetoothSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenBluetoothSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemWifiSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenWifiSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNetworkSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenNetworkSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemAccessibilitySettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenAccessibilitySettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMagnifierSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenMagnifierSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNarratorSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenNarratorSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCaptionsSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenCaptionsSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSpeechSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenSpeechSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMagnifierZoomOutPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMagnifierZoomOut);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMagnifierClosePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCloseMagnifier);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMagnifierOpenPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenMagnifier);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenMouseSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemKeyboardSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenKeyboardSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPrivacySettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenPrivacySettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPowerSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenPowerSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemAppsSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenAppsSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDefaultAppsSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenDefaultAppsSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDateTimeSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenDateTimeSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNotificationsSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenNotificationsSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemWindowsUpdateSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenWindowsUpdateSettings);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPersonalizationSettingsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenPersonalizationSettings);
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

        if (TryStripAnyPrefix(command, SystemSnapWindowLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSnapWindowLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSnapWindowRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSnapWindowRight);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSnapWindowUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSnapWindowUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSnapWindowDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSnapWindowDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemShowSnapLayoutsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionShowSnapLayouts);
            return true;
        }

        if (IsBlockedKeyboardChordCommand(command))
            return false;

        if (TryRouteRepeatedKeyPressAction(command, out route))
            return true;

        if (TryRouteRepeatedDirectionalMoveAction(command, out route))
            return true;

        if (TryRouteHeldModifierAction(command, out route))
            return true;

        if (TryRouteFunctionKeyAction(command, out route))
            return true;

        if (TryRouteDigitKeyAction(command, out route))
            return true;

        if (TryRouteLetterKeyAction(command, out route))
            return true;

        if (TryRouteSymbolKeyAction(command, out route))
            return true;

        if (TryRouteModifierChordAction(command, out route))
            return true;

        if (TryRouteBareSingleKeyAction(command, out route))
            return true;

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

        if (TryStripAnyPrefix(command, SystemPressSpacePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressSpace);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressDeletePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressDelete);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressInsertPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressInsert);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressWindowsPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressWindows);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressContextMenuPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressContextMenu);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemPressCapsLockPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPressCapsLock);
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

        if (TryRouteCountedCharacterAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMovePreviousCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMovePreviousCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveNextCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveNextCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectPreviousCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectPreviousCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectNextCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectNextCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeletePreviousCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeletePreviousCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteNextCharacterPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteNextCharacter);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveSelectionStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveSelectionStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveSelectionEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveSelectionEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemClearSelectionPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionClearSelection);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectCurrentWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectCurrentWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectCurrentLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectCurrentLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectCurrentParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectCurrentParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteCurrentWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteCurrentWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteCurrentLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteCurrentLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteCurrentParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteCurrentParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyCurrentWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyCurrentWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyCurrentLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyCurrentLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyCurrentParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyCurrentParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutCurrentWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutCurrentWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutCurrentLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutCurrentLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutCurrentParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutCurrentParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyPreviousWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyPreviousWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyNextWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyNextWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutPreviousWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutPreviousWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutNextWordPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutNextWord);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyPreviousLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyPreviousLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyNextLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyNextLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutPreviousLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutPreviousLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutNextLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutNextLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyPreviousParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyPreviousParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCopyNextParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopyNextParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutPreviousParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutPreviousParagraph);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCutNextParagraphPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCutNextParagraph);
            return true;
        }

        if (TryRouteUnitFormatAction(command, out route))
        {
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveLineStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveLineStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveLineEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveLineEnd);
            return true;
        }

        if (TryRouteCountedLineAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMovePreviousLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMovePreviousLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveNextLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveNextLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectToLineStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectToLineStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectToLineEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectToLineEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectPreviousLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectPreviousLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectNextLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectNextLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteToLineStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteToLineStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteToLineEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteToLineEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeletePreviousLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeletePreviousLine);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteNextLinePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteNextLine);
            return true;
        }

        if (TryRouteCountedWordAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMoveWordStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveWordStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveWordEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveWordEnd);
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

        if (TryRouteCountedSentenceAction(command, out route))
            return true;

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

        if (TryRouteCountedParagraphAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMoveParagraphStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveParagraphStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMoveParagraphEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMoveParagraphEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectToParagraphStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectToParagraphStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemSelectToParagraphEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSelectToParagraphEnd);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteToParagraphStartPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteToParagraphStart);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemDeleteToParagraphEndPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionDeleteToParagraphEnd);
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

        if (TryRouteMousePointerAction(command, out route))
            return true;

        if (TryStripAnyPrefix(command, SystemMouseButtonDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseButtonDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseButtonUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseButtonUp);
            return true;
        }

        if (TryRouteVisibleSliderAction(command, out route))
            return true;

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

        if (TryStripAnyPrefix(command, SystemMouseTripleClickPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseTripleClick);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseRightClickPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseRightClick);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemScrollTopPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionScrollTop);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemScrollBottomPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionScrollBottom);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemScrollLeftEdgePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionScrollLeftEdge);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemScrollRightEdgePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionScrollRightEdge);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemStartScrollingUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionStartScrollingPrefix}up");
            return true;
        }

        if (TryStripAnyPrefix(command, SystemStartScrollingDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionStartScrollingPrefix}down");
            return true;
        }

        if (TryStripAnyPrefix(command, SystemStartScrollingLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionStartScrollingPrefix}left");
            return true;
        }

        if (TryStripAnyPrefix(command, SystemStartScrollingRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionStartScrollingPrefix}right");
            return true;
        }

        if (TryStripAnyPrefix(command, SystemStopScrollingPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionStopScrolling);
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

        if (TryStripAnyPrefix(command, SystemMouseScrollLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseScrollLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseScrollRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseScrollRight);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseMoveUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseMoveDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseMoveLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseMoveRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveRight);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseDragUpPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseDragUp);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseDragDownPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseDragDown);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseDragLeftPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseDragLeft);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemMouseDragRightPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseDragRight);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemCopyPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCopy);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemPastePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPaste);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemCutPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCut);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemSavePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSave);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemSaveAsPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionSaveAs);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemUndoPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionUndo);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemRedoPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionRedo);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemBoldPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionBold);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemItalicPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionItalic);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemUnderlinePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionUnderline);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemFindPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFind);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemFindNextPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFindNext);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemFindPreviousPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFindPrevious);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemNewWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNewWindow);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemNewDocumentPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionNewDocument);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemOpenFilePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionOpenFile);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemPrintPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionPrint);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemZoomInPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionZoomIn);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemZoomOutPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionZoomOut);
            return true;
        }

        if (TryMatchWholeCommand(command, SystemZoomResetPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionZoomReset);
            return true;
        }

        if (TryStripAnyPrefix(command, SystemCloseWindowPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCloseWindow);
            return true;
        }

        if (TryStripAnyPrefix(command, ChromeBrowserPrefixes, out var chromeTarget))
        {
            if (IsBrowserTabSelectionText(chromeTarget))
                return false;

            route = new AlphaCommandRoute(AlphaCommandKind.Browser, TrimBrowserTargetLeadIn(chromeTarget), BrowserOpenTarget.Chrome);
            return true;
        }

        if (TryStripAnyPrefix(command, BrowserPrefixes, out var browserTarget))
        {
            if (IsBrowserTabSelectionText(browserTarget))
                return false;

            route = new AlphaCommandRoute(AlphaCommandKind.Browser, TrimBrowserTargetLeadIn(browserTarget));
            return true;
        }

        if (TryStripAnyPrefix(command, FileSearchPrefixes, out var fileSearchQuery))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.FileSearch, fileSearchQuery);
            return true;
        }

        if (AlphaVoiceTranscriptParser.TryParseDictationInsertTextCommand(command, out var insertedText))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Dictation, DictationInsertTextActionPrefix + insertedText);
            return true;
        }

        if (AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand(command, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Dictation, string.Empty);
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

    private static bool TryRouteBrowserFindTextAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        if (!TryStripAnyPrefix(command, BrowserFindTextPrefixes, out var searchText)
            && !TryStripBrowserFindTextSuffix(command, out searchText))
            return false;

        var normalizedSearchText = AlphaVoiceTranscriptParser.NormalizeSpeechText(searchText);
        normalizedSearchText = TrimBrowserFindTextLeadIn(normalizedSearchText);
        normalizedSearchText = TrimBrowserFindTextSuffix(normalizedSearchText);
        if (string.IsNullOrWhiteSpace(normalizedSearchText))
            return false;

        if (LooksLikeFileSearchLeadIn(normalizedSearchText))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.Browser, $"{BrowserActionFindTextPrefix}{normalizedSearchText}");
        return true;
    }

    private static bool TryRouteAppScopedFindAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryStripAnyPrefix(command, ["find in app", "find in this app", "search in app", "search this app"], out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFind);
            return true;
        }

        if (TryMatchWholeCommand(command, ["find next in app", "find next in this app", "next match in app", "next match in this app"]))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFindNext);
            return true;
        }

        if (TryMatchWholeCommand(command, ["find previous in app", "find previous in this app", "previous match in app", "previous match in this app"]))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionFindPrevious);
            return true;
        }

        return false;
    }

    private static bool TryRouteBrowserAddressTextAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        if (!TryStripAnyPrefix(command, BrowserAddressTextPrefixes, out var addressText)
            && !TryStripBrowserAddressTextSuffix(command, out addressText))
            return false;

        var normalizedAddressText = AlphaVoiceTranscriptParser.NormalizeSpeechText(addressText);
        normalizedAddressText = TrimBrowserAddressTextLeadIn(normalizedAddressText);
        if (string.IsNullOrWhiteSpace(normalizedAddressText))
            return false;

        if (IsBrowserTabSelectionText(normalizedAddressText))
        {
            return false;
        }

        route = new AlphaCommandRoute(AlphaCommandKind.Browser, $"{BrowserActionAddressTextPrefix}{normalizedAddressText}");
        return true;
    }

    private static bool IsBrowserTabSelectionText(string value)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        return normalized.StartsWith("tab ", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("browser tab ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryRouteBrowserSelectTab(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        foreach (var prefix in BrowserSelectTabPrefixes)
        {
            if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var tabText = command[prefix.Length..]
                .Replace(" tab", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!TryParseResultNumber(tabText, out var tabNumber) || tabNumber is < 1 or > 9)
                return false;

            route = new AlphaCommandRoute(AlphaCommandKind.Browser, $"{BrowserActionSelectTabPrefix}{tabNumber}");
            return true;
        }

        return false;
    }

    private static bool TryStripBrowserFindTextSuffix(string command, out string searchText)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var suffix in BrowserFindTextSuffixes)
        {
            if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var candidate = normalized[..^suffix.Length].Trim();
            foreach (var prefix in new[] { "find ", "search for ", "look for " })
            {
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                searchText = candidate[prefix.Length..].Trim();
                return !string.IsNullOrWhiteSpace(searchText);
            }
        }

        searchText = string.Empty;
        return false;
    }

    private static bool TryStripBrowserAddressTextSuffix(string command, out string addressText)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var suffix in BrowserAddressTextSuffixes)
        {
            if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var candidate = normalized[..^suffix.Length].Trim();
            foreach (var prefix in new[] { "type ", "enter ", "put ", "write " })
            {
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                addressText = candidate[prefix.Length..].Trim();
                return !string.IsNullOrWhiteSpace(addressText);
            }
        }

        addressText = string.Empty;
        return false;
    }

    private static string TrimBrowserFindTextLeadIn(string value)
    {
        var trimmed = value.Trim();
        foreach (var prefix in new[] { "for ", "text ", "the text ", "term ", "phrase ", "word " })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefix.Length..].Trim();
        }

        return trimmed;
    }

    private static string TrimBrowserFindTextSuffix(string value)
    {
        var trimmed = value.Trim();
        foreach (var suffix in BrowserFindTextSuffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return trimmed[..^suffix.Length].Trim();
        }

        return trimmed;
    }

    private static bool LooksLikeFileSearchLeadIn(string value)
    {
        var trimmed = value.Trim();
        foreach (var prefix in new[]
                 {
                     "file ",
                     "folder ",
                     "files ",
                     "folders ",
                     "my file ",
                     "my files ",
                     "my folder ",
                     "my folders ",
                     "my documents ",
                     "my pc ",
                     "my computer ",
                     "this pc ",
                     "the file ",
                     "the folder ",
                     "open file ",
                     "open folder ",
                     "search explorer ",
                     "explorer "
                 })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string TrimBrowserAddressTextLeadIn(string value)
    {
        var trimmed = value.Trim();
        foreach (var prefix in new[] { "for ", "to ", "as ", "the web for ", "web for ", "search for ", "look up " })
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

    private static bool TryMatchWholeCommand(string command, IReadOnlyList<string> phrases)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        return phrases.Any(phrase => normalized.Equals(
            AlphaVoiceTranscriptParser.NormalizeSpeechText(phrase),
            StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryRouteVisibleControlLabelAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        foreach (var prefix in UiVisibleControlDoubleClickLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionDoubleClickLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionDoubleClickLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlTripleClickLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionTripleClickLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionTripleClickLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlRightClickLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionRightClickLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionRightClickLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlToggleLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionToggleLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionToggleLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlExpandLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionExpandLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionExpandLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlCollapseLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionCollapseLabelPrefix}{controlNumber}");
                return true;
            }

            var normalizedLabel = NormalizeVisibleControlLabel(remainder);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                continue;

            if (IsListeningLabel(normalizedLabel))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionCollapseLabelPrefix}{normalizedLabel}");
            return true;
        }

        foreach (var prefix in UiVisibleControlLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (TryParseVisibleControlNumber(remainder, out var controlNumber))
            {
                route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionActivateLabelPrefix}{controlNumber}");
                return true;
            }

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

    private static bool TryRouteVisibleFieldLabelAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        foreach (var prefix in UiVisibleFieldLabelPrefixes)
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

        foreach (var prefix in UiVisibleFieldSuffixLabelPrefixes)
        {
            if (!TryStripAnyPrefix(command, new[] { prefix }, out var remainder))
                continue;

            if (!HasVisibleFieldLabelSuffix(remainder))
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

    private static bool TryRouteVisibleSliderAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:move|adjust|slide)\s+(?:(?<label>.+?)\s+)?slider\s+(?<direction>up|down|left|right)(?:\s+(?<count>.+?))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var direction = match.Groups["direction"].Value.Trim().ToLowerInvariant();
        var label = NormalizeVisibleControlLabel(match.Groups["label"].Value.Trim());
        var countText = match.Groups["count"].Value.Trim();
        var steps = 1;
        if (!string.IsNullOrWhiteSpace(countText))
        {
            foreach (var suffix in new[] { " times", " time", " steps", " step", " notches", " notch" })
            {
                if (countText.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    countText = countText[..^suffix.Length].Trim();
                    break;
                }
            }

            if (!TryParseResultNumber(countText, out steps))
                return false;
        }

        if (steps is < 1 or > 10)
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionMoveSliderPrefix}{direction}:{steps}:{label}");
        return true;
    }

    private static bool TryParseVisibleControlNumber(string value, out int controlNumber)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        normalized = TrimVisibleControlNumberSpeech(normalized);

        if (int.TryParse(normalized, out controlNumber) && controlNumber is >= 1 and <= 40)
            return true;

        if (TryParseSpokenVisibleControlNumber(normalized, out controlNumber))
            return true;

        controlNumber = 0;
        return false;
    }

    private static string TrimVisibleControlNumberSpeech(string value)
    {
        var normalized = value.Trim();
        foreach (var prefix in new[]
                 {
                     "visible control ",
                     "control number ",
                     "button number ",
                     "field number ",
                     "item number ",
                     "option number ",
                     "number ",
                     "control ",
                     "button ",
                     "field ",
                     "item ",
                     "option "
                 })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        foreach (var suffix in new[]
                 {
                     " visible control",
                     " control",
                     " button",
                     " field",
                     " item",
                     " option"
                 })
        {
            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^suffix.Length].Trim();
                break;
            }
        }

        return normalized;
    }

    private static bool TryParseSpokenVisibleControlNumber(string value, out int controlNumber)
    {
        controlNumber = value switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            "ten" or "tenth" => 10,
            "eleven" or "eleventh" => 11,
            "twelve" or "twelfth" => 12,
            "thirteen" or "thirteenth" => 13,
            "fourteen" or "fourteenth" => 14,
            "fifteen" or "fifteenth" => 15,
            "sixteen" or "sixteenth" => 16,
            "seventeen" or "seventeenth" => 17,
            "eighteen" or "eighteenth" => 18,
            "nineteen" or "nineteenth" => 19,
            "twenty" or "twentieth" => 20,
            "thirty" or "thirtieth" => 30,
            "forty" or "fortieth" => 40,
            _ => 0
        };

        if (controlNumber > 0)
            return true;

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length != 2)
            return false;

        var tens = tokens[0] switch
        {
            "twenty" => 20,
            "thirty" => 30,
            _ => 0
        };
        var ones = tokens[1] switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            _ => 0
        };

        controlNumber = tens + ones;
        return tens > 0 && ones > 0 && controlNumber <= 40;
    }

    private static bool TryRouteMouseGridAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryStripAnyPrefix(command, UiShowMouseGridHerePrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowMouseGridHere);
            return true;
        }

        if (TryParseMouseGridDisplayPath(command, out var displayIdentifier, out var pathDigits))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionFocusMouseGridPathPrefix}{displayIdentifier}:{pathDigits}");
            return true;
        }

        if (TryParseMouseGridShortcutPath(command, out var shortcutDigits))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionFocusMouseGridShortcutPathPrefix}{shortcutDigits}");
            return true;
        }

        if (TryParseMouseGridDisplayCommand(command, out displayIdentifier))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionFocusMouseGridDisplayPrefix}{displayIdentifier}");
            return true;
        }

        if (TryStripAnyPrefix(command, UiShowMouseGridPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowMouseGrid);
            return true;
        }

        if (TryStripAnyPrefix(command, UiHideMouseGridPrefixes, out _))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionHideMouseGrid);
            return true;
        }

        if (TryMatchWholeCommand(command, UiUndoMouseGridPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionUndoMouseGrid);
            return true;
        }

        if (TryMatchWholeCommand(command, UiMarkMouseGridPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionMarkMouseGrid);
            return true;
        }

        if (TryParseGridCellCommand(command, UiMarkMouseGridCellPrefixes, out var markCell))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionMarkMouseGridCellPrefix}{markCell}");
            return true;
        }

        if (TryParseGridCellCommand(command, UiClickMouseGridCellPrefixes, out var clickCell))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionClickMouseGridCellPrefix}{clickCell}");
            return true;
        }

        if (TryParseGridDragCommand(command, out var dragFromCell, out var dragToCell))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionDragMouseGridPrefix}{dragFromCell}:{dragToCell}");
            return true;
        }

        if (TryParseGridCellCommand(command, UiSelectMouseGridCellPrefixes, out var selectCell))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSelectMouseGridCellPrefix}{selectCell}");
            return true;
        }

        if (TryMatchWholeCommand(command, UiDragMarkedMouseGridPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionDragMarkedMouseGrid);
            return true;
        }

        return false;
    }

    private static bool TryRouteBrowserPageScrollCount(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryParseBrowserPageScrollCount(command, BrowserScrollUpPageCountPrefixes, out var upCount))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, $"{BrowserActionScrollUpPagesPrefix}{upCount}");
            return true;
        }

        if (TryParseBrowserPageScrollCount(command, BrowserScrollDownPageCountPrefixes, out var downCount))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.Browser, $"{BrowserActionScrollDownPagesPrefix}{downCount}");
            return true;
        }

        return false;
    }

    private static bool TryParseBrowserPageScrollCount(string command, IReadOnlyList<string> prefixes, out int pageCount)
    {
        pageCount = 0;
        foreach (var prefix in prefixes)
        {
            if (!TryStripAnyPrefix(command, [prefix], out var remainder))
                continue;

            var normalized = remainder
                .Replace(" pages", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" page", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (string.Equals(normalized, "twice", StringComparison.OrdinalIgnoreCase))
                normalized = "two";

            if (!TryParseResultNumber(normalized, out pageCount))
                return false;

            if (pageCount < 2)
                return false;

            pageCount = Math.Min(pageCount, 5);
            return true;
        }

        return false;
    }

    private static bool TryRouteVoiceModeAction(string command, out AlphaCommandRoute route)
    {
        if (TryMatchWholeCommand(command, UiCommandsOnlyModePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSetVoiceModePrefix}commands");
            return true;
        }

        if (TryMatchWholeCommand(command, UiDictationOnlyModePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSetVoiceModePrefix}dictation");
            return true;
        }

        if (TryMatchWholeCommand(command, UiDefaultVoiceModePrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSetVoiceModePrefix}default");
            return true;
        }

        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        return false;
    }

    private static bool TryRouteVisibleControlAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryParseNamedVisibleControlsCommand(command, out var windowTarget))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionShowVisibleControlsWindowPrefix}{windowTarget}");
            return true;
        }

        if (TryMatchWholeCommand(command, UiShowTaskbarVisibleControlsPrefixes))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, UiActionShowVisibleControlsTaskbar);
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

        return false;
    }

    private static bool TryParseNamedVisibleControlsCommand(string command, out string windowTarget)
    {
        windowTarget = string.Empty;
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:show\s+numbers|show\s+control\s+numbers|show\s+names|show\s+labels|show\s+all\s+labels)\s+(?:on|for|in)\s+(?<target>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        windowTarget = match.Groups["target"].Value.Trim();
        if (string.IsNullOrWhiteSpace(windowTarget))
            return false;

        if (string.Equals(windowTarget, "taskbar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(windowTarget, "the taskbar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(windowTarget, "here", StringComparison.OrdinalIgnoreCase)
            || string.Equals(windowTarget, "everywhere", StringComparison.OrdinalIgnoreCase)
            || string.Equals(windowTarget, "this window", StringComparison.OrdinalIgnoreCase)
            || string.Equals(windowTarget, "current window", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool TryRouteVocabularyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:add\s+(?:word|phrase|term)\s+(?<word>.+?)\s+to\s+(?:my\s+)?(?:dictation\s+)?vocabulary|add\s+(?<word>.+?)\s+to\s+(?:my\s+)?(?:dictation\s+)?vocabulary|add\s+to\s+(?:my\s+)?(?:dictation\s+)?vocabulary\s+(?<word>.+?)|add\s+(?:word|phrase|term)\s+(?<word>.+?)\s+to\s+(?:my\s+)?dictionary|add\s+(?<word>.+?)\s+to\s+(?:my\s+)?dictionary|add\s+to\s+(?:my\s+)?dictionary\s+(?<word>.+?))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var word = match.Groups["word"].Value.Trim();
        var normalizedWord = DictationVocabularyService.NormalizeEntry(word);
        if (string.IsNullOrWhiteSpace(normalizedWord))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionAddVocabularyPrefix}{normalizedWord}");
        return true;
    }

    private static bool TryRouteDictationOptionAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var setting = normalized switch
        {
            "turn on fluid dictation" or "fluid dictation on" or "enable fluid dictation" or "start fluid dictation" => "fluid-dictation:on",
            "turn off fluid dictation" or "fluid dictation off" or "disable fluid dictation" or "stop fluid dictation" => "fluid-dictation:off",
            "turn on automatic punctuation" or "automatic punctuation on" or "enable automatic punctuation" or "start automatic punctuation" or "turn on auto punctuation" or "auto punctuation on" or "enable auto punctuation" => "automatic-punctuation:on",
            "turn off automatic punctuation" or "automatic punctuation off" or "disable automatic punctuation" or "stop automatic punctuation" or "turn off auto punctuation" or "auto punctuation off" or "disable auto punctuation" => "automatic-punctuation:off",
            "turn on profanity filter" or "profanity filter on" or "enable profanity filter" or "filter profanity" or "hide profanity" => "profanity-filter:on",
            "turn off profanity filter" or "profanity filter off" or "disable profanity filter" or "do not filter profanity" or "show profanity" => "profanity-filter:off",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(setting))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSetDictationOptionPrefix}{setting}");
        return true;
    }

    private static bool TryRouteFileResultAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);

        if (TryParseFileResultCommand(command, UiOpenFileResultPrefixes, out var openNumber))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionOpenFileResultPrefix}{openNumber}");
            return true;
        }

        if (TryParseFileResultCommand(command, UiRevealFileResultPrefixes, out var revealNumber))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionRevealFileResultPrefix}{revealNumber}");
            return true;
        }

        if (TryParseFileResultCommand(command, UiSelectFileResultPrefixes, out var selectNumber))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.UiAction, $"{UiActionSelectFileResultPrefix}{selectNumber}");
            return true;
        }

        return false;
    }

    private static bool TryParseFileResultCommand(string command, IReadOnlyList<string> prefixes, out int resultNumber)
    {
        foreach (var prefix in prefixes)
        {
            if (!TryStripAnyPrefix(command, [prefix], out var remainder))
                continue;

            return TryParseResultNumber(remainder, out resultNumber);
        }

        resultNumber = 0;
        return false;
    }

    private static bool TryParseResultNumber(string value, out int resultNumber)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        normalized = TrimFileResultSpeech(normalized);
        if (int.TryParse(normalized, out resultNumber) && resultNumber > 0)
            return true;

        resultNumber = normalized switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            "ten" or "tenth" => 10,
            "eleven" or "eleventh" => 11,
            "twelve" or "twelfth" => 12,
            "thirteen" or "thirteenth" => 13,
            "fourteen" or "fourteenth" => 14,
            "fifteen" or "fifteenth" => 15,
            "sixteen" or "sixteenth" => 16,
            "seventeen" or "seventeenth" => 17,
            "eighteen" or "eighteenth" => 18,
            "nineteen" or "nineteenth" => 19,
            "twenty" or "twentieth" => 20,
            "thirty" or "thirtieth" => 30,
            "forty" or "fortieth" => 40,
            _ => 0
        };

        if (resultNumber > 0)
            return true;

        return TryParseCompoundResultNumber(normalized, out resultNumber);
    }

    private static bool TryParseNamedWindowSwitchCommand(string command, out string requestedWindow)
    {
        requestedWindow = string.Empty;
        foreach (var prefix in SystemNamedWindowSwitchPrefixes)
        {
            if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = command[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(remainder))
                return false;

            if (remainder is "the next window"
                or "next window"
                or "the next app"
                or "next app"
                or "the next application"
                or "next application"
                or "the previous window"
                or "previous window"
                or "the previous app"
                or "previous app"
                or "the previous application"
                or "previous application"
                or "desktop"
                or "the desktop")
            {
                return false;
            }

            if (remainder.StartsWith("line ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("word ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("sentence ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("paragraph ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("character ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("tab ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the line ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the word ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the sentence ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the paragraph ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the character ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the tab ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("beginning", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the beginning", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("end", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("the end", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("top", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("bottom", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("last ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("previous ", StringComparison.OrdinalIgnoreCase)
                || remainder.StartsWith("next ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            requestedWindow = remainder;
            return true;
        }

        return false;
    }

    private static bool TryParseCompoundResultNumber(string value, out int resultNumber)
    {
        resultNumber = 0;
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length != 2)
            return false;

        var tens = tokens[0] switch
        {
            "twenty" => 20,
            "thirty" => 30,
            _ => 0
        };
        var ones = tokens[1] switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            _ => 0
        };

        resultNumber = tens + ones;
        return tens > 0 && ones > 0 && resultNumber <= 40;
    }

    private static string TrimFileResultSpeech(string value)
    {
        var normalized = value.Trim();
        foreach (var prefix in new[] { "file result ", "folder result ", "search result ", "result ", "number " })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[prefix.Length..].Trim();
        }

        foreach (var suffix in new[] { " file result", " folder result", " search result", " result" })
        {
            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[..^suffix.Length].Trim();
        }

        return normalized;
    }

    private static bool TryParseGridDragCommand(string command, out int fromCell, out int toCell)
    {
        fromCell = 0;
        toCell = 0;

        foreach (var prefix in UiDragMouseGridCellPrefixes)
        {
            if (!TryStripAnyPrefix(command, [prefix], out var remainder))
                continue;

            var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(remainder);
            foreach (var separator in new[] { " to ", " into ", " onto " })
            {
                var separatorIndex = normalized.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
                if (separatorIndex <= 0)
                    continue;

                var fromText = TrimGridCellSpeech(normalized[..separatorIndex].Trim());
                var toText = TrimGridCellSpeech(normalized[(separatorIndex + separator.Length)..].Trim());
                return TryParseGridCellNumber(fromText, out fromCell)
                    && TryParseGridCellNumber(toText, out toCell);
            }
        }

        return false;
    }

    private static string TrimGridCellSpeech(string value)
    {
        var normalized = value.Trim();
        foreach (var prefix in new[] { "mouse grid cell ", "grid cell ", "grid square ", "grid ", "cell ", "square ", "number " })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return normalized[prefix.Length..].Trim();
        }

        return normalized;
    }

    private static bool TryParseMouseGridDisplayPath(string command, out string displayIdentifier, out string pathDigits)
    {
        displayIdentifier = string.Empty;
        pathDigits = string.Empty;

        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:mouse\s+grid|grid)\s+(?<display>[a-z\-]+)\s+(?<path>[1-9]{1,6})\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        if (!TryNormalizeMouseGridDisplayIdentifier(match.Groups["display"].Value, out displayIdentifier))
            return false;

        pathDigits = match.Groups["path"].Value.Trim();
        return pathDigits.Length > 0;
    }

    private static bool TryParseMouseGridShortcutPath(string command, out string pathDigits)
    {
        pathDigits = string.Empty;
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:mouse\s+grid|grid)\s+(?<path>(?:[1-9]\s*){2,6})\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        pathDigits = string.Concat(match.Groups["path"].Value.Where(character => character is >= '1' and <= '9'));
        return pathDigits.Length >= 2;
    }

    private static bool TryParseMouseGridDisplayCommand(string command, out string displayIdentifier)
    {
        displayIdentifier = string.Empty;
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:mouse\s+grid|grid|show\s+mouse\s+grid|show\s+grid)\s+(?<display>[a-z\-]+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        return match.Success
            && TryNormalizeMouseGridDisplayIdentifier(match.Groups["display"].Value, out displayIdentifier);
    }

    private static bool TryNormalizeMouseGridDisplayIdentifier(string value, out string displayIdentifier)
    {
        displayIdentifier = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        if (normalized.Length == 1 && normalized[0] is >= 'a' and <= 'z')
        {
            displayIdentifier = normalized.ToUpperInvariant();
            return true;
        }

        displayIdentifier = normalized switch
        {
            "alpha" => "A",
            "bravo" => "B",
            "charlie" => "C",
            "delta" => "D",
            "echo" => "E",
            "foxtrot" => "F",
            "golf" => "G",
            "hotel" => "H",
            "india" => "I",
            "juliett" or "juliet" => "J",
            "kilo" => "K",
            "lima" => "L",
            "mike" => "M",
            "november" => "N",
            "oscar" => "O",
            "papa" => "P",
            "quebec" => "Q",
            "romeo" => "R",
            "sierra" => "S",
            "tango" => "T",
            "uniform" => "U",
            "victor" => "V",
            "whiskey" => "W",
            "x ray" or "xray" => "X",
            "yankee" => "Y",
            "zulu" => "Z",
            _ => string.Empty
        };

        return displayIdentifier.Length == 1;
    }

    private static bool TryParseGridCellCommand(string command, IReadOnlyList<string> prefixes, out int cellNumber)
    {
        foreach (var prefix in prefixes)
        {
            if (!TryStripAnyPrefix(command, [prefix], out var remainder))
                continue;

            return TryParseGridCellNumber(remainder, out cellNumber);
        }

        cellNumber = 0;
        return false;
    }

    private static bool TryParseGridCellNumber(string value, out int cellNumber)
    {
        value = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        if (int.TryParse(value, out cellNumber) && cellNumber is >= 1 and <= 9)
            return true;

        cellNumber = value switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            _ => 0
        };
        return cellNumber is >= 1 and <= 9;
    }

    private static bool TryRouteFunctionKeyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var prefix in new[] { "press ", "hit ", "tap ", "function key ", "function ", "f key " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var keyText = normalized[prefix.Length..].Trim();
            if ((prefix.Equals("press ", StringComparison.OrdinalIgnoreCase)
                    || prefix.Equals("hit ", StringComparison.OrdinalIgnoreCase)
                    || prefix.Equals("tap ", StringComparison.OrdinalIgnoreCase))
                && !keyText.StartsWith("f", StringComparison.OrdinalIgnoreCase)
                && !keyText.StartsWith("function", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryParseFunctionKeyNumber(keyText, out var functionKeyNumber))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-press-f{functionKeyNumber}");
            return true;
        }

        return false;
    }

    private static bool TryRouteRepeatedKeyPressAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:press|hit|tap)\s+(?<key>.+?)\s+(?<count>.+?)\s+times?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var keyText = match.Groups["key"].Value.Trim();
        var countText = match.Groups["count"].Value.Trim();
        if (!TryParseResultNumber(countText, out var count) || count is < 2 or > 20)
            return false;

        if (!TryRouteRepeatableSingleKeyAction(keyText, out var repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteRepeatedDirectionalMoveAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:move|go)\s+(?<direction>up|down|left|right)\s+(?<count>.+?)\s+times?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var direction = match.Groups["direction"].Value.Trim();
        var countText = match.Groups["count"].Value.Trim();
        if (!TryParseResultNumber(countText, out var count) || count is < 2 or > 20)
            return false;

        var repeatedAction = direction switch
        {
            "up" => SystemActionPressUp,
            "down" => SystemActionPressDown,
            "left" => SystemActionPressLeft,
            "right" => SystemActionPressRight,
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteCountedWordAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<verb>move|go|select|highlight|delete|remove|copy|cut)\s+(?:to\s+)?(?<direction>previous|last|backward|left|next|forward|right)\s+(?<count>.+?)\s+words?|(?<directionOnly>previous|last|backward|left|next|forward|right)\s+(?<directionCount>.+?)\s+words?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var verb = match.Groups["verb"].Success ? match.Groups["verb"].Value.Trim() : "move";
        var direction = match.Groups["direction"].Success
            ? match.Groups["direction"].Value.Trim()
            : match.Groups["directionOnly"].Value.Trim();
        var countText = match.Groups["count"].Success
            ? match.Groups["count"].Value.Trim()
            : match.Groups["directionCount"].Value.Trim();

        direction = direction switch
        {
            "last" or "backward" or "left" => "previous",
            "forward" or "right" => "next",
            _ => direction
        };

        if (!TryParseResultNumber(countText, out var count) || count < 2)
            return false;

        count = Math.Min(count, 10);
        if (verb is "copy" or "cut")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-{verb}-{direction}-words:{count}");
            return true;
        }

        var repeatedAction = (verb, direction) switch
        {
            ("move" or "go", "previous") => SystemActionMovePreviousWord,
            ("move" or "go", "next") => SystemActionMoveNextWord,
            ("select" or "highlight", "previous") => SystemActionSelectPreviousWord,
            ("select" or "highlight", "next") => SystemActionSelectNextWord,
            ("delete" or "remove", "previous") => SystemActionDeletePreviousWord,
            ("delete" or "remove", "next") => SystemActionDeleteNextWord,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteUnitFormatAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<style>bold|boldface|italic|italicize|underline)\s+(?:(?<scope>current|this|previous|last|next)\s+)?(?<unit>word|line|paragraph)|make\s+(?:(?<reverseScope>current|this|previous|last|next)\s+)?(?<reverseUnit>word|line|paragraph)\s+(?<reverseStyle>bold|boldface|italic|italicized|underline|underlined))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var styleText = match.Groups["style"].Success
            ? match.Groups["style"].Value
            : match.Groups["reverseStyle"].Value;
        var unit = match.Groups["unit"].Success
            ? match.Groups["unit"].Value
            : match.Groups["reverseUnit"].Value;
        var scopeText = match.Groups["scope"].Success
            ? match.Groups["scope"].Value
            : match.Groups["reverseScope"].Value;

        var style = styleText switch
        {
            "bold" or "boldface" => "bold",
            "italic" or "italicize" or "italicized" => "italic",
            "underline" or "underlined" => "underline",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(style))
            return false;

        var scope = scopeText switch
        {
            "" or "current" or "this" => "current",
            "previous" or "last" => "previous",
            "next" => "next",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-format-{style}-{scope}-{unit}");
        return true;
    }

    private static bool TryRouteCountedSentenceAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<verb>move|go|select|highlight|delete|remove)\s+(?:to\s+)?(?<direction>previous|next)\s+(?<count>.+?)\s+sentences?|(?<directionOnly>previous|next)\s+(?<directionCount>.+?)\s+sentences?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var verb = match.Groups["verb"].Success ? match.Groups["verb"].Value.Trim() : "move";
        var direction = match.Groups["direction"].Success
            ? match.Groups["direction"].Value.Trim()
            : match.Groups["directionOnly"].Value.Trim();
        var countText = match.Groups["count"].Success
            ? match.Groups["count"].Value.Trim()
            : match.Groups["directionCount"].Value.Trim();

        if (!TryParseResultNumber(countText, out var count) || count < 2)
            return false;

        count = Math.Min(count, 5);
        var repeatedAction = (verb, direction) switch
        {
            ("move" or "go", "previous") => SystemActionMovePreviousSentence,
            ("move" or "go", "next") => SystemActionMoveNextSentence,
            ("select" or "highlight", "previous") => SystemActionSelectPreviousSentence,
            ("select" or "highlight", "next") => SystemActionSelectNextSentence,
            ("delete" or "remove", "previous") => SystemActionDeletePreviousSentence,
            ("delete" or "remove", "next") => SystemActionDeleteNextSentence,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteCountedParagraphAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<verb>move|go|select|highlight|delete|remove|copy|cut)\s+(?:to\s+)?(?<direction>previous|last|backward|up|back|next|forward|down)\s+(?<count>.+?)\s+paragraphs?|(?<directionOnly>previous|last|backward|up|back|next|forward|down)\s+(?<directionCount>.+?)\s+paragraphs?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var verb = match.Groups["verb"].Success ? match.Groups["verb"].Value.Trim() : "move";
        var direction = match.Groups["direction"].Success
            ? match.Groups["direction"].Value.Trim()
            : match.Groups["directionOnly"].Value.Trim();
        direction = direction switch
        {
            "last" or "backward" or "up" or "back" => "previous",
            "forward" or "down" => "next",
            _ => direction
        };

        var countText = match.Groups["count"].Success
            ? match.Groups["count"].Value.Trim()
            : match.Groups["directionCount"].Value.Trim();

        if (!TryParseResultNumber(countText, out var count) || count < 2)
            return false;

        count = Math.Min(count, 3);
        if (verb is "copy" or "cut")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-{verb}-{direction}-paragraphs:{count}");
            return true;
        }

        var repeatedAction = (verb, direction) switch
        {
            ("move" or "go", "previous") => SystemActionMovePreviousParagraph,
            ("move" or "go", "next") => SystemActionMoveNextParagraph,
            ("select" or "highlight", "previous") => SystemActionSelectPreviousParagraph,
            ("select" or "highlight", "next") => SystemActionSelectNextParagraph,
            ("delete" or "remove", "previous") => SystemActionDeletePreviousParagraph,
            ("delete" or "remove", "next") => SystemActionDeleteNextParagraph,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteCountedCharacterAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<verb>move|go|select|highlight|delete|remove|copy|cut)\s+(?:to\s+)?(?<direction>previous|last|backward|left|next|forward|right)\s+(?<count>.+?)\s+(?:characters?|letters?|chars?)|(?<directionOnly>previous|last|backward|left|next|forward|right)\s+(?<directionCount>.+?)\s+(?:characters?|letters?|chars?))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var verb = match.Groups["verb"].Success ? match.Groups["verb"].Value.Trim() : "move";
        var direction = match.Groups["direction"].Success
            ? match.Groups["direction"].Value.Trim()
            : match.Groups["directionOnly"].Value.Trim();
        direction = direction switch
        {
            "last" or "backward" or "left" => "previous",
            "forward" or "right" => "next",
            _ => direction
        };

        var countText = match.Groups["count"].Success
            ? match.Groups["count"].Value.Trim()
            : match.Groups["directionCount"].Value.Trim();

        if (!TryParseResultNumber(countText, out var count) || count < 2)
            return false;

        count = Math.Min(count, 20);
        if (verb is "copy" or "cut")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-{verb}-{direction}-characters:{count}");
            return true;
        }

        var repeatedAction = (verb, direction) switch
        {
            ("move" or "go", "previous") => SystemActionMovePreviousCharacter,
            ("move" or "go", "next") => SystemActionMoveNextCharacter,
            ("select" or "highlight", "previous") => SystemActionSelectPreviousCharacter,
            ("select" or "highlight", "next") => SystemActionSelectNextCharacter,
            ("delete" or "remove", "previous") => SystemActionDeletePreviousCharacter,
            ("delete" or "remove", "next") => SystemActionDeleteNextCharacter,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteCountedLineAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        var match = Regex.Match(
            normalized,
            @"^\s*(?:system\s+)?(?:(?<verb>move|go|select|highlight|delete|remove|copy|cut)\s+(?:to\s+)?(?<direction>previous|last|backward|up|back|next|forward|down)\s+(?<count>.+?)\s+lines?|(?<directionOnly>previous|last|backward|up|back|next|forward|down)\s+(?<directionCount>.+?)\s+lines?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (!match.Success)
            return false;

        var verb = match.Groups["verb"].Success ? match.Groups["verb"].Value.Trim() : "move";
        var direction = match.Groups["direction"].Success
            ? match.Groups["direction"].Value.Trim()
            : match.Groups["directionOnly"].Value.Trim();
        var countText = match.Groups["count"].Success
            ? match.Groups["count"].Value.Trim()
            : match.Groups["directionCount"].Value.Trim();

        direction = direction switch
        {
            "last" or "backward" or "up" or "back" => "previous",
            "forward" or "down" => "next",
            _ => direction
        };

        if (!TryParseResultNumber(countText, out var count) || count < 2)
            return false;

        count = Math.Min(count, 10);
        if (verb is "copy" or "cut")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-{verb}-{direction}-lines:{count}");
            return true;
        }

        var repeatedAction = (verb, direction) switch
        {
            ("move" or "go", "previous") => SystemActionMovePreviousLine,
            ("move" or "go", "next") => SystemActionMoveNextLine,
            ("select" or "highlight", "previous") => SystemActionSelectPreviousLine,
            ("select" or "highlight", "next") => SystemActionSelectNextLine,
            ("delete" or "remove", "previous") => SystemActionDeletePreviousLine,
            ("delete" or "remove", "next") => SystemActionDeleteNextLine,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(repeatedAction))
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionRepeatPrefix}{repeatedAction}:{count}");
        return true;
    }

    private static bool TryRouteRepeatableSingleKeyAction(string keyText, out string action)
    {
        if (TryRouteFunctionKeyAction($"press {keyText}", out var route)
            || TryRouteDigitKeyAction($"press {keyText}", out route)
            || TryRouteLetterKeyAction($"press {keyText}", out route)
            || TryRouteSymbolKeyAction($"press {keyText}", out route))
        {
            action = route.Target;
            return true;
        }

        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(keyText);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        action = normalized switch
        {
            "enter" or "enter key" or "return" or "return key" => SystemActionPressEnter,
            "tab" or "tab key" => SystemActionPressTab,
            "escape" or "escape key" or "esc" or "esc key" => SystemActionPressEscape,
            "backspace" or "backspace key" => SystemActionPressBackspace,
            "space" or "space key" or "space bar" => SystemActionPressSpace,
            "delete" or "delete key" => SystemActionPressDelete,
            "insert" or "insert key" => SystemActionPressInsert,
            "windows" or "windows key" => SystemActionPressWindows,
            "context menu" or "context menu key" or "menu key" => SystemActionPressContextMenu,
            "caps lock" or "caps lock key" => SystemActionPressCapsLock,
            "up" or "up arrow" => SystemActionPressUp,
            "down" or "down arrow" => SystemActionPressDown,
            "left" or "left arrow" => SystemActionPressLeft,
            "right" or "right arrow" => SystemActionPressRight,
            "home" or "home key" => SystemActionPressHome,
            "end" or "end key" => SystemActionPressEnd,
            "page up" or "page up key" => SystemActionPageUp,
            "page down" or "page down key" => SystemActionPageDown,
            _ => string.Empty
        };

        return action.Length > 0;
    }

    private static bool TryRouteBareSingleKeyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var action = normalized switch
        {
            "enter" or "enter key" or "return" or "return key" => SystemActionPressEnter,
            "tab" or "tab key" => SystemActionPressTab,
            "escape" or "escape key" or "esc" or "esc key" => SystemActionPressEscape,
            "backspace" or "backspace key" => SystemActionPressBackspace,
            "space" or "space key" or "space bar" => SystemActionPressSpace,
            "delete" or "delete key" => SystemActionPressDelete,
            "insert" or "insert key" => SystemActionPressInsert,
            "windows" or "windows key" => SystemActionPressWindows,
            "context menu" or "context menu key" or "menu key" => SystemActionPressContextMenu,
            "caps lock" or "caps lock key" => SystemActionPressCapsLock,
            "up" or "up arrow" => SystemActionPressUp,
            "down" or "down arrow" => SystemActionPressDown,
            "left" or "left arrow" => SystemActionPressLeft,
            "right" or "right arrow" => SystemActionPressRight,
            "home" or "home key" => SystemActionPressHome,
            "end" or "end key" => SystemActionPressEnd,
            "page up" or "page up key" => SystemActionPageUp,
            "page down" or "page down key" => SystemActionPageDown,
            _ => string.Empty
        };

        if (action.Length == 0)
            return false;

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, action);
        return true;
    }

    private static bool TryRouteDigitKeyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var prefix in new[] { "press number ", "tap number ", "press digit ", "tap digit ", "number key ", "digit key ", "press ", "tap " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var digitText = normalized[prefix.Length..].Trim();
            if (!TryParseDigitKeyNumber(digitText, out var digit))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-press-digit:{digit}");
            return true;
        }

        return false;
    }

    private static bool TryParseDigitKeyNumber(string value, out int digit)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        if (int.TryParse(normalized, out digit) && digit is >= 0 and <= 9)
            return true;

        digit = normalized switch
        {
            "zero" or "oh" => 0,
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            _ => -1
        };

        return digit is >= 0 and <= 9;
    }

    private static bool TryRouteLetterKeyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var prefix in new[] { "press letter ", "tap letter ", "letter key ", "press ", "tap " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var letterText = normalized[prefix.Length..].Trim();
            if (!TryParseLetterKey(letterText, out var letter))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-press-letter:{letter}");
            return true;
        }

        return false;
    }

    private static bool TryParseLetterKey(string value, out char letter)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        if (normalized.Length == 1 && normalized[0] is >= 'a' and <= 'z')
        {
            letter = normalized[0];
            return true;
        }

        letter = '\0';
        return false;
    }

    private static bool TryRouteSymbolKeyAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        foreach (var prefix in new[] { "press symbol ", "tap symbol ", "press punctuation ", "tap punctuation ", "press ", "tap ", "symbol key ", "punctuation key ", "key " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var symbolText = normalized[prefix.Length..].Trim();
            if (!TryParseSymbolKeyName(symbolText, out var symbolName))
                continue;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-press-symbol:{symbolName}");
            return true;
        }

        return false;
    }

    private static bool TryParseSymbolKeyName(string value, out string symbolName)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        symbolName = normalized switch
        {
            "comma" => "comma",
            "period" or "dot" or "full stop" => "period",
            "slash" or "forward slash" => "slash",
            "question mark" or "question" => "question",
            "semicolon" => "semicolon",
            "colon" => "colon",
            "apostrophe" or "single quote" => "apostrophe",
            "quote" or "double quote" or "quotation mark" => "quote",
            "minus" or "dash" or "hyphen" => "minus",
            "underscore" => "underscore",
            "equals" or "equal" or "equal sign" => "equals",
            "plus" or "plus sign" => "plus",
            "left bracket" or "open bracket" => "left-bracket",
            "right bracket" or "close bracket" => "right-bracket",
            "left brace" or "open brace" or "curly brace left" => "left-brace",
            "right brace" or "close brace" or "curly brace right" => "right-brace",
            "backslash" or "back slash" => "backslash",
            "pipe" or "vertical bar" => "pipe",
            "grave" or "backtick" => "grave",
            "tilde" => "tilde",
            "exclamation" or "exclamation point" or "bang" => "exclamation",
            "at sign" or "at" => "at",
            "pound" or "hash" or "number sign" => "hash",
            "dollar" or "dollar sign" => "dollar",
            "percent" or "percent sign" => "percent",
            "caret" => "caret",
            "ampersand" => "ampersand",
            "asterisk" or "star" => "asterisk",
            "left parenthesis" or "open parenthesis" or "left paren" or "open paren" => "left-parenthesis",
            "right parenthesis" or "close parenthesis" or "right paren" or "close paren" => "right-parenthesis",
            _ => string.Empty
        };

        return symbolName.Length > 0;
    }

    private static bool TryRouteHeldModifierAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command)
            .Replace("ctrl", "control", StringComparison.OrdinalIgnoreCase);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (normalized is "release"
            or "release modifiers"
            or "release all modifiers"
            or "release modifier keys"
            or "release all modifier keys"
            or "release held keys"
            or "release all held keys")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionReleaseModifiers);
            return true;
        }

        foreach (var prefix in new[] { "hold ", "hold down ", "press and hold ", "keep holding " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var modifierText = normalized[prefix.Length..].Trim();
            if (!TryParseHeldModifierName(modifierText, out var modifierName))
                return false;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionHoldModifierPrefix}{modifierName}");
            return true;
        }

        foreach (var prefix in new[] { "release ", "let go of ", "stop holding " })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var modifierText = normalized[prefix.Length..].Trim();
            if (!TryParseHeldModifierName(modifierText, out var modifierName))
                return false;

            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionReleaseModifierPrefix}{modifierName}");
            return true;
        }

        return false;
    }

    private static bool TryRouteMousePointerAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
            return false;

        if (normalized is "stop moving" or "stop moving mouse" or "stop mouse")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseStopMoving);
            return true;
        }

        if (normalized is "move faster" or "faster")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveFaster);
            return true;
        }

        if (normalized is "move slower" or "slower")
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionMouseMoveSlower);
            return true;
        }

        if (TryParseMouseMoveAction(normalized, out var direction, out var distance))
        {
            route = distance.HasValue
                ? new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionMouseMoveFixedPrefix}{direction}:{distance.Value}")
                : new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionMouseStartMovingPrefix}{direction}");
            return true;
        }

        if (TryParseMouseDragDirectionAction(normalized, out direction))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"{SystemActionMouseDragDirectionPrefix}{direction}");
            return true;
        }

        return false;
    }

    private static bool TryParseMouseMoveAction(string normalized, out string direction, out int? distance)
    {
        direction = string.Empty;
        distance = null;
        var remainder = string.Empty;
        foreach (var prefix in MouseMoveActionPrefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            remainder = normalized[prefix.Length..].Trim();
            break;
        }

        if (string.IsNullOrWhiteSpace(remainder))
            return false;

        if (!TryParseMouseDirectionPhrase(remainder, out direction, out var distanceText))
            return false;

        if (string.IsNullOrWhiteSpace(distanceText))
            return true;

        if (!TryParseMouseDistance(distanceText, out var parsedDistance))
            return false;

        distance = parsedDistance;
        return true;
    }

    private static bool TryParseMouseDragDirectionAction(string normalized, out string direction)
    {
        direction = string.Empty;
        var remainder = string.Empty;
        foreach (var prefix in MouseDragActionPrefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            remainder = normalized[prefix.Length..].Trim();
            break;
        }

        if (string.IsNullOrWhiteSpace(remainder))
            return false;

        return TryParseMouseDirectionPhrase(remainder, out direction, out var trailingText)
            && string.IsNullOrWhiteSpace(trailingText);
    }

    private static bool TryParseMouseDirectionPhrase(string value, out string direction, out string trailingText)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        foreach (var (phrase, canonicalDirection) in MouseDirectionAliases)
        {
            if (!normalized.StartsWith(phrase, StringComparison.OrdinalIgnoreCase))
                continue;

            if (normalized.Length > phrase.Length && normalized[phrase.Length] != ' ')
                continue;

            direction = canonicalDirection;
            trailingText = normalized.Length == phrase.Length
                ? string.Empty
                : normalized[(phrase.Length + 1)..].Trim();
            return true;
        }

        direction = string.Empty;
        trailingText = string.Empty;
        return false;
    }

    private static bool TryParseMouseDistance(string value, out int distance)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized is "a little" or "little" or "a bit" or "bit")
        {
            distance = 1;
            return true;
        }

        return TryParseResultNumber(normalized, out distance);
    }

    private static bool TryParseHeldModifierName(string value, out string modifierName)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        if (normalized.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["the ".Length..].Trim();
        if (normalized.EndsWith(" key", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^" key".Length].Trim();

        modifierName = normalized switch
        {
            "shift" or "left shift" or "right shift" => "shift",
            "control" or "left control" or "right control" => "control",
            "alt" or "left alt" or "right alt" or "alternate" => "alt",
            _ => string.Empty
        };

        return modifierName.Length > 0;
    }

    private static bool TryRouteModifierChordAction(string command, out AlphaCommandRoute route)
    {
        route = new AlphaCommandRoute(AlphaCommandKind.None, string.Empty);
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command)
            .Replace("ctrl", "control", StringComparison.OrdinalIgnoreCase)
            .Replace("+", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("-", " ", StringComparison.OrdinalIgnoreCase);

        foreach (var prefix in new[] { "press ", "hit ", "key " })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var chordName = normalized switch
        {
            "shift tab" => "shift-tab",
            "control tab" => "control-tab",
            "control shift tab" or "shift control tab" => "control-shift-tab",
            "alt shift tab" or "shift alt tab" => "alt-shift-tab",
            "control a" or "control letter a" => "control-a",
            "control b" or "control letter b" => "control-b",
            "control c" or "control letter c" => "control-c",
            "control f" or "control letter f" => "control-f",
            "control i" or "control letter i" => "control-i",
            "control n" or "control letter n" => "control-n",
            "control o" or "control letter o" => "control-o",
            "control p" or "control letter p" => "control-p",
            "control s" or "control letter s" => "control-s",
            "control u" or "control letter u" => "control-u",
            "control v" or "control letter v" => "control-v",
            "control x" or "control letter x" => "control-x",
            "control y" or "control letter y" => "control-y",
            "control z" or "control letter z" => "control-z",
            "control plus" or "control plus sign" => "control-plus",
            "control minus" or "control hyphen" => "control-minus",
            "control zero" or "control 0" or "control number zero" => "control-zero",
            "alt left" or "alt left arrow" => "alt-left",
            "alt right" or "alt right arrow" => "alt-right",
            "alt up" or "alt up arrow" => "alt-up",
            "alt down" or "alt down arrow" => "alt-down",
            "control home"
                or "go to document start"
                or "go to start of document"
                or "go to beginning of document"
                or "go to top of document"
                or "move to document start"
                or "move to start of document"
                or "move to beginning of document"
                or "document start"
                or "start of document"
                or "beginning of document" => "control-home",
            "control end"
                or "go to document end"
                or "go to end of document"
                or "go to bottom of document"
                or "move to document end"
                or "move to end of document"
                or "document end"
                or "end of document" => "control-end",
            "control shift home"
                or "shift control home"
                or "select to document start"
                or "select to start of document"
                or "select to beginning of document"
                or "highlight to document start"
                or "highlight to start of document"
                or "highlight to beginning of document" => "control-shift-home",
            "control shift end"
                or "shift control end"
                or "select to document end"
                or "select to end of document"
                or "highlight to document end"
                or "highlight to end of document" => "control-shift-end",
            "alt f4" => SystemActionCloseWindow,
            _ => TryParseControlShiftChordName(normalized, out var parsedControlShiftChordName)
                ? parsedControlShiftChordName
                : TryParseControlChordName(normalized, out var parsedControlChordName)
                    ? parsedControlChordName
                    : TryParseShiftChordName(normalized, out var parsedShiftChordName)
                        ? parsedShiftChordName
                        : TryParseAltChordName(normalized, out var parsedAltChordName)
                            ? parsedAltChordName
                            : string.Empty
        };

        if (chordName.Length == 0)
            return false;

        if (string.Equals(chordName, SystemActionCloseWindow, StringComparison.OrdinalIgnoreCase))
        {
            route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, SystemActionCloseWindow);
            return true;
        }

        route = new AlphaCommandRoute(AlphaCommandKind.SystemControl, $"system-press-chord:{chordName}");
        return true;
    }

    private static bool IsBlockedKeyboardChordCommand(string command)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(command)
            .Replace("ctrl", "control", StringComparison.OrdinalIgnoreCase)
            .Replace("+", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("-", " ", StringComparison.OrdinalIgnoreCase);

        foreach (var prefix in new[] { "press ", "hit ", "key " })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized is "control alt delete"
            or "alt control delete"
            or "control alt del"
            or "alt control del";
    }

    private static bool TryParseControlShiftChordName(string normalized, out string chordName)
    {
        chordName = string.Empty;
        string keyText;
        if (normalized.StartsWith("control shift ", StringComparison.OrdinalIgnoreCase))
            keyText = normalized["control shift ".Length..].Trim();
        else if (normalized.StartsWith("shift control ", StringComparison.OrdinalIgnoreCase))
            keyText = normalized["shift control ".Length..].Trim();
        else
            return false;

        if (keyText.StartsWith("key ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["key ".Length..].Trim();

        if (keyText.StartsWith("letter ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["letter ".Length..].Trim();

        if (TryParseLetterKey(keyText, out var letter))
        {
            chordName = $"control-shift-{letter}";
            return true;
        }

        if (keyText.StartsWith("number ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["number ".Length..].Trim();
        else if (keyText.StartsWith("digit ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["digit ".Length..].Trim();

        if (TryParseDigitKeyNumber(keyText, out var digit))
        {
            chordName = $"control-shift-{digit}";
            return true;
        }

        return false;
    }

    private static bool TryParseControlChordName(string normalized, out string chordName)
    {
        chordName = string.Empty;
        const string prefix = "control ";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyText = normalized[prefix.Length..].Trim();
        if (keyText.StartsWith("key ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["key ".Length..].Trim();

        if (keyText.StartsWith("letter ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["letter ".Length..].Trim();

        if (TryParseLetterKey(keyText, out var letter))
        {
            chordName = $"control-{letter}";
            return true;
        }

        if (keyText.StartsWith("number ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["number ".Length..].Trim();
        else if (keyText.StartsWith("digit ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["digit ".Length..].Trim();

        if (TryParseDigitKeyNumber(keyText, out var digit))
        {
            chordName = $"control-{digit}";
            return true;
        }

        chordName = keyText switch
        {
            "plus" or "plus sign" => "control-plus",
            "minus" or "hyphen" => "control-minus",
            "equals" or "equals sign" or "equal sign" => "control-equals",
            "comma" => "control-comma",
            "period" or "dot" => "control-period",
            "slash" or "forward slash" => "control-slash",
            "backslash" or "back slash" => "control-backslash",
            "semicolon" => "control-semicolon",
            "quote" or "apostrophe" or "single quote" => "control-apostrophe",
            "left bracket" or "open bracket" => "control-left-bracket",
            "right bracket" or "close bracket" => "control-right-bracket",
            "grave" or "backtick" or "back tick" => "control-grave",
            _ => string.Empty
        };

        return chordName.Length > 0;
    }

    private static bool TryParseShiftChordName(string normalized, out string chordName)
    {
        chordName = string.Empty;
        const string prefix = "shift ";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyText = normalized[prefix.Length..].Trim();
        if (keyText.StartsWith("key ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["key ".Length..].Trim();

        if (keyText.StartsWith("letter ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["letter ".Length..].Trim();

        if (TryParseLetterKey(keyText, out var letter))
        {
            chordName = $"shift-{letter}";
            return true;
        }

        if (keyText.StartsWith("number ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["number ".Length..].Trim();
        else if (keyText.StartsWith("digit ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["digit ".Length..].Trim();

        if (TryParseDigitKeyNumber(keyText, out var digit))
        {
            chordName = $"shift-{digit}";
            return true;
        }

        return false;
    }

    private static bool TryParseAltChordName(string normalized, out string chordName)
    {
        chordName = string.Empty;
        const string prefix = "alt ";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyText = normalized[prefix.Length..].Trim();
        if (keyText.StartsWith("key ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["key ".Length..].Trim();

        if (keyText.StartsWith("letter ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["letter ".Length..].Trim();

        if (TryParseLetterKey(keyText, out var letter))
        {
            chordName = $"alt-{letter}";
            return true;
        }

        if (keyText.StartsWith("number ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["number ".Length..].Trim();
        else if (keyText.StartsWith("digit ", StringComparison.OrdinalIgnoreCase))
            keyText = keyText["digit ".Length..].Trim();

        if (TryParseDigitKeyNumber(keyText, out var digit))
        {
            chordName = $"alt-{digit}";
            return true;
        }

        return false;
    }

    private static bool TryParseFunctionKeyNumber(string value, out int functionKeyNumber)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (normalized.StartsWith("f", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        if (int.TryParse(normalized, out functionKeyNumber) && functionKeyNumber is >= 1 and <= 12)
            return true;

        functionKeyNumber = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim() switch
        {
            "f one" or "one" or "first" => 1,
            "f two" or "two" or "second" => 2,
            "f three" or "three" or "third" => 3,
            "f four" or "four" or "fourth" => 4,
            "f five" or "five" or "fifth" => 5,
            "f six" or "six" or "sixth" => 6,
            "f seven" or "seven" or "seventh" => 7,
            "f eight" or "eight" or "eighth" => 8,
            "f nine" or "nine" or "ninth" => 9,
            "f ten" or "ten" or "tenth" => 10,
            "f eleven" or "eleven" or "eleventh" => 11,
            "f twelve" or "twelve" or "twelfth" => 12,
            _ => 0
        };

        return functionKeyNumber is >= 1 and <= 12;
    }

    private static string NormalizeVisibleControlLabel(string value)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = TrimSpeechWords(normalized, VisibleControlLabelLeadingWords, trimFromStart: true);
        normalized = TrimSpeechWords(normalized, VisibleControlLabelTrailingWords, trimFromStart: false);
        if (TryParseVisibleControlNumberLabel(normalized, out var controlNumber))
            return controlNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return normalized;
    }

    private static bool HasVisibleFieldLabelSuffix(string value)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value);
        foreach (var suffix in VisibleFieldLabelSuffixes)
        {
            if (normalized.EndsWith($" {suffix}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryParseVisibleControlNumberLabel(string value, out int controlNumber)
    {
        var normalized = AlphaVoiceTranscriptParser.NormalizeSpeechText(value).Trim();
        if (int.TryParse(normalized, out controlNumber) && controlNumber > 0)
            return true;

        controlNumber = normalized switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            "six" or "sixth" => 6,
            "seven" or "seventh" => 7,
            "eight" or "eighth" => 8,
            "nine" or "ninth" => 9,
            "ten" or "tenth" => 10,
            "eleven" or "eleventh" => 11,
            "twelve" or "twelfth" => 12,
            _ => 0
        };

        return controlNumber > 0;
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
        "launch browser ",
        "launch browser to ",
        "launch browser ",
        "open web browser ",
        "browser open ",
        "browser launch ",
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
        "go back",
        "back a page",
        "back one page",
        "back page",
        "go back a page",
        "go back one page",
        "browser previous page",
        "previous browser page",
        "previous web page",
        "go back in browser",
        "go browser back"
    ];

    private static readonly string[] BrowserForwardPrefixes =
    [
        "browser forward",
        "forward browser",
        "go forward",
        "forward a page",
        "forward one page",
        "forward page",
        "go forward a page",
        "go forward one page",
        "next page",
        "browser next page",
        "next browser page",
        "next web page",
        "go forward in browser",
        "go browser forward"
    ];

    private static readonly string[] BrowserRefreshPrefixes =
    [
        "browser refresh",
        "refresh browser",
        "refresh page",
        "refresh this page",
        "refresh current page",
        "reload browser",
        "reload page",
        "reload this page",
        "reload current page"
    ];

    private static readonly string[] BrowserStopLoadingPrefixes =
    [
        "browser stop loading",
        "browser stop loading page",
        "stop loading page",
        "stop loading this page",
        "stop page loading",
        "cancel page load",
        "cancel loading page",
        "cancel browser load",
        "cancel browser loading"
    ];

    private static readonly string[] BrowserNewTabPrefixes =
    [
        "browser new tab",
        "new browser tab",
        "new tab",
        "open new tab"
    ];

    private static readonly string[] BrowserNewWindowPrefixes =
    [
        "browser new window",
        "new browser window",
        "open browser window",
        "open new browser window"
    ];

    private static readonly string[] BrowserPrivateWindowPrefixes =
    [
        "browser private window",
        "browser incognito",
        "browser incognito window",
        "private browsing",
        "private browser window",
        "open private window",
        "open incognito window",
        "new private window",
        "new incognito window"
    ];

    private static readonly string[] BrowserBookmarkPagePrefixes =
    [
        "browser bookmark page",
        "bookmark this page",
        "add bookmark",
        "add to bookmarks",
        "add to favorites",
        "favorite this page",
        "save bookmark"
    ];

    private static readonly string[] BrowserOpenBookmarksPrefixes =
    [
        "browser open bookmarks",
        "browser bookmarks",
        "open bookmarks",
        "show bookmarks",
        "open bookmarks page",
        "show bookmarks page",
        "browser favorites",
        "open favorites",
        "show favorites",
        "open favorites page",
        "show favorites page",
        "bookmark manager",
        "favorites manager"
    ];

    private static readonly string[] BrowserSavePagePrefixes =
    [
        "browser save page",
        "save page",
        "save this page",
        "save web page",
        "save webpage"
    ];

    private static readonly string[] BrowserPrintPagePrefixes =
    [
        "browser print page",
        "print page",
        "print this page",
        "print web page",
        "print webpage"
    ];

    private static readonly string[] BrowserNextTabPrefixes =
    [
        "browser next tab",
        "next browser tab",
        "switch browser tab",
        "browser tab next",
        "go to browser next tab",
        "switch to browser next tab",
        "move to browser next tab"
    ];

    private static readonly string[] BrowserPreviousTabPrefixes =
    [
        "browser previous tab",
        "previous browser tab",
        "browser tab previous",
        "go to browser previous tab",
        "switch to browser previous tab",
        "move to browser previous tab",
        "browser back tab"
    ];

    private static readonly string[] BrowserMoveTabLeftPrefixes =
    [
        "browser move tab left",
        "browser move current tab left",
        "move browser tab left",
        "move current tab left",
        "move tab left",
        "shift tab left",
        "reorder tab left"
    ];

    private static readonly string[] BrowserMoveTabRightPrefixes =
    [
        "browser move tab right",
        "browser move current tab right",
        "move browser tab right",
        "move current tab right",
        "move tab right",
        "shift tab right",
        "reorder tab right"
    ];

    private static readonly string[] BrowserSelectTabPrefixes =
    [
        "browser tab ",
        "browser go to tab ",
        "browser switch to tab ",
        "go to browser tab ",
        "switch to browser tab ",
        "go to tab ",
        "switch to tab ",
        "select tab ",
        "tab number "
    ];

    private static readonly string[] BrowserCloseTabPrefixes =
    [
        "browser close tab",
        "close browser tab",
        "close tab"
    ];

    private static readonly string[] BrowserReopenClosedTabPrefixes =
    [
        "browser reopen closed tab",
        "reopen closed tab",
        "restore closed tab",
        "browser restore closed tab",
        "undo close tab",
        "reopen last closed tab"
    ];

    private static readonly string[] BrowserDuplicateTabPrefixes =
    [
        "browser duplicate tab",
        "duplicate browser tab",
        "duplicate tab",
        "copy browser tab",
        "copy current tab",
        "clone browser tab",
        "clone current tab",
        "open this page in new tab",
        "open current page in new tab"
    ];

    private static readonly string[] BrowserCopyAddressPrefixes =
    [
        "browser copy address",
        "browser copy url",
        "browser copy link",
        "copy page address",
        "copy page url",
        "copy page link",
        "copy current page address",
        "copy current page url",
        "copy current page link",
        "copy this page address",
        "copy this page url",
        "copy this page link",
        "copy website address",
        "copy web address",
        "copy address bar"
    ];

    private static readonly string[] BrowserFocusAddressBarPrefixes =
    [
        "browser focus address bar",
        "browser address bar",
        "browser url bar",
        "browser url field",
        "focus address bar",
        "focus url bar",
        "open address bar",
        "open url bar",
        "go to address bar",
        "go to url bar",
        "address bar",
        "url bar"
    ];

    private static readonly string[] BrowserAddressTextPrefixes =
    [
        "browser address bar search for ",
        "browser address bar search ",
        "browser address bar go to ",
        "browser address bar open ",
        "browser search address bar for ",
        "browser type address ",
        "browser type in address bar ",
        "go to address bar and type ",
        "go to url bar and type ",
        "type in address bar ",
        "type address bar ",
        "search address bar for ",
        "search in address bar for ",
        "go in address bar to ",
        "open in address bar "
    ];

    private static readonly string[] BrowserHomePrefixes =
    [
        "browser home",
        "browser home page",
        "browser go home",
        "browser go to home",
        "home page",
        "go home page",
        "go to home page",
        "go to my home page",
        "open home page",
        "go browser home",
        "go to browser home",
        "go to browser home page",
        "open browser home",
        "open browser home page"
    ];

    private static readonly string[] BrowserFullscreenPrefixes =
    [
        "browser full screen",
        "browser fullscreen",
        "full screen",
        "fullscreen",
        "browser full screen mode",
        "browser fullscreen mode"
    ];

    private static readonly string[] BrowserShowNumbersPrefixes =
    [
        "browser show numbers",
        "browser show control numbers",
        "browser show labels",
        "browser show clickable controls"
    ];

    private static readonly string[] BrowserShowGridPrefixes =
    [
        "browser show grid",
        "browser show mouse grid",
        "browser show numbered grid"
    ];

    private static readonly string[] BrowserHideOverlaysPrefixes =
    [
        "browser hide overlays",
        "hide browser overlays",
        "hide overlays",
        "hide the overlays",
        "hide browser aids",
        "close browser overlays"
    ];

    private static readonly string[] BrowserDownloadsPrefixes =
    [
        "browser downloads",
        "open browser downloads",
        "show browser downloads",
        "open downloads page",
        "show downloads page",
        "downloads in browser",
        "show downloads"
    ];

    private static readonly string[] BrowserHistoryPrefixes =
    [
        "browser history",
        "open browser history",
        "show browser history",
        "open history page",
        "show history page",
        "history in browser",
        "show history"
    ];

    private static readonly string[] BrowserFindPrefixes =
    [
        "browser find",
        "browser search for",
        "find in page",
        "find on page",
        "search in page",
        "search page",
        "search this page",
        "page search",
        "open find box",
        "show find box",
        "find box",
        "browser search in page",
        "find text",
        "browser find text"
    ];

    private static readonly string[] BrowserFindTextPrefixes =
    [
        "browser find text ",
        "browser find for ",
        "browser find ",
        "browser search for ",
        "find in page for ",
        "find on page for ",
        "find text ",
        "find phrase ",
        "search for ",
        "search in page for ",
        "search page for ",
        "search this page for "
    ];

    private static readonly string[] BrowserFindTextSuffixes =
    [
        " on this page",
        " in this page",
        " on the page",
        " in the page"
    ];

    private static readonly string[] BrowserAddressTextSuffixes =
    [
        " in address bar",
        " in the address bar",
        " into address bar",
        " into the address bar",
        " in url bar",
        " in the url bar",
        " into url bar",
        " into the url bar"
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
        "page up in browser",
        "scroll page up",
        "move up page"
    ];

    private static readonly string[] BrowserScrollUpPageCountPrefixes =
    [
        "browser scroll up ",
        "scroll up ",
        "browser page up ",
        "page up ",
        "scroll page up ",
        "move up "
    ];

    private static readonly string[] BrowserStartScrollUpPrefixes =
    [
        "browser start scrolling up",
        "start browser scrolling up"
    ];

    private static readonly string[] BrowserStartScrollDownPrefixes =
    [
        "browser start scrolling down",
        "start browser scrolling down"
    ];

    private static readonly string[] BrowserStartScrollLeftPrefixes =
    [
        "browser start scrolling left",
        "start browser scrolling left"
    ];

    private static readonly string[] BrowserStartScrollRightPrefixes =
    [
        "browser start scrolling right",
        "start browser scrolling right"
    ];

    private static readonly string[] BrowserStopScrollPrefixes =
    [
        "browser stop scrolling",
        "stop browser scrolling"
    ];

    private static readonly string[] BrowserScrollDownPrefixes =
    [
        "browser scroll down",
        "scroll down",
        "browser page down",
        "page down in browser",
        "scroll page down",
        "move down page"
    ];

    private static readonly string[] BrowserScrollDownPageCountPrefixes =
    [
        "browser scroll down ",
        "scroll down ",
        "browser page down ",
        "page down ",
        "scroll page down ",
        "move down "
    ];

    private static readonly string[] BrowserScrollLeftPrefixes =
    [
        "browser scroll left",
        "browser page left",
        "scroll browser left",
        "move browser left"
    ];

    private static readonly string[] BrowserScrollRightPrefixes =
    [
        "browser scroll right",
        "browser page right",
        "scroll browser right",
        "move browser right"
    ];

    private static readonly string[] BrowserScrollTopPrefixes =
    [
        "browser scroll top",
        "scroll top",
        "top of page",
        "go to top of page",
        "browser top"
    ];

    private static readonly string[] BrowserScrollBottomPrefixes =
    [
        "browser scroll bottom",
        "scroll bottom",
        "bottom of page",
        "go to bottom of page",
        "browser bottom"
    ];

    private static readonly string[] BrowserZoomInPrefixes =
    [
        "browser zoom in",
        "browser bigger",
        "browser larger",
        "browser increase text size",
        "browser make text bigger",
        "browser make page bigger"
    ];

    private static readonly string[] BrowserZoomOutPrefixes =
    [
        "browser zoom out",
        "browser smaller",
        "browser smaller text",
        "browser decrease text size",
        "browser make text smaller",
        "browser make page smaller"
    ];

    private static readonly string[] BrowserZoomResetPrefixes =
    [
        "browser zoom reset",
        "browser reset zoom",
        "browser normal size",
        "browser normal zoom",
        "browser actual size",
        "browser default size",
        "browser default zoom",
        "browser reset size"
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

    private static readonly string[] SystemMediaPlayPausePrefixes =
    [
        "media play pause",
        "play pause media",
        "play or pause",
        "toggle playback",
        "pause media",
        "play media"
    ];

    private static readonly string[] SystemMediaNextTrackPrefixes =
    [
        "media next track",
        "next track",
        "skip track",
        "skip song",
        "next song"
    ];

    private static readonly string[] SystemMediaPreviousTrackPrefixes =
    [
        "media previous track",
        "previous track",
        "back track",
        "previous song",
        "last song"
    ];

    private static readonly string[] SystemMediaStopPrefixes =
    [
        "media stop",
        "stop media",
        "stop playback"
    ];

    private static readonly string[] SystemShowDesktopPrefixes =
    [
        "show desktop",
        "show the desktop",
        "show my desktop",
        "minimize all windows",
        "minimise all windows",
        "hide all windows",
        "hide desktop",
        "go to desktop",
        "go to the desktop",
        "desktop",
        "system show desktop"
    ];

    private static readonly string[] SystemNextWindowPrefixes =
    [
        "next window",
        "next application",
        "switch window",
        "switch windows",
        "switch application",
        "switch to the next window",
        "switch to next window",
        "switch to the next app",
        "switch to next app",
        "switch to the next application",
        "switch to next application",
        "go to next window",
        "go to the next window",
        "go to next app",
        "go to next application",
        "go to the next app window",
        "cycle to next window",
        "cycle to next app",
        "cycle window",
        "next app window",
        "next app",
        "system next window"
    ];

    private static readonly string[] SystemPreviousWindowPrefixes =
    [
        "previous window",
        "previous application",
        "last window",
        "last app",
        "last application",
        "back window",
        "switch back window",
        "switch back app",
        "switch back application",
        "switch to the previous window",
        "switch to previous window",
        "switch to the previous app",
        "switch to previous app",
        "switch to the previous application",
        "switch to previous application",
        "go to previous window",
        "go to the previous window",
        "go to previous app",
        "go to previous application",
        "go to the previous app window",
        "cycle to previous window",
        "cycle to previous app",
        "previous app window",
        "previous app",
        "system previous window"
    ];

    private static readonly string[] SystemTaskViewPrefixes =
    [
        "task view",
        "open task view",
        "show task view",
        "windowing",
        "show windowing",
        "open windowing",
        "show windows",
        "show all windows",
        "show open windows",
        "all windows",
        "switch apps",
        "switch app",
        "switch applications",
        "show apps",
        "app switcher",
        "application switcher",
        "task switcher",
        "window switcher",
        "open window switcher",
        "show window switcher",
        "window overview"
    ];

    private static readonly string[] SystemNamedWindowSwitchPrefixes =
    [
        "switch to ",
        "go to "
    ];

    private static readonly string[] SystemQuickSettingsPrefixes =
    [
        "quick settings",
        "open quick settings",
        "show quick settings",
        "system quick settings",
        "open action center",
        "show action center"
    ];

    private static readonly string[] SystemNotificationCenterPrefixes =
    [
        "notification center",
        "notifications center",
        "open notification center",
        "show notification center",
        "open notifications",
        "show notifications",
        "calendar center"
    ];

    private static readonly string[] SystemEmojiPanelPrefixes =
    [
        "emoji panel",
        "open emoji panel",
        "show emoji panel",
        "emoji picker",
        "open emoji picker",
        "show emoji picker",
        "symbol picker",
        "open symbol picker",
        "show symbol picker",
        "open symbols panel",
        "show symbols panel"
    ];

    private static readonly string[] SystemClipboardHistoryPrefixes =
    [
        "clipboard history",
        "open clipboard history",
        "show clipboard history",
        "open clipboard",
        "show clipboard",
        "clipboard panel",
        "open clipboard panel",
        "show clipboard panel",
        "clipboard picker",
        "open clipboard picker",
        "show clipboard picker"
    ];

    private static readonly string[] SystemSnippingToolbarPrefixes =
    [
        "snipping toolbar",
        "open snipping toolbar",
        "show snipping toolbar",
        "screen snip",
        "open screen snip",
        "show screen snip",
        "snip screen",
        "open snip screen",
        "show snip screen",
        "screenshot toolbar",
        "open screenshot toolbar",
        "show screenshot toolbar",
        "take screenshot",
        "take a screenshot",
        "open screenshot tools",
        "show screenshot tools"
    ];

    private static readonly string[] SystemProjectDisplayPrefixes =
    [
        "project display",
        "project screen",
        "open project display",
        "show project display",
        "display switch",
        "open display switch",
        "show display switch",
        "projection panel"
    ];

    private static readonly string[] SystemCastDisplayPrefixes =
    [
        "cast display",
        "cast screen",
        "open cast display",
        "show cast display",
        "connect display",
        "wireless display",
        "open wireless display",
        "show wireless display"
    ];

    private static readonly string[] SystemNewVirtualDesktopPrefixes =
    [
        "new virtual desktop",
        "create virtual desktop",
        "new desktop",
        "create desktop"
    ];

    private static readonly string[] SystemNextVirtualDesktopPrefixes =
    [
        "next virtual desktop",
        "next desktop",
        "switch to next desktop",
        "go to next desktop"
    ];

    private static readonly string[] SystemPreviousVirtualDesktopPrefixes =
    [
        "previous virtual desktop",
        "previous desktop",
        "switch to previous desktop",
        "go to previous desktop",
        "back desktop"
    ];

    private static readonly string[] SystemTaskManagerPrefixes =
    [
        "task manager",
        "open task manager",
        "show task manager",
        "system task manager"
    ];

    private static readonly string[] SystemSettingsPrefixes =
    [
        "system settings",
        "windows settings",
        "show system settings",
        "show windows settings"
    ];

    private static readonly string[] SystemDisplaySettingsPrefixes =
    [
        "display settings",
        "open display settings",
        "screen settings",
        "open screen settings",
        "system display settings"
    ];

    private static readonly string[] SystemSoundSettingsPrefixes =
    [
        "sound settings",
        "open sound settings",
        "audio settings",
        "open audio settings",
        "system sound settings"
    ];

    private static readonly string[] SystemBluetoothSettingsPrefixes =
    [
        "bluetooth settings",
        "open bluetooth settings",
        "bluetooth and devices settings",
        "open bluetooth and devices settings",
        "devices settings",
        "open devices settings",
        "system bluetooth settings"
    ];

    private static readonly string[] SystemWifiSettingsPrefixes =
    [
        "wifi settings",
        "wi fi settings",
        "open wifi settings",
        "open wi fi settings",
        "wireless settings",
        "system wifi settings"
    ];

    private static readonly string[] SystemNetworkSettingsPrefixes =
    [
        "network settings",
        "open network settings",
        "internet settings",
        "open internet settings",
        "network and internet settings",
        "open network and internet settings",
        "system network settings"
    ];

    private static readonly string[] SystemAccessibilitySettingsPrefixes =
    [
        "accessibility settings",
        "open accessibility settings",
        "ease of access settings",
        "open ease of access settings",
        "system accessibility settings"
    ];

    private static readonly string[] SystemMagnifierSettingsPrefixes =
    [
        "magnifier settings",
        "open magnifier settings",
        "zoom settings",
        "open zoom settings",
        "screen zoom settings",
        "open screen zoom settings",
        "system magnifier settings"
    ];

    private static readonly string[] SystemNarratorSettingsPrefixes =
    [
        "narrator settings",
        "open narrator settings",
        "screen reader settings",
        "open screen reader settings",
        "system narrator settings"
    ];

    private static readonly string[] SystemCaptionsSettingsPrefixes =
    [
        "caption settings",
        "captions settings",
        "open caption settings",
        "open captions settings",
        "closed caption settings",
        "closed captions settings",
        "live caption settings",
        "live captions settings",
        "open live captions settings",
        "subtitle settings",
        "subtitles settings",
        "system captions settings"
    ];

    private static readonly string[] SystemSpeechSettingsPrefixes =
    [
        "speech settings",
        "open speech settings",
        "voice settings",
        "open voice settings",
        "voice access settings",
        "open voice access settings",
        "voice typing settings",
        "open voice typing settings",
        "dictation settings",
        "open dictation settings",
        "speech recognition settings",
        "system speech settings"
    ];

    private static readonly string[] SystemMagnifierOpenPrefixes =
    [
        "open magnifier",
        "show magnifier",
        "start magnifier",
        "magnifier",
        "magnify",
        "magnify screen",
        "zoom screen in",
        "screen zoom in",
        "magnifier zoom in"
    ];

    private static readonly string[] SystemMagnifierZoomOutPrefixes =
    [
        "magnifier zoom out",
        "zoom screen out",
        "screen zoom out",
        "decrease magnifier",
        "zoom magnifier out"
    ];

    private static readonly string[] SystemMagnifierClosePrefixes =
    [
        "close magnifier",
        "hide magnifier",
        "stop magnifier",
        "turn off magnifier",
        "exit magnifier"
    ];

    private static readonly string[] SystemMouseSettingsPrefixes =
    [
        "mouse settings",
        "open mouse settings",
        "touchpad settings",
        "system mouse settings"
    ];

    private static readonly string[] SystemKeyboardSettingsPrefixes =
    [
        "keyboard settings",
        "open keyboard settings",
        "typing settings",
        "system keyboard settings"
    ];

    private static readonly string[] SystemPrivacySettingsPrefixes =
    [
        "privacy settings",
        "open privacy settings",
        "system privacy settings"
    ];

    private static readonly string[] SystemPowerSettingsPrefixes =
    [
        "power settings",
        "open power settings",
        "battery settings",
        "open battery settings",
        "power and battery settings",
        "open power and battery settings",
        "system power settings"
    ];

    private static readonly string[] SystemAppsSettingsPrefixes =
    [
        "apps settings",
        "open apps settings",
        "installed apps settings",
        "open installed apps settings",
        "apps and features settings",
        "open apps and features settings",
        "applications settings",
        "system apps settings"
    ];

    private static readonly string[] SystemDefaultAppsSettingsPrefixes =
    [
        "default apps settings",
        "open default apps settings",
        "default app settings",
        "open default app settings",
        "default application settings",
        "open default application settings",
        "system default apps settings"
    ];

    private static readonly string[] SystemDateTimeSettingsPrefixes =
    [
        "date time settings",
        "date and time settings",
        "open date time settings",
        "open date and time settings",
        "time and language settings",
        "open time and language settings",
        "time settings",
        "open time settings",
        "system date time settings"
    ];

    private static readonly string[] SystemNotificationsSettingsPrefixes =
    [
        "notification settings",
        "notifications settings",
        "open notification settings",
        "open notifications settings",
        "system notification settings"
    ];

    private static readonly string[] SystemWindowsUpdateSettingsPrefixes =
    [
        "windows update settings",
        "open windows update settings",
        "update settings",
        "open update settings",
        "system update settings"
    ];

    private static readonly string[] SystemPersonalizationSettingsPrefixes =
    [
        "personalization settings",
        "personalisation settings",
        "open personalization settings",
        "open personalisation settings",
        "background settings",
        "open background settings",
        "system personalization settings"
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

    private static readonly string[] SystemSnapWindowLeftPrefixes =
    [
        "snap window left",
        "snap left",
        "move window left",
        "dock window left",
        "system snap left"
    ];

    private static readonly string[] SystemSnapWindowRightPrefixes =
    [
        "snap window right",
        "snap right",
        "move window right",
        "dock window right",
        "system snap right"
    ];

    private static readonly string[] SystemSnapWindowUpPrefixes =
    [
        "snap window up",
        "snap up",
        "move window up",
        "dock window up",
        "system snap up"
    ];

    private static readonly string[] SystemSnapWindowDownPrefixes =
    [
        "snap window down",
        "snap down",
        "move window down",
        "dock window down",
        "system snap down"
    ];

    private static readonly string[] SystemShowSnapLayoutsPrefixes =
    [
        "show snap layouts",
        "open snap layouts",
        "snap layouts",
        "window layouts",
        "show window layouts"
    ];

    private static readonly string[] SystemPressEnterPrefixes =
    [
        "press enter",
        "hit enter",
        "tap enter",
        "enter key",
        "press return",
        "hit return",
        "tap return",
        "return key",
        "system press enter"
    ];

    private static readonly string[] SystemPressTabPrefixes =
    [
        "press tab",
        "hit tab",
        "tap tab",
        "tab key",
        "system press tab"
    ];

    private static readonly string[] SystemPressEscapePrefixes =
    [
        "dismiss",
        "press escape",
        "hit escape",
        "tap escape",
        "escape key",
        "esc key",
        "system press escape"
    ];

    private static readonly string[] SystemPressBackspacePrefixes =
    [
        "backspace",
        "press backspace",
        "hit backspace",
        "tap backspace",
        "backspace key",
        "system press backspace"
    ];

    private static readonly string[] SystemPressSpacePrefixes =
    [
        "press space",
        "hit space",
        "tap space",
        "space key",
        "press space bar",
        "tap space bar",
        "space bar",
        "system press space"
    ];

    private static readonly string[] SystemPressDeletePrefixes =
    [
        "press delete",
        "hit delete",
        "tap delete",
        "delete key",
        "system press delete"
    ];

    private static readonly string[] SystemPressInsertPrefixes =
    [
        "press insert",
        "hit insert",
        "tap insert",
        "insert key",
        "system press insert"
    ];

    private static readonly string[] SystemPressWindowsPrefixes =
    [
        "press windows",
        "tap windows",
        "press windows key",
        "tap windows key",
        "windows key",
        "press start key",
        "tap start key",
        "start key",
        "open start",
        "show start",
        "start menu",
        "open start menu",
        "show start menu",
        "open windows search",
        "show windows search",
        "windows search",
        "open search menu",
        "show search menu"
    ];

    private static readonly string[] SystemPressContextMenuPrefixes =
    [
        "press context menu",
        "tap context menu",
        "context menu key",
        "application key",
        "apps key",
        "menu key"
    ];

    private static readonly string[] SystemPressCapsLockPrefixes =
    [
        "press caps lock",
        "tap caps lock",
        "caps lock",
        "caps lock key",
        "toggle caps lock"
    ];

    private static readonly string[] SystemPressUpPrefixes =
    [
        "press up",
        "tap up",
        "press up arrow",
        "tap up arrow",
        "up arrow",
        "system press up"
    ];

    private static readonly string[] SystemPressDownPrefixes =
    [
        "press down",
        "tap down",
        "press down arrow",
        "tap down arrow",
        "down arrow",
        "system press down"
    ];

    private static readonly string[] SystemPressLeftPrefixes =
    [
        "press left",
        "tap left",
        "press left arrow",
        "tap left arrow",
        "left arrow",
        "system press left"
    ];

    private static readonly string[] SystemPressRightPrefixes =
    [
        "press right",
        "tap right",
        "press right arrow",
        "tap right arrow",
        "right arrow",
        "system press right"
    ];

    private static readonly string[] SystemPressHomePrefixes =
    [
        "press home",
        "tap home",
        "home key",
        "go to home",
        "system press home"
    ];

    private static readonly string[] SystemPressEndPrefixes =
    [
        "press end",
        "tap end",
        "end key",
        "go to end",
        "system press end"
    ];

    private static readonly string[] SystemPageUpPrefixes =
    [
        "page up",
        "press page up",
        "tap page up",
        "page up key",
        "go page up",
        "system page up"
    ];

    private static readonly string[] SystemPageDownPrefixes =
    [
        "page down",
        "press page down",
        "tap page down",
        "page down key",
        "go page down",
        "system page down"
    ];

    private static readonly string[] SystemMouseClickPrefixes =
    [
        "click",
        "tap",
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

    private static readonly string[] SystemMouseTripleClickPrefixes =
    [
        "triple click",
        "mouse triple click",
        "system triple click"
    ];

    private static readonly string[] SystemMouseRightClickPrefixes =
    [
        "right click",
        "mouse right click",
        "context click",
        "system right click"
    ];

    private static readonly string[] SystemMouseButtonDownPrefixes =
    [
        "mouse button down",
        "left mouse down",
        "hold mouse",
        "hold mouse button",
        "hold left mouse",
        "hold left mouse button",
        "hold down mouse",
        "hold down mouse button",
        "hold down left mouse button",
        "hold click",
        "click and hold",
        "click and hold mouse",
        "click and hold mouse button",
        "click and hold left mouse button",
        "press and hold mouse",
        "press and hold mouse button",
        "press and hold left mouse button",
        "press mouse button",
        "press left mouse button",
        "start drag",
        "begin drag",
        "system mouse button down"
    ];

    private static readonly string[] SystemMouseButtonUpPrefixes =
    [
        "mouse button up",
        "left mouse up",
        "release mouse",
        "release left mouse",
        "release left mouse button",
        "release click",
        "release mouse button",
        "let go mouse",
        "let go of mouse",
        "let go of mouse button",
        "drop",
        "end drag",
        "system mouse button up"
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

    private static readonly string[] SystemMouseScrollLeftPrefixes =
    [
        "mouse scroll left",
        "scroll left",
        "scroll wheel left",
        "wheel left",
        "system mouse scroll left"
    ];

    private static readonly string[] SystemMouseScrollRightPrefixes =
    [
        "mouse scroll right",
        "scroll right",
        "scroll wheel right",
        "wheel right",
        "system mouse scroll right"
    ];

    private static readonly string[] SystemScrollTopPrefixes =
    [
        "scroll to top",
        "scroll to the top",
        "go to top",
        "go to the top",
        "top of window",
        "top of the window",
        "scroll to top of window",
        "scroll to the top of window"
    ];

    private static readonly string[] SystemScrollBottomPrefixes =
    [
        "scroll to bottom",
        "scroll to the bottom",
        "go to bottom",
        "go to the bottom",
        "bottom of window",
        "bottom of the window",
        "scroll to bottom of window",
        "scroll to the bottom of window"
    ];

    private static readonly string[] SystemScrollLeftEdgePrefixes =
    [
        "scroll to left edge",
        "scroll to the left edge",
        "go to left edge",
        "go to the left edge",
        "left edge",
        "left edge of window"
    ];

    private static readonly string[] SystemScrollRightEdgePrefixes =
    [
        "scroll to right edge",
        "scroll to the right edge",
        "go to right edge",
        "go to the right edge",
        "right edge",
        "right edge of window"
    ];

    private static readonly string[] SystemStartScrollingUpPrefixes =
    [
        "start scrolling up",
        "start scroll up",
        "begin scrolling up",
        "begin scroll up"
    ];

    private static readonly string[] SystemStartScrollingDownPrefixes =
    [
        "start scrolling down",
        "start scroll down",
        "begin scrolling down",
        "begin scroll down"
    ];

    private static readonly string[] SystemStartScrollingLeftPrefixes =
    [
        "start scrolling left",
        "start scroll left",
        "begin scrolling left",
        "begin scroll left"
    ];

    private static readonly string[] SystemStartScrollingRightPrefixes =
    [
        "start scrolling right",
        "start scroll right",
        "begin scrolling right",
        "begin scroll right"
    ];

    private static readonly string[] SystemStopScrollingPrefixes =
    [
        "stop scrolling",
        "stop scroll",
        "stop active scrolling"
    ];

    private static readonly string[] SystemMouseMoveUpPrefixes =
    [
        "mouse up",
        "nudge up",
        "nudge mouse up",
        "pointer up",
        "move pointer up"
    ];

    private static readonly string[] SystemMouseMoveDownPrefixes =
    [
        "mouse down",
        "nudge down",
        "nudge mouse down",
        "pointer down",
        "move pointer down"
    ];

    private static readonly string[] SystemMouseMoveLeftPrefixes =
    [
        "mouse left",
        "nudge left",
        "nudge mouse left",
        "pointer left",
        "move pointer left"
    ];

    private static readonly string[] SystemMouseMoveRightPrefixes =
    [
        "mouse right",
        "nudge right",
        "nudge mouse right",
        "pointer right",
        "move pointer right"
    ];

    private static readonly string[] SystemMouseDragUpPrefixes =
    [
        "mouse drag up",
        "drag pointer up",
        "drag up",
        "system mouse drag up"
    ];

    private static readonly string[] SystemMouseDragDownPrefixes =
    [
        "mouse drag down",
        "drag pointer down",
        "drag down",
        "system mouse drag down"
    ];

    private static readonly string[] SystemMouseDragLeftPrefixes =
    [
        "mouse drag left",
        "drag pointer left",
        "drag left",
        "system mouse drag left"
    ];

    private static readonly string[] SystemMouseDragRightPrefixes =
    [
        "mouse drag right",
        "drag pointer right",
        "drag right",
        "system mouse drag right"
    ];

    private static readonly (string Phrase, string Direction)[] MouseDirectionAliases =
    [
        ("top left", "top-left"),
        ("upper left", "top-left"),
        ("up left", "top-left"),
        ("left up", "top-left"),
        ("top right", "top-right"),
        ("upper right", "top-right"),
        ("up right", "top-right"),
        ("right up", "top-right"),
        ("bottom left", "bottom-left"),
        ("lower left", "bottom-left"),
        ("down left", "bottom-left"),
        ("left down", "bottom-left"),
        ("bottom right", "bottom-right"),
        ("lower right", "bottom-right"),
        ("down right", "bottom-right"),
        ("right down", "bottom-right"),
        ("up", "up"),
        ("down", "down"),
        ("left", "left"),
        ("right", "right")
    ];

    private static readonly string[] MouseMoveActionPrefixes =
    [
        "move mouse ",
        "move the mouse ",
        "move pointer ",
        "move the pointer ",
        "move cursor ",
        "move the cursor "
    ];

    private static readonly string[] MouseDragActionPrefixes =
    [
        "drag mouse ",
        "drag the mouse ",
        "drag pointer ",
        "drag the pointer ",
        "drag cursor ",
        "drag the cursor "
    ];

    private static readonly string[] SystemCopyPrefixes =
    [
        "copy",
        "system copy",
        "copy that",
        "copy selection",
        "copy selected text"
    ];

    private static readonly string[] SystemPastePrefixes =
    [
        "paste",
        "system paste",
        "paste that",
        "paste here",
        "paste clipboard",
        "paste selection"
    ];

    private static readonly string[] SystemCutPrefixes =
    [
        "cut",
        "system cut",
        "cut that",
        "cut selection",
        "cut selected text"
    ];

    private static readonly string[] SystemSelectAllPrefixes =
    [
        "select all",
        "system select all",
        "select all text",
        "highlight all",
        "highlight everything",
        "select everything",
        "highlight all text"
    ];

    private static readonly string[] SystemSavePrefixes =
    [
        "save",
        "system save",
        "save file",
        "save document",
        "save that",
        "save this",
        "save it",
        "save changes",
        "save current file",
        "save current document"
    ];

    private static readonly string[] SystemSaveAsPrefixes =
    [
        "save as",
        "save file as",
        "save document as",
        "save this as",
        "save that as",
        "open save as dialog",
        "show save as dialog"
    ];

    private static readonly string[] SystemUndoPrefixes =
    [
        "undo",
        "system undo",
        "undo last change",
        "undo it"
    ];

    private static readonly string[] SystemRedoPrefixes =
    [
        "redo",
        "system redo",
        "redo last change",
        "redo that",
        "do that again",
        "redo it"
    ];

    private static readonly string[] SystemBoldPrefixes =
    [
        "bold",
        "bold text",
        "bold that",
        "bold selection",
        "bold selected text",
        "make bold",
        "make text bold",
        "make that bold",
        "make selected text bold",
        "toggle bold"
    ];

    private static readonly string[] SystemItalicPrefixes =
    [
        "italic",
        "italic text",
        "italic that",
        "italic selection",
        "italic selected text",
        "italicize that",
        "italicize selection",
        "italicize selected text",
        "italics",
        "make italic",
        "make text italic",
        "make that italic",
        "make selected text italic",
        "toggle italic"
    ];

    private static readonly string[] SystemUnderlinePrefixes =
    [
        "underline",
        "underline text",
        "underline that",
        "underline selection",
        "underline selected text",
        "make underline",
        "make text underline",
        "make that underlined",
        "make selected text underlined",
        "toggle underline"
    ];

    private static readonly string[] SystemFindPrefixes =
    [
        "system find",
        "find in app",
        "find in this app",
        "find text",
        "search in app",
        "search this app"
    ];

    private static readonly string[] SystemFindNextPrefixes =
    [
        "find next in app",
        "find next in this app",
        "next match in app",
        "next match in this app",
        "next search result in app"
    ];

    private static readonly string[] SystemFindPreviousPrefixes =
    [
        "find previous in app",
        "find previous in this app",
        "previous match in app",
        "previous match in this app",
        "previous search result in app"
    ];

    private static readonly string[] SystemNewWindowPrefixes =
    [
        "system new window",
        "new window",
        "open new window"
    ];

    private static readonly string[] SystemNewDocumentPrefixes =
    [
        "new document",
        "new file",
        "start new document",
        "start new file",
        "create new document",
        "create new file",
        "make new document",
        "make new file"
    ];

    private static readonly string[] SystemOpenFilePrefixes =
    [
        "open file",
        "open document",
        "open a file",
        "open a document",
        "open existing file",
        "open existing document",
        "open saved file",
        "open saved document"
    ];

    private static readonly string[] SystemPrintPrefixes =
    [
        "print",
        "print that",
        "print this",
        "print document",
        "open print dialog",
        "show print dialog"
    ];

    private static readonly string[] SystemZoomInPrefixes =
    [
        "zoom in",
        "increase zoom",
        "larger",
        "make bigger",
        "make larger",
        "make text bigger",
        "make text larger",
        "increase text size"
    ];

    private static readonly string[] SystemZoomOutPrefixes =
    [
        "zoom out",
        "decrease zoom",
        "smaller",
        "make smaller",
        "make text smaller",
        "decrease text size"
    ];

    private static readonly string[] SystemZoomResetPrefixes =
    [
        "reset zoom",
        "zoom reset",
        "default zoom",
        "actual size",
        "default size",
        "normal size"
    ];

    private static readonly string[] SystemCloseWindowPrefixes =
    [
        "system close window",
        "close window",
        "close this window",
        "close current window",
        "close active window",
        "close app",
        "close application",
        "close current app",
        "close active app",
        "alt f4",
        "press alt f4"
    ];

    private static readonly string[] SystemMovePreviousCharacterPrefixes =
    [
        "system move previous character",
        "move previous character",
        "previous character",
        "last character",
        "previous letter",
        "last letter",
        "go to previous character",
        "go to last character",
        "move to previous character"
    ];

    private static readonly string[] SystemMoveNextCharacterPrefixes =
    [
        "system move next character",
        "move next character",
        "next character",
        "next letter",
        "go to next character",
        "move to next character"
    ];

    private static readonly string[] SystemSelectPreviousCharacterPrefixes =
    [
        "system select previous character",
        "select previous character",
        "select last character",
        "select previous letter",
        "select last letter",
        "highlight previous character"
    ];

    private static readonly string[] SystemSelectNextCharacterPrefixes =
    [
        "system select next character",
        "select next character",
        "select next letter",
        "highlight next character"
    ];

    private static readonly string[] SystemDeletePreviousCharacterPrefixes =
    [
        "system delete previous character",
        "delete previous character",
        "delete last character",
        "remove previous character",
        "remove last character",
        "backspace character",
        "delete previous letter",
        "delete last letter",
        "backspace letter"
    ];

    private static readonly string[] SystemDeleteNextCharacterPrefixes =
    [
        "system delete next character",
        "delete next character",
        "remove next character",
        "delete next letter"
    ];

    private static readonly string[] SystemMoveSelectionStartPrefixes =
    [
        "system move to beginning of selection",
        "move to beginning of selection",
        "move to start of selection",
        "go to beginning of selection",
        "go to start of selection"
    ];

    private static readonly string[] SystemMoveSelectionEndPrefixes =
    [
        "system move to end of selection",
        "move to end of selection",
        "go to end of selection"
    ];

    private static readonly string[] SystemClearSelectionPrefixes =
    [
        "system clear selection",
        "clear selection",
        "unselect that",
        "unselect",
        "clear selected text"
    ];

    private static readonly string[] SystemSelectCurrentWordPrefixes =
    [
        "system select word",
        "select word",
        "select this word",
        "highlight word",
        "highlight this word"
    ];

    private static readonly string[] SystemSelectCurrentLinePrefixes =
    [
        "system select line",
        "select line",
        "select this line",
        "highlight line",
        "highlight this line"
    ];

    private static readonly string[] SystemSelectCurrentParagraphPrefixes =
    [
        "system select paragraph",
        "select paragraph",
        "select this paragraph",
        "highlight paragraph",
        "highlight this paragraph"
    ];

    private static readonly string[] SystemDeleteCurrentWordPrefixes =
    [
        "system delete word",
        "delete word",
        "delete this word",
        "remove word",
        "remove this word"
    ];

    private static readonly string[] SystemDeleteCurrentLinePrefixes =
    [
        "system delete line",
        "delete line",
        "delete this line",
        "remove line",
        "remove this line"
    ];

    private static readonly string[] SystemDeleteCurrentParagraphPrefixes =
    [
        "system delete paragraph",
        "delete paragraph",
        "delete this paragraph",
        "remove paragraph",
        "remove this paragraph"
    ];

    private static readonly string[] SystemCopyCurrentWordPrefixes =
    [
        "system copy word",
        "copy word",
        "copy this word"
    ];

    private static readonly string[] SystemCopyCurrentLinePrefixes =
    [
        "system copy line",
        "copy line",
        "copy this line"
    ];

    private static readonly string[] SystemCopyCurrentParagraphPrefixes =
    [
        "system copy paragraph",
        "copy paragraph",
        "copy this paragraph"
    ];

    private static readonly string[] SystemCutCurrentWordPrefixes =
    [
        "system cut word",
        "cut word",
        "cut this word"
    ];

    private static readonly string[] SystemCutCurrentLinePrefixes =
    [
        "system cut line",
        "cut line",
        "cut this line"
    ];

    private static readonly string[] SystemCutCurrentParagraphPrefixes =
    [
        "system cut paragraph",
        "cut paragraph",
        "cut this paragraph"
    ];

    private static readonly string[] SystemCopyPreviousWordPrefixes =
    [
        "system copy previous word",
        "system copy last word",
        "copy previous word",
        "copy last word"
    ];

    private static readonly string[] SystemCopyNextWordPrefixes =
    [
        "system copy next word",
        "copy next word"
    ];

    private static readonly string[] SystemCutPreviousWordPrefixes =
    [
        "system cut previous word",
        "system cut last word",
        "cut previous word",
        "cut last word"
    ];

    private static readonly string[] SystemCutNextWordPrefixes =
    [
        "system cut next word",
        "cut next word"
    ];

    private static readonly string[] SystemCopyPreviousLinePrefixes =
    [
        "system copy previous line",
        "system copy last line",
        "copy previous line",
        "copy last line"
    ];

    private static readonly string[] SystemCopyNextLinePrefixes =
    [
        "system copy next line",
        "copy next line"
    ];

    private static readonly string[] SystemCutPreviousLinePrefixes =
    [
        "system cut previous line",
        "system cut last line",
        "cut previous line",
        "cut last line"
    ];

    private static readonly string[] SystemCutNextLinePrefixes =
    [
        "system cut next line",
        "cut next line"
    ];

    private static readonly string[] SystemCopyPreviousParagraphPrefixes =
    [
        "system copy previous paragraph",
        "system copy last paragraph",
        "copy previous paragraph",
        "copy last paragraph"
    ];

    private static readonly string[] SystemCopyNextParagraphPrefixes =
    [
        "system copy next paragraph",
        "copy next paragraph"
    ];

    private static readonly string[] SystemCutPreviousParagraphPrefixes =
    [
        "system cut previous paragraph",
        "system cut last paragraph",
        "cut previous paragraph",
        "cut last paragraph"
    ];

    private static readonly string[] SystemCutNextParagraphPrefixes =
    [
        "system cut next paragraph",
        "cut next paragraph"
    ];

    private static readonly string[] SystemMoveLineStartPrefixes =
    [
        "system go to line start",
        "go to line start",
        "move to line start",
        "line start",
        "start of line"
    ];

    private static readonly string[] SystemMoveLineEndPrefixes =
    [
        "system go to line end",
        "go to line end",
        "move to line end",
        "line end",
        "end of line"
    ];

    private static readonly string[] SystemMovePreviousLinePrefixes =
    [
        "system move previous line",
        "move previous line",
        "previous line",
        "go to previous line"
    ];

    private static readonly string[] SystemMoveNextLinePrefixes =
    [
        "system move next line",
        "move next line",
        "go to next line"
    ];

    private static readonly string[] SystemSelectToLineStartPrefixes =
    [
        "system select to line start",
        "select to line start",
        "select to the start of the line"
    ];

    private static readonly string[] SystemSelectToLineEndPrefixes =
    [
        "system select to line end",
        "select to line end",
        "select to the end of the line"
    ];

    private static readonly string[] SystemSelectPreviousLinePrefixes =
    [
        "system select previous line",
        "select previous line",
        "highlight previous line"
    ];

    private static readonly string[] SystemSelectNextLinePrefixes =
    [
        "system select next line",
        "select next line",
        "highlight next line"
    ];

    private static readonly string[] SystemDeleteToLineStartPrefixes =
    [
        "system delete to line start",
        "delete to line start",
        "delete to the start of the line"
    ];

    private static readonly string[] SystemDeleteToLineEndPrefixes =
    [
        "system delete to line end",
        "delete to line end",
        "delete to the end of the line"
    ];

    private static readonly string[] SystemDeletePreviousLinePrefixes =
    [
        "system delete previous line",
        "delete previous line",
        "remove previous line"
    ];

    private static readonly string[] SystemDeleteNextLinePrefixes =
    [
        "system delete next line",
        "delete next line",
        "remove next line"
    ];

    private static readonly string[] SystemMoveWordStartPrefixes =
    [
        "system go to word start",
        "system go to beginning of word",
        "go to word start",
        "go to beginning of word",
        "go to start of word",
        "move to word start",
        "move to beginning of word",
        "move to start of word",
        "word start",
        "start of word",
        "beginning of word"
    ];

    private static readonly string[] SystemMoveWordEndPrefixes =
    [
        "system go to word end",
        "system go to end of word",
        "go to word end",
        "go to end of word",
        "move to word end",
        "move to end of word",
        "word end",
        "end of word"
    ];

    private static readonly string[] SystemMovePreviousWordPrefixes =
    [
        "system move previous word",
        "system move last word",
        "move previous word",
        "move last word",
        "previous word",
        "last word",
        "go previous word",
        "go to previous word",
        "go to last word",
        "move to previous word"
    ];

    private static readonly string[] SystemMoveNextWordPrefixes =
    [
        "system move next word",
        "move next word",
        "next word",
        "go next word",
        "go to next word",
        "move to next word"
    ];

    private static readonly string[] SystemSelectPreviousWordPrefixes =
    [
        "system select previous word",
        "system select last word",
        "select previous word",
        "select last word",
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
        "system delete last word",
        "delete previous word",
        "delete last word",
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
        "system move last sentence",
        "move previous sentence",
        "move last sentence",
        "previous sentence",
        "last sentence",
        "go to previous sentence",
        "go to last sentence",
        "move to previous sentence"
    ];

    private static readonly string[] SystemMoveNextSentencePrefixes =
    [
        "system move next sentence",
        "move next sentence",
        "next sentence",
        "go to next sentence",
        "move to next sentence"
    ];

    private static readonly string[] SystemSelectPreviousSentencePrefixes =
    [
        "system select previous sentence",
        "system select last sentence",
        "select previous sentence",
        "select last sentence",
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
        "system delete last sentence",
        "delete previous sentence",
        "delete last sentence",
        "remove previous sentence"
    ];

    private static readonly string[] SystemDeleteNextSentencePrefixes =
    [
        "system delete next sentence",
        "delete next sentence",
        "remove next sentence"
    ];

    private static readonly string[] SystemMoveParagraphStartPrefixes =
    [
        "system go to paragraph start",
        "go to paragraph start",
        "move to paragraph start",
        "paragraph start",
        "start of paragraph"
    ];

    private static readonly string[] SystemMoveParagraphEndPrefixes =
    [
        "system go to paragraph end",
        "go to paragraph end",
        "move to paragraph end",
        "paragraph end",
        "end of paragraph"
    ];

    private static readonly string[] SystemSelectToParagraphStartPrefixes =
    [
        "system select to paragraph start",
        "select to paragraph start",
        "select to the start of the paragraph"
    ];

    private static readonly string[] SystemSelectToParagraphEndPrefixes =
    [
        "system select to paragraph end",
        "select to paragraph end",
        "select to the end of the paragraph"
    ];

    private static readonly string[] SystemDeleteToParagraphStartPrefixes =
    [
        "system delete to paragraph start",
        "delete to paragraph start",
        "delete to the start of the paragraph"
    ];

    private static readonly string[] SystemDeleteToParagraphEndPrefixes =
    [
        "system delete to paragraph end",
        "delete to paragraph end",
        "delete to the end of the paragraph"
    ];

    private static readonly string[] SystemMovePreviousParagraphPrefixes =
    [
        "system move previous paragraph",
        "system move last paragraph",
        "move previous paragraph",
        "move last paragraph",
        "previous paragraph",
        "last paragraph",
        "go to previous paragraph",
        "go to last paragraph",
        "move to previous paragraph"
    ];

    private static readonly string[] SystemMoveNextParagraphPrefixes =
    [
        "system move next paragraph",
        "move next paragraph",
        "next paragraph",
        "go to next paragraph",
        "move to next paragraph"
    ];

    private static readonly string[] SystemSelectPreviousParagraphPrefixes =
    [
        "system select previous paragraph",
        "system select last paragraph",
        "select previous paragraph",
        "select last paragraph",
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
        "system delete last paragraph",
        "delete previous paragraph",
        "delete last paragraph",
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
    private const string BrowserActionStopLoading = "browser-stop-loading";
    private const string BrowserActionNewTab = "browser-new-tab";
    private const string BrowserActionNewWindow = "browser-new-window";
    private const string BrowserActionPrivateWindow = "browser-private-window";
    private const string BrowserActionBookmarkPage = "browser-bookmark-page";
    private const string BrowserActionOpenBookmarks = "browser-open-bookmarks";
    private const string BrowserActionSavePage = "browser-save-page";
    private const string BrowserActionPrintPage = "browser-print-page";
    private const string BrowserActionNextTab = "browser-next-tab";
    private const string BrowserActionPreviousTab = "browser-previous-tab";
    private const string BrowserActionMoveTabLeft = "browser-move-tab-left";
    private const string BrowserActionMoveTabRight = "browser-move-tab-right";
    private const string BrowserActionSelectTabPrefix = "browser-select-tab:";
    private const string BrowserActionCloseTab = "browser-close-tab";
    private const string BrowserActionReopenClosedTab = "browser-reopen-closed-tab";
    private const string BrowserActionDuplicateTab = "browser-duplicate-tab";
    private const string BrowserActionCopyAddress = "browser-copy-address";
    private const string BrowserActionFocusAddressBar = "browser-focus-address-bar";
    private const string BrowserActionAddressTextPrefix = "browser-address-text:";
    private const string BrowserActionHome = "browser-home";
    private const string BrowserActionFullscreen = "browser-fullscreen";
    private const string BrowserActionOpenDownloads = "browser-open-downloads";
    private const string BrowserActionOpenHistory = "browser-open-history";
    private const string BrowserActionFind = "browser-find";
    private const string BrowserActionFindTextPrefix = "browser-find-text:";
    private const string BrowserActionFindNext = "browser-find-next";
    private const string BrowserActionFindPrevious = "browser-find-previous";
    private const string BrowserActionStartScrollUp = "browser-start-scroll-up";
    private const string BrowserActionStartScrollDown = "browser-start-scroll-down";
    private const string BrowserActionStartScrollLeft = "browser-start-scroll-left";
    private const string BrowserActionStartScrollRight = "browser-start-scroll-right";
    private const string BrowserActionStopScroll = "browser-stop-scroll";
    private const string BrowserActionScrollUp = "browser-scroll-up";
    private const string BrowserActionScrollDown = "browser-scroll-down";
    private const string BrowserActionScrollUpPagesPrefix = "browser-scroll-up-pages:";
    private const string BrowserActionScrollDownPagesPrefix = "browser-scroll-down-pages:";
    private const string BrowserActionScrollLeft = "browser-scroll-left";
    private const string BrowserActionScrollRight = "browser-scroll-right";
    private const string BrowserActionScrollTop = "browser-scroll-top";
    private const string BrowserActionScrollBottom = "browser-scroll-bottom";
    private const string BrowserActionZoomIn = "browser-zoom-in";
    private const string BrowserActionZoomOut = "browser-zoom-out";
    private const string BrowserActionZoomReset = "browser-zoom-reset";
    private const string SystemActionVolumeUp = "system-volume-up";
    private const string SystemActionVolumeDown = "system-volume-down";
    private const string SystemActionVolumeMute = "system-volume-mute";
    private const string SystemActionMediaPlayPause = "system-media-play-pause";
    private const string SystemActionMediaNextTrack = "system-media-next-track";
    private const string SystemActionMediaPreviousTrack = "system-media-previous-track";
    private const string SystemActionMediaStop = "system-media-stop";
    private const string SystemActionShowDesktop = "system-show-desktop";
    private const string SystemActionNextWindow = "system-next-window";
    private const string SystemActionPreviousWindow = "system-previous-window";
    private const string SystemActionOpenTaskView = "system-open-task-view";
    private const string SystemActionSwitchWindowPrefix = "system-switch-window:";
    private const string SystemActionOpenQuickSettings = "system-open-quick-settings";
    private const string SystemActionOpenNotificationCenter = "system-open-notification-center";
    private const string SystemActionOpenEmojiPanel = "system-open-emoji-panel";
    private const string SystemActionOpenClipboardHistory = "system-open-clipboard-history";
    private const string SystemActionOpenSnippingToolbar = "system-open-snipping-toolbar";
    private const string SystemActionOpenProjectDisplay = "system-open-project-display";
    private const string SystemActionOpenCastDisplay = "system-open-cast-display";
    private const string SystemActionNewVirtualDesktop = "system-new-virtual-desktop";
    private const string SystemActionNextVirtualDesktop = "system-next-virtual-desktop";
    private const string SystemActionPreviousVirtualDesktop = "system-previous-virtual-desktop";
    private const string SystemActionOpenTaskManager = "system-open-task-manager";
    private const string SystemActionOpenSettings = "system-open-settings";
    private const string SystemActionOpenDisplaySettings = "system-open-display-settings";
    private const string SystemActionOpenSoundSettings = "system-open-sound-settings";
    private const string SystemActionOpenBluetoothSettings = "system-open-bluetooth-settings";
    private const string SystemActionOpenWifiSettings = "system-open-wifi-settings";
    private const string SystemActionOpenNetworkSettings = "system-open-network-settings";
    private const string SystemActionOpenAccessibilitySettings = "system-open-accessibility-settings";
    private const string SystemActionOpenMagnifierSettings = "system-open-magnifier-settings";
    private const string SystemActionOpenNarratorSettings = "system-open-narrator-settings";
    private const string SystemActionOpenCaptionsSettings = "system-open-captions-settings";
    private const string SystemActionOpenSpeechSettings = "system-open-speech-settings";
    private const string SystemActionOpenMagnifier = "system-open-magnifier";
    private const string SystemActionMagnifierZoomOut = "system-magnifier-zoom-out";
    private const string SystemActionCloseMagnifier = "system-close-magnifier";
    private const string SystemActionOpenMouseSettings = "system-open-mouse-settings";
    private const string SystemActionOpenKeyboardSettings = "system-open-keyboard-settings";
    private const string SystemActionOpenPrivacySettings = "system-open-privacy-settings";
    private const string SystemActionOpenPowerSettings = "system-open-power-settings";
    private const string SystemActionOpenAppsSettings = "system-open-apps-settings";
    private const string SystemActionOpenDefaultAppsSettings = "system-open-default-apps-settings";
    private const string SystemActionOpenDateTimeSettings = "system-open-date-time-settings";
    private const string SystemActionOpenNotificationsSettings = "system-open-notifications-settings";
    private const string SystemActionOpenWindowsUpdateSettings = "system-open-windows-update-settings";
    private const string SystemActionOpenPersonalizationSettings = "system-open-personalization-settings";
    private const string SystemActionMinimizeWindow = "system-minimize-window";
    private const string SystemActionMaximizeWindow = "system-maximize-window";
    private const string SystemActionRestoreWindow = "system-restore-window";
    private const string SystemActionSnapWindowLeft = "system-snap-window-left";
    private const string SystemActionSnapWindowRight = "system-snap-window-right";
    private const string SystemActionSnapWindowUp = "system-snap-window-up";
    private const string SystemActionSnapWindowDown = "system-snap-window-down";
    private const string SystemActionShowSnapLayouts = "system-show-snap-layouts";
    private const string SystemActionPressEnter = "system-press-enter";
    private const string SystemActionPressTab = "system-press-tab";
    private const string SystemActionPressEscape = "system-press-escape";
    private const string SystemActionPressBackspace = "system-press-backspace";
    private const string SystemActionPressSpace = "system-press-space";
    private const string SystemActionPressDelete = "system-press-delete";
    private const string SystemActionPressInsert = "system-press-insert";
    private const string SystemActionPressWindows = "system-press-windows";
    private const string SystemActionPressContextMenu = "system-press-context-menu";
    private const string SystemActionPressCapsLock = "system-press-caps-lock";
    private const string SystemActionRepeatPrefix = "system-repeat:";
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
    private const string SystemActionMouseTripleClick = "system-mouse-triple-click";
    private const string SystemActionMouseRightClick = "system-mouse-right-click";
    private const string SystemActionMouseButtonDown = "system-mouse-button-down";
    private const string SystemActionMouseButtonUp = "system-mouse-button-up";
    private const string SystemActionScrollTop = "system-scroll-top";
    private const string SystemActionScrollBottom = "system-scroll-bottom";
    private const string SystemActionScrollLeftEdge = "system-scroll-left-edge";
    private const string SystemActionScrollRightEdge = "system-scroll-right-edge";
    private const string SystemActionStartScrollingPrefix = "system-start-scrolling:";
    private const string SystemActionStopScrolling = "system-stop-scrolling";
    private const string SystemActionMouseScrollUp = "system-mouse-scroll-up";
    private const string SystemActionMouseScrollDown = "system-mouse-scroll-down";
    private const string SystemActionMouseScrollLeft = "system-mouse-scroll-left";
    private const string SystemActionMouseScrollRight = "system-mouse-scroll-right";
    private const string SystemActionMouseStartMovingPrefix = "system-mouse-start-moving:";
    private const string SystemActionMouseStopMoving = "system-mouse-stop-moving";
    private const string SystemActionMouseMoveFaster = "system-mouse-move-faster";
    private const string SystemActionMouseMoveSlower = "system-mouse-move-slower";
    private const string SystemActionMouseMoveFixedPrefix = "system-mouse-move-fixed:";
    private const string SystemActionMouseMoveUp = "system-mouse-move-up";
    private const string SystemActionMouseMoveDown = "system-mouse-move-down";
    private const string SystemActionMouseMoveLeft = "system-mouse-move-left";
    private const string SystemActionMouseMoveRight = "system-mouse-move-right";
    private const string SystemActionMouseDragDirectionPrefix = "system-mouse-drag-direction:";
    private const string SystemActionMouseDragUp = "system-mouse-drag-up";
    private const string SystemActionMouseDragDown = "system-mouse-drag-down";
    private const string SystemActionMouseDragLeft = "system-mouse-drag-left";
    private const string SystemActionMouseDragRight = "system-mouse-drag-right";
    private const string SystemActionHoldModifierPrefix = "system-hold-modifier:";
    private const string SystemActionReleaseModifierPrefix = "system-release-modifier:";
    private const string SystemActionReleaseModifiers = "system-release-modifiers";
    private const string SystemActionCopy = "system-copy";
    private const string SystemActionPaste = "system-paste";
    private const string SystemActionCut = "system-cut";
    private const string SystemActionSelectAll = "system-select-all";
    private const string SystemActionSave = "system-save";
    private const string SystemActionSaveAs = "system-save-as";
    private const string SystemActionUndo = "system-undo";
    private const string SystemActionRedo = "system-redo";
    private const string SystemActionBold = "system-bold";
    private const string SystemActionItalic = "system-italic";
    private const string SystemActionUnderline = "system-underline";
    private const string SystemActionFind = "system-find";
    private const string SystemActionFindNext = "system-find-next";
    private const string SystemActionFindPrevious = "system-find-previous";
    private const string SystemActionNewWindow = "system-new-window";
    private const string SystemActionNewDocument = "system-new-document";
    private const string SystemActionOpenFile = "system-open-file";
    private const string SystemActionPrint = "system-print";
    private const string SystemActionZoomIn = "system-zoom-in";
    private const string SystemActionZoomOut = "system-zoom-out";
    private const string SystemActionZoomReset = "system-zoom-reset";
    private const string SystemActionCloseWindow = "system-close-window";
    private const string SystemActionMovePreviousCharacter = "system-move-previous-character";
    private const string SystemActionMoveNextCharacter = "system-move-next-character";
    private const string SystemActionSelectPreviousCharacter = "system-select-previous-character";
    private const string SystemActionSelectNextCharacter = "system-select-next-character";
    private const string SystemActionDeletePreviousCharacter = "system-delete-previous-character";
    private const string SystemActionDeleteNextCharacter = "system-delete-next-character";
    private const string SystemActionMoveSelectionStart = "system-move-selection-start";
    private const string SystemActionMoveSelectionEnd = "system-move-selection-end";
    private const string SystemActionClearSelection = "system-clear-selection";
    private const string SystemActionSelectCurrentWord = "system-select-current-word";
    private const string SystemActionSelectCurrentLine = "system-select-current-line";
    private const string SystemActionSelectCurrentParagraph = "system-select-current-paragraph";
    private const string SystemActionDeleteCurrentWord = "system-delete-current-word";
    private const string SystemActionDeleteCurrentLine = "system-delete-current-line";
    private const string SystemActionDeleteCurrentParagraph = "system-delete-current-paragraph";
    private const string SystemActionCopyCurrentWord = "system-copy-current-word";
    private const string SystemActionCopyCurrentLine = "system-copy-current-line";
    private const string SystemActionCopyCurrentParagraph = "system-copy-current-paragraph";
    private const string SystemActionCutCurrentWord = "system-cut-current-word";
    private const string SystemActionCutCurrentLine = "system-cut-current-line";
    private const string SystemActionCutCurrentParagraph = "system-cut-current-paragraph";
    private const string SystemActionCopyPreviousWord = "system-copy-previous-word";
    private const string SystemActionCopyNextWord = "system-copy-next-word";
    private const string SystemActionCutPreviousWord = "system-cut-previous-word";
    private const string SystemActionCutNextWord = "system-cut-next-word";
    private const string SystemActionCopyPreviousLine = "system-copy-previous-line";
    private const string SystemActionCopyNextLine = "system-copy-next-line";
    private const string SystemActionCutPreviousLine = "system-cut-previous-line";
    private const string SystemActionCutNextLine = "system-cut-next-line";
    private const string SystemActionCopyPreviousParagraph = "system-copy-previous-paragraph";
    private const string SystemActionCopyNextParagraph = "system-copy-next-paragraph";
    private const string SystemActionCutPreviousParagraph = "system-cut-previous-paragraph";
    private const string SystemActionCutNextParagraph = "system-cut-next-paragraph";
    private const string SystemActionMoveLineStart = "system-move-line-start";
    private const string SystemActionMoveLineEnd = "system-move-line-end";
    private const string SystemActionMovePreviousLine = "system-move-previous-line";
    private const string SystemActionMoveNextLine = "system-move-next-line";
    private const string SystemActionSelectToLineStart = "system-select-to-line-start";
    private const string SystemActionSelectToLineEnd = "system-select-to-line-end";
    private const string SystemActionSelectPreviousLine = "system-select-previous-line";
    private const string SystemActionSelectNextLine = "system-select-next-line";
    private const string SystemActionDeleteToLineStart = "system-delete-to-line-start";
    private const string SystemActionDeleteToLineEnd = "system-delete-to-line-end";
    private const string SystemActionDeletePreviousLine = "system-delete-previous-line";
    private const string SystemActionDeleteNextLine = "system-delete-next-line";
    private const string SystemActionMoveWordStart = "system-move-word-start";
    private const string SystemActionMoveWordEnd = "system-move-word-end";
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
    private const string SystemActionMoveParagraphStart = "system-move-paragraph-start";
    private const string SystemActionMoveParagraphEnd = "system-move-paragraph-end";
    private const string SystemActionSelectToParagraphStart = "system-select-to-paragraph-start";
    private const string SystemActionSelectToParagraphEnd = "system-select-to-paragraph-end";
    private const string SystemActionDeleteToParagraphStart = "system-delete-to-paragraph-start";
    private const string SystemActionDeleteToParagraphEnd = "system-delete-to-paragraph-end";
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
    private const string UiActionGettingStarted = "ui-getting-started";
    private const string UiActionOpenPacks = "ui-open-packs";
    private const string UiActionImportPack = "ui-import-pack";
    private const string UiActionImportPackFolder = "ui-import-pack-folder";
    private const string UiActionUpdatePack = "ui-update-pack";
    private const string UiActionRollbackPack = "ui-rollback-pack";
    private const string UiActionRefreshPacks = "ui-refresh-packs";
    private const string UiActionOpenPacksFolder = "ui-open-packs-folder";
    private const string UiActionEnableSelectedPack = "ui-enable-selected-pack";
    private const string UiActionDisableSelectedPack = "ui-disable-selected-pack";
    private const string UiActionRemoveSelectedPack = "ui-remove-selected-pack";
    private const string UiActionOpenShortcuts = "ui-open-shortcuts";
    private const string UiActionNewVoiceShortcut = "ui-new-voice-shortcut";
    private const string UiActionSaveVoiceShortcut = "ui-save-voice-shortcut";
    private const string UiActionDeleteVoiceShortcut = "ui-delete-voice-shortcut";
    private const string UiActionEnableVoiceShortcut = "ui-enable-voice-shortcut";
    private const string UiActionDisableVoiceShortcut = "ui-disable-voice-shortcut";
    private const string UiActionAddVoiceShortcutCommandAction = "ui-add-voice-shortcut-command-action";
    private const string UiActionAddVoiceShortcutWaitAction = "ui-add-voice-shortcut-wait-action";
    private const string UiActionRemoveVoiceShortcutAction = "ui-remove-voice-shortcut-action";
    private const string UiActionStartListening = "ui-start-listening";
    private const string UiActionRestartListening = "ui-restart-listening";
    private const string UiActionStopListening = "ui-stop-listening";
    private const string UiActionCancelSession = "ui-cancel-session";
    private const string UiActionResetSession = "ui-reset-session";
    private const string UiActionBlockedExternalSideEffect = "ui-blocked-external-side-effect";
    private const string UiActionVoiceHelp = "ui-voice-help";
    private const string UiActionVoiceHelpSystem = "ui-voice-help-system";
    private const string UiActionVoiceHelpBrowser = "ui-voice-help-browser";
    private const string UiActionVoiceHelpDictation = "ui-voice-help-dictation";
    private const string UiActionVoiceHelpVisible = "ui-voice-help-visible";
    private const string UiActionVoiceHelpExtensions = "ui-voice-help-extensions";
    private const string UiActionVoiceHelpSafety = "ui-voice-help-safety";
    private const string UiActionVoiceHelpFree = "ui-voice-help-free";
    private const string UiActionVoiceHelpPaid = "ui-voice-help-paid";
    private const string UiActionVoiceHelpPro = "ui-voice-help-pro";
    private const string UiActionVoiceHelpAdvanced = "ui-voice-help-advanced";
    private const string UiActionCheckUpdatesNow = "ui-check-updates-now";
    private const string UiActionReadUpdateSummaryAgain = "ui-read-update-summary-again";
    private const string UiActionReadImportSummaryAgain = "ui-read-import-summary-again";
    private const string UiActionReadWatchStatus = "ui-read-watch-status";
    private const string UiActionOpenReleaseProof = "ui-open-release-proof";
    private const string UiActionReadReleaseProof = "ui-read-release-proof";
    private const string UiActionOpenReleaseEvidence = "ui-open-release-evidence";
    private const string UiActionOpenProofNotes = "ui-open-proof-notes";
    private const string UiActionOpenManualEvidence = "ui-open-manual-evidence";
    private const string UiActionOpenManualEvidenceChecklist = "ui-open-manual-evidence-checklist";
    private const string UiActionOpenInstaller = "ui-open-installer";
    private const string UiActionReadUpdatesStatus = "ui-read-updates-status";
    private const string UiActionReadCheckInStatus = "ui-read-check-in-status";
    private const string UiActionReadEvidenceStatus = "ui-read-evidence-status";
    private const string UiActionReadEvidenceHeader = "ui-read-evidence-header";
    private const string UiActionReadReleaseBlockers = "ui-read-release-blockers";
    private const string UiActionReadNextProof = "ui-read-next-proof";
    private const string UiActionReadReleaseGates = "ui-read-release-gates";
    private const string UiActionReadNextProofInstructions = "ui-read-next-proof-instructions";
    private const string UiActionReadProofNotesStatus = "ui-read-proof-notes-status";
    private const string UiActionCreateNextProofNote = "ui-create-next-proof-note";
    private const string UiActionCreateAllProofNotes = "ui-create-all-proof-notes";
    private const string UiActionCreateEvidenceDraft = "ui-create-evidence-draft";
    private const string UiActionOpenEvidenceDraft = "ui-open-evidence-draft";
    private const string UiActionReadEvidenceDraft = "ui-read-evidence-draft";
    private const string UiActionReadVisualStatus = "ui-read-visual-status";
    private const string UiActionReadRestartProof = "ui-read-restart-proof";
    private const string UiActionReadPlansStatus = "ui-read-plans-status";
    private const string UiActionReadVoiceModeStatus = "ui-read-voice-mode-status";
    private const string UiActionReadClockStatus = "ui-read-clock-status";
    private const string UiActionReadStatus = "ui-read-status";
    private const string UiActionStopStatusReadback = "ui-stop-status-readback";
    private const string UiActionClearRecentSpeech = "ui-clear-recent-speech";
    private const string UiActionHideCommandPalette = "ui-hide-command-palette";
    private const string UiActionHideImportSplash = "ui-hide-import-splash";
    private const string UiActionHideUpdateSplash = "ui-hide-update-splash";
    private const string UiActionShowVisibleControls = "ui-show-visible-controls";
    private const string UiActionShowVisibleControlsTaskbar = "ui-show-visible-controls-taskbar";
    private const string UiActionShowVisibleControlsWindowPrefix = "ui-show-visible-controls-window:";
    private const string UiActionHideVisibleControls = "ui-hide-visible-controls";
    private const string UiActionHideBrowserOverlays = "ui-hide-browser-overlays";
    private const string UiActionSetVoiceModePrefix = "ui-set-voice-mode:";
    private const string UiActionShowKeyboard = "ui-show-keyboard";
    private const string UiActionHideKeyboard = "ui-hide-keyboard";
    private const string UiActionAddVocabularyPrefix = "ui-add-vocabulary:";
    private const string UiActionSetDictationOptionPrefix = "ui-set-dictation-option:";
    private const string UiActionShowMouseGrid = "ui-show-mouse-grid";
    private const string UiActionShowMouseGridHere = "ui-show-mouse-grid-here";
    private const string UiActionHideMouseGrid = "ui-hide-mouse-grid";
    private const string UiActionUndoMouseGrid = "ui-undo-mouse-grid";
    private const string UiActionMarkMouseGrid = "ui-mark-mouse-grid";
    private const string UiActionMarkMouseGridCellPrefix = "ui-mark-mouse-grid-cell:";
    private const string UiActionDragMarkedMouseGrid = "ui-drag-marked-mouse-grid";
    private const string UiActionFocusMouseGridShortcutPathPrefix = "ui-focus-mouse-grid-shortcut-path:";
    private const string UiActionFocusMouseGridDisplayPrefix = "ui-focus-mouse-grid-display:";
    private const string UiActionFocusMouseGridPathPrefix = "ui-focus-mouse-grid-path:";
    private const string UiActionSelectMouseGridCellPrefix = "ui-select-mouse-grid-cell:";
    private const string UiActionClickMouseGridCellPrefix = "ui-click-mouse-grid-cell:";
    private const string UiActionDragMouseGridPrefix = "ui-drag-mouse-grid:";
    private const string UiActionSelectFileResultPrefix = "ui-select-file-result:";
    private const string UiActionOpenFileResultPrefix = "ui-open-file-result:";
    private const string UiActionRevealFileResultPrefix = "ui-reveal-file-result:";
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
        "search my files for ",
        "search my documents for ",
        "search my folders for ",
        "search this pc for ",
        "search my computer for ",
        "look in files for ",
        "look in folders for ",
        "find document ",
        "open file search for ",
        "search explorer for ",
        "search files called ",
        "search folders called "
    ];

    private static readonly string[] UiOpenFileResultPrefixes =
    [
        "open file result ",
        "open search result ",
        "open result ",
        "open file number ",
        "open folder result ",
        "open "
    ];

    private static readonly string[] UiRevealFileResultPrefixes =
    [
        "reveal file result ",
        "reveal search result ",
        "reveal result ",
        "show file result ",
        "show search result ",
        "show result ",
        "open file result folder ",
        "open search result folder ",
        "open result folder ",
        "open containing folder for result ",
        "show containing folder for result ",
        "reveal ",
        "show "
    ];

    private static readonly string[] UiSelectFileResultPrefixes =
    [
        "select file result ",
        "select search result ",
        "select result ",
        "choose file result ",
        "choose search result ",
        "choose result ",
        "select ",
        "choose "
    ];

    private static readonly string[] DictationPrefixes =
    [
        "start dictation",
        "start typing",
        "resume dictation",
        "resume voice dictation",
        "resume typing",
        "continue dictation",
        "continue voice dictation",
        "continue typing",
        "begin dictation",
        "begin typing",
        "dictation",
        "take dictation",
        "start taking dictation",
        "resume taking dictation"
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

    private static readonly string[] UiGettingStartedPrefixes =
    [
        "getting started",
        "open getting started",
        "show getting started",
        "start guide",
        "show setup guide",
        "open setup guide",
        "open voice access guide",
        "show voice access guide",
        "open voice access tutorial",
        "show voice access tutorial"
    ];

    private static readonly string[] UiOpenShortcutsPrefixes =
    [
        "open voice shortcuts",
        "show voice shortcuts",
        "open shortcuts",
        "show shortcuts"
    ];

    private static readonly string[] UiNewVoiceShortcutPrefixes =
    [
        "new voice shortcut",
        "create voice shortcut",
        "create new voice shortcut",
        "new shortcut"
    ];

    private static readonly string[] UiSaveVoiceShortcutPrefixes =
    [
        "save voice shortcut",
        "save shortcut",
        "update voice shortcut"
    ];

    private static readonly string[] UiDeleteVoiceShortcutPrefixes =
    [
        "delete voice shortcut",
        "remove voice shortcut",
        "delete shortcut"
    ];

    private static readonly string[] UiEnableVoiceShortcutPrefixes =
    [
        "enable voice shortcut",
        "turn on voice shortcut",
        "enable shortcut"
    ];

    private static readonly string[] UiDisableVoiceShortcutPrefixes =
    [
        "disable voice shortcut",
        "turn off voice shortcut",
        "disable shortcut"
    ];

    private static readonly string[] UiAddVoiceShortcutCommandActionPrefixes =
    [
        "add voice shortcut command action",
        "add shortcut command action",
        "add command action"
    ];

    private static readonly string[] UiAddVoiceShortcutWaitActionPrefixes =
    [
        "add voice shortcut wait action",
        "add shortcut wait action",
        "add wait action"
    ];

    private static readonly string[] UiRemoveVoiceShortcutActionPrefixes =
    [
        "remove voice shortcut action",
        "remove shortcut action"
    ];

    private static readonly string[] UiStartListeningPrefixes =
    [
        "start listening",
        "start voice",
        "voice access wake up",
        "wake up",
        "wake callsign",
        "start callsign",
        "listen",
        "begin listening",
        "microphone on",
        "mic on",
        "unmute microphone",
        "unmute mic"
    ];

    private static readonly string[] UiRestartListeningPrefixes =
    [
        "restart listening",
        "restart voice",
        "restart callsign",
        "restart microphone",
        "restart voice access",
        "restart listener",
        "relisten"
    ];

    private static readonly string[] UiStopListeningPrefixes =
    [
        "stop listening",
        "stop voice",
        "voice access sleep",
        "go to sleep",
        "sleep",
        "stop callsign",
        "end listening",
        "turn off microphone",
        "turn off mic",
        "turn off voice access",
        "stop voice access",
        "close voice access",
        "exit voice access",
        "quit voice access",
        "mute microphone",
        "mute mic",
        "microphone off",
        "mic off"
    ];

    private static readonly string[] UiCancelSessionPrefixes =
    [
        "cancel",
        "cancel session",
        "stop",
        "stop now",
        "pause",
        "never mind",
        "nevermind",
        "stop command",
        "cancel command",
        "abort command"
    ];

    private static readonly string[] UiResetSessionPrefixes =
    [
        "reset session",
        "reset voice session",
        "start over",
        "restart session",
        "clear session",
        "reset callsign"
    ];

    private static readonly string[] UiBlockedExternalSideEffectPrefixes =
    [
        "submit",
        "submit form",
        "submit this form",
        "click submit",
        "press submit",
        "send",
        "send message",
        "send email",
        "send mail",
        "post",
        "post comment",
        "publish",
        "upload",
        "upload file",
        "accept terms",
        "accept legal terms",
        "purchase",
        "buy now",
        "pay",
        "pay now",
        "delete cloud data",
        "download and run",
        "download and execute",
        "run downloaded file",
        "open downloaded installer"
    ];

    private static readonly string[] UiVoiceHelpPrefixes =
    [
        "voice help",
        "what can i say",
        "what can i do",
        "show all commands",
        "show command list",
        "show voice help",
        "show commands",
        "list commands",
        "help",
        "open voice access help",
        "show voice access help"
    ];

    private static readonly string[] UiVoiceHelpSystemPrefixes =
    [
        "show system commands",
        "show system help",
        "show safe system commands"
    ];

    private static readonly string[] UiVoiceHelpBrowserPrefixes =
    [
        "show browser commands",
        "show browser help"
    ];

    private static readonly string[] UiVoiceHelpDictationPrefixes =
    [
        "show dictation commands",
        "show dictation help"
    ];

    private static readonly string[] UiVoiceHelpVisiblePrefixes =
    [
        "show visible commands",
        "show visible controls commands"
    ];

    private static readonly string[] UiVoiceHelpExtensionsPrefixes =
    [
        "show extension commands",
        "show extensions commands"
    ];

    private static readonly string[] UiVoiceHelpSafetyPrefixes =
    [
        "show safety commands",
        "show stop commands"
    ];

    private static readonly string[] UiVoiceHelpFreePrefixes =
    [
        "show free commands",
        "show built in commands",
        "show built in help"
    ];

    private static readonly string[] UiVoiceHelpPaidPrefixes =
    [
        "show paid commands",
        "show paid help"
    ];

    private static readonly string[] UiVoiceHelpProPrefixes =
    [
        "show pro commands",
        "show pro help"
    ];

    private static readonly string[] UiVoiceHelpAdvancedPrefixes =
    [
        "show advanced commands",
        "show advanced help"
    ];

    private static readonly string[] UiReadStatusPrefixes =
    [
        "what did you hear",
        "what did callsign hear",
        "repeat what you heard",
        "repeat last heard",
        "read last heard",
        "read current status",
        "read status",
        "repeat status",
        "say status",
        "speak status",
        "read the status",
        "repeat the status"
    ];

    private static readonly string[] UiReadClockStatusPrefixes =
    [
        "what time is it",
        "what is the time",
        "tell me the time",
        "read the time",
        "read time",
        "current time",
        "what's today's date",
        "what is today's date",
        "what is todays date",
        "what date is it",
        "tell me the date",
        "read the date",
        "read date",
        "current date",
        "read time and date",
        "read date and time"
    ];

    private static readonly string[] UiStopStatusReadbackPrefixes =
    [
        "stop status readback",
        "stop reading status",
        "stop reading the status",
        "cancel status readback",
        "stop status speech",
        "silence status"
    ];

    private static readonly string[] UiClearRecentSpeechPrefixes =
    [
        "clear recent speech",
        "clear speech history",
        "clear transcript history",
        "clear what you heard",
        "forget recent speech",
        "forget speech history",
        "delete recent speech",
        "delete speech history"
    ];

    private static readonly string[] UiHideCommandPalettePrefixes =
    [
        "hide commands",
        "hide command palette",
        "close commands",
        "close command palette",
        "cancel commands",
        "cancel command palette",
        "dismiss commands",
        "dismiss command palette"
    ];

    private static readonly string[] UiHideUpdateSplashPrefixes =
    [
        "hide update splash",
        "close update splash",
        "cancel update splash",
        "dismiss update splash",
        "hide update",
        "close update",
        "dismiss update"
    ];

    private static readonly string[] UiHideImportSplashPrefixes =
    [
        "hide import splash",
        "close import splash",
        "cancel import splash",
        "dismiss import splash",
        "hide pack import splash",
        "close pack import splash",
        "dismiss pack import splash"
    ];

    private static readonly string[] UiReadUpdateSummaryAgainPrefixes =
    [
        "read update summary again",
        "repeat update summary",
        "read summary again",
        "repeat summary",
        "read update notes again"
    ];

    private static readonly string[] UiReadUpdatesStatusPrefixes =
    [
        "read updates status",
        "repeat updates status",
        "read update status",
        "repeat update status",
        "read the updates status",
        "repeat the updates status"
    ];

    private static readonly string[] UiReadCheckInStatusPrefixes =
    [
        "read check-in status",
        "read check in status",
        "repeat check-in status",
        "repeat check in status",
        "read phone-home status",
        "repeat phone-home status",
        "read phone home status",
        "repeat phone home status"
    ];

    private static readonly string[] UiReadEvidenceStatusPrefixes =
    [
        "read evidence status",
        "repeat evidence status",
        "read release evidence status",
        "repeat release evidence status",
        "read manual evidence status",
        "repeat manual evidence status",
        "read parity evidence status",
        "repeat parity evidence status"
    ];

    private static readonly string[] UiReadReleaseBlockersPrefixes =
    [
        "read release blockers",
        "repeat release blockers",
        "read parity blockers",
        "repeat parity blockers",
        "read blocker status",
        "repeat blocker status",
        "why is release blocked",
        "why is parity blocked",
        "why are we blocked",
        "what is blocking release",
        "what is blocking parity"
    ];

    private static readonly string[] UiReadNextProofPrefixes =
    [
        "read next proof",
        "repeat next proof",
        "read next evidence proof",
        "read next parity proof",
        "read next manual proof",
        "read next manual evidence",
        "read next missing proof",
        "what proof is next",
        "what evidence is next",
        "what manual proof is next"
    ];

    private static readonly string[] UiReadReleaseGatesPrefixes =
    [
        "read release gates",
        "repeat release gates",
        "read parity gates",
        "repeat parity gates",
        "read evidence gates",
        "repeat evidence gates",
        "what gates remain",
        "what release gates remain",
        "what parity gates remain",
        "which gates remain",
        "which release gates remain"
    ];

    private static readonly string[] UiReadNextProofInstructionsPrefixes =
    [
        "read proof steps",
        "repeat proof steps",
        "read next proof steps",
        "repeat next proof steps",
        "read proof instructions",
        "repeat proof instructions",
        "read next proof instructions",
        "repeat next proof instructions",
        "what are the proof steps",
        "what are the next proof steps",
        "how do i prove the next check",
        "how do I prove the next check"
    ];

    private static readonly string[] UiCreateNextProofNotePrefixes =
    [
        "create proof note",
        "create next proof note",
        "make proof note",
        "make next proof note",
        "create evidence note",
        "create next evidence note",
        "make evidence note",
        "make next evidence note",
        "open proof note",
        "open next proof note"
    ];

    private static readonly string[] UiReadVisualStatusPrefixes =
    [
        "read visual status",
        "repeat visual status",
        "read visual polish status",
        "repeat visual polish status",
        "read apple style status",
        "read mac os style status",
        "read macos style status",
        "read visual contract",
        "repeat visual contract",
        "read accessibility mode",
        "repeat accessibility mode",
        "read accessibility status",
        "repeat accessibility status",
        "read high contrast status",
        "repeat high contrast status",
        "read text scale status",
        "repeat text scale status",
        "read text scaling status",
        "repeat text scaling status",
        "read reduced motion status",
        "repeat reduced motion status"
    ];

    private static readonly string[] UiReadRestartProofPrefixes =
    [
        "restart proof",
        "read restart proof",
        "repeat restart proof",
        "read the restart proof",
        "repeat the restart proof"
    ];

    private static readonly string[] UiReadPlansStatusPrefixes =
    [
        "read plans status",
        "repeat plans status",
        "read tier status",
        "repeat tier status",
        "read pricing status",
        "repeat pricing status",
        "read paywall status",
        "repeat paywall status"
    ];

    private static readonly string[] UiReadImportSummaryAgainPrefixes =
    [
        "read import summary again",
        "repeat import summary",
        "read import again",
        "repeat import again",
        "read pack import summary again"
    ];

    private static readonly string[] UiReadWatchStatusPrefixes =
    [
        "read watch status",
        "read packs watch status",
        "read watched folder status",
        "repeat watch status",
        "repeat packs watch status",
        "repeat watched folder status"
    ];

    private static readonly string[] UiCommandsOnlyModePrefixes =
    [
        "commands only mode",
        "command only mode",
        "switch to commands only mode",
        "switch to command only mode",
        "turn on commands only mode",
        "turn on command only mode",
        "start command mode",
        "start commands mode",
        "voice commands only",
        "voice command mode",
        "command mode",
        "commands mode",
        "stop dictation mode",
        "turn off dictation mode"
    ];

    private static readonly string[] UiDictationOnlyModePrefixes =
    [
        "dictation only mode",
        "switch to dictation only mode",
        "turn on dictation only mode",
        "start dictation mode",
        "dictation mode",
        "voice dictation mode",
        "typing mode",
        "voice typing mode"
    ];

    private static readonly string[] UiDefaultVoiceModePrefixes =
    [
        "default mode",
        "switch to default mode",
        "turn on default mode",
        "voice default mode",
        "voice access default mode",
        "commands and dictation mode",
        "command and dictation mode",
        "command plus dictation mode",
        "commands plus dictation mode"
    ];

    private static readonly string[] UiShowTaskbarVisibleControlsPrefixes =
    [
        "show numbers on taskbar",
        "show numbers on the taskbar",
        "show control numbers on taskbar",
        "show control numbers on the taskbar",
        "show names on taskbar",
        "show names on the taskbar",
        "show labels on taskbar",
        "show labels on the taskbar",
        "show all labels on taskbar",
        "show all labels on the taskbar"
    ];

    private static readonly string[] UiShowVisibleControlsPrefixes =
    [
        "show numbers",
        "show numbers here",
        "show numbers everywhere",
        "show numbers for this window",
        "show numbers for current window",
        "show control numbers",
        "show all controls",
        "show names",
        "show labels",
        "show all labels",
        "number controls",
        "number the controls",
        "number buttons",
        "number clickable controls",
        "show visible controls",
        "show controls",
        "show buttons",
        "show clickable buttons",
        "show click targets",
        "show clickable targets",
        "show what i can click",
        "show clickable controls"
    ];

    private static readonly string[] UiHideVisibleControlsPrefixes =
    [
        "hide visible controls",
        "hide control numbers",
        "hide all controls",
        "hide numbers",
        "hide names",
        "hide labels",
        "hide all labels",
        "clear numbers",
        "clear control numbers",
        "remove numbers",
        "close visible controls",
        "close control numbers",
        "cancel visible controls",
        "cancel control numbers",
        "cancel numbers",
        "dismiss visible controls",
        "dismiss control numbers"
    ];

    private static readonly string[] UiShowKeyboardPrefixes =
    [
        "show keyboard",
        "show the keyboard",
        "open keyboard",
        "open the keyboard",
        "show on screen keyboard",
        "show onscreen keyboard",
        "open on screen keyboard",
        "open onscreen keyboard",
        "keyboard overlay"
    ];

    private static readonly string[] UiHideKeyboardPrefixes =
    [
        "hide keyboard",
        "hide the keyboard",
        "close keyboard",
        "close the keyboard",
        "hide on screen keyboard",
        "hide onscreen keyboard",
        "close on screen keyboard",
        "close onscreen keyboard",
        "cancel keyboard",
        "cancel the keyboard",
        "cancel on screen keyboard",
        "cancel onscreen keyboard",
        "dismiss keyboard",
        "dismiss the keyboard"
    ];

    private static readonly string[] UiShowMouseGridHerePrefixes =
    [
        "show grid here",
        "show mouse grid here",
        "mouse grid here"
    ];

    private static readonly string[] UiShowMouseGridPrefixes =
    [
        "show grid",
        "show grid everywhere",
        "show window grid",
        "show mouse grid everywhere",
        "show mouse window grid",
        "show mouse grid",
        "show mousegrid",
        "show numbered grid",
        "mouse grid",
        "mousegrid",
        "numbered grid",
        "open grid"
    ];

    private static readonly string[] UiHideMouseGridPrefixes =
    [
        "hide grid",
        "hide mouse grid",
        "close grid",
        "close mouse grid",
        "cancel grid",
        "cancel mouse grid",
        "dismiss grid"
    ];

    private static readonly string[] UiUndoMouseGridPrefixes =
    [
        "undo that",
        "undo grid",
        "undo mouse grid"
    ];

    private static readonly string[] UiMarkMouseGridPrefixes =
    [
        "mark"
    ];

    private static readonly string[] UiMarkMouseGridCellPrefixes =
    [
        "mark ",
        "mark cell ",
        "mark grid ",
        "mark mouse grid "
    ];

    private static readonly string[] UiDragMarkedMouseGridPrefixes =
    [
        "drag"
    ];

    private static readonly string[] UiSelectMouseGridCellPrefixes =
    [
        "grid ",
        "cell ",
        "choose cell ",
        "select cell ",
        "choose grid cell ",
        "select grid cell ",
        "square ",
        "choose square ",
        "select square ",
        "grid square ",
        "choose grid ",
        "select grid ",
        "mouse grid cell ",
        "mouse grid "
    ];

    private static readonly string[] UiClickMouseGridCellPrefixes =
    [
        "click grid ",
        "click cell ",
        "click grid cell ",
        "click square ",
        "click grid square ",
        "press grid ",
        "press cell ",
        "press grid cell ",
        "press square ",
        "tap grid ",
        "tap cell ",
        "tap grid cell ",
        "tap square ",
        "click mouse grid "
    ];

    private static readonly string[] UiDragMouseGridCellPrefixes =
    [
        "drag grid ",
        "drag mouse grid ",
        "drag from grid ",
        "drag cell ",
        "drag from cell ",
        "drag square ",
        "drag from square ",
        "drag grid cell ",
        "drag from grid cell ",
        "drag grid square ",
        "drag from grid square ",
        "move grid "
    ];

    private static readonly string[] UiNextControlPrefixes =
    [
        "next control",
        "next field",
        "next button",
        "next item",
        "next form field",
        "move to next field",
        "focus next field",
        "tab forward",
        "move forward",
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
        "previous form field",
        "move to previous field",
        "focus previous field",
        "tab backward",
        "tab back",
        "move backward",
        "go to previous control",
        "move to previous control",
        "focus previous control",
        "back control"
    ];

    private static readonly string[] UiActivateControlPrefixes =
    [
        "activate control",
        "click control",
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
        "focus on the ",
        "focus on ",
        "focus the ",
        "focus ",
        "click on the ",
        "click on ",
        "press on ",
        "tap on ",
        "choose the ",
        "select the ",
        "press ",
        "click ",
        "choose ",
        "select ",
        "activate ",
        "tap ",
    ];

    private static readonly string[] UiVisibleFieldLabelPrefixes =
    [
        "go to the field ",
        "go to field ",
        "move to the field ",
        "move to field ",
        "focus the field ",
        "focus field "
    ];

    private static readonly string[] UiVisibleFieldSuffixLabelPrefixes =
    [
        "go to the ",
        "go to ",
        "move to the ",
        "move to "
    ];

    private static readonly string[] UiVisibleControlDoubleClickLabelPrefixes =
    [
        "double click on the ",
        "double click on ",
        "double click the ",
        "double click ",
        "double tap the ",
        "double tap "
    ];

    private static readonly string[] UiVisibleControlTripleClickLabelPrefixes =
    [
        "triple click on the ",
        "triple click on ",
        "triple click the ",
        "triple click ",
        "triple tap the ",
        "triple tap "
    ];

    private static readonly string[] UiVisibleControlRightClickLabelPrefixes =
    [
        "right click on the ",
        "right click on ",
        "right click the ",
        "right click ",
        "context click the ",
        "context click "
    ];

    private static readonly string[] UiVisibleControlToggleLabelPrefixes =
    [
        "toggle on the ",
        "toggle the ",
        "toggle ",
        "flip on the ",
        "flip the ",
        "flip ",
        "check the ",
        "check ",
        "uncheck the ",
        "uncheck ",
        "turn on the ",
        "turn on ",
        "turn off the ",
        "turn off "
    ];

    private static readonly string[] UiVisibleControlExpandLabelPrefixes =
    [
        "expand on the ",
        "expand the ",
        "expand ",
        "open the menu ",
        "open menu ",
        "open the dropdown ",
        "open dropdown ",
        "show options for ",
        "show choices for "
    ];

    private static readonly string[] UiVisibleControlCollapseLabelPrefixes =
    [
        "collapse on the ",
        "collapse the ",
        "collapse ",
        "close the menu ",
        "close menu ",
        "close the dropdown ",
        "close dropdown ",
        "hide options for ",
        "hide choices for "
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
        "show voice tab",
        "open voice access settings",
        "show voice access settings"
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

    private static readonly string[] UiShortcutsPrefixes =
    [
        "open voice shortcuts",
        "show voice shortcuts",
        "go to voice shortcuts",
        "switch to voice shortcuts",
        "voice shortcuts tab",
        "open shortcuts",
        "show shortcuts",
        "shortcuts tab"
    ];

    private static readonly string[] UiPlansPrefixes =
    [
        "open plans",
        "show plans",
        "go to plans",
        "switch to plans",
        "paywall",
        "pay wall",
        "paid wall",
        "upgrade",
        "subscription",
        "plans tab",
        "open plans tab",
        "show plans tab"
    ];

    private static readonly string[] UiUpdatesPrefixes =
    [
        "open updates",
        "show updates",
        "go to updates",
        "switch to updates",
        "updates tab",
        "open updates tab",
        "show updates tab"
    ];

    private static readonly string[] UiCheckUpdatesPrefixes =
    [
        "check for updates now",
        "check updates now",
        "check for updates",
        "check updates",
        "update now",
        "force update check",
        "phone home now",
        "phone home for updates"
    ];

    private static readonly string[] UiOpenInstallerPrefixes =
    [
        "open installer",
        "show installer",
        "open current installer",
        "open update installer",
        "download installer"
    ];

    private static readonly string[] UiOpenReleaseProofPrefixes =
    [
        "open release proof",
        "show release proof",
        "release proof"
    ];

    private static readonly string[] UiReadReleaseProofPrefixes =
    [
        "read release proof",
        "repeat release proof",
        "read the release proof",
        "repeat the release proof"
    ];

    private static readonly string[] UiReadVoiceModeStatusPrefixes =
    [
        "read voice mode status",
        "repeat voice mode status",
        "read voice mode",
        "repeat voice mode"
    ];

    private static readonly string[] UiOpenReleaseEvidencePrefixes =
    [
        "open release evidence",
        "show release evidence",
        "release evidence",
        "open evidence",
        "show evidence"
    ];

    private static readonly string[] UiOpenProofNotesPrefixes =
    [
        "open proof notes",
        "show proof notes",
        "open manual proof notes",
        "show manual proof notes",
        "open evidence notes",
        "show evidence notes",
        "open manual evidence notes",
        "show manual evidence notes"
    ];

    private static readonly string[] UiReadProofNotesStatusPrefixes =
    [
        "read proof notes",
        "repeat proof notes",
        "read proof notes status",
        "repeat proof notes status",
        "read manual proof notes",
        "read evidence notes",
        "what proof notes exist",
        "how many proof notes exist"
    ];

    private static readonly string[] UiCreateAllProofNotesPrefixes =
    [
        "create all proof notes",
        "prepare all proof notes",
        "create all manual proof notes",
        "prepare all manual proof notes",
        "create all evidence notes",
        "prepare all evidence notes"
    ];

    private static readonly string[] UiCreateEvidenceDraftPrefixes =
    [
        "create evidence draft",
        "create manual evidence draft",
        "prefill evidence draft",
        "prefill manual evidence",
        "prefill manual evidence draft",
        "make evidence draft",
        "make manual evidence draft"
    ];

    private static readonly string[] UiOpenEvidenceDraftPrefixes =
    [
        "open evidence draft",
        "show evidence draft",
        "open manual evidence draft",
        "show manual evidence draft",
        "open draft evidence",
        "show draft evidence"
    ];

    private static readonly string[] UiReadEvidenceDraftPrefixes =
    [
        "read evidence draft",
        "repeat evidence draft",
        "read manual evidence draft",
        "repeat manual evidence draft",
        "read draft evidence",
        "what is in the evidence draft",
        "what remains in the evidence draft"
    ];

    private static readonly string[] UiOpenManualEvidencePrefixes =
    [
        "open manual evidence",
        "show manual evidence",
        "open manual evidence template",
        "manual evidence",
        "manual evidence template"
    ];

    private static readonly string[] UiOpenManualEvidenceChecklistPrefixes =
    [
        "open manual evidence checklist",
        "show manual evidence checklist",
        "open checklist",
        "manual evidence checklist",
        "checklist"
    ];

    private static readonly string[] UiReadEvidenceHeaderPrefixes =
    [
        "read evidence header",
        "repeat evidence header",
        "read manual evidence header",
        "repeat manual evidence header",
        "read documentation evidence header",
        "what header is missing",
        "what evidence header is missing"
    ];

    private static readonly string[] UiPacksPrefixes =
    [
        "open packs",
        "show packs",
        "go to packs",
        "switch to packs",
        "packs tab",
        "open packs tab",
        "show packs tab",
        "command packs",
        "manage packs"
    ];

    private static readonly string[] UiImportPackPrefixes =
    [
        "import extension pack",
        "import pack",
        "import dll",
        "add extension pack",
        "add dll",
        "install extension pack",
        "install dll",
        "drop dll",
        "drop extension pack",
        "drag and drop dll"
    ];

    private static readonly string[] UiImportPackFolderPrefixes =
    [
        "import extension folder",
        "import folder",
        "import dll folder",
        "add extension folder",
        "add dll folder",
        "install extension folder",
        "install dll folder",
        "drop dll folder",
        "drag and drop dll folder"
    ];

    private static readonly string[] UiUpdatePackPrefixes =
    [
        "update extension pack",
        "update pack",
        "reimport extension pack",
        "reimport pack"
    ];

    private static readonly string[] UiRollbackPackPrefixes =
    [
        "rollback extension pack",
        "rollback pack",
        "restore extension pack",
        "restore pack"
    ];

    private static readonly string[] UiRefreshPacksPrefixes =
    [
        "refresh packs",
        "reload packs"
    ];

    private static readonly string[] UiOpenPacksFolderPrefixes =
    [
        "open packs folder",
        "show packs folder"
    ];

    private static readonly string[] UiEnableSelectedPackPrefixes =
    [
        "enable selected pack",
        "turn on selected pack"
    ];

    private static readonly string[] UiDisableSelectedPackPrefixes =
    [
        "disable selected pack",
        "turn off selected pack"
    ];

    private static readonly string[] UiRemoveSelectedPackPrefixes =
    [
        "remove selected pack",
        "delete selected pack"
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
        "files",
        "open files",
        "show files",
        "go to files",
        "switch to files",
        "file search",
        "open file search",
        "show file search",
        "go to file search",
        "switch to file search",
        "files tab",
        "file search tab",
        "open files tab",
        "show files tab",
        "open file search tab",
        "show file search tab"
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
        "previous page",
        "show previous tab"
    ];

    private const string UiActionActivateLabelPrefix = "ui-activate-label:";
    private const string UiActionDoubleClickLabelPrefix = "ui-double-click-label:";
    private const string UiActionTripleClickLabelPrefix = "ui-triple-click-label:";
    private const string UiActionRightClickLabelPrefix = "ui-right-click-label:";
    private const string UiActionToggleLabelPrefix = "ui-toggle-label:";
    private const string UiActionExpandLabelPrefix = "ui-expand-label:";
    private const string UiActionCollapseLabelPrefix = "ui-collapse-label:";
    private const string UiActionMoveSliderPrefix = "ui-move-slider:";
    public const string DictationInsertTextActionPrefix = "dictation-insert-text:";

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
        "radio buttons",
        "radio button",
        "menu items",
        "menu item",
        "combo boxes",
        "combo box",
        "text boxes",
        "text box",
        "textboxes",
        "textbox",
        "edit boxes",
        "edit box",
        "editbox",
        "list items",
        "list item",
        "tree items",
        "tree item",
        "split buttons",
        "split button",
        "group boxes",
        "group box",
        "list boxes",
        "list box",
        "scroll bars",
        "scroll bar",
        "toggle button",
        "checkboxes",
        "checkbox",
        "drop down",
        "dropdown",
        "button",
        "control",
        "field",
        "item",
        "tab",
        "page",
        "link",
        "edit",
        "slider",
        "sliders",
        "cells",
        "cell",
        "headings",
        "heading",
        "groups",
        "group",
        "scrollbar",
        "toggle",
        "options",
        "option",
        "rows",
        "row",
        "panes",
        "pane",
        "listbox"
    ];

    private static readonly string[] VisibleFieldLabelSuffixes =
    [
        "field",
        "form field",
        "text box",
        "textbox",
        "edit box",
        "editbox",
        "combo box",
        "combo",
        "drop down",
        "dropdown"
    ];
}
