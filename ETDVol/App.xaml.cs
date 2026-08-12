using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ETDVol;

public partial class App : System.Windows.Application
{
    private const string MutexName = "ETDVol_SingleInstance_Mutex_v1";
    private const string EventName = "ETDVol_OpenSettings_Event_v1";

    private Mutex? _mutex;
    private EventWaitHandle? _eventWaitHandle;
    private WindowsHook? _hook;
    private VolumeController? _volumeController;
    private OSDWindow? _osdWindow;
    private MainWindow? _mainWindow;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // İkinci bir kopya açılmaya çalışıldığında mevcut uygulamaya ayarları açma sinyali gönder
            SendOpenSettingsSignal();
            Current.Shutdown();
            return;
        }

        SettingsManager.Load();
        StartSingleInstanceListener();

        StartBackgroundService();

        // Otomatik başlangıçta (-autostart veya --autostart) ayarlar penceresi açılmaz, arka planda kalır.
        // Manuel çalıştırmada veya kısayoldan açılışta ayarlar penceresi açılır.
        bool isAutostart = e.Args.Any(a => a.Equals("-autostart", StringComparison.OrdinalIgnoreCase) || a.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
        if (!isAutostart)
        {
            OpenSettingsWindow();
        }
    }

    private void StartSingleInstanceListener()
    {
        try
        {
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            ThreadPool.RegisterWaitForSingleObject(_eventWaitHandle, (state, timedOut) =>
            {
                Dispatcher.Invoke(() =>
                {
                    OpenSettingsWindow();
                });
            }, null, -1, false);
        }
        catch { }
    }

    private static void SendOpenSettingsSignal()
    {
        try
        {
            using var evt = EventWaitHandle.OpenExisting(EventName);
            evt.Set();
        }
        catch { }
    }

    private void StartBackgroundService()
    {
        _volumeController = new VolumeController();
        _hook = new WindowsHook();
        
        if (SettingsManager.Current.EnableTrayIcon)
        {
            SetupTrayIcon();
        }

        SettingsManager.OnSettingsSaved += () =>
        {
            if (SettingsManager.Current.EnableTrayIcon)
                SetupTrayIcon();
            else
                HideTrayIcon();
        };

        _hook.OnScroll += (direction) =>
        {
            Dispatcher.Invoke(() =>
            {
                _volumeController?.ChangeVolume(direction);
                ShowOSD();
            });
        };

        _hook.OnMiddleClickShift += () =>
        {
            Dispatcher.Invoke(() =>
            {
                _volumeController?.CycleAudioDevice();
                ShowOSD();
            });
        };

        MemoryOptimizer.TrimWorkingSet();
    }

    private void HideTrayIcon()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private void SetupTrayIcon()
    {
        if (_notifyIcon != null) return;
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        
        try
        {
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? "");
        }
        catch { }
        
        if (_notifyIcon.Icon == null)
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;

        _notifyIcon.Text = "ETDVol";
        _notifyIcon.Visible = true;
        
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Ayarlar (Settings)", null, (s, e) => OpenSettingsWindow());
        contextMenu.Items.Add("Çıkış (Exit)", null, (s, e) => ExitApp());
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        _notifyIcon.DoubleClick += (s, e) => OpenSettingsWindow();
    }

    public void OpenSettingsWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow();
        }

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }
    
    private void ExitApp()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        Current.Shutdown();
    }

    private void ShowOSD()
    {
        if (!SettingsManager.Current.EnableOSD) return;

        Dispatcher.Invoke(() =>
        {
            if (_osdWindow == null)
            {
                _osdWindow = new OSDWindow();
                _osdWindow.OnOSDClicked += () =>
                {
                    Dispatcher.Invoke(() => OpenSettingsWindow());
                };
            }
            if (_volumeController != null)
            {
                string name = _volumeController.GetDefaultDeviceName();
                int vol = _volumeController.GetVolumePercent();
                _osdWindow.ShowUpdate(name, vol);
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _eventWaitHandle?.Dispose();
        _hook?.Dispose();
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
