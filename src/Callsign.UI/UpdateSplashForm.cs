using System.Globalization;
using System.Drawing.Drawing2D;
using Callsign.Extensions;
using System.Speech.Synthesis;

namespace Callsign.UI;

public sealed class UpdateSplashForm : Form
{
    private readonly System.Windows.Forms.Timer _autoCloseTimer = new() { Interval = 18000 };
    private readonly Panel _surface;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _summaryLabel;
    private readonly Label _cueLabel;
    private readonly Label _noteLabel;
    private readonly TextBox _detailsBox;
    private readonly Button _closeButton;
    private readonly string _narrationText;
    private readonly bool _isImportSplash;
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
            RowCount = 8,
            ColumnCount = 1,
            Padding = new Padding(0),
            AutoSize = false
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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

        _closeButton = new Button
        {
            Text = "×",
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

        _narrationText = BuildNarration(manifest, summaryText, isImportSplash);
        var statsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = Padding.Empty,
            Margin = new Padding(0, 10, 0, 0),
            AutoSize = true
        };
        statsRow.Controls.Add(CreateStatBadge("Added", manifest.AddedCommands?.Count ?? 0, Color.FromArgb(220, 237, 255), Color.FromArgb(30, 64, 175)));
        statsRow.Controls.Add(CreateStatBadge("Changed", manifest.ChangedCommands?.Count ?? 0, Color.FromArgb(235, 247, 237), Color.FromArgb(22, 101, 52)));
        statsRow.Controls.Add(CreateStatBadge("Removed", manifest.RemovedCommands?.Count ?? 0, Color.FromArgb(254, 243, 242), Color.FromArgb(153, 27, 27)));
        statsRow.Controls.Add(CreateStatBadge("Packs", manifest.ExtensionPackChanges?.Count ?? 0, Color.FromArgb(243, 232, 255), Color.FromArgb(109, 40, 217)));

        _cueLabel = new Label
        {
            AutoSize = false,
            Text = isImportSplash
                ? "Voice cue: close pack import splash, dismiss pack import splash, hide pack import splash, or cancel pack import splash. Reviewing imported packs does not enable gated commands; policy and entitlement still decide what can run."
                : "Voice cue: close update splash, dismiss update splash, hide update splash, or cancel update splash. Reviewing this update does not enable gated commands; policy and entitlement still decide what can run.",
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

        var footerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            Padding = Padding.Empty
        };
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var autoCloseLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = isImportSplash ? "This import window closes automatically." : "This window closes automatically.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(89, 102, 124),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        footerRow.Controls.Add(autoCloseLabel, 0, 0);
        footerRow.Controls.Add(new Panel { Width = 12, Dock = DockStyle.Fill }, 1, 0);

        layout.Controls.Add(headerRow, 0, 0);
        layout.Controls.Add(_subtitleLabel, 0, 1);
        layout.Controls.Add(_summaryLabel, 0, 2);
        layout.Controls.Add(statsRow, 0, 3);
        layout.Controls.Add(_cueLabel, 0, 4);
        layout.Controls.Add(_noteLabel, 0, 5);
        layout.Controls.Add(_detailsBox, 0, 6);
        layout.Controls.Add(footerRow, 0, 7);
        _surface.Controls.Add(layout);

        _autoCloseTimer.Tick += (_, _) => Hide();
        _autoCloseTimer.Start();
        ApplyRoundedRegion();
    }

    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("update splash");
    public string TitleText => _titleLabel.Text;
    public string CloseGlyphText => _closeButton.Text;
    public string SurfaceAccessibleName => AccessibleName ?? string.Empty;
    public string SurfaceAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string PanelAccessibleName => _surface.AccessibleName ?? string.Empty;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string SubtitleAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string SummaryAccessibleName => _summaryLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string DetailsAccessibleName => _detailsBox.AccessibleName ?? string.Empty;
    public string DetailsAccessibleDescription => _detailsBox.AccessibleDescription ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string SummaryText => _summaryLabel.Text;
    public string CueText => _cueLabel.Text;
    public string DetailsText => _detailsBox.Text;
    public string NarrationText => _narrationText;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyRoundedRegion();
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
            _cueLabel.Dispose();
            _summaryLabel.Dispose();
            _subtitleLabel.Dispose();
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

        AddSection(lines, "Added", manifest.AddedCommands);
        AddSection(lines, "Changed", manifest.ChangedCommands);
        AddSection(lines, "Removed", manifest.RemovedCommands);
        AddSection(lines, "Pack changes", manifest.ExtensionPackChanges);

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

    private static string BuildNarration(CallsignUpdateManifest manifest, string summaryText, bool isImportSplash)
    {
        var parts = new List<string>
        {
            isImportSplash ? "Callsign extension pack import." : $"Callsign update {manifest.Version}."
        };

        if (!string.IsNullOrWhiteSpace(summaryText))
            parts.Add(summaryText.Trim());

        AppendSpokenChangeSummary(parts, "Added", manifest.AddedCommands, item => item.DisplayName);
        AppendSpokenChangeSummary(parts, "Changed", manifest.ChangedCommands, item => item.DisplayName);
        AppendSpokenChangeSummary(parts, "Removed", manifest.RemovedCommands, item => item.DisplayName);
        AppendSpokenChangeSummary(parts, "Pack changes", manifest.ExtensionPackChanges, item => item.DisplayName);

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
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
            TextAlign = ContentAlignment.MiddleCenter
        };
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
            lines.Add($"- {entry.CommandId}: {entry.DisplayName} [{entry.Category}] ({entry.Tier})");
        lines.Add(string.Empty);
    }

    private static void AddSection(List<string> lines, string heading, IReadOnlyList<CallsignUpdateExtensionChange>? entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        lines.Add($"[{heading}]");
        foreach (var entry in entries)
        {
            var signature = string.IsNullOrWhiteSpace(entry.SignatureStatus)
                ? string.Empty
                : $" signature={entry.SignatureStatus}";
            lines.Add($"- {entry.PackId}: {entry.DisplayName} v{entry.Version} [{entry.Tier}] {signature}{Environment.NewLine}   {entry.Summary}".TrimEnd());
        }

        lines.Add(string.Empty);
    }
}
