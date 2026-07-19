using Callsign.Extensions;
using Callsign.UI.Services;
using System.Drawing.Drawing2D;

namespace Callsign.UI;

public sealed class CommandPaletteForm : Form
{
    private readonly TextBox _searchBox;
    private readonly ListView _commandsList;
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private readonly Label _subtitleLabel;
    private readonly Label _contractLabel;
    private readonly Label _voiceCueLabel;
    private readonly FlowLayoutPanel _capabilityStrip;
    private readonly FlowLayoutPanel _quickFiltersPanel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _statusStopBadge;
    private readonly Label _statusScopeBadge;
    private readonly Label _statusResultBadge;
    private readonly Label _statusSafetyBadge;
    private readonly Label _statusSelectedBadge;
    private readonly Label _statusPreviewBadge;
    private readonly Label _statusSourceBadge;
    private readonly Label _statusAvailabilityBadge;
    private readonly Label _statusBrowserBadge;
    private readonly Label _statusBoundaryBadge;
    private readonly Label _statusDiscoveryBadge;
    private readonly Label _capabilityFreeBadge;
    private readonly Label _capabilityProBadge;
    private readonly Label _capabilityAdvancedBadge;
    private readonly Label _scopeLabel;
    private readonly Label _resultLabel;
    private readonly Label _safetyLabel;
    private readonly Label _detailsLabel;
    private IReadOnlyList<CommandDiscoveryEntry> _commands = [];
    private readonly Dictionary<string, ListViewGroup> _categoryGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _quickFilterButtons = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public CommandPaletteForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(248, 250, 253);
        ForeColor = Color.FromArgb(18, 22, 30);
        Opacity = 0.975;
        Width = 760;
        Height = 620;
        MinimumSize = new Size(620, 480);
        Padding = new Padding(16);
        DoubleBuffered = true;
        Text = "Callsign command palette";
        AccessibleName = "Callsign command palette";
        AccessibleDescription = "Searchable voice command discovery surface with command tier, availability, risk, approval, examples, and safety commands. Free Voice Access parity commands stay open-source while paid commands remain gated by entitlement and policy.";

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 13,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "What can I say?",
            AccessibleName = "Command palette title",
            AccessibleDescription = "Names the voice command discovery surface.",
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
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
            AccessibleName = "Close command palette",
            AccessibleDescription = "Dismisses the command palette without changing the active session."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();
        CancelButton = _closeButton;

        var headerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.Controls.Add(_titleLabel, 0, 0);
        headerRow.Controls.Add(_closeButton, 1, 0);
        layout.Controls.Add(headerRow, 0, 0);

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Wake, verify identity, then choose a visible command.",
            AccessibleName = "Command palette session instructions",
            AccessibleDescription = "Explains that commands require wake and identity verification before visible action.",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_subtitleLabel, 0, 1);

        _contractLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 36,
            Margin = new Padding(0, 2, 0, 0),
            Padding = new Padding(12, 7, 12, 7),
            BackColor = Color.FromArgb(236, 242, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold),
            Text = "Contract: search commands -> review availability -> choose a visible action.",
            AccessibleName = "Command palette contract",
            AccessibleDescription = "Summarizes the visible command discovery flow from search through availability review and visible action choice."
        };
        layout.Controls.Add(_contractLabel, 0, 2);

        _voiceCueLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 34,
            Margin = new Padding(0, 2, 0, 0),
            Padding = new Padding(12, 7, 12, 7),
            BackColor = Color.FromArgb(239, 246, 255),
            ForeColor = Color.FromArgb(30, 64, 175),
            Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold),
            Text = BuildCommandPaletteVoiceCueText(),
            AccessibleName = "Command palette voice cue",
            AccessibleDescription = "Shows the spoken cues that mirror visible command discovery and safety actions."
        };
        layout.Controls.Add(_voiceCueLabel, 0, 3);

        _capabilityStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 2),
            Padding = Padding.Empty,
            AccessibleName = "Command palette capability strip",
            AccessibleDescription = "Shows the Free, Pro, and Advanced capability split in compact visible badges."
        };
        _capabilityFreeBadge = CreatePaletteBadge("Free: Voice Access parity open core", "Shows that Windows Voice Access parity capabilities stay in the open-source Free core.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _capabilityProBadge = CreatePaletteBadge("Pro: beyond-parity OS + browser + workflow", "Shows the paid Pro capabilities beyond the Free parity baseline.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _capabilityAdvancedBadge = CreatePaletteBadge("Advanced: beyond-parity recipes + diagnostics + admin", "Shows the paid Advanced capabilities beyond the Free parity baseline.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _capabilityStrip.Controls.Add(_capabilityFreeBadge);
        _capabilityStrip.Controls.Add(_capabilityProBadge);
        _capabilityStrip.Controls.Add(_capabilityAdvancedBadge);
        layout.Controls.Add(_capabilityStrip, 0, 4);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11.25f, FontStyle.Regular),
            PlaceholderText = "Search commands, examples, source filters, categories, tiers, or status",
            AccessibleName = "Command search",
            AccessibleDescription = "Searches command phrases, examples, source filters, categories, extension sources, tiers, availability, load status, risks, and approval requirements. Structured filters such as free parity, voice access parity, category:system, settings, media, window, editing, tier:pro, tier:free, paid, plans, source:free, source:open core, source:trusted, status:disabled, status:gated, approval:visible choice, approval:fresh, and approval:none are supported.",
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        _searchBox.TextChanged += (_, _) => RefreshList();
        _searchBox.KeyDown += SearchBoxOnKeyDown;
        layout.Controls.Add(_searchBox, 0, 5);

        _quickFiltersPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 4),
            Padding = Padding.Empty,
            AccessibleName = "Command palette quick filters",
            AccessibleDescription = "Offers one-click command filters for all commands, available commands, Free commands, Free parity commands, open core commands, paid commands, Pro commands, Advanced commands, community commands, trusted commands, app launch commands, navigation commands, plans and tier boundary commands, profile commands, session commands, runtime commands, voice mode commands, update commands, read updates status commands, read check-in status commands, read plans status commands, diagnostics commands, help commands, approval-gated commands, visible-choice commands, risk-gated commands, system commands, browser commands, file commands, keyboard commands, mouse commands, visible control commands, show numbers commands, show grid commands, show keyboard commands, settings commands, media commands, window commands, windowing commands, task view commands, snap layouts commands, minimize window commands, maximize window commands, restore window commands, task manager commands, show desktop commands, close window commands, getting started commands, release proof commands, release evidence commands, manual evidence commands, manual evidence checklist commands, editing commands, disabled commands, gated commands, safety commands, dictation, and extension commands."
        };
        AddQuickFilterButton("All", string.Empty, "Shows every discoverable Callsign command.");
        AddQuickFilterButton("Available", "status:available", "Shows commands that are available to run right now.");
        AddQuickFilterButton("Free", "free", "Shows commands in the Free open-source parity core.");
        AddQuickFilterButton("Free Parity", "free parity", "Shows Windows Voice Access parity commands that stay in the Free open-source core.");
        AddQuickFilterButton("Open Core", "source:free", "Shows built-in open-core commands only.");
        AddQuickFilterButton("Pro", "tier:pro", "Shows only Pro-tier commands and packs.");
        AddQuickFilterButton("Advanced", "tier:advanced", "Shows only Advanced-tier commands and packs.");
        AddQuickFilterButton("Paid", "paid", "Shows the paid command boundary across Pro and Advanced commands.");
        AddQuickFilterButton("Launch", "category:App launch", "Shows visible app-launch commands.");
        AddQuickFilterButton("Navigate", "category:Navigation", "Shows setup and surface-navigation commands.");
        AddQuickFilterButton("Community", "source:community", "Shows commands imported from community extension packs.");
        AddQuickFilterButton("Trusted", "source:trusted", "Shows commands imported from trusted extension packs.");
        AddQuickFilterButton("Plans", "plans", "Shows the visible Plans tab and tier boundary commands.");
        AddQuickFilterButton("Paywall", "paywall", "Shows the Plans tab, the paywall-status readback, and paid boundary commands.");
        AddQuickFilterButton("Read Plans Status", "read plans status", "Shows the visible Plans boundary, paywall status, and entitlement readback command.");
        AddQuickFilterButton("Profile", "category:Profile setup", "Shows profile, enrollment, and setup commands.");
        AddQuickFilterButton("Session", "session", "Shows session safety, runtime, and launch-flow commands.");
        AddQuickFilterButton("Runtime", "category:Runtime", "Shows listener, status, and voice-mode commands.");
        AddQuickFilterButton("Voice Mode", "voice mode", "Shows commands-only, dictation-only, and default voice mode commands.");
        AddQuickFilterButton("Updates", "category:Updates", "Shows update and release-splash commands, including feature-only review splashes.");
        AddQuickFilterButton("Read Updates Status", "read updates status", "Shows the visible update status, release proof, restart proof, and feature-only review splash command.");
        AddQuickFilterButton("Read Voice Mode", "read voice mode status", "Shows the visible voice-mode selection readback command.");
        AddQuickFilterButton("Read Check-In Status", "read check-in status", "Shows the visible last phone-home check-in readback command.");
        AddQuickFilterButton("Diagnostics", "category:Diagnostics", "Shows local diagnostics and folder commands.");
        AddQuickFilterButton("Help", "category:Help", "Shows help and command-discovery commands.");
        AddQuickFilterButton("Packs", "category:Packs", "Shows import, refresh, enable, disable, and remove pack commands.");
        AddQuickFilterButton("System", "category:System", "Shows safe local system commands.");
        AddQuickFilterButton("Browser", "category:Browser tabs", "Shows browser navigation and page control commands.");
        AddQuickFilterButton("Browser Open", "browser open", "Shows browser open, search, and address-bar commands.");
        AddQuickFilterButton("Browser Find", "browser find", "Shows in-page browser find commands and visible search commands.");
        AddQuickFilterButton("Browser Show Numbers", "browser show numbers", "Shows browser-visible control commands and numbered overlays.");
        AddQuickFilterButton("Browser Show Grid", "browser show grid", "Shows browser mouse-grid commands and visible targeting overlays.");
        AddQuickFilterButton("Browser Hide Overlays", "browser hide overlays", "Shows browser overlay-dismiss commands.");
        AddQuickFilterButton("Browser Tabs", "category:Browser tabs", "Shows browser tab and window commands.");
        AddQuickFilterButton("Files", "category:Files tab", "Shows visible file search and Explorer result commands.");
        AddQuickFilterButton("File Search", "file search", "Shows Explorer-backed file search commands.");
        AddQuickFilterButton("Keyboard", "category:Keyboard", "Shows keyboard and modifier commands.");
        AddQuickFilterButton("Mouse", "category:Mouse grid", "Shows mouse, grid, and pointer commands.");
        AddQuickFilterButton("Visible", "category:Visible controls", "Shows visible control, overlay, and readout commands.");
        AddQuickFilterButton("Show Numbers", "show numbers", "Shows the visible numbered control overlay and click, double-click, or right-click commands.");
        AddQuickFilterButton("Show Grid", "show grid", "Shows the mouse grid overlay and visible targeting commands.");
        AddQuickFilterButton("Show Keyboard", "show keyboard", "Shows the keyboard overlay and visible key commands.");
        AddQuickFilterButton("Settings", "settings", "Shows safe Windows Settings page commands.");
        AddQuickFilterButton("Media", "media", "Shows playback and media session commands.");
        AddQuickFilterButton("Window", "window", "Shows window switching and window-management commands.");
        AddQuickFilterButton("Windowing", "windowing", "Shows Task View, snap layouts, and virtual desktop commands.");
        AddQuickFilterButton("Task View", "task view", "Shows Task View and visible window-switching commands.");
        AddQuickFilterButton("Snap Layouts", "snap layouts", "Shows Snap Layouts and window-snapping commands.");
        AddQuickFilterButton("Minimize Window", "minimize window", "Shows minimize-window and show-desktop commands.");
        AddQuickFilterButton("Maximize Window", "maximize window", "Shows maximize-window and restore-window commands.");
        AddQuickFilterButton("Restore Window", "restore window", "Shows restore-window and maximize-window commands.");
        AddQuickFilterButton("Task Manager", "task manager", "Shows Task Manager and close-window commands.");
        AddQuickFilterButton("Show Desktop", "show desktop", "Shows show-desktop and minimize-all-windows commands.");
        AddQuickFilterButton("Close Window", "close window", "Shows close-window and window-closing commands.");
        AddQuickFilterButton("Getting Started", "getting started", "Shows the clean-install walkthrough and setup guidance.");
        AddQuickFilterButton("Open Voice Access Guide", "open voice access guide", "Shows the clean-install walkthrough and Voice Access guide path.");
        AddQuickFilterButton("Release Proof", "open release proof", "Shows the walkthrough's release-proof step and installer verification guidance.");
        AddQuickFilterButton("Read Release Proof", "read release proof", "Shows the release-proof readback for the installer hash comparison and public download target.");
        AddQuickFilterButton("Restart Proof", "read restart proof", "Shows the visible restart-proof line and downloaded installer state.");
        AddQuickFilterButton("Restart Listening", "restart listening", "Shows the visible restart-listening command and recovery flow.");
        AddQuickFilterButton("Open Installer", "open installer", "Shows the current installer download action in the Updates flow.");
        AddQuickFilterButton("Read Import Again", "read import summary again", "Shows the pack-import splash replay command and imported-pack summary.");
        AddQuickFilterButton("Read Watch Status", "read watch status", "Shows the watched-folder status readback command and recent change time.");
        AddQuickFilterButton("Open Release Evidence", "open release evidence", "Shows the local release artifacts folder and generated parity evidence files.");
        AddQuickFilterButton("Open Manual Evidence", "open manual evidence", "Shows the manual evidence template for the public clean-install walkthrough.");
        AddQuickFilterButton("Open Checklist", "open checklist", "Shows the human-readable manual evidence checklist beside the release-proof template.");
        AddQuickFilterButton("Open Manual Evidence Checklist", "open manual evidence checklist", "Shows the human-readable manual evidence checklist beside the release-proof template.");
        AddQuickFilterButton("Import Pack", "import extension pack", "Shows the community pack import flow for visible review before enablement.");
        AddQuickFilterButton("Import DLL", "import dll", "Shows the community DLL import flow for visible review before enablement.");
        AddQuickFilterButton("Drop DLL", "drop dll", "Shows the community DLL drag-and-drop import flow for visible review before enablement.");
        AddQuickFilterButton("Drop DLL Folder", "drop dll folder", "Shows the folder drag-and-drop flow for community extension DLLs.");
        AddQuickFilterButton("Import Folder", "import extension folder", "Shows the folder import flow for community extension DLLs.");
        AddQuickFilterButton("Import DLL Folder", "import dll folder", "Shows the folder import flow for community extension DLLs.");
        AddQuickFilterButton("Open Packs Folder", "open packs folder", "Shows the local Packs folder and its watched command-pack files.");
        AddQuickFilterButton("Update Pack", "update extension pack", "Shows the command for updating an installed extension pack.");
        AddQuickFilterButton("Rollback Pack", "rollback extension pack", "Shows the command for restoring the last backed-up extension pack version.");
        AddQuickFilterButton("Refresh Packs", "refresh packs", "Shows the command for refreshing the visible installed-pack list.");
        AddQuickFilterButton("Enable Pack", "enable selected pack", "Shows the command for enabling the currently selected pack after review.");
        AddQuickFilterButton("Disable Pack", "disable selected pack", "Shows the command for disabling the currently selected pack.");
        AddQuickFilterButton("Remove Pack", "remove selected pack", "Shows the command for removing the currently selected pack.");
        AddQuickFilterButton("Desktop", "desktop", "Shows virtual desktop and show-desktop commands.");
        AddQuickFilterButton("Editing", "category:Editing", "Shows copy, paste, formatting, and text-movement commands.");
        AddQuickFilterButton("Approval", "approval", "Shows commands that require approval or fresh identity.");
        AddQuickFilterButton("Visible Choice", "approval:visible choice", "Shows commands that stop for a visible choice when the spoken target is ambiguous.");
        AddQuickFilterButton("Fresh ID", "approval:fresh", "Shows commands that require a fresh identity check.");
        AddQuickFilterButton("No Approval", "approval:none", "Shows commands that do not require approval.");
        AddQuickFilterButton("Risk", "risk", "Shows commands by risk and consent tier.");
        AddQuickFilterButton("Observe", "risk:observe", "Shows observe-only commands with no state change.");
        AddQuickFilterButton("Local", "risk:local", "Shows local changes and reversible commands.");
        AddQuickFilterButton("External", "risk:external", "Shows commands with external side effects.");
        AddQuickFilterButton("Blocked", "risk:blocked", "Shows blocked or policy-disabled commands.");
        AddQuickFilterButton("Safety", "safety", "Shows visible stop, cancel, and reset commands.");
        AddQuickFilterButton("Disabled", "status:disabled", "Shows disabled commands and imported packs.");
        AddQuickFilterButton("Gated", "status:gated", "Shows entitlement-gated or signature-gated commands.");
        AddQuickFilterButton("Dictation", "dictation", "Shows dictation and visible text-review commands.");
        AddQuickFilterButton("Read Dictation", "read dictation", "Shows dictation readback and stop-reading commands.");
        AddQuickFilterButton("Show Corrections", "show correction alternatives", "Shows the correction alternatives surface and visible review commands.");
        AddQuickFilterButton("Visible Controls", "visible controls", "Shows numbered controls, grid, overlay, and visible action commands.");
        AddQuickFilterButton("Extensions", "extension", "Shows community, Pro, Advanced, disabled, signature-gated, and entitlement-gated extension commands.");
        layout.Controls.Add(_quickFiltersPanel, 0, 6);

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 0),
            Padding = Padding.Empty,
            AccessibleName = "Command palette status strip",
            AccessibleDescription = "Shows the current stop state, command scope, result count, source counts, browser helper discovery, availability counts, safety cue, selected command, discovery source, and visible boundary cues in compact badges."
        };
        _statusStopBadge = CreatePaletteBadge("STOP", "Shows that stop, cancel, and reset remain visible while browsing commands.", Color.FromArgb(255, 238, 235), Color.FromArgb(153, 27, 27));
        _statusScopeBadge = CreatePaletteBadge("Scope: all commands", "Shows the current visible command scope.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _statusResultBadge = CreatePaletteBadge("Results: 0", "Shows how many commands match the current search.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _statusSafetyBadge = CreatePaletteBadge("Safety: cancel · stop listening · reset session", "Shows the current session safety phrases.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusSelectedBadge = CreatePaletteBadge("Selected: none", "Shows the selected command in the palette.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _statusPreviewBadge = CreatePaletteBadge("Preview: choose a command", "Shows a compact preview of the selected command and its best example.", Color.FromArgb(244, 242, 255), Color.FromArgb(91, 33, 182));
        _statusSourceBadge = CreatePaletteBadge("Source: open core 0 · extensions 0", "Shows the current split between the open core and extension commands in view.", Color.FromArgb(240, 249, 255), Color.FromArgb(7, 89, 133));
        _statusAvailabilityBadge = CreatePaletteBadge("Availability: loaded 0 · disabled 0 · gated 0", "Shows the current availability split for the commands in view.", Color.FromArgb(255, 251, 235), Color.FromArgb(180, 83, 9));
        _statusBrowserBadge = CreatePaletteBadge("Browser: helpers visible", "Shows that browser overlay helpers stay discoverable from the palette.", Color.FromArgb(240, 249, 255), Color.FromArgb(2, 132, 199));
        _statusBoundaryBadge = CreatePaletteBadge("Boundary: Free open", "Shows that the Free core stays open-source while paid commands remain gated by entitlement and policy.", Color.FromArgb(255, 247, 237), Color.FromArgb(154, 52, 18));
        _statusDiscoveryBadge = CreatePaletteBadge("Discovery: open core + extensions", "Shows that the palette includes open-core commands and imported extension packs.", Color.FromArgb(240, 249, 255), Color.FromArgb(7, 89, 133));
        _statusStrip.Controls.Add(_statusStopBadge);
        _statusStrip.Controls.Add(_statusScopeBadge);
        _statusStrip.Controls.Add(_statusResultBadge);
        _statusStrip.Controls.Add(_statusSafetyBadge);
        _statusStrip.Controls.Add(_statusSelectedBadge);
        _statusStrip.Controls.Add(_statusPreviewBadge);
        _statusStrip.Controls.Add(_statusSourceBadge);
        _statusStrip.Controls.Add(_statusAvailabilityBadge);
        _statusStrip.Controls.Add(_statusBrowserBadge);
        _statusStrip.Controls.Add(_statusDiscoveryBadge);
        _statusStrip.Controls.Add(_statusBoundaryBadge);
        layout.Controls.Add(_statusStrip, 0, 7);

        _scopeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Scope: all commands",
            AccessibleName = "Command palette scope summary",
            AccessibleDescription = "Shows the current command scope, including visible Available, Free, Pro, Advanced, approval, risk, disabled, and gated command counts.",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
            ForeColor = Color.FromArgb(52, 65, 84),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        layout.Controls.Add(_scopeLabel, 0, 8);

        _resultLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "0 commands",
            AccessibleName = "Command palette result count",
            AccessibleDescription = "Shows how many voice commands match the current search.",
            Font = new Font("Segoe UI Semibold", 9.1f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_resultLabel, 0, 9);

        _safetyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Safety: cancel · stop listening · reset session",
            AccessibleName = "Command palette safety commands",
            AccessibleDescription = "Lists spoken commands that stop or reset the current Callsign session.",
            Font = new Font("Segoe UI", 8.95f, FontStyle.Regular),
            ForeColor = Color.FromArgb(75, 90, 112),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        layout.Controls.Add(_safetyLabel, 0, 10);

        _detailsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Select a command to see details.",
            AccessibleName = "Command palette command details",
            AccessibleDescription = "Shows the selected command phrase, aliases, source, tier, availability, risk, approval requirement, and examples.",
            Font = new Font("Segoe UI", 9.1f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        layout.Controls.Add(_detailsLabel, 0, 11);

        _commandsList = new ListView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            View = View.Details,
            ShowGroups = true,
            BackColor = Color.FromArgb(252, 252, 253),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9.4f, FontStyle.Regular),
            AccessibleName = "Command palette results",
            AccessibleDescription = "Lists visible voice commands with category, phrase, preview, source, tier, availability, risk, approval, and aliases."
        };
        _commandsList.Columns.Add("Category", 120);
        _commandsList.Columns.Add("Say", 180);
        _commandsList.Columns.Add("Preview", 215);
        _commandsList.Columns.Add("Source", 90);
        _commandsList.Columns.Add("Tier", 70);
        _commandsList.Columns.Add("Availability", 175);
        _commandsList.Columns.Add("Risk", 85);
        _commandsList.Columns.Add("Approval", 110);
        _commandsList.Columns.Add("Aliases", 240);
        _commandsList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _commandsList.SelectedIndexChanged += (_, _) => UpdateDetails();
        _commandsList.KeyDown += CommandsListOnKeyDown;
        layout.Controls.Add(_commandsList, 0, 12);

        ApplyPaletteRegion();
    }

    public int VisibleCommandCount => _commandsList.Items.Count;
    public string ContractText => _contractLabel.Text;
    public string VoiceCueText => _voiceCueLabel.Text;
    public int VisibleCategoryCount => _commandsList.Groups.Count;
    public string ResultSummaryText => _resultLabel.Text;
    public string ScopeSummaryText => _scopeLabel.Text;
    public string SafetySummaryText => _safetyLabel.Text;
    public string SearchText => _searchBox.Text;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("command palette");
    public string DetailsText => _detailsLabel.Text;
    public string SurfaceAccessibleName => AccessibleName ?? string.Empty;
    public string SurfaceAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string SubtitleAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string SearchPlaceholderText => _searchBox.PlaceholderText ?? string.Empty;
    public string QuickFiltersAccessibleName => _quickFiltersPanel.AccessibleName ?? string.Empty;
    public string QuickFiltersAccessibleDescription => _quickFiltersPanel.AccessibleDescription ?? string.Empty;
    public string QuickFilterTexts => string.Join(" ", _quickFiltersPanel.Controls.OfType<Button>().Select(button => button.Text));
    public string ActiveQuickFilterText => _quickFiltersPanel.Controls.OfType<Button>()
        .FirstOrDefault(button => button.BackColor.ToArgb() == Color.FromArgb(37, 99, 235).ToArgb())?.Text ?? string.Empty;
    public string VisibleCommandPhrases => string.Join(" ", _commandsList.Items.OfType<ListViewItem>().Select(item => item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty));
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusStopBadgeText => _statusStopBadge.Text;
    public string StatusScopeBadgeText => _statusScopeBadge.Text;
    public string StatusResultBadgeText => _statusResultBadge.Text;
    public string StatusSafetyBadgeText => _statusSafetyBadge.Text;
    public string StatusSelectedBadgeText => _statusSelectedBadge.Text;
    public string StatusPreviewBadgeText => _statusPreviewBadge.Text;
    public string StatusSourceBadgeText => _statusSourceBadge.Text;
    public string StatusAvailabilityBadgeText => _statusAvailabilityBadge.Text;
    public string StatusBrowserBadgeText => _statusBrowserBadge.Text;
    public string StatusDiscoveryBadgeText => _statusDiscoveryBadge.Text;
    public string StatusBoundaryBadgeText => _statusBoundaryBadge.Text;
    public string CapabilityStripAccessibleName => _capabilityStrip.AccessibleName ?? string.Empty;
    public string CapabilityFreeBadgeText => _capabilityFreeBadge.Text;
    public string CapabilityProBadgeText => _capabilityProBadge.Text;
    public string CapabilityAdvancedBadgeText => _capabilityAdvancedBadge.Text;
    public string SearchAccessibleName => _searchBox.AccessibleName ?? string.Empty;
    public string SearchAccessibleDescription => _searchBox.AccessibleDescription ?? string.Empty;
    public string ResultAccessibleName => _resultLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string DetailsAccessibleName => _detailsLabel.AccessibleName ?? string.Empty;
    public string DetailsAccessibleDescription => _detailsLabel.AccessibleDescription ?? string.Empty;
    public string ResultsAccessibleName => _commandsList.AccessibleName ?? string.Empty;
    public string ResultsAccessibleDescription => _commandsList.AccessibleDescription ?? string.Empty;
    public string ResultsPreviewColumnText => _commandsList.Columns.Count > 2 ? _commandsList.Columns[2].Text : string.Empty;
    public string? SelectedCommandPhrase => _commandsList.SelectedItems.Count > 0
        ? _commandsList.SelectedItems[0].SubItems.Count > 1
            ? _commandsList.SelectedItems[0].SubItems[1].Text
            : _commandsList.SelectedItems[0].Text
        : null;
    public string? FirstVisibleTierText => _commandsList.Items.Count > 0 && _commandsList.Items[0].SubItems.Count > 4
        ? _commandsList.Items[0].SubItems[4].Text
        : null;
    public string? FirstVisibleAvailabilityText => _commandsList.Items.Count > 0 && _commandsList.Items[0].SubItems.Count > 5
        ? _commandsList.Items[0].SubItems[5].Text
        : null;
    public string? FirstVisibleApprovalText => _commandsList.Items.Count > 0 && _commandsList.Items[0].SubItems.Count > 7
        ? _commandsList.Items[0].SubItems[7].Text
        : null;

    public void SetSearchText(string text)
    {
        _searchBox.Text = text;
        RefreshList();
    }

    public void ShowPalette(IWin32Window owner, IReadOnlyList<CommandDiscoveryEntry> commands, string? initialFilter = null)
    {
        _commands = commands;
        if (!string.IsNullOrWhiteSpace(initialFilter))
            _searchBox.Text = initialFilter;
        RefreshList();

        if (Visible)
        {
            BringToFront();
            _searchBox.Focus();
            return;
        }

        Show(owner);
        _searchBox.Focus();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyPaletteRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyPaletteRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var shadowPen = new Pen(Color.FromArgb(36, 0, 0, 0), 4f);
        using var borderPen = new Pen(Color.FromArgb(110, 255, 255, 255), 1.2f);
        using var path = CreateRoundedPath(ClientRectangle, 26);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(shadowPen, path);
        e.Graphics.DrawPath(borderPen, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _searchBox.Dispose();
            _commandsList.Dispose();
            _titleLabel.Dispose();
            _closeButton.Dispose();
            _subtitleLabel.Dispose();
            _contractLabel.Dispose();
            _voiceCueLabel.Dispose();
            _capabilityFreeBadge.Dispose();
            _capabilityProBadge.Dispose();
            _capabilityAdvancedBadge.Dispose();
            _capabilityStrip.Dispose();
            _quickFiltersPanel.Dispose();
            _statusStrip.Dispose();
            _statusStopBadge.Dispose();
            _statusScopeBadge.Dispose();
            _statusResultBadge.Dispose();
            _statusSafetyBadge.Dispose();
            _statusSelectedBadge.Dispose();
            _statusPreviewBadge.Dispose();
            _statusSourceBadge.Dispose();
            _statusAvailabilityBadge.Dispose();
            _statusBrowserBadge.Dispose();
            _statusBoundaryBadge.Dispose();
            _statusDiscoveryBadge.Dispose();
            _resultLabel.Dispose();
            _safetyLabel.Dispose();
            _detailsLabel.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private void RefreshList()
    {
        var filter = _searchBox.Text.Trim();
        var visibleCommands = string.IsNullOrWhiteSpace(filter)
            ? _commands.ToArray()
            : _commands.Where(command => Matches(command, filter)).ToArray();

        _commandsList.BeginUpdate();
        try
        {
            _commandsList.Items.Clear();
            _commandsList.Groups.Clear();
            _categoryGroups.Clear();
            _scopeLabel.Text = BuildScopeSummary(visibleCommands);
            for (var index = 0; index < visibleCommands.Length; index++)
            {
                var command = visibleCommands[index];
                var item = new ListViewItem(command.Category);
                item.SubItems.Add(command.Phrase);
                item.SubItems.Add(BuildListDescriptionPreview(command.Description));
                item.SubItems.Add(command.Source);
                item.SubItems.Add(FormatTier(command.Tier));
                item.SubItems.Add(command.Availability);
                item.SubItems.Add(FormatRisk(command.RiskTier));
                item.SubItems.Add(FormatApproval(command.ApprovalRequirement));
                item.SubItems.Add(FormatAliases(command));
                item.ForeColor = command.LoadStatus == CallsignPackLoadStatus.Loaded
                    ? GetRiskColor(command.RiskTier)
                    : GetAvailabilityColor(command.LoadStatus);
                item.BackColor = index % 2 == 0 ? Color.FromArgb(249, 250, 252) : Color.FromArgb(242, 245, 250);
                item.Tag = command;
                item.Group = GetOrCreateGroup(command.Category);
                _commandsList.Items.Add(item);
            }

            _resultLabel.Text = FormatResultSummary(visibleCommands.Length, _commands.Count, filter);
            var selectedIndex = ChooseInitialSelectionIndex(visibleCommands, filter);
            if (_commandsList.Items.Count > 0)
            {
                _commandsList.Items[selectedIndex].Selected = true;
                _commandsList.Items[selectedIndex].Focused = true;
            }
            var selectedCommand = visibleCommands.ElementAtOrDefault(selectedIndex);
            UpdateDetails(selectedCommand);
            UpdateVoiceCue(filter, selectedCommand, visibleCommands.Length, _commands.Count);
            UpdateStatusStrip(visibleCommands.Length, _commands.Count, filter, visibleCommands.ElementAtOrDefault(selectedIndex), visibleCommands);
        }
        finally
        {
            _commandsList.EndUpdate();
        }

        UpdateQuickFilterSelection(filter);
    }

    private void AddQuickFilterButton(string label, string filter, string description)
    {
        var button = CreateQuickFilterButton(label, filter, description);
        _quickFilterButtons[filter] = button;
        _quickFiltersPanel.Controls.Add(button);
    }

    private Button CreateQuickFilterButton(string label, string filter, string description)
    {
        var button = new Button
        {
            Text = label,
            AutoSize = true,
            Height = 26,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(239, 246, 255),
            ForeColor = Color.FromArgb(30, 64, 175),
            Margin = new Padding(0, 0, 6, 0),
            AccessibleName = $"Command quick filter {label}",
            AccessibleDescription = $"{description} Sets the command search to '{(string.IsNullOrWhiteSpace(filter) ? "all commands" : filter)}'."
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
        button.FlatAppearance.BorderSize = 1;
        button.Tag = filter;
        button.Click += (_, _) =>
        {
            _searchBox.Text = filter;
            _searchBox.Focus();
        };
        return button;
    }

    private void UpdateQuickFilterSelection(string filter)
    {
        var normalized = filter.Trim();
        foreach (var (quickFilter, button) in _quickFilterButtons)
        {
            var isSelected = MatchesQuickFilterSelection(quickFilter, normalized);

            if (isSelected)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(29, 78, 216);
                button.AccessibleDescription = $"{(button.Tag as string ?? string.Empty)}. Selected filter.";
            }
            else
            {
                button.BackColor = Color.FromArgb(239, 246, 255);
                button.ForeColor = Color.FromArgb(30, 64, 175);
                button.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
                button.AccessibleDescription = $"{button.AccessibleName?.Replace("Command quick filter ", string.Empty, StringComparison.OrdinalIgnoreCase)} filter. Click to search for {(string.IsNullOrWhiteSpace(quickFilter) ? "all commands" : quickFilter)}.";
            }
        }
    }

    private static bool MatchesQuickFilterSelection(string quickFilter, string normalizedFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter))
            return string.IsNullOrWhiteSpace(normalizedFilter);

        if (string.Equals(quickFilter, normalizedFilter, StringComparison.OrdinalIgnoreCase))
            return true;

        return quickFilter.Trim().ToLowerInvariant() switch
        {
            "approval" => string.Equals(normalizedFilter, "approval", StringComparison.OrdinalIgnoreCase),
            "visible choice" => string.Equals(normalizedFilter, "approval:visible choice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "approval:ambiguous", StringComparison.OrdinalIgnoreCase),
            "fresh id" => string.Equals(normalizedFilter, "approval:fresh", StringComparison.OrdinalIgnoreCase),
            "no approval" => string.Equals(normalizedFilter, "approval:none", StringComparison.OrdinalIgnoreCase),
            "free parity" => IsFreeParityFilter(normalizedFilter),
            "community" => string.Equals(normalizedFilter, "source:community", StringComparison.OrdinalIgnoreCase),
            "open core" => string.Equals(normalizedFilter, "source:free", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "source:open core", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "source:open-core", StringComparison.OrdinalIgnoreCase),
            "plans" => string.Equals(normalizedFilter, "plans", StringComparison.OrdinalIgnoreCase),
            "read plans status" => string.Equals(normalizedFilter, "read plans status", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "plans", StringComparison.OrdinalIgnoreCase),
            "risk" => string.Equals(normalizedFilter, "risk", StringComparison.OrdinalIgnoreCase),
            "observe" => string.Equals(normalizedFilter, "risk:observe", StringComparison.OrdinalIgnoreCase),
            "local" => string.Equals(normalizedFilter, "risk:local", StringComparison.OrdinalIgnoreCase),
            "external" => string.Equals(normalizedFilter, "risk:external", StringComparison.OrdinalIgnoreCase),
            "blocked" => string.Equals(normalizedFilter, "risk:blocked", StringComparison.OrdinalIgnoreCase),
            "window" => string.Equals(normalizedFilter, "window", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "windowing", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "window management", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "task view", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "snap layouts", StringComparison.OrdinalIgnoreCase),
            "file search" => string.Equals(normalizedFilter, "file search", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFilter, "files", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool Matches(CommandDiscoveryEntry command, string filter)
    {
        if (TryMatchStructuredFilter(command, filter, out var structuredMatch))
            return structuredMatch;

        if (IsExtensionFilter(filter))
            return !string.Equals(command.Source, "Built-in", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(filter.Trim(), "source:community", StringComparison.OrdinalIgnoreCase))
            return command.Source.Contains("community", StringComparison.OrdinalIgnoreCase);

        if (IsFreeParityFilter(filter))
            return command.Tier == CallsignPackTier.Free && IsOpenCoreSource(command.Source);

        if (IsSafetyFilter(filter))
            return IsCoreSessionSafetyCommand(command);

        if (string.Equals(filter.Trim(), "plans", StringComparison.OrdinalIgnoreCase))
            return string.Equals(command.Category, "Navigation", StringComparison.OrdinalIgnoreCase)
                && string.Equals(command.Phrase, "open plans", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(filter.Trim(), "session", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(command.Category, "Session safety", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command.Category, "Runtime", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command.Category, "App launch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command.Category, "Navigation", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(command.Phrase, "open session", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(filter.Trim(), "windowing", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(command.Category, "System", StringComparison.OrdinalIgnoreCase)
                && (
                    Contains(command.Phrase, "window")
                    || Contains(command.Phrase, "desktop")
                    || Contains(command.Phrase, "task view")
                    || Contains(command.Phrase, "snap")
                    || Contains(command.Description, "window")
                    || Contains(command.Description, "desktop")
                    || Contains(command.Description, "task view")
                    || Contains(command.Description, "snap")
                    || Contains(command.Description, "window management"));
        }

        return Contains(command.Category, filter)
            || Contains(command.Phrase, filter)
            || Contains(command.Description, filter)
            || Contains(command.Source, filter)
            || Contains(command.Availability, filter)
            || Contains(command.Tier.ToString(), filter)
            || Contains(command.LoadStatus.ToString(), filter)
            || Contains(FormatRisk(command.RiskTier), filter)
            || Contains(command.RiskTier.ToString(), filter)
            || Contains(FormatApproval(command.ApprovalRequirement), filter)
            || Contains(command.ApprovalRequirement.ToString(), filter)
            || (command.VoicePhrases?.Any(phrase => Contains(phrase, filter)) == true)
            || (command.Examples?.Any(example => Contains(example, filter)) == true);
    }

    private static bool IsExtensionFilter(string filter) =>
        string.Equals(filter.Trim(), "extension", StringComparison.OrdinalIgnoreCase)
        || string.Equals(filter.Trim(), "extensions", StringComparison.OrdinalIgnoreCase)
        || string.Equals(filter.Trim(), "command pack", StringComparison.OrdinalIgnoreCase)
        || string.Equals(filter.Trim(), "command packs", StringComparison.OrdinalIgnoreCase);

    private static bool IsFreeParityFilter(string filter)
    {
        var trimmed = filter.Trim();
        return string.Equals(trimmed, "free parity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "free voice access", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "voice access parity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "open core parity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "windows voice access parity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafetyFilter(string filter) =>
        string.Equals(filter.Trim(), "safety", StringComparison.OrdinalIgnoreCase)
        || string.Equals(filter.Trim(), "stop cancel reset", StringComparison.OrdinalIgnoreCase);

    private static string BuildScopeSummary(IReadOnlyList<CommandDiscoveryEntry> commands)
    {
        if (commands.Count == 0)
            return "Scope: no commands visible";

        var freeCount = commands.Count(command => command.Tier == CallsignPackTier.Free);
        var proCount = commands.Count(command => command.Tier == CallsignPackTier.Pro);
        var advancedCount = commands.Count(command => command.Tier == CallsignPackTier.Advanced);
        var disabledCount = commands.Count(command => command.LoadStatus == CallsignPackLoadStatus.Disabled);
        var gatedCount = commands.Count(command => command.LoadStatus is CallsignPackLoadStatus.EntitlementRequired or CallsignPackLoadStatus.SignatureRequired);
        var extensionCount = commands.Count(command => !IsOpenCoreSource(command.Source));

        var loadedCount = commands.Count(command => command.LoadStatus == CallsignPackLoadStatus.Loaded);
        return $"Scope: Available {loadedCount} · Free {freeCount} · Pro {proCount} · Advanced {advancedCount} · Disabled {disabledCount} · Gated {gatedCount} · Extensions {extensionCount}";
    }

    private static bool TryMatchStructuredFilter(CommandDiscoveryEntry command, string filter, out bool matched)
    {
        matched = false;
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        var trimmed = filter.Trim();
        if (TryGetFilterValue(trimmed, "tier", out var tierValue))
        {
            matched = string.Equals(FormatTier(command.Tier), tierValue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command.Tier.ToString(), tierValue, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        if (TryGetFilterValue(trimmed, "status", out var statusValue))
        {
            matched = MatchesStatus(command, statusValue);
            return true;
        }

        if (TryGetFilterValue(trimmed, "source", out var sourceValue))
        {
            matched = IsBuiltInSourceFilter(sourceValue)
                ? IsBuiltInSource(command.Source)
                : Contains(command.Source, sourceValue);
            return true;
        }

        if (TryGetFilterValue(trimmed, "category", out var categoryValue))
        {
            matched = Contains(command.Category, categoryValue);
            return true;
        }

        if (TryGetFilterValue(trimmed, "approval", out var approvalValue))
        {
            matched = MatchesApproval(command, approvalValue);
            return true;
        }

        if (TryGetFilterValue(trimmed, "risk", out var riskValue))
        {
            matched = MatchesRisk(command, riskValue);
            return true;
        }

        return false;
    }

    private static bool TryGetFilterValue(string filter, string key, out string value)
    {
        var prefix = key + ":";
        if (!filter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = string.Empty;
            return false;
        }

        value = filter[prefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsBuiltInSourceFilter(string sourceValue) =>
        string.Equals(sourceValue, "free", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceValue, "open core", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceValue, "open-core", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceValue, "built-in", StringComparison.OrdinalIgnoreCase);

    private static bool IsBuiltInSource(string source) =>
        IsOpenCoreSource(source);

    private static bool IsOpenCoreSource(string source) =>
        source.StartsWith("Open Core", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesStatus(CommandDiscoveryEntry command, string statusValue)
    {
        if (string.Equals(statusValue, "available", StringComparison.OrdinalIgnoreCase))
            return string.Equals(command.Availability, "Available", StringComparison.OrdinalIgnoreCase)
                || command.LoadStatus == CallsignPackLoadStatus.Loaded;

        if (string.Equals(statusValue, "disabled", StringComparison.OrdinalIgnoreCase))
            return command.LoadStatus == CallsignPackLoadStatus.Disabled;

        if (string.Equals(statusValue, "gated", StringComparison.OrdinalIgnoreCase))
            return command.LoadStatus is CallsignPackLoadStatus.EntitlementRequired or CallsignPackLoadStatus.SignatureRequired;

        if (string.Equals(statusValue, "loaded", StringComparison.OrdinalIgnoreCase))
            return command.LoadStatus == CallsignPackLoadStatus.Loaded;

        return Contains(command.Availability, statusValue)
            || Contains(command.LoadStatus.ToString(), statusValue);
    }

    private static bool MatchesApproval(CommandDiscoveryEntry command, string approvalValue)
    {
        if (string.Equals(approvalValue, "none", StringComparison.OrdinalIgnoreCase))
            return command.ApprovalRequirement == CallsignCommandApprovalRequirement.None;

        if (string.Equals(approvalValue, "approval", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "required", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "require approval", StringComparison.OrdinalIgnoreCase))
            return command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireApproval;

        if (string.Equals(approvalValue, "fresh", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "fresh id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "fresh identity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "identity", StringComparison.OrdinalIgnoreCase))
            return command.ApprovalRequirement == CallsignCommandApprovalRequirement.RequireFreshIdentity;

        if (string.Equals(approvalValue, "ambiguous", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "choice", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "visible choice", StringComparison.OrdinalIgnoreCase)
            || string.Equals(approvalValue, "visible-choice", StringComparison.OrdinalIgnoreCase))
            return command.ApprovalRequirement == CallsignCommandApprovalRequirement.AskWhenAmbiguous;

        if (string.Equals(approvalValue, "blocked", StringComparison.OrdinalIgnoreCase))
            return command.ApprovalRequirement == CallsignCommandApprovalRequirement.Blocked;

        return Contains(FormatApproval(command.ApprovalRequirement), approvalValue)
            || Contains(command.ApprovalRequirement.ToString(), approvalValue);
    }

    private static bool MatchesRisk(CommandDiscoveryEntry command, string riskValue)
    {
        if (string.Equals(riskValue, "observe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "observe only", StringComparison.OrdinalIgnoreCase))
            return command.RiskTier == CallsignCommandRiskTier.Observe;

        if (string.Equals(riskValue, "local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "local change", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "state change", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "local state change", StringComparison.OrdinalIgnoreCase))
            return command.RiskTier == CallsignCommandRiskTier.LocalStateChange;

        if (string.Equals(riskValue, "reversible", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "local reversible", StringComparison.OrdinalIgnoreCase))
            return command.RiskTier == CallsignCommandRiskTier.LocalReversible;

        if (string.Equals(riskValue, "external", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "external side effect", StringComparison.OrdinalIgnoreCase))
            return command.RiskTier == CallsignCommandRiskTier.ExternalSideEffect;

        if (string.Equals(riskValue, "blocked", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "dangerous", StringComparison.OrdinalIgnoreCase)
            || string.Equals(riskValue, "dangerous or blocked", StringComparison.OrdinalIgnoreCase))
            return command.RiskTier == CallsignCommandRiskTier.DangerousOrBlocked;

        return Contains(FormatRisk(command.RiskTier), riskValue)
            || Contains(command.RiskTier.ToString(), riskValue);
    }

    private static bool IsSafetyCommand(CommandDiscoveryEntry command)
    {
        static bool HasSafetyPhrase(string value) =>
            Contains(value, "cancel")
            || Contains(value, "stop listening")
            || Contains(value, "reset session")
            || Contains(value, "stop voice")
            || Contains(value, "go to sleep")
            || Contains(value, "dismiss");

        return HasSafetyPhrase(command.Phrase)
            || HasSafetyPhrase(command.Description)
            || (command.VoicePhrases?.Any(HasSafetyPhrase) == true)
            || (command.Examples?.Any(HasSafetyPhrase) == true);
    }

    private static bool IsCoreSessionSafetyCommand(CommandDiscoveryEntry command)
    {
        return string.Equals(command.Phrase, "cancel", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command.Phrase, "stop listening", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command.Phrase, "reset session", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string value, string filter) =>
        value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static string FormatResultSummary(int visibleCount, int totalCount, string filter)
    {
        var noun = visibleCount == 1 ? "command" : "commands";
        if (string.IsNullOrWhiteSpace(filter))
            return $"{visibleCount} {noun} available";

        return $"{visibleCount} of {totalCount} {noun} match \"{filter.Trim()}\"";
    }

    private static Color GetRiskColor(Callsign.Extensions.CallsignCommandRiskTier riskTier) =>
        riskTier switch
        {
            Callsign.Extensions.CallsignCommandRiskTier.Observe => Color.FromArgb(15, 118, 110),
            Callsign.Extensions.CallsignCommandRiskTier.LocalReversible => Color.FromArgb(30, 64, 175),
            Callsign.Extensions.CallsignCommandRiskTier.LocalStateChange => Color.FromArgb(146, 64, 14),
            Callsign.Extensions.CallsignCommandRiskTier.ExternalSideEffect => Color.FromArgb(185, 28, 28),
            Callsign.Extensions.CallsignCommandRiskTier.DangerousOrBlocked => Color.FromArgb(127, 29, 29),
            _ => Color.FromArgb(15, 23, 42)
        };

    private static Color GetAvailabilityColor(CallsignPackLoadStatus loadStatus) =>
        loadStatus switch
        {
            CallsignPackLoadStatus.EntitlementRequired => Color.FromArgb(146, 64, 14),
            CallsignPackLoadStatus.SignatureRequired => Color.FromArgb(185, 28, 28),
            CallsignPackLoadStatus.Disabled => Color.FromArgb(71, 85, 105),
            _ => Color.FromArgb(100, 116, 139)
        };

    private static string FormatRisk(CallsignCommandRiskTier riskTier) =>
        riskTier switch
        {
            CallsignCommandRiskTier.Observe => "Observe",
            CallsignCommandRiskTier.LocalReversible => "Reversible",
            CallsignCommandRiskTier.LocalStateChange => "Local change",
            CallsignCommandRiskTier.ExternalSideEffect => "External",
            CallsignCommandRiskTier.DangerousOrBlocked => "Blocked",
            _ => riskTier.ToString()
        };

    private static string FormatTier(CallsignPackTier tier) =>
        tier switch
        {
            CallsignPackTier.Free => "Free",
            CallsignPackTier.Pro => "Pro",
            CallsignPackTier.Advanced => "Advanced",
            _ => tier.ToString()
        };

    private static string FormatApproval(CallsignCommandApprovalRequirement approvalRequirement) =>
        approvalRequirement switch
        {
            CallsignCommandApprovalRequirement.None => "None",
            CallsignCommandApprovalRequirement.AskWhenAmbiguous => "Visible Choice",
            CallsignCommandApprovalRequirement.RequireApproval => "Approval",
            CallsignCommandApprovalRequirement.RequireFreshIdentity => "Fresh ID",
            CallsignCommandApprovalRequirement.Blocked => "Blocked",
            _ => approvalRequirement.ToString()
        };

    private static string DescribeBoundary(CommandDiscoveryEntry command)
    {
        if (command.LoadStatus == CallsignPackLoadStatus.Disabled)
            return $"Boundary: {FormatTier(command.Tier)} disabled in Packs";

        if (command.LoadStatus == CallsignPackLoadStatus.EntitlementRequired)
            return $"Boundary: {FormatTier(command.Tier)} locked until entitlement";

        if (command.LoadStatus == CallsignPackLoadStatus.SignatureRequired)
            return $"Boundary: {FormatTier(command.Tier)} locked until signature";

        return command.Tier switch
        {
            CallsignPackTier.Free => "Boundary: Free open",
            CallsignPackTier.Pro => "Boundary: Pro unlocked",
            CallsignPackTier.Advanced => "Boundary: Advanced unlocked",
            _ => $"Boundary: {FormatTier(command.Tier)}"
        };
    }

    private static Label CreatePaletteBadge(string text, string description, Color backColor, Color foreColor)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 4),
            Padding = new Padding(10, 4, 10, 4),
            BackColor = backColor,
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.9f, FontStyle.Bold),
            Text = text,
            AccessibleName = text,
            AccessibleDescription = description
        };
    }

    private static string BuildCommandPaletteVoiceCueText(string filter = "", CommandDiscoveryEntry? selectedCommand = null, int visibleCount = 0, int totalCount = 0)
    {
        if (selectedCommand is not null)
        {
            var routeGate = selectedCommand.LoadStatus == CallsignPackLoadStatus.Loaded
                ? "Policy still decides what can run"
                : $"{selectedCommand.Availability}. This command is listed for discovery only and will not route yet";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                return $"Voice cue: selected {selectedCommand.Phrase} while searching {filter}. {routeGate}. Next: choose a visible action, press escape to close, open voice access guide, or refine the search. Help stays local, and feature-only update splashes still appear when releases only add capabilities.";
            }

            return $"Voice cue: selected {selectedCommand.Phrase}. {routeGate}. Next: choose a visible action, press escape to close, open voice access guide, or refine the search. Help stays local, and feature-only update splashes still appear when releases only add capabilities.";
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var noun = visibleCount == 1 ? "command" : "commands";
            return $"Voice cue: {visibleCount} of {totalCount} {noun} match {filter}. Next: refine the search, review availability, or choose a visible action. Help stays local and policy still decides what can run, and feature-only update splashes still appear when releases only add capabilities.";
        }

        return "Voice cue: search commands, review availability, and choose a visible action. Say what can I say, show numbers, show grid, show keyboard, browser open, browser find, read dictation, show corrections, getting started, open voice access guide, restart listening, read voice mode status, read check-in status, read plans status, read paywall status, open release proof, read release proof, read import summary again, read watch status, open release evidence, open manual evidence, open checklist, open manual evidence checklist, read restart proof, or open packs. Help stays local and policy still decides what can run, and feature-only update splashes still appear when releases only add capabilities.";
    }

    private ListViewGroup GetOrCreateGroup(string category)
    {
        if (_categoryGroups.TryGetValue(category, out var existing))
            return existing;

        var group = new ListViewGroup(category, HorizontalAlignment.Left);
        _categoryGroups[category] = group;
        _commandsList.Groups.Add(group);
        return group;
    }

    private static int ChooseInitialSelectionIndex(IReadOnlyList<CommandDiscoveryEntry> visibleCommands, string filter)
    {
        if (visibleCommands.Count == 0)
            return 0;

        if (IsExtensionFilter(filter))
        {
            var extensionMatch = visibleCommands
                .Select((command, index) => (command, index))
                .FirstOrDefault(item => !IsOpenCoreSource(item.command.Source));
            if (extensionMatch.command is not null)
                return extensionMatch.index;
        }

        if (IsSafetyFilter(filter))
        {
            var safetyMatch = visibleCommands
                .Select((command, index) => (command, index))
                .FirstOrDefault(item => IsCoreSessionSafetyCommand(item.command));
            if (safetyMatch.command is not null)
                return safetyMatch.index;
        }

        if (string.IsNullOrWhiteSpace(filter))
            return 0;

        var primaryMatch = visibleCommands
            .Select((command, index) => (command, index))
            .FirstOrDefault(item =>
                Contains(item.command.Phrase, filter)
                || item.command.VoicePhrases?.Any(phrase => Contains(phrase, filter)) == true
                || item.command.Examples?.Any(example => Contains(example, filter)) == true);

        if (primaryMatch.command is not null)
            return primaryMatch.index;

        var categoryMatch = visibleCommands
            .Select((command, index) => (command, index))
            .FirstOrDefault(item => Contains(item.command.Category, filter));

        return categoryMatch.command is not null ? categoryMatch.index : 0;
    }

    private void UpdateDetails(CommandDiscoveryEntry? command = null)
    {
        command ??= _commandsList.SelectedItems.Count > 0
            ? _commandsList.SelectedItems[0].Tag as CommandDiscoveryEntry
            : null;

        if (command is null)
        {
            _detailsLabel.Text = "Select a command to see details.";
            if (_statusSelectedBadge != null)
                _statusSelectedBadge.Text = "Selected: none";
            return;
        }

        var examples = FormatExamples(command, _searchBox.Text);
        var aliases = FormatAliases(command);
        var featurePreview = BuildFeaturePreview(command);
        var routingGate = DescribeRoutingGate(command);
        _detailsLabel.Text = string.Join(Environment.NewLine, new[]
        {
            $"Selected: {command.Category} · {command.Phrase}",
            $"Source: {command.Source} | Tier: {FormatTier(command.Tier)} | Availability: {command.Availability}",
            $"Feature preview: {featurePreview}",
            $"Boundary: {DescribeBoundary(command)}",
            $"Routing gate: {routingGate}",
            $"Risk: {FormatRisk(command.RiskTier)} | Approval: {FormatApproval(command.ApprovalRequirement)}",
            $"Aliases: {aliases}",
            examples
        });
        if (_statusSelectedBadge != null)
            _statusSelectedBadge.Text = $"Selected: {command.Category} · {command.Phrase}";
    }

    private void UpdateStatusStrip(int visibleCount, int totalCount, string filter, CommandDiscoveryEntry? selectedCommand, IReadOnlyList<CommandDiscoveryEntry> visibleCommands)
    {
        if (_statusStrip == null || _statusStopBadge == null || _statusScopeBadge == null || _statusResultBadge == null || _statusSafetyBadge == null || _statusSelectedBadge == null || _statusPreviewBadge == null || _statusSourceBadge == null || _statusAvailabilityBadge == null || _statusDiscoveryBadge == null || _statusBoundaryBadge == null)
            return;

        var openCoreCount = visibleCommands.Count(command => IsOpenCoreSource(command.Source));
        var extensionCount = visibleCommands.Count - openCoreCount;
        var loadedCount = visibleCommands.Count(command => command.LoadStatus == CallsignPackLoadStatus.Loaded);
        var disabledCount = visibleCommands.Count(command => command.LoadStatus == CallsignPackLoadStatus.Disabled);
        var gatedCount = visibleCommands.Count(command => command.LoadStatus is CallsignPackLoadStatus.EntitlementRequired or CallsignPackLoadStatus.SignatureRequired);

        _statusStopBadge.Text = "STOP";
        _statusScopeBadge.Text = string.IsNullOrWhiteSpace(filter)
            ? "Scope: all commands"
            : $"Scope: {filter}";
        _statusResultBadge.Text = $"Results: {FormatResultSummary(visibleCount, totalCount, filter)}";
        _statusSafetyBadge.Text = "Safety: cancel · stop listening · reset session";
        _statusSelectedBadge.Text = selectedCommand is null
            ? "Selected: none"
            : $"Selected: {selectedCommand.Category} · {selectedCommand.Phrase}";
        _statusPreviewBadge.Text = selectedCommand is null
            ? "Preview: choose a command"
            : $"Preview: {BuildCommandPreviewText(selectedCommand)}";
        _statusSourceBadge.Text = $"Source: open core {openCoreCount} · extensions {extensionCount}";
        _statusAvailabilityBadge.Text = $"Availability: loaded {loadedCount} · disabled {disabledCount} · gated {gatedCount}";
        _statusBrowserBadge.Text = "Browser: helpers visible";
        _statusDiscoveryBadge.Text = "Discovery: open core + extensions";
        _statusBoundaryBadge.Text = selectedCommand is null
            ? "Boundary: Free open"
            : DescribeBoundary(selectedCommand);
    }

    private static string BuildCommandPreviewText(CommandDiscoveryEntry command)
    {
        if (string.Equals(command.Phrase, "read voice mode status", StringComparison.OrdinalIgnoreCase))
            return "current voice-mode selection";

        var example = command.Examples?.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item) && !string.Equals(item, command.Phrase, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(example))
            return $"{command.Phrase} -> {example}";

        return command.Phrase;
    }

    private static string DescribeRoutingGate(CommandDiscoveryEntry command)
    {
        return command.LoadStatus switch
        {
            CallsignPackLoadStatus.Loaded => "available after wake, identity, policy, visibility, approval, and audit gates",
            CallsignPackLoadStatus.Disabled => "listed for discovery only and will not route until enabled from Packs",
            CallsignPackLoadStatus.EntitlementRequired => "listed for discovery only and will not route until entitlement is satisfied",
            CallsignPackLoadStatus.SignatureRequired => "listed for discovery only and will not route until a valid signature is present",
            CallsignPackLoadStatus.InvalidPack => "invalid pack metadata; will not route until fixed",
            CallsignPackLoadStatus.MissingAssembly => "missing assembly; will not route until restored",
            CallsignPackLoadStatus.MissingPackType => "missing pack type; will not route until fixed",
            CallsignPackLoadStatus.DuplicatePackId => "duplicate pack id; will not route until resolved",
            CallsignPackLoadStatus.LoadFailure => "load failure; will not route until fixed",
            _ => $"{command.LoadStatus}; policy still decides whether execution may run"
        };
    }

    private static string BuildFeaturePreview(CommandDiscoveryEntry command)
    {
        if (string.Equals(command.Phrase, "read voice mode status", StringComparison.OrdinalIgnoreCase))
            return "current voice mode selection";

        var summary = string.IsNullOrWhiteSpace(command.Description)
            ? command.Phrase
            : command.Description.Trim();

        summary = SummarizePreviewText(summary, 112);

        if (summary.EndsWith(".", StringComparison.Ordinal))
            return summary;

        return $"{summary}.";
    }

    private static string SummarizePreviewText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmed = text.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;

        var sentenceEnd = trimmed.IndexOfAny(['.', '!', '?']);
        if (sentenceEnd > 0 && sentenceEnd + 1 <= maxLength + 16)
        {
            trimmed = trimmed[..(sentenceEnd + 1)].Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;
        }

        var truncatedLength = Math.Max(0, maxLength - 3);
        return trimmed[..truncatedLength].TrimEnd() + "...";
    }

    private static string BuildListDescriptionPreview(string description) =>
        SummarizePreviewText(string.IsNullOrWhiteSpace(description) ? string.Empty : description, 88);

    private static string FormatExamples(CommandDiscoveryEntry command, string filter)
    {
        if (command.Examples is not { Count: > 0 })
            return "No examples listed.";

        var examples = command.Examples
            .Where(example => !string.IsNullOrWhiteSpace(example))
            .Where(example => !string.Equals(example, command.Phrase, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (examples.Length == 0)
            return "No examples listed.";

        var prioritizedExamples = PrioritizeExamples(examples, filter);
        var representativeExamples = prioritizedExamples
            .Take(2)
            .Concat(prioritizedExamples.Skip(Math.Max(0, prioritizedExamples.Length - 2)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4);

        return $"Try: {string.Join(" · ", representativeExamples)}";
    }

    private static string[] PrioritizeExamples(string[] examples, string filter)
    {
        var normalizedFilter = filter.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFilter))
            return examples;

        var tokens = normalizedFilter
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return examples
            .Select((example, index) => new
            {
                Example = example,
                Index = index,
                ExactMatch = example.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase),
                TokenMatchCount = tokens.Count(token => example.Contains(token, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(item => item.ExactMatch)
            .ThenByDescending(item => item.TokenMatchCount)
            .ThenBy(item => item.Index)
            .Select(item => item.Example)
            .ToArray();
    }

    private static string FormatAliases(CommandDiscoveryEntry command)
    {
        var aliases = command.VoicePhrases?
            .Where(phrase => !string.Equals(phrase, command.Phrase, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToArray();

        return aliases is { Length: > 0 }
            ? string.Join(" · ", aliases)
            : "No aliases listed.";
    }

    private void SearchBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Down || _commandsList.Items.Count == 0)
            return;

        MoveSelection(0);
        _commandsList.Focus();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void CommandsListOnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                MoveSelection(-1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Down:
                MoveSelection(1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Enter:
                Hide();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Escape:
                Hide();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        if (_commandsList.Items.Count == 0)
            return;

        var currentIndex = _commandsList.SelectedIndices.Count > 0
            ? _commandsList.SelectedIndices[0]
            : 0;
        var nextIndex = Math.Clamp(currentIndex + delta, 0, _commandsList.Items.Count - 1);
        _commandsList.BeginUpdate();
        try
        {
            _commandsList.SelectedItems.Clear();
            _commandsList.Items[nextIndex].Selected = true;
            _commandsList.Items[nextIndex].Focused = true;
            _commandsList.EnsureVisible(nextIndex);
        }
        finally
        {
            _commandsList.EndUpdate();
        }

        _commandsList.Focus();
        UpdateDetails();
        UpdateVoiceCue(_searchBox.Text.Trim(), _commandsList.SelectedItems.Count > 0 ? _commandsList.SelectedItems[0].Tag as CommandDiscoveryEntry : null, _commandsList.Items.Count, _commands.Count);
    }

    private void UpdateVoiceCue(string filter, CommandDiscoveryEntry? selectedCommand, int visibleCount, int totalCount)
    {
        _voiceCueLabel.Text = BuildCommandPaletteVoiceCueText(filter, selectedCommand, visibleCount, totalCount);
    }

    private void ApplyPaletteRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        Region = new Region(CreateRoundedPath(ClientRectangle, 26));
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);

        var topLeft = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
        var topRight = new Rectangle(bounds.Right - diameter, bounds.Top, diameter, diameter);
        var bottomRight = new Rectangle(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter);
        var bottomLeft = new Rectangle(bounds.Left, bounds.Bottom - diameter, diameter, diameter);

        path.AddArc(topLeft, 180, 90);
        path.AddLine(bounds.Left + radius, bounds.Top, bounds.Right - radius, bounds.Top);
        path.AddArc(topRight, 270, 90);
        path.AddLine(bounds.Right, bounds.Top + radius, bounds.Right, bounds.Bottom - radius);
        path.AddArc(bottomRight, 0, 90);
        path.AddLine(bounds.Right - radius, bounds.Bottom, bounds.Left + radius, bounds.Bottom);
        path.AddArc(bottomLeft, 90, 90);
        path.AddLine(bounds.Left, bounds.Bottom - radius, bounds.Left, bounds.Top + radius);
        path.CloseFigure();
        return path;
    }
}
