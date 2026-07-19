using System.Drawing.Drawing2D;
using System.Globalization;

namespace Callsign.UI;

public readonly record struct MouseGridDisplayRegion(string Identifier, Rectangle Bounds);

public sealed class MouseGridOverlayForm : Form
{
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private readonly Panel _headerPanel;
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private readonly Label _contractLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _statusStopBadge;
    private readonly Label _statusScopeBadge;
    private readonly Label _statusFocusBadge;
    private readonly Label _statusMarkedBadge;
    private readonly Label _statusSafetyBadge;
    private readonly Label _cueLabel;
    private readonly Label _safetyLabel;
    private readonly Stack<MouseGridState> _history = new();
    private Rectangle _gridBounds;
    private int? _focusedCellNumber;
    private string? _focusedDisplayIdentifier;
    private IReadOnlyList<MouseGridDisplayRegion> _displayRegions = Array.Empty<MouseGridDisplayRegion>();
    private MouseGridState _rootState;
    private Point? _markedPoint;
    private bool _disposed;

    public MouseGridOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(248, 250, 253);
        ForeColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.86;
        DoubleBuffered = true;
        AccessibleName = "Mouse grid overlay";
        AccessibleDescription = "Visible numbered grid for spoken mouse targeting, clicking, and dragging.";

        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(242, 247, 253),
            Padding = new Padding(8, 0, 8, 0),
            AccessibleName = "Mouse grid header",
            AccessibleDescription = "Contains the mouse grid title and dismiss control."
        };
        Controls.Add(_headerPanel);

        _titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 160,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            Text = "Mouse grid",
            AccessibleName = "Mouse grid title",
            AccessibleDescription = "Names the mouse grid overlay."
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
            AccessibleName = "Close mouse grid overlay",
            AccessibleDescription = "Hides the mouse grid overlay without clicking or dragging."
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => Hide();
        _headerPanel.Controls.Add(_closeButton);
        _closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

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
            Text = "Contract: choose grid -> refine target -> click or drag visibly.",
            AccessibleName = "Mouse grid contract",
            AccessibleDescription = "Summarizes the visible mouse-grid flow from coarse targeting through refinement and visible pointer action."
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
            AccessibleName = "Mouse grid status strip",
            AccessibleDescription = "Shows the current stop state, grid scope, focus, marked drag state, and safety boundary in compact badges."
        };
        _statusStopBadge = CreateStatusBadge("STOP", "Shows that stop, cancel, and hide grid remain visible while targeting the mouse grid.", Color.FromArgb(255, 238, 235), Color.FromArgb(153, 27, 27));
        _statusScopeBadge = CreateStatusBadge("Scope: current view", "Shows what surface the grid is numbering.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _statusFocusBadge = CreateStatusBadge("Focus: root", "Shows the currently focused grid state.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _statusMarkedBadge = CreateStatusBadge("Mark: none", "Shows whether a drag start has been marked.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusSafetyBadge = CreateStatusBadge("Safety: visible pointer only", "Shows the visible-pointer safety boundary.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _statusStrip.Controls.Add(_statusStopBadge);
        _statusStrip.Controls.Add(_statusScopeBadge);
        _statusStrip.Controls.Add(_statusFocusBadge);
        _statusStrip.Controls.Add(_statusMarkedBadge);
        _statusStrip.Controls.Add(_statusSafetyBadge);
        Controls.Add(_statusStrip);

        _safetyLabel = new Label
        {
            AutoSize = false,
            Height = 32,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(249, 251, 254),
            ForeColor = Color.FromArgb(51, 65, 85),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.9f, FontStyle.Bold),
            Text = "Safety: grid actions are visible pointer actions only. Refine or undo before click or drag; hide grid or cancel exits without acting.",
            AccessibleName = "Mouse grid safety",
            AccessibleDescription = "Explains that mouse grid commands are visible pointer actions only, can be refined or undone before click or drag, and that hide grid or cancel exits without acting."
        };
        Controls.Add(_safetyLabel);
        _safetyLabel.Paint += (_, args) => PaintCueFrame(args.Graphics, _safetyLabel.ClientRectangle);

        _cueLabel = new Label
        {
            AutoSize = false,
            Height = 40,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(242, 247, 253),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 11.25f, FontStyle.Bold),
            Text = "Mouse grid: say grid 1-9, click grid 1-9, drag grid 1 to grid 9, or hide grid.",
            AccessibleName = "Mouse grid voice cue",
            AccessibleDescription = "Shows the current mouse grid targeting command options."
        };
        Controls.Add(_cueLabel);
        _cueLabel.Paint += (_, args) => PaintCueFrame(args.Graphics, _cueLabel.ClientRectangle);
    }

    public Rectangle GridBounds => _gridBounds;
    public int? FocusedCellNumber => _focusedCellNumber;
    public string? FocusedDisplayIdentifier => _focusedDisplayIdentifier;
    public IReadOnlyList<MouseGridDisplayRegion> DisplayRegions => _displayRegions;
    public bool CanUndo => _history.Count > 0;
    public Point? MarkedPoint => _markedPoint;
    public string CueText => _cueLabel.Text;
    public string OverlayAccessibleName => AccessibleName ?? string.Empty;
    public string OverlayAccessibleDescription => AccessibleDescription ?? string.Empty;
    public string CueAccessibleName => _cueLabel.AccessibleName ?? string.Empty;
    public string CueAccessibleDescription => _cueLabel.AccessibleDescription ?? string.Empty;
    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string StatusStripAccessibleDescription => _statusStrip.AccessibleDescription ?? string.Empty;
    public string StatusStripTexts => string.Join(" ", _statusStrip.Controls.OfType<Control>().Select(control => control.Text));
    public string StatusStopBadgeText => _statusStopBadge.Text;
    public string StatusScopeBadgeText => _statusScopeBadge.Text;
    public string StatusFocusBadgeText => _statusFocusBadge.Text;
    public string StatusMarkedBadgeText => _statusMarkedBadge.Text;
    public string StatusSafetyBadgeText => _statusSafetyBadge.Text;
    public string SafetyText => _safetyLabel.Text;
    public string SafetyAccessibleName => _safetyLabel.AccessibleName ?? string.Empty;
    public string SafetyAccessibleDescription => _safetyLabel.AccessibleDescription ?? string.Empty;
    public string CloseButtonAccessibleName => _closeButton.AccessibleName ?? string.Empty;
    public string CloseButtonText => _closeButton.Text;
    public string VisualStyleName => CallsignVisualStyle.DescribeSurface("mouse grid");

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

    public void ShowGrid(Rectangle bounds, IReadOnlyList<MouseGridDisplayRegion>? displayRegions = null)
    {
        _markedPoint = null;
        _history.Clear();
        _rootState = new MouseGridState(
            bounds,
            FocusedCellNumber: null,
            FocusedDisplayIdentifier: null,
            displayRegions?.Count > 0 ? displayRegions.ToArray() : Array.Empty<MouseGridDisplayRegion>());
        ApplyState(_rootState);
        Bounds = bounds;
        if (!Visible)
            Show();

        TopMost = true;
        BringToFront();
        PositionHeaderChrome();
        UpdateCueRegion();
        Invalidate();
    }

    public Rectangle FocusDisplay(string identifier)
    {
        if (!TryNormalizeDisplayIdentifier(identifier, out var normalizedIdentifier))
            return Rectangle.Empty;

        var displayBounds = ResolveDisplayBounds(_displayRegions, normalizedIdentifier);
        if (displayBounds.IsEmpty)
            return Rectangle.Empty;

        _history.Push(CaptureState());
        ApplyState(new MouseGridState(displayBounds, null, normalizedIdentifier, _displayRegions.ToArray()));
        Bounds = displayBounds;
        UpdateStatusStrip();
        return displayBounds;
    }

    public Rectangle RefineToCell(int cellNumber)
    {
        var cell = CalculateCellBounds(_gridBounds, cellNumber);
        if (!cell.IsEmpty)
        {
            _history.Push(CaptureState());
            ApplyState(new MouseGridState(cell, cellNumber, _focusedDisplayIdentifier, _displayRegions.ToArray()));
            Bounds = cell;
            UpdateStatusStrip();
        }

        return cell;
    }

    public bool Undo()
    {
        if (_history.Count == 0)
            return false;

        ApplyState(_history.Pop());
        Bounds = _gridBounds;
        UpdateStatusStrip();
        return true;
    }

    public void ResetToRoot()
    {
        _history.Clear();
        ApplyState(_rootState);
        Bounds = _gridBounds;
        UpdateStatusStrip();
    }

    public void SetMarkedPoint(Point markedPoint)
    {
        _markedPoint = markedPoint;
        UpdateCueFromState();
        UpdateStatusStrip();
        Invalidate();
    }

    public void ClearMarkedPoint()
    {
        if (!_markedPoint.HasValue)
            return;

        _markedPoint = null;
        UpdateCueFromState();
        UpdateStatusStrip();
        Invalidate();
    }

    public static IReadOnlyList<MouseGridDisplayRegion> CreateDisplayRegions(Rectangle virtualBounds, IReadOnlyList<Rectangle> displayBounds)
    {
        if (displayBounds.Count == 0)
            return Array.Empty<MouseGridDisplayRegion>();

        var orderedBounds = displayBounds
            .Where(bounds => !bounds.IsEmpty)
            .OrderBy(bounds => bounds.Top)
            .ThenBy(bounds => bounds.Left)
            .ToArray();

        var regions = new List<MouseGridDisplayRegion>(orderedBounds.Length);
        for (var index = 0; index < orderedBounds.Length; index++)
        {
            var identifier = ((char)('A' + index)).ToString(CultureInfo.InvariantCulture);
            var bounded = Rectangle.Intersect(virtualBounds, orderedBounds[index]);
            if (!bounded.IsEmpty)
                regions.Add(new MouseGridDisplayRegion(identifier, bounded));
        }

        return regions;
    }

    public static Rectangle CalculateGridPathBounds(Rectangle bounds, string pathDigits)
    {
        if (bounds.IsEmpty || string.IsNullOrWhiteSpace(pathDigits))
            return Rectangle.Empty;

        var currentBounds = bounds;
        foreach (var character in pathDigits)
        {
            if (character is < '1' or > '9')
                return Rectangle.Empty;

            currentBounds = CalculateCellBounds(currentBounds, character - '0');
            if (currentBounds.IsEmpty)
                return Rectangle.Empty;
        }

        return currentBounds;
    }

    public static Rectangle ResolveDisplayBounds(IReadOnlyList<MouseGridDisplayRegion> displayRegions, string identifier)
    {
        if (!TryNormalizeDisplayIdentifier(identifier, out var normalizedIdentifier))
            return Rectangle.Empty;

        foreach (var displayRegion in displayRegions)
        {
            if (string.Equals(displayRegion.Identifier, normalizedIdentifier, StringComparison.OrdinalIgnoreCase))
                return displayRegion.Bounds;
        }

        return Rectangle.Empty;
    }

    public static bool TryNormalizeDisplayIdentifier(string identifier, out string normalizedIdentifier)
    {
        normalizedIdentifier = string.Empty;
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        var normalized = identifier.Trim().ToLowerInvariant();
        if (normalized.Length == 1 && normalized[0] is >= 'a' and <= 'z')
        {
            normalizedIdentifier = normalized.ToUpperInvariant();
            return true;
        }

        normalizedIdentifier = normalized switch
        {
            "alpha" => "A",
            "bravo" => "B",
            "charlie" => "C",
            "delta" => "D",
            "echo" => "E",
            "foxtrot" => "F",
            "golf" => "G",
            "hotel" => "H",
            "india" => "I",
            "juliett" or "juliet" => "J",
            "kilo" => "K",
            "lima" => "L",
            "mike" => "M",
            "november" => "N",
            "oscar" => "O",
            "papa" => "P",
            "quebec" => "Q",
            "romeo" => "R",
            "sierra" => "S",
            "tango" => "T",
            "uniform" => "U",
            "victor" => "V",
            "whiskey" => "W",
            "xray" or "x-ray" => "X",
            "yankee" => "Y",
            "zulu" => "Z",
            _ => string.Empty
        };

        return normalizedIdentifier.Length == 1;
    }

    public static Point CalculateCellCenter(Rectangle bounds, int cellNumber)
    {
        var cell = CalculateCellBounds(bounds, cellNumber);
        return cell.IsEmpty
            ? Point.Empty
            : new Point(cell.Left + (cell.Width / 2), cell.Top + (cell.Height / 2));
    }

    public static Rectangle CalculateCellBounds(Rectangle bounds, int cellNumber)
    {
        if (cellNumber is < 1 or > 9 || bounds.Width <= 0 || bounds.Height <= 0)
            return Rectangle.Empty;

        var index = cellNumber - 1;
        var column = index % 3;
        var row = index / 3;
        var baseWidth = bounds.Width / 3;
        var baseHeight = bounds.Height / 3;
        var x = bounds.Left + (column * baseWidth);
        var y = bounds.Top + (row * baseHeight);
        var width = column == 2 ? bounds.Right - x : baseWidth;
        var height = row == 2 ? bounds.Bottom - y : baseHeight;
        return new Rectangle(x, y, Math.Max(1, width), Math.Max(1, height));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var clientBounds = ClientRectangle;
        var topChromeHeight = _headerPanel.Height + _contractLabel.Height + _statusStrip.Height + _cueLabel.Height + _safetyLabel.Height;
        clientBounds.Y += topChromeHeight;
        clientBounds.Height -= topChromeHeight;
        if (clientBounds.Width <= 0 || clientBounds.Height <= 0)
            return;

        using var linePen = new Pen(Color.FromArgb(165, 90, 105, 128), 1.6f);
        using var accentPen = new Pen(Color.FromArgb(180, 37, 99, 235), 2.2f);
        using var numberFill = new SolidBrush(Color.FromArgb(248, 255, 255, 255));
        using var numberRing = new Pen(Color.FromArgb(132, 15, 23, 42), 1.2f);
        using var numberText = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var numberFont = new Font("Segoe UI Semibold", 20.5f, FontStyle.Bold);
        using var displayFont = new Font("Segoe UI Semibold", 26f, FontStyle.Bold);
        using var subtleFill = new SolidBrush(Color.FromArgb(96, 252, 253, 255));
        using var focusedFill = new SolidBrush(Color.FromArgb(40, 37, 99, 235));
        using var focusedRing = new Pen(Color.FromArgb(220, 59, 130, 246), 3f);
        using var displayFill = new SolidBrush(Color.FromArgb(116, 15, 23, 42));
        using var displayAccent = new Pen(Color.FromArgb(118, 37, 99, 235), 2f);
        using var markRing = new Pen(Color.FromArgb(224, 220, 38, 38), 3f);
        using var markCenterFill = new SolidBrush(Color.FromArgb(232, 220, 38, 38));
        using var markHalo = new SolidBrush(Color.FromArgb(80, 248, 113, 113));

        e.Graphics.FillRectangle(subtleFill, clientBounds);
        e.Graphics.DrawRectangle(accentPen, clientBounds.Left + 2, clientBounds.Top + 2, clientBounds.Width - 4, clientBounds.Height - 4);
        if (_focusedCellNumber.HasValue)
        {
            e.Graphics.FillRectangle(focusedFill, clientBounds);
            e.Graphics.DrawRectangle(focusedRing, clientBounds.Left + 2, clientBounds.Top + 2, clientBounds.Width - 4, clientBounds.Height - 4);
        }

        for (var i = 1; i <= 2; i++)
        {
            var x = clientBounds.Left + (clientBounds.Width * i / 3);
            var y = clientBounds.Top + (clientBounds.Height * i / 3);
            e.Graphics.DrawLine(linePen, x, clientBounds.Top, x, clientBounds.Bottom);
            e.Graphics.DrawLine(linePen, clientBounds.Left, y, clientBounds.Right, y);
        }

        if (_displayRegions.Count > 1 && !_focusedCellNumber.HasValue)
        {
            foreach (var displayRegion in _displayRegions)
            {
                var localBounds = new Rectangle(
                    displayRegion.Bounds.Left - Bounds.Left,
                    displayRegion.Bounds.Top - Bounds.Top + topChromeHeight,
                    displayRegion.Bounds.Width,
                    displayRegion.Bounds.Height);
                if (localBounds.Width <= 0 || localBounds.Height <= 0)
                    continue;

                e.Graphics.DrawRectangle(displayAccent, localBounds);
                var identifierSize = e.Graphics.MeasureString(displayRegion.Identifier, displayFont);
                var identifierX = localBounds.Left + ((localBounds.Width - identifierSize.Width) / 2f);
                var identifierY = localBounds.Top + ((localBounds.Height - identifierSize.Height) / 2f);
                e.Graphics.DrawString(displayRegion.Identifier, displayFont, displayFill, identifierX, identifierY);
            }
        }

        for (var cell = 1; cell <= 9; cell++)
        {
            var cellBounds = CalculateCellBounds(clientBounds, cell);
            var badge = new Rectangle(cellBounds.Left + (cellBounds.Width / 2) - 20, cellBounds.Top + (cellBounds.Height / 2) - 20, 40, 40);
            e.Graphics.FillEllipse(numberFill, badge);
            e.Graphics.DrawEllipse(numberRing, badge);
            var text = cell.ToString();
            var textSize = e.Graphics.MeasureString(text, numberFont);
            e.Graphics.DrawString(text, numberFont, numberText, badge.Left + ((badge.Width - textSize.Width) / 2), badge.Top + ((badge.Height - textSize.Height) / 2));
        }

        if (_markedPoint.HasValue)
        {
            var markedLocalPoint = new Point(
                _markedPoint.Value.X - Bounds.Left,
                _markedPoint.Value.Y - Bounds.Top + topChromeHeight);
            if (clientBounds.Contains(markedLocalPoint))
            {
                var haloBounds = new Rectangle(markedLocalPoint.X - 18, markedLocalPoint.Y - 18, 36, 36);
                var ringBounds = new Rectangle(markedLocalPoint.X - 12, markedLocalPoint.Y - 12, 24, 24);
                e.Graphics.FillEllipse(markHalo, haloBounds);
                e.Graphics.DrawEllipse(markRing, ringBounds);
                e.Graphics.FillEllipse(markCenterFill, markedLocalPoint.X - 4, markedLocalPoint.Y - 4, 8, 8);
                e.Graphics.DrawLine(markRing, markedLocalPoint.X - 18, markedLocalPoint.Y, markedLocalPoint.X + 18, markedLocalPoint.Y);
                e.Graphics.DrawLine(markRing, markedLocalPoint.X, markedLocalPoint.Y - 18, markedLocalPoint.X, markedLocalPoint.Y + 18);
            }
        }
    }

    private void UpdateCueRegion()
    {
        if (_cueLabel.Width <= 0 || _cueLabel.Height <= 0)
            return;

        _cueLabel.Region?.Dispose();
        _cueLabel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _cueLabel.Size), 16));

        if (_safetyLabel.Width <= 0 || _safetyLabel.Height <= 0)
            return;

        _safetyLabel.Region?.Dispose();
        _safetyLabel.Region = new Region(CreateRoundedPath(new Rectangle(Point.Empty, _safetyLabel.Size), 14));
    }

    private void PositionHeaderChrome()
    {
        if (_headerPanel is null || _titleLabel is null || _closeButton is null || _headerPanel.Width <= 0 || _closeButton.Width <= 0)
            return;

        _titleLabel.Width = Math.Max(120, _headerPanel.Width - _closeButton.Width - 28);
        _closeButton.Left = _headerPanel.Width - _closeButton.Width - 8;
        _closeButton.Top = 2;
        _closeButton.BringToFront();
        _titleLabel.BringToFront();
        _headerPanel.BringToFront();
    }

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

    private MouseGridState CaptureState() =>
        new(_gridBounds, _focusedCellNumber, _focusedDisplayIdentifier, _displayRegions.ToArray());

    private void ApplyState(MouseGridState state)
    {
        _gridBounds = state.Bounds;
        _focusedCellNumber = state.FocusedCellNumber;
        _focusedDisplayIdentifier = state.FocusedDisplayIdentifier;
        _displayRegions = state.DisplayRegions;
        UpdateCueFromState();
        UpdateStatusStrip();
        UpdateCueRegion();
        Invalidate();
    }

    private void UpdateCueFromState()
    {
        var markedSuffix = _markedPoint.HasValue
            ? " A drag start is marked. Say drag to drop the marked item, undo, or hide grid."
            : " Say mark or mark 4 to set a drag start, drag grid 1 to grid 9 for direct cell drags, drag to drop a marked item, undo, or hide grid.";

        if (_focusedCellNumber.HasValue)
        {
            _cueLabel.Text = $"Mouse grid refined to {_focusedCellNumber.Value}: focus moved to cell {_focusedCellNumber.Value}. Say grid 1-9, click grid 1-9,{markedSuffix}";
            _cueLabel.AccessibleDescription = $"Mouse grid focus is refined to cell {_focusedCellNumber.Value}; available spoken options are grid 1 through 9, click grid 1 through 9, mark, drag grid 1 to grid 9, drag, undo, or hide grid.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(_focusedDisplayIdentifier))
        {
            _cueLabel.Text = $"Mouse grid focused on display {_focusedDisplayIdentifier}: say grid 1-9, click grid 1-9,{markedSuffix}";
            _cueLabel.AccessibleDescription = $"Mouse grid focus is on display {_focusedDisplayIdentifier}; available spoken options are grid 1 through 9, click grid 1 through 9, mark, drag grid 1 to grid 9, drag, undo, or hide grid.";
            return;
        }

        _cueLabel.Text = _displayRegions.Count > 1
            ? $"Mouse grid: say A or Alpha to choose a display, mouse grid A 114 for a shortcut, grid 1-9 to refine, click grid 1-9,{markedSuffix}"
            : $"Mouse grid: say grid 1-9, click grid 1-9,{markedSuffix}";
        _cueLabel.AccessibleDescription = _markedPoint.HasValue
            ? "Shows the current mouse grid targeting command options with a marked drag start."
            : "Shows the current mouse grid targeting command options before a cell is refined.";
    }

    private void UpdateStatusStrip()
    {
        if (_statusStrip == null || _statusScopeBadge == null || _statusFocusBadge == null || _statusMarkedBadge == null || _statusSafetyBadge == null)
            return;

        _statusStopBadge.Text = "STOP";
        _statusStopBadge.ForeColor = Color.FromArgb(153, 27, 27);
        _statusScopeBadge.Text = string.IsNullOrWhiteSpace(_focusedDisplayIdentifier)
            ? "Scope: current view"
            : $"Scope: display {_focusedDisplayIdentifier}";
        _statusFocusBadge.Text = _focusedCellNumber.HasValue
            ? $"Focus: cell {_focusedCellNumber.Value}"
            : string.IsNullOrWhiteSpace(_focusedDisplayIdentifier)
                ? "Focus: root"
                : $"Focus: display {_focusedDisplayIdentifier}";
        _statusMarkedBadge.Text = _markedPoint.HasValue
            ? $"Mark: set at {_markedPoint.Value.X}, {_markedPoint.Value.Y}"
            : "Mark: none";
        _statusSafetyBadge.Text = "Safety: visible pointer only";
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

    private readonly record struct MouseGridState(
        Rectangle Bounds,
        int? FocusedCellNumber,
        string? FocusedDisplayIdentifier,
        IReadOnlyList<MouseGridDisplayRegion> DisplayRegions);

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
            _statusStopBadge.Dispose();
            _statusScopeBadge.Dispose();
            _statusFocusBadge.Dispose();
            _statusMarkedBadge.Dispose();
            _statusSafetyBadge.Dispose();
            _cueLabel.Dispose();
            _safetyLabel.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}
