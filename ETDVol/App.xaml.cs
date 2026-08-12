using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ETDVol;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private WindowsHook? _hook;
    private VolumeController? _volumeController;
    private OSDWindow? _osdWindow;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        _mutex = new Mutex(true, "ETDVol_SingleInstance_Mutex", out createdNew);
        if (!createdNew)
        {
            Current.Shutdown();
            return;
        }

        SettingsManager.Load();

        bool isSettingsMode = e.Args.Contains("-settings");
        if (isSettingsMode)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        else
        {
            StartBackgroundService();
        }
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
            _volumeController.ChangeVolume(direction);
            ShowOSD();
        };

        _hook.OnMiddleClickShift += () =>
        {
            // Shift + Tek Tıklama: Doğrudan ses cihazını değiştir
            _volumeController.CycleAudioDevice();
            ShowOSD();
        };
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
        contextMenu.Items.Add("Ayarlar (Settings)", null, (s, e) => OpenSettings());
        contextMenu.Items.Add("Çıkış (Exit)", null, (s, e) => ExitApp());
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        _notifyIcon.DoubleClick += (s, e) => OpenSettings();
    }

    private void OpenSettings()
    {
        foreach (Window w in System.Windows.Application.Current.Windows)
        {
            if (w is MainWindow mw)
            {
                mw.Activate();
                return;
            }
        }
        var mainWnd = new MainWindow();
        mainWnd.Show();
        mainWnd.Activate();
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
                    Dispatcher.Invoke(() => OpenSettings());
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
        _hook?.Dispose();
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
