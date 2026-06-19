using System.Globalization;

namespace Callsign.UI;

public sealed class VisibleControlsOverlayForm : Form
{
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;

    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _cueLabel;
    private readonly Label _heardLabel;
    private readonly Label _hintLabel;
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
        BackColor = Color.FromArgb(16, 20, 28);
        ForeColor = Color.White;
        Opacity = 0.97;
        Width = 520;
        Height = 640;
        MinimumSize = new Size(420, 460);
        MaximumSize = new Size(760, 840);
        Padding = new Padding(14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 8
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        Controls.Add(layout);

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(190, 255, 239),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            Text = "Visible controls"
        };
        layout.Controls.Add(_titleLabel, 0, 0);

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(190, 216, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Text = "Say click plus a number to activate a listed control."
        };
        layout.Controls.Add(_subtitleLabel, 0, 1);

        _cueLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(255, 240, 165),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Text = "Voice cue: nothing heard yet."
        };
        layout.Controls.Add(_cueLabel, 0, 2);

        _heardLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(215, 248, 240),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.9f, FontStyle.Bold),
            Text = "Heard: nothing yet."
        };
        layout.Controls.Add(_heardLabel, 0, 3);

        _hintLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(180, 245, 232),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Italic),
            Text = "Say a number to activate a visible control."
        };
        layout.Controls.Add(_hintLabel, 0, 4);

        _focusLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(255, 255, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Text = "Focused: none"
        };
        layout.Controls.Add(_focusLabel, 0, 5);

        _summaryBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(24, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            WordWrap = true
        };
        layout.Controls.Add(_summaryBox, 0, 6);

        _itemsList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(24, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        layout.Controls.Add(_itemsList, 0, 7);

    }

    public bool IsReady => true;
    public string FocusText => _focusLabel.Text;
    public string CueText => _cueLabel.Text;
    public string HeardText => _heardLabel.Text;

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
        PositionOverlay(_ownerBounds);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Visible)
            PositionOverlay(_ownerBounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _summaryBox.Dispose();
            _itemsList.Dispose();
            _titleLabel.Dispose();
            _subtitleLabel.Dispose();
            _cueLabel.Dispose();
            _heardLabel.Dispose();
            _hintLabel.Dispose();
            _focusLabel.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    public void ShowOverlay(Rectangle ownerBounds, string summary, string cue, string heard, IReadOnlyList<string> numberedItems, IReadOnlyList<VisibleControlOverlayAnnotation> annotations)
    {
        if (_disposed)
            return;

        _annotations = annotations;
        _ownerBounds = ownerBounds;
        _summaryBox.Text = summary.Trim();
        _cueLabel.Text = string.IsNullOrWhiteSpace(cue) ? "Voice cue: nothing heard yet." : cue.Trim();
        _heardLabel.Text = string.IsNullOrWhiteSpace(heard) ? "Heard: nothing yet." : heard.Trim();
        _hintLabel.Text = "Say a number to activate a visible control.";
        var focusedAnnotation = _annotations.FirstOrDefault(item => item.IsFocused);
        _focusLabel.Text = focusedAnnotation is null
            ? "Focused: none"
            : $"Focused: {focusedAnnotation.Number}. {focusedAnnotation.Label}";
        _itemsList.BeginUpdate();
        try
        {
            _itemsList.Items.Clear();
            foreach (var item in numberedItems)
                _itemsList.Items.Add(item);

            var focusedNumber = _annotations.FirstOrDefault(item => item.IsFocused)?.Number;
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

        if (!Visible)
            Show();

        PositionOverlay(ownerBounds);
        TopMost = true;
        BringToFront();
        Invalidate();
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

        if (_annotations.Count == 0)
            return;

        var g = e.Graphics;
        using var badgeFill = new SolidBrush(Color.FromArgb(235, 28, 158, 255));
        using var badgeFocusedFill = new SolidBrush(Color.FromArgb(245, 0, 174, 110));
        using var badgeBorder = new Pen(Color.FromArgb(255, 220, 245, 255), 2f);
        using var badgeFocusedBorder = new Pen(Color.FromArgb(255, 255, 255, 255), 3f);
        using var badgeTextBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);

        foreach (var annotation in _annotations)
        {
            if (annotation.Bounds.IsEmpty)
                continue;

            var badgeBounds = new Rectangle(annotation.Bounds.Left - Left - 14, annotation.Bounds.Top - Top - 18, 30, 30);
            if (badgeBounds.Right < 0 || badgeBounds.Bottom < 0 || badgeBounds.Left > Width || badgeBounds.Top > Height)
                continue;

            var fill = annotation.IsFocused ? badgeFocusedFill : badgeFill;
            var border = annotation.IsFocused ? badgeFocusedBorder : badgeBorder;
            var badgeSize = annotation.IsFocused ? 34 : 30;
            var badgeOffset = annotation.IsFocused ? 2 : 0;
            var adjusted = new Rectangle(
                badgeBounds.Left - badgeOffset,
                badgeBounds.Top - badgeOffset,
                badgeSize,
                badgeSize);

            g.FillEllipse(fill, adjusted);
            g.DrawEllipse(border, adjusted);
            var text = annotation.Number.ToString(CultureInfo.InvariantCulture);
            var textSize = g.MeasureString(text, font);
            var textX = adjusted.Left + (adjusted.Width - textSize.Width) / 2f - 1f;
            var textY = adjusted.Top + (adjusted.Height - textSize.Height) / 2f - 1f;
            g.DrawString(text, font, badgeTextBrush, textX, textY);
        }
    }

    private void PositionOverlay(Rectangle ownerBounds)
    {
        Bounds = ownerBounds;
    }
}

public sealed record VisibleControlOverlayAnnotation(int Number, Rectangle Bounds, string Label, bool IsFocused);
