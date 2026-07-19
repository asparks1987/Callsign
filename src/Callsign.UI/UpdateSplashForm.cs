using System.Globalization;
using System.Drawing.Drawing2D;
using Callsign.Extensions;
using System.Speech.Synthesis;

namespace Callsign.UI;

public sealed class UpdateSplashForm : Form
{
    private readonly System.Windows.Forms.Timer _autoCloseTimer = new() { Interval = 30000 };
    private readonly Panel _surface;
    private readonly Label _titleLabel;
    private readonly Label _contractLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _summaryLabel;
    private readonly Label _highlightLabel;
    private readonly Label _cueLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Control _statusStopBadge;
    private readonly Control _statusBrowserBadge;
    private readonly Label _noteLabel;
    private readonly TextBox _detailsBox;
    private readonly Button _repeatButton;
    private readonly Button _closeButton;
    private readonly string _narrationText;
    private readonly bool _isImportSplash;
    private readonly string _scopeBadgeText;
    private readonly string _kindBadgeText;
    private SpeechSynthesizer? _speechSynthesizer;
    private bool _disposed;

    public UpdateSplashForm(CallsignUpdateManifest manifest, bool isImportSplash = false)
    {
        _isImportSplash = isImportSplash;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        ShowInTaskbar = false;
        Size = new Size(720, 560);
        MinimumSize = new Size(560, 420);
        BackColor = Color.FromArgb(244, 246, 251);
        ForeColor = Color.FromArgb(20, 25, 36);
        Opacity = 0.98;
        DoubleBuffered = true;
        Padding = new Padding(14);
        AccessibleName = isImportSplash ? "Callsign extension pack import splash" : "Callsign update splash";
        AccessibleDescription = isImportSplash
            ? "Visible import summary showing newly imported extension packs and their commands."
            : "Visible update summary showing newly added, changed, removed, and extension-pack commands.";

        _surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(18),
            AccessibleName = isImportSplash ? "Extension pack import splash surface" : "Update splash surface",
            AccessibleDescription = isImportSplash
                ? "Contains the import summary, command changes, pack changes, and dismissal control."
                : "Contains the update summary, command changes, feature changes, and dismissal control."
        };
        Controls.Add(_surface);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            Padding = new Padding(0),
            AutoSize = false
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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

        var released = manifest.PublishedUtc?.ToLocalTime().ToString("g", CultureInfo.InvariantCulture) ?? "unknown time";
        _titleLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
            Text = isImportSplash ? "Callsign Extension Pack Import" : $"Callsign Update {manifest.Version}",
            ForeColor = Color.FromArgb(14, 20, 32),
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            AccessibleName = isImportSplash ? "Extension pack import title" : "Update splash title",
            AccessibleDescription = isImportSplash
                ? "Shows that the current splash summarizes imported extension packs."
                : "Shows the Callsign version that was installed."
        };

        _contractLabel = new Label
        {
            AutoSize = false,
            Text = isImportSplash
                ? "Contract: review imported packs -> inspect changes -> continue."
                : "Contract: review manifest -> inspect changes -> continue.",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            BackColor = Color.FromArgb(233, 241, 255),
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10, 0, 10, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = isImportSplash ? "Pack import contract" : "Update splash contract",
            AccessibleDescription = isImportSplash
                ? "Summarizes the visible pack import flow from review to change inspection before continuing."
                : "Summarizes the visible update flow from manifest review to change inspection before continuing."
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
            TabStop = false,
            AccessibleName = "Close update splash",
            AccessibleDescription = "Dismisses the Callsign update splash."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();

        AcceptButton = _closeButton;
        CancelButton = _closeButton;

        headerRow.Controls.Add(_titleLabel, 0, 0);
        headerRow.Controls.Add(_closeButton, 1, 0);

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = $"Published: {released}",
            Font = new Font("Segoe UI", 9.4f),
            ForeColor = Color.FromArgb(75, 90, 112),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
            AccessibleName = isImportSplash ? "Import timestamp" : "Update published time",
            AccessibleDescription = isImportSplash
                ? "Shows when the imported pack summary was created."
                : "Shows when the update manifest was published."
        };

        var summaryText = string.IsNullOrWhiteSpace(manifest.SplashSummary)
            ? manifest.ReleaseNotes
            : manifest.SplashSummary;
        var isFeatureOnlyManifest = !isImportSplash
            && (manifest.AddedCommands?.Count ?? 0) == 0
            && (manifest.ChangedCommands?.Count ?? 0) == 0
            && (manifest.RemovedCommands?.Count ?? 0) == 0
            && (manifest.ExtensionPackChanges?.Count ?? 0) == 0
            && (manifest.FeatureHighlights?.Count ?? 0) > 0;

        if (isFeatureOnlyManifest && !string.IsNullOrWhiteSpace(summaryText))
            summaryText = $"Feature-only update. {summaryText}";
        else if (isFeatureOnlyManifest)
            summaryText = "Feature-only update.";

        _summaryLabel = new Label
        {
            AutoSize = false,
            Text = summaryText,
            Font = new Font("Segoe UI", 10.2f, FontStyle.Regular),
            ForeColor = Color.FromArgb(26, 34, 48),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0),
            AccessibleName = isImportSplash ? "Import summary" : "Update summary",
            AccessibleDescription = isImportSplash
                ? "Summarizes newly imported Callsign command packs and commands."
                : "Summarizes newly added Callsign commands or features."
        };

        var highlightText = BuildHighlightSummary(manifest, isImportSplash);
        _highlightLabel = new Label
        {
            AutoSize = false,
            Text = highlightText,
            Font = new Font("Segoe UI Semibold", 9.35f, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 44, 62),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0),
            AccessibleName = isImportSplash ? "Pack import highlight summary" : "Update highlight summary",
            AccessibleDescription = isImportSplash
                ? "Shows the first imported pack and command changes at a glance."
                : "Shows the first added command, changed command, and pack change at a glance."
        };

        _narrationText = BuildNarration(manifest, summaryText, highlightText, isImportSplash);
        _scopeBadgeText = BuildScopeBadgeText(manifest, isImportSplash);
        _kindBadgeText = BuildKindBadgeText(manifest, isImportSplash);
        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = Padding.Empty,
            Margin = new Padding(0, 10, 0, 0),
            AutoSize = true,
            AccessibleName = isImportSplash ? "Pack import status strip" : "Update splash status strip",
            AccessibleDescription = isImportSplash
                ? "Shows imported-pack scope, command counts, feature counts, Free boundary, stop state, and browser helper discovery as compact visible badges."
                : "Shows update scope, command counts, feature counts, Free boundary, stop state, and browser helper discovery as compact visible badges."
        };
        var scopeBadge = CreateStatBadge("Scope", 0, Color.FromArgb(245, 243, 255), Color.FromArgb(109, 40, 217));
        scopeBadge.Text = _scopeBadgeText;
        scopeBadge.AccessibleDescription = isImportSplash
            ? "Shows that the splash is showing an imported pack review summary."
            : "Shows whether the update contains commands, packs, features, or a mix at a glance.";
        _statusStrip.Controls.Add(scopeBadge);
        var kindBadge = CreateStatBadge(isImportSplash ? "Type: import" : "Type: update", 0, Color.FromArgb(233, 241, 255), Color.FromArgb(30, 64, 175));
        kindBadge.Text = _kindBadgeText;
        kindBadge.AccessibleDescription = isImportSplash
            ? "Shows that the splash is reviewing imported packs."
            : "Shows whether the manifest is feature-only or mixed with commands and packs.";
        _statusStrip.Controls.Add(kindBadge);
        _statusStrip.Controls.Add(CreateStatBadge("Added", manifest.AddedCommands?.Count ?? 0, Color.FromArgb(220, 237, 255), Color.FromArgb(30, 64, 175)));
        _statusStrip.Controls.Add(CreateStatBadge("Changed", manifest.ChangedCommands?.Count ?? 0, Color.FromArgb(235, 247, 237), Color.FromArgb(22, 101, 52)));
        _statusStrip.Controls.Add(CreateStatBadge("Removed", manifest.RemovedCommands?.Count ?? 0, Color.FromArgb(254, 243, 242), Color.FromArgb(153, 27, 27)));
        _statusStrip.Controls.Add(CreateStatBadge("Packs", manifest.ExtensionPackChanges?.Count ?? 0, Color.FromArgb(243, 232, 255), Color.FromArgb(109, 40, 217)));
        _statusStrip.Controls.Add(CreateStatBadge("Features", manifest.FeatureHighlights?.Count ?? 0, Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70)));
        var boundaryBadge = CreateStatBadge("Boundary", 0, Color.FromArgb(255, 247, 237), Color.FromArgb(154, 52, 18));
        boundaryBadge.Text = "Boundary: Free open";
        boundaryBadge.AccessibleDescription = "Shows that the Free core remains open-source while paid commands stay gated by entitlement and policy.";
        _statusStrip.Controls.Add(boundaryBadge);
        _statusStopBadge = CreateStatBadge("STOP", 0, Color.FromArgb(255, 235, 235), Color.FromArgb(185, 28, 28));
        _statusStopBadge.Text = "STOP";
        _statusStopBadge.AccessibleDescription = "Shows the visible stop and cancel state for the update splash.";
        _statusStrip.Controls.Add(_statusStopBadge);
        _statusBrowserBadge = CreateStatBadge("Browser: helpers visible", 0, Color.FromArgb(240, 249, 255), Color.FromArgb(2, 132, 199));
        _statusBrowserBadge.Text = "Browser: helpers visible";
        _statusBrowserBadge.AccessibleDescription = "Shows that browser overlay helpers stay discoverable from the update splash.";
        _statusStrip.Controls.Add(_statusBrowserBadge);

        _cueLabel = new Label
        {
            AutoSize = false,
            Text = BuildCueText(manifest, highlightText, isImportSplash),
            Font = new Font("Segoe UI", 9.2f),
            ForeColor = Color.FromArgb(75, 90, 112),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0),
            AccessibleName = isImportSplash ? "Pack import voice cue" : "Update splash voice cue",
            AccessibleDescription = isImportSplash
                ? "Explains voice dismissal phrases and that imported pack details do not bypass command policy or entitlement gates."
                : "Explains voice dismissal phrases and that update details do not bypass command policy or entitlement gates."
        };

        _noteLabel = new Label
        {
            AutoSize = false,
            Height = 24,
            Text = "What is new",
            Font = new Font("Segoe UI Semibold", 9.8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 44, 62),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
            AccessibleName = isImportSplash ? "Pack import details heading" : "Update details heading",
            AccessibleDescription = isImportSplash
                ? "Labels the imported pack command and feature details."
                : "Labels the update command and feature details."
        };

        _detailsBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.15f, FontStyle.Regular),
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(20, 27, 44),
            Margin = new Padding(0, 6, 0, 0),
            AccessibleName = isImportSplash ? "Imported pack command details" : "Update command details",
            AccessibleDescription = isImportSplash
                ? "Lists imported pack additions and extension-pack command changes."
                : "Lists added, changed, removed, and extension-pack command changes from the update manifest."
        };
        _detailsBox.Text = BuildDetails(manifest);

        _repeatButton = new Button
        {
            Text = isImportSplash ? "Read Import Again" : "Read Summary Again",
            AutoSize = true,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(20, 27, 44),
            Margin = new Padding(0, 0, 8, 0),
            AccessibleName = isImportSplash ? "Read import summary again" : "Read update summary again",
            AccessibleDescription = isImportSplash
                ? "Repeats the spoken import summary and resets the auto-close timer."
                : "Repeats the spoken update summary and resets the auto-close timer."
        };
        _repeatButton.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 230);
        _repeatButton.FlatAppearance.BorderSize = 1;
        _repeatButton.Click += (_, _) =>
        {
            ResetAutoCloseTimer();
            SpeakNarration();
        };

        var footerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            Padding = Padding.Empty
        };
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var autoCloseLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = isImportSplash ? "This import window closes automatically. Read it again if needed." : "This window closes automatically. Read it again if needed.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(89, 102, 124),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        footerRow.Controls.Add(autoCloseLabel, 0, 0);
        footerRow.Controls.Add(_repeatButton, 1, 0);
        footerRow.Controls.Add(new Panel { Width = 12, Dock = DockStyle.Fill }, 2, 0);

        layout.Controls.Add(headerRow, 0, 0);
        layout.Controls.Add(_contractLabel, 0, 1);
        layout.Controls.Add(_subtitleLabel, 0, 2);
        layout.Controls.Add(_summaryLabel, 0, 3);
        layout.Controls.Add(_highlightLabel, 0, 4);
        layout.Controls.Add(_statusStrip, 0, 5);
        layout.Controls.Add(_cueLabel, 0, 6);
        layout.Controls.Add(_noteLabel, 0, 7);
        layout.Controls.Add(_detailsBox, 0, 8);
        layout.Controls.Add(footerRow, 0, 9);
        _surface.Controls.Add(layout);

        _autoCloseTimer.Tick += (_, _) => Hide();
        WireAutoCloseActivityHandlers(this);
        ApplyRoundedRegion();
    }

    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("update splash");
    public string TitleText => _titleLabel.Text;
    public string CloseGlyphText => _closeButton.Text;
    public string SurfaceAccessibleName => AccessibleName ?? string.Empty;
    public string SurfaceAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string PanelAccessibleName => _surface.AccessibleName ?? string.Empty;
    public int AutoCloseIntervalMilliseconds => _autoCloseTimer.Interval;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string SubtitleAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string SummaryAccessibleName => _summaryLabel.AccessibleName ?? string.Empty;
    public string HighlightAccessibleName => _highlightLabel.AccessibleName ?? string.Empty;
    public string HighlightText => _highlightLabel.Text;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripAccessibleDescription => _statusStrip.AccessibleDescription ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusStopBadgeText => _statusStopBadge.Text;
    public string StatusBrowserBadgeText => _statusBrowserBadge.Text;
    public string ScopeBadgeText => _scopeBadgeText;
    public string KindBadgeText => _kindBadgeText;
    public string DetailsAccessibleName => _detailsBox.AccessibleName ?? string.Empty;
    public string DetailsAccessibleDescription => _detailsBox.AccessibleDescription ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string RepeatButtonText => _repeatButton.Text;
    public string RepeatButtonAccessibleName => _repeatButton.AccessibleName ?? string.Empty;
    public bool IsImportSplash => _isImportSplash;
    public string SummaryText => _summaryLabel.Text;
    public string CueText => _cueLabel.Text;
    public string DetailsText => _detailsBox.Text;
    public string NarrationText => _narrationText;
    public void RepeatNarration()
    {
        ResetAutoCloseTimer();
        SpeakNarration();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyRoundedRegion();
        ResetAutoCloseTimer();
        SpeakNarration();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyRoundedRegion();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
            e.Cancel = true;

        Hide();
        base.OnFormClosing(e);
    }

    public void ShowSplash(IWin32Window owner)
    {
        StartPosition = FormStartPosition.CenterParent;
        if (Visible)
            return;

        Show(owner);
        TopMost = true;
        BringToFront();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _speechSynthesizer?.Dispose();
            _autoCloseTimer.Dispose();
            _detailsBox.Dispose();
            _noteLabel.Dispose();
            _repeatButton.Dispose();
            _cueLabel.Dispose();
            _summaryLabel.Dispose();
            _highlightLabel.Dispose();
            _contractLabel.Dispose();
            _subtitleLabel.Dispose();
            _statusStrip.Dispose();
            _statusStopBadge.Dispose();
            _titleLabel.Dispose();
            _closeButton.Dispose();
            _surface.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private static string BuildDetails(CallsignUpdateManifest manifest)
    {
        var lines = new List<string>();

        if (manifest.Version.StartsWith("pack-import-", StringComparison.OrdinalIgnoreCase))
            lines.Add("Imported packs stay disabled by default until they are reviewed and enabled from Packs.");

        AddHighlightSection(lines, manifest);
        AddSection(lines, "Added", manifest.AddedCommands);
        AddSection(lines, "Changed", manifest.ChangedCommands);
        AddSection(lines, "Removed", manifest.RemovedCommands);
        AddSection(lines, "Pack changes", manifest.ExtensionPackChanges);
        AddSection(lines, "Features", manifest.FeatureHighlights);

        if (lines.Count == 0 && string.IsNullOrWhiteSpace(manifest.ReleaseNotes))
        {
            lines.Add("No detailed changes were included in this update payload.");
            return string.Join(Environment.NewLine, lines);
        }

        if (string.IsNullOrWhiteSpace(manifest.ReleaseNotes))
            return string.Join(Environment.NewLine, lines);

        if (lines.Count > 0)
            lines.Add(string.Empty);

        lines.Add($"Release notes: {manifest.ReleaseNotes}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildNarration(CallsignUpdateManifest manifest, string summaryText, string highlightText, bool isImportSplash)
    {
        var parts = new List<string>
        {
            isImportSplash ? "Callsign extension pack import ready to review." : $"Callsign update {manifest.Version} is ready to review."
        };

        if (isImportSplash)
            parts.Add("Imported packs stay disabled by default until they are reviewed and enabled from Packs.");

        if (!string.IsNullOrWhiteSpace(summaryText))
            parts.Add(summaryText.Trim());

        if (!string.IsNullOrWhiteSpace(highlightText))
            parts.Add(highlightText.Trim());

        AppendSpokenChangeSummary(parts, "Added", manifest.AddedCommands, item => item.DisplayName);
        AppendSpokenChangeSummary(parts, "Changed", manifest.ChangedCommands, item => item.DisplayName);
        AppendSpokenChangeSummary(parts, "Removed", manifest.RemovedCommands, item => item.DisplayName);
        AppendSpokenPackChangeSummary(parts, manifest.ExtensionPackChanges);
        AppendSpokenFeatureSummary(parts, manifest.FeatureHighlights);
        parts.Add(isImportSplash ? "Next: dismiss this import splash to continue." : "Next: close the update splash to continue.");

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildCueText(CallsignUpdateManifest manifest, string highlightText, bool isImportSplash)
    {
        var addedCount = manifest.AddedCommands?.Count ?? 0;
        var changedCount = manifest.ChangedCommands?.Count ?? 0;
        var removedCount = manifest.RemovedCommands?.Count ?? 0;
        var packCount = manifest.ExtensionPackChanges?.Count ?? 0;
        var featureCount = manifest.FeatureHighlights?.Count ?? 0;
        var cuePrefix = isImportSplash
            ? "Voice cue: close pack import splash, dismiss pack import splash, hide pack import splash, cancel pack import splash, or read import summary again."
            : "Voice cue: close update splash, dismiss update splash, hide update splash, or cancel update splash.";
        var stateSummary = isImportSplash
            ? $"Imported packs: {packCount}; added commands: {addedCount}; changed commands: {changedCount}; removed commands: {removedCount}; features: {featureCount}."
            : $"Added commands: {addedCount}; changed commands: {changedCount}; removed commands: {removedCount}; pack changes: {packCount}; features: {featureCount}.";
        var nextStep = isImportSplash
            ? "Next: dismiss this import splash to continue."
            : "Next: close the update splash to continue.";
        var boundary = "Reviewing this update does not enable gated commands; policy and entitlement still decide what can run.";
        var highlights = string.IsNullOrWhiteSpace(highlightText) ? string.Empty : $" {highlightText.Trim()}";

        return $"{cuePrefix} {stateSummary} {nextStep} {boundary}{highlights}";
    }

    private static string BuildScopeBadgeText(CallsignUpdateManifest manifest, bool isImportSplash)
    {
        if (isImportSplash)
            return "Scope: import";

        var kinds = new List<string>();
        if ((manifest.AddedCommands?.Count ?? 0) > 0 || (manifest.ChangedCommands?.Count ?? 0) > 0 || (manifest.RemovedCommands?.Count ?? 0) > 0)
            kinds.Add("commands");
        if ((manifest.ExtensionPackChanges?.Count ?? 0) > 0)
            kinds.Add("packs");
        if ((manifest.FeatureHighlights?.Count ?? 0) > 0)
            kinds.Add("features");

        return kinds.Count == 0
            ? "Scope: manifest"
            : $"Scope: {string.Join(" + ", kinds)}";
    }

    private static string BuildKindBadgeText(CallsignUpdateManifest manifest, bool isImportSplash)
    {
        if (isImportSplash)
            return "Type: import";

        var hasCommands = (manifest.AddedCommands?.Count ?? 0) > 0
            || (manifest.ChangedCommands?.Count ?? 0) > 0
            || (manifest.RemovedCommands?.Count ?? 0) > 0;
        var hasPacks = (manifest.ExtensionPackChanges?.Count ?? 0) > 0;
        var hasFeatures = (manifest.FeatureHighlights?.Count ?? 0) > 0;

        if (hasFeatures && !hasCommands && !hasPacks)
            return "Type: feature-only";

        if (hasCommands || hasPacks)
            return hasFeatures ? "Type: mixed" : "Type: commands";

        return "Type: manifest";
    }

    private void SpeakNarration()
    {
        try
        {
            _speechSynthesizer ??= new SpeechSynthesizer();
            _speechSynthesizer.SpeakAsyncCancelAll();
            _speechSynthesizer.SpeakAsync(_narrationText);
        }
        catch
        {
            // Narration is best-effort and should never block the splash.
        }
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        Region = new Region(CreateRoundedPath(ClientRectangle, 20));
    }

    private static Control CreateStatBadge(string label, int count, Color backColor, Color foreColor)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 4, 10, 4),
            Text = $"{label}: {count}",
            BackColor = backColor,
            ForeColor = foreColor,
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = $"{label}: {count}",
            AccessibleDescription = $"Shows the {label.ToLowerInvariant()} count on the update or import splash."
        };
    }

    private static string BuildHighlightSummary(CallsignUpdateManifest manifest, bool isImportSplash)
    {
        var highlights = new List<string>();
        AddCommandHighlightSummary(highlights, "Added", manifest.AddedCommands);
        AddCommandHighlightSummary(highlights, "Changed", manifest.ChangedCommands);
        AddPackHighlightSummary(highlights, "Packs", manifest.ExtensionPackChanges);
        AddFeatureHighlightSummary(highlights, "Features", manifest.FeatureHighlights);

        if (highlights.Count == 0)
            return isImportSplash ? "Highlights: no imported changes." : "Highlights: no new changes.";

        return $"Highlights: {string.Join(" \u00B7 ", highlights)}.";
    }

    private static void AddHighlightSection(List<string> lines, CallsignUpdateManifest manifest)
    {
        var highlights = new List<string>();
        AddCommandHighlightNames(highlights, "Added", manifest.AddedCommands);
        AddCommandHighlightNames(highlights, "Changed", manifest.ChangedCommands);
        AddPackHighlightNames(highlights, "Packs", manifest.ExtensionPackChanges);
        AddFeatureHighlightNames(highlights, "Features", manifest.FeatureHighlights);

        if (highlights.Count == 0)
            return;

        lines.Add("[Highlights]");
        foreach (var highlight in highlights)
            lines.Add($"- {highlight}");
        lines.Add(string.Empty);
    }

    private void ResetAutoCloseTimer()
    {
        if (_disposed)
            return;

        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    private void WireAutoCloseActivityHandlers(Control control)
    {
        control.MouseEnter += (_, _) => ResetAutoCloseTimer();
        control.MouseMove += (_, _) => ResetAutoCloseTimer();
        control.Enter += (_, _) => ResetAutoCloseTimer();
        control.GotFocus += (_, _) => ResetAutoCloseTimer();

        foreach (Control child in control.Controls)
            WireAutoCloseActivityHandlers(child);
    }

    private static void AddCommandHighlightSummary(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateCommandChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var first = entries
            .Select(entry => (entry.CommandId, entry.DisplayName, entry.Category))
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.DisplayName));

        if (string.IsNullOrWhiteSpace(first.DisplayName))
            return;

        var display = string.IsNullOrWhiteSpace(first.CommandId)
            ? first.DisplayName
            : $"{first.CommandId} ({first.DisplayName})";
        if (!string.IsNullOrWhiteSpace(first.Category))
            display += $" [{first.Category}]";

        highlights.Add($"{label}: {display}");
    }

    private static void AddCommandHighlightNames(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateCommandChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var names = entries
            .Select(entry => (entry.CommandId, entry.DisplayName, entry.Category))
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .Take(2)
            .ToArray();

        if (names.Length == 0)
            return;

        highlights.Add($"{label}: {string.Join(", ", names.Select(item =>
        {
            var display = string.IsNullOrWhiteSpace(item.CommandId) ? item.DisplayName : $"{item.CommandId} ({item.DisplayName})";
            return string.IsNullOrWhiteSpace(item.Category) ? display : $"{display} [{item.Category}]";
        }))}");
    }

    private static void AddPackHighlightSummary(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateExtensionChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var first = entries
            .Select(entry => (entry.PackId, entry.DisplayName, entry.Version))
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.DisplayName));

        if (string.IsNullOrWhiteSpace(first.DisplayName))
            return;

        var display = string.IsNullOrWhiteSpace(first.PackId)
            ? first.DisplayName
            : $"{first.PackId} ({first.DisplayName})";
        if (!string.IsNullOrWhiteSpace(first.Version))
            display += $" v{first.Version}";

        highlights.Add($"{label}: {display}");
    }

    private static void AddPackHighlightNames(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateExtensionChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var names = entries
            .Select(entry => (entry.PackId, entry.DisplayName, entry.Version))
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .Take(2)
            .ToArray();

        if (names.Length == 0)
            return;

        highlights.Add($"{label}: {string.Join(", ", names.Select(item =>
        {
            var display = string.IsNullOrWhiteSpace(item.PackId) ? item.DisplayName : $"{item.PackId} ({item.DisplayName})";
            return string.IsNullOrWhiteSpace(item.Version) ? display : $"{display} v{item.Version}";
        }))}");
    }

    private static void AddFeatureHighlightSummary(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateFeatureChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var first = entries
            .Select(entry => (entry.FeatureId, entry.DisplayName, entry.Category))
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.DisplayName));

        if (string.IsNullOrWhiteSpace(first.DisplayName))
            return;

        var display = string.IsNullOrWhiteSpace(first.FeatureId)
            ? first.DisplayName
            : $"{first.FeatureId} ({first.DisplayName})";
        if (!string.IsNullOrWhiteSpace(first.Category))
            display += $" [{first.Category}]";

        highlights.Add($"{label}: {display}");
    }

    private static void AddFeatureHighlightNames(
        List<string> highlights,
        string label,
        IReadOnlyList<CallsignUpdateFeatureChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var names = entries
            .Select(entry => (entry.FeatureId, entry.DisplayName, entry.Category))
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .Take(2)
            .ToArray();

        if (names.Length == 0)
            return;

        highlights.Add($"{label}: {string.Join(", ", names.Select(item =>
        {
            var display = string.IsNullOrWhiteSpace(item.FeatureId) ? item.DisplayName : $"{item.FeatureId} ({item.DisplayName})";
            return string.IsNullOrWhiteSpace(item.Category) ? display : $"{display} [{item.Category}]";
        }))}");
    }

    private static void AppendSpokenFeatureSummary(
        List<string> parts,
        IReadOnlyList<CallsignUpdateFeatureChange>? entries)
    {
        if (entries is not { Count: > 0 })
            return;

        var names = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.DisplayName))
            .Take(2)
            .Select(entry => string.IsNullOrWhiteSpace(entry.Category)
                ? entry.DisplayName
                : $"{entry.DisplayName} in {entry.Category}")
            .ToArray();

        if (names.Length == 0)
            return;

        parts.Add($"Features: {string.Join(", ", names)}.");
    }

    private static void AppendSpokenChangeSummary<T>(
        List<string> parts,
        string label,
        IReadOnlyList<T>? entries,
        Func<T, string> displayNameSelector)
    {
        var count = entries?.Count ?? 0;
        if (count == 0)
        {
            parts.Add($"{label} 0.");
            return;
        }

        var names = entries!
            .Select(displayNameSelector)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(3)
            .ToArray();

        if (names.Length == 0)
        {
            parts.Add($"{label} {count}.");
            return;
        }

        var suffix = count > names.Length ? $" and {count - names.Length} more" : string.Empty;
        parts.Add($"{label} {count}: {string.Join(", ", names)}{suffix}.");
    }

    private static void AppendSpokenPackChangeSummary(
        List<string> parts,
        IReadOnlyList<CallsignUpdateExtensionChange>? entries)
    {
        var count = entries?.Count ?? 0;
        if (count == 0)
        {
            parts.Add("Pack changes 0.");
            return;
        }

        var names = entries!
            .Select(entry =>
            {
                var source = entry.IsCommunity ? "community" : "trusted";
                var signature = string.IsNullOrWhiteSpace(entry.SignatureStatus) ? "unknown signature" : $"signature {entry.SignatureStatus}";
                return $"{entry.DisplayName} ({source}, {FormatTier(entry.Tier)}, {signature})";
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(3)
            .ToArray();

        if (names.Length == 0)
        {
            parts.Add($"Pack changes {count}.");
            return;
        }

        var suffix = count > names.Length ? $" and {count - names.Length} more" : string.Empty;
        parts.Add($"Pack changes {count}: {string.Join(", ", names)}{suffix}.");
    }

    private static string FormatTier(CallsignPackTier tier) =>
        tier switch
        {
            CallsignPackTier.Free => "Free",
            CallsignPackTier.Pro => "Pro",
            CallsignPackTier.Advanced => "Advanced",
            _ => tier.ToString()
        };

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

    private static void AddSection(List<string> lines, string heading, IReadOnlyList<CallsignUpdateCommandChange>? entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        lines.Add($"[{heading}]");
        foreach (var entry in entries)
        {
            var summary = string.IsNullOrWhiteSpace(entry.Summary)
                ? string.Empty
                : $"{Environment.NewLine}   {entry.Summary}";
            lines.Add($"- {entry.CommandId}: {entry.DisplayName} [{entry.Category}] ({entry.Tier}){summary}".TrimEnd());
        }
        lines.Add(string.Empty);
    }

    private static void AddSection(List<string> lines, string heading, IReadOnlyList<CallsignUpdateExtensionChange>? entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        lines.Add($"[{heading}]");
        foreach (var entry in entries)
        {
            var source = entry.IsCommunity ? "community" : "trusted";
            var signature = string.IsNullOrWhiteSpace(entry.SignatureStatus)
                ? string.Empty
                : $" signature={entry.SignatureStatus}";
            lines.Add($"- {entry.PackId}: {entry.DisplayName} v{entry.Version} [{entry.Tier}; {source}]{signature}{Environment.NewLine}   {entry.Summary}".TrimEnd());
        }

        lines.Add(string.Empty);
    }

    private static void AddSection(List<string> lines, string heading, IReadOnlyList<CallsignUpdateFeatureChange>? entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        lines.Add($"[{heading}]");
        foreach (var entry in entries)
        {
            var summary = string.IsNullOrWhiteSpace(entry.Summary)
                ? string.Empty
                : $"{Environment.NewLine}   {entry.Summary}";
            lines.Add($"- {entry.FeatureId}: {entry.DisplayName} [{entry.Category}]{summary}".TrimEnd());
        }

        lines.Add(string.Empty);
    }
}
