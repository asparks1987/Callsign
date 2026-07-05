using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Text.Json;
using Callsign.Extensions;
using Callsign.UI.Models;
using Callsign.UI.Services;

namespace Callsign.UI;

public sealed class MainForm : Form
{
    private enum VisibleControlsScope
    {
        CurrentSurface,
        Taskbar,
        NamedWindow
    }

    private enum MouseGridScope
    {
        Desktop,
        CurrentWindow
    }

    private readonly ProfileStore _profileStore;
    private readonly AlphaSessionStateMachine _session = new();
    private readonly StartMenuLauncher _launcher = new();
    private readonly AlphaAuditLog _auditLog;
    private readonly VoiceShortcutStore _voiceShortcutStore = new();
    private readonly VoiceCommandService _voiceCommandService = new();
    private readonly VoiceSampleCaptureService _voiceSampleCapture = new();
    private readonly VoiceBiometricVerificationService _voiceBiometricVerificationService = new();
    private readonly BrowserLaunchService _browserLaunchService = new();
    private readonly SystemControlService _systemControlService = new();
    private readonly FileSearchService _fileSearchService = new();
    private readonly DesktopVisibleControlService _desktopVisibleControlService = new();
    private readonly RuntimeStateMonitor _runtimeStateMonitor = new();
    private readonly System.Windows.Forms.Timer _sessionTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _visibleControlsRefreshTimer = new() { Interval = 250 };
    private readonly UpdateCheckService _updateCheckService = new();
    private readonly System.Windows.Forms.Timer _updateCheckTimer;
    private SpeechSynthesizer? _dictationReadbackSynthesizer;
    private SpeechSynthesizer? _statusReadbackSynthesizer;
    private WakeOverlayForm? _wakeOverlay;
    private UpdateSplashForm? _updateSplash;
    private bool _wakeOverlayMissingLogged;
    private bool _updateCheckInProgress;
    private bool _startupWalkthroughShownThisSession;
    private string? _lastShownManifestVersion;
    private readonly string _updateSplashStatePath;
    private readonly string _startupWalkthroughStatePath;

    private readonly List<UserProfile> _profiles = [];
    private UserProfile? _activeProfile;
    private bool _updatingUi;
    private bool _updatingVoiceModeControls;
    private bool _formReadyForListener;
    private bool _dictationActive;
    private string _voiceAccessMode = "Default";
    private DateTime? _lastHandledRuntimeUiRequestUtc;
    private DateTime? _lastAppliedServiceDictationUtc;
    private DateTime? _runtimeStopRequestedUtc;
    private bool _usingLocalPreviewListener;
    private bool _voiceActivationBusy;

    private TabControl _tabs = null!;
    private ComboBox _profilePicker = null!;
    private TextBox _callsignText = null!;
    private TextBox _displayNameText = null!;
    private TextBox _emailText = null!;
    private TextBox _departmentText = null!;
    private TextBox _notesText = null!;
    private Label _accountPathLabel = null!;
    private Label _accountStateLabel = null!;

    private Label _voiceStateLabel = null!;
    private Label _voiceSamplesLabel = null!;
    private Label _voiceLastTrainedLabel = null!;
    private Label _voiceRecognitionModeLabel = null!;
    private Label _voiceRecordingStateLabel = null!;
    private Label _voicePlaybackStateLabel = null!;
    private Label _voiceNextStepLabel = null!;
    private Label _voiceFailureLabel = null!;
    private TextBox _voiceHelpTextBox = null!;
    private TextBox _voicePromptText = null!;
    private ProgressBar _voiceProgress = null!;

    private Label _sessionStateLabel = null!;
    private Label _sessionPhaseLabel = null!;
    private Label _sessionNextActionLabel = null!;
    private Label _sessionHintLabel = null!;
    private Label _sessionIdentityLabel = null!;
    private Label _sessionCommandLabel = null!;
    private Label _sessionCountdownLabel = null!;
    private Label _sessionResultLabel = null!;
    private Label _sessionSpeechCueLabel = null!;
    private Label _sessionTranscriptHistoryLabel = null!;
    private ListBox _sessionTranscriptHistoryList = null!;
    private Label _listeningStateLabel = null!;
    private Label _lastHeardLabel = null!;
    private Label _wakeReliabilityLabel = null!;
    private Label _wakeCandidateLabel = null!;
    private Label _wakeScoreLabel = null!;
    private Label _wakeQualityLabel = null!;
    private Label _runtimeOwnerLabel = null!;
    private Label _runtimeProofLabel = null!;
    private Label _micLevelLabel = null!;
    private Label _micDetailLabel = null!;
    private RadioButton _voiceModeCommandsOnlyRadio = null!;
    private RadioButton _voiceModeDictationOnlyRadio = null!;
    private RadioButton _voiceModeDefaultRadio = null!;
    private TextBox _spokenCallsignText = null!;
    private TextBox _spokenCommandText = null!;
    private TextBox _appNameText = null!;
    private Label _appCandidateHintLabel = null!;
    private ListBox _appCandidateList = null!;
    private TextBox _voicePhraseText = null!;

    private Button _recordSampleButton = null!;
    private Button _playSampleButton = null!;
    private Button _trainVoiceButton = null!;
    private Button _resetVoiceButton = null!;
    private Button _wakeButton = null!;
    private Button _startListeningButton = null!;
    private Button _stopListeningButton = null!;
    private Button _readStatusButton = null!;
    private Button _stopStatusReadbackButton = null!;
    private Button _clearRecentSpeechButton = null!;
    private Button _rehearsePhraseButton = null!;
    private Button _verifyButton = null!;
    private Button _captureButton = null!;
    private Button _launchButton = null!;
    private Button _confirmAppCandidateButton = null!;
    private Button _clearAppCandidateButton = null!;
    private Button _cancelButton = null!;
    private Button _resetSessionButton = null!;
    private Button _newProfileButton = null!;
    private Button _saveProfileButton = null!;
    private Button _deleteProfileButton = null!;
    private List<VisibleControlSummaryEntry> _visibleControlsSummary = [];
    private List<DesktopVisibleControlEntry> _desktopVisibleControlsSummary = [];
    private VisibleControlsScope _visibleControlsScope = VisibleControlsScope.CurrentSurface;
    private string _visibleControlsNamedWindowTarget = string.Empty;
    private VisibleControlsOverlayForm? _visibleControlsOverlay;
    private CommandPaletteForm? _commandPalette;
    private MouseGridOverlayForm? _mouseGridOverlay;
    private KeyboardOverlayForm? _keyboardOverlay;
    private DictationCorrectionForm? _dictationCorrectionForm;
    private DictationCorrectionSession? _dictationCorrectionSession;
    private StartMenuAppResolution? _pendingAppResolution;
    private VisibleWindowSwitchResolution? _pendingWindowSwitchResolution;

    private TextBox _dictationTextBox = null!;
    private Label _dictationStatusLabel = null!;
    private Label _dictationHintLabel = null!;
    private Label _dictationSafetyLabel = null!;
    private Label _dictationSpeechCueLabel = null!;
    private Label _dictationLastHeardLabel = null!;
    private Label _dictationHistoryLabel = null!;
    private ListBox _dictationHistoryList = null!;
    private Button _startDictationButton = null!;
    private Button _stopDictationButton = null!;
    private Button _copyDictationButton = null!;
    private Button _readDictationButton = null!;
    private Button _stopDictationReadbackButton = null!;
    private Button _pasteDictationButton = null!;
    private Button _clearDictationButton = null!;
    private Button _cutDictationButton = null!;
    private Button _undoDictationButton = null!;
    private Button _redoDictationButton = null!;
    private Button _goToStartDictationButton = null!;
    private Button _goToEndDictationButton = null!;
    private Button _selectToStartDictationButton = null!;
    private Button _selectToEndDictationButton = null!;
    private Button _deleteToStartDictationButton = null!;
    private Button _deleteToEndDictationButton = null!;
    private Button _goToLineStartDictationButton = null!;
    private Button _goToLineEndDictationButton = null!;
    private Button _selectToLineStartDictationButton = null!;
    private Button _selectToLineEndDictationButton = null!;
    private Button _deleteToLineStartDictationButton = null!;
    private Button _deleteToLineEndDictationButton = null!;
    private Button _goToParagraphStartDictationButton = null!;
    private Button _goToParagraphEndDictationButton = null!;
    private Button _selectToParagraphStartDictationButton = null!;
    private Button _selectToParagraphEndDictationButton = null!;
    private Button _deleteToParagraphStartDictationButton = null!;
    private Button _deleteToParagraphEndDictationButton = null!;
    private Button _replacePreviousParagraphDictationButton = null!;
    private Button _newlineDictationButton = null!;
    private Button _paragraphDictationButton = null!;
    private Button _deleteWordDictationButton = null!;
    private Button _goToPreviousWordDictationButton = null!;
    private Button _goToNextWordDictationButton = null!;
    private Button _selectPreviousWordDictationButton = null!;
    private Button _selectNextWordDictationButton = null!;
    private Button _deletePreviousWordDictationButton = null!;
    private Button _deleteNextWordDictationButton = null!;
    private Button _goToPreviousSentenceDictationButton = null!;
    private Button _goToNextSentenceDictationButton = null!;
    private Button _selectPreviousSentenceDictationButton = null!;
    private Button _selectNextSentenceDictationButton = null!;
    private Button _deletePreviousSentenceDictationButton = null!;
    private Button _deleteNextSentenceDictationButton = null!;
    private Button _goToPreviousParagraphDictationButton = null!;
    private Button _goToNextParagraphDictationButton = null!;
    private Button _replacePreviousWordDictationButton = null!;
    private Button _replacePreviousSentenceDictationButton = null!;
    private Button _replaceAllDictationButton = null!;
    private Button _commaDictationButton = null!;
    private Button _periodDictationButton = null!;
    private Button _questionDictationButton = null!;
    private Button _exclamationDictationButton = null!;
    private Button _semicolonDictationButton = null!;
    private Button _colonDictationButton = null!;
    private Button _apostropheDictationButton = null!;
    private Button _quoteDictationButton = null!;
    private Button _openParenthesisDictationButton = null!;
    private Button _closeParenthesisDictationButton = null!;
    private Button _hyphenDictationButton = null!;
    private Button _dashDictationButton = null!;
    private Button _slashDictationButton = null!;
    private Button _atSignDictationButton = null!;

    private TextBox _browserInputText = null!;
    private TextBox _browserAddressTextInput = null!;
    private TextBox _browserFindTextInput = null!;
    private Label _browserStatusLabel = null!;
    private Label _browserSafetyLabel = null!;
    private Label _browserVoiceCueLabel = null!;
    private Label _browserLastHeardLabel = null!;
    private Label _browserLastActionLabel = null!;
    private string _lastLocalBrowserActionLabel = "Last action: none yet.";
    private Button _openBrowserButton = null!;
    private Button _searchBrowserButton = null!;
    private Button _copyBrowserTargetButton = null!;
    private Button _browserBackButton = null!;
    private Button _browserForwardButton = null!;
    private Button _browserRefreshButton = null!;
    private Button _browserNewTabButton = null!;
    private Button _browserNewWindowButton = null!;
    private Button _browserPrivateWindowButton = null!;
    private Button _browserBookmarkPageButton = null!;
    private Button _browserOpenBookmarksButton = null!;
    private Button _browserSavePageButton = null!;
    private Button _browserPrintPageButton = null!;
    private Button _browserNextTabButton = null!;
    private Button _browserPreviousTabButton = null!;
    private Button _browserCloseTabButton = null!;
    private Button _browserReopenClosedTabButton = null!;
    private Button _browserAddressBarButton = null!;
    private Button _browserAddressTextButton = null!;
    private Button _browserHomeButton = null!;
    private Button _browserDownloadsButton = null!;
    private Button _browserHistoryButton = null!;
    private Button _browserScrollUpButton = null!;
    private Button _browserScrollDownButton = null!;
    private Button _browserScrollTopButton = null!;
    private Button _browserScrollBottomButton = null!;
    private Button _browserFindButton = null!;
    private Button _browserFindTextButton = null!;
    private Button _browserFindNextButton = null!;
    private Button _browserFindPreviousButton = null!;
    private Button _browserStartScrollUpButton = null!;
    private Button _browserStartScrollDownButton = null!;
    private Button _browserStartScrollLeftButton = null!;
    private Button _browserStartScrollRightButton = null!;
    private Button _browserStopScrollButton = null!;
    private Button _browserScrollLeftButton = null!;
    private Button _browserScrollRightButton = null!;
    private Button _browserFullscreenButton = null!;
    private Button _browserZoomInButton = null!;
    private Button _browserZoomOutButton = null!;
    private Button _browserZoomResetButton = null!;

    private Label _systemStatusLabel = null!;
    private Button _systemVolumeUpButton = null!;
    private Button _systemVolumeDownButton = null!;
    private Button _systemMuteButton = null!;
    private Button _systemMediaPlayPauseButton = null!;
    private Button _systemMediaNextButton = null!;
    private Button _systemMediaPreviousButton = null!;
    private Button _systemMediaStopButton = null!;
    private Button _systemShowDesktopButton = null!;
    private Button _systemNextWindowButton = null!;
    private Button _systemPreviousWindowButton = null!;
    private Button _systemTaskViewButton = null!;
    private TextBox _systemSwitchWindowText = null!;
    private Button _systemSwitchWindowButton = null!;
    private Button _systemConfirmWindowChoiceButton = null!;
    private Button _systemClearWindowChoicesButton = null!;
    private Label _systemWindowChoiceHintLabel = null!;
    private ListBox _systemWindowChoiceList = null!;
    private Button _systemNewVirtualDesktopButton = null!;
    private Button _systemNextVirtualDesktopButton = null!;
    private Button _systemPreviousVirtualDesktopButton = null!;
    private Button _systemTaskManagerButton = null!;
    private Button _systemSettingsButton = null!;
    private Button _systemDisplaySettingsButton = null!;
    private Button _systemSoundSettingsButton = null!;
    private Button _systemBluetoothSettingsButton = null!;
    private Button _systemNetworkSettingsButton = null!;
    private Button _systemAccessibilitySettingsButton = null!;
    private Button _systemMinimizeWindowButton = null!;
    private Button _systemMaximizeWindowButton = null!;
    private Button _systemRestoreWindowButton = null!;
    private Button _systemSnapLeftButton = null!;
    private Button _systemSnapRightButton = null!;
    private Button _systemSnapUpButton = null!;
    private Button _systemSnapDownButton = null!;
    private Button _systemSnapLayoutsButton = null!;
    private Button _systemEnterButton = null!;
    private Button _systemTabButton = null!;
    private Button _systemEscapeButton = null!;
    private Button _systemBackspaceButton = null!;
    private Button _systemSpaceButton = null!;
    private Button _systemDeleteButton = null!;
    private Button _systemInsertButton = null!;
    private Button _systemWindowsKeyButton = null!;
    private Button _systemContextMenuButton = null!;
    private Button _systemCapsLockButton = null!;
    private Button[] _systemDigitButtons = [];
    private Button[] _systemLetterButtons = [];
    private Button[] _systemSymbolButtons = [];
    private Button[] _systemChordButtons = [];
    private Button _systemUpButton = null!;
    private Button _systemDownButton = null!;
    private Button _systemLeftButton = null!;
    private Button _systemRightButton = null!;
    private Button _systemHomeButton = null!;
    private Button _systemEndButton = null!;
    private Button _systemPageUpButton = null!;
    private Button _systemPageDownButton = null!;
    private Button _systemMouseClickButton = null!;
    private Button _systemMouseDoubleClickButton = null!;
    private Button _systemMouseRightClickButton = null!;
    private Button _systemMouseButtonDownButton = null!;
    private Button _systemMouseButtonUpButton = null!;
    private Button _systemMouseScrollUpButton = null!;
    private Button _systemMouseScrollDownButton = null!;
    private Button _systemMouseScrollLeftButton = null!;
    private Button _systemMouseScrollRightButton = null!;
    private Button _systemMouseMoveUpButton = null!;
    private Button _systemMouseMoveDownButton = null!;
    private Button _systemMouseMoveLeftButton = null!;
    private Button _systemMouseMoveRightButton = null!;
    private Button _systemMouseDragUpButton = null!;
    private Button _systemMouseDragDownButton = null!;
    private Button _systemMouseDragLeftButton = null!;
    private Button _systemMouseDragRightButton = null!;
    private Button _systemCopyButton = null!;
    private Button _systemPasteButton = null!;
    private Button _systemCutButton = null!;
    private Button _systemSelectAllButton = null!;
    private Button _systemSaveButton = null!;
    private Button _systemUndoButton = null!;
    private Button _systemRedoButton = null!;
    private Button _systemBoldButton = null!;
    private Button _systemItalicButton = null!;
    private Button _systemUnderlineButton = null!;
    private Button _systemFindButton = null!;
    private Button _systemNewWindowButton = null!;
    private Button _systemNewDocumentButton = null!;
    private Button _systemOpenFileButton = null!;
    private Button _systemPrintButton = null!;
    private Button _systemZoomInButton = null!;
    private Button _systemZoomOutButton = null!;
    private Button _systemZoomResetButton = null!;
    private Button _systemCloseWindowButton = null!;
    private Button _systemMovePreviousCharacterButton = null!;
    private Button _systemMoveNextCharacterButton = null!;
    private Button _systemSelectPreviousCharacterButton = null!;
    private Button _systemSelectNextCharacterButton = null!;
    private Button _systemDeletePreviousCharacterButton = null!;
    private Button _systemDeleteNextCharacterButton = null!;
    private Button _systemMoveLineStartButton = null!;
    private Button _systemMoveLineEndButton = null!;
    private Button _systemMovePreviousLineButton = null!;
    private Button _systemMoveNextLineButton = null!;
    private Button _systemSelectToLineStartButton = null!;
    private Button _systemSelectToLineEndButton = null!;
    private Button _systemSelectPreviousLineButton = null!;
    private Button _systemSelectNextLineButton = null!;
    private Button _systemDeleteToLineStartButton = null!;
    private Button _systemDeleteToLineEndButton = null!;
    private Button _systemDeletePreviousLineButton = null!;
    private Button _systemDeleteNextLineButton = null!;
    private Button _systemMovePreviousWordButton = null!;
    private Button _systemMoveNextWordButton = null!;
    private Button _systemSelectPreviousWordButton = null!;
    private Button _systemSelectNextWordButton = null!;
    private Button _systemDeletePreviousWordButton = null!;
    private Button _systemDeleteNextWordButton = null!;
    private Button _systemMovePreviousSentenceButton = null!;
    private Button _systemMoveNextSentenceButton = null!;
    private Button _systemSelectPreviousSentenceButton = null!;
    private Button _systemSelectNextSentenceButton = null!;
    private Button _systemDeletePreviousSentenceButton = null!;
    private Button _systemDeleteNextSentenceButton = null!;
    private Button _systemMovePreviousParagraphButton = null!;
    private Button _systemMoveNextParagraphButton = null!;
    private Button _systemSelectPreviousParagraphButton = null!;
    private Button _systemSelectNextParagraphButton = null!;
    private Button _systemDeletePreviousParagraphButton = null!;
    private Button _systemDeleteNextParagraphButton = null!;
    private Label _systemSelectedActionLabel = null!;
    private Label _systemSafetyLabel = null!;
    private Label _systemLastActionLabel = null!;
    private Label _systemVoiceCueLabel = null!;
    private Label _systemLastHeardLabel = null!;
    private string _lastLocalSystemActionLabel = "Last action: none yet.";

    private TextBox _fileSearchQueryText = null!;
    private Label _fileSearchStatusLabel = null!;
    private Label _fileSearchSafetyLabel = null!;
    private Label _fileSearchSelectionLabel = null!;
    private ListBox _fileSearchResultsList = null!;
    private Label _fileSearchVoiceCueLabel = null!;
    private Label _fileSearchLastHeardLabel = null!;
    private Label _fileSearchLastActionLabel = null!;
    private string _lastLocalFileSearchActionLabel = "Last action: none yet.";
    private NumericUpDown _fileSearchResultNumber = null!;
    private Button _searchFilesButton = null!;
    private Button _selectFileResultButton = null!;
    private Button _openFileResultButton = null!;
    private Button _openFileFolderButton = null!;
    private Button _openFileResultByNumberButton = null!;
    private Button _openFileFolderByNumberButton = null!;

    private Label _updatesServerLabel = null!;
    private Label _updatesCadenceLabel = null!;
    private Label _updatesStateLabel = null!;
    private Label _updatesPendingLabel = null!;
    private Button _checkUpdatesButton = null!;

    private Label _packsRootLabel = null!;
    private Label _packsStatusLabel = null!;
    private Label _packsDropZoneLabel = null!;
    private Label _packsSelectedSummaryLabel = null!;
    private Label _packsEnablementLabel = null!;
    private ListBox _packsList = null!;
    private ListBox _packCommandsList = null!;
    private Button _refreshPacksButton = null!;
    private Button _importPackButton = null!;
    private Button _importPackFolderButton = null!;
    private Button _openPacksFolderButton = null!;
    private Button _enablePackButton = null!;
    private Button _disablePackButton = null!;
    private Button _removePackButton = null!;

    private Label _voiceShortcutStatusLabel = null!;
    private Label _voiceShortcutSafetyLabel = null!;
    private ListBox _voiceShortcutsList = null!;
    private ListBox _voiceShortcutActionsList = null!;
    private TextBox _voiceShortcutTitleText = null!;
    private TextBox _voiceShortcutPhraseText = null!;
    private TextBox _voiceShortcutGroupText = null!;
    private TextBox _voiceShortcutCommandActionText = null!;
    private NumericUpDown _voiceShortcutWaitMilliseconds = null!;
    private Button _newVoiceShortcutButton = null!;
    private Button _saveVoiceShortcutButton = null!;
    private Button _deleteVoiceShortcutButton = null!;
    private Button _enableVoiceShortcutButton = null!;
    private Button _disableVoiceShortcutButton = null!;
    private Button _addVoiceShortcutCommandButton = null!;
    private Button _addVoiceShortcutWaitButton = null!;
    private Button _removeVoiceShortcutActionButton = null!;
    private Button _moveVoiceShortcutActionUpButton = null!;
    private Button _moveVoiceShortcutActionDownButton = null!;
    private VoiceShortcutDefinition? _selectedVoiceShortcut;
    private readonly List<VoiceShortcutAction> _voiceShortcutDraftActions = [];

    private DateTime? _dictationStartedUtc;
    private DateTime? _dictationLastTranscriptUtc;
    private string? _dictationLastTranscriptText;
    private DictationCasingMode _dictationCasingMode = DictationCasingMode.Default;
    private string? _dictationUndoSnapshot;
    private int _dictationUndoSelectionStart;
    private int _dictationUndoSelectionLength;
    private string? _dictationRedoSnapshot;
    private int _dictationRedoSelectionStart;
    private int _dictationRedoSelectionLength;
    private string? _lastHeardTranscriptText;
    private float? _lastHeardTranscriptConfidence;
    private DateTime? _lastSessionTranscriptHistoryRuntimeUpdateUtc;
    private readonly List<string> _dictationHistoryEntries = [];

    private Label _statusLabel = null!;

    public MainForm(ProfileStore? profileStore = null)
    {
        _profileStore = profileStore ?? new ProfileStore();
        _updateCheckTimer = new() { Interval = (int)UpdateCheckService.DefaultCheckInterval.TotalMilliseconds };
        _updateSplashStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "updates-splash-state.json");
        _startupWalkthroughStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "startup-walkthrough-state.json");
        _auditLog = new AlphaAuditLog(_profileStore);
        _lastShownManifestVersion = LoadLastShownUpdateManifestVersion();

        Text = "Callsign Alpha Setup";
        Width = 1080;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 250, 253);
        ForeColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 9.75f);

        BuildForm();
        RefreshCommandRegistry();
        PreloadWakeOverlay();

        _voiceCommandService.TranscriptReceived += VoiceTranscriptReceived;
        _voiceCommandService.WakeWordDetected += VoiceWakeWordDetected;
        _voiceCommandService.RecognitionError += VoiceRecognitionError;
        _runtimeStateMonitor.Changed += RuntimeStateMonitorChanged;
        _voiceCommandService.SpeechActivityChanged += (_, _) => RunOnUiThread(() =>
        {
            UpdateVoiceCueRefreshRate();
            UpdateLiveSpeechCueFromActivity();
            RefreshSessionPanel();
            if (_dictationActive || IsWakeOverlaySessionActive(_session.State))
            {
                var phase = _dictationActive
                    ? "Dictation"
                    : FormatOverlayPhase(_session.State.ToString());
                var dictationReadout = _dictationActive ? BuildLocalDictationReadout() : null;
                ShowWakeOverlay(
                    _dictationActive ? dictationReadout : BuildLocalOverlayReadout(),
                    phase,
                    GetLocalTranscriptHistory(),
                    BuildLocalOverlayActivityLevel(),
                    BuildLocalActivityTextForWakeOverlay(),
                    _voiceCommandService.IsSpeechActive,
                    _dictationActive
                        ? BuildLocalDictationOverlayCaptionText()
                        : BuildLocalOverlayCaptionText(_session.State, _voiceCommandService.IsSpeechActive),
                    FormatLocalWakeCandidateReadout(),
                    BuildWakeOverlayAuthorityText());
            }
        });
        _voiceCommandService.ListeningStateChanged += (_, _) => RunOnUiThread(() =>
        {
            UpdateListeningPanel();
            RefreshVoicePanel();
            RefreshSessionPanel();
            if (!_voiceCommandService.IsListening)
                HideWakeOverlay();
        });

        _sessionTimer.Tick += (_, _) => OnSessionTick();
        _visibleControlsRefreshTimer.Tick += (_, _) => OnVisibleControlsRefreshTick();
        _updateCheckTimer.Tick += async (_, _) => await CheckForUpdatesAsync(force: false);
        LoadProfiles();
        _sessionTimer.Start();
        _updateCheckTimer.Start();

        UpdateStatus("Create an account, activate voice, then launch installed apps with wake word + callsign.");
        Shown += (_, _) =>
        {
            _formReadyForListener = true;
            ShowStartupWalkthroughIfNeeded();
            TryStartListenerForActiveProfile();
            _ = CheckForUpdatesAsync(force: true, attemptInstall: true);
        };
    }

    private void BuildForm()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            ItemSize = new Size(130, 28),
            SizeMode = TabSizeMode.Fixed,
            Padding = new Point(14, 6),
            HotTrack = true,
            DrawMode = TabDrawMode.Normal,
            BackColor = Color.FromArgb(248, 250, 253),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        _tabs.TabPages.Add(BuildAccountTab());
        _tabs.TabPages.Add(BuildVoiceTab());
        _tabs.TabPages.Add(BuildSessionTab());
        _tabs.TabPages.Add(BuildDictationTab());
        _tabs.TabPages.Add(BuildShortcutsTab());
        _tabs.TabPages.Add(BuildBrowserTab());
        _tabs.TabPages.Add(BuildSystemTab());
        _tabs.TabPages.Add(BuildFileSearchTab());
        _tabs.TabPages.Add(BuildPacksTab());
        _tabs.TabPages.Add(BuildUpdatesTab());
        foreach (TabPage page in _tabs.TabPages)
        {
            page.BackColor = Color.FromArgb(248, 250, 253);
            page.ForeColor = Color.FromArgb(15, 23, 42);
            page.Padding = new Padding(2);
        }
        root.Controls.Add(_tabs, 0, 0);

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            AutoSize = false,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 6, 10, 6),
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };
        root.Controls.Add(_statusLabel, 0, 1);

        Controls.Add(root);
    }

    private TabPage BuildAccountTab()
    {
        var tab = new TabPage("Account");
        var layout = BuildTwoColumnLayout(12);

        var heading = CreateHeading("Create the user account Callsign will verify against");

        _profilePicker = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Active account" };
        _profilePicker.SelectedIndexChanged += ProfilePickerChanged;

        _callsignText = BuildTextInput("Callsign");
        _displayNameText = BuildTextInput("Display name");
        _emailText = BuildTextInput("Email");
        _departmentText = BuildTextInput("Department");
        _notesText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 80,
            AccessibleName = "Notes",
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42)
        };

        _newProfileButton = CreateShellButton("Create New", 130);
        _newProfileButton.AccessibleName = "Account create new";
        _newProfileButton.AccessibleDescription = "Voice phrase: create new account.";
        _newProfileButton.Click += (_, _) => CreateNewProfile();

        _saveProfileButton = CreateShellButton("Save Account", 130);
        _saveProfileButton.AccessibleName = "Account save";
        _saveProfileButton.AccessibleDescription = "Voice phrase: save account.";
        _saveProfileButton.Click += (_, _) => SaveProfile();

        _deleteProfileButton = CreateShellButton("Delete Account", 130);
        _deleteProfileButton.AccessibleName = "Account delete";
        _deleteProfileButton.AccessibleDescription = "Voice phrase: delete account.";
        _deleteProfileButton.Click += (_, _) => DeleteProfile();

        var openFolderButton = CreateShellButton("Open Data Folder", 150);
        openFolderButton.AccessibleName = "Account open data folder";
        openFolderButton.AccessibleDescription = "Voice phrase: open data folder.";
        openFolderButton.Click += (_, _) => OpenProfileFolder();

        var openLogsButton = CreateShellButton("Open Logs Folder", 150);
        openLogsButton.AccessibleName = "Account open logs folder";
        openLogsButton.AccessibleDescription = "Voice phrase: open logs folder.";
        openLogsButton.Click += (_, _) => OpenLogsFolder();

        var openInstalledAppButton = CreateShellButton("Open App Folder", 150);
        openInstalledAppButton.AccessibleName = "Account open app folder";
        openInstalledAppButton.AccessibleDescription = "Voice phrase: open app folder.";
        openInstalledAppButton.Click += (_, _) => OpenInstalledAppFolder();

        var runWakeSetupButton = CreateShellButton("Repair Wakeword", 150);
        runWakeSetupButton.AccessibleName = "Account repair wakeword";
        runWakeSetupButton.AccessibleDescription = "Voice phrase: repair wakeword.";
        runWakeSetupButton.Click += (_, _) => RunOpenWakeWordSetupHelper();

        var runPyannoteSetupButton = CreateShellButton("Train Voice Identity", 170);
        runPyannoteSetupButton.AccessibleName = "Account train voice identity";
        runPyannoteSetupButton.AccessibleDescription = "Voice phrase: train voice identity.";
        runPyannoteSetupButton.Click += (_, _) => OpenVoiceIdentityTrainingForActiveProfile();

        var showVoiceHelpButton = CreateShellButton("Voice Help", 110);
        showVoiceHelpButton.AccessibleName = "Account voice help";
        showVoiceHelpButton.AccessibleDescription = "Voice phrase: voice help.";
        showVoiceHelpButton.Click += (_, _) => ShowVoiceHelp();

        var showGettingStartedButton = CreateShellButton("Getting Started", 140);
        showGettingStartedButton.AccessibleName = "Account getting started";
        showGettingStartedButton.AccessibleDescription = "Voice phrase: getting started.";
        showGettingStartedButton.Click += (_, _) => ShowStartupWalkthrough();

        _accountPathLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105), Text = "No profile selected." };
        _accountStateLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105), Text = "Voice not activated." };

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddRow(layout, "Active account", _profilePicker, row++);
        AddRow(layout, "Callsign", _callsignText, row++);
        AddFullWidth(layout, new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = "Tip: choose something easy to say out loud, like Alpha or Aryn One. Prefer spoken words over digits."
        }, row++);
        AddRow(layout, "Display name", _displayNameText, row++);
        AddRow(layout, "Email", _emailText, row++);
        AddRow(layout, "Department", _departmentText, row++);
        AddRow(layout, "Notes", _notesText, row++);

        layout.Controls.Add(new Label { Text = "Profile folder", ForeColor = Color.FromArgb(71, 85, 105) }, 0, row);
        layout.Controls.Add(_accountPathLabel, 1, row++);
        layout.Controls.Add(new Label { Text = "Voice status", ForeColor = Color.FromArgb(71, 85, 105) }, 0, row);
        layout.Controls.Add(_accountStateLabel, 1, row++);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = Padding.Empty,
            Margin = new Padding(0, 6, 0, 0)
        };
        buttons.Controls.Add(_newProfileButton);
        buttons.Controls.Add(_saveProfileButton);
        buttons.Controls.Add(_deleteProfileButton);
        buttons.Controls.Add(openFolderButton);
        buttons.Controls.Add(openLogsButton);
        buttons.Controls.Add(openInstalledAppButton);
        buttons.Controls.Add(runWakeSetupButton);
        buttons.Controls.Add(runPyannoteSetupButton);
        buttons.Controls.Add(showVoiceHelpButton);
        buttons.Controls.Add(showGettingStartedButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildVoiceTab()
    {
        var tab = new TabPage("Voice");
        var layout = BuildTwoColumnLayout(12);

        var heading = CreateHeading("Record and review the callsign that unlocks voice control");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Alpha v1 uses the activated callsign as the voice command gate. Hold the red record button while you speak, release to save the sample, then play it back before you activate voice control for this account."
        };
        var recordingTip = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = "The red button is press-and-hold only: keep it down while speaking so the user always knows when recording is live."
        };

        _voicePromptText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            Height = 90,
            ScrollBars = ScrollBars.Vertical,
            AccessibleName = "Sample prompt"
        };

        _voiceProgress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 3, Value = 0 };
        _voiceStateLabel = new Label { AutoSize = true, Text = "Not activated." };
        _voiceSamplesLabel = new Label { AutoSize = true, Text = "0 / 3 samples" };
        _voiceLastTrainedLabel = new Label { AutoSize = true, Text = "Never activated." };
        _voiceRecognitionModeLabel = new Label { AutoSize = true, Text = "Recognition mode: initializing..." };
        _voiceRecordingStateLabel = new Label { AutoSize = true, Text = "No sample recording in progress." };
        _voicePlaybackStateLabel = new Label { AutoSize = true, Text = "No sample available for playback." };
        _voiceNextStepLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Next step: create or pick a profile." };
        _voiceFailureLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Failure type: none yet." };
        _voiceHelpTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 220,
            AccessibleName = "Voice help"
        };
        _voiceHelpTextBox.Text = BuildVoiceHelpText();

        _recordSampleButton = new Button
        {
            Text = "REC Hold to Record",
            Width = 160,
            Height = 44,
            BackColor = Color.Firebrick,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font, FontStyle.Bold),
            Cursor = Cursors.Hand,
            AccessibleName = "Voice record sample",
            AccessibleDescription = "Voice phrase: record voice sample."
        };
        _recordSampleButton.FlatAppearance.BorderSize = 0;
        _recordSampleButton.MouseDown += RecordSampleButtonMouseDown;
        _recordSampleButton.MouseUp += RecordSampleButtonMouseUp;
        _recordSampleButton.MouseLeave += RecordSampleButtonMouseLeave;

        _playSampleButton = CreateShellButton("Play Sample", 120, 44);
        _playSampleButton.AccessibleName = "Voice play sample";
        _playSampleButton.AccessibleDescription = "Voice phrase: play voice sample.";
        _playSampleButton.Click += (_, _) => PlayLatestVoiceSample();

        _trainVoiceButton = CreateShellButton("Train Voice Identity", 160);
        _trainVoiceButton.AccessibleName = "Voice train identity";
        _trainVoiceButton.AccessibleDescription = "Voice phrase: train voice identity.";
        _trainVoiceButton.Click += (_, _) => TrainVoiceIdentity();

        _resetVoiceButton = CreateShellButton("Reset Voice", 130);
        _resetVoiceButton.AccessibleName = "Voice reset";
        _resetVoiceButton.AccessibleDescription = "Voice phrase: reset voice.";
        _resetVoiceButton.Click += (_, _) => ResetVoiceIdentity();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, recordingTip, row++);
        AddRow(layout, "Sample prompt", _voicePromptText, row++);
        AddRow(layout, "Training progress", _voiceProgress, row++);
        AddRow(layout, "Voice status", _voiceStateLabel, row++);
        AddRow(layout, "Samples recorded", _voiceSamplesLabel, row++);
        AddRow(layout, "Last activated", _voiceLastTrainedLabel, row++);
        AddRow(layout, "Recognition mode", _voiceRecognitionModeLabel, row++);
        AddRow(layout, "Recording", _voiceRecordingStateLabel, row++);
        AddRow(layout, "Playback", _voicePlaybackStateLabel, row++);
        AddRow(layout, "Next step", _voiceNextStepLabel, row++);
        AddRow(layout, "Failure type", _voiceFailureLabel, row++);
        AddFullWidth(layout, new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            Text = "Voice help"
        }, row++);
        AddFullWidth(layout, _voiceHelpTextBox, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_recordSampleButton);
        buttons.Controls.Add(_playSampleButton);
        buttons.Controls.Add(_trainVoiceButton);
        buttons.Controls.Add(_resetVoiceButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildSessionTab()
    {
        var tab = new TabPage("Session");
        var layout = BuildTwoColumnLayout(22);

        var heading = CreateHeading("Wake word, verify callsign, then launch an installed app");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "The alpha flow is: save a callsign, start listening, say 'Callsign' plus your callsign, then ask Callsign to launch an installed app through Start search. Starting the listener can activate voice for the saved callsign."
        };
        var examples = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = "Try: 'Callsign Alpha open Notepad' or 'Callsign Alpha launch Notepad', or say 'Callsign Alpha' then the app name. If Callsign shows multiple app choices, say '1', 'click 1', 'choose result 1', 'confirm app', or 'cancel'. Test Phrase Launch can open the app. Alpha accepts app names only, not paths, URLs, or terminal commands."
        };

        _spokenCallsignText = BuildTextInput("Spoken callsign");
        _spokenCommandText = BuildTextInput("Spoken command");
        _appNameText = BuildTextInput("App to launch");
        _voicePhraseText = BuildTextInput("Launch test phrase");
        _voicePhraseText.PlaceholderText = "Callsign Alpha launch Notepad";
        _appCandidateHintLabel = CreateStatusLabel("App confirmation: no ambiguous match pending.");
        _appCandidateHintLabel.MaximumSize = new Size(760, 0);
        _appCandidateList = CreateShellListBox(72);
        _appCandidateList.AccessibleDescription = "Visible numbered app choices for ambiguous launch requests. Voice phrases: 1, click 1, choose result 1, confirm app, next app choice, previous app choice, clear app choices, cancel.";
        _appCandidateList.Enabled = false;
        _appCandidateList.SelectedIndexChanged += (_, _) => RefreshPendingAppConfirmationHint();

        _sessionStateLabel = new Label { AutoSize = true, Text = "Idle." };
        _sessionPhaseLabel = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = "Phase: Listening" };
        _sessionNextActionLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(37, 99, 235), Font = new Font(Font, FontStyle.Bold), Text = "Next: say Callsign." };
        _sessionHintLabel = CreateStatusLabel("Next: say Callsign.");
        _sessionIdentityLabel = new Label { AutoSize = true, Text = "Waiting for wake word." };
        _sessionCommandLabel = new Label { AutoSize = true, Text = "No command captured." };
        _sessionCountdownLabel = new Label { AutoSize = true, Text = "No timer running." };
        _sessionResultLabel = new Label { AutoSize = true, Text = "No launch yet." };
        _sessionSpeechCueLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Speech cue: nothing heard yet." };
        _sessionTranscriptHistoryLabel = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = "Recent speech" };
        _sessionTranscriptHistoryList = CreateShellListBox(110);
        _listeningStateLabel = new Label { AutoSize = true, Text = "Microphone listener is stopped." };
        _lastHeardLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Nothing heard yet." };
        _wakeReliabilityLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "No wake event detected yet." };
        _wakeCandidateLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Wake candidate: nothing heard yet." };
        _wakeScoreLabel = new Label { AutoSize = true, Text = "Wake score unavailable." };
        _wakeQualityLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Audio quality diagnostics unavailable." };
        _runtimeOwnerLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Runtime owner unavailable." };
        _runtimeProofLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Runtime proof unavailable." };
        _micLevelLabel = new Label { AutoSize = true, Text = "Microphone level unavailable." };
        _micDetailLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "No microphone telemetry yet." };

        _startListeningButton = new Button { Text = "Start Listening", Width = 140, AccessibleName = "Session start listening", AccessibleDescription = "Voice phrases: start listening, start voice, voice access wake up, wake up, unmute microphone." };
        _startListeningButton.Click += (_, _) => StartVoiceListening();

        _stopListeningButton = new Button { Text = "Stop Listening", Width = 130, Enabled = false, AccessibleName = "Session stop listening", AccessibleDescription = "Voice phrases: stop listening, stop voice, voice access sleep, go to sleep, turn off microphone, turn off voice access, stop voice access, close voice access, exit voice access, quit voice access, mute microphone." };
        _stopListeningButton.Click += (_, _) => StopVoiceListening();

        _readStatusButton = new Button { Text = "Read Status", Width = 115, AccessibleName = "Session read status", AccessibleDescription = "Voice phrases: what did you hear, read status, repeat status." };
        _readStatusButton.Click += (_, _) => ReadCurrentStatusAloud();

        _stopStatusReadbackButton = new Button { Text = "Stop Status", Width = 115, AccessibleName = "Session stop status readback", AccessibleDescription = "Voice phrases: stop status readback, stop reading status." };
        _stopStatusReadbackButton.Click += (_, _) => StopStatusReadback();

        _clearRecentSpeechButton = new Button { Text = "Clear Speech", Width = 120, AccessibleName = "Session clear recent speech", AccessibleDescription = "Voice phrases: clear recent speech, clear speech history." };
        _clearRecentSpeechButton.Click += (_, _) => ClearRecentSpeechHistory();

        _voiceModeCommandsOnlyRadio = new RadioButton
        {
            AutoSize = true,
            Text = "Commands Only",
            Tag = "Voice mode",
            AccessibleName = "Voice mode commands only",
            AccessibleDescription = "Voice phrases: commands only mode, start command mode, turn off dictation mode."
        };
        _voiceModeCommandsOnlyRadio.CheckedChanged += (_, _) =>
        {
            if (_updatingVoiceModeControls || !_voiceModeCommandsOnlyRadio.Checked)
                return;

            TrySetVoiceAccessMode("commands only");
        };

        _voiceModeDictationOnlyRadio = new RadioButton
        {
            AutoSize = true,
            Text = "Dictation Only",
            Tag = "Voice mode",
            AccessibleName = "Voice mode dictation only",
            AccessibleDescription = "Voice phrases: dictation mode, start dictation mode, typing mode."
        };
        _voiceModeDictationOnlyRadio.CheckedChanged += (_, _) =>
        {
            if (_updatingVoiceModeControls || !_voiceModeDictationOnlyRadio.Checked)
                return;

            TrySetVoiceAccessMode("dictation only");
        };

        _voiceModeDefaultRadio = new RadioButton
        {
            AutoSize = true,
            Text = "Commands + Dictation",
            Tag = "Voice mode",
            AccessibleName = "Voice mode default",
            AccessibleDescription = "Voice phrases: default mode, commands and dictation mode, commands plus dictation mode."
        };
        _voiceModeDefaultRadio.CheckedChanged += (_, _) =>
        {
            if (_updatingVoiceModeControls || !_voiceModeDefaultRadio.Checked)
                return;

            TrySetVoiceAccessMode("default");
        };

        var voiceModeButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        voiceModeButtons.AccessibleName = "Voice mode chooser";
        voiceModeButtons.AccessibleDescription = "Visible voice mode controls for commands only, dictation only, or commands plus dictation.";
        voiceModeButtons.Controls.Add(_voiceModeCommandsOnlyRadio);
        voiceModeButtons.Controls.Add(_voiceModeDictationOnlyRadio);
        voiceModeButtons.Controls.Add(_voiceModeDefaultRadio);
        SyncVoiceModeControls();

        _rehearsePhraseButton = new Button { Text = "Test Phrase Launch", Width = 150, AccessibleName = "Session test phrase launch", AccessibleDescription = "Voice phrase: test phrase launch." };
        _rehearsePhraseButton.Click += (_, _) => RehearseVoicePhrase();

        _wakeButton = new Button { Text = "Wake Word", Width = 120, AccessibleName = "Session wake word", AccessibleDescription = "Voice phrase: wake word." };
        _wakeButton.Click += (_, _) => WakeSession();

        _verifyButton = new Button { Text = "Verify Callsign", Width = 140, AccessibleName = "Session verify callsign", AccessibleDescription = "Voice phrase: verify callsign." };
        _verifyButton.Click += (_, _) => VerifyIdentity();

        _captureButton = new Button { Text = "Capture Command", Width = 150, AccessibleName = "Session capture command", AccessibleDescription = "Voice phrase: capture command." };
        _captureButton.Click += (_, _) => CaptureCommand();

        _launchButton = new Button { Text = "Launch via Start Menu", Width = 170, AccessibleName = "Session launch via Start menu", AccessibleDescription = "Voice phrase: launch via Start menu." };
        _launchButton.Click += (_, _) => LaunchAppFromStartMenu();

        _confirmAppCandidateButton = new Button { Text = "Confirm App", Width = 120, Enabled = false, AccessibleName = "Session confirm app", AccessibleDescription = "Voice phrases: confirm app, 1, click 1, choose result one." };
        _confirmAppCandidateButton.Click += (_, _) => ConfirmSelectedAppCandidate();

        _clearAppCandidateButton = new Button { Text = "Clear App Choices", Width = 140, Enabled = false, AccessibleName = "Session clear app choices", AccessibleDescription = "Voice phrases: clear app choices, cancel app choices." };
        _clearAppCandidateButton.Click += (_, _) => ClearPendingAppConfirmation("App confirmation cleared.");

        _cancelButton = new Button { Text = "Cancel", Width = 100, AccessibleName = "Session cancel", AccessibleDescription = "Voice phrase: cancel." };
        _cancelButton.Click += (_, _) => CancelSession();

        _resetSessionButton = new Button { Text = "Reset Session", Width = 120, AccessibleName = "Session reset", AccessibleDescription = "Voice phrase: reset session." };
        _resetSessionButton.Click += (_, _) => ResetSession();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, examples, row++);
        AddRow(layout, "Launch test phrase", _voicePhraseText, row++);
        AddFullWidth(layout, _sessionPhaseLabel, row++);
        AddFullWidth(layout, _sessionNextActionLabel, row++);
        AddFullWidth(layout, _sessionHintLabel, row++);
        AddRow(layout, "Listener", _listeningStateLabel, row++);
        AddRow(layout, "Last heard", _lastHeardLabel, row++);
        AddRow(layout, "Wake detector", _wakeReliabilityLabel, row++);
        AddRow(layout, "Wake candidate", _wakeCandidateLabel, row++);
        AddRow(layout, "Wake score", _wakeScoreLabel, row++);
        AddRow(layout, "Audio quality", _wakeQualityLabel, row++);
        AddRow(layout, "Runtime owner", _runtimeOwnerLabel, row++);
        AddRow(layout, "Runtime proof", _runtimeProofLabel, row++);
        AddRow(layout, "Mic level", _micLevelLabel, row++);
        AddRow(layout, "Mic details", _micDetailLabel, row++);
        AddRow(layout, "Voice mode", voiceModeButtons, row++);
        AddRow(layout, "Spoken callsign", _spokenCallsignText, row++);
        AddRow(layout, "Spoken command", _spokenCommandText, row++);
        AddRow(layout, "App to launch", _appNameText, row++);
        AddFullWidth(layout, _appCandidateHintLabel, row++);
        AddFullWidth(layout, _appCandidateList, row++);
        AddRow(layout, "State", _sessionStateLabel, row++);
        AddRow(layout, "Identity", _sessionIdentityLabel, row++);
        AddRow(layout, "Command", _sessionCommandLabel, row++);
        AddRow(layout, "Timeout", _sessionCountdownLabel, row++);
        AddRow(layout, "Result", _sessionResultLabel, row++);
        AddRow(layout, "Speech cue", _sessionSpeechCueLabel, row++);
        AddFullWidth(layout, _sessionTranscriptHistoryLabel, row++);
        AddFullWidth(layout, _sessionTranscriptHistoryList, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_startListeningButton);
        buttons.Controls.Add(_stopListeningButton);
        buttons.Controls.Add(_readStatusButton);
        buttons.Controls.Add(_stopStatusReadbackButton);
        buttons.Controls.Add(_clearRecentSpeechButton);
        buttons.Controls.Add(_rehearsePhraseButton);
        buttons.Controls.Add(_wakeButton);
        buttons.Controls.Add(_verifyButton);
        buttons.Controls.Add(_captureButton);
        buttons.Controls.Add(_launchButton);
        buttons.Controls.Add(_confirmAppCandidateButton);
        buttons.Controls.Add(_clearAppCandidateButton);
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_resetSessionButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildDictationTab()
    {
        var tab = new TabPage("Dictation");
        var layout = BuildTwoColumnLayout(14);

        var heading = CreateHeading("Dictate text, review it, then copy or paste it");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Dictation captures speech and exposes the transcribed text in a visible box. Use Start Dictation to begin listening, then stop or paste the result into the active app when you are ready."
        };
        _dictationHintLabel = CreateStatusLabel("Dictation is visible first: the app shows what it heard, the last error, and whether listening is active.");
        _dictationSafetyLabel = CreateStatusLabel("Safety: dictated text stays in Callsign's review buffer until you copy or paste it. Paste into sensitive targets is blocked; readback is local and stop reading leaves text unchanged.");
        _dictationSafetyLabel.AccessibleName = "Dictation review safety";
        _dictationSafetyLabel.AccessibleDescription = "Explains that dictated text stays in the review buffer until copy or paste, paste is blocked for sensitive targets, and readback is local.";
        _dictationStatusLabel = new Label
        {
            AutoSize = true,
            Text = "Dictation is stopped."
        };
        _dictationSpeechCueLabel = CreateStatusLabel("Speech cue: dictation is stopped.");
        _dictationLastHeardLabel = CreateStatusLabel("Last heard: nothing yet.");
        _dictationTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 180,
            AccessibleName = "Dictated text",
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        _dictationTextBox.TextChanged += (_, _) => RefreshDictationPanel();

        _dictationHistoryLabel = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Recent speech"
        };
        _dictationHistoryList = CreateShellListBox(110);
        _dictationHistoryList.AccessibleName = "Recent dictated speech";

        _startDictationButton = new Button { Text = "Start Dictation", Width = 130, AccessibleName = "Dictation start", AccessibleDescription = "Voice phrase: start dictation." };
        _startDictationButton.Click += (_, _) => StartDictation();

        _stopDictationButton = new Button { Text = "Stop Dictation", Width = 120, AccessibleName = "Dictation stop", AccessibleDescription = "Voice phrase: stop dictation." };
        _stopDictationButton.Click += (_, _) => StopDictation();

        _copyDictationButton = new Button { Text = "Copy Text", Width = 110, AccessibleName = "Dictation copy text", AccessibleDescription = "Voice phrase: copy dictated text." };
        _copyDictationButton.Click += (_, _) => CopyDictationText();

        _readDictationButton = new Button { Text = "Read Aloud", Width = 110, AccessibleName = "Dictation read aloud", AccessibleDescription = "Voice phrases: read dictation, read that back." };
        _readDictationButton.Click += (_, _) => ReadDictationTextAloud();

        _stopDictationReadbackButton = new Button { Text = "Stop Reading", Width = 120, AccessibleName = "Dictation stop reading", AccessibleDescription = "Voice phrases: stop reading, stop readback." };
        _stopDictationReadbackButton.Click += (_, _) => StopDictationReadback();

        _pasteDictationButton = new Button { Text = "Paste Into Active App", Width = 180, AccessibleName = "Dictation paste into active app", AccessibleDescription = "Voice phrase: paste dictated text." };
        _pasteDictationButton.Click += (_, _) => PasteDictationText();

        _clearDictationButton = new Button { Text = "Clear", Width = 90, AccessibleName = "Dictation clear", AccessibleDescription = "Voice phrase: clear dictation." };
        _clearDictationButton.Click += (_, _) => ClearDictationText();

        _cutDictationButton = new Button { Text = "Cut Text", Width = 100, AccessibleName = "Dictation cut text", AccessibleDescription = "Voice phrase: cut dictated text." };
        _cutDictationButton.Click += (_, _) => CutDictationText();

        _undoDictationButton = new Button { Text = "Undo", Width = 90, AccessibleName = "Dictation undo", AccessibleDescription = "Voice phrase: undo dictation edit." };
        _undoDictationButton.Click += (_, _) => UndoDictationText();

        _redoDictationButton = new Button { Text = "Redo", Width = 90, AccessibleName = "Dictation redo", AccessibleDescription = "Voice phrase: redo dictation edit." };
        _redoDictationButton.Click += (_, _) => RedoDictationText();

        _goToStartDictationButton = new Button { Text = "Go To Start", Width = 105, AccessibleName = "Dictation go to start", AccessibleDescription = "Voice phrase: go to start." };
        _goToStartDictationButton.Click += (_, _) => GoToStartDictationText();

        _goToEndDictationButton = new Button { Text = "Go To End", Width = 100, AccessibleName = "Dictation go to end", AccessibleDescription = "Voice phrase: go to end." };
        _goToEndDictationButton.Click += (_, _) => GoToEndDictationText();

        _selectToStartDictationButton = new Button { Text = "Select To Start", Width = 125, AccessibleName = "Dictation select to start", AccessibleDescription = "Voice phrase: select to start." };
        _selectToStartDictationButton.Click += (_, _) => SelectToStartDictationText();

        _selectToEndDictationButton = new Button { Text = "Select To End", Width = 115, AccessibleName = "Dictation select to end", AccessibleDescription = "Voice phrase: select to end." };
        _selectToEndDictationButton.Click += (_, _) => SelectToEndDictationText();

        _deleteToStartDictationButton = new Button { Text = "Delete To Start", Width = 125, AccessibleName = "Dictation delete to start", AccessibleDescription = "Voice phrase: delete to start." };
        _deleteToStartDictationButton.Click += (_, _) => DeleteToStartDictationText();

        _deleteToEndDictationButton = new Button { Text = "Delete To End", Width = 115, AccessibleName = "Dictation delete to end", AccessibleDescription = "Voice phrase: delete to end." };
        _deleteToEndDictationButton.Click += (_, _) => DeleteToEndDictationText();

        _goToLineStartDictationButton = new Button { Text = "Line Start", Width = 95, AccessibleName = "Dictation line start", AccessibleDescription = "Voice phrase: go to line start." };
        _goToLineStartDictationButton.Click += (_, _) => GoToLineStartDictationText();

        _goToLineEndDictationButton = new Button { Text = "Line End", Width = 90, AccessibleName = "Dictation line end", AccessibleDescription = "Voice phrase: go to line end." };
        _goToLineEndDictationButton.Click += (_, _) => GoToLineEndDictationText();

        _selectToLineStartDictationButton = new Button { Text = "Select To Line Start", Width = 150, AccessibleName = "Dictation select to line start", AccessibleDescription = "Voice phrase: select to line start." };
        _selectToLineStartDictationButton.Click += (_, _) => SelectToLineStartDictationText();

        _selectToLineEndDictationButton = new Button { Text = "Select To Line End", Width = 145, AccessibleName = "Dictation select to line end", AccessibleDescription = "Voice phrase: select to line end." };
        _selectToLineEndDictationButton.Click += (_, _) => SelectToLineEndDictationText();

        _deleteToLineStartDictationButton = new Button { Text = "Delete To Line Start", Width = 150, AccessibleName = "Dictation delete to line start", AccessibleDescription = "Voice phrase: delete to line start." };
        _deleteToLineStartDictationButton.Click += (_, _) => DeleteToLineStartDictationText();

        _deleteToLineEndDictationButton = new Button { Text = "Delete To Line End", Width = 145, AccessibleName = "Dictation delete to line end", AccessibleDescription = "Voice phrase: delete to line end." };
        _deleteToLineEndDictationButton.Click += (_, _) => DeleteToLineEndDictationText();

        _goToParagraphStartDictationButton = new Button { Text = "Paragraph Start", Width = 120, AccessibleName = "Dictation paragraph start", AccessibleDescription = "Voice phrase: go to paragraph start." };
        _goToParagraphStartDictationButton.Click += (_, _) => GoToParagraphStartDictationText();

        _goToParagraphEndDictationButton = new Button { Text = "Paragraph End", Width = 110, AccessibleName = "Dictation paragraph end", AccessibleDescription = "Voice phrase: go to paragraph end." };
        _goToParagraphEndDictationButton.Click += (_, _) => GoToParagraphEndDictationText();

        _selectToParagraphStartDictationButton = new Button { Text = "Select To Paragraph Start", Width = 175, AccessibleName = "Dictation select to paragraph start", AccessibleDescription = "Voice phrase: select to paragraph start." };
        _selectToParagraphStartDictationButton.Click += (_, _) => SelectToParagraphStartDictationText();

        _selectToParagraphEndDictationButton = new Button { Text = "Select To Paragraph End", Width = 170, AccessibleName = "Dictation select to paragraph end", AccessibleDescription = "Voice phrase: select to paragraph end." };
        _selectToParagraphEndDictationButton.Click += (_, _) => SelectToParagraphEndDictationText();

        _deleteToParagraphStartDictationButton = new Button { Text = "Delete To Paragraph Start", Width = 180, AccessibleName = "Dictation delete to paragraph start", AccessibleDescription = "Voice phrase: delete to paragraph start." };
        _deleteToParagraphStartDictationButton.Click += (_, _) => DeleteToParagraphStartDictationText();

        _deleteToParagraphEndDictationButton = new Button { Text = "Delete To Paragraph End", Width = 175, AccessibleName = "Dictation delete to paragraph end", AccessibleDescription = "Voice phrase: delete to paragraph end." };
        _deleteToParagraphEndDictationButton.Click += (_, _) => DeleteToParagraphEndDictationText();

        _replacePreviousParagraphDictationButton = new Button { Text = "Replace Prev Paragraph", Width = 170, AccessibleName = "Dictation replace previous paragraph", AccessibleDescription = "Voice phrase: replace previous paragraph with ..." };
        _replacePreviousParagraphDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous paragraph with ...' to apply a paragraph replacement.");

        _newlineDictationButton = new Button { Text = "New Line", Width = 95, AccessibleName = "Dictation new line", AccessibleDescription = "Voice phrase: new line." };
        _newlineDictationButton.Click += (_, _) => InsertDictationLineBreak();

        _paragraphDictationButton = new Button { Text = "New Paragraph", Width = 120, AccessibleName = "Dictation new paragraph", AccessibleDescription = "Voice phrase: new paragraph." };
        _paragraphDictationButton.Click += (_, _) => InsertDictationParagraphBreak();

        _deleteWordDictationButton = new Button { Text = "Delete Word", Width = 110, AccessibleName = "Dictation delete word", AccessibleDescription = "Voice phrase: delete word." };
        _deleteWordDictationButton.Click += (_, _) => DeleteLastDictationWord();

        _goToPreviousWordDictationButton = new Button { Text = "Prev Word", Width = 100, AccessibleName = "Dictation previous word", AccessibleDescription = "Voice phrase: go to previous word." };
        _goToPreviousWordDictationButton.Click += (_, _) => GoToPreviousDictationWord();

        _goToNextWordDictationButton = new Button { Text = "Next Word", Width = 100, AccessibleName = "Dictation next word", AccessibleDescription = "Voice phrase: go to next word." };
        _goToNextWordDictationButton.Click += (_, _) => GoToNextDictationWord();

        _selectPreviousWordDictationButton = new Button { Text = "Select Prev Word", Width = 130, AccessibleName = "Dictation select previous word", AccessibleDescription = "Voice phrase: select previous word." };
        _selectPreviousWordDictationButton.Click += (_, _) => SelectPreviousDictationWord();

        _selectNextWordDictationButton = new Button { Text = "Select Next Word", Width = 130, AccessibleName = "Dictation select next word", AccessibleDescription = "Voice phrase: select next word." };
        _selectNextWordDictationButton.Click += (_, _) => SelectNextDictationWord();

        _deletePreviousWordDictationButton = new Button { Text = "Delete Prev Word", Width = 130, AccessibleName = "Dictation delete previous word", AccessibleDescription = "Voice phrase: delete previous word." };
        _deletePreviousWordDictationButton.Click += (_, _) => DeletePreviousDictationWord();

        _deleteNextWordDictationButton = new Button { Text = "Delete Next Word", Width = 130, AccessibleName = "Dictation delete next word", AccessibleDescription = "Voice phrase: delete next word." };
        _deleteNextWordDictationButton.Click += (_, _) => DeleteNextDictationWord();

        _goToPreviousSentenceDictationButton = new Button { Text = "Prev Sentence", Width = 125, AccessibleName = "Dictation previous sentence", AccessibleDescription = "Voice phrase: go to previous sentence." };
        _goToPreviousSentenceDictationButton.Click += (_, _) => GoToPreviousDictationSentence();

        _goToNextSentenceDictationButton = new Button { Text = "Next Sentence", Width = 125, AccessibleName = "Dictation next sentence", AccessibleDescription = "Voice phrase: go to next sentence." };
        _goToNextSentenceDictationButton.Click += (_, _) => GoToNextDictationSentence();

        _selectPreviousSentenceDictationButton = new Button { Text = "Select Prev Sentence", Width = 145, AccessibleName = "Dictation select previous sentence", AccessibleDescription = "Voice phrase: select previous sentence." };
        _selectPreviousSentenceDictationButton.Click += (_, _) => SelectPreviousDictationSentence();

        _selectNextSentenceDictationButton = new Button { Text = "Select Next Sentence", Width = 145, AccessibleName = "Dictation select next sentence", AccessibleDescription = "Voice phrase: select next sentence." };
        _selectNextSentenceDictationButton.Click += (_, _) => SelectNextDictationSentence();

        _deletePreviousSentenceDictationButton = new Button { Text = "Delete Prev Sentence", Width = 150, AccessibleName = "Dictation delete previous sentence", AccessibleDescription = "Voice phrase: delete previous sentence." };
        _deletePreviousSentenceDictationButton.Click += (_, _) => DeletePreviousDictationSentence();

        _deleteNextSentenceDictationButton = new Button { Text = "Delete Next Sentence", Width = 150, AccessibleName = "Dictation delete next sentence", AccessibleDescription = "Voice phrase: delete next sentence." };
        _deleteNextSentenceDictationButton.Click += (_, _) => DeleteNextDictationSentence();

        _goToPreviousParagraphDictationButton = new Button { Text = "Prev Paragraph", Width = 125, AccessibleName = "Dictation previous paragraph", AccessibleDescription = "Voice phrase: go to previous paragraph." };
        _goToPreviousParagraphDictationButton.Click += (_, _) => GoToPreviousDictationParagraph();

        _goToNextParagraphDictationButton = new Button { Text = "Next Paragraph", Width = 125, AccessibleName = "Dictation next paragraph", AccessibleDescription = "Voice phrase: go to next paragraph." };
        _goToNextParagraphDictationButton.Click += (_, _) => GoToNextDictationParagraph();

        _replacePreviousWordDictationButton = new Button { Text = "Replace Prev Word", Width = 140, AccessibleName = "Dictation replace previous word", AccessibleDescription = "Voice phrase: replace previous word with ..." };
        _replacePreviousWordDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous word with ...' to apply a voice replacement.");

        _replacePreviousSentenceDictationButton = new Button { Text = "Replace Prev Sentence", Width = 160, AccessibleName = "Dictation replace previous sentence", AccessibleDescription = "Voice phrase: replace previous sentence with ..." };
        _replacePreviousSentenceDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous sentence with ...' to apply a voice replacement.");

        _replaceAllDictationButton = new Button { Text = "Replace All", Width = 105, AccessibleName = "Dictation replace all", AccessibleDescription = "Voice phrase: replace all with ..." };
        _replaceAllDictationButton.Click += (_, _) => UpdateStatus("Say 'replace all with ...' to replace the entire dictated text.");

        _commaDictationButton = new Button { Text = "Comma", Width = 80, AccessibleName = "Dictation comma", AccessibleDescription = "Voice phrase: comma." };
        _commaDictationButton.Click += (_, _) => InsertDictationPunctuation(", ");

        _periodDictationButton = new Button { Text = "Period", Width = 80, AccessibleName = "Dictation period", AccessibleDescription = "Voice phrases: period, full stop." };
        _periodDictationButton.Click += (_, _) => InsertDictationPunctuation(". ");

        _questionDictationButton = new Button { Text = "Question", Width = 90, AccessibleName = "Dictation question mark", AccessibleDescription = "Voice phrase: question mark." };
        _questionDictationButton.Click += (_, _) => InsertDictationPunctuation("? ");

        _exclamationDictationButton = new Button { Text = "Exclaim", Width = 90, AccessibleName = "Dictation exclamation mark", AccessibleDescription = "Voice phrases: exclamation, exclamation mark, exclamation point." };
        _exclamationDictationButton.Click += (_, _) => InsertDictationPunctuation("! ");

        _semicolonDictationButton = new Button { Text = "Semicolon", Width = 95, AccessibleName = "Dictation semicolon", AccessibleDescription = "Voice phrases: semicolon, semi colon." };
        _semicolonDictationButton.Click += (_, _) => InsertDictationPunctuation("; ");

        _colonDictationButton = new Button { Text = "Colon", Width = 80, AccessibleName = "Dictation colon", AccessibleDescription = "Voice phrase: colon." };
        _colonDictationButton.Click += (_, _) => InsertDictationPunctuation(": ");

        _apostropheDictationButton = new Button { Text = "Apostrophe", Width = 100, AccessibleName = "Dictation apostrophe", AccessibleDescription = "Voice phrase: apostrophe." };
        _apostropheDictationButton.Click += (_, _) => InsertDictationPunctuation("'");

        _quoteDictationButton = new Button { Text = "Quote", Width = 80, AccessibleName = "Dictation quote", AccessibleDescription = "Voice phrase: quote." };
        _quoteDictationButton.Click += (_, _) => InsertDictationPunctuation("\"");

        _openParenthesisDictationButton = new Button { Text = "Open (", Width = 80, AccessibleName = "Dictation open parenthesis", AccessibleDescription = "Voice phrases: open parenthesis, open parentheses." };
        _openParenthesisDictationButton.Click += (_, _) => InsertDictationPunctuation("(");

        _closeParenthesisDictationButton = new Button { Text = "Close )", Width = 80, AccessibleName = "Dictation close parenthesis", AccessibleDescription = "Voice phrases: close parenthesis, close parentheses." };
        _closeParenthesisDictationButton.Click += (_, _) => InsertDictationPunctuation(")");

        _hyphenDictationButton = new Button { Text = "Hyphen", Width = 80, AccessibleName = "Dictation hyphen", AccessibleDescription = "Voice phrase: hyphen." };
        _hyphenDictationButton.Click += (_, _) => InsertDictationPunctuation("-");

        _dashDictationButton = new Button { Text = "Dash", Width = 80, AccessibleName = "Dictation dash", AccessibleDescription = "Voice phrase: dash." };
        _dashDictationButton.Click += (_, _) => InsertDictationPunctuation(" - ");

        _slashDictationButton = new Button { Text = "Slash", Width = 80, AccessibleName = "Dictation slash", AccessibleDescription = "Voice phrase: slash." };
        _slashDictationButton.Click += (_, _) => InsertDictationPunctuation("/");

        _atSignDictationButton = new Button { Text = "At", Width = 70, AccessibleName = "Dictation at sign", AccessibleDescription = "Voice phrase: at sign." };
        _atSignDictationButton.Click += (_, _) => InsertDictationPunctuation("@");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _dictationHintLabel, row++);
        AddFullWidth(layout, _dictationSafetyLabel, row++);
        AddRow(layout, "Dictation status", _dictationStatusLabel, row++);
        AddRow(layout, "Speech cue", _dictationSpeechCueLabel, row++);
        AddRow(layout, "Last heard", _dictationLastHeardLabel, row++);
        AddRow(layout, "Dictated text", _dictationTextBox, row++);
        AddFullWidth(layout, _dictationHistoryLabel, row++);
        AddFullWidth(layout, _dictationHistoryList, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_startDictationButton);
        buttons.Controls.Add(_stopDictationButton);
        buttons.Controls.Add(_copyDictationButton);
        buttons.Controls.Add(_readDictationButton);
        buttons.Controls.Add(_stopDictationReadbackButton);
        buttons.Controls.Add(_pasteDictationButton);
        buttons.Controls.Add(_clearDictationButton);
        buttons.Controls.Add(_cutDictationButton);
        buttons.Controls.Add(_undoDictationButton);
        buttons.Controls.Add(_redoDictationButton);
        buttons.Controls.Add(_goToStartDictationButton);
        buttons.Controls.Add(_goToEndDictationButton);
        buttons.Controls.Add(_selectToStartDictationButton);
        buttons.Controls.Add(_selectToEndDictationButton);
        buttons.Controls.Add(_deleteToStartDictationButton);
        buttons.Controls.Add(_deleteToEndDictationButton);
        buttons.Controls.Add(_goToLineStartDictationButton);
        buttons.Controls.Add(_goToLineEndDictationButton);
        buttons.Controls.Add(_selectToLineStartDictationButton);
        buttons.Controls.Add(_selectToLineEndDictationButton);
        buttons.Controls.Add(_deleteToLineStartDictationButton);
        buttons.Controls.Add(_deleteToLineEndDictationButton);
        buttons.Controls.Add(_goToParagraphStartDictationButton);
        buttons.Controls.Add(_goToParagraphEndDictationButton);
        buttons.Controls.Add(_selectToParagraphStartDictationButton);
        buttons.Controls.Add(_selectToParagraphEndDictationButton);
        buttons.Controls.Add(_deleteToParagraphStartDictationButton);
        buttons.Controls.Add(_deleteToParagraphEndDictationButton);
        buttons.Controls.Add(_replacePreviousParagraphDictationButton);
        buttons.Controls.Add(_newlineDictationButton);
        buttons.Controls.Add(_paragraphDictationButton);
        buttons.Controls.Add(_deleteWordDictationButton);
        buttons.Controls.Add(_goToPreviousWordDictationButton);
        buttons.Controls.Add(_goToNextWordDictationButton);
        buttons.Controls.Add(_selectPreviousWordDictationButton);
        buttons.Controls.Add(_selectNextWordDictationButton);
        buttons.Controls.Add(_deletePreviousWordDictationButton);
        buttons.Controls.Add(_deleteNextWordDictationButton);
        buttons.Controls.Add(_goToPreviousSentenceDictationButton);
        buttons.Controls.Add(_goToNextSentenceDictationButton);
        buttons.Controls.Add(_selectPreviousSentenceDictationButton);
        buttons.Controls.Add(_selectNextSentenceDictationButton);
        buttons.Controls.Add(_deletePreviousSentenceDictationButton);
        buttons.Controls.Add(_deleteNextSentenceDictationButton);
        buttons.Controls.Add(_goToPreviousParagraphDictationButton);
        buttons.Controls.Add(_goToNextParagraphDictationButton);
        buttons.Controls.Add(_replacePreviousWordDictationButton);
        buttons.Controls.Add(_replacePreviousSentenceDictationButton);
        buttons.Controls.Add(_replaceAllDictationButton);
        buttons.Controls.Add(_commaDictationButton);
        buttons.Controls.Add(_periodDictationButton);
        buttons.Controls.Add(_questionDictationButton);
        buttons.Controls.Add(_exclamationDictationButton);
        buttons.Controls.Add(_semicolonDictationButton);
        buttons.Controls.Add(_colonDictationButton);
        buttons.Controls.Add(_apostropheDictationButton);
        buttons.Controls.Add(_quoteDictationButton);
        buttons.Controls.Add(_openParenthesisDictationButton);
        buttons.Controls.Add(_closeParenthesisDictationButton);
        buttons.Controls.Add(_hyphenDictationButton);
        buttons.Controls.Add(_dashDictationButton);
        buttons.Controls.Add(_slashDictationButton);
        buttons.Controls.Add(_atSignDictationButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildBrowserTab()
    {
        var tab = new TabPage("Browser");
        var layout = BuildTwoColumnLayout(12);

        var heading = CreateHeading("Open a website or search the web");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Enter a URL or a search phrase. Callsign will open the default browser and hand the browser a visible target or web search."
        };
        _browserSafetyLabel = CreateStatusLabel("Safety: browser targets are web-only. Callsign accepts http/https, bare domains, and search text; file, script, settings, installer, and app schemes are blocked here. Browser commands use visible shortcuts and do not inspect page contents or run hidden scripts.");
        _browserSafetyLabel.AccessibleName = "Browser safety";
        _browserSafetyLabel.AccessibleDescription = "Explains the web-only browser boundary, blocked non-web schemes, visible shortcut behavior, and no hidden page inspection.";
        _browserStatusLabel = new Label { AutoSize = true, Text = "Browser target not opened yet." };
        _browserVoiceCueLabel = new Label { AutoSize = true, Text = "Voice cue: browser is waiting for speech." };
        _browserLastHeardLabel = new Label { AutoSize = true, Text = "Last heard: nothing yet." };
        _browserLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _browserInputText = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search the web or open a URL, such as example.com or callsign desktop assistant", AccessibleName = "Browser target" };
        _browserInputText.TextChanged += (_, _) => RefreshBrowserPanel();
        _browserAddressTextInput = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Type text into the current browser address bar, such as example.com", AccessibleName = "Browser address bar text" };
        _browserAddressTextInput.TextChanged += (_, _) => RefreshBrowserPanel();
        _browserFindTextInput = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Find text on the current page, such as privacy policy", AccessibleName = "Browser find text" };
        _browserFindTextInput.TextChanged += (_, _) => RefreshBrowserPanel();

        _openBrowserButton = new Button { Text = "Open / Search", Width = 130, AccessibleName = "Browser open or search", AccessibleDescription = "Voice phrases: browser open, browser search, open website." };
        _openBrowserButton.Click += (_, _) => OpenBrowserTarget();

        _searchBrowserButton = new Button { Text = "Search Web", Width = 110, AccessibleName = "Browser search web", AccessibleDescription = "Voice phrase: search the web." };
        _searchBrowserButton.Click += (_, _) => OpenBrowserTarget(forceSearch: true);

        _copyBrowserTargetButton = new Button { Text = "Copy Target", Width = 110, AccessibleName = "Browser copy target", AccessibleDescription = "Voice phrase: copy browser target." };
        _copyBrowserTargetButton.Click += (_, _) => CopyBrowserTarget();

        _browserBackButton = new Button { Text = "Back", Width = 80, AccessibleName = "Browser back", AccessibleDescription = "Voice phrase: browser back." };
        _browserBackButton.Click += (_, _) => ExecuteBrowserAction("browser-back", "Browser back requested.");

        _browserForwardButton = new Button { Text = "Forward", Width = 80, AccessibleName = "Browser forward", AccessibleDescription = "Voice phrase: browser forward." };
        _browserForwardButton.Click += (_, _) => ExecuteBrowserAction("browser-forward", "Browser forward requested.");

        _browserRefreshButton = new Button { Text = "Refresh", Width = 80, AccessibleName = "Browser refresh", AccessibleDescription = "Voice phrase: browser refresh." };
        _browserRefreshButton.Click += (_, _) => ExecuteBrowserAction("browser-refresh", "Browser refresh requested.");

        _browserNewTabButton = new Button { Text = "New Tab", Width = 90, AccessibleName = "Browser new tab", AccessibleDescription = "Voice phrase: browser new tab." };
        _browserNewTabButton.Click += (_, _) => ExecuteBrowserAction("browser-new-tab", "Browser new tab requested.");

        _browserNewWindowButton = new Button { Text = "New Window", Width = 100, AccessibleName = "Browser new window", AccessibleDescription = "Voice phrase: browser new window." };
        _browserNewWindowButton.Click += (_, _) => ExecuteBrowserAction("browser-new-window", "Browser new window requested.");

        _browserPrivateWindowButton = new Button { Text = "Private Window", Width = 120, AccessibleName = "Browser private window", AccessibleDescription = "Voice phrases: browser private window, browser incognito." };
        _browserPrivateWindowButton.Click += (_, _) => ExecuteBrowserAction("browser-private-window", "Browser private window requested.");

        _browserBookmarkPageButton = new Button { Text = "Bookmark Page", Width = 120, AccessibleName = "Browser bookmark page", AccessibleDescription = "Voice phrase: browser bookmark page." };
        _browserBookmarkPageButton.Click += (_, _) => ExecuteBrowserAction("browser-bookmark-page", "Browser bookmark page requested.");

        _browserOpenBookmarksButton = new Button { Text = "Bookmarks", Width = 100, AccessibleName = "Browser bookmarks", AccessibleDescription = "Voice phrase: browser bookmarks." };
        _browserOpenBookmarksButton.Click += (_, _) => ExecuteBrowserAction("browser-open-bookmarks", "Browser bookmarks requested.");

        _browserSavePageButton = new Button { Text = "Save Page", Width = 100, AccessibleName = "Browser save page", AccessibleDescription = "Voice phrase: browser save page." };
        _browserSavePageButton.Click += (_, _) => ExecuteBrowserAction("browser-save-page", "Browser save page requested.");

        _browserPrintPageButton = new Button { Text = "Print Page", Width = 100, AccessibleName = "Browser print page", AccessibleDescription = "Voice phrase: browser print page." };
        _browserPrintPageButton.Click += (_, _) => ExecuteBrowserAction("browser-print-page", "Browser print page requested.");

        _browserNextTabButton = new Button { Text = "Next Tab", Width = 90, AccessibleName = "Browser next tab", AccessibleDescription = "Voice phrase: browser next tab." };
        _browserNextTabButton.Click += (_, _) => ExecuteBrowserAction("browser-next-tab", "Browser next tab requested.");

        _browserPreviousTabButton = new Button { Text = "Previous Tab", Width = 110, AccessibleName = "Browser previous tab", AccessibleDescription = "Voice phrase: browser previous tab." };
        _browserPreviousTabButton.Click += (_, _) => ExecuteBrowserAction("browser-previous-tab", "Browser previous tab requested.");

        _browserCloseTabButton = new Button { Text = "Close Tab", Width = 90, AccessibleName = "Browser close tab", AccessibleDescription = "Voice phrases: browser close tab, close browser tab." };
        _browserCloseTabButton.Click += (_, _) => ExecuteBrowserAction("browser-close-tab", "Browser close tab requested.");

        _browserReopenClosedTabButton = new Button { Text = "Reopen Tab", Width = 105, AccessibleName = "Browser reopen closed tab", AccessibleDescription = "Voice phrases: reopen closed tab, undo close tab." };
        _browserReopenClosedTabButton.Click += (_, _) => ExecuteBrowserAction("browser-reopen-closed-tab", "Browser reopen closed tab requested.");

        _browserAddressBarButton = new Button { Text = "Address Bar", Width = 100, AccessibleName = "Browser focus address bar", AccessibleDescription = "Voice phrase: browser focus address bar." };
        _browserAddressBarButton.Click += (_, _) => ExecuteBrowserAction("browser-focus-address-bar", "Browser address bar requested.");

        _browserAddressTextButton = new Button { Text = "Send Address Text", Width = 130, AccessibleName = "Browser send address bar text", AccessibleDescription = "Voice phrases: type in address bar example.com, search address bar for callsign." };
        _browserAddressTextButton.Click += (_, _) => SendBrowserAddressText();

        _browserHomeButton = new Button { Text = "Home", Width = 80, AccessibleName = "Browser home", AccessibleDescription = "Voice phrase: browser home." };
        _browserHomeButton.Click += (_, _) => ExecuteBrowserAction("browser-home", "Browser home page requested.");

        _browserDownloadsButton = new Button { Text = "Downloads", Width = 95, AccessibleName = "Browser downloads", AccessibleDescription = "Voice phrases: browser downloads, show downloads." };
        _browserDownloadsButton.Click += (_, _) => ExecuteBrowserAction("browser-open-downloads", "Browser downloads requested.");

        _browserHistoryButton = new Button { Text = "History", Width = 85, AccessibleName = "Browser history", AccessibleDescription = "Voice phrases: browser history, show history." };
        _browserHistoryButton.Click += (_, _) => ExecuteBrowserAction("browser-open-history", "Browser history requested.");

        _browserFindButton = new Button { Text = "Find In Page", Width = 110, AccessibleName = "Browser find in page", AccessibleDescription = "Voice phrase: browser find." };
        _browserFindButton.Click += (_, _) => ExecuteBrowserAction("browser-find", "Browser find in page requested.");

        _browserFindTextButton = new Button { Text = "Find Text", Width = 100, AccessibleName = "Browser find page text", AccessibleDescription = "Voice phrases: search this page for privacy policy, find privacy policy on this page." };
        _browserFindTextButton.Click += (_, _) => FindBrowserPageText();

        _browserFindNextButton = new Button { Text = "Find Next", Width = 90, AccessibleName = "Browser find next", AccessibleDescription = "Voice phrase: find next." };
        _browserFindNextButton.Click += (_, _) => ExecuteBrowserAction("browser-find-next", "Browser find next requested.");

        _browserFindPreviousButton = new Button { Text = "Find Previous", Width = 110, AccessibleName = "Browser find previous", AccessibleDescription = "Voice phrase: find previous." };
        _browserFindPreviousButton.Click += (_, _) => ExecuteBrowserAction("browser-find-previous", "Browser find previous requested.");

        _browserStartScrollUpButton = new Button { Text = "Start Up", Width = 90, AccessibleName = "Browser start scrolling up", AccessibleDescription = "Voice phrase: start scrolling up." };
        _browserStartScrollUpButton.Click += (_, _) => ExecuteBrowserAction("browser-start-scroll-up", "Browser start scrolling up requested.");

        _browserStartScrollDownButton = new Button { Text = "Start Down", Width = 100, AccessibleName = "Browser start scrolling down", AccessibleDescription = "Voice phrase: start scrolling down." };
        _browserStartScrollDownButton.Click += (_, _) => ExecuteBrowserAction("browser-start-scroll-down", "Browser start scrolling down requested.");

        _browserStartScrollLeftButton = new Button { Text = "Start Left", Width = 95, AccessibleName = "Browser start scrolling left", AccessibleDescription = "Voice phrase: browser start scrolling left." };
        _browserStartScrollLeftButton.Click += (_, _) => ExecuteBrowserAction("browser-start-scroll-left", "Browser start scrolling left requested.");

        _browserStartScrollRightButton = new Button { Text = "Start Right", Width = 100, AccessibleName = "Browser start scrolling right", AccessibleDescription = "Voice phrase: browser start scrolling right." };
        _browserStartScrollRightButton.Click += (_, _) => ExecuteBrowserAction("browser-start-scroll-right", "Browser start scrolling right requested.");

        _browserStopScrollButton = new Button { Text = "Stop Scroll", Width = 100, AccessibleName = "Browser stop scrolling", AccessibleDescription = "Voice phrase: stop scrolling." };
        _browserStopScrollButton.Click += (_, _) => ExecuteBrowserAction("browser-stop-scroll", "Browser stop scrolling requested.");

        _browserScrollUpButton = new Button { Text = "Scroll Up", Width = 90, AccessibleName = "Browser scroll up", AccessibleDescription = "Voice phrase: browser scroll up." };
        _browserScrollUpButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-up", "Browser scroll up requested.");

        _browserScrollDownButton = new Button { Text = "Scroll Down", Width = 100, AccessibleName = "Browser scroll down", AccessibleDescription = "Voice phrase: browser scroll down." };
        _browserScrollDownButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-down", "Browser scroll down requested.");

        _browserScrollLeftButton = new Button { Text = "Scroll Left", Width = 90, AccessibleName = "Browser scroll left", AccessibleDescription = "Voice phrase: browser scroll left." };
        _browserScrollLeftButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-left", "Browser scroll left requested.");

        _browserScrollRightButton = new Button { Text = "Scroll Right", Width = 100, AccessibleName = "Browser scroll right", AccessibleDescription = "Voice phrase: browser scroll right." };
        _browserScrollRightButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-right", "Browser scroll right requested.");

        _browserScrollTopButton = new Button { Text = "Scroll Top", Width = 95, AccessibleName = "Browser scroll to top", AccessibleDescription = "Voice phrase: scroll to top." };
        _browserScrollTopButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-top", "Browser scroll to top requested.");

        _browserScrollBottomButton = new Button { Text = "Scroll Bottom", Width = 110, AccessibleName = "Browser scroll to bottom", AccessibleDescription = "Voice phrase: scroll to bottom." };
        _browserScrollBottomButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-bottom", "Browser scroll to bottom requested.");

        _browserFullscreenButton = new Button { Text = "Full Screen", Width = 100, AccessibleName = "Browser full screen", AccessibleDescription = "Voice phrase: browser full screen." };
        _browserFullscreenButton.Click += (_, _) => ExecuteBrowserAction("browser-fullscreen", "Browser full screen requested.");

        _browserZoomInButton = new Button { Text = "Zoom In", Width = 80, AccessibleName = "Browser zoom in", AccessibleDescription = "Voice phrase: browser zoom in." };
        _browserZoomInButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-in", "Browser zoom in requested.");

        _browserZoomOutButton = new Button { Text = "Zoom Out", Width = 90, AccessibleName = "Browser zoom out", AccessibleDescription = "Voice phrase: browser zoom out." };
        _browserZoomOutButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-out", "Browser zoom out requested.");

        _browserZoomResetButton = new Button { Text = "Zoom Reset", Width = 100, AccessibleName = "Browser zoom reset", AccessibleDescription = "Voice phrase: browser zoom reset." };
        _browserZoomResetButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-reset", "Browser zoom reset requested.");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _browserSafetyLabel, row++);
        AddRow(layout, "Target", _browserInputText, row++);
        AddRow(layout, "Address text", _browserAddressTextInput, row++);
        AddRow(layout, "Find text", _browserFindTextInput, row++);
        AddRow(layout, "Status", _browserStatusLabel, row++);
        AddRow(layout, "Voice cue", _browserVoiceCueLabel, row++);
        AddRow(layout, "Last heard", _browserLastHeardLabel, row++);
        AddRow(layout, "Last action", _browserLastActionLabel, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_openBrowserButton);
        buttons.Controls.Add(_searchBrowserButton);
        buttons.Controls.Add(_copyBrowserTargetButton);
        buttons.Controls.Add(_browserBackButton);
        buttons.Controls.Add(_browserForwardButton);
        buttons.Controls.Add(_browserRefreshButton);
        buttons.Controls.Add(_browserNewTabButton);
        buttons.Controls.Add(_browserNewWindowButton);
        buttons.Controls.Add(_browserPrivateWindowButton);
        buttons.Controls.Add(_browserBookmarkPageButton);
        buttons.Controls.Add(_browserOpenBookmarksButton);
        buttons.Controls.Add(_browserSavePageButton);
        buttons.Controls.Add(_browserPrintPageButton);
        buttons.Controls.Add(_browserNextTabButton);
        buttons.Controls.Add(_browserPreviousTabButton);
        buttons.Controls.Add(_browserCloseTabButton);
        buttons.Controls.Add(_browserReopenClosedTabButton);
        buttons.Controls.Add(_browserAddressBarButton);
        buttons.Controls.Add(_browserAddressTextButton);
        buttons.Controls.Add(_browserHomeButton);
        buttons.Controls.Add(_browserDownloadsButton);
        buttons.Controls.Add(_browserHistoryButton);
        buttons.Controls.Add(_browserFindButton);
        buttons.Controls.Add(_browserFindTextButton);
        buttons.Controls.Add(_browserFindNextButton);
        buttons.Controls.Add(_browserFindPreviousButton);
        buttons.Controls.Add(_browserStartScrollUpButton);
        buttons.Controls.Add(_browserStartScrollDownButton);
        buttons.Controls.Add(_browserStartScrollLeftButton);
        buttons.Controls.Add(_browserStartScrollRightButton);
        buttons.Controls.Add(_browserStopScrollButton);
        buttons.Controls.Add(_browserScrollUpButton);
        buttons.Controls.Add(_browserScrollDownButton);
        buttons.Controls.Add(_browserScrollLeftButton);
        buttons.Controls.Add(_browserScrollRightButton);
        buttons.Controls.Add(_browserScrollTopButton);
        buttons.Controls.Add(_browserScrollBottomButton);
        buttons.Controls.Add(_browserFullscreenButton);
        buttons.Controls.Add(_browserZoomInButton);
        buttons.Controls.Add(_browserZoomOutButton);
        buttons.Controls.Add(_browserZoomResetButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildSystemTab()
    {
        var tab = new TabPage("System");
        var layout = BuildTwoColumnLayout(8);

        var heading = CreateHeading("Adjust common system controls visibly");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "System controls stay visible and local: change volume, mute audio, control media playback, switch to a named app or window, manage the active window, open shell surfaces such as Quick Settings or clipboard history, open safe Windows Settings pages, open Task Manager, and show the desktop without opening a shell."
        };
        _systemStatusLabel = new Label { AutoSize = true, Text = "No system action run yet." };
        _systemSelectedActionLabel = new Label { AutoSize = true, Text = "Selected action: none." };
        _systemSafetyLabel = CreateStatusLabel("Safety: system commands stay visible and reversible where possible. Settings, clipboard history, snipping, project, cast, keyboard, mouse, and window actions open visible Windows surfaces or send bounded input; Callsign does not toggle settings, read clipboard contents, capture screenshots, force-kill apps, or act in hidden windows from this surface.");
        _systemSafetyLabel.AccessibleName = "System safety";
        _systemSafetyLabel.AccessibleDescription = "Explains that System actions use visible Windows surfaces or bounded input and do not toggle settings, read clipboard contents, capture screenshots, force-kill apps, or act in hidden windows.";
        _systemLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _systemVoiceCueLabel = new Label { AutoSize = true, Text = "Voice cue: system is waiting for speech." };
        _systemLastHeardLabel = new Label { AutoSize = true, Text = "Last heard: nothing yet." };
        _systemSwitchWindowText = BuildTextInput("Switch to app or window");
        _systemSwitchWindowText.PlaceholderText = "Edge, Notepad, or Explorer";
        _systemSwitchWindowText.TextChanged += (_, _) => RefreshSystemPanel();
        _systemWindowChoiceHintLabel = CreateStatusLabel("Window switch: no pending choice.");
        _systemWindowChoiceHintLabel.MaximumSize = new Size(760, 0);
        _systemWindowChoiceList = CreateShellListBox(78);
        _systemWindowChoiceList.AccessibleName = "Window switch choices";
        _systemWindowChoiceList.AccessibleDescription = "Visible numbered window choices for app switching. Voice phrases: 1, click 1, choose window 1, confirm window, next window choice, previous window choice, clear window choices, cancel.";
        _systemWindowChoiceList.Enabled = false;
        _systemWindowChoiceList.SelectedIndexChanged += (_, _) => RefreshPendingWindowSwitchHint();

        _systemVolumeUpButton = new Button { Text = "Volume Up", Width = 90, AccessibleName = "System volume up", AccessibleDescription = "Voice phrase: volume up." };
        _systemVolumeUpButton.Click += (_, _) => ExecuteSystemAction("system-volume-up", "Volume up requested.");

        _systemVolumeDownButton = new Button { Text = "Volume Down", Width = 100, AccessibleName = "System volume down", AccessibleDescription = "Voice phrase: volume down." };
        _systemVolumeDownButton.Click += (_, _) => ExecuteSystemAction("system-volume-down", "Volume down requested.");

        _systemMuteButton = new Button { Text = "Mute", Width = 80, AccessibleName = "System mute volume", AccessibleDescription = "Voice phrases: mute volume, mute audio." };
        _systemMuteButton.Click += (_, _) => ExecuteSystemAction("system-volume-mute", "Volume mute requested.");

        _systemMediaPlayPauseButton = new Button { Text = "Play/Pause", Width = 90, AccessibleName = "System play or pause media", AccessibleDescription = "Voice phrases: play or pause, play media, pause media." };
        _systemMediaPlayPauseButton.Click += (_, _) => ExecuteSystemAction("system-media-play-pause", "Media play/pause requested.");

        _systemMediaNextButton = new Button { Text = "Next Track", Width = 95, AccessibleName = "System next track", AccessibleDescription = "Voice phrase: next track." };
        _systemMediaNextButton.Click += (_, _) => ExecuteSystemAction("system-media-next-track", "Media next track requested.");

        _systemMediaPreviousButton = new Button { Text = "Prev Track", Width = 95, AccessibleName = "System previous track", AccessibleDescription = "Voice phrases: previous track, previous song." };
        _systemMediaPreviousButton.Click += (_, _) => ExecuteSystemAction("system-media-previous-track", "Media previous track requested.");

        _systemMediaStopButton = new Button { Text = "Stop Media", Width = 95, AccessibleName = "System stop media", AccessibleDescription = "Voice phrases: stop media, stop playback." };
        _systemMediaStopButton.Click += (_, _) => ExecuteSystemAction("system-media-stop", "Media stop requested.");

        _systemShowDesktopButton = new Button { Text = "Show Desktop", Width = 110, AccessibleName = "System show desktop", AccessibleDescription = "Voice phrases: show desktop, minimize all windows." };
        _systemShowDesktopButton.Click += (_, _) => ExecuteSystemAction("system-show-desktop", "Show desktop requested.");

        _systemNextWindowButton = new Button { Text = "Next Window", Width = 105, AccessibleName = "System next window", AccessibleDescription = "Voice phrases: next window, switch to the next app." };
        _systemNextWindowButton.Click += (_, _) => ExecuteSystemAction("system-next-window", "Next window requested.");

        _systemPreviousWindowButton = new Button { Text = "Previous Window", Width = 120, AccessibleName = "System previous window", AccessibleDescription = "Voice phrases: previous window, switch to the previous app." };
        _systemPreviousWindowButton.Click += (_, _) => ExecuteSystemAction("system-previous-window", "Previous window requested.");

        _systemTaskViewButton = new Button { Text = "Task View", Width = 90, AccessibleName = "System task view", AccessibleDescription = "Voice phrases: task view, show open windows." };
        _systemTaskViewButton.Click += (_, _) => ExecuteSystemAction("system-open-task-view", "Task view requested.");

        var systemQuickSettingsButton = new Button { Text = "Quick Settings", Width = 110, AccessibleName = "System quick settings", AccessibleDescription = "Voice phrase: quick settings." };
        systemQuickSettingsButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-quick-settings", "Quick Settings requested.");

        var systemNotificationCenterButton = new Button { Text = "Notifications", Width = 105, AccessibleName = "System notification center", AccessibleDescription = "Voice phrases: notification center, show notifications." };
        systemNotificationCenterButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-notification-center", "Notification Center requested.");

        var systemEmojiPanelButton = new Button { Text = "Emoji Panel", Width = 100, AccessibleName = "System emoji panel", AccessibleDescription = "Voice phrases: emoji panel, emoji picker, symbol picker." };
        systemEmojiPanelButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-emoji-panel", "Emoji panel requested.");

        var systemClipboardHistoryButton = new Button { Text = "Clipboard", Width = 85, AccessibleName = "System clipboard history", AccessibleDescription = "Voice phrases: clipboard history, open clipboard, show clipboard picker." };
        systemClipboardHistoryButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-clipboard-history", "Clipboard history requested.");

        var systemSnippingToolbarButton = new Button { Text = "Snipping", Width = 80, AccessibleName = "System snipping toolbar", AccessibleDescription = "Voice phrases: snipping toolbar, show screenshot toolbar, open screenshot tools." };
        systemSnippingToolbarButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-snipping-toolbar", "Snipping toolbar requested.");

        var systemProjectDisplayButton = new Button { Text = "Project", Width = 75, AccessibleName = "System project display", AccessibleDescription = "Voice phrases: project display, display switch." };
        systemProjectDisplayButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-project-display", "Project display requested.");

        var systemCastDisplayButton = new Button { Text = "Cast", Width = 65, AccessibleName = "System cast display", AccessibleDescription = "Voice phrases: cast display, wireless display." };
        systemCastDisplayButton.Click += (_, _) => ExecuteSystemShellSurfaceAction("system-open-cast-display", "Cast display requested.");

        _systemSwitchWindowButton = new Button { Text = "Switch to App", Width = 110, AccessibleName = "System switch to app or window", AccessibleDescription = "Voice phrases: switch to Edge, go to Notepad." };
        _systemSwitchWindowButton.Click += (_, _) => ExecuteSystemAction($"system-switch-window:{_systemSwitchWindowText.Text.Trim()}", "Window switch requested.");

        _systemConfirmWindowChoiceButton = new Button { Text = "Confirm Window", Width = 125, Enabled = false, AccessibleName = "System confirm window choice", AccessibleDescription = "Voice phrases: confirm window, 1, click 1, choose window one." };
        _systemConfirmWindowChoiceButton.Click += (_, _) => ConfirmSelectedWindowSwitchChoice();

        _systemClearWindowChoicesButton = new Button { Text = "Clear Window Choices", Width = 150, Enabled = false, AccessibleName = "System clear window choices", AccessibleDescription = "Voice phrases: clear window choices, cancel window choices." };
        _systemClearWindowChoicesButton.Click += (_, _) => ClearPendingWindowSwitch("Window choice cleared.");

        _systemNewVirtualDesktopButton = new Button { Text = "New Desktop", Width = 105, AccessibleName = "System new virtual desktop", AccessibleDescription = "Voice phrase: new desktop." };
        _systemNewVirtualDesktopButton.Click += (_, _) => ExecuteSystemAction("system-new-virtual-desktop", "New virtual desktop requested.");

        _systemNextVirtualDesktopButton = new Button { Text = "Next Desktop", Width = 105, AccessibleName = "System next virtual desktop", AccessibleDescription = "Voice phrase: next desktop." };
        _systemNextVirtualDesktopButton.Click += (_, _) => ExecuteSystemAction("system-next-virtual-desktop", "Next virtual desktop requested.");

        _systemPreviousVirtualDesktopButton = new Button { Text = "Prev Desktop", Width = 105, AccessibleName = "System previous virtual desktop", AccessibleDescription = "Voice phrase: previous desktop." };
        _systemPreviousVirtualDesktopButton.Click += (_, _) => ExecuteSystemAction("system-previous-virtual-desktop", "Previous virtual desktop requested.");

        _systemTaskManagerButton = new Button { Text = "Task Manager", Width = 110, AccessibleName = "System open Task Manager", AccessibleDescription = "Voice phrase: open task manager." };
        _systemTaskManagerButton.Click += (_, _) => ExecuteSystemAction("system-open-task-manager", "Task Manager requested.");

        _systemSettingsButton = new Button { Text = "Settings", Width = 85, AccessibleName = "System Windows settings", AccessibleDescription = "Voice phrases: windows settings, open settings." };
        _systemSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-settings", "Windows Settings requested.");

        _systemDisplaySettingsButton = new Button { Text = "Display Settings", Width = 125, AccessibleName = "System display settings", AccessibleDescription = "Voice phrase: open display settings." };
        _systemDisplaySettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-display-settings", "Display settings requested.");

        _systemSoundSettingsButton = new Button { Text = "Sound Settings", Width = 120, AccessibleName = "System sound settings", AccessibleDescription = "Voice phrase: open sound settings." };
        _systemSoundSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-sound-settings", "Sound settings requested.");

        _systemBluetoothSettingsButton = new Button { Text = "Bluetooth", Width = 95, AccessibleName = "System Bluetooth settings", AccessibleDescription = "Voice phrase: open bluetooth settings." };
        _systemBluetoothSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-bluetooth-settings", "Bluetooth settings requested.");

        _systemNetworkSettingsButton = new Button { Text = "Network", Width = 90, AccessibleName = "System network settings", AccessibleDescription = "Voice phrases: open network settings, open wifi settings." };
        _systemNetworkSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-network-settings", "Network settings requested.");

        _systemAccessibilitySettingsButton = new Button { Text = "Accessibility", Width = 105, AccessibleName = "System accessibility settings", AccessibleDescription = "Voice phrase: open accessibility settings." };
        _systemAccessibilitySettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-accessibility-settings", "Accessibility settings requested.");

        var systemMagnifierSettingsButton = new Button { Text = "Magnifier Settings", Width = 130, AccessibleName = "System magnifier settings", AccessibleDescription = "Voice phrases: magnifier settings, zoom settings." };
        systemMagnifierSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-magnifier-settings", "Magnifier settings requested.");

        var systemNarratorSettingsButton = new Button { Text = "Narrator Settings", Width = 125, AccessibleName = "System narrator settings", AccessibleDescription = "Voice phrases: narrator settings, screen reader settings." };
        systemNarratorSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-narrator-settings", "Narrator settings requested.");

        var systemCaptionsSettingsButton = new Button { Text = "Captions Settings", Width = 125, AccessibleName = "System captions settings", AccessibleDescription = "Voice phrases: captions settings, live captions settings." };
        systemCaptionsSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-captions-settings", "Captions settings requested.");

        var systemSpeechSettingsButton = new Button { Text = "Speech Settings", Width = 115, AccessibleName = "System speech settings", AccessibleDescription = "Voice phrases: speech settings, voice access settings, voice typing settings, dictation settings." };
        systemSpeechSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-speech-settings", "Speech settings requested.");

        var systemMouseSettingsButton = new Button { Text = "Mouse Settings", Width = 120, AccessibleName = "System mouse settings", AccessibleDescription = "Voice phrase: open mouse settings." };
        systemMouseSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-mouse-settings", "Mouse settings requested.");

        var systemKeyboardSettingsButton = new Button { Text = "Keyboard Settings", Width = 135, AccessibleName = "System keyboard settings", AccessibleDescription = "Voice phrase: open keyboard settings." };
        systemKeyboardSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-keyboard-settings", "Keyboard settings requested.");

        var systemPrivacySettingsButton = new Button { Text = "Privacy Settings", Width = 130, AccessibleName = "System privacy settings", AccessibleDescription = "Voice phrase: open privacy settings." };
        systemPrivacySettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-privacy-settings", "Privacy settings requested.");

        var systemPowerSettingsButton = new Button { Text = "Power Settings", Width = 120, AccessibleName = "System power settings", AccessibleDescription = "Voice phrases: power and battery settings, open power and battery settings." };
        systemPowerSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-power-settings", "Power settings requested.");

        var systemInstalledAppsSettingsButton = new Button { Text = "Installed Apps", Width = 120, AccessibleName = "System installed apps settings", AccessibleDescription = "Voice phrases: installed apps settings, open installed apps settings." };
        systemInstalledAppsSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-apps-settings", "Installed apps settings requested.");

        var systemDefaultAppsSettingsButton = new Button { Text = "Default Apps", Width = 110, AccessibleName = "System default apps settings", AccessibleDescription = "Voice phrases: default apps settings, open default apps settings." };
        systemDefaultAppsSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-default-apps-settings", "Default apps settings requested.");

        var systemDateTimeSettingsButton = new Button { Text = "Date/Time", Width = 95, AccessibleName = "System date and time settings", AccessibleDescription = "Voice phrases: date and time settings, open date and time settings." };
        systemDateTimeSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-date-time-settings", "Date and time settings requested.");

        var systemNotificationsSettingsButton = new Button { Text = "Notifications", Width = 110, AccessibleName = "System notifications settings", AccessibleDescription = "Voice phrases: notifications settings, open notifications settings." };
        systemNotificationsSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-notifications-settings", "Notifications settings requested.");

        var systemWindowsUpdateSettingsButton = new Button { Text = "Windows Update", Width = 120, AccessibleName = "System Windows Update settings", AccessibleDescription = "Voice phrases: windows update settings, open windows update settings." };
        systemWindowsUpdateSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-windows-update-settings", "Windows Update settings requested.");

        var systemPersonalizationSettingsButton = new Button { Text = "Personalize", Width = 100, AccessibleName = "System personalization settings", AccessibleDescription = "Voice phrases: personalization settings, open personalization settings." };
        systemPersonalizationSettingsButton.Click += (_, _) => ExecuteSystemAction("system-open-personalization-settings", "Personalization settings requested.");

        var systemOpenMagnifierButton = new Button { Text = "Open Magnifier", Width = 115, AccessibleName = "System open magnifier", AccessibleDescription = "Voice phrases: open magnifier, show magnifier, magnify screen." };
        systemOpenMagnifierButton.Click += (_, _) => ExecuteSystemAction("system-open-magnifier", "Open magnifier requested.");

        var systemMagnifierZoomOutButton = new Button { Text = "Magnifier Out", Width = 105, AccessibleName = "System magnifier zoom out", AccessibleDescription = "Voice phrase: magnifier zoom out." };
        systemMagnifierZoomOutButton.Click += (_, _) => ExecuteSystemAction("system-magnifier-zoom-out", "Magnifier zoom out requested.");

        var systemCloseMagnifierButton = new Button { Text = "Close Magnifier", Width = 120, AccessibleName = "System close magnifier", AccessibleDescription = "Voice phrase: close magnifier." };
        systemCloseMagnifierButton.Click += (_, _) => ExecuteSystemAction("system-close-magnifier", "Close magnifier requested.");

        _systemMinimizeWindowButton = new Button { Text = "Minimize Window", Width = 125, AccessibleName = "System minimize window", AccessibleDescription = "Voice phrase: minimize window." };
        _systemMinimizeWindowButton.Click += (_, _) => ExecuteSystemAction("system-minimize-window", "Minimize window requested.");

        _systemMaximizeWindowButton = new Button { Text = "Maximize Window", Width = 125, AccessibleName = "System maximize window", AccessibleDescription = "Voice phrase: maximize window." };
        _systemMaximizeWindowButton.Click += (_, _) => ExecuteSystemAction("system-maximize-window", "Maximize window requested.");

        _systemRestoreWindowButton = new Button { Text = "Restore Window", Width = 120, AccessibleName = "System restore window", AccessibleDescription = "Voice phrase: restore window." };
        _systemRestoreWindowButton.Click += (_, _) => ExecuteSystemAction("system-restore-window", "Restore window requested.");

        _systemSnapLeftButton = new Button { Text = "Snap Left", Width = 90, AccessibleName = "System snap window left", AccessibleDescription = "Voice phrase: snap window left." };
        _systemSnapLeftButton.Click += (_, _) => ExecuteSystemAction("system-snap-window-left", "Snap window left requested.");

        _systemSnapRightButton = new Button { Text = "Snap Right", Width = 95, AccessibleName = "System snap window right", AccessibleDescription = "Voice phrase: snap window right." };
        _systemSnapRightButton.Click += (_, _) => ExecuteSystemAction("system-snap-window-right", "Snap window right requested.");

        _systemSnapUpButton = new Button { Text = "Snap Up", Width = 85, AccessibleName = "System snap window up", AccessibleDescription = "Voice phrase: snap up." };
        _systemSnapUpButton.Click += (_, _) => ExecuteSystemAction("system-snap-window-up", "Snap window up requested.");

        _systemSnapDownButton = new Button { Text = "Snap Down", Width = 100, AccessibleName = "System snap window down", AccessibleDescription = "Voice phrase: snap down." };
        _systemSnapDownButton.Click += (_, _) => ExecuteSystemAction("system-snap-window-down", "Snap window down requested.");

        _systemSnapLayoutsButton = new Button { Text = "Snap Layouts", Width = 110, AccessibleName = "System show snap layouts", AccessibleDescription = "Voice phrase: show snap layouts." };
        _systemSnapLayoutsButton.Click += (_, _) => ExecuteSystemAction("system-show-snap-layouts", "Snap layouts requested.");

        _systemEnterButton = new Button { Text = "Enter", Width = 75, AccessibleName = "System press enter", AccessibleDescription = "Voice phrase: press enter." };
        _systemEnterButton.Click += (_, _) => ExecuteSystemAction("system-press-enter", "Enter requested.");

        _systemTabButton = new Button { Text = "Tab", Width = 65, AccessibleName = "System press tab", AccessibleDescription = "Voice phrase: press tab." };
        _systemTabButton.Click += (_, _) => ExecuteSystemAction("system-press-tab", "Tab requested.");

        _systemEscapeButton = new Button { Text = "Escape", Width = 75, AccessibleName = "System press escape", AccessibleDescription = "Voice phrase: press escape." };
        _systemEscapeButton.Click += (_, _) => ExecuteSystemAction("system-press-escape", "Escape requested.");

        _systemBackspaceButton = new Button { Text = "Backspace", Width = 90, AccessibleName = "System press backspace", AccessibleDescription = "Voice phrase: press backspace." };
        _systemBackspaceButton.Click += (_, _) => ExecuteSystemAction("system-press-backspace", "Backspace requested.");

        _systemSpaceButton = new Button { Text = "Space", Width = 75, AccessibleName = "System press space", AccessibleDescription = "Voice phrase: press space." };
        _systemSpaceButton.Click += (_, _) => ExecuteSystemAction("system-press-space", "Space requested.");

        _systemDeleteButton = new Button { Text = "Delete", Width = 75, AccessibleName = "System press delete", AccessibleDescription = "Voice phrase: press delete." };
        _systemDeleteButton.Click += (_, _) => ExecuteSystemAction("system-press-delete", "Delete requested.");

        _systemInsertButton = new Button { Text = "Insert", Width = 75, AccessibleName = "System press insert", AccessibleDescription = "Voice phrase: press insert." };
        _systemInsertButton.Click += (_, _) => ExecuteSystemAction("system-press-insert", "Insert requested.");

        _systemWindowsKeyButton = new Button { Text = "Windows", Width = 80, AccessibleName = "System press Windows key", AccessibleDescription = "Voice phrases: press windows key, windows key." };
        _systemWindowsKeyButton.Click += (_, _) => ExecuteSystemAction("system-press-windows", "Windows key requested.");

        _systemContextMenuButton = new Button { Text = "Context Menu", Width = 115, AccessibleName = "System press context menu key", AccessibleDescription = "Voice phrases: press context menu, context menu key." };
        _systemContextMenuButton.Click += (_, _) => ExecuteSystemAction("system-press-context-menu", "Context menu key requested.");

        _systemCapsLockButton = new Button { Text = "Caps Lock", Width = 85, AccessibleName = "System press Caps Lock", AccessibleDescription = "Voice phrases: press caps lock, caps lock." };
        _systemCapsLockButton.Click += (_, _) => ExecuteSystemAction("system-press-caps-lock", "Caps Lock requested.");

        _systemDigitButtons = Enumerable.Range(0, 10)
            .Select(digit =>
            {
                var button = new Button { Text = digit.ToString(), Width = 42 };
                button.Click += (_, _) => ExecuteSystemAction($"system-press-digit:{digit}", $"Digit {digit} requested.");
                return button;
            })
            .ToArray();

        _systemLetterButtons = Enumerable.Range('A', 26)
            .Select(code =>
            {
                var letter = (char)code;
                var lower = char.ToLowerInvariant(letter);
                var button = new Button { Text = letter.ToString(), Width = 42 };
                button.Click += (_, _) => ExecuteSystemAction($"system-press-letter:{lower}", $"Letter {letter} requested.");
                return button;
            })
            .ToArray();

        _systemSymbolButtons = new (string Text, string Symbol, string Message)[]
            {
                (",", "comma", "Comma requested."),
                (".", "period", "Period requested."),
                ("/", "slash", "Slash requested."),
                ("?", "question", "Question mark requested."),
                (";", "semicolon", "Semicolon requested."),
                (":", "colon", "Colon requested."),
                ("'", "apostrophe", "Apostrophe requested."),
                ("\"", "quote", "Quote requested."),
                ("-", "minus", "Minus requested."),
                ("_", "underscore", "Underscore requested."),
                ("=", "equals", "Equals requested."),
                ("+", "plus", "Plus requested."),
                ("@", "at", "At sign requested."),
                ("(", "left-parenthesis", "Left parenthesis requested.")
            }
            .Select(symbol =>
            {
                var button = new Button { Text = symbol.Text, Width = 42 };
                button.Click += (_, _) => ExecuteSystemAction($"system-press-symbol:{symbol.Symbol}", symbol.Message);
                return button;
            })
            .ToArray();

        _systemChordButtons = new (string Text, string Chord, string Message)[]
            {
                ("Shift+Tab", "shift-tab", "Shift Tab requested."),
                ("Ctrl+Tab", "control-tab", "Control Tab requested."),
                ("Ctrl+Shift+Tab", "control-shift-tab", "Control Shift Tab requested."),
                ("Alt+Left", "alt-left", "Alt Left requested."),
                ("Alt+Right", "alt-right", "Alt Right requested."),
                ("Alt+Up", "alt-up", "Alt Up requested."),
                ("Alt+Down", "alt-down", "Alt Down requested."),
                ("Ctrl+Home", "control-home", "Control Home requested."),
                ("Ctrl+End", "control-end", "Control End requested."),
                ("Ctrl+Shift+Home", "control-shift-home", "Control Shift Home requested."),
                ("Ctrl+Shift+End", "control-shift-end", "Control Shift End requested.")
            }
            .Select(chord =>
            {
                var button = new Button { Text = chord.Text, Width = 115 };
                button.Click += (_, _) => ExecuteSystemAction($"system-press-chord:{chord.Chord}", chord.Message);
                return button;
            })
            .ToArray();

        _systemUpButton = new Button { Text = "Up", Width = 60, AccessibleName = "System press up arrow", AccessibleDescription = "Voice phrases: press up arrow, up arrow." };
        _systemUpButton.Click += (_, _) => ExecuteSystemAction("system-press-up", "Up arrow requested.");

        _systemDownButton = new Button { Text = "Down", Width = 65, AccessibleName = "System press down arrow", AccessibleDescription = "Voice phrases: press down arrow, down arrow." };
        _systemDownButton.Click += (_, _) => ExecuteSystemAction("system-press-down", "Down arrow requested.");

        _systemLeftButton = new Button { Text = "Left", Width = 60, AccessibleName = "System press left arrow", AccessibleDescription = "Voice phrases: press left arrow, left arrow." };
        _systemLeftButton.Click += (_, _) => ExecuteSystemAction("system-press-left", "Left arrow requested.");

        _systemRightButton = new Button { Text = "Right", Width = 65, AccessibleName = "System press right arrow", AccessibleDescription = "Voice phrases: press right arrow, right arrow." };
        _systemRightButton.Click += (_, _) => ExecuteSystemAction("system-press-right", "Right arrow requested.");

        _systemHomeButton = new Button { Text = "Home", Width = 65, AccessibleName = "System press Home", AccessibleDescription = "Voice phrases: press home, home key." };
        _systemHomeButton.Click += (_, _) => ExecuteSystemAction("system-press-home", "Home requested.");

        _systemEndButton = new Button { Text = "End", Width = 60, AccessibleName = "System press End", AccessibleDescription = "Voice phrases: press end, end key." };
        _systemEndButton.Click += (_, _) => ExecuteSystemAction("system-press-end", "End requested.");

        _systemPageUpButton = new Button { Text = "Page Up", Width = 80, AccessibleName = "System press Page Up", AccessibleDescription = "Voice phrases: press page up, page up." };
        _systemPageUpButton.Click += (_, _) => ExecuteSystemAction("system-page-up", "Page up requested.");

        _systemPageDownButton = new Button { Text = "Page Down", Width = 90, AccessibleName = "System press Page Down", AccessibleDescription = "Voice phrases: press page down, page down." };
        _systemPageDownButton.Click += (_, _) => ExecuteSystemAction("system-page-down", "Page down requested.");

        _systemMouseClickButton = new Button { Text = "Click", Width = 70, AccessibleName = "System mouse click", AccessibleDescription = "Voice phrase: click." };
        _systemMouseClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-click", "Mouse click requested.");

        _systemMouseDoubleClickButton = new Button { Text = "Double Click", Width = 100, AccessibleName = "System mouse double click", AccessibleDescription = "Voice phrase: double click." };
        _systemMouseDoubleClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-double-click", "Mouse double-click requested.");

        _systemMouseRightClickButton = new Button { Text = "Right Click", Width = 90, AccessibleName = "System mouse right click", AccessibleDescription = "Voice phrase: right click." };
        _systemMouseRightClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-right-click", "Mouse right-click requested.");

        _systemMouseButtonDownButton = new Button { Text = "Mouse Button Down", Width = 145, AccessibleName = "System hold mouse", AccessibleDescription = "Voice phrase: hold mouse." };
        _systemMouseButtonDownButton.Click += (_, _) => ExecuteSystemAction("system-mouse-button-down", "Mouse button down requested.");

        _systemMouseButtonUpButton = new Button { Text = "Mouse Button Up", Width = 130, AccessibleName = "System release mouse", AccessibleDescription = "Voice phrases: release mouse, release mouse button." };
        _systemMouseButtonUpButton.Click += (_, _) => ExecuteSystemAction("system-mouse-button-up", "Mouse button up requested.");

        _systemMouseScrollUpButton = new Button { Text = "Mouse Scroll Up", Width = 120, AccessibleName = "System mouse scroll up", AccessibleDescription = "Voice phrases: mouse scroll up, mouse scroll up a little." };
        _systemMouseScrollUpButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-up", "Mouse scroll up requested.");

        _systemMouseScrollDownButton = new Button { Text = "Mouse Scroll Down", Width = 130, AccessibleName = "System mouse scroll down", AccessibleDescription = "Voice phrases: mouse scroll down, mouse scroll down a little." };
        _systemMouseScrollDownButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-down", "Mouse scroll down requested.");

        _systemMouseScrollLeftButton = new Button { Text = "Mouse Scroll Left", Width = 125, AccessibleName = "System mouse scroll left", AccessibleDescription = "Voice phrases: mouse scroll left, scroll left." };
        _systemMouseScrollLeftButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-left", "Mouse scroll left requested.");

        _systemMouseScrollRightButton = new Button { Text = "Mouse Scroll Right", Width = 130, AccessibleName = "System mouse scroll right", AccessibleDescription = "Voice phrases: mouse scroll right, scroll right." };
        _systemMouseScrollRightButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-right", "Mouse scroll right requested.");

        _systemMouseMoveUpButton = new Button { Text = "Mouse Up", Width = 85, AccessibleName = "System move mouse up", AccessibleDescription = "Voice phrases: move mouse up, nudge up." };
        _systemMouseMoveUpButton.Click += (_, _) => ExecuteSystemAction("system-mouse-move-up", "Mouse move up requested.");

        _systemMouseMoveDownButton = new Button { Text = "Mouse Down", Width = 100, AccessibleName = "System move mouse down", AccessibleDescription = "Voice phrases: move mouse down, nudge down." };
        _systemMouseMoveDownButton.Click += (_, _) => ExecuteSystemAction("system-mouse-move-down", "Mouse move down requested.");

        _systemMouseMoveLeftButton = new Button { Text = "Mouse Left", Width = 95, AccessibleName = "System move mouse left", AccessibleDescription = "Voice phrases: move mouse left, nudge left." };
        _systemMouseMoveLeftButton.Click += (_, _) => ExecuteSystemAction("system-mouse-move-left", "Mouse move left requested.");

        _systemMouseMoveRightButton = new Button { Text = "Mouse Right", Width = 100, AccessibleName = "System move mouse right", AccessibleDescription = "Voice phrases: move mouse right, nudge right." };
        _systemMouseMoveRightButton.Click += (_, _) => ExecuteSystemAction("system-mouse-move-right", "Mouse move right requested.");

        _systemMouseDragUpButton = new Button { Text = "Drag Up", Width = 80, AccessibleName = "System drag mouse up", AccessibleDescription = "Voice phrase: drag mouse up." };
        _systemMouseDragUpButton.Click += (_, _) => ExecuteSystemAction("system-mouse-drag-up", "Mouse drag up requested.");

        _systemMouseDragDownButton = new Button { Text = "Drag Down", Width = 95, AccessibleName = "System drag mouse down", AccessibleDescription = "Voice phrase: drag mouse down." };
        _systemMouseDragDownButton.Click += (_, _) => ExecuteSystemAction("system-mouse-drag-down", "Mouse drag down requested.");

        _systemMouseDragLeftButton = new Button { Text = "Drag Left", Width = 85, AccessibleName = "System drag mouse left", AccessibleDescription = "Voice phrase: drag mouse left." };
        _systemMouseDragLeftButton.Click += (_, _) => ExecuteSystemAction("system-mouse-drag-left", "Mouse drag left requested.");

        _systemMouseDragRightButton = new Button { Text = "Drag Right", Width = 90, AccessibleName = "System drag mouse right", AccessibleDescription = "Voice phrase: drag mouse right." };
        _systemMouseDragRightButton.Click += (_, _) => ExecuteSystemAction("system-mouse-drag-right", "Mouse drag right requested.");

        _systemCopyButton = new Button { Text = "Copy", Width = 65, AccessibleName = "System copy", AccessibleDescription = "Voice phrase: copy." };
        _systemCopyButton.Click += (_, _) => ExecuteSystemAction("system-copy", "Copy requested.");

        _systemPasteButton = new Button { Text = "Paste", Width = 70, AccessibleName = "System paste", AccessibleDescription = "Voice phrase: paste." };
        _systemPasteButton.Click += (_, _) => ExecuteSystemAction("system-paste", "Paste requested.");

        _systemCutButton = new Button { Text = "Cut", Width = 55, AccessibleName = "System cut", AccessibleDescription = "Voice phrase: cut." };
        _systemCutButton.Click += (_, _) => ExecuteSystemAction("system-cut", "Cut requested.");

        _systemSelectAllButton = new Button { Text = "Select All", Width = 90, AccessibleName = "System select all", AccessibleDescription = "Voice phrase: select all." };
        _systemSelectAllButton.Click += (_, _) => ExecuteSystemAction("system-select-all", "Select all requested.");

        _systemSaveButton = new Button { Text = "Save", Width = 65, AccessibleName = "System save", AccessibleDescription = "Voice phrase: save." };
        _systemSaveButton.Click += (_, _) => ExecuteSystemAction("system-save", "Save requested.");

        _systemUndoButton = new Button { Text = "Undo", Width = 65, AccessibleName = "System undo", AccessibleDescription = "Voice phrase: undo." };
        _systemUndoButton.Click += (_, _) => ExecuteSystemAction("system-undo", "Undo requested.");

        _systemRedoButton = new Button { Text = "Redo", Width = 65, AccessibleName = "System redo", AccessibleDescription = "Voice phrase: redo." };
        _systemRedoButton.Click += (_, _) => ExecuteSystemAction("system-redo", "Redo requested.");

        _systemBoldButton = new Button { Text = "Bold", Width = 65, AccessibleName = "System bold", AccessibleDescription = "Voice phrases: bold, bold that." };
        _systemBoldButton.Click += (_, _) => ExecuteSystemAction("system-bold", "Bold requested.");

        _systemItalicButton = new Button { Text = "Italic", Width = 65, AccessibleName = "System italic", AccessibleDescription = "Voice phrases: italic, italicize that." };
        _systemItalicButton.Click += (_, _) => ExecuteSystemAction("system-italic", "Italic requested.");

        _systemUnderlineButton = new Button { Text = "Underline", Width = 90, AccessibleName = "System underline", AccessibleDescription = "Voice phrases: underline, underline that." };
        _systemUnderlineButton.Click += (_, _) => ExecuteSystemAction("system-underline", "Underline requested.");

        _systemFindButton = new Button { Text = "Find", Width = 65, AccessibleName = "System find", AccessibleDescription = "Voice phrase: find." };
        _systemFindButton.Click += (_, _) => ExecuteSystemAction("system-find", "Find requested.");

        _systemNewWindowButton = new Button { Text = "New Window", Width = 100, AccessibleName = "System new window", AccessibleDescription = "Voice phrase: new window." };
        _systemNewWindowButton.Click += (_, _) => ExecuteSystemAction("system-new-window", "New window requested.");

        _systemNewDocumentButton = new Button { Text = "New Doc", Width = 80, AccessibleName = "System new document", AccessibleDescription = "Voice phrase: new document." };
        _systemNewDocumentButton.Click += (_, _) => ExecuteSystemAction("system-new-document", "New document requested.");

        _systemOpenFileButton = new Button { Text = "Open File", Width = 85, AccessibleName = "System open file", AccessibleDescription = "Voice phrase: open file." };
        _systemOpenFileButton.Click += (_, _) => ExecuteSystemAction("system-open-file", "Open file dialog requested.");

        _systemPrintButton = new Button { Text = "Print", Width = 65, AccessibleName = "System print", AccessibleDescription = "Voice phrase: print." };
        _systemPrintButton.Click += (_, _) => ExecuteSystemAction("system-print", "Print dialog requested.");

        _systemZoomInButton = new Button { Text = "Zoom In", Width = 80, AccessibleName = "System zoom in", AccessibleDescription = "Voice phrase: zoom in." };
        _systemZoomInButton.Click += (_, _) => ExecuteSystemAction("system-zoom-in", "Zoom in requested.");

        _systemZoomOutButton = new Button { Text = "Zoom Out", Width = 85, AccessibleName = "System zoom out", AccessibleDescription = "Voice phrase: zoom out." };
        _systemZoomOutButton.Click += (_, _) => ExecuteSystemAction("system-zoom-out", "Zoom out requested.");

        _systemZoomResetButton = new Button { Text = "Reset Zoom", Width = 95, AccessibleName = "System reset zoom", AccessibleDescription = "Voice phrase: reset zoom." };
        _systemZoomResetButton.Click += (_, _) => ExecuteSystemAction("system-zoom-reset", "Zoom reset requested.");

        _systemCloseWindowButton = new Button { Text = "Close Window", Width = 100, AccessibleName = "System close window", AccessibleDescription = "Voice phrases: close this window, close active app." };
        _systemCloseWindowButton.Click += (_, _) => ExecuteSystemAction("system-close-window", "Close window requested.");

        _systemMovePreviousCharacterButton = new Button { Text = "Prev Char", Width = 85, AccessibleName = "System move previous character", AccessibleDescription = "Voice phrase: previous character." };
        _systemMovePreviousCharacterButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-character", "Move previous character requested.");

        _systemMoveNextCharacterButton = new Button { Text = "Next Char", Width = 85, AccessibleName = "System move next character", AccessibleDescription = "Voice phrase: next character." };
        _systemMoveNextCharacterButton.Click += (_, _) => ExecuteSystemAction("system-move-next-character", "Move next character requested.");

        _systemSelectPreviousCharacterButton = new Button { Text = "Select Prev Char", Width = 125, AccessibleName = "System select previous character", AccessibleDescription = "Voice phrase: select previous character." };
        _systemSelectPreviousCharacterButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-character", "Select previous character requested.");

        _systemSelectNextCharacterButton = new Button { Text = "Select Next Char", Width = 125, AccessibleName = "System select next character", AccessibleDescription = "Voice phrase: select next character." };
        _systemSelectNextCharacterButton.Click += (_, _) => ExecuteSystemAction("system-select-next-character", "Select next character requested.");

        _systemDeletePreviousCharacterButton = new Button { Text = "Delete Prev Char", Width = 125, AccessibleName = "System delete previous character", AccessibleDescription = "Voice phrase: delete previous character." };
        _systemDeletePreviousCharacterButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-character", "Delete previous character requested.");

        _systemDeleteNextCharacterButton = new Button { Text = "Delete Next Char", Width = 125, AccessibleName = "System delete next character", AccessibleDescription = "Voice phrase: delete next character." };
        _systemDeleteNextCharacterButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-character", "Delete next character requested.");

        _systemMoveLineStartButton = new Button { Text = "Line Start", Width = 90, AccessibleName = "System move line start", AccessibleDescription = "Voice phrase: go to line start." };
        _systemMoveLineStartButton.Click += (_, _) => ExecuteSystemAction("system-move-line-start", "Move to line start requested.");

        _systemMoveLineEndButton = new Button { Text = "Line End", Width = 85, AccessibleName = "System move line end", AccessibleDescription = "Voice phrase: go to line end." };
        _systemMoveLineEndButton.Click += (_, _) => ExecuteSystemAction("system-move-line-end", "Move to line end requested.");

        _systemMovePreviousLineButton = new Button { Text = "Prev Line", Width = 85, AccessibleName = "System move previous line", AccessibleDescription = "Voice phrase: go to previous line." };
        _systemMovePreviousLineButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-line", "Move previous line requested.");

        _systemMoveNextLineButton = new Button { Text = "Next Line", Width = 85, AccessibleName = "System move next line", AccessibleDescription = "Voice phrase: go to next line." };
        _systemMoveNextLineButton.Click += (_, _) => ExecuteSystemAction("system-move-next-line", "Move next line requested.");

        _systemSelectToLineStartButton = new Button { Text = "Select To Line Start", Width = 145, AccessibleName = "System select to line start", AccessibleDescription = "Voice phrase: select to line start." };
        _systemSelectToLineStartButton.Click += (_, _) => ExecuteSystemAction("system-select-to-line-start", "Select to line start requested.");

        _systemSelectToLineEndButton = new Button { Text = "Select To Line End", Width = 140, AccessibleName = "System select to line end", AccessibleDescription = "Voice phrase: select to line end." };
        _systemSelectToLineEndButton.Click += (_, _) => ExecuteSystemAction("system-select-to-line-end", "Select to line end requested.");

        _systemSelectPreviousLineButton = new Button { Text = "Select Prev Line", Width = 125, AccessibleName = "System select previous line", AccessibleDescription = "Voice phrase: select previous line." };
        _systemSelectPreviousLineButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-line", "Select previous line requested.");

        _systemSelectNextLineButton = new Button { Text = "Select Next Line", Width = 125, AccessibleName = "System select next line", AccessibleDescription = "Voice phrase: select next line." };
        _systemSelectNextLineButton.Click += (_, _) => ExecuteSystemAction("system-select-next-line", "Select next line requested.");

        _systemDeleteToLineStartButton = new Button { Text = "Delete To Line Start", Width = 145, AccessibleName = "System delete to line start", AccessibleDescription = "Voice phrase: delete to line start." };
        _systemDeleteToLineStartButton.Click += (_, _) => ExecuteSystemAction("system-delete-to-line-start", "Delete to line start requested.");

        _systemDeleteToLineEndButton = new Button { Text = "Delete To Line End", Width = 140, AccessibleName = "System delete to line end", AccessibleDescription = "Voice phrase: delete to line end." };
        _systemDeleteToLineEndButton.Click += (_, _) => ExecuteSystemAction("system-delete-to-line-end", "Delete to line end requested.");

        _systemDeletePreviousLineButton = new Button { Text = "Delete Prev Line", Width = 125, AccessibleName = "System delete previous line", AccessibleDescription = "Voice phrase: delete previous line." };
        _systemDeletePreviousLineButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-line", "Delete previous line requested.");

        _systemDeleteNextLineButton = new Button { Text = "Delete Next Line", Width = 125, AccessibleName = "System delete next line", AccessibleDescription = "Voice phrase: delete next line." };
        _systemDeleteNextLineButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-line", "Delete next line requested.");

        _systemMovePreviousWordButton = new Button { Text = "Prev Word", Width = 85, AccessibleName = "System move previous word", AccessibleDescription = "Voice phrase: go to previous word." };
        _systemMovePreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-word", "Move previous word requested.");

        _systemMoveNextWordButton = new Button { Text = "Next Word", Width = 85, AccessibleName = "System move next word", AccessibleDescription = "Voice phrase: go to next word." };
        _systemMoveNextWordButton.Click += (_, _) => ExecuteSystemAction("system-move-next-word", "Move next word requested.");

        _systemSelectPreviousWordButton = new Button { Text = "Select Prev Word", Width = 130, AccessibleName = "System select previous word", AccessibleDescription = "Voice phrase: select previous word." };
        _systemSelectPreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-word", "Select previous word requested.");

        _systemSelectNextWordButton = new Button { Text = "Select Next Word", Width = 130, AccessibleName = "System select next word", AccessibleDescription = "Voice phrase: select next word." };
        _systemSelectNextWordButton.Click += (_, _) => ExecuteSystemAction("system-select-next-word", "Select next word requested.");

        _systemDeletePreviousWordButton = new Button { Text = "Delete Prev Word", Width = 130, AccessibleName = "System delete previous word", AccessibleDescription = "Voice phrase: delete previous word." };
        _systemDeletePreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-word", "Delete previous word requested.");

        _systemDeleteNextWordButton = new Button { Text = "Delete Next Word", Width = 125, AccessibleName = "System delete next word", AccessibleDescription = "Voice phrase: delete next word." };
        _systemDeleteNextWordButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-word", "Delete next word requested.");

        _systemMovePreviousSentenceButton = new Button { Text = "Prev Sentence", Width = 105, AccessibleName = "System move previous sentence", AccessibleDescription = "Voice phrase: go to previous sentence." };
        _systemMovePreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-sentence", "Move previous sentence requested.");

        _systemMoveNextSentenceButton = new Button { Text = "Next Sentence", Width = 105, AccessibleName = "System move next sentence", AccessibleDescription = "Voice phrase: go to next sentence." };
        _systemMoveNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-move-next-sentence", "Move next sentence requested.");

        _systemSelectPreviousSentenceButton = new Button { Text = "Select Prev Sentence", Width = 150, AccessibleName = "System select previous sentence", AccessibleDescription = "Voice phrase: select previous sentence." };
        _systemSelectPreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-sentence", "Select previous sentence requested.");

        _systemSelectNextSentenceButton = new Button { Text = "Select Next Sentence", Width = 150, AccessibleName = "System select next sentence", AccessibleDescription = "Voice phrase: select next sentence." };
        _systemSelectNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-select-next-sentence", "Select next sentence requested.");

        _systemDeletePreviousSentenceButton = new Button { Text = "Delete Prev Sentence", Width = 150, AccessibleName = "System delete previous sentence", AccessibleDescription = "Voice phrase: delete previous sentence." };
        _systemDeletePreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-sentence", "Delete previous sentence requested.");

        _systemDeleteNextSentenceButton = new Button { Text = "Delete Next Sentence", Width = 145, AccessibleName = "System delete next sentence", AccessibleDescription = "Voice phrase: delete next sentence." };
        _systemDeleteNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-sentence", "Delete next sentence requested.");

        _systemMovePreviousParagraphButton = new Button { Text = "Prev Paragraph", Width = 110, AccessibleName = "System move previous paragraph", AccessibleDescription = "Voice phrase: go to previous paragraph." };
        _systemMovePreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-paragraph", "Move previous paragraph requested.");

        _systemMoveNextParagraphButton = new Button { Text = "Next Paragraph", Width = 110, AccessibleName = "System move next paragraph", AccessibleDescription = "Voice phrase: go to next paragraph." };
        _systemMoveNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-move-next-paragraph", "Move next paragraph requested.");

        _systemSelectPreviousParagraphButton = new Button { Text = "Select Prev Para", Width = 130, AccessibleName = "System select previous paragraph", AccessibleDescription = "Voice phrase: select previous paragraph." };
        _systemSelectPreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-paragraph", "Select previous paragraph requested.");

        _systemSelectNextParagraphButton = new Button { Text = "Select Next Para", Width = 130, AccessibleName = "System select next paragraph", AccessibleDescription = "Voice phrase: select next paragraph." };
        _systemSelectNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-select-next-paragraph", "Select next paragraph requested.");

        _systemDeletePreviousParagraphButton = new Button { Text = "Delete Prev Para", Width = 130, AccessibleName = "System delete previous paragraph", AccessibleDescription = "Voice phrase: delete previous paragraph." };
        _systemDeletePreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-paragraph", "Delete previous paragraph requested.");

        _systemDeleteNextParagraphButton = new Button { Text = "Delete Next Para", Width = 125, AccessibleName = "System delete next paragraph", AccessibleDescription = "Voice phrase: delete next paragraph." };
        _systemDeleteNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-paragraph", "Delete next paragraph requested.");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _systemSafetyLabel, row++);
        AddRow(layout, "Status", _systemStatusLabel, row++);
        AddRow(layout, "Selected", _systemSelectedActionLabel, row++);
        AddRow(layout, "Last action", _systemLastActionLabel, row++);
        AddRow(layout, "Voice cue", _systemVoiceCueLabel, row++);
        AddRow(layout, "Last heard", _systemLastHeardLabel, row++);
        AddRow(layout, "Switch target", _systemSwitchWindowText, row++);
        AddFullWidth(layout, _systemWindowChoiceHintLabel, row++);
        AddFullWidth(layout, _systemWindowChoiceList, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_systemVolumeUpButton);
        buttons.Controls.Add(_systemVolumeDownButton);
        buttons.Controls.Add(_systemMuteButton);
        buttons.Controls.Add(_systemMediaPlayPauseButton);
        buttons.Controls.Add(_systemMediaNextButton);
        buttons.Controls.Add(_systemMediaPreviousButton);
        buttons.Controls.Add(_systemMediaStopButton);
        buttons.Controls.Add(_systemShowDesktopButton);
        buttons.Controls.Add(_systemNextWindowButton);
        buttons.Controls.Add(_systemPreviousWindowButton);
        buttons.Controls.Add(_systemTaskViewButton);
        buttons.Controls.Add(systemQuickSettingsButton);
        buttons.Controls.Add(systemNotificationCenterButton);
        buttons.Controls.Add(systemEmojiPanelButton);
        buttons.Controls.Add(systemClipboardHistoryButton);
        buttons.Controls.Add(systemSnippingToolbarButton);
        buttons.Controls.Add(systemProjectDisplayButton);
        buttons.Controls.Add(systemCastDisplayButton);
        buttons.Controls.Add(_systemSwitchWindowButton);
        buttons.Controls.Add(_systemConfirmWindowChoiceButton);
        buttons.Controls.Add(_systemClearWindowChoicesButton);
        buttons.Controls.Add(_systemNewVirtualDesktopButton);
        buttons.Controls.Add(_systemNextVirtualDesktopButton);
        buttons.Controls.Add(_systemPreviousVirtualDesktopButton);
        buttons.Controls.Add(_systemTaskManagerButton);
        buttons.Controls.Add(_systemSettingsButton);
        buttons.Controls.Add(_systemDisplaySettingsButton);
        buttons.Controls.Add(_systemSoundSettingsButton);
        buttons.Controls.Add(_systemBluetoothSettingsButton);
        buttons.Controls.Add(_systemNetworkSettingsButton);
        buttons.Controls.Add(_systemAccessibilitySettingsButton);
        buttons.Controls.Add(systemMagnifierSettingsButton);
        buttons.Controls.Add(systemNarratorSettingsButton);
        buttons.Controls.Add(systemCaptionsSettingsButton);
        buttons.Controls.Add(systemSpeechSettingsButton);
        buttons.Controls.Add(systemMouseSettingsButton);
        buttons.Controls.Add(systemKeyboardSettingsButton);
        buttons.Controls.Add(systemPrivacySettingsButton);
        buttons.Controls.Add(systemPowerSettingsButton);
        buttons.Controls.Add(systemInstalledAppsSettingsButton);
        buttons.Controls.Add(systemDefaultAppsSettingsButton);
        buttons.Controls.Add(systemDateTimeSettingsButton);
        buttons.Controls.Add(systemNotificationsSettingsButton);
        buttons.Controls.Add(systemWindowsUpdateSettingsButton);
        buttons.Controls.Add(systemPersonalizationSettingsButton);
        buttons.Controls.Add(systemOpenMagnifierButton);
        buttons.Controls.Add(systemMagnifierZoomOutButton);
        buttons.Controls.Add(systemCloseMagnifierButton);
        buttons.Controls.Add(_systemMinimizeWindowButton);
        buttons.Controls.Add(_systemMaximizeWindowButton);
        buttons.Controls.Add(_systemRestoreWindowButton);
        buttons.Controls.Add(_systemSnapLeftButton);
        buttons.Controls.Add(_systemSnapRightButton);
        buttons.Controls.Add(_systemSnapUpButton);
        buttons.Controls.Add(_systemSnapDownButton);
        buttons.Controls.Add(_systemSnapLayoutsButton);
        buttons.Controls.Add(_systemEnterButton);
        buttons.Controls.Add(_systemTabButton);
        buttons.Controls.Add(_systemEscapeButton);
        buttons.Controls.Add(_systemBackspaceButton);
        buttons.Controls.Add(_systemSpaceButton);
        buttons.Controls.Add(_systemDeleteButton);
        buttons.Controls.Add(_systemInsertButton);
        buttons.Controls.Add(_systemWindowsKeyButton);
        buttons.Controls.Add(_systemContextMenuButton);
        buttons.Controls.Add(_systemCapsLockButton);
        foreach (var button in _systemDigitButtons)
            buttons.Controls.Add(button);
        foreach (var button in _systemLetterButtons)
            buttons.Controls.Add(button);
        foreach (var button in _systemSymbolButtons)
            buttons.Controls.Add(button);
        foreach (var button in _systemChordButtons)
            buttons.Controls.Add(button);
        buttons.Controls.Add(_systemUpButton);
        buttons.Controls.Add(_systemDownButton);
        buttons.Controls.Add(_systemLeftButton);
        buttons.Controls.Add(_systemRightButton);
        buttons.Controls.Add(_systemHomeButton);
        buttons.Controls.Add(_systemEndButton);
        buttons.Controls.Add(_systemPageUpButton);
        buttons.Controls.Add(_systemPageDownButton);
        buttons.Controls.Add(_systemMouseClickButton);
        buttons.Controls.Add(_systemMouseDoubleClickButton);
        buttons.Controls.Add(_systemMouseRightClickButton);
        buttons.Controls.Add(_systemMouseButtonDownButton);
        buttons.Controls.Add(_systemMouseButtonUpButton);
        buttons.Controls.Add(_systemMouseScrollUpButton);
        buttons.Controls.Add(_systemMouseScrollDownButton);
        buttons.Controls.Add(_systemMouseScrollLeftButton);
        buttons.Controls.Add(_systemMouseScrollRightButton);
        buttons.Controls.Add(_systemMouseMoveUpButton);
        buttons.Controls.Add(_systemMouseMoveDownButton);
        buttons.Controls.Add(_systemMouseMoveLeftButton);
        buttons.Controls.Add(_systemMouseMoveRightButton);
        buttons.Controls.Add(_systemMouseDragUpButton);
        buttons.Controls.Add(_systemMouseDragDownButton);
        buttons.Controls.Add(_systemMouseDragLeftButton);
        buttons.Controls.Add(_systemMouseDragRightButton);
        buttons.Controls.Add(_systemCopyButton);
        buttons.Controls.Add(_systemPasteButton);
        buttons.Controls.Add(_systemCutButton);
        buttons.Controls.Add(_systemSelectAllButton);
        buttons.Controls.Add(_systemSaveButton);
        buttons.Controls.Add(_systemUndoButton);
        buttons.Controls.Add(_systemRedoButton);
        buttons.Controls.Add(_systemBoldButton);
        buttons.Controls.Add(_systemItalicButton);
        buttons.Controls.Add(_systemUnderlineButton);
        buttons.Controls.Add(_systemFindButton);
        buttons.Controls.Add(_systemNewWindowButton);
        buttons.Controls.Add(_systemNewDocumentButton);
        buttons.Controls.Add(_systemOpenFileButton);
        buttons.Controls.Add(_systemPrintButton);
        buttons.Controls.Add(_systemZoomInButton);
        buttons.Controls.Add(_systemZoomOutButton);
        buttons.Controls.Add(_systemZoomResetButton);
        buttons.Controls.Add(_systemCloseWindowButton);
        buttons.Controls.Add(_systemMovePreviousCharacterButton);
        buttons.Controls.Add(_systemMoveNextCharacterButton);
        buttons.Controls.Add(_systemSelectPreviousCharacterButton);
        buttons.Controls.Add(_systemSelectNextCharacterButton);
        buttons.Controls.Add(_systemDeletePreviousCharacterButton);
        buttons.Controls.Add(_systemDeleteNextCharacterButton);
        buttons.Controls.Add(_systemMoveLineStartButton);
        buttons.Controls.Add(_systemMoveLineEndButton);
        buttons.Controls.Add(_systemMovePreviousLineButton);
        buttons.Controls.Add(_systemMoveNextLineButton);
        buttons.Controls.Add(_systemSelectToLineStartButton);
        buttons.Controls.Add(_systemSelectToLineEndButton);
        buttons.Controls.Add(_systemSelectPreviousLineButton);
        buttons.Controls.Add(_systemSelectNextLineButton);
        buttons.Controls.Add(_systemDeleteToLineStartButton);
        buttons.Controls.Add(_systemDeleteToLineEndButton);
        buttons.Controls.Add(_systemDeletePreviousLineButton);
        buttons.Controls.Add(_systemDeleteNextLineButton);
        buttons.Controls.Add(_systemMovePreviousWordButton);
        buttons.Controls.Add(_systemMoveNextWordButton);
        buttons.Controls.Add(_systemSelectPreviousWordButton);
        buttons.Controls.Add(_systemSelectNextWordButton);
        buttons.Controls.Add(_systemDeletePreviousWordButton);
        buttons.Controls.Add(_systemDeleteNextWordButton);
        buttons.Controls.Add(_systemMovePreviousSentenceButton);
        buttons.Controls.Add(_systemMoveNextSentenceButton);
        buttons.Controls.Add(_systemSelectPreviousSentenceButton);
        buttons.Controls.Add(_systemSelectNextSentenceButton);
        buttons.Controls.Add(_systemDeletePreviousSentenceButton);
        buttons.Controls.Add(_systemDeleteNextSentenceButton);
        buttons.Controls.Add(_systemMovePreviousParagraphButton);
        buttons.Controls.Add(_systemMoveNextParagraphButton);
        buttons.Controls.Add(_systemSelectPreviousParagraphButton);
        buttons.Controls.Add(_systemSelectNextParagraphButton);
        buttons.Controls.Add(_systemDeletePreviousParagraphButton);
        buttons.Controls.Add(_systemDeleteNextParagraphButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildFileSearchTab()
    {
        var tab = new TabPage("Files");
        var layout = BuildTwoColumnLayout(12);

        var heading = CreateHeading("Search local files and open the result");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Searches the intended alpha scope: common user folders plus Callsign data. Results show clearly, empty states are explained, and selected items can be opened."
        };
        _fileSearchStatusLabel = CreateStatusLabel("No file search run yet.");
        _fileSearchSafetyLabel = CreateStatusLabel("Safety: file search stays in common user folders and Callsign data. Results are shown before action; executable or script-like files are blocked from direct open and should be revealed in Explorer instead.");
        _fileSearchSafetyLabel.AccessibleName = "Files search safety";
        _fileSearchSafetyLabel.AccessibleDescription = "Explains the allowed file-search scope, visible result review, and blocked direct-open behavior for executable or script-like files.";
        _fileSearchSelectionLabel = CreateStatusLabel("Selected result: none.");
        _fileSearchVoiceCueLabel = CreateStatusLabel("Voice cue: file search is waiting for speech.");
        _fileSearchLastHeardLabel = CreateStatusLabel("Last heard: nothing yet.");
        _fileSearchLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _fileSearchQueryText = BuildTextInput("Search");
        _fileSearchQueryText.PlaceholderText = "Search for a filename or folder name";
        _fileSearchQueryText.TextChanged += (_, _) => RefreshFileSearchPanel();
        _fileSearchResultsList = CreateShellListBox(220);
        _fileSearchResultsList.AccessibleName = "Search results";
        _fileSearchResultsList.SelectedIndexChanged += (_, _) => RefreshFileSearchPanel();
        _fileSearchResultsList.DoubleClick += (_, _) => OpenSelectedFileResult();

        _searchFilesButton = new Button { Text = "Search Files", Width = 120, AccessibleName = "Files search", AccessibleDescription = "Voice phrase: search my files." };
        _searchFilesButton.Click += (_, _) => SearchFiles();

        _fileSearchResultNumber = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 1,
            Increment = 1,
            Value = 1,
            Width = 90,
            Enabled = false,
            AccessibleName = "Files result number",
            AccessibleDescription = "Choose which visible file result number to target with spoken result commands."
        };

        _selectFileResultButton = new Button
        {
            Text = "Select Result #",
            Width = 125,
            Enabled = false,
            AccessibleName = "Files select result number",
            AccessibleDescription = "Voice phrases: select result 1, select first result, choose result thirty second."
        };
        _selectFileResultButton.Click += (_, _) => SelectFileSearchResult((int)_fileSearchResultNumber.Value);

        _openFileResultButton = new Button { Text = "Open Result", Width = 110, AccessibleName = "Files open selected result", AccessibleDescription = "Voice phrase: open selected file." };
        _openFileResultButton.Click += (_, _) => OpenSelectedFileResult();

        _openFileFolderButton = new Button { Text = "Open Folder", Width = 110, AccessibleName = "Files reveal selected result", AccessibleDescription = "Voice phrase: reveal selected file." };
        _openFileFolderButton.Click += (_, _) => OpenSelectedFileFolder();

        _openFileResultByNumberButton = new Button
        {
            Text = "Open Result #",
            Width = 120,
            Enabled = false,
            AccessibleName = "Files open result number",
            AccessibleDescription = "Voice phrases: open file result 1, open result twenty one."
        };
        _openFileResultByNumberButton.Click += (_, _) => OpenFileSearchResultByNumber((int)_fileSearchResultNumber.Value);

        _openFileFolderByNumberButton = new Button
        {
            Text = "Reveal Result #",
            Width = 125,
            Enabled = false,
            AccessibleName = "Files reveal result number",
            AccessibleDescription = "Voice phrases: reveal file result 1, open containing folder for result 1, show containing folder for result 2."
        };
        _openFileFolderByNumberButton.Click += (_, _) => RevealFileSearchResultByNumber((int)_fileSearchResultNumber.Value);

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _fileSearchSafetyLabel, row++);
        AddRow(layout, "Search", _fileSearchQueryText, row++);
        AddRow(layout, "Status", _fileSearchStatusLabel, row++);
        AddRow(layout, "Selected", _fileSearchSelectionLabel, row++);
        AddRow(layout, "Voice cue", _fileSearchVoiceCueLabel, row++);
        AddRow(layout, "Last heard", _fileSearchLastHeardLabel, row++);
        AddRow(layout, "Last action", _fileSearchLastActionLabel, row++);
        AddFullWidth(layout, _fileSearchResultsList, row++);

        var resultActions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        resultActions.Controls.Add(_fileSearchResultNumber);
        resultActions.Controls.Add(_selectFileResultButton);
        resultActions.Controls.Add(_openFileResultByNumberButton);
        resultActions.Controls.Add(_openFileFolderByNumberButton);
        AddRow(layout, "Result #", resultActions, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_searchFilesButton);
        buttons.Controls.Add(_openFileResultButton);
        buttons.Controls.Add(_openFileFolderButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildPacksTab()
    {
        var tab = new TabPage("Packs");
        var layout = BuildTwoColumnLayout(11);

        var heading = CreateHeading("Load extension packs from a local folder");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Installed packs are discovered from the local Callsign packs folder. Drag and drop one or more `.dll` files (or folders of .dlls) to import. Community packs stay disabled by default, but their command metadata remains visible so you can review permissions before enabling them."
        };
        _packsRootLabel = CreateStatusLabel("Pack folder: not loaded yet.");
        _packsStatusLabel = CreateStatusLabel("No packs scanned yet.");
        _packsDropZoneLabel = CreateStatusLabel("Drop community command pack .dll files or folders here. Imports are copied locally, disabled by default, and must be reviewed before enablement.");
        _packsDropZoneLabel.AccessibleName = "Packs drop zone";
        _packsDropZoneLabel.AccessibleDescription = "Visible drag-and-drop target for community command pack DLL files or folders. Dropped packs are imported disabled by default so tier, signature, source, and command permissions can be reviewed before enablement.";
        _packsDropZoneLabel.BackColor = Color.FromArgb(239, 246, 255);
        _packsDropZoneLabel.Padding = new Padding(10, 8, 10, 8);
        _packsDropZoneLabel.AllowDrop = true;
        _packsDropZoneLabel.DragEnter += (_, e) => PacksDropEnter(e);
        _packsDropZoneLabel.DragDrop += (_, e) => PacksDrop(_packsDropZoneLabel, e);
        _packsSelectedSummaryLabel = CreateStatusLabel("Selected pack: none. Tier, signature, source, and command-gate details will appear here.");
        _packsSelectedSummaryLabel.AccessibleName = "Selected pack summary";
        _packsSelectedSummaryLabel.AccessibleDescription = "Shows the selected pack tier, load status, source, signature status, import status, and command gate before enablement.";
        _packsEnablementLabel = CreateStatusLabel("Enablement readiness: select a pack to see whether commands can run, are disabled for review, or are blocked by signature or entitlement.");
        _packsEnablementLabel.AccessibleName = "Pack enablement readiness";
        _packsEnablementLabel.AccessibleDescription = "Explains whether the selected pack can be enabled, is disabled for review, or is blocked by signature, entitlement, invalid metadata, or missing files.";
        _packsList = CreateShellListBox(180);
        _packsList.AccessibleName = "Installed packs";
        _packsList.AccessibleDescription = "Installed pack rows show display name, version, tier, load status, and high-level entitlement or signature gates.";
        _packsList.AllowDrop = true;
        _packsList.SelectedIndexChanged += (_, _) => RefreshSelectedPackCommands();
        _packsList.DragEnter += (_, e) => PacksDropEnter(e);
        _packsList.DragDrop += (_, e) => PacksDrop(_packsList, e);
        _packCommandsList = CreateShellListBox(180);
        _packCommandsList.AccessibleName = "Pack commands";
        _packCommandsList.AccessibleDescription = "Shows the selected pack security summary plus command metadata including risk, privacy, approval, and visibility.";

        _refreshPacksButton = new Button { Text = "Refresh Packs", Width = 120, AccessibleName = "Packs refresh", AccessibleDescription = "Voice phrase: refresh packs." };
        _refreshPacksButton.Click += (_, _) => RefreshPacksPanel(forceReload: true);

        _importPackButton = new Button { Text = "Import Pack...", Width = 120, AccessibleName = "Packs import pack", AccessibleDescription = "Voice phrase: import extension pack." };
        _importPackButton.Click += (_, _) => ImportCommunityPack();

        _importPackFolderButton = new Button { Text = "Import Folder...", Width = 120, AccessibleName = "Packs import folder", AccessibleDescription = "Voice phrase: import extension folder." };
        _importPackFolderButton.Click += (_, _) => ImportCommunityPackFolder();

        _openPacksFolderButton = new Button { Text = "Open Folder", Width = 110, AccessibleName = "Packs open folder", AccessibleDescription = "Voice phrase: open packs folder." };
        _openPacksFolderButton.Click += (_, _) => OpenPacksFolder();

        _enablePackButton = new Button { Text = "Enable", Width = 90, AccessibleName = "Packs enable selected pack", AccessibleDescription = "Voice phrase: enable selected pack." };
        _enablePackButton.Click += (_, _) => ToggleSelectedPack(enabled: true);

        _disablePackButton = new Button { Text = "Disable", Width = 90, AccessibleName = "Packs disable selected pack", AccessibleDescription = "Voice phrase: disable selected pack." };
        _disablePackButton.Click += (_, _) => ToggleSelectedPack(enabled: false);

        _removePackButton = new Button { Text = "Remove", Width = 90, AccessibleName = "Packs remove selected pack", AccessibleDescription = "Voice phrase: remove selected pack." };
        _removePackButton.Click += (_, _) => RemoveSelectedPack();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Drop zone", _packsDropZoneLabel, row++);
        AddRow(layout, "Folder", _packsRootLabel, row++);
        AddRow(layout, "Status", _packsStatusLabel, row++);
        AddRow(layout, "Selected pack", _packsSelectedSummaryLabel, row++);
        AddRow(layout, "Enablement", _packsEnablementLabel, row++);
        AddFullWidth(layout, _packsList, row++);
        AddFullWidth(layout, _packCommandsList, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_refreshPacksButton);
        buttons.Controls.Add(_importPackButton);
        buttons.Controls.Add(_importPackFolderButton);
        buttons.Controls.Add(_openPacksFolderButton);
        buttons.Controls.Add(_enablePackButton);
        buttons.Controls.Add(_disablePackButton);
        buttons.Controls.Add(_removePackButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildUpdatesTab()
    {
        var tab = new TabPage("Updates");
        var layout = BuildTwoColumnLayout(9);

        var heading = CreateHeading("Update checks, server state, and visible install readiness");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Callsign phones home on startup and every 25 hours while running, then checks the update server for a manifest and installer. The update flow stays visible so users can see the server, cadence, last check, and next due time."
        };

        _updatesServerLabel = CreateStatusLabel("Update server: not checked yet.");
        _updatesServerLabel.AccessibleName = "Update server";
        _updatesServerLabel.AccessibleDescription = "Shows the configured update server URL and channel.";
        _updatesCadenceLabel = CreateStatusLabel("Cadence: checks on startup and every 25 hours while Callsign is running.");
        _updatesCadenceLabel.AccessibleName = "Update cadence";
        _updatesCadenceLabel.AccessibleDescription = "Shows when Callsign checks for updates while it is running.";
        _updatesStateLabel = CreateStatusLabel("Last check: never.");
        _updatesStateLabel.AccessibleName = "Update check state";
        _updatesStateLabel.AccessibleDescription = "Shows the last update check time, next due time, and known version.";
        _updatesPendingLabel = CreateStatusLabel("Pending update: none yet.");
        _updatesPendingLabel.AccessibleName = "Pending update manifest";
        _updatesPendingLabel.AccessibleDescription = "Shows the latest known manifest and whether an installer has been launched.";

        _checkUpdatesButton = new Button
        {
            Text = "Check Now",
            Width = 110,
            AccessibleName = "Updates check now",
            AccessibleDescription = "Voice phrase: check for updates now."
        };
        _checkUpdatesButton.Click += (_, _) =>
        {
            UpdateStatus("Checking for updates now.");
            _ = CheckForUpdatesAsync(force: true, attemptInstall: true);
        };

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Server", _updatesServerLabel, row++);
        AddRow(layout, "Cadence", _updatesCadenceLabel, row++);
        AddRow(layout, "Status", _updatesStateLabel, row++);
        AddRow(layout, "Pending", _updatesPendingLabel, row++);
        layout.Controls.Add(_checkUpdatesButton, 1, row);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildShortcutsTab()
    {
        var tab = new TabPage("Shortcuts");
        var layout = BuildTwoColumnLayout(14);

        var heading = CreateHeading("Create local voice shortcuts from visible Callsign commands");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Voice shortcuts let you save a spoken phrase that runs one to eight existing Callsign commands with optional bounded wait steps. Each step still routes through Callsign's visible policy-gated command pipeline."
        };

        _voiceShortcutStatusLabel = CreateStatusLabel("No voice shortcuts saved yet.");
        _voiceShortcutSafetyLabel = CreateStatusLabel("Safety: voice shortcuts compose existing visible Callsign commands. Every shortcut still requires wake, identity, policy, visibility, audit, and any paid entitlement gates; bounded waits do not add new privileges.");
        _voiceShortcutSafetyLabel.AccessibleName = "Voice shortcuts safety";
        _voiceShortcutSafetyLabel.AccessibleDescription = "Explains that local voice shortcuts run only through existing Callsign commands and cannot bypass wake, identity, policy, visibility, audit, or entitlement gates.";
        _voiceShortcutsList = CreateShellListBox(170);
        _voiceShortcutsList.AccessibleName = "Voice shortcuts";
        _voiceShortcutsList.SelectedIndexChanged += (_, _) => SelectVoiceShortcutFromList();

        _voiceShortcutActionsList = CreateShellListBox(150);
        _voiceShortcutActionsList.AccessibleName = "Voice shortcut actions";

        _voiceShortcutTitleText = BuildTextInput("Voice shortcut title");
        _voiceShortcutPhraseText = BuildTextInput("Voice shortcut spoken phrase");
        _voiceShortcutGroupText = BuildTextInput("Voice shortcut group");
        _voiceShortcutCommandActionText = BuildTextInput("Voice shortcut command action");
        _voiceShortcutWaitMilliseconds = new NumericUpDown
        {
            Minimum = VoiceShortcutConstants.MinWaitMilliseconds,
            Maximum = VoiceShortcutConstants.MaxWaitMilliseconds,
            Increment = 100,
            Value = 1000,
            Width = 140,
            AccessibleName = "Voice shortcut wait milliseconds"
        };

        _newVoiceShortcutButton = CreateShellButton("New Shortcut", 130);
        _newVoiceShortcutButton.AccessibleName = "Voice shortcuts create new";
        _newVoiceShortcutButton.AccessibleDescription = "Voice phrase: new voice shortcut.";
        _newVoiceShortcutButton.Click += (_, _) => CreateNewVoiceShortcut();

        _saveVoiceShortcutButton = CreateShellButton("Save Shortcut", 130);
        _saveVoiceShortcutButton.AccessibleName = "Voice shortcuts save";
        _saveVoiceShortcutButton.AccessibleDescription = "Voice phrase: save voice shortcut.";
        _saveVoiceShortcutButton.Click += (_, _) => SaveVoiceShortcut();

        _deleteVoiceShortcutButton = CreateShellButton("Delete Shortcut", 130);
        _deleteVoiceShortcutButton.AccessibleName = "Voice shortcuts delete";
        _deleteVoiceShortcutButton.AccessibleDescription = "Voice phrase: delete voice shortcut.";
        _deleteVoiceShortcutButton.Click += (_, _) => DeleteSelectedVoiceShortcut();

        _enableVoiceShortcutButton = CreateShellButton("Enable", 90);
        _enableVoiceShortcutButton.AccessibleName = "Voice shortcuts enable selected";
        _enableVoiceShortcutButton.AccessibleDescription = "Voice phrase: enable voice shortcut.";
        _enableVoiceShortcutButton.Click += (_, _) => SetSelectedVoiceShortcutEnabled(true);

        _disableVoiceShortcutButton = CreateShellButton("Disable", 90);
        _disableVoiceShortcutButton.AccessibleName = "Voice shortcuts disable selected";
        _disableVoiceShortcutButton.AccessibleDescription = "Voice phrase: disable voice shortcut.";
        _disableVoiceShortcutButton.Click += (_, _) => SetSelectedVoiceShortcutEnabled(false);

        _addVoiceShortcutCommandButton = CreateShellButton("Add Command", 120);
        _addVoiceShortcutCommandButton.AccessibleName = "Voice shortcuts add command action";
        _addVoiceShortcutCommandButton.AccessibleDescription = "Voice phrase: add voice shortcut command action.";
        _addVoiceShortcutCommandButton.Click += (_, _) => AddVoiceShortcutCommandAction();

        _addVoiceShortcutWaitButton = CreateShellButton("Add Wait", 100);
        _addVoiceShortcutWaitButton.AccessibleName = "Voice shortcuts add wait action";
        _addVoiceShortcutWaitButton.AccessibleDescription = "Voice phrase: add voice shortcut wait action.";
        _addVoiceShortcutWaitButton.Click += (_, _) => AddVoiceShortcutWaitAction();

        _removeVoiceShortcutActionButton = CreateShellButton("Remove Action", 120);
        _removeVoiceShortcutActionButton.AccessibleName = "Voice shortcuts remove action";
        _removeVoiceShortcutActionButton.AccessibleDescription = "Voice phrase: remove voice shortcut action.";
        _removeVoiceShortcutActionButton.Click += (_, _) => RemoveSelectedVoiceShortcutAction();

        _moveVoiceShortcutActionUpButton = CreateShellButton("Move Up", 100);
        _moveVoiceShortcutActionUpButton.AccessibleName = "Voice shortcuts move action up";
        _moveVoiceShortcutActionUpButton.AccessibleDescription = "Voice phrase: move shortcut action up.";
        _moveVoiceShortcutActionUpButton.Click += (_, _) => MoveSelectedVoiceShortcutAction(-1);

        _moveVoiceShortcutActionDownButton = CreateShellButton("Move Down", 110);
        _moveVoiceShortcutActionDownButton.AccessibleName = "Voice shortcuts move action down";
        _moveVoiceShortcutActionDownButton.AccessibleDescription = "Voice phrase: move shortcut action down.";
        _moveVoiceShortcutActionDownButton.Click += (_, _) => MoveSelectedVoiceShortcutAction(1);

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _voiceShortcutSafetyLabel, row++);
        AddRow(layout, "Status", _voiceShortcutStatusLabel, row++);
        AddFullWidth(layout, _voiceShortcutsList, row++);
        AddRow(layout, "Title", _voiceShortcutTitleText, row++);
        AddRow(layout, "When I say", _voiceShortcutPhraseText, row++);
        AddRow(layout, "Group", _voiceShortcutGroupText, row++);

        var commandActionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        commandActionPanel.Controls.Add(_voiceShortcutCommandActionText);
        commandActionPanel.Controls.Add(_addVoiceShortcutCommandButton);
        _voiceShortcutCommandActionText.Width = 420;
        AddRow(layout, "Command step", commandActionPanel, row++);

        var waitPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        waitPanel.Controls.Add(_voiceShortcutWaitMilliseconds);
        waitPanel.Controls.Add(_addVoiceShortcutWaitButton);
        AddRow(layout, "Wait step", waitPanel, row++);

        AddFullWidth(layout, _voiceShortcutActionsList, row++);

        var actionButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        actionButtons.Controls.Add(_removeVoiceShortcutActionButton);
        actionButtons.Controls.Add(_moveVoiceShortcutActionUpButton);
        actionButtons.Controls.Add(_moveVoiceShortcutActionDownButton);
        AddFullWidth(layout, actionButtons, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_newVoiceShortcutButton);
        buttons.Controls.Add(_saveVoiceShortcutButton);
        buttons.Controls.Add(_deleteVoiceShortcutButton);
        buttons.Controls.Add(_enableVoiceShortcutButton);
        buttons.Controls.Add(_disableVoiceShortcutButton);
        AddFullWidth(layout, buttons, row++);

        tab.Controls.Add(layout);
        return tab;
    }

    private void RefreshCommandRegistry()
    {
        CallsignCommandRegistry.Shared.Refresh();
        CallsignCommandRegistry.Shared.RegisterPack(new VoiceShortcutCommandPack(_voiceShortcutStore.GetShortcuts()), "<voice-shortcuts>");
    }

    private sealed record PackListItem(CallsignPackInfo Pack)
    {
        public override string ToString() =>
            FormatPackListDisplay(Pack);
    }

    private sealed record VoiceShortcutListItem(VoiceShortcutDefinition Shortcut)
    {
        public override string ToString()
        {
            var state = Shortcut.Enabled ? "Enabled" : "Disabled";
            return $"{Shortcut.Title} ({state}) - say '{Shortcut.WhenISay}'";
        }
    }

    public static string FormatPackListDisplay(CallsignPackInfo pack)
    {
        var source = pack.IsCommunity ? " community" : string.Empty;
        var gate = pack.LoadStatus switch
        {
            CallsignPackLoadStatus.EntitlementRequired => " - entitlement required",
            CallsignPackLoadStatus.SignatureRequired => " - signature required",
            _ => string.Empty
        };

        return $"{pack.DisplayName} v{pack.Version} [{pack.Tier}] ({pack.LoadStatus}){source}{gate}";
    }

    public static string FormatPackSecuritySummary(CallsignPackInfo pack)
    {
        var signatureStatus = string.IsNullOrWhiteSpace(pack.SignatureStatus)
            ? "unknown"
            : pack.SignatureStatus;
        var signatureRequirement = pack.RequiresSignature ? "required" : "not required";
        var source = pack.IsCommunity ? "community" : "built-in or trusted source";
        var importState = pack.WasImported ? "imported" : "not imported";
        var gate = pack.LoadStatus switch
        {
            CallsignPackLoadStatus.EntitlementRequired => $"{pack.Tier} entitlement required before commands can run",
            CallsignPackLoadStatus.SignatureRequired => "valid signature required before commands can run",
            CallsignPackLoadStatus.Disabled => "disabled until the user enables it",
            CallsignPackLoadStatus.Loaded => "loadable; commands still require wake, identity, policy, visibility, and audit",
            _ => pack.Message
        };

        return $"Security: tier={pack.Tier}; status={pack.LoadStatus}; signature={signatureStatus} ({signatureRequirement}); source={source}; import={importState}; gate={gate}.";
    }

    public static string FormatPackEnablementReadiness(CallsignPackInfo pack)
    {
        var commandText = pack.CommandCount == 1 ? "1 command" : $"{pack.CommandCount} commands";
        var reviewText = "Review tier, signature, risk, privacy, approval, and visibility before enabling.";
        return pack.LoadStatus switch
        {
            CallsignPackLoadStatus.Loaded =>
                $"Enablement readiness: enabled; {commandText} may route only after wake, identity, policy, visibility, and audit gates.",
            CallsignPackLoadStatus.Disabled =>
                $"Enablement readiness: disabled for review; {commandText} visible. {reviewText}",
            CallsignPackLoadStatus.EntitlementRequired =>
                $"Enablement readiness: blocked; {pack.Tier} entitlement required before {commandText} can route.",
            CallsignPackLoadStatus.SignatureRequired =>
                $"Enablement readiness: blocked; a valid signed pack is required before {commandText} can route.",
            CallsignPackLoadStatus.InvalidPack =>
                $"Enablement readiness: blocked; command metadata is invalid. {reviewText}",
            CallsignPackLoadStatus.MissingAssembly =>
                "Enablement readiness: blocked; the pack assembly file is missing.",
            CallsignPackLoadStatus.MissingPackType =>
                "Enablement readiness: blocked; no command pack type was found in the assembly.",
            CallsignPackLoadStatus.DuplicatePackId =>
                "Enablement readiness: blocked; another command pack already uses this pack id.",
            CallsignPackLoadStatus.LoadFailure =>
                $"Enablement readiness: blocked; the pack failed to load. {pack.Message}",
            _ =>
                $"Enablement readiness: blocked; {pack.Message}"
        };
    }

    private static TableLayoutPanel BuildTwoColumnLayout(int rowCount)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = rowCount
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (var i = 0; i < rowCount; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        return layout;
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            ForeColor = Color.FromArgb(71, 85, 105)
        }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddFullWidth(TableLayoutPanel layout, Control control, int row)
    {
        layout.Controls.Add(control, 0, row);
        layout.SetColumnSpan(control, 2);
    }

    private static TextBox BuildTextInput(string? accessibleName = null) =>
        new()
        {
            Dock = DockStyle.Fill,
            AccessibleName = accessibleName,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 4)
        };

    private static Label CreateHeading(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };

    private static Button CreateShellButton(string text, int width, int height = 40)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 8, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 230);
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    private static ListBox CreateShellListBox(int height)
    {
        return new ListBox
        {
            Dock = DockStyle.Fill,
            Height = height,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
        };
    }

    private static Label CreateStatusLabel(string text) =>
        new()
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = text
        };

    private void LoadProfiles()
    {
        _updatingUi = true;
        try
        {
            _profiles.Clear();
            _profiles.AddRange(_profileStore.GetProfiles());
            _profilePicker.Items.Clear();
            foreach (var profile in _profiles)
                _profilePicker.Items.Add(profile.StorageLabel);

            if (_profiles.Count > 0)
            {
                _profilePicker.SelectedIndex = 0;
                SelectProfile(_profiles[0]);
            }
            else
            {
                CreateNewProfile();
            }
        }
        finally
        {
            _updatingUi = false;
        }

        RefreshAllPanels();
    }

    private async Task CheckForUpdatesAsync(bool force = false, bool attemptInstall = true)
    {
        if (IsDisposed)
            return;

        if (_updateCheckInProgress)
            return;

        _updateCheckInProgress = true;
        try
        {
            await _updateCheckService.SendCheckInAsync(_activeProfile?.Callsign, GetInstalledVersion());
            var result = await _updateCheckService.CheckForUpdateAsync(
                force: force,
                attemptInstall: attemptInstall,
                showProgress: message =>
                {
                    RunOnUiThread(() => UpdateStatus(message));
                });

            if (!result.Succeeded)
                return;

            if (result.Manifest is null)
                return;

            if (ShouldShowSplashForManifest(result.Manifest))
                RunOnUiThread(() => ShowUpdateSplash(result.Manifest));

            if (result.InstallerStarted)
                UpdateStatus("Update installer started in background.");
            else if (result.UpdateAvailable && !result.ManualInstallRecommended && !result.InstallerStarted)
                UpdateStatus($"Update {result.Manifest.Version} is available: {result.Message}");

            RunOnUiThread(RefreshUpdatesPanel);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Update check failed: {ex.Message}");
            RunOnUiThread(RefreshUpdatesPanel);
        }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private bool ShouldShowSplashForManifest(CallsignUpdateManifest manifest)
    {
        if (_lastShownManifestVersion == manifest.Version)
            return false;

        var hasAnyChanges = (manifest.AddedCommands?.Count > 0)
            || (manifest.ChangedCommands?.Count > 0)
            || (manifest.RemovedCommands?.Count > 0)
            || (manifest.ExtensionPackChanges?.Count > 0)
            || !string.IsNullOrWhiteSpace(manifest.SplashSummary)
            || !string.IsNullOrWhiteSpace(manifest.ReleaseNotes);

        if (!hasAnyChanges)
            return false;

        _lastShownManifestVersion = manifest.Version;
        PersistLastShownUpdateManifestVersion(manifest.Version);
        return true;
    }

    private void ShowUpdateSplash(CallsignUpdateManifest manifest)
    {
        ShowManifestSplash(manifest, persistLastShownVersion: true);
    }

    private void ShowPackImportSplash(CallsignUpdateManifest manifest)
    {
        ShowManifestSplash(manifest, persistLastShownVersion: false);
    }

    private void ShowManifestSplash(CallsignUpdateManifest manifest, bool persistLastShownVersion)
    {
        if (persistLastShownVersion && !ShouldShowSplashForManifest(manifest))
            return;

        if (_updateSplash != null && !_updateSplash.IsDisposed)
        {
            _updateSplash.Hide();
            _updateSplash.Dispose();
            _updateSplash = null;
        }

        _updateSplash = new UpdateSplashForm(manifest, isImportSplash: !persistLastShownVersion);
        _updateSplash.Show(this);
        _updateSplash.BringToFront();

        RecordExtensionPackUiAudit(
            "update_splash",
            "succeeded",
            manifest.Version,
            persistLastShownVersion ? "shown" : "shown_pack_import",
            true,
            persistLastShownVersion
                ? "Update splash was shown in the visible update surface."
                : "Pack import splash was shown in the visible import surface.");
    }

    private void HideUpdateSplash()
    {
        if (_updateSplash?.Visible == true)
            _updateSplash.Hide();

        UpdateStatus("Update splash hidden.");
        RecordExtensionPackUiAudit(
            "update_splash",
            "succeeded",
            "hide_update_splash",
            "hidden",
            true,
            "Update splash dismissal was shown in the visible update surface.");
    }

    private static string BuildPackImportSummary(CallsignPackInfo pack)
    {
        var commandText = pack.CommandCount == 1 ? "1 command" : $"{pack.CommandCount} commands";
        var source = pack.IsCommunity ? "community" : "trusted";
        var signature = string.IsNullOrWhiteSpace(pack.SignatureStatus) ? "unknown signature" : pack.SignatureStatus;
        return $"Imported {commandText} from the {source} pack '{pack.DisplayName}' ({pack.Tier}, {signature}).";
    }

    public static int FindPreferredPackIndex(IReadOnlyList<CallsignPackInfo> packs, string? preferredPackId)
    {
        if (string.IsNullOrWhiteSpace(preferredPackId))
            return -1;

        for (var index = 0; index < packs.Count; index++)
        {
            if (string.Equals(packs[index].PackId, preferredPackId, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }

    private string GetInstalledVersion()
    {
        try
        {
            var file = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
            return file.ProductVersion ?? file.FileVersion ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private string? LoadLastShownUpdateManifestVersion()
    {
        try
        {
            if (!File.Exists(_updateSplashStatePath))
                return null;

            var json = File.ReadAllText(_updateSplashStatePath);
            var state = JsonSerializer.Deserialize<UpdateSplashState>(json);
            return state?.LastShownManifestVersion;
        }
        catch
        {
            return null;
        }
    }

    private void PersistLastShownUpdateManifestVersion(string version)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_updateSplashStatePath)!);
            File.WriteAllText(_updateSplashStatePath, JsonSerializer.Serialize(new UpdateSplashState(version)));
        }
        catch
        {
            // Non-blocking persistence.
        }
    }

    private bool HasSeenStartupWalkthrough()
    {
        try
        {
            if (!File.Exists(_startupWalkthroughStatePath))
                return false;

            var json = File.ReadAllText(_startupWalkthroughStatePath);
            var state = JsonSerializer.Deserialize<StartupWalkthroughState>(json);
            return state?.HasSeenWalkthrough == true;
        }
        catch
        {
            return false;
        }
    }

    private void PersistStartupWalkthroughSeen()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_startupWalkthroughStatePath)!);
            File.WriteAllText(_startupWalkthroughStatePath, JsonSerializer.Serialize(new StartupWalkthroughState(true)));
        }
        catch
        {
            // Non-blocking persistence.
        }
    }

    private void ProfilePickerChanged(object? sender, EventArgs e)
    {
        if (_updatingUi || _profilePicker.SelectedIndex < 0 || _profilePicker.SelectedIndex >= _profiles.Count)
            return;

        SelectProfile(_profiles[_profilePicker.SelectedIndex]);
    }

    private void ShowStartupWalkthroughIfNeeded()
    {
        if (_startupWalkthroughShownThisSession || HasSeenStartupWalkthrough())
            return;

        using var walkthrough = new StartupWalkthroughForm(tabName => SelectTab(tabName));
        var result = walkthrough.ShowDialog(this);
        _startupWalkthroughShownThisSession = true;

        if (result == DialogResult.OK)
            PersistStartupWalkthroughSeen();
    }

    private void CreateNewProfile()
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        _updatingUi = true;
        try
        {
            _activeProfile = new UserProfile();
            _profilePicker.SelectedIndex = -1;
            _callsignText.ReadOnly = false;
            _callsignText.Text = string.Empty;
            _displayNameText.Text = string.Empty;
            _emailText.Text = string.Empty;
            _departmentText.Text = string.Empty;
            _notesText.Text = string.Empty;
            _spokenCallsignText.Text = string.Empty;
            _spokenCommandText.Text = "Launch Notepad";
            _appNameText.Text = "Notepad";
            _voicePhraseText.Text = "Callsign Alpha open Notepad";
            _session.Reset();
        }
        finally
        {
            _updatingUi = false;
        }

        RefreshAllPanels();
        UpdateStatus("Create a new account, then save it before voice activation.");
    }

    private void SelectProfile(UserProfile profile)
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        _activeProfile = profile;
        _updatingUi = true;
        try
        {
            LoadProfileIntoControls(profile);
            _callsignText.ReadOnly = true;
            _spokenCallsignText.Text = profile.Callsign;
            _spokenCommandText.Text = profile.Settings.LastLaunchedApp == null
                ? "Launch Notepad"
                : $"Launch {profile.Settings.LastLaunchedApp}";
            _appNameText.Text = profile.Settings.LastLaunchedApp ?? "Notepad";
            _voicePhraseText.Text = $"Callsign {profile.Callsign} open {_appNameText.Text}";
            _session.Reset();
        }
        finally
        {
            _updatingUi = false;
        }

        RefreshAllPanels();
        UpdateStatus($"Editing account '{profile.Callsign}'.");
        if (_formReadyForListener)
            TryStartListenerForActiveProfile();
    }

    private void LoadProfileIntoControls(UserProfile profile)
    {
        _callsignText.Text = profile.Callsign;
        _displayNameText.Text = profile.DisplayName;
        _emailText.Text = profile.Email;
        _departmentText.Text = profile.Department;
        _notesText.Text = profile.Notes;
    }

    private void RefreshAllPanels()
    {
        RefreshAccountPanel();
        RefreshVoicePanel();
        RefreshSessionPanel();
        RefreshDictationPanel();
        RefreshVoiceShortcutsPanel();
        RefreshBrowserPanel();
        RefreshFileSearchPanel();
        RefreshPacksPanel();
        RefreshUpdatesPanel();
    }

    private void RefreshAccountPanel()
    {
        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            _accountPathLabel.Text = "No profile selected.";
            _accountStateLabel.Text = "Voice not activated.";
            _newProfileButton.Enabled = true;
            _saveProfileButton.Enabled = true;
            _deleteProfileButton.Enabled = false;
            return;
        }

        _accountPathLabel.Text = $"{_profileStore.ResolveCallsSignFolder(_activeProfile.Callsign)} (settings.json and alpha-audit.jsonl)";
        _accountStateLabel.Text = GetVoiceStatusText(_activeProfile.Settings);
        _deleteProfileButton.Enabled = true;
    }

    private void RefreshVoicePanel()
    {
        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            _voiceStateLabel.Text = _activeProfile == null ? "No account selected." : "Save the account before voice activation.";
            _voiceSamplesLabel.Text = "0 / 0 samples";
            _voiceLastTrainedLabel.Text = "Never activated.";
            _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription} | voice mode: {_voiceAccessMode}";
            _voiceRecordingStateLabel.Text = "No sample recording in progress.";
            _voicePlaybackStateLabel.Text = "No sample available for playback.";
            _voiceNextStepLabel.Text = "Next step: create or pick a profile.";
            _voiceFailureLabel.Text = "Failure type: none yet.";
            _voiceProgress.Value = 0;
            _voiceProgress.Maximum = 1;
            _voicePromptText.Text = _activeProfile == null
                ? "Create an account first."
                : "Enter a callsign and save the account before recording voice samples.";
            _recordSampleButton.Enabled = false;
            _playSampleButton.Enabled = false;
            _trainVoiceButton.Enabled = false;
            _resetVoiceButton.Enabled = false;
            UpdateRecordButtonAppearance();
            return;
        }

        var settings = _activeProfile.Settings;
        settings.VoiceSamplesRequired = Math.Max(3, settings.VoiceSamplesRequired);
        var recordedSamplePaths = GetRecordedVoiceSamplePaths(_activeProfile);
        var recordedSampleCount = Math.Max(0, recordedSamplePaths.Count);
        if (settings.VoiceSamplesRecorded != recordedSampleCount)
            settings.VoiceSamplesRecorded = recordedSampleCount;

        _voiceStateLabel.Text = _voiceActivationBusy
            ? "Enrolling voice identity with pyannote..."
            : settings.VoiceSamplesRecorded < 3
                ? "Voice fingerprint is weak: collect 3 fresh samples."
                : settings.VoiceEnrollmentStatus;
        _voiceSamplesLabel.Text = $"{settings.VoiceSamplesRecorded} / {settings.VoiceSamplesRequired} samples";
        _voiceLastTrainedLabel.Text = settings.VoiceEnrolledUtc.HasValue
            ? settings.VoiceEnrolledUtc.Value.ToLocalTime().ToString("f", CultureInfo.CurrentCulture)
            : "Never activated.";
        var micSummary = settings.VoiceAutoGainEnabled
            ? $" Mic gain {settings.VoiceInputGainDb:0.0} dB with auto gain toward RMS {settings.VoiceTargetRms:0.000}."
            : $" Mic gain {settings.VoiceInputGainDb:0.0} dB with auto gain off.";
        _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription} | voice mode: {_voiceAccessMode}{GetOpenWakeWordSetupHint(_voiceCommandService.CurrentWakeWordEngine)}{micSummary}";
        _voiceProgress.Maximum = settings.VoiceSamplesRequired;
        _voiceProgress.Value = Math.Min(settings.VoiceSamplesRecorded, _voiceProgress.Maximum);
        _voiceProgress.Style = _voiceActivationBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        _voiceProgress.MarqueeAnimationSpeed = _voiceActivationBusy ? 20 : 0;
        _voicePromptText.Text = GetVoicePrompt(_activeProfile, settings);
        _voiceNextStepLabel.Text = GetVoiceNextStepText(settings, _voiceActivationBusy);
        _voiceFailureLabel.Text = GetVoiceFailureText(settings, _voiceActivationBusy, VoiceBiometricVerificationService.ReadEnrollmentSampleProof(_profileStore, _activeProfile));
        var latestSamplePath = GetLatestVoiceSamplePath(_activeProfile);
        var hasSample = recordedSamplePaths.Count > 0 || File.Exists(latestSamplePath);

        if (_voiceSampleCapture.IsRecording && _voiceSampleCapture.LastTelemetry != null)
        {
            _voiceRecordingStateLabel.Text = $"Recording now. {_voiceSampleCapture.LastTelemetry.LevelState}. Raw RMS {_voiceSampleCapture.LastTelemetry.RawRms:0.000}, peak {_voiceSampleCapture.LastTelemetry.RawPeak:0.00}, gain {_voiceSampleCapture.LastTelemetry.AppliedGainDb:0.0} dB.";
        }
        else
        {
            _voiceRecordingStateLabel.Text = hasSample
                ? "One or more fresh samples are ready for playback."
                : "No sample recording in progress.";
        }
        _voicePlaybackStateLabel.Text = hasSample
            ? (recordedSamplePaths.Count > 0
                ? $"Latest sample: {Path.GetFileName(latestSamplePath)}"
                : $"Latest sample: {Path.GetFileName(latestSamplePath)}")
            : "No sample available for playback.";

        _recordSampleButton.Enabled = !_voiceActivationBusy;
        _playSampleButton.Enabled = hasSample && !_voiceSampleCapture.IsRecording && !_voiceActivationBusy;
        _trainVoiceButton.Enabled = !_voiceActivationBusy;
        _resetVoiceButton.Enabled = !_voiceActivationBusy;
        UpdateRecordButtonAppearance();
    }

    private void SetVoiceActivationBusy(bool busy, string? status = null)
    {
        _voiceActivationBusy = busy;
        if (!string.IsNullOrWhiteSpace(status))
            _voiceStateLabel.Text = status;

        _voiceProgress.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        _voiceProgress.MarqueeAnimationSpeed = busy ? 20 : 0;
        _recordSampleButton.Enabled = !busy;
        var latestSamplePath = _activeProfile == null ? null : GetLatestVoiceSamplePath(_activeProfile);
        _playSampleButton.Enabled = !busy
            && !string.IsNullOrWhiteSpace(latestSamplePath)
            && File.Exists(latestSamplePath)
            && !_voiceSampleCapture.IsRecording;
        _trainVoiceButton.Enabled = !busy;
        _resetVoiceButton.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        UpdateRecordButtonAppearance();
    }

    private void RefreshSessionPanel()
    {
        _session.Tick();
        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var wakeOverlayShouldBeVisible = false;
        string? wakeOverlayReadout = null;
        string? wakeOverlayPhase = null;

        if (runtimeSnapshot != null)
        {
            var runningServiceProcessCount = CountCallsignServiceProcesses();
            var runtimeAge = DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime();
            var runtimeIsStale = runtimeAge > TimeSpan.FromSeconds(30);
            var runtimeSuffix = runtimeIsStale
                ? $"service snapshot stale, {Math.Ceiling(runtimeAge.TotalSeconds):0}s old"
                : "service";
            var wakeEngine = string.IsNullOrWhiteSpace(runtimeSnapshot.CurrentWakeWordEngine)
                ? runtimeSnapshot.ModeDescription
                : runtimeSnapshot.CurrentWakeWordEngine;

            ApplyRuntimeUiRequests(runtimeSnapshot);
            _sessionStateLabel.Text = string.IsNullOrWhiteSpace(runtimeSnapshot.RuntimeRole)
                ? $"{runtimeSnapshot.ServiceState} ({runtimeSuffix})"
                : $"{runtimeSnapshot.ServiceState} ({runtimeSnapshot.RuntimeRole}, {runtimeSuffix})";
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RuntimeAuthorityStatus))
                _statusLabel.Text = $"Runtime: {runtimeSnapshot.RuntimeAuthorityStatus}. {runtimeSnapshot.StatusMessage}";
            _sessionPhaseLabel.Text = $"Phase: {FormatOverlayPhase(runtimeSnapshot.SessionState)}";
            _sessionNextActionLabel.Text = FormatSessionHint(runtimeSnapshot.SessionState, runtimeSnapshot.VerifiedCallsign, runtimeSnapshot.PendingCommand);
            _sessionHintLabel.Text = GetSessionHintDetails(runtimeSnapshot.SessionState, runtimeSnapshot.VerifiedCallsign, runtimeSnapshot.PendingCommand);
            _lastHeardLabel.Text = FormatRuntimeLastHeardLabel(runtimeSnapshot);
            _sessionIdentityLabel.Text = FormatRuntimeIdentityStatus(runtimeSnapshot);
            _sessionCommandLabel.Text = FormatRuntimeCommandLabel(runtimeSnapshot);
            _sessionCountdownLabel.Text = "Service-managed session.";
            _sessionResultLabel.Text = string.IsNullOrWhiteSpace(runtimeSnapshot.StatusMessage)
                ? "Background service is running."
                : runtimeSnapshot.StatusMessage;
            _sessionSpeechCueLabel.Text = BuildRuntimeSessionSpeechCueText(runtimeSnapshot);
            if (runtimeSnapshot.LastTranscriptUpdatedUtc.HasValue
                && runtimeSnapshot.LastTranscriptUpdatedUtc != _lastSessionTranscriptHistoryRuntimeUpdateUtc
                && !string.IsNullOrWhiteSpace(runtimeSnapshot.LastTranscriptText))
            {
                _lastSessionTranscriptHistoryRuntimeUpdateUtc = runtimeSnapshot.LastTranscriptUpdatedUtc;
                SetSessionTranscriptHistory(runtimeSnapshot.RecentTranscriptHistory ?? Array.Empty<string>());
            }
            _listeningStateLabel.Text = runtimeSnapshot.IsListening && !runtimeIsStale
                ? runtimeSnapshot.CanHearAudio == true
                    ? $"Background service is running, authoritative, and hearing audio from {runtimeSnapshot.ActiveMicrophoneDeviceName ?? "the active microphone"}. {runtimeSnapshot.ModeDescription}"
                    : HasRecentAudioPacket(runtimeSnapshot)
                        ? "Runtime is receiving microphone packets, but speech is below the active threshold."
                        : "Runtime running but no microphone audio packets are arriving."
                : runtimeIsStale
                    ? $"Background service status is stale. Last update was {Math.Ceiling(runtimeAge.TotalSeconds):0} seconds ago."
                    : "Background service listener is stopped.";
        var wakeTransitionSource = string.IsNullOrWhiteSpace(runtimeSnapshot.LastWakeTransitionSource)
            ? "none"
            : runtimeSnapshot.LastWakeTransitionSource;
        _wakeReliabilityLabel.Text = string.IsNullOrWhiteSpace(wakeEngine)
            ? $"Wake detector unavailable.{GetOpenWakeWordSetupHint(wakeEngine)}"
            : $"Current wake detector: {wakeEngine}. Last wake source: {wakeTransitionSource}.{GetOpenWakeWordSetupHint(wakeEngine)}";
        _wakeCandidateLabel.Text = FormatRuntimeWakeCandidateReadout(runtimeSnapshot);
        _wakeScoreLabel.Text = runtimeSnapshot.LastWakeWordScore.HasValue && runtimeSnapshot.WakeWordThreshold.HasValue
            ? runtimeSnapshot.LastWakeWordScore.Value >= runtimeSnapshot.WakeWordThreshold.Value
                ? $"Last wake accepted: {runtimeSnapshot.LastWakeWordScore.Value:P0} confidence / {runtimeSnapshot.WakeWordThreshold.Value:P0} threshold via {runtimeSnapshot.LastWakeWordEngine ?? wakeEngine}."
                : $"Last wake candidate rejected below threshold: {runtimeSnapshot.LastWakeWordScore.Value:P0} confidence / {runtimeSnapshot.WakeWordThreshold.Value:P0} threshold via {runtimeSnapshot.LastWakeWordEngine ?? wakeEngine}."
                : "No wake score reported by the service yet.";
            _wakeQualityLabel.Text = runtimeSnapshot.WakeWordAudioQualityWarnings is { Count: > 0 }
                ? string.Join(" ", runtimeSnapshot.WakeWordAudioQualityWarnings)
                : runtimeIsStale
                    ? "Runtime state is stale; service may not be running."
                    : "No wake audio warnings reported by the service.";
            _micLevelLabel.Text = FormatMicLevel(runtimeSnapshot);
            _runtimeOwnerLabel.Text = RuntimeStatusFormatter.FormatOwnershipProof(runtimeSnapshot, runningServiceProcessCount);
            _runtimeProofLabel.Text = RuntimeStatusFormatter.FormatHearingProof(runtimeSnapshot);
            _micDetailLabel.Text = FormatMicDetails(runtimeSnapshot);
            wakeOverlayShouldBeVisible = !runtimeIsStale
                && runtimeSnapshot.IsListening
                && IsWakeOverlaySessionActive(runtimeSnapshot.SessionState);
            wakeOverlayReadout = BuildRuntimeOverlayReadout(runtimeSnapshot);
            wakeOverlayPhase = FormatOverlayPhase(runtimeSnapshot.SessionState);
        }
        else
        {
            _sessionStateLabel.Text = _session.State.ToString();
            _sessionPhaseLabel.Text = $"Phase: {FormatOverlayPhase(_session.State.ToString())}";
            _sessionNextActionLabel.Text = FormatSessionHint(_session.State.ToString(), _session.VerifiedCallsign, _session.PendingCommand);
            _sessionHintLabel.Text = GetSessionHintDetails(_session.State.ToString(), _session.VerifiedCallsign, _session.PendingCommand);
            _sessionIdentityLabel.Text = _session.VerifiedCallsign == null
                ? "Waiting for identity."
                : $"Verified: {_session.VerifiedCallsign}";
            _sessionCommandLabel.Text = FormatLocalCommandLabel();

            var lockoutRemaining = _session.GetLockoutRemaining();
            _sessionCountdownLabel.Text = lockoutRemaining.HasValue
                ? $"Lockout remaining: {Math.Ceiling(lockoutRemaining.Value.TotalSeconds):0} seconds"
                : "No timeout active.";

            _sessionResultLabel.Text = _pendingAppResolution?.IsAmbiguous == true
                ? FormatPendingAppConfirmationStatus(_pendingAppResolution)
                : (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
                    && !string.IsNullOrWhiteSpace(_activeProfile?.Settings.LastLaunchedApp)
                    ? $"Last launched through Start menu: {_activeProfile.Settings.LastLaunchedApp}"
                    : _session.StatusMessage;
            _sessionSpeechCueLabel.Text = BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _micLevelLabel.Text = _voiceCommandService.CurrentAudioTelemetry == null
                ? "Microphone telemetry unavailable."
                : $"Microphone level: {_voiceCommandService.CurrentAudioTelemetry.LevelState}.";
            _runtimeOwnerLabel.Text = RuntimeStatusFormatter.FormatOwnershipProof(null, CountCallsignServiceProcesses());
            _runtimeProofLabel.Text = _voiceCommandService.CurrentAudioTelemetry == null
                ? "Runtime proof unavailable; no service snapshot or local microphone telemetry yet."
                : $"Runtime proof: local preview listener; CanHearAudio={(_voiceCommandService.IsSpeechActive ? "true" : "false")}; packet age=local; mic=local preview microphone.";
            _wakeCandidateLabel.Text = FormatLocalWakeCandidateReadout();
            _micDetailLabel.Text = _voiceCommandService.CurrentAudioTelemetry == null
                ? "No microphone telemetry yet."
                : $"Raw RMS {_voiceCommandService.CurrentAudioTelemetry.RawRms:0.000}, peak {_voiceCommandService.CurrentAudioTelemetry.RawPeak:0.00}, gain {_voiceCommandService.CurrentAudioTelemetry.AppliedGainDb:0.0} dB, noise floor {_voiceCommandService.CurrentAudioTelemetry.NoiseFloorRms:0.000}, threshold {_voiceCommandService.CurrentAudioTelemetry.SpeechThresholdRms:0.000}.";
            wakeOverlayShouldBeVisible = IsWakeOverlaySessionActive(_session.State);
            wakeOverlayReadout = BuildLocalOverlayReadout();
            wakeOverlayPhase = FormatOverlayPhase(_session.State.ToString());
            if (_sessionTranscriptHistoryList.Items.Count == 0)
                SetSessionTranscriptHistory(Array.Empty<string>());
        }

        var hasProfile = _activeProfile != null;
        var isEnrolled = hasProfile && IsVoiceEnrolled(_activeProfile!.Settings);
        var canVerify = hasProfile && isEnrolled && _session.State == AlphaSessionState.WaitingForIdentity;
        var canCapture = hasProfile && _session.State == AlphaSessionState.WaitingForCommand;
        var canLaunch = hasProfile && _session.State == AlphaSessionState.ReadyToLaunch;

        _wakeButton.Enabled = hasProfile;
        _verifyButton.Enabled = canVerify;
        _captureButton.Enabled = canCapture;
        _launchButton.Enabled = canLaunch;
        var hasPendingAppConfirmation = _pendingAppResolution?.IsAmbiguous == true;
        _appCandidateList.Enabled = hasPendingAppConfirmation;
        _confirmAppCandidateButton.Enabled = hasPendingAppConfirmation && _appCandidateList.SelectedItem is AppCandidateListItem;
        _clearAppCandidateButton.Enabled = hasPendingAppConfirmation;
        _cancelButton.Enabled = _session.State != AlphaSessionState.Idle;
        _resetSessionButton.Enabled = _session.State != AlphaSessionState.Idle;
        var wakeOverlayActivityLevel = runtimeSnapshot != null
            ? BuildRuntimeOverlayActivityLevel(runtimeSnapshot)
            : BuildLocalOverlayActivityLevel();
        var wakeOverlayActivityText = runtimeSnapshot != null
            ? BuildRuntimeOverlayActivityText(runtimeSnapshot)
            : BuildLocalOverlayActivityText();
        var wakeOverlayCaptionText = runtimeSnapshot != null
            ? BuildRuntimeOverlayCaptionText(runtimeSnapshot)
            : BuildLocalOverlayCaptionText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, confidence: _voiceCommandService.LastWakeWordDetection?.Score is double score ? (float?)score : null);
        var wakeOverlayStatusText = runtimeSnapshot != null
            ? FormatRuntimeWakeCandidateReadout(runtimeSnapshot)
            : FormatLocalWakeCandidateReadout();
        var wakeOverlayAuthorityText = BuildWakeOverlayAuthorityText(runtimeSnapshot);
        SyncWakeOverlay(wakeOverlayShouldBeVisible, wakeOverlayReadout, wakeOverlayPhase,
            runtimeSnapshot?.RecentTranscriptHistory ?? GetLocalTranscriptHistory(), wakeOverlayActivityLevel, wakeOverlayActivityText, wakeOverlayCaptionText, wakeOverlayStatusText, wakeOverlayAuthorityText);
    }

    private void UpdateLiveSpeechCueFromActivity()
    {
        if (!_voiceCommandService.IsSpeechActive)
            return;

        if (_dictationActive)
        {
            _dictationSpeechCueLabel.Text = "Speech cue: Hearing dictation...";
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}.";
            if (_dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText))
                _dictationLastHeardLabel.Text = FormatLastHeardLabel(_dictationLastTranscriptText);
            return;
        }

        var liveCue = _session.State switch
        {
            AlphaSessionState.WaitingForIdentity => "Hearing your callsign...",
            AlphaSessionState.WaitingForCommand => "Hearing your command...",
            AlphaSessionState.ReadyToLaunch => "Listening for launch...",
            AlphaSessionState.Launching => "Launching...",
            _ => "Hearing speech..."
        };

        _sessionSpeechCueLabel.Text = $"Speech cue: {liveCue}";
    }

    private void UpdateVoiceCueRefreshRate()
    {
        var activeVoiceSession = _voiceCommandService.IsSpeechActive || _dictationActive || IsWakeOverlaySessionActive(_session.State);
        var targetInterval = activeVoiceSession ? 100 : 1000;
        if (_sessionTimer.Interval != targetInterval)
            _sessionTimer.Interval = targetInterval;
    }

    private void RuntimeStateMonitorChanged(object? sender, EventArgs e)
    {
        RunOnUiThread(RefreshSessionPanel);
    }

    private static string FormatRuntimeIdentityStatus(RuntimeStateSnapshot runtimeSnapshot)
    {
        if (!string.IsNullOrWhiteSpace(runtimeSnapshot.VerifiedCallsign))
            return $"Verified: {runtimeSnapshot.VerifiedCallsign}";

        if (runtimeSnapshot.LastIdentityAccepted == false)
        {
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.LastIdentityBiometricRejectReason))
                return $"Voice biometric rejected: {runtimeSnapshot.LastIdentityBiometricRejectReason}";

            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.LastIdentityRetryPrompt))
                return runtimeSnapshot.LastIdentityRetryPrompt;

            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.LastIdentityRejectReason))
                return $"Identity rejected: {runtimeSnapshot.LastIdentityRejectReason}";
        }

        return "Waiting for identity.";
    }

    private void ApplyRuntimeUiRequests(RuntimeStateSnapshot runtimeSnapshot)
    {
        if (runtimeSnapshot.ServiceDictationUpdatedUtc.HasValue
            && runtimeSnapshot.ServiceDictationUpdatedUtc != _lastAppliedServiceDictationUtc)
        {
            _lastAppliedServiceDictationUtc = runtimeSnapshot.ServiceDictationUpdatedUtc;
            if (runtimeSnapshot.ServiceDictationHistory is { Count: > 0 })
            {
                _dictationTextBox.Text = DictationReviewTextService.BuildReviewedText(
                    runtimeSnapshot.ServiceDictationHistory,
                    _dictationCasingMode,
                    IsFluidDictationEnabled(),
                    IsAutomaticPunctuationEnabled(),
                    IsProfanityFilterEnabled());
            }
            else if (!string.IsNullOrWhiteSpace(runtimeSnapshot.ServiceDictationText))
            {
                _dictationTextBox.Text = DictationReviewTextService.BuildReviewedText(
                    [runtimeSnapshot.ServiceDictationText],
                    _dictationCasingMode,
                    IsFluidDictationEnabled(),
                    IsAutomaticPunctuationEnabled(),
                    IsProfanityFilterEnabled());
            }
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.LastTranscriptText))
            {
                _dictationLastTranscriptText = runtimeSnapshot.LastTranscriptText;
                _dictationLastHeardLabel.Text = FormatLastHeardLabel(runtimeSnapshot.LastTranscriptText, runtimeSnapshot.LastTranscriptConfidence.HasValue ? (float?)runtimeSnapshot.LastTranscriptConfidence.Value : null);
            }
            SetDictationHistory(runtimeSnapshot.ServiceDictationHistory ?? Array.Empty<string>());
            _dictationStatusLabel.Text = runtimeSnapshot.ServiceDictationActive
                ? "Service dictation is active. Say 'stop dictation' when finished."
                : "Service dictation is ready for review.";
            _dictationSpeechCueLabel.Text = BuildRuntimeDictationSpeechCueText(runtimeSnapshot);
        }

        if (runtimeSnapshot.RequestedUiModeUtc.HasValue
            && runtimeSnapshot.RequestedUiModeUtc != _lastHandledRuntimeUiRequestUtc)
        {
            _lastHandledRuntimeUiRequestUtc = runtimeSnapshot.RequestedUiModeUtc;
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RequestedUiMode))
            {
                ApplyRequestedUiMode(runtimeSnapshot.RequestedUiMode);
                UpdateStatus(GetUiModeStatus(runtimeSnapshot));
            }
        }
    }

    private static string GetUiModeStatus(RuntimeStateSnapshot runtimeSnapshot)
    {
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "Dictation", StringComparison.OrdinalIgnoreCase))
        {
            return runtimeSnapshot.ServiceDictationActive
                ? "Service dictation is active. Review captured text in the Dictation tab."
                : "Service dictation is ready for review in the Dictation tab.";
        }

        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-repair-wakeword", StringComparison.OrdinalIgnoreCase))
            return "Repair Wakeword requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-train-voice-identity", StringComparison.OrdinalIgnoreCase))
            return "Train Voice Identity requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-create-account", StringComparison.OrdinalIgnoreCase))
            return "Create New Account requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-save-account", StringComparison.OrdinalIgnoreCase))
            return "Save Account requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-delete-account", StringComparison.OrdinalIgnoreCase))
            return "Delete Account requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-open-data-folder", StringComparison.OrdinalIgnoreCase))
            return "Open Data Folder requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-open-logs-folder", StringComparison.OrdinalIgnoreCase))
            return "Open Logs Folder requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-open-app-folder", StringComparison.OrdinalIgnoreCase))
            return "Open App Folder requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-open-packs", StringComparison.OrdinalIgnoreCase))
            return "Open Packs requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-open-shortcuts", StringComparison.OrdinalIgnoreCase))
            return "Open Voice Shortcuts requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-start-listening", StringComparison.OrdinalIgnoreCase))
            return "Start Listening requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-stop-listening", StringComparison.OrdinalIgnoreCase))
            return "Stop Listening requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-voice-help", StringComparison.OrdinalIgnoreCase))
            return "Voice Help requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-next-control", StringComparison.OrdinalIgnoreCase))
            return "Next Control requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-previous-control", StringComparison.OrdinalIgnoreCase))
            return "Previous Control requested.";
        if (string.Equals(runtimeSnapshot.RequestedUiMode, "ui-activate-control", StringComparison.OrdinalIgnoreCase))
            return "Activate Control requested.";

        if (string.Equals(runtimeSnapshot.RequestedUiMode, "Next", StringComparison.OrdinalIgnoreCase))
            return "Voice navigation moved to the next tab.";

        if (string.Equals(runtimeSnapshot.RequestedUiMode, "Previous", StringComparison.OrdinalIgnoreCase))
            return "Voice navigation moved to the previous tab.";

        if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RequestedUiMode))
            return $"Open {runtimeSnapshot.RequestedUiMode} tab.";

        return "Voice navigation updated.";
    }

    private void ApplyRequestedUiMode(string mode)
    {
        if (TryExecuteRequestedUiAction(mode))
            return;

        if (string.Equals(mode, "Next", StringComparison.OrdinalIgnoreCase))
        {
            SelectAdjacentTab(1);
            return;
        }

        if (string.Equals(mode, "Previous", StringComparison.OrdinalIgnoreCase))
        {
            SelectAdjacentTab(-1);
            return;
        }

        SelectTab(mode);
    }

    private bool TryExecuteRequestedUiAction(string mode)
    {
        var normalized = NormalizeSpeechText(mode);
        const string visibleControlLabelPrefix = "ui activate label ";
        const string visibleControlDoubleClickLabelPrefix = "ui double click label ";
        const string visibleControlRightClickLabelPrefix = "ui right click label ";
        const string voiceModePrefix = "ui set voice mode ";
        const string addVocabularyPrefix = "ui add vocabulary ";
        const string dictationOptionPrefix = "ui set dictation option ";
        const string visibleControlsWindowPrefix = "ui show visible controls window ";

        if (normalized.StartsWith(visibleControlLabelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var visibleLabel = normalized[visibleControlLabelPrefix.Length..].Trim();
            return TryActivateVisibleControlByLabel(visibleLabel);
        }

        if (normalized.StartsWith(visibleControlDoubleClickLabelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var visibleLabel = normalized[visibleControlDoubleClickLabelPrefix.Length..].Trim();
            return TryMouseActionVisibleControlByLabel(visibleLabel, DesktopVisibleControlMouseAction.DoubleClick);
        }

        if (normalized.StartsWith(visibleControlRightClickLabelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var visibleLabel = normalized[visibleControlRightClickLabelPrefix.Length..].Trim();
            return TryMouseActionVisibleControlByLabel(visibleLabel, DesktopVisibleControlMouseAction.RightClick);
        }

        if (normalized.StartsWith(voiceModePrefix, StringComparison.OrdinalIgnoreCase))
            return TrySetVoiceAccessMode(normalized[voiceModePrefix.Length..].Trim());

        if (normalized.StartsWith(addVocabularyPrefix, StringComparison.OrdinalIgnoreCase))
            return TryAddDictationVocabulary(normalized[addVocabularyPrefix.Length..].Trim());

        if (normalized.StartsWith(dictationOptionPrefix, StringComparison.OrdinalIgnoreCase))
            return TrySetDictationOption(normalized[dictationOptionPrefix.Length..].Trim());

        if (normalized.StartsWith(visibleControlsWindowPrefix, StringComparison.OrdinalIgnoreCase))
            return ShowVisibleControlsForNamedWindow(normalized[visibleControlsWindowPrefix.Length..].Trim());

        switch (normalized)
        {
            case "ui repair wakeword":
                RunOpenWakeWordSetupHelper();
                return true;
            case "ui train voice identity":
                OpenVoiceIdentityTrainingForActiveProfile();
                return true;
            case "ui create account":
                CreateNewProfile();
                return true;
            case "ui save account":
                SaveProfile();
                return true;
            case "ui delete account":
                DeleteProfile();
                return true;
            case "ui open data folder":
                OpenProfileFolder();
                return true;
            case "ui open logs folder":
                OpenLogsFolder();
                return true;
            case "ui open app folder":
                OpenInstalledAppFolder();
                return true;
            case "ui getting started":
                ShowStartupWalkthrough();
                return true;
            case "ui open packs":
                ShowPacksTab();
                return true;
            case "ui open shortcuts":
                ShowShortcutsTab();
                return true;
            case "ui new voice shortcut":
                ShowShortcutsTab();
                CreateNewVoiceShortcut();
                return true;
            case "ui save voice shortcut":
                ShowShortcutsTab();
                SaveVoiceShortcut();
                return true;
            case "ui delete voice shortcut":
                ShowShortcutsTab();
                DeleteSelectedVoiceShortcut();
                return true;
            case "ui enable voice shortcut":
                ShowShortcutsTab();
                SetSelectedVoiceShortcutEnabled(true);
                return true;
            case "ui disable voice shortcut":
                ShowShortcutsTab();
                SetSelectedVoiceShortcutEnabled(false);
                return true;
            case "ui add voice shortcut command action":
                ShowShortcutsTab();
                AddVoiceShortcutCommandAction();
                return true;
            case "ui add voice shortcut wait action":
                ShowShortcutsTab();
                AddVoiceShortcutWaitAction();
                return true;
            case "ui remove voice shortcut action":
                ShowShortcutsTab();
                RemoveSelectedVoiceShortcutAction();
                return true;
            case "ui start listening":
                StartVoiceListening();
                return true;
            case "ui stop listening":
                StopVoiceListening();
                return true;
            case "ui cancel session":
                CancelSession();
                return true;
            case "ui reset session":
                ResetSession();
                return true;
            case "ui voice help":
                ShowVoiceHelp();
                return true;
            case "ui read status":
                ReadCurrentStatusAloud();
                return true;
            case "ui stop status readback":
                StopStatusReadback();
                return true;
            case "ui clear recent speech":
                ClearRecentSpeechHistory();
                return true;
            case "ui hide command palette":
                HideCommandPalette();
                return true;
            case "ui hide update splash":
                HideUpdateSplash();
                return true;
            case "ui show visible controls":
                ShowVisibleControlsSummary(VisibleControlsScope.CurrentSurface);
                return true;
            case "ui show visible controls taskbar":
                ShowVisibleControlsSummary(VisibleControlsScope.Taskbar);
                return true;
            case "ui hide visible controls":
                HideVisibleControlsOverlay();
                return true;
            case "ui show keyboard":
                ShowKeyboardOverlay();
                return true;
            case "ui hide keyboard":
                HideKeyboardOverlay();
                return true;
            case "ui show mouse grid":
                ShowMouseGrid(MouseGridScope.Desktop);
                return true;
            case "ui show mouse grid here":
                ShowMouseGrid(MouseGridScope.CurrentWindow);
                return true;
            case "ui hide mouse grid":
                HideMouseGrid();
                return true;
            case "ui next control":
                MoveUiFocus(1);
                return true;
            case "ui previous control":
                MoveUiFocus(-1);
                return true;
            case "ui activate control":
                ActivateFocusedUiControl();
                return true;
            default:
                if (TryHandleFileSearchResultTarget(normalized))
                    return true;

                if (TryHandleMouseGridTarget(normalized))
                    return true;

                return false;
        }
    }

    private bool TrySetVoiceAccessMode(string mode)
    {
        var normalizedMode = NormalizeSpeechText(mode);
        switch (normalizedMode)
        {
            case "commands":
            case "command":
            case "commands only":
            case "command only":
                _voiceAccessMode = "Commands only";
                if (_dictationActive)
                    StopDictation();
                UpdateListeningPanel();
                RefreshDictationPanel();
                SyncVoiceModeControls();
                UpdateStatus("Voice mode set to Commands only. Dictation commands are paused; command routing stays visible.");
                RecordVoiceControlAudit(
                    "voice_mode_change",
                    "succeeded",
                    _voiceAccessMode,
                    "commands_only",
                    true,
                    "Voice access mode change was shown in the visible status surface.");
                return true;

            case "dictation":
            case "dictation only":
                _voiceAccessMode = "Dictation only";
                SelectTab("Dictation");
                if (!_dictationActive)
                    StartDictation();
                else
                {
                    SyncVoiceModeControls();
                    UpdateStatus("Voice mode set to Dictation only. Review text before copy or paste.");
                    RecordVoiceControlAudit(
                        "voice_mode_change",
                        "succeeded",
                        _voiceAccessMode,
                        "dictation_only",
                        true,
                        "Voice access mode change was shown in the visible status surface.");
                }
                return true;

            case "default":
            case "commands and dictation":
            case "command and dictation":
                _voiceAccessMode = "Default";
                if (_dictationActive)
                    StopDictation();
                UpdateListeningPanel();
                RefreshDictationPanel();
                SyncVoiceModeControls();
                UpdateStatus("Voice mode set to Default. Callsign can route commands and open visible dictation review.");
                RecordVoiceControlAudit(
                    "voice_mode_change",
                    "succeeded",
                    _voiceAccessMode,
                    "default",
                    true,
                    "Voice access mode change was shown in the visible status surface.");
                return true;

            default:
                UpdateStatus("Voice mode was not recognized. Say 'commands only mode', 'dictation mode', or 'default mode'.");
                RecordVoiceControlAudit(
                    "voice_mode_change",
                    "failed",
                    normalizedMode,
                    "unrecognized_mode",
                    false,
                    "Voice access mode failure was shown in the visible status surface.");
                return true;
        }
    }

    private bool TryAddDictationVocabulary(string phrase)
    {
        if (!EnsureActiveProfile(out var profile))
        {
            UpdateStatus("Create or select a profile before adding dictation vocabulary.");
            RecordVoiceControlAudit(
                "dictation_vocabulary_add",
                "failed",
                phrase,
                "no_active_profile",
                false,
                "Vocabulary update failure was shown in the visible status surface.");
            return true;
        }

        var result = DictationVocabularyService.Add(profile, phrase);
        if (result.Status == DictationVocabularyAddStatus.Invalid)
        {
            UpdateStatus("That vocabulary entry was not added. Say a short word or phrase, such as 'add womprat to vocabulary'.");
            RecordVoiceControlAudit(
                "dictation_vocabulary_add",
                "failed",
                phrase,
                "invalid_entry",
                false,
                "Vocabulary update failure was shown in the visible status surface.");
            return true;
        }

        _profileStore.Save(profile);
        UpdateStatus($"{result.Message} Vocabulary entries: {result.Count}.");
        RecordVoiceControlAudit(
            "dictation_vocabulary_add",
            result.Status == DictationVocabularyAddStatus.Added ? "succeeded" : "unchanged",
            result.Word,
            result.Status.ToString().ToLowerInvariant(),
            true,
            "Local dictation vocabulary update was shown in the visible status surface and saved to the active profile.");
        return true;
    }

    private bool TrySetDictationOption(string option)
    {
        if (!EnsureActiveProfile(out var profile))
        {
            UpdateStatus("Create or select a profile before changing dictation settings.");
            RecordVoiceControlAudit(
                "dictation_option_change",
                "failed",
                option,
                "no_active_profile",
                false,
                "Dictation setting failure was shown in the visible status surface.");
            return true;
        }

        var normalized = NormalizeSpeechText(option);
        var enabled = normalized.EndsWith(" on", StringComparison.OrdinalIgnoreCase);
        var status = normalized switch
        {
            "fluid dictation on" => SetFluidDictation(true),
            "fluid dictation off" => SetFluidDictation(false),
            "automatic punctuation on" => SetAutomaticPunctuation(true),
            "automatic punctuation off" => SetAutomaticPunctuation(false),
            "profanity filter on" => SetProfanityFilter(true),
            "profanity filter off" => SetProfanityFilter(false),
            _ => false
        };

        if (!status)
        {
            UpdateStatus("Dictation setting was not recognized. Say 'turn on fluid dictation', 'turn on automatic punctuation', or 'turn off profanity filter'.");
            RecordVoiceControlAudit(
                "dictation_option_change",
                "failed",
                normalized,
                "unrecognized_option",
                false,
                "Dictation setting failure was shown in the visible status surface.");
            return true;
        }

        _profileStore.Save(profile);
        var optionName = normalized.StartsWith("fluid dictation", StringComparison.OrdinalIgnoreCase)
            ? "Fluid dictation"
            : normalized.StartsWith("automatic punctuation", StringComparison.OrdinalIgnoreCase)
                ? "Automatic punctuation"
                : "Profanity filter";
        UpdateStatus($"{optionName} turned {(enabled ? "on" : "off")} for local dictation review.");
        RecordVoiceControlAudit(
            "dictation_option_change",
            "succeeded",
            normalized,
            enabled ? "enabled" : "disabled",
            true,
            "Dictation setting update was shown in the visible status surface and saved to the active profile.");
        return true;

        bool SetFluidDictation(bool value)
        {
            profile.Settings.DictationFluidModeEnabled = value;
            return true;
        }

        bool SetAutomaticPunctuation(bool value)
        {
            profile.Settings.DictationAutomaticPunctuationEnabled = value;
            return true;
        }

        bool SetProfanityFilter(bool value)
        {
            profile.Settings.DictationProfanityFilterEnabled = value;
            return true;
        }
    }

    private bool TryHandleFileSearchResultTarget(string normalizedTarget)
    {
        if (TryParseNumberedUiAction(normalizedTarget, "ui select file result ", out var selectResult))
        {
            SelectFileSearchResult(selectResult);
            return true;
        }

        if (TryParseNumberedUiAction(normalizedTarget, "ui open file result ", out var openResult))
        {
            OpenFileSearchResultByNumber(openResult);
            return true;
        }

        if (TryParseNumberedUiAction(normalizedTarget, "ui reveal file result ", out var revealResult))
        {
            RevealFileSearchResultByNumber(revealResult);
            return true;
        }

        return false;
    }

    private bool TryHandleMouseGridTarget(string normalizedTarget)
    {
        if (string.Equals(normalizedTarget, "ui undo mouse grid", StringComparison.OrdinalIgnoreCase))
        {
            UndoMouseGrid();
            return true;
        }

        if (string.Equals(normalizedTarget, "ui mark mouse grid", StringComparison.OrdinalIgnoreCase))
        {
            MarkMouseGrid();
            return true;
        }

        if (TryParseNumberedUiAction(normalizedTarget, "ui mark mouse grid cell ", out var markCell))
        {
            MarkMouseGrid(markCell);
            return true;
        }

        if (normalizedTarget.StartsWith("ui focus mouse grid path ", StringComparison.OrdinalIgnoreCase)
            && TryParseMouseGridPathTarget(normalizedTarget, out var pathDisplayIdentifier, out var pathDigits))
        {
            FocusMouseGridPath(pathDisplayIdentifier, pathDigits);
            return true;
        }

        if (normalizedTarget.StartsWith("ui focus mouse grid shortcut path ", StringComparison.OrdinalIgnoreCase))
        {
            var shortcutDigits = normalizedTarget["ui focus mouse grid shortcut path ".Length..].Trim();
            FocusMouseGridShortcutPath(shortcutDigits);
            return true;
        }

        if (normalizedTarget.StartsWith("ui focus mouse grid display ", StringComparison.OrdinalIgnoreCase))
        {
            var displayIdentifier = normalizedTarget["ui focus mouse grid display ".Length..].Trim();
            FocusMouseGridDisplay(displayIdentifier);
            return true;
        }

        if (TryParseNumberedUiAction(normalizedTarget, "ui select mouse grid cell ", out var selectCell))
        {
            SelectMouseGridCell(selectCell, click: false);
            return true;
        }

        if (TryParseNumberedUiAction(normalizedTarget, "ui click mouse grid cell ", out var clickCell))
        {
            SelectMouseGridCell(clickCell, click: true);
            return true;
        }

        if (normalizedTarget.StartsWith("ui drag mouse grid ", StringComparison.OrdinalIgnoreCase)
            && TryParseGridDragTarget(normalizedTarget, out var fromCell, out var toCell))
        {
            DragMouseGridCells(fromCell, toCell);
            return true;
        }

        if (string.Equals(normalizedTarget, "ui drag marked mouse grid", StringComparison.OrdinalIgnoreCase))
        {
            DragMarkedMouseGrid();
            return true;
        }

        return false;
    }

    private static bool TryParseMouseGridPathTarget(string normalizedTarget, out string displayIdentifier, out string pathDigits)
    {
        displayIdentifier = string.Empty;
        pathDigits = string.Empty;
        var prefix = "ui focus mouse grid path ";
        if (!normalizedTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = normalizedTarget[prefix.Length..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        displayIdentifier = parts[0];
        pathDigits = parts[1];
        return !string.IsNullOrWhiteSpace(displayIdentifier)
            && !string.IsNullOrWhiteSpace(pathDigits)
            && pathDigits.All(character => character is >= '1' and <= '9');
    }

    private static bool TryParseNumberedUiAction(string normalizedTarget, string prefix, out int number)
    {
        number = 0;
        return normalizedTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(normalizedTarget[prefix.Length..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            && number > 0;
    }

    private bool TryExecuteExtensionCommand(
        AlphaVoiceIntent intent,
        out CallsignCommandExecutionResult result,
        int shortcutDepth = 0,
        HashSet<string>? activeExtensionCommands = null)
    {
        result = new CallsignCommandExecutionResult(false, string.Empty);
        if (intent.Kind != AlphaVoiceIntentKind.ExtensionCommand)
            return false;

        result = ExecuteExtensionCommand(intent, shortcutDepth, activeExtensionCommands);
        return true;
    }

    private CallsignCommandExecutionResult ExecuteExtensionCommand(
        AlphaVoiceIntent intent,
        int shortcutDepth = 0,
        HashSet<string>? activeExtensionCommands = null)
    {
        var correlationId = $"extension_{Guid.NewGuid():N}";
        if (!CallsignCommandRegistry.Shared.TryResolve(intent.NormalizedCommand, out var commandResolution))
        {
            if (EnsureActiveProfile(out var unresolvedProfile))
            {
                _auditLog.TryRecordCommand(
                    unresolvedProfile,
                    eventType: "alpha.command_execution",
                    actionName: "extension_command",
                    status: "blocked",
                    out _,
                    commandFamily: "extension",
                    actionTarget: intent.NormalizedCommand,
                    details: "no_matching_extension_command",
                    success: false,
                    correlationId: correlationId,
                    verificationMethod: "registry_resolution",
                    verificationSummary: "No extension command matched the normalized spoken phrase.");
            }

            return new CallsignCommandExecutionResult(false, "No extension pack command matched the spoken phrase.");
        }

        var identityVerified = string.Equals(_session.VerifiedCallsign, _activeProfile?.Callsign, StringComparison.OrdinalIgnoreCase);
        var freshIdentity = _session.HasFreshIdentity(UpdateCheckService.DefaultIdentityFreshness);
        var policy = CallsignCommandPolicy.Evaluate(commandResolution.Definition, identityVerified, freshIdentity);
        var commandLabel = $"{commandResolution.PackId}/{commandResolution.CommandId}";

        if (policy.Decision == CallsignPolicyDecision.BlockedDangerousAction)
        {
            if (EnsureActiveProfile(out var blockedProfile))
            {
                _auditLog.TryRecordCommand(
                    blockedProfile,
                    eventType: "alpha.command_execution",
                    actionName: "extension_command",
                    status: "blocked",
                    out _,
                    commandFamily: "extension",
                    actionTarget: commandLabel,
                    details: policy.Reason,
                    success: false,
                    correlationId: correlationId,
                    verificationMethod: "policy_evaluation",
                    verificationSummary: "Extension command was blocked by Callsign policy before execution.");
            }

            return new CallsignCommandExecutionResult(false, policy.Reason, AuditEvent: $"command_blocked:{commandResolution.PackId}:{commandResolution.CommandId}");
        }

        if (policy.Decision == CallsignPolicyDecision.RequireFreshIdentity)
        {
            if (EnsureActiveProfile(out var freshProfile))
            {
                _auditLog.TryRecordCommand(
                    freshProfile,
                    eventType: "alpha.command_execution",
                    actionName: "extension_command",
                    status: "blocked",
                    out _,
                    commandFamily: "extension",
                    actionTarget: commandLabel,
                    details: policy.Reason,
                    success: false,
                    correlationId: correlationId,
                    verificationMethod: "policy_evaluation",
                    verificationSummary: "Extension command required fresh identity before execution.");
            }

            return new CallsignCommandExecutionResult(false, policy.Reason);
        }

        var approvalGranted = false;
        if (policy.Decision == CallsignPolicyDecision.RequireApproval)
        {
            var prompt = $"The command '{commandResolution.CommandDisplayName}' from '{commandResolution.PackDisplayName}' may need explicit approval. "
                + $"Risk: {policy.RiskTier}, Privacy impact: {commandResolution.Definition.PrivacyImpact}. Proceed?";
            var answer = MessageBox.Show(this, prompt, "Approve command", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                if (EnsureActiveProfile(out var deniedProfile))
                {
                    _auditLog.TryRecordCommand(
                        deniedProfile,
                        eventType: "alpha.command_execution",
                        actionName: "extension_command",
                        status: "blocked",
                        out _,
                        commandFamily: "extension",
                        actionTarget: commandLabel,
                        details: "user_denied_approval",
                        success: false,
                        correlationId: correlationId,
                        verificationMethod: "user_approval",
                        verificationSummary: "User denied extension command approval before execution.");
                }

                return new CallsignCommandExecutionResult(false, "Command blocked by user.");
            }

            approvalGranted = true;
        }

        var context = new CallsignCommandExecutionContext(
            intent.PackId,
            intent.Target,
            intent.NormalizedCommand,
            intent.NormalizedCommand,
            intent.ArgumentText,
            _activeProfile?.Callsign,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        if (!CallsignCommandRegistry.Shared.TryExecute(
            context,
            out var result,
            identityVerified,
            freshIdentity,
            approvalGranted))
            result = new CallsignCommandExecutionResult(false, "No extension pack command matched the spoken phrase.");

        var commandLabelKey = $"{commandResolution.PackId}/{commandResolution.CommandId}";
        if (result.Succeeded && result.FollowUpSteps is { Count: > 0 })
        {
            if (shortcutDepth >= 4)
            {
                result = new CallsignCommandExecutionResult(false, "Voice shortcut nesting is limited to four levels.");
            }
            else
            {
                activeExtensionCommands ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!activeExtensionCommands.Add(commandLabelKey))
                {
                    result = new CallsignCommandExecutionResult(false, "Voice shortcut execution detected a loop and was blocked.");
                }
                else
                {
                    result = ExecuteVoiceShortcutFollowUpSteps(commandResolution, result, shortcutDepth + 1, activeExtensionCommands);
                    activeExtensionCommands.Remove(commandLabelKey);
                }
            }
        }

        if (EnsureActiveProfile(out var executionProfile))
        {
            var status = result.Succeeded ? "succeeded" : "failed";
            var details = string.IsNullOrWhiteSpace(result.Message) ? commandLabel : result.Message;
            _auditLog.TryRecordCommand(
                executionProfile,
                eventType: "alpha.command_execution",
                actionName: "extension_command",
                status,
                out _,
                commandFamily: "extension",
                actionTarget: commandLabel,
                details: details,
                success: result.Succeeded,
                correlationId: correlationId,
                verificationMethod: "pack_execution",
                verificationSummary: FormatExtensionVerificationSummary(commandResolution.Definition.VerificationStrategy, result));
        }

        if (result.Succeeded)
            _session.CompleteLaunch();

        return result;
    }

    private CallsignCommandExecutionResult ExecuteVoiceShortcutFollowUpSteps(
        CallsignCommandResolution commandResolution,
        CallsignCommandExecutionResult baseResult,
        int shortcutDepth,
        HashSet<string> activeExtensionCommands)
    {
        ApplyRequestedUiMode("Shortcuts");
        var steps = baseResult.FollowUpSteps ?? Array.Empty<CallsignFollowUpStep>();
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            switch (step.Kind)
            {
                case CallsignFollowUpStepKind.Wait:
                    var waitMilliseconds = Math.Clamp(
                        step.DurationMilliseconds,
                        VoiceShortcutConstants.MinWaitMilliseconds,
                        VoiceShortcutConstants.MaxWaitMilliseconds);
                    UpdateStatus($"Voice shortcut '{commandResolution.CommandDisplayName}' waiting {waitMilliseconds} ms before the next visible step.");
                    Application.DoEvents();
                    Thread.Sleep(waitMilliseconds);
                    break;
                case CallsignFollowUpStepKind.Command:
                    var spokenCommand = step.Value?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(spokenCommand))
                    {
                        return new CallsignCommandExecutionResult(false, $"Voice shortcut '{commandResolution.CommandDisplayName}' contains an empty command step.");
                    }

                    if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
                    {
                        return new CallsignCommandExecutionResult(false, "Select an account before running voice shortcuts.");
                    }

                    UpdateStatus($"Voice shortcut '{commandResolution.CommandDisplayName}' running step {index + 1} of {steps.Count}: {spokenCommand}");
                    var transcript = $"Callsign {_activeProfile.Callsign} {spokenCommand}";
                    var intent = AlphaVoiceIntentParser.ParseVerifiedTranscript(transcript, "Callsign", _activeProfile.Callsign);
                    if (!TryExecuteVerifiedIntent(intent, shortcutDepth, activeExtensionCommands))
                    {
                        return new CallsignCommandExecutionResult(false, $"Voice shortcut step '{spokenCommand}' could not be routed.");
                    }

                    break;
            }
        }

        return baseResult with
        {
            Message = $"Voice shortcut '{commandResolution.CommandDisplayName}' executed {steps.Count} saved action(s)."
        };
    }

    private static string FormatExtensionVerificationSummary(CallsignCommandVerificationStrategy strategy, CallsignCommandExecutionResult result)
    {
        var outcome = result.Succeeded ? "reported success" : "reported failure";
        var message = string.IsNullOrWhiteSpace(result.Message) ? "no result message" : result.Message;
        return $"Extension pack execution {outcome} using {strategy} verification strategy: {message}";
    }

    private void RecordVisibleUiActionAudit(string actionName, string status, string? actionTarget, string details, bool success, string verificationSummary)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_execution",
            actionName: actionName,
            status: status,
            out _,
            commandFamily: "visible_ui",
            actionTarget: actionTarget,
            details: details,
            success: success,
            verificationMethod: "visible_status",
            verificationSummary: verificationSummary);
    }

    private void RecordDictationActionAudit(string actionName, string status, string? actionTarget, string details, bool success, string verificationSummary)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_execution",
            actionName: actionName,
            status: status,
            out _,
            commandFamily: "dictation",
            actionTarget: actionTarget,
            details: details,
            success: success,
            verificationMethod: "visible_status",
            verificationSummary: verificationSummary);
    }

    private void ReadCurrentStatusAloud()
    {
        var readout = BuildCurrentStatusReadout();
        if (string.IsNullOrWhiteSpace(readout))
        {
            UpdateStatus("No visible status is available to read.");
            RecordVisibleUiActionAudit(
                "status_readback",
                "failed",
                "status_readback",
                "empty_status",
                false,
                "Status readback failure was shown in the visible status surface.");
            return;
        }

        try
        {
            _statusReadbackSynthesizer ??= new SpeechSynthesizer();
            _statusReadbackSynthesizer.SpeakAsyncCancelAll();
            _statusReadbackSynthesizer.SpeakAsync(readout);
            UpdateStatus("Reading the current visible status aloud locally.");
            RecordVisibleUiActionAudit(
                "status_readback",
                "succeeded",
                "status_readback",
                "local_speech_synthesis_started",
                true,
                "Current status readback was shown in the visible status surface and used local speech synthesis.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to read current status aloud: {ex.Message}");
            RecordVisibleUiActionAudit(
                "status_readback",
                "failed",
                "status_readback",
                ex.Message,
                false,
                "Status readback failure was shown in the visible status surface.");
        }
    }

    private void StopStatusReadback()
    {
        if (_statusReadbackSynthesizer == null)
        {
            UpdateStatus("No status readback is playing.");
            RecordVisibleUiActionAudit(
                "status_readback_stop",
                "failed",
                "status_readback",
                "no_active_readback",
                false,
                "Status readback stop was shown in the visible status surface.");
            return;
        }

        try
        {
            _statusReadbackSynthesizer.SpeakAsyncCancelAll();
            UpdateStatus("Stopped status readback.");
            RecordVisibleUiActionAudit(
                "status_readback_stop",
                "succeeded",
                "status_readback",
                "local_speech_synthesis_cancelled",
                true,
                "Status readback stop was shown in the visible status surface.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to stop status readback: {ex.Message}");
            RecordVisibleUiActionAudit(
                "status_readback_stop",
                "failed",
                "status_readback",
                ex.Message,
                false,
                "Status readback stop failure was shown in the visible status surface.");
        }
    }

    private void ClearRecentSpeechHistory()
    {
        RuntimeControlFiles.RequestClearTranscriptHistory();
        SetSessionTranscriptHistory(Array.Empty<string>());
        _lastSessionTranscriptHistoryRuntimeUpdateUtc = null;
        _lastHeardTranscriptText = null;
        _lastHeardTranscriptConfidence = null;
        _lastHeardLabel.Text = "Nothing heard yet.";
        _sessionSpeechCueLabel.Text = "Speech cue: nothing heard yet.";
        _wakeOverlay?.SetTranscriptHistory(Array.Empty<string>());
        RefreshBrowserPanel();
        RefreshFileSearchPanel();
        RefreshSystemPanel();
        UpdateStatus("Recent speech history cleared.");
        RecordVisibleUiActionAudit(
            "recent_speech_clear",
            "succeeded",
            "recent_speech",
            "transcript_history_cleared",
            true,
            "Recent speech history clear was shown in the visible status surface.");
    }

    private string BuildCurrentStatusReadout()
    {
        var lines = new List<string>();
        AddStatusReadoutLine(lines, _statusLabel?.Text);
        AddStatusReadoutLine(lines, _sessionSpeechCueLabel?.Text);
        AddStatusReadoutLine(lines, _lastHeardLabel?.Text);
        AddStatusReadoutLine(lines, _sessionNextActionLabel?.Text);
        AddStatusReadoutLine(lines, _sessionResultLabel?.Text);
        AddStatusReadoutLine(lines, _dictationStatusLabel?.Text);
        AddStatusReadoutLine(lines, _dictationLastHeardLabel?.Text);
        AddStatusReadoutLine(lines, _browserLastActionLabel?.Text);
        AddStatusReadoutLine(lines, _fileSearchLastActionLabel?.Text);
        AddStatusReadoutLine(lines, _systemStatusLabel?.Text);
        return string.Join(". ", lines);
    }

    private static void AddStatusReadoutLine(List<string> lines, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
        if (normalized.Equals("Nothing heard yet.", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Last heard: nothing yet.", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("No launch yet.", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (lines.Any(line => string.Equals(line, normalized, StringComparison.OrdinalIgnoreCase)))
            return;

        lines.Add(normalized.TrimEnd('.'));
    }

    private void RecordHelpDiscoveryAudit(string actionName, string status, string? actionTarget, string details, bool success, string verificationSummary)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_execution",
            actionName: actionName,
            status: status,
            out _,
            commandFamily: "help_discovery",
            actionTarget: actionTarget,
            details: details,
            success: success,
            verificationMethod: "visible_status",
            verificationSummary: verificationSummary);
    }

    private void RecordExtensionPackUiAudit(string actionName, string status, string? actionTarget, string details, bool success, string verificationSummary)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_execution",
            actionName: actionName,
            status: status,
            out _,
            commandFamily: "extension_pack",
            actionTarget: actionTarget,
            details: details,
            success: success,
            verificationMethod: "visible_status",
            verificationSummary: verificationSummary);
    }

    private void RecordVoiceControlAudit(string actionName, string status, string? actionTarget, string details, bool success, string verificationSummary)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_execution",
            actionName: actionName,
            status: status,
            out _,
            commandFamily: "voice_control",
            actionTarget: actionTarget,
            details: details,
            success: success,
            verificationMethod: "visible_status",
            verificationSummary: verificationSummary);
    }

    private void MoveUiFocus(int direction)
    {
        var fromControl = FindFocusedControl(this) ?? this;
        if (!SelectNextControl(fromControl, direction > 0, true, true, true))
        {
            UpdateStatus("No other control was available to focus.");
            RecordVisibleUiActionAudit(
                "visible_control_focus",
                "failed",
                direction > 0 ? "next_control" : "previous_control",
                "no_focusable_control",
                false,
                "Visible control focus failure was shown in the visible status surface.");
            return;
        }

        UpdateStatus(direction > 0 ? "Moved to the next control." : "Moved to the previous control.");
        UpdateVisibleControlsOverlay();
        RecordVisibleUiActionAudit(
            "visible_control_focus",
            "succeeded",
            direction > 0 ? "next_control" : "previous_control",
            "focus_moved",
            true,
            "Visible control focus movement was shown in the visible status surface.");
    }

    private void ActivateFocusedUiControl()
    {
        var focused = FindFocusedControl(this);
        if (focused is Button button && button.Enabled)
        {
            button.PerformClick();
            UpdateStatus($"Activated '{button.Text}'.");
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                "succeeded",
                button.Text,
                "focused_button",
                true,
                "Visible control activation was shown in the visible status surface.");
            return;
        }

        if (focused is TextBoxBase textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
            UpdateStatus("Selected the active text field.");
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                "succeeded",
                GetControlVoiceLabel(textBox),
                "focused_text_field",
                true,
                "Visible control activation was shown in the visible status surface.");
            return;
        }

        if (focused is TabControl tabControl)
        {
            UpdateStatus($"Focused tab control on '{tabControl.SelectedTab?.Text}'.");
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                "succeeded",
                tabControl.SelectedTab?.Text,
                "focused_tab_control",
                true,
                "Visible control activation was shown in the visible status surface.");
            return;
        }

        UpdateStatus("Focused control could not be activated directly.");
        RecordVisibleUiActionAudit(
            "visible_control_activate",
            "failed",
            focused == null ? null : GetControlVoiceLabel(focused),
            "unsupported_focused_control",
            false,
            "Visible control activation failure was shown in the visible status surface.");
    }

    private bool TryActivateVisibleControlByLabel(string label)
    {
        var normalizedLabel = NormalizeVisibleControlLabel(label);
        if (string.IsNullOrWhiteSpace(normalizedLabel))
        {
            UpdateStatus("No visible control label was provided.");
            return true;
        }

        if (TryActivateVisibleControlByNumber(normalizedLabel, out var numberMessage))
        {
            UpdateStatus(numberMessage);
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                numberMessage.Contains("Show visible controls first", StringComparison.OrdinalIgnoreCase)
                    || numberMessage.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                        ? "failed"
                        : "succeeded",
                normalizedLabel,
                numberMessage,
                !numberMessage.Contains("Show visible controls first", StringComparison.OrdinalIgnoreCase)
                    && !numberMessage.Contains("no longer available", StringComparison.OrdinalIgnoreCase),
                numberMessage.Contains("Show visible controls first", StringComparison.OrdinalIgnoreCase)
                    || numberMessage.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                        ? "Visible control activation failure was shown in the visible status surface."
                        : "Visible numbered control activation was shown in the visible status surface.");
            return true;
        }

        var desktopEntry = _desktopVisibleControlsSummary.FirstOrDefault(entry => DesktopVisibleControlService.LabelsMatch(entry.Label, normalizedLabel));
        if (desktopEntry != null)
        {
            var activated = _desktopVisibleControlService.TryActivate(desktopEntry, out var desktopMessage);
            UpdateStatus(desktopMessage);
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                activated ? "succeeded" : "failed",
                desktopEntry.Label,
                desktopMessage,
                activated,
                activated
                    ? "Visible desktop control activation was shown in the visible status surface."
                    : "Visible control activation failure was shown in the visible status surface.");
            return true;
        }

        if (TryFindVisibleControlByLabel(this, normalizedLabel, out var control))
        {
            if (control is Button button && button.Enabled)
            {
                button.PerformClick();
                UpdateStatus($"Activated '{button.Text}'.");
                UpdateVisibleControlsOverlay();
                RecordVisibleUiActionAudit(
                    "visible_control_activate",
                    "succeeded",
                    button.Text,
                    "label_button",
                    true,
                    "Visible control activation was shown in the visible status surface.");
                return true;
            }

            if (control is TextBoxBase textBox && textBox.Enabled)
            {
                textBox.Focus();
                textBox.SelectAll();
                UpdateStatus($"Focused '{GetControlVoiceLabel(textBox)}'.");
                UpdateVisibleControlsOverlay();
                RecordVisibleUiActionAudit(
                    "visible_control_activate",
                    "succeeded",
                    GetControlVoiceLabel(textBox),
                    "label_text_field",
                    true,
                    "Visible control activation was shown in the visible status surface.");
                return true;
            }

            if (control is ComboBox comboBox && comboBox.Enabled)
            {
                comboBox.Focus();
                UpdateStatus($"Focused '{GetControlVoiceLabel(comboBox)}'.");
                UpdateVisibleControlsOverlay();
                RecordVisibleUiActionAudit(
                    "visible_control_activate",
                    "succeeded",
                    GetControlVoiceLabel(comboBox),
                    "label_combo_box",
                    true,
                    "Visible control activation was shown in the visible status surface.");
                return true;
            }

            if (control is ListBox listBox && listBox.Enabled)
            {
                listBox.Focus();
                if (listBox.Items.Count > 0 && listBox.SelectedIndex < 0)
                    listBox.SelectedIndex = 0;
                UpdateStatus($"Focused '{GetControlVoiceLabel(listBox)}'.");
                UpdateVisibleControlsOverlay();
                RecordVisibleUiActionAudit(
                    "visible_control_activate",
                    "succeeded",
                    GetControlVoiceLabel(listBox),
                    "label_list_box",
                    true,
                    "Visible control activation was shown in the visible status surface.");
                return true;
            }

            if (control is TabPage tabPage && tabPage.Parent is TabControl tabControl)
            {
                tabControl.SelectedTab = tabPage;
                UpdateStatus($"Opened tab '{tabPage.Text}'.");
                UpdateVisibleControlsOverlay();
                RecordVisibleUiActionAudit(
                    "visible_control_activate",
                    "succeeded",
                    tabPage.Text,
                    "label_tab_page",
                    true,
                    "Visible control activation was shown in the visible status surface.");
                return true;
            }
        }

        if (IsListeningLabel(normalizedLabel))
        {
            StartVoiceListening();
            UpdateStatus("Started listening.");
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                "visible_control_activate",
                "succeeded",
                normalizedLabel,
                "listening_label",
                true,
                "Visible control activation was shown in the visible status surface.");
            return true;
        }

        UpdateStatus($"No visible control matched '{label}'.");
        RecordVisibleUiActionAudit(
            "visible_control_activate",
            "failed",
            normalizedLabel,
            "no_matching_visible_control",
            false,
            "Visible control activation failure was shown in the visible status surface.");
        return true;
    }

    private bool TryMouseActionVisibleControlByLabel(string label, DesktopVisibleControlMouseAction action)
    {
        var normalizedLabel = NormalizeVisibleControlLabel(label);
        var actionName = action == DesktopVisibleControlMouseAction.DoubleClick ? "double_click" : "right_click";
        if (string.IsNullOrWhiteSpace(normalizedLabel))
        {
            UpdateStatus("No visible control label was provided.");
            return true;
        }

        if (TryMouseActionVisibleControlByNumber(normalizedLabel, action, out var numberMessage))
        {
            UpdateStatus(numberMessage);
            UpdateVisibleControlsOverlay();
            var succeeded = !numberMessage.Contains("Show visible controls first", StringComparison.OrdinalIgnoreCase)
                && !numberMessage.Contains("no longer", StringComparison.OrdinalIgnoreCase)
                && !numberMessage.Contains("Unsupported", StringComparison.OrdinalIgnoreCase);
            RecordVisibleUiActionAudit(
                $"visible_control_{actionName}",
                succeeded ? "succeeded" : "failed",
                normalizedLabel,
                numberMessage,
                succeeded,
                succeeded
                    ? "Visible numbered control mouse action was shown in the visible status surface."
                    : "Visible control mouse action failure was shown in the visible status surface.");
            return true;
        }

        var desktopEntry = _desktopVisibleControlsSummary.FirstOrDefault(entry => DesktopVisibleControlService.LabelsMatch(entry.Label, normalizedLabel));
        if (desktopEntry != null)
        {
            var succeeded = _desktopVisibleControlService.TryMouseAction(desktopEntry, action, out var desktopMessage);
            UpdateStatus(desktopMessage);
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                $"visible_control_{actionName}",
                succeeded ? "succeeded" : "failed",
                desktopEntry.Label,
                desktopMessage,
                succeeded,
                succeeded
                    ? "Visible desktop control mouse action was shown in the visible status surface."
                    : "Visible control mouse action failure was shown in the visible status surface.");
            return true;
        }

        if (TryFindVisibleControlByLabel(this, normalizedLabel, out var control) && control is not null)
        {
            var succeeded = TryPerformControlMouseAction(control, action, out var localMessage);
            UpdateStatus(localMessage);
            UpdateVisibleControlsOverlay();
            RecordVisibleUiActionAudit(
                $"visible_control_{actionName}",
                succeeded ? "succeeded" : "failed",
                GetControlVoiceLabel(control),
                localMessage,
                succeeded,
                succeeded
                    ? "Visible Callsign control mouse action was shown in the visible status surface."
                    : "Visible control mouse action failure was shown in the visible status surface.");
            return true;
        }

        UpdateStatus($"No visible control matched '{label}'.");
        RecordVisibleUiActionAudit(
            $"visible_control_{actionName}",
            "failed",
            normalizedLabel,
            "no_matching_visible_control",
            false,
            "Visible control mouse action failure was shown in the visible status surface.");
        return true;
    }

    private bool TryActivateVisibleControlByNumber(string normalizedLabel, out string message)
    {
        message = string.Empty;

        if (!int.TryParse(normalizedLabel, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number <= 0)
            return false;

        var entry = _visibleControlsSummary.FirstOrDefault(item => item.Number == number);
        var desktopEntry = _desktopVisibleControlsSummary.FirstOrDefault(item => item.Number == number);
        if (desktopEntry != null)
        {
            _desktopVisibleControlService.TryActivate(desktopEntry, out message);
            return true;
        }

        if (entry is null)
        {
            message = "Show visible controls first, then say a number from the list.";
            return true;
        }

        var control = entry.Control;
        if (!control.Visible || !control.Enabled)
        {
            message = $"Visible control {number} is no longer available.";
            return true;
        }

        if (control is Button button)
        {
            button.PerformClick();
            message = $"Activated '{entry.Label}' from visible controls.";
            return true;
        }

        if (control is TextBoxBase textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
            message = $"Focused '{entry.Label}' from visible controls.";
            return true;
        }

        if (control is ComboBox comboBox)
        {
            comboBox.Focus();
            message = $"Focused '{entry.Label}' from visible controls.";
            return true;
        }

        if (control is ListBox listBox)
        {
            listBox.Focus();
            if (listBox.Items.Count > 0 && listBox.SelectedIndex < 0)
                listBox.SelectedIndex = 0;
            message = $"Focused '{entry.Label}' from visible controls.";
            return true;
        }

        if (control is TabPage tabPage && tabPage.Parent is TabControl tabControl)
        {
            tabControl.SelectedTab = tabPage;
            message = $"Opened tab '{entry.Label}' from visible controls.";
            return true;
        }

        control.Focus();
        message = $"Focused '{entry.Label}' from visible controls.";
        return true;
    }

    private bool TryMouseActionVisibleControlByNumber(string normalizedLabel, DesktopVisibleControlMouseAction action, out string message)
    {
        message = string.Empty;

        if (!int.TryParse(normalizedLabel, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number <= 0)
            return false;

        var desktopEntry = _desktopVisibleControlsSummary.FirstOrDefault(item => item.Number == number);
        if (desktopEntry != null)
        {
            _desktopVisibleControlService.TryMouseAction(desktopEntry, action, out message);
            return true;
        }

        var entry = _visibleControlsSummary.FirstOrDefault(item => item.Number == number);
        if (entry is null)
        {
            message = "Show visible controls first, then say a number from the list.";
            return true;
        }

        var control = entry.Control;
        if (!control.Visible || !control.Enabled)
        {
            message = $"Visible control {number} is no longer available.";
            return true;
        }

        return TryPerformControlMouseAction(control, action, out message, entry.Label);
    }

    private bool TryPerformControlMouseAction(Control control, DesktopVisibleControlMouseAction action, out string message, string? label = null)
    {
        if (!control.Visible || !control.Enabled)
        {
            message = $"Visible control '{label ?? GetControlVoiceLabel(control)}' is no longer available.";
            return false;
        }

        var screenBounds = control.RectangleToScreen(control.ClientRectangle);
        if (screenBounds.IsEmpty)
        {
            message = $"Visible control '{label ?? GetControlVoiceLabel(control)}' has no visible bounds.";
            return false;
        }

        var center = new Point(screenBounds.Left + screenBounds.Width / 2, screenBounds.Top + screenBounds.Height / 2);
        Cursor.Position = center;
        switch (action)
        {
            case DesktopVisibleControlMouseAction.DoubleClick:
                mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                message = $"Double-clicked '{label ?? GetControlVoiceLabel(control)}' from visible controls.";
                return true;
            case DesktopVisibleControlMouseAction.RightClick:
                mouse_event(MouseEventRightDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);
                message = $"Right-clicked '{label ?? GetControlVoiceLabel(control)}' from visible controls.";
                return true;
            default:
                message = $"Unsupported visible control mouse action: {action}.";
                return false;
        }
    }

    private static bool TryFindVisibleControlByLabel(Control root, string normalizedLabel, out Control? match)
    {
        if (root.Visible && root.Enabled && IsMatchingVisibleControlLabel(root, normalizedLabel))
        {
            match = root;
            return true;
        }

        foreach (Control child in root.Controls)
        {
            if (TryFindVisibleControlByLabel(child, normalizedLabel, out match))
                return true;
        }

        match = null;
        return false;
    }

    private static bool IsMatchingVisibleControlLabel(Control control, string normalizedLabel)
    {
        if (string.IsNullOrWhiteSpace(normalizedLabel))
            return false;

        foreach (var commandVariant in EnumerateVisibleControlCommandVariants(normalizedLabel))
        {
            foreach (var candidate in EnumerateControlVoiceLabels(control))
            {
                if (string.Equals(NormalizeVisibleControlLabel(candidate), commandVariant, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateVisibleControlCommandVariants(string normalizedLabel)
    {
        var current = normalizedLabel.Trim();
        if (string.IsNullOrWhiteSpace(current))
            yield break;

        yield return current;

        var prefixes = new[]
        {
            "browser",
            "system",
            "files",
            "dictation",
            "voice",
            "session",
            "account"
        };

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var prefix in prefixes)
            {
                if (!current.StartsWith($"{prefix} ", StringComparison.OrdinalIgnoreCase))
                    continue;

                current = current[prefix.Length..].Trim();
                if (string.IsNullOrWhiteSpace(current))
                    yield break;

                yield return current;
                changed = true;
                break;
            }
        }
    }

    private static IEnumerable<string> EnumerateControlVoiceLabels(Control control)
    {
        if (!string.IsNullOrWhiteSpace(control.Text))
            yield return control.Text;

        if (!string.IsNullOrWhiteSpace(control.AccessibleName))
            yield return control.AccessibleName;

        if (!string.IsNullOrWhiteSpace(control.Name))
            yield return control.Name;

        if (control.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            yield return tag;
    }

    private static string GetControlVoiceLabel(Control control)
    {
        var label = EnumerateControlVoiceLabels(control).FirstOrDefault();
        return string.IsNullOrWhiteSpace(label) ? control.GetType().Name : label;
    }

    private static string NormalizeVisibleControlLabel(string value)
    {
        var normalized = NormalizeSpeechText(value);
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

    private void ShowVoiceHelp()
    {
        _tabs.SelectedIndex = 1;
        _voiceHelpTextBox.Text = BuildVoiceHelpText();
        ShowCommandPalette();
        UpdateStatus("Opened voice help.");
        RecordHelpDiscoveryAudit(
            "help_command_palette",
            "succeeded",
            "what_can_i_say",
            "opened_command_palette",
            true,
            "Command palette was shown in the visible help surface.");
    }

    private void ShowStartupWalkthrough()
    {
        using var walkthrough = new StartupWalkthroughForm(tabName => SelectTab(tabName));
        var result = walkthrough.ShowDialog(this);
        _startupWalkthroughShownThisSession = true;

        if (result == DialogResult.OK)
            PersistStartupWalkthroughSeen();

        UpdateStatus("Opened getting started.");
        RecordHelpDiscoveryAudit(
            "help_startup_walkthrough",
            "succeeded",
            "getting_started",
            result.ToString(),
            true,
            "Getting Started walkthrough was shown in the visible help surface.");
    }

    private void ShowPacksTab()
    {
        var packTab = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals(page.Text, "Packs", StringComparison.OrdinalIgnoreCase));
        if (packTab != null)
            _tabs.SelectedTab = packTab;
        RefreshPacksPanel(forceReload: false);
        UpdateStatus("Opened extension packs.");
        RecordHelpDiscoveryAudit(
            "help_pack_management",
            "succeeded",
            "packs",
            "opened_pack_management",
            true,
            "Extension pack management was shown in the visible help surface.");
    }

    private static string BuildVoiceHelpText()
    {
        return CommandDiscoveryService.BuildHelpText();
    }

    private void ShowCommandPalette()
    {
        if (_commandPalette == null || _commandPalette.IsDisposed)
            _commandPalette = new CommandPaletteForm();

        _commandPalette.ShowPalette(this, CommandDiscoveryService.GetCommands());
    }

    private void HideCommandPalette()
    {
        if (_commandPalette?.Visible == true)
            _commandPalette.Hide();

        UpdateStatus("Command palette hidden.");
        RecordHelpDiscoveryAudit(
            "help_command_palette",
            "succeeded",
            "hide_command_palette",
            "hidden",
            true,
            "Command palette dismissal was shown in the visible help surface.");
    }

    private void ShowDictationCorrectionChoices(DictationReplacementScope scope)
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to correct.");
            RecordDictationActionAudit(
                "dictation_correction_show",
                "failed",
                scope.ToString(),
                "empty_review_buffer",
                false,
                "Dictation correction failure was shown in the visible Dictation review surface.");
            return;
        }

        var session = DictationCorrectionService.CreateSession(_dictationTextBox.Text, scope);
        if (session.Choices.Count == 0)
        {
            UpdateStatus("There is no dictated text in that scope to correct.");
            RecordDictationActionAudit(
                "dictation_correction_show",
                "failed",
                scope.ToString(),
                "no_choices",
                false,
                "Dictation correction failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationCorrectionSession = session;
        if (_dictationCorrectionForm == null || _dictationCorrectionForm.IsDisposed)
            _dictationCorrectionForm = new DictationCorrectionForm();

        var firstChoice = session.Choices[0];
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = firstChoice.Start;
        _dictationTextBox.SelectionLength = firstChoice.Length;
        _dictationCorrectionForm.ShowCorrections(this, session.Choices, scope);
        UpdateStatus($"Showing {session.Choices.Count} correction alternatives for {FormatDictationCorrectionScope(scope)}.");
        RecordDictationActionAudit(
            "dictation_correction_show",
            "succeeded",
            scope.ToString(),
            $"{session.Choices.Count} choices",
            true,
            "Dictation correction alternatives were shown in the visible correction HUD.");
    }

    private void ChooseDictationCorrection(int choiceNumber)
    {
        if (_dictationCorrectionSession == null)
        {
            UpdateStatus("No correction alternatives are open.");
            RecordDictationActionAudit(
                "dictation_correction_choose",
                "failed",
                $"choice_{choiceNumber}",
                "no_open_session",
                false,
                "Dictation correction choice failure was shown in the visible Dictation review surface.");
            return;
        }

        var choice = _dictationCorrectionSession.Choices.FirstOrDefault(candidate => candidate.Number == choiceNumber);
        if (choice == null)
        {
            UpdateStatus($"Correction {choiceNumber} is not available.");
            RecordDictationActionAudit(
                "dictation_correction_choose",
                "failed",
                $"choice_{choiceNumber}",
                "choice_unavailable",
                false,
                "Dictation correction choice failure was shown in the visible Dictation review surface.");
            return;
        }

        if (!DictationCorrectionService.TryApplyChoice(_dictationTextBox.Text, choice, out var updatedText, out var selectionStart))
        {
            UpdateStatus("The dictated text changed before the correction could be applied.");
            RecordDictationActionAudit(
                "dictation_correction_choose",
                "failed",
                $"choice_{choiceNumber}",
                "text_changed",
                false,
                "Dictation correction choice failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationTextBox.Text = updatedText;
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = Math.Clamp(selectionStart, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        _dictationCorrectionForm?.Hide();
        _dictationCorrectionSession = null;
        RefreshDictationPanel();
        UpdateStatus($"Applied correction {choiceNumber}: {choice.Text}.");
        RecordDictationActionAudit(
            "dictation_correction_choose",
            "succeeded",
            $"choice_{choiceNumber}",
            choice.Label,
            true,
            "Dictation correction choice was applied in the visible Dictation review surface.");
    }

    private void CancelDictationCorrection()
    {
        _dictationCorrectionForm?.Hide();
        _dictationCorrectionSession = null;
        _dictationTextBox.Focus();
        UpdateStatus("Correction alternatives cancelled.");
        RecordDictationActionAudit(
            "dictation_correction_cancel",
            "succeeded",
            "cancel",
            "cancelled",
            true,
            "Dictation correction cancellation was shown in the visible Dictation review surface.");
    }

    private void AcceptSelectedDictationCorrection()
    {
        if (_dictationCorrectionSession == null || _dictationCorrectionForm == null || _dictationCorrectionForm.IsDisposed)
        {
            UpdateStatus("No correction alternatives are open.");
            RecordDictationActionAudit(
                "dictation_correction_accept",
                "failed",
                "selected",
                "no_open_session",
                false,
                "Dictation correction accept failure was shown in the visible correction HUD.");
            return;
        }

        if (!int.TryParse(_dictationCorrectionForm.SelectedChoiceNumber, out var choiceNumber))
        {
            UpdateStatus("No correction alternative is selected.");
            RecordDictationActionAudit(
                "dictation_correction_accept",
                "failed",
                "selected",
                "no_selected_choice",
                false,
                "Dictation correction accept failure was shown in the visible correction HUD.");
            return;
        }

        ChooseDictationCorrection(choiceNumber);
    }

    private void MoveDictationCorrectionSelection(int delta)
    {
        if (_dictationCorrectionSession == null || _dictationCorrectionForm == null || _dictationCorrectionForm.IsDisposed)
        {
            UpdateStatus("No correction alternatives are open.");
            RecordDictationActionAudit(
                "dictation_correction_navigate",
                "failed",
                delta < 0 ? "previous" : "next",
                "no_open_session",
                false,
                "Dictation correction navigation failure was shown in the visible correction HUD.");
            return;
        }

        var moved = _dictationCorrectionForm.MoveSelectionByVoice(delta);
        UpdateStatus(moved
            ? $"Selected {_dictationCorrectionForm.SelectedChoiceNumber}: {_dictationCorrectionForm.SelectedChoiceText}."
            : "No more correction alternatives in that direction.");
        RecordDictationActionAudit(
            "dictation_correction_navigate",
            moved ? "succeeded" : "unchanged",
            delta < 0 ? "previous" : "next",
            _dictationCorrectionForm.SelectedChoiceNumber,
            true,
            "Dictation correction navigation was shown in the visible correction HUD.");
    }

    private static string FormatDictationCorrectionScope(DictationReplacementScope scope) =>
        scope switch
        {
            DictationReplacementScope.PreviousSentence => "the previous sentence",
            DictationReplacementScope.PreviousParagraph => "the previous paragraph",
            DictationReplacementScope.AllText => "all dictated text",
            _ => "the previous word"
        };

    private void ShowVisibleControlsSummary(VisibleControlsScope scope = VisibleControlsScope.CurrentSurface)
    {
        _visibleControlsScope = scope;
        if (scope != VisibleControlsScope.NamedWindow)
            _visibleControlsNamedWindowTarget = string.Empty;
        UpdateVisibleControlsOverlay();
        var statusMessage = scope switch
        {
            VisibleControlsScope.Taskbar => "Opened taskbar visible controls summary.",
            VisibleControlsScope.NamedWindow => $"Opened visible controls summary for {_visibleControlsNamedWindowTarget}.",
            _ => "Opened visible controls summary."
        };
        UpdateStatus(statusMessage);
        RecordVisibleUiActionAudit(
            "visible_controls_overlay",
            "succeeded",
            scope switch
            {
                VisibleControlsScope.Taskbar => "show_taskbar",
                VisibleControlsScope.NamedWindow => $"show_named_window:{_visibleControlsNamedWindowTarget}",
                _ => "show"
            },
            scope switch
            {
                VisibleControlsScope.Taskbar => "opened_taskbar_visible_controls_summary",
                VisibleControlsScope.NamedWindow => "opened_named_window_visible_controls_summary",
                _ => "opened_visible_controls_summary"
            },
            true,
            "Visible controls overlay state was shown in the visible status surface.");
    }

    private bool ShowVisibleControlsForNamedWindow(string windowTarget)
    {
        if (string.IsNullOrWhiteSpace(windowTarget))
        {
            UpdateStatus("Say the app or window name after 'show numbers on'.");
            RecordVisibleUiActionAudit(
                "visible_controls_overlay",
                "failed",
                "show_named_window",
                "missing_window_target",
                false,
                "Visible controls overlay failure was shown in the visible status surface.");
            return true;
        }

        _visibleControlsNamedWindowTarget = windowTarget.Trim();
        ShowVisibleControlsSummary(VisibleControlsScope.NamedWindow);
        return true;
    }

    private void UpdateVisibleControlsOverlay()
    {
        if (_visibleControlsOverlay == null || !_visibleControlsOverlay.Visible)
            return;

        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        var focusedControl = FindFocusedControl(this);
        var summary = BuildVisibleControlsSummary(_visibleControlsScope);
        var overlayBounds = _desktopVisibleControlsSummary.Count > 0
            ? SystemInformation.VirtualScreen
            : Bounds;
        var cue = runtimeIsFresh
            ? _dictationActive
                ? BuildRuntimeDictationSpeechCueText(runtimeSnapshot!)
                : BuildRuntimeSessionSpeechCueText(runtimeSnapshot!)
            : _dictationActive
                ? BuildLocalDictationOverlayCaptionText()
                : _voiceCommandService.IsSpeechActive
                    ? BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence)
                    : "Voice cue: nothing heard yet.";
        var heard = runtimeIsFresh
            ? FormatRuntimeHeardLabel(runtimeSnapshot!)
            : _dictationActive
                ? BuildLocalDictationReadout()
                : _voiceCommandService.IsSpeechActive
                    ? BuildLocalOverlayReadout()
                    : "Heard: nothing yet.";
        var items = _desktopVisibleControlsSummary.Count > 0
            ? _desktopVisibleControlsSummary.Select(entry => $"{entry.Number}. {entry.Label}").ToList()
            : _visibleControlsSummary.Select(entry => $"{entry.Number}. {entry.Label}").ToList();
        var annotations = _desktopVisibleControlsSummary.Count > 0
            ? _desktopVisibleControlsSummary
                .Select(entry => new VisibleControlOverlayAnnotation(
                    entry.Number,
                    entry.Bounds,
                    entry.Label,
                    false))
                .ToList()
            : _visibleControlsSummary
                .Select(entry => new VisibleControlOverlayAnnotation(
                    entry.Number,
                    entry.Control.RectangleToScreen(entry.Control.ClientRectangle),
                    entry.Label,
                    ReferenceEquals(entry.Control, focusedControl)))
                .ToList();
        ShowVisibleControlsOverlay(overlayBounds, summary, cue, heard, items, annotations);
    }

    private void ShowVisibleControlsOverlay(Rectangle overlayBounds, string summary, string cue, string heard, IReadOnlyList<string> numberedItems, IReadOnlyList<VisibleControlOverlayAnnotation> annotations)
    {
        if (_visibleControlsOverlay == null)
        {
            try
            {
                _visibleControlsOverlay = new VisibleControlsOverlayForm();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Visible controls overlay could not be created: {ex.Message}");
                return;
            }
        }

        _visibleControlsOverlay.ShowOverlay(overlayBounds, summary, cue, heard, numberedItems, annotations);
        if (!_visibleControlsRefreshTimer.Enabled)
            _visibleControlsRefreshTimer.Start();
    }

    private void HideVisibleControlsOverlay()
    {
        _visibleControlsOverlay?.HideOverlay();
        _desktopVisibleControlsSummary = [];
        _visibleControlsScope = VisibleControlsScope.CurrentSurface;
        _visibleControlsNamedWindowTarget = string.Empty;
        if (_visibleControlsRefreshTimer.Enabled)
            _visibleControlsRefreshTimer.Stop();
        RecordVisibleUiActionAudit(
            "visible_controls_overlay",
            "succeeded",
            "hide",
            "hidden_visible_controls_summary",
            true,
            "Visible controls overlay state was shown in the visible status surface.");
    }

    private void ShowKeyboardOverlay()
    {
        if (_keyboardOverlay == null || _keyboardOverlay.IsDisposed)
        {
            try
            {
                _keyboardOverlay = new KeyboardOverlayForm();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Keyboard overlay could not be created: {ex.Message}");
                return;
            }
        }

        _keyboardOverlay.ShowKeyboard(SystemInformation.VirtualScreen);
        UpdateStatus("Keyboard shown. Say 'press A', 'press Space', 'press Enter', or 'hide keyboard'.");
    }

    private void HideKeyboardOverlay()
    {
        if (_keyboardOverlay?.Visible == true)
            _keyboardOverlay.Hide();

        UpdateStatus("Keyboard hidden.");
    }

    private void ShowMouseGrid(MouseGridScope scope)
    {
        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
        {
            try
            {
                _mouseGridOverlay = new MouseGridOverlayForm();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Mouse grid could not be created: {ex.Message}");
                RecordVisibleUiActionAudit(
                    "mouse_grid",
                    "failed",
                    "show",
                    ex.Message,
                    false,
                    "Mouse grid failure was shown in the visible status surface.");
                return;
            }
        }

        Rectangle gridBounds;
        IReadOnlyList<MouseGridDisplayRegion>? displayRegions = null;
        string statusMessage;
        string actionTarget;

        if (scope == MouseGridScope.CurrentWindow)
        {
            if (!_desktopVisibleControlService.TryGetForegroundWindowBounds(out gridBounds, out var windowTitle, out var warning))
            {
                UpdateStatus($"Mouse grid could not target the current window. {warning}");
                RecordVisibleUiActionAudit(
                    "mouse_grid",
                    "failed",
                    "show_here",
                    warning,
                    false,
                    "Mouse grid failure was shown in the visible status surface.");
                return;
            }

            _mouseGridOverlay.ShowGrid(gridBounds);
            statusMessage = $"Mouse grid shown for {windowTitle}. Say 'grid 1' through 'grid 9', 'click grid 5', 'drag grid 1 to grid 9', or 'hide grid'.";
            actionTarget = "show_here";
        }
        else
        {
            displayRegions = GetMouseGridDisplayRegions();
            gridBounds = SystemInformation.VirtualScreen;
            _mouseGridOverlay.ShowGrid(gridBounds, displayRegions);
            var multiDisplayHint = displayRegions.Count > 1
                ? " Say 'A' or 'Alpha' after 'mouse grid', or use a shortcut like 'mouse grid A 114'."
                : string.Empty;
            statusMessage = $"Mouse grid shown. Say 'grid 1' through 'grid 9', 'click grid 5', 'drag grid 1 to grid 9', or 'hide grid'.{multiDisplayHint}";
            actionTarget = "show";
        }

        UpdateStatus(statusMessage);
        RecordVisibleUiActionAudit(
            "mouse_grid",
            "succeeded",
            actionTarget,
            "shown",
            true,
            "Mouse grid state was shown in the visible status surface.");
    }

    private IReadOnlyList<MouseGridDisplayRegion> GetMouseGridDisplayRegions()
    {
        var displayBounds = Screen.AllScreens
            .Select(screen => screen.Bounds)
            .Where(bounds => !bounds.IsEmpty)
            .ToArray();
        return MouseGridOverlayForm.CreateDisplayRegions(SystemInformation.VirtualScreen, displayBounds);
    }

    private void HideMouseGrid()
    {
        _mouseGridOverlay?.ClearMarkedPoint();
        if (_mouseGridOverlay?.Visible == true)
            _mouseGridOverlay.Hide();

        UpdateStatus("Mouse grid hidden.");
        RecordVisibleUiActionAudit(
            "mouse_grid",
            "succeeded",
            "hide",
            "hidden",
            true,
            "Mouse grid state was shown in the visible status surface.");
    }

    private void UndoMouseGrid()
    {
        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
        {
            ExecuteSystemAction("system-undo", "Undo requested.");
            return;
        }

        if (!_mouseGridOverlay.Undo())
        {
            UpdateStatus("Mouse grid is already at the widest view.");
            RecordVisibleUiActionAudit(
                "mouse_grid_undo",
                "unchanged",
                "undo",
                "already_at_root",
                true,
                "Mouse grid undo state was shown in the visible status surface.");
            return;
        }

        Cursor.Position = GetRectangleCenter(_mouseGridOverlay.GridBounds);
        UpdateStatus("Mouse grid undone. Focus returned to the previous grid view.");
        RecordVisibleUiActionAudit(
            "mouse_grid_undo",
            "succeeded",
            "undo",
            "reverted",
            true,
            "Mouse grid undo state was shown in the visible status surface.");
    }

    private void MarkMouseGrid(int? cellNumber = null)
    {
        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        if (cellNumber is < 0 or > 9)
        {
            UpdateStatus("Choose a grid number from 1 to 9.");
            RecordVisibleUiActionAudit(
                "mouse_grid_mark",
                "failed",
                $"cell_{cellNumber}",
                "invalid_cell",
                false,
                "Mouse grid mark failure was shown in the visible status surface.");
            return;
        }

        Point markPoint;
        if (cellNumber.HasValue)
        {
            markPoint = MouseGridOverlayForm.CalculateCellCenter(_mouseGridOverlay.GridBounds, cellNumber.Value);
        }
        else if (_mouseGridOverlay.GridBounds.Contains(Cursor.Position))
        {
            markPoint = Cursor.Position;
        }
        else
        {
            markPoint = GetRectangleCenter(_mouseGridOverlay.GridBounds);
        }

        if (markPoint == Point.Empty)
        {
            UpdateStatus("Mouse grid could not mark that drag start.");
            RecordVisibleUiActionAudit(
                "mouse_grid_mark",
                "failed",
                cellNumber.HasValue ? $"cell_{cellNumber.Value}" : "current",
                "empty_mark_point",
                false,
                "Mouse grid mark failure was shown in the visible status surface.");
            return;
        }

        Cursor.Position = markPoint;
        _mouseGridOverlay.SetMarkedPoint(markPoint);
        _mouseGridOverlay.ResetToRoot();
        UpdateStatus(cellNumber.HasValue
            ? $"Mouse grid marked cell {cellNumber.Value}. Drill to the destination and say 'drag'."
            : "Mouse grid marked the current drag start. Drill to the destination and say 'drag'.");
        RecordVisibleUiActionAudit(
            "mouse_grid_mark",
            "succeeded",
            cellNumber.HasValue ? $"cell_{cellNumber.Value}" : "current",
            "marked",
            true,
            "Mouse grid mark state was shown in the visible status surface.");
    }

    private void SelectMouseGridCell(int cellNumber, bool click)
    {
        if (cellNumber is < 1 or > 9)
        {
            UpdateStatus("Choose a grid number from 1 to 9.");
            RecordVisibleUiActionAudit(
                "mouse_grid_select",
                "failed",
                $"cell_{cellNumber}",
                "invalid_cell",
                false,
                "Mouse grid selection failure was shown in the visible status surface.");
            return;
        }

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        var cellBounds = _mouseGridOverlay.RefineToCell(cellNumber);
        if (cellBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not resolve that cell.");
            RecordVisibleUiActionAudit(
                "mouse_grid_select",
                "failed",
                $"cell_{cellNumber}",
                "empty_cell_bounds",
                false,
                "Mouse grid selection failure was shown in the visible status surface.");
            return;
        }

        var center = GetRectangleCenter(cellBounds);
        Cursor.Position = center;

        if (!click)
        {
            UpdateStatus($"Mouse grid refined to {cellNumber}. Cursor moved to the cell center.");
            RecordVisibleUiActionAudit(
                "mouse_grid_select",
                "succeeded",
                $"cell_{cellNumber}",
                "refined",
                true,
                "Mouse grid selection was shown in the visible status surface.");
            return;
        }

        if (_systemControlService.TryExecute("system-mouse-click", out var message))
        {
            HideMouseGrid();
            UpdateStatus($"Clicked grid {cellNumber}. {message}");
            RecordVisibleUiActionAudit(
                "mouse_grid_click",
                "succeeded",
                $"cell_{cellNumber}",
                message,
                true,
                "Mouse grid click was shown in the visible status surface.");
            return;
        }

        UpdateStatus(message);
        RecordVisibleUiActionAudit(
            "mouse_grid_click",
            "failed",
            $"cell_{cellNumber}",
            message,
            false,
            "Mouse grid click failure was shown in the visible status surface.");
    }

    private void FocusMouseGridDisplay(string identifier)
    {
        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        var displayBounds = _mouseGridOverlay.FocusDisplay(identifier);
        if (displayBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not find that display. Try A, B, C, or a NATO display name such as Alpha or Bravo.");
            RecordVisibleUiActionAudit(
                "mouse_grid_display_focus",
                "failed",
                identifier,
                "display_not_found",
                false,
                "Mouse grid display focus failure was shown in the visible status surface.");
            return;
        }

        UpdateStatus($"Mouse grid focused on display {_mouseGridOverlay.FocusedDisplayIdentifier}. Say 'grid 1' through 'grid 9' to refine the display.");
        RecordVisibleUiActionAudit(
            "mouse_grid_display_focus",
            "succeeded",
            _mouseGridOverlay.FocusedDisplayIdentifier,
            "focused",
            true,
            "Mouse grid display focus was shown in the visible status surface.");
    }

    private void FocusMouseGridPath(string identifier, string pathDigits)
    {
        if (string.IsNullOrWhiteSpace(pathDigits) || pathDigits.Any(character => character is < '1' or > '9'))
        {
            UpdateStatus("Mouse grid path must use digits 1 through 9.");
            RecordVisibleUiActionAudit(
                "mouse_grid_path",
                "failed",
                $"{identifier}:{pathDigits}",
                "invalid_path",
                false,
                "Mouse grid path failure was shown in the visible status surface.");
            return;
        }

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        var displayBounds = _mouseGridOverlay.FocusDisplay(identifier);
        if (displayBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not find that display for the shortcut path.");
            RecordVisibleUiActionAudit(
                "mouse_grid_path",
                "failed",
                $"{identifier}:{pathDigits}",
                "display_not_found",
                false,
                "Mouse grid path failure was shown in the visible status surface.");
            return;
        }

        Rectangle finalBounds = displayBounds;
        foreach (var pathCharacter in pathDigits)
            finalBounds = _mouseGridOverlay.RefineToCell(pathCharacter - '0');

        if (finalBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not resolve that shortcut path.");
            RecordVisibleUiActionAudit(
                "mouse_grid_path",
                "failed",
                $"{identifier}:{pathDigits}",
                "empty_path_bounds",
                false,
                "Mouse grid path failure was shown in the visible status surface.");
            return;
        }

        Cursor.Position = GetRectangleCenter(finalBounds);
        UpdateStatus($"Mouse grid moved to display {_mouseGridOverlay.FocusedDisplayIdentifier} path {pathDigits}. Cursor moved to the refined cell center.");
        RecordVisibleUiActionAudit(
            "mouse_grid_path",
            "succeeded",
            $"{_mouseGridOverlay.FocusedDisplayIdentifier}:{pathDigits}",
            "focused",
            true,
            "Mouse grid path refinement was shown in the visible status surface.");
    }

    private void FocusMouseGridShortcutPath(string pathDigits)
    {
        if (string.IsNullOrWhiteSpace(pathDigits) || pathDigits.Length < 2 || pathDigits.Any(character => character is < '1' or > '9'))
        {
            UpdateStatus("Mouse grid shortcut paths must use digits 1 through 9.");
            RecordVisibleUiActionAudit(
                "mouse_grid_shortcut_path",
                "failed",
                pathDigits,
                "invalid_path",
                false,
                "Mouse grid path failure was shown in the visible status surface.");
            return;
        }

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        Rectangle finalBounds = _mouseGridOverlay.GridBounds;
        foreach (var pathCharacter in pathDigits)
            finalBounds = _mouseGridOverlay.RefineToCell(pathCharacter - '0');

        if (finalBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not resolve that shortcut path.");
            RecordVisibleUiActionAudit(
                "mouse_grid_shortcut_path",
                "failed",
                pathDigits,
                "empty_path_bounds",
                false,
                "Mouse grid path failure was shown in the visible status surface.");
            return;
        }

        Cursor.Position = GetRectangleCenter(finalBounds);
        UpdateStatus($"Mouse grid moved to path {pathDigits}. Cursor moved to the refined cell center.");
        RecordVisibleUiActionAudit(
            "mouse_grid_shortcut_path",
            "succeeded",
            pathDigits,
            "focused",
            true,
            "Mouse grid path refinement was shown in the visible status surface.");
    }

    private static bool TryParseTrailingNumber(string value, out int number)
    {
        var marker = value.LastIndexOf(':');
        var numberText = marker >= 0 ? value[(marker + 1)..].Trim() : value.Trim();
        return int.TryParse(numberText, out number);
    }

    private static bool TryParseGridDragTarget(string value, out int fromCell, out int toCell)
    {
        fromCell = 0;
        toCell = 0;

        var normalizedPrefix = "ui drag mouse grid ";
        if (value.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = value[normalizedPrefix.Length..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length >= 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out fromCell)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out toCell)
                && fromCell is >= 1 and <= 9
                && toCell is >= 1 and <= 9;
        }

        var marker = value.LastIndexOf(':');
        if (marker < 0)
            return false;

        var previousMarker = value.LastIndexOf(':', marker - 1);
        if (previousMarker < 0)
            return false;

        return int.TryParse(value[(previousMarker + 1)..marker].Trim(), out fromCell)
            && int.TryParse(value[(marker + 1)..].Trim(), out toCell)
            && fromCell is >= 1 and <= 9
            && toCell is >= 1 and <= 9;
    }

    private void DragMouseGridCells(int fromCell, int toCell)
    {
        if (fromCell is < 1 or > 9 || toCell is < 1 or > 9)
        {
            UpdateStatus("Choose grid numbers from 1 to 9.");
            RecordVisibleUiActionAudit(
                "mouse_grid_drag",
                "failed",
                $"{fromCell}->{toCell}",
                "invalid_cell",
                false,
                "Mouse grid drag failure was shown in the visible status surface.");
            return;
        }

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
            ShowMouseGrid(MouseGridScope.Desktop);

        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed)
            return;

        var bounds = _mouseGridOverlay.GridBounds;
        var fromBounds = MouseGridOverlayForm.CalculateCellBounds(bounds, fromCell);
        var toBounds = MouseGridOverlayForm.CalculateCellBounds(bounds, toCell);
        if (fromBounds.IsEmpty || toBounds.IsEmpty)
        {
            UpdateStatus("Mouse grid could not resolve the drag cells.");
            RecordVisibleUiActionAudit(
                "mouse_grid_drag",
                "failed",
                $"{fromCell}->{toCell}",
                "empty_cell_bounds",
                false,
                "Mouse grid drag failure was shown in the visible status surface.");
            return;
        }

        var fromPoint = GetRectangleCenter(fromBounds);
        var toPoint = GetRectangleCenter(toBounds);
        Cursor.Position = fromPoint;
        SendVisibleMouseDrag(fromPoint, toPoint);
        HideMouseGrid();
        UpdateStatus($"Dragged grid {fromCell} to grid {toCell}.");
        RecordVisibleUiActionAudit(
            "mouse_grid_drag",
            "succeeded",
            $"{fromCell}->{toCell}",
            "dragged",
            true,
            "Mouse grid drag was shown in the visible status surface.");
    }

    private void DragMarkedMouseGrid()
    {
        if (_mouseGridOverlay == null || _mouseGridOverlay.IsDisposed || !_mouseGridOverlay.Visible)
        {
            UpdateStatus("Show grid first, then say 'mark' and 'drag'.");
            RecordVisibleUiActionAudit(
                "mouse_grid_drag_marked",
                "failed",
                "drag",
                "grid_not_visible",
                false,
                "Mouse grid drag failure was shown in the visible status surface.");
            return;
        }

        if (!_mouseGridOverlay.MarkedPoint.HasValue)
        {
            UpdateStatus("Mark a drag start in the mouse grid first.");
            RecordVisibleUiActionAudit(
                "mouse_grid_drag_marked",
                "failed",
                "drag",
                "missing_mark",
                false,
                "Mouse grid drag failure was shown in the visible status surface.");
            return;
        }

        var destinationPoint = _mouseGridOverlay.GridBounds.Contains(Cursor.Position)
            ? Cursor.Position
            : GetRectangleCenter(_mouseGridOverlay.GridBounds);
        if (destinationPoint == Point.Empty)
        {
            UpdateStatus("Mouse grid could not resolve the marked drag destination.");
            RecordVisibleUiActionAudit(
                "mouse_grid_drag_marked",
                "failed",
                "drag",
                "empty_destination",
                false,
                "Mouse grid drag failure was shown in the visible status surface.");
            return;
        }

        SendVisibleMouseDrag(_mouseGridOverlay.MarkedPoint.Value, destinationPoint);
        HideMouseGrid();
        UpdateStatus("Dragged the marked item to the current mouse-grid location.");
        RecordVisibleUiActionAudit(
            "mouse_grid_drag_marked",
            "succeeded",
            "drag",
            "dragged_marked_item",
            true,
            "Mouse grid drag was shown in the visible status surface.");
    }

    private static Point GetRectangleCenter(Rectangle bounds) =>
        new(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));

    private static void SendVisibleMouseDrag(Point fromPoint, Point toPoint)
    {
        Cursor.Position = fromPoint;
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        Cursor.Position = toPoint;
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    private string BuildVisibleControlsSummary(VisibleControlsScope scope)
    {
        if (scope == VisibleControlsScope.NamedWindow)
        {
            if (_desktopVisibleControlService.TryCaptureNamedWindow(Environment.ProcessId, _visibleControlsNamedWindowTarget, out var namedWindowSnapshot))
            {
                _desktopVisibleControlsSummary = namedWindowSnapshot.Controls.ToList();
                _visibleControlsSummary = [];
                return $"Visible controls for {namedWindowSnapshot.WindowTitle}:\n\n" + string.Join(
                    Environment.NewLine,
                    namedWindowSnapshot.Controls.Select(entry => $"{entry.Number}. {entry.Label}"));
            }

            _desktopVisibleControlsSummary = [];
            _visibleControlsSummary = [];
            var warning = string.IsNullOrWhiteSpace(namedWindowSnapshot.Warning)
                ? $"No visible app or window matched '{_visibleControlsNamedWindowTarget}'."
                : namedWindowSnapshot.Warning;
            return $"Visible controls for {_visibleControlsNamedWindowTarget}:\n\n{warning}";
        }

        if (scope == VisibleControlsScope.Taskbar)
        {
            if (_desktopVisibleControlService.TryCaptureTaskbar(out var taskbarSnapshot))
            {
                _desktopVisibleControlsSummary = taskbarSnapshot.Controls.ToList();
                _visibleControlsSummary = [];
                return $"Visible controls for {taskbarSnapshot.WindowTitle}:\n\n" + string.Join(
                    Environment.NewLine,
                    taskbarSnapshot.Controls.Select(entry => $"{entry.Number}. {entry.Label}"));
            }

            _desktopVisibleControlsSummary = [];
            _visibleControlsSummary = [];
            var warning = string.IsNullOrWhiteSpace(taskbarSnapshot.Warning)
                ? "No visible taskbar controls were found."
                : taskbarSnapshot.Warning;
            return $"Visible controls for Taskbar:\n\n{warning}";
        }

        if (_desktopVisibleControlService.TryCaptureForegroundWindow(Environment.ProcessId, out var desktopSnapshot))
        {
            _desktopVisibleControlsSummary = desktopSnapshot.Controls.ToList();
            _visibleControlsSummary = [];
            return $"Visible controls for {desktopSnapshot.WindowTitle}:\n\n" + string.Join(
                Environment.NewLine,
                desktopSnapshot.Controls.Select(entry => $"{entry.Number}. {entry.Label}"));
        }

        _desktopVisibleControlsSummary = [];
        var activeTabName = _tabs?.SelectedTab?.Text ?? "Current view";
        var visibleControls = EnumerateVisibleInteractiveControls(this)
            .Where(control => control.Visible && control.Enabled)
            .Where(control => _tabs?.SelectedTab == null || IsInSelectedTab(control))
            .Take(40)
            .Select((control, index) => new VisibleControlSummaryEntry(
                index + 1,
                control,
                GetControlVoiceLabel(control)))
            .ToList();

        _visibleControlsSummary = visibleControls;

        if (visibleControls.Count == 0)
            return $"Visible controls for {activeTabName}:\n\nNo interactive controls were found.";

        return $"Visible controls for {activeTabName}:\n\n" + string.Join(
            Environment.NewLine,
            visibleControls.Select(entry => $"{entry.Number}. {entry.Label}"));
    }

    private bool IsInSelectedTab(Control control)
    {
        var selectedTab = _tabs?.SelectedTab;
        if (selectedTab == null)
            return true;

        return IsDescendantOf(control, selectedTab);
    }

    private static bool IsDescendantOf(Control control, Control ancestor)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    private static IEnumerable<Control> EnumerateVisibleInteractiveControls(Control root)
    {
        if (!root.Visible || !root.Enabled)
            yield break;

        if (IsVisibleInteractiveControl(root))
            yield return root;

        foreach (Control child in root.Controls)
        {
            foreach (var descendant in EnumerateVisibleInteractiveControls(child))
                yield return descendant;
        }
    }

    private static bool IsVisibleInteractiveControl(Control control) =>
        control is Button or TextBoxBase or ComboBox or ListBox or TabPage or CheckBox or RadioButton or LinkLabel;

    private sealed record VisibleControlSummaryEntry(int Number, Control Control, string Label);

    private static Control? FindFocusedControl(Control root)
    {
        if (root.Focused || root.ContainsFocus)
            return root;

        foreach (Control child in root.Controls)
        {
            var focused = FindFocusedControl(child);
            if (focused != null)
                return focused;
        }

        return null;
    }

    private void SelectTab(string text)
    {
        foreach (TabPage page in _tabs.TabPages)
        {
            if (!string.Equals(page.Text, text, StringComparison.OrdinalIgnoreCase))
                continue;

            _tabs.SelectedTab = page;
            UpdateVisibleControlsOverlay();
            return;
        }
    }

    private void SelectAdjacentTab(int offset)
    {
        if (_tabs.TabPages.Count == 0)
            return;

        var currentIndex = _tabs.SelectedIndex;
        if (currentIndex < 0)
            currentIndex = 0;

        var nextIndex = (currentIndex + offset) % _tabs.TabPages.Count;
        if (nextIndex < 0)
            nextIndex += _tabs.TabPages.Count;

        _tabs.SelectedIndex = nextIndex;
        UpdateVisibleControlsOverlay();
    }

    private void RefreshDictationPanel()
    {
        if (_dictationStatusLabel == null || _startDictationButton == null || _dictationHistoryList == null || _dictationLastHeardLabel == null || _dictationSpeechCueLabel == null)
            return;

        if (!_dictationActive)
        {
            _dictationStatusLabel.Text = "Dictation is stopped.";
            _dictationSpeechCueLabel.Text = "Speech cue: dictation is stopped.";
        }
        else if (!_voiceCommandService.IsListening)
        {
            _dictationStatusLabel.Text = "Dictation is active but the microphone listener is stopped.";
            _dictationSpeechCueLabel.Text = "Speech cue: microphone listener is stopped.";
        }
        else if (_dictationTextBox.TextLength > 0)
        {
            _dictationStatusLabel.Text = $"Dictation is active{FormatActiveDictationCasingSuffix()}. Captured {_dictationTextBox.Text.Length} characters.";
            _dictationSpeechCueLabel.Text = _voiceCommandService.IsSpeechActive
                ? "Speech cue: Hearing dictation..."
                : _dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
                    ? $"Speech cue: Heard {_dictationLastTranscriptText}"
                    : "Speech cue: dictation is waiting for speech.";
        }
        else if (_dictationLastTranscriptUtc.HasValue)
        {
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}{FormatActiveDictationCasingSuffix()}.";
            _dictationSpeechCueLabel.Text = _voiceCommandService.IsSpeechActive
                ? "Speech cue: Hearing dictation..."
                : $"Speech cue: Heard {_dictationLastTranscriptText}";
        }
        else if (_dictationStartedUtc.HasValue && DateTime.UtcNow - _dictationStartedUtc.Value > TimeSpan.FromSeconds(6))
        {
            _dictationStatusLabel.Text = "No speech detected yet. Check microphone permission or speak closer to the mic.";
            _dictationSpeechCueLabel.Text = "Speech cue: waiting for speech.";
        }
        else
        {
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}{FormatActiveDictationCasingSuffix()}.";
            _dictationSpeechCueLabel.Text = BuildLocalDictationSpeechCueText();
        }

        _startDictationButton.Enabled = !_dictationActive;
        _stopDictationButton.Enabled = _dictationActive;
        _copyDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _readDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _stopDictationReadbackButton.Enabled = true;
        _pasteDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _clearDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _dictationLastHeardLabel.Text = _dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
            ? FormatLastHeardLabel(_dictationLastTranscriptText)
            : "Last heard: nothing yet.";
    }

    private string FormatActiveDictationCasingSuffix() =>
        _dictationCasingMode == DictationCasingMode.Default
            ? string.Empty
            : $" ({FormatDictationCasingMode(_dictationCasingMode)})";

    private void RefreshBrowserPanel()
    {
        if (_browserStatusLabel == null)
            return;

        var value = _browserInputText?.Text?.Trim();
        _browserStatusLabel.Text = string.IsNullOrWhiteSpace(value)
            ? "Browser target not opened yet."
            : $"Ready to open: {value}";
        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        if (runtimeIsFresh)
        {
            _browserVoiceCueLabel.Text = runtimeSnapshot!.ServiceDictationActive
                ? BuildRuntimeDictationSpeechCueText(runtimeSnapshot)
                : BuildRuntimeSessionSpeechCueText(runtimeSnapshot);
            _browserLastHeardLabel.Text = FormatRuntimeLastHeardLabel(runtimeSnapshot);
            _browserLastActionLabel.Text = FormatRuntimeLastActionLabel(runtimeSnapshot);
        }
        else
        {
            _browserVoiceCueLabel.Text = BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _browserLastHeardLabel.Text = string.IsNullOrWhiteSpace(_lastHeardTranscriptText)
                ? "Last heard: nothing yet."
                : FormatLastHeardLabel(_lastHeardTranscriptText, _lastHeardTranscriptConfidence);
        _browserLastActionLabel.Text = _lastLocalBrowserActionLabel;
        }
        _openBrowserButton.Enabled = !string.IsNullOrWhiteSpace(value);
        _searchBrowserButton.Enabled = !string.IsNullOrWhiteSpace(value);
        _copyBrowserTargetButton.Enabled = !string.IsNullOrWhiteSpace(value);
        _browserAddressTextButton.Enabled = !string.IsNullOrWhiteSpace(_browserAddressTextInput?.Text?.Trim());
        _browserFindTextButton.Enabled = !string.IsNullOrWhiteSpace(_browserFindTextInput?.Text?.Trim());
    }

    private void RefreshSystemPanel()
    {
        if (_systemStatusLabel == null)
            return;

        var selectedSystemButton = FindFocusedSystemButton();
        _systemSelectedActionLabel.Text = selectedSystemButton == null
            ? "Selected action: none."
            : $"Selected action: {selectedSystemButton.Text}.";

        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        if (runtimeIsFresh)
        {
            _systemVoiceCueLabel.Text = runtimeSnapshot!.ServiceDictationActive
                ? BuildRuntimeDictationSpeechCueText(runtimeSnapshot)
                : BuildRuntimeSessionSpeechCueText(runtimeSnapshot);
            _systemLastHeardLabel.Text = FormatRuntimeLastHeardLabel(runtimeSnapshot);
            _systemLastActionLabel.Text = FormatRuntimeLastActionLabel(runtimeSnapshot);
        }
        else
        {
            _systemVoiceCueLabel.Text = BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _systemLastHeardLabel.Text = string.IsNullOrWhiteSpace(_lastHeardTranscriptText)
                ? "Last heard: nothing yet."
                : FormatLastHeardLabel(_lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _systemLastActionLabel.Text = _lastLocalSystemActionLabel;
        }

        var hasPendingWindowSwitch = _pendingWindowSwitchResolution?.IsAmbiguous == true;
        _systemWindowChoiceList.Enabled = hasPendingWindowSwitch;
        _systemConfirmWindowChoiceButton.Enabled = hasPendingWindowSwitch && _systemWindowChoiceList.SelectedItem is WindowSwitchListItem;
        _systemClearWindowChoicesButton.Enabled = hasPendingWindowSwitch;
        _systemSwitchWindowButton.Enabled = !string.IsNullOrWhiteSpace(_systemSwitchWindowText.Text);
    }

    private void RefreshFileSearchPanel()
    {
        if (_fileSearchStatusLabel == null)
            return;

        var query = _fileSearchQueryText?.Text?.Trim();
        var resultCount = _fileSearchResultsList?.Items.Count ?? 0;
        _fileSearchStatusLabel.Text = string.IsNullOrWhiteSpace(query)
            ? "No file search run yet."
            : resultCount > 0
                ? $"Found {resultCount} result{(resultCount == 1 ? string.Empty : "s")} for: {query}"
                : $"Ready to search for: {query}";
        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        if (runtimeIsFresh)
        {
            _fileSearchVoiceCueLabel.Text = runtimeSnapshot!.ServiceDictationActive
                ? BuildRuntimeDictationSpeechCueText(runtimeSnapshot)
                : BuildRuntimeSessionSpeechCueText(runtimeSnapshot);
            _fileSearchLastHeardLabel.Text = FormatRuntimeLastHeardLabel(runtimeSnapshot);
            _fileSearchLastActionLabel.Text = FormatRuntimeLastActionLabel(runtimeSnapshot);
        }
        else
        {
            _fileSearchVoiceCueLabel.Text = BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _fileSearchLastHeardLabel.Text = string.IsNullOrWhiteSpace(_lastHeardTranscriptText)
                ? "Last heard: nothing yet."
                : FormatLastHeardLabel(_lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _fileSearchLastActionLabel.Text = _lastLocalFileSearchActionLabel;
        }
        _searchFilesButton.Enabled = !string.IsNullOrWhiteSpace(query);
        var hasResults = resultCount > 0;
        if (_fileSearchResultNumber != null)
        {
            _fileSearchResultNumber.Enabled = hasResults;
            _fileSearchResultNumber.Maximum = Math.Max(1, resultCount);
            if (_fileSearchResultNumber.Value > _fileSearchResultNumber.Maximum)
            {
                _fileSearchResultNumber.Value = _fileSearchResultNumber.Maximum;
            }
        }

        _selectFileResultButton.Enabled = hasResults;
        _openFileResultButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
        _openFileFolderButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
        _openFileResultByNumberButton.Enabled = hasResults;
        _openFileFolderByNumberButton.Enabled = hasResults;
        _fileSearchSelectionLabel.Text = _fileSearchResultsList?.SelectedItem is FileSearchResult selected
            ? FileSearchService.DescribeResult(selected, _fileSearchResultsList.Items.Count)
            : "Selected result: none.";
    }

    private Button? FindFocusedSystemButton()
    {
        var focused = FindFocusedControl(this);
        if (focused is not Button button)
            return null;

        var systemTab = _tabs?.TabPages["System"];
        if (systemTab == null)
            return null;

        return IsDescendantOf(button, systemTab) ? button : null;
    }

    private void OnSessionTick()
    {
        if (_updatingUi)
            return;

        UpdateLiveSpeechCueFromActivity();
        RefreshSessionPanel();
        RefreshDictationPanel();
        if (_dictationActive || IsWakeOverlaySessionActive(_session.State))
        {
            var phase = _dictationActive
                ? "Dictation"
                : FormatOverlayPhase(_session.State.ToString());
            var captionText = _dictationActive
                ? BuildLocalDictationOverlayCaptionText()
                : BuildLocalOverlayCaptionText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc);
            var readout = _dictationActive
                ? BuildLocalDictationReadout()
                : BuildLocalOverlayReadout();
            ShowWakeOverlay(
                readout,
                phase,
                GetLocalTranscriptHistory(),
                BuildLocalOverlayActivityLevel(),
                BuildLocalActivityTextForWakeOverlay(),
                _voiceCommandService.IsSpeechActive,
                captionText,
                FormatLocalWakeCandidateReadout(),
                BuildWakeOverlayAuthorityText());
        }
        UpdateVisibleControlsOverlay();
        RefreshSystemPanel();
        RefreshFileSearchPanel();
    }

    private void OnVisibleControlsRefreshTick()
    {
        if (_updatingUi || _visibleControlsOverlay == null || !_visibleControlsOverlay.Visible)
        {
            if (_visibleControlsRefreshTimer.Enabled && (_visibleControlsOverlay == null || !_visibleControlsOverlay.Visible))
                _visibleControlsRefreshTimer.Stop();
            return;
        }

        UpdateVisibleControlsOverlay();
    }

    private void SaveProfile()
    {
        if (_updatingUi)
            return;

        if (!ValidateCallsign(_callsignText.Text, out var normalizedCallsign))
        {
            UpdateStatus("Callsign must be 2-32 chars and contain only letters, numbers, spaces, underscores, or hyphens.");
            return;
        }

        if (_activeProfile != null && _callsignText.ReadOnly)
        {
            _activeProfile.Callsign = normalizedCallsign;
            UpdateProfileMetadata(_activeProfile);
            SaveVoiceState(_activeProfile);
            _profileStore.Save(_activeProfile);
            LoadProfiles();
            var loadedIndex = _profiles.FindIndex(p => string.Equals(p.Callsign, normalizedCallsign, StringComparison.OrdinalIgnoreCase));
            if (loadedIndex >= 0)
            {
                _profilePicker.SelectedIndex = loadedIndex;
                SelectProfile(_profiles[loadedIndex]);
            }
            UpdateStatus($"Account '{normalizedCallsign}' saved. Try: Callsign {normalizedCallsign} open Notepad.");
            return;
        }

        if (_profiles.Any(p => string.Equals(p.Callsign, normalizedCallsign, StringComparison.OrdinalIgnoreCase)))
        {
            UpdateStatus("That callsign already exists. Select it to edit.");
            return;
        }

        var profile = new UserProfile
        {
            Callsign = normalizedCallsign,
            DisplayName = _displayNameText.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(_emailText.Text) ? null : _emailText.Text.Trim(),
            Department = string.IsNullOrWhiteSpace(_departmentText.Text) ? null : _departmentText.Text.Trim(),
            Notes = _notesText.Text
        };

        SaveVoiceState(profile);
        _profileStore.Save(profile);
        LoadProfiles();
        var createdProfileIndex = _profiles.FindIndex(p => string.Equals(p.Callsign, profile.Callsign, StringComparison.OrdinalIgnoreCase));
        if (createdProfileIndex >= 0)
        {
            _profilePicker.SelectedIndex = createdProfileIndex;
            SelectProfile(_profiles[createdProfileIndex]);
        }
        UpdateStatus($"Account '{profile.Callsign}' created. Try: Callsign {profile.Callsign} open Notepad.");
    }

    private void DeleteProfile()
    {
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            UpdateStatus("Choose an account to delete.");
            return;
        }

        var callsign = _activeProfile.Callsign;
        var result = MessageBox.Show(
            $"Delete '{callsign}' and all of its settings?",
            "Delete account",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _profileStore.Delete(callsign);
        _activeProfile = null;
        _session.Reset();
        LoadProfiles();
        UpdateStatus($"Deleted account '{callsign}'.");
    }

    private void RecordVoiceSample()
    {
        throw new NotSupportedException("Use press-and-hold recording events.");
    }

    private void TrainVoiceIdentity()
    {
        StopVoiceSampleRecording();
        if ((_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
            && !string.IsNullOrWhiteSpace(_callsignText.Text))
        {
            SaveProfile();
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        OpenVoiceIdentityTraining(profile);
    }

    private async Task<bool> ActivateVoiceForProfileAsync(UserProfile profile, bool startingListener)
    {
        var settings = profile.Settings;
        settings.VoiceSamplesRequired = Math.Max(3, settings.VoiceSamplesRequired);
        var samplePaths = VoiceBiometricVerificationService.GetEnrollmentSamplePaths(_profileStore, profile);
        settings.VoiceSamplesRecorded = samplePaths.Count;
        if (settings.VoiceSamplesRecorded < settings.VoiceSamplesRequired)
        {
            UpdateStatus($"Record {settings.VoiceSamplesRequired - settings.VoiceSamplesRecorded} more voice sample(s) before activation.");
            return false;
        }

        if (samplePaths.Count < 3)
        {
            UpdateStatus("Collect 3 fresh voice samples in Train Voice Identity before activation.");
            return false;
        }

        SetVoiceActivationBusy(true, "Enrolling voice identity with pyannote...");
        try
        {
            var enrollment = await Task.Run(() => _voiceBiometricVerificationService.EnrollFreshSamples(_profileStore, profile, samplePaths));
            if (!enrollment.Accepted)
            {
                settings.VoiceEnrollmentStatus = VoiceBiometricVerificationService.IsSampleProofRejectReason(enrollment.RejectReason)
                    ? enrollment.Message
                    : "pyannote setup required";
                settings.VoiceEnrolledUtc = null;
                SaveVoiceState(profile);
                _profileStore.Save(profile);
                RefreshAllPanels();
                UpdateStatus($"{enrollment.Message} Open Train Voice Identity and use Repair Identity Runtime if prompted.");
                return false;
            }

            settings.VoiceEnrollmentStatus = "Activated";
            settings.VoiceEnrolledUtc = DateTime.UtcNow;
            settings.VoiceSamplesRecorded = samplePaths.Count;
            var wakeCalibrationPaths = GetWakeCalibrationSamplePaths(profile);
            var wakeScoreSource = wakeCalibrationPaths.Count > 0 ? wakeCalibrationPaths : samplePaths;
            var wakeScores = new List<(string Path, double Score)>();
            foreach (var samplePath in wakeScoreSource)
            {
                var score = await _voiceCommandService.TryScoreWakeWordSampleAsync(samplePath, CancellationToken.None);
                if (score.HasValue)
                    wakeScores.Add((samplePath, score.Value));
            }

            if (wakeScores.Count > 0)
            {
                var bestWakeSample = wakeScores.OrderByDescending(entry => entry.Score).First();
                if (VoiceCommandService.ComputeCalibratedWakeThreshold(bestWakeSample.Score).HasValue)
                {
                    VoiceCommandService.ApplyWakeCalibration(
                        settings,
                        bestWakeSample.Score,
                        wakeScores.Count,
                        Path.GetFileName(bestWakeSample.Path));
                }
            }

            SaveVoiceState(profile);
            _profileStore.Save(profile);
            RefreshAllPanels();
            UpdateStatus(startingListener
                ? $"Voice activated for '{profile.Callsign}' with pyannote identity ({enrollment.SamplesEnrolled} fresh sample(s)). The background service will use this profile for always-on listening."
                : $"Voice activated for '{profile.Callsign}' with pyannote identity ({enrollment.SamplesEnrolled} sample(s)).");
            return true;
        }
        catch (Exception ex)
        {
            settings.VoiceEnrollmentStatus = "pyannote setup required";
            settings.VoiceEnrolledUtc = null;
            SaveVoiceState(profile);
            _profileStore.Save(profile);
            RefreshAllPanels();
            UpdateStatus($"Voice identity enrollment failed: {ex.Message}");
            return false;
        }
        finally
        {
            SetVoiceActivationBusy(false);
            RefreshAllPanels();
        }
    }

    private void ResetVoiceIdentity()
    {
        StopVoiceSampleRecording(commit: false);
        if (!EnsureActiveProfile(out var profile))
            return;

        profile.Settings.VoiceSamplesRecorded = 0;
        profile.Settings.VoiceEnrollmentStatus = "Not activated";
        profile.Settings.VoiceEnrolledUtc = null;
        VoiceBiometricVerificationService.ResetEnrollmentArtifacts(_profileStore, profile);
        StopVoiceListening();
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus("Voice activation reset.");
    }

    private void WakeSession()
    {
        _session.DetectWakeWord(AlphaSessionStateMachine.ManualUiSource);
        ShowWakeOverlay(activityLevel: BuildLocalOverlayActivityLevel(), activityText: BuildLocalActivityTextForWakeOverlay(), speechActive: _voiceCommandService.IsSpeechActive, authorityText: BuildWakeOverlayAuthorityText());
        RefreshSessionPanel();
        UpdateStatus(_session.StatusMessage);
    }

    private async void StartVoiceListening()
    {
        StopVoiceSampleRecording();
        _dictationActive = false;
        RefreshDictationPanel();
        if (_voiceCommandService.IsListening)
        {
            UpdateStatus("Callsign is already listening.");
            RecordVoiceControlAudit(
                "voice_listening_start",
                "succeeded",
                "already_listening",
                "listener_already_running",
                true,
                "Voice listening start was shown in the visible status surface.");
            return;
        }

        if ((_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
            && !string.IsNullOrWhiteSpace(_callsignText.Text))
        {
            SaveProfile();
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        if (!IsVoiceEnrolled(profile.Settings) && !await ActivateVoiceForProfileAsync(profile, startingListener: true))
            return;

        _spokenCallsignText.Text = string.Empty;
        _spokenCommandText.Text = string.Empty;
        _appNameText.Text = string.Empty;
        _session.Reset();
        RefreshSessionPanel();

        var runtimeStartResult = TryStartInstalledUserRuntime(out var runtimeMessage);
        if (runtimeStartResult != InstalledUserRuntimeStartResult.Unavailable)
        {
            _usingLocalPreviewListener = false;
            UpdateListeningPanel();
            UpdateStatus(runtimeMessage);
            RecordVoiceControlAudit(
                "voice_listening_start",
                "succeeded",
                runtimeStartResult.ToString(),
                runtimeMessage,
                true,
                "Voice listening start was shown in the visible status surface.");
            return;
        }

        _usingLocalPreviewListener = true;
        _voiceCommandService.Start(
            profile.Settings.LanguageCode,
            profile.Settings.WakeWord,
            profile.Callsign,
            profile.Settings.VoiceWakeThreshold,
            profile.Settings.VoiceWakeSensitivity,
            profile.Settings.VoiceWakeDiagnosticsEnabled,
            MicrophoneAudioSettings.From(profile.Settings),
            profile.Settings.VoiceSilenceMilliseconds);
        UpdateListeningPanel();
        if (_voiceCommandService.IsListening)
        {
            var warning = string.IsNullOrWhiteSpace(_voiceCommandService.LastStartupWarning)
                ? string.Empty
                : $" {_voiceCommandService.LastStartupWarning}";
            UpdateStatus($"Listening with {_voiceCommandService.CurrentModeDescription} Say 'Callsign', your callsign, and the app you want to launch.{warning}");
            RecordVoiceControlAudit(
                "voice_listening_start",
                "succeeded",
                "local_preview_listener",
                _voiceCommandService.CurrentModeDescription,
                true,
                "Voice listening start was shown in the visible status surface.");
        }
        else
        {
            UpdateStatus("Unable to start voice listening.");
            RecordVoiceControlAudit(
                "voice_listening_start",
                "failed",
                "local_preview_listener",
                "listener_not_started",
                false,
                "Voice listening start failure was shown in the visible status surface.");
        }
    }

    private void TryStartListenerForActiveProfile()
    {
        if (_activeProfile == null)
            return;

        if (!IsVoiceEnrolled(_activeProfile.Settings))
            return;

        UpdateStatus($"Background service will listen using '{_activeProfile.Callsign}'. Use the UI only for local preview and configuration.");
    }

    private void StopVoiceListening()
    {
        _dictationActive = false;
        RefreshDictationPanel();

        if (!_usingLocalPreviewListener)
        {
            RuntimeControlFiles.RequestStopUserRuntime();
            _runtimeStopRequestedUtc = DateTime.UtcNow;
            UpdateListeningPanel();
            UpdateStatus("Requested background user runtime stop. The service monitor will update after the runtime exits.");
            RecordVoiceControlAudit(
                "voice_listening_stop",
                "succeeded",
                "background_user_runtime",
                "runtime_stop_requested",
                true,
                "Voice listening stop was shown in the visible status surface.");
            return;
        }

        if (!_voiceCommandService.IsListening)
        {
            UpdateListeningPanel();
            UpdateStatus("Voice listener is already stopped.");
            RecordVoiceControlAudit(
                "voice_listening_stop",
                "succeeded",
                "already_stopped",
                "listener_already_stopped",
                true,
                "Voice listening stop was shown in the visible status surface.");
            return;
        }

        _voiceCommandService.Stop();
        _usingLocalPreviewListener = false;
        UpdateListeningPanel();
        UpdateStatus("Voice listener stopped.");
        RecordVoiceControlAudit(
            "voice_listening_stop",
            "succeeded",
            "local_preview_listener",
            "listener_stopped",
            true,
            "Voice listening stop was shown in the visible status surface.");
    }

    private enum InstalledUserRuntimeStartResult
    {
        Started,
        AlreadyRunning,
        Unavailable
    }

    private InstalledUserRuntimeStartResult TryStartInstalledUserRuntime(out string message)
    {
        var runtimeExe = Path.Combine(GetInstalledAppDirectory(), "Callsign.Service.exe");
        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runningProcessCount = 0;
        try
        {
            runningProcessCount = Process.GetProcessesByName("Callsign.Service").Length;
        }
        catch
        {
            runningProcessCount = 0;
        }

        var decision = RuntimeOwnershipService.EvaluateStart(File.Exists(runtimeExe), runtimeSnapshot, runningProcessCount);
        if (decision.State == UserRuntimeOwnershipState.Started)
        {
            try
            {
                RuntimeControlFiles.ClearStopUserRuntimeRequest();
                Process.Start(new ProcessStartInfo
                {
                    FileName = runtimeExe,
                    Arguments = "--user-runtime --service-installed",
                    WorkingDirectory = GetInstalledAppDirectory(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                message = decision.Message;
                return InstalledUserRuntimeStartResult.Started;
            }
            catch (Exception ex)
            {
                message = $"Unable to start installed user runtime; using local preview listener. {ex.Message}";
                return InstalledUserRuntimeStartResult.Unavailable;
            }
        }

        message = decision.Message;
        return decision.State switch
        {
            UserRuntimeOwnershipState.AlreadyRunningAuthoritative => InstalledUserRuntimeStartResult.AlreadyRunning,
            UserRuntimeOwnershipState.AlreadyRunningNonAuthoritative => InstalledUserRuntimeStartResult.AlreadyRunning,
            _ => InstalledUserRuntimeStartResult.Unavailable
        };
    }

    private void RecordSampleButtonMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        StartVoiceSampleRecording();
    }

    private void RecordSampleButtonMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        StopVoiceSampleRecording();
    }

    private void RecordSampleButtonMouseLeave(object? sender, EventArgs e)
    {
        if (_voiceSampleCapture.IsRecording)
            _recordSampleButton.Capture = true;
    }

    private void StartVoiceSampleRecording()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        if (_voiceSampleCapture.IsRecording)
            return;

        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        if (!TryGetNextVoiceSamplePath(profile, out var samplePath))
        {
            UpdateStatus("Collect 3 fresh voice samples or reset the identity before recording more.");
            return;
        }

        try
        {
            _voiceSampleCapture.Start(samplePath, MicrophoneAudioSettings.From(profile.Settings));
            _recordSampleButton.Capture = true;
            UpdateRecordButtonAppearance();
            _voiceRecordingStateLabel.Text = "Recording now. Keep holding the red button while you speak.";
            _voicePlaybackStateLabel.Text = "Playback is available after you release the button.";
            UpdateStatus("Recording sample. Keep holding the red button while you speak.");
        }
        catch (Exception ex)
        {
            _recordSampleButton.Capture = false;
            UpdateRecordButtonAppearance();
            UpdateStatus($"Unable to start recording: {ex.Message}");
        }
    }

    private void StopVoiceSampleRecording(bool commit = true)
    {
        if (!_voiceSampleCapture.IsRecording)
        {
            UpdateRecordButtonAppearance();
            return;
        }

        var profile = _activeProfile;
        var samplePath = profile != null && TryGetCurrentVoiceSamplePath(profile, out var resolvedSamplePath)
            ? resolvedSamplePath
            : null;

        try
        {
            _voiceSampleCapture.Stop();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to stop recording cleanly: {ex.Message}");
        }
        finally
        {
            _recordSampleButton.Capture = false;
            UpdateRecordButtonAppearance();
        }

        if (!commit || profile == null || string.IsNullOrWhiteSpace(samplePath))
        {
            if (!commit && !string.IsNullOrWhiteSpace(samplePath) && File.Exists(samplePath))
            {
                try
                {
                    File.Delete(samplePath);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }

            RefreshVoicePanel();
            return;
        }

        try
        {
            var fileInfo = new FileInfo(samplePath);
            if (!fileInfo.Exists || fileInfo.Length <= 64)
            {
                if (fileInfo.Exists)
                    fileInfo.Delete();

                _voiceRecordingStateLabel.Text = "Recording was too short. Hold the button a little longer next time.";
                _voicePlaybackStateLabel.Text = "No sample available for playback.";
                UpdateStatus("Recording was too short to save.");
                RefreshVoicePanel();
                return;
            }

            CopyVoiceSampleToLatest(samplePath);
            profile.Settings.VoiceSamplesRequired = Math.Max(3, profile.Settings.VoiceSamplesRequired);
            profile.Settings.VoiceSamplesRecorded = GetRecordedVoiceSampleCount(profile);
            profile.Settings.VoiceEnrollmentStatus = profile.Settings.VoiceSamplesRecorded >= profile.Settings.VoiceSamplesRequired
                ? "Ready to activate"
                : $"Collecting sample {profile.Settings.VoiceSamplesRecorded} of {profile.Settings.VoiceSamplesRequired}";
            profile.Settings.VoiceEnrolledUtc = null;
            SaveVoiceState(profile);
            _profileStore.Save(profile);
            RefreshAllPanels();

            var remaining = Math.Max(0, profile.Settings.VoiceSamplesRequired - profile.Settings.VoiceSamplesRecorded);
            UpdateStatus(remaining == 0
                ? "Sample saved. You can play it back or activate voice now."
                : $"Sample saved. {remaining} more sample(s) before activation.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Recording was captured, but profile save failed: {ex.Message}");
        }
    }

    private void PlayLatestVoiceSample()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        if (!TryGetLatestVoiceSamplePath(profile, out var samplePath))
        {
            UpdateStatus("Save the account with a callsign before playing voice samples.");
            return;
        }

        if (!File.Exists(samplePath))
        {
            var recordedSamples = GetRecordedVoiceSamplePaths(profile);
            if (recordedSamples.Count == 0)
            {
                UpdateStatus("Record a sample before playing it back.");
                return;
            }

            samplePath = recordedSamples[^1];
        }

        try
        {
            using var player = new SoundPlayer(samplePath);
            player.Play();
            UpdateStatus($"Playing back {Path.GetFileName(samplePath)}.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to play voice sample: {ex.Message}");
        }
    }

    private async void RehearseVoicePhrase()
    {
        if ((_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
            && !string.IsNullOrWhiteSpace(_callsignText.Text))
        {
            SaveProfile();
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        if (!IsVoiceEnrolled(profile.Settings) && !await ActivateVoiceForProfileAsync(profile, startingListener: false))
            return;

        var phrase = _voicePhraseText.Text.Trim();
        if (string.IsNullOrWhiteSpace(phrase))
        {
            var appName = string.IsNullOrWhiteSpace(_appNameText.Text) ? "Notepad" : _appNameText.Text.Trim();
            phrase = _session.State == AlphaSessionState.WaitingForCommand
                ? appName
                : $"Callsign {profile.Callsign} open {appName}";
            _voicePhraseText.Text = phrase;
        }

        if (string.IsNullOrWhiteSpace(phrase))
        {
            UpdateStatus("Enter a launch test phrase, such as 'Callsign Alpha open Notepad'.");
            return;
        }

        if (_session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
        {
            _session.DetectWakeWord(AlphaSessionStateMachine.ManualUiSource);
            RefreshSessionPanel();
        }

        HandleVoiceTranscript($"[rehearsal] {phrase}", phrase, 1.0f);
    }

    private void VoiceTranscriptReceived(object? sender, VoiceTranscriptEventArgs e)
    {
        if (IsDisposed)
            return;

        RunOnUiThread(() => HandleVoiceTranscript(e.Text, e.Text, e.Confidence));
    }

    private void VoiceWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)
    {
        if (IsDisposed)
            return;

        RunOnUiThread(() =>
        {
            _wakeReliabilityLabel.Text = $"Wake accepted by {e.Result.Engine}.";
            _wakeScoreLabel.Text = $"{e.Result.Score:P0} confidence / {e.Result.Threshold:P0} threshold.";
            _wakeQualityLabel.Text = e.Result.AudioQualityWarnings.Count == 0
                ? "Audio quality looks clean."
                : string.Join(" ", e.Result.AudioQualityWarnings);

            if (!_dictationActive)
            {
                ShowWakeOverlay(activityLevel: BuildLocalOverlayActivityLevel(), activityText: BuildLocalActivityTextForWakeOverlay(), speechActive: _voiceCommandService.IsSpeechActive, wakeStatusText: FormatLocalWakeCandidateReadout(), authorityText: BuildWakeOverlayAuthorityText());
            }

            if (!_dictationActive && _session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
            {
                _session.DetectWakeWord(AlphaSessionStateMachine.AudioWakeDetectorSource);
                RefreshSessionPanel();
            }

            UpdateStatus($"Wake word detected by {e.Result.Engine}; waiting for callsign identity.");
        });
    }

    private static string GetOpenWakeWordSetupHint(string? wakeEngine)
    {
        if (!string.IsNullOrWhiteSpace(wakeEngine)
            && wakeEngine.Contains("openWakeWord", StringComparison.OrdinalIgnoreCase)
            && !wakeEngine.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var modelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Models",
            "callsign.onnx");
        var setupScriptPath = Path.Combine(GetInstalledAppDirectory(), "setupopenwakeword.ps1");
        var modelPresent = File.Exists(modelPath);
        var setupScriptPresent = File.Exists(setupScriptPath);
        var missing = modelPresent
            ? setupScriptPresent ? "runtime or packages" : "repair helper"
            : "bundled wake model";

        return $"Wake detection is not ready. Missing or damaged piece: {missing}. Use Repair Wakeword on the Account tab to restore the installed wake model and runtime.";
    }

    private void VoiceRecognitionError(object? sender, VoiceRecognitionErrorEventArgs e)
    {
        if (IsDisposed)
            return;

        RunOnUiThread(() =>
        {
            HideWakeOverlay();
            if (_voiceCommandService.IsListening)
                StopVoiceListening();
            UpdateListeningPanel();
            UpdateStatus(e.Message);
        });
    }

    private void HandleVoiceTranscript(string displayTranscript, string transcript, float confidence)
    {
        if (IsIgnorableSpeechTranscript(transcript))
            return;

        var acceptsTranscript = _dictationActive || IsWakeOverlaySessionActive(_session.State);

        if (!acceptsTranscript)
        {
            if (IsStopListeningCommand(transcript))
            {
                StopVoiceListening();
                return;
            }

            if (IsCancelCommand(transcript))
            {
                CancelSession();
                return;
            }

            if (EnsureActiveProfile(out var idleProfile))
            {
                var idleWakeWord = string.IsNullOrWhiteSpace(idleProfile.Settings.WakeWord)
                    ? "Callsign"
                    : idleProfile.Settings.WakeWord;
                if (ContainsWakeWord(transcript, idleWakeWord))
                    UpdateStatus("Wake-like transcript heard, but Callsign waits for the wake detector before opening a session.");
            }

            return;
        }

        AppendSessionTranscriptHistory($"[{DateTime.Now:t}] {displayTranscript} ({confidence:P0})");
        _lastHeardTranscriptText = displayTranscript;
        _lastHeardTranscriptConfidence = confidence;
        _lastHeardLabel.Text = FormatLastHeardLabel(displayTranscript, confidence);
        if (_dictationActive || IsWakeOverlaySessionActive(_session.State))
        {
            var phase = _dictationActive
                ? "Dictation"
                : FormatOverlayPhase(_session.State.ToString());
            ShowWakeOverlay(
                _dictationActive ? BuildLocalDictationReadout(transcript) : BuildLocalOverlayReadout(transcript),
                phase,
                GetLocalTranscriptHistory(),
                BuildLocalOverlayActivityLevel(),
                BuildLocalActivityTextForWakeOverlay(),
                _voiceCommandService.IsSpeechActive,
                _dictationActive
                    ? BuildLocalDictationOverlayCaptionText(displayTranscript)
                    : BuildLocalOverlayCaptionText(_session.State, _voiceCommandService.IsSpeechActive, latestTranscript: displayTranscript),
                FormatLocalWakeCandidateReadout(),
                BuildWakeOverlayAuthorityText());
        }

        if (_dictationActive)
        {
            if (IsPauseDictationCommand(transcript))
            {
                PauseDictation();
                return;
            }

            if (IsStopDictationCommand(transcript))
            {
                StopDictation();
                return;
            }

            if (TryHandleDictationVoiceAction(transcript))
                return;

            if (confidence < 0.45f)
            {
                UpdateStatus("Dictation heard speech, but confidence was too low. Try again clearly.");
                return;
            }

            AppendDictationTranscript(displayTranscript);
            return;
        }

        if (IsStopListeningCommand(transcript))
        {
            StopVoiceListening();
            return;
        }

        if (IsCancelCommand(transcript))
        {
            CancelSession();
            return;
        }

        if (confidence < 0.45f)
        {
            UpdateStatus("Heard speech, but confidence was too low. Try again clearly.");
            return;
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        var wakeWord = string.IsNullOrWhiteSpace(profile.Settings.WakeWord)
            ? "Callsign"
            : profile.Settings.WakeWord;

        if (ContainsWakeWord(transcript, wakeWord)
            && _session.State is AlphaSessionState.Idle or AlphaSessionState.Completed)
        {
            UpdateStatus("Wake-like transcript heard, but Callsign now waits for the wake detector before opening a session.");
        }

        var normalizedCommand = NormalizeLaunchCommand(ExtractCommandFromTranscript(transcript, wakeWord, profile.Callsign));
        var hasCommandHint = !string.IsNullOrWhiteSpace(normalizedCommand);
        if (TryHandlePendingAppCandidateVoiceSelection(transcript))
            return;
        if (TryHandlePendingWindowSwitchVoiceSelection(transcript))
            return;

        if (_session.State == AlphaSessionState.WaitingForIdentity
            && ContainsSpeechPhrase(transcript, profile.Callsign))
        {
            _spokenCallsignText.Text = profile.Callsign;
            VerifyIdentity();

            if (hasCommandHint && _session.State == AlphaSessionState.WaitingForCommand)
            {
                _spokenCommandText.Text = normalizedCommand;
            }
        }

        if (_session.State == AlphaSessionState.WaitingForCommand)
        {
            var uiNavigationIntent = AlphaVoiceIntentParser.ParseVerifiedTranscript(transcript, wakeWord, profile.Callsign);
            // TryExecuteVerifiedIntent handles uiNavigationIntent.Kind == AlphaVoiceIntentKind.SystemControl,
            // uiNavigationIntent.Kind == AlphaVoiceIntentKind.FileSearch,
            // uiNavigationIntent.Kind == AlphaVoiceIntentKind.Dictation,
            // uiNavigationIntent.Kind == AlphaVoiceIntentKind.Browser,
            // uiNavigationIntent.Kind == AlphaVoiceIntentKind.UiNavigation,
            // uiNavigationIntent.Kind == AlphaVoiceIntentKind.UiAction,
            // and uiNavigationIntent.Kind == AlphaVoiceIntentKind.ExtensionCommand.
            if (TryExecuteVerifiedIntent(uiNavigationIntent))
                return;

            if (!string.IsNullOrWhiteSpace(normalizedCommand))
            {
                _spokenCommandText.Text = normalizedCommand;
                var appName = InferAppName(normalizedCommand);
                if (!string.IsNullOrWhiteSpace(appName))
                {
                    _appNameText.Text = appName;
                    _sessionResultLabel.Text = $"Parsed Start menu target: {appName}";
                }

                CaptureCommand();
            }
            else
            {
                var normalizedTranscript = NormalizeSpeechText(transcript);
                if (normalizedTranscript.Contains(" open ", StringComparison.OrdinalIgnoreCase)
                    || normalizedTranscript.Contains(" launch ", StringComparison.OrdinalIgnoreCase)
                    || normalizedTranscript.Contains(" start ", StringComparison.OrdinalIgnoreCase)
                    || normalizedTranscript.Contains(" run ", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateStatus("Identity verified, but I could not parse a clear app name. Try: 'Callsign <callsign> open Notepad' or 'Callsign <callsign> launch Notepad'.");
                }
                else
                {
                    UpdateStatus("Identity verified. Say an app command like 'open Notepad' or 'launch Notepad'.");
                }
            }
        }

        if (_session.State == AlphaSessionState.ReadyToLaunch
            && _pendingAppResolution?.IsAmbiguous != true
            && !string.IsNullOrWhiteSpace(_appNameText.Text))
        {
            _sessionResultLabel.Text = $"Action intent: launch '{_appNameText.Text.Trim()}' through Start menu search.";
            LaunchAppFromStartMenu();
        }
    }

    private bool TryExecuteVerifiedIntent(
        AlphaVoiceIntent intent,
        int shortcutDepth = 0,
        HashSet<string>? activeExtensionCommands = null)
    {
        if (!intent.ContainsCallsign)
            return false;

        switch (intent.Kind)
        {
            case AlphaVoiceIntentKind.UiNavigation:
                ApplyRequestedUiMode(intent.Target);
                _sessionResultLabel.Text = $"Voice navigation: open {intent.Target} tab.";
                RefreshSessionPanel();
                UpdateStatus($"Voice navigation moved to the {intent.Target.ToLowerInvariant()} tab.");
                return true;
            case AlphaVoiceIntentKind.UiAction:
                if (TryExecuteRequestedUiAction(intent.Target))
                {
                    _sessionResultLabel.Text = $"Voice action: {intent.Target}.";
                    RefreshSessionPanel();
                    UpdateStatus($"Voice action executed: {intent.Target.Replace("ui-", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("-", " ", StringComparison.OrdinalIgnoreCase)}.");
                    return true;
                }

                return false;
            case AlphaVoiceIntentKind.ExtensionCommand:
                if (TryExecuteExtensionCommand(intent, out var extensionResult, shortcutDepth, activeExtensionCommands))
                {
                    _sessionResultLabel.Text = $"Pack command: {intent.PackId}/{intent.Target}.";
                    RefreshSessionPanel();
                    UpdateStatus(extensionResult.Message);
                    return true;
                }

                return false;
            case AlphaVoiceIntentKind.Browser:
                ExecuteBrowserIntent(intent);
                return true;
            case AlphaVoiceIntentKind.SystemControl:
                ExecuteSystemControlIntent(intent);
                return true;
            case AlphaVoiceIntentKind.FileSearch:
                ExecuteFileSearchIntent(intent);
                return true;
            case AlphaVoiceIntentKind.Dictation:
                ExecuteDictationIntent(intent);
                return true;
            case AlphaVoiceIntentKind.StartMenuLaunch:
                _spokenCommandText.Text = intent.NormalizedCommand;
                _appNameText.Text = intent.Target;
                _sessionResultLabel.Text = $"Parsed Start menu target: {intent.Target}";
                CaptureCommand();
                if (_session.State == AlphaSessionState.ReadyToLaunch
                    && _pendingAppResolution?.IsAmbiguous != true
                    && !string.IsNullOrWhiteSpace(_appNameText.Text))
                {
                    LaunchAppFromStartMenu();
                }

                return true;
            default:
                return false;
        }
    }

    private void ExecuteBrowserIntent(AlphaVoiceIntent intent)
    {
        ApplyRequestedUiMode("Browser");
        _spokenCommandText.Text = intent.NormalizedCommand;

        if (intent.Target.StartsWith("browser-", StringComparison.OrdinalIgnoreCase))
        {
            ExecuteBrowserAction(intent.Target, FormatBrowserIntentStatus(intent.Target));
            _sessionResultLabel.Text = $"Browser action: {intent.Target}.";
            RefreshSessionPanel();
            return;
        }

        EnsureActiveProfile(out var profileForAudit);
        _browserInputText.Text = intent.Target;
        if (_browserLaunchService.TryOpen(intent.Target, out var message, out var targetUri, browserTarget: intent.BrowserTarget))
        {
            _browserStatusLabel.Text = $"Opened: {targetUri}";
            _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel("browser-open", message, succeeded: true);
            _sessionResultLabel.Text = $"Browser target: {targetUri}.";
            RefreshBrowserPanel();
            RefreshSessionPanel();
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "browser_open",
                    status: "succeeded",
                    out _,
                    commandFamily: "browser",
                    actionTarget: targetUri?.ToString(),
                    details: intent.NormalizedCommand,
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "Browser open request was shown in the visible Browser tab status.");
            }
            UpdateStatus(message);
            return;
        }

        _browserStatusLabel.Text = "Browser target failed.";
        _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel("browser-open", message, succeeded: false);
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "browser_open",
                status: "failed",
                out _,
                commandFamily: "browser",
                actionTarget: intent.NormalizedCommand,
                details: message,
                success: false,
                verificationMethod: "visible_status",
                verificationSummary: "Browser open failure was shown in the visible Browser tab status.");
        }
        RefreshBrowserPanel();
        RefreshSessionPanel();
        UpdateStatus(message);
    }

    private void ExecuteSystemControlIntent(AlphaVoiceIntent intent)
    {
        if (!TryAuthorizeBuiltInIntent(intent, out var policyMessage))
        {
            _sessionResultLabel.Text = policyMessage;
            RefreshSessionPanel();
            UpdateStatus(policyMessage);
            return;
        }

        ApplyRequestedUiMode("System");
        _spokenCommandText.Text = intent.NormalizedCommand;
        if (string.Equals(intent.Target, "system-undo", StringComparison.OrdinalIgnoreCase)
            && _mouseGridOverlay?.Visible == true)
        {
            UndoMouseGrid();
            _sessionResultLabel.Text = "Mouse grid action: undo.";
            RefreshSessionPanel();
            return;
        }

        ExecuteSystemAction(intent.Target, FormatBuiltInIntentStatus(intent.Target, "System action"));
        _sessionResultLabel.Text = string.IsNullOrWhiteSpace(_systemStatusLabel?.Text)
            ? $"System action: {intent.Target}."
            : _systemStatusLabel.Text;
        RefreshSessionPanel();
    }

    private void ExecuteFileSearchIntent(AlphaVoiceIntent intent)
    {
        if (!TryAuthorizeBuiltInIntent(intent, out var policyMessage))
        {
            _sessionResultLabel.Text = policyMessage;
            RefreshSessionPanel();
            UpdateStatus(policyMessage);
            return;
        }

        ApplyRequestedUiMode("Files");
        _spokenCommandText.Text = intent.NormalizedCommand;
        _fileSearchQueryText.Text = intent.Target;
        SearchFiles();
        _sessionResultLabel.Text = $"File search: {intent.Target}.";
        RefreshSessionPanel();
    }

    private void ExecuteDictationIntent(AlphaVoiceIntent intent)
    {
        if (!TryAuthorizeBuiltInIntent(intent, out var policyMessage))
        {
            _sessionResultLabel.Text = policyMessage;
            RefreshSessionPanel();
            UpdateStatus(policyMessage);
            return;
        }

        ApplyRequestedUiMode("Dictation");
        _spokenCommandText.Text = intent.NormalizedCommand;
        StartDictation();
        if (intent.Target.StartsWith(AlphaCommandRouter.DictationInsertTextActionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var insertedText = intent.Target[AlphaCommandRouter.DictationInsertTextActionPrefix.Length..].Trim();
            AppendDictationTranscript(insertedText);
            _sessionResultLabel.Text = "Dictation text added to the visible review surface.";
            RefreshSessionPanel();
            return;
        }

        if (TryHandleDictationVoiceAction(intent.NormalizedCommand))
        {
            _sessionResultLabel.Text = "Dictation command applied in the visible review surface.";
            RefreshSessionPanel();
            return;
        }

        _sessionResultLabel.Text = "Dictation started in the visible review surface.";
        RefreshSessionPanel();
    }

    private bool TryAuthorizeBuiltInIntent(AlphaVoiceIntent intent, out string message) =>
        TryAuthorizeBuiltInIntent(intent, requireVoiceIdentity: true, out message);

    private bool TryAuthorizeBuiltInIntent(AlphaVoiceIntent intent, bool requireVoiceIdentity, out string message)
    {
        var definition = CreateBuiltInCommandDefinition(intent);
        var identityVerified = requireVoiceIdentity
            ? string.Equals(_session.VerifiedCallsign, _activeProfile?.Callsign, StringComparison.OrdinalIgnoreCase)
            : true;
        var freshIdentity = requireVoiceIdentity
            ? _session.HasFreshIdentity(UpdateCheckService.DefaultIdentityFreshness)
            : true;
        var policy = CallsignCommandPolicy.Evaluate(definition, identityVerified, freshIdentity);
        var actionTarget = string.IsNullOrWhiteSpace(intent.Target) ? intent.NormalizedCommand : intent.Target;

        if (policy.Decision is CallsignPolicyDecision.BlockedDangerousAction or CallsignPolicyDecision.Deny)
        {
            AuditBuiltInPolicyDecision(intent, "blocked", policy.Reason, success: false);
            message = policy.Reason;
            return false;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireFreshIdentity)
        {
            AuditBuiltInPolicyDecision(intent, "blocked", policy.Reason, success: false);
            message = policy.Reason;
            return false;
        }

        if (policy.Decision == CallsignPolicyDecision.RequireApproval)
        {
            var prompt = $"The built-in command '{definition.DisplayName}' may affect local state. "
                + $"Risk: {policy.RiskTier}, privacy impact: {definition.PrivacyImpact}. Proceed?";
            var answer = MessageBox.Show(this, prompt, "Approve Callsign command", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                AuditBuiltInPolicyDecision(intent, "blocked", "user_denied_approval", success: false);
                message = "Command blocked by user.";
                return false;
            }
        }

        AuditBuiltInPolicyDecision(intent, "allowed", $"policy_allowed:{actionTarget}", success: true);
        message = policy.Reason;
        return true;
    }

    private void AuditBuiltInPolicyDecision(AlphaVoiceIntent intent, string status, string details, bool success)
    {
        if (!EnsureActiveProfile(out var profileForAudit))
            return;

        _auditLog.TryRecordCommand(
            profileForAudit,
            eventType: "alpha.command_policy",
            actionName: "built_in_command",
            status,
            out _,
            commandFamily: intent.Kind.ToString(),
            actionTarget: string.IsNullOrWhiteSpace(intent.Target) ? intent.NormalizedCommand : intent.Target,
            details: details,
            success: success,
            verificationMethod: "policy_evaluation",
            verificationSummary: success
                ? "Built-in command passed Callsign policy evaluation before visible execution."
                : "Built-in command was blocked by Callsign policy evaluation before execution.");
    }

    private static CallsignCommandDefinition CreateBuiltInCommandDefinition(AlphaVoiceIntent intent)
    {
        var kind = intent.Kind switch
        {
            AlphaVoiceIntentKind.Browser => CallsignCommandKind.Browser,
            AlphaVoiceIntentKind.FileSearch => CallsignCommandKind.FileSearch,
            AlphaVoiceIntentKind.Dictation => CallsignCommandKind.Dictation,
            AlphaVoiceIntentKind.SystemControl => CallsignCommandKind.SystemControl,
            AlphaVoiceIntentKind.UiAction => CallsignCommandKind.UiAction,
            AlphaVoiceIntentKind.StartMenuLaunch => CallsignCommandKind.StartMenuLaunch,
            _ => CallsignCommandKind.Extension
        };

        var risk = intent.Kind switch
        {
            AlphaVoiceIntentKind.FileSearch => CallsignCommandRiskTier.Observe,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("print", StringComparison.OrdinalIgnoreCase) => CallsignCommandRiskTier.LocalStateChange,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("close-window", StringComparison.OrdinalIgnoreCase) => CallsignCommandRiskTier.LocalStateChange,
            AlphaVoiceIntentKind.SystemControl => CallsignCommandRiskTier.LocalReversible,
            AlphaVoiceIntentKind.Dictation => CallsignCommandRiskTier.LocalStateChange,
            _ => CallsignCommandRiskTier.LocalReversible
        };

        var privacy = intent.Kind switch
        {
            AlphaVoiceIntentKind.FileSearch => CallsignCommandPrivacyImpact.FilePath,
            AlphaVoiceIntentKind.Dictation => CallsignCommandPrivacyImpact.Clipboard,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("snipping-toolbar", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.ScreenshotOrOcr,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("clipboard-history", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.Clipboard,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("copy", StringComparison.OrdinalIgnoreCase)
                || intent.Target.Contains("paste", StringComparison.OrdinalIgnoreCase)
                || intent.Target.Contains("cut", StringComparison.OrdinalIgnoreCase) => CallsignCommandPrivacyImpact.Clipboard,
            _ => CallsignCommandPrivacyImpact.WindowTitleOrProcess
        };

        var approval = intent.Kind switch
        {
            AlphaVoiceIntentKind.Dictation => CallsignCommandApprovalRequirement.RequireFreshIdentity,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("snipping-toolbar", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("clipboard-history", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("print", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            AlphaVoiceIntentKind.SystemControl when intent.Target.Contains("close-window", StringComparison.OrdinalIgnoreCase) => CallsignCommandApprovalRequirement.RequireApproval,
            _ => CallsignCommandApprovalRequirement.None
        };

        return new CallsignCommandDefinition(
            CommandId: $"builtin.{intent.Kind.ToString().ToLowerInvariant()}",
            DisplayName: FormatBuiltInIntentStatus(string.IsNullOrWhiteSpace(intent.Target) ? intent.NormalizedCommand : intent.Target, intent.Kind.ToString()),
            VoicePhrases: [intent.NormalizedCommand],
            Description: "Built-in free Voice Access parity command.",
            Kind: kind,
            Tier: CallsignPackTier.Free,
            RiskTier: risk,
            VisibleAction: true,
            Target: intent.Target,
            Category: intent.Kind.ToString(),
            PrivacyImpact: privacy,
            ApprovalRequirement: approval,
            HelpText: "Built-in Callsign command executed only after wake and identity verification.",
            Examples: [intent.NormalizedCommand],
            VerificationStrategy: CallsignCommandVerificationStrategy.VisibleStatus);
    }

    private static string FormatBuiltInIntentStatus(string action, string fallback)
    {
        if (string.IsNullOrWhiteSpace(action))
            return $"{fallback} requested.";

        return action
            .Replace("system-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("ui-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", " ", StringComparison.OrdinalIgnoreCase)
            + " requested.";
    }

    private static string FormatBrowserIntentStatus(string action)
    {
        if (BrowserLaunchService.TryParseFindTextAction(action, out var findText))
            return $"Browser find text requested: {findText}";

        return action.Replace("browser-", "Browser ", StringComparison.OrdinalIgnoreCase)
            .Replace("-", " ", StringComparison.OrdinalIgnoreCase)
            + " requested.";
    }

    private static bool IsIgnorableSpeechTranscript(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return true;

        var trimmed = transcript.Trim();
        return trimmed.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("BLANK_AUDIO", StringComparison.OrdinalIgnoreCase);
    }

    private void AppendSessionTranscriptHistory(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return;

        var existing = _sessionTranscriptHistoryList.Items
            .Cast<object?>()
            .Select(item => item?.ToString())
            .FirstOrDefault();
        if (string.Equals(existing, entry, StringComparison.OrdinalIgnoreCase))
            return;

        _sessionTranscriptHistoryList.Items.Insert(0, entry);
        while (_sessionTranscriptHistoryList.Items.Count > 8)
            _sessionTranscriptHistoryList.Items.RemoveAt(_sessionTranscriptHistoryList.Items.Count - 1);
    }

    private void SetSessionTranscriptHistory(IReadOnlyList<string> entries)
    {
        _sessionTranscriptHistoryList.BeginUpdate();
        try
        {
            _sessionTranscriptHistoryList.Items.Clear();
            foreach (var entry in entries.Take(8))
                _sessionTranscriptHistoryList.Items.Add(entry);
        }
        finally
        {
            _sessionTranscriptHistoryList.EndUpdate();
        }
    }

    private void AppendDictationTranscript(string transcript)
    {
        var normalized = transcript.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        var displayText = DictationReviewTextService.FormatReviewedSegment(
            normalized,
            _dictationCasingMode,
            IsFluidDictationEnabled(),
            IsAutomaticPunctuationEnabled(),
            IsProfanityFilterEnabled(),
            DictationReviewTextService.ShouldTreatNextSegmentAsSentenceStart(_dictationTextBox.Text));
        _dictationLastTranscriptUtc = DateTime.UtcNow;
        _dictationLastTranscriptText = displayText;
        AppendDictationHistory(displayText);
        if (_dictationLastHeardLabel != null)
            _dictationLastHeardLabel.Text = FormatLastHeardLabel(displayText);
        _dictationTextBox.Text = DictationReviewTextService.AppendReviewedText(
            _dictationTextBox.Text,
            normalized,
            _dictationCasingMode,
            IsFluidDictationEnabled(),
            IsAutomaticPunctuationEnabled(),
            IsProfanityFilterEnabled());
        _dictationTextBox.SelectionStart = _dictationTextBox.TextLength;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Dictation updated.");
    }

    private void AppendDictationHistory(string transcript)
    {
        var normalized = transcript.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (_dictationHistoryEntries.Count > 0
            && string.Equals(_dictationHistoryEntries[0], normalized, StringComparison.OrdinalIgnoreCase))
            return;

        _dictationHistoryEntries.Insert(0, normalized);
        while (_dictationHistoryEntries.Count > 8)
            _dictationHistoryEntries.RemoveAt(_dictationHistoryEntries.Count - 1);

        SetDictationHistory(_dictationHistoryEntries);
    }

    private void SetDictationHistory(IReadOnlyList<string> entries)
    {
        if (_dictationHistoryList == null)
            return;

        _dictationHistoryList.BeginUpdate();
        try
        {
            _dictationHistoryList.Items.Clear();
            foreach (var entry in entries.Where(entry => !string.IsNullOrWhiteSpace(entry)).Take(8))
            {
                var displayEntry = DictationReviewTextService.FormatReviewedSegment(
                    entry,
                    _dictationCasingMode,
                    IsFluidDictationEnabled(),
                    IsAutomaticPunctuationEnabled(),
                    IsProfanityFilterEnabled(),
                    startOfSentence: true);
                _dictationHistoryList.Items.Add(displayEntry.Trim());
            }
        }
        finally
        {
            _dictationHistoryList.EndUpdate();
        }
    }

    private bool IsAutomaticPunctuationEnabled() =>
        _activeProfile?.Settings.DictationAutomaticPunctuationEnabled ?? true;

    private bool IsFluidDictationEnabled() =>
        _activeProfile?.Settings.DictationFluidModeEnabled ?? false;

    private bool IsProfanityFilterEnabled() =>
        _activeProfile?.Settings.DictationProfanityFilterEnabled ?? true;

    private void VerifyIdentity()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        var voiceEnrolled = IsVoiceEnrolled(profile.Settings);
        if (!_session.TryVerifyIdentity(_spokenCallsignText.Text, profile.Callsign, voiceEnrolled, out var message))
        {
            RefreshSessionPanel();
            UpdateStatus(message);
            return;
        }

        RefreshSessionPanel();
        UpdateStatus(_pendingAppResolution?.IsAmbiguous == true
            ? $"{_pendingAppResolution.Message} Say '1', 'click 1', 'choose result 1', 'confirm app', or 'cancel'."
            : message);
    }

    private void SetPendingAppConfirmation(StartMenuAppResolution resolution)
    {
        _pendingAppResolution = resolution;
        _appCandidateList.BeginUpdate();
        _appCandidateList.Items.Clear();
        var number = 1;
        foreach (var candidate in resolution.Candidates.Take(5))
        {
            _appCandidateList.Items.Add(new AppCandidateListItem(number, candidate));
            number++;
        }

        if (_appCandidateList.Items.Count > 0)
            _appCandidateList.SelectedIndex = 0;

        _appCandidateList.EndUpdate();
        _appCandidateHintLabel.ForeColor = Color.FromArgb(180, 92, 0);
        RefreshPendingAppConfirmationHint();
        RefreshSessionPanel();
    }

    private void ClearPendingAppConfirmation(string? statusMessage = null)
    {
        _pendingAppResolution = null;
        _appCandidateList.Items.Clear();
        _appCandidateHintLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _appCandidateHintLabel.Text = "App confirmation: no ambiguous match pending.";
        RefreshSessionPanel();
        if (!string.IsNullOrWhiteSpace(statusMessage))
            UpdateStatus(statusMessage);
    }

    private void ConfirmSelectedAppCandidate()
    {
        if (_appCandidateList.SelectedItem is not AppCandidateListItem item)
        {
            UpdateStatus("Choose an app candidate first.");
            return;
        }

        SelectPendingAppCandidate(item.Number, launchAfterSelection: _session.State == AlphaSessionState.ReadyToLaunch);
    }

    private bool TryHandlePendingAppCandidateVoiceSelection(string transcript)
    {
        if (_pendingAppResolution?.IsAmbiguous != true)
            return false;

        if (StartMenuLauncher.IsConfirmAppCandidateCommand(transcript))
        {
            ConfirmSelectedAppCandidate();
            return true;
        }

        if (StartMenuLauncher.IsClearAppCandidateCommand(transcript))
        {
            ClearPendingAppConfirmation("App confirmation cleared.");
            return true;
        }

        if (StartMenuLauncher.IsNextAppCandidateCommand(transcript))
        {
            MovePendingAppCandidateSelection(1);
            return true;
        }

        if (StartMenuLauncher.IsPreviousAppCandidateCommand(transcript))
        {
            MovePendingAppCandidateSelection(-1);
            return true;
        }

        if (!StartMenuLauncher.TryParseAppCandidateSelectionNumber(transcript, out var candidateNumber))
            return false;

        SelectPendingAppCandidate(candidateNumber, launchAfterSelection: _session.State == AlphaSessionState.ReadyToLaunch);
        return true;
    }

    private void SelectPendingAppCandidate(int candidateNumber, bool launchAfterSelection)
    {
        var item = _appCandidateList.Items
            .OfType<AppCandidateListItem>()
            .FirstOrDefault(candidate => candidate.Number == candidateNumber);
        if (item == null)
        {
            UpdateStatus($"App choice {candidateNumber} is not available.");
            return;
        }

        _appNameText.Text = item.Candidate.DisplayName;
        ClearPendingAppConfirmation();
        _sessionResultLabel.Text = $"Confirmed app target: {item.Candidate.DisplayName}.";
        UpdateStatus($"Confirmed app target '{item.Candidate.DisplayName}'.");
        if (launchAfterSelection)
            LaunchAppFromStartMenu();
    }

    private void MovePendingAppCandidateSelection(int delta)
    {
        if (_appCandidateList.Items.Count == 0)
        {
            UpdateStatus("No app choices are available.");
            return;
        }

        var selectedIndex = _appCandidateList.SelectedIndex < 0 ? 0 : _appCandidateList.SelectedIndex;
        var nextIndex = Math.Clamp(selectedIndex + delta, 0, _appCandidateList.Items.Count - 1);
        _appCandidateList.SelectedIndex = nextIndex;
        if (_appCandidateList.SelectedItem is AppCandidateListItem item)
            UpdateStatus($"Selected app choice {item.Number}: {item.Candidate.DisplayName}.");
        else
            RefreshPendingAppConfirmationHint();
    }

    private void RefreshPendingAppConfirmationHint()
    {
        if (_pendingAppResolution?.IsAmbiguous != true)
            return;

        var selected = _appCandidateList.SelectedItem as AppCandidateListItem
            ?? _appCandidateList.Items.Cast<object>().OfType<AppCandidateListItem>().FirstOrDefault();
        var selectedText = selected == null
            ? "No app choice selected yet."
            : $"Selected: {selected.Number}. {selected.Candidate.DisplayName}.";
        _appCandidateHintLabel.Text = $"{FormatPendingAppConfirmationStatus(_pendingAppResolution)} {selectedText} Say 'next app choice' or 'previous app choice' to move the selection.";
    }

    private static string FormatPendingAppConfirmationStatus(StartMenuAppResolution resolution)
    {
        var choices = resolution.Candidates.Count == 0
            ? "No choices were found."
            : string.Join(", ", resolution.Candidates.Take(5).Select((candidate, index) => $"{index + 1}. {candidate.DisplayName}"));
        return $"App confirmation needed for '{resolution.RequestedName}'. {choices} Say '1', 'click 1', 'choose result 1', 'confirm app', or 'cancel'.";
    }

    private void SetPendingWindowSwitch(VisibleWindowSwitchResolution resolution)
    {
        _pendingWindowSwitchResolution = resolution;
        _systemWindowChoiceList.BeginUpdate();
        _systemWindowChoiceList.Items.Clear();
        var number = 1;
        foreach (var candidate in resolution.Candidates.Take(5))
        {
            _systemWindowChoiceList.Items.Add(new WindowSwitchListItem(number, candidate));
            number++;
        }

        if (_systemWindowChoiceList.Items.Count > 0)
            _systemWindowChoiceList.SelectedIndex = 0;

        _systemWindowChoiceList.EndUpdate();
        _systemWindowChoiceHintLabel.ForeColor = Color.FromArgb(180, 92, 0);
        RefreshPendingWindowSwitchHint();
        RefreshSystemPanel();
    }

    private void ClearPendingWindowSwitch(string? statusMessage = null)
    {
        _pendingWindowSwitchResolution = null;
        _systemWindowChoiceList.Items.Clear();
        _systemWindowChoiceHintLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _systemWindowChoiceHintLabel.Text = "Window switch: no pending choice.";
        RefreshSystemPanel();
        if (!string.IsNullOrWhiteSpace(statusMessage))
            UpdateStatus(statusMessage);
    }

    private void ConfirmSelectedWindowSwitchChoice()
    {
        if (_systemWindowChoiceList.SelectedItem is not WindowSwitchListItem item)
        {
            UpdateStatus("Choose a window option first.");
            return;
        }

        ActivatePendingWindowSwitchChoice(item.Number);
    }

    private bool TryHandlePendingWindowSwitchVoiceSelection(string transcript)
    {
        if (_pendingWindowSwitchResolution?.IsAmbiguous != true)
            return false;

        if (SystemControlService.IsConfirmVisibleWindowSelectionCommand(transcript))
        {
            ConfirmSelectedWindowSwitchChoice();
            return true;
        }

        if (SystemControlService.IsClearVisibleWindowSelectionCommand(transcript))
        {
            ClearPendingWindowSwitch("Window choice cleared.");
            return true;
        }

        if (SystemControlService.IsNextVisibleWindowSelectionCommand(transcript))
        {
            MovePendingWindowSwitchSelection(1);
            return true;
        }

        if (SystemControlService.IsPreviousVisibleWindowSelectionCommand(transcript))
        {
            MovePendingWindowSwitchSelection(-1);
            return true;
        }

        if (!SystemControlService.TryParseVisibleWindowSelectionNumber(transcript, out var candidateNumber))
            return false;

        ActivatePendingWindowSwitchChoice(candidateNumber);
        return true;
    }

    private void ActivatePendingWindowSwitchChoice(int candidateNumber)
    {
        var item = _systemWindowChoiceList.Items
            .OfType<WindowSwitchListItem>()
            .FirstOrDefault(candidate => candidate.Number == candidateNumber);
        if (item == null)
        {
            UpdateStatus($"Window choice {candidateNumber} is not available.");
            return;
        }

        _systemSwitchWindowText.Text = item.Candidate.DisplayName;
        EnsureActiveProfile(out var profileForAudit);
        if (!_systemControlService.TryActivateVisibleWindow(item.Candidate.Handle, out var message))
        {
            _systemStatusLabel.Text = message;
            _lastLocalSystemActionLabel = FormatLocalSystemActionLabel($"system-switch-window:{_pendingWindowSwitchResolution?.RequestedName}", message, succeeded: false);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "system_window_switch",
                    status: "failed",
                    out _,
                    commandFamily: "system",
                    actionTarget: item.Candidate.DisplayName,
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "Visible window-switch failure was shown in the System tab status.");
            }

            UpdateStatus(message);
            RefreshSystemPanel();
            return;
        }

        ClearPendingWindowSwitch();
        _systemStatusLabel.Text = message;
        _lastLocalSystemActionLabel = FormatLocalSystemActionLabel($"system-switch-window:{item.Candidate.DisplayName}", message, succeeded: true);
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "system_window_switch",
                status: "succeeded",
                out _,
                commandFamily: "system",
                actionTarget: item.Candidate.DisplayName,
                details: message,
                success: true,
                verificationMethod: "visible_status",
                verificationSummary: "Visible window-switch request was shown in the System tab status before focus changed.");
        }

        UpdateStatus(message);
        RefreshSystemPanel();
    }

    private void MovePendingWindowSwitchSelection(int delta)
    {
        if (_systemWindowChoiceList.Items.Count == 0)
        {
            UpdateStatus("No window choices are available.");
            return;
        }

        var selectedIndex = _systemWindowChoiceList.SelectedIndex < 0 ? 0 : _systemWindowChoiceList.SelectedIndex;
        var nextIndex = Math.Clamp(selectedIndex + delta, 0, _systemWindowChoiceList.Items.Count - 1);
        _systemWindowChoiceList.SelectedIndex = nextIndex;
        if (_systemWindowChoiceList.SelectedItem is WindowSwitchListItem item)
            UpdateStatus($"Selected window choice {item.Number}: {item.Candidate.DisplayName}.");
        else
            RefreshPendingWindowSwitchHint();
    }

    private void RefreshPendingWindowSwitchHint()
    {
        if (_pendingWindowSwitchResolution?.IsAmbiguous != true)
            return;

        var selected = _systemWindowChoiceList.SelectedItem as WindowSwitchListItem
            ?? _systemWindowChoiceList.Items.Cast<object>().OfType<WindowSwitchListItem>().FirstOrDefault();
        var selectedText = selected == null
            ? "No window choice selected yet."
            : $"Selected: {selected.Number}. {selected.Candidate.DisplayName}.";
        _systemWindowChoiceHintLabel.Text = $"{FormatPendingWindowSwitchStatus(_pendingWindowSwitchResolution)} {selectedText} Say 'next window choice' or 'previous window choice' to move the selection.";
    }

    private static string FormatPendingWindowSwitchStatus(VisibleWindowSwitchResolution resolution)
    {
        var choices = resolution.Candidates.Count == 0
            ? "No window choices were found."
            : string.Join(", ", resolution.Candidates.Take(5).Select((candidate, index) => $"{index + 1}. {candidate.DisplayName}"));
        return $"Window choice needed for '{resolution.RequestedName}'. {choices} Say '1', 'click 1', 'choose window 1', 'confirm window', or 'cancel'.";
    }

    private void CaptureCommand()
    {
        if (!_session.TryCaptureCommand(_spokenCommandText.Text, out var message))
        {
            RefreshSessionPanel();
            UpdateStatus(message);
            return;
        }

        var inferredApp = InferAppName(_spokenCommandText.Text);
        if (!string.IsNullOrWhiteSpace(inferredApp) && string.IsNullOrWhiteSpace(_appNameText.Text))
        {
            var resolution = _launcher.ResolveInstalledAppName(inferredApp);
            if (resolution.IsAmbiguous)
            {
                _appNameText.Text = inferredApp;
                SetPendingAppConfirmation(resolution);
            }
            else if (resolution.IsResolved && !string.IsNullOrWhiteSpace(resolution.SelectedName))
            {
                ClearPendingAppConfirmation();
                _appNameText.Text = resolution.SelectedName;
            }
            else if (_launcher.TryResolveInstalledAppName(inferredApp, out var resolvedApp))
            {
                ClearPendingAppConfirmation();
                _appNameText.Text = resolvedApp;
            }
            else
            {
                ClearPendingAppConfirmation();
                _appNameText.Text = inferredApp;
            }
        }

        RefreshSessionPanel();
        UpdateStatus(message);
    }

    private void LaunchAppFromStartMenu()
    {
        if (!EnsureActiveProfile(out var profile))
            return;
        var targetForStatus = string.IsNullOrWhiteSpace(_appNameText.Text)
            ? InferAppName(_spokenCommandText.Text)
            : _appNameText.Text.Trim();

        var target = targetForStatus;

        if (!string.IsNullOrWhiteSpace(target))
        {
            var resolution = _launcher.ResolveInstalledAppName(target);
            if (resolution.IsAmbiguous)
            {
                SetPendingAppConfirmation(resolution);
                _sessionResultLabel.Text = FormatPendingAppConfirmationStatus(resolution);
                UpdateStatus($"{resolution.Message} Say '1', 'click 1', 'choose result 1', 'confirm app', or 'cancel'.");
                return;
            }

            if (resolution.IsResolved && !string.IsNullOrWhiteSpace(resolution.SelectedName))
            {
                target = resolution.SelectedName;
                ClearPendingAppConfirmation();
            }
            else if (_launcher.TryResolveInstalledAppName(target, out var resolvedTarget))
            {
                target = resolvedTarget;
                ClearPendingAppConfirmation();
            }
        }

        if (!_session.TryBeginLaunch(target, out var beginMessage))
        {
            if (!string.IsNullOrWhiteSpace(targetForStatus))
            {
                _auditLog.TryRecordCommand(
                    profile,
                    eventType: "alpha.command_execution",
                    actionName: "start_menu_launch",
                    status: "failed",
                    out _,
                    commandFamily: "start_menu",
                    actionTarget: targetForStatus,
                    details: $"validation_failed:{beginMessage}",
                    success: false);
            }

            RefreshSessionPanel();
            UpdateStatus(beginMessage);
            return;
        }

        RefreshSessionPanel();
        var launchResult = _launcher.LaunchWithResult(target);
        if (launchResult.Succeeded)
        {
            profile.Settings.LastLaunchedApp = target;
            SaveVoiceState(profile);
            _profileStore.Save(profile);
            var auditRecorded = _auditLog.TryRecordStartMenuLaunch(
                profile,
                target,
                out var auditWarning,
                launchPath: launchResult.LaunchPath,
                visibleStartMenuPath: launchResult.IsVisibleStartMenuPath);
            _session.CompleteLaunch();
            _spokenCallsignText.Text = string.Empty;
            _spokenCommandText.Text = string.Empty;
            _appNameText.Text = string.Empty;
            RefreshAllPanels();
            _sessionResultLabel.Text = launchResult.IsVisibleStartMenuPath
                ? $"Launched '{target}' through visible Start menu search."
                : launchResult.Message;
            _auditLog.TryRecordCommand(
                profile,
                eventType: "alpha.command_execution",
                actionName: "start_menu_launch",
                status: "succeeded",
                out _,
                commandFamily: "start_menu",
                actionTarget: target,
                launchPath: launchResult.LaunchPath,
                details: beginMessage,
                success: true);
            UpdateStatus(auditRecorded
                ? $"{launchResult.Message} Recorded the local alpha audit event. Say 'Callsign {profile.Callsign}' to launch another app."
                : auditWarning ?? $"{launchResult.Message} Audit logging reported a warning.");
            return;
        }

        _session.FailLaunch(launchResult.Message);
        _auditLog.TryRecordCommand(
            profile,
            eventType: "alpha.command_execution",
            actionName: "start_menu_launch",
            status: "failed",
            out _,
            commandFamily: "start_menu",
            actionTarget: targetForStatus,
            details: launchResult.Message,
            success: false);
        RefreshSessionPanel();
        UpdateStatus(launchResult.Message);
    }

    private void StartDictation()
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        _voiceAccessMode = "Dictation only";
        _dictationActive = true;
        _dictationCasingMode = DictationCasingMode.Default;
        _dictationStartedUtc = DateTime.UtcNow;
        _dictationLastTranscriptUtc = null;
        _dictationHistoryEntries.Clear();
        ClearDictationUndoSnapshots();
        SetDictationHistory(_dictationHistoryEntries);
        _session.Reset();
        RefreshSessionPanel();

        _voiceCommandService.Start(
            _activeProfile?.Settings.LanguageCode ?? "en-US",
            "Dictation",
            string.Empty,
            microphoneSettings: _activeProfile != null ? MicrophoneAudioSettings.From(_activeProfile.Settings) : null,
            segmentSilenceMilliseconds: _activeProfile?.Settings.VoiceSilenceMilliseconds);

        if (!_voiceCommandService.IsListening)
        {
            _dictationActive = false;
            RefreshDictationPanel();
            UpdateListeningPanel();
            UpdateStatus("Unable to start dictation.");
            RecordDictationActionAudit(
                "dictation_start",
                "failed",
                "start",
                "listener_not_started",
                false,
                "Dictation start failure was shown in the visible Dictation review surface.");
            return;
        }

        RefreshDictationPanel();
        UpdateListeningPanel();
        UpdateStatus("Voice mode set to Dictation only. Speak naturally and review the text below.");
        RecordDictationActionAudit(
            "dictation_start",
            "succeeded",
            "start",
            "dictation_only",
            true,
            "Dictation start was shown in the visible Dictation review surface.");
    }

    private void StopDictation()
    {
        _dictationActive = false;
        _dictationCasingMode = DictationCasingMode.Default;
        _dictationStartedUtc = null;
        _dictationLastTranscriptUtc = null;
        _dictationLastTranscriptText = null;
        if (_dictationLastHeardLabel != null)
            _dictationLastHeardLabel.Text = "Last heard: nothing yet.";

        if (_voiceCommandService.IsListening)
            _voiceCommandService.Stop();

        RefreshDictationPanel();
        UpdateListeningPanel();
        UpdateStatus("Dictation stopped.");
        RecordDictationActionAudit(
            "dictation_stop",
            "succeeded",
            "stop",
            "stopped",
            true,
            "Dictation stop was shown in the visible Dictation review surface.");
    }

    private void PauseDictation()
    {
        _dictationActive = false;
        _dictationStartedUtc = null;

        if (_voiceCommandService.IsListening)
            _voiceCommandService.Stop();

        RefreshDictationPanel();
        UpdateListeningPanel();
        UpdateStatus("Dictation paused. Reviewed text is preserved. Say 'resume dictation' after wake and identity verification to continue.");
        RecordDictationActionAudit(
            "dictation_pause",
            "succeeded",
            "pause",
            "review_preserved",
            true,
            "Dictation pause was shown in the visible Dictation review surface.");
    }

    private void ClearDictationText()
    {
        _dictationTextBox.Clear();
        _dictationCasingMode = DictationCasingMode.Default;
        ClearDictationUndoSnapshots();
        _dictationHistoryEntries.Clear();
        SetDictationHistory(_dictationHistoryEntries);
        _dictationLastTranscriptText = null;
        if (_dictationLastHeardLabel != null)
            _dictationLastHeardLabel.Text = "Last heard: nothing yet.";
        RefreshDictationPanel();
        UpdateStatus("Dictation text cleared.");
        RecordDictationActionAudit(
            "dictation_clear",
            "succeeded",
            "clear",
            "review_buffer_cleared",
            true,
            "Dictation clear was shown in the visible Dictation review surface.");
    }

    private void CutDictationText()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to cut.");
            RecordDictationActionAudit(
                "dictation_cut",
                "failed",
                "cut",
                "empty_review_buffer",
                false,
                "Dictation cut failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationTextBox.Focus();
        if (_dictationTextBox.SelectionLength > 0)
        {
            Clipboard.SetText(_dictationTextBox.SelectedText);
            _dictationTextBox.SelectedText = string.Empty;
        }
        else
        {
            Clipboard.SetText(_dictationTextBox.Text);
            _dictationTextBox.Clear();
        }

        RefreshDictationPanel();
        UpdateStatus("Dictated text cut to the clipboard.");
        RecordDictationActionAudit(
            "dictation_cut",
            "succeeded",
            "cut",
            "clipboard_updated",
            true,
            "Dictation cut was shown in the visible Dictation review surface.");
    }

    private void UndoDictationText()
    {
        _dictationTextBox.Focus();
        if (_dictationUndoSnapshot != null)
        {
            var redoText = _dictationTextBox.Text;
            var redoStart = _dictationTextBox.SelectionStart;
            var redoLength = _dictationTextBox.SelectionLength;

            _dictationTextBox.Text = _dictationUndoSnapshot;
            _dictationTextBox.SelectionStart = Math.Clamp(_dictationUndoSelectionStart, 0, _dictationTextBox.TextLength);
            _dictationTextBox.SelectionLength = Math.Clamp(_dictationUndoSelectionLength, 0, _dictationTextBox.TextLength - _dictationTextBox.SelectionStart);
            _dictationRedoSnapshot = redoText;
            _dictationRedoSelectionStart = redoStart;
            _dictationRedoSelectionLength = redoLength;
            _dictationUndoSnapshot = null;
            RefreshDictationPanel();
            UpdateStatus("Dictation edit undone.");
            RecordDictationActionAudit(
                "dictation_undo",
                "succeeded",
                "undo",
                "snapshot_restored",
                true,
                "Dictation undo was shown in the visible Dictation review surface.");
            return;
        }

        if (_dictationTextBox.CanUndo)
        {
            _dictationTextBox.Undo();
            RefreshDictationPanel();
            UpdateStatus("Dictation edit undone.");
            RecordDictationActionAudit(
                "dictation_undo",
                "succeeded",
                "undo",
                "textbox_undo",
                true,
                "Dictation undo was shown in the visible Dictation review surface.");
            return;
        }

        UpdateStatus("There is no dictation edit to undo.");
        RecordDictationActionAudit(
            "dictation_undo",
            "failed",
            "undo",
            "no_undo_available",
            false,
            "Dictation undo failure was shown in the visible Dictation review surface.");
    }

    private void RedoDictationText()
    {
        _dictationTextBox.Focus();
        if (_dictationRedoSnapshot != null)
        {
            _dictationUndoSnapshot = _dictationTextBox.Text;
            _dictationUndoSelectionStart = _dictationTextBox.SelectionStart;
            _dictationUndoSelectionLength = _dictationTextBox.SelectionLength;
            _dictationTextBox.Text = _dictationRedoSnapshot;
            _dictationTextBox.SelectionStart = Math.Clamp(_dictationRedoSelectionStart, 0, _dictationTextBox.TextLength);
            _dictationTextBox.SelectionLength = Math.Clamp(_dictationRedoSelectionLength, 0, _dictationTextBox.TextLength - _dictationTextBox.SelectionStart);
            _dictationRedoSnapshot = null;
            RefreshDictationPanel();
            UpdateStatus("Dictation edit redone.");
            RecordDictationActionAudit(
                "dictation_redo",
                "succeeded",
                "redo",
                "snapshot_restored",
                true,
                "Dictation redo was shown in the visible Dictation review surface.");
            return;
        }

        SendKeys.SendWait("^y");
        RefreshDictationPanel();
        UpdateStatus("Dictation edit redone.");
        RecordDictationActionAudit(
            "dictation_redo",
            "succeeded",
            "redo",
            "sendkeys_redo",
            true,
            "Dictation redo was shown in the visible Dictation review surface.");
    }

    private void GoToStartDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = 0;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the start of the dictated text.");
    }

    private void GoToEndDictationText()
    {
        _dictationTextBox.Focus();
        var end = _dictationTextBox.TextLength;
        _dictationTextBox.SelectionStart = end;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the end of the dictated text.");
    }

    private void SelectToStartDictationText()
    {
        _dictationTextBox.Focus();
        var selectionEnd = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        _dictationTextBox.SelectionStart = 0;
        _dictationTextBox.SelectionLength = selectionEnd;
        UpdateStatus("Selected text to the start of the dictated text.");
    }

    private void SelectToEndDictationText()
    {
        _dictationTextBox.Focus();
        var selectionStart = _dictationTextBox.SelectionStart;
        _dictationTextBox.SelectionLength = Math.Max(0, _dictationTextBox.TextLength - selectionStart);
        UpdateStatus("Selected text to the end of the dictated text.");
    }

    private void DeleteToStartDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var selectionEnd = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        _dictationTextBox.Text = _dictationTextBox.Text[selectionEnd..];
        _dictationTextBox.SelectionStart = 0;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the start of the dictated text.");
    }

    private void DeleteToEndDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var selectionStart = _dictationTextBox.SelectionStart;
        _dictationTextBox.Text = _dictationTextBox.Text[..selectionStart];
        _dictationTextBox.SelectionStart = _dictationTextBox.TextLength;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the end of the dictated text.");
    }

    private int GetCurrentLineStart()
    {
        if (_dictationTextBox.TextLength == 0)
            return 0;

        var caret = Math.Max(0, _dictationTextBox.SelectionStart);
        return _dictationTextBox.GetFirstCharIndexOfCurrentLine();
    }

    private int GetCurrentLineEnd()
    {
        if (_dictationTextBox.TextLength == 0)
            return 0;

        var lineIndex = _dictationTextBox.GetLineFromCharIndex(Math.Max(0, _dictationTextBox.SelectionStart));
        var lines = _dictationTextBox.Lines;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return _dictationTextBox.TextLength;

        var lineStart = _dictationTextBox.GetFirstCharIndexFromLine(lineIndex);
        var lineLength = lines[lineIndex].Length;
        return Math.Min(_dictationTextBox.TextLength, lineStart + lineLength);
    }

    private int GetCurrentLineIndex()
        => _dictationTextBox.TextLength == 0
            ? -1
            : _dictationTextBox.GetLineFromCharIndex(Math.Clamp(_dictationTextBox.SelectionStart, 0, _dictationTextBox.TextLength));

    private (int Start, int Length) GetLineTextSpan(int lineIndex)
    {
        var lines = _dictationTextBox.Lines;
        if (_dictationTextBox.TextLength == 0 || lineIndex < 0 || lineIndex >= lines.Length)
            return (0, 0);

        var start = _dictationTextBox.GetFirstCharIndexFromLine(lineIndex);
        if (start < 0)
            return (0, 0);

        return (start, Math.Min(lines[lineIndex].Length, _dictationTextBox.TextLength - start));
    }

    private (int Start, int Length) GetLineRemovalSpan(int lineIndex)
    {
        var (start, length) = GetLineTextSpan(lineIndex);
        if (length <= 0)
            return (0, 0);

        var text = _dictationTextBox.Text;
        var end = start + length;
        if (end < text.Length && text[end] == '\r')
            end++;
        if (end < text.Length && text[end] == '\n')
            end++;
        else if (start > 0 && text[start - 1] == '\n')
        {
            start--;
            if (start > 0 && text[start - 1] == '\r')
                start--;
        }

        return (start, end - start);
    }

    private void GoToLineStartDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = GetCurrentLineStart();
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the start of the current line.");
    }

    private void GoToLineEndDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = GetCurrentLineEnd();
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the end of the current line.");
    }

    private void GoToPreviousDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineTextSpan(currentLine - 1);
        if (length <= 0)
        {
            UpdateStatus("There is no previous dictated line to move to.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the previous line.");
    }

    private void GoToNextDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineTextSpan(currentLine + 1);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated line to move to.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the next line.");
    }

    private void SelectToLineStartDictationText()
    {
        _dictationTextBox.Focus();
        var selectionEnd = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        _dictationTextBox.SelectionStart = GetCurrentLineStart();
        _dictationTextBox.SelectionLength = Math.Max(0, selectionEnd - _dictationTextBox.SelectionStart);
        UpdateStatus("Selected text to the start of the current line.");
    }

    private void SelectToLineEndDictationText()
    {
        _dictationTextBox.Focus();
        var selectionStart = _dictationTextBox.SelectionStart;
        _dictationTextBox.SelectionLength = Math.Max(0, GetCurrentLineEnd() - selectionStart);
        UpdateStatus("Selected text to the end of the current line.");
    }

    private void DeleteToLineStartDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var start = GetCurrentLineStart();
        var end = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        if (end < start)
            end = start;

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, end - start);
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the start of the current line.");
    }

    private void DeleteToLineEndDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var start = _dictationTextBox.SelectionStart;
        var end = GetCurrentLineEnd();
        if (end < start)
            end = start;

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, end - start);
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the end of the current line.");
    }

    private void SelectPreviousDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineTextSpan(currentLine - 1);
        if (length <= 0)
        {
            UpdateStatus("There is no previous dictated line to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the previous line.");
    }

    private void SelectNextDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineTextSpan(currentLine + 1);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated line to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the next line.");
    }

    private void DeletePreviousDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineRemovalSpan(currentLine - 1);
        if (length <= 0)
        {
            UpdateStatus("There is no previous dictated line to delete.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length);
        _dictationTextBox.SelectionStart = Math.Clamp(start, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the previous line.");
    }

    private void DeleteNextDictationLine()
    {
        var currentLine = GetCurrentLineIndex();
        var (start, length) = GetLineRemovalSpan(currentLine + 1);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated line to delete.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length);
        _dictationTextBox.SelectionStart = Math.Clamp(start, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the next line.");
    }

    private int GetCurrentParagraphStart()
    {
        if (_dictationTextBox.TextLength == 0)
            return 0;

        var text = _dictationTextBox.Text;
        var caret = Math.Clamp(_dictationTextBox.SelectionStart, 0, text.Length);
        var start = text.LastIndexOf(Environment.NewLine + Environment.NewLine, Math.Max(0, caret - 1), StringComparison.Ordinal);
        if (start < 0)
            return 0;

        return start + (Environment.NewLine + Environment.NewLine).Length;
    }

    private int GetCurrentParagraphEnd()
    {
        if (_dictationTextBox.TextLength == 0)
            return 0;

        var text = _dictationTextBox.Text;
        var caret = Math.Clamp(_dictationTextBox.SelectionStart, 0, text.Length);
        var nextBreak = text.IndexOf(Environment.NewLine + Environment.NewLine, caret, StringComparison.Ordinal);
        if (nextBreak < 0)
            return text.Length;

        return nextBreak;
    }

    private void GoToParagraphStartDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = GetCurrentParagraphStart();
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the start of the current paragraph.");
    }

    private void GoToParagraphEndDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = GetCurrentParagraphEnd();
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the end of the current paragraph.");
    }

    private void SelectToParagraphStartDictationText()
    {
        _dictationTextBox.Focus();
        var selectionEnd = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        _dictationTextBox.SelectionStart = GetCurrentParagraphStart();
        _dictationTextBox.SelectionLength = Math.Max(0, selectionEnd - _dictationTextBox.SelectionStart);
        UpdateStatus("Selected text to the start of the current paragraph.");
    }

    private void SelectToParagraphEndDictationText()
    {
        _dictationTextBox.Focus();
        var selectionStart = _dictationTextBox.SelectionStart;
        _dictationTextBox.SelectionLength = Math.Max(0, GetCurrentParagraphEnd() - selectionStart);
        UpdateStatus("Selected text to the end of the current paragraph.");
    }

    private void DeleteToParagraphStartDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var start = GetCurrentParagraphStart();
        var end = _dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength;
        if (end < start)
            end = start;

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, end - start);
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the start of the current paragraph.");
    }

    private void DeleteToParagraphEndDictationText()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        var start = _dictationTextBox.SelectionStart;
        var end = GetCurrentParagraphEnd();
        if (end < start)
            end = start;

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, end - start);
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted text to the end of the current paragraph.");
    }

    private void ReplaceParagraphSpan(string replacementText)
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to replace.");
            return;
        }

        var start = GetCurrentParagraphStart();
        var end = GetCurrentParagraphEnd();
        var length = Math.Max(0, end - start);
        if (length <= 0)
        {
            UpdateStatus("There is no dictated paragraph to replace.");
            return;
        }

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).Insert(start, replacementText);
        RefreshDictationPanel();
        UpdateStatus("Replaced the previous paragraph.");
    }

    private void InsertDictationLineBreak()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = Environment.NewLine;
        RefreshDictationPanel();
        UpdateStatus("Dictation line break inserted.");
    }

    private void InsertDictationParagraphBreak()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = Environment.NewLine + Environment.NewLine;
        RefreshDictationPanel();
        UpdateStatus("Dictation paragraph inserted.");
    }

    private void InsertDictationSentenceBreak()
    {
        _dictationTextBox.Focus();
        if (_dictationTextBox.SelectionStart > 0
            && _dictationTextBox.SelectionStart == _dictationTextBox.TextLength
            && !_dictationTextBox.Text.EndsWith(" ", StringComparison.Ordinal)
            && !_dictationTextBox.Text.EndsWith(".", StringComparison.Ordinal)
            && !_dictationTextBox.Text.EndsWith("!", StringComparison.Ordinal)
            && !_dictationTextBox.Text.EndsWith("?", StringComparison.Ordinal))
        {
            _dictationTextBox.SelectedText = ". ";
        }
        else
        {
            _dictationTextBox.SelectedText = " ";
        }

        RefreshDictationPanel();
        UpdateStatus("Dictation sentence break inserted.");
    }

    private void InsertDictationTab()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = "\t";
        RefreshDictationPanel();
        UpdateStatus("Dictation tab inserted.");
    }

    private void DeleteLastDictationWord()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        SendKeys.SendWait("^{BACKSPACE}");
        RefreshDictationPanel();
        UpdateStatus("Deleted the last dictated word.");
    }

    private void GoToPreviousDictationWord()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^{LEFT}");
        UpdateStatus("Moved to the previous word.");
    }

    private void GoToNextDictationWord()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^{RIGHT}");
        UpdateStatus("Moved to the next word.");
    }

    private void SelectPreviousDictationWord()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^+{LEFT}");
        UpdateStatus("Selected the previous word.");
    }

    private void SelectNextDictationWord()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^+{RIGHT}");
        UpdateStatus("Selected the next word.");
    }

    private void DeletePreviousDictationWord()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        SendKeys.SendWait("^{BACKSPACE}");
        RefreshDictationPanel();
        UpdateStatus("Deleted the previous dictated word.");
    }

    private void DeleteNextDictationWord()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        _dictationTextBox.Focus();
        SendKeys.SendWait("^{DELETE}");
        RefreshDictationPanel();
        UpdateStatus("Deleted the next dictated word.");
    }

    private void SelectPreviousDictationCharacter()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated character to select.");
            return;
        }

        _dictationTextBox.Focus();
        var selectionStart = Math.Clamp(_dictationTextBox.SelectionStart, 0, _dictationTextBox.TextLength);
        if (selectionStart <= 0)
        {
            UpdateStatus("There is no previous dictated character to select.");
            return;
        }

        _dictationTextBox.SelectionStart = selectionStart - 1;
        _dictationTextBox.SelectionLength = 1;
        UpdateStatus("Selected the previous character.");
    }

    private void SelectNextDictationCharacter()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated character to select.");
            return;
        }

        _dictationTextBox.Focus();
        var selectionEnd = Math.Clamp(_dictationTextBox.SelectionStart + _dictationTextBox.SelectionLength, 0, _dictationTextBox.TextLength);
        if (selectionEnd >= _dictationTextBox.TextLength)
        {
            UpdateStatus("There is no next dictated character to select.");
            return;
        }

        _dictationTextBox.SelectionStart = selectionEnd;
        _dictationTextBox.SelectionLength = 1;
        UpdateStatus("Selected the next character.");
    }

    private void DeletePreviousDictationCharacter()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated character to delete.");
            return;
        }

        _dictationTextBox.Focus();
        if (_dictationTextBox.SelectionLength > 0)
        {
            _dictationTextBox.SelectedText = string.Empty;
            RefreshDictationPanel();
            UpdateStatus("Deleted the selected dictated character.");
            return;
        }

        var selectionStart = Math.Clamp(_dictationTextBox.SelectionStart, 0, _dictationTextBox.TextLength);
        if (selectionStart <= 0)
        {
            UpdateStatus("There is no previous dictated character to delete.");
            return;
        }

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(selectionStart - 1, 1);
        _dictationTextBox.SelectionStart = selectionStart - 1;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the previous character.");
    }

    private void DeleteNextDictationCharacter()
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus("There is no dictated character to delete.");
            return;
        }

        _dictationTextBox.Focus();
        if (_dictationTextBox.SelectionLength > 0)
        {
            _dictationTextBox.SelectedText = string.Empty;
            RefreshDictationPanel();
            UpdateStatus("Deleted the selected dictated character.");
            return;
        }

        var selectionStart = Math.Clamp(_dictationTextBox.SelectionStart, 0, _dictationTextBox.TextLength);
        if (selectionStart >= _dictationTextBox.TextLength)
        {
            UpdateStatus("There is no next dictated character to delete.");
            return;
        }

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(selectionStart, 1);
        _dictationTextBox.SelectionStart = selectionStart;
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the next character.");
    }

    private void GoToPreviousDictationSentence()
    {
        var (start, length) = GetLastSentenceSpan(_dictationTextBox.Text, includeTrailingPunctuation: false);
        if (length <= 0)
        {
            UpdateStatus("There is no previous dictated sentence to move to.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the previous sentence.");
    }

    private void GoToNextDictationSentence()
    {
        var (start, length) = GetNextSentenceSpan(_dictationTextBox.Text);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated sentence to move to.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = 0;
        UpdateStatus("Moved to the next sentence.");
    }

    private void SelectPreviousDictationSentence()
    {
        var (start, length) = GetLastSentenceSpan(_dictationTextBox.Text, includeTrailingPunctuation: false);
        if (length <= 0)
        {
            UpdateStatus("There is no dictated sentence to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the previous sentence.");
    }

    private void SelectNextDictationSentence()
    {
        var (start, length) = GetNextSentenceSpan(_dictationTextBox.Text);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated sentence to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the next sentence.");
    }

    private void DeletePreviousDictationSentence()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        var (start, length) = GetLastSentenceSpan(_dictationTextBox.Text, includeTrailingPunctuation: true);
        if (length <= 0)
        {
            UpdateStatus("There is no dictated sentence to delete.");
            return;
        }

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).TrimStart();
        RefreshDictationPanel();
        UpdateStatus("Deleted the previous sentence.");
    }

    private void DeleteNextDictationSentence()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to delete.");
            return;
        }

        var (start, length) = GetNextSentenceSpan(_dictationTextBox.Text);
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated sentence to delete.");
            return;
        }

        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).TrimStart();
        RefreshDictationPanel();
        UpdateStatus("Deleted the next sentence.");
    }

    private void GoToPreviousDictationParagraph()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^{UP}");
        UpdateStatus("Moved to the previous paragraph.");
    }

    private void GoToNextDictationParagraph()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^{DOWN}");
        UpdateStatus("Moved to the next paragraph.");
    }

    private void SelectPreviousDictationParagraph()
    {
        var (start, length) = GetCurrentParagraphSpan();
        if (length <= 0)
        {
            UpdateStatus("There is no dictated paragraph to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the previous paragraph.");
    }

    private void SelectNextDictationParagraph()
    {
        var (start, length) = GetNextParagraphSpan();
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated paragraph to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        UpdateStatus("Selected the next paragraph.");
    }

    private void DeletePreviousDictationParagraph()
    {
        var (start, length) = GetCurrentParagraphSpan();
        if (length <= 0)
        {
            UpdateStatus("There is no dictated paragraph to delete.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).TrimStart();
        _dictationTextBox.SelectionStart = Math.Clamp(start, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the previous paragraph.");
    }

    private void DeleteNextDictationParagraph()
    {
        var (start, length) = GetNextParagraphSpan();
        if (length <= 0)
        {
            UpdateStatus("There is no next dictated paragraph to delete.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).TrimStart();
        _dictationTextBox.SelectionStart = Math.Clamp(start, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the next paragraph.");
    }

    private (int Start, int Length) GetCurrentParagraphSpan()
    {
        if (_dictationTextBox.TextLength == 0)
            return (0, 0);

        var start = GetCurrentParagraphStart();
        var end = GetCurrentParagraphEnd();
        return end <= start ? (0, 0) : (start, end - start);
    }

    private (int Start, int Length) GetNextParagraphSpan()
    {
        if (_dictationTextBox.TextLength == 0)
            return (0, 0);

        var text = _dictationTextBox.Text;
        var separator = Environment.NewLine + Environment.NewLine;
        var currentEnd = GetCurrentParagraphEnd();
        var nextStart = currentEnd < text.Length
            ? currentEnd + separator.Length
            : text.Length;
        if (nextStart >= text.Length)
            return (0, 0);

        var nextEnd = text.IndexOf(separator, nextStart, StringComparison.Ordinal);
        nextEnd = nextEnd < 0 ? text.Length : nextEnd;
        return nextEnd <= nextStart ? (0, 0) : (nextStart, nextEnd - nextStart);
    }

    private void InsertDictationPunctuation(string punctuation)
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = punctuation;
        RefreshDictationPanel();
        UpdateStatus($"Inserted {punctuation.Trim()} punctuation.");
        RecordDictationActionAudit(
            "dictation_insert_punctuation",
            "succeeded",
            punctuation.Trim(),
            "punctuation_inserted",
            true,
            "Dictation punctuation insertion was shown in the visible Dictation review surface.");
    }

    private void InsertDictationSpace()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = " ";
        RefreshDictationPanel();
        UpdateStatus("Inserted a space.");
        RecordDictationActionAudit(
            "dictation_insert_space",
            "succeeded",
            "space",
            "space_inserted",
            true,
            "Dictation spacing edit was shown in the visible Dictation review surface.");
    }

    private void RemoveSpaceBeforeLastDictationPhrase()
    {
        var text = _dictationTextBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            UpdateStatus("There is no dictated text to join.");
            return;
        }

        var phrase = !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
            ? _dictationLastTranscriptText.Trim()
            : _dictationHistoryEntries.FirstOrDefault()?.Trim();
        int phraseStart;
        if (!string.IsNullOrWhiteSpace(phrase))
        {
            phraseStart = text.LastIndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            if (phraseStart < 0)
                phraseStart = GetLastWordSpan(text).Start;
        }
        else
        {
            phraseStart = GetLastWordSpan(text).Start;
        }

        if (phraseStart <= 0 || phraseStart > text.Length)
        {
            UpdateStatus("There is no preceding space to remove.");
            return;
        }

        var removeStart = phraseStart;
        while (removeStart > 0 && char.IsWhiteSpace(text[removeStart - 1]))
            removeStart--;

        if (removeStart == phraseStart)
        {
            UpdateStatus("There is no preceding space to remove.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = text.Remove(removeStart, phraseStart - removeStart);
        _dictationTextBox.SelectionStart = Math.Clamp(removeStart, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Removed the space before the last dictated phrase.");
    }

    private void WrapSelectedOrLastDictationPhrase(string openText, string closeText, string description)
    {
        if (_dictationTextBox.TextLength == 0)
        {
            UpdateStatus($"There is no dictated text to {description}.");
            return;
        }

        _dictationTextBox.Focus();
        var start = _dictationTextBox.SelectionStart;
        var length = _dictationTextBox.SelectionLength;
        if (length <= 0 && !TryGetLastDictationPhraseSpan(out start, out length))
        {
            UpdateStatus($"There is no recent dictated phrase to {description}.");
            return;
        }

        if (length <= 0 || start < 0 || start + length > _dictationTextBox.TextLength)
        {
            UpdateStatus($"There is no valid dictated text to {description}.");
            return;
        }

        var selectedText = _dictationTextBox.Text.Substring(start, length);
        var wrappedText = openText + selectedText + closeText;
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).Insert(start, wrappedText);
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = wrappedText.Length;
        RefreshDictationPanel();
        UpdateStatus($"Wrapped dictated text with {description}.");
    }

    private void InsertDictationSpelledText(string text)
    {
        var spelledText = text.Trim();
        if (string.IsNullOrWhiteSpace(spelledText))
        {
            UpdateStatus("No spelled text was captured.");
            RecordDictationActionAudit(
                "dictation_insert_spelled_text",
                "failed",
                "spelled_text",
                "empty_spelled_text",
                false,
                "Dictation spelled-text insertion failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationTextBox.Focus();
        if (_dictationTextBox.SelectionStart > 0
            && _dictationTextBox.SelectionStart == _dictationTextBox.TextLength
            && !_dictationTextBox.Text.EndsWith(" ", StringComparison.Ordinal))
        {
            _dictationTextBox.SelectedText = " ";
        }

        _dictationTextBox.SelectedText = spelledText;
        RefreshDictationPanel();
        UpdateStatus($"Spelled text inserted: '{spelledText}'.");
        RecordDictationActionAudit(
            "dictation_insert_spelled_text",
            "succeeded",
            "spelled_text",
            "spelled_text_inserted",
            true,
            "Dictation spelled-text insertion was shown in the visible Dictation review surface.");
    }

    private void ApplyDictationFormatCommand(DictationFormatCommand command)
    {
        if (!DictationFormattingService.TryApply(_dictationTextBox.Text, command, out var result))
        {
            UpdateStatus("There is no dictated text to format yet.");
            RecordDictationActionAudit(
                "dictation_format",
                "failed",
                command.Format.ToString(),
                "empty_or_invalid_scope",
                false,
                "Dictation formatting failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = result.Text;
        _dictationTextBox.SelectionStart = Math.Clamp(result.SelectionStart, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = Math.Clamp(result.SelectionLength, 0, _dictationTextBox.TextLength - _dictationTextBox.SelectionStart);
        RefreshDictationPanel();
        UpdateStatus($"Formatted {FormatDictationCorrectionScope(command.Scope)}.");
        RecordDictationActionAudit(
            "dictation_format",
            "succeeded",
            command.Format.ToString(),
            command.Scope.ToString(),
            true,
            "Dictation formatting was shown in the visible Dictation review surface.");
    }

    private void ApplyDictationTargetTextCommand(DictationTargetTextCommand command)
    {
        if (!DictationTargetTextService.TryApply(_dictationTextBox.Text, command, out var result))
        {
            UpdateStatus($"Could not find '{command.TargetText}' in the dictated text.");
            RecordDictationActionAudit(
                "dictation_target_text",
                "failed",
                command.Action.ToString(),
                "target_text_not_found",
                false,
                "Dictation target-text failure was shown in the visible Dictation review surface.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = result.Text;
        _dictationTextBox.SelectionStart = Math.Clamp(result.SelectionStart, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = Math.Clamp(result.SelectionLength, 0, _dictationTextBox.TextLength - _dictationTextBox.SelectionStart);
        RefreshDictationPanel();

        var status = command.Action switch
        {
            DictationTargetTextAction.Select => $"Selected '{result.MatchedText}' in the dictated text.",
            DictationTargetTextAction.MoveBefore => $"Moved before '{result.MatchedText}' in the dictated text.",
            DictationTargetTextAction.MoveAfter => $"Moved after '{result.MatchedText}' in the dictated text.",
            DictationTargetTextAction.Delete => $"Deleted '{result.MatchedText}' from the dictated text.",
            DictationTargetTextAction.Replace => $"Replaced '{result.MatchedText}' in the dictated text.",
            _ => "Updated dictated text."
        };
        UpdateStatus(status);
        RecordDictationActionAudit(
            "dictation_target_text",
            "succeeded",
            command.Action.ToString(),
            "target_text_applied",
            true,
            "Dictation target-text edit was shown in the visible Dictation review surface.");
    }

    private void ReplaceDictationSpan(DictationReplacementCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReplacementText))
        {
            UpdateStatus("No replacement text was captured.");
            RecordDictationActionAudit(
                "dictation_replace",
                "failed",
                command.Scope.ToString(),
                "empty_replacement_text",
                false,
                "Dictation replacement failure was shown in the visible Dictation review surface.");
            return;
        }

        var (start, length) = command.Scope switch
        {
            DictationReplacementScope.PreviousSentence => GetLastSentenceSpan(_dictationTextBox.Text, includeTrailingPunctuation: true),
            DictationReplacementScope.PreviousParagraph => (GetCurrentParagraphStart(), GetCurrentParagraphEnd() - GetCurrentParagraphStart()),
            _ => GetLastWordSpan(_dictationTextBox.Text)
        };

        if (length <= 0)
        {
            UpdateStatus("There is nothing to replace yet.");
            RecordDictationActionAudit(
                "dictation_replace",
                "failed",
                command.Scope.ToString(),
                "empty_or_invalid_scope",
                false,
                "Dictation replacement failure was shown in the visible Dictation review surface.");
            return;
        }

        var replacementText = command.ReplacementText.Trim();
        var text = _dictationTextBox.Text;
        if (command.Scope == DictationReplacementScope.AllText)
        {
            _dictationTextBox.Text = replacementText;
        }
        else
        {
            _dictationTextBox.Text = text.Remove(start, length).Insert(start, replacementText);
        }
        RefreshDictationPanel();
        UpdateStatus(command.Scope switch
        {
            DictationReplacementScope.PreviousSentence => "Replaced the previous sentence.",
            DictationReplacementScope.PreviousParagraph => "Replaced the previous paragraph.",
            DictationReplacementScope.AllText => "Replaced all dictated text.",
            _ => "Replaced the previous word."
        });
        RecordDictationActionAudit(
            "dictation_replace",
            "succeeded",
            command.Scope.ToString(),
            "replacement_applied",
            true,
            "Dictation replacement was shown in the visible Dictation review surface.");
    }

    private bool TryHandleDictationVoiceAction(string transcript)
    {
        if (!_dictationActive && !string.Equals(_tabs.SelectedTab?.Text, "Dictation", StringComparison.OrdinalIgnoreCase))
            return false;

        if (AlphaVoiceTranscriptParser.TryParseDictationReplacementCommand(transcript, out var replacementCommand) && replacementCommand != null)
        {
            return ExecuteDictationVoiceAction(() =>
            {
                if (replacementCommand.Scope == DictationReplacementScope.PreviousParagraph)
                {
                    ReplaceParagraphSpan(replacementCommand.ReplacementText);
                    return;
                }

                ReplaceDictationSpan(replacementCommand);
            }, replacementCommand.Scope switch
            {
                DictationReplacementScope.PreviousSentence => "replace the previous sentence",
                DictationReplacementScope.PreviousParagraph => "replace the previous paragraph",
                DictationReplacementScope.AllText => "replace all dictated text",
                _ => "replace the previous word"
            });
        }

        if (AlphaVoiceTranscriptParser.TryParseDictationCorrectionCommand(transcript, out var correctionCommand) && correctionCommand != null)
        {
            return correctionCommand.Action switch
            {
                DictationCorrectionVoiceAction.ShowAlternatives => ExecuteDictationVoiceAction(
                    () => ShowDictationCorrectionChoices(correctionCommand.Scope),
                    $"show correction alternatives for {FormatDictationCorrectionScope(correctionCommand.Scope)}"),
                DictationCorrectionVoiceAction.ChooseAlternative => ExecuteDictationVoiceAction(
                    () => ChooseDictationCorrection(correctionCommand.ChoiceNumber),
                    $"choose correction {correctionCommand.ChoiceNumber}"),
                DictationCorrectionVoiceAction.PreviousAlternative => ExecuteDictationVoiceAction(
                    () => MoveDictationCorrectionSelection(-1),
                    "previous correction"),
                DictationCorrectionVoiceAction.NextAlternative => ExecuteDictationVoiceAction(
                    () => MoveDictationCorrectionSelection(1),
                    "next correction"),
                DictationCorrectionVoiceAction.AcceptCurrentAlternative => ExecuteDictationVoiceAction(
                    AcceptSelectedDictationCorrection,
                    "accept correction"),
                DictationCorrectionVoiceAction.CancelAlternatives => ExecuteDictationVoiceAction(
                    CancelDictationCorrection,
                    "cancel correction alternatives"),
                _ => false
            };
        }

        if (AlphaVoiceTranscriptParser.TryParseDictationFormatCommand(transcript, out var formatCommand) && formatCommand != null)
            return ExecuteDictationVoiceAction(
                () => ApplyDictationFormatCommand(formatCommand),
                DictationFormattingService.FormatDescription(formatCommand));

        if (AlphaVoiceTranscriptParser.TryParseDictationCasingCommand(transcript, out var casingCommand) && casingCommand != null)
            return ExecuteDictationVoiceAction(
                () => ApplyDictationCasingCommand(casingCommand),
                FormatDictationCasingMode(casingCommand.Mode));

        if (AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand(transcript, out var spellingCommand) && spellingCommand != null)
            return ExecuteDictationVoiceAction(() => InsertDictationSpelledText(spellingCommand.Text), $"spell out '{spellingCommand.Text}'");

        var voiceAction = AlphaVoiceTranscriptParser.ParseDictationVoiceAction(transcript);
        if (voiceAction != DictationVoiceAction.None)
        {
            return voiceAction switch
        {
            DictationVoiceAction.Copy => ExecuteDictationVoiceAction(CopyDictationText, "copy the dictated text"),
            DictationVoiceAction.ReadBack => ExecuteDictationVoiceAction(ReadDictationTextAloud, "read the dictated text aloud", captureUndoSnapshot: false),
            DictationVoiceAction.StopReadBack => ExecuteDictationVoiceAction(StopDictationReadback, "stop reading the dictated text aloud", captureUndoSnapshot: false),
            DictationVoiceAction.Paste => ExecuteDictationVoiceAction(PasteDictationText, "paste the dictated text"),
            DictationVoiceAction.Clear => ExecuteDictationVoiceAction(ClearDictationText, "clear the dictated text"),
            DictationVoiceAction.SelectAll => ExecuteDictationVoiceAction(SelectAllDictationText, "select all dictated text"),
            DictationVoiceAction.Cut => ExecuteDictationVoiceAction(CutDictationText, "cut the dictated text"),
            DictationVoiceAction.Undo => ExecuteDictationVoiceAction(UndoDictationText, "undo the last dictated edit", captureUndoSnapshot: false),
            DictationVoiceAction.Redo => ExecuteDictationVoiceAction(RedoDictationText, "redo the last dictated edit", captureUndoSnapshot: false),
            DictationVoiceAction.SelectThat => ExecuteDictationVoiceAction(SelectLastDictationPhrase, "select the last dictated phrase"),
            DictationVoiceAction.DeleteThat => ExecuteDictationVoiceAction(DeleteLastDictationPhrase, "delete the last dictated phrase"),
            DictationVoiceAction.GoToStart => ExecuteDictationVoiceAction(GoToStartDictationText, "go to the start of the dictated text"),
            DictationVoiceAction.GoToEnd => ExecuteDictationVoiceAction(GoToEndDictationText, "go to the end of the dictated text"),
            DictationVoiceAction.SelectToStart => ExecuteDictationVoiceAction(SelectToStartDictationText, "select to the start of the dictated text"),
            DictationVoiceAction.SelectToEnd => ExecuteDictationVoiceAction(SelectToEndDictationText, "select to the end of the dictated text"),
            DictationVoiceAction.DeleteToStart => ExecuteDictationVoiceAction(DeleteToStartDictationText, "delete to the start of the dictated text"),
            DictationVoiceAction.DeleteToEnd => ExecuteDictationVoiceAction(DeleteToEndDictationText, "delete to the end of the dictated text"),
            DictationVoiceAction.GoToLineStart => ExecuteDictationVoiceAction(GoToLineStartDictationText, "go to the start of the current line"),
            DictationVoiceAction.GoToLineEnd => ExecuteDictationVoiceAction(GoToLineEndDictationText, "go to the end of the current line"),
            DictationVoiceAction.GoToPreviousLine => ExecuteDictationVoiceAction(GoToPreviousDictationLine, "go to the previous line"),
            DictationVoiceAction.GoToNextLine => ExecuteDictationVoiceAction(GoToNextDictationLine, "go to the next line"),
            DictationVoiceAction.SelectToLineStart => ExecuteDictationVoiceAction(SelectToLineStartDictationText, "select to the start of the current line"),
            DictationVoiceAction.SelectToLineEnd => ExecuteDictationVoiceAction(SelectToLineEndDictationText, "select to the end of the current line"),
            DictationVoiceAction.DeleteToLineStart => ExecuteDictationVoiceAction(DeleteToLineStartDictationText, "delete to the start of the current line"),
            DictationVoiceAction.DeleteToLineEnd => ExecuteDictationVoiceAction(DeleteToLineEndDictationText, "delete to the end of the current line"),
            DictationVoiceAction.SelectPreviousLine => ExecuteDictationVoiceAction(SelectPreviousDictationLine, "select the previous line"),
            DictationVoiceAction.SelectNextLine => ExecuteDictationVoiceAction(SelectNextDictationLine, "select the next line"),
            DictationVoiceAction.DeletePreviousLine => ExecuteDictationVoiceAction(DeletePreviousDictationLine, "delete the previous line"),
            DictationVoiceAction.DeleteNextLine => ExecuteDictationVoiceAction(DeleteNextDictationLine, "delete the next line"),
            DictationVoiceAction.GoToParagraphStart => ExecuteDictationVoiceAction(GoToParagraphStartDictationText, "go to the start of the current paragraph"),
            DictationVoiceAction.GoToParagraphEnd => ExecuteDictationVoiceAction(GoToParagraphEndDictationText, "go to the end of the current paragraph"),
            DictationVoiceAction.SelectToParagraphStart => ExecuteDictationVoiceAction(SelectToParagraphStartDictationText, "select to the start of the current paragraph"),
            DictationVoiceAction.SelectToParagraphEnd => ExecuteDictationVoiceAction(SelectToParagraphEndDictationText, "select to the end of the current paragraph"),
            DictationVoiceAction.DeleteToParagraphStart => ExecuteDictationVoiceAction(DeleteToParagraphStartDictationText, "delete to the start of the current paragraph"),
            DictationVoiceAction.DeleteToParagraphEnd => ExecuteDictationVoiceAction(DeleteToParagraphEndDictationText, "delete to the end of the current paragraph"),
            DictationVoiceAction.NewLine => ExecuteDictationVoiceAction(InsertDictationLineBreak, "insert a new line"),
            DictationVoiceAction.NewParagraph => ExecuteDictationVoiceAction(InsertDictationParagraphBreak, "insert a new paragraph"),
            DictationVoiceAction.NewSentence => ExecuteDictationVoiceAction(InsertDictationSentenceBreak, "insert a new sentence break"),
            DictationVoiceAction.Tab => ExecuteDictationVoiceAction(InsertDictationTab, "insert a tab"),
            DictationVoiceAction.DeleteLastWord => ExecuteDictationVoiceAction(DeleteLastDictationWord, "delete the last word"),
            DictationVoiceAction.GoToPreviousWord => ExecuteDictationVoiceAction(GoToPreviousDictationWord, "go to the previous word"),
            DictationVoiceAction.GoToNextWord => ExecuteDictationVoiceAction(GoToNextDictationWord, "go to the next word"),
            DictationVoiceAction.SelectPreviousWord => ExecuteDictationVoiceAction(SelectPreviousDictationWord, "select the previous word"),
            DictationVoiceAction.SelectNextWord => ExecuteDictationVoiceAction(SelectNextDictationWord, "select the next word"),
            DictationVoiceAction.DeletePreviousWord => ExecuteDictationVoiceAction(DeletePreviousDictationWord, "delete the previous word"),
            DictationVoiceAction.DeleteNextWord => ExecuteDictationVoiceAction(DeleteNextDictationWord, "delete the next word"),
            DictationVoiceAction.SelectPreviousCharacter => ExecuteDictationVoiceAction(SelectPreviousDictationCharacter, "select the previous character"),
            DictationVoiceAction.SelectNextCharacter => ExecuteDictationVoiceAction(SelectNextDictationCharacter, "select the next character"),
            DictationVoiceAction.DeletePreviousCharacter => ExecuteDictationVoiceAction(DeletePreviousDictationCharacter, "delete the previous character"),
            DictationVoiceAction.DeleteNextCharacter => ExecuteDictationVoiceAction(DeleteNextDictationCharacter, "delete the next character"),
            DictationVoiceAction.GoToPreviousSentence => ExecuteDictationVoiceAction(GoToPreviousDictationSentence, "go to the previous sentence"),
            DictationVoiceAction.GoToNextSentence => ExecuteDictationVoiceAction(GoToNextDictationSentence, "go to the next sentence"),
            DictationVoiceAction.SelectPreviousSentence => ExecuteDictationVoiceAction(SelectPreviousDictationSentence, "select the previous sentence"),
            DictationVoiceAction.SelectNextSentence => ExecuteDictationVoiceAction(SelectNextDictationSentence, "select the next sentence"),
            DictationVoiceAction.DeletePreviousSentence => ExecuteDictationVoiceAction(DeletePreviousDictationSentence, "delete the previous sentence"),
            DictationVoiceAction.DeleteNextSentence => ExecuteDictationVoiceAction(DeleteNextDictationSentence, "delete the next sentence"),
            DictationVoiceAction.GoToPreviousParagraph => ExecuteDictationVoiceAction(GoToPreviousDictationParagraph, "go to the previous paragraph"),
            DictationVoiceAction.GoToNextParagraph => ExecuteDictationVoiceAction(GoToNextDictationParagraph, "go to the next paragraph"),
            DictationVoiceAction.SelectPreviousParagraph => ExecuteDictationVoiceAction(SelectPreviousDictationParagraph, "select the previous paragraph"),
            DictationVoiceAction.SelectNextParagraph => ExecuteDictationVoiceAction(SelectNextDictationParagraph, "select the next paragraph"),
            DictationVoiceAction.DeletePreviousParagraph => ExecuteDictationVoiceAction(DeletePreviousDictationParagraph, "delete the previous paragraph"),
            DictationVoiceAction.DeleteNextParagraph => ExecuteDictationVoiceAction(DeleteNextDictationParagraph, "delete the next paragraph"),
            DictationVoiceAction.Comma => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(", "), "insert a comma"),
            DictationVoiceAction.Period => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(". "), "insert a period"),
            DictationVoiceAction.QuestionMark => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("? "), "insert a question mark"),
            DictationVoiceAction.ExclamationMark => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("! "), "insert an exclamation point"),
            DictationVoiceAction.Semicolon => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("; "), "insert a semicolon"),
            DictationVoiceAction.Colon => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(": "), "insert a colon"),
            DictationVoiceAction.Apostrophe => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("'"), "insert an apostrophe"),
            DictationVoiceAction.Quote => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("\""), "insert a quote"),
            DictationVoiceAction.QuoteThat => ExecuteDictationVoiceAction(() => WrapSelectedOrLastDictationPhrase("\"", "\"", "quotes"), "quote the selected or last dictated phrase"),
            DictationVoiceAction.OpenParenthesis => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("("), "insert an open parenthesis"),
            DictationVoiceAction.CloseParenthesis => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(")"), "insert a close parenthesis"),
            DictationVoiceAction.ParenthesizeThat => ExecuteDictationVoiceAction(() => WrapSelectedOrLastDictationPhrase("(", ")", "parentheses"), "put the selected or last dictated phrase in parentheses"),
            DictationVoiceAction.OpenBracket => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("["), "insert an open bracket"),
            DictationVoiceAction.CloseBracket => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("]"), "insert a close bracket"),
            DictationVoiceAction.BracketThat => ExecuteDictationVoiceAction(() => WrapSelectedOrLastDictationPhrase("[", "]", "brackets"), "put the selected or last dictated phrase in brackets"),
            DictationVoiceAction.OpenBrace => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("{"), "insert an open brace"),
            DictationVoiceAction.CloseBrace => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("}"), "insert a close brace"),
            DictationVoiceAction.BraceThat => ExecuteDictationVoiceAction(() => WrapSelectedOrLastDictationPhrase("{", "}", "braces"), "put the selected or last dictated phrase in braces"),
            DictationVoiceAction.Hyphen => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("-"), "insert a hyphen"),
            DictationVoiceAction.Dash => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(" - "), "insert a dash"),
            DictationVoiceAction.Slash => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("/"), "insert a slash"),
            DictationVoiceAction.Backslash => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("\\"), "insert a backslash"),
            DictationVoiceAction.Pipe => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("|"), "insert a pipe"),
            DictationVoiceAction.Grave => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("`"), "insert a grave accent"),
            DictationVoiceAction.Tilde => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("~"), "insert a tilde"),
            DictationVoiceAction.Underscore => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("_"), "insert an underscore"),
            DictationVoiceAction.Plus => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("+"), "insert a plus sign"),
            DictationVoiceAction.Equals => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("="), "insert an equals sign"),
            DictationVoiceAction.Hash => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("#"), "insert a number sign"),
            DictationVoiceAction.Dollar => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("$"), "insert a dollar sign"),
            DictationVoiceAction.Ampersand => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("&"), "insert an ampersand"),
            DictationVoiceAction.Percent => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("%"), "insert a percent sign"),
            DictationVoiceAction.Caret => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("^"), "insert a caret"),
            DictationVoiceAction.Asterisk => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("*"), "insert an asterisk"),
            DictationVoiceAction.AtSign => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("@"), "insert an at sign"),
            DictationVoiceAction.Space => ExecuteDictationVoiceAction(InsertDictationSpace, "insert a space"),
            DictationVoiceAction.NoSpaceThat => ExecuteDictationVoiceAction(RemoveSpaceBeforeLastDictationPhrase, "remove the space before the last dictated phrase"),
            _ => false
        };
        }

        if (AlphaVoiceTranscriptParser.TryParseDictationTargetTextCommand(transcript, out var targetTextCommand) && targetTextCommand != null)
            return ExecuteDictationVoiceAction(
                () => ApplyDictationTargetTextCommand(targetTextCommand),
                targetTextCommand.Action switch
                {
                    DictationTargetTextAction.Select => $"select '{targetTextCommand.TargetText}'",
                    DictationTargetTextAction.MoveBefore => $"move before '{targetTextCommand.TargetText}'",
                    DictationTargetTextAction.MoveAfter => $"move after '{targetTextCommand.TargetText}'",
                    DictationTargetTextAction.Delete => $"delete '{targetTextCommand.TargetText}'",
                    DictationTargetTextAction.Replace => $"replace '{targetTextCommand.TargetText}'",
                    _ => "edit dictated text"
                });

        return false;
    }

    private void ApplyDictationCasingCommand(DictationCasingCommand command)
    {
        _dictationCasingMode = command.Mode;
        RefreshDictationPanel();
        UpdateStatus($"Dictation casing mode set to {FormatDictationCasingMode(command.Mode)}.");
    }

    private static string FormatDictationCasingMode(DictationCasingMode mode) =>
        mode switch
        {
            DictationCasingMode.Caps => "caps on",
            DictationCasingMode.AllCaps => "all caps on",
            DictationCasingMode.NoCaps => "no caps on",
            _ => "normal case"
        };

    private void SelectLastDictationPhrase()
    {
        if (!TryGetLastDictationPhraseSpan(out var start, out var length))
        {
            UpdateStatus("No recent dictated phrase is available to select.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.SelectionStart = start;
        _dictationTextBox.SelectionLength = length;
        RefreshDictationPanel();
        UpdateStatus("Selected the last dictated phrase.");
    }

    private void DeleteLastDictationPhrase()
    {
        if (!TryGetLastDictationPhraseSpan(out var start, out var length))
        {
            UpdateStatus("No recent dictated phrase is available to delete.");
            return;
        }

        _dictationTextBox.Focus();
        _dictationTextBox.Text = _dictationTextBox.Text.Remove(start, length).TrimEnd();
        _dictationTextBox.SelectionStart = Math.Clamp(start, 0, _dictationTextBox.TextLength);
        _dictationTextBox.SelectionLength = 0;
        RefreshDictationPanel();
        UpdateStatus("Deleted the last dictated phrase.");
    }

    private bool TryGetLastDictationPhraseSpan(out int start, out int length)
    {
        start = 0;
        length = 0;
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
            return false;

        var phrase = !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
            ? _dictationLastTranscriptText.Trim()
            : _dictationHistoryEntries.FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(phrase))
            return false;

        var text = _dictationTextBox.Text;
        var index = text.LastIndexOf(phrase, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        start = index;
        length = phrase.Length;
        return length > 0;
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

        if (start >= end)
            return (0, 0);

        return (start, end - start);
    }

    private static (int Start, int Length) GetLastSentenceSpan(string text, bool includeTrailingPunctuation)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0);

        var trimmed = text.TrimEnd();
        var end = trimmed.Length;
        var sentenceStart = 0;
        for (var index = end - 1; index >= 0; index--)
        {
            var ch = trimmed[index];
            if (ch is '.' or '!' or '?')
            {
                sentenceStart = index + 1;
                while (sentenceStart < end && char.IsWhiteSpace(trimmed[sentenceStart]))
                    sentenceStart++;
                break;
            }
        }

        if (sentenceStart >= end)
            return (0, 0);

        if (!includeTrailingPunctuation)
        {
            var selectionEnd = end;
            for (var index = end - 1; index >= sentenceStart; index--)
            {
                if (trimmed[index] is '.' or '!' or '?')
                {
                    selectionEnd = index;
                    break;
                }
            }

            if (selectionEnd <= sentenceStart)
                return (0, 0);

            return (sentenceStart, selectionEnd - sentenceStart);
        }

        return (sentenceStart, end - sentenceStart);
    }

    private static (int Start, int Length) GetNextSentenceSpan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0);

        var trimmed = text.Trim();
        var start = 0;
        for (var index = 0; index < trimmed.Length; index++)
        {
            if (trimmed[index] is '.' or '!' or '?')
            {
                start = index + 1;
                while (start < trimmed.Length && char.IsWhiteSpace(trimmed[start]))
                    start++;
                break;
            }
        }

        if (start >= trimmed.Length)
            return (0, 0);

        var end = trimmed.Length;
        for (var index = start; index < trimmed.Length; index++)
        {
            if (trimmed[index] is '.' or '!' or '?')
            {
                end = index;
                break;
            }
        }

        if (end <= start)
            return (0, 0);

        return (start, end - start);
    }

    private bool ExecuteDictationVoiceAction(Action action, string description, bool captureUndoSnapshot = true)
    {
        var beforeText = _dictationTextBox.Text;
        var beforeSelectionStart = _dictationTextBox.SelectionStart;
        var beforeSelectionLength = _dictationTextBox.SelectionLength;

        action();
        if (captureUndoSnapshot
            && (!string.Equals(beforeText, _dictationTextBox.Text, StringComparison.Ordinal)
            || beforeSelectionStart != _dictationTextBox.SelectionStart
            || beforeSelectionLength != _dictationTextBox.SelectionLength))
        {
            _dictationUndoSnapshot = beforeText;
            _dictationUndoSelectionStart = beforeSelectionStart;
            _dictationUndoSelectionLength = beforeSelectionLength;
            _dictationRedoSnapshot = null;
        }

        UpdateStatus($"Voice command: {description}.");
        return true;
    }

    private void ClearDictationUndoSnapshots()
    {
        _dictationUndoSnapshot = null;
        _dictationRedoSnapshot = null;
        _dictationUndoSelectionStart = 0;
        _dictationUndoSelectionLength = 0;
        _dictationRedoSelectionStart = 0;
        _dictationRedoSelectionLength = 0;
    }

    private void SelectAllDictationText()
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectAll();
        UpdateStatus("Dictation text selected.");
    }

    private void CopyDictationText()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to copy.");
            RecordDictationActionAudit(
                "dictation_copy",
                "failed",
                "copy",
                "empty_review_buffer",
                false,
                "Dictation copy failure was shown in the visible Dictation review surface.");
            return;
        }

        Clipboard.SetText(_dictationTextBox.Text);
        UpdateStatus("Dictated text copied to the clipboard.");
        RecordDictationActionAudit(
            "dictation_copy",
            "succeeded",
            "copy",
            "clipboard_updated",
            true,
            "Dictation copy was shown in the visible Dictation review surface.");
    }

    private void ReadDictationTextAloud()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to read aloud.");
            RecordDictationActionAudit(
                "dictation_readback",
                "failed",
                "readback",
                "empty_review_buffer",
                false,
                "Dictation readback failure was shown in the visible Dictation review surface.");
            return;
        }

        var textToRead = string.IsNullOrWhiteSpace(_dictationTextBox.SelectedText)
            ? _dictationTextBox.Text
            : _dictationTextBox.SelectedText;

        try
        {
            _dictationReadbackSynthesizer ??= new SpeechSynthesizer();
            _dictationReadbackSynthesizer.SpeakAsyncCancelAll();
            _dictationReadbackSynthesizer.SpeakAsync(textToRead);
            var scope = string.IsNullOrWhiteSpace(_dictationTextBox.SelectedText) ? "review_buffer" : "selection";
            UpdateStatus(scope == "selection"
                ? "Reading the selected dictated text aloud locally."
                : "Reading the dictated text aloud locally.");
            RecordDictationActionAudit(
                "dictation_readback",
                "succeeded",
                scope,
                "local_speech_synthesis_started",
                true,
                "Dictation readback was shown in the visible Dictation review surface and used local speech synthesis.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to read dictated text aloud: {ex.Message}");
            RecordDictationActionAudit(
                "dictation_readback",
                "failed",
                "readback",
                ex.Message,
                false,
                "Dictation readback failure was shown in the visible Dictation review surface.");
        }
    }

    private void StopDictationReadback()
    {
        if (_dictationReadbackSynthesizer == null)
        {
            UpdateStatus("No dictation readback is playing.");
            RecordDictationActionAudit(
                "dictation_readback_stop",
                "failed",
                "stop_readback",
                "no_active_readback",
                false,
                "Dictation readback stop was shown in the visible Dictation review surface.");
            return;
        }

        try
        {
            _dictationReadbackSynthesizer.SpeakAsyncCancelAll();
            UpdateStatus("Stopped dictation readback.");
            RecordDictationActionAudit(
                "dictation_readback_stop",
                "succeeded",
                "stop_readback",
                "local_speech_synthesis_cancelled",
                true,
                "Dictation readback stop was shown in the visible Dictation review surface.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to stop dictation readback: {ex.Message}");
            RecordDictationActionAudit(
                "dictation_readback_stop",
                "failed",
                "stop_readback",
                ex.Message,
                false,
                "Dictation readback stop failure was shown in the visible Dictation review surface.");
        }
    }

    private void PasteDictationText()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to paste.");
            RecordDictationActionAudit(
                "dictation_paste",
                "failed",
                "paste",
                "empty_review_buffer",
                false,
                "Dictation paste failure was shown in the visible Dictation review surface.");
            return;
        }

        if (!TryAllowDictationPasteTarget(out var blockedMessage))
        {
            UpdateStatus(blockedMessage);
            if (_dictationStatusLabel != null)
                _dictationStatusLabel.Text = blockedMessage;
            RecordDictationActionAudit(
                "dictation_paste",
                "blocked",
                "paste",
                blockedMessage,
                false,
                "Dictation paste block was shown in the visible Dictation review surface.");
            return;
        }

        Clipboard.SetText(_dictationTextBox.Text);
        SendKeys.SendWait("^v");
        UpdateStatus("Dictated text copied to the clipboard and sent as a paste request.");
        RecordDictationActionAudit(
            "dictation_paste",
            "succeeded",
            "paste",
            "paste_request_sent",
            true,
            "Dictation paste request was shown in the visible Dictation review surface.");
    }

    private static bool TryAllowDictationPasteTarget(out string message)
    {
        message = string.Empty;
        if (!DictationTargetSafetyService.TryGetForegroundTarget(out var target))
            return true;

        if (!DictationTargetSafetyService.IsSensitiveTarget(target, out var reason))
            return true;

        message = $"Dictation paste blocked. {reason} Review the text and paste manually only if this is intended.";
        return false;
    }

    private void OpenBrowserTarget(bool forceSearch = false)
    {
        var input = _browserInputText.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            UpdateStatus("Enter a web address or search phrase first.");
            return;
        }

        if (_browserLaunchService.TryOpen(input, out var message, out var targetUri, forceSearch))
        {
            _browserStatusLabel.Text = $"Opened: {targetUri}";
            _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action: "browser-open", message, succeeded: true);
            UpdateStatus(message);
            return;
        }

        _browserStatusLabel.Text = "Browser target failed.";
        _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action: "browser-open", message, succeeded: false);
        UpdateStatus(message);
    }

    private void CopyBrowserTarget()
    {
        if (string.IsNullOrWhiteSpace(_browserInputText.Text))
        {
            UpdateStatus("Enter a web address or search phrase first.");
            return;
        }

        if (!BrowserLaunchService.TryBuildTargetUri(_browserInputText.Text, out var targetUri, out var reason))
        {
            _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action: "browser-copy", reason, succeeded: false);
            UpdateStatus(reason);
            return;
        }

        Clipboard.SetText(targetUri!.ToString());
        _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action: "browser-copy", message: "Resolved browser target copied to the clipboard.", succeeded: true);
        UpdateStatus("Resolved browser target copied to the clipboard.");
    }

    private void SendBrowserAddressText()
    {
        var text = _browserAddressTextInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            UpdateStatus("Enter address-bar text first.");
            return;
        }

        ExecuteBrowserAction($"browser-address-text:{text}", $"Browser address bar text requested: {text}");
    }

    private void FindBrowserPageText()
    {
        var text = _browserFindTextInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            UpdateStatus("Enter page-find text first.");
            return;
        }

        ExecuteBrowserAction($"browser-find-text:{text}", $"Browser find text requested: {text}");
    }

    private void ExecuteBrowserAction(string action, string statusMessage)
    {
        EnsureActiveProfile(out var profileForAudit);
        if (!_browserLaunchService.TryExecuteBrowserAction(action, out var message))
        {
            _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action, message, succeeded: false);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "browser_action",
                    status: "failed",
                    out _,
                    commandFamily: "browser",
                    actionTarget: action,
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "Browser action failure was shown in the visible Browser tab status.");
            }

            UpdateStatus(message);
            return;
        }

        _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action, statusMessage, succeeded: true);
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "browser_action",
                status: "succeeded",
                out _,
                commandFamily: "browser",
                actionTarget: action,
                details: statusMessage,
                success: true,
                verificationMethod: "visible_status",
                verificationSummary: "Browser action request was shown in the visible Browser tab status.");
        }

        UpdateStatus(statusMessage);
    }

    private void ExecuteSystemAction(string action, string statusMessage)
    {
        EnsureActiveProfile(out var profileForAudit);
        if (action.StartsWith("system-switch-window:", StringComparison.OrdinalIgnoreCase))
        {
            var requestedWindow = action["system-switch-window:".Length..].Trim();
            _systemSwitchWindowText.Text = requestedWindow;
            var resolution = _systemControlService.ResolveVisibleWindow(requestedWindow, ignoredProcessId: Environment.ProcessId);
            if (resolution.IsAmbiguous)
            {
                SetPendingWindowSwitch(resolution);
                _systemStatusLabel.Text = FormatPendingWindowSwitchStatus(resolution);
                _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, resolution.Message, succeeded: false);
                if (profileForAudit != null)
                {
                    _auditLog.TryRecordCommand(
                        profileForAudit,
                        eventType: "alpha.command_execution",
                        actionName: "system_window_switch",
                        status: "blocked",
                        out _,
                        commandFamily: "system",
                        actionTarget: requestedWindow,
                        details: "window_choice_required",
                        success: false,
                        verificationMethod: "visible_status",
                        verificationSummary: "Visible numbered window choices were shown in the System tab before focus changed.");
                }

                UpdateStatus($"{resolution.Message} Say '1', 'click 1', 'choose window 1', 'confirm window', or 'cancel'.");
                RefreshSystemPanel();
                return;
            }

            if (!resolution.IsResolved || resolution.SelectedCandidate == null)
            {
                ClearPendingWindowSwitch();
                var failureMessage = resolution.Message;
                if (_systemStatusLabel != null)
                    _systemStatusLabel.Text = failureMessage;
                _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, failureMessage, succeeded: false);
                if (profileForAudit != null)
                {
                    _auditLog.TryRecordCommand(
                        profileForAudit,
                        eventType: "alpha.command_execution",
                        actionName: "system_window_switch",
                        status: "failed",
                        out _,
                        commandFamily: "system",
                        actionTarget: requestedWindow,
                        details: failureMessage,
                        success: false,
                        verificationMethod: "visible_status",
                        verificationSummary: "Visible window-switch failure was shown in the System tab status.");
                }

                UpdateStatus(failureMessage);
                RefreshSystemPanel();
                return;
            }

            ClearPendingWindowSwitch();
            _systemSwitchWindowText.Text = resolution.SelectedCandidate.DisplayName;
            if (!_systemControlService.TryActivateVisibleWindow(resolution.SelectedCandidate.Handle, out var switchMessage))
            {
                if (_systemStatusLabel != null)
                    _systemStatusLabel.Text = switchMessage;
                _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, switchMessage, succeeded: false);
                if (profileForAudit != null)
                {
                    _auditLog.TryRecordCommand(
                        profileForAudit,
                        eventType: "alpha.command_execution",
                        actionName: "system_window_switch",
                        status: "failed",
                        out _,
                        commandFamily: "system",
                        actionTarget: resolution.SelectedCandidate.DisplayName,
                        details: switchMessage,
                        success: false,
                        verificationMethod: "visible_status",
                        verificationSummary: "Visible window-switch failure was shown in the System tab status.");
                }

                UpdateStatus(switchMessage);
                RefreshSystemPanel();
                return;
            }

            if (_systemStatusLabel != null)
                _systemStatusLabel.Text = switchMessage;
            _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, switchMessage, succeeded: true);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "system_window_switch",
                    status: "succeeded",
                    out _,
                    commandFamily: "system",
                    actionTarget: resolution.SelectedCandidate.DisplayName,
                    details: switchMessage,
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "Visible window-switch request was shown in the System tab status before focus changed.");
            }

            UpdateStatus(switchMessage);
            RefreshSystemPanel();
            return;
        }

        if (!_systemControlService.TryExecute(action, out var message))
        {
            if (_systemStatusLabel != null)
                _systemStatusLabel.Text = message;
            _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, message, succeeded: false);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "system_action",
                    status: "failed",
                    out _,
                    commandFamily: "system",
                    actionTarget: action,
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "System action failure was shown in the visible System tab status.");
            }

            UpdateStatus(message);
            RefreshSystemPanel();
            return;
        }

        if (_systemStatusLabel != null)
            _systemStatusLabel.Text = statusMessage;
        _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, statusMessage, succeeded: true);
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "system_action",
                status: "succeeded",
                out _,
                commandFamily: "system",
                actionTarget: action,
                details: statusMessage,
                success: true,
                verificationMethod: "visible_status",
                verificationSummary: "System action request was shown in the visible System tab status before returning to the session.");
        }

        UpdateStatus(statusMessage);
        RefreshSystemPanel();
    }

    private void ExecuteSystemShellSurfaceAction(string action, string statusMessage)
    {
        var intent = new AlphaVoiceIntent(
            ContainsCallsign: true,
            NormalizedCommand: action,
            Kind: AlphaVoiceIntentKind.SystemControl,
            Target: action);

        if (!TryAuthorizeBuiltInIntent(intent, requireVoiceIdentity: false, out var policyMessage))
        {
            if (_systemStatusLabel != null)
                _systemStatusLabel.Text = policyMessage;
            _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, policyMessage, succeeded: false);
            UpdateStatus(policyMessage);
            RefreshSystemPanel();
            return;
        }

        ExecuteSystemAction(action, statusMessage);
    }

    private void SearchFiles()
    {
        EnsureActiveProfile(out var profileForAudit);
        var query = _fileSearchQueryText.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_search",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: string.Empty,
                    details: "empty_query",
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File search failure was shown in the visible Files tab status.");
            }

            UpdateStatus("Enter a file or folder name to search for.");
            return;
        }

        var report = _fileSearchService.Search(query, maxResults: 50);
        _fileSearchResultsList.BeginUpdate();
        try
        {
            _fileSearchResultsList.Items.Clear();
            foreach (var result in report.Results)
                _fileSearchResultsList.Items.Add(result);
        }
        finally
        {
            _fileSearchResultsList.EndUpdate();
        }

        if (_fileSearchResultsList.Items.Count > 0)
            _fileSearchResultsList.SelectedIndex = 0;

        RefreshFileSearchPanel();

        var message = report.Results.Count == 0
            ? $"No files matched '{query}'."
            : $"Found {report.Results.Count} file result(s) for '{query}' using {report.SearchEngine} search.";

        if (report.Warnings.Count > 0)
            message += $" Warnings: {string.Join(" ", report.Warnings)}";

        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "file_search",
                status: "succeeded",
                out _,
                commandFamily: "file_search",
                actionTarget: query,
                details: $"{report.Results.Count} results",
                success: true,
                verificationMethod: "visible_status",
                verificationSummary: "File search results were shown in the visible Files tab status.");
        }

        UpdateStatus(message);
    }

    private void OpenSelectedFileResult()
    {
        EnsureActiveProfile(out var profileForAudit);
        if (_fileSearchResultsList.SelectedItem is not FileSearchResult result)
        {
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_open",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    details: "no_selected_file_result",
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result open failure was shown in the visible Files tab status.");
            }

            UpdateStatus("Select a file search result first.");
            return;
        }

        if (_fileSearchService.TryOpen(result, out var message))
        {
            _lastLocalFileSearchActionLabel = $"Last action: opened selected result - {result.Name}.";
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_open",
                    status: "succeeded",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: result.FullPath,
                    details: $"selected_result:{result.Name}",
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result open request was shown in the visible Files tab status.");
            }
            RefreshFileSearchPanel();
            UpdateStatus(message);
            return;
        }

        _lastLocalFileSearchActionLabel = "Last action: open selected result was blocked or failed.";
        RefreshFileSearchPanel();
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "file_result_open",
                status: "failed",
                out _,
                commandFamily: "file_search",
                actionTarget: result.FullPath,
                details: message,
                success: false,
                verificationMethod: "visible_status",
                verificationSummary: "File result open failure was shown in the visible Files tab status.");
        }

        UpdateStatus(message);
    }

    private void SelectFileSearchResult(int resultNumber)
    {
        EnsureActiveProfile(out var profileForAudit);
        if (!TrySelectFileSearchResult(resultNumber, out var result, out var message))
        {
            UpdateStatus(message);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_select",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: $"result_{resultNumber}",
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result selection failure was shown in the visible Files tab status.");
            }

            return;
        }

        _tabs.SelectedTab = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals(page.Text, "Files", StringComparison.OrdinalIgnoreCase)) ?? _tabs.SelectedTab;
        _lastLocalFileSearchActionLabel = $"Last action: selected result {resultNumber} - {result!.Name}.";
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "file_result_select",
                status: "succeeded",
                out _,
                commandFamily: "file_search",
                actionTarget: result.FullPath,
                success: true,
                verificationMethod: "visible_status",
                verificationSummary: "File result selection was shown in the visible Files tab status.");
        }

        RefreshFileSearchPanel();
        UpdateStatus($"Selected file search result {resultNumber}: '{result.Name}'.");
    }

    private void OpenFileSearchResultByNumber(int resultNumber)
    {
        EnsureActiveProfile(out var profileForAudit);
        if (!TrySelectFileSearchResult(resultNumber, out var result, out var message))
        {
            UpdateStatus(message);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_open",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: $"result_{resultNumber}",
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result open failure was shown in the visible Files tab status.");
            }

            return;
        }

        if (result == null)
        {
            UpdateStatus($"File search result {resultNumber} could not be read.");
            return;
        }

        if (_fileSearchService.TryOpen(result, out message))
        {
            _lastLocalFileSearchActionLabel = $"Last action: opened result {resultNumber} - {result.Name}.";
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_open",
                    status: "succeeded",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: result.FullPath,
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result open request was shown in the visible Files tab status.");
            }

            RefreshFileSearchPanel();
            UpdateStatus(message);
            return;
        }

        _lastLocalFileSearchActionLabel = $"Last action: open result {resultNumber} was blocked or failed.";
        RefreshFileSearchPanel();
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "file_result_open",
                status: "failed",
                out _,
                commandFamily: "file_search",
                actionTarget: result.FullPath,
                details: message,
                success: false,
                verificationMethod: "visible_status",
                verificationSummary: "File result open failure was shown in the visible Files tab status.");
        }

        UpdateStatus(message);
    }

    private void RevealFileSearchResultByNumber(int resultNumber)
    {
        EnsureActiveProfile(out var profileForAudit);
        if (!TrySelectFileSearchResult(resultNumber, out var result, out var message))
        {
            UpdateStatus(message);
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_reveal",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: $"result_{resultNumber}",
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result reveal failure was shown in the visible Files tab status.");
            }

            return;
        }

        if (result == null)
        {
            UpdateStatus($"File search result {resultNumber} could not be read.");
            return;
        }

        if (_fileSearchService.TryReveal(result, out message))
        {
            _lastLocalFileSearchActionLabel = $"Last action: revealed result {resultNumber} - {result.Name}.";
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_reveal",
                    status: "succeeded",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: result.FullPath,
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result reveal request was shown in the visible Files tab status.");
            }

            RefreshFileSearchPanel();
            UpdateStatus(message);
            return;
        }

        _lastLocalFileSearchActionLabel = $"Last action: reveal result {resultNumber} failed.";
        RefreshFileSearchPanel();
        if (profileForAudit != null)
        {
            _auditLog.TryRecordCommand(
                profileForAudit,
                eventType: "alpha.command_execution",
                actionName: "file_result_reveal",
                status: "failed",
                out _,
                commandFamily: "file_search",
                actionTarget: result.FullPath,
                details: message,
                success: false,
                verificationMethod: "visible_status",
                verificationSummary: "File result reveal failure was shown in the visible Files tab status.");
        }

        UpdateStatus(message);
    }

    private bool TrySelectFileSearchResult(int resultNumber, out FileSearchResult? result, out string message)
    {
        result = null;
        if (resultNumber <= 0)
        {
            message = "Choose a file search result number greater than zero.";
            return false;
        }

        if (_fileSearchResultsList.Items.Count == 0)
        {
            message = "Run a file search first, then choose a result number.";
            return false;
        }

        var index = resultNumber - 1;
        if (index < 0 || index >= _fileSearchResultsList.Items.Count)
        {
            message = $"File search result {resultNumber} is not available.";
            return false;
        }

        _fileSearchResultsList.SelectedIndex = index;
        result = _fileSearchResultsList.Items[index] as FileSearchResult;
        if (result == null)
        {
            message = $"File search result {resultNumber} could not be read.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void OpenSelectedFileFolder()
    {
        EnsureActiveProfile(out var profileForAudit);
        if (_fileSearchResultsList.SelectedItem is not FileSearchResult result)
        {
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_reveal",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    details: "no_selected_file_result",
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result reveal failure was shown in the visible Files tab status.");
            }

            UpdateStatus("Select a file search result first.");
            return;
        }

        if (_fileSearchService.TryReveal(result, out var message))
        {
            _lastLocalFileSearchActionLabel = $"Last action: revealed selected result - {result.Name}.";
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_reveal",
                    status: "succeeded",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: result.FullPath,
                    success: true,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result reveal request was shown in the visible Files tab status.");
            }
        }
        else
        {
            _lastLocalFileSearchActionLabel = "Last action: reveal selected result failed.";
            if (profileForAudit != null)
            {
                _auditLog.TryRecordCommand(
                    profileForAudit,
                    eventType: "alpha.command_execution",
                    actionName: "file_result_reveal",
                    status: "failed",
                    out _,
                    commandFamily: "file_search",
                    actionTarget: result.FullPath,
                    details: message,
                    success: false,
                    verificationMethod: "visible_status",
                    verificationSummary: "File result reveal failure was shown in the visible Files tab status.");
            }
        }

        RefreshFileSearchPanel();
        UpdateStatus(message);
    }

    private void CancelSession()
    {
        StopVoiceSampleRecording(commit: false);
        ClearPendingAppConfirmation();
        ClearPendingWindowSwitch();
        _session.Cancel("Session cancelled.");
        RefreshSessionPanel();
        UpdateStatus("Session cancelled.");
        RecordVoiceControlAudit(
            "voice_session_cancel",
            "succeeded",
            _session.State.ToString(),
            "session_cancelled",
            true,
            "Voice session cancel was shown in the visible status surface.");
    }

    private void ResetSession()
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        ClearPendingAppConfirmation();
        ClearPendingWindowSwitch();
        _session.Reset();
        RefreshSessionPanel();
        UpdateStatus("Session reset to idle.");
        RecordVoiceControlAudit(
            "voice_session_reset",
            "succeeded",
            _session.State.ToString(),
            "session_reset",
            true,
            "Voice session reset was shown in the visible status surface.");
    }

    private IReadOnlyList<string> GetWakeCalibrationSamplePaths(UserProfile profile)
    {
        var wakeFolder = Path.Combine(_profileStore.ResolveCallsSignFolder(profile.Callsign), "wake-samples");
        if (!Directory.Exists(wakeFolder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(wakeFolder, "wake-*.wav", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void OpenProfileFolder()
    {
        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            UpdateStatus("Select an account to open its folder.");
            return;
        }

        var folder = _profileStore.ResolveCallsSignFolder(_activeProfile.Callsign);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
            UpdateStatus($"Opened data folder for '{_activeProfile.Callsign}'.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to open data folder: {ex.Message}");
        }
    }

    private void OpenLogsFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Logs");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true
            });
            UpdateStatus($"Opened Callsign logs folder in Explorer: '{folder}'.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to open logs folder: {ex.Message}");
        }
    }

    private void OpenInstalledAppFolder()
    {
        var folder = GetInstalledAppDirectory();
        if (!Directory.Exists(folder))
        {
            UpdateStatus($"Installed app folder is not available: '{folder}'.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true
            });
            UpdateStatus($"Opened Callsign app folder in Explorer: '{folder}'.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to open app folder: {ex.Message}");
        }
    }

    private void OpenPacksFolder()
    {
        var folder = CallsignCommandRegistry.Shared.PackRoot;
        Directory.CreateDirectory(folder);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true
            });
            UpdateStatus($"Opened extension pack folder: '{folder}'.");
            RecordExtensionPackUiAudit(
                "extension_pack_open_folder",
                "succeeded",
                folder,
                "opened_folder",
                true,
                "Extension pack folder request was shown in the visible Packs surface.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to open pack folder: {ex.Message}");
            RecordExtensionPackUiAudit(
                "extension_pack_open_folder",
                "failed",
                folder,
                ex.Message,
                false,
                "Extension pack folder failure was shown in the visible Packs surface.");
        }
    }

    private void ImportCommunityPack()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import Callsign command pack",
            Filter = "Callsign command pack (*.dll)|*.dll",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            RecordExtensionPackUiAudit(
                "extension_pack_import",
                "cancelled",
                "file_dialog",
                "user_cancelled",
                false,
                "Extension pack import cancellation was shown in the visible Packs surface.");
            return;
        }

        ImportCommunityPacks(dialog.FileNames);
    }

    private void ImportCommunityPackFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a folder that contains one or more Callsign command pack DLLs.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            RecordExtensionPackUiAudit(
                "extension_pack_import_folder",
                "cancelled",
                "folder_dialog",
                "user_cancelled",
                false,
                "Extension pack folder import cancellation was shown in the visible Packs surface.");
            return;
        }

        ImportCommunityPacks(CallsignCommandRegistry.ExpandImportablePackPaths(new[] { dialog.SelectedPath }));
    }

    private void PacksDrop(Control target, DragEventArgs e)
    {
        if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var data = e.Data.GetData(DataFormats.FileDrop);
        if (data is not string[] droppedPaths || droppedPaths.Length == 0)
            return;

        var paths = CallsignCommandRegistry.ExpandImportablePackPaths(droppedPaths).ToArray();
        if (paths.Length == 0)
        {
            UpdateStatus("Drag and drop currently supports .dll files or folders containing .dll files.");
            RecordExtensionPackUiAudit(
                "extension_pack_drag_drop",
                "failed",
                "drag_drop",
                "no_importable_dlls",
                false,
                "Extension pack drag-and-drop failure was shown in the visible Packs surface.");
            MessageBox.Show(
                this,
                "Drop packages: .dll files or folders containing .dll files.",
                "No command pack files",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var message = target == _packsDropZoneLabel
            ? "Command pack dropped on the visible Packs drop zone. It will be imported disabled for review."
            : target == _packsList
                ? "Command pack dropped to the pack list. It will be imported disabled for review."
                : "Command pack dropped. It will be imported disabled for review.";
        UpdateStatus(message);
        RecordExtensionPackUiAudit(
            "extension_pack_drag_drop",
            "succeeded",
            $"{paths.Length} path(s)",
            "drop_accepted",
            true,
            "Extension pack drag-and-drop was shown in the visible Packs surface.");
        ImportCommunityPacks(paths);
    }

    private static void PacksDropEnter(DragEventArgs e)
    {
        if (e.Data != null
            && e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] paths
            && paths.Any(path =>
                (File.Exists(path) && Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                || Directory.Exists(path)))
        {
            e.Effect = DragDropEffects.Copy;
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void ImportCommunityPacks(IEnumerable<string> sourcePackPaths)
    {
        var packPaths = sourcePackPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (packPaths.Length == 0)
        {
            UpdateStatus("No command pack paths were provided.");
            RecordExtensionPackUiAudit(
                "extension_pack_import",
                "failed",
                "import",
                "no_paths",
                false,
                "Extension pack import failure was shown in the visible Packs surface.");
            MessageBox.Show(this, "No command pack files were found.", "Command pack import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var successes = new List<string>();
        var failures = new List<string>();

        try
        {
            foreach (var path in packPaths)
            {
                var result = CallsignCommandRegistry.Shared.ImportPack(path, enableImmediately: false);
                if (result.Succeeded)
                {
                    successes.Add($"{Path.GetFileName(path)}");
                }
                else
                {
                    failures.Add($"{Path.GetFileName(path)}: {result.Message}");
                }
            }

            var importedPacks = CallsignCommandRegistry.Shared.GetPacks()
                .Where(pack =>
                    pack.WasImported
                    && packPaths.Any(path => string.Equals(Path.GetFullPath(pack.AssemblyPath), path, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            RefreshPacksPanel(forceReload: true, preferredPackId: successes.Count > 0 ? importedPacks.FirstOrDefault()?.PackId : null);

            if (successes.Count > 0)
            {
                UpdateStatus($"Imported {successes.Count} command pack(s). Review and enable each before running commands.");
                RecordExtensionPackUiAudit(
                    "extension_pack_import",
                    "succeeded",
                    $"{successes.Count} pack(s)",
                    "imported_disabled_by_default",
                    true,
                    "Extension pack import was shown in the visible Packs surface with review-before-enable status.");
            }

            if (failures.Count > 0)
            {
                RecordExtensionPackUiAudit(
                    "extension_pack_import",
                    successes.Count > 0 ? "partial" : "failed",
                    $"{failures.Count} failure(s)",
                    string.Join("; ", failures),
                    successes.Count > 0,
                    "Extension pack import failure was shown in the visible Packs surface.");
                MessageBox.Show(
                    this,
                    $"{failures.Count} command pack(s) failed to import.{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, failures)}",
                    "Command pack import failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (importedPacks.Length > 0)
                ShowPackImportSplash(BuildPackImportManifest(importedPacks, CallsignCommandRegistry.Shared));
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to import command pack(s): {ex.Message}");
            RecordExtensionPackUiAudit(
                "extension_pack_import",
                "failed",
                "import",
                ex.Message,
                false,
                "Extension pack import failure was shown in the visible Packs surface.");
            MessageBox.Show(this, ex.Message, "Command pack import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public static CallsignUpdateManifest BuildPackImportManifest(IReadOnlyList<CallsignPackInfo> importedPacks, CallsignCommandRegistry? registry = null)
    {
        var packList = importedPacks
            .Where(pack => pack.WasImported)
            .OrderBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        registry ??= CallsignCommandRegistry.Shared;
        var packIds = new HashSet<string>(packList.Select(pack => pack.PackId), StringComparer.OrdinalIgnoreCase);
        var addedCommands = registry.GetCommands()
            .Where(command => packIds.Contains(command.PackId))
            .Select(command => new CallsignUpdateCommandChange(
                command.CommandId,
                command.CommandDisplayName,
                command.Definition.Category ?? string.Empty,
                command.Definition.HelpText ?? command.Definition.Description,
                command.Tier))
            .OrderBy(change => change.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var extensionPackChanges = packList
            .Select(pack => new CallsignUpdateExtensionChange(
                pack.PackId,
                pack.DisplayName,
                pack.Version,
                pack.Tier,
                BuildPackImportSummary(pack),
                pack.SignatureStatus))
            .ToArray();

        var summary = packList.Length switch
        {
            0 => "Imported command pack metadata was reviewed locally.",
            1 => $"Imported {packList[0].DisplayName}. Review and enable it from Packs.",
            _ => $"Imported {packList.Length} command packs. Review and enable each from Packs."
        };

        return new CallsignUpdateManifest(
            Version: $"pack-import-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            InstallerUrl: string.Empty,
            InstallerSha256: string.Empty,
            InstallerSizeBytes: 0,
            ReleaseNotes: summary,
            AddedCommands: addedCommands,
            ExtensionPackChanges: extensionPackChanges,
            SplashSummary: summary,
            PublishedUtc: DateTimeOffset.UtcNow);
    }

    private void RemoveSelectedPack()
    {
        if (_packsList?.SelectedItem is not PackListItem packItem)
        {
            UpdateStatus("Select a pack first.");
            RecordExtensionPackUiAudit(
                "extension_pack_remove",
                "failed",
                "remove",
                "no_pack_selected",
                false,
                "Extension pack removal failure was shown in the visible Packs surface.");
            return;
        }

        var pack = packItem.Pack;
        var message = $"Are you sure you want to remove '{pack.DisplayName}'?";
        if (pack.IsCommunity)
            message += " This unregisters the pack from this client.";

        var confirm = MessageBox.Show(
            this,
            message,
            "Remove command pack",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
        {
            UpdateStatus("Pack removal cancelled.");
            RecordExtensionPackUiAudit(
                "extension_pack_remove",
                "cancelled",
                pack.PackId,
                "user_cancelled",
                false,
                "Extension pack removal cancellation was shown in the visible Packs surface.");
            return;
        }

        var removed = CallsignCommandRegistry.Shared.RemovePack(
            pack.PackId,
            out var removeMessage,
            deleteAssemblyFile: false);
        if (!removed)
        {
            MessageBox.Show(
                this,
                string.IsNullOrWhiteSpace(removeMessage)
                    ? "The selected command pack could not be removed."
                    : removeMessage,
                "Unable to remove pack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            UpdateStatus("Command pack removal failed.");
            RecordExtensionPackUiAudit(
                "extension_pack_remove",
                "failed",
                pack.PackId,
                string.IsNullOrWhiteSpace(removeMessage) ? "remove_failed" : removeMessage,
                false,
                "Extension pack removal failure was shown in the visible Packs surface.");
            return;
        }

        RefreshPacksPanel(forceReload: true);
        UpdateStatus($"Removed pack '{pack.DisplayName}'.");
        RecordExtensionPackUiAudit(
            "extension_pack_remove",
            "succeeded",
            pack.PackId,
            "removed_or_unregistered",
            true,
            "Extension pack removal was shown in the visible Packs surface.");
    }

    private void RefreshPacksPanel(bool forceReload = false, string? preferredPackId = null)
    {
        if (_packsRootLabel == null || _packsStatusLabel == null || _packsList == null || _packCommandsList == null)
            return;

        if (forceReload)
            RefreshCommandRegistry();

        var packs = CallsignCommandRegistry.Shared.GetPacks();
        _packsRootLabel.Text = CallsignCommandRegistry.Shared.PackRoot;
        _packsStatusLabel.Text = packs.Count == 0
            ? "No packs were discovered. Import a DLL via the button or drag-and-drop into the list, then refresh."
            : $"{packs.Count} pack(s) discovered.";

        _packsList.BeginUpdate();
        try
        {
            _packsList.Items.Clear();
            foreach (var pack in packs)
                _packsList.Items.Add(new PackListItem(pack));
        }
        finally
        {
            _packsList.EndUpdate();
        }

        var preferredIndex = FindPreferredPackIndex(packs, preferredPackId);
        if (preferredIndex >= 0)
            _packsList.SelectedIndex = preferredIndex;

        if (_packsList.Items.Count > 0 && _packsList.SelectedIndex < 0)
            _packsList.SelectedIndex = 0;

        RefreshSelectedPackCommands();
    }

    private void RefreshUpdatesPanel()
    {
        if (_updatesServerLabel == null || _updatesCadenceLabel == null || _updatesStateLabel == null || _updatesPendingLabel == null)
            return;

        _updatesServerLabel.Text = $"Update server: {_updateCheckService.ServerUrl} ({_updateCheckService.Channel})";
        _updatesCadenceLabel.Text = $"Cadence: checks on startup and every {_updateCheckService.CheckInterval.TotalHours:0} hours while Callsign is running.";
        _updatesStateLabel.Text = _updateCheckService.DescribeStatus(DateTimeOffset.UtcNow);
        var pendingManifest = _updateCheckService.PendingManifest;
        _updatesPendingLabel.Text = pendingManifest == null
            ? "Pending update: none yet."
            : $"Pending update: v{pendingManifest.Version}; installer {(string.IsNullOrWhiteSpace(pendingManifest.InstallerUrl) ? "not staged" : "staged or downloading")}; splash summary {(string.IsNullOrWhiteSpace(pendingManifest.SplashSummary) ? "not provided" : "available")}.";
    }

    private void RefreshSelectedPackCommands()
    {
        if (_packsList == null || _packCommandsList == null || _packsSelectedSummaryLabel == null || _packsEnablementLabel == null)
            return;

        _packCommandsList.BeginUpdate();
        try
        {
            _packCommandsList.Items.Clear();
            if (_packsList.SelectedItem is not PackListItem packItem)
            {
                _packsSelectedSummaryLabel.Text = "Selected pack: none. Tier, signature, source, and command-gate details will appear here.";
                _packsEnablementLabel.Text = "Enablement readiness: select a pack to see whether commands can run, are disabled for review, or are blocked by signature or entitlement.";
                return;
            }

            _packsSelectedSummaryLabel.Text = $"Selected pack: {packItem.Pack.DisplayName} v{packItem.Pack.Version}. {FormatPackSecuritySummary(packItem.Pack)}";
            _packsEnablementLabel.Text = FormatPackEnablementReadiness(packItem.Pack);
            _packCommandsList.Items.Add($"Pack: {packItem.Pack.Message}");
            _packCommandsList.Items.Add(FormatPackSecuritySummary(packItem.Pack));
            _packCommandsList.Items.Add(FormatPackEnablementReadiness(packItem.Pack));
            _packCommandsList.Items.Add(string.Empty);

            foreach (var command in CallsignCommandRegistry.Shared.GetCommands().Where(command => string.Equals(command.PackId, packItem.Pack.PackId, StringComparison.OrdinalIgnoreCase)))
            {
                var phrase = command.Definition.VoicePhrases.FirstOrDefault() ?? command.CommandDisplayName;
                _packCommandsList.Items.Add($"{phrase} -> {command.CommandDisplayName}");
                _packCommandsList.Items.Add($"  {command.Definition.Kind}; {command.Definition.RiskTier}; {command.Definition.PrivacyImpact}; {command.Definition.ApprovalRequirement}; {command.Definition.VisibilityRequirement}");
            }
        }
        finally
        {
            _packCommandsList.EndUpdate();
        }
    }

    private void ToggleSelectedPack(bool enabled)
    {
        if (_packsList?.SelectedItem is not PackListItem packItem)
        {
            UpdateStatus("Select a pack first.");
            RecordExtensionPackUiAudit(
                enabled ? "extension_pack_enable" : "extension_pack_disable",
                "failed",
                enabled ? "enable" : "disable",
                "no_pack_selected",
                false,
                "Extension pack enablement failure was shown in the visible Packs surface.");
            return;
        }

        var changed = enabled
            ? CallsignCommandRegistry.Shared.EnablePack(packItem.Pack.PackId)
            : CallsignCommandRegistry.Shared.DisablePack(packItem.Pack.PackId);

        if (!changed)
        {
            UpdateStatus($"Pack '{packItem.Pack.DisplayName}' could not be updated.");
            RecordExtensionPackUiAudit(
                enabled ? "extension_pack_enable" : "extension_pack_disable",
                "failed",
                packItem.Pack.PackId,
                "state_unchanged",
                false,
                "Extension pack enablement failure was shown in the visible Packs surface.");
            return;
        }

        RefreshPacksPanel(forceReload: false);
        UpdateStatus(enabled
            ? $"Enabled pack '{packItem.Pack.DisplayName}'."
            : $"Disabled pack '{packItem.Pack.DisplayName}'.");
        RecordExtensionPackUiAudit(
            enabled ? "extension_pack_enable" : "extension_pack_disable",
            "succeeded",
            packItem.Pack.PackId,
            enabled ? "enabled" : "disabled",
            true,
            "Extension pack enablement change was shown in the visible Packs surface.");
    }

    private void ShowShortcutsTab()
    {
        var shortcutTab = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals(page.Text, "Shortcuts", StringComparison.OrdinalIgnoreCase));
        if (shortcutTab == null)
            return;

        _tabs.SelectedTab = shortcutTab;
        RefreshVoiceShortcutsPanel();
        UpdateStatus("Opened voice shortcuts.");
    }

    private void RefreshVoiceShortcutsPanel()
    {
        if (_voiceShortcutsList == null
            || _voiceShortcutStatusLabel == null
            || _voiceShortcutActionsList == null
            || _voiceShortcutTitleText == null
            || _voiceShortcutPhraseText == null
            || _voiceShortcutGroupText == null)
        {
            return;
        }

        var shortcuts = _voiceShortcutStore.GetShortcuts();
        _voiceShortcutStatusLabel.Text = shortcuts.Count == 0
            ? "No voice shortcuts saved yet. Create one from visible Callsign commands."
            : $"{shortcuts.Count} voice shortcut(s) saved. Shortcuts can run up to {VoiceShortcutConstants.MaxActionsPerShortcut} actions.";

        _voiceShortcutsList.BeginUpdate();
        try
        {
            _voiceShortcutsList.Items.Clear();
            foreach (var shortcut in shortcuts)
                _voiceShortcutsList.Items.Add(new VoiceShortcutListItem(shortcut));
        }
        finally
        {
            _voiceShortcutsList.EndUpdate();
        }

        if (_selectedVoiceShortcut != null)
        {
            var selectedIndex = shortcuts
                .Select((shortcut, index) => new { shortcut, index })
                .FirstOrDefault(item => string.Equals(item.shortcut.ShortcutId, _selectedVoiceShortcut.ShortcutId, StringComparison.OrdinalIgnoreCase))
                ?.index ?? -1;
            if (selectedIndex >= 0)
                _voiceShortcutsList.SelectedIndex = selectedIndex;
        }

        if (_voiceShortcutsList.Items.Count == 0)
        {
            CreateNewVoiceShortcut();
            return;
        }

        if (_voiceShortcutsList.SelectedIndex < 0)
            _voiceShortcutsList.SelectedIndex = 0;
    }

    private void SelectVoiceShortcutFromList()
    {
        if (_voiceShortcutsList?.SelectedItem is not VoiceShortcutListItem item)
            return;

        _selectedVoiceShortcut = item.Shortcut;
        _voiceShortcutTitleText.Text = item.Shortcut.Title;
        _voiceShortcutPhraseText.Text = item.Shortcut.WhenISay;
        _voiceShortcutGroupText.Text = item.Shortcut.Group;
        _voiceShortcutDraftActions.Clear();
        _voiceShortcutDraftActions.AddRange(item.Shortcut.Actions);
        RefreshVoiceShortcutActionList();
    }

    private void CreateNewVoiceShortcut()
    {
        _selectedVoiceShortcut = _voiceShortcutStore.CreateDraft();
        _voiceShortcutTitleText.Text = string.Empty;
        _voiceShortcutPhraseText.Text = string.Empty;
        _voiceShortcutGroupText.Text = "General";
        _voiceShortcutCommandActionText.Text = string.Empty;
        _voiceShortcutWaitMilliseconds.Value = 1000;
        _voiceShortcutDraftActions.Clear();
        RefreshVoiceShortcutActionList();
        UpdateStatus("Ready for a new voice shortcut.");
    }

    private void SaveVoiceShortcut()
    {
        var shortcut = BuildVoiceShortcutFromInputs(enabled: _selectedVoiceShortcut?.Enabled ?? true);
        var result = _voiceShortcutStore.Save(shortcut);
        if (!result.Succeeded || result.Shortcut == null)
        {
            UpdateStatus(result.Message);
            MessageBox.Show(this, result.Message, "Voice shortcut not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedVoiceShortcut = result.Shortcut;
        RefreshCommandRegistry();
        RefreshPacksPanel(forceReload: false);
        RefreshVoiceShortcutsPanel();
        UpdateStatus($"Saved voice shortcut '{result.Shortcut.Title}'.");
    }

    private void DeleteSelectedVoiceShortcut()
    {
        if (_selectedVoiceShortcut == null)
        {
            UpdateStatus("Select a voice shortcut first.");
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Delete voice shortcut '{_selectedVoiceShortcut.Title}'?",
            "Delete voice shortcut",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
        {
            UpdateStatus("Voice shortcut deletion cancelled.");
            return;
        }

        if (!_voiceShortcutStore.Delete(_selectedVoiceShortcut.ShortcutId, out var message))
        {
            UpdateStatus(message);
            MessageBox.Show(this, message, "Voice shortcut not deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedVoiceShortcut = null;
        RefreshCommandRegistry();
        RefreshPacksPanel(forceReload: false);
        RefreshVoiceShortcutsPanel();
        UpdateStatus(message);
    }

    private void SetSelectedVoiceShortcutEnabled(bool enabled)
    {
        if (_selectedVoiceShortcut == null)
        {
            UpdateStatus("Select a voice shortcut first.");
            return;
        }

        if (!_voiceShortcutStore.SetEnabled(_selectedVoiceShortcut.ShortcutId, enabled, out var message))
        {
            UpdateStatus(message);
            return;
        }

        RefreshCommandRegistry();
        RefreshPacksPanel(forceReload: false);
        RefreshVoiceShortcutsPanel();
        UpdateStatus(message);
    }

    private void AddVoiceShortcutCommandAction()
    {
        var command = _voiceShortcutCommandActionText.Text.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            UpdateStatus("Enter a Callsign command to add as a shortcut step.");
            return;
        }

        if (_voiceShortcutDraftActions.Count >= VoiceShortcutConstants.MaxActionsPerShortcut)
        {
            UpdateStatus($"Voice shortcuts are limited to {VoiceShortcutConstants.MaxActionsPerShortcut} actions.");
            return;
        }

        _voiceShortcutDraftActions.Add(new VoiceShortcutAction(VoiceShortcutActionKind.Command, command));
        _voiceShortcutCommandActionText.Clear();
        RefreshVoiceShortcutActionList();
        UpdateStatus("Added a command step to the voice shortcut.");
    }

    private void AddVoiceShortcutWaitAction()
    {
        if (_voiceShortcutDraftActions.Count >= VoiceShortcutConstants.MaxActionsPerShortcut)
        {
            UpdateStatus($"Voice shortcuts are limited to {VoiceShortcutConstants.MaxActionsPerShortcut} actions.");
            return;
        }

        _voiceShortcutDraftActions.Add(new VoiceShortcutAction(
            VoiceShortcutActionKind.Wait,
            string.Empty,
            (int)_voiceShortcutWaitMilliseconds.Value));
        RefreshVoiceShortcutActionList();
        UpdateStatus("Added a wait step to the voice shortcut.");
    }

    private void RemoveSelectedVoiceShortcutAction()
    {
        if (_voiceShortcutActionsList.SelectedIndex < 0 || _voiceShortcutActionsList.SelectedIndex >= _voiceShortcutDraftActions.Count)
        {
            UpdateStatus("Select a shortcut action first.");
            return;
        }

        _voiceShortcutDraftActions.RemoveAt(_voiceShortcutActionsList.SelectedIndex);
        RefreshVoiceShortcutActionList();
        UpdateStatus("Removed the selected shortcut action.");
    }

    private void MoveSelectedVoiceShortcutAction(int direction)
    {
        var selectedIndex = _voiceShortcutActionsList.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _voiceShortcutDraftActions.Count)
        {
            UpdateStatus("Select a shortcut action first.");
            return;
        }

        var newIndex = selectedIndex + direction;
        if (newIndex < 0 || newIndex >= _voiceShortcutDraftActions.Count)
            return;

        var action = _voiceShortcutDraftActions[selectedIndex];
        _voiceShortcutDraftActions.RemoveAt(selectedIndex);
        _voiceShortcutDraftActions.Insert(newIndex, action);
        RefreshVoiceShortcutActionList(newIndex);
        UpdateStatus("Moved the selected shortcut action.");
    }

    private void RefreshVoiceShortcutActionList(int selectedIndex = -1)
    {
        _voiceShortcutActionsList.BeginUpdate();
        try
        {
            _voiceShortcutActionsList.Items.Clear();
            for (var i = 0; i < _voiceShortcutDraftActions.Count; i++)
            {
                _voiceShortcutActionsList.Items.Add($"{i + 1}. {VoiceShortcutCommandPack.FormatActionSummary(_voiceShortcutDraftActions[i])}");
            }
        }
        finally
        {
            _voiceShortcutActionsList.EndUpdate();
        }

        if (selectedIndex >= 0 && selectedIndex < _voiceShortcutActionsList.Items.Count)
            _voiceShortcutActionsList.SelectedIndex = selectedIndex;
        else if (_voiceShortcutActionsList.Items.Count > 0)
            _voiceShortcutActionsList.SelectedIndex = _voiceShortcutActionsList.Items.Count - 1;
    }

    private VoiceShortcutDefinition BuildVoiceShortcutFromInputs(bool enabled)
    {
        var existing = _selectedVoiceShortcut ?? _voiceShortcutStore.CreateDraft();
        return existing with
        {
            Title = _voiceShortcutTitleText.Text.Trim(),
            WhenISay = _voiceShortcutPhraseText.Text.Trim(),
            Group = string.IsNullOrWhiteSpace(_voiceShortcutGroupText.Text) ? "General" : _voiceShortcutGroupText.Text.Trim(),
            Enabled = enabled,
            Actions = _voiceShortcutDraftActions.ToArray(),
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private void RunOpenWakeWordSetupHelper()
    {
        var setupScript = Path.Combine(GetInstalledAppDirectory(), "setupopenwakeword.ps1");
        if (!File.Exists(setupScript))
        {
            UpdateStatus($"openWakeWord setup helper was not found: '{setupScript}'. Rebuild or reinstall Callsign.");
            return;
        }

        try
        {
            var confirmation = MessageBox.Show(
                this,
                "Callsign will repair the installed openWakeWord runtime, restore the packaged Callsign wake model, and restart the per-user listener. Continue?",
                "Repair Callsign Wakeword",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (confirmation != DialogResult.OK)
            {
                UpdateStatus("Wake setup cancelled.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{setupScript}\" -InstallPythonPackages -RestartCallsign",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(startInfo);
            UpdateStatus("Opened wakeword repair. Keep the progress window open until it finishes.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to start openWakeWord setup helper: {ex.Message}");
        }
    }

    private void OpenVoiceIdentityTrainingForActiveProfile()
    {
        if ((_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
            && !string.IsNullOrWhiteSpace(_callsignText.Text))
        {
            SaveProfile();
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        OpenVoiceIdentityTraining(profile);
    }

    private void OpenVoiceIdentityTraining(UserProfile profile)
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        using var trainingForm = new VoiceIdentityTrainingForm(_profileStore, profile, _voiceCommandService);
        trainingForm.ShowDialog(this);

        var reloaded = _profileStore.Load(profile.Callsign);
        if (reloaded != null)
        {
            var index = _profiles.FindIndex(p => string.Equals(p.Callsign, reloaded.Callsign, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                _profiles[index] = reloaded;
            _activeProfile = reloaded;
        }

        RefreshAllPanels();
        UpdateStatus($"Voice identity training closed for '{profile.Callsign}'.");
    }

    private void RunPyannoteSetupHelper()
    {
        var setupScript = Path.Combine(GetInstalledAppDirectory(), "setuppyannote.ps1");
        if (!File.Exists(setupScript))
        {
            UpdateStatus($"pyannote setup helper was not found: '{setupScript}'. Rebuild or reinstall Callsign.");
            return;
        }

        try
        {
            var confirmation = MessageBox.Show(
                this,
                "Callsign will repair the bundled pyannote speaker identity runtime and use the packaged model cache when present. Continue?",
                "Repair Callsign Identity Runtime",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (confirmation != DialogResult.OK)
            {
                UpdateStatus("Identity setup cancelled.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{setupScript}\" -InstallPythonPackages -DownloadModel -TestEmbedding",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(startInfo);
            UpdateStatus("Opened identity runtime repair. Keep the PowerShell window open until it finishes.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to start pyannote setup helper: {ex.Message}");
        }
    }

    private bool EnsureActiveProfile(out UserProfile profile)
    {
        profile = _activeProfile ?? new UserProfile();
        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            UpdateStatus("Select or create an account first.");
            return false;
        }

        return true;
    }

    private static string EscapePowerShellArgument(string value) =>
        value.Replace("`", "``", StringComparison.Ordinal)
            .Replace("\"", "`\"", StringComparison.Ordinal);

    private static string GetInstalledAppDirectory()
    {
        var localAppDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "App");
        if (Directory.Exists(localAppDir))
            return localAppDir;

        return AppContext.BaseDirectory;
    }

    private static string GetInstalledWakeModelPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "Models",
            "callsign.onnx");

    private void UpdateProfileMetadata(UserProfile profile)
    {
        profile.DisplayName = _displayNameText.Text.Trim();
        profile.Email = string.IsNullOrWhiteSpace(_emailText.Text) ? null : _emailText.Text.Trim();
        profile.Department = string.IsNullOrWhiteSpace(_departmentText.Text) ? null : _departmentText.Text.Trim();
        profile.Notes = _notesText.Text;
        profile.Callsign = _callsignText.Text.Trim();
    }

    private void SaveVoiceState(UserProfile profile)
    {
        profile.Settings.VoiceSamplesRequired = Math.Max(3, profile.Settings.VoiceSamplesRequired);
        profile.Settings.VoiceSamplesRecorded = Math.Max(0, profile.Settings.VoiceSamplesRecorded);
        if (string.IsNullOrWhiteSpace(profile.Settings.VoiceEnrollmentStatus))
            profile.Settings.VoiceEnrollmentStatus = "Not activated";
        else
            profile.Settings.VoiceEnrollmentStatus = NormalizeVoiceStatus(profile.Settings.VoiceEnrollmentStatus);
    }

    private static string GetVoiceStatusText(UserSettings settings)
    {
        if (settings.VoiceEnrolledUtc.HasValue)
            return $"Activated on {settings.VoiceEnrolledUtc.Value.ToLocalTime():f}. Listener resumes when Callsign opens.";

        return string.IsNullOrWhiteSpace(settings.VoiceEnrollmentStatus)
            ? "Not activated."
            : NormalizeVoiceStatus(settings.VoiceEnrollmentStatus);
    }

    public string VoiceNextStepText => _voiceNextStepLabel.Text;
    public string VoiceFailureText => _voiceFailureLabel.Text;

    private static string GetVoiceNextStepText(UserSettings settings, bool busy)
    {
        if (busy)
            return "Next step: wait for voice activation or repair to finish.";

        var required = Math.Max(3, settings.VoiceSamplesRequired);
        if (settings.VoiceSamplesRecorded < required)
            return $"Next step: record {required - settings.VoiceSamplesRecorded} more fresh sample(s).";

        if (settings.VoiceEnrolledUtc.HasValue)
            return "Next step: voice identity is ready.";

        return "Next step: enroll voice identity.";
    }

    private static string GetVoiceFailureText(UserSettings settings, bool busy, VoiceEnrollmentSampleProof? sampleProof = null)
    {
        if (busy)
            return "Failure type: service.";

        var required = Math.Max(3, settings.VoiceSamplesRequired);
        if (settings.VoiceSamplesRecorded < required)
            return "Failure type: not enough samples yet.";

        if (settings.VoiceEnrolledUtc.HasValue)
            return "Failure type: none.";

        if (sampleProof is { Accepted: false } && sampleProof.SampleCount >= required && VoiceBiometricVerificationService.IsSampleProofRejectReason(sampleProof.RejectReason))
            return VoiceBiometricVerificationService.DescribeEnrollmentFailureType(sampleProof.RejectReason, sampleProof.Message, sampleProof);

        if (string.Equals(settings.VoiceEnrollmentStatus, "pyannote setup required", StringComparison.OrdinalIgnoreCase))
            return "Failure type: identity runtime or model cache.";

        if ((settings.VoiceEnrollmentStatus ?? string.Empty).Contains("collecting sample", StringComparison.OrdinalIgnoreCase))
            return "Failure type: sample collection in progress.";

        return "Failure type: identity runtime, model cache, or service.";
    }

    private static string NormalizeVoiceStatus(string status) =>
        status.Trim() switch
        {
            "Enrolled" => "Activated",
            "Not enrolled" => "Not activated",
            "Ready to train" => "Ready to activate",
            var value when value.StartsWith("Need ", StringComparison.OrdinalIgnoreCase) => value.Replace("sample(s).", "sample(s) before activation.", StringComparison.OrdinalIgnoreCase),
            var value => value
        };

    private static bool IsVoiceEnrolled(UserSettings settings) =>
        settings.VoiceEnrolledUtc.HasValue && settings.VoiceSamplesRecorded >= settings.VoiceSamplesRequired;

    private static string GetVoicePrompt(UserProfile? profile, UserSettings settings)
    {
        var nextSample = Math.Min(settings.VoiceSamplesRecorded + 1, Math.Max(3, settings.VoiceSamplesRequired));
        var callsign = string.IsNullOrWhiteSpace(profile?.Callsign) ? "your callsign" : profile.Callsign;
        return nextSample switch
        {
            1 => $"Sample 1: Say 'Callsign {callsign}'.",
            2 => $"Sample 2: Say 'Callsign {callsign}, open Notepad'.",
            3 => $"Sample 3: Say 'Callsign {callsign}, launch Calculator'.",
            _ => "Review your samples and activate voice control."
        };
    }

    private static string InferAppName(string command)
    {
        var trimmed = command.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        foreach (var prefix in new[]
        {
            "to open ",
            "to launch ",
            "to start ",
            "to run ",
            "to ",
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
        })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefix.Length..].Trim();
        }

        return trimmed;
    }

    private static bool IsWakeOverlaySessionActive(AlphaSessionState state) =>
        state is AlphaSessionState.WaitingForIdentity
            or AlphaSessionState.WaitingForCommand
            or AlphaSessionState.ReadyToLaunch
            or AlphaSessionState.Launching;

    private static bool IsWakeOverlaySessionActive(string? stateText) =>
        Enum.TryParse<AlphaSessionState>(stateText, ignoreCase: true, out var parsedState)
            && IsWakeOverlaySessionActive(parsedState);

    private static string FormatOverlayPhase(string? stateText)
    {
        if (!Enum.TryParse<AlphaSessionState>(stateText, ignoreCase: true, out var parsedState))
            return "Listening";

        return OverlayReadoutFormatter.FormatPhase(parsedState);
    }

    private static string FormatSessionHint(string? stateText, string? verifiedCallsign, string? pendingCommand)
    {
        if (!Enum.TryParse<AlphaSessionState>(stateText, ignoreCase: true, out var state))
            return "Next: say Callsign.";

        return state switch
        {
            AlphaSessionState.Idle => "Next: say Callsign.",
            AlphaSessionState.WaitingForIdentity => "Next: say your callsign.",
            AlphaSessionState.WaitingForCommand when string.IsNullOrWhiteSpace(verifiedCallsign) => "Next: verify your callsign.",
            AlphaSessionState.WaitingForCommand => "Next: say the app name or command.",
            AlphaSessionState.ReadyToLaunch when string.IsNullOrWhiteSpace(pendingCommand) => "Next: say the app name.",
            AlphaSessionState.ReadyToLaunch => $"Next: launch {pendingCommand}.",
            AlphaSessionState.Launching => "Next: wait for the app to open.",
            AlphaSessionState.Completed => "Next: say Callsign to start a new session.",
            AlphaSessionState.LockedOut => "Next: wait for lockout to clear, then try again.",
            _ => "Next: say Callsign."
        };
    }

    private static string GetSessionHintDetails(string? stateText, string? verifiedCallsign, string? pendingCommand)
    {
        if (!Enum.TryParse<AlphaSessionState>(stateText, ignoreCase: true, out var state))
            return "Say Callsign to begin a visible session.";

        return state switch
        {
            AlphaSessionState.Idle => "Say Callsign to begin a visible session.",
            AlphaSessionState.WaitingForIdentity => "Say your callsign after the wake word to verify identity.",
            AlphaSessionState.WaitingForCommand when string.IsNullOrWhiteSpace(verifiedCallsign) => "The session is waiting for identity before it accepts a command.",
            AlphaSessionState.WaitingForCommand => "After identity, speak the app name or command you want to launch.",
            AlphaSessionState.ReadyToLaunch when string.IsNullOrWhiteSpace(pendingCommand) => "Speak the app name to continue.",
            AlphaSessionState.ReadyToLaunch => $"Speak to launch {pendingCommand}.",
            AlphaSessionState.Launching => "The app is launching now.",
            AlphaSessionState.Completed => "The session is done. Say Callsign to start again.",
            AlphaSessionState.LockedOut => "The session is locked out for safety. Wait for it to clear, then try again.",
            _ => "Say Callsign to begin a visible session."
        };
    }

    private static string BuildRuntimeOverlayReadout(RuntimeStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.OverlayReadout))
            return snapshot.OverlayReadout;

        if (!Enum.TryParse<AlphaSessionState>(snapshot.SessionState, ignoreCase: true, out var state))
            return "Listening.";

        return OverlayReadoutFormatter.FormatReadout(
            state,
            snapshot.LastTranscriptText ?? snapshot.LastIdentityTranscript,
            snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : snapshot.LastIdentityConfidence.HasValue ? (float?)snapshot.LastIdentityConfidence.Value : null,
            snapshot.VerifiedCallsign,
            snapshot.PendingCommand,
            snapshot.PendingApp,
            snapshot.LastIdentityRetryPrompt,
            speechActive: snapshot.IsSpeechActive == true,
            dictationTranscript: snapshot.ServiceDictationText,
            dictationActive: snapshot.ServiceDictationActive);
    }

    private static string FormatRuntimeWakeCandidateReadout(RuntimeStateSnapshot snapshot)
    {
        var candidate = !string.IsNullOrWhiteSpace(snapshot.LastTranscriptText)
            ? snapshot.LastTranscriptText.Trim()
            : !string.IsNullOrWhiteSpace(snapshot.LastIdentityTranscript)
                ? snapshot.LastIdentityTranscript.Trim()
                : null;
        return FormatWakeCandidateStatus(candidate, snapshot.LastWakeWordScore, snapshot.WakeWordThreshold);
    }

    private string FormatLocalWakeCandidateReadout()
    {
        var candidate = !string.IsNullOrWhiteSpace(_lastHeardTranscriptText)
            ? _lastHeardTranscriptText.Trim()
            : _voiceCommandService.LastWakeWordDetection?.Detected == true
                ? _session.State is AlphaSessionState.WaitingForIdentity or AlphaSessionState.WaitingForCommand
                    ? _spokenCallsignText.Text.Trim()
                    : _voiceCommandService.LastWakeWordDetection.Engine
                : null;
        return FormatWakeCandidateStatus(
            candidate,
            _voiceCommandService.LastWakeWordDetection?.Score,
            _voiceCommandService.LastWakeWordDetection?.Threshold);
    }

    private static string FormatWakeCandidateStatus(string? candidate, double? score, double? threshold)
    {
        if (candidate == null)
        {
            if (score.HasValue && threshold.HasValue)
                return score.Value >= threshold.Value
                    ? $"Wake candidate: accepted wake score ({score.Value:P0} / {threshold.Value:P0}), but no transcript text was captured."
                    : $"Wake candidate: below threshold ({score.Value:P0} / {threshold.Value:P0}), and no transcript text was captured.";

            return "Wake candidate: nothing heard yet.";
        }

        if (score.HasValue && threshold.HasValue)
        {
            if (score.Value < threshold.Value)
                return "Wake candidate: listening for Callsign.";

            return score.Value >= threshold.Value
                ? $"Wake candidate: heard '{candidate}' ({score.Value:P0} / {threshold.Value:P0}) and it cleared threshold."
                : $"Wake candidate: heard '{candidate}' ({score.Value:P0} / {threshold.Value:P0}) but it stayed below threshold.";
        }

        return $"Wake candidate: heard '{candidate}'.";
    }

    private static AlphaSessionState ParseSessionState(string sessionState)
    {
        return Enum.TryParse<AlphaSessionState>(sessionState, ignoreCase: true, out var state)
            ? state
            : AlphaSessionState.Idle;
    }

    private static double? BuildRuntimeOverlayActivityLevel(RuntimeStateSnapshot snapshot)
    {
        var raw = snapshot.LastMicrophoneRawRms;
        var threshold = snapshot.LastMicrophoneSpeechThresholdRms;
        var noiseFloor = snapshot.LastMicrophoneNoiseFloorRms ?? 0d;
        if (!raw.HasValue && !snapshot.IsSpeechActive.HasValue)
            return null;

        var baseline = Math.Max(threshold ?? 0.01, 0.001);
        var value = Math.Max(raw ?? 0, noiseFloor);
        var normalized = (value - noiseFloor) / Math.Max(baseline - noiseFloor, 0.001);
        if (snapshot.IsSpeechActive == true && normalized < 0.2)
            normalized = 0.2;

        return Math.Clamp(normalized, 0d, 1d);
    }

    private static string BuildRuntimeOverlayActivityText(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.IsSpeechActive == true)
            return $"Mic: live{FormatLiveActivityDuration(snapshot.LastSpeechActivityUtc)}";

        if (snapshot.CanHearAudio == true)
            return "Mic: ready";

        return "Mic: idle";
    }

    private static string? BuildRuntimeOverlayCaptionText(RuntimeStateSnapshot snapshot)
    {
        static string? AppendActionLine(string? heardText, RuntimeStateSnapshot snap)
        {
            if (string.IsNullOrWhiteSpace(snap.LastServiceActionMessage))
                return heardText;

            if (!Enum.TryParse<AlphaSessionState>(snap.SessionState, ignoreCase: true, out var state))
                return heardText;

            if (state is not AlphaSessionState.Launching and not AlphaSessionState.ReadyToLaunch and not AlphaSessionState.Completed)
                return heardText;

            var actionLine = FormatServiceActionLabel(
                snap.LastServiceActionKind,
                snap.LastServiceActionTarget,
                snap.LastServiceActionMessage,
                snap.LastServiceActionSucceeded,
                snap.LastServiceActionUtc);

            return string.IsNullOrWhiteSpace(heardText)
                ? actionLine
                : $"{heardText}{Environment.NewLine}{actionLine}";
        }

        if (!string.IsNullOrWhiteSpace(snapshot.LastTranscriptText))
        {
            var heard = FormatHeardTranscript(snapshot.LastTranscriptText.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null);
            if (snapshot.IsSpeechActive == true)
                return AppendActionLine($"{FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc)}{Environment.NewLine}{heard}", snapshot);

            return AppendActionLine(heard, snapshot);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.LastIdentityTranscript))
        {
            var heard = FormatHeardTranscript(snapshot.LastIdentityTranscript.Trim(), snapshot.LastIdentityConfidence.HasValue ? (float?)snapshot.LastIdentityConfidence.Value : null);
            if (snapshot.IsSpeechActive == true)
                return AppendActionLine($"{FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc)}{Environment.NewLine}{heard}", snapshot);

            return AppendActionLine(heard, snapshot);
        }

        if (snapshot.IsSpeechActive == true)
            return AppendActionLine(FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc), snapshot);

        return AppendActionLine(null, snapshot);
    }

    private static string BuildRuntimeDictationSpeechCueText(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.ServiceDictationActive && snapshot.IsSpeechActive == true)
            return "Speech cue: " + FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc).Replace("Hearing your", "Hearing dictation").Replace("Listening for launch", "Hearing dictation");

        if (snapshot.ServiceDictationActive && !string.IsNullOrWhiteSpace(snapshot.ServiceDictationText))
            return "Speech cue: Dictation text is updating.";

        if (snapshot.ServiceDictationActive)
            return "Speech cue: Dictation is waiting for speech.";

        return "Speech cue: dictation is stopped.";
    }

    private static string BuildRuntimeSessionSpeechCueText(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.IsSpeechActive == true)
        {
            return "Speech cue: " + FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.LastTranscriptText))
            return $"Speech cue: {FormatHeardTranscript(snapshot.LastTranscriptText.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null)}";

        if (!string.IsNullOrWhiteSpace(snapshot.LastIdentityTranscript))
            return $"Speech cue: {FormatHeardTranscript(snapshot.LastIdentityTranscript.Trim(), snapshot.LastIdentityConfidence.HasValue ? (float?)snapshot.LastIdentityConfidence.Value : null)}";

        return "Speech cue: nothing heard yet.";
    }

    private static string FormatRuntimeHeardLabel(RuntimeStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.LastTranscriptText))
            return FormatHeardTranscript(snapshot.LastTranscriptText.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null);

        if (!string.IsNullOrWhiteSpace(snapshot.LastIdentityTranscript))
            return FormatHeardTranscript(snapshot.LastIdentityTranscript.Trim(), snapshot.LastIdentityConfidence.HasValue ? (float?)snapshot.LastIdentityConfidence.Value : null);

        if (snapshot.IsSpeechActive == true)
            return $"Heard: {FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc)}";

        return "Heard: nothing yet.";
    }

    private static string FormatRuntimeCommandLabel(RuntimeStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.PendingCommand))
            return FormatCommandLabel(snapshot.PendingCommand.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null);

        if (!string.IsNullOrWhiteSpace(snapshot.LastTranscriptText))
            return FormatCommandLabel(snapshot.LastTranscriptText.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null);

        return "No command captured.";
    }

    private static string FormatRuntimeLastHeardLabel(RuntimeStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.LastTranscriptText))
            return FormatLastHeardLabel(snapshot.LastTranscriptText.Trim(), snapshot.LastTranscriptConfidence.HasValue ? (float?)snapshot.LastTranscriptConfidence.Value : null);

        if (!string.IsNullOrWhiteSpace(snapshot.LastIdentityTranscript))
            return FormatLastHeardLabel(snapshot.LastIdentityTranscript.Trim(), snapshot.LastIdentityConfidence.HasValue ? (float?)snapshot.LastIdentityConfidence.Value : null);

        if (snapshot.IsSpeechActive == true)
            return $"Last heard: {FormatLiveHearingCue(ParseSessionState(snapshot.SessionState), snapshot.LastSpeechActivityUtc)}";

        return "Last heard: nothing yet.";
    }

    private static string FormatRuntimeLastActionLabel(RuntimeStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.LastServiceActionMessage))
            return FormatServiceActionLabel(
                snapshot.LastServiceActionKind,
                snapshot.LastServiceActionTarget,
                snapshot.LastServiceActionMessage,
                snapshot.LastServiceActionSucceeded,
                snapshot.LastServiceActionUtc);

        if (snapshot.RecentServiceActions is { Count: > 0 })
        {
            var latest = snapshot.RecentServiceActions[^1];
            return FormatServiceActionLabel(latest.Kind, latest.Target, latest.Message, latest.Succeeded, latest.Utc);
        }

        return "Last action: none yet.";
    }

    private static string FormatServiceActionLabel(string? kind, string? target, string message, bool? succeeded, DateTime? utc)
    {
        var status = succeeded == true
            ? "Success"
            : succeeded == false
                ? "Failed"
                : "Status unknown";

        var details = string.Join(" ", new[]
        {
            string.IsNullOrWhiteSpace(kind) ? null : kind,
            string.IsNullOrWhiteSpace(target) ? null : $"on {target}",
            utc.HasValue ? $"at {utc.Value.ToLocalTime():h:mm tt}" : null
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(details)
            ? $"Last action: {status}. {message}"
            : $"Last action: {status}. {details}. {message}";
    }

    private string FormatLocalSystemActionLabel(string action, string message, bool succeeded)
    {
        var status = succeeded ? "Success" : "Failed";
        return $"Last action: {status}. {action}. {message}";
    }

    private string FormatLocalCommandLabel()
    {
        if (!string.IsNullOrWhiteSpace(_session.PendingCommand))
            return FormatCommandLabel(_session.PendingCommand.Trim(), _lastHeardTranscriptConfidence);

        if (!string.IsNullOrWhiteSpace(_lastHeardTranscriptText))
            return FormatCommandLabel(_lastHeardTranscriptText.Trim(), _lastHeardTranscriptConfidence);

        return "No command captured.";
    }

    private string BuildLocalOverlayReadout(string? latestTranscript = null, float? confidence = null)
    {
        if (_dictationActive)
            latestTranscript ??= _dictationTextBox.Text;

        return OverlayReadoutFormatter.FormatReadout(
            _session.State,
            latestTranscript ?? _spokenCallsignText.Text,
            confidence ?? _lastHeardTranscriptConfidence,
            _session.VerifiedCallsign,
            _session.PendingCommand,
            _session.PendingApp,
            speechActive: _voiceCommandService.IsSpeechActive,
            dictationTranscript: _dictationActive ? _dictationTextBox.Text : null,
            dictationActive: _dictationActive);
    }

    private string BuildLocalDictationReadout(string? latestTranscript = null, float? confidence = null)
    {
        latestTranscript ??= _dictationTextBox.Text;
        return OverlayReadoutFormatter.FormatReadout(
            _session.State,
            latestTranscript,
            confidence ?? _lastHeardTranscriptConfidence,
            _session.VerifiedCallsign,
            _session.PendingCommand,
            _session.PendingApp,
            speechActive: _voiceCommandService.IsSpeechActive,
            dictationTranscript: latestTranscript,
            dictationActive: true);
    }

    private string BuildLocalDictationOverlayCaptionText(string? latestTranscript = null)
    {
        latestTranscript ??= _dictationTextBox.Text;
        if (!string.IsNullOrWhiteSpace(latestTranscript))
            return $"Dictation: {latestTranscript.Trim()}";

        return _voiceCommandService.IsSpeechActive
            ? $"Dictation: {FormatLiveHearingCue(_session.State, _voiceCommandService.LastSpeechActivityUtc).Replace("Hearing your", "hearing").Replace("Listening for launch", "hearing")}"
            : "Dictation: waiting for speech.";
    }

    private double? BuildLocalOverlayActivityLevel()
    {
        var telemetry = _voiceCommandService.CurrentAudioTelemetry;
        if (telemetry == null)
            return _voiceCommandService.IsSpeechActive ? 0.25 : null;

        var baseline = Math.Max(telemetry.SpeechThresholdRms, 0.001);
        var value = Math.Max(telemetry.ProcessedRms, telemetry.RawRms);
        var normalized = (value - telemetry.NoiseFloorRms) / Math.Max(baseline - telemetry.NoiseFloorRms, 0.001);
        if (_voiceCommandService.IsSpeechActive && normalized < 0.2)
            normalized = 0.2;

        return Math.Clamp(normalized, 0d, 1d);
    }

    private string BuildLocalOverlayActivityText()
    {
        var telemetry = _voiceCommandService.CurrentAudioTelemetry;
        if (_voiceCommandService.IsSpeechActive)
            return telemetry?.LevelState == "Good"
                ? $"Mic: live{FormatLiveActivityDuration(_voiceCommandService.LastSpeechActivityUtc)}"
                : $"Mic: hearing{FormatLiveActivityDuration(_voiceCommandService.LastSpeechActivityUtc)}";

        if (telemetry != null)
            return $"Mic: {telemetry.LevelState.ToLowerInvariant()}";

        return "Mic: idle";
    }

    private string BuildLocalActivityTextForWakeOverlay() => BuildLocalOverlayActivityText();

    private string BuildWakeOverlayAuthorityText(RuntimeStateSnapshot? runtimeSnapshot = null)
        => RuntimeStatusFormatter.FormatAuthority(runtimeSnapshot, _voiceCommandService.IsListening, _usingLocalPreviewListener);

    private string? BuildLocalOverlayCaptionText(AlphaSessionState state, bool speechActive, DateTime? lastSpeechActivityUtc = null, string? latestTranscript = null, float? confidence = null)
    {
        string? AppendLocalActionLine(string? captionText)
        {
            if (state is not AlphaSessionState.Launching and not AlphaSessionState.ReadyToLaunch and not AlphaSessionState.Completed)
                return captionText;

            if (string.IsNullOrWhiteSpace(_lastLocalSystemActionLabel) || _lastLocalSystemActionLabel.Contains("none yet.", StringComparison.OrdinalIgnoreCase))
                return captionText;

            return string.IsNullOrWhiteSpace(captionText)
                ? _lastLocalSystemActionLabel
                : $"{captionText}{Environment.NewLine}{_lastLocalSystemActionLabel}";
        }

        if (string.IsNullOrWhiteSpace(latestTranscript))
        {
            if (speechActive)
            {
                return AppendLocalActionLine(FormatLiveHearingCue(state, lastSpeechActivityUtc));
            }

            return AppendLocalActionLine(null);
        }

        var heard = FormatHeardTranscript(latestTranscript.Trim(), confidence);
        if (speechActive)
            return AppendLocalActionLine($"{FormatLiveHearingCue(state, lastSpeechActivityUtc)}{Environment.NewLine}{heard}");

        return AppendLocalActionLine(heard);
    }

    private static string BuildLocalSessionSpeechCueText(AlphaSessionState state, bool speechActive, DateTime? lastSpeechActivityUtc = null, string? latestHeardText = null, float? confidence = null)
    {
        var heard = string.IsNullOrWhiteSpace(latestHeardText)
            ? null
            : latestHeardText.Trim();

        if (speechActive)
        {
            return "Speech cue: " + FormatLiveHearingCue(state, lastSpeechActivityUtc);
        }

        if (!string.IsNullOrWhiteSpace(heard) && !heard.Equals("Nothing heard yet.", StringComparison.OrdinalIgnoreCase))
            return $"Speech cue: {FormatHeardTranscript(heard, confidence)}";

        return "Speech cue: nothing heard yet.";
    }

    private static string FormatHeardTranscript(string transcript, float? confidence = null)
    {
        if (confidence.HasValue)
            return $"Heard: {transcript} ({confidence.Value:P0})";

        return $"Heard: {transcript}";
    }

    private static string FormatLastHeardLabel(string transcript, float? confidence = null)
    {
        if (confidence.HasValue)
            return $"Last heard: {transcript} ({confidence.Value:P0})";

        return $"Last heard: {transcript}";
    }

    private static string FormatCommandLabel(string transcript, float? confidence = null)
    {
        if (confidence.HasValue)
            return $"Command: {transcript} ({confidence.Value:P0})";

        return $"Command: {transcript}";
    }

    private static string FormatLiveHearingCue(AlphaSessionState state, DateTime? lastSpeechActivityUtc)
    {
        var baseCue = state switch
        {
            AlphaSessionState.WaitingForIdentity => "Hearing your callsign...",
            AlphaSessionState.WaitingForCommand => "Hearing your command...",
            AlphaSessionState.ReadyToLaunch => "Listening for launch...",
            AlphaSessionState.Launching => "Launching...",
            _ => "Hearing speech..."
        };

        if (!lastSpeechActivityUtc.HasValue)
            return baseCue;

        var seconds = Math.Max(0d, (DateTime.UtcNow - lastSpeechActivityUtc.Value).TotalSeconds);
        if (seconds > 6)
            return baseCue;

        return $"{baseCue.TrimEnd('.')} ({seconds:0.0}s ago)...";
    }

    private static string FormatLiveActivityDuration(DateTime? lastSpeechActivityUtc)
    {
        if (!lastSpeechActivityUtc.HasValue)
            return string.Empty;

        var seconds = Math.Max(0d, (DateTime.UtcNow - lastSpeechActivityUtc.Value).TotalSeconds);
        if (seconds <= 0.4)
            return string.Empty;

        return $" ({seconds:0.0}s)";
    }

    private string BuildLocalDictationSpeechCueText()
    {
        if (!_dictationActive)
            return "Speech cue: dictation is stopped.";

        if (!_voiceCommandService.IsListening)
            return "Speech cue: microphone listener is stopped.";

        if (_voiceCommandService.IsSpeechActive)
            return "Speech cue: " + FormatLiveHearingCue(_session.State, _voiceCommandService.LastSpeechActivityUtc).Replace("Hearing your", "Hearing dictation").Replace("Listening for launch", "Hearing dictation");

        if (_dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText))
            return $"Speech cue: Heard {_dictationLastTranscriptText}";

        return "Speech cue: dictation is waiting for speech.";
    }

    private void SyncWakeOverlay(bool shouldBeVisible, string? readout = null, string? phase = null, IReadOnlyList<string>? transcriptHistory = null, double? activityLevel = null, string? activityText = null, string? captionText = null, string? wakeStatusText = null, string? authorityText = null)
    {
        if (shouldBeVisible)
        {
            ShowWakeOverlay(readout, phase, transcriptHistory, activityLevel, activityText, _voiceCommandService.IsSpeechActive, captionText, wakeStatusText, authorityText);
            return;
        }

        HideWakeOverlay();
    }

    private void ShowWakeOverlay(string? readout = null, string? phase = null, IReadOnlyList<string>? transcriptHistory = null, double? activityLevel = null, string? activityText = null, bool speechActive = false, string? captionText = null, string? wakeStatusText = null, string? authorityText = null)
    {
        if (_wakeOverlayMissingLogged)
        {
            _wakeOverlay?.HideOverlay();
            return;
        }

        if (_wakeOverlay == null)
        {
            try
            {
                _wakeOverlay = new WakeOverlayForm();
            }
            catch (Exception ex)
            {
                _wakeOverlayMissingLogged = true;
                UpdateStatus($"Wake overlay could not be created: {ex.Message}");
                return;
            }

            if (!_wakeOverlay.IsReady)
            {
                _wakeOverlayMissingLogged = true;
                UpdateStatus("Wake overlay asset callsign.gif was not found. Voice flow continues without the overlay.");
                _wakeOverlay.Dispose();
                _wakeOverlay = null;
                return;
            }
        }

        _wakeOverlay.ShowOverlay(readout, phase, transcriptHistory, activityLevel, activityText, speechActive, captionText, wakeStatusText, authorityText);
    }

    private void PreloadWakeOverlay()
    {
        if (_wakeOverlay != null || _wakeOverlayMissingLogged)
            return;

        try
        {
            _wakeOverlay = new WakeOverlayForm();
            if (!_wakeOverlay.IsReady)
            {
                _wakeOverlayMissingLogged = true;
                UpdateStatus("Wake overlay asset callsign.gif was not found. Voice flow continues without the overlay.");
                _wakeOverlay.Dispose();
                _wakeOverlay = null;
            }
        }
        catch (Exception ex)
        {
            _wakeOverlayMissingLogged = true;
            UpdateStatus($"Wake overlay could not be preloaded: {ex.Message}");
            _wakeOverlay?.Dispose();
            _wakeOverlay = null;
        }
    }

    private IReadOnlyList<string> GetLocalTranscriptHistory()
    {
        return _sessionTranscriptHistoryList.Items
            .Cast<object?>()
            .Select(item => item?.ToString())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Take(3)
            .ToArray()!;
    }

    private void HideWakeOverlay()
    {
        _wakeOverlay?.HideOverlay();
    }

    private void UpdateListeningPanel()
    {
        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var backgroundRuntimeListening = runtimeSnapshot is
        {
            RuntimeRole: "user-runtime",
            IsListening: true
        } && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(30);
        var backgroundRuntimeCanHearAudio = backgroundRuntimeListening && runtimeSnapshot?.CanHearAudio == true;
        var anyListenerRunning = _voiceCommandService.IsListening || backgroundRuntimeListening;
        var stopRequestPending = _runtimeStopRequestedUtc.HasValue
            && backgroundRuntimeListening
            && DateTime.UtcNow - _runtimeStopRequestedUtc.Value <= TimeSpan.FromSeconds(15);
        if (!backgroundRuntimeListening)
            _runtimeStopRequestedUtc = null;

        _listeningStateLabel.Text = _voiceCommandService.IsListening
            ? _dictationActive
                ? $"Microphone listener is running for dictation. {_voiceCommandService.CurrentModeDescription}"
                : $"Microphone listener is running. {_voiceCommandService.CurrentModeDescription}"
            : stopRequestPending
                ? "Background user runtime stop requested. Waiting for the runtime to exit."
                : backgroundRuntimeListening
                    ? backgroundRuntimeCanHearAudio
                        ? runtimeSnapshot!.LastWakeWordScore.HasValue && runtimeSnapshot.WakeWordThreshold.HasValue
                            ? runtimeSnapshot.LastWakeWordScore.Value >= runtimeSnapshot.WakeWordThreshold.Value
                                ? $"Background user runtime is listening, hearing audio, and a wake candidate passed threshold. {runtimeSnapshot.ModeDescription}"
                                : $"Background user runtime is listening and hearing audio, but the latest wake candidate stayed below threshold. {runtimeSnapshot.ModeDescription}"
                            : $"Background user runtime is listening and hearing audio. {runtimeSnapshot.ModeDescription}"
                        : HasRecentAudioPacket(runtimeSnapshot!)
                            ? "Background user runtime is receiving microphone packets, but the speech level is too quiet."
                            : "Background user runtime is running but no microphone audio packets are arriving."
                : "Microphone listener is stopped.";
        _startListeningButton.Enabled = !anyListenerRunning && !stopRequestPending;
        _stopListeningButton.Enabled = anyListenerRunning && !stopRequestPending;
        SyncVoiceModeControls();

        if (!anyListenerRunning)
            HideVisibleControlsOverlay();
    }

    private void SyncVoiceModeControls()
    {
        if (_voiceModeCommandsOnlyRadio == null || _voiceModeDictationOnlyRadio == null || _voiceModeDefaultRadio == null)
            return;

        _updatingVoiceModeControls = true;
        try
        {
            var normalizedMode = NormalizeSpeechText(_voiceAccessMode);
            _voiceModeCommandsOnlyRadio.Checked = normalizedMode is "commands only" or "command only";
            _voiceModeDictationOnlyRadio.Checked = normalizedMode is "dictation only" or "dictation";
            _voiceModeDefaultRadio.Checked = !_voiceModeCommandsOnlyRadio.Checked && !_voiceModeDictationOnlyRadio.Checked;
        }
        finally
        {
            _updatingVoiceModeControls = false;
        }
    }

    private static bool HasRecentAudioPacket(RuntimeStateSnapshot snapshot) =>
        RuntimeStatusFormatter.HasRecentAudioPacket(snapshot);

    private static int CountCallsignServiceProcesses()
    {
        try
        {
            return Process.GetProcessesByName("Callsign.Service").Length;
        }
        catch
        {
            return -1;
        }
    }

    private static string FormatMicLevel(RuntimeStateSnapshot snapshot) =>
        RuntimeStatusFormatter.FormatMicLevel(snapshot);

    private static string FormatMicDetails(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.LastMicrophoneRawRms == null)
            return "No microphone telemetry yet.";

        var warnings = snapshot.LastMicrophoneWarnings is { Count: > 0 }
            ? $" Warnings: {string.Join(" ", snapshot.LastMicrophoneWarnings)}"
            : string.Empty;
        var device = string.IsNullOrWhiteSpace(snapshot.ActiveMicrophoneDeviceName)
            ? string.Empty
            : $" Device: {snapshot.ActiveMicrophoneDeviceName}.";
        var packetAge = snapshot.SecondsSinceLastAudioPacket.HasValue
            ? $" Last audio packet {snapshot.SecondsSinceLastAudioPacket.Value:0.0}s ago."
            : string.Empty;
        var process = snapshot.CurrentProcessId.HasValue && snapshot.ProcessStartedUtc.HasValue
            ? $" PID {snapshot.CurrentProcessId.Value}, started {snapshot.ProcessStartedUtc.Value.ToLocalTime():f}."
            : string.Empty;
        var authority = string.IsNullOrWhiteSpace(snapshot.RuntimeAuthorityStatus)
            ? string.Empty
            : $" Authority: {snapshot.RuntimeAuthorityStatus}.";
        return $"Raw RMS {snapshot.LastMicrophoneRawRms:0.000}, peak {snapshot.LastMicrophonePeak:0.00}, gain {snapshot.LastMicrophoneGainDb:0.0} dB, noise floor {snapshot.LastMicrophoneNoiseFloorRms:0.000}, threshold {snapshot.LastMicrophoneSpeechThresholdRms:0.000}, clipping {snapshot.LastMicrophoneClippingRatio:P0}.{device}{packetAge}{process}{authority}{warnings}";
    }

    private void UpdateRecordButtonAppearance()
    {
        if (_recordSampleButton == null)
            return;

        if (_voiceSampleCapture.IsRecording)
        {
            _recordSampleButton.Text = "Recording - release to stop";
            _recordSampleButton.BackColor = Color.Maroon;
        }
        else
        {
            _recordSampleButton.Text = "REC Hold to Record";
            _recordSampleButton.BackColor = Color.Firebrick;
        }
    }

    private string GetLatestVoiceSamplePath(UserProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Callsign))
            throw new InvalidOperationException("Save the account with a callsign before using voice samples.");

        var folder = Path.Combine(_profileStore.ResolveCallsSignFolder(profile.Callsign), "voice-samples");
        return Path.Combine(folder, "latest.wav");
    }

    private IReadOnlyList<string> GetRecordedVoiceSamplePaths(UserProfile profile)
    {
        var folder = VoiceBiometricVerificationService.GetEnrollmentSampleFolder(_profileStore, profile);
        if (!Directory.Exists(folder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(folder, "sample-*.wav", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private int GetRecordedVoiceSampleCount(UserProfile profile) =>
        GetRecordedVoiceSamplePaths(profile).Count;

    private bool TryGetNextVoiceSamplePath(UserProfile profile, out string samplePath)
    {
        samplePath = string.Empty;
        var count = GetRecordedVoiceSampleCount(profile);
        if (count >= 3)
            return false;

        samplePath = VoiceBiometricVerificationService.GetEnrollmentSamplePath(_profileStore, profile, count + 1);
        return true;
    }

    private bool TryGetCurrentVoiceSamplePath(UserProfile profile, out string samplePath)
    {
        samplePath = _voiceSampleCapture.CurrentSamplePath ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(samplePath) && File.Exists(samplePath))
            return true;

        return TryGetLatestVoiceSamplePath(profile, out samplePath);
    }

    private void CopyVoiceSampleToLatest(string samplePath)
    {
        try
        {
            if (!File.Exists(samplePath))
                return;

            var latestPath = Path.Combine(_profileStore.ResolveCallsSignFolder(_activeProfile!.Callsign), "voice-samples", "latest.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(latestPath)!);
            if (!string.Equals(Path.GetFullPath(samplePath), Path.GetFullPath(latestPath), StringComparison.OrdinalIgnoreCase))
                File.Copy(samplePath, latestPath, overwrite: true);
        }
        catch
        {
            // Best-effort convenience copy only.
        }
    }

    private bool TryGetLatestVoiceSamplePath(UserProfile profile, out string samplePath)
    {
        samplePath = string.Empty;
        if (string.IsNullOrWhiteSpace(profile.Callsign))
            return false;

        samplePath = GetLatestVoiceSamplePath(profile);
        return true;
    }

    private static bool ContainsSpeechPhrase(string transcript, string phrase)
    {
        return AlphaVoiceTranscriptParser.ContainsSpeechPhrase(transcript, phrase);
    }

    private static bool ContainsWakeWord(string transcript, string wakeWord) =>
        AlphaVoiceTranscriptParser.ContainsWakeWord(transcript, wakeWord);

    private static string ExtractCommandFromTranscript(string transcript, string wakeWord, string callsign)
        => AlphaVoiceTranscriptParser.ExtractCommandFromTranscript(transcript, wakeWord, callsign);

    private static string NormalizeLaunchCommand(string command)
        => AlphaVoiceTranscriptParser.NormalizeLaunchCommand(command);

    private static bool IsCancelCommand(string transcript)
        => AlphaVoiceTranscriptParser.IsCancelCommand(transcript);

    private static bool IsStopListeningCommand(string transcript)
        => AlphaVoiceTranscriptParser.IsStopListeningCommand(transcript);

    private static bool IsStopDictationCommand(string transcript)
        => AlphaVoiceTranscriptParser.IsStopDictationCommand(transcript);

    private static bool IsPauseDictationCommand(string transcript)
        => AlphaVoiceTranscriptParser.IsPauseDictationCommand(transcript);

    private static string NormalizeSpeechText(string value) =>
        AlphaVoiceTranscriptParser.NormalizeSpeechText(value);

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

    private sealed record AppCandidateListItem(int Number, StartMenuAppCandidate Candidate)
    {
        public override string ToString() =>
            $"{Number}. {Candidate.DisplayName} ({Candidate.MatchKind}, {Candidate.Score:P0})";
    }

    private sealed record WindowSwitchListItem(int Number, VisibleWindowSwitchCandidate Candidate)
    {
        public override string ToString()
        {
            var minimized = Candidate.IsMinimized ? " minimized" : string.Empty;
            return $"{Number}. {Candidate.DisplayName} ({Candidate.MatchKind}{minimized})";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _runtimeStateMonitor.Changed -= RuntimeStateMonitorChanged;
        _runtimeStateMonitor.Dispose();
        _voiceSampleCapture.Dispose();
        _voiceCommandService.Dispose();
        _updateCheckTimer.Stop();
        _wakeOverlay?.HideOverlay();
        _wakeOverlay?.Dispose();
        _wakeOverlay = null;
        _visibleControlsOverlay?.HideOverlay();
        _visibleControlsOverlay?.Dispose();
        _visibleControlsOverlay = null;
        _mouseGridOverlay?.Hide();
        _mouseGridOverlay?.Dispose();
        _mouseGridOverlay = null;
        _keyboardOverlay?.Hide();
        _keyboardOverlay?.Dispose();
        _keyboardOverlay = null;
        _commandPalette?.Dispose();
        _commandPalette = null;
        _updateSplash?.Dispose();
        _updateSplash = null;
        _dictationCorrectionForm?.Dispose();
        _dictationCorrectionForm = null;
        _dictationReadbackSynthesizer?.SpeakAsyncCancelAll();
        _dictationReadbackSynthesizer?.Dispose();
        _dictationReadbackSynthesizer = null;
        _statusReadbackSynthesizer?.SpeakAsyncCancelAll();
        _statusReadbackSynthesizer?.Dispose();
        _statusReadbackSynthesizer = null;
        base.OnFormClosing(e);
    }

    private void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void RunOnUiThread(Action action)
    {
        if (IsDisposed)
            return;

        if (!IsHandleCreated)
            return;

        if (!InvokeRequired)
        {
            action();
            return;
        }

        BeginInvoke((Action)(() =>
        {
            if (!IsDisposed)
                action();
        }));
    }

    private static bool ValidateCallsign(string callsign, out string normalized)
    {
        normalized = callsign.Trim().ToLowerInvariant();
        if (normalized.Length < 2 || normalized.Length > 32)
            return false;

        if (!Regex.IsMatch(normalized, @"^[a-z0-9][a-z0-9_\- ]{0,31}$"))
            return false;

        if (normalized.Contains("__") || normalized.Contains("  ") || normalized.Contains("- ") || normalized.Contains(" -"))
            return false;

        return true;
    }

    private sealed record UpdateSplashState(string? LastShownManifestVersion);

    private sealed record StartupWalkthroughState(bool HasSeenWalkthrough);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
}
