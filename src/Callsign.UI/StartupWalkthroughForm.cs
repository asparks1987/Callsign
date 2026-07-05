using System.Drawing.Drawing2D;

namespace Callsign.UI;

public sealed class StartupWalkthroughForm : Form
{
    private readonly Panel _surface;
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private readonly Label _subtitleLabel;
    private readonly Label _safetyLabel;
    private readonly ListBox _stepsList;
    private readonly Label _statusLabel;
    private readonly Button _accountButton;
    private readonly Button _voiceButton;
    private readonly Button _sessionButton;
    private readonly Button _shortcutsButton;
    private readonly Button _packsButton;
    private readonly Button _continueButton;
    private readonly Button _remindLaterButton;
    private readonly Action<string> _navigateToTab;
    private bool _disposed;

    public StartupWalkthroughForm(Action<string> navigateToTab)
    {
        _navigateToTab = navigateToTab ?? throw new ArgumentNullException(nameof(navigateToTab));

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
        AccessibleDescription = "Accessible clean-install walkthrough for account setup, voice enrollment, wake verification, visible launch, local voice shortcuts, and extension packs.";

        _surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(18),
            AccessibleName = "Callsign startup walkthrough",
            AccessibleDescription = "Guides a clean install through account setup, voice enrollment, wake verification, visible app launch, local voice shortcuts, and extension packs."
        };
        Controls.Add(_surface);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = Padding.Empty
        };
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

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 48,
            Text = "A clean install begins with a short visible flow: create a profile, enroll voice, wake with Callsign, verify identity, then launch an installed app.",
            AccessibleName = "Startup walkthrough summary",
            AccessibleDescription = "Summarizes the wake, identity verification, and visible action workflow.",
            Font = new Font("Segoe UI", 9.6f, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105)
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

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 24,
            Text = "Use the buttons below to jump to setup surfaces, including Shortcuts and Packs.",
            AccessibleName = "Startup walkthrough status",
            AccessibleDescription = "Shows which setup surface was opened from the walkthrough.",
            Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175)
        };

        _stepsList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AccessibleName = "Startup walkthrough steps",
            AccessibleDescription = "Lists the clean-install steps for profile setup, voice enrollment, wake verification, visible launch, local voice shortcuts, and extension packs."
        };
        _stepsList.Items.Add("1. Create or pick a callsign profile.");
        _stepsList.Items.Add("2. Record at least three voice samples.");
        _stepsList.Items.Add("3. Say Callsign, then your callsign.");
        _stepsList.Items.Add("4. Confirm the visible wake overlay and live readout.");
        _stepsList.Items.Add("5. Launch an installed app through Start search.");
        _stepsList.Items.Add("6. Open Shortcuts to save local voice shortcut phrases.");
        _stepsList.Items.Add("7. Open Packs to import or review extension packs.");

        var navigationRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = Padding.Empty,
            Margin = new Padding(0, 8, 0, 0),
            AutoSize = true
        };

        _accountButton = CreateNavButton("Open Account", () => _navigateToTab("Account"));
        _voiceButton = CreateNavButton("Open Voice", () => _navigateToTab("Voice"));
        _sessionButton = CreateNavButton("Open Session", () => _navigateToTab("Session"));
        _shortcutsButton = CreateNavButton("Open Shortcuts", () => _navigateToTab("Shortcuts"));
        _packsButton = CreateNavButton("Open Packs", () => _navigateToTab("Packs"));
        navigationRow.Controls.Add(_accountButton);
        navigationRow.Controls.Add(_voiceButton);
        navigationRow.Controls.Add(_sessionButton);
        navigationRow.Controls.Add(_shortcutsButton);
        navigationRow.Controls.Add(_packsButton);

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
        layout.Controls.Add(_subtitleLabel, 0, 1);
        layout.Controls.Add(_safetyLabel, 0, 2);
        layout.Controls.Add(_statusLabel, 0, 3);
        layout.Controls.Add(_stepsList, 0, 4);
        layout.Controls.Add(navigationRow, 0, 5);
        layout.Controls.Add(footerRow, 0, 6);
        _surface.Controls.Add(layout);

        ApplyRoundedRegion();
    }

    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("clean-install walkthrough");
    public string FormAccessibleName => AccessibleName ?? string.Empty;
    public string FormAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string SurfaceAccessibleName => _surface.AccessibleName ?? string.Empty;
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string SummaryAccessibleName => _subtitleLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string StatusAccessibleName => _statusLabel.AccessibleName ?? string.Empty;
    public string StepsAccessibleName => _stepsList.AccessibleName ?? string.Empty;
    public string StepsAccessibleDescription => _stepsList.AccessibleDescription ?? string.Empty;
    public string ContinueAccessibleName => _continueButton.AccessibleName ?? string.Empty;
    public string RemindLaterAccessibleName => _remindLaterButton.AccessibleName ?? string.Empty;
    public string AccountButtonAccessibleName => _accountButton.AccessibleName ?? string.Empty;
    public string VoiceButtonAccessibleName => _voiceButton.AccessibleName ?? string.Empty;
    public string SessionButtonAccessibleName => _sessionButton.AccessibleName ?? string.Empty;
    public string ShortcutsButtonAccessibleName => _shortcutsButton.AccessibleName ?? string.Empty;
    public string PacksButtonAccessibleName => _packsButton.AccessibleName ?? string.Empty;
    public string TitleText => _titleLabel.Text;
    public string SummaryText => _subtitleLabel.Text;
    public string SafetyText => _safetyLabel.Text;
    public string StatusText => _statusLabel.Text;
    public string StepsText => string.Join(Environment.NewLine, _stepsList.Items.Cast<object>().Select(item => item?.ToString() ?? string.Empty));
    public string AccountButtonText => _accountButton.Text;
    public string VoiceButtonText => _voiceButton.Text;
    public string SessionButtonText => _sessionButton.Text;
    public string ShortcutsButtonText => _shortcutsButton.Text;
    public string PacksButtonText => _packsButton.Text;
    public string ContinueButtonText => _continueButton.Text;
    public string RemindLaterButtonText => _remindLaterButton.Text;

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
            _packsButton.Dispose();
            _stepsList.Dispose();
            _statusLabel.Dispose();
            _safetyLabel.Dispose();
            _subtitleLabel.Dispose();
            _titleLabel.Dispose();
            _surface.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private Button CreateNavButton(string text, Action action)
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
}
