using System.Globalization;
using System.Drawing.Drawing2D;

namespace Callsign.UI;

public sealed class VisibleControlsOverlayForm : Form
{
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private static readonly Color OverlayTransparencyColor = Color.Magenta;

    private readonly Panel _hudPanel;
    private readonly Button _closeButton;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _contractLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _statusStopBadge;
    private readonly Label _statusTargetsBadge;
    private readonly Label _statusScopeBadge;
    private readonly Label _statusFocusBadge;
    private readonly Label _statusCueBadge;
    private readonly Label _statusSafetyBadge;
    private readonly Label _cueLabel;
    private readonly Label _heardLabel;
    private readonly Label _hintLabel;
    private readonly Label _safetyLabel;
    private readonly Label _focusLabel;
    private readonly TextBox _summaryBox;
    private readonly ListBox _itemsList;
    private IReadOnlyList<VisibleControlOverlayAnnotation> _annotations = [];
    private Rectangle _ownerBounds;
    private bool _disposed;

    public VisibleControlsOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = OverlayTransparencyColor;
        TransparencyKey = OverlayTransparencyColor;
        ForeColor = Color.White;
        Opacity = 0.96;
        DoubleBuffered = true;
        Width = 760;
        Height = 640;
        MinimumSize = new Size(320, 240);
        Padding = Padding.Empty;
        AccessibleName = "Visible controls overlay";
        AccessibleDescription = "Visible numbered control overlay for spoken click, double-click, triple-click, and right-click commands.";

        _hudPanel = new Panel
        {
            BackColor = Color.FromArgb(232, 248, 250, 253),
            Size = new Size(430, 360),
            Padding = new Padding(16),
            AccessibleName = "Visible controls overlay HUD",
            AccessibleDescription = "Compact numbered-control status surface."
        };
        Controls.Add(_hudPanel);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 12
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        _hudPanel.Controls.Add(layout);

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            Text = "Visible controls",
            AccessibleName = "Visible controls overlay title",
            AccessibleDescription = "Names the compact numbered-control overlay used for visible spoken targeting."
        };

        _closeButton = new Button
        {
            Text = "\u00D7",
            Width = 34,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(236, 239, 246),
            ForeColor = Color.FromArgb(44, 54, 74),
            AccessibleName = "Close visible controls overlay",
            AccessibleDescription = "Hides the visible controls overlay without activating any control."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();

        var headerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.Controls.Add(_titleLabel, 0, 0);
        headerRow.Controls.Add(_closeButton, 1, 0);
        layout.Controls.Add(headerRow, 0, 0);

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Text = "Say click, double click, triple click, or right click plus a number.",
            AccessibleName = "Visible controls overlay subtitle",
            AccessibleDescription = "Explains the available spoken click, double-click, triple-click, and right-click commands for visible numbers."
        };
        layout.Controls.Add(_subtitleLabel, 0, 1);

        _contractLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(236, 242, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            AutoEllipsis = true,
            Text = "Contract: visible numbers -> choose target -> click, double click, triple click, or right click.",
            AccessibleName = "Visible controls contract",
            AccessibleDescription = "Summarizes the visible-controls flow from numbered targets through the voice action that activates them."
        };
        layout.Controls.Add(_contractLabel, 0, 2);

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 2),
            Padding = Padding.Empty,
            AccessibleName = "Visible controls status strip",
            AccessibleDescription = "Shows the current stop state, target count, scope, focus, voice cue, and safety state in compact visible badges."
        };
        _statusStopBadge = CreateStatusBadge("STOP", "Shows that stop, cancel, and reset remain visible while targeting controls.", Color.FromArgb(255, 238, 235), Color.FromArgb(153, 27, 27));
        _statusTargetsBadge = CreateStatusBadge("Targets: none", "Shows how many visible controls are numbered.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _statusScopeBadge = CreateStatusBadge("Scope: current view", "Shows what visible surface is being numbered.", Color.FromArgb(240, 249, 255), Color.FromArgb(7, 89, 133));
        _statusFocusBadge = CreateStatusBadge("Focus: none", "Shows the currently focused target.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _statusCueBadge = CreateStatusBadge("Cue: waiting", "Shows the spoken targeting cue.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusSafetyBadge = CreateStatusBadge("Safety: visible", "Shows the visible-target safety boundary.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _statusStrip.Controls.Add(_statusStopBadge);
        _statusStrip.Controls.Add(_statusTargetsBadge);
        _statusStrip.Controls.Add(_statusScopeBadge);
        _statusStrip.Controls.Add(_statusFocusBadge);
        _statusStrip.Controls.Add(_statusCueBadge);
        _statusStrip.Controls.Add(_statusSafetyBadge);
        layout.Controls.Add(_statusStrip, 0, 3);

        _cueLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(37, 99, 235),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Text = "Voice cue: nothing heard yet.",
            AccessibleName = "Visible controls voice cue",
            AccessibleDescription = "Shows the current spoken targeting cue for the numbered-control overlay."
        };
        layout.Controls.Add(_cueLabel, 0, 4);

        _heardLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(20, 27, 44),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.9f, FontStyle.Bold),
            Text = "Heard: nothing yet.",
            AccessibleName = "Visible controls heard transcript",
            AccessibleDescription = "Shows what Callsign heard while the visible-control overlay is active."
        };
        layout.Controls.Add(_heardLabel, 0, 5);

        _hintLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Italic),
            Text = "Say click, double click, triple click, or right click plus a visible number.",
            AccessibleName = "Visible controls target summary",
            AccessibleDescription = "Summarizes how many targets are numbered and how to activate them by voice."
        };
        layout.Controls.Add(_hintLabel, 0, 6);

        _safetyLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(244, 247, 252),
            ForeColor = Color.FromArgb(51, 65, 85),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
            Text = "Safety: numbers act only on visible targets. Hide or cancel exits without clicking; use mouse grid when a target is not numbered.",
            AccessibleName = "Visible controls safety",
            AccessibleDescription = "Explains that numbered-control commands act only on visible targets, that hide or cancel exits without clicking, and that mouse grid is the fallback when a target is not numbered."
        };
        layout.Controls.Add(_safetyLabel, 0, 7);

        _focusLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Text = "Focused: none",
            AccessibleName = "Visible controls focused target",
            AccessibleDescription = "Identifies the currently focused numbered control target, if one is available."
        };
        layout.Controls.Add(_focusLabel, 0, 8);

        _summaryBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            WordWrap = true,
            AccessibleName = "Visible controls summary",
            AccessibleDescription = "Lists the foreground app or Callsign controls that can be targeted by visible number or label."
        };
        layout.Controls.Add(_summaryBox, 0, 9);

        _itemsList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            AccessibleName = "Visible controls numbered targets",
            AccessibleDescription = "Contains the numbered targets available for click, double-click, triple-click, or right-click voice commands."
        };
        layout.Controls.Add(_itemsList, 0, 10);

        ApplyHudRegion();
    }

    public bool IsReady => true;
    public string FocusText => _focusLabel.Text;
    public string SubtitleText => _subtitleLabel.Text;
    public string ContractText => _contractLabel.Text;
    public string CueText => _cueLabel.Text;
    public string HeardText => _heardLabel.Text;
    public string TargetSummaryText => _hintLabel.Text;
    public string SafetyText => _safetyLabel.Text;
    public string ItemsText => string.Join(Environment.NewLine, _itemsList.Items.Cast<object>().Select(item => item?.ToString() ?? string.Empty));
    public Rectangle HudBounds => _hudPanel.Bounds;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("compact overlay");
    public string OverlayAccessibleName => AccessibleName ?? string.Empty;
    public string OverlayAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string HudAccessibleName => _hudPanel.AccessibleName ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusStopBadgeText => _statusStopBadge.Text;
    public string StatusTargetsBadgeText => _statusTargetsBadge.Text;
    public string StatusScopeBadgeText => _statusScopeBadge.Text;
    public string StatusFocusBadgeText => _statusFocusBadge.Text;
    public string StatusCueBadgeText => _statusCueBadge.Text;
    public string StatusSafetyBadgeText => _statusSafetyBadge.Text;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string HeardAccessibleName => _heardLabel.AccessibleName ?? string.Empty;
    public string HeardAccessibleDescription => _heardLabel.AccessibleDescription ?? string.Empty;
    public string FocusAccessibleName => _focusLabel.AccessibleName ?? string.Empty;
    public string FocusAccessibleDescription => _focusLabel.AccessibleDescription ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string TargetsAccessibleName => _itemsList.AccessibleName ?? string.Empty;
    public string TargetsAccessibleDescription => _itemsList.AccessibleDescription ?? string.Empty;
    public string SummaryAccessibleDescription => _summaryBox.AccessibleDescription ?? string.Empty;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExToolWindow | WsExTransparent | WsExNoActivate;
            return createParams;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_hudPanel is null)
            return;
        ApplyHudRegion();
        TryPositionOverlay(_ownerBounds);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_hudPanel is null)
            return;
        ApplyHudRegion();
        if (Visible)
            TryPositionOverlay(_ownerBounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _closeButton.Dispose();
            _summaryBox.Dispose();
            _itemsList.Dispose();
            _titleLabel.Dispose();
            _subtitleLabel.Dispose();
            _contractLabel.Dispose();
            _statusStrip.Dispose();
            _statusStopBadge.Dispose();
            _statusTargetsBadge.Dispose();
            _statusScopeBadge.Dispose();
            _statusFocusBadge.Dispose();
            _statusCueBadge.Dispose();
            _statusSafetyBadge.Dispose();
            _cueLabel.Dispose();
            _heardLabel.Dispose();
            _hintLabel.Dispose();
            _safetyLabel.Dispose();
            _focusLabel.Dispose();
            _hudPanel.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    public void ShowOverlay(Rectangle ownerBounds, string summary, string cue, string heard, IReadOnlyList<string> numberedItems, IReadOnlyList<VisibleControlOverlayAnnotation> annotations, string? scopeText = null)
    {
        if (_disposed)
            return;

        _annotations = annotations;
        _ownerBounds = ownerBounds;
        _summaryBox.Text = summary.Trim();
        var scopeLabel = string.IsNullOrWhiteSpace(scopeText) ? "Scope: current view" : scopeText.Trim();
        var focusedAnnotation = _annotations.FirstOrDefault(item => item.IsFocused);
        _cueLabel.Text = BuildCueText(cue, focusedAnnotation, numberedItems.Count, scopeLabel);
        _heardLabel.Text = string.IsNullOrWhiteSpace(heard) ? "Heard: nothing yet." : heard.Trim();
        _focusLabel.Text = focusedAnnotation is null
            ? "Focused: none"
            : $"Focused: {focusedAnnotation.Number}. {focusedAnnotation.Label}";
        _subtitleLabel.Text = BuildSubtitleText(focusedAnnotation, numberedItems.Count);
        _hintLabel.Text = FormatTargetSummary(numberedItems.Count, _annotations.Count);
        UpdateStatusStrip(numberedItems.Count, scopeLabel, focusedAnnotation, _cueLabel.Text);
        var focusedNumber = focusedAnnotation?.Number;
        _itemsList.BeginUpdate();
        try
        {
            _itemsList.Items.Clear();
            for (var index = 0; index < numberedItems.Count; index++)
            {
                var item = numberedItems[index];
                var display = focusedNumber.HasValue && index + 1 == focusedNumber.Value
                    ? $"{item} (focused)"
                    : item;
                _itemsList.Items.Add(display);
            }

            var focusedIndex = focusedNumber.HasValue ? focusedNumber.Value - 1 : -1;
            if (focusedIndex >= 0 && focusedIndex < _itemsList.Items.Count)
                _itemsList.SelectedIndex = focusedIndex;
            else
                _itemsList.ClearSelected();
        }
        finally
        {
            _itemsList.EndUpdate();
        }

        try
        {
            TryShowOverlay();
            TryPositionOverlay(ownerBounds);
            TopMost = true;
            TryBringToFront();
            Invalidate();
        }
        catch
        {
        }
    }

    public void HideOverlay()
    {
        if (_disposed)
            return;

        if (Visible)
            Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_hudPanel.Width > 0 && _hudPanel.Height > 0)
        {
            using var borderPen = new Pen(Color.FromArgb(106, 255, 255, 255), 1.3f);
            using var softPen = new Pen(Color.FromArgb(46, 0, 0, 0), 2.8f);
            using var panelPath = CreateRoundedPath(_hudPanel.Bounds, 24);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(softPen, panelPath);
            e.Graphics.DrawPath(borderPen, panelPath);
        }

        if (_annotations.Count == 0)
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var controlRing = new Pen(Color.FromArgb(150, 37, 99, 235), 1.8f);
        using var focusRing = new Pen(Color.FromArgb(220, 59, 130, 246), 2.8f);
        using var badgeFill = new SolidBrush(Color.FromArgb(246, 255, 255, 255));
        using var badgeFocusedFill = new SolidBrush(Color.FromArgb(240, 219, 234, 254));
        using var badgeBorder = new Pen(Color.FromArgb(140, 148, 163, 184), 1.4f);
        using var badgeFocusedBorder = new Pen(Color.FromArgb(220, 59, 130, 246), 2.4f);
        using var badgeTextBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);

        foreach (var annotation in _annotations)
        {
            if (annotation.Bounds.IsEmpty)
                continue;

            var localTargetBounds = new Rectangle(
                annotation.Bounds.Left - Left,
                annotation.Bounds.Top - Top,
                annotation.Bounds.Width,
                annotation.Bounds.Height);
            if (localTargetBounds.Width > 0 && localTargetBounds.Height > 0)
                g.DrawRectangle(annotation.IsFocused ? focusRing : controlRing, localTargetBounds);

            var badgeBounds = CalculateBadgeBounds(annotation.Bounds, Bounds, annotation.IsFocused);
            if (badgeBounds.Right < 0 || badgeBounds.Bottom < 0 || badgeBounds.Left > Width || badgeBounds.Top > Height)
                continue;

            var fill = annotation.IsFocused ? badgeFocusedFill : badgeFill;
            var border = annotation.IsFocused ? badgeFocusedBorder : badgeBorder;
            g.FillEllipse(fill, badgeBounds);
            g.DrawEllipse(border, badgeBounds);
            var text = annotation.Number.ToString(CultureInfo.InvariantCulture);
            var textSize = g.MeasureString(text, font);
            var textX = badgeBounds.Left + (badgeBounds.Width - textSize.Width) / 2f - 1f;
            var textY = badgeBounds.Top + (badgeBounds.Height - textSize.Height) / 2f - 1f;
            g.DrawString(text, font, badgeTextBrush, textX, textY);
        }
    }

    private void PositionOverlay(Rectangle ownerBounds)
    {
        Bounds = ownerBounds;
        _hudPanel.Bounds = CalculateHudBounds(ClientRectangle, _hudPanel.Size);
    }

    private void TryPositionOverlay(Rectangle ownerBounds)
    {
        try
        {
            PositionOverlay(ownerBounds);
        }
        catch
        {
            Bounds = ownerBounds.IsEmpty ? new Rectangle(18, 18, Width, Height) : ownerBounds;
            _hudPanel.Bounds = CalculateHudBounds(ClientRectangle, _hudPanel.Size);
        }
    }

    private void TryShowOverlay()
    {
        if (Visible)
            return;

        try
        {
            Show();
        }
        catch
        {
        }
    }

    private void TryBringToFront()
    {
        try
        {
            BringToFront();
        }
        catch
        {
        }
    }

    private void ApplyHudRegion()
    {
        if (_hudPanel is null || _hudPanel.Width <= 0 || _hudPanel.Height <= 0)
            return;

        _hudPanel.Region?.Dispose();
        _hudPanel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _hudPanel.Size), 24));
    }

    private void UpdateStatusStrip(int targetCount, string scopeText, VisibleControlOverlayAnnotation? focusedAnnotation, string cueText)
    {
        if (_statusStrip == null || _statusTargetsBadge == null || _statusScopeBadge == null || _statusFocusBadge == null || _statusCueBadge == null || _statusSafetyBadge == null)
            return;

        _statusStopBadge.Text = "STOP";
        _statusStopBadge.ForeColor = Color.FromArgb(153, 27, 27);
        _statusTargetsBadge.Text = targetCount == 0
            ? "Targets: none"
            : targetCount == 1
                ? "Targets: 1 visible"
                : $"Targets: {targetCount} visible";
        _statusScopeBadge.Text = string.IsNullOrWhiteSpace(scopeText)
            ? "Scope: current view"
            : scopeText.StartsWith("Scope:", StringComparison.OrdinalIgnoreCase)
                ? scopeText
                : $"Scope: {scopeText}";
        _statusFocusBadge.Text = focusedAnnotation is null
            ? "Focus: none"
            : $"Focus: {focusedAnnotation.Number}. {focusedAnnotation.Label}";
        _statusCueBadge.Text = string.IsNullOrWhiteSpace(cueText)
            ? "Cue: waiting"
            : cueText.StartsWith("Voice cue:", StringComparison.OrdinalIgnoreCase)
                ? cueText.Replace("Voice cue:", "Cue:", StringComparison.OrdinalIgnoreCase)
                : $"Cue: {cueText}";
        _statusSafetyBadge.Text = "Safety: visible targets only";
    }

    private static string BuildCueText(string cue, VisibleControlOverlayAnnotation? focusedAnnotation, int targetCount, string scopeText)
    {
        var baseCue = string.IsNullOrWhiteSpace(cue) ? "Voice cue: nothing heard yet." : cue.Trim();
        var targetSummary = targetCount == 0
            ? "No visible targets yet."
            : targetCount == 1
                ? "1 visible target."
                : $"{targetCount} visible targets.";

        var focusSummary = focusedAnnotation is null
            ? "No focus selected."
            : $"Focused on {focusedAnnotation.Number}. {focusedAnnotation.Label}.";

        var scopeSummary = string.IsNullOrWhiteSpace(scopeText)
            ? "Scope: current view."
            : scopeText.StartsWith("Scope:", StringComparison.OrdinalIgnoreCase)
                ? $"{scopeText}."
                : $"Scope: {scopeText}.";

        return $"{baseCue} {targetSummary} {focusSummary} {scopeSummary} Say click, double click, triple click, or right click plus a visible number.";
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

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);

        var topLeft = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
        var topRight = new Rectangle(bounds.Right - diameter, bounds.Top, diameter, diameter);
        var bottomRight = new Rectangle(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter);
        var bottomLeft = new Rectangle(bounds.Left, bounds.Bottom - diameter, diameter, diameter);

        path.StartFigure();
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

    public static Rectangle CalculateBadgeBounds(Rectangle targetBounds, Rectangle overlayBounds, bool focused)
    {
        if (targetBounds.IsEmpty || overlayBounds.Width <= 0 || overlayBounds.Height <= 0)
            return Rectangle.Empty;

        var size = focused ? 36 : 32;
        var x = targetBounds.Left - overlayBounds.Left - (size / 2);
        var y = targetBounds.Top - overlayBounds.Top - (size / 2);
        x = Math.Clamp(x, 4, Math.Max(4, overlayBounds.Width - size - 4));
        y = Math.Clamp(y, 4, Math.Max(4, overlayBounds.Height - size - 4));
        return new Rectangle(x, y, size, size);
    }

    public static Rectangle CalculateHudBounds(Rectangle clientBounds, Size desiredSize)
    {
        if (clientBounds.Width <= 0 || clientBounds.Height <= 0)
            return Rectangle.Empty;

        const int margin = 18;
        var width = Math.Min(Math.Max(320, desiredSize.Width), Math.Max(320, clientBounds.Width - (margin * 2)));
        var height = Math.Min(Math.Max(240, desiredSize.Height), Math.Max(240, clientBounds.Height - (margin * 2)));
        var x = clientBounds.Right - width - margin;
        var y = clientBounds.Top + margin;

        if (clientBounds.Width < width + (margin * 2))
            x = clientBounds.Left + margin;
        if (clientBounds.Height < height + (margin * 2))
            y = clientBounds.Top + margin;

        return new Rectangle(Math.Max(clientBounds.Left + margin, x), Math.Max(clientBounds.Top + margin, y), width, height);
    }

    public static string FormatTargetSummary(int listedControlCount, int annotatedControlCount)
    {
        var count = Math.Max(listedControlCount, annotatedControlCount);
        return count switch
        {
            0 => "No visible controls numbered yet.",
            1 => "1 control numbered. Say click, double click, triple click, or right click one.",
            _ => $"{count.ToString(CultureInfo.InvariantCulture)} controls numbered. Say click, double click, triple click, or right click plus a number."
        };
    }

    private static string BuildSubtitleText(VisibleControlOverlayAnnotation? focusedAnnotation, int numberedItemCount)
    {
        if (numberedItemCount <= 0)
            return "Say click, double click, triple click, or right click plus a number.";

        if (focusedAnnotation is null)
            return "Say click, double click, triple click, or right click plus a number or label.";

        return $"Say click {focusedAnnotation.Number}, double click {focusedAnnotation.Number}, triple click {focusedAnnotation.Number}, or right click {focusedAnnotation.Label}.";
    }
}

public sealed record VisibleControlOverlayAnnotation(int Number, Rectangle Bounds, string Label, bool IsFocused);
