using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Callsign.UI.Services;

public sealed record VisibleWindowSwitchCandidate(
    nint Handle,
    string Title,
    string ProcessName,
    bool IsMinimized,
    int Score,
    string MatchKind)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Title) ? ProcessName : Title;
}

public sealed record VisibleWindowSwitchResolution(
    string RequestedName,
    string NormalizedName,
    bool IsResolved,
    bool IsAmbiguous,
    VisibleWindowSwitchCandidate? SelectedCandidate,
    IReadOnlyList<VisibleWindowSwitchCandidate> Candidates,
    string Message);

public sealed class SystemControlService
{
    private readonly bool _dryRun;
    private readonly object _mouseMotionSync = new();
    private System.Threading.Timer? _mouseMotionTimer;
    private int _mouseMotionSpeedIndex = DefaultMouseMotionSpeedIndex;
    private MouseDirection _activeMouseMotionDirection;
    private bool _mouseMotionActive;

    public SystemControlService(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public bool TryExecute(string action, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(action))
        {
            message = "System action was empty.";
            return false;
        }

        try
        {
            var normalizedAction = action.Trim().ToLowerInvariant();
            if (TryExecuteRepeatedAction(normalizedAction, out message))
                return true;

            if (TryParseFunctionKeyAction(normalizedAction, out var functionKey, out var functionKeyName))
            {
                SendKey(functionKey);
                message = $"{functionKeyName} requested.";
                return true;
            }

            if (TryParseDigitKeyAction(normalizedAction, out var digitKey, out var digitKeyName))
            {
                SendKey(digitKey);
                message = $"{digitKeyName} requested.";
                return true;
            }

            if (TryParseLetterKeyAction(normalizedAction, out var letterKey, out var letterKeyName))
            {
                SendKey(letterKey);
                message = $"{letterKeyName} requested.";
                return true;
            }

            if (TryParseSymbolKeyAction(normalizedAction, out var symbolKey, out var symbolRequiresShift, out var symbolKeyName))
            {
                if (symbolRequiresShift)
                    SendKeyChord(VK_SHIFT, symbolKey);
                else
                    SendKey(symbolKey);

                message = $"{symbolKeyName} requested.";
                return true;
            }

            if (TryExecuteModifierChordAction(normalizedAction, out var chordName))
            {
                message = $"{chordName} requested.";
                return true;
            }

            if (TryExecuteHeldModifierAction(normalizedAction, out message))
                return true;

            if (TryExecuteParameterizedMouseAction(normalizedAction, out message))
                return true;

            switch (normalizedAction)
            {
                case "system-volume-up":
                    SendVolumeKey(VK_VOLUME_UP);
                    message = "Volume up requested.";
                    return true;
                case "system-volume-down":
                    SendVolumeKey(VK_VOLUME_DOWN);
                    message = "Volume down requested.";
                    return true;
                case "system-volume-mute":
                    SendVolumeKey(VK_VOLUME_MUTE);
                    message = "Volume mute requested.";
                    return true;
                case "system-media-play-pause":
                    SendMediaKey(VK_MEDIA_PLAY_PAUSE);
                    message = "Media play/pause requested.";
                    return true;
                case "system-media-next-track":
                    SendMediaKey(VK_MEDIA_NEXT_TRACK);
                    message = "Media next track requested.";
                    return true;
                case "system-media-previous-track":
                    SendMediaKey(VK_MEDIA_PREV_TRACK);
                    message = "Media previous track requested.";
                    return true;
                case "system-media-stop":
                    SendMediaKey(VK_MEDIA_STOP);
                    message = "Media stop requested.";
                    return true;
                case "system-show-desktop":
                    SendDesktopShortcut();
                    message = "Show desktop requested.";
                    return true;
                case "system-next-window":
                    SendAltTab();
                    message = "Next window requested.";
                    return true;
                case "system-previous-window":
                    SendAltShiftTab();
                    message = "Previous window requested.";
                    return true;
                case "system-open-task-view":
                    SendWindowsKey(VK_TAB);
                    message = "Task view requested.";
                    return true;
                case "system-open-quick-settings":
                    SendWindowsKey(VK_A);
                    message = "Quick Settings requested.";
                    return true;
                case "system-open-notification-center":
                    SendWindowsKey(VK_N);
                    message = "Notification Center requested.";
                    return true;
                case "system-open-emoji-panel":
                    SendWindowsKey(VK_OEM_PERIOD);
                    message = "Emoji panel requested.";
                    return true;
                case "system-open-clipboard-history":
                    SendWindowsKey(VK_V);
                    message = "Clipboard history requested.";
                    return true;
                case "system-open-snipping-toolbar":
                    SendTwoModifierKeyChord(VirtualKeyLeftWindows, VK_SHIFT, VK_S);
                    message = "Snipping toolbar requested.";
                    return true;
                case "system-open-project-display":
                    SendWindowsKey(VK_P);
                    message = "Project display requested.";
                    return true;
                case "system-open-cast-display":
                    SendWindowsKey(VK_K);
                    message = "Cast display requested.";
                    return true;
                case "system-new-virtual-desktop":
                    SendCtrlWindowsKey(VirtualKeyD);
                    message = "New virtual desktop requested.";
                    return true;
                case "system-next-virtual-desktop":
                    SendCtrlWindowsKey(VK_RIGHT);
                    message = "Next virtual desktop requested.";
                    return true;
                case "system-previous-virtual-desktop":
                    SendCtrlWindowsKey(VK_LEFT);
                    message = "Previous virtual desktop requested.";
                    return true;
                case "system-open-task-manager":
                    SendTaskManagerShortcut();
                    message = "Task Manager requested.";
                    return true;
                case "system-open-settings":
                    OpenSettingsUri("ms-settings:", "Windows Settings");
                    message = "Windows Settings requested.";
                    return true;
                case "system-open-display-settings":
                    OpenSettingsUri("ms-settings:display", "Display settings");
                    message = "Display settings requested.";
                    return true;
                case "system-open-sound-settings":
                    OpenSettingsUri("ms-settings:sound", "Sound settings");
                    message = "Sound settings requested.";
                    return true;
                case "system-open-bluetooth-settings":
                    OpenSettingsUri("ms-settings:bluetooth", "Bluetooth settings");
                    message = "Bluetooth settings requested.";
                    return true;
                case "system-open-wifi-settings":
                    OpenSettingsUri("ms-settings:network-wifi", "Wi-Fi settings");
                    message = "Wi-Fi settings requested.";
                    return true;
                case "system-open-network-settings":
                    OpenSettingsUri("ms-settings:network", "Network settings");
                    message = "Network settings requested.";
                    return true;
                case "system-open-accessibility-settings":
                    OpenSettingsUri("ms-settings:easeofaccess", "Accessibility settings");
                    message = "Accessibility settings requested.";
                    return true;
                case "system-open-magnifier-settings":
                    OpenSettingsUri("ms-settings:easeofaccess-magnifier", "Magnifier settings");
                    message = "Magnifier settings requested.";
                    return true;
                case "system-open-narrator-settings":
                    OpenSettingsUri("ms-settings:easeofaccess-narrator", "Narrator settings");
                    message = "Narrator settings requested.";
                    return true;
                case "system-open-captions-settings":
                    OpenSettingsUri("ms-settings:easeofaccess-closedcaptioning", "Captions settings");
                    message = "Captions settings requested.";
                    return true;
                case "system-open-speech-settings":
                    OpenSettingsUri("ms-settings:speech", "Speech settings");
                    message = "Speech settings requested.";
                    return true;
                case "system-open-magnifier":
                    SendWindowsKey(VK_OEM_PLUS);
                    message = "Magnifier requested.";
                    return true;
                case "system-magnifier-zoom-out":
                    SendWindowsKey(VK_OEM_MINUS);
                    message = "Magnifier zoom out requested.";
                    return true;
                case "system-close-magnifier":
                    SendWindowsKey(VK_ESCAPE);
                    message = "Close magnifier requested.";
                    return true;
                case "system-open-mouse-settings":
                    OpenSettingsUri("ms-settings:mousetouchpad", "Mouse settings");
                    message = "Mouse settings requested.";
                    return true;
                case "system-open-keyboard-settings":
                    OpenSettingsUri("ms-settings:keyboard", "Keyboard settings");
                    message = "Keyboard settings requested.";
                    return true;
                case "system-open-privacy-settings":
                    OpenSettingsUri("ms-settings:privacy", "Privacy settings");
                    message = "Privacy settings requested.";
                    return true;
                case "system-open-power-settings":
                    OpenSettingsUri("ms-settings:powersleep", "Power settings");
                    message = "Power settings requested.";
                    return true;
                case "system-open-apps-settings":
                    OpenSettingsUri("ms-settings:appsfeatures", "Apps settings");
                    message = "Apps settings requested.";
                    return true;
                case "system-open-default-apps-settings":
                    OpenSettingsUri("ms-settings:defaultapps", "Default apps settings");
                    message = "Default apps settings requested.";
                    return true;
                case "system-open-date-time-settings":
                    OpenSettingsUri("ms-settings:dateandtime", "Date and time settings");
                    message = "Date and time settings requested.";
                    return true;
                case "system-open-notifications-settings":
                    OpenSettingsUri("ms-settings:notifications", "Notifications settings");
                    message = "Notifications settings requested.";
                    return true;
                case "system-open-windows-update-settings":
                    OpenSettingsUri("ms-settings:windowsupdate", "Windows Update settings");
                    message = "Windows Update settings requested.";
                    return true;
                case "system-open-personalization-settings":
                    OpenSettingsUri("ms-settings:personalization", "Personalization settings");
                    message = "Personalization settings requested.";
                    return true;
                case "system-minimize-window":
                    ShowForegroundWindow(SW_MINIMIZE);
                    message = "Minimize window requested.";
                    return true;
                case "system-maximize-window":
                    ShowForegroundWindow(SW_MAXIMIZE);
                    message = "Maximize window requested.";
                    return true;
                case "system-restore-window":
                    ShowForegroundWindow(SW_RESTORE);
                    message = "Restore window requested.";
                    return true;
                case "system-snap-window-left":
                    SendWindowsKey(VK_LEFT);
                    message = "Snap window left requested.";
                    return true;
                case "system-snap-window-right":
                    SendWindowsKey(VK_RIGHT);
                    message = "Snap window right requested.";
                    return true;
                case "system-snap-window-up":
                    SendWindowsKey(VK_UP);
                    message = "Snap window up requested.";
                    return true;
                case "system-snap-window-down":
                    SendWindowsKey(VK_DOWN);
                    message = "Snap window down requested.";
                    return true;
                case "system-show-snap-layouts":
                    SendWindowsKey(VK_Z);
                    message = "Snap layouts requested.";
                    return true;
                case "system-press-enter":
                    SendKey(VK_RETURN);
                    message = "Enter requested.";
                    return true;
                case "system-press-tab":
                    SendKey(VK_TAB);
                    message = "Tab requested.";
                    return true;
                case "system-press-escape":
                    SendKey(VK_ESCAPE);
                    message = "Escape requested.";
                    return true;
                case "system-press-backspace":
                    SendKey(VK_BACK);
                    message = "Backspace requested.";
                    return true;
                case "system-press-space":
                    SendKey(VK_SPACE);
                    message = "Space requested.";
                    return true;
                case "system-press-delete":
                    SendKey(VK_DELETE);
                    message = "Delete requested.";
                    return true;
                case "system-press-insert":
                    SendKey(VK_INSERT);
                    message = "Insert requested.";
                    return true;
                case "system-press-windows":
                    SendKey(VirtualKeyLeftWindows);
                    message = "Windows key requested.";
                    return true;
                case "system-press-context-menu":
                    SendKey(VK_APPS);
                    message = "Context menu key requested.";
                    return true;
                case "system-press-caps-lock":
                    SendKey(VK_CAPITAL);
                    message = "Caps Lock requested.";
                    return true;
                case "system-press-up":
                    SendKey(VK_UP);
                    message = "Up arrow requested.";
                    return true;
                case "system-press-down":
                    SendKey(VK_DOWN);
                    message = "Down arrow requested.";
                    return true;
                case "system-press-left":
                    SendKey(VK_LEFT);
                    message = "Left arrow requested.";
                    return true;
                case "system-press-right":
                    SendKey(VK_RIGHT);
                    message = "Right arrow requested.";
                    return true;
                case "system-press-home":
                    SendKey(VK_HOME);
                    message = "Home requested.";
                    return true;
                case "system-press-end":
                    SendKey(VK_END);
                    message = "End requested.";
                    return true;
                case "system-page-up":
                    SendKey(VK_PRIOR);
                    message = "Page up requested.";
                    return true;
                case "system-page-down":
                    SendKey(VK_NEXT);
                    message = "Page down requested.";
                    return true;
                case "system-mouse-click":
                    StopContinuousMouseMove();
                    SendMouseClick();
                    message = "Mouse click requested.";
                    return true;
                case "system-mouse-double-click":
                    StopContinuousMouseMove();
                    SendMouseDoubleClick();
                    message = "Mouse double-click requested.";
                    return true;
                case "system-mouse-triple-click":
                    StopContinuousMouseMove();
                    SendMouseTripleClick();
                    message = "Mouse triple-click requested.";
                    return true;
                case "system-mouse-right-click":
                    StopContinuousMouseMove();
                    SendMouseRightClick();
                    message = "Mouse right-click requested.";
                    return true;
                case "system-mouse-button-down":
                    StopContinuousMouseMove();
                    SendMouseButtonDown();
                    message = "Mouse button down requested.";
                    return true;
                case "system-mouse-button-up":
                    StopContinuousMouseMove();
                    SendMouseButtonUp();
                    message = "Mouse button up requested.";
                    return true;
                case "system-mouse-scroll-up":
                    StopContinuousMouseMove();
                    SendMouseWheel(MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll up requested.";
                    return true;
                case "system-mouse-scroll-down":
                    StopContinuousMouseMove();
                    SendMouseWheel(-MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll down requested.";
                    return true;
                case "system-mouse-scroll-left":
                    StopContinuousMouseMove();
                    SendMouseHorizontalWheel(-MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll left requested.";
                    return true;
                case "system-mouse-scroll-right":
                    StopContinuousMouseMove();
                    SendMouseHorizontalWheel(MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll right requested.";
                    return true;
                case "system-mouse-move-up":
                    StopContinuousMouseMove();
                    SendMouseMove(0, -MOUSE_NUDGE_PIXELS);
                    message = "Mouse move up requested.";
                    return true;
                case "system-mouse-move-down":
                    StopContinuousMouseMove();
                    SendMouseMove(0, MOUSE_NUDGE_PIXELS);
                    message = "Mouse move down requested.";
                    return true;
                case "system-mouse-move-left":
                    StopContinuousMouseMove();
                    SendMouseMove(-MOUSE_NUDGE_PIXELS, 0);
                    message = "Mouse move left requested.";
                    return true;
                case "system-mouse-move-right":
                    StopContinuousMouseMove();
                    SendMouseMove(MOUSE_NUDGE_PIXELS, 0);
                    message = "Mouse move right requested.";
                    return true;
                case "system-mouse-drag-up":
                    StopContinuousMouseMove();
                    SendMouseDrag(0, -MOUSE_NUDGE_PIXELS);
                    message = "Mouse drag up requested.";
                    return true;
                case "system-mouse-drag-down":
                    StopContinuousMouseMove();
                    SendMouseDrag(0, MOUSE_NUDGE_PIXELS);
                    message = "Mouse drag down requested.";
                    return true;
                case "system-mouse-drag-left":
                    StopContinuousMouseMove();
                    SendMouseDrag(-MOUSE_NUDGE_PIXELS, 0);
                    message = "Mouse drag left requested.";
                    return true;
                case "system-mouse-drag-right":
                    StopContinuousMouseMove();
                    SendMouseDrag(MOUSE_NUDGE_PIXELS, 0);
                    message = "Mouse drag right requested.";
                    return true;
                case "system-copy":
                    SendKeyChord(VK_CONTROL, VK_C);
                    message = "Copy requested.";
                    return true;
                case "system-paste":
                    SendKeyChord(VK_CONTROL, VK_V);
                    message = "Paste requested.";
                    return true;
                case "system-cut":
                    SendKeyChord(VK_CONTROL, VK_X);
                    message = "Cut requested.";
                    return true;
                case "system-select-all":
                    SendKeyChord(VK_CONTROL, VK_A);
                    message = "Select all requested.";
                    return true;
                case "system-save":
                    SendKeyChord(VK_CONTROL, VK_S);
                    message = "Save requested.";
                    return true;
                case "system-undo":
                    SendKeyChord(VK_CONTROL, VK_Z);
                    message = "Undo requested.";
                    return true;
                case "system-redo":
                    SendKeyChord(VK_CONTROL, VK_Y);
                    message = "Redo requested.";
                    return true;
                case "system-bold":
                    SendKeyChord(VK_CONTROL, VK_B);
                    message = "Bold requested.";
                    return true;
                case "system-italic":
                    SendKeyChord(VK_CONTROL, VK_I);
                    message = "Italic requested.";
                    return true;
                case "system-underline":
                    SendKeyChord(VK_CONTROL, VK_U);
                    message = "Underline requested.";
                    return true;
                case "system-find":
                    SendKeyChord(VK_CONTROL, VK_F);
                    message = "Find requested.";
                    return true;
                case "system-new-window":
                    SendKeyChord(VK_CONTROL, VK_N);
                    message = "New window requested.";
                    return true;
                case "system-new-document":
                    SendKeyChord(VK_CONTROL, VK_N);
                    message = "New document requested.";
                    return true;
                case "system-open-file":
                    SendKeyChord(VK_CONTROL, VK_O);
                    message = "Open file dialog requested.";
                    return true;
                case "system-print":
                    SendKeyChord(VK_CONTROL, VK_P);
                    message = "Print dialog requested.";
                    return true;
                case "system-zoom-in":
                    SendKeyChord(VK_CONTROL, VK_OEM_PLUS);
                    message = "Zoom in requested.";
                    return true;
                case "system-zoom-out":
                    SendKeyChord(VK_CONTROL, VK_OEM_MINUS);
                    message = "Zoom out requested.";
                    return true;
                case "system-zoom-reset":
                    SendKeyChord(VK_CONTROL, VK_0);
                    message = "Zoom reset requested.";
                    return true;
                case "system-close-window":
                    SendAltKey(VK_F4);
                    message = "Close window requested.";
                    return true;
                case "system-move-previous-character":
                    SendKey(VK_LEFT);
                    message = "Move previous character requested.";
                    return true;
                case "system-move-next-character":
                    SendKey(VK_RIGHT);
                    message = "Move next character requested.";
                    return true;
                case "system-select-previous-character":
                    SendKeyChord(VK_SHIFT, VK_LEFT);
                    message = "Select previous character requested.";
                    return true;
                case "system-select-next-character":
                    SendKeyChord(VK_SHIFT, VK_RIGHT);
                    message = "Select next character requested.";
                    return true;
                case "system-delete-previous-character":
                    SendKey(VK_BACK);
                    message = "Delete previous character requested.";
                    return true;
                case "system-delete-next-character":
                    SendKey(VK_DELETE);
                    message = "Delete next character requested.";
                    return true;
                case "system-move-line-start":
                    SendKey(VK_HOME);
                    message = "Move to line start requested.";
                    return true;
                case "system-move-line-end":
                    SendKey(VK_END);
                    message = "Move to line end requested.";
                    return true;
                case "system-move-previous-line":
                    SendKey(VK_UP);
                    message = "Move previous line requested.";
                    return true;
                case "system-move-next-line":
                    SendKey(VK_DOWN);
                    message = "Move next line requested.";
                    return true;
                case "system-select-to-line-start":
                    SendKeyChord(VK_SHIFT, VK_HOME);
                    message = "Select to line start requested.";
                    return true;
                case "system-select-to-line-end":
                    SendKeyChord(VK_SHIFT, VK_END);
                    message = "Select to line end requested.";
                    return true;
                case "system-select-previous-line":
                    SendKeyChord(VK_SHIFT, VK_UP);
                    message = "Select previous line requested.";
                    return true;
                case "system-select-next-line":
                    SendKeyChord(VK_SHIFT, VK_DOWN);
                    message = "Select next line requested.";
                    return true;
                case "system-delete-to-line-start":
                    SendKeyChord(VK_SHIFT, VK_HOME);
                    SendKey(VK_BACK);
                    message = "Delete to line start requested.";
                    return true;
                case "system-delete-to-line-end":
                    SendKeyChord(VK_SHIFT, VK_END);
                    SendKey(VK_DELETE);
                    message = "Delete to line end requested.";
                    return true;
                case "system-delete-previous-line":
                    SendKeyChord(VK_SHIFT, VK_UP);
                    SendKey(VK_BACK);
                    message = "Delete previous line requested.";
                    return true;
                case "system-delete-next-line":
                    SendKeyChord(VK_SHIFT, VK_DOWN);
                    SendKey(VK_DELETE);
                    message = "Delete next line requested.";
                    return true;
                case "system-move-previous-word":
                    SendCtrlArrow(VK_LEFT);
                    message = "Move previous word requested.";
                    return true;
                case "system-move-next-word":
                    SendCtrlArrow(VK_RIGHT);
                    message = "Move next word requested.";
                    return true;
                case "system-select-previous-word":
                    SendCtrlShiftArrow(VK_LEFT);
                    message = "Select previous word requested.";
                    return true;
                case "system-select-next-word":
                    SendCtrlShiftArrow(VK_RIGHT);
                    message = "Select next word requested.";
                    return true;
                case "system-delete-previous-word":
                    SendKeyChord(VK_CONTROL, VK_BACK);
                    message = "Delete previous word requested.";
                    return true;
                case "system-delete-next-word":
                    SendCtrlDelete();
                    message = "Delete next word requested.";
                    return true;
                case "system-move-previous-sentence":
                    SendCtrlArrow(VK_UP);
                    message = "Move previous sentence requested.";
                    return true;
                case "system-move-next-sentence":
                    SendCtrlArrow(VK_DOWN);
                    message = "Move next sentence requested.";
                    return true;
                case "system-select-previous-sentence":
                    SendCtrlShiftArrow(VK_UP);
                    message = "Select previous sentence requested.";
                    return true;
                case "system-select-next-sentence":
                    SendCtrlShiftArrow(VK_DOWN);
                    message = "Select next sentence requested.";
                    return true;
                case "system-delete-previous-sentence":
                    SendKeyChord(VK_CONTROL, VK_BACK);
                    message = "Delete previous sentence requested.";
                    return true;
                case "system-delete-next-sentence":
                    SendKeyChord(VK_CONTROL, VK_DELETE);
                    message = "Delete next sentence requested.";
                    return true;
                case "system-move-paragraph-start":
                    SendAltKey(VK_UP);
                    message = "Move to paragraph start requested.";
                    return true;
                case "system-move-paragraph-end":
                    SendAltKey(VK_DOWN);
                    message = "Move to paragraph end requested.";
                    return true;
                case "system-select-to-paragraph-start":
                    SendAltShiftKey(VK_UP);
                    message = "Select to paragraph start requested.";
                    return true;
                case "system-select-to-paragraph-end":
                    SendAltShiftKey(VK_DOWN);
                    message = "Select to paragraph end requested.";
                    return true;
                case "system-delete-to-paragraph-start":
                    SendAltShiftKey(VK_UP);
                    SendKey(VK_BACK);
                    message = "Delete to paragraph start requested.";
                    return true;
                case "system-delete-to-paragraph-end":
                    SendAltShiftKey(VK_DOWN);
                    SendKey(VK_DELETE);
                    message = "Delete to paragraph end requested.";
                    return true;
                case "system-move-previous-paragraph":
                    SendAltKey(VK_UP);
                    message = "Move previous paragraph requested.";
                    return true;
                case "system-move-next-paragraph":
                    SendAltKey(VK_DOWN);
                    message = "Move next paragraph requested.";
                    return true;
                case "system-select-previous-paragraph":
                    SendAltShiftKey(VK_UP);
                    message = "Select previous paragraph requested.";
                    return true;
                case "system-select-next-paragraph":
                    SendAltShiftKey(VK_DOWN);
                    message = "Select next paragraph requested.";
                    return true;
                case "system-delete-previous-paragraph":
                    SendAltBackspace();
                    message = "Delete previous paragraph requested.";
                    return true;
                case "system-delete-next-paragraph":
                    SendAltDelete();
                    message = "Delete next paragraph requested.";
                    return true;
                default:
                    message = $"Unknown system action: {action}";
                    return false;
            }
        }
        catch (Exception ex)
        {
            message = $"Unable to execute system action: {ex.Message}";
            return false;
        }
    }

    public VisibleWindowSwitchResolution ResolveVisibleWindow(string requestedWindow, int maxCandidates = 5, int ignoredProcessId = 0)
    {
        var requestedName = requestedWindow?.Trim() ?? string.Empty;
        var normalizedRequest = NormalizeWindowName(requestedName);
        if (string.IsNullOrWhiteSpace(normalizedRequest))
        {
            return new VisibleWindowSwitchResolution(
                requestedName,
                normalizedRequest,
                IsResolved: false,
                IsAmbiguous: false,
                SelectedCandidate: null,
                Candidates: [],
                Message: "Say the app or window name you want to switch to.");
        }

        var candidates = EnumerateVisibleWindowCandidates(normalizedRequest, ignoredProcessId, maxCandidates);
        if (candidates.Count == 0)
        {
            return new VisibleWindowSwitchResolution(
                requestedName,
                normalizedRequest,
                IsResolved: false,
                IsAmbiguous: false,
                SelectedCandidate: null,
                Candidates: [],
                Message: $"No open app or window matched '{requestedName}'.");
        }

        var top = candidates[0];
        var tied = candidates
            .Where(candidate => candidate.Score == top.Score)
            .Take(Math.Max(1, maxCandidates))
            .ToArray();
        if (tied.Length > 1)
        {
            return new VisibleWindowSwitchResolution(
                requestedName,
                normalizedRequest,
                IsResolved: false,
                IsAmbiguous: true,
                SelectedCandidate: null,
                Candidates: tied,
                Message: $"Multiple open windows match '{requestedName}'. Choose one before Callsign switches focus.");
        }

        return new VisibleWindowSwitchResolution(
            requestedName,
            normalizedRequest,
            IsResolved: true,
            IsAmbiguous: false,
            SelectedCandidate: top,
            Candidates: [top],
            Message: $"Resolved '{requestedName}' to '{top.DisplayName}'.");
    }

    public bool TryActivateVisibleWindow(nint handle, out string message)
    {
        message = string.Empty;
        if (handle == nint.Zero)
        {
            message = "No visible window was selected.";
            return false;
        }

        if (_dryRun)
        {
            message = "Window switch requested.";
            return true;
        }

        if (IsIconic(handle))
            ShowWindow(handle, SW_RESTORE);

        if (!SetForegroundWindow(handle))
        {
            message = "Windows would not bring the requested app to the foreground.";
            return false;
        }

        var title = ReadWindowText(handle);
        message = string.IsNullOrWhiteSpace(title)
            ? "Window switch requested."
            : $"Switched to '{title}'.";
        return true;
    }

    public static bool TryParseVisibleWindowSelectionNumber(string transcript, out int candidateNumber)
    {
        candidateNumber = 0;
        var normalized = NormalizeWindowName(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (TryParseChoiceNumber(normalized, out candidateNumber))
            return true;

        var candidateText = normalized;
        foreach (var prefix in new[]
                 {
                     "click ",
                     "tap ",
                     "open ",
                     "switch to ",
                     "go to ",
                     "use ",
                     "pick ",
                     "choose ",
                     "select "
                 })
        {
            if (!candidateText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            candidateText = candidateText[prefix.Length..].Trim();
            break;
        }

        foreach (var marker in new[]
                 {
                     "window ",
                     "choice ",
                     "option ",
                     "result ",
                     "number "
                 })
        {
            if (!candidateText.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                continue;

            candidateText = candidateText[marker.Length..].Trim();
            break;
        }

        return TryParseChoiceNumber(candidateText, out candidateNumber);
    }

    public static bool IsConfirmVisibleWindowSelectionCommand(string transcript)
    {
        var normalized = NormalizeWindowName(transcript);
        return normalized is "confirm window"
            or "confirm choice"
            or "confirm result"
            or "confirm selection"
            or "open selected window"
            or "switch to selected window"
            or "go to selected window";
    }

    public static bool IsClearVisibleWindowSelectionCommand(string transcript)
    {
        var normalized = NormalizeWindowName(transcript);
        return normalized is "cancel"
            or "clear window choices"
            or "clear window choice"
            or "clear choices"
            or "dismiss window choices"
            or "cancel window choices"
            or "hide window choices"
            or "close window choices";
    }

    public static bool IsNextVisibleWindowSelectionCommand(string transcript)
    {
        var normalized = NormalizeWindowName(transcript);
        return normalized is "next window choice"
            or "next choice"
            or "next window"
            or "next result"
            or "move to next window choice";
    }

    public static bool IsPreviousVisibleWindowSelectionCommand(string transcript)
    {
        var normalized = NormalizeWindowName(transcript);
        return normalized is "previous window choice"
            or "previous choice"
            or "previous result"
            or "move to previous window choice"
            or "last window choice";
    }

    private bool TryExecuteRepeatedAction(string action, out string message)
    {
        const string prefix = "system-repeat:";
        message = string.Empty;
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var payload = action[prefix.Length..].Trim();
        var separatorIndex = payload.LastIndexOf(':');
        if (separatorIndex <= 0)
        {
            message = "Repeated system action was malformed.";
            return false;
        }

        var repeatedAction = payload[..separatorIndex].Trim();
        var countText = payload[(separatorIndex + 1)..].Trim();
        if (!int.TryParse(countText, out var count) || count is < 2 or > 20)
        {
            message = "Repeated system action count was invalid.";
            return false;
        }

        string? lastMessage = null;
        for (var index = 0; index < count; index++)
        {
            if (!TryExecute(repeatedAction, out lastMessage))
            {
                message = lastMessage ?? "Repeated system action failed.";
                return false;
            }
        }

        var repeatedDisplay = FormatRepeatedActionDisplay(lastMessage, repeatedAction);
        message = $"{repeatedDisplay} {count} times requested.";
        return true;
    }

    private static string FormatRepeatedActionDisplay(string? lastMessage, string repeatedAction)
    {
        if (!string.IsNullOrWhiteSpace(lastMessage))
        {
            var trimmedMessage = lastMessage.Trim();
            if (trimmedMessage.EndsWith(" requested.", StringComparison.OrdinalIgnoreCase))
                return trimmedMessage[..^" requested.".Length];
        }

        if (repeatedAction.StartsWith("system-", StringComparison.OrdinalIgnoreCase))
            return repeatedAction["system-".Length..].Replace('-', ' ');

        return repeatedAction.Replace('-', ' ');
    }

    private bool TryExecuteParameterizedMouseAction(string action, out string message)
    {
        if (string.Equals(action, "system-mouse-stop-moving", StringComparison.OrdinalIgnoreCase))
        {
            StopContinuousMouseMove();
            message = "Mouse stop moving requested.";
            return true;
        }

        if (string.Equals(action, "system-mouse-move-faster", StringComparison.OrdinalIgnoreCase))
        {
            AdjustContinuousMouseMoveSpeed(1);
            message = "Mouse move faster requested.";
            return true;
        }

        if (string.Equals(action, "system-mouse-move-slower", StringComparison.OrdinalIgnoreCase))
        {
            AdjustContinuousMouseMoveSpeed(-1);
            message = "Mouse move slower requested.";
            return true;
        }

        if (TryParseMouseDirectionAction(action, "system-mouse-start-moving:", out var direction, out _))
        {
            StartContinuousMouseMove(direction);
            message = $"Mouse move {direction.DisplayName} requested.";
            return true;
        }

        if (TryParseMouseDirectionAction(action, "system-mouse-drag-direction:", out direction, out _))
        {
            StopContinuousMouseMove();
            SendMouseDrag(direction.DeltaX * MOUSE_NUDGE_PIXELS, direction.DeltaY * MOUSE_NUDGE_PIXELS);
            message = $"Mouse drag {direction.DisplayName} requested.";
            return true;
        }

        if (TryParseMouseDirectionAction(action, "system-mouse-move-fixed:", out direction, out var remainder)
            && int.TryParse(remainder, out var distance)
            && distance > 0)
        {
            StopContinuousMouseMove();
            SendMouseMove(direction.DeltaX * MOUSE_FIXED_DISTANCE_PIXELS * distance, direction.DeltaY * MOUSE_FIXED_DISTANCE_PIXELS * distance);
            message = $"Mouse move {direction.DisplayName} {distance} requested.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    private void OpenSettingsUri(string uri, string displayName)
    {
        if (_dryRun)
            return;

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true
        });

        if (process is null)
            throw new InvalidOperationException($"{displayName} did not launch.");
    }

    private void SendVolumeKey(ushort virtualKey)
    {
        SendKey(virtualKey);
    }

    private void SendMediaKey(ushort virtualKey)
    {
        SendKey(virtualKey);
    }

    private static bool TryParseFunctionKeyAction(string action, out ushort virtualKey, out string displayName)
    {
        const string prefix = "system-press-f";
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(action[prefix.Length..], out var number)
            || number is < 1 or > 12)
        {
            virtualKey = 0;
            displayName = string.Empty;
            return false;
        }

        virtualKey = (ushort)(VK_F1 + number - 1);
        displayName = $"F{number}";
        return true;
    }

    private static bool TryParseDigitKeyAction(string action, out ushort virtualKey, out string displayName)
    {
        const string prefix = "system-press-digit:";
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(action[prefix.Length..], out var number)
            || number is < 0 or > 9)
        {
            virtualKey = 0;
            displayName = string.Empty;
            return false;
        }

        virtualKey = (ushort)(VK_0 + number);
        displayName = $"Digit {number}";
        return true;
    }

    private static bool TryParseLetterKeyAction(string action, out ushort virtualKey, out string displayName)
    {
        const string prefix = "system-press-letter:";
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || action.Length != prefix.Length + 1
            || action[prefix.Length] is < 'a' or > 'z')
        {
            virtualKey = 0;
            displayName = string.Empty;
            return false;
        }

        var letter = action[prefix.Length];
        virtualKey = (ushort)(VK_A + letter - 'a');
        displayName = $"Letter {char.ToUpperInvariant(letter)}";
        return true;
    }

    private static bool TryParseSymbolKeyAction(string action, out ushort virtualKey, out bool requiresShift, out string displayName)
    {
        const string prefix = "system-press-symbol:";
        virtualKey = 0;
        requiresShift = false;
        displayName = string.Empty;
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var symbol = action[prefix.Length..];
        (virtualKey, requiresShift, displayName) = symbol switch
        {
            "comma" => (VK_OEM_COMMA, false, "Comma"),
            "period" => (VK_OEM_PERIOD, false, "Period"),
            "slash" => (VK_OEM_2, false, "Slash"),
            "question" => (VK_OEM_2, true, "Question mark"),
            "semicolon" => (VK_OEM_1, false, "Semicolon"),
            "colon" => (VK_OEM_1, true, "Colon"),
            "apostrophe" => (VK_OEM_7, false, "Apostrophe"),
            "quote" => (VK_OEM_7, true, "Quote"),
            "minus" => (VK_OEM_MINUS, false, "Minus"),
            "underscore" => (VK_OEM_MINUS, true, "Underscore"),
            "equals" => (VK_OEM_PLUS, false, "Equals"),
            "plus" => (VK_OEM_PLUS, true, "Plus"),
            "left-bracket" => (VK_OEM_4, false, "Left bracket"),
            "right-bracket" => (VK_OEM_6, false, "Right bracket"),
            "left-brace" => (VK_OEM_4, true, "Left brace"),
            "right-brace" => (VK_OEM_6, true, "Right brace"),
            "backslash" => (VK_OEM_5, false, "Backslash"),
            "pipe" => (VK_OEM_5, true, "Pipe"),
            "grave" => (VK_OEM_3, false, "Grave accent"),
            "tilde" => (VK_OEM_3, true, "Tilde"),
            "exclamation" => (VK_1, true, "Exclamation point"),
            "at" => (VK_2, true, "At sign"),
            "hash" => (VK_3, true, "Number sign"),
            "dollar" => (VK_4, true, "Dollar sign"),
            "percent" => (VK_5, true, "Percent sign"),
            "caret" => (VK_6, true, "Caret"),
            "ampersand" => (VK_7, true, "Ampersand"),
            "asterisk" => (VK_8, true, "Asterisk"),
            "left-parenthesis" => (VK_9, true, "Left parenthesis"),
            "right-parenthesis" => (VK_0, true, "Right parenthesis"),
            _ => ((ushort)0, false, string.Empty)
        };

        return virtualKey != 0;
    }

    private bool TryExecuteModifierChordAction(string action, out string displayName)
    {
        const string prefix = "system-press-chord:";
        displayName = string.Empty;
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var chord = action[prefix.Length..];
        switch (chord)
        {
            case "shift-tab":
                SendKeyChord(VK_SHIFT, VK_TAB);
                displayName = "Shift Tab";
                return true;
            case "control-tab":
                SendKeyChord(VK_CONTROL, VK_TAB);
                displayName = "Control Tab";
                return true;
            case "control-shift-tab":
                SendTwoModifierKeyChord(VK_CONTROL, VK_SHIFT, VK_TAB);
                displayName = "Control Shift Tab";
                return true;
            case "alt-shift-tab":
                SendTwoModifierKeyChord(VK_MENU, VK_SHIFT, VK_TAB);
                displayName = "Alt Shift Tab";
                return true;
            case "control-a":
                SendKeyChord(VK_CONTROL, VK_A);
                displayName = "Control A";
                return true;
            case "control-b":
                SendKeyChord(VK_CONTROL, VK_B);
                displayName = "Control B";
                return true;
            case "control-c":
                SendKeyChord(VK_CONTROL, VK_C);
                displayName = "Control C";
                return true;
            case "control-f":
                SendKeyChord(VK_CONTROL, VK_F);
                displayName = "Control F";
                return true;
            case "control-i":
                SendKeyChord(VK_CONTROL, VK_I);
                displayName = "Control I";
                return true;
            case "control-n":
                SendKeyChord(VK_CONTROL, VK_N);
                displayName = "Control N";
                return true;
            case "control-o":
                SendKeyChord(VK_CONTROL, VK_O);
                displayName = "Control O";
                return true;
            case "control-p":
                SendKeyChord(VK_CONTROL, VK_P);
                displayName = "Control P";
                return true;
            case "control-s":
                SendKeyChord(VK_CONTROL, VK_S);
                displayName = "Control S";
                return true;
            case "control-u":
                SendKeyChord(VK_CONTROL, VK_U);
                displayName = "Control U";
                return true;
            case "control-v":
                SendKeyChord(VK_CONTROL, VK_V);
                displayName = "Control V";
                return true;
            case "control-x":
                SendKeyChord(VK_CONTROL, VK_X);
                displayName = "Control X";
                return true;
            case "control-y":
                SendKeyChord(VK_CONTROL, VK_Y);
                displayName = "Control Y";
                return true;
            case "control-z":
                SendKeyChord(VK_CONTROL, VK_Z);
                displayName = "Control Z";
                return true;
            case "control-plus":
                SendKeyChord(VK_CONTROL, VK_OEM_PLUS);
                displayName = "Control Plus";
                return true;
            case "control-minus":
                SendKeyChord(VK_CONTROL, VK_OEM_MINUS);
                displayName = "Control Minus";
                return true;
            case "control-zero":
                SendKeyChord(VK_CONTROL, VK_0);
                displayName = "Control Zero";
                return true;
            case "alt-left":
                SendAltKey(VK_LEFT);
                displayName = "Alt Left";
                return true;
            case "alt-right":
                SendAltKey(VK_RIGHT);
                displayName = "Alt Right";
                return true;
            case "alt-up":
                SendAltKey(VK_UP);
                displayName = "Alt Up";
                return true;
            case "alt-down":
                SendAltKey(VK_DOWN);
                displayName = "Alt Down";
                return true;
            case "control-home":
                SendKeyChord(VK_CONTROL, VK_HOME);
                displayName = "Control Home";
                return true;
            case "control-end":
                SendKeyChord(VK_CONTROL, VK_END);
                displayName = "Control End";
                return true;
            case "control-shift-home":
                SendTwoModifierKeyChord(VK_CONTROL, VK_SHIFT, VK_HOME);
                displayName = "Control Shift Home";
                return true;
            case "control-shift-end":
                SendTwoModifierKeyChord(VK_CONTROL, VK_SHIFT, VK_END);
                displayName = "Control Shift End";
                return true;
            default:
                if (TryParseControlShiftChord(chord, out var controlShiftVirtualKey, out displayName))
                {
                    SendTwoModifierKeyChord(VK_CONTROL, VK_SHIFT, controlShiftVirtualKey);
                    return true;
                }

                if (TryParseControlChord(chord, out var controlVirtualKey, out displayName))
                {
                    SendKeyChord(VK_CONTROL, controlVirtualKey);
                    return true;
                }

                if (TryParseShiftChord(chord, out var shiftVirtualKey, out displayName))
                {
                    SendKeyChord(VK_SHIFT, shiftVirtualKey);
                    return true;
                }

                if (TryParseAltChord(chord, out var altVirtualKey, out displayName))
                {
                    SendAltKey(altVirtualKey);
                    return true;
                }

                return false;
        }
    }

    private bool TryExecuteHeldModifierAction(string action, out string message)
    {
        message = string.Empty;
        if (action.Equals("system-release-modifiers", StringComparison.OrdinalIgnoreCase))
        {
            ReleaseAllModifiers();
            message = "All held modifier keys released.";
            return true;
        }

        const string holdPrefix = "system-hold-modifier:";
        if (action.StartsWith(holdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var modifierName = action[holdPrefix.Length..].Trim();
            if (!TryGetHeldModifierVirtualKey(modifierName, out var virtualKey, out var displayName))
            {
                message = "Unsupported modifier key.";
                return false;
            }

            SendKeyDown(virtualKey);
            message = $"{displayName} held.";
            return true;
        }

        const string releasePrefix = "system-release-modifier:";
        if (action.StartsWith(releasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var modifierName = action[releasePrefix.Length..].Trim();
            if (!TryGetHeldModifierVirtualKey(modifierName, out var virtualKey, out var displayName))
            {
                message = "Unsupported modifier key.";
                return false;
            }

            SendKeyUp(virtualKey);
            message = $"{displayName} released.";
            return true;
        }

        return false;
    }

    private static bool TryGetHeldModifierVirtualKey(string modifierName, out ushort virtualKey, out string displayName)
    {
        switch (modifierName)
        {
            case "shift":
                virtualKey = VK_SHIFT;
                displayName = "Shift";
                return true;
            case "control":
                virtualKey = VK_CONTROL;
                displayName = "Control";
                return true;
            case "alt":
                virtualKey = VK_MENU;
                displayName = "Alt";
                return true;
            default:
                virtualKey = 0;
                displayName = string.Empty;
                return false;
        }
    }

    private static bool TryParseControlShiftChord(string chord, out ushort virtualKey, out string displayName)
    {
        const string prefix = "control-shift-";
        virtualKey = 0;
        displayName = string.Empty;
        if (!chord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyName = chord[prefix.Length..];
        if (keyName.Length == 1 && keyName[0] is >= 'a' and <= 'z')
        {
            virtualKey = (ushort)(VK_A + keyName[0] - 'a');
            displayName = $"Control Shift {char.ToUpperInvariant(keyName[0])}";
            return true;
        }

        if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9')
        {
            virtualKey = (ushort)(VK_0 + keyName[0] - '0');
            displayName = $"Control Shift {keyName[0]}";
            return true;
        }

        return false;
    }

    private static bool TryParseControlChord(string chord, out ushort virtualKey, out string displayName)
    {
        const string prefix = "control-";
        virtualKey = 0;
        displayName = string.Empty;
        if (!chord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyName = chord[prefix.Length..];
        if (keyName.Length == 1 && keyName[0] is >= 'a' and <= 'z')
        {
            virtualKey = (ushort)(VK_A + keyName[0] - 'a');
            displayName = $"Control {char.ToUpperInvariant(keyName[0])}";
            return true;
        }

        if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9')
        {
            virtualKey = (ushort)(VK_0 + keyName[0] - '0');
            displayName = $"Control {keyName[0]}";
            return true;
        }

        (virtualKey, displayName) = keyName switch
        {
            "equals" => (VK_OEM_PLUS, "Control Equals"),
            "comma" => (VK_OEM_COMMA, "Control Comma"),
            "period" => (VK_OEM_PERIOD, "Control Period"),
            "slash" => (VK_OEM_2, "Control Slash"),
            "backslash" => (VK_OEM_5, "Control Backslash"),
            "semicolon" => (VK_OEM_1, "Control Semicolon"),
            "apostrophe" => (VK_OEM_7, "Control Apostrophe"),
            "left-bracket" => (VK_OEM_4, "Control Left Bracket"),
            "right-bracket" => (VK_OEM_6, "Control Right Bracket"),
            "grave" => (VK_OEM_3, "Control Grave"),
            _ => ((ushort)0, string.Empty)
        };

        return virtualKey != 0;
    }

    private static bool TryParseShiftChord(string chord, out ushort virtualKey, out string displayName)
    {
        const string prefix = "shift-";
        virtualKey = 0;
        displayName = string.Empty;
        if (!chord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyName = chord[prefix.Length..];
        if (keyName.Length == 1 && keyName[0] is >= 'a' and <= 'z')
        {
            virtualKey = (ushort)(VK_A + keyName[0] - 'a');
            displayName = $"Shift {char.ToUpperInvariant(keyName[0])}";
            return true;
        }

        if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9')
        {
            virtualKey = (ushort)(VK_0 + keyName[0] - '0');
            displayName = $"Shift {keyName[0]}";
            return true;
        }

        return false;
    }

    private static bool TryParseAltChord(string chord, out ushort virtualKey, out string displayName)
    {
        const string prefix = "alt-";
        virtualKey = 0;
        displayName = string.Empty;
        if (!chord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var keyName = chord[prefix.Length..];
        if (keyName.Length == 1 && keyName[0] is >= 'a' and <= 'z')
        {
            virtualKey = (ushort)(VK_A + keyName[0] - 'a');
            displayName = $"Alt {char.ToUpperInvariant(keyName[0])}";
            return true;
        }

        if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9')
        {
            virtualKey = (ushort)(VK_0 + keyName[0] - '0');
            displayName = $"Alt {keyName[0]}";
            return true;
        }

        return false;
    }

    private void SendKey(ushort virtualKey)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendKeyDown(ushort virtualKey)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendKeyUp(ushort virtualKey)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void ReleaseAllModifiers()
    {
        SendKeyUp(VK_SHIFT);
        SendKeyUp(VK_CONTROL);
        SendKeyUp(VK_MENU);
    }

    private void SendKeyChord(ushort modifierKey, ushort key)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendTwoModifierKeyChord(ushort firstModifierKey, ushort secondModifierKey, ushort key)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = firstModifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = secondModifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = secondModifierKey, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = firstModifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendAltKey(ushort key)
    {
        SendKeyChord(VK_MENU, key);
    }

    private void SendWindowsKey(ushort key)
    {
        SendKeyChord(VirtualKeyLeftWindows, key);
    }

    private void SendCtrlWindowsKey(ushort key)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendAltShiftKey(ushort key)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MENU } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MENU, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendAltBackspace()
    {
        SendAltKey(VK_BACK);
    }

    private void SendAltDelete()
    {
        SendAltKey(VK_DELETE);
    }

    private void SendCtrlArrow(ushort arrowKey)
    {
        SendKeyChord(VK_CONTROL, arrowKey);
    }

    private void SendCtrlShiftArrow(ushort arrowKey)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = arrowKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = arrowKey, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendCtrlDelete()
    {
        SendKeyChord(VK_CONTROL, VK_DELETE);
    }

    private void SendMouseClick()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
    }

    private void SendMouseDoubleClick()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
    }

    private void SendMouseTripleClick()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
    }

    private void SendMouseRightClick()
    {
        SendMouseButton(MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP);
    }

    private void SendMouseButtonDown()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN);
    }

    private void SendMouseButtonUp()
    {
        SendMouseButton(MOUSEEVENTF_LEFTUP);
    }

    private void SendMouseWheel(int delta)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new()
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_WHEEL,
                        mouseData = unchecked((uint)delta)
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendMouseHorizontalWheel(int delta)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new()
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_HWHEEL,
                        mouseData = unchecked((uint)delta)
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendMouseMove(int deltaX, int deltaY)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new()
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = deltaX,
                        dy = deltaY,
                        dwFlags = MOUSEEVENTF_MOVE
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendMouseDrag(int deltaX, int deltaY)
    {
        if (_dryRun)
            return;

        SendMouseButtonDown();
        Thread.Sleep(80);
        SendMouseMove(deltaX, deltaY);
        Thread.Sleep(80);
        SendMouseButtonUp();
    }

    private void StartContinuousMouseMove(MouseDirection direction)
    {
        lock (_mouseMotionSync)
        {
            _activeMouseMotionDirection = direction;
            _mouseMotionActive = true;
            if (_dryRun)
                return;

            _mouseMotionTimer ??= new System.Threading.Timer(_ => TickContinuousMouseMove(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _mouseMotionTimer.Change(MouseMotionTickInterval, MouseMotionTickInterval);
        }
    }

    private void StopContinuousMouseMove()
    {
        lock (_mouseMotionSync)
        {
            _mouseMotionActive = false;
            if (_dryRun)
                return;

            _mouseMotionTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void AdjustContinuousMouseMoveSpeed(int delta)
    {
        lock (_mouseMotionSync)
        {
            _mouseMotionSpeedIndex = Math.Clamp(_mouseMotionSpeedIndex + delta, 0, MouseMotionSpeedPixelsPerTick.Length - 1);
        }
    }

    private void TickContinuousMouseMove()
    {
        MouseDirection direction;
        int pixelsPerTick;
        lock (_mouseMotionSync)
        {
            if (!_mouseMotionActive)
                return;

            direction = _activeMouseMotionDirection;
            pixelsPerTick = MouseMotionSpeedPixelsPerTick[_mouseMotionSpeedIndex];
        }

        SendMouseMove(direction.DeltaX * pixelsPerTick, direction.DeltaY * pixelsPerTick);
    }

    private static bool TryParseMouseDirectionAction(string action, string prefix, out MouseDirection direction, out string remainder)
    {
        direction = default;
        remainder = string.Empty;
        if (!action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var value = action[prefix.Length..].Trim();
        var separatorIndex = value.IndexOf(':');
        if (separatorIndex >= 0)
        {
            remainder = value[(separatorIndex + 1)..].Trim();
            value = value[..separatorIndex].Trim();
        }

        return TryParseMouseDirection(value, out direction);
    }

    private static bool TryParseMouseDirection(string value, out MouseDirection direction)
    {
        direction = value switch
        {
            "up" => new MouseDirection(0, -1, "up"),
            "down" => new MouseDirection(0, 1, "down"),
            "left" => new MouseDirection(-1, 0, "left"),
            "right" => new MouseDirection(1, 0, "right"),
            "top-left" => new MouseDirection(-1, -1, "top left"),
            "top-right" => new MouseDirection(1, -1, "top right"),
            "bottom-left" => new MouseDirection(-1, 1, "bottom left"),
            "bottom-right" => new MouseDirection(1, 1, "bottom right"),
            _ => default
        };

        return direction != default;
    }

    private void SendMouseButton(uint downFlag, uint upFlag)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = downFlag } } },
            new() { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = upFlag } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendMouseButton(uint flag)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flag } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendDesktopShortcut()
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyD } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyD, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendAltTab()
    {
        SendChord(VirtualKeyLeftAlt, VirtualKeyTab);
    }

    private void SendAltShiftTab()
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftAlt } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyTab } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyTab, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftAlt, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void SendTaskManagerShortcut()
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private void ShowForegroundWindow(int command)
    {
        if (_dryRun)
            return;

        var hwnd = GetForegroundWindow();
        if (hwnd == nint.Zero)
            throw new InvalidOperationException("No foreground window was available.");

        if (!ShowWindow(hwnd, command))
            throw new InvalidOperationException("ShowWindow returned false.");
    }

    private static List<VisibleWindowSwitchCandidate> EnumerateVisibleWindowCandidates(string normalizedRequest, int ignoredProcessId, int maxCandidates)
    {
        var candidates = new List<VisibleWindowSwitchCandidate>();
        EnumWindows((hwnd, _) =>
        {
            if (hwnd == nint.Zero || !IsWindowVisible(hwnd))
                return true;

            if (!GetWindowRect(hwnd, out var rect))
                return true;

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
                return true;

            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ignoredProcessId)
                return true;

            var title = ReadWindowText(hwnd);
            var processName = TryGetProcessName(processId);
            var score = ScoreVisibleWindowCandidate(normalizedRequest, title, processName);
            if (score <= 0)
                return true;

            var matchKind = score switch
            {
                >= 400 => "exact",
                >= 300 => "starts-with",
                >= 200 => "contains",
                _ => "tokens"
            };

            candidates.Add(new VisibleWindowSwitchCandidate(
                hwnd,
                title,
                processName,
                IsIconic(hwnd),
                score,
                matchKind));
            return true;
        }, IntPtr.Zero);

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxCandidates))
            .ToList();
    }

    private static int ScoreVisibleWindowCandidate(string normalizedRequest, string title, string processName)
    {
        if (string.IsNullOrWhiteSpace(normalizedRequest))
            return 0;

        var normalizedTitle = NormalizeWindowName(title);
        var normalizedProcess = NormalizeWindowName(processName);
        var normalizedCombined = NormalizeWindowName($"{processName} {title}");

        if (string.Equals(normalizedTitle, normalizedRequest, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedProcess, normalizedRequest, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedCombined, normalizedRequest, StringComparison.OrdinalIgnoreCase))
            return 400;

        if ((!string.IsNullOrWhiteSpace(normalizedTitle) && normalizedTitle.StartsWith(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedProcess) && normalizedProcess.StartsWith(normalizedRequest, StringComparison.OrdinalIgnoreCase)))
            return 300;

        if ((!string.IsNullOrWhiteSpace(normalizedTitle) && normalizedTitle.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedProcess) && normalizedProcess.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedCombined) && normalizedCombined.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase)))
            return 200;

        var requestTokens = normalizedRequest.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requestTokens.Length > 0
            && requestTokens.All(token => normalizedCombined.Contains(token, StringComparison.OrdinalIgnoreCase)))
            return 100;

        return 0;
    }

    private static string ReadWindowText(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0)
            return string.Empty;

        var buffer = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString().Trim();
    }

    private static string TryGetProcessName(int processId)
    {
        if (processId <= 0)
            return string.Empty;

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeWindowName(string value)
    {
        var normalized = StartMenuLauncher.ResolveAppName(value).Trim().ToLowerInvariant();
        normalized = normalized
            .Replace("&", " and ", StringComparison.Ordinal)
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("\\", " ", StringComparison.Ordinal)
            .Replace("+", " plus ", StringComparison.Ordinal)
            .Replace("#", " number ", StringComparison.Ordinal)
            .Replace("@", " at ", StringComparison.Ordinal)
            .Replace("*", " star ", StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace("\"", string.Empty, StringComparison.Ordinal);
        return string.Join(
            " ",
            normalized.Split([' ', '\t', '\r', '\n', '_', '-', '.', ',', ':', ';', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static bool TryParseChoiceNumber(string value, out int candidateNumber)
    {
        candidateNumber = NormalizeWindowName(value) switch
        {
            "one" or "first" => 1,
            "two" or "second" => 2,
            "three" or "third" => 3,
            "four" or "fourth" => 4,
            "five" or "fifth" => 5,
            var numberText when int.TryParse(numberText, out var parsed) => parsed,
            _ => 0
        };
        return candidateNumber is >= 1 and <= 5;
    }

    private void SendChord(ushort modifierKey, ushort key)
    {
        if (_dryRun)
            return;

        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private const int InputKeyboard = 1;
    private const int InputMouse = 0;
    private const ushort VK_VOLUME_UP = 0xAF;
    private const ushort VK_VOLUME_DOWN = 0xAE;
    private const ushort VK_VOLUME_MUTE = 0xAD;
    private const ushort VK_MEDIA_NEXT_TRACK = 0xB0;
    private const ushort VK_MEDIA_PREV_TRACK = 0xB1;
    private const ushort VK_MEDIA_STOP = 0xB2;
    private const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_A = 0x41;
    private const ushort VK_C = 0x43;
    private const ushort VK_B = 0x42;
    private const ushort VK_I = 0x49;
    private const ushort VK_K = 0x4B;
    private const ushort VK_U = 0x55;
    private const ushort VK_X = 0x58;
    private const ushort VK_V = 0x56;
    private const ushort VK_S = 0x53;
    private const ushort VK_Z = 0x5A;
    private const ushort VK_Y = 0x59;
    private const ushort VK_F = 0x46;
    private const ushort VK_N = 0x4E;
    private const ushort VK_O = 0x4F;
    private const ushort VK_P = 0x50;
    private const ushort VK_0 = 0x30;
    private const ushort VK_1 = 0x31;
    private const ushort VK_2 = 0x32;
    private const ushort VK_3 = 0x33;
    private const ushort VK_4 = 0x34;
    private const ushort VK_5 = 0x35;
    private const ushort VK_6 = 0x36;
    private const ushort VK_7 = 0x37;
    private const ushort VK_8 = 0x38;
    private const ushort VK_9 = 0x39;
    private const ushort VK_OEM_1 = 0xBA;
    private const ushort VK_OEM_PLUS = 0xBB;
    private const ushort VK_OEM_COMMA = 0xBC;
    private const ushort VK_OEM_MINUS = 0xBD;
    private const ushort VK_OEM_PERIOD = 0xBE;
    private const ushort VK_OEM_2 = 0xBF;
    private const ushort VK_OEM_3 = 0xC0;
    private const ushort VK_OEM_4 = 0xDB;
    private const ushort VK_OEM_5 = 0xDC;
    private const ushort VK_OEM_6 = 0xDD;
    private const ushort VK_OEM_7 = 0xDE;
    private const ushort VK_F4 = 0x73;
    private const ushort VK_DELETE = 0x2E;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_ESCAPE = 0x1B;
    private const ushort VK_BACK = 0x08;
    private const ushort VK_SPACE = 0x20;
    private const ushort VK_INSERT = 0x2D;
    private const ushort VK_APPS = 0x5D;
    private const ushort VK_CAPITAL = 0x14;
    private const ushort VK_UP = 0x26;
    private const ushort VK_DOWN = 0x28;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_RIGHT = 0x27;
    private const ushort VK_HOME = 0x24;
    private const ushort VK_END = 0x23;
    private const ushort VK_PRIOR = 0x21;
    private const ushort VK_NEXT = 0x22;
    private const ushort VK_F1 = 0x70;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const ushort VirtualKeyD = 0x44;
    private const ushort VirtualKeyLeftAlt = 0xA4;
    private const ushort VirtualKeyLeftShift = 0xA0;
    private const ushort VirtualKeyLeftCtrl = 0xA2;
    private const ushort VirtualKeyTab = 0x09;
    private const ushort VirtualKeyEscape = 0x1B;
    private const int SW_MINIMIZE = 6;
    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL = 0x01000;
    private const int MOUSE_WHEEL_DELTA = 120;
    private const int MOUSE_NUDGE_PIXELS = 80;
    private const int MOUSE_FIXED_DISTANCE_PIXELS = 32;
    private const int DefaultMouseMotionSpeedIndex = 1;
    private static readonly int[] MouseMotionSpeedPixelsPerTick = [16, 28, 44, 64];
    private static readonly TimeSpan MouseMotionTickInterval = TimeSpan.FromMilliseconds(35);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out Rect lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(nint hWnd, IntPtr lParam);
    private readonly record struct MouseDirection(int DeltaX, int DeltaY, string DisplayName);
}
