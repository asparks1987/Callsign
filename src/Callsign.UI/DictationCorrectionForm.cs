using Callsign.UI.Services;
using System.Drawing.Drawing2D;

namespace Callsign.UI;

public sealed class DictationCorrectionForm : Form
{
    private readonly Panel _surface;
    private readonly Label _titleLabel;
    private readonly Label _contractLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _summaryLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _scopeBadge;
    private readonly Label _choiceBadge;
    private readonly Label _safetyBadge;
    private readonly Label _cueLabel;
    private readonly Label _safetyLabel;
    private readonly Button _closeButton;
    private readonly ListView _choicesList;
    private bool _disposed;

    public DictationCorrectionForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(248, 250, 253);
        ForeColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.975;
        DoubleBuffered = true;
        Width = 660;
        Height = 440;
        MinimumSize = new Size(480, 320);
        Padding = new Padding(16);
        Text = "Callsign correction alternatives";
        AccessibleName = "Dictation correction alternatives";
        AccessibleDescription = "Visible correction chooser for reviewed dictation text with numbered alternatives and spoken navigation commands.";

        _surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(16),
            AccessibleName = "Dictation correction surface",
            AccessibleDescription = "Shows correction scope, voice cues, and numbered alternatives before replacing reviewed dictation text."
        };
        Controls.Add(_surface);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _surface.Controls.Add(layout);

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Correction alternatives",
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(14, 20, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = "Dictation correction title",
            AccessibleDescription = "Names the correction alternatives surface."
        };

        _contractLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Contract: review text -> choose alternative -> accept or cancel.",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            BackColor = Color.FromArgb(233, 241, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            AutoEllipsis = true,
            AccessibleName = "Dictation correction contract",
            AccessibleDescription = "Summarizes the visible correction workflow from review to choice, accept, or cancel."
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
            AccessibleName = "Close correction alternatives",
            AccessibleDescription = "Dismisses the correction alternatives surface without changing reviewed text."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();

        AcceptButton = _closeButton;
        CancelButton = _closeButton;

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

        layout.Controls.Add(headerRow, 0, 0);
        layout.Controls.Add(_contractLabel, 0, 1);

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Say next correction, previous correction, accept correction, choose correction 1, or close correction.",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = "Dictation correction scope",
            AccessibleDescription = "Shows the reviewed text scope and spoken correction navigation commands."
        };
        layout.Controls.Add(_subtitleLabel, 0, 2);

        _summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Review alternatives before replacing text.",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(83, 95, 117),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = "Dictation correction summary",
            AccessibleDescription = "Summarizes available correction choices and the selected alternative."
        };
        layout.Controls.Add(_summaryLabel, 0, 3);

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
            Padding = Padding.Empty,
            AccessibleName = "Correction status strip",
            AccessibleDescription = "Shows the reviewed text scope, the selected correction number, and the safety state at a glance."
        };
        _scopeBadge = CreateBadge("Scope: none", "Shows the reviewed text scope.");
        _choiceBadge = CreateBadge("Choice: none", "Shows the currently selected correction number.");
        _safetyBadge = CreateBadge("Safety: close leaves text unchanged", "Shows that closing or cancelling leaves the reviewed text unchanged.");
        _statusStrip.Controls.Add(_scopeBadge);
        _statusStrip.Controls.Add(_choiceBadge);
        _statusStrip.Controls.Add(_safetyBadge);
        layout.Controls.Add(_statusStrip, 0, 4);

        _cueLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = BuildCueText("Choose by voice: next correction | previous correction | accept correction | choose correction 1 | close correction", choicesCount: 0, scopeText: "Scope: none", selectedChoiceText: string.Empty),
            Font = new Font("Segoe UI Semibold", 9.1f, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 78, 216),
            BackColor = Color.FromArgb(235, 242, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            AutoEllipsis = true,
            AccessibleName = "Dictation correction voice cue",
            AccessibleDescription = "Lists spoken commands for moving through, choosing, accepting, or closing correction alternatives."
        };
        layout.Controls.Add(_cueLabel, 0, 5);

        _safetyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Safety: choose or accept replaces reviewed text; close or cancel leaves the review buffer unchanged.",
            Font = new Font("Segoe UI", 8.9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(91, 33, 182),
            BackColor = Color.FromArgb(245, 243, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            AutoEllipsis = true,
            AccessibleName = "Dictation correction safety",
            AccessibleDescription = "Explains that choosing or accepting applies a replacement, while closing or cancelling leaves the reviewed dictation text unchanged."
        };
        layout.Controls.Add(_safetyLabel, 0, 6);

        _choicesList = new ListView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            View = View.Details,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            AccessibleName = "Dictation correction alternatives",
            AccessibleDescription = "Lists numbered correction alternatives that can be chosen by voice before replacement."
        };
        _choicesList.Columns.Add("#", 48);
        _choicesList.Columns.Add("Alternative", 300);
        _choicesList.Columns.Add("Kind", 150);
        _choicesList.SelectedIndexChanged += (_, _) => UpdateSummaryFromSelection();
        _choicesList.KeyDown += ChoicesListOnKeyDown;
        layout.Controls.Add(_choicesList, 0, 7);

        ApplyRoundedRegion();
    }

    public int ChoiceCount => _choicesList.Items.Count;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("correction alternatives");
    public string SummaryText => _summaryLabel.Text;
    public string CueText => _cueLabel.Text;
    public string SafetyText => _safetyLabel.Text;
    public string ScopeText => _subtitleLabel.Text;
    public Size HudSize => Size;
    public string SelectedChoiceText => _choicesList.SelectedItems.Count > 0 ? _choicesList.SelectedItems[0].SubItems[1].Text : string.Empty;
    public string SelectedChoiceNumber => _choicesList.SelectedItems.Count > 0 ? _choicesList.SelectedItems[0].Text : string.Empty;
    public string SurfaceAccessibleName => AccessibleName ?? string.Empty;
    public string SurfaceAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string PanelAccessibleName => _surface.AccessibleName ?? string.Empty;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string ScopeAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string SummaryAccessibleName => _summaryLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripAccessibleDescription => _statusStrip.AccessibleDescription ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string ScopeBadgeText => _scopeBadge.Text;
    public string ChoiceBadgeText => _choiceBadge.Text;
    public string SafetyBadgeText => _safetyBadge.Text;
    public string ChoicesAccessibleName => _choicesList.AccessibleName ?? string.Empty;
    public string ChoicesAccessibleDescription => _choicesList.AccessibleDescription ?? string.Empty;

    public void ShowCorrections(IWin32Window owner, IReadOnlyList<DictationCorrectionChoice> choices, DictationReplacementScope scope)
    {
        var scopeText = $"Scope: {FormatScope(scope)}.";
        _subtitleLabel.Text = $"{scopeText} Say next correction, previous correction, accept correction, choose correction 1, or close correction.";
        _summaryLabel.Text = choices.Count == 0
            ? "No alternatives are available for this span."
            : $"Showing {choices.Count} correction alternative{(choices.Count == 1 ? string.Empty : "s")} for the reviewed text.";
        _cueLabel.Text = BuildCueText(
            choices.Count == 0
                ? "No replacement will run until alternatives are available."
                : $"Choose by voice: next correction | previous correction | accept correction | choose correction 1-{choices.Count} | close correction",
            choices.Count,
            scopeText,
            string.Empty);
        UpdateStatusStrip(scope, choices.Count);
        _choicesList.BeginUpdate();
        try
        {
            _choicesList.Items.Clear();
            foreach (var choice in choices)
            {
                var item = new ListViewItem(choice.Number.ToString());
                item.SubItems.Add(choice.Text);
                item.SubItems.Add(choice.Label);
                item.BackColor = choice.Number % 2 == 1 ? Color.White : Color.FromArgb(244, 247, 252);
                _choicesList.Items.Add(item);
            }
        }
        finally
        {
            _choicesList.EndUpdate();
        }

        if (_choicesList.Items.Count > 0)
        {
            _choicesList.Items[0].Selected = true;
            _choicesList.Items[0].Focused = true;
            _choicesList.Items[0].EnsureVisible();
        }

        if (Visible)
        {
            BringToFront();
            _choicesList.Focus();
            return;
        }

        Show(owner);
        BringToFront();
        _choicesList.Focus();
    }

    public bool MoveSelectionByVoice(int delta)
    {
        if (_choicesList.Items.Count == 0)
            return false;

        var before = _choicesList.SelectedIndices.Count > 0
            ? _choicesList.SelectedIndices[0]
            : 0;
        MoveSelection(delta);
        var after = _choicesList.SelectedIndices.Count > 0
            ? _choicesList.SelectedIndices[0]
            : before;
        return after != before;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var shadowPen = new Pen(Color.FromArgb(36, 0, 0, 0), 4f);
        using var borderPen = new Pen(Color.FromArgb(108, 255, 255, 255), 1.1f);
        using var path = CreateRoundedPath(ClientRectangle, 24);
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
            _summaryLabel.Dispose();
            _titleLabel.Dispose();
            _contractLabel.Dispose();
            _closeButton.Dispose();
            _subtitleLabel.Dispose();
            _statusStrip.Dispose();
            _scopeBadge.Dispose();
            _choiceBadge.Dispose();
            _safetyBadge.Dispose();
            _cueLabel.Dispose();
            _safetyLabel.Dispose();
            _choicesList.Dispose();
            _surface.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private void UpdateSummaryFromSelection()
    {
        if (_choicesList.SelectedItems.Count == 0)
            return;

        var item = _choicesList.SelectedItems[0];
        if (item.SubItems.Count < 3)
            return;

        _summaryLabel.Text = $"Selected {item.SubItems[0].Text}: {item.SubItems[1].Text} ({item.SubItems[2].Text}).";
        _choiceBadge.Text = $"Choice: {item.SubItems[0].Text} of {_choicesList.Items.Count}";
        _cueLabel.Text = BuildCueText(
            $"Choose by voice: next correction | previous correction | accept correction | choose correction {item.SubItems[0].Text} | close correction",
            _choicesList.Items.Count,
            _subtitleLabel.Text,
            _summaryLabel.Text);
    }

    private static string BuildCueText(string prefix, int choicesCount, string scopeText, string selectedChoiceText)
    {
        var choiceSummary = choicesCount == 0
            ? "No alternatives available."
            : choicesCount == 1
                ? "1 alternative available."
                : $"{choicesCount} alternatives available.";
        var selectionSummary = string.IsNullOrWhiteSpace(selectedChoiceText)
            ? "No choice selected."
            : $"Selected choice: {selectedChoiceText}";
        return $"{prefix} {choiceSummary} {selectionSummary} {scopeText} Choose or accept replaces reviewed text; close or cancel leaves the review buffer unchanged.";
    }

    private void ChoicesListOnKeyDown(object? sender, KeyEventArgs e)
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
        if (_choicesList.Items.Count == 0)
            return;

        var currentIndex = _choicesList.SelectedIndices.Count > 0
            ? _choicesList.SelectedIndices[0]
            : 0;
        var nextIndex = Math.Clamp(currentIndex + delta, 0, _choicesList.Items.Count - 1);
        _choicesList.BeginUpdate();
        try
        {
            _choicesList.SelectedItems.Clear();
            _choicesList.Items[nextIndex].Selected = true;
            _choicesList.Items[nextIndex].Focused = true;
            _choicesList.Items[nextIndex].EnsureVisible();
        }
        finally
        {
            _choicesList.EndUpdate();
        }

        _choicesList.Focus();
        UpdateSummaryFromSelection();
    }

    private void UpdateStatusStrip(DictationReplacementScope scope, int choiceCount)
    {
        _scopeBadge.Text = $"Scope: {FormatScope(scope)}";
        _choiceBadge.Text = choiceCount > 0 ? $"Choice: 1 of {choiceCount}" : "Choice: none";
        _safetyBadge.Text = "Safety: close leaves text unchanged";
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        Region = new Region(CreateRoundedPath(ClientRectangle, 24));
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

    private static string FormatScope(DictationReplacementScope scope) =>
        scope switch
        {
            DictationReplacementScope.PreviousSentence => "previous sentence",
            DictationReplacementScope.PreviousParagraph => "previous paragraph",
            DictationReplacementScope.AllText => "all dictated text",
            _ => "previous word"
        };

    private static Label CreateBadge(string text, string description)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 4, 10, 4),
            BackColor = Color.FromArgb(237, 242, 255),
            ForeColor = Color.FromArgb(30, 64, 175),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = text,
            AccessibleDescription = description
        };
    }
}
