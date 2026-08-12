using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace ETDVol.Uninstall;

public partial class UninstallWindow : Window
{
    private string _currentLang = "tr";

    public UninstallWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETDVol", "settings.json");
            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                if (json.Contains("\"Language\": \"en\"", StringComparison.OrdinalIgnoreCase))
                {
                    _currentLang = "en";
                }
            }
        }
        catch { }

        if (_currentLang == "en")
            UpdateLocalizationEN();
        else
            UpdateLocalizationTR();
    }

    private void BtnLangTR_Click(object sender, RoutedEventArgs e)
    {
        UpdateLocalizationTR();
    }

    private void BtnLangEN_Click(object sender, RoutedEventArgs e)
    {
        UpdateLocalizationEN();
    }

    private void UpdateLocalizationTR()
    {
        _currentLang = "tr";
        this.Title = "ETDVol Kaldırma Sihirbazı";
        TxtTitle.Text = "ETDVol Kaldırma";
        TxtPrompt.Text = "ETDVol uygulamasını bilgisayarınızdan kaldırmak istediğinize emin misiniz?";
        BtnUninstall.Content = "Kaldır";
        BtnCancel.Content = "İptal";
    }

    private void UpdateLocalizationEN()
    {
        _currentLang = "en";
        this.Title = "ETDVol Uninstall Wizard";
        TxtTitle.Text = "ETDVol Uninstall";
        TxtPrompt.Text = "Are you sure you want to uninstall ETDVol from your computer?";
        BtnUninstall.Content = "Uninstall";
        BtnCancel.Content = "Cancel";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void BtnUninstall_Click(object sender, RoutedEventArgs e)
    {
        BtnUninstall.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        TxtStatus.Text = _currentLang == "tr" ? "Kaldırılıyor..." : "Uninstalling...";

        bool success = false;
        string errorMessage = "";

        await Task.Run(() =>
        {
            try
            {
                // Kill running processes
                var currentProcess = Process.GetCurrentProcess();
                foreach (var process in Process.GetProcessesByName("ETDVol"))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                        catch { }
                    }
                }

                // Remove Startup registry key
                using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    runKey?.DeleteValue("ETDVol", false);
                }

                // Remove Control Panel Uninstall registry key
                using (var uninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    uninstallKey?.DeleteSubKeyTree("ETDVol", false);
                }

                // Remove Start Menu shortcut
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ETDVol");
                if (Directory.Exists(startMenu))
                {
                    try { Directory.Delete(startMenu, true); } catch { }
                }

                success = true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        });

        if (success)
        {
            TxtStatus.Text = _currentLang == "tr" ? "Kaldırma başarıyla tamamlandı!" : "Uninstall completed successfully!";
            await Task.Delay(1200);
            
            // Delete installed directory batch command after process exits
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(exeDir) && exeDir.Contains("ETDVol"))
            {
                string cmd = $"/c timeout /t 1 /nobreak & rmdir /s /q \"{exeDir.TrimEnd('\\')}\"";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmd,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }

            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            TxtStatus.Text = (_currentLang == "tr" ? "Hata: " : "Error: ") + errorMessage;
            BtnUninstall.IsEnabled = true;
            BtnCancel.IsEnabled = true;
        }
    }
}
