using System.Drawing.Drawing2D;
using System.Security.Cryptography;

namespace Callsign.UI;

public sealed class StartupWalkthroughForm : Form
{
    private readonly Panel _surface;
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private readonly Label _contractLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _statusFlowBadge;
    private readonly Label _statusTierBadge;
    private readonly Label _statusSafetyBadge;
    private readonly Label _statusStopBadge;
    private readonly Label _statusNextBadge;
    private readonly Label _statusProgressBadge;
    private readonly Label _statusBrowserBadge;
    private readonly Label _statusBoundaryBadge;
    private readonly Label _statusDeviceBadge;
    private readonly Label _statusReleaseBadge;
    private readonly Label _statusStepBadge;
    private readonly Label _statusCurrentBadge;
    private readonly Label _subtitleLabel;
    private readonly Label _checklistLabel;
    private readonly Label _safetyLabel;
    private readonly Label _voiceCueLabel;
    private readonly ListBox _stepsList;
    private readonly Label _statusLabel;
    private readonly Label _releaseProofSummaryLabel;
    private readonly Button _accountButton;
    private readonly Button _voiceButton;
    private readonly Button _wakeRepairButton;
    private readonly Button _voiceIdentityButton;
    private readonly Button _sessionButton;
    private readonly Button _shortcutsButton;
    private readonly Button _voiceHelpButton;
    private readonly Button _voiceGuideButton;
    private readonly Button _browserOpenButton;
    private readonly Button _browserFindButton;
    private readonly Button _browserShowNumbersButton;
    private readonly Button _browserShowGridButton;
    private readonly Button _browserHideOverlaysButton;
    private readonly Button _visibleControlsButton;
    private readonly Button _showNumbersButton;
    private readonly Button _showGridButton;
    private readonly Button _showKeyboardButton;
    private readonly Button _systemButton;
    private readonly Button _filesButton;
    private readonly Button _updatesButton;
    private readonly Button _readUpdateSummaryButton;
    private readonly Button _releaseProofButton;
    private readonly Button _openInstallerButton;
    private readonly Button _readCheckInStatusButton;
    private readonly Button _packsButton;
    private readonly Button _importPackButton;
    private readonly Button _importPackFolderButton;
    private readonly Button _dropPackFolderButton;
    private readonly Button _readImportSummaryButton;
    private readonly Button _openPacksFolderButton;
    private readonly Action? _openReleaseEvidence;
    private readonly Action? _openManualEvidenceTemplate;
    private readonly Action? _openInstaller;
    private readonly Action? _importPack;
    private readonly Action? _importPackFolder;
    private readonly Action? _repeatUpdateSummary;
    private readonly Action? _repeatImportSummary;
    private readonly Action? _openPacksFolder;
    private readonly Func<string?>? _releaseProofSummaryProvider;
    private readonly Button _plansButton;
    private readonly Button _readUpdatesStatusButton;
    private readonly Button _readRestartProofButton;
    private readonly Button _readReleaseProofButton;
    private readonly Button _readPlansStatusButton;
    private readonly Button _openEvidenceButton;
    private readonly Button _openManualEvidenceTemplateButton;
    private readonly Button _openManualEvidenceChecklistButton;
    private readonly Button _continueButton;
    private readonly Button _remindLaterButton;
    private readonly Action<string> _navigateToTab;
    private readonly Action<string?>? _openVoiceHelp;
    private bool _disposed;
    private string _currentStep = "Account";

    public StartupWalkthroughForm(
        Action<string> navigateToTab,
        Action<string?>? openVoiceHelp = null,
        Action? openReleaseEvidence = null,
        Action? openManualEvidenceTemplate = null,
        Action? openInstaller = null,
        Action? importPack = null,
        Action? importPackFolder = null,
        Action? repeatUpdateSummary = null,
        Action? repeatImportSummary = null,
        Action? openPacksFolder = null,
        Func<string?>? releaseProofSummaryProvider = null)
    {
        _navigateToTab = navigateToTab ?? throw new ArgumentNullException(nameof(navigateToTab));
        _openVoiceHelp = openVoiceHelp;
        _openReleaseEvidence = openReleaseEvidence;
        _openManualEvidenceTemplate = openManualEvidenceTemplate;
        _openInstaller = openInstaller;
        _importPack = importPack;
        _importPackFolder = importPackFolder;
        _repeatUpdateSummary = repeatUpdateSummary;
        _repeatImportSummary = repeatImportSummary;
        _openPacksFolder = openPacksFolder;
        _releaseProofSummaryProvider = releaseProofSummaryProvider;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(244, 247, 252);
        ForeColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.985;
        Size = new Size(720, 520);
        MinimumSize = new Size(620, 440);
        Padding = new Padding(16);
        DoubleBuffered = true;
        Text = "Callsign first-run walkthrough";
        AccessibleName = "Callsign first-run walkthrough";
        AccessibleDescription = "Accessible clean-install walkthrough for account setup, voice enrollment, wake verification, visible launch, local voice shortcuts, plans, updates, and extension packs with macOS Voice Control-style visual clarity.";

        _surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(18),
            AccessibleName = "Callsign startup walkthrough",
            AccessibleDescription = "Guides a clean install through account setup, voice enrollment, wake verification, visible app launch, local voice shortcuts, plans, updates, and extension packs with macOS Voice Control-style visual clarity."
        };
        Controls.Add(_surface);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 32,
            Text = "Start with Callsign",
            AccessibleName = "Startup walkthrough title",
            AccessibleDescription = "Introduces the Callsign clean-install walkthrough.",
            Font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold),
            ForeColor = Color.FromArgb(14, 20, 32)
        };

        _closeButton = new Button
        {
            Text = "\u00D7",
            Width = 36,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            BackColor = Color.FromArgb(236, 239, 246),
            ForeColor = Color.FromArgb(44, 54, 74),
            AccessibleName = "Close startup walkthrough",
            AccessibleDescription = "Dismisses the startup walkthrough without marking setup complete."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var headerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.Controls.Add(_titleLabel, 0, 0);
        headerRow.Controls.Add(_closeButton, 1, 0);

        _contractLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 30,
            Text = "Contract: create profile -> enroll voice -> verify wake -> launch visibly.",
            AccessibleName = "Startup walkthrough contract",
            AccessibleDescription = "Summarizes the visible clean-install flow from profile creation through voice enrollment, wake verification, and visible app launch.",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            BackColor = Color.FromArgb(233, 241, 255),
            Padding = new Padding(12, 0, 12, 0)
        };

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 4, 0, 0),
            Padding = Padding.Empty,
            AccessibleName = "Startup walkthrough status strip",
            AccessibleDescription = "Shows the current first-run flow, stop state, browser helper discovery, tier boundary, safety reminder, update privacy id, and next setup step in compact visible badges."
        };
        _statusFlowBadge = CreateStatusBadge("Flow: clean install", "Shows the current walkthrough flow.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _statusTierBadge = CreateStatusBadge("Tier: Free alpha", "Shows the open-source free-core boundary.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _statusStopBadge = CreateStatusBadge("STOP", "Shows that stop, cancel, stop listening, and reset session remain visible from the first-run flow.", Color.FromArgb(255, 238, 235), Color.FromArgb(153, 27, 27));
        _statusSafetyBadge = CreateStatusBadge("Safety: stop visible", "Shows the visible stop and cancel boundary.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusNextBadge = CreateStatusBadge("Next: create account", "Shows the next setup step in the walkthrough.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _statusProgressBadge = CreateStatusBadge("Progress: Account -> Voice", "Shows the current walkthrough surface and next visible action.", Color.FromArgb(232, 244, 255), Color.FromArgb(30, 64, 175));
        _statusBrowserBadge = CreateStatusBadge("Browser: helpers visible", "Shows the browser overlay helper routes that stay discoverable from the walkthrough.", Color.FromArgb(240, 249, 255), Color.FromArgb(2, 132, 199));
        _statusBoundaryBadge = CreateStatusBadge("Boundary: Free open", "Shows that the Free core stays open-source while paid commands remain gated by entitlement and policy.", Color.FromArgb(255, 247, 237), Color.FromArgb(154, 52, 18));
        _statusDeviceBadge = CreateStatusBadge("Privacy id: hashed", "Shows that the local update identity is hashed before Callsign phones home.", Color.FromArgb(254, 243, 242), Color.FromArgb(153, 27, 27));
        _statusReleaseBadge = CreateStatusBadge("Release: compare installer + site", "Shows the release-proof reminder to compare the local Callsign-Setup.exe installer with the public /downloads/Callsign-Setup.exe download.", Color.FromArgb(255, 250, 240), Color.FromArgb(146, 64, 14));
        _statusStepBadge = CreateStatusBadge("Step: 1 / 17", "Shows the current setup-step position in the walkthrough.", Color.FromArgb(245, 243, 255), Color.FromArgb(88, 28, 135));
        _statusCurrentBadge = CreateStatusBadge("Current: Account", "Shows the current visible setup surface in the walkthrough.", Color.FromArgb(236, 242, 252), Color.FromArgb(30, 41, 59));
        _statusStrip.Controls.Add(_statusFlowBadge);
        _statusStrip.Controls.Add(_statusTierBadge);
        _statusStrip.Controls.Add(_statusStopBadge);
        _statusStrip.Controls.Add(_statusSafetyBadge);
        _statusStrip.Controls.Add(_statusNextBadge);
        _statusStrip.Controls.Add(_statusProgressBadge);
        _statusStrip.Controls.Add(_statusBrowserBadge);
        _statusStrip.Controls.Add(_statusBoundaryBadge);
        _statusStrip.Controls.Add(_statusDeviceBadge);
        _statusStrip.Controls.Add(_statusReleaseBadge);
        _statusStrip.Controls.Add(_statusStepBadge);
        _statusStrip.Controls.Add(_statusCurrentBadge);

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 48,
            Text = "A clean install begins with a short visible flow: create a profile, enroll voice, wake with Callsign, verify identity, review Session, then launch an installed app while the update privacy id stays visible in Updates.",
            AccessibleName = "Startup walkthrough summary",
            AccessibleDescription = "Summarizes the wake, identity verification, update privacy id, session review, and visible action workflow.",
            Font = new Font("Segoe UI", 9.6f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105)
        };

        _checklistLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 38,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(12, 7, 12, 7),
            Text = "Checklist: Account -> Voice -> Session -> Shortcuts -> System -> Files -> Plans -> Updates -> Packs.",
            AccessibleName = "Startup walkthrough checklist",
            AccessibleDescription = "Summarizes the visible setup surfaces in the recommended order for a clean install.",
            Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            BackColor = Color.FromArgb(236, 242, 252)
        };

        _safetyLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 78,
            Margin = new Padding(0, 2, 0, 6),
            Padding = new Padding(12, 8, 12, 8),
            Text = "Free alpha core: wake, identity, visible action, and Voice Access parity stay free. Say stop, cancel, stop listening, or reset session to halt. Community, Pro, and Advanced packs are reviewed, disabled until trusted, signed when distributed, and policy-gated before commands can run.",
            AccessibleName = "Startup walkthrough safety and tier summary",
            AccessibleDescription = "Explains the Free parity core, visible stop and cancel path, and community, Pro, and Advanced extension-pack gates before setup continues.",
            Font = new Font("Segoe UI", 8.7f, FontStyle.Regular),
            ForeColor = Color.FromArgb(42, 57, 81),
            BackColor = Color.FromArgb(239, 246, 255)
        };

        _voiceCueLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 58,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(12, 8, 12, 8),
            Text = BuildVoiceCueText("Account", "Create or pick a callsign profile."),
            AccessibleName = "Startup walkthrough voice cue",
            AccessibleDescription = "Shows the spoken cues that mirror the current walkthrough step.",
            Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
            ForeColor = Color.FromArgb(75, 90, 112),
            BackColor = Color.FromArgb(250, 251, 254)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 24,
            Text = "Use the buttons below to jump to setup surfaces, including Shortcuts, System, Files, Plans, Updates, and Packs.",
            AccessibleName = "Startup walkthrough status",
            AccessibleDescription = "Shows which setup surface was opened from the walkthrough.",
            Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175)
        };

        _releaseProofSummaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 54,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(12, 8, 12, 8),
            Text = GetReleaseProofSummaryText(),
            AccessibleName = "Startup walkthrough release proof summary",
            AccessibleDescription = "Shows the installer hash and size comparison needed before release.",
            Font = new Font("Segoe UI", 8.7f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        _stepsList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AccessibleName = "Startup walkthrough steps",
            AccessibleDescription = "Lists the clean-install steps for profile setup, voice enrollment, wake verification, session review, visible launch, local voice shortcuts, file search, plans, updates, and extension packs."
        };
        _stepsList.Items.Add("1. Create or pick a callsign profile.");
        _stepsList.Items.Add("2. Record at least three voice samples.");
        _stepsList.Items.Add("3. Open Wake Repair if the wake word feels weak or inconsistent.");
        _stepsList.Items.Add("4. Open Voice Identity to review the enrollment samples.");
        _stepsList.Items.Add("5. Say Callsign, then your callsign.");
        _stepsList.Items.Add("6. Confirm the visible wake overlay and live readout.");
        _stepsList.Items.Add("7. Open Session to review the wake, identity, and launch-ready state.");
        _stepsList.Items.Add("8. Launch an installed app through Start search.");
        _stepsList.Items.Add("9. Open Shortcuts to save local voice shortcut phrases.");
        _stepsList.Items.Add("10. Open System to review windowing, keyboard, mouse, media, and settings commands.");
        _stepsList.Items.Add("11. Open Files to review visible Explorer-backed file search.");
        _stepsList.Items.Add("12. Open Plans to review the Free core boundary and paid tiers, then read the current plans status.");
        _stepsList.Items.Add("13. Open Updates to see the phone-home cadence, update privacy id, live installer checks, feature-only review splashes, Open Installer, Open Release Proof, Read Summary Again, read updates status, read check-in status, read release proof, read restart proof, and Open Release Evidence.");
        _stepsList.Items.Add("14. Open Voice Help to review command discovery and visible help, including browser open, browser find, browser show numbers, browser show grid, and browser hide overlays.");
        _stepsList.Items.Add("15. Open Voice Access Guide to reopen this walkthrough from the start.");
        _stepsList.Items.Add("16. Open Packs to import or review extension packs with Import Pack, Import Folder, Read Import Again, Read Watch Status, Open Packs Folder, dragged DLLs or folders, and watched-folder live discovery.");
        _stepsList.Items.Add("17. Verify the local Callsign-Setup.exe installer, the release evidence folder, the public /downloads/Callsign-Setup.exe download, and the manual evidence checklist before release. Then open the manual evidence template and complete the accessibility visual audit before release.");

        var navigationRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = Padding.Empty,
            Margin = new Padding(0, 8, 0, 0),
            AutoSize = true
        };

        _accountButton = CreateNavButton("Open Account", () => NavigateToTabStep("Account", 1, "Create a profile and verify the callsign."));
        _voiceButton = CreateNavButton("Open Voice", () => NavigateToTabStep("Voice", 2, "Train voice and wake calibration."));
        _wakeRepairButton = CreateNavButton("Open Wake Repair", () => NavigateToTabStep("Voice", 2, "Repair wakeword or review calibration."));
        _wakeRepairButton.AccessibleDescription = "Jumps to the Voice tab so you can repair the wake word and review wake calibration.";
        _voiceIdentityButton = CreateNavButton("Open Voice Identity", () => NavigateToTabStep("Voice", 2, "Train or review the voice identity samples."));
        _voiceIdentityButton.AccessibleDescription = "Jumps to the Voice tab so you can train or review the voice identity samples.";
        _sessionButton = CreateNavButton("Open Session", () => NavigateToTabStep("Session", 3, "Review wake, identity, and visible launch."));
        _shortcutsButton = CreateNavButton("Open Shortcuts", () => NavigateToTabStep("Shortcuts", 4, "Save local voice shortcuts."));
        _voiceHelpButton = CreateNavButton("Open Voice Help", () =>
        {
            SetWalkthroughStep("Voice Help", 4, "Review visible command discovery.");
            if (_openVoiceHelp is not null)
            {
                _openVoiceHelp(null);
                return;
            }

            _navigateToTab("Account");
        });
        _voiceHelpButton.AccessibleDescription = "Opens the visible voice help and command discovery surface.";
        _voiceGuideButton = CreateNavButton("Open Voice Access Guide", () => NavigateToTabStep("Account", 1, "Open the voice access guide from the walkthrough."));
        _voiceGuideButton.AccessibleDescription = "Reopens the visible clean-install walkthrough from the start. Voice phrases: open voice access guide, show voice access guide.";
        _browserOpenButton = CreateNavButton("Open Browser", () => OpenVoiceHelpStep("browser open", 4, "Review browser open and search commands."));
        _browserOpenButton.AccessibleDescription = "Opens the visible help surface filtered to browser open and search commands.";
        _browserFindButton = CreateNavButton("Open Browser Find", () => OpenVoiceHelpStep("browser find", 4, "Review browser find commands."));
        _browserFindButton.AccessibleDescription = "Opens the visible help surface filtered to browser find commands.";
        _browserShowNumbersButton = CreateNavButton("Open Browser Show Numbers", () => OpenVoiceHelpStep("browser show numbers", 4, "Review browser visible-control commands."));
        _browserShowNumbersButton.AccessibleDescription = "Opens the visible help surface filtered to browser show numbers commands.";
        _browserShowGridButton = CreateNavButton("Open Browser Show Grid", () => OpenVoiceHelpStep("browser show grid", 4, "Review browser mouse-grid commands."));
        _browserShowGridButton.AccessibleDescription = "Opens the visible help surface filtered to browser show grid commands.";
        _browserHideOverlaysButton = CreateNavButton("Open Browser Hide Overlays", () => OpenVoiceHelpStep("browser hide overlays", 4, "Review browser overlay-dismiss commands."));
        _browserHideOverlaysButton.AccessibleDescription = "Opens the visible help surface filtered to browser hide overlays commands.";
        _visibleControlsButton = CreateNavButton("Open Visible Controls", () => OpenVoiceHelpStep("category:Visible controls", 4, "Review visible control overlays."));
        _visibleControlsButton.AccessibleDescription = "Opens the visible help surface filtered to numbered controls and visible action overlays.";
        _showNumbersButton = CreateNavButton("Open Show Numbers", () => OpenVoiceHelpStep("show numbers", 4, "Review numbered visible controls."));
        _showNumbersButton.AccessibleDescription = "Opens the visible help surface filtered to numbered controls.";
        _showGridButton = CreateNavButton("Open Show Grid", () => OpenVoiceHelpStep("show grid", 4, "Review mouse grid targeting."));
        _showGridButton.AccessibleDescription = "Opens the visible help surface filtered to mouse-grid targeting.";
        _showKeyboardButton = CreateNavButton("Open Show Keyboard", () => OpenVoiceHelpStep("show keyboard", 4, "Review the keyboard overlay."));
        _showKeyboardButton.AccessibleDescription = "Opens the visible help surface filtered to the keyboard overlay.";
        _systemButton = CreateNavButton("Open System", () => NavigateToTabStep("System", 10, "Review windowing, keyboard, mouse, media, and settings commands."));
        _systemButton.AccessibleDescription = "Jumps to the System tab so you can review desktop, keyboard, mouse, media, and settings commands.";
        _filesButton = CreateNavButton("Open Files", () => NavigateToTabStep("Files", 11, "Review visible Explorer-backed file search."));
        _filesButton.AccessibleDescription = "Jumps to the Files tab so you can review visible Explorer-backed file search.";
        _plansButton = CreateNavButton("Open Plans", () => NavigateToTabStep("Plans", 12, "Review the Free core boundary and paid tiers."));
        _updatesButton = CreateNavButton("Open Updates", () => NavigateToTabStep("Updates", 13, "See the phone-home cadence, update privacy id, and live installer checks."));
        _readUpdateSummaryButton = CreateNavButton("Read Summary Again", () =>
        {
            SetWalkthroughStep("Updates", 13, "Replay the latest update summary.");
            if (_repeatUpdateSummary is not null)
            {
                _repeatUpdateSummary();
                return;
            }

            NavigateToTabStep("Updates", 13, "Replay the latest update summary.");
        });
        _readUpdateSummaryButton.AccessibleDescription = "Replays the latest update summary or opens the Updates tab when no summary is available.";
        _readUpdatesStatusButton = CreateNavButton("Read Updates Status", () => NavigateToTabStep("Updates", 13, "Read the visible update status, release proof, and restart proof."));
        _readUpdatesStatusButton.AccessibleDescription = "Jumps to the Updates tab so you can read the visible update status, release proof, and restart proof.";
        _readCheckInStatusButton = CreateNavButton("Read Check-In Status", () => NavigateToTabStep("Updates", 13, "Read the visible check-in status and update privacy id."));
        _readCheckInStatusButton.AccessibleDescription = "Jumps to the Updates tab so you can read the visible check-in status and the hashed update privacy id.";
        _readRestartProofButton = CreateNavButton("Read Restart Proof", () => NavigateToTabStep("Updates", 13, "Read the visible restart proof."));
        _readRestartProofButton.AccessibleDescription = "Jumps to the Updates tab so you can read the visible restart proof and installer-download state.";
        _readReleaseProofButton = CreateNavButton("Read Release Proof", () => NavigateToTabStep("Updates", 13, "Read the visible release proof."));
        _readReleaseProofButton.AccessibleDescription = "Jumps to the Updates tab so you can read the visible installer hash comparison and public download target.";
        _readPlansStatusButton = CreateNavButton("Read Plans Status", () => NavigateToTabStep("Plans", 12, "Read the visible Plans boundary and entitlement state."));
        _readPlansStatusButton.AccessibleDescription = "Jumps to the Plans tab so you can read the visible boundary and entitlement state.";
        _openEvidenceButton = CreateNavButton("Open Release Evidence", () => OpenReleaseEvidenceFolder(), updateStatus: false);
        _openEvidenceButton.AccessibleDescription = "Opens the local release evidence folder while keeping the release-proof reminder visible, or falls back to the Updates tab when the folder action is unavailable.";
        _openManualEvidenceTemplateButton = CreateNavButton("Open Manual Evidence", () => OpenManualEvidenceTemplate(), updateStatus: false);
        _openManualEvidenceTemplateButton.AccessibleDescription = "Opens the manual evidence template while keeping the release-proof reminder visible, or falls back to the Updates tab when the template is unavailable.";
        _openManualEvidenceChecklistButton = CreateNavButton("Open Checklist", () => OpenManualEvidenceChecklist(), updateStatus: false);
        _openManualEvidenceChecklistButton.AccessibleDescription = "Opens the manual evidence checklist while keeping the release-proof reminder visible, or falls back to the Updates tab when the checklist is unavailable. Voice phrases: open manual evidence checklist, open checklist.";
        _openInstallerButton = CreateNavButton("Open Installer", () =>
        {
            SetWalkthroughStep("Updates", 13, "Open the current installer download.");
            if (_openInstaller is not null)
            {
                _openInstaller();
                return;
            }

            NavigateToTabStep("Updates", 13, "Open the current installer download.");
        });
        _openInstallerButton.AccessibleDescription = "Opens the current installer download from the Updates flow when available, or falls back to the Updates tab.";
        _releaseProofButton = CreateNavButton("Open Release Proof", () => OpenReleaseProofStep());
        _releaseProofButton.AccessibleDescription = "Jumps to the release-proof reminder so you can compare the local Callsign-Setup.exe installer and the public /downloads/Callsign-Setup.exe download before release.";
        _packsButton = CreateNavButton("Open Packs", () => NavigateToTabStep("Packs", 15, "Import, review, or live-discover extension packs."));
        _importPackButton = CreateNavButton("Import Pack", () => NavigateToTabStep("Packs", 15, "Import a community command pack."));
        _importPackButton.Click += (_, _) => _importPack?.Invoke();
        _importPackButton.AccessibleDescription = "Jumps to the Packs tab so you can import a community command pack.";
        _importPackFolderButton = CreateNavButton("Import Folder", () => NavigateToTabStep("Packs", 15, "Import a folder of command pack DLLs."));
        _importPackFolderButton.Click += (_, _) => _importPackFolder?.Invoke();
        _importPackFolderButton.AccessibleDescription = "Jumps to the Packs tab so you can import a folder of command pack DLLs.";
        _dropPackFolderButton = CreateNavButton("Drop DLL Folder", () => NavigateToTabStep("Packs", 15, "Drop a folder of command pack DLLs into the Packs surface."));
        _dropPackFolderButton.Click += (_, _) => _importPackFolder?.Invoke();
        _dropPackFolderButton.AccessibleDescription = "Jumps to the Packs tab so you can drag and drop a folder of command pack DLLs.";
        _readImportSummaryButton = CreateNavButton("Read Import Again", () =>
        {
            SetWalkthroughStep("Packs", 15, "Replay the most recent pack import summary.");
            if (_repeatImportSummary is not null)
            {
                _repeatImportSummary();
                return;
            }

            NavigateToTabStep("Packs", 15, "Replay the most recent pack import summary.");
        });
        _readImportSummaryButton.AccessibleDescription = "Replays the most recent pack import summary or opens the Packs tab when no import summary is available.";
        _openPacksFolderButton = CreateNavButton("Open Packs Folder", () => NavigateToTabStep("Packs", 15, "Open the local packs folder."));
        _openPacksFolderButton.Click += (_, _) => _openPacksFolder?.Invoke();
        _openPacksFolderButton.AccessibleDescription = "Jumps to the Packs tab so you can open the local packs folder.";
        navigationRow.Controls.Add(_accountButton);
        navigationRow.Controls.Add(_voiceButton);
        navigationRow.Controls.Add(_wakeRepairButton);
        navigationRow.Controls.Add(_voiceIdentityButton);
        navigationRow.Controls.Add(_sessionButton);
        navigationRow.Controls.Add(_shortcutsButton);
        navigationRow.Controls.Add(_voiceHelpButton);
        navigationRow.Controls.Add(_voiceGuideButton);
        navigationRow.Controls.Add(_browserOpenButton);
        navigationRow.Controls.Add(_browserFindButton);
        navigationRow.Controls.Add(_browserShowNumbersButton);
        navigationRow.Controls.Add(_browserShowGridButton);
        navigationRow.Controls.Add(_browserHideOverlaysButton);
        navigationRow.Controls.Add(_visibleControlsButton);
        navigationRow.Controls.Add(_showNumbersButton);
        navigationRow.Controls.Add(_showGridButton);
        navigationRow.Controls.Add(_showKeyboardButton);
        navigationRow.Controls.Add(_systemButton);
        navigationRow.Controls.Add(_filesButton);
        navigationRow.Controls.Add(_plansButton);
        navigationRow.Controls.Add(_updatesButton);
        navigationRow.Controls.Add(_readUpdateSummaryButton);
        navigationRow.Controls.Add(_readUpdatesStatusButton);
        navigationRow.Controls.Add(_readCheckInStatusButton);
        navigationRow.Controls.Add(_readRestartProofButton);
        navigationRow.Controls.Add(_readReleaseProofButton);
        navigationRow.Controls.Add(_readPlansStatusButton);
        navigationRow.Controls.Add(_openEvidenceButton);
        navigationRow.Controls.Add(_openManualEvidenceTemplateButton);
        navigationRow.Controls.Add(_openManualEvidenceChecklistButton);
        navigationRow.Controls.Add(_openInstallerButton);
        navigationRow.Controls.Add(_releaseProofButton);
        navigationRow.Controls.Add(_packsButton);
        navigationRow.Controls.Add(_importPackButton);
        navigationRow.Controls.Add(_importPackFolderButton);
        navigationRow.Controls.Add(_dropPackFolderButton);
        navigationRow.Controls.Add(_readImportSummaryButton);
        navigationRow.Controls.Add(_openPacksFolderButton);

        var footerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            Padding = Padding.Empty
        };
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _remindLaterButton = new Button
        {
            Text = "Remind me later",
            AutoSize = true,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(236, 239, 246),
            ForeColor = Color.FromArgb(44, 54, 74),
            Margin = new Padding(0, 0, 8, 0),
            AccessibleName = "Remind me later",
            AccessibleDescription = "Dismisses the startup walkthrough without marking setup complete."
        };
        _remindLaterButton.FlatAppearance.BorderSize = 0;
        _remindLaterButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _continueButton = new Button
        {
            Text = "Continue to Callsign",
            AutoSize = true,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            AccessibleName = "Continue to Callsign",
            AccessibleDescription = "Closes the startup walkthrough and marks it as seen."
        };
        _continueButton.FlatAppearance.BorderSize = 0;
        _continueButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        AcceptButton = _continueButton;
        CancelButton = _remindLaterButton;

        footerRow.Controls.Add(_remindLaterButton, 0, 0);
        footerRow.Controls.Add(_continueButton, 1, 0);

        layout.Controls.Add(headerRow, 0, 0);
        layout.Controls.Add(_contractLabel, 0, 1);
        layout.Controls.Add(_statusStrip, 0, 2);
        layout.Controls.Add(_subtitleLabel, 0, 3);
        layout.Controls.Add(_checklistLabel, 0, 4);
        layout.Controls.Add(_safetyLabel, 0, 5);
        layout.Controls.Add(_voiceCueLabel, 0, 6);
        layout.Controls.Add(_statusLabel, 0, 7);
        layout.Controls.Add(_releaseProofSummaryLabel, 0, 8);
        layout.Controls.Add(_stepsList, 0, 9);
        layout.Controls.Add(navigationRow, 0, 10);
        layout.Controls.Add(footerRow, 0, 11);
        _surface.Controls.Add(layout);

        SetWalkthroughStep(_currentStep, 1, "Create or pick a callsign profile.");
        ApplyRoundedRegion();
    }

    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("clean-install walkthrough");
    public string FormAccessibleName => AccessibleName ?? string.Empty;
    public string FormAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string SurfaceAccessibleName => _surface.AccessibleName ?? string.Empty;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusFlowBadgeText => _statusFlowBadge.Text;
    public string StatusTierBadgeText => _statusTierBadge.Text;
    public string StatusStopBadgeText => _statusStopBadge.Text;
    public string StatusSafetyBadgeText => _statusSafetyBadge.Text;
    public string StatusNextBadgeText => _statusNextBadge.Text;
    public string StatusProgressBadgeText => _statusProgressBadge.Text;
    public string StatusBrowserBadgeText => _statusBrowserBadge.Text;
    public string StatusBoundaryBadgeText => _statusBoundaryBadge.Text;
    public string StatusDeviceBadgeText => _statusDeviceBadge.Text;
    public string StatusReleaseBadgeText => _statusReleaseBadge.Text;
    public string StatusReleaseBadgeAccessibleDescription => _statusReleaseBadge.AccessibleDescription ?? string.Empty;
    public string StatusStepBadgeText => _statusStepBadge.Text;
    public string StatusCurrentBadgeText => _statusCurrentBadge.Text;
    public string CurrentStepText => _currentStep;
    public string SelectedStepText => _stepsList.SelectedItem?.ToString() ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string SummaryAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string ChecklistAccessibleName => _checklistLabel.AccessibleName ?? string.Empty;
    public string ChecklistAccessibleDescription => _checklistLabel.AccessibleDescription ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string VoiceCueAccessibleName => _voiceCueLabel.AccessibleName ?? string.Empty;
    public string VoiceCueAccessibleDescription => _voiceCueLabel.AccessibleDescription ?? string.Empty;
    public string VoiceCueText => _voiceCueLabel.Text;
    public string StatusAccessibleName => _statusLabel.AccessibleName ?? string.Empty;
    public string StatusLabelText => _statusLabel.Text;
    public string ReleaseProofSummaryText => _releaseProofSummaryLabel.Text;
    public string StepsAccessibleName => _stepsList.AccessibleName ?? string.Empty;
    public string StepsAccessibleDescription => _stepsList.AccessibleDescription ?? string.Empty;
    public string ContinueAccessibleName => _continueButton.AccessibleName ?? string.Empty;
    public string RemindLaterAccessibleName => _remindLaterButton.AccessibleName ?? string.Empty;
    public string AccountButtonAccessibleName => _accountButton.AccessibleName ?? string.Empty;
    public string VoiceButtonAccessibleName => _voiceButton.AccessibleName ?? string.Empty;
    public string WakeRepairButtonAccessibleName => _wakeRepairButton.AccessibleName ?? string.Empty;
    public string VoiceIdentityButtonAccessibleName => _voiceIdentityButton.AccessibleName ?? string.Empty;
    public string SessionButtonAccessibleName => _sessionButton.AccessibleName ?? string.Empty;
    public string ShortcutsButtonAccessibleName => _shortcutsButton.AccessibleName ?? string.Empty;
    public string VoiceHelpButtonAccessibleName => _voiceHelpButton.AccessibleName ?? string.Empty;
    public string VoiceGuideButtonAccessibleName => _voiceGuideButton.AccessibleName ?? string.Empty;
    public string BrowserOpenButtonAccessibleName => _browserOpenButton.AccessibleName ?? string.Empty;
    public string BrowserFindButtonAccessibleName => _browserFindButton.AccessibleName ?? string.Empty;
    public string BrowserShowNumbersButtonAccessibleName => _browserShowNumbersButton.AccessibleName ?? string.Empty;
    public string BrowserShowGridButtonAccessibleName => _browserShowGridButton.AccessibleName ?? string.Empty;
    public string BrowserHideOverlaysButtonAccessibleName => _browserHideOverlaysButton.AccessibleName ?? string.Empty;
    public string VisibleControlsButtonAccessibleName => _visibleControlsButton.AccessibleName ?? string.Empty;
    public string ShowNumbersButtonAccessibleName => _showNumbersButton.AccessibleName ?? string.Empty;
    public string ShowGridButtonAccessibleName => _showGridButton.AccessibleName ?? string.Empty;
    public string ShowKeyboardButtonAccessibleName => _showKeyboardButton.AccessibleName ?? string.Empty;
    public string SystemButtonAccessibleName => _systemButton.AccessibleName ?? string.Empty;
    public string FilesButtonAccessibleName => _filesButton.AccessibleName ?? string.Empty;
    public string PlansButtonAccessibleName => _plansButton.AccessibleName ?? string.Empty;
    public string UpdatesButtonAccessibleName => _updatesButton.AccessibleName ?? string.Empty;
    public string ReadUpdatesStatusButtonAccessibleName => _readUpdatesStatusButton.AccessibleName ?? string.Empty;
    public string ReadCheckInStatusButtonAccessibleName => _readCheckInStatusButton.AccessibleName ?? string.Empty;
    public string ReadRestartProofButtonAccessibleName => _readRestartProofButton.AccessibleName ?? string.Empty;
    public string ReadReleaseProofButtonAccessibleName => _readReleaseProofButton.AccessibleName ?? string.Empty;
    public string ReadPlansStatusButtonAccessibleName => _readPlansStatusButton.AccessibleName ?? string.Empty;
    public string OpenEvidenceButtonAccessibleName => _openEvidenceButton.AccessibleName ?? string.Empty;
    public string ReleaseProofButtonAccessibleName => _releaseProofButton.AccessibleName ?? string.Empty;
    public string PacksButtonAccessibleName => _packsButton.AccessibleName ?? string.Empty;
    public string ImportPackButtonAccessibleName => _importPackButton.AccessibleName ?? string.Empty;
    public string ImportPackFolderButtonAccessibleName => _importPackFolderButton.AccessibleName ?? string.Empty;
    public string DropPackFolderButtonAccessibleName => _dropPackFolderButton.AccessibleName ?? string.Empty;
    public string ReadImportSummaryButtonAccessibleName => _readImportSummaryButton.AccessibleName ?? string.Empty;
    public string OpenPacksFolderButtonAccessibleName => _openPacksFolderButton.AccessibleName ?? string.Empty;
    public string TitleText => _titleLabel.Text;
    public string SummaryText => _subtitleLabel.Text;
    public string ChecklistText => _checklistLabel.Text;
    public string SafetyText => _safetyLabel.Text;
    public string StatusText => _statusLabel.Text;
    public string StepsText => string.Join(Environment.NewLine, _stepsList.Items.Cast<object>().Select(item => item?.ToString() ?? string.Empty));
    public string AccountButtonText => _accountButton.Text;
    public string VoiceButtonText => _voiceButton.Text;
    public string WakeRepairButtonText => _wakeRepairButton.Text;
    public string VoiceIdentityButtonText => _voiceIdentityButton.Text;
    public string SessionButtonText => _sessionButton.Text;
    public string ShortcutsButtonText => _shortcutsButton.Text;
    public string VoiceHelpButtonText => _voiceHelpButton.Text;
    public string VoiceGuideButtonText => _voiceGuideButton.Text;
    public string BrowserOpenButtonText => _browserOpenButton.Text;
    public string BrowserFindButtonText => _browserFindButton.Text;
    public string BrowserShowNumbersButtonText => _browserShowNumbersButton.Text;
    public string BrowserShowGridButtonText => _browserShowGridButton.Text;
    public string BrowserHideOverlaysButtonText => _browserHideOverlaysButton.Text;
    public string VisibleControlsButtonText => _visibleControlsButton.Text;
    public string ShowNumbersButtonText => _showNumbersButton.Text;
    public string ShowGridButtonText => _showGridButton.Text;
    public string ShowKeyboardButtonText => _showKeyboardButton.Text;
    public string SystemButtonText => _systemButton.Text;
    public string FilesButtonText => _filesButton.Text;
    public string PlansButtonText => _plansButton.Text;
    public string UpdatesButtonText => _updatesButton.Text;
    public string ReadUpdateSummaryButtonText => _readUpdateSummaryButton.Text;
    public string ReadUpdatesStatusButtonText => _readUpdatesStatusButton.Text;
    public string ReadCheckInStatusButtonText => _readCheckInStatusButton.Text;
    public string ReadRestartProofButtonText => _readRestartProofButton.Text;
    public string ReadReleaseProofButtonText => _readReleaseProofButton.Text;
    public string ReadPlansStatusButtonText => _readPlansStatusButton.Text;
    public string OpenEvidenceButtonText => _openEvidenceButton.Text;
    public string OpenManualEvidenceTemplateButtonText => _openManualEvidenceTemplateButton.Text;
    public string OpenManualEvidenceTemplateButtonAccessibleName => _openManualEvidenceTemplateButton.AccessibleName ?? string.Empty;
    public string OpenManualEvidenceChecklistButtonText => _openManualEvidenceChecklistButton.Text;
    public string OpenManualEvidenceChecklistButtonAccessibleName => _openManualEvidenceChecklistButton.AccessibleName ?? string.Empty;
    public string OpenManualEvidenceChecklistButtonAccessibleDescription => _openManualEvidenceChecklistButton.AccessibleDescription ?? string.Empty;
    public string OpenInstallerButtonText => _openInstallerButton.Text;
    public string ReleaseProofButtonText => _releaseProofButton.Text;
    public string PacksButtonText => _packsButton.Text;
    public string ImportPackButtonText => _importPackButton.Text;
    public string ImportPackFolderButtonText => _importPackFolderButton.Text;
    public string DropPackFolderButtonText => _dropPackFolderButton.Text;
    public string ReadImportSummaryButtonText => _readImportSummaryButton.Text;
    public string OpenPacksFolderButtonText => _openPacksFolderButton.Text;
    public string ContinueButtonText => _continueButton.Text;
    public string RemindLaterButtonText => _remindLaterButton.Text;

    private void NavigateToTabStep(string tabName, int stepNumber, string nextAction)
    {
        SetWalkthroughStep(tabName, stepNumber, nextAction);
        _navigateToTab(tabName);
    }

    private void OpenVoiceHelpStep(string? filter, int stepNumber, string nextAction)
    {
        SetWalkthroughStep("Voice Help", stepNumber, nextAction);
        _openVoiceHelp?.Invoke(filter);
        if (_openVoiceHelp is null)
            _navigateToTab("Help");
    }

    public void OpenReleaseProofStep()
    {
        SetWalkthroughStep("Release Proof", 17, "Compare the local Callsign-Setup.exe installer, the release evidence folder, and the public /downloads/Callsign-Setup.exe download before release.");
        _navigateToTab("Updates");
    }

    public void OpenReleaseEvidenceFolder()
    {
        SetWalkthroughStep("Release Proof", 17, "Open the release evidence folder from the release-proof step.");
        if (_openReleaseEvidence is not null)
        {
            _openReleaseEvidence();
            return;
        }

        _navigateToTab("Updates");
    }

    public void OpenManualEvidenceTemplate()
    {
        SetWalkthroughStep("Release Proof", 17, "Open the manual evidence template from the release-proof step.");
        if (_openManualEvidenceTemplate is not null)
        {
            _openManualEvidenceTemplate();
            return;
        }

        _navigateToTab("Updates");
    }

    public void OpenManualEvidenceChecklist()
    {
        SetWalkthroughStep("Release Proof", 17, "Open the manual evidence checklist from the release-proof step.");
        if (_openManualEvidenceChecklistButton is not null)
        {
            var checklistPath = MainForm.TryGetManualEvidenceChecklistPath();
            if (!string.IsNullOrWhiteSpace(checklistPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = checklistPath,
                        UseShellExecute = true
                    });
                    return;
                }
                catch
                {
                }
            }
        }

        _navigateToTab("Updates");
    }

    private void SetWalkthroughStep(string surfaceName, int stepNumber, string nextAction)
    {
        _currentStep = surfaceName;
        var isReleaseProofStep = string.Equals(surfaceName, "Release Proof", StringComparison.OrdinalIgnoreCase);
        var releaseProofState = isReleaseProofStep ? GetReleaseProofState() : default;
        var releaseProofDetail = isReleaseProofStep
            ? $" {releaseProofState.Description}"
            : string.Empty;
        _statusLabel.Text = isReleaseProofStep
            ? $"Step {stepNumber} of {_stepsList.Items.Count}: {surfaceName}. {releaseProofState.Description}"
            : $"Step {stepNumber} of {_stepsList.Items.Count}: {surfaceName}. Next: {nextAction}";
        _voiceCueLabel.Text = BuildVoiceCueText(surfaceName, nextAction);
        _releaseProofSummaryLabel.Text = isReleaseProofStep
            ? $"Release proof summary: {GetReleaseProofSummaryText(releaseProofState.Description)}"
            : GetReleaseProofSummaryText();
        _statusFlowBadge.Text = $"Flow: clean install -> {surfaceName}";
        _statusNextBadge.Text = $"Next: {nextAction}{releaseProofDetail}";
        _statusProgressBadge.Text = $"Progress: {surfaceName} -> {nextAction}{releaseProofDetail}";
        _statusStepBadge.Text = $"Step: {stepNumber} / {_stepsList.Items.Count}";
        _statusCurrentBadge.Text = $"Current: {surfaceName}";
        ApplyReleaseProofBadgeState(isReleaseProofStep, releaseProofState);
        var selectedIndex = Math.Clamp(stepNumber - 1, 0, Math.Max(0, _stepsList.Items.Count - 1));
        if (_stepsList.Items.Count > 0)
            _stepsList.SelectedIndex = selectedIndex;
    }

    private void ApplyReleaseProofBadgeState(bool isReleaseProofStep, (string Text, string Description) releaseProofState)
    {
        _statusDeviceBadge.Text = "Privacy id: hashed";
        _statusDeviceBadge.AccessibleName = "Update privacy id reminder";
        _statusDeviceBadge.AccessibleDescription = "Shows the reminder to open Updates and read the privacy-preserving identity used for phone-home requests.";
        _statusReleaseBadge.Text = "Release: compare installer + site";
        _statusReleaseBadge.AccessibleName = "Release compare installer plus site";
        _statusReleaseBadge.AccessibleDescription = isReleaseProofStep
            ? releaseProofState.Description
            : "Shows the release-proof reminder to compare the local Callsign-Setup.exe installer with the public /downloads/Callsign-Setup.exe download.";
    }

    private string GetReleaseProofSummaryText(string? fallback = null)
    {
        if (_releaseProofSummaryProvider is not null)
        {
            var providedText = _releaseProofSummaryProvider();
            if (!string.IsNullOrWhiteSpace(providedText))
                return providedText.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();

        return "Release proof summary: open the Updates tab to compare the local Callsign-Setup.exe installer, the last downloaded installer path, the release evidence folder, the manual evidence checklist with its accessibility visual audit, and the public /downloads/Callsign-Setup.exe download.";
    }

    private static string BuildVoiceCueText(string surfaceName, string nextAction)
    {
        const string updateReplayHint = " You can also say read summary again, read updates status, read check-in status, read release proof, open installer, open release evidence, open manual evidence, open checklist, or open voice access guide. Feature-only review splashes still appear for capability-only updates.";
        const string packReplayHint = " You can also say read import summary again, read watch status, read import again, open packs folder, import pack, import folder, or drop DLLs here.";
        return surfaceName switch
        {
            "Account" => "Voice cue: Create or pick a callsign profile. You can also say compare installer + site, open release proof, complete the accessibility visual audit, or read the Updates surface proof: server, privacy id, and installer proof visible. You can also say read summary again, read import summary again, read updates status, read check-in status, read release proof, open installer, open release evidence, open manual evidence, or open checklist. Next: " + nextAction + updateReplayHint + packReplayHint,
            "Release Proof" => "Voice cue: compare installer + site, open release proof, read release proof, open release evidence, open manual evidence, open checklist, complete the accessibility visual audit, or open updates. Next: " + nextAction + updateReplayHint + " Reviewing the proof does not change installed commands.",
            "Updates" => "Voice cue: read summary again, read updates status, read check-in status, read release proof, open installer, open release evidence, open manual evidence, or open checklist. Next: " + nextAction + " The Updates surface keeps the update server, privacy id, and installer proof visible, feature-only updates still show a review splash, and reviewing update details does not change installed commands.",
            "Packs" => "Voice cue: open packs, import pack, import folder, drop DLLs or folders, or open packs folder. Next: " + nextAction + packReplayHint + " Imported packs stay disabled until reviewed and enabled.",
            "Voice Help" => "Voice cue: open voice help, open voice access guide, browser open, browser find, browser show numbers, browser show grid, browser hide overlays, show numbers, show grid, or show keyboard. Next: " + nextAction + updateReplayHint,
            _ => "Voice cue: " + nextAction + updateReplayHint + " You can also say open voice access guide."
        };
    }

    private static (string Text, string Description) GetReleaseProofState()
    {
        var installerPath = TryGetLocalInstallerPath();
        if (installerPath is null)
        {
            return (
                "Release: compare installer + site",
                "No local Callsign-Setup.exe installer was found. Open the release evidence folder and manual evidence checklist, complete the accessibility visual audit, then compare the local installer with the public /downloads/Callsign-Setup.exe download before release.");
        }

        var installerInfo = new FileInfo(installerPath);
        using var stream = File.OpenRead(installerPath);
        var hash = Convert.ToHexString(SHA256.HashData(stream));
        var shortHash = hash.Length >= 8 ? hash[..8] : hash;
        return (
            $"Release: {shortHash}",
            $"Local installer ready: {installerInfo.Length:n0} bytes, SHA-256 {hash}, path {installerPath}. Open the release evidence folder and manual evidence checklist, complete the accessibility visual audit, then compare it with the public /downloads/Callsign-Setup.exe download before release.");
    }

    private static string? TryGetLocalInstallerPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "Callsign-Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "Callsign-Setup.exe")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyRoundedRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyRoundedRegion();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _remindLaterButton.Dispose();
            _continueButton.Dispose();
            _closeButton.Dispose();
            _accountButton.Dispose();
            _voiceButton.Dispose();
            _sessionButton.Dispose();
            _shortcutsButton.Dispose();
            _voiceHelpButton.Dispose();
            _voiceGuideButton.Dispose();
            _browserOpenButton.Dispose();
            _browserFindButton.Dispose();
            _visibleControlsButton.Dispose();
            _showNumbersButton.Dispose();
            _showGridButton.Dispose();
            _showKeyboardButton.Dispose();
            _systemButton.Dispose();
            _filesButton.Dispose();
            _plansButton.Dispose();
        _updatesButton.Dispose();
        _readUpdatesStatusButton.Dispose();
        _readRestartProofButton.Dispose();
        _readReleaseProofButton.Dispose();
        _openEvidenceButton.Dispose();
        _releaseProofButton.Dispose();
            _packsButton.Dispose();
            _stepsList.Dispose();
            _contractLabel.Dispose();
            _statusStrip.Dispose();
            _statusFlowBadge.Dispose();
            _statusTierBadge.Dispose();
            _statusStopBadge.Dispose();
            _statusSafetyBadge.Dispose();
            _statusNextBadge.Dispose();
            _statusProgressBadge.Dispose();
            _statusBrowserBadge.Dispose();
            _statusBoundaryBadge.Dispose();
            _statusDeviceBadge.Dispose();
            _statusReleaseBadge.Dispose();
            _statusStepBadge.Dispose();
            _statusCurrentBadge.Dispose();
            _releaseProofSummaryLabel.Dispose();
            _checklistLabel.Dispose();
            _statusLabel.Dispose();
            _safetyLabel.Dispose();
            _subtitleLabel.Dispose();
            _titleLabel.Dispose();
            _surface.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private Button CreateNavButton(string text, Action action, bool updateStatus = true)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 8, 0),
            AccessibleName = text,
            AccessibleDescription = $"Opens the {text.Replace("Open ", string.Empty, StringComparison.OrdinalIgnoreCase)} setup surface."
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 230);
        button.FlatAppearance.BorderSize = 1;
        button.Click += (_, _) =>
        {
            action();
            if (updateStatus)
                _statusLabel.Text = $"Opened {text.Replace("Open ", string.Empty, StringComparison.OrdinalIgnoreCase)}.";
        };
        return button;
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        Region = new Region(CreateRoundedPath(ClientRectangle, 20));
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Label CreateStatusBadge(string text, string description, Color backColor, Color foreColor)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 4),
            Padding = new Padding(10, 4, 10, 4),
            BackColor = backColor,
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            Text = text,
            AccessibleName = text,
            AccessibleDescription = description
        };
    }
}
