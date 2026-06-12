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
    private readonly BrowserLaunchService _browserLaunchService = new();
    private readonly FileSearchService _fileSearchService = new();
    private readonly System.Windows.Forms.Timer _sessionTimer = new() { Interval = 1000 };

    private readonly List<UserProfile> _profiles = [];
    private UserProfile? _activeProfile;
    private bool _updatingUi;
    private bool _formReadyForListener;
    private bool _dictationActive;

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
    private TextBox _voicePromptText = null!;
    private ProgressBar _voiceProgress = null!;

    private Label _sessionStateLabel = null!;
    private Label _sessionIdentityLabel = null!;
    private Label _sessionCommandLabel = null!;
    private Label _sessionCountdownLabel = null!;
    private Label _sessionResultLabel = null!;
    private Label _listeningStateLabel = null!;
    private Label _lastHeardLabel = null!;
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

    private TextBox _dictationTextBox = null!;
    private Label _dictationStatusLabel = null!;
    private Label _dictationHintLabel = null!;
    private Button _startDictationButton = null!;
    private Button _stopDictationButton = null!;
    private Button _copyDictationButton = null!;
    private Button _pasteDictationButton = null!;
    private Button _clearDictationButton = null!;

    private TextBox _browserInputText = null!;
    private Label _browserStatusLabel = null!;
    private Button _openBrowserButton = null!;
    private Button _searchBrowserButton = null!;
    private Button _copyBrowserTargetButton = null!;

    private TextBox _fileSearchQueryText = null!;
    private Label _fileSearchStatusLabel = null!;
    private ListBox _fileSearchResultsList = null!;
    private Button _searchFilesButton = null!;
    private Button _openFileResultButton = null!;
    private Button _openFileFolderButton = null!;

    private DateTime? _dictationStartedUtc;
    private DateTime? _dictationLastTranscriptUtc;

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
        _voiceCommandService.RecognitionError += VoiceRecognitionError;
        _voiceCommandService.ListeningStateChanged += (_, _) => RunOnUiThread(() =>
        {
            UpdateListeningPanel();
            RefreshVoicePanel();
        });

        _sessionTimer.Tick += (_, _) => OnSessionTick();
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

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildAccountTab());
        tabs.TabPages.Add(BuildVoiceTab());
        tabs.TabPages.Add(BuildSessionTab());
        tabs.TabPages.Add(BuildDictationTab());
        tabs.TabPages.Add(BuildBrowserTab());
        tabs.TabPages.Add(BuildFileSearchTab());
        root.Controls.Add(tabs, 0, 0);

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

        _profilePicker = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _profilePicker.SelectedIndexChanged += ProfilePickerChanged;

        _callsignText = BuildTextInput();
        _displayNameText = BuildTextInput();
        _emailText = BuildTextInput();
        _departmentText = BuildTextInput();
        _notesText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 80
        };

        _newProfileButton = new Button { Text = "Create New", Width = 130 };
        _newProfileButton.Click += (_, _) => CreateNewProfile();

        _saveProfileButton = new Button { Text = "Save Account", Width = 130 };
        _saveProfileButton.Click += (_, _) => SaveProfile();

        _deleteProfileButton = new Button { Text = "Delete Account", Width = 130 };
        _deleteProfileButton.Click += (_, _) => DeleteProfile();

        var openFolderButton = new Button { Text = "Open Data Folder", Width = 150 };
        openFolderButton.Click += (_, _) => OpenProfileFolder();

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
        layout.Controls.Add(buttons, 1, row);
        layout.SetColumnSpan(buttons, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildVoiceTab()
    {
        var tab = new TabPage("Voice");
        var layout = BuildTwoColumnLayout(10);

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
            ScrollBars = ScrollBars.Vertical
        };

        _voiceProgress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 3, Value = 0 };
        _voiceStateLabel = new Label { AutoSize = true, Text = "Not activated." };
        _voiceSamplesLabel = new Label { AutoSize = true, Text = "0 / 3 samples" };
        _voiceLastTrainedLabel = new Label { AutoSize = true, Text = "Never activated." };
        _voiceRecognitionModeLabel = new Label { AutoSize = true, Text = "Recognition mode: initializing..." };
        _voiceRecordingStateLabel = new Label { AutoSize = true, Text = "No sample recording in progress." };
        _voicePlaybackStateLabel = new Label { AutoSize = true, Text = "No sample available for playback." };

        _recordSampleButton = new Button
        {
            Text = "● Hold to Record",
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

        _trainVoiceButton = new Button { Text = "Activate Voice", Width = 130 };
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
        var layout = BuildTwoColumnLayout(15);

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

        _spokenCallsignText = BuildTextInput();
        _spokenCommandText = BuildTextInput();
        _appNameText = BuildTextInput();
        _voicePhraseText = BuildTextInput();
        _voicePhraseText.PlaceholderText = "Callsign Alpha open Notepad";

        _sessionStateLabel = new Label { AutoSize = true, Text = "Idle." };
        _sessionIdentityLabel = new Label { AutoSize = true, Text = "Waiting for wake word." };
        _sessionCommandLabel = new Label { AutoSize = true, Text = "No command captured." };
        _sessionCountdownLabel = new Label { AutoSize = true, Text = "No timer running." };
        _sessionResultLabel = new Label { AutoSize = true, Text = "No launch yet." };
        _listeningStateLabel = new Label { AutoSize = true, Text = "Microphone listener is stopped." };
        _lastHeardLabel = new Label { AutoSize = true, MaximumSize = new Size(760, 0), Text = "Nothing heard yet." };

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
        AddRow(layout, "Listener", _listeningStateLabel, row++);
        AddRow(layout, "Last heard", _lastHeardLabel, row++);
        AddRow(layout, "Spoken callsign", _spokenCallsignText, row++);
        AddRow(layout, "Spoken command", _spokenCommandText, row++);
        AddRow(layout, "App to launch", _appNameText, row++);
        AddRow(layout, "State", _sessionStateLabel, row++);
        AddRow(layout, "Identity", _sessionIdentityLabel, row++);
        AddRow(layout, "Command", _sessionCommandLabel, row++);
        AddRow(layout, "Timeout", _sessionCountdownLabel, row++);
        AddRow(layout, "Result", _sessionResultLabel, row++);

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
        var layout = BuildTwoColumnLayout(9);

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
        _dictationTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 180
        };
        _dictationTextBox.TextChanged += (_, _) => RefreshDictationPanel();

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

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddFullWidth(layout, _dictationHintLabel, row++);
        AddRow(layout, "Dictation status", _dictationStatusLabel, row++);
        AddRow(layout, "Dictated text", _dictationTextBox, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_startDictationButton);
        buttons.Controls.Add(_stopDictationButton);
        buttons.Controls.Add(_copyDictationButton);
        buttons.Controls.Add(_pasteDictationButton);
        buttons.Controls.Add(_clearDictationButton);
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
        _browserInputText = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search the web or open a URL, such as example.com or callsign desktop assistant" };
        _browserInputText.TextChanged += (_, _) => RefreshBrowserPanel();

        _openBrowserButton = new Button { Text = "Open / Search", Width = 130 };
        _openBrowserButton.Click += (_, _) => OpenBrowserTarget();

        _searchBrowserButton = new Button { Text = "Search Web", Width = 110 };
        _searchBrowserButton.Click += (_, _) => OpenBrowserTarget(forceSearch: true);

        _copyBrowserTargetButton = new Button { Text = "Copy Target", Width = 110 };
        _copyBrowserTargetButton.Click += (_, _) => CopyBrowserTarget();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Target", _browserInputText, row++);
        AddRow(layout, "Status", _browserStatusLabel, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_openBrowserButton);
        buttons.Controls.Add(_searchBrowserButton);
        buttons.Controls.Add(_copyBrowserTargetButton);
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
        _fileSearchQueryText = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search for a filename or folder name" };
        _fileSearchQueryText.TextChanged += (_, _) => RefreshFileSearchPanel();
        _fileSearchResultsList = new ListBox
        {
            Dock = DockStyle.Fill,
            Height = 220
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

    private static TextBox BuildTextInput() =>
        new() { Dock = DockStyle.Fill };

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
        if (_activeProfile == null)
        {
            _voiceStateLabel.Text = "No account selected.";
            _voiceSamplesLabel.Text = "0 / 0 samples";
            _voiceLastTrainedLabel.Text = "Never activated.";
            _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription}";
            _voiceRecordingStateLabel.Text = "No sample recording in progress.";
            _voicePlaybackStateLabel.Text = "No sample available for playback.";
            _voiceProgress.Value = 0;
            _voiceProgress.Maximum = 1;
            _voicePromptText.Text = "Create an account first.";
            _recordSampleButton.Enabled = false;
            _playSampleButton.Enabled = false;
            _trainVoiceButton.Enabled = false;
            _resetVoiceButton.Enabled = false;
            UpdateRecordButtonAppearance();
            return;
        }

        var settings = _activeProfile.Settings;
        settings.VoiceSamplesRequired = Math.Max(1, settings.VoiceSamplesRequired);
        settings.VoiceSamplesRecorded = Math.Max(0, settings.VoiceSamplesRecorded);

        _voiceStateLabel.Text = settings.VoiceEnrollmentStatus;
        _voiceSamplesLabel.Text = $"{settings.VoiceSamplesRecorded} / {settings.VoiceSamplesRequired} samples";
        _voiceLastTrainedLabel.Text = settings.VoiceEnrolledUtc.HasValue
            ? settings.VoiceEnrolledUtc.Value.ToLocalTime().ToString("f", CultureInfo.CurrentCulture)
            : "Never activated.";
        _voiceRecognitionModeLabel.Text = $"Recognition mode: {_voiceCommandService.CurrentModeDescription}";
        _voiceProgress.Maximum = settings.VoiceSamplesRequired;
        _voiceProgress.Value = Math.Min(settings.VoiceSamplesRecorded, _voiceProgress.Maximum);
        _voicePromptText.Text = GetVoicePrompt(_activeProfile, settings);
        var latestSamplePath = GetLatestVoiceSamplePath(_activeProfile);
        var hasSample = File.Exists(latestSamplePath);

        _voiceRecordingStateLabel.Text = _voiceSampleCapture.IsRecording
            ? "Recording now. Keep holding the red button until you finish speaking."
            : hasSample
                ? "Latest sample is ready for playback."
                : "No sample recording in progress.";
        _voicePlaybackStateLabel.Text = hasSample
            ? $"Latest sample: {Path.GetFileName(latestSamplePath)}"
            : "No sample available for playback.";

        _recordSampleButton.Enabled = true;
        _playSampleButton.Enabled = hasSample && !_voiceSampleCapture.IsRecording;
        _trainVoiceButton.Enabled = true;
        _resetVoiceButton.Enabled = true;
        UpdateRecordButtonAppearance();
    }

    private void RefreshSessionPanel()
    {
        _session.Tick();

        _sessionStateLabel.Text = _session.State.ToString();
        _sessionIdentityLabel.Text = _session.VerifiedCallsign == null
            ? "Waiting for identity."
            : $"Verified: {_session.VerifiedCallsign}";
        _sessionCommandLabel.Text = _session.PendingCommand == null
            ? "No command captured."
            : _session.PendingCommand;

        var lockoutRemaining = _session.GetLockoutRemaining();
        _sessionCountdownLabel.Text = lockoutRemaining.HasValue
            ? $"Lockout remaining: {Math.Ceiling(lockoutRemaining.Value.TotalSeconds):0} seconds"
            : "No timeout active.";

        _sessionResultLabel.Text = _session.State is AlphaSessionState.Idle or AlphaSessionState.Completed
            && !string.IsNullOrWhiteSpace(_activeProfile?.Settings.LastLaunchedApp)
            ? $"Last launched through Start menu: {_activeProfile.Settings.LastLaunchedApp}"
            : _session.StatusMessage;

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
    }

    private void RefreshDictationPanel()
    {
        if (_dictationStatusLabel == null || _startDictationButton == null)
            return;

        if (!_dictationActive)
        {
            _dictationStatusLabel.Text = "Dictation is stopped.";
        }
        else if (!_voiceCommandService.IsListening)
        {
            _dictationStatusLabel.Text = "Dictation is active but the microphone listener is stopped.";
        }
        else if (_dictationTextBox.TextLength > 0)
        {
            _dictationStatusLabel.Text = $"Dictation is active. Captured {_dictationTextBox.Text.Length} characters.";
        }
        else if (_dictationLastTranscriptUtc.HasValue)
        {
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}.";
        }
        else if (_dictationStartedUtc.HasValue && DateTime.UtcNow - _dictationStartedUtc.Value > TimeSpan.FromSeconds(6))
        {
            _dictationStatusLabel.Text = "No speech detected yet. Check microphone permission or speak closer to the mic.";
        }
        else
        {
            _dictationStatusLabel.Text = $"Dictation is active with {_voiceCommandService.CurrentModeDescription}.";
        }

        _startDictationButton.Enabled = !_dictationActive;
        _stopDictationButton.Enabled = _dictationActive;
        _copyDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _pasteDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
        _clearDictationButton.Enabled = !string.IsNullOrWhiteSpace(_dictationTextBox.Text);
    }

    private void RefreshBrowserPanel()
    {
        if (_browserStatusLabel == null)
            return;

        var value = _browserInputText?.Text?.Trim();
        _browserStatusLabel.Text = string.IsNullOrWhiteSpace(value)
            ? "Browser target not opened yet."
            : $"Ready to open: {value}";
        _openBrowserButton.Enabled = !string.IsNullOrWhiteSpace(value);
        _searchBrowserButton.Enabled = !string.IsNullOrWhiteSpace(value);
        _copyBrowserTargetButton.Enabled = !string.IsNullOrWhiteSpace(value);
    }

    private void RefreshFileSearchPanel()
    {
        if (_fileSearchStatusLabel == null)
            return;

        var query = _fileSearchQueryText?.Text?.Trim();
        _fileSearchStatusLabel.Text = string.IsNullOrWhiteSpace(query)
            ? "No file search run yet."
            : $"Ready to search for: {query}";
        _searchFilesButton.Enabled = !string.IsNullOrWhiteSpace(query);
        _openFileResultButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
        _openFileFolderButton.Enabled = _fileSearchResultsList?.SelectedItem is FileSearchResult;
    }

    private void OnSessionTick()
    {
        if (_updatingUi)
            return;

        RefreshSessionPanel();
        RefreshDictationPanel();
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

        ActivateVoiceForProfile(profile, startingListener: true);
        StartVoiceListening();
    }

    private void ActivateVoiceForProfile(UserProfile profile, bool startingListener)
    {
        var settings = profile.Settings;
        settings.VoiceSamplesRequired = Math.Max(1, settings.VoiceSamplesRequired);
        if (settings.VoiceSamplesRecorded < settings.VoiceSamplesRequired)
            settings.VoiceSamplesRecorded = settings.VoiceSamplesRequired;

        settings.VoiceEnrollmentStatus = "Activated";
        settings.VoiceEnrolledUtc = DateTime.UtcNow;
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus(startingListener
            ? $"Voice activated for '{profile.Callsign}'. Starting listener."
            : $"Voice activated for '{profile.Callsign}'.");
    }

    private void ResetVoiceIdentity()
    {
        StopVoiceSampleRecording(commit: false);
        if (!EnsureActiveProfile(out var profile))
            return;

        profile.Settings.VoiceSamplesRecorded = 0;
        profile.Settings.VoiceEnrollmentStatus = "Not activated";
        profile.Settings.VoiceEnrolledUtc = null;
        StopVoiceListening();
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus("Voice activation reset.");
    }

    private void WakeSession()
    {
        _session.DetectWakeWord();
        RefreshSessionPanel();
        UpdateStatus(_session.StatusMessage);
    }

    private void StartVoiceListening()
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

        if (!IsVoiceEnrolled(profile.Settings))
        {
            ActivateVoiceForProfile(profile, startingListener: true);
        }

        _spokenCallsignText.Text = string.Empty;
        _spokenCommandText.Text = string.Empty;
        _appNameText.Text = string.Empty;
        _session.Reset();
        RefreshSessionPanel();
        _voiceCommandService.Start(profile.Settings.LanguageCode, profile.Settings.WakeWord, profile.Callsign);
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
        if (_activeProfile == null || _voiceCommandService.IsListening)
            return;

        if (!IsVoiceEnrolled(_activeProfile.Settings))
            return;

        StartVoiceListening();
    }

    private void StopVoiceListening()
    {
        _dictationActive = false;
        RefreshDictationPanel();

        if (!_voiceCommandService.IsListening)
        {
            UpdateListeningPanel();
            UpdateStatus("Voice listener is already stopped.");
            return;
        }

        _voiceCommandService.Stop();
        UpdateListeningPanel();
        UpdateStatus("Voice listener stopped.");
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

        var samplePath = GetLatestVoiceSamplePath(profile);
        try
        {
            _voiceSampleCapture.Start(samplePath);
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
        var samplePath = profile == null ? null : GetLatestVoiceSamplePath(profile);

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

            profile.Settings.VoiceSamplesRequired = Math.Max(1, profile.Settings.VoiceSamplesRequired);
            profile.Settings.VoiceSamplesRecorded = Math.Min(profile.Settings.VoiceSamplesRequired, profile.Settings.VoiceSamplesRecorded + 1);
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

        var samplePath = GetLatestVoiceSamplePath(profile);
        if (!File.Exists(samplePath))
        {
            UpdateStatus("Record a sample before playing it back.");
            return;
        }

        try
        {
            using var player = new SoundPlayer(samplePath);
            player.Play();
            UpdateStatus("Playing back the latest voice sample.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unable to play voice sample: {ex.Message}");
        }
    }

    private void RehearseVoicePhrase()
    {
        if ((_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
            && !string.IsNullOrWhiteSpace(_callsignText.Text))
        {
            SaveProfile();
        }

        if (!EnsureActiveProfile(out var profile))
            return;

        if (!IsVoiceEnrolled(profile.Settings))
            ActivateVoiceForProfile(profile, startingListener: false);

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

        HandleVoiceTranscript($"[rehearsal] {phrase}", phrase, 1.0f);
    }

    private void VoiceTranscriptReceived(object? sender, VoiceTranscriptEventArgs e)
    {
        if (IsDisposed)
            return;

        RunOnUiThread(() => HandleVoiceTranscript(e.Text, e.Text, e.Confidence));
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
        _lastHeardLabel.Text = $"{displayTranscript} ({confidence:P0} confidence)";

        if (_dictationActive)
        {
            if (IsStopDictationCommand(transcript))
            {
                StopDictation();
                return;
            }

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
            WakeSession();
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

    private void AppendDictationTranscript(string transcript)
    {
        var normalized = transcript.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        _dictationLastTranscriptUtc = DateTime.UtcNow;
        if (_dictationTextBox.TextLength > 0 && !_dictationTextBox.Text.EndsWith(" "))
            _dictationTextBox.AppendText(" ");

        _dictationTextBox.AppendText(normalized);
        RefreshDictationPanel();
        UpdateStatus("Dictation updated.");
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
        _session.Reset();
        RefreshSessionPanel();

        _voiceCommandService.Start(
            _activeProfile?.Settings.LanguageCode ?? "en-US",
            "Dictation",
            string.Empty);

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

        if (_voiceCommandService.IsListening)
            _voiceCommandService.Stop();

        RefreshDictationPanel();
        UpdateListeningPanel();
        UpdateStatus("Dictation stopped.");
    }

    private void ClearDictationText()
    {
        _dictationTextBox.Clear();
        RefreshDictationPanel();
        UpdateStatus("Dictation text cleared.");
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
            UpdateStatus(message);
            return;
        }

        _browserStatusLabel.Text = "Browser target failed.";
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
            UpdateStatus(reason);
            return;
        }

        Clipboard.SetText(targetUri!.ToString());
        UpdateStatus("Resolved browser target copied to the clipboard.");
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

        RefreshFileSearchPanel();

        var message = report.Results.Count == 0
            ? $"No files matched '{query}'."
            : $"Found {report.Results.Count} file result(s) for '{query}'.";

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
                FileName = folderPath,
                UseShellExecute = true
            });
            UpdateStatus($"Opened folder '{folderPath}'.");
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
        profile.Settings.VoiceSamplesRequired = Math.Max(1, profile.Settings.VoiceSamplesRequired);
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
        var nextSample = Math.Min(settings.VoiceSamplesRecorded + 1, settings.VoiceSamplesRequired);
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

    private void UpdateListeningPanel()
    {
        _listeningStateLabel.Text = _voiceCommandService.IsListening
            ? _dictationActive
                ? $"Microphone listener is running for dictation. {_voiceCommandService.CurrentModeDescription}"
                : $"Microphone listener is running. {_voiceCommandService.CurrentModeDescription}"
            : "Microphone listener is stopped.";
        _startListeningButton.Enabled = !_voiceCommandService.IsListening;
        _stopListeningButton.Enabled = _voiceCommandService.IsListening;
    }

    private void UpdateRecordButtonAppearance()
    {
        if (_recordSampleButton == null)
            return;

        if (_voiceSampleCapture.IsRecording)
        {
            _recordSampleButton.Text = "■ Recording - release to stop";
            _recordSampleButton.BackColor = Color.Maroon;
        }
        else
        {
            _recordSampleButton.Text = "● Hold to Record";
            _recordSampleButton.BackColor = Color.Firebrick;
        }
    }

    private string GetLatestVoiceSamplePath(UserProfile profile)
    {
        var folder = Path.Combine(_profileStore.ResolveCallsSignFolder(profile.Callsign), "voice-samples");
        return Path.Combine(folder, "latest.wav");
    }

    private static bool ContainsSpeechPhrase(string transcript, string phrase)
    {
        var normalizedTranscript = $" {NormalizeSpeechText(transcript)} ";
        var normalizedPhrase = NormalizeSpeechText(phrase);
        return !string.IsNullOrWhiteSpace(normalizedPhrase)
            && normalizedTranscript.Contains($" {normalizedPhrase} ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsWakeWord(string transcript, string wakeWord) =>
        ContainsSpeechPhrase(transcript, wakeWord)
        || ContainsSpeechPhrase(transcript, "call sign");

    private static string ExtractCommandFromTranscript(string transcript, string wakeWord, string callsign)
    {
        var command = NormalizeSpeechText(transcript);
        command = RemoveSpeechPhrase(command, wakeWord);
        command = RemoveSpeechPhrase(command, "call sign");
        command = RemoveSpeechPhrase(command, callsign);
        return command.Trim();
    }

    private static string NormalizeLaunchCommand(string command)
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

    private static bool IsCancelCommand(string transcript)
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

    private static bool IsStopListeningCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "stop listening" or "callsign stop listening" or "call sign stop listening";
    }

    private static bool IsStopDictationCommand(string transcript)
    {
        var normalized = NormalizeSpeechText(transcript);
        return normalized is "stop dictation"
            or "callsign stop dictation"
            or "call sign stop dictation"
            or "end dictation"
            or "finish dictation";
    }

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

    private static string NormalizeSpeechText(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _voiceSampleCapture.Dispose();
        _voiceCommandService.Dispose();
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
