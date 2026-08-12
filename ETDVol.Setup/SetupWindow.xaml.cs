using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace ETDVol.Setup;

public partial class SetupWindow : Window
{
    private string _currentLang = "tr";

    public SetupWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string defaultProgramFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ETDVol");
        TxtInstallPath.Text = defaultProgramFiles;
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
        this.Title = "ETDVol Kurulum Sihirbazı";
        TxtTitle.Text = "ETDVol Kurulum Sihirbazı";
        TxtDescription.Text = "ETDVol uygulamasını bilgisayarınıza kurmak için aşağıdaki seçenekleri belirleyin.";
        TxtFolderLabel.Text = "Hedef Kurulum Klasörü:";
        BtnBrowse.Content = "Gözat...";
        ChkAutoStart.Content = "Windows başladığında otomatik çalıştır";
        BtnInstall.Content = "Kurulumu Başlat";
    }

    private void UpdateLocalizationEN()
    {
        _currentLang = "en";
        this.Title = "ETDVol Setup Wizard";
        TxtTitle.Text = "ETDVol Setup Wizard";
        TxtDescription.Text = "Configure the options below to install ETDVol on your computer.";
        TxtFolderLabel.Text = "Destination Folder:";
        BtnBrowse.Content = "Browse...";
        ChkAutoStart.Content = "Run automatically when Windows starts";
        BtnInstall.Content = "Start Installation";
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = _currentLang == "tr" ? "Kurulum Klasörünü Seçin" : "Select Installation Folder",
                InitialDirectory = TxtInstallPath.Text
            };
            if (dialog.ShowDialog() == true)
            {
                TxtInstallPath.Text = Path.Combine(dialog.FolderName, "ETDVol");
            }
        }
        catch
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = _currentLang == "tr" ? "Kurulum Klasöründeki Bir Dosyayı Seçin" : "Select Any File in Target Folder",
                CheckFileExists = false,
                FileName = "Klasör Seç"
            };
            if (dlg.ShowDialog() == true)
            {
                string? dir = Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(dir)) TxtInstallPath.Text = Path.Combine(dir, "ETDVol");
            }
        }
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        string targetDir = TxtInstallPath.Text.Trim();
        if (string.IsNullOrEmpty(targetDir))
        {
            TxtStatus.Text = _currentLang == "tr" ? "Lütfen geçerli bir klasör seçin!" : "Please select a valid directory!";
            return;
        }

        BtnInstall.IsEnabled = false;
        BtnBrowse.IsEnabled = false;
        TxtInstallPath.IsEnabled = false;
        ChkAutoStart.IsEnabled = false;
        TxtStatus.Text = _currentLang == "tr" ? "Kuruluyor..." : "Installing...";

        bool autoStart = ChkAutoStart.IsChecked ?? true;
        bool success = false;
        string errorMessage = "";

        await Task.Run(() =>
        {
            try
            {
                string targetExe = Path.Combine(targetDir, "ETDVol.exe");
                string uninstallExe = Path.Combine(targetDir, "Uninstall.exe");

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Kill existing ETDVol processes
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

                // Extract embedded executables into target directory
                ExtractEmbeddedPayload("ETDVol.exe", targetExe);
                ExtractEmbeddedPayload("Uninstall.exe", uninstallExe);

                // Save initial settings.json in AppData with chosen Language & AutoStart
                string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETDVol");
                if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);
                string settingsJson = $"{{\n  \"Language\": \"{_currentLang}\",\n  \"AutoStart\": {(autoStart ? "true" : "false")}\n}}";
                File.WriteAllText(Path.Combine(appDataFolder, "settings.json"), settingsJson);

                // Register Windows Startup in Registry if autoStart checked
                using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (autoStart)
                            runKey.SetValue("ETDVol", $"\"{targetExe}\" -autostart");
                        else
                            runKey.DeleteValue("ETDVol", false);
                    }
                }

                // Register in Windows Control Panel (Add/Remove Programs) pointing UninstallString to Uninstall.exe
                RegisterControlPanelUninstall(targetDir, targetExe, uninstallExe);

                // Create Start Menu Shortcut via WScript.Shell COM
                try
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ETDVol");
                    if (!Directory.Exists(startMenu)) Directory.CreateDirectory(startMenu);

                    string shortcutPath = Path.Combine(startMenu, "ETDVol.lnk");
                    CreateWScriptShortcut(shortcutPath, targetExe, "");

                    string settingsShortcut = Path.Combine(startMenu, "ETDVol Ayarları.lnk");
                    CreateWScriptShortcut(settingsShortcut, targetExe, "-settings");
                }
                catch { }

                // Launch installed main app executable
                if (File.Exists(targetExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        UseShellExecute = true
                    });
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
            TxtStatus.Text = _currentLang == "tr" ? "Kurulum başarıyla tamamlandı!" : "Installation completed successfully!";
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            TxtStatus.Text = (_currentLang == "tr" ? "Hata: " : "Error: ") + errorMessage;
            BtnInstall.IsEnabled = true;
            BtnBrowse.IsEnabled = true;
            TxtInstallPath.IsEnabled = true;
            ChkAutoStart.IsEnabled = true;
        }
    }

    private static void ExtractEmbeddedPayload(string resourceName, string targetPath)
    {
        var asm = typeof(SetupWindow).Assembly;
        string? matchName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

        if (matchName != null)
        {
            using var stream = asm.GetManifestResourceStream(matchName);
            if (stream != null)
            {
                int retries = 10;
                while (retries > 0)
                {
                    try
                    {
                        using var fs = File.Create(targetPath);
                        stream.CopyTo(fs);
                        return;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries == 0) throw;
                        Thread.Sleep(300);
                    }
                }
            }
        }
        throw new FileNotFoundException($"Gömülü kaynak bulunamadı: {resourceName}");
    }

    private static void RegisterControlPanelUninstall(string targetDir, string targetExe, string uninstallExe)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);
            if (key == null) return;

            using var appKey = key.CreateSubKey("ETDVol");
            if (appKey != null)
            {
                appKey.SetValue("DisplayName", "ETDVol Audio Control");
                appKey.SetValue("DisplayIcon", targetExe);
                appKey.SetValue("DisplayVersion", "1.0.0");
                appKey.SetValue("Publisher", "Emir Tuğra Dağ");
                appKey.SetValue("HelpLink", "https://github.com/emirtugra-dag/ETDVol");
                appKey.SetValue("URLInfoAbout", "https://github.com/emirtugra-dag/ETDVol");
                appKey.SetValue("URLUpdateInfo", "https://github.com/emirtugra-dag/ETDVol/releases");
                appKey.SetValue("UninstallString", $"\"{uninstallExe}\"");
                appKey.SetValue("InstallLocation", targetDir);
                appKey.SetValue("EstimatedSize", 4096, RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    private static void CreateWScriptShortcut(string shortcutPath, string targetExe, string arguments)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;
        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null) return;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetExe;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe) ?? "";
        shortcut.Save();
    }
}
