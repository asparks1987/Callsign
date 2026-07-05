using System.Reflection;

namespace Callsign.UI;

public sealed class WakeOverlayForm : Form
{
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private static readonly Color OverlayTransparencyColor = Color.Magenta;

    private readonly Label _titleLabel;
    private readonly TableLayoutPanel _layout;
    private readonly PictureBox _pictureBox;
    private readonly Panel _messagePanel;
    private readonly Panel _headerPanel;
    private readonly Label _phaseLabel;
    private readonly Label _liveBadgeLabel;
    private readonly Label _readoutLabel;
    private readonly Label _safetyLabel;
    private readonly Label _transcriptHeadingLabel;
    private readonly Label _captionLabel;
    private readonly Label _wakeStatusLabel;
    private readonly Label _authorityLabel;
    private readonly Label _activityLabel;
    private readonly Panel _activityTrack;
    private readonly Panel _activityFill;
    private readonly Label _historyLabel;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly MemoryStream? _assetStream;
    private readonly Image? _overlayImage;
    private bool _disposed;
    private string _baseReadout = "Callsign heard. Say your callsign.";
    private string _baseHistoryText = string.Empty;
    private string _activityText = "Mic: idle";
    private Color _accentColor = Color.FromArgb(105, 245, 214);
    private Color _accentSoftColor = Color.FromArgb(168, 0, 0, 0);
    private string _accentName = "Listening";
    private int _animatedDots;
    private bool _animateReadout;
    private int _pulsePhase;
    private bool _audioLive;
    private double _activityLevel;

    public WakeOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = OverlayTransparencyColor;
        TransparencyKey = OverlayTransparencyColor;
        ForeColor = Color.White;
        Width = 430;
        Height = 628;
        MinimumSize = new Size(340, 448);
        MaximumSize = new Size(560, 748);
        Padding = new Padding(14);

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 72));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 268));
        Controls.Add(_layout);

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(190, 255, 239),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            Text = "Callsign",
            AccessibleName = "Wake overlay title",
            AccessibleDescription = "Names the visible Callsign wake overlay."
        };
        _layout.Controls.Add(_titleLabel, 0, 0);

        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        _layout.Controls.Add(_pictureBox, 0, 1);

        _messagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(16, 12, 16, 10)
        };

        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 24,
            BackColor = Color.Transparent
        };

        _phaseLabel = new Label
        {
            Dock = DockStyle.Left,
            Width = 140,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(105, 245, 214),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Text = "LISTENING",
            AccessibleName = "Wake overlay phase",
            AccessibleDescription = "Shows the current wake, identity, command, or action phase."
        };
        _headerPanel.Controls.Add(_phaseLabel);

        _liveBadgeLabel = new Label
        {
            Dock = DockStyle.Right,
            Width = 72,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(105, 245, 214),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(4, 2, 4, 2),
            Text = "READY",
            AccessibleName = "Wake overlay live badge",
            AccessibleDescription = "Shows whether Callsign is ready, listening, or hearing speech."
        };
        _headerPanel.Controls.Add(_liveBadgeLabel);
        _messagePanel.Controls.Add(_headerPanel);

        _readoutLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 44,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 11.25f, FontStyle.Bold),
            Padding = new Padding(12, 6, 12, 2),
            Text = "Callsign heard. Say your callsign.",
            AccessibleName = "Wake overlay live readout",
            AccessibleDescription = "Shows what Callsign expects next or what it heard."
        };
        _messagePanel.Controls.Add(_readoutLabel);

        _safetyLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 32,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(210, 242, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            Text = "Safety: say stop, cancel, stop listening, or reset session. Commands stay blocked until identity is confirmed.",
            AccessibleName = "Wake overlay safety",
            AccessibleDescription = "Shows visible stop, cancel, stop listening, and reset session escape phrases, and explains that commands remain blocked until identity is confirmed."
        };
        _messagePanel.Controls.Add(_safetyLabel);

        _transcriptHeadingLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 18,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(166, 255, 246),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.0f, FontStyle.Bold),
            Padding = new Padding(4, 0, 4, 0),
            Text = "LIVE TRANSCRIPT",
            Visible = false,
            AccessibleName = "Wake overlay transcript heading",
            AccessibleDescription = "Labels the live transcript area."
        };
        _messagePanel.Controls.Add(_transcriptHeadingLabel);

        _captionLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 52,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(232, 255, 248),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 9.4f, FontStyle.Bold),
            Padding = new Padding(6, 2, 6, 2),
            Text = string.Empty,
            Visible = false,
            AccessibleName = "Wake overlay transcript",
            AccessibleDescription = "Shows the most recent spoken transcript or dictation readout."
        };
        _messagePanel.Controls.Add(_captionLabel);

        _wakeStatusLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 18,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(255, 211, 121),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(4, 0, 4, 0),
            Text = string.Empty,
            Visible = false,
            AccessibleName = "Wake overlay wake status",
            AccessibleDescription = "Shows wake detector confidence or retry status."
        };
        _messagePanel.Controls.Add(_wakeStatusLabel);

        _authorityLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 18,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(170, 230, 250),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            Padding = new Padding(4, 0, 4, 0),
            Text = string.Empty,
            Visible = false,
            AccessibleName = "Wake overlay runtime authority",
            AccessibleDescription = "Shows which Callsign runtime owns the active microphone listener."
        };
        _messagePanel.Controls.Add(_authorityLabel);

        _activityLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(190, 235, 249),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(2, 0, 2, 0),
            Text = "Mic: idle",
            AccessibleName = "Wake overlay microphone activity",
            AccessibleDescription = "Shows microphone activity and speech detection status."
        };
        _messagePanel.Controls.Add(_activityLabel);

        _activityTrack = new Panel
        {
            Dock = DockStyle.Top,
            Height = 12,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 3, 0, 4)
        };
        _activityTrack.Resize += (_, _) => UpdateActivityMeter();
        _activityFill = new Panel
        {
            Dock = DockStyle.Left,
            Width = 0,
            BackColor = Color.FromArgb(105, 245, 214)
        };
        _activityTrack.Controls.Add(_activityFill);
        _messagePanel.Controls.Add(_activityTrack);

        _historyLabel = new Label
        {
            Dock = DockStyle.Bottom,
            AutoSize = false,
            Height = 44,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(190, 235, 249),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            Padding = new Padding(8, 0, 8, 2),
            Text = string.Empty,
            Visible = false,
            AccessibleName = "Wake overlay transcript history",
            AccessibleDescription = "Shows recent transcript history during the active voice session."
        };
        _messagePanel.Controls.Add(_historyLabel);
        _layout.Controls.Add(_messagePanel, 0, 2);
        ApplyMessageRegion();

        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = 350
        };
        _animationTimer.Tick += (_, _) => AdvanceAnimationFrame();

        (_assetStream, _overlayImage) = LoadOverlayImage();
        if (_overlayImage != null)
        {
            _pictureBox.Image = _overlayImage;
            if (ImageAnimator.CanAnimate(_overlayImage))
                ImageAnimator.Animate(_overlayImage, OnFrameChanged);
        }
    }

    public bool IsReady => _overlayImage != null;
    public string PhaseText => _phaseLabel.Text;
    public string ReadoutText => _readoutLabel.Text;
    public string SafetyText => _safetyLabel.Text;
    public string TranscriptHeadingText => _transcriptHeadingLabel.Text;
    public string CaptionText => _captionLabel.Text;
    public string WakeStatusText => _wakeStatusLabel.Text;
    public string AuthorityText => _authorityLabel.Text;
    public string HistoryText => _historyLabel.Text;
    public string AccentName => _accentName;
    public string LiveBadgeText => _liveBadgeLabel.Text;
    public string ActivityText => _activityLabel.Text;
    public double ActivityLevel => _activityLevel;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("wake overlay");
    public string TitleAccessibleName => _titleLabel.AccessibleName ?? string.Empty;
    public string PhaseAccessibleName => _phaseLabel.AccessibleName ?? string.Empty;
    public string ReadoutAccessibleName => _readoutLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string TranscriptAccessibleName => _captionLabel.AccessibleName ?? string.Empty;
    public string ActivityAccessibleName => _activityLabel.AccessibleName ?? string.Empty;
    public string AuthorityAccessibleDescription => _authorityLabel.AccessibleDescription ?? string.Empty;
    public bool IsTopMostOverlay => TopMost;
    public bool IsNonActivatingOverlay => ShowWithoutActivation;
    public bool UsesNoActivateClickThroughStyles
    {
        get
        {
            var flags = CreateParams.ExStyle;
            return (flags & WsExNoActivate) == WsExNoActivate
                && (flags & WsExTransparent) == WsExTransparent
                && (flags & WsExToolWindow) == WsExToolWindow;
        }
    }

    public string WindowBehaviorSummary =>
        UsesNoActivateClickThroughStyles && IsTopMostOverlay && IsNonActivatingOverlay
            ? "Topmost no-activate click-through tool-window overlay."
            : "Overlay window behavior incomplete.";

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
        if (_messagePanel is null)
            return;
        ApplyMessageRegion();
        TryPositionOverlay();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_messagePanel is null)
            return;
        ApplyMessageRegion();
        if (Visible)
            TryPositionOverlay();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_messagePanel.Width <= 0 || _messagePanel.Height <= 0)
            return;

        using var fillBrush = new SolidBrush(Color.FromArgb(182, 18, 24, 36));
        using var shadowPen = new Pen(Color.FromArgb(72, 0, 0, 0), 3f);
        using var borderPen = new Pen(Color.FromArgb(105, 255, 255, 255), 1.4f);
        using var framePath = CreateRoundedPath(_messagePanel.Bounds, 22);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillPath(fillBrush, framePath);
        e.Graphics.DrawPath(shadowPen, framePath);
        e.Graphics.DrawPath(borderPen, framePath);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_overlayImage != null && ImageAnimator.CanAnimate(_overlayImage))
                ImageAnimator.StopAnimate(_overlayImage, OnFrameChanged);

            _animationTimer.Stop();
            _animationTimer.Dispose();
            _pictureBox.Image = null;
            _overlayImage?.Dispose();
            _assetStream?.Dispose();
            _titleLabel.Dispose();
            _pictureBox.Dispose();
            _messagePanel.Dispose();
            _headerPanel.Dispose();
            _phaseLabel.Dispose();
            _liveBadgeLabel.Dispose();
            _readoutLabel.Dispose();
            _safetyLabel.Dispose();
            _transcriptHeadingLabel.Dispose();
            _captionLabel.Dispose();
            _wakeStatusLabel.Dispose();
            _authorityLabel.Dispose();
            _activityLabel.Dispose();
            _activityTrack.Dispose();
            _activityFill.Dispose();
            _historyLabel.Dispose();
            _layout.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    public void ShowOverlay(string? readout = null, string? phase = null, IReadOnlyList<string>? transcriptHistory = null, double? activityLevel = null, string? activityText = null, bool speechActive = false, string? captionText = null, string? wakeStatusText = null, string? authorityText = null)
    {
        if (_disposed || !IsReady)
            return;

        SetReadout(readout, phase);
        SetCaptionText(captionText);
        SetWakeStatusText(wakeStatusText);
        SetAuthorityText(authorityText);
        SetTranscriptHistory(transcriptHistory);
        SetAudioActivity(activityLevel, activityText, speechActive);

        try
        {
            TryShowOverlay();
            TryPositionOverlay();
            TopMost = true;
            TryBringToFront();
            if (_overlayImage != null && ImageAnimator.CanAnimate(_overlayImage))
                ImageAnimator.UpdateFrames(_overlayImage);
            TryInvalidatePictureBox();
        }
        catch
        {
        }
    }

    public void SetReadout(string? readout, string? phase = null)
    {
        if (_disposed)
            return;

        if (!string.IsNullOrWhiteSpace(phase))
            ApplyPhaseStyle(phase);

        if (string.IsNullOrWhiteSpace(readout))
            return;

        _baseReadout = TrimReadout(readout);
        _animateReadout = _baseReadout.StartsWith("Hearing ", StringComparison.OrdinalIgnoreCase);
        _animatedDots = 0;
        _readoutLabel.Text = _animateReadout ? AnimateReadoutText() : _baseReadout;
        UpdateLiveBadge();

        if (_animateReadout)
            _animationTimer.Start();
        else
            _animationTimer.Stop();
    }

    public void SetTranscriptHistory(IReadOnlyList<string>? transcriptHistory)
    {
        if (_disposed)
            return;

        var history = transcriptHistory?
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Select(entry => entry.Trim())
            .Take(3)
            .ToArray() ?? [];

        if (history.Length == 0)
        {
            _baseHistoryText = string.Empty;
            _historyLabel.Visible = false;
            _historyLabel.Text = string.Empty;
            return;
        }

        _baseHistoryText = "Recent speech" + Environment.NewLine + string.Join(Environment.NewLine, history);
        _historyLabel.Visible = true;
        _historyLabel.Text = _baseHistoryText;
    }

    public void SetCaptionText(string? captionText)
    {
        if (_disposed)
            return;

        if (string.IsNullOrWhiteSpace(captionText))
        {
            _transcriptHeadingLabel.Visible = false;
            _captionLabel.Visible = false;
            _captionLabel.Text = string.Empty;
            return;
        }

        _transcriptHeadingLabel.Visible = true;
        _transcriptHeadingLabel.Text = _audioLive ? "LIVE TRANSCRIPT" : "LAST HEARD";
        _captionLabel.Text = TrimReadout(captionText);
        _captionLabel.Visible = true;
    }

    public void SetWakeStatusText(string? wakeStatusText)
    {
        if (_disposed)
            return;

        if (string.IsNullOrWhiteSpace(wakeStatusText))
        {
            _wakeStatusLabel.Visible = false;
            _wakeStatusLabel.Text = string.Empty;
            return;
        }

        _wakeStatusLabel.Visible = true;
        _wakeStatusLabel.Text = TrimReadout(wakeStatusText);
    }

    public void SetAuthorityText(string? authorityText)
    {
        if (_disposed)
            return;

        if (string.IsNullOrWhiteSpace(authorityText))
        {
            _authorityLabel.Visible = false;
            _authorityLabel.Text = string.Empty;
            return;
        }

        var text = TrimReadout(authorityText);
        _authorityLabel.Visible = true;
        _authorityLabel.Text = text.StartsWith("Authority:", StringComparison.OrdinalIgnoreCase)
            ? text
            : $"Authority: {text}";
    }

    public void SetAudioActivity(double? activityLevel, string? activityText = null, bool speechActive = false)
    {
        if (_disposed)
            return;

        _activityLevel = activityLevel.HasValue ? Math.Clamp(activityLevel.Value, 0d, 1d) : 0d;
        _audioLive = speechActive || _activityLevel > 0.001;
        if (!string.IsNullOrWhiteSpace(activityText))
        {
            _activityText = activityText.Trim();
        }
        else if (activityLevel.HasValue)
            _activityText = _activityLevel > 0.05 ? "Mic: active" : "Mic: idle";
        else
            _activityText = "Mic: idle";

        _activityLabel.Text = _activityText;
        UpdateActivityMeter();
        UpdateLiveBadge();
    }

    public void HideOverlay()
    {
        if (_disposed)
            return;

        if (Visible)
            Hide();

        _captionLabel.Visible = false;
        _captionLabel.Text = string.Empty;
        _transcriptHeadingLabel.Visible = false;
        _transcriptHeadingLabel.Text = "LIVE TRANSCRIPT";
        _authorityLabel.Visible = false;
        _authorityLabel.Text = string.Empty;
    }

    private static string TrimReadout(string value)
    {
        var text = value.Trim();
        return text.Length <= 140 ? text : text[..137] + "...";
    }

    private static (MemoryStream? Stream, Image? Image) LoadOverlayImage()
    {
        var stream = OpenOverlayAssetStream();
        if (stream == null)
            return (null, null);

        MemoryStream? copy = null;
        try
        {
            using var source = stream;
            copy = new MemoryStream();
            source.CopyTo(copy);
            copy.Position = 0;
            var image = Image.FromStream(copy);
            return (copy, image);
        }
        catch
        {
            copy?.Dispose();
            stream.Dispose();
            return (null, null);
        }
    }

    private static Stream? OpenOverlayAssetStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = new[]
        {
            "callsign.gif",
            "Callsign.UI.callsign.gif"
        };

        foreach (var resourceName in resourceNames)
        {
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream != null)
                return resourceStream;
        }

        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "callsign.gif"),
            Path.Combine(AppContext.BaseDirectory, "..", "callsign.gif"),
            Path.Combine(Environment.CurrentDirectory, "callsign.gif")
        };

        foreach (var candidatePath in candidatePaths)
        {
            if (File.Exists(candidatePath))
                return File.OpenRead(candidatePath);
        }

        return null;
    }

    private void PositionOverlay()
    {
        var activeScreen = Screen.FromPoint(Cursor.Position);
        var workingArea = activeScreen?.WorkingArea ?? Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        var margin = 24;
        var x = Math.Max(workingArea.Left + margin, workingArea.Left + (workingArea.Width - Width) / 2);
        var y = workingArea.Top + margin;
        Bounds = new Rectangle(x, y, Width, Height);
    }

    private void TryPositionOverlay()
    {
        try
        {
            PositionOverlay();
        }
        catch
        {
            Bounds = new Rectangle(24, 24, Width, Height);
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

    private void TryInvalidatePictureBox()
    {
        try
        {
            _pictureBox.Invalidate();
        }
        catch
        {
        }
    }

    private void ApplyMessageRegion()
    {
        if (_messagePanel is null || _messagePanel.Width <= 0 || _messagePanel.Height <= 0)
            return;

        _messagePanel.Region?.Dispose();
        _messagePanel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _messagePanel.Size), 22));
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
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

    private void OnFrameChanged(object? sender, EventArgs e)
    {
        if (_disposed || IsDisposed)
            return;

        if (IsHandleCreated)
            BeginInvoke(new Action(() => _pictureBox.Invalidate()));
    }

    private void AdvanceAnimationFrame()
    {
        if (_disposed || IsDisposed || !_animateReadout)
            return;

        _animatedDots = (_animatedDots + 1) % 4;
        _pulsePhase = (_pulsePhase + 1) % 12;
        _readoutLabel.Text = AnimateReadoutText();
        _messagePanel.Invalidate();
    }

    private string AnimateReadoutText()
    {
        if (!_animateReadout)
            return _baseReadout;

        var baseText = _baseReadout.TrimEnd('.');
        return baseText + new string('.', _animatedDots + 1);
    }

    private void ApplyPhaseStyle(string phase)
    {
        var normalized = phase.Trim();
        _phaseLabel.Text = normalized.ToUpperInvariant();

        var (accentColor, accentSoftColor, accentName) = normalized.ToLowerInvariant() switch
        {
            "identity" => (Color.FromArgb(133, 255, 194), Color.FromArgb(24, 42, 24), "Identity"),
            "command" => (Color.FromArgb(105, 245, 214), Color.FromArgb(20, 36, 38), "Command"),
            "launching" => (Color.FromArgb(255, 196, 105), Color.FromArgb(42, 28, 10), "Launching"),
            "ready" => (Color.FromArgb(102, 166, 255), Color.FromArgb(16, 24, 42), "Ready"),
            _ => (Color.FromArgb(105, 245, 214), Color.FromArgb(20, 28, 33), "Listening")
        };

        _accentColor = accentColor;
        _accentSoftColor = accentSoftColor;
        _accentName = accentName;
        _phaseLabel.ForeColor = accentColor;
        _liveBadgeLabel.ForeColor = accentColor;
        _liveBadgeLabel.BackColor = Color.FromArgb(
            Math.Min(255, accentSoftColor.R + 6),
            Math.Min(255, accentSoftColor.G + 6),
            Math.Min(255, accentSoftColor.B + 6));
        _readoutLabel.ForeColor = Color.White;
        _historyLabel.ForeColor = Color.FromArgb(
            Math.Min(255, accentColor.R + 70),
            Math.Min(255, accentColor.G + 20),
            Math.Min(255, accentColor.B + 20));
        _activityLabel.ForeColor = Color.FromArgb(
            Math.Min(255, accentColor.R + 70),
            Math.Min(255, accentColor.G + 20),
            Math.Min(255, accentColor.B + 20));
        _activityTrack.BackColor = Color.FromArgb(
            Math.Min(255, accentSoftColor.R + 30),
            Math.Min(255, accentSoftColor.G + 30),
            Math.Min(255, accentSoftColor.B + 30));
        UpdateLiveBadge();
        _messagePanel.Invalidate();
    }

    private void UpdateLiveBadge()
    {
        _liveBadgeLabel.Text = _animateReadout || _audioLive ? "LIVE" : "READY";
    }

    private void UpdateActivityMeter()
    {
        if (_activityTrack.IsDisposed || _activityFill.IsDisposed)
            return;

        _activityFill.BackColor = _accentColor;
        _activityFill.Visible = _activityLevel > 0.001;
        _activityFill.Height = Math.Max(0, _activityTrack.ClientSize.Height - 2);
        _activityFill.Width = Math.Max(0, (int)Math.Round(Math.Max(0, _activityTrack.ClientSize.Width - 2) * _activityLevel));
        _activityTrack.Invalidate();
    }
}
