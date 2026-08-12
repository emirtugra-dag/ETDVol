using System;
using System.Diagnostics;
using System.Windows;
using WpfCheckBox = System.Windows.Controls.CheckBox;

namespace ETDVol;

public partial class MainWindow : Window
{
    private bool _isLoaded = false;
    private VolumeController _volController;

    public MainWindow()
    {
        InitializeComponent();
        _volController = new VolumeController();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsManager.Load();
        var s = SettingsManager.Current;
        
        ChkAutoStart.IsChecked = s.AutoStart;
        ChkAcceleration.IsChecked = s.EnableAcceleration;
        ChkOSD.IsChecked = s.EnableOSD;
        ChkTrayIcon.IsChecked = s.EnableTrayIcon;
        SldStepSize.Value = s.StepSize;
        SldOsdDuration.Value = s.OSDDurationMs;
        
        LoadDevices();
        if (s.Language == "en")
            UpdateLocalizationEN();
        else
            UpdateLocalizationTR();
        
        _isLoaded = true;
    }

    private void LoadDevices()
    {
        var devices = _volController.GetAudioDevices();
        PanelDevices.Children.Clear();
        var enabledDevices = SettingsManager.Current.EnabledDevices;

        foreach (var dev in devices)
        {
            var chk = new WpfCheckBox
            {
                Content = dev.Name,
                Tag = dev.Id,
                IsChecked = (enabledDevices.Count == 0) || enabledDevices.Contains(dev.Id)
            };
            chk.Checked += SettingChanged;
            chk.Unchecked += SettingChanged;
            PanelDevices.Children.Add(chk);
        }
    }

    private void BtnLangTR_Click(object sender, RoutedEventArgs e)
    {
        UpdateLocalizationTR();
        SettingsManager.Current.Language = "tr";
        SettingsManager.Save();
    }

    private void BtnLangEN_Click(object sender, RoutedEventArgs e)
    {
        UpdateLocalizationEN();
        SettingsManager.Current.Language = "en";
        SettingsManager.Save();
    }
    
    private void UpdateLocalizationTR()
    {
        this.Title = "ETDVol Ayarları";
        TxtHeader.Text = "ETDVol Ayarları";
        ChkAutoStart.Content = "Windows ile Birlikte Başlat";
        ChkAcceleration.Content = "Dinamik Scroll İvmelenmesini Kullan";
        ChkOSD.Content = "Ekrandaki Görsel Göstergeyi (OSD) Göster";
        ChkTrayIcon.Content = "Sistem Tepsisi Simgesini Göster (Tray Icon)";
        TxtStepSize.Text = "Ses Değişim Adımı (Yüzdelik):";
        TxtOsdDuration.Text = "OSD Gösterim Süresi (ms):";
        TxtDevices.Text = "Cihaz Geçiş Döngüsüne Dahil Edilecekler (Shift + Orta Tuş):";
        TxtNote.Text = "Kısayollar:\n• Görev Çubuğunda Scroll: Ses Artırma / Azaltma\n• Görev Çubuğunda Shift + Orta Tuş: Ses Cihazı Değiştirme\n• Ekrandaki Ses Göstergesine (OSD) Tıklama: Bu Ayarlar Menüsünü Açar";
        BtnApply.Content = "Uygula ve Arka Planda Başlat";
    }
    
    private void UpdateLocalizationEN()
    {
        this.Title = "ETDVol Settings";
        TxtHeader.Text = "ETDVol Settings";
        ChkAutoStart.Content = "Start with Windows";
        ChkAcceleration.Content = "Use Dynamic Scroll Acceleration";
        ChkOSD.Content = "Show On-Screen Display (OSD)";
        ChkTrayIcon.Content = "Show System Tray Icon";
        TxtStepSize.Text = "Volume Step Size (Percentage):";
        TxtOsdDuration.Text = "OSD Duration (ms):";
        TxtDevices.Text = "Devices to include in cycle (Shift + Middle Click):";
        TxtNote.Text = "Shortcuts:\n• Scroll on taskbar: Volume Up / Down\n• Shift + Middle Click on taskbar: Cycle Audio Device\n• Click on On-Screen OSD Indicator: Opens this Settings Menu";
        BtnApply.Content = "Apply and Start in Background";
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        
        var s = SettingsManager.Current;
        s.AutoStart = ChkAutoStart.IsChecked ?? true;
        s.EnableAcceleration = ChkAcceleration.IsChecked ?? true;
        s.EnableOSD = ChkOSD.IsChecked ?? true;
        s.EnableTrayIcon = ChkTrayIcon.IsChecked ?? true;
        s.StepSize = SldStepSize.Value;
        s.OSDDurationMs = (int)SldOsdDuration.Value;
        
        s.EnabledDevices.Clear();
        foreach (UIElement child in PanelDevices.Children)
        {
            if (child is WpfCheckBox chk && chk.IsChecked == true && chk.Tag is string id)
            {
                s.EnabledDevices.Add(id);
            }
        }
        
        SettingsManager.Save();
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        string? currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(currentExe))
        {
            var procInfo = new ProcessStartInfo
            {
                FileName = currentExe,
                UseShellExecute = true
            };
            Process.Start(procInfo);
        }
        
        System.Windows.Application.Current.Shutdown();
    }
}