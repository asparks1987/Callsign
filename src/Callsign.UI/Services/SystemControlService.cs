using System.Runtime.InteropServices;

namespace Callsign.UI.Services;

public sealed class SystemControlService
{
    public bool TryExecute(string action, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(action))
        {
            message = "System action was empty.";
            return false;
        }

        try
        {
            switch (action.Trim().ToLowerInvariant())
            {
                case "system-volume-up":
                    SendVolumeKey(VK_VOLUME_UP);
                    message = "Volume up requested.";
                    return true;
                case "system-volume-down":
                    SendVolumeKey(VK_VOLUME_DOWN);
                    message = "Volume down requested.";
                    return true;
                case "system-volume-mute":
                    SendVolumeKey(VK_VOLUME_MUTE);
                    message = "Volume mute requested.";
                    return true;
                case "system-show-desktop":
                    SendDesktopShortcut();
                    message = "Show desktop requested.";
                    return true;
                case "system-next-window":
                    SendAltTab();
                    message = "Next window requested.";
                    return true;
                case "system-previous-window":
                    SendAltShiftTab();
                    message = "Previous window requested.";
                    return true;
                case "system-open-task-manager":
                    SendTaskManagerShortcut();
                    message = "Task Manager requested.";
                    return true;
                case "system-minimize-window":
                    ShowForegroundWindow(SW_MINIMIZE);
                    message = "Minimize window requested.";
                    return true;
                case "system-maximize-window":
                    ShowForegroundWindow(SW_MAXIMIZE);
                    message = "Maximize window requested.";
                    return true;
                case "system-restore-window":
                    ShowForegroundWindow(SW_RESTORE);
                    message = "Restore window requested.";
                    return true;
                case "system-press-enter":
                    SendKey(VK_RETURN);
                    message = "Enter requested.";
                    return true;
                case "system-press-tab":
                    SendKey(VK_TAB);
                    message = "Tab requested.";
                    return true;
                case "system-press-escape":
                    SendKey(VK_ESCAPE);
                    message = "Escape requested.";
                    return true;
                case "system-press-backspace":
                    SendKey(VK_BACK);
                    message = "Backspace requested.";
                    return true;
                case "system-press-up":
                    SendKey(VK_UP);
                    message = "Up arrow requested.";
                    return true;
                case "system-press-down":
                    SendKey(VK_DOWN);
                    message = "Down arrow requested.";
                    return true;
                case "system-press-left":
                    SendKey(VK_LEFT);
                    message = "Left arrow requested.";
                    return true;
                case "system-press-right":
                    SendKey(VK_RIGHT);
                    message = "Right arrow requested.";
                    return true;
                case "system-press-home":
                    SendKey(VK_HOME);
                    message = "Home requested.";
                    return true;
                case "system-press-end":
                    SendKey(VK_END);
                    message = "End requested.";
                    return true;
                case "system-page-up":
                    SendKey(VK_PRIOR);
                    message = "Page up requested.";
                    return true;
                case "system-page-down":
                    SendKey(VK_NEXT);
                    message = "Page down requested.";
                    return true;
                case "system-mouse-click":
                    SendMouseClick();
                    message = "Mouse click requested.";
                    return true;
                case "system-mouse-double-click":
                    SendMouseDoubleClick();
                    message = "Mouse double-click requested.";
                    return true;
                case "system-mouse-right-click":
                    SendMouseRightClick();
                    message = "Mouse right-click requested.";
                    return true;
                case "system-mouse-scroll-up":
                    SendMouseWheel(MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll up requested.";
                    return true;
                case "system-mouse-scroll-down":
                    SendMouseWheel(-MOUSE_WHEEL_DELTA);
                    message = "Mouse scroll down requested.";
                    return true;
                case "system-copy":
                    SendKeyChord(VK_CONTROL, VK_C);
                    message = "Copy requested.";
                    return true;
                case "system-paste":
                    SendKeyChord(VK_CONTROL, VK_V);
                    message = "Paste requested.";
                    return true;
                case "system-cut":
                    SendKeyChord(VK_CONTROL, VK_X);
                    message = "Cut requested.";
                    return true;
                case "system-select-all":
                    SendKeyChord(VK_CONTROL, VK_A);
                    message = "Select all requested.";
                    return true;
                case "system-save":
                    SendKeyChord(VK_CONTROL, VK_S);
                    message = "Save requested.";
                    return true;
                case "system-undo":
                    SendKeyChord(VK_CONTROL, VK_Z);
                    message = "Undo requested.";
                    return true;
                case "system-redo":
                    SendKeyChord(VK_CONTROL, VK_Y);
                    message = "Redo requested.";
                    return true;
                case "system-find":
                    SendKeyChord(VK_CONTROL, VK_F);
                    message = "Find requested.";
                    return true;
                case "system-new-window":
                    SendKeyChord(VK_CONTROL, VK_N);
                    message = "New window requested.";
                    return true;
                case "system-close-window":
                    SendAltKey(VK_F4);
                    message = "Close window requested.";
                    return true;
                case "system-move-previous-word":
                    SendCtrlArrow(VK_LEFT);
                    message = "Move previous word requested.";
                    return true;
                case "system-move-next-word":
                    SendCtrlArrow(VK_RIGHT);
                    message = "Move next word requested.";
                    return true;
                case "system-select-previous-word":
                    SendCtrlShiftArrow(VK_LEFT);
                    message = "Select previous word requested.";
                    return true;
                case "system-select-next-word":
                    SendCtrlShiftArrow(VK_RIGHT);
                    message = "Select next word requested.";
                    return true;
                case "system-delete-previous-word":
                    SendKeyChord(VK_CONTROL, VK_BACK);
                    message = "Delete previous word requested.";
                    return true;
                case "system-delete-next-word":
                    SendCtrlDelete();
                    message = "Delete next word requested.";
                    return true;
                case "system-move-previous-sentence":
                    SendCtrlArrow(VK_UP);
                    message = "Move previous sentence requested.";
                    return true;
                case "system-move-next-sentence":
                    SendCtrlArrow(VK_DOWN);
                    message = "Move next sentence requested.";
                    return true;
                case "system-select-previous-sentence":
                    SendCtrlShiftArrow(VK_UP);
                    message = "Select previous sentence requested.";
                    return true;
                case "system-select-next-sentence":
                    SendCtrlShiftArrow(VK_DOWN);
                    message = "Select next sentence requested.";
                    return true;
                case "system-delete-previous-sentence":
                    SendKeyChord(VK_CONTROL, VK_BACK);
                    message = "Delete previous sentence requested.";
                    return true;
                case "system-delete-next-sentence":
                    SendKeyChord(VK_CONTROL, VK_DELETE);
                    message = "Delete next sentence requested.";
                    return true;
                case "system-move-previous-paragraph":
                    SendAltKey(VK_UP);
                    message = "Move previous paragraph requested.";
                    return true;
                case "system-move-next-paragraph":
                    SendAltKey(VK_DOWN);
                    message = "Move next paragraph requested.";
                    return true;
                case "system-select-previous-paragraph":
                    SendAltShiftKey(VK_UP);
                    message = "Select previous paragraph requested.";
                    return true;
                case "system-select-next-paragraph":
                    SendAltShiftKey(VK_DOWN);
                    message = "Select next paragraph requested.";
                    return true;
                case "system-delete-previous-paragraph":
                    SendAltBackspace();
                    message = "Delete previous paragraph requested.";
                    return true;
                case "system-delete-next-paragraph":
                    SendAltDelete();
                    message = "Delete next paragraph requested.";
                    return true;
                default:
                    message = $"Unknown system action: {action}";
                    return false;
            }
        }
        catch (Exception ex)
        {
            message = $"Unable to execute system action: {ex.Message}";
            return false;
        }
    }

    private static void SendVolumeKey(ushort virtualKey)
    {
        SendKey(virtualKey);
    }

    private static void SendKey(ushort virtualKey)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyChord(ushort modifierKey, ushort key)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendAltKey(ushort key)
    {
        SendKeyChord(VK_MENU, key);
    }

    private static void SendAltShiftKey(ushort key)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MENU } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MENU, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendAltBackspace()
    {
        SendAltKey(VK_BACK);
    }

    private static void SendAltDelete()
    {
        SendAltKey(VK_DELETE);
    }

    private static void SendCtrlArrow(ushort arrowKey)
    {
        SendKeyChord(VK_CONTROL, arrowKey);
    }

    private static void SendCtrlShiftArrow(ushort arrowKey)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = arrowKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = arrowKey, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendCtrlDelete()
    {
        SendKeyChord(VK_CONTROL, VK_DELETE);
    }

    private static void SendMouseClick()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
    }

    private static void SendMouseDoubleClick()
    {
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        SendMouseButton(MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
    }

    private static void SendMouseRightClick()
    {
        SendMouseButton(MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP);
    }

    private static void SendMouseWheel(int delta)
    {
        var inputs = new INPUT[]
        {
            new()
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_WHEEL,
                        mouseData = unchecked((uint)delta)
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendMouseButton(uint downFlag, uint upFlag)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = downFlag } } },
            new() { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = upFlag } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendDesktopShortcut()
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyD } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyD, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftWindows, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendAltTab()
    {
        SendChord(VirtualKeyLeftAlt, VirtualKeyTab);
    }

    private static void SendAltShiftTab()
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftAlt } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyTab } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyTab, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftAlt, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendTaskManagerShortcut()
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyEscape, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftShift, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VirtualKeyLeftCtrl, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void ShowForegroundWindow(int command)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == nint.Zero)
            throw new InvalidOperationException("No foreground window was available.");

        if (!ShowWindow(hwnd, command))
            throw new InvalidOperationException("ShowWindow returned false.");
    }

    private static void SendChord(ushort modifierKey, ushort key)
    {
        var inputs = new INPUT[]
        {
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = KeyEventKeyUp } } },
            new() { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = modifierKey, dwFlags = KeyEventKeyUp } } }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private const int InputKeyboard = 1;
    private const int InputMouse = 0;
    private const ushort VK_VOLUME_UP = 0xAF;
    private const ushort VK_VOLUME_DOWN = 0xAE;
    private const ushort VK_VOLUME_MUTE = 0xAD;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_A = 0x41;
    private const ushort VK_C = 0x43;
    private const ushort VK_X = 0x58;
    private const ushort VK_V = 0x56;
    private const ushort VK_S = 0x53;
    private const ushort VK_Z = 0x5A;
    private const ushort VK_Y = 0x59;
    private const ushort VK_F = 0x46;
    private const ushort VK_N = 0x4E;
    private const ushort VK_F4 = 0x73;
    private const ushort VK_DELETE = 0x2E;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_ESCAPE = 0x1B;
    private const ushort VK_BACK = 0x08;
    private const ushort VK_UP = 0x26;
    private const ushort VK_DOWN = 0x28;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_RIGHT = 0x27;
    private const ushort VK_HOME = 0x24;
    private const ushort VK_END = 0x23;
    private const ushort VK_PRIOR = 0x21;
    private const ushort VK_NEXT = 0x22;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const ushort VirtualKeyD = 0x44;
    private const ushort VirtualKeyLeftAlt = 0xA4;
    private const ushort VirtualKeyLeftShift = 0xA0;
    private const ushort VirtualKeyLeftCtrl = 0xA2;
    private const ushort VirtualKeyTab = 0x09;
    private const ushort VirtualKeyEscape = 0x1B;
    private const int SW_MINIMIZE = 6;
    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const int MOUSE_WHEEL_DELTA = 120;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }
}
