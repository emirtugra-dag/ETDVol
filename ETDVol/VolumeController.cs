using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ETDVol;

public class VolumeController
{
    private readonly IMMDeviceEnumerator _enumerator;
    private IMMDevice? _defaultDevice;
    private IAudioEndpointVolume? _endpointVolume;
    
    private DateTime _lastScrollTime = DateTime.MinValue;
    private int _scrollStreak = 0;

    public VolumeController()
    {
        _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        RefreshDefaultDevice();
    }

    public void PreWarm()
    {
        try
        {
            RefreshDefaultDevice();
            GetDefaultDeviceName();
            GetVolumePercent();
        }
        catch { }
    }

    public void RefreshDefaultDevice()
    {
        _defaultDevice = null;
        _endpointVolume = null;
        try
        {
            _enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice dev);
            _defaultDevice = dev;
            if (_defaultDevice != null)
            {
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                _defaultDevice.Activate(ref iid, 23, IntPtr.Zero, out object interfaceObj);
                _endpointVolume = (IAudioEndpointVolume)interfaceObj;
            }
        }
        catch { }
    }

    public string GetDefaultDeviceName()
    {
        if (_defaultDevice == null) return "Unknown Device";
        try
        {
            _defaultDevice.OpenPropertyStore(0, out IPropertyStore store);
            PROPERTYKEY key = new PROPERTYKEY { fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), pid = 14 };
            store.GetValue(ref key, out PROPVARIANT pv);
            string? name = Marshal.PtrToStringUni(pv.pwszVal);
            return name ?? "Unknown Device";
        }
        catch { return "Unknown Device"; }
    }

    public void ChangeVolume(int direction)
    {
        if (_endpointVolume == null) RefreshDefaultDevice();
        try
        {
            if (_endpointVolume == null) return;

            double step = SettingsManager.Current.StepSize;
            if (SettingsManager.Current.EnableAcceleration)
            {
                var now = DateTime.Now;
                if ((now - _lastScrollTime).TotalMilliseconds < 150)
                    _scrollStreak = Math.Min(_scrollStreak + 1, 15);
                else
                    _scrollStreak = 0;
                _lastScrollTime = now;
                
                step += _scrollStreak * 0.5;
            }

            step /= 100.0;
            
            _endpointVolume.GetMasterVolumeLevelScalar(out float currentLevel);
            float newLevel = currentLevel + (float)(direction * step);
            newLevel = Math.Max(0, Math.Min(1, newLevel));
            
            _endpointVolume.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);
        }
        catch { }
    }
    
    public int GetVolumePercent()
    {
        try
        {
            if (_endpointVolume == null) RefreshDefaultDevice();
            if (_endpointVolume == null) return 0;
            _endpointVolume.GetMasterVolumeLevelScalar(out float currentLevel);
            return (int)Math.Round(currentLevel * 100);
        }
        catch { return 0; }
    }

    public void CycleAudioDevice()
    {
        try
        {
            _enumerator.EnumAudioEndpoints(0, 1, out IMMDeviceCollection collection);
            collection.GetCount(out uint count);
            
            var devices = new List<string>();
            string currentId = "";
            
            if (_defaultDevice != null)
                _defaultDevice.GetId(out currentId);

            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out IMMDevice dev);
                dev.GetId(out string id);
                
                if (SettingsManager.Current.EnabledDevices != null && 
                    SettingsManager.Current.EnabledDevices.Count > 0 && 
                    !SettingsManager.Current.EnabledDevices.Contains(id))
                {
                    continue;
                }
                
                devices.Add(id);
            }

            if (devices.Count < 2) return;
            
            int currentIndex = devices.IndexOf(currentId);
            if (currentIndex == -1) currentIndex = 0;
            int nextIndex = (currentIndex + 1) % devices.Count;
            
            var policyConfig = (IPolicyConfig)new PolicyConfigComObject();
            policyConfig.SetDefaultEndpoint(devices[nextIndex], 0);
            policyConfig.SetDefaultEndpoint(devices[nextIndex], 1);
            policyConfig.SetDefaultEndpoint(devices[nextIndex], 2);
            
            RefreshDefaultDevice();
        }
        catch { }
    }

    public List<(string Id, string Name)> GetAudioDevices()
    {
        var list = new List<(string Id, string Name)>();
        try
        {
            _enumerator.EnumAudioEndpoints(0, 1, out IMMDeviceCollection collection);
            collection.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out IMMDevice dev);
                dev.GetId(out string id);
                
                dev.OpenPropertyStore(0, out IPropertyStore store);
                PROPERTYKEY key = new PROPERTYKEY { fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), pid = 14 };
                store.GetValue(ref key, out PROPVARIANT pv);
                string name = Marshal.PtrToStringUni(pv.pwszVal) ?? "Unknown Device";
                
                list.Add((id, name));
            }
        }
        catch { }
        return list;
    }
}
