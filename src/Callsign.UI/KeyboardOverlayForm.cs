using System.Drawing.Drawing2D;

namespace Callsign.UI;

public sealed class KeyboardOverlayForm : Form
{
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private readonly Panel _headerPanel;
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private readonly Label _contractLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _statusModeBadge;
    private readonly Label _statusTargetBadge;
    private readonly Label _statusModifierBadge;
    private readonly Label _statusSafetyBadge;
    private readonly Label _cueLabel;
    private readonly Label _safetyLabel;
    private readonly IReadOnlyList<KeyboardOverlayKey> _keys;
    private bool _disposed;

    public KeyboardOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(248, 250, 253);
        ForeColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.94;
        DoubleBuffered = true;
        AccessibleName = "Keyboard overlay";
        AccessibleDescription = "Visible on-screen keyboard for spoken keyboard commands.";
        Width = 980;
        Height = 370;
        MinimumSize = new Size(720, 320);
        Padding = new Padding(16);

        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(242, 247, 253),
            Padding = new Padding(8, 0, 8, 0),
            AccessibleName = "Keyboard overlay header",
            AccessibleDescription = "Contains the keyboard overlay title and dismiss control."
        };
        Controls.Add(_headerPanel);

        _titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 180,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            Text = "Keyboard overlay",
            AccessibleName = "Keyboard overlay title",
            AccessibleDescription = "Names the on-screen keyboard overlay."
        };
        _headerPanel.Controls.Add(_titleLabel);

        _closeButton = new Button
        {
            Text = "\u00D7",
            Width = 28,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            BackColor = Color.FromArgb(236, 239, 246),
            ForeColor = Color.FromArgb(44, 54, 74),
            AccessibleName = "Close keyboard overlay",
            AccessibleDescription = "Hides the keyboard overlay without sending a keypress."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();
        _closeButton.Dock = DockStyle.Right;
        _headerPanel.Controls.Add(_closeButton);

        _contractLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(236, 242, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.6f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            Text = "Contract: choose key -> visible foreground app -> press or release modifiers.",
            AccessibleName = "Keyboard overlay contract",
            AccessibleDescription = "Summarizes the visible keyboard flow from key choice through foreground-app execution and modifier release."
        };
        Controls.Add(_contractLabel);

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8, 3, 8, 3),
            BackColor = Color.FromArgb(242, 247, 253),
            AccessibleName = "Keyboard overlay status strip",
            AccessibleDescription = "Shows keyboard mode, target surface, modifier state, and safety boundary in compact visible badges."
        };
        _statusModeBadge = CreateStatusBadge("Mode: keyboard", "Shows the current keyboard overlay mode.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _statusTargetBadge = CreateStatusBadge("Target: foreground app", "Shows the visible keyboard target.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _statusModifierBadge = CreateStatusBadge("Modifiers: ready", "Shows the current modifier state.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusSafetyBadge = CreateStatusBadge("Safety: visible foreground only", "Shows the visible-app safety boundary.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _statusStrip.Controls.Add(_statusModeBadge);
        _statusStrip.Controls.Add(_statusTargetBadge);
        _statusStrip.Controls.Add(_statusModifierBadge);
        _statusStrip.Controls.Add(_statusSafetyBadge);
        Controls.Add(_statusStrip);

        _safetyLabel = new Label
        {
            AutoSize = false,
            Height = 34,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(249, 251, 254),
            ForeColor = Color.FromArgb(51, 65, 85),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.1f, FontStyle.Bold),
            Text = "Safety: keys go to the visible foreground app only. Use release all modifiers to clear held Shift, Control, or Alt.",
            AccessibleName = "Keyboard overlay safety",
            AccessibleDescription = "Explains that keyboard commands target the visible foreground app only and that release all modifiers clears held Shift, Control, or Alt."
        };
        Controls.Add(_safetyLabel);
        _safetyLabel.Paint += (_, args) => PaintCueFrame(args.Graphics, _safetyLabel.ClientRectangle);

        _cueLabel = new Label
        {
            AutoSize = false,
            Height = 42,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(242, 247, 253),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10.8f, FontStyle.Bold),
            Text = "Keyboard: say press A, press F5, press Space, hold Shift, release all modifiers, or hide keyboard.",
            AccessibleName = "Keyboard overlay voice cue",
            AccessibleDescription = "Shows available spoken keyboard commands, including key presses, held modifiers, release all modifiers, and hide keyboard."
        };
        Controls.Add(_cueLabel);
        _cueLabel.Paint += (_, args) => PaintCueFrame(args.Graphics, _cueLabel.ClientRectangle);
        _keys = BuildKeys();
        UpdateStatusStrip();
    }

    public string CueText => _cueLabel.Text;
    public string OverlayAccessibleName => AccessibleName ?? string.Empty;
    public string OverlayAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusModeBadgeText => _statusModeBadge.Text;
    public string StatusTargetBadgeText => _statusTargetBadge.Text;
    public string StatusModifierBadgeText => _statusModifierBadge.Text;
    public string StatusSafetyBadgeText => _statusSafetyBadge.Text;
    public string SafetyText => _safetyLabel.Text;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("keyboard overlay");
    public IReadOnlyList<KeyboardOverlayKey> Keys => _keys;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExToolWindow | WsExNoActivate;
            return createParams;
        }
    }

    public void ShowKeyboard(Rectangle screenBounds)
    {
        Bounds = CalculateOverlayBounds(screenBounds);
        if (!Visible)
            Show();

        TopMost = true;
        BringToFront();
        PositionHeaderChrome();
        UpdateStatusStrip();
        UpdateCueRegion();
        Invalidate();
    }

    public static Rectangle CalculateOverlayBounds(Rectangle screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
            return new Rectangle(40, 40, 980, 360);

        const int margin = 32;
        var width = Math.Min(980, Math.Max(720, screenBounds.Width - (margin * 2)));
        var height = Math.Min(370, Math.Max(320, screenBounds.Height / 3));
        var x = screenBounds.Left + Math.Max(margin, (screenBounds.Width - width) / 2);
        var y = screenBounds.Bottom - height - margin;
        return new Rectangle(x, Math.Max(screenBounds.Top + margin, y), width, height);
    }

    public static IReadOnlyList<KeyboardOverlayKey> BuildKeys()
    {
        var keys = new List<KeyboardOverlayKey>();
        var rows = new[]
        {
            new[] { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" },
            new[] { "Esc", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Backspace" },
            new[] { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "Enter" },
            new[] { "Shift", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Caps" },
            new[] { "Ctrl", "Alt", "Z", "X", "C", "V", "B", "N", "M", "Space", "Menu" },
            new[] { "Home", "End", "PgUp", "PgDn", "Left", "Up", "Down", "Right" }
        };

        for (var row = 0; row < rows.Length; row++)
        {
            var column = 0;
            foreach (var label in rows[row])
            {
                var span = label switch
                {
                    "Backspace" or "Enter" or "Shift" => 2,
                    "Space" => 4,
                    _ => 1
                };
                keys.Add(new KeyboardOverlayKey(label, row, column, span));
                column += span;
            }
        }

        return keys;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PositionHeaderChrome();
        UpdateCueRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionHeaderChrome();
        UpdateCueRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_cueLabel is null || _keys is null)
            return;

        var keyboardBounds = ClientRectangle;
        keyboardBounds.Y += _headerPanel.Height + _contractLabel.Height + _statusStrip.Height + _cueLabel.Height + _safetyLabel.Height + 18;
        keyboardBounds.Height -= _headerPanel.Height + _contractLabel.Height + _statusStrip.Height + _cueLabel.Height + _safetyLabel.Height + 34;
        keyboardBounds.X += 16;
        keyboardBounds.Width -= 32;
        if (keyboardBounds.Width <= 0 || keyboardBounds.Height <= 0)
            return;

        using var keyFill = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        using var specialFill = new SolidBrush(Color.FromArgb(238, 241, 245, 249));
        using var keyBorder = new Pen(Color.FromArgb(130, 148, 163, 184), 1.2f);
        using var textBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);

        foreach (var key in _keys)
        {
            var bounds = CalculateKeyBounds(keyboardBounds, key);
            using var path = CreateRoundedPath(bounds, 9);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(IsSpecialKey(key.Label) ? specialFill : keyFill, path);
            e.Graphics.DrawPath(keyBorder, path);

            var textSize = e.Graphics.MeasureString(key.Label, font);
            e.Graphics.DrawString(
                key.Label,
                font,
                textBrush,
                bounds.Left + ((bounds.Width - textSize.Width) / 2f),
                bounds.Top + ((bounds.Height - textSize.Height) / 2f));
        }
    }

    public static Rectangle CalculateKeyBounds(Rectangle keyboardBounds, KeyboardOverlayKey key)
    {
        if (keyboardBounds.Width <= 0 || keyboardBounds.Height <= 0)
            return Rectangle.Empty;

        const int rowCount = 6;
        const int columnCount = 14;
        const int gap = 6;
        var cellWidth = Math.Max(1, (keyboardBounds.Width - (gap * (columnCount - 1))) / columnCount);
        var cellHeight = Math.Max(1, (keyboardBounds.Height - (gap * (rowCount - 1))) / rowCount);
        var x = keyboardBounds.Left + (key.Column * (cellWidth + gap));
        var y = keyboardBounds.Top + (key.Row * (cellHeight + gap));
        var width = (cellWidth * key.ColumnSpan) + (gap * (key.ColumnSpan - 1));
        return new Rectangle(x, y, width, cellHeight);
    }

    private void UpdateCueRegion()
    {
        if (_cueLabel is null)
            return;

        if (_cueLabel.Width <= 0 || _cueLabel.Height <= 0)
            return;

        _cueLabel.Region?.Dispose();
        _cueLabel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _cueLabel.Size), 16));

        if (_safetyLabel is null || _safetyLabel.Width <= 0 || _safetyLabel.Height <= 0)
            return;

        _safetyLabel.Region?.Dispose();
        _safetyLabel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _safetyLabel.Size), 14));
    }

    private void UpdateStatusStrip()
    {
        if (_statusStrip == null || _statusModeBadge == null || _statusTargetBadge == null || _statusModifierBadge == null || _statusSafetyBadge == null)
            return;

        _statusModeBadge.Text = "Mode: keyboard";
        _statusTargetBadge.Text = "Target: foreground app";
        var modifierState = BuildModifierStatusText();
        _statusModifierBadge.Text = modifierState;
        _statusModifierBadge.AccessibleDescription = modifierState;
        _statusSafetyBadge.Text = "Safety: visible foreground only";
    }

    private static string BuildModifierStatusText()
    {
        var modifiers = Control.ModifierKeys;
        var held = new List<string>(3);

        if ((modifiers & System.Windows.Forms.Keys.Shift) == System.Windows.Forms.Keys.Shift)
            held.Add("Shift");

        if ((modifiers & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control)
            held.Add("Control");

        if ((modifiers & System.Windows.Forms.Keys.Alt) == System.Windows.Forms.Keys.Alt)
            held.Add("Alt");

        return held.Count == 0
            ? "Modifiers: none held"
            : $"Modifiers: {string.Join(", ", held)} held";
    }

    private static Label CreateStatusBadge(string text, string description, Color backColor, Color foreColor)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(8, 3, 8, 3),
            BackColor = backColor,
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Text = text,
            AccessibleName = text,
            AccessibleDescription = description
        };
    }

    private void PositionHeaderChrome()
    {
        if (_headerPanel is null || _titleLabel is null || _closeButton is null || _headerPanel.Width <= 0)
            return;

        _titleLabel.Width = Math.Max(140, _headerPanel.Width - _closeButton.Width - 28);
        _closeButton.Left = _headerPanel.Width - _closeButton.Width - 8;
        _closeButton.Top = 2;
        _closeButton.BringToFront();
        _titleLabel.BringToFront();
        _headerPanel.BringToFront();
    }

    private static bool IsSpecialKey(string label) =>
        label.Length > 1 && !string.Equals(label, "Space", StringComparison.OrdinalIgnoreCase);

    private static void PaintCueFrame(Graphics graphics, Rectangle bounds)
    {
        using var borderPen = new Pen(Color.FromArgb(98, 255, 255, 255), 1.1f);
        using var shadowPen = new Pen(Color.FromArgb(44, 0, 0, 0), 2.2f);
        using var cuePath = CreateRoundedPath(bounds, 16);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawPath(shadowPen, cuePath);
        graphics.DrawPath(borderPen, cuePath);
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

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _closeButton.Dispose();
            _titleLabel.Dispose();
            _headerPanel.Dispose();
            _contractLabel.Dispose();
            _statusStrip.Dispose();
            _statusModeBadge.Dispose();
            _statusTargetBadge.Dispose();
            _statusModifierBadge.Dispose();
            _statusSafetyBadge.Dispose();
            _cueLabel.Dispose();
            _safetyLabel.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}

public sealed record KeyboardOverlayKey(string Label, int Row, int Column, int ColumnSpan = 1);
