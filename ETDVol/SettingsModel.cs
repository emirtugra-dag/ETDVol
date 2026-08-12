using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace ETDVol;

public class AppSettings
{
    public string Language { get; set; } = "tr";
    public bool AutoStart { get; set; } = true;
    public double StepSize { get; set; } = 2.0;
    public bool EnableAcceleration { get; set; } = true;
    
    // Hotkeys (Modifiers: 1=Ctrl, 2=Alt, 4=Shift)
    public bool EnableHotkeys { get; set; } = true;
    public int VolUpKey { get; set; } = 38; // Up arrow
    public int VolUpModifiers { get; set; } = 1; // Ctrl
    public int VolDownKey { get; set; } = 40; // Down arrow
    public int VolDownModifiers { get; set; } = 1; // Ctrl
    public int MuteKey { get; set; } = 77; // M key
    public int MuteModifiers { get; set; } = 1; // Ctrl
    public int DeviceKey { get; set; } = 68; // D key
    public int DeviceModifiers { get; set; } = 1; // Ctrl

    public bool EnableOSD { get; set; } = true;
    public int OSDDurationMs { get; set; } = 1500;
    
    public bool EnableTrayIcon { get; set; } = true;

    public System.Collections.Generic.List<string> EnabledDevices { get; set; } = new();
}

public static class SettingsManager
{
    private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETDVol");
    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");
    
    public static AppSettings Current { get; private set; } = new AppSettings();
    public static event Action? OnSettingsSaved;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                // Migration: If old settings file has VolUpModifiers = 3 (Ctrl+Alt from previous defaults), migrate to 1 (Ctrl)
                if (Current.VolUpModifiers == 3 && Current.VolDownModifiers == 3 && Current.MuteModifiers == 3 && Current.DeviceModifiers == 3)
                {
                    Current.VolUpModifiers = 1;
                    Current.VolDownModifiers = 1;
                    Current.MuteModifiers = 1;
                    Current.DeviceModifiers = 1;
                    Save();
                }
            }
        }
        catch { Current = new AppSettings(); }
    }

    public static void Save()
    {
        try
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            RegisterAutoStart(Current.AutoStart);
            OnSettingsSaved?.Invoke();
        }
        catch { }
    }

    private static void RegisterAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

            if (enable && !string.IsNullOrEmpty(exePath))
                key.SetValue("ETDVol", $"\"{exePath}\"");
            else
                key.DeleteValue("ETDVol", false);
        }
        catch { }
    }
}

