using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETDVol;

public class WindowsHook : IDisposable
{
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MBUTTONDOWN = 0x0207;

    private readonly HookProc _mouseProc;
    private IntPtr _mouseHookID = IntPtr.Zero;

    private IntPtr _lastHwnd = IntPtr.Zero;
    private bool _lastIsTaskbar = false;
    private POINT _lastPoint = new POINT { x = -99999, y = -99999 };

    public event Action<int>? OnScroll;
    public event Action? OnMiddleClickShift;

    public WindowsHook()
    {
        _mouseProc = MouseHookCallback;
        
        using (Process curProcess = Process.GetCurrentProcess())
        {
            ProcessModule? curModule = curProcess.MainModule;
            if (curModule != null)
            {
                IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName ?? "");
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
            }
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;

            // Farenin normal hareketlerini (WM_MOUSEMOVE, tıklamalar vb.) anında sıfır gecikmeyle geç
            if (msg == WM_MOUSEWHEEL || msg == WM_MBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                bool isTaskbar;
                if (hookStruct.pt.x == _lastPoint.x && hookStruct.pt.y == _lastPoint.y)
                {
                    isTaskbar = _lastIsTaskbar;
                }
                else
                {
                    IntPtr hwnd = WindowFromPoint(hookStruct.pt);
                    if (hwnd == _lastHwnd)
                    {
                        isTaskbar = _lastIsTaskbar;
                    }
                    else
                    {
                        IntPtr rootHwnd = GetAncestor(hwnd, 2); // GA_ROOT
                        StringBuilder sb = new StringBuilder(128);
                        GetClassName(rootHwnd, sb, sb.Capacity);
                        string className = sb.ToString();

                        StringBuilder sbSub = new StringBuilder(128);
                        GetClassName(hwnd, sbSub, sbSub.Capacity);
                        string subClassName = sbSub.ToString();

                        isTaskbar = className == "Shell_TrayWnd" || 
                                    className == "Shell_SecondaryTrayWnd" || 
                                    className == "Windows.UI.Core.CoreWindow" || 
                                    className == "XamlExplorerHostIslandWindow" ||
                                    className == "TrayNotifyWnd" ||
                                    subClassName == "MSTaskListWClass" ||
                                    subClassName == "ToolbarWindow32";

                        _lastHwnd = hwnd;
                        _lastIsTaskbar = isTaskbar;
                        _lastPoint = hookStruct.pt;
                    }
                }

                if (isTaskbar)
                {
                    if (msg == WM_MOUSEWHEEL)
                    {
                        int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                        int direction = delta > 0 ? 1 : -1;
                        Task.Run(() => OnScroll?.Invoke(direction));
                        return (IntPtr)1; // Consume event on taskbar
                    }
                    else if (msg == WM_MBUTTONDOWN)
                    {
                        // Shift tuş durumunu doğrudan asenkron donanım sorgusu ile oku
                        bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0 || 
                                             (GetAsyncKeyState(0xA0) & 0x8000) != 0 || 
                                             (GetAsyncKeyState(0xA1) & 0x8000) != 0;

                        if (shiftPressed)
                        {
                            Task.Run(() => OnMiddleClickShift?.Invoke());
                            return (IntPtr)1; // Consume event on taskbar
                        }
                    }
                }
            }
        }
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
            _mouseHookID = IntPtr.Zero;
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
