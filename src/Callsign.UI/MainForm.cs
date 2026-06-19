using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Text.RegularExpressions;
using Callsign.UI.Models;
using Callsign.UI.Services;

namespace Callsign.UI;

public sealed class MainForm : Form
{
    private readonly ProfileStore _profileStore = new();
    private readonly AlphaSessionStateMachine _session = new();
    private readonly StartMenuLauncher _launcher = new();
    private readonly AlphaAuditLog _auditLog;
    private readonly VoiceCommandService _voiceCommandService = new();
    private readonly VoiceSampleCaptureService _voiceSampleCapture = new();
    private readonly VoiceBiometricVerificationService _voiceBiometricVerificationService = new();
    private readonly BrowserLaunchService _browserLaunchService = new();
    private readonly SystemControlService _systemControlService = new();
    private readonly FileSearchService _fileSearchService = new();
    private readonly RuntimeStateMonitor _runtimeStateMonitor = new();
    private readonly System.Windows.Forms.Timer _sessionTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _visibleControlsRefreshTimer = new() { Interval = 250 };
    private WakeOverlayForm? _wakeOverlay;
    private bool _wakeOverlayMissingLogged;

    private readonly List<UserProfile> _profiles = [];
    private UserProfile? _activeProfile;
    private bool _updatingUi;
    private bool _formReadyForListener;
    private bool _dictationActive;
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
    private Label _micLevelLabel = null!;
    private Label _micDetailLabel = null!;
    private TextBox _spokenCallsignText = null!;
    private TextBox _spokenCommandText = null!;
    private TextBox _appNameText = null!;
    private TextBox _voicePhraseText = null!;

    private Button _recordSampleButton = null!;
    private Button _playSampleButton = null!;
    private Button _trainVoiceButton = null!;
    private Button _resetVoiceButton = null!;
    private Button _wakeButton = null!;
    private Button _startListeningButton = null!;
    private Button _stopListeningButton = null!;
    private Button _rehearsePhraseButton = null!;
    private Button _verifyButton = null!;
    private Button _captureButton = null!;
    private Button _launchButton = null!;
    private Button _cancelButton = null!;
    private Button _resetSessionButton = null!;
    private Button _newProfileButton = null!;
    private Button _saveProfileButton = null!;
    private Button _deleteProfileButton = null!;
    private List<VisibleControlSummaryEntry> _visibleControlsSummary = [];
    private VisibleControlsOverlayForm? _visibleControlsOverlay;

    private TextBox _dictationTextBox = null!;
    private Label _dictationStatusLabel = null!;
    private Label _dictationHintLabel = null!;
    private Label _dictationSpeechCueLabel = null!;
    private Label _dictationLastHeardLabel = null!;
    private Label _dictationHistoryLabel = null!;
    private ListBox _dictationHistoryList = null!;
    private Button _startDictationButton = null!;
    private Button _stopDictationButton = null!;
    private Button _copyDictationButton = null!;
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
    private Button _selectPreviousWordDictationButton = null!;
    private Button _selectNextWordDictationButton = null!;
    private Button _deletePreviousWordDictationButton = null!;
    private Button _selectPreviousSentenceDictationButton = null!;
    private Button _selectNextSentenceDictationButton = null!;
    private Button _deletePreviousSentenceDictationButton = null!;
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

    private TextBox _browserInputText = null!;
    private Label _browserStatusLabel = null!;
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
    private Button _browserCloseTabButton = null!;
    private Button _browserAddressBarButton = null!;
    private Button _browserScrollUpButton = null!;
    private Button _browserScrollDownButton = null!;
    private Button _browserScrollTopButton = null!;
    private Button _browserScrollBottomButton = null!;
    private Button _browserFindButton = null!;
    private Button _browserFindNextButton = null!;
    private Button _browserFindPreviousButton = null!;
    private Button _browserZoomInButton = null!;
    private Button _browserZoomOutButton = null!;
    private Button _browserZoomResetButton = null!;

    private Label _systemStatusLabel = null!;
    private Button _systemVolumeUpButton = null!;
    private Button _systemVolumeDownButton = null!;
    private Button _systemMuteButton = null!;
    private Button _systemShowDesktopButton = null!;
    private Button _systemNextWindowButton = null!;
    private Button _systemPreviousWindowButton = null!;
    private Button _systemTaskManagerButton = null!;
    private Button _systemMinimizeWindowButton = null!;
    private Button _systemMaximizeWindowButton = null!;
    private Button _systemRestoreWindowButton = null!;
    private Button _systemEnterButton = null!;
    private Button _systemTabButton = null!;
    private Button _systemEscapeButton = null!;
    private Button _systemBackspaceButton = null!;
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
    private Button _systemMouseScrollUpButton = null!;
    private Button _systemMouseScrollDownButton = null!;
    private Button _systemCopyButton = null!;
    private Button _systemPasteButton = null!;
    private Button _systemCutButton = null!;
    private Button _systemSelectAllButton = null!;
    private Button _systemSaveButton = null!;
    private Button _systemUndoButton = null!;
    private Button _systemRedoButton = null!;
    private Button _systemFindButton = null!;
    private Button _systemNewWindowButton = null!;
    private Button _systemCloseWindowButton = null!;
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
    private Label _systemLastActionLabel = null!;
    private Label _systemVoiceCueLabel = null!;
    private Label _systemLastHeardLabel = null!;
    private string _lastLocalSystemActionLabel = "Last action: none yet.";

    private TextBox _fileSearchQueryText = null!;
    private Label _fileSearchStatusLabel = null!;
    private Label _fileSearchSelectionLabel = null!;
    private ListBox _fileSearchResultsList = null!;
    private Label _fileSearchVoiceCueLabel = null!;
    private Label _fileSearchLastHeardLabel = null!;
    private Label _fileSearchLastActionLabel = null!;
    private string _lastLocalFileSearchActionLabel = "Last action: none yet.";
    private Button _searchFilesButton = null!;
    private Button _openFileResultButton = null!;
    private Button _openFileFolderButton = null!;

    private DateTime? _dictationStartedUtc;
    private DateTime? _dictationLastTranscriptUtc;
    private string? _dictationLastTranscriptText;
    private string? _lastHeardTranscriptText;
    private float? _lastHeardTranscriptConfidence;
    private DateTime? _lastSessionTranscriptHistoryRuntimeUpdateUtc;
    private readonly List<string> _dictationHistoryEntries = [];

    private Label _statusLabel = null!;

    public MainForm()
    {
        _auditLog = new AlphaAuditLog(_profileStore);

        Text = "Callsign Alpha Setup";
        Width = 1080;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        BuildForm();

        _voiceCommandService.TranscriptReceived += VoiceTranscriptReceived;
        _voiceCommandService.WakeWordDetected += VoiceWakeWordDetected;
        _voiceCommandService.RecognitionError += VoiceRecognitionError;
        _runtimeStateMonitor.Changed += RuntimeStateMonitorChanged;
        _voiceCommandService.SpeechActivityChanged += (_, _) => RunOnUiThread(() =>
        {
            UpdateVoiceCueRefreshRate();
            UpdateLiveSpeechCueFromActivity();
            RefreshSessionPanel();
            if (_voiceCommandService.IsListening || _dictationActive || IsWakeOverlaySessionActive(_session.State))
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
        });

        _sessionTimer.Tick += (_, _) => OnSessionTick();
        _visibleControlsRefreshTimer.Tick += (_, _) => OnVisibleControlsRefreshTick();
        LoadProfiles();
        _sessionTimer.Start();

        UpdateStatus("Create an account, activate voice, then launch installed apps with wake word + callsign.");
        Shown += (_, _) =>
        {
            _formReadyForListener = true;
            TryStartListenerForActiveProfile();
        };
    }

    private void BuildForm()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(BuildAccountTab());
        _tabs.TabPages.Add(BuildVoiceTab());
        _tabs.TabPages.Add(BuildSessionTab());
        _tabs.TabPages.Add(BuildDictationTab());
        _tabs.TabPages.Add(BuildBrowserTab());
        _tabs.TabPages.Add(BuildSystemTab());
        _tabs.TabPages.Add(BuildFileSearchTab());
        root.Controls.Add(_tabs, 0, 0);

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            AutoSize = false,
            BorderStyle = BorderStyle.Fixed3D,
            Padding = new Padding(8, 4, 8, 4)
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
            AccessibleName = "Notes"
        };

        _newProfileButton = new Button { Text = "Create New", Width = 130 };
        _newProfileButton.Click += (_, _) => CreateNewProfile();

        _saveProfileButton = new Button { Text = "Save Account", Width = 130 };
        _saveProfileButton.Click += (_, _) => SaveProfile();

        _deleteProfileButton = new Button { Text = "Delete Account", Width = 130 };
        _deleteProfileButton.Click += (_, _) => DeleteProfile();

        var openFolderButton = new Button { Text = "Open Data Folder", Width = 150 };
        openFolderButton.Click += (_, _) => OpenProfileFolder();

        var openLogsButton = new Button { Text = "Open Logs Folder", Width = 150 };
        openLogsButton.Click += (_, _) => OpenLogsFolder();

        var openInstalledAppButton = new Button { Text = "Open App Folder", Width = 150 };
        openInstalledAppButton.Click += (_, _) => OpenInstalledAppFolder();

        var runWakeSetupButton = new Button { Text = "Repair Wakeword", Width = 150 };
        runWakeSetupButton.Click += (_, _) => RunOpenWakeWordSetupHelper();

        var runPyannoteSetupButton = new Button { Text = "Train Voice Identity", Width = 170 };
        runPyannoteSetupButton.Click += (_, _) => OpenVoiceIdentityTrainingForActiveProfile();

        var showVoiceHelpButton = new Button { Text = "Voice Help", Width = 110 };
        showVoiceHelpButton.Click += (_, _) => ShowVoiceHelp();

        _accountPathLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "No profile selected." };
        _accountStateLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "Voice not activated." };

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddRow(layout, "Active account", _profilePicker, row++);
        AddRow(layout, "Callsign", _callsignText, row++);
        AddFullWidth(layout, new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Tip: choose something easy to say out loud, like Alpha or Aryn One. Prefer spoken words over digits."
        }, row++);
        AddRow(layout, "Display name", _displayNameText, row++);
        AddRow(layout, "Email", _emailText, row++);
        AddRow(layout, "Department", _departmentText, row++);
        AddRow(layout, "Notes", _notesText, row++);

        layout.Controls.Add(new Label { Text = "Profile folder" }, 0, row);
        layout.Controls.Add(_accountPathLabel, 1, row++);
        layout.Controls.Add(new Label { Text = "Voice status" }, 0, row);
        layout.Controls.Add(_accountStateLabel, 1, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_newProfileButton);
        buttons.Controls.Add(_saveProfileButton);
        buttons.Controls.Add(_deleteProfileButton);
        buttons.Controls.Add(openFolderButton);
        buttons.Controls.Add(openLogsButton);
        buttons.Controls.Add(openInstalledAppButton);
        buttons.Controls.Add(runWakeSetupButton);
        buttons.Controls.Add(runPyannoteSetupButton);
        buttons.Controls.Add(showVoiceHelpButton);
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
            ForeColor = Color.DimGray,
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
            Cursor = Cursors.Hand
        };
        _recordSampleButton.FlatAppearance.BorderSize = 0;
        _recordSampleButton.MouseDown += RecordSampleButtonMouseDown;
        _recordSampleButton.MouseUp += RecordSampleButtonMouseUp;
        _recordSampleButton.MouseLeave += RecordSampleButtonMouseLeave;

        _playSampleButton = new Button { Text = "Play Sample", Width = 120, Height = 44 };
        _playSampleButton.Click += (_, _) => PlayLatestVoiceSample();

        _trainVoiceButton = new Button { Text = "Train Voice Identity", Width = 160 };
        _trainVoiceButton.Click += (_, _) => TrainVoiceIdentity();

        _resetVoiceButton = new Button { Text = "Reset Voice", Width = 130 };
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
        var layout = BuildTwoColumnLayout(21);

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
            ForeColor = Color.DimGray,
            Text = "Try: 'Callsign Alpha open Notepad', or say 'Callsign Alpha' then 'Notepad'. Test Phrase Launch can open the app. Say 'cancel' to clear the current command or 'stop listening' to turn off the microphone. Alpha accepts app names only, not paths, URLs, or terminal commands."
        };

        _spokenCallsignText = BuildTextInput("Spoken callsign");
        _spokenCommandText = BuildTextInput("Spoken command");
        _appNameText = BuildTextInput("App to launch");
        _voicePhraseText = BuildTextInput("Launch test phrase");
        _voicePhraseText.PlaceholderText = "Callsign Alpha open Notepad";

        _sessionStateLabel = new Label { AutoSize = true, Text = "Idle." };
        _sessionPhaseLabel = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = "Phase: Listening" };
        _sessionNextActionLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(0, 120, 215), Font = new Font(Font, FontStyle.Bold), Text = "Next: say Callsign." };
        _sessionHintLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "Next: say Callsign." };
        _sessionIdentityLabel = new Label { AutoSize = true, Text = "Waiting for wake word." };
        _sessionCommandLabel = new Label { AutoSize = true, Text = "No command captured." };
        _sessionCountdownLabel = new Label { AutoSize = true, Text = "No timer running." };
        _sessionResultLabel = new Label { AutoSize = true, Text = "No launch yet." };
        _sessionSpeechCueLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Speech cue: nothing heard yet." };
        _sessionTranscriptHistoryLabel = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = "Recent speech" };
        _sessionTranscriptHistoryList = new ListBox
        {
            Dock = DockStyle.Fill,
            Height = 110,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
        };
        _listeningStateLabel = new Label { AutoSize = true, Text = "Microphone listener is stopped." };
        _lastHeardLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Nothing heard yet." };
        _wakeReliabilityLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "No wake event detected yet." };
        _wakeCandidateLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Wake candidate: nothing heard yet." };
        _wakeScoreLabel = new Label { AutoSize = true, Text = "Wake score unavailable." };
        _wakeQualityLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Audio quality diagnostics unavailable." };
        _micLevelLabel = new Label { AutoSize = true, Text = "Microphone level unavailable." };
        _micDetailLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "No microphone telemetry yet." };

        _startListeningButton = new Button { Text = "Start Listening", Width = 140 };
        _startListeningButton.Click += (_, _) => StartVoiceListening();

        _stopListeningButton = new Button { Text = "Stop Listening", Width = 130, Enabled = false };
        _stopListeningButton.Click += (_, _) => StopVoiceListening();

        _rehearsePhraseButton = new Button { Text = "Test Phrase Launch", Width = 150 };
        _rehearsePhraseButton.Click += (_, _) => RehearseVoicePhrase();

        _wakeButton = new Button { Text = "Wake Word", Width = 120 };
        _wakeButton.Click += (_, _) => WakeSession();

        _verifyButton = new Button { Text = "Verify Callsign", Width = 140 };
        _verifyButton.Click += (_, _) => VerifyIdentity();

        _captureButton = new Button { Text = "Capture Command", Width = 150 };
        _captureButton.Click += (_, _) => CaptureCommand();

        _launchButton = new Button { Text = "Launch via Start Menu", Width = 170 };
        _launchButton.Click += (_, _) => LaunchAppFromStartMenu();

        _cancelButton = new Button { Text = "Cancel", Width = 100 };
        _cancelButton.Click += (_, _) => CancelSession();

        _resetSessionButton = new Button { Text = "Reset Session", Width = 120 };
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
        AddRow(layout, "Mic level", _micLevelLabel, row++);
        AddRow(layout, "Mic details", _micDetailLabel, row++);
        AddRow(layout, "Spoken callsign", _spokenCallsignText, row++);
        AddRow(layout, "Spoken command", _spokenCommandText, row++);
        AddRow(layout, "App to launch", _appNameText, row++);
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
        buttons.Controls.Add(_rehearsePhraseButton);
        buttons.Controls.Add(_wakeButton);
        buttons.Controls.Add(_verifyButton);
        buttons.Controls.Add(_captureButton);
        buttons.Controls.Add(_launchButton);
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
        var layout = BuildTwoColumnLayout(11);

        var heading = CreateHeading("Dictate text, review it, then copy or paste it");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Dictation captures speech and exposes the transcribed text in a visible box. Use Start Dictation to begin listening, then stop or paste the result into the active app when you are ready."
        };
        _dictationHintLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Dictation is visible first: the app shows what it heard, the last error, and whether listening is active."
        };
        _dictationStatusLabel = new Label
        {
            AutoSize = true,
            Text = "Dictation is stopped."
        };
        _dictationSpeechCueLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Speech cue: dictation is stopped."
        };
        _dictationLastHeardLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Last heard: nothing yet."
        };
        _dictationTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 180,
            AccessibleName = "Dictated text"
        };
        _dictationTextBox.TextChanged += (_, _) => RefreshDictationPanel();

        _dictationHistoryLabel = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Recent speech"
        };
        _dictationHistoryList = new ListBox
        {
            Dock = DockStyle.Fill,
            Height = 110,
            AccessibleName = "Recent dictated speech"
        };

        _startDictationButton = new Button { Text = "Start Dictation", Width = 130 };
        _startDictationButton.Click += (_, _) => StartDictation();

        _stopDictationButton = new Button { Text = "Stop Dictation", Width = 120 };
        _stopDictationButton.Click += (_, _) => StopDictation();

        _copyDictationButton = new Button { Text = "Copy Text", Width = 110 };
        _copyDictationButton.Click += (_, _) => CopyDictationText();

        _pasteDictationButton = new Button { Text = "Paste Into Active App", Width = 180 };
        _pasteDictationButton.Click += (_, _) => PasteDictationText();

        _clearDictationButton = new Button { Text = "Clear", Width = 90 };
        _clearDictationButton.Click += (_, _) => ClearDictationText();

        _cutDictationButton = new Button { Text = "Cut Text", Width = 100 };
        _cutDictationButton.Click += (_, _) => CutDictationText();

        _undoDictationButton = new Button { Text = "Undo", Width = 90 };
        _undoDictationButton.Click += (_, _) => UndoDictationText();

        _redoDictationButton = new Button { Text = "Redo", Width = 90 };
        _redoDictationButton.Click += (_, _) => RedoDictationText();

        _goToStartDictationButton = new Button { Text = "Go To Start", Width = 105 };
        _goToStartDictationButton.Click += (_, _) => GoToStartDictationText();

        _goToEndDictationButton = new Button { Text = "Go To End", Width = 100 };
        _goToEndDictationButton.Click += (_, _) => GoToEndDictationText();

        _selectToStartDictationButton = new Button { Text = "Select To Start", Width = 125 };
        _selectToStartDictationButton.Click += (_, _) => SelectToStartDictationText();

        _selectToEndDictationButton = new Button { Text = "Select To End", Width = 115 };
        _selectToEndDictationButton.Click += (_, _) => SelectToEndDictationText();

        _deleteToStartDictationButton = new Button { Text = "Delete To Start", Width = 125 };
        _deleteToStartDictationButton.Click += (_, _) => DeleteToStartDictationText();

        _deleteToEndDictationButton = new Button { Text = "Delete To End", Width = 115 };
        _deleteToEndDictationButton.Click += (_, _) => DeleteToEndDictationText();

        _goToLineStartDictationButton = new Button { Text = "Line Start", Width = 95 };
        _goToLineStartDictationButton.Click += (_, _) => GoToLineStartDictationText();

        _goToLineEndDictationButton = new Button { Text = "Line End", Width = 90 };
        _goToLineEndDictationButton.Click += (_, _) => GoToLineEndDictationText();

        _selectToLineStartDictationButton = new Button { Text = "Select To Line Start", Width = 150 };
        _selectToLineStartDictationButton.Click += (_, _) => SelectToLineStartDictationText();

        _selectToLineEndDictationButton = new Button { Text = "Select To Line End", Width = 145 };
        _selectToLineEndDictationButton.Click += (_, _) => SelectToLineEndDictationText();

        _deleteToLineStartDictationButton = new Button { Text = "Delete To Line Start", Width = 150 };
        _deleteToLineStartDictationButton.Click += (_, _) => DeleteToLineStartDictationText();

        _deleteToLineEndDictationButton = new Button { Text = "Delete To Line End", Width = 145 };
        _deleteToLineEndDictationButton.Click += (_, _) => DeleteToLineEndDictationText();

        _goToParagraphStartDictationButton = new Button { Text = "Paragraph Start", Width = 120 };
        _goToParagraphStartDictationButton.Click += (_, _) => GoToParagraphStartDictationText();

        _goToParagraphEndDictationButton = new Button { Text = "Paragraph End", Width = 110 };
        _goToParagraphEndDictationButton.Click += (_, _) => GoToParagraphEndDictationText();

        _selectToParagraphStartDictationButton = new Button { Text = "Select To Paragraph Start", Width = 175 };
        _selectToParagraphStartDictationButton.Click += (_, _) => SelectToParagraphStartDictationText();

        _selectToParagraphEndDictationButton = new Button { Text = "Select To Paragraph End", Width = 170 };
        _selectToParagraphEndDictationButton.Click += (_, _) => SelectToParagraphEndDictationText();

        _deleteToParagraphStartDictationButton = new Button { Text = "Delete To Paragraph Start", Width = 180 };
        _deleteToParagraphStartDictationButton.Click += (_, _) => DeleteToParagraphStartDictationText();

        _deleteToParagraphEndDictationButton = new Button { Text = "Delete To Paragraph End", Width = 175 };
        _deleteToParagraphEndDictationButton.Click += (_, _) => DeleteToParagraphEndDictationText();

        _replacePreviousParagraphDictationButton = new Button { Text = "Replace Prev Paragraph", Width = 170 };
        _replacePreviousParagraphDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous paragraph with ...' to apply a paragraph replacement.");

        _newlineDictationButton = new Button { Text = "New Line", Width = 95 };
        _newlineDictationButton.Click += (_, _) => InsertDictationLineBreak();

        _paragraphDictationButton = new Button { Text = "New Paragraph", Width = 120 };
        _paragraphDictationButton.Click += (_, _) => InsertDictationParagraphBreak();

        _deleteWordDictationButton = new Button { Text = "Delete Word", Width = 110 };
        _deleteWordDictationButton.Click += (_, _) => DeleteLastDictationWord();

        _selectPreviousWordDictationButton = new Button { Text = "Select Prev Word", Width = 130 };
        _selectPreviousWordDictationButton.Click += (_, _) => SelectPreviousDictationWord();

        _selectNextWordDictationButton = new Button { Text = "Select Next Word", Width = 130 };
        _selectNextWordDictationButton.Click += (_, _) => SelectNextDictationWord();

        _deletePreviousWordDictationButton = new Button { Text = "Delete Prev Word", Width = 130 };
        _deletePreviousWordDictationButton.Click += (_, _) => DeletePreviousDictationWord();

        _selectPreviousSentenceDictationButton = new Button { Text = "Select Prev Sentence", Width = 145 };
        _selectPreviousSentenceDictationButton.Click += (_, _) => SelectPreviousDictationSentence();

        _selectNextSentenceDictationButton = new Button { Text = "Select Next Sentence", Width = 145 };
        _selectNextSentenceDictationButton.Click += (_, _) => SelectNextDictationSentence();

        _deletePreviousSentenceDictationButton = new Button { Text = "Delete Prev Sentence", Width = 150 };
        _deletePreviousSentenceDictationButton.Click += (_, _) => DeletePreviousDictationSentence();

        _replacePreviousWordDictationButton = new Button { Text = "Replace Prev Word", Width = 140 };
        _replacePreviousWordDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous word with ...' to apply a voice replacement.");

        _replacePreviousSentenceDictationButton = new Button { Text = "Replace Prev Sentence", Width = 160 };
        _replacePreviousSentenceDictationButton.Click += (_, _) => UpdateStatus("Say 'replace previous sentence with ...' to apply a voice replacement.");

        _replaceAllDictationButton = new Button { Text = "Replace All", Width = 105 };
        _replaceAllDictationButton.Click += (_, _) => UpdateStatus("Say 'replace all with ...' to replace the entire dictated text.");

        _commaDictationButton = new Button { Text = "Comma", Width = 80 };
        _commaDictationButton.Click += (_, _) => InsertDictationPunctuation(", ");

        _periodDictationButton = new Button { Text = "Period", Width = 80 };
        _periodDictationButton.Click += (_, _) => InsertDictationPunctuation(". ");

        _questionDictationButton = new Button { Text = "Question", Width = 90 };
        _questionDictationButton.Click += (_, _) => InsertDictationPunctuation("? ");

        _exclamationDictationButton = new Button { Text = "Exclaim", Width = 90 };
        _exclamationDictationButton.Click += (_, _) => InsertDictationPunctuation("! ");

        _semicolonDictationButton = new Button { Text = "Semicolon", Width = 95 };
        _semicolonDictationButton.Click += (_, _) => InsertDictationPunctuation("; ");

        _colonDictationButton = new Button { Text = "Colon", Width = 80 };
        _colonDictationButton.Click += (_, _) => InsertDictationPunctuation(": ");

        _apostropheDictationButton = new Button { Text = "Apostrophe", Width = 100 };
        _apostropheDictationButton.Click += (_, _) => InsertDictationPunctuation("'");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _dictationHintLabel, row++);
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
        buttons.Controls.Add(_selectPreviousWordDictationButton);
        buttons.Controls.Add(_selectNextWordDictationButton);
        buttons.Controls.Add(_deletePreviousWordDictationButton);
        buttons.Controls.Add(_selectPreviousSentenceDictationButton);
        buttons.Controls.Add(_selectNextSentenceDictationButton);
        buttons.Controls.Add(_deletePreviousSentenceDictationButton);
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
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildBrowserTab()
    {
        var tab = new TabPage("Browser");
        var layout = BuildTwoColumnLayout(8);

        var heading = CreateHeading("Open a website or search the web");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Enter a URL or a search phrase. Callsign will open the default browser and hand the browser a visible target or web search."
        };
        _browserStatusLabel = new Label { AutoSize = true, Text = "Browser target not opened yet." };
        _browserVoiceCueLabel = new Label { AutoSize = true, Text = "Voice cue: browser is waiting for speech." };
        _browserLastHeardLabel = new Label { AutoSize = true, Text = "Last heard: nothing yet." };
        _browserLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _browserInputText = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search the web or open a URL, such as example.com or callsign desktop assistant", AccessibleName = "Browser target" };
        _browserInputText.TextChanged += (_, _) => RefreshBrowserPanel();

        _openBrowserButton = new Button { Text = "Open / Search", Width = 130 };
        _openBrowserButton.Click += (_, _) => OpenBrowserTarget();

        _searchBrowserButton = new Button { Text = "Search Web", Width = 110 };
        _searchBrowserButton.Click += (_, _) => OpenBrowserTarget(forceSearch: true);

        _copyBrowserTargetButton = new Button { Text = "Copy Target", Width = 110 };
        _copyBrowserTargetButton.Click += (_, _) => CopyBrowserTarget();

        _browserBackButton = new Button { Text = "Back", Width = 80 };
        _browserBackButton.Click += (_, _) => ExecuteBrowserAction("browser-back", "Browser back requested.");

        _browserForwardButton = new Button { Text = "Forward", Width = 80 };
        _browserForwardButton.Click += (_, _) => ExecuteBrowserAction("browser-forward", "Browser forward requested.");

        _browserRefreshButton = new Button { Text = "Refresh", Width = 80 };
        _browserRefreshButton.Click += (_, _) => ExecuteBrowserAction("browser-refresh", "Browser refresh requested.");

        _browserNewTabButton = new Button { Text = "New Tab", Width = 90 };
        _browserNewTabButton.Click += (_, _) => ExecuteBrowserAction("browser-new-tab", "Browser new tab requested.");

        _browserCloseTabButton = new Button { Text = "Close Tab", Width = 90 };
        _browserCloseTabButton.Click += (_, _) => ExecuteBrowserAction("browser-close-tab", "Browser close tab requested.");

        _browserAddressBarButton = new Button { Text = "Address Bar", Width = 100 };
        _browserAddressBarButton.Click += (_, _) => ExecuteBrowserAction("browser-focus-address-bar", "Browser address bar requested.");

        _browserFindButton = new Button { Text = "Find In Page", Width = 110 };
        _browserFindButton.Click += (_, _) => ExecuteBrowserAction("browser-find", "Browser find in page requested.");

        _browserFindNextButton = new Button { Text = "Find Next", Width = 90 };
        _browserFindNextButton.Click += (_, _) => ExecuteBrowserAction("browser-find-next", "Browser find next requested.");

        _browserFindPreviousButton = new Button { Text = "Find Previous", Width = 110 };
        _browserFindPreviousButton.Click += (_, _) => ExecuteBrowserAction("browser-find-previous", "Browser find previous requested.");

        _browserScrollUpButton = new Button { Text = "Scroll Up", Width = 90 };
        _browserScrollUpButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-up", "Browser scroll up requested.");

        _browserScrollDownButton = new Button { Text = "Scroll Down", Width = 100 };
        _browserScrollDownButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-down", "Browser scroll down requested.");

        _browserScrollTopButton = new Button { Text = "Scroll Top", Width = 95 };
        _browserScrollTopButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-top", "Browser scroll to top requested.");

        _browserScrollBottomButton = new Button { Text = "Scroll Bottom", Width = 110 };
        _browserScrollBottomButton.Click += (_, _) => ExecuteBrowserAction("browser-scroll-bottom", "Browser scroll to bottom requested.");

        _browserZoomInButton = new Button { Text = "Zoom In", Width = 80 };
        _browserZoomInButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-in", "Browser zoom in requested.");

        _browserZoomOutButton = new Button { Text = "Zoom Out", Width = 90 };
        _browserZoomOutButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-out", "Browser zoom out requested.");

        _browserZoomResetButton = new Button { Text = "Zoom Reset", Width = 100 };
        _browserZoomResetButton.Click += (_, _) => ExecuteBrowserAction("browser-zoom-reset", "Browser zoom reset requested.");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Target", _browserInputText, row++);
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
        buttons.Controls.Add(_browserCloseTabButton);
        buttons.Controls.Add(_browserAddressBarButton);
        buttons.Controls.Add(_browserFindButton);
        buttons.Controls.Add(_browserFindNextButton);
        buttons.Controls.Add(_browserFindPreviousButton);
        buttons.Controls.Add(_browserScrollUpButton);
        buttons.Controls.Add(_browserScrollDownButton);
        buttons.Controls.Add(_browserScrollTopButton);
        buttons.Controls.Add(_browserScrollBottomButton);
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
            Text = "System controls stay visible and local: change volume, mute audio, move between windows, manage the active window, open Task Manager, and show the desktop without opening a shell."
        };
        _systemStatusLabel = new Label { AutoSize = true, Text = "No system action run yet." };
        _systemSelectedActionLabel = new Label { AutoSize = true, Text = "Selected action: none." };
        _systemLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _systemVoiceCueLabel = new Label { AutoSize = true, Text = "Voice cue: system is waiting for speech." };
        _systemLastHeardLabel = new Label { AutoSize = true, Text = "Last heard: nothing yet." };

        _systemVolumeUpButton = new Button { Text = "Volume Up", Width = 90 };
        _systemVolumeUpButton.Click += (_, _) => ExecuteSystemAction("system-volume-up", "Volume up requested.");

        _systemVolumeDownButton = new Button { Text = "Volume Down", Width = 100 };
        _systemVolumeDownButton.Click += (_, _) => ExecuteSystemAction("system-volume-down", "Volume down requested.");

        _systemMuteButton = new Button { Text = "Mute", Width = 80 };
        _systemMuteButton.Click += (_, _) => ExecuteSystemAction("system-volume-mute", "Volume mute requested.");

        _systemShowDesktopButton = new Button { Text = "Show Desktop", Width = 110 };
        _systemShowDesktopButton.Click += (_, _) => ExecuteSystemAction("system-show-desktop", "Show desktop requested.");

        _systemNextWindowButton = new Button { Text = "Next Window", Width = 105 };
        _systemNextWindowButton.Click += (_, _) => ExecuteSystemAction("system-next-window", "Next window requested.");

        _systemPreviousWindowButton = new Button { Text = "Previous Window", Width = 120 };
        _systemPreviousWindowButton.Click += (_, _) => ExecuteSystemAction("system-previous-window", "Previous window requested.");

        _systemTaskManagerButton = new Button { Text = "Task Manager", Width = 110 };
        _systemTaskManagerButton.Click += (_, _) => ExecuteSystemAction("system-open-task-manager", "Task Manager requested.");

        _systemMinimizeWindowButton = new Button { Text = "Minimize Window", Width = 125 };
        _systemMinimizeWindowButton.Click += (_, _) => ExecuteSystemAction("system-minimize-window", "Minimize window requested.");

        _systemMaximizeWindowButton = new Button { Text = "Maximize Window", Width = 125 };
        _systemMaximizeWindowButton.Click += (_, _) => ExecuteSystemAction("system-maximize-window", "Maximize window requested.");

        _systemRestoreWindowButton = new Button { Text = "Restore Window", Width = 120 };
        _systemRestoreWindowButton.Click += (_, _) => ExecuteSystemAction("system-restore-window", "Restore window requested.");

        _systemEnterButton = new Button { Text = "Enter", Width = 75 };
        _systemEnterButton.Click += (_, _) => ExecuteSystemAction("system-press-enter", "Enter requested.");

        _systemTabButton = new Button { Text = "Tab", Width = 65 };
        _systemTabButton.Click += (_, _) => ExecuteSystemAction("system-press-tab", "Tab requested.");

        _systemEscapeButton = new Button { Text = "Escape", Width = 75 };
        _systemEscapeButton.Click += (_, _) => ExecuteSystemAction("system-press-escape", "Escape requested.");

        _systemBackspaceButton = new Button { Text = "Backspace", Width = 90 };
        _systemBackspaceButton.Click += (_, _) => ExecuteSystemAction("system-press-backspace", "Backspace requested.");

        _systemUpButton = new Button { Text = "Up", Width = 60 };
        _systemUpButton.Click += (_, _) => ExecuteSystemAction("system-press-up", "Up arrow requested.");

        _systemDownButton = new Button { Text = "Down", Width = 65 };
        _systemDownButton.Click += (_, _) => ExecuteSystemAction("system-press-down", "Down arrow requested.");

        _systemLeftButton = new Button { Text = "Left", Width = 60 };
        _systemLeftButton.Click += (_, _) => ExecuteSystemAction("system-press-left", "Left arrow requested.");

        _systemRightButton = new Button { Text = "Right", Width = 65 };
        _systemRightButton.Click += (_, _) => ExecuteSystemAction("system-press-right", "Right arrow requested.");

        _systemHomeButton = new Button { Text = "Home", Width = 65 };
        _systemHomeButton.Click += (_, _) => ExecuteSystemAction("system-press-home", "Home requested.");

        _systemEndButton = new Button { Text = "End", Width = 60 };
        _systemEndButton.Click += (_, _) => ExecuteSystemAction("system-press-end", "End requested.");

        _systemPageUpButton = new Button { Text = "Page Up", Width = 80 };
        _systemPageUpButton.Click += (_, _) => ExecuteSystemAction("system-page-up", "Page up requested.");

        _systemPageDownButton = new Button { Text = "Page Down", Width = 90 };
        _systemPageDownButton.Click += (_, _) => ExecuteSystemAction("system-page-down", "Page down requested.");

        _systemMouseClickButton = new Button { Text = "Click", Width = 70 };
        _systemMouseClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-click", "Mouse click requested.");

        _systemMouseDoubleClickButton = new Button { Text = "Double Click", Width = 100 };
        _systemMouseDoubleClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-double-click", "Mouse double-click requested.");

        _systemMouseRightClickButton = new Button { Text = "Right Click", Width = 90 };
        _systemMouseRightClickButton.Click += (_, _) => ExecuteSystemAction("system-mouse-right-click", "Mouse right-click requested.");

        _systemMouseScrollUpButton = new Button { Text = "Mouse Scroll Up", Width = 120 };
        _systemMouseScrollUpButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-up", "Mouse scroll up requested.");

        _systemMouseScrollDownButton = new Button { Text = "Mouse Scroll Down", Width = 130 };
        _systemMouseScrollDownButton.Click += (_, _) => ExecuteSystemAction("system-mouse-scroll-down", "Mouse scroll down requested.");

        _systemCopyButton = new Button { Text = "Copy", Width = 65 };
        _systemCopyButton.Click += (_, _) => ExecuteSystemAction("system-copy", "Copy requested.");

        _systemPasteButton = new Button { Text = "Paste", Width = 70 };
        _systemPasteButton.Click += (_, _) => ExecuteSystemAction("system-paste", "Paste requested.");

        _systemCutButton = new Button { Text = "Cut", Width = 55 };
        _systemCutButton.Click += (_, _) => ExecuteSystemAction("system-cut", "Cut requested.");

        _systemSelectAllButton = new Button { Text = "Select All", Width = 90 };
        _systemSelectAllButton.Click += (_, _) => ExecuteSystemAction("system-select-all", "Select all requested.");

        _systemSaveButton = new Button { Text = "Save", Width = 65 };
        _systemSaveButton.Click += (_, _) => ExecuteSystemAction("system-save", "Save requested.");

        _systemUndoButton = new Button { Text = "Undo", Width = 65 };
        _systemUndoButton.Click += (_, _) => ExecuteSystemAction("system-undo", "Undo requested.");

        _systemRedoButton = new Button { Text = "Redo", Width = 65 };
        _systemRedoButton.Click += (_, _) => ExecuteSystemAction("system-redo", "Redo requested.");

        _systemFindButton = new Button { Text = "Find", Width = 65 };
        _systemFindButton.Click += (_, _) => ExecuteSystemAction("system-find", "Find requested.");

        _systemNewWindowButton = new Button { Text = "New Window", Width = 100 };
        _systemNewWindowButton.Click += (_, _) => ExecuteSystemAction("system-new-window", "New window requested.");

        _systemCloseWindowButton = new Button { Text = "Close Window", Width = 100 };
        _systemCloseWindowButton.Click += (_, _) => ExecuteSystemAction("system-close-window", "Close window requested.");

        _systemMovePreviousWordButton = new Button { Text = "Prev Word", Width = 85 };
        _systemMovePreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-word", "Move previous word requested.");

        _systemMoveNextWordButton = new Button { Text = "Next Word", Width = 85 };
        _systemMoveNextWordButton.Click += (_, _) => ExecuteSystemAction("system-move-next-word", "Move next word requested.");

        _systemSelectPreviousWordButton = new Button { Text = "Select Prev Word", Width = 130 };
        _systemSelectPreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-word", "Select previous word requested.");

        _systemSelectNextWordButton = new Button { Text = "Select Next Word", Width = 130 };
        _systemSelectNextWordButton.Click += (_, _) => ExecuteSystemAction("system-select-next-word", "Select next word requested.");

        _systemDeletePreviousWordButton = new Button { Text = "Delete Prev Word", Width = 130 };
        _systemDeletePreviousWordButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-word", "Delete previous word requested.");

        _systemDeleteNextWordButton = new Button { Text = "Delete Next Word", Width = 125 };
        _systemDeleteNextWordButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-word", "Delete next word requested.");

        _systemMovePreviousSentenceButton = new Button { Text = "Prev Sentence", Width = 105 };
        _systemMovePreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-sentence", "Move previous sentence requested.");

        _systemMoveNextSentenceButton = new Button { Text = "Next Sentence", Width = 105 };
        _systemMoveNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-move-next-sentence", "Move next sentence requested.");

        _systemSelectPreviousSentenceButton = new Button { Text = "Select Prev Sentence", Width = 150 };
        _systemSelectPreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-sentence", "Select previous sentence requested.");

        _systemSelectNextSentenceButton = new Button { Text = "Select Next Sentence", Width = 150 };
        _systemSelectNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-select-next-sentence", "Select next sentence requested.");

        _systemDeletePreviousSentenceButton = new Button { Text = "Delete Prev Sentence", Width = 150 };
        _systemDeletePreviousSentenceButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-sentence", "Delete previous sentence requested.");

        _systemDeleteNextSentenceButton = new Button { Text = "Delete Next Sentence", Width = 145 };
        _systemDeleteNextSentenceButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-sentence", "Delete next sentence requested.");

        _systemMovePreviousParagraphButton = new Button { Text = "Prev Paragraph", Width = 110 };
        _systemMovePreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-move-previous-paragraph", "Move previous paragraph requested.");

        _systemMoveNextParagraphButton = new Button { Text = "Next Paragraph", Width = 110 };
        _systemMoveNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-move-next-paragraph", "Move next paragraph requested.");

        _systemSelectPreviousParagraphButton = new Button { Text = "Select Prev Para", Width = 130 };
        _systemSelectPreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-select-previous-paragraph", "Select previous paragraph requested.");

        _systemSelectNextParagraphButton = new Button { Text = "Select Next Para", Width = 130 };
        _systemSelectNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-select-next-paragraph", "Select next paragraph requested.");

        _systemDeletePreviousParagraphButton = new Button { Text = "Delete Prev Para", Width = 130 };
        _systemDeletePreviousParagraphButton.Click += (_, _) => ExecuteSystemAction("system-delete-previous-paragraph", "Delete previous paragraph requested.");

        _systemDeleteNextParagraphButton = new Button { Text = "Delete Next Para", Width = 125 };
        _systemDeleteNextParagraphButton.Click += (_, _) => ExecuteSystemAction("system-delete-next-paragraph", "Delete next paragraph requested.");

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Status", _systemStatusLabel, row++);
        AddRow(layout, "Selected", _systemSelectedActionLabel, row++);
        AddRow(layout, "Last action", _systemLastActionLabel, row++);
        AddRow(layout, "Voice cue", _systemVoiceCueLabel, row++);
        AddRow(layout, "Last heard", _systemLastHeardLabel, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_systemVolumeUpButton);
        buttons.Controls.Add(_systemVolumeDownButton);
        buttons.Controls.Add(_systemMuteButton);
        buttons.Controls.Add(_systemShowDesktopButton);
        buttons.Controls.Add(_systemNextWindowButton);
        buttons.Controls.Add(_systemPreviousWindowButton);
        buttons.Controls.Add(_systemTaskManagerButton);
        buttons.Controls.Add(_systemMinimizeWindowButton);
        buttons.Controls.Add(_systemMaximizeWindowButton);
        buttons.Controls.Add(_systemRestoreWindowButton);
        buttons.Controls.Add(_systemEnterButton);
        buttons.Controls.Add(_systemTabButton);
        buttons.Controls.Add(_systemEscapeButton);
        buttons.Controls.Add(_systemBackspaceButton);
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
        buttons.Controls.Add(_systemMouseScrollUpButton);
        buttons.Controls.Add(_systemMouseScrollDownButton);
        buttons.Controls.Add(_systemCopyButton);
        buttons.Controls.Add(_systemPasteButton);
        buttons.Controls.Add(_systemCutButton);
        buttons.Controls.Add(_systemSelectAllButton);
        buttons.Controls.Add(_systemSaveButton);
        buttons.Controls.Add(_systemUndoButton);
        buttons.Controls.Add(_systemRedoButton);
        buttons.Controls.Add(_systemFindButton);
        buttons.Controls.Add(_systemNewWindowButton);
        buttons.Controls.Add(_systemCloseWindowButton);
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
        var layout = BuildTwoColumnLayout(10);

        var heading = CreateHeading("Search local files and open the result");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Searches the intended alpha scope: common user folders plus Callsign data. Results show clearly, empty states are explained, and selected items can be opened."
        };
        _fileSearchStatusLabel = new Label { AutoSize = true, Text = "No file search run yet." };
        _fileSearchSelectionLabel = new Label { AutoSize = true, Text = "Selected result: none." };
        _fileSearchVoiceCueLabel = new Label { AutoSize = true, Text = "Voice cue: file search is waiting for speech." };
        _fileSearchLastHeardLabel = new Label { AutoSize = true, Text = "Last heard: nothing yet." };
        _fileSearchLastActionLabel = new Label { AutoSize = true, MaximumSize = new Size(900, 0), Text = "Last action: none yet." };
        _fileSearchQueryText = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search for a filename or folder name", AccessibleName = "Search" };
        _fileSearchQueryText.TextChanged += (_, _) => RefreshFileSearchPanel();
        _fileSearchResultsList = new ListBox
        {
            Dock = DockStyle.Fill,
            Height = 220,
            AccessibleName = "Search results"
        };
        _fileSearchResultsList.SelectedIndexChanged += (_, _) => RefreshFileSearchPanel();
        _fileSearchResultsList.DoubleClick += (_, _) => OpenSelectedFileResult();

        _searchFilesButton = new Button { Text = "Search Files", Width = 120 };
        _searchFilesButton.Click += (_, _) => SearchFiles();

        _openFileResultButton = new Button { Text = "Open Result", Width = 110 };
        _openFileResultButton.Click += (_, _) => OpenSelectedFileResult();

        _openFileFolderButton = new Button { Text = "Open Folder", Width = 110 };
        _openFileFolderButton.Click += (_, _) => OpenSelectedFileFolder();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Search", _fileSearchQueryText, row++);
        AddRow(layout, "Status", _fileSearchStatusLabel, row++);
        AddRow(layout, "Selected", _fileSearchSelectionLabel, row++);
        AddRow(layout, "Voice cue", _fileSearchVoiceCueLabel, row++);
        AddRow(layout, "Last heard", _fileSearchLastHeardLabel, row++);
        AddRow(layout, "Last action", _fileSearchLastActionLabel, row++);
        AddFullWidth(layout, _fileSearchResultsList, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_searchFilesButton);
        buttons.Controls.Add(_openFileResultButton);
        buttons.Controls.Add(_openFileFolderButton);
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private static TableLayoutPanel BuildTwoColumnLayout(int rowCount)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
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
            Padding = new Padding(0, 8, 0, 0)
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
            AccessibleName = accessibleName
        };

    private static Label CreateHeading(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
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

    private void ProfilePickerChanged(object? sender, EventArgs e)
    {
        if (_updatingUi || _profilePicker.SelectedIndex < 0 || _profilePicker.SelectedIndex >= _profiles.Count)
            return;

        SelectProfile(_profiles[_profilePicker.SelectedIndex]);
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
        RefreshBrowserPanel();
        RefreshFileSearchPanel();
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
            _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription}";
            _voiceRecordingStateLabel.Text = "No sample recording in progress.";
            _voicePlaybackStateLabel.Text = "No sample available for playback.";
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
        _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription}{GetOpenWakeWordSetupHint(_voiceCommandService.CurrentWakeWordEngine)}{micSummary}";
        _voiceProgress.Maximum = settings.VoiceSamplesRequired;
        _voiceProgress.Value = Math.Min(settings.VoiceSamplesRecorded, _voiceProgress.Maximum);
        _voiceProgress.Style = _voiceActivationBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        _voiceProgress.MarqueeAnimationSpeed = _voiceActivationBusy ? 20 : 0;
        _voicePromptText.Text = GetVoicePrompt(_activeProfile, settings);
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
                    : "Runtime running but no microphone audio is arriving."
                : runtimeIsStale
                    ? $"Background service status is stale. Last update was {Math.Ceiling(runtimeAge.TotalSeconds):0} seconds ago."
                    : "Background service listener is stopped.";
        _wakeReliabilityLabel.Text = string.IsNullOrWhiteSpace(wakeEngine)
            ? $"Wake detector unavailable.{GetOpenWakeWordSetupHint(wakeEngine)}"
            : $"Current wake detector: {wakeEngine}.{GetOpenWakeWordSetupHint(wakeEngine)}";
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
            _micDetailLabel.Text = FormatMicDetails(runtimeSnapshot);
            wakeOverlayShouldBeVisible = !runtimeIsStale
                && runtimeSnapshot.IsListening
                && (IsWakeOverlaySessionActive(runtimeSnapshot.SessionState) || runtimeSnapshot.IsSpeechActive == true);
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

            _sessionResultLabel.Text = _session.State is AlphaSessionState.Idle or AlphaSessionState.Completed
                && !string.IsNullOrWhiteSpace(_activeProfile?.Settings.LastLaunchedApp)
                ? $"Last launched through Start menu: {_activeProfile.Settings.LastLaunchedApp}"
                : _session.StatusMessage;
            _sessionSpeechCueLabel.Text = BuildLocalSessionSpeechCueText(_session.State, _voiceCommandService.IsSpeechActive, _voiceCommandService.LastSpeechActivityUtc, _lastHeardTranscriptText, _lastHeardTranscriptConfidence);
            _micLevelLabel.Text = _voiceCommandService.CurrentAudioTelemetry == null
                ? "Microphone telemetry unavailable."
                : $"Microphone level: {_voiceCommandService.CurrentAudioTelemetry.LevelState}.";
            _wakeCandidateLabel.Text = FormatLocalWakeCandidateReadout();
            _micDetailLabel.Text = _voiceCommandService.CurrentAudioTelemetry == null
                ? "No microphone telemetry yet."
                : $"Raw RMS {_voiceCommandService.CurrentAudioTelemetry.RawRms:0.000}, peak {_voiceCommandService.CurrentAudioTelemetry.RawPeak:0.00}, gain {_voiceCommandService.CurrentAudioTelemetry.AppliedGainDb:0.0} dB, noise floor {_voiceCommandService.CurrentAudioTelemetry.NoiseFloorRms:0.000}, threshold {_voiceCommandService.CurrentAudioTelemetry.SpeechThresholdRms:0.000}.";
            wakeOverlayShouldBeVisible = _voiceCommandService.IsListening
                && (_voiceCommandService.IsSpeechActive || IsWakeOverlaySessionActive(_session.State));
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
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.ServiceDictationText))
                _dictationTextBox.Text = runtimeSnapshot.ServiceDictationText;
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

        if (normalized.StartsWith(visibleControlLabelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var visibleLabel = normalized[visibleControlLabelPrefix.Length..].Trim();
            return TryActivateVisibleControlByLabel(visibleLabel);
        }

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
            case "ui start listening":
                StartVoiceListening();
                return true;
            case "ui stop listening":
                StopVoiceListening();
                return true;
            case "ui voice help":
                ShowVoiceHelp();
                return true;
            case "ui show visible controls":
                ShowVisibleControlsSummary();
                return true;
            case "ui hide visible controls":
                HideVisibleControlsOverlay();
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
                return false;
        }
    }

    private void MoveUiFocus(int direction)
    {
        var fromControl = FindFocusedControl(this) ?? this;
        if (!SelectNextControl(fromControl, direction > 0, true, true, true))
        {
            UpdateStatus("No other control was available to focus.");
            return;
        }

        UpdateStatus(direction > 0 ? "Moved to the next control." : "Moved to the previous control.");
        UpdateVisibleControlsOverlay();
    }

    private void ActivateFocusedUiControl()
    {
        var focused = FindFocusedControl(this);
        if (focused is Button button && button.Enabled)
        {
            button.PerformClick();
            UpdateStatus($"Activated '{button.Text}'.");
            UpdateVisibleControlsOverlay();
            return;
        }

        if (focused is TextBoxBase textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
            UpdateStatus("Selected the active text field.");
            UpdateVisibleControlsOverlay();
            return;
        }

        if (focused is TabControl tabControl)
        {
            UpdateStatus($"Focused tab control on '{tabControl.SelectedTab?.Text}'.");
            UpdateVisibleControlsOverlay();
            return;
        }

        UpdateStatus("Focused control could not be activated directly.");
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
            return true;
        }

        if (TryFindVisibleControlByLabel(this, normalizedLabel, out var control))
        {
            if (control is Button button && button.Enabled)
            {
                button.PerformClick();
                UpdateStatus($"Activated '{button.Text}'.");
                UpdateVisibleControlsOverlay();
                return true;
            }

            if (control is TextBoxBase textBox && textBox.Enabled)
            {
                textBox.Focus();
                textBox.SelectAll();
                UpdateStatus($"Focused '{GetControlVoiceLabel(textBox)}'.");
                UpdateVisibleControlsOverlay();
                return true;
            }

            if (control is ComboBox comboBox && comboBox.Enabled)
            {
                comboBox.Focus();
                UpdateStatus($"Focused '{GetControlVoiceLabel(comboBox)}'.");
                UpdateVisibleControlsOverlay();
                return true;
            }

            if (control is ListBox listBox && listBox.Enabled)
            {
                listBox.Focus();
                if (listBox.Items.Count > 0 && listBox.SelectedIndex < 0)
                    listBox.SelectedIndex = 0;
                UpdateStatus($"Focused '{GetControlVoiceLabel(listBox)}'.");
                UpdateVisibleControlsOverlay();
                return true;
            }

            if (control is TabPage tabPage && tabPage.Parent is TabControl tabControl)
            {
                tabControl.SelectedTab = tabPage;
                UpdateStatus($"Opened tab '{tabPage.Text}'.");
                UpdateVisibleControlsOverlay();
                return true;
            }
        }

        if (IsListeningLabel(normalizedLabel))
        {
            StartVoiceListening();
            UpdateStatus("Started listening.");
            UpdateVisibleControlsOverlay();
            return true;
        }

        UpdateStatus($"No visible control matched '{label}'.");
        return true;
    }

    private bool TryActivateVisibleControlByNumber(string normalizedLabel, out string message)
    {
        message = string.Empty;

        if (!int.TryParse(normalizedLabel, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number <= 0)
            return false;

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
        UpdateStatus("Opened voice help.");
    }

    private static string BuildVoiceHelpText()
    {
        return
            "Callsign voice help:\n\n" +
            "The live flow is:\n" +
            "- say Callsign\n" +
            "- watch the GIF and live text cue\n" +
            "- say your callsign\n" +
            "- say the command or app name\n\n" +
            "Setup actions:\n" +
            "- repair wakeword\n" +
            "- train voice identity\n" +
            "- create new account\n" +
            "- save account\n" +
            "- delete account\n" +
            "- open data folder\n" +
            "- open logs folder\n" +
            "- open app folder\n" +
            "- start listening\n" +
            "- stop listening\n" +
            "- voice help / what can I say\n\n" +
            "Navigation:\n" +
            "- next tab\n" +
            "- previous tab\n" +
            "- open account / voice / session / dictation / browser / files / system\n" +
            "- next control\n" +
            "- previous control\n" +
            "- activate control\n\n" +
            "Voice features:\n" +
            "- press / click / activate <visible control>\n" +
            "- show numbers / show visible controls\n" +
            "- click 1 / click 2 / click 3 after show numbers\n" +
            "- hide visible controls\n" +
            "- click callsign / click active account / click notes\n" +
            "- start dictation\n" +
            "- stop dictation / end dictation / finish dictation\n" +
            "- copy that / paste that / clear dictation / select all / cut dictation / undo dictation / redo dictation\n" +
            "- go to start / go to end / new line / new paragraph\n" +
            "- replace previous word / phrase / sentence / paragraph with <text>\n" +
            "- change previous word / phrase / sentence / paragraph to <text>\n" +
            "- correct previous word / phrase / sentence / paragraph with <text>\n" +
            "- spell out / spell it out / type out <letters or words>\n" +
            "- browser back / refresh / new tab / close tab\n" +
            "- find file <name> / search my pc for <name> / find a file called <name>\n" +
            "- open file explorer / show file explorer / open settings / show settings\n" +
            "- volume up / down / mute\n" +
            "- show desktop\n" +
            "- task manager\n" +
            "- click / double click / right click\n\n" +
            "Safety:\n" +
            "- cancel\n" +
            "- stop listening\n" +
            "- reset session";
    }

    private void ShowVisibleControlsSummary()
    {
        UpdateVisibleControlsOverlay();
        UpdateStatus("Opened visible controls summary.");
    }

    private void UpdateVisibleControlsOverlay()
    {
        if (_visibleControlsOverlay == null || !_visibleControlsOverlay.Visible)
            return;

        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        var focusedControl = FindFocusedControl(this);
        var summary = BuildVisibleControlsSummary();
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
        var items = _visibleControlsSummary.Select(entry => $"{entry.Number}. {entry.Label}").ToList();
        var annotations = _visibleControlsSummary
            .Select(entry => new VisibleControlOverlayAnnotation(
                entry.Number,
                entry.Control.RectangleToScreen(entry.Control.ClientRectangle),
                entry.Label,
                ReferenceEquals(entry.Control, focusedControl)))
            .ToList();
        ShowVisibleControlsOverlay(summary, cue, heard, items, annotations);
    }

    private void ShowVisibleControlsOverlay(string summary, string cue, string heard, IReadOnlyList<string> numberedItems, IReadOnlyList<VisibleControlOverlayAnnotation> annotations)
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

        _visibleControlsOverlay.ShowOverlay(Bounds, summary, cue, heard, numberedItems, annotations);
        if (!_visibleControlsRefreshTimer.Enabled)
            _visibleControlsRefreshTimer.Start();
    }

    private void HideVisibleControlsOverlay()
    {
        _visibleControlsOverlay?.HideOverlay();
        if (_visibleControlsRefreshTimer.Enabled)
            _visibleControlsRefreshTimer.Stop();
    }

    private string BuildVisibleControlsSummary()
    {
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
            _dictationStatusLabel.Text = $"Dictation is active. Captured {_dictationTextBox.Text.Length} characters.";
            _dictationSpeechCueLabel.Text = _voiceCommandService.IsSpeechActive
                ? "Speech cue: Hearing dictation..."
                : _dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
                    ? $"Speech cue: Heard {_dictationLastTranscriptText}"
                    : "Speech cue: dictation is waiting for speech.";
        }
        else if (_dictationLastTranscriptUtc.HasValue)
        {
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}.";
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
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}.";
            _dictationSpeechCueLabel.Text = BuildLocalDictationSpeechCueText();
        }

        _startDictationButton.Enabled = !_dictationActive;
        _stopDictationButton.Enabled = _dictationActive;
        _copyDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _pasteDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _clearDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _dictationLastHeardLabel.Text = _dictationLastTranscriptUtc.HasValue && !string.IsNullOrWhiteSpace(_dictationLastTranscriptText)
            ? FormatLastHeardLabel(_dictationLastTranscriptText)
            : "Last heard: nothing yet.";
    }

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
    }

    private void RefreshFileSearchPanel()
    {
        if (_fileSearchStatusLabel == null)
            return;

        var query = _fileSearchQueryText?.Text?.Trim();
        _fileSearchStatusLabel.Text = string.IsNullOrWhiteSpace(query)
            ? "No file search run yet."
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
        _openFileResultButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
        _openFileFolderButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
        _fileSearchSelectionLabel.Text = _fileSearchResultsList?.SelectedItem is FileSearchResult selected
            ? $"Selected result: {(selected.IsDirectory ? "folder" : "file")} {selected.Name}."
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
        if (_voiceCommandService.IsListening || _dictationActive || IsWakeOverlaySessionActive(_session.State))
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
                settings.VoiceEnrollmentStatus = "pyannote setup required";
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
            var wakeScores = new List<double>();
            foreach (var samplePath in samplePaths)
            {
                var score = await _voiceCommandService.TryScoreWakeWordSampleAsync(samplePath, CancellationToken.None);
                if (score.HasValue)
                    wakeScores.Add(score.Value);
            }

            if (wakeScores.Count > 0)
            {
                var calibratedThreshold = VoiceCommandService.ComputeCalibratedWakeThreshold(wakeScores.Max());
                if (calibratedThreshold.HasValue)
                {
                    settings.VoiceWakeThreshold = calibratedThreshold.Value;
                    settings.VoiceWakeSensitivity = "More responsive";
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
        _session.DetectWakeWord();
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
            return;
        }

        if (!_voiceCommandService.IsListening)
        {
            UpdateListeningPanel();
            UpdateStatus("Voice listener is already stopped.");
            return;
        }

        _voiceCommandService.Stop();
        _usingLocalPreviewListener = false;
        UpdateListeningPanel();
        UpdateStatus("Voice listener stopped.");
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
        if (!File.Exists(runtimeExe))
        {
            message = "Installed user runtime was not found; using local preview listener.";
            return InstalledUserRuntimeStartResult.Unavailable;
        }

        var runtimeSnapshot = _runtimeStateMonitor.Read();
        var runtimeIsFresh = runtimeSnapshot != null
            && string.Equals(runtimeSnapshot.RuntimeRole, "user-runtime", StringComparison.OrdinalIgnoreCase)
            && runtimeSnapshot.IsListening
            && DateTime.UtcNow - runtimeSnapshot.UpdatedUtc.ToUniversalTime() <= TimeSpan.FromSeconds(15);
        var runtimeCanHearAudio = runtimeIsFresh && runtimeSnapshot?.CanHearAudio == true;
        var runningProcessCount = 0;
        try
        {
            runningProcessCount = Process.GetProcessesByName("Callsign.Service").Length;
        }
        catch
        {
            runningProcessCount = 0;
        }

        if (runtimeCanHearAudio && runningProcessCount > 0)
        {
            message = "Background user runtime is already authoritative and hearing audio.";
            return InstalledUserRuntimeStartResult.AlreadyRunning;
        }

        if (runningProcessCount > 0 && runtimeSnapshot != null)
        {
            if (runtimeSnapshot.CanHearAudio == false)
            {
                message = "Background user runtime is already running but not hearing microphone audio yet. Use the Session tab to verify the listener or restart the runtime.";
                return InstalledUserRuntimeStartResult.AlreadyRunning;
            }

            message = "Background user runtime is already running as the authoritative listener. Watch the Session tab for fresh user-runtime status.";
            return InstalledUserRuntimeStartResult.AlreadyRunning;
        }

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

            message = "Requested background user runtime start. Watch the Session tab for authoritative user-runtime status.";
            return InstalledUserRuntimeStartResult.Started;
        }
        catch (Exception ex)
        {
            message = $"Unable to start installed user runtime; using local preview listener. {ex.Message}";
            return InstalledUserRuntimeStartResult.Unavailable;
        }
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
            _session.DetectWakeWord();
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
                _session.DetectWakeWord();
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
            UpdateListeningPanel();
            UpdateStatus(e.Message);
        });
    }

    private void HandleVoiceTranscript(string displayTranscript, string transcript, float confidence)
    {
        AppendSessionTranscriptHistory($"[{DateTime.Now:t}] {displayTranscript} ({confidence:P0})");
        _lastHeardTranscriptText = displayTranscript;
        _lastHeardTranscriptConfidence = confidence;
        _lastHeardLabel.Text = FormatLastHeardLabel(displayTranscript, confidence);
        if (_voiceCommandService.IsListening || _dictationActive || IsWakeOverlaySessionActive(_session.State))
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
            if (uiNavigationIntent.ContainsCallsign && uiNavigationIntent.Kind == AlphaVoiceIntentKind.UiNavigation)
            {
                ApplyRequestedUiMode(uiNavigationIntent.Target);
                _sessionResultLabel.Text = $"Voice navigation: open {uiNavigationIntent.Target} tab.";
                RefreshSessionPanel();
                UpdateStatus($"Voice navigation moved to the {uiNavigationIntent.Target.ToLowerInvariant()} tab.");
                return;
            }

            if (uiNavigationIntent.ContainsCallsign && uiNavigationIntent.Kind == AlphaVoiceIntentKind.UiAction)
            {
                if (TryExecuteRequestedUiAction(uiNavigationIntent.Target))
                {
                    _sessionResultLabel.Text = $"Voice action: {uiNavigationIntent.Target}.";
                    RefreshSessionPanel();
                    UpdateStatus($"Voice action executed: {uiNavigationIntent.Target.Replace("ui-", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("-", " ", StringComparison.OrdinalIgnoreCase)}.");
                    return;
                }
            }

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
                    UpdateStatus("Identity verified, but I could not parse a clear app name. Try: 'Callsign <callsign> open Notepad'.");
                }
                else
                {
                    UpdateStatus("Identity verified. Say an app command like 'open Notepad'.");
                }
            }
        }

        if (_session.State == AlphaSessionState.ReadyToLaunch
            && !string.IsNullOrWhiteSpace(_appNameText.Text))
        {
            _sessionResultLabel.Text = $"Action intent: launch '{_appNameText.Text.Trim()}' through Start menu search.";
            LaunchAppFromStartMenu();
        }
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

        _dictationLastTranscriptUtc = DateTime.UtcNow;
        _dictationLastTranscriptText = normalized;
        AppendDictationHistory(normalized);
        if (_dictationLastHeardLabel != null)
            _dictationLastHeardLabel.Text = FormatLastHeardLabel(normalized);
        if (_dictationTextBox.TextLength > 0 && !_dictationTextBox.Text.EndsWith(" "))
            _dictationTextBox.AppendText(" ");

        _dictationTextBox.AppendText(normalized);
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
                _dictationHistoryList.Items.Add(entry.Trim());
        }
        finally
        {
            _dictationHistoryList.EndUpdate();
        }
    }

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
        UpdateStatus(message);
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
            if (_launcher.TryResolveInstalledAppName(inferredApp, out var resolvedApp))
                _appNameText.Text = resolvedApp;
            else
                _appNameText.Text = inferredApp;
        }

        RefreshSessionPanel();
        UpdateStatus(message);
    }

    private void LaunchAppFromStartMenu()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        var target = string.IsNullOrWhiteSpace(_appNameText.Text)
            ? InferAppName(_spokenCommandText.Text)
            : _appNameText.Text.Trim();

        if (!string.IsNullOrWhiteSpace(target) && _launcher.TryResolveInstalledAppName(target, out var resolvedTarget))
            target = resolvedTarget;

        if (!_session.TryBeginLaunch(target, out var beginMessage))
        {
            RefreshSessionPanel();
            UpdateStatus(beginMessage);
            return;
        }

        RefreshSessionPanel();
        if (_launcher.Launch(target, out var launchMessage))
        {
            profile.Settings.LastLaunchedApp = target;
            SaveVoiceState(profile);
            _profileStore.Save(profile);
            var auditRecorded = _auditLog.TryRecordStartMenuLaunch(profile, target, out var auditWarning);
            _session.CompleteLaunch();
            _spokenCallsignText.Text = string.Empty;
            _spokenCommandText.Text = string.Empty;
            _appNameText.Text = string.Empty;
            RefreshAllPanels();
            _sessionResultLabel.Text = $"Launched '{target}' through Start menu search.";
            UpdateStatus(auditRecorded
                ? $"Launched '{target}' from Start menu and recorded the local alpha audit event. Say 'Callsign {profile.Callsign}' to launch another app."
                : auditWarning ?? $"Launched '{target}' from Start menu, but audit logging reported a warning.");
            return;
        }

        _session.FailLaunch(launchMessage);
        RefreshSessionPanel();
        UpdateStatus(launchMessage);
    }

    private void StartDictation()
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        _dictationActive = true;
        _dictationStartedUtc = DateTime.UtcNow;
        _dictationLastTranscriptUtc = null;
        _dictationHistoryEntries.Clear();
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
            return;
        }

        RefreshDictationPanel();
        UpdateListeningPanel();
        UpdateStatus("Dictation started. Speak naturally and watch the text appear below.");
    }

    private void StopDictation()
    {
        _dictationActive = false;
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
    }

    private void ClearDictationText()
    {
        _dictationTextBox.Clear();
        _dictationHistoryEntries.Clear();
        SetDictationHistory(_dictationHistoryEntries);
        _dictationLastTranscriptText = null;
        if (_dictationLastHeardLabel != null)
            _dictationLastHeardLabel.Text = "Last heard: nothing yet.";
        RefreshDictationPanel();
        UpdateStatus("Dictation text cleared.");
    }

    private void CutDictationText()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to cut.");
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
    }

    private void UndoDictationText()
    {
        _dictationTextBox.Focus();
        if (_dictationTextBox.CanUndo)
        {
            _dictationTextBox.Undo();
            RefreshDictationPanel();
            UpdateStatus("Dictation edit undone.");
            return;
        }

        UpdateStatus("There is no dictation edit to undo.");
    }

    private void RedoDictationText()
    {
        _dictationTextBox.Focus();
        SendKeys.SendWait("^y");
        RefreshDictationPanel();
        UpdateStatus("Dictation edit redone.");
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

    private void InsertDictationPunctuation(string punctuation)
    {
        _dictationTextBox.Focus();
        _dictationTextBox.SelectedText = punctuation;
        RefreshDictationPanel();
        UpdateStatus($"Inserted {punctuation.Trim()} punctuation.");
    }

    private void InsertDictationSpelledText(string text)
    {
        var spelledText = text.Trim();
        if (string.IsNullOrWhiteSpace(spelledText))
        {
            UpdateStatus("No spelled text was captured.");
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
    }

    private void ReplaceDictationSpan(DictationReplacementCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReplacementText))
        {
            UpdateStatus("No replacement text was captured.");
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

        if (AlphaVoiceTranscriptParser.TryParseDictationSpellingCommand(transcript, out var spellingCommand) && spellingCommand != null)
            return ExecuteDictationVoiceAction(() => InsertDictationSpelledText(spellingCommand.Text), $"spell out '{spellingCommand.Text}'");

        return AlphaVoiceTranscriptParser.ParseDictationVoiceAction(transcript) switch
        {
            DictationVoiceAction.Copy => ExecuteDictationVoiceAction(CopyDictationText, "copy the dictated text"),
            DictationVoiceAction.Paste => ExecuteDictationVoiceAction(PasteDictationText, "paste the dictated text"),
            DictationVoiceAction.Clear => ExecuteDictationVoiceAction(ClearDictationText, "clear the dictated text"),
            DictationVoiceAction.SelectAll => ExecuteDictationVoiceAction(SelectAllDictationText, "select all dictated text"),
            DictationVoiceAction.Cut => ExecuteDictationVoiceAction(CutDictationText, "cut the dictated text"),
            DictationVoiceAction.Undo => ExecuteDictationVoiceAction(UndoDictationText, "undo the last dictated edit"),
            DictationVoiceAction.Redo => ExecuteDictationVoiceAction(RedoDictationText, "redo the last dictated edit"),
            DictationVoiceAction.GoToStart => ExecuteDictationVoiceAction(GoToStartDictationText, "go to the start of the dictated text"),
            DictationVoiceAction.GoToEnd => ExecuteDictationVoiceAction(GoToEndDictationText, "go to the end of the dictated text"),
            DictationVoiceAction.SelectToStart => ExecuteDictationVoiceAction(SelectToStartDictationText, "select to the start of the dictated text"),
            DictationVoiceAction.SelectToEnd => ExecuteDictationVoiceAction(SelectToEndDictationText, "select to the end of the dictated text"),
            DictationVoiceAction.DeleteToStart => ExecuteDictationVoiceAction(DeleteToStartDictationText, "delete to the start of the dictated text"),
            DictationVoiceAction.DeleteToEnd => ExecuteDictationVoiceAction(DeleteToEndDictationText, "delete to the end of the dictated text"),
            DictationVoiceAction.GoToLineStart => ExecuteDictationVoiceAction(GoToLineStartDictationText, "go to the start of the current line"),
            DictationVoiceAction.GoToLineEnd => ExecuteDictationVoiceAction(GoToLineEndDictationText, "go to the end of the current line"),
            DictationVoiceAction.SelectToLineStart => ExecuteDictationVoiceAction(SelectToLineStartDictationText, "select to the start of the current line"),
            DictationVoiceAction.SelectToLineEnd => ExecuteDictationVoiceAction(SelectToLineEndDictationText, "select to the end of the current line"),
            DictationVoiceAction.DeleteToLineStart => ExecuteDictationVoiceAction(DeleteToLineStartDictationText, "delete to the start of the current line"),
            DictationVoiceAction.DeleteToLineEnd => ExecuteDictationVoiceAction(DeleteToLineEndDictationText, "delete to the end of the current line"),
            DictationVoiceAction.GoToParagraphStart => ExecuteDictationVoiceAction(GoToParagraphStartDictationText, "go to the start of the current paragraph"),
            DictationVoiceAction.GoToParagraphEnd => ExecuteDictationVoiceAction(GoToParagraphEndDictationText, "go to the end of the current paragraph"),
            DictationVoiceAction.SelectToParagraphStart => ExecuteDictationVoiceAction(SelectToParagraphStartDictationText, "select to the start of the current paragraph"),
            DictationVoiceAction.SelectToParagraphEnd => ExecuteDictationVoiceAction(SelectToParagraphEndDictationText, "select to the end of the current paragraph"),
            DictationVoiceAction.DeleteToParagraphStart => ExecuteDictationVoiceAction(DeleteToParagraphStartDictationText, "delete to the start of the current paragraph"),
            DictationVoiceAction.DeleteToParagraphEnd => ExecuteDictationVoiceAction(DeleteToParagraphEndDictationText, "delete to the end of the current paragraph"),
            DictationVoiceAction.NewLine => ExecuteDictationVoiceAction(InsertDictationLineBreak, "insert a new line"),
            DictationVoiceAction.NewParagraph => ExecuteDictationVoiceAction(InsertDictationParagraphBreak, "insert a new paragraph"),
            DictationVoiceAction.DeleteLastWord => ExecuteDictationVoiceAction(DeleteLastDictationWord, "delete the last word"),
            DictationVoiceAction.SelectPreviousWord => ExecuteDictationVoiceAction(SelectPreviousDictationWord, "select the previous word"),
            DictationVoiceAction.SelectNextWord => ExecuteDictationVoiceAction(SelectNextDictationWord, "select the next word"),
            DictationVoiceAction.DeletePreviousWord => ExecuteDictationVoiceAction(DeletePreviousDictationWord, "delete the previous word"),
            DictationVoiceAction.SelectPreviousSentence => ExecuteDictationVoiceAction(SelectPreviousDictationSentence, "select the previous sentence"),
            DictationVoiceAction.SelectNextSentence => ExecuteDictationVoiceAction(SelectNextDictationSentence, "select the next sentence"),
            DictationVoiceAction.DeletePreviousSentence => ExecuteDictationVoiceAction(DeletePreviousDictationSentence, "delete the previous sentence"),
            DictationVoiceAction.Comma => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(", "), "insert a comma"),
            DictationVoiceAction.Period => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(". "), "insert a period"),
            DictationVoiceAction.QuestionMark => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("? "), "insert a question mark"),
            DictationVoiceAction.ExclamationMark => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("! "), "insert an exclamation point"),
            DictationVoiceAction.Semicolon => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("; "), "insert a semicolon"),
            DictationVoiceAction.Colon => ExecuteDictationVoiceAction(() => InsertDictationPunctuation(": "), "insert a colon"),
            DictationVoiceAction.Apostrophe => ExecuteDictationVoiceAction(() => InsertDictationPunctuation("'"), "insert an apostrophe"),
            _ => false
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

    private bool ExecuteDictationVoiceAction(Action action, string description)
    {
        action();
        UpdateStatus($"Voice command: {description}.");
        return true;
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
            return;
        }

        Clipboard.SetText(_dictationTextBox.Text);
        UpdateStatus("Dictated text copied to the clipboard.");
    }

    private void PasteDictationText()
    {
        if (string.IsNullOrWhiteSpace(_dictationTextBox.Text))
        {
            UpdateStatus("There is no dictated text to paste.");
            return;
        }

        Clipboard.SetText(_dictationTextBox.Text);
        SendKeys.SendWait("^v");
        UpdateStatus("Dictated text copied to the clipboard and sent as a paste request.");
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

    private void ExecuteBrowserAction(string action, string statusMessage)
    {
        if (!_browserLaunchService.TryExecuteBrowserAction(action, out var message))
        {
            _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action, message, succeeded: false);
            UpdateStatus(message);
            return;
        }

        _lastLocalBrowserActionLabel = FormatLocalSystemActionLabel(action, statusMessage, succeeded: true);
        UpdateStatus(statusMessage);
    }

    private void ExecuteSystemAction(string action, string statusMessage)
    {
        if (!_systemControlService.TryExecute(action, out var message))
        {
            if (_systemStatusLabel != null)
                _systemStatusLabel.Text = message;
            _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, message, succeeded: false);
            UpdateStatus(message);
            RefreshSystemPanel();
            return;
        }

        if (_systemStatusLabel != null)
            _systemStatusLabel.Text = statusMessage;
        _lastLocalSystemActionLabel = FormatLocalSystemActionLabel(action, statusMessage, succeeded: true);
        UpdateStatus(statusMessage);
        RefreshSystemPanel();
    }

    private void SearchFiles()
    {
        var query = _fileSearchQueryText.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
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

        UpdateStatus(message);
    }

    private void OpenSelectedFileResult()
    {
        if (_fileSearchResultsList.SelectedItem is not FileSearchResult result)
        {
            UpdateStatus("Select a file search result first.");
            return;
        }

        if (_fileSearchService.TryOpen(result, out var message))
        {
            UpdateStatus(message);
            return;
        }

        UpdateStatus(message);
    }

    private void OpenSelectedFileFolder()
    {
        if (_fileSearchResultsList.SelectedItem is not FileSearchResult result)
        {
            UpdateStatus("Select a file search result first.");
            return;
        }

        var folderPath = result.IsDirectory ? result.FullPath : Path.GetDirectoryName(result.FullPath);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            UpdateStatus("Could not resolve the folder for the selected item.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
            UpdateStatus($"Opened folder in Explorer: '{folderPath}'.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to open the folder: {ex.Message}");
        }
    }

    private void CancelSession()
    {
        StopVoiceSampleRecording(commit: false);
        _session.Cancel("Session cancelled.");
        RefreshSessionPanel();
        UpdateStatus("Session cancelled.");
    }

    private void ResetSession()
    {
        StopVoiceSampleRecording(commit: false);
        if (_voiceCommandService.IsListening)
            StopVoiceListening();

        _session.Reset();
        RefreshSessionPanel();
        UpdateStatus("Session reset to idle.");
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
    {
        if (runtimeSnapshot != null)
        {
            if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RuntimeAuthorityStatus))
            {
                var status = runtimeSnapshot.RuntimeAuthorityStatus.Trim();
                return string.Equals(status, "authoritative-user-runtime", StringComparison.OrdinalIgnoreCase)
                    ? runtimeSnapshot.CanHearAudio == true
                        ? "Authoritative user runtime hearing audio"
                        : "Authoritative user runtime running but silent"
                    : status;
            }

            return runtimeSnapshot.IsListening
                ? runtimeSnapshot.CanHearAudio == true
                    ? "Background service hearing audio"
                    : "Background service running but silent"
                : "Background service idle";
        }

        if (_voiceCommandService.IsListening)
            return _usingLocalPreviewListener
                ? "Local preview listener"
                : "Authoritative user runtime";

        return "Idle";
    }

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
                        : "Background user runtime is running but not hearing microphone audio."
                : "Microphone listener is stopped.";
        _startListeningButton.Enabled = !anyListenerRunning && !stopRequestPending;
        _stopListeningButton.Enabled = anyListenerRunning && !stopRequestPending;

        if (!anyListenerRunning)
            HideVisibleControlsOverlay();
    }

    private static string FormatMicLevel(RuntimeStateSnapshot snapshot)
    {
        if (snapshot.LastMicrophoneLevelState == null)
            return "Microphone telemetry unavailable.";

        if (snapshot.CanHearAudio == false && snapshot.IsListening)
            return "Runtime running but no microphone audio is arriving.";

        if (snapshot.CanHearAudio == true && string.Equals(snapshot.RuntimeAuthorityStatus, "authoritative-user-runtime", StringComparison.OrdinalIgnoreCase))
            return $"Microphone level: {snapshot.LastMicrophoneLevelState}. Authoritative runtime is hearing audio.";

        return snapshot.LastWakeWordScore.HasValue && snapshot.WakeWordThreshold.HasValue
            ? snapshot.LastWakeWordScore.Value >= snapshot.WakeWordThreshold.Value
                ? $"Microphone level: {snapshot.LastMicrophoneLevelState}. Wake candidate passed threshold."
                : $"Microphone level: {snapshot.LastMicrophoneLevelState}. Wake candidate heard but below threshold."
            : $"Microphone level: {snapshot.LastMicrophoneLevelState}.";
    }

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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _runtimeStateMonitor.Changed -= RuntimeStateMonitorChanged;
        _runtimeStateMonitor.Dispose();
        _voiceSampleCapture.Dispose();
        _voiceCommandService.Dispose();
        _wakeOverlay?.HideOverlay();
        _wakeOverlay?.Dispose();
        _wakeOverlay = null;
        _visibleControlsOverlay?.HideOverlay();
        _visibleControlsOverlay?.Dispose();
        _visibleControlsOverlay = null;
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
}
