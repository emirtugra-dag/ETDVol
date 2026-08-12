using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ETDVol;

public class WindowsHook : IDisposable
{
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private readonly HookProc _mouseProc;
    private readonly HookProc _keyboardProc;
    private IntPtr _mouseHookID = IntPtr.Zero;
    private IntPtr _keyboardHookID = IntPtr.Zero;

    private bool _isCtrlDown;
    private bool _isShiftDown;
    private bool _isAltDown;

    public event Action<int>? OnScroll;
    public event Action? OnMiddleClickShift;

    public WindowsHook()
    {
        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;

        _isCtrlDown = (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0xA2) & 0x8000) != 0 || (GetAsyncKeyState(0xA3) & 0x8000) != 0;
        _isShiftDown = (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0xA0) & 0x8000) != 0 || (GetAsyncKeyState(0xA1) & 0x8000) != 0;
        _isAltDown = (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0xA4) & 0x8000) != 0 || (GetAsyncKeyState(0xA5) & 0x8000) != 0;
        
        using (Process curProcess = Process.GetCurrentProcess())
        {
            ProcessModule? curModule = curProcess.MainModule;
            if (curModule != null)
            {
                IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName ?? "");
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
                _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
            }
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WM_MOUSEWHEEL || msg == WM_MBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                IntPtr hwnd = WindowFromPoint(hookStruct.pt);
                IntPtr rootHwnd = GetAncestor(hwnd, 2); // GA_ROOT

                StringBuilder sb = new StringBuilder(256);
                GetClassName(rootHwnd, sb, sb.Capacity);
                string className = sb.ToString();

                StringBuilder sbSub = new StringBuilder(256);
                GetClassName(hwnd, sbSub, sbSub.Capacity);
                string subClassName = sbSub.ToString();

                bool isTaskbar = className == "Shell_TrayWnd" || 
                                 className == "Shell_SecondaryTrayWnd" || 
                                 className == "Windows.UI.Core.CoreWindow" || 
                                 className == "XamlExplorerHostIslandWindow" ||
                                 className == "TrayNotifyWnd" ||
                                 subClassName == "MSTaskListWClass" ||
                                 subClassName == "ToolbarWindow32";

                if (isTaskbar)
                {
                    if (msg == WM_MOUSEWHEEL)
                    {
                        int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                        OnScroll?.Invoke(delta > 0 ? 1 : -1);
                        return (IntPtr)1; // Consume
                    }
                    else if (msg == WM_MBUTTONDOWN)
                    {
                        bool shiftPressed = _isShiftDown || (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0xA0) & 0x8000) != 0 || (GetAsyncKeyState(0xA1) & 0x8000) != 0;

                        if (shiftPressed)
                        {
                            OnMiddleClickShift?.Invoke();
                            return (IntPtr)1; // Consume
                        }
                    }
                }
            }
        }
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            int vkCode = Marshal.ReadInt32(lParam);

            // Update internal modifier state on key down and key up (for mouse shortcuts like Ctrl+Wheel, Ctrl+MiddleClick)
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                if (vkCode == 0x11 || vkCode == 0xA2 || vkCode == 0xA3) _isCtrlDown = true;
                if (vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1) _isShiftDown = true;
                if (vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5) _isAltDown = true;
            }
            else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
            {
                if (vkCode == 0x11 || vkCode == 0xA2 || vkCode == 0xA3) _isCtrlDown = false;
                if (vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1) _isShiftDown = false;
                if (vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5) _isAltDown = false;
            }
        }
        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    private static bool MatchModifiers(int currentModifiers, int targetModifiers)
    {
        if (targetModifiers == 0) return true;
        return (currentModifiers & targetModifiers) == targetModifiers;
    }

    public void Dispose()
    {
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
            _mouseHookID = IntPtr.Zero;
        }
        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            _keyboardHookID = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    [DllImport("user32.dll")] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
}

