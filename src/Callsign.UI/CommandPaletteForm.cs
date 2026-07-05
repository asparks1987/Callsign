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
    private readonly FlowLayoutPanel _quickFiltersPanel;
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
        AccessibleDescription = "Searchable voice command discovery surface with command tier, availability, risk, approval, examples, and safety commands.";

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
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
            Text = "Say Callsign, verify your callsign, then choose a visible command.",
            AccessibleName = "Command palette session instructions",
            AccessibleDescription = "Explains that commands require wake and identity verification before visible action.",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_subtitleLabel, 0, 1);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11.25f, FontStyle.Regular),
            PlaceholderText = "Search commands, examples, categories, tiers, or status",
            AccessibleName = "Command search",
            AccessibleDescription = "Searches command phrases, examples, categories, extension sources, tiers, availability, load status, risks, and approval requirements. Structured filters such as category:system, settings, media, window, editing, tier:pro, tier:free, status:disabled, and status:gated are supported.",
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        _searchBox.TextChanged += (_, _) => RefreshList();
        _searchBox.KeyDown += SearchBoxOnKeyDown;
        layout.Controls.Add(_searchBox, 0, 2);

        _quickFiltersPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 4),
            Padding = Padding.Empty,
            AccessibleName = "Command palette quick filters",
            AccessibleDescription = "Offers one-click command filters for all commands, available commands, Free commands, Pro commands, Advanced commands, app launch commands, navigation commands, profile commands, runtime commands, update commands, diagnostics commands, help commands, approval-gated commands, risk-gated commands, system commands, browser commands, file commands, keyboard commands, mouse commands, visible control commands, settings commands, media commands, window commands, editing commands, disabled commands, gated commands, safety commands, dictation, and extension commands."
        };
        AddQuickFilterButton("All", string.Empty, "Shows every discoverable Callsign command.");
        AddQuickFilterButton("Available", "status:available", "Shows commands that are available to run right now.");
        AddQuickFilterButton("Free", "free", "Shows commands in the Free open-source parity core.");
        AddQuickFilterButton("Pro", "tier:pro", "Shows only Pro-tier commands and packs.");
        AddQuickFilterButton("Advanced", "tier:advanced", "Shows only Advanced-tier commands and packs.");
        AddQuickFilterButton("Launch", "category:App launch", "Shows visible app-launch commands.");
        AddQuickFilterButton("Navigate", "category:Navigation", "Shows setup and surface-navigation commands.");
        AddQuickFilterButton("Profile", "category:Profile setup", "Shows profile, enrollment, and setup commands.");
        AddQuickFilterButton("Runtime", "category:Runtime", "Shows listener, status, and voice-mode commands.");
        AddQuickFilterButton("Updates", "category:Updates", "Shows update and release-splash commands.");
        AddQuickFilterButton("Diagnostics", "category:Diagnostics", "Shows local diagnostics and folder commands.");
        AddQuickFilterButton("Help", "category:Help", "Shows help and command-discovery commands.");
        AddQuickFilterButton("System", "category:System", "Shows safe local system commands.");
        AddQuickFilterButton("Browser", "category:Browser tabs", "Shows browser navigation and page control commands.");
        AddQuickFilterButton("Files", "category:Files tab", "Shows visible file search and Explorer result commands.");
        AddQuickFilterButton("Keyboard", "category:Keyboard", "Shows keyboard and modifier commands.");
        AddQuickFilterButton("Mouse", "category:Mouse grid", "Shows mouse, grid, and pointer commands.");
        AddQuickFilterButton("Visible", "category:Visible controls", "Shows visible control, overlay, and readout commands.");
        AddQuickFilterButton("Settings", "settings", "Shows safe Windows Settings page commands.");
        AddQuickFilterButton("Media", "media", "Shows playback and media session commands.");
        AddQuickFilterButton("Window", "window", "Shows window switching and window-management commands.");
        AddQuickFilterButton("Editing", "category:Editing", "Shows copy, paste, formatting, and text-movement commands.");
        AddQuickFilterButton("Approval", "approval", "Shows commands that require approval or fresh identity.");
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
        AddQuickFilterButton("Visible Controls", "visible controls", "Shows numbered controls, grid, overlay, and visible action commands.");
        AddQuickFilterButton("Extensions", "extension", "Shows community, Pro, Advanced, disabled, signature-gated, and entitlement-gated extension commands.");
        AddQuickFilterButton("Built-in", "source:Built-in", "Shows built-in commands only.");
        layout.Controls.Add(_quickFiltersPanel, 0, 3);

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
        layout.Controls.Add(_scopeLabel, 0, 4);

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
        layout.Controls.Add(_resultLabel, 0, 5);

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
        layout.Controls.Add(_safetyLabel, 0, 6);

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
        layout.Controls.Add(_detailsLabel, 0, 7);

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
            AccessibleDescription = "Lists visible voice commands with category, phrase, description, source, tier, availability, risk, approval, and aliases."
        };
        _commandsList.Columns.Add("Category", 120);
        _commandsList.Columns.Add("Say", 180);
        _commandsList.Columns.Add("Description", 215);
        _commandsList.Columns.Add("Source", 90);
        _commandsList.Columns.Add("Tier", 70);
        _commandsList.Columns.Add("Availability", 175);
        _commandsList.Columns.Add("Risk", 85);
        _commandsList.Columns.Add("Approval", 110);
        _commandsList.Columns.Add("Aliases", 240);
        _commandsList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _commandsList.SelectedIndexChanged += (_, _) => UpdateDetails();
        _commandsList.KeyDown += CommandsListOnKeyDown;
        layout.Controls.Add(_commandsList, 0, 8);

        ApplyPaletteRegion();
    }

    public int VisibleCommandCount => _commandsList.Items.Count;
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
    public string QuickFiltersAccessibleName => _quickFiltersPanel.AccessibleName ?? string.Empty;
    public string QuickFiltersAccessibleDescription => _quickFiltersPanel.AccessibleDescription ?? string.Empty;
    public string QuickFilterTexts => string.Join(" ", _quickFiltersPanel.Controls.OfType<Button>().Select(button => button.Text));
    public string ActiveQuickFilterText => _quickFiltersPanel.Controls.OfType<Button>()
        .FirstOrDefault(button => button.BackColor.ToArgb() == Color.FromArgb(37, 99, 235).ToArgb())?.Text ?? string.Empty;
    public string SearchAccessibleName => _searchBox.AccessibleName ?? string.Empty;
    public string SearchAccessibleDescription => _searchBox.AccessibleDescription ?? string.Empty;
    public string ResultAccessibleName => _resultLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string DetailsAccessibleName => _detailsLabel.AccessibleName ?? string.Empty;
    public string DetailsAccessibleDescription => _detailsLabel.AccessibleDescription ?? string.Empty;
    public string ResultsAccessibleName => _commandsList.AccessibleName ?? string.Empty;
    public string ResultsAccessibleDescription => _commandsList.AccessibleDescription ?? string.Empty;
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

    public void ShowPalette(IWin32Window owner, IReadOnlyList<CommandDiscoveryEntry> commands)
    {
        _commands = commands;
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
            _quickFiltersPanel.Dispose();
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
                item.SubItems.Add(command.Description);
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
            UpdateDetails(visibleCommands.ElementAtOrDefault(selectedIndex));
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
            "fresh id" => string.Equals(normalizedFilter, "approval:fresh", StringComparison.OrdinalIgnoreCase),
            "no approval" => string.Equals(normalizedFilter, "approval:none", StringComparison.OrdinalIgnoreCase),
            "risk" => string.Equals(normalizedFilter, "risk", StringComparison.OrdinalIgnoreCase),
            "observe" => string.Equals(normalizedFilter, "risk:observe", StringComparison.OrdinalIgnoreCase),
            "local" => string.Equals(normalizedFilter, "risk:local", StringComparison.OrdinalIgnoreCase),
            "external" => string.Equals(normalizedFilter, "risk:external", StringComparison.OrdinalIgnoreCase),
            "blocked" => string.Equals(normalizedFilter, "risk:blocked", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool Matches(CommandDiscoveryEntry command, string filter)
    {
        if (TryMatchStructuredFilter(command, filter, out var structuredMatch))
            return structuredMatch;

        if (IsExtensionFilter(filter))
            return !string.Equals(command.Source, "Built-in", StringComparison.OrdinalIgnoreCase);

        if (IsSafetyFilter(filter))
            return IsSafetyCommand(command);

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
        var extensionCount = commands.Count(command => !string.Equals(command.Source, "Built-in", StringComparison.OrdinalIgnoreCase));

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
            matched = Contains(command.Source, sourceValue);
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

        if (string.Equals(approvalValue, "ambiguous", StringComparison.OrdinalIgnoreCase))
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
        static bool HasCoreSafetyPhrase(string value) =>
            Contains(value, "stop listening")
            || Contains(value, "reset session")
            || Contains(value, "go to sleep")
            || Contains(value, "cancel commands")
            || Contains(value, "cancel visible")
            || Contains(value, "cancel grid");

        return HasCoreSafetyPhrase(command.Phrase)
            || HasCoreSafetyPhrase(command.Description)
            || (command.VoicePhrases?.Any(HasCoreSafetyPhrase) == true)
            || (command.Examples?.Any(HasCoreSafetyPhrase) == true);
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
            CallsignCommandApprovalRequirement.AskWhenAmbiguous => "If ambiguous",
            CallsignCommandApprovalRequirement.RequireApproval => "Approval",
            CallsignCommandApprovalRequirement.RequireFreshIdentity => "Fresh ID",
            CallsignCommandApprovalRequirement.Blocked => "Blocked",
            _ => approvalRequirement.ToString()
        };

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
                .FirstOrDefault(item => !item.command.Source.StartsWith("Built-in", StringComparison.OrdinalIgnoreCase));
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
            return;
        }

        var examples = FormatExamples(command);
        var aliases = FormatAliases(command);
        _detailsLabel.Text = $"Category: {command.Category} | Command: {command.Phrase} | aliases: {aliases} | {command.Source} | Tier: {FormatTier(command.Tier)} | {command.Availability} | {FormatRisk(command.RiskTier)} | {FormatApproval(command.ApprovalRequirement)} | {examples}";
    }

    private static string FormatExamples(CommandDiscoveryEntry command)
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

        var representativeExamples = examples
            .Take(2)
            .Concat(examples.Skip(Math.Max(0, examples.Length - 2)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4);

        return $"Try: {string.Join(" · ", representativeExamples)}";
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
