using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace Callsign.UI.Services;

public sealed record DesktopVisibleControlSnapshot(
    string WindowTitle,
    nint WindowHandle,
    IReadOnlyList<DesktopVisibleControlEntry> Controls,
    string? Warning = null);

public sealed record DesktopVisibleControlEntry(
    int Number,
    string Label,
    Rectangle Bounds,
    string ControlType,
    string AutomationId,
    bool IsKeyboardFocusable,
    bool IsEnabled,
    bool IsActionable,
    AutomationElement Element);

public sealed class DesktopVisibleControlService
{
    private const int MaxControls = 40;

    public bool TryGetForegroundWindowBounds(out Rectangle bounds, out string windowTitle, out string warning)
    {
        bounds = Rectangle.Empty;
        windowTitle = "Current window";
        warning = string.Empty;

        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == nint.Zero)
            {
                warning = "No foreground window was available.";
                return false;
            }

            if (!GetWindowRect(hwnd, out var rect))
            {
                warning = "The foreground window bounds were unavailable.";
                return false;
            }

            bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                warning = "The foreground window did not have visible bounds.";
                bounds = Rectangle.Empty;
                return false;
            }

            windowTitle = TryGetWindowTitle(hwnd);
            return true;
        }
        catch (ElementNotAvailableException ex)
        {
            warning = $"Foreground window changed before grid targeting completed: {ex.Message}";
            bounds = Rectangle.Empty;
            return false;
        }
        catch (COMException ex)
        {
            warning = $"Unable to resolve foreground window bounds: {ex.Message}";
            bounds = Rectangle.Empty;
            return false;
        }
        catch (InvalidOperationException ex)
        {
            warning = $"Unable to resolve foreground window bounds: {ex.Message}";
            bounds = Rectangle.Empty;
            return false;
        }
    }

    public bool TryCaptureForegroundWindow(int ignoredProcessId, out DesktopVisibleControlSnapshot snapshot)
    {
        snapshot = new DesktopVisibleControlSnapshot("Foreground window", nint.Zero, []);

        var hwnd = GetForegroundWindow();
        if (hwnd == nint.Zero)
        {
            snapshot = snapshot with { Warning = "No foreground window was available." };
            return false;
        }

        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == ignoredProcessId)
        {
            snapshot = snapshot with { WindowHandle = hwnd, Warning = "Foreground window belongs to Callsign." };
            return false;
        }

        return TryCaptureWindow(hwnd, "Foreground window", "foreground window", out snapshot);
    }

    public bool TryCaptureTaskbar(out DesktopVisibleControlSnapshot snapshot)
    {
        snapshot = new DesktopVisibleControlSnapshot("Taskbar", nint.Zero, []);
        var hwnd = FindWindow("Shell_TrayWnd", null);
        if (hwnd == nint.Zero)
        {
            snapshot = snapshot with { Warning = "The Windows taskbar window was not available." };
            return false;
        }

        return TryCaptureWindow(hwnd, "Taskbar", "taskbar", out snapshot);
    }

    public bool TryCaptureNamedWindow(int ignoredProcessId, string requestedWindow, out DesktopVisibleControlSnapshot snapshot)
    {
        snapshot = new DesktopVisibleControlSnapshot("Named window", nint.Zero, []);
        if (string.IsNullOrWhiteSpace(requestedWindow))
        {
            snapshot = snapshot with { Warning = "No app or window name was provided." };
            return false;
        }

        var normalizedRequest = Normalize(requestedWindow);
        var bestCandidate = default(WindowCandidate?);
        var bestScore = 0;

        EnumWindows((hwnd, lParam) =>
        {
            if (hwnd == nint.Zero || !IsWindowVisible(hwnd) || IsIconic(hwnd))
                return true;

            if (!GetWindowRect(hwnd, out var rect))
                return true;

            var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return true;

            _ = GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ignoredProcessId)
                return true;

            var title = ReadWindowText(hwnd);
            var processName = TryGetProcessName(processId);
            var score = ScoreWindowCandidate(normalizedRequest, title, processName);
            if (score <= 0)
                return true;

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = new WindowCandidate(hwnd, title, processName);
            }

            return true;
        }, IntPtr.Zero);

        if (bestCandidate is null)
        {
            snapshot = snapshot with { Warning = $"No visible app or window matched '{requestedWindow}'." };
            return false;
        }

        var fallbackTitle = string.IsNullOrWhiteSpace(bestCandidate.Value.Title)
            ? bestCandidate.Value.ProcessName
            : bestCandidate.Value.Title;
        return TryCaptureWindow(bestCandidate.Value.Handle, fallbackTitle, "named window", out snapshot);
    }

    public bool TryActivate(DesktopVisibleControlEntry entry, out string message)
    {
        message = string.Empty;
        try
        {
            if (!entry.IsEnabled)
            {
                message = $"Visible control {entry.Number} is no longer enabled.";
                return false;
            }

            if (entry.Element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                ((InvokePattern)invokePattern).Invoke();
                message = $"Activated '{entry.Label}' from the foreground window.";
                return true;
            }

            if (entry.Element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPattern))
            {
                ((SelectionItemPattern)selectionItemPattern).Select();
                message = $"Selected '{entry.Label}' from the foreground window.";
                return true;
            }

            if (entry.Element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
            {
                ((TogglePattern)togglePattern).Toggle();
                message = $"Toggled '{entry.Label}' from the foreground window.";
                return true;
            }

            if (entry.Element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePattern))
            {
                var pattern = (ExpandCollapsePattern)expandCollapsePattern;
                if (pattern.Current.ExpandCollapseState == ExpandCollapseState.Expanded)
                    pattern.Collapse();
                else
                    pattern.Expand();

                message = $"Opened '{entry.Label}' from the foreground window.";
                return true;
            }

            entry.Element.SetFocus();
            message = $"Focused '{entry.Label}' from the foreground window.";
            return true;
        }
        catch (ElementNotAvailableException)
        {
            message = $"Visible control {entry.Number} is no longer available.";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            message = $"Unable to activate visible control {entry.Number}: {ex.Message}";
            return false;
        }
        catch (COMException ex)
        {
            message = $"Unable to activate visible control {entry.Number}: {ex.Message}";
            return false;
        }
    }

    public bool TryMouseAction(DesktopVisibleControlEntry entry, DesktopVisibleControlMouseAction action, out string message)
    {
        if (!entry.IsEnabled)
        {
            message = $"Visible control {entry.Number} is no longer enabled.";
            return false;
        }

        if (entry.Bounds.IsEmpty)
        {
            message = $"Visible control {entry.Number} no longer has visible bounds.";
            return false;
        }

        var center = new Point(entry.Bounds.Left + entry.Bounds.Width / 2, entry.Bounds.Top + entry.Bounds.Height / 2);
        Cursor.Position = center;

        switch (action)
        {
            case DesktopVisibleControlMouseAction.DoubleClick:
                mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                message = $"Double-clicked '{entry.Label}' from the foreground window.";
                return true;
            case DesktopVisibleControlMouseAction.RightClick:
                mouse_event(MouseEventRightDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);
                message = $"Right-clicked '{entry.Label}' from the foreground window.";
                return true;
            default:
                message = $"Unsupported visible control mouse action: {action}.";
                return false;
        }
    }

    public static bool LabelsMatch(string candidate, string normalizedLabel)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(normalizedLabel))
            return false;

        return string.Equals(Normalize(candidate), Normalize(normalizedLabel), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateEntry(AutomationElement element, int number, out DesktopVisibleControlEntry entry)
    {
        entry = null!;
        var bounds = SafeGetBounds(element);
        if (bounds.Width < 8 || bounds.Height < 8)
            return false;

        var label = BuildLabel(element);
        if (string.IsNullOrWhiteSpace(label))
            return false;

        entry = new DesktopVisibleControlEntry(
            number,
            label,
            bounds,
            SafeGetControlType(element),
            SafeGetString(element, AutomationElement.AutomationIdProperty),
            SafeGetBool(element, AutomationElement.IsKeyboardFocusableProperty),
            SafeGetBool(element, AutomationElement.IsEnabledProperty),
            IsActionable(element),
            element);
        return true;
    }

    public static IReadOnlyList<DesktopVisibleControlEntry> PrioritizeEntries(IEnumerable<DesktopVisibleControlEntry> entries) =>
        entries
            .OrderByDescending(entry => entry.IsActionable)
            .ThenByDescending(entry => entry.IsKeyboardFocusable)
            .ThenBy(entry => entry.Bounds.Top)
            .ThenBy(entry => entry.Bounds.Left)
            .ThenBy(entry => entry.Number)
            .ToArray();

    private static bool TryCaptureWindow(nint hwnd, string fallbackTitle, string windowDescription, out DesktopVisibleControlSnapshot snapshot)
    {
        snapshot = new DesktopVisibleControlSnapshot(fallbackTitle, hwnd, []);

        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root == null)
            {
                snapshot = snapshot with { Warning = $"UI Automation could not inspect the {windowDescription}." };
                return false;
            }

            var title = SafeGetString(root, AutomationElement.NameProperty);
            var elements = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.IsControlElementProperty, true),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false),
                    new PropertyCondition(AutomationElement.IsEnabledProperty, true)));

            var entries = new List<DesktopVisibleControlEntry>();
            for (var index = 0; index < elements.Count && entries.Count < MaxControls; index++)
            {
                if (TryCreateEntry(elements[index], entries.Count + 1, out var entry))
                    entries.Add(entry);
            }

            entries = PrioritizeEntries(entries).ToList();
            for (var index = 0; index < entries.Count; index++)
                entries[index] = entries[index] with { Number = index + 1 };

            snapshot = new DesktopVisibleControlSnapshot(
                string.IsNullOrWhiteSpace(title) ? fallbackTitle : title,
                hwnd,
                entries,
                entries.Count == 0 ? $"No interactive UI Automation controls were found in the {windowDescription}." : null);

            return entries.Count > 0;
        }
        catch (ElementNotAvailableException ex)
        {
            snapshot = snapshot with { Warning = $"{fallbackTitle} changed before UI Automation inspection completed: {ex.Message}" };
            return false;
        }
        catch (COMException ex)
        {
            snapshot = snapshot with { Warning = $"UI Automation inspection failed: {ex.Message}" };
            return false;
        }
        catch (InvalidOperationException ex)
        {
            snapshot = snapshot with { Warning = $"UI Automation inspection failed: {ex.Message}" };
            return false;
        }
    }

    private static string BuildLabel(AutomationElement element)
    {
        var name = SafeGetString(element, AutomationElement.NameProperty);
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var automationId = SafeGetString(element, AutomationElement.AutomationIdProperty);
        var controlType = SafeGetControlType(element);
        if (!string.IsNullOrWhiteSpace(automationId))
            return $"{controlType} {automationId}".Trim();

        return controlType;
    }

    private static Rectangle SafeGetBounds(AutomationElement element)
    {
        try
        {
            var rect = element.Current.BoundingRectangle;
            if (rect.IsEmpty || double.IsNaN(rect.Width) || double.IsNaN(rect.Height))
                return Rectangle.Empty;

            return new Rectangle(
                (int)Math.Round(rect.Left),
                (int)Math.Round(rect.Top),
                Math.Max(0, (int)Math.Round(rect.Width)),
                Math.Max(0, (int)Math.Round(rect.Height)));
        }
        catch (ElementNotAvailableException)
        {
            return Rectangle.Empty;
        }
    }

    private static string SafeGetControlType(AutomationElement element)
    {
        try
        {
            return element.Current.ControlType?.LocalizedControlType ?? "control";
        }
        catch (ElementNotAvailableException)
        {
            return "control";
        }
    }

    private static string SafeGetString(AutomationElement element, AutomationProperty property)
    {
        try
        {
            return element.GetCurrentPropertyValue(property, ignoreDefaultValue: true) as string ?? string.Empty;
        }
        catch (ElementNotAvailableException)
        {
            return string.Empty;
        }
    }

    private static bool SafeGetBool(AutomationElement element, AutomationProperty property)
    {
        try
        {
            return element.GetCurrentPropertyValue(property, ignoreDefaultValue: true) is bool value && value;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static bool IsActionable(AutomationElement element)
    {
        try
        {
            return element.TryGetCurrentPattern(InvokePattern.Pattern, out _)
                || element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _)
                || element.TryGetCurrentPattern(TogglePattern.Pattern, out _)
                || element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out _);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static int ScoreWindowCandidate(string normalizedRequest, string title, string processName)
    {
        if (string.IsNullOrWhiteSpace(normalizedRequest))
            return 0;

        var normalizedTitle = Normalize(title);
        var normalizedProcess = Normalize(processName);
        var normalizedCombined = Normalize($"{processName} {title}");

        if (string.Equals(normalizedTitle, normalizedRequest, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedProcess, normalizedRequest, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedCombined, normalizedRequest, StringComparison.OrdinalIgnoreCase))
            return 400;

        if ((!string.IsNullOrWhiteSpace(normalizedTitle) && normalizedTitle.StartsWith(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedProcess) && normalizedProcess.StartsWith(normalizedRequest, StringComparison.OrdinalIgnoreCase)))
            return 300;

        if ((!string.IsNullOrWhiteSpace(normalizedTitle) && normalizedTitle.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedProcess) && normalizedProcess.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(normalizedCombined) && normalizedCombined.Contains(normalizedRequest, StringComparison.OrdinalIgnoreCase)))
            return 200;

        var requestTokens = normalizedRequest.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requestTokens.Length > 0
            && requestTokens.All(token => normalizedCombined.Contains(token, StringComparison.OrdinalIgnoreCase)))
            return 100;

        return 0;
    }

    private static string ReadWindowText(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0)
            return string.Empty;

        var buffer = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString().Trim();
    }

    private static string TryGetProcessName(int processId)
    {
        if (processId <= 0)
            return string.Empty;

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryGetWindowTitle(nint hwnd)
    {
        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            var title = root == null ? string.Empty : SafeGetString(root, AutomationElement.NameProperty);
            return string.IsNullOrWhiteSpace(title) ? "Current window" : title;
        }
        catch (ElementNotAvailableException)
        {
            return "Current window";
        }
        catch (COMException)
        {
            return "Current window";
        }
        catch (InvalidOperationException)
        {
            return "Current window";
        }
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLower(CultureInfo.InvariantCulture);
        normalized = normalized
            .Replace("&", " and ", StringComparison.Ordinal)
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("\\", " ", StringComparison.Ordinal)
            .Replace("+", " plus ", StringComparison.Ordinal)
            .Replace("#", " number ", StringComparison.Ordinal)
            .Replace("@", " at ", StringComparison.Ordinal)
            .Replace("*", " star ", StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace("’", string.Empty, StringComparison.Ordinal)
            .Replace("\"", string.Empty, StringComparison.Ordinal);
        normalized = string.Join(
            " ",
            normalized.Split([' ', '\t', '\r', '\n', '_', '-', '.', ',', ':', ';', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out Rect lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out int processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct WindowCandidate(nint Handle, string Title, string ProcessName);

    private delegate bool EnumWindowsProc(nint hWnd, IntPtr lParam);
}

public enum DesktopVisibleControlMouseAction
{
    DoubleClick,
    RightClick
}
