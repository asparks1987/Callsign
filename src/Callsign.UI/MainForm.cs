using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Callsign.UI.Models;
using Callsign.UI.Services;

namespace Callsign.UI;

public sealed class MainForm : Form
{
    private readonly ProfileStore _profileStore = new();
    private readonly AlphaSessionStateMachine _session = new();
    private readonly StartMenuLauncher _launcher = new();
    private readonly System.Windows.Forms.Timer _sessionTimer = new() { Interval = 1000 };

    private readonly List<UserProfile> _profiles = [];
    private UserProfile? _activeProfile;
    private bool _updatingUi;

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
    private TextBox _voicePromptText = null!;
    private ProgressBar _voiceProgress = null!;

    private Label _sessionStateLabel = null!;
    private Label _sessionIdentityLabel = null!;
    private Label _sessionCommandLabel = null!;
    private Label _sessionCountdownLabel = null!;
    private Label _sessionResultLabel = null!;
    private TextBox _spokenCallsignText = null!;
    private TextBox _spokenCommandText = null!;
    private TextBox _appNameText = null!;

    private Button _recordSampleButton = null!;
    private Button _trainVoiceButton = null!;
    private Button _resetVoiceButton = null!;
    private Button _wakeButton = null!;
    private Button _verifyButton = null!;
    private Button _captureButton = null!;
    private Button _launchButton = null!;
    private Button _cancelButton = null!;
    private Button _resetSessionButton = null!;
    private Button _newProfileButton = null!;
    private Button _saveProfileButton = null!;
    private Button _deleteProfileButton = null!;

    private Label _statusLabel = null!;

    public MainForm()
    {
        Text = "Callsign Alpha Setup";
        Width = 1080;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        BuildForm();

        _sessionTimer.Tick += (_, _) => OnSessionTick();
        LoadProfiles();
        _sessionTimer.Start();

        UpdateStatus("Create an account, enroll voice, then launch installed apps with wake word + callsign.");
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
        var layout = BuildTwoColumnLayout(11);

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
        _accountStateLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "Voice not enrolled." };

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddRow(layout, "Active account", _profilePicker, row++);
        AddRow(layout, "Callsign", _callsignText, row++);
        AddRow(layout, "Display name", _displayNameText, row++);
        AddRow(layout, "Email", _emailText, row++);
        AddRow(layout, "Department", _departmentText, row++);
        AddRow(layout, "Notes", _notesText, row++);

        layout.Controls.Add(new Label { Text = "Profile folder" }, 0, row);
        layout.Controls.Add(_accountPathLabel, 1, row++);
        layout.Controls.Add(new Label { Text = "Enrollment status" }, 0, row);
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

        var heading = CreateHeading("Train the service to recognize the user's voice");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "Alpha v1 uses voice-only identity. Record a few clear samples, then mark the profile as enrolled."
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
        _voiceStateLabel = new Label { AutoSize = true, Text = "Not enrolled." };
        _voiceSamplesLabel = new Label { AutoSize = true, Text = "0 / 3 samples" };
        _voiceLastTrainedLabel = new Label { AutoSize = true, Text = "Never trained." };

        _recordSampleButton = new Button { Text = "Record Sample", Width = 130 };
        _recordSampleButton.Click += (_, _) => RecordVoiceSample();

        _trainVoiceButton = new Button { Text = "Train Voice", Width = 130 };
        _trainVoiceButton.Click += (_, _) => TrainVoiceIdentity();

        _resetVoiceButton = new Button { Text = "Reset Voice", Width = 130 };
        _resetVoiceButton.Click += (_, _) => ResetVoiceIdentity();

        var row = 0;
        AddFullWidth(layout, heading, row++);
        AddFullWidth(layout, description, row++);
        AddRow(layout, "Sample prompt", _voicePromptText, row++);
        AddRow(layout, "Training progress", _voiceProgress, row++);
        AddRow(layout, "Enrollment status", _voiceStateLabel, row++);
        AddRow(layout, "Samples recorded", _voiceSamplesLabel, row++);
        AddRow(layout, "Last trained", _voiceLastTrainedLabel, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_recordSampleButton);
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
        var layout = BuildTwoColumnLayout(11);

        var heading = CreateHeading("Wake word, verify callsign, then launch an installed app");
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = "The visible alpha flow is: say 'Callsign', say your callsign, speak the command, then launch the app through Start search."
        };

        _spokenCallsignText = BuildTextInput();
        _spokenCommandText = BuildTextInput();
        _appNameText = BuildTextInput();

        _sessionStateLabel = new Label { AutoSize = true, Text = "Idle." };
        _sessionIdentityLabel = new Label { AutoSize = true, Text = "Waiting for wake word." };
        _sessionCommandLabel = new Label { AutoSize = true, Text = "No command captured." };
        _sessionCountdownLabel = new Label { AutoSize = true, Text = "No timer running." };
        _sessionResultLabel = new Label { AutoSize = true, Text = "No launch yet." };

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
        AddRow(layout, "Spoken callsign", _spokenCallsignText, row++);
        AddRow(layout, "Spoken command", _spokenCommandText, row++);
        AddRow(layout, "App to launch", _appNameText, row++);
        AddRow(layout, "State", _sessionStateLabel, row++);
        AddRow(layout, "Identity", _sessionIdentityLabel, row++);
        AddRow(layout, "Command", _sessionCommandLabel, row++);
        AddRow(layout, "Timeout", _sessionCountdownLabel, row++);
        AddRow(layout, "Result", _sessionResultLabel, row++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
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
            _session.Reset();
        }
        finally
        {
            _updatingUi = false;
        }

        RefreshAllPanels();
        UpdateStatus("Create a new account, then save it before voice enrollment.");
    }

    private void SelectProfile(UserProfile profile)
    {
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
            _session.Reset();
        }
        finally
        {
            _updatingUi = false;
        }

        RefreshAllPanels();
        UpdateStatus($"Editing account '{profile.Callsign}'.");
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
    }

    private void RefreshAccountPanel()
    {
        if (_activeProfile == null || string.IsNullOrWhiteSpace(_activeProfile.Callsign))
        {
            _accountPathLabel.Text = "No profile selected.";
            _accountStateLabel.Text = "Voice not enrolled.";
            _newProfileButton.Enabled = true;
            _saveProfileButton.Enabled = true;
            _deleteProfileButton.Enabled = false;
            return;
        }

        _accountPathLabel.Text = _profileStore.ResolveCallsSignFolder(_activeProfile.Callsign);
        _accountStateLabel.Text = GetVoiceStatusText(_activeProfile.Settings);
        _deleteProfileButton.Enabled = true;
    }

    private void RefreshVoicePanel()
    {
        if (_activeProfile == null)
        {
            _voiceStateLabel.Text = "No account selected.";
            _voiceSamplesLabel.Text = "0 / 0 samples";
            _voiceLastTrainedLabel.Text = "Never trained.";
            _voiceProgress.Value = 0;
            _voiceProgress.Maximum = 1;
            _voicePromptText.Text = "Create an account first.";
            _recordSampleButton.Enabled = false;
            _trainVoiceButton.Enabled = false;
            _resetVoiceButton.Enabled = false;
            return;
        }

        var settings = _activeProfile.Settings;
        settings.VoiceSamplesRequired = Math.Max(1, settings.VoiceSamplesRequired);
        settings.VoiceSamplesRecorded = Math.Max(0, settings.VoiceSamplesRecorded);

        _voiceStateLabel.Text = settings.VoiceEnrollmentStatus;
        _voiceSamplesLabel.Text = $"{settings.VoiceSamplesRecorded} / {settings.VoiceSamplesRequired} samples";
        _voiceLastTrainedLabel.Text = settings.VoiceEnrolledUtc.HasValue
            ? settings.VoiceEnrolledUtc.Value.ToLocalTime().ToString("f", CultureInfo.CurrentCulture)
            : "Never trained.";
        _voiceProgress.Maximum = settings.VoiceSamplesRequired;
        _voiceProgress.Value = Math.Min(settings.VoiceSamplesRecorded, _voiceProgress.Maximum);
        _voicePromptText.Text = GetVoicePrompt(settings);
        _recordSampleButton.Enabled = true;
        _trainVoiceButton.Enabled = true;
        _resetVoiceButton.Enabled = true;
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

        _sessionResultLabel.Text = _session.StatusMessage;

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

    private void OnSessionTick()
    {
        if (_updatingUi)
            return;

        RefreshSessionPanel();
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
            UpdateStatus($"Account '{normalizedCallsign}' saved.");
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
        var loadedIndex = _profiles.FindIndex(p => string.Equals(p.Callsign, profile.Callsign, StringComparison.OrdinalIgnoreCase));
        if (loadedIndex >= 0)
        {
            _profilePicker.SelectedIndex = loadedIndex;
            SelectProfile(_profiles[loadedIndex]);
        }
        UpdateStatus($"Account '{profile.Callsign}' created.");
    }

    private void DeleteProfile()
    {
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
        if (!EnsureActiveProfile(out var profile))
            return;

        profile.Settings.VoiceSamplesRequired = Math.Max(1, profile.Settings.VoiceSamplesRequired);
        profile.Settings.VoiceSamplesRecorded = Math.Min(profile.Settings.VoiceSamplesRequired, profile.Settings.VoiceSamplesRecorded + 1);
        profile.Settings.VoiceEnrollmentStatus = profile.Settings.VoiceSamplesRecorded >= profile.Settings.VoiceSamplesRequired
            ? "Ready to train"
            : $"Collecting sample {profile.Settings.VoiceSamplesRecorded} of {profile.Settings.VoiceSamplesRequired}";
        profile.Settings.VoiceEnrolledUtc = null;
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus("Voice sample recorded.");
    }

    private void TrainVoiceIdentity()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        var settings = profile.Settings;
        settings.VoiceSamplesRequired = Math.Max(1, settings.VoiceSamplesRequired);
        if (settings.VoiceSamplesRecorded < settings.VoiceSamplesRequired)
        {
            settings.VoiceEnrollmentStatus = $"Need {settings.VoiceSamplesRequired - settings.VoiceSamplesRecorded} more sample(s).";
            SaveVoiceState(profile);
            _profileStore.Save(profile);
            RefreshAllPanels();
            UpdateStatus(settings.VoiceEnrollmentStatus);
            return;
        }

        settings.VoiceEnrollmentStatus = "Enrolled";
        settings.VoiceEnrolledUtc = DateTime.UtcNow;
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus($"Voice enrolled for '{profile.Callsign}'.");
    }

    private void ResetVoiceIdentity()
    {
        if (!EnsureActiveProfile(out var profile))
            return;

        profile.Settings.VoiceSamplesRecorded = 0;
        profile.Settings.VoiceEnrollmentStatus = "Not enrolled";
        profile.Settings.VoiceEnrolledUtc = null;
        SaveVoiceState(profile);
        _profileStore.Save(profile);
        RefreshAllPanels();
        UpdateStatus("Voice enrollment reset.");
    }

    private void WakeSession()
    {
        _session.DetectWakeWord();
        RefreshSessionPanel();
        UpdateStatus(_session.StatusMessage);
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
            _appNameText.Text = inferredApp;

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
            _session.CompleteLaunch();
            RefreshAllPanels();
            UpdateStatus($"Launched '{target}' from Start menu.");
            return;
        }

        _session.FailLaunch(launchMessage);
        RefreshSessionPanel();
        UpdateStatus(launchMessage);
    }

    private void CancelSession()
    {
        _session.Cancel("Session cancelled.");
        RefreshSessionPanel();
        UpdateStatus("Session cancelled.");
    }

    private void ResetSession()
    {
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
            profile.Settings.VoiceEnrollmentStatus = "Not enrolled";
    }

    private static string GetVoiceStatusText(UserSettings settings)
    {
        if (settings.VoiceEnrolledUtc.HasValue)
            return $"Enrolled on {settings.VoiceEnrolledUtc.Value.ToLocalTime():f}";

        return string.IsNullOrWhiteSpace(settings.VoiceEnrollmentStatus)
            ? "Not enrolled."
            : settings.VoiceEnrollmentStatus;
    }

    private static bool IsVoiceEnrolled(UserSettings settings) =>
        settings.VoiceEnrolledUtc.HasValue && settings.VoiceSamplesRecorded >= settings.VoiceSamplesRequired;

    private static string GetVoicePrompt(UserSettings settings)
    {
        var nextSample = Math.Min(settings.VoiceSamplesRecorded + 1, settings.VoiceSamplesRequired);
        return nextSample switch
        {
            1 => "Sample 1: Say 'Callsign'.",
            2 => "Sample 2: Say 'Callsign, open Notepad'.",
            3 => "Sample 3: Say 'Callsign, launch an app'.",
            _ => "Review your samples and train the profile."
        };
    }

    private static string InferAppName(string command)
    {
        var trimmed = command.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        foreach (var prefix in new[] { "launch ", "open ", "start ", "run " })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefix.Length..].Trim();
        }

        return trimmed;
    }

    private void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
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
